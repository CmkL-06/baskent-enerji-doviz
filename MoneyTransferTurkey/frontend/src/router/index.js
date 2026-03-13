import { createRouter, createWebHistory } from 'vue-router'
import { useAuth } from '../composables/useAuth'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/LoginView.vue'),
    meta: { public: true },
  },
  {
    path: '/',
    component: () => import('../layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: { name: 'Dashboard' } },
      { path: 'dashboard', name: 'Dashboard', component: () => import('../views/DashboardView.vue') },
      { path: 'user', name: 'User', component: () => import('../views/UserView.vue') },
      { path: 'exchange', name: 'Exchange', component: () => import('../views/ExchangeView.vue') },
      { path: 'vault', name: 'Vault', component: () => import('../views/VaultView.vue') },
      { path: 'party', name: 'Party', component: () => import('../views/PartyView.vue') },
      { path: 'rates', name: 'Rates', component: () => import('../views/RatesView.vue') },
      { path: 'site', name: 'Site', component: () => import('../views/SiteView.vue') },
      { path: 'blog', name: 'Blog', component: () => import('../views/BlogView.vue') },
      { path: 'coin', name: 'Coin', component: () => import('../views/CoinView.vue') },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

router.beforeEach((to, _from, next) => {
  const { token } = useAuth()
  const isPublic = to.matched.some((r) => r.meta.public)
  if (!isPublic && !token.value) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
    return
  }
  if (to.name === 'Login' && token.value) {
    next({ name: 'Dashboard' })
    return
  }
  next()
})

export default router
