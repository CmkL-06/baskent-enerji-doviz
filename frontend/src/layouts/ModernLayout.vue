<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import LanguageSelector from '@/components/common/LanguageSelector.vue'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()

// Ref for router-view to access child component methods
const routerViewRef = ref<any>(null)

// Open USDT Modal
const openUSDTModal = () => {
  // Emit an event to open the modal
  window.dispatchEvent(new CustomEvent('open-usdt-modal'))
}

// Mobile menu state
const isMobileMenuOpen = ref(false)
const isSidebarOpen = ref(false)
const isUserMenuOpen = ref(false)

// All navigation items
const allNavItems = computed(() => [
  { id: 'home', icon: 'dashboard', label: t('navbar.dashboard'), category: 'GENEL', path: '/ihtiyar/dashboard', adminOnly: false },
  { id: 'z-report', icon: 'insert_chart', label: t('navbar.zReport'), category: 'RAPORLAR', path: '/ihtiyar/z-report', adminOnly: true },
  // { id: 'z-report-v2', icon: 'analytics', label: t('navbar.zReportV2'), category: 'RAPORLAR', path: '/ihtiyar/z-report-v2', adminOnly: true, isNew: true },
  { id: 'exchange-v2', icon: 'paid', label: t('navbar.exchange'), category: 'İŞLEMLER', path: '/ihtiyar/exchange-v2', adminOnly: false },
  { id: 'parties', icon: 'contacts', label: t('navbar.parties'), category: 'İŞLEMLER', path: '/ihtiyar/parties', adminOnly: false },
  // { id: 'ghost-party', icon: 'auto_awesome', label: t('navbar.ghostParty'), category: 'İŞLEMLER', path: '/ihtiyar/ghost-party', adminOnly: true },
  { id: 'expenses', icon: 'receipt_long', label: t('navbar.expenses'), category: 'İŞLEMLER', path: '/ihtiyar/expenses', adminOnly: false },
  { id: 'vaults', icon: 'account_balance_wallet', label: t('navbar.vaults'), category: 'İŞLEMLER', path: '/ihtiyar/vaults', adminOnly: false },
  { id: 'users', icon: 'manage_accounts', label: t('navbar.users'), category: 'YÖNETİM', path: '/ihtiyar/users', adminOnly: true },
  { id: 'vault-counts', icon: 'inventory_2', label: 'Kasa Sayımları', category: 'YÖNETİM', path: '/ihtiyar/vault-counts', adminOnly: true },
  { id: 'vault-snapshot', icon: 'photo_camera', label: 'Kasa Snapshot', category: 'YÖNETİM', path: '/ihtiyar/vault-snapshot', adminOnly: true },
  { id: 'auto-rate-management', icon: 'currency_exchange', label: 'Otomatik Kur Yönetimi', category: 'YÖNETİM', path: '/ihtiyar/auto-rate-management', adminOnly: true },
  { id: 'currencies', icon: 'payments', label: 'Para Birimleri', category: 'YÖNETİM', path: '/ihtiyar/currencies', adminOnly: true },
  { id: 'user-offices', icon: 'admin_panel_settings', label: 'Yetkilendirme', category: 'YÖNETİM', path: '/ihtiyar/user-offices', adminOnly: true },
  { id: 'settings', icon: 'tune', label: t('navbar.settings'), category: 'YÖNETİM', path: '/ihtiyar/settings', adminOnly: false },
  { id: 'owner-panel',       icon: 'crown',           label: 'Owner Panel',        category: 'OWNER', path: '/ihtiyar/owner-panel',       adminOnly: false, ownerOnly: true },
  { id: 'office-hierarchy', icon: 'account_tree',    label: 'Ofis Hiyerarşisi',   category: 'OWNER', path: '/ihtiyar/office-hierarchy',  adminOnly: false, ownerOnly: true },
  { id: 'office-transfers', icon: 'swap_horiz',      label: 'Ofislerarası Transfer', category: 'YÖNETİM', path: '/ihtiyar/office-transfers', adminOnly: true, ownerOnly: false },
])

// Helper function to get nav item by id
const getNavItem = (id: string) => allNavItems.value.find(item => item.id === id)

// Filtered navigation items based on user role
const topNavItems = computed(() => {
  return allNavItems.value.filter(item => {
    if (item.ownerOnly) return authStore.isOwner
    if (item.adminOnly) return authStore.isAdmin
    return true
  })
})

const leftSidebarItems = ref([
  { id: 'general', label: 'GENEL', active: true },
  { id: 'reports', label: 'RAPORLAR', active: false },
  { id: 'transactions', label: 'İŞLEMLER', active: false }
])

// Hide quick actions on exchange pages
const showQuickActions = computed(() => {
  const path = route.path
  return !path.includes('/exchange') && !path.includes('/exchange-v2')
})

