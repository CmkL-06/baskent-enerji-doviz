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
        { path: 'exchange',             name: 'Exchange',          component: () => import('@/views/ModernExchange.vue') },
        { path: 'exchange-v2',          name: 'ExchangeV2',        component: () => import('@/views/ModernExchangeV2.vue') },
        { path: 'exchange-rates',       name: 'ExchangeRates',     component: () => import('@/views/ModernExchangeRates.vue') },
        { path: 'history',              name: 'History',           component: () => import('@/views/ModernHistory.vue') },
        { path: 'z-report',             name: 'ZReport',           component: () => import('@/views/ModernZReport.vue') },
        { path: 'z-report-v2',          name: 'ZReportV2',         component: () => import('@/views/ModernZReportV2.vue') },
        { path: 'parties',              name: 'Parties',           component: () => import('@/views/ModernPartyAccounts.vue') },
        { path: 'ghost-party',          name: 'GhostParty',        component: () => import('@/views/ModernGhostParty.vue') },
        { path: 'expenses',             name: 'Expenses',          component: () => import('@/views/ExpensesManagement.vue') },
        { path: 'vaults',               name: 'Vaults',            component: () => import('@/views/VaultView.vue') },
        { path: 'vaults/:id',           name: 'VaultDetail',       component: () => import('@/views/VaultView.vue') },
        { path: 'vault-counts',         name: 'VaultCounts',       component: () => import('@/views/VaultCountManagement.vue') },
        { path: 'vault-snapshot',       name: 'VaultSnapshot',     component: () => import('@/views/VaultSnapshot.vue') },
        { path: 'vault-management',     name: 'VaultManagement',   component: () => import('@/views/VaultManagement.vue') },
        { path: 'users',                name: 'Users',             component: () => import('@/views/ModernUserManagementV2.vue') },
        { path: 'user-offices',         name: 'UserOffices',       component: () => import('@/views/ModernUserOfficeManagement.vue') },
        { path: 'auto-rate-management', name: 'AutoRate',          component: () => import('@/views/AutoRateManagement.vue') },
        { path: 'currencies',           name: 'Currencies',        component: () => import('@/views/CurrencyManagement.vue') },
        { path: 'settings',             name: 'Settings',          component: () => import('@/views/ModernSettings.vue') },
        { path: 'inter-office',         name: 'InterOffice',       component: () => import('@/views/InterOfficeExchange.vue') },
        { path: 'owner-panel',          name: 'OwnerPanel',        component: () => import('@/views/OwnerPanel.vue') },
        { path: 'office-hierarchy',     name: 'OfficeHierarchy',   component: () => import('@/views/OfficeHierarchy.vue') },
        { path: 'office-transfers',     name: 'OfficeTransfers',   component: () => import('@/views/OfficeTransfers.vue') },

      ]
    },
    // Masaüstü (eski) UI
    {
      path: '/desktop',
      name: 'Desktop',
      component: () => import('@/components/Desktop.vue'),
      meta: { requiresAuth: true }
    },
    { path: '/:pathMatch(.*)*', redirect: '/login' }
  ]
})

router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    next('/login')
  } else if (to.path === '/login' && auth.isAuthenticated) {
    next('/ihtiyar/dashboard')
  } else {
    next()
  }
})

export default router
