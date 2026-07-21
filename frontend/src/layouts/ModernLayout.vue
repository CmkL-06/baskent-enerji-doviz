<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import LanguageSelector from '@/components/common/LanguageSelector.vue'
import { useNotification } from '@/composables/useNotification'
import { formatAmount } from '@/utils/currency'

const notification = useNotification()
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
  { id: 'z-report', icon: 'insert_chart', label: t('navbar.zReport'), category: 'RAPORLAR', path: '/ihtiyar/z-report', adminOnly: false },
  { id: 'exchange-v2', icon: 'paid', label: t('navbar.exchange'), category: 'İŞLEMLER', path: '/ihtiyar/exchange-v2', adminOnly: false },
  { id: 'parties', icon: 'contacts', label: t('navbar.parties'), category: 'İŞLEMLER', path: '/ihtiyar/parties', adminOnly: false },
  // { id: 'ghost-party', icon: 'auto_awesome', label: t('navbar.ghostParty'), category: 'İŞLEMLER', path: '/ihtiyar/ghost-party', adminOnly: true },
  { id: 'expenses', icon: 'receipt_long', label: t('navbar.expenses'), category: 'İŞLEMLER', path: '/ihtiyar/expenses', adminOnly: false },
  { id: 'vaults', icon: 'account_balance_wallet', label: t('navbar.vaults'), category: 'İŞLEMLER', path: '/ihtiyar/vaults', adminOnly: false },
  { id: 'owner-panel', icon: 'shield_person', label: 'Yönetim Paneli', category: 'YÖNETİM', path: '/ihtiyar/owner-panel', ownerOnly: true },
  { id: 'users', icon: 'manage_accounts', label: 'Kullanıcı Yönetimi', category: 'YÖNETİM', path: '/ihtiyar/users', adminOnly: true },
  { id: 'exchange-rates', icon: 'tune', label: 'Manuel Kur Yönetimi', category: 'YÖNETİM', path: '/ihtiyar/exchange-rates', adminOnly: true },
  { id: 'auto-rate-management', icon: 'currency_exchange', label: 'Otomatik Kur Yönetimi', category: 'YÖNETİM', path: '/ihtiyar/auto-rate-management', adminOnly: true },
  { id: 'currencies', icon: 'payments', label: 'Para Birimleri', category: 'YÖNETİM', path: '/ihtiyar/currencies', adminOnly: true },
  { id: 'settings', icon: 'tune', label: t('navbar.settings'), category: 'YÖNETİM', path: '/ihtiyar/settings', adminOnly: false },
  { id: 'tg-admin', icon: 'smart_toy', label: 'TG Yönetim', category: 'TELEGRAM', path: '/ihtiyar/tg-admin', adminOnly: true },
  { id: 'tg-operator', icon: 'support_agent', label: 'TG Operatör', category: 'TELEGRAM', path: '/ihtiyar/tg-operator', adminOnly: false },
  { id: 'tg-dealer', icon: 'storefront', label: 'TG Bayi', category: 'TELEGRAM', path: '/ihtiyar/tg-dealer', adminOnly: false },
])

// Filtered navigation items based on user role
const topNavItems = computed(() => {
  return allNavItems.value.filter(item => {
    if (item.ownerOnly) return authStore.isOwner
    if (item.adminOnly) return authStore.isAdmin
    return true
  })
})


const tickerStatus = ref<'loading' | 'ready' | 'error'>('loading')
let tickerInterval: number | null = null

// Ticker visibility state - default is false (hidden)
const showTicker = ref(false)


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

    if (exchangeStore.exchangeRates && exchangeStore.exchangeRates.length > 0) {
      tickerStatus.value = 'ready'
    } else if (previousRates && previousRates.length > 0) {
      exchangeStore.exchangeRates = previousRates
      tickerStatus.value = 'ready'
    }
  } catch (error) {
    console.error('Failed to load exchange rates:', error)
    if (tickerStatus.value === 'loading') {
      tickerStatus.value = 'error'
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
          buyRate: formatAmount(rate.buyRate, 2),
          sellRate: formatAmount(rate.sellRate, 2),
          countryCode: getCurrencyCountryCode(rate.sourceCurrencyCode),
          officeId: rate.officeId
        })
      }
    })

  return Array.from(uniqueRatesMap.values())
})