const quickActions = computed(() => [
  { id: 'buy-currency', label: 'DÖVİZ ALIMI YAP', path: '/ihtiyar/exchange-v2?type=buy&currency=fiat' },
  { id: 'sell-currency', label: 'DÖVİZ SATIŞI YAP', path: '/ihtiyar/exchange-v2?type=sell&currency=fiat' },
  { id: 'card-ruble-buy', label: 'KART RUBLE ALIMI YAP', path: '/ihtiyar/exchange-v2?type=buy&currency=krub' },
  { id: 'card-ruble-sell', label: 'KART RUBLE SATIŞI YAP', path: '/ihtiyar/exchange-v2?type=sell&currency=krub' },
  { id: 'usdt-buy', label: 'USDT ALIMI YAP', path: '/ihtiyar/exchange-v2?type=buy&currency=usdt' },
  { id: 'usdt-sell', label: 'USDT SATIŞI YAP', path: '/ihtiyar/exchange-v2?type=sell&currency=usdt' }
])

const currentRoute = ref('dashboard')
const tickerText = ref('Kurlar yükleniyor...')
let tickerInterval: number | null = null

// Ticker visibility state - default is false (hidden)
const showTicker = ref(false)

// Party related data for sidebar
const parties = ref<any[]>([])
const partyTotals = ref({
  totalReceivables: 0,
  totalDebts: 0,
  netBalance: 0
})

// Load exchange rates and build ticker text
const loadExchangeRates = async () => {
  try {
    // Don't clear existing rates while loading new ones
    const previousRates = exchangeStore.exchangeRates

    // Don't call loadExchangeRates if we don't have offices loaded yet
    // For non-admin users, ensure we have their offices first
    if (!authStore.isAdmin && authStore.user?.id && exchangeStore.offices.length === 0) {
      // Load user's offices first
      await exchangeStore.loadOffices()
    }

    await exchangeStore.loadExchangeRates()

    // Only update if we got valid rates
    if (exchangeStore.exchangeRates && exchangeStore.exchangeRates.length > 0) {
      updateTickerText()
    } else if (previousRates && previousRates.length > 0) {
      // Restore previous rates if new load failed
      exchangeStore.exchangeRates = previousRates
    }
  } catch (error) {
    console.error('Failed to load exchange rates:', error)
    // Don't change existing ticker if load failed
    if (!tickerText.value || tickerText.value === 'Kurlar yükleniyor...') {
      tickerText.value = 'Kur bilgileri yüklenemedi'
    }
  }
}

// Helper to get country code for currency
const getCurrencyCountryCode = (currencyCode: string): string => {
  const countryMap: Record<string, string> = {
    'USD': 'us',
    'EUR': 'eu',
    'GBP': 'gb',
    'JPY': 'jp',
    'CHF': 'ch',
    'AUD': 'au',
    'CAD': 'ca',
    'CNY': 'cn',
    'RUB': 'ru',
    'AED': 'ae',
    'SAR': 'sa',
    'KWD': 'kw',
    'NOK': 'no',
    'SEK': 'se',
    'DKK': 'dk',
    'PLN': 'pl',
    'HUF': 'hu',
    'CZK': 'cz',
    'BGN': 'bg',
    'RON': 'ro',
    'ZAR': 'za',
    'KRW': 'kr',
    'SGD': 'sg',
    'HKD': 'hk',
    'INR': 'in',
    'MXN': 'mx',
    'BRL': 'br',
    'NZD': 'nz',
    'THB': 'th',
    'MYR': 'my',
    'PHP': 'ph',
    'IDR': 'id'
  }
  return countryMap[currencyCode] || ''
}

// Ticker rates for HTML rendering
const tickerRates = computed(() => {
  const rates = exchangeStore.exchangeRates
  if (!rates || rates.length === 0) return []

  // Get unique rates by source currency code and office
  const uniqueRatesMap = new Map()

  rates
    .filter(rate => rate && rate.targetCurrencyCode === 'TRY' && rate.isActive !== false && rate.buyRate && rate.sellRate)
    .forEach(rate => {
      // Create unique key with currency and office
      const key = `${rate.sourceCurrencyCode}-${rate.officeId || 'default'}`
      if (!uniqueRatesMap.has(key)) {
        uniqueRatesMap.set(key, {
          code: rate.sourceCurrencyCode,
          buyRate: rate.buyRate.toFixed(2),
          sellRate: rate.sellRate.toFixed(2),
          countryCode: getCurrencyCountryCode(rate.sourceCurrencyCode),
          officeId: rate.officeId
        })
      }
    })

  return Array.from(uniqueRatesMap.values())
})

// Update ticker text from exchange rates
const updateTickerText = () => {
  const rates = exchangeStore.exchangeRates
  const selectedOffice = exchangeStore.selectedOffice

  if (!rates || rates.length === 0) {
    tickerText.value = 'Kur bilgisi bulunamadı'
    return
  }

  // Get office name to display
  const officeName = selectedOffice?.officeName || exchangeStore.offices[0]?.officeName || ''
  const officePrefix = officeName ? `[${officeName}] ` : ''

  // Build ticker text from exchange rates with better formatting
  const uniqueRatesMap = new Map()

  rates
    .filter(rate =>
      rate &&
      rate.targetCurrencyCode === 'TRY' &&
      rate.isActive !== false &&
      rate.sellRate
    )
    .forEach(rate => {
      // Use the first rate for each currency if there are duplicates
      if (!uniqueRatesMap.has(rate.sourceCurrencyCode)) {
        uniqueRatesMap.set(rate.sourceCurrencyCode, rate)
      }
    })

  const validRates = Array.from(uniqueRatesMap.values())

  if (validRates.length === 0) {
    tickerText.value = 'Kur bilgisi yok'
    return
  }

  const rateTexts = validRates
    .map(rate => {
      return `${rate.sourceCurrencyCode}: ${rate.sellRate.toFixed(2)} ₺`
    })
    .join('   •   ')

  // Add office name and repeat the text for continuous scrolling
  const fullText = `${officePrefix}${rateTexts}`
  tickerText.value = `${fullText}   •   ${fullText}   •   ${fullText}`
}

