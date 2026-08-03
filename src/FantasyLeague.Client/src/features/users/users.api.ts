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
    apiClient<User>('/users/sign-in', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  create: (request: CreateUserRequest) =>
    apiClient<User>('/users', { method: 'POST', body: JSON.stringify(request) }),
}
