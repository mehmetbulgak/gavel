type ProblemDetails = {
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly code: string | undefined
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, message: string, code?: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.fieldErrors = fieldErrors
  }
}

const TOO_MANY_REQUESTS = 429
const RATE_LIMITED_MESSAGE = 'Too many attempts. Please wait a minute and try again.'

export async function toApiError(response: Response): Promise<ApiError> {
  const fallback = response.status === TOO_MANY_REQUESTS ? RATE_LIMITED_MESSAGE : `Request failed (${response.status})`

  try {
    const problem = (await response.json()) as ProblemDetails
    const fieldErrors = problem.errors ?? {}
    const firstFieldError = Object.values(fieldErrors)[0]?.[0]
    const title = response.status === TOO_MANY_REQUESTS ? undefined : problem.title
    const message = problem.detail ?? firstFieldError ?? title ?? fallback
    return new ApiError(response.status, message, problem.code, fieldErrors)
  } catch {
    return new ApiError(response.status, fallback)
  }
}

export function describeError(error: unknown) {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.'
}
