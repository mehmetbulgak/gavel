import { BookOpen, Camera, Gem, Guitar, Lamp, Palette, Shirt, ToyBrick, Trophy, Watch, type LucideIcon } from 'lucide-react'
import type { AuctionCategory } from '@/lib/api/types'
import { cn } from '@/lib/cn'

type Art = { icon: LucideIcon; surface: string; mark: string }

const art: Record<AuctionCategory, Art> = {
  Electronics: { icon: Camera, surface: 'bg-[#e3e6ff]', mark: 'text-[#2636e6]' },
  Watches: { icon: Watch, surface: 'bg-[#f3e7d3]', mark: 'text-[#8a5a12]' },
  Fashion: { icon: Shirt, surface: 'bg-[#f6dfe8]', mark: 'text-[#a3285a]' },
  Home: { icon: Lamp, surface: 'bg-[#dcefe6]', mark: 'text-[#16704e]' },
  Collectibles: { icon: Gem, surface: 'bg-[#e9e0f7]', mark: 'text-[#5b3aa8]' },
  Art: { icon: Palette, surface: 'bg-[#ffe3d9]', mark: 'text-[#b33a17]' },
  Music: { icon: Guitar, surface: 'bg-[#dfeef7]', mark: 'text-[#1d5f86]' },
  Sports: { icon: Trophy, surface: 'bg-[#e6f0d5]', mark: 'text-[#4d6b14]' },
  Books: { icon: BookOpen, surface: 'bg-[#efe8dd]', mark: 'text-[#6b4f2a]' },
  Toys: { icon: ToyBrick, surface: 'bg-[#fdf0c9]', mark: 'text-[#8a6a00]' },
}

const hash = (value: string) => [...value].reduce((total, char) => (total * 31 + char.charCodeAt(0)) >>> 0, 7)

type LotArtProps = { id: string; category: AuctionCategory; className?: string; size?: 'card' | 'hero' }

export function LotArt({ id, category, className, size = 'card' }: LotArtProps) {
  const { icon: Icon, surface, mark } = art[category]
  const seed = hash(id)
  const rotation = (seed % 4) * 22.5
  const spacing = 14 + (seed % 3) * 4
  const patternId = `lot-${id}`

  return (
    <div className={cn('relative overflow-hidden', surface, mark, className)} aria-hidden>
      <svg className="absolute inset-0 size-full opacity-25" preserveAspectRatio="none">
        <defs>
          <pattern id={patternId} width={spacing} height={spacing} patternUnits="userSpaceOnUse" patternTransform={`rotate(${rotation})`}>
            <circle cx={spacing / 2} cy={spacing / 2} r="1.6" fill="currentColor" />
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill={`url(#${patternId})`} />
      </svg>
      <Icon className={cn('absolute', size === 'hero' ? '-right-6 -bottom-8 size-64' : '-right-3 -bottom-4 size-32')} strokeWidth={1.25} />
    </div>
  )
}
