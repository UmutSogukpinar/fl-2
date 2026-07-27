const API_URL = import.meta.env.VITE_API_URL ?? '/api'

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message)
  }
}

export async function apiClient<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options?.headers },
  })
  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(
      problem?.detail ?? problem?.title ?? texts.errors.requestFailed,
      response.status,
    )
  }
  return response.json() as Promise<T>
}
import { texts } from '../constants/texts'