// Categories for navigation
const navigationCategories = ['GENEL', 'RAPORLAR', 'İŞLEMLER', 'YÖNETİM']

// Get items for each category
const getCategoryItems = (category: string) => {
  return topNavItems.value.filter(item => item.category === category)
}

const activeNav = computed(() => {
  return topNavItems.value.find(item => item.id === currentRoute.value) || topNavItems.value[0]
})

const isOnExchangePage = computed(() => {
  return route.path.includes('/exchange') && !route.path.includes('exchange-v2')
})

const isOnVaultsPage = computed(() => {
  return route.path.includes('/vaults')
})

const isOnZReportPage = computed(() => {
  return route.path.includes('z-report')
})

const isOnDashboard = computed(() => {
  return route.path === '/ihtiyar/dashboard' || route.path === '/ihtiyar'
})

const shouldShowSidebar = computed(() => true)

// Get current page name for sidebar content
const currentPageName = computed(() => {
  const path = route.path
  if (path.includes('dashboard')) return 'dashboard'
  if (path.includes('z-report')) return 'z-report'
  if (path.includes('exchange')) return 'exchange'
  if (path.includes('ghost-party')) return 'ghost-party'
  if (path.includes('parties')) return 'parties'
  if (path.includes('expenses')) return 'expenses'
  if (path.includes('vaults')) return 'vaults'
  if (path.includes('users')) return 'users'
  if (path.includes('currencies')) return 'currencies'
  if (path.includes('auto-rate-management')) return 'auto-rate-management'
  if (path.includes('settings')) return 'settings'
  return 'dashboard'
})

// New computed: nav groups for sidebar
const navGroups = computed(() => {
  const cats = ['GENEL', 'İŞLEMLER', 'RAPORLAR', 'YÖNETİM']
  if (authStore.isOwner) cats.push('OWNER')
  return cats
    .map(cat => ({ cat, items: getCategoryItems(cat) }))
    .filter(g => g.items.length > 0)
})

// New computed: page title for top header
const pageTitle = computed(() => {
  const match = topNavItems.value.find(item =>
    route.path === item.path || route.path.startsWith(item.path + '/')
  )
  return match?.label ?? 'Dashboard'
})

// New computed: whether to show quick sidebar
const hasQuickSidebar = computed(() =>
  ['dashboard', 'z-report', 'parties', 'vaults', 'exchange'].includes(currentPageName.value)
)

const navigateTo = (item: any) => {
  currentRoute.value = item.id
  router.push(item.path)
}

const setSidebarTab = (item: any) => {
  leftSidebarItems.value.forEach(i => i.active = false)
  item.active = true
}

const handleQuickAction = (action: any) => {
  if (!action.path) return

  // If navigating to exchange with different type, force reload
  if (action.path.includes('/exchange?type=')) {
    const currentPath = router.currentRoute.value.fullPath
    if (currentPath.includes('/exchange') && currentPath !== action.path) {
      // Force page reload by using window.location
      window.location.href = action.path
      return
    }
  }

  router.push(action.path)
}

const logout = () => {
  // Close dropdown
  isUserMenuOpen.value = false

  // Clear ALL localStorage data
  localStorage.clear()

  // Clear session storage too
  sessionStorage.clear()

  // Call auth store logout
  authStore.logout()

  // Navigate to login
  router.push('/')
}

const toggleMobileMenu = () => {
  isMobileMenuOpen.value = !isMobileMenuOpen.value
}

const toggleSidebar = () => {
  isSidebarOpen.value = !isSidebarOpen.value
}

// Z-Report actions - call the component's print function
const handlePrintReport = async () => {
  // Try to call the printReport method on the child component
  if (routerViewRef.value?.printReport) {
    await routerViewRef.value.printReport()
  } else {
    // Fallback to clicking the actual print button in the page
    const printButton = document.querySelector('.btn-print, button[class*="print"]')
    if (printButton instanceof HTMLElement) {
      printButton.click()
    } else {
      console.warn('Print function not available')
    }
  }
}

const handleCreateVault = () => {
  // Navigate to vault management page with create query param
  router.push({ name: 'VaultManagement', query: { create: 'true' } })
}

// Party-related methods
const loadPartyData = async () => {
  if (!exchangeStore.selectedOffice?.officeId) return

  try {
    const response = await apiService.getParties(exchangeStore.selectedOffice.officeId)
    parties.value = response

    // Calculate totals
    let receivables = 0
    let debts = 0

    response.forEach((party: any) => {
      party.accounts?.forEach((account: any) => {
        if (account.balance > 0) {
          receivables += account.balance
        } else if (account.balance < 0) {
          debts += Math.abs(account.balance)
        }
      })
    })

    partyTotals.value = {
      totalReceivables: receivables,
      totalDebts: debts,
      netBalance: receivables - debts
    }
  } catch (error) {
    console.error('Failed to load party data:', error)
  }
}