// Get items for each category
const getCategoryItems = (category: string) => {
  return topNavItems.value.filter(item => item.category === category)
}

// Menü ikon rozetlerinin rengi — AppKpiCard/topbar-btn ailesindeki "canlı ikon rozeti" dilini
// menüye taşımak için her kategoriye kendi vurgu rengi atanır.
const categoryColors: Record<string, string> = {
  GENEL: 'var(--color-primary)',
  'İŞLEMLER': 'var(--color-secondary)',
  RAPORLAR: '#8b5cf6',
  YÖNETİM: 'var(--color-warning)',
  TELEGRAM: 'var(--color-success)',
}
const categoryColor = (category: string) => categoryColors[category] || 'var(--color-primary)'


// New computed: nav groups for sidebar
const navGroups = computed(() => {
  const cats = ['GENEL', 'İŞLEMLER', 'RAPORLAR', 'YÖNETİM', 'TELEGRAM']
  if (authStore.isOwner) cats.push('OWNER')
  return cats
    .map(cat => ({ cat, items: getCategoryItems(cat) }))
    .filter(g => g.items.length > 0)
})

// New computed: page title for top header
const routeTitleMap: Record<string, string> = {
  'dashboard': 'Dashboard',
  'exchange-v2': 'Döviz İşlemleri',
  'exchange-rates': 'Manuel Kur Yönetimi',
  'history': 'Z-Raporu',
  'z-report': 'Z-Raporu',
  'z-report-v2': 'Z-Raporu V2',
  'parties': 'Cari Hesaplar',
  'ghost-party': 'Hayalet Cari',
  'expenses': 'Gider Yönetimi',
  'vaults': 'Kasalar',
  'vault-counts': 'Kasalar',
  'vault-management': 'Kasalar',
  'users': 'Kullanıcı Yönetimi',
  'auto-rate-management': 'Otomatik Kur Yönetimi',
  'currencies': 'Para Birimleri',
  'settings': 'Ayarlar',
  'owner-panel': 'Owner Panel',
  'tg-admin': 'TG Yönetim Paneli',
  'tg-dealer': 'TG Bayi Paneli',
  'tg-operator': 'TG Operatör Paneli',
}

const pageTitle = computed(() => {
  const segments = route.path.split('/').filter(Boolean)
  for (let i = segments.length - 1; i >= 0; i--) {
    if (routeTitleMap[segments[i]]) return routeTitleMap[segments[i]]
  }
  return 'Dashboard'
})


const logout = () => {
  isUserMenuOpen.value = false
  isSidebarOpen.value = false
  authStore.logout()
}

const toggleMobileMenu = () => {
  isMobileMenuOpen.value = !isMobileMenuOpen.value
}

const toggleSidebar = () => {
  isSidebarOpen.value = !isSidebarOpen.value
}


const handleEndOfDay = async () => {
  // If we have a ref to the router view component, use its closeDay function
  if (routerViewRef.value?.performEndOfDay) {
    await routerViewRef.value.performEndOfDay()
  } else {
    // Fallback to the old implementation
    const selectedOffice = exchangeStore.selectedOffice || exchangeStore.offices[0]

    if (!selectedOffice) {
      notification.warning('Lütfen bir ofis seçiniz!')
      return
    }

    if (!confirm('Gün sonu işlemini yapmak istediğinizden emin misiniz?')) {
      return
    }

    try {
      await apiService.endDay(selectedOffice.officeId)
      notification.success('Gün sonu işlemi başarıyla tamamlandı!')
      router.push('/ihtiyar/vaults')
    } catch (error) {
      console.error('Gün sonu işlemi başarısız:', error)
      notification.error('Gün sonu işlemi başarısız oldu. Lütfen tekrar deneyiniz.')
    }
  }
}

// Handle keyboard shortcuts
const handleKeyPress = (e: KeyboardEvent) => {
  if ((e.ctrlKey || e.metaKey) && e.key === 'p' && route.path.includes('z-report')) {
    e.preventDefault()
    if (routerViewRef.value?.printReport) {
      routerViewRef.value.printReport()
    }
  }
}


// Initialize on mount
const handleVisibilityChange = () => {
  if (document.hidden) {
    if (tickerInterval) { clearInterval(tickerInterval); tickerInterval = null }
  } else {
    loadExchangeRates()
    tickerInterval = setInterval(loadExchangeRates, 60000) as unknown as number
  }
}

