import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL || ''

export const api = axios.create({
  baseURL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token')
      const base = import.meta.env.BASE_URL || '/'
      const normalizedBase = base.endsWith('/') ? base : `${base}/`
      window.location.href = `${normalizedBase}login`
    }
    return Promise.reject(err)
  }
)

export default api
