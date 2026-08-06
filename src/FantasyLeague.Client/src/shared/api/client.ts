import { texts } from '../constants/texts'

const API_URL = import.meta.env.VITE_API_URL ?? '/api'
const AUTH_EXPIRED_EVENT = 'auth:expired'

type ApiRequestOptions = RequestInit & {
  skipAuthRefresh?: boolean
}

let refreshRequest: Promise<void> | null = null

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message)
  }
}

async function request(path: string, options?: RequestInit) {
  if (options?.signal?.aborted) {
    throw new DOMException('The request was aborted.', 'AbortError')
  }

  try {
    return await fetch(`${API_URL}${path}`, {
      ...options,
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', ...options?.headers },
    })
  } catch (error) {
    if (options?.signal?.aborted) {
      throw new DOMException('The request was aborted.', 'AbortError')
    }
    throw error
  }
}

async function parseResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(
      problem?.detail ?? problem?.title ?? texts.errors.requestFailed,
      response.status,
    )
  }

  const responseBody = await response.text()
  return responseBody ? JSON.parse(responseBody) as T : undefined as T
}

async function refreshSession() {
  if (!refreshRequest) {
    refreshRequest = request('/auth/refresh', { method: 'POST' })
      .then(async (response) => {
        if (!response.ok) {
          throw new ApiError(texts.errors.requestFailed, response.status)
        }
      })
      .finally(() => {
        refreshRequest = null
      })
  }

  return refreshRequest
}

export async function apiClient<T>(
  path: string,
  options?: ApiRequestOptions,
): Promise<T> {
  const { skipAuthRefresh = false, ...requestOptions } = options ?? {}
  let response = await request(path, requestOptions)

  if (response.status === 401 && !skipAuthRefresh) {
    try {
      await refreshSession()
      response = await request(path, requestOptions)
    } catch (error) {
      if (requestOptions.signal?.aborted) throw error
      window.dispatchEvent(new Event(AUTH_EXPIRED_EVENT))
    }
  }

  return parseResponse<T>(response)
}

export function onAuthExpired(callback: () => void) {
  window.addEventListener(AUTH_EXPIRED_EVENT, callback)
  return () => window.removeEventListener(AUTH_EXPIRED_EVENT, callback)
}