onMounted(async () => {
  // Load ticker preference from localStorage
  const savedTickerPreference = localStorage.getItem('showTicker')
  showTicker.value = savedTickerPreference === 'true' // Only show if explicitly set to 'true'

  await loadExchangeRates()
  // Refresh rates every 60 seconds (instead of 30)
  tickerInterval = setInterval(loadExchangeRates, 60000) as unknown as number
  // Add keyboard listener
  window.addEventListener('keydown', handleKeyPress)
  document.addEventListener('visibilitychange', handleVisibilityChange)
})

// Cleanup on unmount
onUnmounted(() => {
  if (tickerInterval) {
    clearInterval(tickerInterval)
  }
  window.removeEventListener('keydown', handleKeyPress)
  document.removeEventListener('visibilitychange', handleVisibilityChange)
})

const reloadPage = () => window.location.reload()

const toggleTicker = () => {
  showTicker.value = !showTicker.value
  localStorage.setItem('showTicker', String(showTicker.value))
}
</script>

<template>
  <div class="layout-root">

    <!-- ── Left Nav Sidebar ───────────────────────────── -->
    <aside class="nav-sidebar" :class="{ 'is-open': isMobileMenuOpen }">

      <!-- Brand -->
      <div class="nav-brand">
        <div class="brand-icon">
          <span class="material-symbols-outlined" aria-hidden="true">currency_exchange</span>
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
            <span class="nav-link-icon-badge" :style="{ '--nav-badge-color': categoryColor(group.cat) }">
              <span class="material-symbols-outlined nav-link-icon">{{ item.icon }}</span>
            </span>
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
            <div class="ud-row"><span class="material-symbols-outlined" aria-hidden="true">mail</span>{{ authStore.user?.mail }}</div>
          </div>
          <button class="ud-logout" @click="logout">
            <span class="material-symbols-outlined" aria-hidden="true">power_settings_new</span>
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
          <span class="material-symbols-outlined" aria-hidden="true">menu</span>
        </button>
        <h1 class="header-title">{{ pageTitle }}</h1>
        <div class="header-actions">
          <button class="header-icon-btn" @click="reloadPage" title="Sayfayı Yenile">
            <span class="material-symbols-outlined" aria-hidden="true">refresh</span>
          </button>
          <button
            class="header-icon-btn"
            :class="{ active: showTicker }"
            @click="toggleTicker"
            title="Kur Ticker'ı"
          >
            <span class="material-symbols-outlined" aria-hidden="true">show_chart</span>
          </button>
          <button class="header-user-btn" @click="isUserMenuOpen = !isUserMenuOpen">
            <div class="header-avatar">{{ (authStore.user?.firstname || '?')[0].toUpperCase() }}</div>
            <span class="header-user-name">{{ authStore.user?.firstname }}</span>
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size:16px">expand_more</span>
          </button>
        </div>
      </header>

      <!-- Ticker -->
      <div v-if="showTicker" class="ticker-bar">
        <div v-if="tickerStatus === 'loading'" class="ticker-fallback">Kurlar yükleniyor...</div>
        <div v-else-if="tickerStatus === 'error'" class="ticker-fallback">Kur bilgileri yüklenemedi</div>
        <div v-else-if="tickerRates.length === 0" class="ticker-fallback">Kur bilgisi bulunamadı</div>
        <div v-else class="ticker-inner">
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
  background: var(--color-bg-page);
}

