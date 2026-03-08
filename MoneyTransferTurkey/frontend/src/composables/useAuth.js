import { ref, computed } from 'vue'

const token = ref(localStorage.getItem('token'))
const user = ref(JSON.parse(localStorage.getItem('user') || 'null'))

export function useAuth() {
  const isAuthenticated = computed(() => !!token.value)

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

  function logout() {
    setAuth(null, null)
  }

  return { token, user, isAuthenticated, setAuth, logout }
}
