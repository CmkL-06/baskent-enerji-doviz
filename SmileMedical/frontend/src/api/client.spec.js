import { describe, it, expect, beforeEach, vi } from 'vitest'

// Mock axios create to get our instance; we'll test interceptors by using the real api module
// but with mocked localStorage and optional window.location
describe('api client', () => {
  beforeEach(() => {
    vi.resetModules()
    localStorage.clear()
  })

  it('request interceptor adds Authorization header when token exists', async () => {
    localStorage.setItem('token', 'bearer-token-123')
    const { api } = await import('./client')
    let capturedConfig
    await api.get('/any', {
      adapter: (config) => {
        capturedConfig = config
        return Promise.resolve({ status: 200, data: {}, config, headers: config.headers })
      },
    })
    expect(capturedConfig.headers.Authorization).toBe('Bearer bearer-token-123')
  })

  it('request interceptor does not add Authorization when no token', async () => {
    const { api } = await import('./client')
    let capturedConfig
    await api.get('/any', {
      adapter: (config) => {
        capturedConfig = config
        return Promise.resolve({ status: 200, data: {}, config, headers: config.headers })
      },
    })
    expect(capturedConfig.headers.Authorization).toBeUndefined()
  })

  it('on 401 response clears token from localStorage', async () => {
    localStorage.setItem('token', 'x')
    const { api } = await import('./client')
    await expect(
      api.get('/any', {
        adapter: () => Promise.reject({ response: { status: 401 } }),
      })
    ).rejects.toMatchObject({ response: { status: 401 } })
    expect(localStorage.getItem('token')).toBeNull()
  })
})
