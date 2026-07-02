<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'

const router = useRouter()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()

const isLoading = ref(true)
const dashboard = ref<any>(null)
const now = new Date()
const dateStr = now.toLocaleDateString('tr-TR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })

const fmtNum = (n: number | null | undefined, dec = 0) =>
  new Intl.NumberFormat('tr-TR', { minimumFractionDigits: dec, maximumFractionDigits: dec }).format(n ?? 0)
const fmtMoney = (n: number | null | undefined) => n == null ? '—' : fmtNum(n, 2)
const plColor = (n: number) => Number(n ?? 0) >= 0 ? '#22c55e' : '#ef4444'
const plSign = (n: number) => Number(n ?? 0) >= 0 ? '+' : ''
const g = (o: any, k: string) => {
  if (!o) return undefined
  const lk = k[0].toLowerCase() + k.slice(1)
  return o[lk] !== undefined ? o[lk] : o[k]
}

// Tab system
type Tab = 'overview' | 'offices' | 'users' | 'qr'
const activeTab = ref<Tab>('overview')

// Owner: Offices
const offices = ref<any[]>([])
const officesLoading = ref(false)
const officesError = ref('')

async function loadOffices() {
  officesLoading.value = true
  officesError.value = ''
  try {
    offices.value = await apiService.getOfficeSummaries() ?? []
  } catch (e: any) {
    officesError.value = e?.response?.data?.message || e.message || 'Veri yüklenemedi'
  } finally {
    officesLoading.value = false
  }
}

// Kargıcak bir şube DEĞİL, merkezdir. Merkezi şubelerden ayırıyoruz.
const isMerkez = (o: any) => /merkez/i.test(o?.officeName ?? '')
// Merkez her zaman en üstte listelenir, ardından şubeler
const sortedOffices = computed(() =>
  [...offices.value].sort((a, b) => (isMerkez(b) ? 1 : 0) - (isMerkez(a) ? 1 : 0))
)
const merkezCount = computed(() => offices.value.filter(isMerkez).length)
const subeCount = computed(() => offices.value.filter(o => !isMerkez(o)).length)

const totalVaults = computed(() => offices.value.reduce((a, o) => a + (o.vaultCount ?? 0), 0))
const totalDailyPL = computed(() => offices.value.reduce((a, o) => a + (o.dailyProfitLoss ?? 0), 0))
const totalMonthlyPL = computed(() => offices.value.reduce((a, o) => a + (o.monthlyProfitLoss ?? 0), 0))

// Owner: QR
const qrInput = ref('')
const qrLabel = ref('')
const qrSize = ref(280)
const qrGenerated = ref(false)
const qrImgUrl = ref('')

function generateQr() {
  const text = qrInput.value.trim()
  if (!text) return
  qrImgUrl.value = `https://api.qrserver.com/v1/create-qr-code/?size=${qrSize.value}x${qrSize.value}&data=${encodeURIComponent(text)}&margin=10&ecc=M`
  qrGenerated.value = true
}

function downloadQr() {
  if (!qrImgUrl.value) return
  const a = document.createElement('a')
  a.href = qrImgUrl.value
  a.download = (qrLabel.value.trim() || 'qrcode') + '.png'
  a.target = '_blank'
  a.click()
}

function resetQr() {
  qrInput.value = ''
  qrLabel.value = ''
  qrGenerated.value = false
  qrImgUrl.value = ''
}

const currencyKeys = (obj: Record<string, number> | null | undefined) =>
  obj ? Object.entries(obj).slice(0, 4) : []

// Admin: User Management
const umUsers = ref<any[]>([])
const umOffices = ref<any[]>([])
const umLoading = ref(false)
const umError = ref('')
const umSearch = ref('')
const umSelected = ref<any>(null)
const umDetailTab = ref<'info' | 'password' | 'offices'>('info')
const umSaving = ref(false)
const umSaveError = ref('')
const umSaveOk = ref(false)
const umEditForm = ref({ username: '', mail: '', firstname: '', lastname: '', rank: 1 })
const umPwForm = ref({ newPassword: '', confirm: '' })
const umCreateForm = ref({ username: '', mail: '', password: '', firstname: '', lastname: '' })
const umCreateMode = ref(false)
const umUserOffices = ref<any[]>([])

const RANKS = [
  { value: 0, label: 'Yasaklı', color: '#ef4444', bg: '#fef2f2', ring: '#fca5a5' },
  { value: 1, label: 'Kullanıcı', color: '#6b7280', bg: '#f9fafb', ring: '#d1d5db' },
  { value: 2, label: 'Müşteri', color: '#0ea5e9', bg: '#f0f9ff', ring: '#7dd3fc' },
  { value: 50, label: 'Personel', color: '#8b5cf6', bg: '#f5f3ff', ring: '#c4b5fd' },
  { value: 99, label: 'Admin', color: '#dc2626', bg: '#fef2f2', ring: '#fca5a5' },
  { value: 100, label: 'Owner', color: '#d97706', bg: '#fffbeb', ring: '#fcd34d' },
]

function rankInfo(rank: any) { return RANKS.find(r => r.label === rank || r.value === rank) ?? RANKS[1] }
function rankValue(rank: any) { return typeof rank === 'number' ? rank : (RANKS.find(r => r.label === rank)?.value ?? 1) }
function avatarColor(name: string) {
  const colors = ['#6366f1','#8b5cf6','#ec4899','#f97316','#14b8a6','#0ea5e9','#84cc16','#ef4444']
  return colors[(name?.charCodeAt(0) ?? 0) % colors.length]
}

const umFiltered = computed(() => {
  const q = umSearch.value.toLowerCase()
  if (!q) return umUsers.value
  return umUsers.value.filter(u =>
    u.username?.toLowerCase().includes(q) || u.mail?.toLowerCase().includes(q) ||
    (u.firstname + ' ' + u.lastname).toLowerCase().includes(q)
  )
})

const umAssignedIds = computed(() => new Set(umUserOffices.value.map((o: any) => o.officeId ?? o.id)))

const umActiveCount = computed(() => umUsers.value.filter(u => rankValue(u.rank) > 0).length)
const umAdminCount = computed(() => umUsers.value.filter(u => rankValue(u.rank) >= 99).length)
const umStaffCount = computed(() => umUsers.value.filter(u => rankValue(u.rank) === 50).length)
const umBannedCount = computed(() => umUsers.value.filter(u => rankValue(u.rank) === 0).length)

async function loadUsers() {
  umLoading.value = true; umError.value = ''
  try {
    const [u, o] = await Promise.all([apiService.getUsers(), apiService.getOffices()])
    umUsers.value = u ?? []; umOffices.value = o ?? []
  } catch (e: any) { umError.value = e.response?.data?.message || 'Veriler yüklenemedi' }
  finally { umLoading.value = false }
}

async function umSelectUser(user: any) {
  umSelected.value = user; umCreateMode.value = false; umDetailTab.value = 'info'
  umSaveError.value = ''; umSaveOk.value = false
  umEditForm.value = { username: user.username ?? '', mail: user.mail ?? '', firstname: user.firstname ?? '', lastname: user.lastname ?? '', rank: rankValue(user.rank) }
  umPwForm.value = { newPassword: '', confirm: '' }
  umUserOffices.value = []
  try { umUserOffices.value = await apiService.getUserOffices(user.id) ?? [] } catch { umUserOffices.value = [] }
}

function umOpenCreate() {
  umSelected.value = null; umCreateMode.value = true; umDetailTab.value = 'info'
  umSaveError.value = ''; umSaveOk.value = false
  umCreateForm.value = { username: '', mail: '', password: '', firstname: '', lastname: '' }
}

async function umSaveInfo() {
  umSaveError.value = ''; umSaveOk.value = false; umSaving.value = true
  try {
    await apiService.updateUser({ Id: umSelected.value.id, ...umEditForm.value })
    umSaveOk.value = true; await loadUsers()
    const updated = umUsers.value.find(u => u.id === umSelected.value.id)
    if (updated) umSelected.value = updated
  } catch (e: any) { umSaveError.value = e.response?.data?.message || 'Kaydedilemedi' }
  finally { umSaving.value = false }
}

async function umCreateUser() {
  umSaveError.value = ''; umSaveOk.value = false
  const f = umCreateForm.value
  if (!f.username || !f.mail || !f.password) { umSaveError.value = 'Kullanıcı adı, e-posta ve şifre zorunludur.'; return }
  umSaving.value = true
  try {
    await apiService.registerUser(f); umSaveOk.value = true; await loadUsers()
    setTimeout(() => { umCreateMode.value = false }, 1200)
  } catch (e: any) { umSaveError.value = e.response?.data?.message || 'Kullanıcı oluşturulamadı' }
  finally { umSaving.value = false }
}

async function umSavePassword() {
  umSaveError.value = ''; umSaveOk.value = false
  if (!umPwForm.value.newPassword) { umSaveError.value = 'Şifre boş olamaz'; return }
  if (umPwForm.value.newPassword !== umPwForm.value.confirm) { umSaveError.value = 'Şifreler eşleşmiyor'; return }
  umSaving.value = true
  try {
    await apiService.changeUserPassword(umSelected.value.id, umPwForm.value.newPassword)
    umSaveOk.value = true; umPwForm.value = { newPassword: '', confirm: '' }
  } catch (e: any) { umSaveError.value = e.response?.data?.message || 'Şifre değiştirilemedi' }
  finally { umSaving.value = false }
}

async function umConfirmDelete() {
  if (!umSelected.value) return
  const name = (umSelected.value.firstname || '') + ' ' + (umSelected.value.lastname || umSelected.value.username)
  if (!confirm(`"${name.trim()}" kullanıcısını yasaklamak (erişimini kaldırmak) istediğinize emin misiniz?`)) return
  try {
    await apiService.updateUser({ Id: umSelected.value.id, rank: 0 })
    await loadUsers()
    const updated = umUsers.value.find(u => u.id === umSelected.value.id)
    if (updated) { umSelected.value = updated; umEditForm.value.rank = 0 }
  } catch (e: any) { umError.value = e.response?.data?.message || 'İşlem başarısız'; setTimeout(() => umError.value = '', 3000) }
}

async function umToggleOffice(office: any) {
  if (!umSelected.value || !authStore.isAdmin) return
  const officeId = office.officeId ?? office.id
  try {
    if (umAssignedIds.value.has(officeId)) {
      await apiService.removeOfficeFromUser(umSelected.value.id, officeId)
      umUserOffices.value = umUserOffices.value.filter((o: any) => (o.officeId ?? o.id) !== officeId)
    } else {
      await apiService.attachOfficeToUser({ userId: umSelected.value.id, officeId })
      umUserOffices.value = [...umUserOffices.value, { officeId, ...office }]
    }
  } catch (e: any) { umError.value = e.response?.data?.message || 'İşlem başarısız'; setTimeout(() => umError.value = '', 3000) }
}

// Dashboard data
const sum = computed(() => dashboard.value?.summary ?? {})

const topCurrencies = computed(() =>
  (dashboard.value?.currencyDistribution?.currencies ?? [])
    .filter((c: any) => c.totalAmount > 0)
    .sort((a: any, b: any) => b.totalValueInBaseCurrency - a.totalValueInBaseCurrency)
    .slice(0, 6)
)
const totalFV = computed(() => {
  const v = dashboard.value?.currencyDistribution?.totalForeignCurrencyValue
  return v && v > 0 ? v : 1
})

const adminKpi = computed(() => {
  const s = sum.value
  const cards = []
  if (authStore.isOwner) cards.push({ icon: 'account_balance', label: 'Toplam Varlık', value: fmtNum(g(s, 'totalAssets')), unit: '₺', color: '#7c3aed', bg: '#f3f0ff' })
  const tp = Number(g(s, 'todayProfit') ?? 0)
  cards.push({ icon: 'trending_up', label: 'Bugün K/Z', value: plSign(tp) + fmtNum(tp), unit: '₺', color: plColor(tp), bg: tp >= 0 ? '#f0fdf4' : '#fef2f2' })
  const mp = Number(g(s, 'monthlyProfit') ?? 0)
  cards.push({ icon: 'calendar_month', label: 'Aylık K/Z', value: plSign(mp) + fmtNum(mp), unit: '₺', color: plColor(mp), bg: mp >= 0 ? '#f0fdf4' : '#fef2f2' })
  cards.push({ icon: 'receipt_long', label: 'Bugün İşlem', value: String(g(s, 'todayTransactionCount') ?? 0), unit: 'adet', color: '#6366f1', bg: '#eef2ff' })
  if (authStore.isOwner) cards.push({ icon: 'currency_exchange', label: 'Döviz Varlık', value: fmtNum(g(s, 'totalForeignCurrencyValue')), unit: '₺', color: '#2563eb', bg: '#eff6ff' })
  cards.push({ icon: 'people', label: 'Cari Net', value: fmtNum(g(s, 'netPartyBalance')), unit: '₺', color: '#d97706', bg: '#fffbeb' })
  return cards
})

const staffKpi = computed(() => {
  const s = sum.value
  const tp = Number(g(s, 'todayProfit') ?? 0)
  return [
    { icon: 'receipt_long', label: 'Bugünkü İşlem', value: String(g(s, 'todayTransactionCount') ?? 0), unit: 'adet', color: '#6366f1', bg: '#eef2ff' },
    { icon: 'trending_up', label: 'Bugün K/Z', value: plSign(tp) + fmtNum(tp), unit: '₺', color: plColor(tp), bg: tp >= 0 ? '#f0fdf4' : '#fef2f2' },
  ]
})

const quickLinks = [
  { p: '/ihtiyar/exchange-v2?type=buy&currency=fiat', i: 'south_east', l: 'Döviz Al' },
  { p: '/ihtiyar/exchange-v2?type=sell&currency=fiat', i: 'north_east', l: 'Döviz Sat' },
  { p: '/ihtiyar/exchange-v2?type=buy&currency=usdt', i: 'generating_tokens', l: 'USDT Al' },
  { p: '/ihtiyar/exchange-v2?type=sell&currency=usdt', i: 'token', l: 'USDT Sat' },
  { p: '/ihtiyar/z-report', i: 'insert_chart', l: 'Z-Raporu' },
  { p: '/ihtiyar/parties', i: 'contacts', l: 'Cariler' },
  { p: '/ihtiyar/vaults', i: 'account_balance_wallet', l: 'Kasalar' },
  { p: '/ihtiyar/expenses', i: 'receipt_long', l: 'Giderler' },
]

const adminQuickLinks = computed(() => {
  const links = [...quickLinks]
  if (authStore.isAdmin) {
    links.push({ p: '/ihtiyar/users', i: 'manage_accounts', l: 'Kullanıcılar' })
    links.push({ p: '/ihtiyar/auto-rate-management', i: 'currency_exchange', l: 'Kur Yönetimi' })
    links.push({ p: '/ihtiyar/currencies', i: 'payments', l: 'Para Birimleri' })
  }
  return links
})

async function load() {
  isLoading.value = true
  try {
    const calls: Promise<any>[] = [apiService.getDashboardData()]
    if (authStore.isOwner) calls.push(loadOffices())
    const [db] = await Promise.all(calls)
    dashboard.value = db
  } catch (e) { console.error(e) }
  finally { isLoading.value = false }
}

let timer: ReturnType<typeof setInterval>
onMounted(() => { load(); timer = setInterval(load, 60000) })
onUnmounted(() => clearInterval(timer))
</script>

<template>
  <div class="db">

    <div v-if="isLoading" class="db-loading">
      <div class="spinner"></div><span>Yükleniyor...</span>
    </div>

    <template v-else>
      <!-- Header -->
      <div class="db-header">
        <div>
          <h1 class="db-title">Hoş Geldiniz, {{ authStore.user?.firstname ?? authStore.user?.username }}</h1>
          <p class="db-date">{{ dateStr }}</p>
        </div>
        <button class="refresh-btn" @click="load">
          <span class="material-symbols-outlined">refresh</span> Yenile
        </button>
      </div>

      <!-- ═══════════════════════════ STAFF VIEW ═══ -->
      <template v-if="!authStore.isAdmin">
        <div class="kpi-grid-small">
          <div v-for="k in staffKpi" :key="k.label" class="kpi-card" :style="{'--kc':k.color,'--kb':k.bg}">
            <div class="kpi-icon"><span class="material-symbols-outlined">{{ k.icon }}</span></div>
            <div class="kpi-body">
              <p class="kpi-label">{{ k.label }}</p>
              <p class="kpi-value">{{ k.value }} <span class="kpi-unit">{{ k.unit }}</span></p>
            </div>
          </div>
        </div>

      </template>

      <!-- ═══════════════════════════ ADMIN / OWNER VIEW ═══ -->
      <template v-else>
        <!-- KPI Cards -->
        <div class="kpi-grid">
          <div v-for="k in adminKpi" :key="k.label" class="kpi-card" :style="{'--kc':k.color,'--kb':k.bg}">
            <div class="kpi-icon"><span class="material-symbols-outlined">{{ k.icon }}</span></div>
            <div class="kpi-body">
              <p class="kpi-label">{{ k.label }}</p>
              <p class="kpi-value" :style="{color:k.color}">{{ k.value }} <span class="kpi-unit">{{ k.unit }}</span></p>
            </div>
          </div>
        </div>

        <!-- Tab Navigation (Owner gets extra tabs) -->
        <div class="db-tabs">
          <button class="db-tab" :class="{ active: activeTab === 'overview' }" @click="activeTab = 'overview'">
            <span class="material-symbols-outlined">dashboard</span> Genel Bakış
          </button>
          <button v-if="authStore.isOwner" class="db-tab" :class="{ active: activeTab === 'offices' }" @click="activeTab = 'offices'">
            <span class="material-symbols-outlined">store</span> Şube Yönetimi
          </button>
          <button v-if="authStore.isAdmin" class="db-tab" :class="{ active: activeTab === 'users' }" @click="activeTab = 'users'; if (!umUsers.length) loadUsers()">
            <span class="material-symbols-outlined">manage_accounts</span> Kullanıcı Yönetimi
          </button>
          <button v-if="authStore.isOwner" class="db-tab" :class="{ active: activeTab === 'qr' }" @click="activeTab = 'qr'">
            <span class="material-symbols-outlined">qr_code_2</span> QR Oluştur
          </button>
        </div>

        <!-- ── Tab: Genel Bakış ── -->
        <template v-if="activeTab === 'overview'">
          <!-- Currency Distribution -->
          <div class="db-row" v-if="authStore.isOwner && topCurrencies.length">
            <div class="panel">
              <div class="panel-header">
                <span class="material-symbols-outlined">pie_chart</span><h3>Döviz Dağılımı</h3>
                <button class="see-all" @click="router.push('/ihtiyar/vaults')">Detay <span class="material-symbols-outlined">chevron_right</span></button>
              </div>
              <div class="curr-list">
                <div v-for="c in topCurrencies" :key="c.currencyCode" class="curr-row">
                  <span class="curr-code">{{ c.currencyCode }}</span>
                  <div class="cr-bar-wrap"><div class="cr-bar" :style="{width:Math.min(100,c.totalValueInBaseCurrency/totalFV*100)+'%'}"></div></div>
                  <span class="curr-try">{{ fmtNum(c.totalValueInBaseCurrency) }} ₺</span>
                </div>
              </div>
            </div>
          </div>
        </template>

        <!-- ── Tab: Şube Yönetimi ── -->
        <template v-if="activeTab === 'offices' && authStore.isOwner">
          <div class="sec-stats">
            <div class="sec-stat-card">
              <div class="sec-stat-icon amber"><span class="material-symbols-outlined">hub</span></div>
              <div class="sec-stat-body">
                <div class="sec-stat-val">{{ merkezCount }}</div>
                <div class="sec-stat-lbl">Merkez</div>
              </div>
            </div>
            <div class="sec-stat-card">
              <div class="sec-stat-icon blue"><span class="material-symbols-outlined">store</span></div>
              <div class="sec-stat-body">
                <div class="sec-stat-val">{{ subeCount }}</div>
                <div class="sec-stat-lbl">Şube</div>
              </div>
            </div>
            <div class="sec-stat-card">
              <div class="sec-stat-icon purple"><span class="material-symbols-outlined">account_balance_wallet</span></div>
              <div class="sec-stat-body">
                <div class="sec-stat-val">{{ totalVaults }}</div>
                <div class="sec-stat-lbl">Toplam Kasa</div>
              </div>
            </div>
            <div class="sec-stat-card">
              <div class="sec-stat-icon" :class="totalDailyPL >= 0 ? 'green' : 'red'">
                <span class="material-symbols-outlined">trending_up</span>
              </div>
              <div class="sec-stat-body">
                <div class="sec-stat-val" :style="{color: plColor(totalDailyPL)}">{{ fmtMoney(totalDailyPL) }} ₺</div>
                <div class="sec-stat-lbl">Günlük K/Z</div>
              </div>
            </div>
            <div class="sec-stat-card">
              <div class="sec-stat-icon" :class="totalMonthlyPL >= 0 ? 'green' : 'red'">
                <span class="material-symbols-outlined">calendar_month</span>
              </div>
              <div class="sec-stat-body">
                <div class="sec-stat-val" :style="{color: plColor(totalMonthlyPL)}">{{ fmtMoney(totalMonthlyPL) }} ₺</div>
                <div class="sec-stat-lbl">Aylık K/Z</div>
              </div>
            </div>
          </div>

          <div class="panel">
            <div class="panel-header">
              <span class="material-symbols-outlined">store</span><h3>Merkez &amp; Şubeler</h3>
              <div class="header-actions">
                <button class="action-btn add" @click="router.push('/ihtiyar/dashboard?tab=offices&action=add')">
                  <span class="material-symbols-outlined">add_business</span> Şube Ekle
                </button>
                <button class="action-btn remove" @click="router.push('/ihtiyar/dashboard?tab=offices&action=remove')">
                  <span class="material-symbols-outlined">remove_circle_outline</span> Şube Kaldır
                </button>
              </div>
              <button class="refresh-btn-sm" @click="loadOffices">
                <span class="material-symbols-outlined">refresh</span>
              </button>
            </div>
            <div v-if="officesLoading" class="state-msg">
              <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
            </div>
            <div v-else-if="officesError" class="state-msg error">
              <span class="material-symbols-outlined">error</span> {{ officesError }}
            </div>
            <div v-else-if="offices.length === 0" class="state-msg">
              <span class="material-symbols-outlined">store</span> Şube bulunamadı
            </div>
            <div v-else class="o-grid">
              <div v-for="o in sortedOffices" :key="o.officeId" class="o-card" :class="{ merkez: isMerkez(o) }">
                <div class="o-card-accent" :class="{ merkez: isMerkez(o) }"></div>
                <div class="o-card-head">
                  <div class="o-avatar" :class="{ merkez: isMerkez(o) }">
                    <span class="material-symbols-outlined">{{ isMerkez(o) ? 'hub' : 'store' }}</span>
                  </div>
                  <div class="o-title">
                    <div class="o-name">{{ o.officeName }}</div>
                    <div class="o-sub"><span class="material-symbols-outlined">account_balance_wallet</span>{{ o.vaultCount }} kasa</div>
                  </div>
                  <span v-if="isMerkez(o)" class="o-status merkez"><span class="material-symbols-outlined">verified</span>Merkez</span>
                  <span v-else class="o-status"><span class="o-dot"></span>Şube</span>
                </div>
                <div class="o-asset">
                  <span class="o-asset-lbl">Toplam Varlık</span>
                  <span class="o-asset-val">{{ fmtMoney(o.totalValueInBaseCurrency) }} ₺</span>
                </div>
                <div class="o-metrics">
                  <div class="o-metric">
                    <span class="o-metric-lbl">Günlük K/Z</span>
                    <span class="o-metric-val" :style="{color: plColor(o.dailyProfitLoss)}">
                      <span class="material-symbols-outlined">{{ o.dailyProfitLoss >= 0 ? 'arrow_upward' : 'arrow_downward' }}</span>
                      {{ fmtMoney(o.dailyProfitLoss) }} ₺
                    </span>
                  </div>
                  <div class="o-metric">
                    <span class="o-metric-lbl">Aylık K/Z</span>
                    <span class="o-metric-val" :style="{color: plColor(o.monthlyProfitLoss)}">
                      <span class="material-symbols-outlined">{{ o.monthlyProfitLoss >= 0 ? 'arrow_upward' : 'arrow_downward' }}</span>
                      {{ fmtMoney(o.monthlyProfitLoss) }} ₺
                    </span>
                  </div>
                </div>
                <div class="o-footer" v-if="o.totalBalancesByCurrency && Object.keys(o.totalBalancesByCurrency).length">
                  <span v-for="[cur, bal] in currencyKeys(o.totalBalancesByCurrency)" :key="cur" class="currency-badge">
                    {{ cur }} {{ fmtMoney(bal) }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </template>

        <!-- ── Tab: Kullanıcı Yönetimi ── -->
        <template v-if="activeTab === 'users' && authStore.isAdmin">
          <div class="sec-stats">
            <div class="sec-stat-card">
              <div class="sec-stat-icon blue"><span class="material-symbols-outlined">group</span></div>
              <div class="sec-stat-body">
                <div class="sec-stat-val">{{ umUsers.length }}</div>
                <div class="sec-stat-lbl">Toplam Kullanıcı</div>
              </div>
            </div>
            <div class="sec-stat-card">
              <div class="sec-stat-icon green"><span class="material-symbols-outlined">check_circle</span></div>
              <div class="sec-stat-body">
                <div class="sec-stat-val">{{ umActiveCount }}</div>
                <div class="sec-stat-lbl">Aktif</div>
              </div>
            </div>
            <div class="sec-stat-card">
              <div class="sec-stat-icon purple"><span class="material-symbols-outlined">shield_person</span></div>
              <div class="sec-stat-body">
                <div class="sec-stat-val">{{ umAdminCount }}</div>
                <div class="sec-stat-lbl">Admin / Owner</div>
              </div>
            </div>
            <div class="sec-stat-card">
              <div class="sec-stat-icon" :class="umBannedCount > 0 ? 'red' : 'gray'">
                <span class="material-symbols-outlined">block</span>
              </div>
              <div class="sec-stat-body">
                <div class="sec-stat-val">{{ umBannedCount }}</div>
                <div class="sec-stat-lbl">Yasaklı</div>
              </div>
            </div>
          </div>

          <div class="panel">
            <div class="panel-header">
              <span class="material-symbols-outlined">manage_accounts</span>
              <h3>Kullanıcı Detayları</h3>
              <div class="header-actions">
                <button v-if="authStore.isOwner" class="action-btn add" @click="umOpenCreate">
                  <span class="material-symbols-outlined">person_add</span> Kullanıcı Ekle
                </button>
                <button v-if="authStore.isOwner && umSelected && !umCreateMode" class="action-btn remove" @click="umConfirmDelete">
                  <span class="material-symbols-outlined">person_remove</span> Kullanıcı Kaldır
                </button>
              </div>
              <button class="refresh-btn-sm" @click="loadUsers">
                <span class="material-symbols-outlined" :class="{ spin: umLoading }">refresh</span>
              </button>
            </div>

            <div v-if="umError" class="um-error-bar">{{ umError }}</div>

            <div v-if="umLoading" class="state-msg">
              <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
            </div>

            <template v-else>
              <!-- Search -->
              <div class="um-toolbar">
                <div class="um-search-wrap">
                  <span class="material-symbols-outlined um-search-icon">search</span>
                  <input v-model="umSearch" class="um-search" placeholder="Kullanıcı ara..." />
                </div>
                <span class="panel-badge">{{ umFiltered.length }} / {{ umUsers.length }}</span>
              </div>

              <div class="um-layout">
                <!-- User Cards List -->
                <div class="um-list">
                  <div v-if="!umFiltered.length" class="state-msg">Kullanıcı bulunamadı</div>
                  <div v-for="u in umFiltered" :key="u.id" class="um-card" :class="{ active: umSelected?.id === u.id && !umCreateMode }" @click="umSelectUser(u)">
                    <div class="um-avatar" :style="{ background: avatarColor(u.firstname || u.username) }">
                      {{ (u.firstname || u.username || '?')[0].toUpperCase() }}
                    </div>
                    <div class="um-card-body">
                      <div class="um-card-name">{{ u.firstname }} {{ u.lastname }}</div>
                      <div class="um-card-mail"><span class="material-symbols-outlined">alternate_email</span>{{ u.username }}</div>
                    </div>
                    <span class="um-rank-badge" :style="{ color: rankInfo(u.rank).color, background: rankInfo(u.rank).bg, border: '1px solid ' + rankInfo(u.rank).ring }">
                      {{ rankInfo(u.rank).label }}
                    </span>
                  </div>
                </div>

                <!-- Detail Panel -->
                <div class="um-detail">
                  <div v-if="!umSelected && !umCreateMode" class="um-placeholder">
                    <span class="material-symbols-outlined">manage_accounts</span>
                    <p>Listeden bir kullanıcı seçin</p>
                  </div>

                  <!-- Create Mode -->
                  <template v-if="umCreateMode">
                    <div class="um-detail-header">
                      <div class="um-detail-avatar" style="background:#6366f1">
                        <span class="material-symbols-outlined" style="font-size:20px">person_add</span>
                      </div>
                      <div><div class="um-detail-name">Yeni Kullanıcı</div></div>
                    </div>
                    <div class="um-detail-body">
                      <div class="um-field-row">
                        <div class="um-field"><label>Ad</label><input v-model="umCreateForm.firstname" class="um-input" placeholder="Ad" /></div>
                        <div class="um-field"><label>Soyad</label><input v-model="umCreateForm.lastname" class="um-input" placeholder="Soyad" /></div>
                      </div>
                      <div class="um-field"><label>Kullanıcı Adı *</label><input v-model="umCreateForm.username" class="um-input" placeholder="kullanici_adi" /></div>
                      <div class="um-field"><label>E-posta *</label><input v-model="umCreateForm.mail" type="email" class="um-input" placeholder="ornek@email.com" /></div>
                      <div class="um-field"><label>Şifre *</label><input v-model="umCreateForm.password" type="password" class="um-input" placeholder="••••••••" /></div>
                      <div v-if="umSaveOk" class="um-msg ok">Kullanıcı oluşturuldu</div>
                      <div v-if="umSaveError" class="um-msg err">{{ umSaveError }}</div>
                      <button class="um-btn primary" @click="umCreateUser" :disabled="umSaving">
                        {{ umSaving ? 'Oluşturuluyor...' : 'Kullanıcı Oluştur' }}
                      </button>
                    </div>
                  </template>

                  <!-- Edit Mode -->
                  <template v-if="umSelected && !umCreateMode">
                    <div class="um-detail-header">
                      <div class="um-detail-avatar" :style="{ background: avatarColor(umSelected.firstname || umSelected.username) }">
                        {{ (umSelected.firstname || umSelected.username || '?')[0].toUpperCase() }}
                      </div>
                      <div class="um-detail-identity">
                        <div class="um-detail-name">{{ umSelected.firstname }} {{ umSelected.lastname }}</div>
                        <div class="um-detail-sub">@{{ umSelected.username }}</div>
                      </div>
                      <span class="um-rank-badge" :style="{ color: rankInfo(umSelected.rank).color, background: rankInfo(umSelected.rank).bg, border: '1px solid ' + rankInfo(umSelected.rank).ring }">
                        {{ rankInfo(umSelected.rank).label }}
                      </span>
                    </div>

                    <div class="um-tabs">
                      <button class="um-tab" :class="{ active: umDetailTab === 'info' }" @click="umDetailTab = 'info'; umSaveError = ''; umSaveOk = false">
                        <span class="material-symbols-outlined">edit</span> Bilgiler
                      </button>
                      <button class="um-tab" :class="{ active: umDetailTab === 'password' }" @click="umDetailTab = 'password'; umSaveError = ''; umSaveOk = false">
                        <span class="material-symbols-outlined">key</span> Şifre
                      </button>
                      <button v-if="authStore.isAdmin" class="um-tab" :class="{ active: umDetailTab === 'offices' }" @click="umDetailTab = 'offices'; umSaveError = ''; umSaveOk = false">
                        <span class="material-symbols-outlined">store</span> Ofisler
                      </button>
                    </div>

                    <div v-if="umDetailTab === 'info'" class="um-detail-body">
                      <div class="um-field-row">
                        <div class="um-field"><label>Ad</label><input v-model="umEditForm.firstname" class="um-input" :disabled="!authStore.isOwner" /></div>
                        <div class="um-field"><label>Soyad</label><input v-model="umEditForm.lastname" class="um-input" :disabled="!authStore.isOwner" /></div>
                      </div>
                      <div class="um-field"><label>Kullanıcı Adı</label><input v-model="umEditForm.username" class="um-input" :disabled="!authStore.isOwner" /></div>
                      <div class="um-field"><label>E-posta</label><input v-model="umEditForm.mail" type="email" class="um-input" :disabled="!authStore.isOwner" /></div>
                      <div class="um-field">
                        <label>Yetki Seviyesi</label>
                        <div class="um-rank-grid">
                          <button v-for="r in RANKS" :key="r.value" class="um-rank-chip" :class="{ selected: umEditForm.rank === r.value }"
                            :style="umEditForm.rank === r.value ? { background: r.bg, color: r.color, borderColor: r.ring } : {}"
                            @click="authStore.isOwner && (umEditForm.rank = r.value)">{{ r.label }}</button>
                        </div>
                      </div>
                      <div v-if="umSaveOk" class="um-msg ok">Kaydedildi</div>
                      <div v-if="umSaveError" class="um-msg err">{{ umSaveError }}</div>
                      <button v-if="authStore.isOwner" class="um-btn primary" @click="umSaveInfo" :disabled="umSaving">
                        {{ umSaving ? 'Kaydediliyor...' : 'Kaydet' }}
                      </button>
                    </div>

                    <div v-if="umDetailTab === 'password'" class="um-detail-body">
                      <div class="um-field"><label>Yeni Şifre</label><input v-model="umPwForm.newPassword" type="password" class="um-input" placeholder="••••••••" :disabled="!authStore.isOwner" /></div>
                      <div class="um-field"><label>Şifre Tekrar</label><input v-model="umPwForm.confirm" type="password" class="um-input" placeholder="••••••••" :disabled="!authStore.isOwner" /></div>
                      <div v-if="umSaveOk" class="um-msg ok">Şifre değiştirildi</div>
                      <div v-if="umSaveError" class="um-msg err">{{ umSaveError }}</div>
                      <button v-if="authStore.isOwner" class="um-btn primary" @click="umSavePassword" :disabled="umSaving">
                        {{ umSaving ? 'Değiştiriliyor...' : 'Şifre Değiştir' }}
                      </button>
                    </div>

                    <div v-if="umDetailTab === 'offices'" class="um-detail-body">
                      <div v-for="o in umOffices" :key="o.officeId ?? o.id" class="um-office-row" @click="umToggleOffice(o)">
                        <span class="material-symbols-outlined" :style="{ color: umAssignedIds.has(o.officeId ?? o.id) ? '#22c55e' : '#d1d5db' }">
                          {{ umAssignedIds.has(o.officeId ?? o.id) ? 'check_circle' : 'radio_button_unchecked' }}
                        </span>
                        <span>{{ o.officeName ?? o.name }}</span>
                      </div>
                      <div v-if="!umOffices.length" class="state-msg">Ofis bulunamadı</div>
                    </div>
                  </template>
                </div>
              </div>
            </template>
          </div>
        </template>

        <!-- ── Tab: QR Oluştur ── -->
        <template v-if="activeTab === 'qr' && authStore.isOwner">
          <div class="panel">
            <div class="qr-layout">
              <div class="qr-form">
                <h3 class="qr-title">
                  <span class="material-symbols-outlined">qr_code_2</span> QR Kod Oluşturucu
                </h3>

                <div class="qr-field">
                  <label class="qr-label">İçerik / URL</label>
                  <textarea v-model="qrInput" class="qr-textarea" rows="3" placeholder="QR içeriğini girin (URL, metin, telefon, vb.)" @keydown.enter.prevent="generateQr" />
                </div>

                <div class="qr-field">
                  <label class="qr-label">Dosya Adı (opsiyonel)</label>
                  <input v-model="qrLabel" type="text" class="qr-input" placeholder="örn: bayi-ankara" />
                </div>

                <div class="qr-field">
                  <label class="qr-label">Boyut: {{ qrSize }}×{{ qrSize }} px</label>
                  <input v-model.number="qrSize" type="range" min="100" max="500" step="20" class="qr-range" />
                </div>

                <div class="qr-btns">
                  <button class="qr-btn primary" :disabled="!qrInput.trim()" @click="generateQr">
                    <span class="material-symbols-outlined">qr_code</span> Oluştur
                  </button>
                  <button class="qr-btn secondary" @click="resetQr">
                    <span class="material-symbols-outlined">refresh</span> Temizle
                  </button>
                </div>
              </div>

              <div class="qr-preview">
                <div v-if="!qrGenerated" class="qr-empty">
                  <span class="material-symbols-outlined">qr_code_2</span>
                  <p>Sol taraftan içerik girin ve<br><strong>Oluştur</strong>'a basın</p>
                </div>
                <div v-else class="qr-result">
                  <img :src="qrImgUrl" :alt="qrLabel || 'QR Kod'" class="qr-img" />
                  <p class="qr-caption">{{ qrLabel || 'QR Kod' }}</p>
                  <button class="qr-btn primary full-w" @click="downloadQr">
                    <span class="material-symbols-outlined">download</span> İndir (.png)
                  </button>
                </div>
              </div>
            </div>
          </div>
        </template>

      </template>
    </template>
  </div>
</template>

<style scoped>
.db { padding: 24px; display: flex; flex-direction: column; gap: 20px; }
.db-loading { display:flex; flex-direction:column; align-items:center; justify-content:center; height:60vh; gap:16px; color:#64748b; }
.spinner { width:40px; height:40px; border:3px solid #e2e8f0; border-top-color:#6366f1; border-radius:50%; animation:spin .8s linear infinite; }
@keyframes spin { to { transform:rotate(360deg); } }
.db-header { display:flex; align-items:center; justify-content:space-between; }
.db-title { font-size:1.4rem; font-weight:700; margin:0; color:#0f172a; }
.db-date  { font-size:.875rem; color:#64748b; margin:4px 0 0; }
.refresh-btn { display:flex; align-items:center; gap:6px; padding:8px 16px; background:#f8fafc; border:1px solid #e2e8f0; border-radius:8px; cursor:pointer; font-size:14px; font-weight:500; transition:.15s; }
.refresh-btn:hover { background:#e2e8f0; }
.refresh-btn-sm { display:flex; align-items:center; padding:6px; background:#f8fafc; border:1px solid #e2e8f0; border-radius:6px; cursor:pointer; margin-left:auto; }
.refresh-btn-sm:hover { background:#e2e8f0; }
.refresh-btn-sm .material-symbols-outlined { font-size:18px; color:#64748b; }

/* KPI */
.kpi-grid       { display:grid; grid-template-columns:repeat(auto-fill,minmax(150px,1fr)); gap:12px; }
.kpi-grid-small { display:grid; grid-template-columns:repeat(2,1fr); gap:12px; max-width:400px; }
.kpi-card { background:var(--kb); border:1px solid rgba(0,0,0,.06); border-radius:14px; padding:16px; display:flex; align-items:center; gap:12px; transition:.15s; }
.kpi-card:hover { transform:translateY(-2px); box-shadow:0 4px 16px rgba(0,0,0,.08); }
.kpi-icon { width:42px; height:42px; display:flex; align-items:center; justify-content:center; border-radius:10px; background:white; flex-shrink:0; }
.kpi-icon .material-symbols-outlined { font-size:22px; color:var(--kc); }
.kpi-label { font-size:10px; color:#6b7280; margin:0 0 3px; font-weight:600; text-transform:uppercase; letter-spacing:.04em; }
.kpi-value { font-size:17px; font-weight:800; color:#111; margin:0; }
.kpi-unit  { font-size:11px; font-weight:400; color:#9ca3af; }

/* Tabs */
.db-tabs { display:flex; gap:6px; padding:4px; background:#f1f5f9; border-radius:12px; }
.db-tab {
  display:flex; align-items:center; gap:7px;
  padding:10px 20px; background:none; border:none;
  border-radius:9px;
  cursor:pointer; font-size:13px; color:#64748b; font-weight:500;
  transition:all .2s ease;
  position:relative;
}
.db-tab:hover { color:#4f46e5; background:rgba(99,102,241,.06); }
.db-tab.active { color:#4f46e5; background:#fff; font-weight:600; box-shadow:0 1px 3px rgba(0,0,0,.08), 0 1px 2px rgba(0,0,0,.04); }
.db-tab .material-symbols-outlined { font-size:18px; }

/* Panels */
.panel { background:#fff; border:1px solid #e5e7eb; border-radius:14px; overflow:hidden; }
.panel-header { display:flex; align-items:center; gap:8px; padding:14px 18px; border-bottom:1px solid #f3f4f6; }
.panel-header h3 { margin:0; font-size:14px; font-weight:700; color:#111; flex:1; }
.panel-header .material-symbols-outlined { font-size:20px; color:#6366f1; }

.db-row { display:flex; flex-direction:column; gap:16px; }

/* Header action buttons */
.header-actions { display:flex; gap:6px; margin-left:auto; }
.action-btn { display:flex; align-items:center; gap:4px; padding:6px 12px; border:none; border-radius:6px; cursor:pointer; font-size:12px; font-weight:600; transition:.15s; }
.action-btn .material-symbols-outlined { font-size:16px; }
.action-btn.add { background:#f0fdf4; color:#16a34a; border:1px solid #bbf7d0; }
.action-btn.add:hover { background:#dcfce7; }
.action-btn.remove { background:#fef2f2; color:#dc2626; border:1px solid #fecaca; }
.action-btn.remove:hover { background:#fee2e2; }

/* Currency distribution */
.curr-list { padding:12px 16px; display:flex; flex-direction:column; gap:8px; }
.curr-row  { display:flex; align-items:center; gap:8px; }
.curr-code { font-weight:700; font-size:13px; color:#374151; width:52px; flex-shrink:0; }
.cr-bar-wrap { flex:1; background:#f3f4f6; border-radius:4px; height:8px; }
.cr-bar { height:8px; border-radius:4px; background:linear-gradient(90deg,#6366f1,#8b5cf6); transition:width .4s; }
.curr-try { font-size:13px; color:#374151; font-weight:600; width:110px; text-align:right; }

.see-all { display:flex; align-items:center; gap:2px; background:none; border:none; cursor:pointer; color:#6366f1; font-size:13px; font-weight:600; padding:4px 8px; border-radius:6px; margin-left:auto; }
.see-all:hover { background:#ede9fe; }
.see-all .material-symbols-outlined { font-size:16px; }

/* ═══ Shared Section Stats — modern ═══ */
.sec-stats { display:grid; grid-template-columns:repeat(auto-fit,minmax(190px,1fr)); gap:14px; }
.sec-stat-card {
  position:relative; overflow:hidden;
  background:#fff; border:1px solid #eef0f4; border-radius:18px;
  padding:20px; transition:transform .25s cubic-bezier(.4,0,.2,1), box-shadow .25s;
}
.sec-stat-card::after {
  content:''; position:absolute; right:-20px; top:-20px;
  width:90px; height:90px; border-radius:50%;
  background:var(--sc-glow,rgba(99,102,241,.08)); filter:blur(4px);
}
.sec-stat-card:hover { transform:translateY(-3px); box-shadow:0 12px 28px -8px rgba(30,41,59,.16); }
.sec-stat-icon { position:relative; z-index:1; width:44px; height:44px; border-radius:13px; display:flex; align-items:center; justify-content:center; box-shadow:0 4px 10px -3px var(--sc-glow,rgba(99,102,241,.3)); }
.sec-stat-icon .material-symbols-outlined { font-size:23px; color:#fff; }
.sec-stat-icon.blue   { background:linear-gradient(135deg,#3b82f6,#60a5fa); --sc-glow:rgba(59,130,246,.28); }
.sec-stat-icon.purple { background:linear-gradient(135deg,#8b5cf6,#a78bfa); --sc-glow:rgba(139,92,246,.28); }
.sec-stat-icon.green  { background:linear-gradient(135deg,#22c55e,#4ade80); --sc-glow:rgba(34,197,94,.28); }
.sec-stat-icon.red    { background:linear-gradient(135deg,#ef4444,#f87171); --sc-glow:rgba(239,68,68,.28); }
.sec-stat-icon.gray   { background:linear-gradient(135deg,#94a3b8,#cbd5e1); --sc-glow:rgba(148,163,184,.28); }
.sec-stat-icon.amber  { background:linear-gradient(135deg,#f59e0b,#fbbf24); --sc-glow:rgba(245,158,11,.28); }
.sec-stat-val  { position:relative; z-index:1; font-size:1.7rem; font-weight:800; color:#0f172a; line-height:1.1; margin-top:14px; letter-spacing:-.02em; }
.sec-stat-lbl  { position:relative; z-index:1; font-size:12px; color:#64748b; margin-top:4px; font-weight:600; }

/* ═══ Office Cards — modern grid ═══ */
.o-grid { display:grid; grid-template-columns:repeat(auto-fill,minmax(300px,1fr)); gap:16px; padding:18px; }
.o-card {
  position:relative; overflow:hidden;
  background:#fff; border:1px solid #eef0f4; border-radius:18px; padding:18px;
  transition:transform .25s cubic-bezier(.4,0,.2,1), box-shadow .25s, border-color .25s;
}
.o-card:hover { transform:translateY(-3px); box-shadow:0 16px 32px -12px rgba(30,41,59,.2); border-color:#c7d2fe; }
/* Merkez kartı özel — altın vurgulu, öne çıkar */
.o-card.merkez { border-color:#fcd34d; box-shadow:0 8px 24px -10px rgba(245,158,11,.28); }
.o-card.merkez:hover { border-color:#fbbf24; box-shadow:0 16px 34px -12px rgba(245,158,11,.4); }
.o-card-accent { position:absolute; top:0; left:0; right:0; height:4px; background:linear-gradient(90deg,#6366f1,#8b5cf6,#ec4899); }
.o-card-accent.merkez { background:linear-gradient(90deg,#f59e0b,#fbbf24,#fcd34d); }
.o-avatar.merkez { background:linear-gradient(135deg,#f59e0b,#fbbf24); box-shadow:0 6px 14px -4px rgba(245,158,11,.5); }
.o-status.merkez { background:#fffbeb; color:#d97706; }
.o-status.merkez .material-symbols-outlined { font-size:14px; }
.o-card-head { display:flex; align-items:center; gap:12px; }
.o-avatar { width:46px; height:46px; border-radius:14px; display:flex; align-items:center; justify-content:center; flex-shrink:0; background:linear-gradient(135deg,#6366f1,#818cf8); box-shadow:0 6px 14px -4px rgba(99,102,241,.5); }
.o-avatar .material-symbols-outlined { font-size:24px; color:#fff; }
.o-title { flex:1; min-width:0; }
.o-name { font-size:15px; font-weight:700; color:#0f172a; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
.o-sub { display:flex; align-items:center; gap:4px; font-size:12px; color:#94a3b8; margin-top:2px; }
.o-sub .material-symbols-outlined { font-size:14px; }
.o-status { display:flex; align-items:center; gap:5px; padding:5px 11px; border-radius:20px; font-size:11px; font-weight:700; background:#f0fdf4; color:#16a34a; }
.o-dot { width:7px; height:7px; border-radius:50%; background:#22c55e; box-shadow:0 0 0 3px rgba(34,197,94,.15); }
.o-asset { margin-top:16px; padding:14px 16px; border-radius:14px; background:linear-gradient(135deg,#f8fafc,#eef2ff); border:1px solid #eef0f4; }
.o-asset-lbl { display:block; font-size:11px; color:#64748b; font-weight:600; text-transform:uppercase; letter-spacing:.03em; }
.o-asset-val { display:block; font-size:1.35rem; font-weight:800; color:#0f172a; margin-top:2px; letter-spacing:-.02em; }
.o-metrics { display:grid; grid-template-columns:1fr 1fr; gap:10px; margin-top:12px; }
.o-metric { padding:10px 12px; border-radius:12px; background:#fafbfc; border:1px solid #f1f3f7; }
.o-metric-lbl { display:block; font-size:10px; color:#94a3b8; font-weight:600; text-transform:uppercase; letter-spacing:.03em; }
.o-metric-val { display:flex; align-items:center; gap:2px; font-size:13px; font-weight:700; margin-top:3px; }
.o-metric-val .material-symbols-outlined { font-size:15px; }
.o-footer { display:flex; flex-wrap:wrap; gap:6px; margin-top:14px; padding-top:14px; border-top:1px dashed #e5e7eb; }

.currency-badge { display:inline-flex; align-items:center; padding:4px 10px; background:#eef2ff; color:#4f46e5; border-radius:8px; font-size:11px; font-weight:700; }

.state-msg { display:flex; align-items:center; justify-content:center; gap:8px; padding:40px; color:#94a3b8; font-size:14px; }
.state-msg.error { color:#ef4444; }
.spin { animation:spin 1s linear infinite; }
.dimmed { color:#9ca3af; font-size:12px; }

/* ═══ QR Tab ═══ */
.qr-layout { display:grid; grid-template-columns:1fr 1fr; gap:24px; padding:20px; }
@media(max-width:768px) { .qr-layout { grid-template-columns:1fr; } }
.qr-title { display:flex; align-items:center; gap:8px; font-size:16px; font-weight:700; color:#1e293b; margin:0 0 20px; }
.qr-field { margin-bottom:16px; }
.qr-label { display:block; font-size:12px; font-weight:600; color:#475569; margin-bottom:6px; }
.qr-textarea, .qr-input {
  width:100%; padding:10px 12px; border:2px solid #e2e8f0;
  border-radius:8px; font-size:14px; color:#1e293b;
  background:#f8fafc; transition:border-color .2s; outline:none; resize:vertical;
  box-sizing:border-box;
}
.qr-textarea:focus, .qr-input:focus { border-color:#6366f1; background:white; }
.qr-range { width:100%; accent-color:#6366f1; }
.qr-btns { display:flex; gap:10px; }
.qr-btn {
  display:flex; align-items:center; gap:6px;
  padding:10px 18px; border:none; border-radius:8px;
  cursor:pointer; font-size:14px; font-weight:600; transition:.15s;
}
.qr-btn.primary { background:#6366f1; color:white; }
.qr-btn.primary:hover:not(:disabled) { background:#4f46e5; }
.qr-btn.secondary { background:#f1f5f9; color:#475569; border:1px solid #e2e8f0; }
.qr-btn.secondary:hover { background:#e2e8f0; }
.qr-btn:disabled { opacity:.5; cursor:not-allowed; }
.qr-btn.full-w { width:100%; justify-content:center; }

.qr-preview {
  display:flex; flex-direction:column; align-items:center; justify-content:center;
  background:#f8fafc; border:2px dashed #e2e8f0; border-radius:12px;
  padding:24px; min-height:280px;
}
.qr-empty { display:flex; flex-direction:column; align-items:center; gap:12px; color:#94a3b8; text-align:center; }
.qr-empty .material-symbols-outlined { font-size:48px; opacity:.3; }
.qr-result { display:flex; flex-direction:column; align-items:center; gap:12px; width:100%; }
.qr-img { border-radius:8px; box-shadow:0 4px 12px rgba(0,0,0,.12); }
.qr-caption { font-weight:600; color:#1e293b; }

/* ═══ Users Tab ═══ */
.um-error-bar { background:#fef2f2; color:#dc2626; padding:8px 16px; font-size:13px; font-weight:500; }

.um-toolbar { display:flex; align-items:center; gap:10px; padding:12px 18px; border-bottom:1px solid #f3f4f6; }
.um-search-wrap { position:relative; flex:1; max-width:320px; }
.um-search-icon { position:absolute; left:10px; top:50%; transform:translateY(-50%); font-size:18px; color:#9ca3af; }
.um-search { width:100%; padding:9px 10px 9px 36px; border:2px solid #e5e7eb; border-radius:10px; font-size:13px; outline:none; box-sizing:border-box; background:#f8fafc; transition:.2s; }
.um-search:focus { border-color:#6366f1; background:#fff; }

.um-layout { display:grid; grid-template-columns:340px 1fr; min-height:440px; }
@media(max-width:900px) { .um-layout { grid-template-columns:1fr; } }

.um-list { border-right:1px solid #f1f3f7; display:flex; flex-direction:column; gap:8px; overflow-y:auto; max-height:520px; padding:14px; background:#fafbfc; }
.um-card { display:flex; align-items:center; gap:12px; cursor:pointer; padding:12px; border-radius:14px; background:#fff; border:1px solid #eef0f4; transition:transform .2s cubic-bezier(.4,0,.2,1), box-shadow .2s, border-color .2s; }
.um-card:hover { transform:translateY(-2px); box-shadow:0 8px 18px -8px rgba(30,41,59,.15); border-color:#c7d2fe; }
.um-card.active { border-color:#6366f1; box-shadow:0 8px 18px -8px rgba(99,102,241,.35); background:linear-gradient(135deg,#fff,#f5f3ff); }
.um-avatar { width:42px; height:42px; border-radius:12px; display:flex; align-items:center; justify-content:center; color:white; font-weight:700; font-size:16px; flex-shrink:0; box-shadow:0 4px 10px -3px rgba(0,0,0,.25); }
.um-card-body { flex:1; min-width:0; }
.um-card-name { font-size:14px; font-weight:700; color:#0f172a; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
.um-card-mail { display:flex; align-items:center; gap:3px; font-size:12px; color:#94a3b8; margin-top:1px; }
.um-card-mail .material-symbols-outlined { font-size:14px; }

.um-detail { padding:22px; display:flex; flex-direction:column; gap:16px; }
.um-placeholder { display:flex; flex-direction:column; align-items:center; justify-content:center; gap:12px; height:100%; min-height:360px; color:#cbd5e1; }
.um-placeholder .material-symbols-outlined { font-size:64px; opacity:.4; }
.um-placeholder p { font-size:14px; color:#94a3b8; margin:0; }

.um-detail-header { position:relative; overflow:hidden; display:flex; align-items:center; gap:14px; padding:18px 18px; background:linear-gradient(135deg,#eef2ff,#faf5ff); border:1px solid #eef0f4; border-radius:16px; }
.um-detail-header::after { content:''; position:absolute; right:-30px; top:-30px; width:120px; height:120px; border-radius:50%; background:radial-gradient(circle,rgba(99,102,241,.12),transparent 70%); }
.um-detail-avatar { position:relative; z-index:1; width:52px; height:52px; border-radius:15px; display:flex; align-items:center; justify-content:center; color:white; font-weight:700; font-size:20px; flex-shrink:0; box-shadow:0 6px 16px -4px rgba(0,0,0,.3); }
.um-detail-identity { position:relative; z-index:1; flex:1; min-width:0; }
.um-detail-name { font-size:17px; font-weight:800; color:#0f172a; letter-spacing:-.01em; }
.um-detail-sub { font-size:12px; color:#7c85a3; margin-top:2px; }
.um-rank-badge { position:relative; z-index:1; padding:5px 13px; border-radius:20px; font-size:11px; font-weight:700; white-space:nowrap; }

.um-tabs { display:flex; gap:4px; padding:3px; background:#f1f5f9; border-radius:10px; }
.um-tab { display:flex; align-items:center; gap:5px; padding:7px 14px; background:none; border:none; border-radius:7px; cursor:pointer; font-size:12px; color:#64748b; font-weight:500; transition:.2s; }
.um-tab:hover { color:#4f46e5; background:rgba(99,102,241,.06); }
.um-tab.active { color:#4f46e5; background:#fff; font-weight:600; box-shadow:0 1px 2px rgba(0,0,0,.06); }
.um-tab .material-symbols-outlined { font-size:16px; }

.um-detail-body { display:flex; flex-direction:column; gap:12px; padding-top:4px; }
.um-field { display:flex; flex-direction:column; gap:4px; }
.um-field label { font-size:11px; font-weight:600; color:#64748b; text-transform:uppercase; }
.um-field-row { display:grid; grid-template-columns:1fr 1fr; gap:10px; }
.um-input { padding:10px 12px; border:2px solid #e5e7eb; border-radius:9px; font-size:13px; outline:none; box-sizing:border-box; background:#f8fafc; transition:.2s; }
.um-input:focus { border-color:#6366f1; background:#fff; }
.um-input:disabled { background:#f9fafb; color:#94a3b8; }

.um-rank-grid { display:flex; flex-wrap:wrap; gap:6px; }
.um-rank-chip { padding:4px 10px; border:1px solid #e5e7eb; border-radius:6px; font-size:11px; font-weight:600; cursor:pointer; background:#f9fafb; color:#64748b; transition:.15s; }
.um-rank-chip.selected { font-weight:700; }

.um-msg { padding:8px 12px; border-radius:6px; font-size:12px; font-weight:500; }
.um-msg.ok { background:#f0fdf4; color:#16a34a; }
.um-msg.err { background:#fef2f2; color:#dc2626; }

.um-btn { padding:10px 20px; border:none; border-radius:10px; cursor:pointer; font-size:13px; font-weight:600; transition:.2s; }
.um-btn.primary { background:linear-gradient(135deg,#6366f1,#818cf8); color:white; box-shadow:0 2px 8px rgba(99,102,241,.25); }
.um-btn.primary:hover:not(:disabled) { background:linear-gradient(135deg,#4f46e5,#6366f1); box-shadow:0 4px 12px rgba(99,102,241,.35); transform:translateY(-1px); }
.um-btn:disabled { opacity:.5; cursor:not-allowed; transform:none; box-shadow:none; }

.um-office-row { display:flex; align-items:center; gap:10px; padding:11px 12px; cursor:pointer; font-size:13px; font-weight:500; color:#374151; border-radius:8px; transition:.15s; }
.um-office-row:hover { background:#f8fafc; color:#6366f1; }
.um-office-row .material-symbols-outlined { font-size:22px; }

.panel-badge { font-size:11px; background:#ede9fe; color:#6366f1; padding:2px 8px; border-radius:12px; font-weight:600; }

@media(max-width:600px) {
  .db { padding:16px; gap:16px; }
  .kpi-grid { grid-template-columns:repeat(2,1fr); }
  .sec-stats { grid-template-columns:1fr 1fr; }
  .o-grid { grid-template-columns:1fr; padding:14px; }
  .um-layout { grid-template-columns:1fr; }
  .um-field-row { grid-template-columns:1fr; }
}
</style>
