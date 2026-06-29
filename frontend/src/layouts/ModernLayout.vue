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
const clickDomSelector = (selector: string) => {
  const el = document.querySelector(selector) as HTMLElement
  el?.click()
}
</script>

<template>
  <div class="h-screen bg-gray-50 flex flex-col">
    <!-- Top Navigation Bar -->
    <nav class="bg-white border-b border-gray-200 shadow-sm flex-shrink-0">
      <!-- Mobile Header -->
      <div class="lg:hidden flex items-center justify-between p-4">
        <button 
          @click="toggleMobileMenu"
          class="p-2 rounded-lg hover:bg-gray-100"
        >
          <span class="material-symbols-outlined">menu</span>
        </button>
        
        <h1 class="text-lg font-bold text-gray-800">Exchange Office</h1>
        
        <div class="flex items-center gap-2">
          <button 
            @click="toggleSidebar"
            class="p-2 rounded-lg hover:bg-gray-100"
          >
            <span class="material-symbols-outlined">dashboard</span>
          </button>
          <button class="p-2 rounded-lg hover:bg-gray-100" @click="logout">
            <span class="material-symbols-outlined">account_circle</span>
          </button>
        </div>
      </div>
      
      <!-- Desktop Navigation -->
      <div class="hidden lg:flex items-center justify-between px-6 py-4">
        <div class="nav-items flex items-start">
          <!-- GENEL Section -->
          <div class="nav-section">
            <div class="nav-group">
              <router-link
                :to="getNavItem('home')?.path || '/ihtiyar/dashboard'"
                class="nav-item"
                :class="{ active: currentRoute === 'home' }"
                @click.left="currentRoute = 'home'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('home')?.icon }}</span>
                <div class="nav-label">{{ t('navbar.dashboard').toUpperCase() }}</div>
              </router-link>
            </div>
            <div class="nav-category-label">GENEL</div>
          </div>
          
          <div class="nav-divider"></div>
          
          <!-- RAPORLAR Section -->
          <div v-if="authStore.isAdmin" class="nav-section">
            <div class="nav-group">
              <router-link
                :to="getNavItem('z-report')?.path || '/ihtiyar/z-report'"
                class="nav-item"
                :class="{ active: currentRoute === 'z-report' }"
                @click.left="currentRoute = 'z-report'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('z-report')?.icon }}</span>
                <div class="nav-label">{{ t('navbar.zReport').toUpperCase() }}</div>
              </router-link>
              <!-- Z Report V2 - Hidden
              <div
                class="nav-item relative"
                :class="{ active: currentRoute === 'z-report-v2' }"
                @click="navigateTo(getNavItem('z-report-v2'))"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('z-report-v2')?.icon }}</span>
                <div class="nav-label">{{ t('navbar.zReportV2').toUpperCase() }}</div>
                <span class="absolute -top-1 -right-1 px-2 py-0.5 bg-red-600 text-white text-xs font-bold rounded-full animate-pulse flex items-center gap-0.5">
                  <span class="material-symbols-outlined text-xs">warning</span>
                  DOKUNMA
                </span>
              </div>
              -->
            </div>
            <div class="nav-category-label">RAPORLAR</div>
          </div>
          
          <div class="nav-divider"></div>
          
          <!-- İŞLEMLER Section -->
          <div class="nav-section">
            <div class="nav-group">
              <!-- Exchange V2 - Main Exchange -->
              <router-link
                :to="getNavItem('exchange-v2')?.path || '/ihtiyar/exchange-v2'"
                class="nav-item"
                :class="{ active: currentRoute === 'exchange-v2' }"
                @click.left="currentRoute = 'exchange-v2'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('exchange-v2')?.icon }}</span>
                <div class="nav-label">{{ t('navbar.exchange').toUpperCase() }}</div>
              </router-link>
              <!-- Parties - Now visible to all users -->
              <router-link
                :to="getNavItem('parties')?.path || '/ihtiyar/parties'"
                class="nav-item relative"
                :class="{ active: currentRoute === 'parties' }"
                @click.left="currentRoute = 'parties'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('parties')?.icon }}</span>
                <div class="nav-label">{{ t('navbar.parties').toUpperCase() }}</div>
              </router-link>
              <!-- Ghost Party - Hidden
              <div v-if="authStore.isAdmin"
                class="nav-item relative"
                :class="{ active: currentRoute === 'ghost-party' }"
                @click="navigateTo(getNavItem('ghost-party'))"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('ghost-party')?.icon }}</span>
                <div class="nav-label">{{ t('navbar.ghostParty').toUpperCase() }}</div>
              </div>
              -->
              
              <!-- Expenses - Always visible -->
              <router-link
                :to="getNavItem('expenses')?.path || '/ihtiyar/expenses'"
                class="nav-item"
                :class="{ active: currentRoute === 'expenses' }"
                @click.left="currentRoute = 'expenses'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('expenses')?.icon }}</span>
                <div class="nav-label">{{ t('navbar.expenses').toUpperCase() }}</div>
              </router-link>
          
              <!-- Vaults - Always visible -->
              <router-link
                :to="getNavItem('vaults')?.path || '/ihtiyar/vaults'"
                class="nav-item"
                :class="{ active: currentRoute === 'vaults' }"
                @click.left="currentRoute = 'vaults'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('vaults')?.icon }}</span>
                <div class="nav-label">{{ t('navbar.vaults').toUpperCase() }}</div>
              </router-link>
            </div>
            <div class="nav-category-label">İŞLEMLER</div>
          </div>
          
          <div class="nav-divider"></div>
          
          <!-- YÖNETİM Section -->
          <div class="nav-section">
            <div class="nav-group">
              <router-link v-if="authStore.isAdmin"
                :to="getNavItem('users')?.path || '/ihtiyar/users'"
                class="nav-item"
                :class="{ active: currentRoute === 'users' }"
                @click.left="currentRoute = 'users'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('users')?.icon }}</span>
                <div class="nav-label">{{ t('navbar.users').toUpperCase() }}</div>
              </router-link>
              <router-link v-if="authStore.isAdmin"
                :to="getNavItem('vault-counts')?.path || '/ihtiyar/vault-counts'"
                class="nav-item"
                :class="{ active: currentRoute === 'vault-counts' }"
                @click.left="currentRoute = 'vault-counts'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('vault-counts')?.icon }}</span>
                <div class="nav-label">KASA SAYIMLARI</div>
              </router-link>
              <router-link v-if="authStore.isAdmin"
                :to="getNavItem('currencies')?.path || '/ihtiyar/currencies'"
                class="nav-item"
                :class="{ active: currentRoute === 'currencies' }"
                @click.left="currentRoute = 'currencies'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('currencies')?.icon }}</span>
                <div class="nav-label">PARA BİRİMLERİ</div>
              </router-link>
               <router-link v-if="authStore.isAdmin"
                :to="getNavItem('vault-snapshot')?.path || '/ihtiyar/vault-snapshot'"
                class="nav-item"
                :class="{ active: currentRoute === 'vault-snapshot' }"
                @click.left="currentRoute = 'vault-snapshot'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('vault-snapshot')?.icon }}</span>
                <div class="nav-label">KASA KAYITLARI</div>
              </router-link>
              <router-link v-if="authStore.isAdmin"
                :to="getNavItem('auto-rate-management')?.path || '/ihtiyar/auto-rate-management'"
                class="nav-item"
                :class="{ active: currentRoute === 'auto-rate-management' }"
                @click.left="currentRoute = 'auto-rate-management'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('auto-rate-management')?.icon }}</span>
                <div class="nav-label">OTOMATİK KUR</div>
              </router-link>

              <router-link v-if="authStore.isAdmin"
                :to="getNavItem('office-transfers')?.path || '/ihtiyar/office-transfers'"
                class="nav-item"
                :class="{ active: currentRoute === 'office-transfers' }"
                @click.left="currentRoute = 'office-transfers'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('office-transfers')?.icon }}</span>
                <div class="nav-label">ŞUBE TRANSFER</div>
              </router-link>

              <router-link
                :to="getNavItem('settings')?.path || '/ihtiyar/settings'"
                class="nav-item"
                :class="{ active: currentRoute === 'settings' }"
                @click.left="currentRoute = 'settings'"
              >
                <span class="nav-icon material-symbols-outlined">{{ getNavItem('settings')?.icon }}</span>
                <div class="nav-label">{{ t('navbar.settings').toUpperCase() }}</div>
              </router-link>
            </div>
            <div class="nav-category-label">YÖNETİM</div>
          </div>

          <!-- OWNER Section -->
          <div v-if="authStore.isOwner" class="nav-section">
            <div class="nav-group">
              <router-link
                :to="getNavItem('owner-panel')?.path || '/ihtiyar/owner-panel'"
                class="nav-item nav-item-owner"
                :class="{ active: currentRoute === 'owner-panel' }"
                @click.left="currentRoute = 'owner-panel'"
              >
                <span class="nav-icon material-symbols-outlined">crown</span>
                <div class="nav-label">OWNER PANEL</div>
              </router-link>
              <router-link
                :to="getNavItem('office-hierarchy')?.path || '/ihtiyar/office-hierarchy'"
                class="nav-item nav-item-owner"
                :class="{ active: currentRoute === 'office-hierarchy' }"
                @click.left="currentRoute = 'office-hierarchy'"
              >
                <span class="nav-icon material-symbols-outlined">account_tree</span>
                <div class="nav-label">OFİS HİYERARŞİSİ</div>
              </router-link>
            </div>
            <div class="nav-category-label nav-category-owner">👑 OWNER</div>
          </div>
        </div>

        <div class="flex items-center gap-3">
          <!-- Language Selector -->
          <LanguageSelector />
          
          <!-- User Dropdown -->
          <div class="relative">
            <button 
              @click="isUserMenuOpen = !isUserMenuOpen"
              class="user-menu-btn"
            >
              <div class="user-avatar">
                <span class="material-symbols-outlined">person</span>
              </div>
              <span class="text-sm font-medium hidden sm:inline">{{ authStore.user?.firstname || 'Kullanıcı' }}</span>
              <span class="material-symbols-outlined text-sm">{{ isUserMenuOpen ? 'expand_less' : 'expand_more' }}</span>
            </button>
            
            <!-- Dropdown Menu -->
            <div 
              v-if="isUserMenuOpen"
              class="absolute right-0 mt-2 w-80 bg-white rounded-xl shadow-2xl border border-gray-200 z-50 overflow-hidden"
            >
              <!-- User Profile Card -->
              <div class="bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 p-6 text-white">
                <div class="flex items-center gap-4">
                  <div class="w-16 h-16 bg-white/20 backdrop-blur-xl rounded-full flex items-center justify-center border-2 border-white/30">
                    <span class="material-symbols-outlined text-3xl">account_circle</span>
                  </div>
                  <div class="flex-1">
                    <h3 class="font-bold text-lg">{{ authStore.user?.firstname }} {{ authStore.user?.lastname }}</h3>
                    <p class="text-sm opacity-90">@{{ authStore.user?.username }}</p>
                  </div>
                </div>
              </div>
              
              <!-- User Info -->
              <div class="p-4 space-y-3">
                <div class="flex items-center gap-3 text-sm">
                  <span class="material-symbols-outlined text-indigo-500 text-xl">mail</span>
                  <div class="flex-1">
                    <p class="text-gray-500 text-xs">E-posta</p>
                    <p class="text-gray-800 font-medium">{{ authStore.user?.mail }}</p>
                  </div>
                </div>
                
                <div class="flex items-center gap-3 text-sm">
                  <span class="material-symbols-outlined text-purple-500 text-xl">badge</span>
                  <div class="flex-1">
                    <p class="text-gray-500 text-xs">Kullanıcı Adı</p>
                    <p class="text-gray-800 font-medium">{{ authStore.user?.username }}</p>
                  </div>
                </div>
                
                <div class="flex items-center gap-3 text-sm">
                  <span class="material-symbols-outlined text-green-500 text-xl">shield</span>
                  <div class="flex-1">
                    <p class="text-gray-500 text-xs">Yetki Seviyesi</p>
                    <p class="text-gray-800 font-medium">
                      <span v-if="authStore.isOwner" class="px-2 py-1 bg-yellow-100 text-yellow-700 rounded-md text-xs font-semibold">👑 Owner</span>
                      <span v-else-if="authStore.isAdmin" class="px-2 py-1 bg-red-100 text-red-700 rounded-md text-xs font-semibold">Admin</span>
                      <span v-else-if="authStore.isModerator" class="px-2 py-1 bg-blue-100 text-blue-700 rounded-md text-xs font-semibold">Moderatör</span>
                      <span v-else class="px-2 py-1 bg-gray-100 text-gray-700 rounded-md text-xs font-semibold">Kullanıcı</span>
                    </p>
                  </div>
                </div>
              </div>
              
              <!-- Logout Button -->
              <div class="border-t border-gray-200 p-4">
                <button 
                  @click="logout"
                  class="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-gradient-to-r from-red-500 to-pink-500 text-white rounded-lg hover:from-red-600 hover:to-pink-600 transition-all font-medium shadow-md hover:shadow-lg"
                >
                  <span class="material-symbols-outlined">power_settings_new</span>
                  <span>{{ t('navbar.logout') }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </nav>
    
    <!-- Mobile Navigation Menu -->
    <div 
      v-if="isMobileMenuOpen"
      class="lg:hidden fixed inset-0 z-50 bg-black bg-opacity-50"
      @click="toggleMobileMenu"
    >
      <div 
        class="bg-white w-80 max-w-full h-full overflow-y-auto"
        @click.stop
      >
        <div class="p-4 border-b border-gray-200 flex items-center justify-between">
          <h2 class="text-xl font-bold text-gray-800">Menü</h2>
          <button @click="toggleMobileMenu" class="p-2 hover:bg-gray-100 rounded-lg">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>
        
        <div class="p-4 space-y-2">
          <div v-for="item in topNavItems" :key="item.id">
            <button
              @click="navigateTo(item); toggleMobileMenu()"
              class="w-full flex items-center gap-3 p-3 rounded-lg hover:bg-gray-100 transition-colors"
              :class="{ 'bg-purple-100 text-purple-700': currentRoute === item.id }"
            >
              <span class="material-symbols-outlined">{{ item.icon }}</span>
              <span class="font-medium">{{ item.label }}</span>
            </button>
          </div>
        </div>
        
        <div class="p-4 border-t border-gray-200">
          <button
            @click="logout"
            class="w-full flex items-center gap-3 p-3 rounded-lg bg-red-50 text-red-600 hover:bg-red-100 transition-colors"
          >
            <span class="material-symbols-outlined">logout</span>
            <span class="font-medium">Çıkış Yap</span>
          </button>
        </div>
      </div>
    </div>
    
    <!-- Ticker -->
    <div v-if="showTicker" class="bg-black text-yellow-400 py-3 overflow-hidden flex-shrink-0 shadow-lg relative">
      <div class="ticker-wrapper">
        <div class="ticker-content inline-flex whitespace-nowrap animate-ticker font-medium text-base tracking-wide items-center">
          <!-- Office Name -->
          <span v-if="exchangeStore.selectedOffice" class="inline-flex items-center mx-4 text-white bg-yellow-600 px-3 py-1 rounded">
            <span class="font-bold">{{ exchangeStore.selectedOffice.officeName }}</span>
          </span>
          <span class="mx-3">•</span>
          
          <!-- First set -->
          <div class="inline-flex items-center">
            <template v-for="(rate, index) in tickerRates" :key="`a-${index}`">
              <span class="inline-flex items-center mx-4">
                <!-- Special icons for USDT and KRUB -->
                <span v-if="rate.code === 'USDT'" class="mr-2 text-lg">₮</span>
                <span v-else-if="rate.code === 'KRUB'" class="mr-2">💳</span>
                <!-- Flag icons for other currencies -->
                <i v-else-if="rate.countryCode" :class="`fi fi-${rate.countryCode} mr-2`"></i>
                <span>{{ rate.code }}: </span>
                <span class="text-green-400">A:{{ rate.buyRate }}</span>
                <span class="mx-1">/</span>
                <span class="text-red-400">S:{{ rate.sellRate }}</span>
                <span class="ml-1">₺</span>
              </span>
              <span class="mx-3">•</span>
            </template>
          </div>
          
          <!-- Office Name again for continuity -->
          <span v-if="exchangeStore.selectedOffice" class="inline-flex items-center mx-4 text-white bg-yellow-600 px-3 py-1 rounded">
            <span class="font-bold">{{ exchangeStore.selectedOffice.officeName }}</span>
          </span>
          <span class="mx-3">•</span>
          
          <!-- Second set for continuous scroll -->
          <div class="inline-flex items-center">
            <template v-for="(rate, index) in tickerRates" :key="`b-${index}`">
              <span class="inline-flex items-center mx-4">
                <span v-if="rate.code === 'USDT'" class="mr-2 text-lg">₮</span>
                <span v-else-if="rate.code === 'KRUB'" class="mr-2">💳</span>
                <i v-else-if="rate.countryCode" :class="`fi fi-${rate.countryCode} mr-2`"></i>
                <span>{{ rate.code }}: </span>
                <span class="text-green-400">A:{{ rate.buyRate }}</span>
                <span class="mx-1">/</span>
                <span class="text-red-400">S:{{ rate.sellRate }}</span>
                <span class="ml-1">₺</span>
              </span>
              <span class="mx-3">•</span>
            </template>
          </div>
          
          <!-- Office Name again for continuity -->
          <span v-if="exchangeStore.selectedOffice" class="inline-flex items-center mx-4 text-white bg-yellow-600 px-3 py-1 rounded">
            <span class="font-bold">{{ exchangeStore.selectedOffice.officeName }}</span>
          </span>
          <span class="mx-3">•</span>
          
          <!-- Third set for continuous scroll -->
          <div class="inline-flex items-center">
            <template v-for="(rate, index) in tickerRates" :key="`c-${index}`">
              <span class="inline-flex items-center mx-4">
                <span v-if="rate.code === 'USDT'" class="mr-2 text-lg">₮</span>
                <span v-else-if="rate.code === 'KRUB'" class="mr-2">💳</span>
                <i v-else-if="rate.countryCode" :class="`fi fi-${rate.countryCode} mr-2`"></i>
                <span>{{ rate.code }}: </span>
                <span class="text-green-400">A:{{ rate.buyRate }}</span>
                <span class="mx-1">/</span>
                <span class="text-red-400">S:{{ rate.sellRate }}</span>
                <span class="ml-1">₺</span>
              </span>
              <span class="mx-3">•</span>
            </template>
          </div>
        </div>
      </div>
    </div>
    
    <!-- Main Container -->
    <div class="flex flex-1 overflow-hidden">
      <!-- Left Sidebar - Desktop -->
      <aside v-if="shouldShowSidebar" class="modern-sidebar hidden lg:block">
        <div class="sidebar-content">
          <!-- Dynamic Page Title -->
          <div class="sidebar-header">
            <h3 class="sidebar-title">
              <span v-if="currentPageName === 'dashboard'">Hızlı İşlemler</span>
              <span v-else-if="currentPageName === 'z-report'">Rapor İşlemleri</span>
              <span v-else-if="currentPageName === 'exchange'">Döviz İşlemleri</span>
              <span v-else-if="currentPageName === 'parties'">Cari İşlemleri</span>
              <span v-else-if="currentPageName === 'expenses'">Gider İşlemleri</span>
              <span v-else-if="currentPageName === 'vaults'">Kasa İşlemleri</span>
              <span v-else>Hızlı İşlemler</span>
            </h3>
          </div>
          
          <div class="sidebar-actions">
            <!-- Hide quick search for now -->
            <!-- <div>
              <h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">HIZLI ARAMA</h3>
              <input 
                type="text" 
                placeholder="Ara..." 
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
              >
            </div> -->
            
            <!-- Dashboard Page Actions -->
            <div v-if="currentPageName === 'dashboard'" class="action-group">
              <button
                @click="() => router.push('/ihtiyar/z-report')"
                class="sidebar-btn primary"
              >
                <span class="material-symbols-outlined">analytics</span>
                <span>Z Raporu</span>
              </button>
              <button
                @click="() => router.push('/ihtiyar/exchange')"
                class="sidebar-btn"
              >
                <span class="material-symbols-outlined">currency_exchange</span>
                <span>Döviz İşlemleri</span>
              </button>
              <button
                @click="() => router.push('/ihtiyar/parties')"
                class="sidebar-btn"
              >
                <span class="material-symbols-outlined">groups</span>
                <span>Cariler</span>
              </button>
              <button
                @click="() => router.push('/ihtiyar/expenses')"
                class="sidebar-btn"
              >
                <span class="material-symbols-outlined">payments</span>
                <span>{{ t('sidebar.expenses') }}</span>
              </button>
            </div>
            
            <!-- Z-Report Page Actions - Hidden for cleaner UI -->
            <div v-if="currentPageName === 'z-report' && false" class="action-group">
              <button
                @click="handlePrintReport"
                class="sidebar-btn"
              >
                <span class="material-symbols-outlined">print</span>
                <span>Raporu Yazdır</span>
              </button>
              <button
                v-if="authStore.isAdmin"
                @click="handleEndOfDay"
                class="sidebar-btn warning"
              >
                <span class="material-symbols-outlined">lock</span>
                <span>Gün Sonu</span>
              </button>
              <button
                @click="reloadPage"
                class="sidebar-btn"
              >
                <span class="material-symbols-outlined">refresh</span>
                <span>Yenile</span>
              </button>
            </div>
            
            <!-- Z-Report Page Actions -->
            <div v-if="currentPageName === 'z-report'" class="action-group">
              <button
                @click="handlePrintReport"
                class="sidebar-btn primary"
              >
                <span class="material-symbols-outlined">print</span>
                <span>Raporu Yazdır</span>
              </button>
            </div>
            
            <!-- Exchange Page Actions -->
            <div v-if="currentPageName === 'exchange'" class="action-group">
              <button
                v-if="authStore.isAdmin"
                @click="() => router.push({ name: 'ExchangeRates' })"
                class="sidebar-btn primary"
              >
                <span class="material-symbols-outlined">settings</span>
                <span>{{ t('sidebar.exchangeRates') }}</span>
              </button>
              
              <!-- USDT TRC20 Payments Button -->
              <button
                @click="openUSDTModal"
                class="sidebar-btn"
                style="background: linear-gradient(135deg, #26A17B 0%, #1E8E66 100%); color: white;"
              >
                <span style="font-size: 1.2rem; font-weight: bold;">₮</span>
                <span>{{ t('sidebar.usdtPayments') }}</span>
              </button>
              <div v-if="showQuickActions" class="sidebar-divider"></div>
              <button 
                v-if="showQuickActions"
                v-for="action in quickActions" 
                :key="action.id"
                @click="handleQuickAction(action)"
                class="sidebar-btn"
                :class="action.id.includes('buy') ? 'success' : 'danger'"
              >
                  <!-- Currency icons -->
                  <div v-if="action.id.includes('usdt')" class="w-5 h-5">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <circle cx="12" cy="12" r="12" fill="#26A17B"/>
                      <path d="M13.2 10.5V9.3H17.4V7.2H6.6V9.3H10.8V10.5C7.8 10.65 5.4 11.25 5.4 12C5.4 12.75 7.8 13.35 10.8 13.5V18.6H13.2V13.5C16.2 13.35 18.6 12.75 18.6 12C18.6 11.25 16.2 10.65 13.2 10.5ZM13.2 12.45C13.2 12.45 12.6 12.6 12 12.6C11.4 12.6 10.8 12.45 10.8 12.45C8.1 12.3 6.3 11.85 6.3 12C6.3 12.15 8.1 11.7 10.8 11.55V12.15C10.8 12.15 11.4 12.3 12 12.3C12.6 12.3 13.2 12.15 13.2 12.15V11.55C15.9 11.7 17.7 12.15 17.7 12C17.7 11.85 15.9 12.3 13.2 12.45Z" fill="white"/>
                    </svg>
                  </div>
                  <div v-else-if="action.id.includes('card-ruble')" class="flex items-center gap-1">
                    <span class="material-symbols-outlined text-base">credit_card</span>
                    <i class="fi fi-ru text-xs"></i>
                  </div>
                  <span v-else class="material-symbols-outlined text-xl">
                    {{ action.id.includes('buy') ? 'add_circle' : 'remove_circle' }}
                  </span>
                  
                  <span class="text-sm">{{ action.label }}</span>
                </button>
            </div>
            
            <!-- Expenses Page Actions -->
            <div v-if="currentPageName === 'expenses'" class="action-group">
              <button
                @click="clickDomSelector('.expenses-management button[class*=primary]')"
                class="sidebar-btn primary"
              >
                <span class="material-symbols-outlined">add_card</span>
                <span>{{ t('sidebar.newPaymentMethod') }}</span>
              </button>
              <button
                @click="clickDomSelector('.expenses-management .tab-btn:nth-child(2)')"
                class="sidebar-btn"
              >
                <span class="material-symbols-outlined">category</span>
                <span>{{ t('sidebar.expenseCategories') }}</span>
              </button>
              <button
                @click="clickDomSelector('.expenses-management .tab-btn:nth-child(3)')"
                class="sidebar-btn"
              >
                <span class="material-symbols-outlined">analytics</span>
                <span>{{ t('sidebar.expenseReports') }}</span>
              </button>
            </div>
            
            <!-- Parties Page Actions -->
            <div v-if="currentPageName === 'parties'" class="action-group">
              <!-- Quick Actions -->
              <div class="quick-actions-section">
                <h4 class="section-subtitle">
                  <span class="material-symbols-outlined">flash_on</span>
                  Hızlı İşlemler
                </h4>
                <button
                  @click="handleCreateParty()"
                  class="sidebar-btn primary gradient"
                >
                  <span class="material-symbols-outlined">person_add</span>
                  <span>Yeni Cari Ekle</span>
                </button>
              </div>
              
              <!-- Divider -->
              <div class="sidebar-divider"></div>
              
              <!-- Party Totals - Modern Cards -->
              <div class="stats-section">
                <h4 class="section-subtitle">
                  <span class="material-symbols-outlined">analytics</span>
                  Genel Bakış
                </h4>
                <div class="party-stats-modern">
                  <div class="stat-card-modern receivables clickable"
                     @click="handleFilterReceivables">
                    <div class="stat-icon-wrapper">
                      <span class="material-symbols-outlined">trending_up</span>
                    </div>
                    <div class="stat-content">
                      <span class="stat-label">Toplam Alacak</span>
                      <span class="stat-value">₺{{ formatCurrency(partyTotals.totalReceivables) }}</span>
                    </div>
                  </div>
                  
                  <div class="stat-card-modern debts clickable"
                     :class="{ 'has-debt': partyTotals.totalDebts > 0 }"
                     @click="handleFilterDebts">
                    <div class="stat-icon-wrapper">
                      <span class="material-symbols-outlined">trending_down</span>
                    </div>
                    <div class="stat-content">
                      <span class="stat-label">Toplam Borç</span>
                      <span class="stat-value">₺{{ formatCurrency(partyTotals.totalDebts) }}</span>
                    </div>
                  </div>
                  
                  <div class="stat-card-modern balance clickable" 
                     :class="{ 'positive': partyTotals.netBalance > 0, 'negative': partyTotals.netBalance < 0 }"
                     @click="handleFilterAll">
                    <div class="stat-icon-wrapper">
                      <span class="material-symbols-outlined">account_balance_wallet</span>
                    </div>
                    <div class="stat-content">
                      <span class="stat-label">Net Bakiye</span>
                      <span class="stat-value">
                        ₺{{ formatCurrency(partyTotals.netBalance) }}
                      </span>
                    </div>
                    <div class="stat-indicator" v-if="partyTotals.netBalance !== 0">
                      <span class="material-symbols-outlined">
                        {{ partyTotals.netBalance > 0 ? 'arrow_upward' : 'arrow_downward' }}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            
            <!-- Vaults Page Actions -->
            <div v-if="currentPageName === 'vaults'" class="action-group">
              <button
                v-if="authStore.isAdmin"
                @click="handleCreateVault()"
                class="sidebar-btn primary"
              >
                <span class="material-symbols-outlined">add_circle</span>
                <span>Yeni Kasa</span>
              </button>
            </div>
          </div>
        </div>
      </aside>
      
      <!-- Mobile Sidebar -->
      <div 
        v-if="isSidebarOpen && shouldShowSidebar"
        class="lg:hidden fixed inset-0 z-50 bg-black bg-opacity-50"
        @click="toggleSidebar"
      >
        <div 
          class="absolute right-0 top-0 w-80 max-w-full h-full bg-white overflow-y-auto shadow-xl"
          @click.stop
        >
          <div class="p-4 border-b border-gray-200 flex items-center justify-between">
            <h2 class="text-xl font-bold text-gray-800">Hızlı İşlemler</h2>
            <button @click="toggleSidebar" class="p-2 hover:bg-gray-100 rounded-lg">
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>
          
          <div class="p-5">
            <div class="space-y-4">
              <div>
                <h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">HIZLI ARAMA</h3>
                <input 
                  type="text" 
                  placeholder="Ara..." 
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                >
              </div>
              
              <!-- Z-Report Buttons - Only on Z-Report page -->
              <div v-if="currentPageName === 'z-report'">
                <h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Z-RAPOR İŞLEMLERİ</h3>
                <div class="space-y-2">
                  <button
                    @click="handleEndOfDay; toggleSidebar()"
                    class="w-full px-4 py-3 rounded-lg font-medium transition-all flex items-center justify-center gap-2 bg-amber-100 text-amber-700 hover:bg-amber-200 border-2 border-amber-300"
                  >
                    <span class="material-symbols-outlined text-lg">lock</span>
                    <span class="text-sm">GÜN SONU</span>
                  </button>

                  <button
                    @click="handlePrintReport; toggleSidebar()"
                    class="w-full px-4 py-3 rounded-lg font-medium transition-all flex items-center justify-center gap-2 bg-blue-100 text-blue-700 hover:bg-blue-200 border-2 border-blue-300"
                  >
                    <span class="material-symbols-outlined text-lg">print</span>
                    <span class="text-sm">YAZDIR</span>
                  </button>
                </div>
              </div>
              
              <div v-if="isOnExchangePage">
                <h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">HIZLI İŞLEMLER</h3>
                <div class="space-y-2">
                  <!-- Exchange Rates Management Button - At the top -->
                  <router-link
                    v-if="authStore.isAdmin"
                    :to="{ name: 'ExchangeRates' }"
                    @click="toggleSidebar"
                    class="w-full px-4 py-3 rounded-lg font-medium transition-all flex items-center justify-center gap-2 bg-purple-100 text-purple-700 hover:bg-purple-200 border-2 border-purple-300"
                  >
                    <span class="material-symbols-outlined text-lg">currency_exchange</span>
                    <span class="text-sm">KUR YÖNETİMİ</span>
                  </router-link>
                  
                  <!-- Quick actions removed for exchange v2 -->
                </div>
              </div>
              
              <div v-if="isOnVaultsPage && authStore.isAdmin">
                <h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">KASA İŞLEMLERİ</h3>
                <div class="space-y-2">
                  <button
                    @click="handleCreateVault(); toggleSidebar()"
                    class="w-full px-4 py-3 rounded-lg font-medium transition-all flex items-center justify-center gap-2 bg-blue-100 text-blue-700 hover:bg-blue-200 border-2 border-blue-300"
                  >
                    <span class="material-symbols-outlined text-lg">add_circle</span>
                    <span class="text-sm">YENİ KASA OLUŞTUR</span>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <!-- Content Area -->
      <main class="flex-1 overflow-y-auto">
        <div class="p-4 lg:p-6">
          <router-view v-slot="{ Component }">
            <component :is="Component" ref="routerViewRef" />
          </router-view>
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
/* Navigation Styles */
.nav-items {
  display: flex;
  align-items: flex-start;
  gap: 20px;
}

.nav-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}

