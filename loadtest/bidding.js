import http from 'k6/http'
import { check, fail, sleep } from 'k6'
import { Counter, Trend } from 'k6/metrics'

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080'
const AUCTIONS = Number(__ENV.AUCTIONS || 5)
const RUN_ID = __ENV.RUN_ID || `${Date.now()}`
const PASSWORD = __ENV.BIDDER_PASSWORD || `Load-${RUN_ID}-pw1`

const bidLatency = new Trend('bid_latency', true)
const bidsAccepted = new Counter('bids_accepted')
const bidsRejected = new Counter('bids_rejected')

export const options = {
  scenarios: {
    bidders: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '20s', target: Number(__ENV.VUS || 50) },
        { duration: __ENV.HOLD || '1m', target: Number(__ENV.VUS || 50) },
        { duration: '10s', target: 0 },
      ],
      gracefulRampDown: '5s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    bid_latency: ['p(95)<250', 'p(99)<500'],
    checks: ['rate>0.99'],
  },
}

const json = { headers: { 'Content-Type': 'application/json' } }
const authorized = (token) => ({ headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` } })

function register(label) {
  const response = http.post(
    `${BASE_URL}/api/auth/register`,
    JSON.stringify({ displayName: label, email: `${label.toLowerCase().replace(/\s+/g, '-')}-${RUN_ID}@load.test`, password: PASSWORD }),
    json,
  )
  if (response.status !== 200 && response.status !== 201) fail(`registration failed for ${label}: ${response.status} ${response.body}`)
  return response.json('accessToken')
}

export function setup() {
  const token = register('Load Seller')
  const ids = []
  for (let i = 0; i < AUCTIONS; i++) {
    const response = http.post(
      `${BASE_URL}/api/auctions`,
      JSON.stringify({ title: `Load test lot ${i + 1} ${RUN_ID}`, category: 'Collectibles', startingPrice: 100, durationMinutes: 60 }),
      authorized(token),
    )
    if (response.status !== 201) fail(`creating auction failed: ${response.status} ${response.body}`)
    ids.push(response.json('id'))
  }
  return { ids }
}

let token = null

export default function ({ ids }) {
  if (!token) token = register(`Load Bidder ${__VU}`)
  const auctionId = ids[Math.floor(Math.random() * ids.length)]

  const details = http.get(`${BASE_URL}/api/auctions/${auctionId}`, { ...authorized(token), tags: { name: 'GET /api/auctions/{id}' } })
  if (!check(details, { 'auction loads': (r) => r.status === 200 })) return

  const viewer = details.json('viewer')
  const minimum = details.json('minimumNextBid')
  const increment = details.json('increment')
  const floor = viewer && viewer.isLeading ? viewer.maxBid + increment : minimum
  const maxAmount = Math.round((floor + increment * Math.ceil(Math.random() * 6)) * 100) / 100

  const bid = http.post(`${BASE_URL}/api/auctions/${auctionId}/bids`, JSON.stringify({ maxAmount }), {
    ...authorized(token),
    tags: { name: 'POST /api/auctions/{id}/bids' },
    responseCallback: http.expectedStatuses(200, 400),
  })
  bidLatency.add(bid.timings.duration)

  if (bid.status === 200) bidsAccepted.add(1)
  else bidsRejected.add(1)

  check(bid, {
    'bid is accepted or loses a race': (r) => r.status === 200 || (r.status === 400 && /Bid\.(TooLow|MaxNotIncreased)/.test(r.body)),
  })

  sleep(0.2 + Math.random() * 0.8)
}