const handleCreateParty = () => {
  // Navigate to parties page and emit event to open modal
  router.push('/ihtiyar/parties')
  // After navigation, trigger the modal opening
  setTimeout(() => {
    const event = new CustomEvent('openCreatePartyModal')
    window.dispatchEvent(event)
  }, 100)
}

const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('tr-TR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(amount)
}

const handleFilterReceivables = () => {
  const event = new CustomEvent('filterParties', { detail: 'receivables' });
  window.dispatchEvent(event);
}

const handleFilterDebts = () => {
  const event = new CustomEvent('filterParties', { detail: 'debts' });
  window.dispatchEvent(event);
}

const handleFilterAll = () => {
  const event = new CustomEvent('filterParties', { detail: 'all' });
  window.dispatchEvent(event);
}

const handleEndOfDay = async () => {
  // If we have a ref to the router view component, use its closeDay function
  if (routerViewRef.value?.performEndOfDay) {
    await routerViewRef.value.performEndOfDay()
  } else {
    // Fallback to the old implementation
    const selectedOffice = exchangeStore.selectedOffice || exchangeStore.offices[0]

    if (!selectedOffice) {
      alert('Lütfen bir ofis seçiniz!')
      return
    }

    if (!confirm('Gün sonu işlemini yapmak istediğinizden emin misiniz?')) {
      return
    }

    try {
      await apiService.endDay(selectedOffice.officeId)
      alert('Gün sonu işlemi başarıyla tamamlandı!')
      router.push('/ihtiyar/vaults')
    } catch (error) {
      console.error('Gün sonu işlemi başarısız:', error)
      alert('Gün sonu işlemi başarısız oldu. Lütfen tekrar deneyiniz.')
    }
  }
}

// Handle keyboard shortcuts
const handleKeyPress = (e: KeyboardEvent) => {
  // Ctrl+P or Cmd+P for print
  if ((e.ctrlKey || e.metaKey) && e.key === 'p' && isOnZReportPage.value) {
    e.preventDefault()
    handlePrintReport()
  }
}

// Watch route changes to load party data when on parties page
watch(() => route.path, async (newPath) => {
  if (newPath.includes('/parties')) {
    await loadPartyData()
  }
})

// Watch office selection changes
watch(() => exchangeStore.selectedOffice, async () => {
  if (route.path.includes('/parties')) {
    await loadPartyData()
  }
})

// Update party totals when parties component updates them
const updatePartyTotals = (event: CustomEvent) => {
  if (event.detail) {
    partyTotals.value = event.detail
  }
}

// Initialize on mount
onMounted(async () => {
  // Load ticker preference from localStorage
  const savedTickerPreference = localStorage.getItem('showTicker')
  showTicker.value = savedTickerPreference === 'true' // Only show if explicitly set to 'true'

  await loadExchangeRates()
  // Refresh rates every 60 seconds (instead of 30)
  tickerInterval = setInterval(loadExchangeRates, 60000) as unknown as number
  // Add keyboard listener
  window.addEventListener('keydown', handleKeyPress)
  // Add party totals update listener
  window.addEventListener('updatePartyTotals', updatePartyTotals as any)

  // Load party data if on parties page
  if (route.path.includes('/parties')) {
    await loadPartyData()
  }
})

// Cleanup on unmount
onUnmounted(() => {
  if (tickerInterval) {
    clearInterval(tickerInterval)
  }
  window.removeEventListener('keydown', handleKeyPress)
  window.removeEventListener('updatePartyTotals', updatePartyTotals as any)
})

const reloadPage = () => window.location.reload()
</script>