.nav-group {
  display: flex;
  gap: 5px;
}

.nav-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 12px 20px;
  cursor: pointer;
  border-radius: 12px;
  transition: transform 0.2s ease, box-shadow 0.2s ease, background 0.2s ease, border-color 0.2s ease;
  min-width: 100px;
  border: 1px solid transparent;
  position: relative;
  will-change: transform, box-shadow;
  transform: translateZ(0); /* Hardware acceleration */
  backface-visibility: hidden; /* Prevent flickering */
}

.nav-item:hover {
  background: linear-gradient(145deg, #f9fafb, #ffffff);
  border-color: rgba(99, 102, 241, 0.2);
  transform: translateY(-2px) translateZ(0);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.15);
}

.nav-item:hover .nav-icon {
  transform: scale(1.1) translateZ(0);
  color: #6366f1;
}

.nav-item.active {
  background: linear-gradient(145deg, #e0e7ff, #c7d2fe);
  border-color: #6366f1;
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.25);
}

.nav-item.active::before {
  content: '';
  position: absolute;
  bottom: -2px;
  left: 50%;
  transform: translateX(-50%);
  width: 40%;
  height: 3px;
  background: linear-gradient(90deg, #6366f1, #8b5cf6);
  border-radius: 2px;
}

.nav-icon {
  font-size: 32px;
  margin-bottom: 8px;
  color: #4b5563;
  line-height: 1;
  transition: transform 0.2s ease, color 0.2s ease;
  will-change: transform;
  transform: translateZ(0);
}

.nav-item.active .nav-icon {
  color: #6366f1;
  filter: drop-shadow(0 2px 4px rgba(99, 102, 241, 0.3));
}

.nav-label {
  font-size: 12px;
  color: #1a1a1a;
  text-align: center;
  white-space: nowrap;
  font-weight: 500;
}

.nav-category-label {
  font-size: 10px;
  color: #6b7280;
  text-align: center;
  white-space: nowrap;
  margin-top: 4px;
  letter-spacing: 0.5px;
  font-weight: 600;
}
.nav-category-owner {
  color: #b45309;
}
.nav-item-owner {
  border-left: 2px solid transparent;
}
.nav-item-owner:hover, .nav-item-owner.active {
  border-left-color: #f59e0b;
  background: rgba(245, 158, 11, 0.08);
  color: #b45309;
}
.nav-item-owner .nav-icon {
  color: #d97706;
}

.nav-divider {
  width: 1px;
  height: 60px;
  background: rgba(0, 0, 0, 0.1);
  margin: 0 16px;
  align-self: center;
}

/* Material Symbols Outlined */
.material-symbols-outlined {
  font-variation-settings: 
    'FILL' 0,
    'wght' 400,
    'GRAD' 0,
    'opsz' 24;
}

.nav-icon.material-symbols-outlined {
  font-size: 32px;
  font-weight: 400;
  font-variation-settings: 
    'FILL' 1,
    'wght' 400,
    'GRAD' 200,
    'opsz' 48;
}

/* Ticker Animation - Continuous loop */
@keyframes ticker {
  0% { transform: translateX(0); }
  100% { transform: translateX(-33.333%); }
}

.animate-ticker {
  animation: ticker 60s linear infinite; /* Slowed down from 30s to 60s */
}

.ticker-wrapper {
  display: flex;
  align-items: center;
}

/* Modern Sidebar Styles */
.modern-sidebar {
  width: 280px;
  background: white;
  border-right: 1px solid #e5e7eb;
  overflow-y: auto;
  flex-shrink: 0;
}

.sidebar-content {
  padding: 1.5rem;
}

.sidebar-header {
  margin-bottom: 1.5rem;
}

.sidebar-title {
  font-size: 1.1rem;
  font-weight: 600;
  color: #111827;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.sidebar-actions {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.action-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.sidebar-btn {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  border: none;
  border-radius: 10px;
  background: #f3f4f6;
  color: #374151;
  font-size: 0.95rem;
  font-weight: 500;
  cursor: pointer;
  transition: transform 0.15s ease, background 0.15s ease, box-shadow 0.15s ease;
  text-align: left;
  will-change: transform;
  transform: translateZ(0);
}

.sidebar-btn:hover {
  background: #e5e7eb;
  transform: translateX(5px) translateZ(0);
}

.sidebar-btn.primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.sidebar-btn.primary.gradient {
  background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.3);
  font-weight: 700;
}

