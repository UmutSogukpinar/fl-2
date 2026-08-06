import { apiClient } from '../../shared/api/client'
import type { User } from './types'

export interface CreateUserRequest {
  username: string
  email: string
  password: string
  location: string
}

export const usersApi = {
  getById: (id: string, signal?: AbortSignal) =>
    apiClient<User>(`/users/${id}`, { signal }),
  signIn: (email: string, password: string) =>
    apiClient<User>('/auth/sign-in', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
      skipAuthRefresh: true,
    }),
  create: (request: CreateUserRequest) =>
    apiClient<User>('/users', {
      method: 'POST',
      body: JSON.stringify(request),
      skipAuthRefresh: true,
    }),
  refresh: (signal?: AbortSignal) =>
    apiClient<User>('/auth/refresh', {
      method: 'POST',
      signal,
      skipAuthRefresh: true,
    }),
  signOut: () =>
    apiClient<void>('/auth/sign-out', {
      method: 'POST',
      skipAuthRefresh: true,
    }),
}
