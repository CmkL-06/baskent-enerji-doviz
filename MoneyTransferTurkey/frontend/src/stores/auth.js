import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '../api/client'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token'))
    const user = ref(JSON.parse(localStorage.getItem('user') || 'null'))
      const loading = ref(false)
        const error = ref(null)

          const isAuthenticated = computed(() => !!token.value)
            const userName = computed(() => user.value?.fullName || user.value?.email || '')

              function setAuth(newToken, userData) {
                  token.value = newToken
                      user.value = userData
                          if (newToken) {
                                localStorage.setItem('token', newToken)
                                      localStorage.setItem('user', JSON.stringify(userData || {}))
                                          } else {
                                                localStorage.removeItem('token')
                                                      localStorage.removeItem('user')
                                                          }
                                                            }

                                                              async function login(credentials) {
                                                                  loading.value = true
                                                                      error.value = null
                                                                          try {
                                                                                const { data } = await api.post('/api/User/login', credentials)
                                                                                      setAuth(data.token, data.user)
                                                                                            return data
                                                                                                } catch (err) {
                                                                                                      error.value = err.response?.data?.message || 'Login failed'
                                                                                                            throw err
                                                                                                                } finally {
                                                                                                                      loading.value = false
                                                                                                                          }
                                                                                                                            }
                                                                                                                            
                                                                                                                              function logout() {
                                                                                                                                  setAuth(null, null)
                                                                                                                                    }
                                                                                                                                    
                                                                                                                                      return { token, user, loading, error, isAuthenticated, userName, setAuth, login, logout }
                                                                                                                                      })