.sidebar-btn.primary.gradient:hover {
  transform: translateX(4px) scale(1.02);
  box-shadow: 0 6px 20px rgba(99, 102, 241, 0.4);
}

.sidebar-btn.success-light {
  background: #f0fdf4;
  color: #10b981;
  border: 1px solid #bbf7d0;
  font-weight: 600;
}

.sidebar-btn.success-light:hover {
  background: #dcfce7;
  border-color: #86efac;
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.15);
}

.sidebar-btn.danger-light {
  background: #fef2f2;
  color: #ef4444;
  border: 1px solid #fecaca;
  font-weight: 600;
}

.sidebar-btn.danger-light:hover {
  background: #fee2e2;
  border-color: #fca5a5;
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.15);
}

.sidebar-btn.primary:hover {
  box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
}

.sidebar-btn.success {
  background: #d1fae5;
  color: #065f46;
}

.sidebar-btn.success:hover {
  background: #a7f3d0;
}

.sidebar-btn.danger {
  background: #fee2e2;
  color: #991b1b;
}

.sidebar-btn.danger:hover {
  background: #fecaca;
}

.sidebar-btn.warning {
  background: #fed7aa;
  color: #92400e;
}

.sidebar-btn.warning:hover {
  background: #fbbf24;
}

.sidebar-btn .material-symbols-outlined {
  font-size: 20px;
}

