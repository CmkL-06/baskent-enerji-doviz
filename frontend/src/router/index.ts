import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/login'
    },
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/Login.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/ihtiyar',
      component: () => import('@/layouts/ModernLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: '', redirect: '/ihtiyar/dashboard' },
        { path: 'dashboard',            name: 'Dashboard',         component: () => import('@/views/ModernDashboard.vue') },
        { path: 'exchange',             redirect: '/ihtiyar/exchange-v2' },
        { path: 'exchange-v2',          name: 'ExchangeV2',        component: () => import('@/views/ModernExchangeV2.vue') },
        { path: 'exchange-rates',       name: 'ExchangeRates',     component: () => import('@/views/ModernExchangeRates.vue') },
        { path: 'history',              redirect: '/ihtiyar/z-report' },
        { path: 'z-report',             name: 'ZReport',           component: () => import('@/views/ModernZReport.vue') },
        { path: 'z-report-v2',          redirect: '/ihtiyar/z-report' },
        { path: 'parties',              name: 'Parties',           component: () => import('@/views/ModernPartyAccounts.vue') },
        { path: 'ghost-party',          redirect: '/ihtiyar/parties' },
        { path: 'expenses',             name: 'Expenses',          component: () => import('@/views/ExpensesManagement.vue') },
        { path: 'vaults/:id?',          name: 'Vaults',            component: () => import('@/views/VaultView.vue') },
        { path: 'vault-counts',         redirect: '/ihtiyar/vaults' },
        { path: 'vault-snapshot',       redirect: '/ihtiyar/vaults' },
        { path: 'vault-management',     redirect: '/ihtiyar/vaults' },
        { path: 'users',                name: 'Users',             component: () => import('@/views/ModernUserManagementV2.vue'), meta: { adminOnly: true } },
        { path: 'user-offices',         name: 'UserOffices',       redirect: { name: 'Users' } },
        { path: 'auto-rate-management', name: 'AutoRate',          component: () => import('@/views/AutoRateManagement.vue'), meta: { adminOnly: true } },
        { path: 'currencies',           name: 'Currencies',        component: () => import('@/views/CurrencyManagement.vue'), meta: { adminOnly: true } },
        { path: 'settings',             name: 'Settings',          component: () => import('@/views/ModernSettings.vue') },
        { path: 'inter-office',         redirect: '/ihtiyar/dashboard' },
        { path: 'owner-panel',          name: 'OwnerPanel',        component: () => import('@/views/OwnerPanel.vue'), meta: { ownerOnly: true } },
        { path: 'office-transfers',     redirect: '/ihtiyar/owner-panel' },

        // Telegram MTT
        { path: 'tg-admin',             name: 'TgAdmin',           component: () => import('@/views/Telegram/TgAdminPanel.vue'), meta: { adminOnly: true } },
        { path: 'tg-dealer',            name: 'TgDealer',          component: () => import('@/views/Telegram/TgDealerPanel.vue') },
        { path: 'tg-operator',          name: 'TgOperator',        component: () => import('@/views/Telegram/TgOperatorPanel.vue') },
      ]
    },
    { path: '/desktop', redirect: '/ihtiyar/dashboard' },
    { path: '/:pathMatch(.*)*', redirect: '/login' }
  ]
})

router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    next('/login')
  } else if (to.path === '/login' && auth.isAuthenticated) {
    next('/ihtiyar/dashboard')
  } else if (to.meta.ownerOnly && !auth.isOwner) {
    next('/ihtiyar/dashboard')
  } else if (to.meta.adminOnly && !auth.isAdmin) {
    next('/ihtiyar/dashboard')
  } else {
    next()
  }
})

export default router
