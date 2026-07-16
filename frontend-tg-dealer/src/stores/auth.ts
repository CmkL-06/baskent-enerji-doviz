import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import router from '@/router'
import apiService from '@/services/apiservice'

export const useAuthStore = defineStore('auth', () => {

  const token      = ref<string | null>(localStorage.getItem('token'))
  let _parsedUser: any = null
  try { _parsedUser = JSON.parse(localStorage.getItem('user') || 'null') } catch { localStorage.removeItem('user') }
  const user       = ref<any | null>(_parsedUser)
  const isLoading  = ref(false)
  const error      = ref<string | null>(null)
  const userOffices = ref<any[]>([])

  const isAuthenticated = computed(() => !!token.value)
  const isOwner = computed(() => {
    const rank = user.value?.rank
    return rank >= 100
  })
  const isAdmin = computed(() => {
    const rank = user.value?.rank
    return rank >= 99 || user.value?.isAdmin === true
  })
  const isModerator = computed(() => {
    const rank = user.value?.rank
    return rank >= 50
  })

  async function login(credentials: { mail: string; password: string }) {
    isLoading.value = true
    error.value = null
    try {
      const res = await apiService.login(credentials)
      const jwt  = res.apiToken ?? res.token
      const info = res.userInfo ?? res.user
      if (!jwt || !info) throw new Error('Geçersiz sunucu yanıtı')
      token.value = jwt
      user.value  = info
      localStorage.setItem('token', jwt)
      localStorage.setItem('user', JSON.stringify(info))
      localStorage.setItem('uiStyle', 'modern')
      if (info?.officeId) localStorage.setItem('selectedOfficeId', info.officeId)
      await loadUserOffices()
      await router.push('/dealer')
    } catch (err: any) {
      error.value = err.response?.data?.message || err.message || 'Giriş başarısız'
      throw err
    } finally {
      isLoading.value = false
    }
  }

  function logout() {
    token.value = null
    user.value  = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    router.push('/login')
  }

  async function loadUserOffices() {
    try {
      const data = await apiService.getMyOfficeAccess()
      userOffices.value = Array.isArray(data) ? data : []
    } catch {
      userOffices.value = []
    }
  }

  function initialize() {
    const t = localStorage.getItem('token')
    const u = localStorage.getItem('user')
    if (t && u) {
      token.value = t
      try { user.value = JSON.parse(u) } catch { localStorage.removeItem('user'); user.value = null }
    }
  }

  function getOfficeRole(officeId: string | undefined | null): string | null {
    if (!officeId) return null
    return userOffices.value.find((o: any) => o.officeId === officeId)?.role ?? null
  }

  function isViewerForOffice(officeId: string | undefined | null): boolean {
    if (isAdmin.value) return false
    return getOfficeRole(officeId) === 'Viewer'
  }

  return { token, user, isLoading, error, isAuthenticated, isOwner, isAdmin, isModerator, userOffices, login, logout, initialize, loadUserOffices, getOfficeRole, isViewerForOffice }
})