.sidebar-divider {
  height: 1px;
  background: #e5e7eb;
  margin: 1rem 0;
}

.sidebar-stats {
  background: #f9fafb;
  border-radius: 10px;
  padding: 1rem;
}

.stats-title {
  font-size: 0.85rem;
  font-weight: 600;
  color: #6b7280;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 1rem;
}

.stat-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.5rem 0;
}

.stat-item:not(:last-child) {
  border-bottom: 1px solid #e5e7eb;
}

.stat-label {
  font-size: 0.9rem;
  color: #6b7280;
}

.stat-value {
  font-size: 1rem;
  font-weight: 600;
  color: #7c3aed;
}

/* Quick Actions Section */
.quick-actions-section {
  margin-bottom: 1.5rem;
}

.section-subtitle {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  color: #6b7280;
  letter-spacing: 0.5px;
  margin-bottom: 1rem;
}

.section-subtitle .material-symbols-outlined {
  font-size: 16px;
  color: #8b5cf6;
}

/* Stats Section */
.stats-section {
  margin-top: 1.5rem;
}

/* Party Stats Modern Cards */
.party-stats-modern {
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
}

.stat-card-modern {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: white;
  border-radius: 14px;
  border: 1px solid #f3f4f6;
  position: relative;
  overflow: hidden;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.03);
}

