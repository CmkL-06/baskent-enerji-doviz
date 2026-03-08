import { describe, it, expect, beforeEach, vi } from 'vitest'

describe('useAuth', () => {
  beforeEach(() => {
    localStorage.clear()
    // useAuth uses module-level refs; reload module to reset state
    vi.resetModules()
  })

  it('isAuthenticated is false when no token', async () => {
    const { useAuth: useAuthFresh } = await import('./useAuth')
    const { isAuthenticated } = useAuthFresh()
    expect(isAuthenticated.value).toBe(false)
  })

  it('setAuth stores token and user in localStorage', async () => {
    const { useAuth: useAuthFresh } = await import('./useAuth')
    const { setAuth, isAuthenticated, user } = useAuthFresh()
    setAuth('jwt-123', { email: 'a@b.com', userName: 'test' })
    expect(isAuthenticated.value).toBe(true)
    expect(localStorage.getItem('token')).toBe('jwt-123')
    expect(JSON.parse(localStorage.getItem('user'))).toEqual({ email: 'a@b.com', userName: 'test' })
    expect(user.value).toEqual({ email: 'a@b.com', userName: 'test' })
  })

  it('logout clears token and user', async () => {
    const { useAuth: useAuthFresh } = await import('./useAuth')
    const { setAuth, logout, isAuthenticated } = useAuthFresh()
    setAuth('jwt-123', { email: 'a@b.com' })
    expect(isAuthenticated.value).toBe(true)
    logout()
    expect(isAuthenticated.value).toBe(false)
    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('user')).toBeNull()
  })

  it('setAuth with null clears storage', async () => {
    const { useAuth: useAuthFresh } = await import('./useAuth')
    const { setAuth } = useAuthFresh()
    setAuth('x', {})
    setAuth(null, null)
    expect(localStorage.getItem('token')).toBeNull()
  })
})