/* ═══ Nav Sidebar ════════════════════════════════════ */
.nav-sidebar {
  width: 230px;
  background: linear-gradient(180deg, rgba(238,242,255,0.55), var(--glass-bg));
  backdrop-filter: var(--glass-blur-light); -webkit-backdrop-filter: var(--glass-blur-light);
  border-right: var(--border-crisp);
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
  border-bottom: 1px solid var(--color-border);
  flex-shrink: 0;
}
.brand-icon {
  width: 34px; height: 34px;
  background: linear-gradient(135deg, var(--color-primary), #8b5cf6);
  border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center;
  box-shadow: var(--shadow-glow-primary);
}
.brand-icon .material-symbols-outlined { color: #fff; font-size: 18px; }
.brand-name {
  font-size: 14px; font-weight: 700; color: var(--color-text); letter-spacing: -0.3px;
}

/* Nav menu */
.nav-menu {
  flex: 1;
  overflow-y: auto;
  padding: 12px 10px;
  scrollbar-width: thin;
  scrollbar-color: var(--color-border) transparent;
}
.nav-menu::-webkit-scrollbar { width: 4px; }
.nav-menu::-webkit-scrollbar-thumb { background: var(--color-border); border-radius: 2px; }

.nav-group-label {
  font-size: 10px; font-weight: 700; color: var(--color-text-muted);
  text-transform: uppercase; letter-spacing: .6px;
  padding: 10px 8px 4px; margin-top: 4px;
}
.nav-group-label:first-child { margin-top: 0; }

.nav-link {
  display: flex; align-items: center; gap: 10px;
  padding: 10px 12px;
  border-radius: var(--radius-md);
  border: var(--border-crisp);
  background: var(--color-bg-card);
  font-size: 13.5px; font-weight: 700; color: var(--color-text-secondary);
  text-decoration: none;
  transition: background-color .14s, color .14s, border-color .14s, box-shadow .14s, transform .14s;
  position: relative;
  margin-bottom: 6px;
  box-shadow: var(--shadow-md);
}
.nav-link:hover {
  background: var(--color-bg-page); color: var(--color-text);
  border-color: var(--color-primary);
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}
.nav-link.is-active {
  background: linear-gradient(135deg, var(--color-primary), #8b5cf6); color: #fff;
  border-color: var(--color-primary);
  box-shadow: var(--shadow-glow-primary);
}
.nav-link-icon { font-size: 15px; flex-shrink: 0; }
.nav-link-text { flex: 1; }

/* İkon rozetleri — AppKpiCard/topbar-btn ailesindeki "canlı ikon rozeti" dili */
.nav-link-icon-badge {
  width: 26px; height: 26px; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  border-radius: var(--radius-sm);
  background: var(--color-bg-page);
  transition: background-color .12s, transform .12s;
}
.nav-link-icon-badge .nav-link-icon {
  color: var(--nav-badge-color, var(--color-text-muted));
  font-variation-settings: 'FILL' 0, 'wght' 500, 'GRAD' 0, 'opsz' 24;
}
.nav-link:hover .nav-link-icon-badge { transform: scale(1.06); }
.nav-link.is-active .nav-link-icon-badge {
  background: rgba(255,255,255,0.22);
  box-shadow: 0 2px 8px -1px rgba(0,0,0,0.2);
}
.nav-link.is-active .nav-link-icon-badge .nav-link-icon {
  color: #fff;
  font-variation-settings: 'FILL' 1, 'wght' 600, 'GRAD' 0, 'opsz' 24;
}
.nav-badge {
  font-size: 9px; font-weight: 700; background: var(--color-danger); color: #fff;
  padding: 1px 5px; border-radius: var(--radius-sm); letter-spacing: .3px;
}

/* Footer */
.nav-footer {
  border-top: 1px solid var(--color-border);
  padding: 12px 10px;
  flex-shrink: 0;
  position: relative;
}
.nav-lang { margin-bottom: 8px; }

.nav-user-btn {
  display: flex; align-items: center; gap: 8px;
  width: 100%; padding: 8px 10px;
  border: none; background: transparent;
  border-radius: var(--radius-md); cursor: pointer;
  text-align: left; transition: background .12s;
}
.nav-user-btn:hover { background: var(--color-bg-page); }
.nav-avatar {
  width: 30px; height: 30px; border-radius: 50%;
  background: linear-gradient(135deg, var(--color-primary), #8b5cf6);
  color: #fff; font-size: 13px; font-weight: 700;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.nav-user-info { flex: 1; min-width: 0; }
.nav-user-name { display: block; font-size: 13px; font-weight: 600; color: var(--color-text); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.nav-user-role { display: block; font-size: 11px; color: var(--color-text-muted); }
.nav-user-chevron { font-size: 16px; color: var(--color-text-muted); }

/* User dropdown */
.user-dropdown {
  position: absolute; bottom: calc(100% + 4px); left: 10px; right: 10px;
  background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  box-shadow: var(--shadow-bold); z-index: 100; overflow: hidden;
}
.ud-header {
  display: flex; align-items: center; gap: 12px;
  padding: 16px; background: linear-gradient(135deg, var(--color-primary), #8b5cf6);
}
.ud-avatar {
  width: 42px; height: 42px; border-radius: 50%;
  background: rgba(255,255,255,.25); color: #fff; font-size: 16px; font-weight: 700;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.ud-name { font-size: 14px; font-weight: 700; color: #fff; }
.ud-username { font-size: 12px; color: rgba(255,255,255,.8); }
.ud-info { padding: 12px 16px; }
.ud-row { display: flex; align-items: center; gap: 8px; font-size: 12px; color: var(--color-text-secondary); }
.ud-row .material-symbols-outlined { font-size: 16px; color: var(--color-text-muted); }
.ud-logout {
  display: flex; align-items: center; gap: 8px; justify-content: center;
  width: 100%; padding: 10px 16px;
  border: none; background: #fef2f2; color: var(--color-danger);
  font-size: 13px; font-weight: 600; cursor: pointer;
  border-top: 1px solid var(--color-border);
}
.ud-logout .material-symbols-outlined { font-size: 16px; }
.ud-logout:hover { background: var(--color-danger-bg); }

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
  z-index: 39;
}

/* ═══ Right Area ══════════════════════════════════════ */
.right-area {
  flex: 1; display: flex; flex-direction: column; overflow: hidden;
}

/* Top Header */
.top-header {
  height: 56px; display: flex; align-items: center; gap: 12px;
  padding: 0 20px;
  background: rgba(255, 255, 255, 0.92); backdrop-filter: var(--glass-blur); -webkit-backdrop-filter: var(--glass-blur);
  border-bottom: 2px solid var(--color-border);
  flex-shrink: 0; z-index: 10;
}
.hamburger {
  display: none; padding: 6px; border: none; background: transparent;
  border-radius: var(--radius-md); cursor: pointer; color: var(--color-text);
}
.hamburger:hover { background: var(--color-bg-page); }
.hamburger .material-symbols-outlined { font-size: 22px; }
@media (max-width: 1023px) { .hamburger { display: flex; } }
.header-title {
  flex: 1; font-size: 15px; font-weight: 700; color: var(--color-text);
}
.header-actions { display: flex; align-items: center; gap: 8px; }
.header-icon-btn {
  width: 34px; height: 34px; display: flex; align-items: center; justify-content: center;
  border: none; background: transparent; border-radius: var(--radius-md); cursor: pointer; color: var(--color-text-secondary);
}
.header-icon-btn:hover { background: var(--color-bg-page); color: var(--color-text); }
.header-icon-btn.active { background: var(--color-primary-light); color: var(--color-primary); }
.header-icon-btn .material-symbols-outlined { font-size: 20px; }
.header-user-btn {
  display: flex; align-items: center; gap: 8px;
  padding: 5px 10px; border: 1px solid var(--color-border); border-radius: var(--radius-xl);
  background: transparent; cursor: pointer; font-size: 13px; font-weight: 500; color: var(--color-text);
}
.header-user-btn:hover { background: var(--color-bg-page); }
.header-avatar {
  width: 26px; height: 26px; border-radius: 50%;
  background: linear-gradient(135deg, var(--color-primary), #8b5cf6);
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
  will-change: transform;
}
.ticker-office { background: var(--color-warning); color: #fff; padding: 2px 8px; border-radius: 4px; font-weight: 700; font-size: 11px; }
.ticker-item { display: inline-flex; align-items: center; gap: 4px; }
.ticker-buy { color: #4ade80; }
.ticker-sell { color: #f87171; }
.ticker-sep { color: #475569; }
.ticker-fallback {
  display: flex; align-items: center; justify-content: center;
  height: 100%; font-size: 12px; color: #94a3b8;
}
@keyframes ticker { from { transform: translateX(0); } to { transform: translateX(-33.333%); } }

/* ═══ Body Area ══════════════════════════════════════ */
.body-area {
  flex: 1; display: flex; overflow: hidden;
}

/* ═══ Main Content ══════════════════════════════════ */
.main-content {
  flex: 1; overflow-y: auto; padding: 20px 24px;
}
@media (max-width: 768px) { .main-content { padding: 12px 16px; } }
@media (max-width: 640px) {
  .header-icon-btn {
    min-width: 44px;
    min-height: 44px;
  }
  .header-user-btn {
    min-height: 44px;
  }
}

/* Material symbols */
.material-symbols-outlined {
  font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
</style>