.stat-card-modern.clickable {
  cursor: pointer;
}

.stat-card-modern.clickable:hover {
  background: #f9fafb;
  border-color: #e5e7eb;
}

.stat-card-modern::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, transparent, currentColor, transparent);
  opacity: 0;
  transition: opacity 0.3s ease;
}

.stat-card-modern:hover {
  transform: translateX(4px) scale(1.01) translateZ(0);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  background: white;
}

.stat-card-modern:hover::before {
  opacity: 0.5;
}

.stat-card-modern.receivables {
  background: #f0fdf4;
  border-left: 4px solid #10b981;
}

.stat-card-modern.debts {
  background: #f9fafb;
  border-left: 4px solid #9ca3af;
}

.stat-card-modern.debts.has-debt {
  background: #fef2f2;
  border-left: 4px solid #ef4444;
}

.stat-card-modern.balance {
  background: #f3f4fb;
  border-left: 4px solid #6366f1;
}

.stat-card-modern.balance.positive {
  background: #f0fdf4;
  border-left: 4px solid #10b981;
}

.stat-card-modern.balance.negative {
  background: #fef2f2;
  border-left: 4px solid #ef4444;
}

.stat-icon-wrapper {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
  position: relative;
}

.stat-icon-wrapper .material-symbols-outlined {
  font-size: 24px;
  font-variation-settings: 
    'FILL' 1,
    'wght' 500,
    'GRAD' 0,
    'opsz' 48;
}