<template>
  <div class="layout-root">

    <!-- ── Left Nav Sidebar ───────────────────────────── -->
    <aside class="nav-sidebar" :class="{ 'is-open': isMobileMenuOpen }">

      <!-- Brand -->
      <div class="nav-brand">
        <div class="brand-icon">
          <span class="material-symbols-outlined">currency_exchange</span>
        </div>
        <span class="brand-name">Exchange Office</span>
      </div>

      <!-- Navigation -->
      <nav class="nav-menu">
        <template v-for="group in navGroups" :key="group.cat">
          <div class="nav-group-label">{{ group.cat }}</div>
          <router-link
            v-for="item in group.items"
            :key="item.id"
            :to="item.path"
            class="nav-link"
            active-class="is-active"
            @click="isMobileMenuOpen = false"
          >
            <span class="material-symbols-outlined nav-link-icon">{{ item.icon }}</span>
            <span class="nav-link-text">{{ item.label }}</span>
            <span v-if="(item as any).isNew" class="nav-badge">YENİ</span>
          </router-link>
        </template>
      </nav>

      <!-- Footer -->
      <div class="nav-footer">
        <LanguageSelector class="nav-lang" />
        <button class="nav-user-btn" @click="isUserMenuOpen = !isUserMenuOpen">
          <div class="nav-avatar">{{ (authStore.user?.firstname || authStore.user?.username || '?')[0].toUpperCase() }}</div>
          <div class="nav-user-info">
            <span class="nav-user-name">{{ authStore.user?.firstname || authStore.user?.username }}</span>
            <span class="nav-user-role">
              <span v-if="authStore.isOwner">👑 Owner</span>
              <span v-else-if="authStore.isAdmin">Admin</span>
              <span v-else>Personel</span>
            </span>
          </div>
          <span class="material-symbols-outlined nav-user-chevron">expand_more</span>
        </button>

        <!-- User dropdown (inside sidebar footer) -->
        <div v-if="isUserMenuOpen" class="user-dropdown">
          <div class="ud-header">
            <div class="ud-avatar">{{ (authStore.user?.firstname || '?')[0].toUpperCase() }}</div>
            <div>
              <div class="ud-name">{{ authStore.user?.firstname }} {{ authStore.user?.lastname }}</div>
              <div class="ud-username">@{{ authStore.user?.username }}</div>
            </div>
          </div>
          <div class="ud-info">
            <div class="ud-row"><span class="material-symbols-outlined">mail</span>{{ authStore.user?.mail }}</div>
          </div>
          <button class="ud-logout" @click="logout">
            <span class="material-symbols-outlined">power_settings_new</span>
            {{ t('navbar.logout') }}
          </button>
        </div>
      </div>
    </aside>

    <!-- Mobile overlay -->
    <div v-if="isMobileMenuOpen" class="nav-overlay" @click="isMobileMenuOpen = false" />

    <!-- ── Right Area ──────────────────────────────────── -->
    <div class="right-area">

      <!-- Top Header -->
      <header class="top-header">
        <button class="hamburger" @click="toggleMobileMenu">
          <span class="material-symbols-outlined">menu</span>
        </button>
        <h1 class="header-title">{{ pageTitle }}</h1>
        <div class="header-actions">
          <button
            class="header-icon-btn"
            :class="{ active: showTicker }"
            @click="showTicker = !showTicker; localStorage.setItem('showTicker', String(showTicker))"
            title="Kur Ticker'ı"
          >
            <span class="material-symbols-outlined">show_chart</span>
          </button>
          <button class="header-user-btn" @click="isUserMenuOpen = !isUserMenuOpen">
            <div class="header-avatar">{{ (authStore.user?.firstname || '?')[0].toUpperCase() }}</div>
            <span class="header-user-name">{{ authStore.user?.firstname }}</span>
            <span class="material-symbols-outlined" style="font-size:16px">expand_more</span>
          </button>
        </div>
      </header>

      <!-- Ticker -->
      <div v-if="showTicker" class="ticker-bar">
        <div class="ticker-inner">
          <div class="ticker-track">
            <template v-for="rep in 3" :key="rep">
              <span v-if="exchangeStore.selectedOffice" class="ticker-office">{{ exchangeStore.selectedOffice.officeName }}</span>
              <template v-for="(rate, i) in tickerRates" :key="`${rep}-${i}`">
                <span class="ticker-item">
                  <i v-if="rate.countryCode" :class="`fi fi-${rate.countryCode}`"></i>
                  <span v-else-if="rate.code === 'USDT'">₮</span>
                  {{ rate.code }}:
                  <span class="ticker-buy">A:{{ rate.buyRate }}</span>
                  <span class="ticker-sell">S:{{ rate.sellRate }}</span> ₺
                </span>
                <span class="ticker-sep">•</span>
              </template>
            </template>
          </div>
        </div>
      </div>

      <!-- Body: Quick sidebar + Content -->
      <div class="body-area">

        <!-- Quick Sidebar (desktop only, contextual) -->
        <aside v-if="hasQuickSidebar" class="quick-sidebar">
          <div class="qs-header">
            <span class="qs-title">
              <span v-if="currentPageName === 'dashboard'">Hızlı İşlemler</span>
              <span v-else-if="currentPageName === 'z-report'">Rapor</span>
              <span v-else-if="currentPageName === 'parties'">Cari İşlemleri</span>
              <span v-else-if="currentPageName === 'vaults'">Kasa İşlemleri</span>
              <span v-else>İşlemler</span>
            </span>
          </div>

          <div class="qs-body">
            <!-- Dashboard actions -->
            <div v-if="currentPageName === 'dashboard'" class="qs-group">
              <button class="qs-btn primary" @click="router.push('/ihtiyar/z-report')">
                <span class="material-symbols-outlined">analytics</span>Z Raporu
              </button>
              <button class="qs-btn" @click="router.push('/ihtiyar/exchange-v2')">
                <span class="material-symbols-outlined">currency_exchange</span>Döviz İşlemi
              </button>
              <button class="qs-btn" @click="router.push('/ihtiyar/parties')">
                <span class="material-symbols-outlined">groups</span>Cariler
              </button>
              <button class="qs-btn" @click="router.push('/ihtiyar/expenses')">
                <span class="material-symbols-outlined">payments</span>Giderler
              </button>
            </div>

            <!-- Z-Report actions -->
            <div v-if="currentPageName === 'z-report'" class="qs-group">
              <button class="qs-btn primary" @click="handlePrintReport">
                <span class="material-symbols-outlined">print</span>Yazdır
              </button>
            </div>

            <!-- Exchange page actions -->
            <div v-if="currentPageName === 'exchange'" class="qs-group">
              <button v-if="authStore.isAdmin" class="qs-btn primary" @click="router.push({ name: 'ExchangeRates' })">
                <span class="material-symbols-outlined">settings</span>Kur Yönetimi
              </button>
              <button class="qs-btn usdt" @click="openUSDTModal">
                <span style="font-weight:800">₮</span>USDT Ödemeleri
              </button>
              <template v-if="showQuickActions">
                <div class="qs-divider"></div>
                <button
                  v-for="action in quickActions"
                  :key="action.id"
                  class="qs-btn"
                  :class="action.id.includes('buy') ? 'buy' : 'sell'"
                  @click="handleQuickAction(action)"
                >
                  <div v-if="action.id.includes('usdt')" style="width:18px;height:18px">
                    <svg viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="12" fill="#26A17B"/><path d="M13.2 10.5V9.3H17.4V7.2H6.6V9.3H10.8V10.5C7.8 10.65 5.4 11.25 5.4 12C5.4 12.75 7.8 13.35 10.8 13.5V18.6H13.2V13.5C16.2 13.35 18.6 12.75 18.6 12C18.6 11.25 16.2 10.65 13.2 10.5Z" fill="white"/></svg>
                  </div>
                  <div v-else-if="action.id.includes('card-ruble')" class="flex gap-1 items-center">
                    <span class="material-symbols-outlined" style="font-size:16px">credit_card</span>
                    <i class="fi fi-ru" style="font-size:11px"></i>
                  </div>
                  <span v-else class="material-symbols-outlined" style="font-size:18px">
                    {{ action.id.includes('buy') ? 'add_circle' : 'remove_circle' }}
                  </span>
                  {{ action.label }}
                </button>
              </template>
            </div>

            <!-- Parties actions -->
            <div v-if="currentPageName === 'parties'" class="qs-group">
              <button class="qs-btn primary" @click="handleCreateParty()">
                <span class="material-symbols-outlined">person_add</span>Yeni Cari Ekle
              </button>
              <div class="qs-divider"></div>
              <div class="qs-stat-cards">
                <div class="qs-stat green" @click="handleFilterReceivables">
                  <span class="material-symbols-outlined">trending_up</span>
                  <div><div class="qs-stat-label">Toplam Alacak</div><div class="qs-stat-val">₺{{ formatCurrency(partyTotals.totalReceivables) }}</div></div>
                </div>
                <div class="qs-stat" :class="partyTotals.totalDebts > 0 ? 'red' : 'gray'" @click="handleFilterDebts">
                  <span class="material-symbols-outlined">trending_down</span>
                  <div><div class="qs-stat-label">Toplam Borç</div><div class="qs-stat-val">₺{{ formatCurrency(partyTotals.totalDebts) }}</div></div>
                </div>
                <div class="qs-stat" :class="partyTotals.netBalance >= 0 ? 'indigo' : 'red'" @click="handleFilterAll">
                  <span class="material-symbols-outlined">balance</span>
                  <div><div class="qs-stat-label">Net Bakiye</div><div class="qs-stat-val">₺{{ formatCurrency(partyTotals.netBalance) }}</div></div>
                </div>
              </div>
            </div>

            <!-- Vaults actions -->
            <div v-if="currentPageName === 'vaults' && authStore.isAdmin" class="qs-group">
              <button class="qs-btn primary" @click="handleCreateVault()">
                <span class="material-symbols-outlined">add_circle</span>Yeni Kasa
              </button>
            </div>
          </div>
        </aside>

        <!-- Main content -->
        <main class="main-content">
          <router-view v-slot="{ Component }">
            <component :is="Component" ref="routerViewRef" />
          </router-view>
        </main>

      </div><!-- /body-area -->
    </div><!-- /right-area -->

  </div>
