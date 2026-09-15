import axios from 'axios'

const API_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:8080/api').replace(/\/+$/, '')

const client = axios.create({ baseURL: API_URL, headers: { 'Content-Type': 'application/json' } })

client.interceptors.request.use((config) => {
  const token = localStorage.getItem('cloudops.accessToken')
  const expiresAt = localStorage.getItem('cloudops.expiresAtUtc')
  const hasExpiredToken = expiresAt && new Date(expiresAt).getTime() <= Date.now() + 30000

  if (hasExpiredToken) {
    localStorage.removeItem('cloudops.accessToken')
    localStorage.removeItem('cloudops.expiresAtUtc')
    localStorage.removeItem('cloudops.user')
    return config
  }

  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

export class ApiError extends Error { constructor(message: string, public readonly status: number) { super(message) } }

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  try {
    const response = await client.request<T>({
      url: path,
      method: options.method,
      data: options.body,
      headers: options.headers as Record<string, string> | undefined
    })
    return response.data
  } catch (reason) {
    if (axios.isAxiosError(reason)) {
      if (reason.response?.status === 401) {
        localStorage.removeItem('cloudops.accessToken')
        localStorage.removeItem('cloudops.expiresAtUtc')
        localStorage.removeItem('cloudops.user')
      }

      const body = reason.response?.data as { detail?: string; title?: string } | undefined
      const message = reason.code === 'ERR_NETWORK'
        ? `Unable to reach the CloudOps API at ${API_URL}. Start the ASP.NET API and try again.`
        : body?.detail ?? body?.title ?? reason.message ?? 'Request failed.'
      throw new ApiError(message, reason.response?.status ?? 0)
    }
    throw reason
  }
}