.stat-card-modern.receivables .stat-icon-wrapper {
  background: rgba(16, 185, 129, 0.1);
}

.stat-card-modern.receivables .material-symbols-outlined {
  color: #10b981;
}

.stat-card-modern.receivables .stat-value {
  color: #10b981;
}

.stat-card-modern.debts .stat-icon-wrapper {
  background: rgba(156, 163, 175, 0.1);
}

.stat-card-modern.debts .material-symbols-outlined {
  color: #9ca3af;
}

.stat-card-modern.debts .stat-value {
  color: #6b7280;
}

.stat-card-modern.debts.has-debt .stat-icon-wrapper {
  background: rgba(239, 68, 68, 0.1);
}

.stat-card-modern.debts.has-debt .material-symbols-outlined {
  color: #ef4444;
}

.stat-card-modern.debts.has-debt .stat-value {
  color: #ef4444;
}

.stat-card-modern.balance .stat-icon-wrapper {
  background: rgba(99, 102, 241, 0.1);
}

.stat-card-modern.balance .material-symbols-outlined {
  color: #6366f1;
}

.stat-card-modern.balance.positive .stat-icon-wrapper {
  background: rgba(16, 185, 129, 0.1);
}

.stat-card-modern.balance.positive .material-symbols-outlined {
  color: #10b981;
}