</template>

<style scoped>
/* ═══ Root Layout ═══════════════════════════════════ */
.layout-root {
  display: flex;
  height: 100vh;
  overflow: hidden;
  background: #f8fafc;
}

/* ═══ Nav Sidebar ════════════════════════════════════ */
.nav-sidebar {
  width: 230px;
  background: #fff;
  border-right: 1px solid #e5e7eb;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  height: 100vh;
  overflow: hidden;
  z-index: 40;
}

/* Brand */
.nav-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 18px 16px;
  border-bottom: 1px solid #f3f4f6;
  flex-shrink: 0;
}
.brand-icon {
  width: 34px; height: 34px;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  border-radius: 9px;
  display: flex; align-items: center; justify-content: center;
}
.brand-icon .material-symbols-outlined { color: #fff; font-size: 18px; }
.brand-name {
  font-size: 14px; font-weight: 700; color: #111; letter-spacing: -0.3px;
}

/* Nav menu */
.nav-menu {
  flex: 1;
  overflow-y: auto;
  padding: 12px 10px;
  scrollbar-width: thin;
  scrollbar-color: #e5e7eb transparent;
}
.nav-menu::-webkit-scrollbar { width: 4px; }
.nav-menu::-webkit-scrollbar-thumb { background: #e5e7eb; border-radius: 2px; }

.nav-group-label {
  font-size: 10px; font-weight: 700; color: #9ca3af;
  text-transform: uppercase; letter-spacing: .6px;
  padding: 10px 8px 4px; margin-top: 4px;
}
.nav-group-label:first-child { margin-top: 0; }

.nav-link {
  display: flex; align-items: center; gap: 10px;
  padding: 8px 10px;
  border-radius: 8px;
  font-size: 13px; font-weight: 500; color: #4b5563;
  text-decoration: none;
  transition: background .12s, color .12s;
  position: relative;
  margin-bottom: 1px;
}
.nav-link:hover { background: #f3f4f6; color: #111; }
.nav-link.is-active {
  background: #eef2ff; color: #4f46e5; font-weight: 600;
}
.nav-link.is-active .nav-link-icon { color: #4f46e5; }
.nav-link-icon { font-size: 18px; color: #9ca3af; flex-shrink: 0; transition: color .12s; }
.nav-link-text { flex: 1; }
.nav-badge {
  font-size: 9px; font-weight: 700; background: #ef4444; color: #fff;
  padding: 1px 5px; border-radius: 4px; letter-spacing: .3px;
}

/* Footer */
.nav-footer {
  border-top: 1px solid #f3f4f6;
  padding: 12px 10px;
  flex-shrink: 0;
  position: relative;
}
.nav-lang { margin-bottom: 8px; }

.nav-user-btn {
  display: flex; align-items: center; gap: 8px;
  width: 100%; padding: 8px 10px;
  border: none; background: transparent;
  border-radius: 9px; cursor: pointer;
  text-align: left; transition: background .12s;
}
.nav-user-btn:hover { background: #f3f4f6; }
.nav-avatar {
  width: 30px; height: 30px; border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: #fff; font-size: 13px; font-weight: 700;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.nav-user-info { flex: 1; min-width: 0; }
.nav-user-name { display: block; font-size: 13px; font-weight: 600; color: #111; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.nav-user-role { display: block; font-size: 11px; color: #9ca3af; }
.nav-user-chevron { font-size: 16px; color: #9ca3af; }

/* User dropdown */
.user-dropdown {
  position: absolute; bottom: calc(100% + 4px); left: 10px; right: 10px;
  background: #fff; border: 1px solid #e5e7eb; border-radius: 12px;
  box-shadow: 0 8px 24px rgba(0,0,0,.12); z-index: 100; overflow: hidden;
}
.ud-header {
  display: flex; align-items: center; gap: 12px;
  padding: 16px; background: linear-gradient(135deg, #6366f1, #8b5cf6);
}
.ud-avatar {
  width: 42px; height: 42px; border-radius: 50%;
  background: rgba(255,255,255,.25); color: #fff; font-size: 16px; font-weight: 700;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.ud-name { font-size: 14px; font-weight: 700; color: #fff; }
.ud-username { font-size: 12px; color: rgba(255,255,255,.8); }
.ud-info { padding: 12px 16px; }
.ud-row { display: flex; align-items: center; gap: 8px; font-size: 12px; color: #6b7280; }
.ud-row .material-symbols-outlined { font-size: 16px; color: #9ca3af; }
.ud-logout {
  display: flex; align-items: center; gap: 8px; justify-content: center;
  width: 100%; padding: 10px 16px;
  border: none; background: #fef2f2; color: #dc2626;
  font-size: 13px; font-weight: 600; cursor: pointer;
  border-top: 1px solid #e5e7eb;
}
.ud-logout .material-symbols-outlined { font-size: 16px; }
.ud-logout:hover { background: #fee2e2; }

/* Mobile sidebar */
@media (max-width: 1023px) {
  .nav-sidebar {
    position: fixed; left: 0; top: 0;
    transform: translateX(-100%);
    transition: transform .25s ease;
    box-shadow: 4px 0 20px rgba(0,0,0,.15);
  }
  .nav-sidebar.is-open { transform: translateX(0); }
}

/* ═══ Mobile overlay ═════════════════════════════════ */
.nav-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.4);
  z-index: 39; display: none;
}
@media (max-width: 1023px) {
  .nav-overlay { display: block; }
}

/* ═══ Right Area ══════════════════════════════════════ */
.right-area {
  flex: 1; display: flex; flex-direction: column; overflow: hidden;
}

/* Top Header */
.top-header {
  height: 56px; display: flex; align-items: center; gap: 12px;
  padding: 0 20px;
  background: #fff; border-bottom: 1px solid #e5e7eb;
  flex-shrink: 0; z-index: 10;
}
.hamburger {
  display: none; padding: 6px; border: none; background: transparent;
  border-radius: 8px; cursor: pointer; color: #374151;
}
.hamburger:hover { background: #f3f4f6; }
.hamburger .material-symbols-outlined { font-size: 22px; }
@media (max-width: 1023px) { .hamburger { display: flex; } }
.header-title {
  flex: 1; font-size: 15px; font-weight: 700; color: #111;
}
.header-actions { display: flex; align-items: center; gap: 8px; }
.header-icon-btn {
  width: 34px; height: 34px; display: flex; align-items: center; justify-content: center;
  border: none; background: transparent; border-radius: 8px; cursor: pointer; color: #6b7280;
}
.header-icon-btn:hover { background: #f3f4f6; color: #111; }
.header-icon-btn.active { background: #eef2ff; color: #6366f1; }
.header-icon-btn .material-symbols-outlined { font-size: 20px; }
.header-user-btn {
  display: flex; align-items: center; gap: 8px;
  padding: 5px 10px; border: 1px solid #e5e7eb; border-radius: 20px;
  background: transparent; cursor: pointer; font-size: 13px; font-weight: 500; color: #374151;
}
.header-user-btn:hover { background: #f3f4f6; }
.header-avatar {
  width: 26px; height: 26px; border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: #fff; font-size: 11px; font-weight: 700;
  display: flex; align-items: center; justify-content: center;
}
.header-user-name { font-size: 13px; }
@media (max-width: 640px) { .header-user-name { display: none; } }

/* Ticker */
.ticker-bar { background: #0f172a; color: #fbbf24; overflow: hidden; height: 34px; flex-shrink: 0; }
.ticker-inner { width: 100%; height: 100%; overflow: hidden; display: flex; align-items: center; }
.ticker-track {
  display: inline-flex; align-items: center; gap: 12px;
  white-space: nowrap; font-size: 12px; font-weight: 500;
  animation: ticker 60s linear infinite;
}
.ticker-office { background: #d97706; color: #fff; padding: 2px 8px; border-radius: 4px; font-weight: 700; font-size: 11px; }
.ticker-item { display: inline-flex; align-items: center; gap: 4px; }
.ticker-buy { color: #4ade80; }
.ticker-sell { color: #f87171; }
.ticker-sep { color: #475569; }
@keyframes ticker { from { transform: translateX(0); } to { transform: translateX(-33.333%); } }

/* ═══ Body Area ══════════════════════════════════════ */
.body-area {
  flex: 1; display: flex; overflow: hidden;
}

/* Quick Sidebar */
.quick-sidebar {
  width: 260px; background: #fff; border-right: 1px solid #e5e7eb;
  display: flex; flex-direction: column; flex-shrink: 0; overflow-y: auto;
}
@media (max-width: 1023px) { .quick-sidebar { display: none; } }
.qs-header {
  padding: 14px 16px; border-bottom: 1px solid #f3f4f6;
}
.qs-title { font-size: 12px; font-weight: 700; color: #6b7280; text-transform: uppercase; letter-spacing: .5px; }
.qs-body { padding: 12px; display: flex; flex-direction: column; gap: 6px; }
.qs-group { display: flex; flex-direction: column; gap: 6px; }
.qs-divider { height: 1px; background: #f3f4f6; margin: 4px 0; }

.qs-btn {
  display: flex; align-items: center; gap: 10px;
  padding: 9px 12px; border: none; border-radius: 9px;
  background: #f9fafb; color: #374151;
  font-size: 13px; font-weight: 500; cursor: pointer; text-align: left;
  transition: background .12s, transform .12s;
}
.qs-btn:hover { background: #f3f4f6; transform: translateX(3px); }
.qs-btn .material-symbols-outlined { font-size: 18px; color: #9ca3af; }
.qs-btn.primary { background: linear-gradient(135deg, #6366f1, #8b5cf6); color: #fff; }
.qs-btn.primary .material-symbols-outlined { color: #fff; }
.qs-btn.primary:hover { box-shadow: 0 4px 12px rgba(99,102,241,.35); transform: translateX(3px); }
.qs-btn.usdt { background: linear-gradient(135deg, #26A17B, #1E8E66); color: #fff; font-weight: 700; }
.qs-btn.buy { background: #f0fdf4; color: #166534; }
.qs-btn.buy .material-symbols-outlined { color: #22c55e; }
.qs-btn.sell { background: #fef2f2; color: #991b1b; }
.qs-btn.sell .material-symbols-outlined { color: #ef4444; }

/* Party stat cards */
.qs-stat-cards { display: flex; flex-direction: column; gap: 8px; }
.qs-stat {
  display: flex; align-items: center; gap: 10px;
  padding: 10px 12px; border-radius: 10px; cursor: pointer;
  transition: transform .12s;
  border-left: 3px solid transparent;
}
.qs-stat:hover { transform: translateX(3px); }
.qs-stat .material-symbols-outlined { font-size: 20px; }
.qs-stat-label { font-size: 10px; font-weight: 700; text-transform: uppercase; color: #9ca3af; letter-spacing: .4px; }
.qs-stat-val { font-size: 15px; font-weight: 800; }
.qs-stat.green { background: #f0fdf4; border-color: #22c55e; }
.qs-stat.green .material-symbols-outlined { color: #22c55e; }
.qs-stat.green .qs-stat-val { color: #166534; }
.qs-stat.red { background: #fef2f2; border-color: #ef4444; }
.qs-stat.red .material-symbols-outlined { color: #ef4444; }
.qs-stat.red .qs-stat-val { color: #991b1b; }
.qs-stat.gray { background: #f9fafb; border-color: #d1d5db; }
.qs-stat.gray .material-symbols-outlined { color: #9ca3af; }
.qs-stat.gray .qs-stat-val { color: #6b7280; }
.qs-stat.indigo { background: #eef2ff; border-color: #6366f1; }
.qs-stat.indigo .material-symbols-outlined { color: #6366f1; }
.qs-stat.indigo .qs-stat-val { color: #4f46e5; }

/* ═══ Main Content ══════════════════════════════════ */
.main-content {
  flex: 1; overflow-y: auto; padding: 20px 24px;
}
@media (max-width: 768px) { .main-content { padding: 12px 16px; } }

/* Material symbols */
.material-symbols-outlined {
  font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
</style>