.stat-card-modern.balance.negative .stat-icon-wrapper {
  background: rgba(239, 68, 68, 0.1);
}

.stat-card-modern.balance.negative .material-symbols-outlined {
  color: #ef4444;
}

.stat-card-modern .stat-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.stat-card-modern .stat-label {
  font-size: 0.7rem;
  font-weight: 700;
  text-transform: uppercase;
  color: #6b7280;
  letter-spacing: 0.5px;
}

.stat-card-modern .stat-value {
  font-size: 1.25rem;
  font-weight: 800;
  color: #1f2937;
  letter-spacing: -0.5px;
}

.stat-indicator {
  position: absolute;
  right: 1rem;
  top: 50%;
  transform: translateY(-50%);
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  background: rgba(0, 0, 0, 0.03);
}

.stat-indicator .material-symbols-outlined {
  font-size: 18px;
  font-variation-settings: 
    'FILL' 1,
    'wght' 700,
    'GRAD' 0,
    'opsz' 24;
}

.stat-card-modern.balance.positive .stat-indicator {
  background: rgba(16, 185, 129, 0.1);
}

.stat-card-modern.balance.positive .stat-indicator .material-symbols-outlined {
  color: #10b981;
}

.stat-card-modern.balance.negative .stat-indicator {
  background: rgba(239, 68, 68, 0.1);
}

.stat-card-modern.balance.negative .stat-indicator .material-symbols-outlined {
  color: #ef4444;
}

/* Office Selector in Sidebar */
.office-selector-sidebar {
  margin-bottom: 1rem;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 8px;
}

.office-selector-sidebar.modern {
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.05), rgba(139, 92, 246, 0.05));
  border: 1px solid rgba(99, 102, 241, 0.1);
  border-radius: 12px;
  padding: 1.25rem;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  will-change: transform;
  transform: translateZ(0);
}

.office-selector-sidebar.modern:hover {
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.1);
  transform: translateY(-2px) translateZ(0);
}

.selector-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.selector-header .material-symbols-outlined {
  font-size: 18px;
  color: #6366f1;
  font-variation-settings: 
    'FILL' 1,
    'wght' 500,
    'GRAD' 0,
    'opsz' 24;
}

.office-selector-sidebar .selector-label {
  display: block;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  color: #6b7280;
  letter-spacing: 0.5px;
  margin-bottom: 0.5rem;
}

.office-select-sidebar {
  width: 100%;
  padding: 0.5rem;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  background: white;
  font-size: 0.9rem;
  color: #1f2937;
  transition: all 0.2s ease;
}

.office-select-sidebar.modern {
  padding: 0.75rem;
  border: 2px solid rgba(99, 102, 241, 0.2);
  border-radius: 10px;
  background: white;
  font-weight: 600;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.office-select-sidebar:focus {
  outline: none;
  border-color: #7c3aed;
  box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.1);
}

.office-select-sidebar.modern:focus {
  border-color: #6366f1;
  box-shadow: 0 0 0 4px rgba(99, 102, 241, 0.15);
}

/* User Menu Button */
.user-menu-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 12px;
  background: linear-gradient(145deg, #ffffff, #f3f4f6);
  border: 1px solid rgba(0, 0, 0, 0.05);
  transition: transform 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  will-change: transform;
  transform: translateZ(0);
}

.user-menu-btn:hover {
  background: linear-gradient(145deg, #f3f4f6, #e5e7eb);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
  transform: translateY(-1px) translateZ(0);
}

.user-avatar {
  width: 32px;
  height: 32px;
  border-radius: 10px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
}

.user-avatar .material-symbols-outlined {
  font-size: 20px;
  font-variation-settings: 
    'FILL' 1,
    'wght' 400,
    'GRAD' 0,
    'opsz' 24;
}

/* Icon animations - Optimized */
@keyframes iconPulse {
  0% { transform: scale(1) translateZ(0); }
  50% { transform: scale(1.05) translateZ(0); }
  100% { transform: scale(1) translateZ(0); }
}

.nav-item.active .nav-icon {
  animation: iconPulse 2s ease-in-out infinite;
  will-change: transform;
}

/* Custom scrollbar for desktop */
@media (min-width: 1024px) {
  .modern-sidebar::-webkit-scrollbar {
    width: 6px;
  }
  
  .modern-sidebar::-webkit-scrollbar-track {
    background: #f3f4f6;
  }
  
  .modern-sidebar::-webkit-scrollbar-thumb {
    background: #d1d5db;
    border-radius: 3px;
  }
  
  .modern-sidebar::-webkit-scrollbar-thumb:hover {
    background: #9ca3af;
  }
}
</style>