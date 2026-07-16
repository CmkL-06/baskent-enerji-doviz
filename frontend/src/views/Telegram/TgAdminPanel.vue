<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import apiService from '@/services/apiservice'
import { formatAmount } from '@/utils/currency'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()

const loading = ref(true)
const activeTab = ref('dashboard')

const dashboard = ref<any>({
  total_tx: 0, total_tl: 0, total_usdt: 0, total_rub: 0,
  total_dealer_balance: 0, total_customers: 0, pending_tx: 0, completed_tx: 0,
  recent_transactions: []
})
const stats = ref<any>({
  today_count: 0, week_count: 0, month_count: 0, active_dealers: 0, recentLogins: []
})
const botStatus = ref<Record<string, any>>({})
const transactions = ref<any[]>([])
const dealers = ref<any[]>([])
const operators = ref<any[]>([])
const cryptoDeposits = ref<any[]>([])
const cryptoSummary = ref<any>({
  total_deposits: 0, pending_deposits: 0, confirmed_deposits: 0,
  pending_count: 0, confirmed_count: 0,
  verified_transactions: 0, unverified_transactions: 0, total_usdt_transactions: 0
})
const exchangeRates = ref<any[]>([])
const editingRate = ref<any>(null)
const newRate = ref({ currency: '', buyRate: 0, sellRate: 0 })
const showNewRateForm = ref(false)
const apiQueue = ref<any>({ items: [], summary: { pending: 0, failed: 0, success: 0 } })
const loginLogs = ref<any[]>([])

// Cari Hesap
const cariSummary = ref<any>({ dealers: [], totals: { totalPayable: 0, totalReceivable: 0, netPosition: 0 } })
const cariEntries = ref<any>({ dealerCode: '', dealerName: '', balance: 0, balanceType: 'settled', entries: [] })
const selectedCariDealer = ref<string | null>(null)
const showPaymentForm = ref(false)
const paymentForm = ref({ dealerCode: '', amount: 0, description: '', paymentReference: '' })
const paymentLoading = ref(false)

// Create forms
const showCreateDealer = ref(false)
const newDealer = ref({ username: '', name: '', dealerType: 'External', vaultId: '' })
const vaults = ref<any[]>([])
const createdDealerResult = ref<{ dealerCode: string; qrLink: string; qrImageUrl: string } | null>(null)
const BOT_USERNAME = 'MoneyExchangeTurkeyBot'

// Bayi Kur Yönetimi — settlement (alış/satış) kuru, para birimi bazında
const DEALER_RATE_CURRENCIES = ['USDT', 'KRUB']
const editingDealerRatesFor = ref<string | null>(null)
const dealerRateForm = ref<Record<string, { buyRate: number; sellRate: number }>>({})
const dealerRateLoading = ref(false)
const showCreateOperator = ref(false)
const newOperator = ref({ username: '', telegramId: '' })
const createLoading = ref(false)

// Transaction filter
const txFilter = ref('all')
const txSearch = ref('')
const filteredTransactions = computed(() => {
  let list = transactions.value
  if (txFilter.value !== 'all') list = list.filter(t => t.status === txFilter.value)
  if (txSearch.value.trim()) {
    const q = txSearch.value.toLowerCase()
    list = list.filter(t =>
      String(t.id).includes(q) ||
      t.customerName?.toLowerCase().includes(q) ||
      t.customerUsername?.toLowerCase().includes(q) ||
      t.referralCode?.toLowerCase().includes(q) ||
      t.currency?.toLowerCase().includes(q)
    )
  }
  return list
})

const botNames: Record<string, string> = {
  main_bot: 'Ana Bot',
  operator_bot: 'Operatör Bot',
  ruble_bot: 'Ruble Bot'
}

let refreshInterval: number | null = null

async function loadDashboard() {
  try {
    const [dashRes, statsRes, botRes] = await Promise.all([
      apiService.get('/tg/admin/dashboard'),
      apiService.get('/tg/admin/stats'),
      apiService.get('/tg/admin/bot-status')
    ])
    dashboard.value = dashRes
    stats.value = statsRes
    botStatus.value = botRes
  } catch (e) { console.error('Dashboard load error:', e) }
}

async function loadTransactions() {
  try {
    const res = await apiService.get('/tg/admin/transactions')
    transactions.value = res?.transactions ?? []
  } catch (e) { console.error(e) }
}

async function loadDealers() {
  try {
    const res = await apiService.get('/tg/admin/dealers')
    dealers.value = res?.dealers ?? []
  } catch (e) { console.error(e) }
}

async function loadOperators() {
  try {
    const res = await apiService.get('/tg/admin/operators')
    operators.value = res?.operators ?? []
  } catch (e) { console.error(e) }
}

async function loadCryptoDeposits() {
  try {
    const [depRes, sumRes, rateRes] = await Promise.all([
      apiService.get('/tg/admin/crypto-deposits'),
      apiService.get('/tg/admin/crypto-summary'),
      apiService.get('/tg/admin/exchange-rates')
    ])
    cryptoDeposits.value = depRes?.deposits ?? []
    cryptoSummary.value = sumRes ?? cryptoSummary.value
    exchangeRates.value = rateRes?.rates ?? []
  } catch (e) { console.error(e) }
}

async function saveRate(rate: any) {
  try {
    await apiService.put(`/tg/admin/exchange-rates/${rate.id}`, { buyRate: rate.buyRate, sellRate: rate.sellRate })
    editingRate.value = null
    await loadCryptoDeposits()
  } catch (e) { console.error('Rate update error:', e) }
}

async function createRate() {
  try {
    await apiService.post('/tg/admin/exchange-rates', {
      currency: newRate.value.currency, buyRate: newRate.value.buyRate, sellRate: newRate.value.sellRate
    })
    showNewRateForm.value = false
    newRate.value = { currency: '', buyRate: 0, sellRate: 0 }
    await loadCryptoDeposits()
  } catch (e) { console.error('Rate create error:', e) }
}

async function loadApiQueue() {
  try {
    const res = await apiService.get('/tg/admin/baskent-queue')
    apiQueue.value = res
  } catch (e) { console.error(e) }
}

async function loadLoginLogs() {
  try {
    const res = await apiService.get('/tg/admin/login-logs')
    loginLogs.value = res?.logs ?? []
  } catch (e) { console.error(e) }
}

async function loadCariSummary() {
  try {
    const res = await apiService.getTgCariSummary()
    cariSummary.value = res ?? cariSummary.value
  } catch (e) { console.error(e) }
}

async function loadCariEntries(code: string) {
  selectedCariDealer.value = code
  try {
    const res = await apiService.getTgCariEntries(code)
    cariEntries.value = res ?? cariEntries.value
  } catch (e) { console.error(e) }
}

function openPaymentForm(d: any) {
  paymentForm.value = { dealerCode: d.dealerCode, amount: Math.abs(d.balance), description: '', paymentReference: '' }
  showPaymentForm.value = true
}

async function submitPayment() {
  if (!paymentForm.value.dealerCode || paymentForm.value.amount <= 0) return
  paymentLoading.value = true
  try {
    await apiService.recordTgPayment(paymentForm.value)
    showPaymentForm.value = false
    await loadCariSummary()
    if (selectedCariDealer.value === paymentForm.value.dealerCode) {
      await loadCariEntries(paymentForm.value.dealerCode)
    }
  } catch (e: any) {
    notification.error(e?.response?.data?.message || e?.message || 'Ödeme kaydedilemedi')
  } finally { paymentLoading.value = false }
}

async function retryQueue(queueId: number) {
  try {
    await apiService.post(`/tg/admin/baskent-queue/${queueId}/retry`)
    await loadApiQueue()
  } catch (e) { console.error(e) }
}

async function loadVaults() {
  if (vaults.value.length) return
  try {
    const res = await apiService.getVaults()
    vaults.value = Array.isArray(res) ? res : (res?.data ?? [])
  } catch { vaults.value = [] }
}

async function createDealer() {
  if (!newDealer.value.username.trim() || !newDealer.value.name.trim()) return
  if (newDealer.value.dealerType === 'Branch' && !newDealer.value.vaultId) {
    notification.error('Şube tipi için bir Kasa seçmelisiniz.')
    return
  }
  createLoading.value = true
  try {
    const res = await apiService.post('/tg/admin/dealers', {
      username: newDealer.value.username,
      name: newDealer.value.name,
      dealerType: newDealer.value.dealerType,
      vaultId: newDealer.value.dealerType === 'Branch' ? newDealer.value.vaultId : null
    })
    const dealerCode = res?.dealer_code
    const qrLink = `https://t.me/${BOT_USERNAME}?start=${dealerCode}`
    createdDealerResult.value = {
      dealerCode,
      qrLink,
      qrImageUrl: `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(qrLink)}`
    }
    newDealer.value = { username: '', name: '', dealerType: 'External', vaultId: '' }
    await loadDealers()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || e?.message || 'Hata oluştu')
  } finally { createLoading.value = false }
}

function closeCreateDealer() {
  showCreateDealer.value = false
  createdDealerResult.value = null
}

// Şube (Branch) bayilerde gerçek ofis kuru (branch-rates), Harici bayilerde settlement
// kuru (rates) düzenlenir — endpoint tabanı buna göre seçilir.
function rateBasePath(code: string, isBranch: boolean) {
  return isBranch ? `/tg/admin/dealers/${code}/branch-rates` : `/tg/admin/dealers/${code}/rates`
}

const editingDealerIsBranch = ref(false)
const dealerRateHistory = ref<Record<string, any[]>>({})
const showHistoryFor = ref<string | null>(null)
const merkezRatesCache = ref<any[] | null>(null)
const bulkApplyLoading = ref<string | null>(null)

// --- Bayi/Şube bilgisi düzenleme (Owner Panel'deki ile aynı desen: sayfa içinde, ayrı yere gitmeden) ---
const DEALER_RANKS = [
  { value: 0, label: 'Yasaklı' },
  { value: 1, label: 'Kullanıcı' },
  { value: 50, label: 'Personel' },
  { value: 99, label: 'Admin' },
]
const editingDealerInfoFor = ref<string | null>(null)
const dealerInfoForm = ref({ dealerName: '', city: '', address: '', rank: 50 })
const dealerInfoSaving = ref(false)

function openDealerInfoEdit(d: any) {
  if (editingDealerInfoFor.value === d.dealer_code) { editingDealerInfoFor.value = null; return }
  editingDealerRatesFor.value = null
  editingDealerInfoFor.value = d.dealer_code
  dealerInfoForm.value = {
    dealerName: d.dealer_name || '',
    city: d.city || '',
    address: d.address || '',
    rank: d.rank ?? (d.is_active ? 50 : 0)
  }
}

async function saveDealerInfo(code: string) {
  dealerInfoSaving.value = true
  try {
    await apiService.put(`/tg/admin/dealers/${code}`, {
      dealerName: dealerInfoForm.value.dealerName,
      city: dealerInfoForm.value.city,
      address: dealerInfoForm.value.address,
      rank: dealerInfoForm.value.rank
    })
    notification.success('Bayi bilgileri güncellendi')
    editingDealerInfoFor.value = null
    await loadDealers()
  } catch (e: any) {
    notification.error(e.response?.data?.message || 'Güncelleme başarısız')
  } finally {
    dealerInfoSaving.value = false
  }
}

async function openDealerRates(code: string, isBranch: boolean) {
  if (editingDealerRatesFor.value === code) { editingDealerRatesFor.value = null; return }
  editingDealerInfoFor.value = null
  editingDealerRatesFor.value = code
  editingDealerIsBranch.value = isBranch
  showHistoryFor.value = null
  const form: Record<string, { buyRate: number; sellRate: number }> = {}
  for (const c of DEALER_RATE_CURRENCIES) form[c] = { buyRate: 0, sellRate: 0 }
  try {
    const res = await apiService.get(rateBasePath(code, isBranch))
    for (const r of (res?.rates ?? [])) {
      if (form[r.currency]) form[r.currency] = { buyRate: r.buyRate, sellRate: r.sellRate }
    }
  } catch (e) { console.error(e) }
  dealerRateForm.value = form
}

async function saveDealerRate(code: string, currency: string) {
  const rate = dealerRateForm.value[currency]
  if (!rate || rate.buyRate <= 0 || rate.sellRate <= 0) {
    notification.error('Alış ve satış kuru sıfırdan büyük olmalı.')
    return
  }
  dealerRateLoading.value = true
  try {
    await apiService.post(rateBasePath(code, editingDealerIsBranch.value), {
      currency, buyRate: rate.buyRate, sellRate: rate.sellRate
    })
    notification.success(`${currency} kuru güncellendi.`)
    await loadDealers()
    if (showHistoryFor.value === currency) await loadRateHistory(code, currency)
  } catch (e: any) {
    notification.error(e?.response?.data?.message || e?.message || 'Kur güncellenemedi')
  } finally { dealerRateLoading.value = false }
}

async function toggleRateHistory(code: string, currency: string) {
  if (showHistoryFor.value === currency) { showHistoryFor.value = null; return }
  showHistoryFor.value = currency
  await loadRateHistory(code, currency)
}

async function loadRateHistory(code: string, currency: string) {
  try {
    const res = await apiService.get(`${rateBasePath(code, editingDealerIsBranch.value)}/${currency}/history`)
    dealerRateHistory.value = { ...dealerRateHistory.value, [currency]: res?.history ?? [] }
  } catch (e) { console.error(e) }
}

async function copyFromMerkez(currency: string) {
  try {
    if (!merkezRatesCache.value) {
      const res = await apiService.get('/tg/admin/exchange-rates')
      merkezRatesCache.value = res?.rates ?? []
    }
    const match = merkezRatesCache.value?.find((r: any) => r.currency === currency)
    if (!match) {
      notification.error(`Merkez'de ${currency} için tanımlı bir kur bulunamadı.`)
      return
    }
    dealerRateForm.value[currency] = { buyRate: match.buyRate, sellRate: match.sellRate }
    notification.success(`Merkez kuru getirildi — kaydetmeden önce spread'i ayarlayabilirsiniz.`)
  } catch (e) { console.error(e) }
}

async function applyRateToAllExternal(code: string, currency: string) {
  const rate = dealerRateForm.value[currency]
  if (!rate || rate.buyRate <= 0 || rate.sellRate <= 0) {
    notification.error('Alış ve satış kuru sıfırdan büyük olmalı.')
    return
  }
  if (!confirm(`${currency} kuru (Alış ${rate.buyRate} / Satış ${rate.sellRate}) TÜM harici bayilere uygulansın mı?`)) return
  bulkApplyLoading.value = currency
  try {
    const res = await apiService.post('/tg/admin/dealers/rates/bulk', {
      currency, buyRate: rate.buyRate, sellRate: rate.sellRate
    })
    notification.success(`${res?.dealersUpdated ?? 0} harici bayiye ${currency} kuru uygulandı.`)
    await loadDealers()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || e?.message || 'Toplu güncelleme başarısız')
  } finally { bulkApplyLoading.value = null }
}

function marginPreview(currency: string) {
  const rate = dealerRateForm.value[currency]
  if (!rate || rate.buyRate <= 0 || rate.sellRate <= 0) return null
  return {
    buyTl: (100 * rate.buyRate).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
    sellTl: (100 * rate.sellRate).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  }
}

function rateStalenessDays(oldestRateUpdate: string | null): number | null {
  if (!oldestRateUpdate) return null
  const diffMs = Date.now() - new Date(oldestRateUpdate).getTime()
  return Math.floor(diffMs / (1000 * 60 * 60 * 24))
}

async function deleteDealer(userId: string) {
  if (!confirm('Bayi atamasını kaldırmak istediğinize emin misiniz?')) return
  try {
    await apiService.delete(`/tg/admin/dealers/${userId}`)
    await loadDealers()
  } catch (e) { console.error(e) }
}

async function createOperator() {
  if (!newOperator.value.username.trim()) return
  createLoading.value = true
  try {
    await apiService.post('/tg/admin/operators', {
      username: newOperator.value.username,
      telegramId: newOperator.value.telegramId ? Number(newOperator.value.telegramId) : null
    })
    showCreateOperator.value = false
    newOperator.value = { username: '', telegramId: '' }
    await loadOperators()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || e?.message || 'Hata oluştu')
  } finally { createLoading.value = false }
}

async function deleteOperator(userId: string) {
  if (!confirm('Operatör atamasını kaldırmak istediğinize emin misiniz?')) return
  try {
    await apiService.delete(`/tg/admin/operators/${userId}`)
    await loadOperators()
  } catch (e) { console.error(e) }
}

function switchTab(tab: string) {
  activeTab.value = tab
  if (tab === 'transactions') loadTransactions()
  else if (tab === 'dealers') loadDealers()
  else if (tab === 'operators') loadOperators()
  else if (tab === 'crypto') loadCryptoDeposits()
  else if (tab === 'cari') loadCariSummary()
  else if (tab === 'queue') loadApiQueue()
  else if (tab === 'logs') loadLoginLogs()
}

function formatDate(d: string | null) {
  if (!d) return '—'
  return new Date(d).toLocaleString('tr-TR')
}
function formatMoney(n: number | null) {
  return formatAmount(n ?? 0, 2)
}
function statusColor(status: string) {
  const map: Record<string, string> = {
    completed: '#10b981', approved: '#3b82f6', pending: '#f59e0b',
    processing: '#8b5cf6', cancelled: '#ef4444', rejected: '#ef4444',
    online: '#10b981', offline: '#ef4444', unknown: '#6b7280'
  }
  return map[status] || '#6b7280'
}
function statusLabel(s: string) {
  const map: Record<string, string> = {
    completed: 'Tamamlandı', approved: 'Onaylandı', pending: 'Bekliyor',
    processing: 'İşleniyor', cancelled: 'İptal', rejected: 'Reddedildi',
    online: 'Çevrimiçi', offline: 'Çevrimdışı', unknown: 'Bilinmiyor'
  }
  return map[s] || s
}

const handleVisibilityChange = () => {
  if (document.hidden) {
    if (refreshInterval) { clearInterval(refreshInterval); refreshInterval = null }
  } else {
    loadDashboard()
    refreshInterval = window.setInterval(loadDashboard, 30000)
  }
}

onMounted(async () => {
  await loadDashboard()
  loading.value = false
  refreshInterval = window.setInterval(loadDashboard, 30000)
  document.addEventListener('visibilitychange', handleVisibilityChange)
})

onUnmounted(() => {
  if (refreshInterval) clearInterval(refreshInterval)
  document.removeEventListener('visibilitychange', handleVisibilityChange)
})
</script>

<template>
  <div class="tg-admin">
    <!-- Tab Bar -->
    <div class="tg-tabs">
      <button v-for="tab in [
        { key: 'dashboard', icon: 'dashboard', label: 'Dashboard' },
        { key: 'transactions', icon: 'receipt_long', label: 'İşlemler' },
        { key: 'dealers', icon: 'storefront', label: 'Bayiler' },
        { key: 'operators', icon: 'support_agent', label: 'Operatörler' },
        { key: 'crypto', icon: 'currency_bitcoin', label: 'Kripto' },
        { key: 'cari', icon: 'account_balance_wallet', label: 'Cari Hesap' },
        { key: 'queue', icon: 'queue', label: 'API Kuyruk' },
        { key: 'logs', icon: 'history', label: 'Loglar' },
      ]" :key="tab.key"
        class="tg-tab" :class="{ active: activeTab === tab.key }"
        @click="switchTab(tab.key)">
        <span class="material-symbols-outlined" aria-hidden="true">{{ tab.icon }}</span>
        <span class="tab-label">{{ tab.label }}</span>
      </button>
    </div>

    <div v-if="loading" class="tg-loading">
      <span class="material-symbols-outlined spin">progress_activity</span>
      Yükleniyor...
    </div>

    <!-- ═══════ DASHBOARD ═══════ -->
    <div v-else-if="activeTab === 'dashboard'" class="tg-content">
      <!-- Primary Stats -->
      <div class="stat-grid four">
        <div class="stat-card accent-green">
          <div class="stat-icon-wrap green"><span class="material-symbols-outlined" aria-hidden="true">swap_horiz</span></div>
          <div class="stat-body">
            <div class="stat-value">{{ dashboard.total_tx }}</div>
            <div class="stat-label">Toplam İşlem</div>
          </div>
        </div>
        <div class="stat-card accent-blue">
          <div class="stat-icon-wrap blue"><span class="material-symbols-outlined" aria-hidden="true">payments</span></div>
          <div class="stat-body">
            <div class="stat-value">₺{{ formatMoney(dashboard.total_tl) }}</div>
            <div class="stat-label">Toplam TL Hacmi</div>
          </div>
        </div>
        <div class="stat-card accent-purple">
          <div class="stat-icon-wrap purple"><span class="material-symbols-outlined" aria-hidden="true">token</span></div>
          <div class="stat-body">
            <div class="stat-value">${{ formatMoney(dashboard.total_usdt) }}</div>
            <div class="stat-label">Toplam USDT</div>
          </div>
        </div>
        <div class="stat-card accent-orange">
          <div class="stat-icon-wrap orange"><span class="material-symbols-outlined" aria-hidden="true">currency_ruble</span></div>
          <div class="stat-body">
            <div class="stat-value">₽{{ formatMoney(dashboard.total_rub) }}</div>
            <div class="stat-label">Toplam RUB</div>
          </div>
        </div>
      </div>

      <!-- Secondary Stats Row -->
      <div class="stat-grid six">
        <div class="stat-card mini">
          <div class="mini-val">{{ stats.today_count }}</div>
          <div class="mini-label">Bugün</div>
        </div>
        <div class="stat-card mini">
          <div class="mini-val">{{ stats.week_count }}</div>
          <div class="mini-label">Bu Hafta</div>
        </div>
        <div class="stat-card mini">
          <div class="mini-val">{{ stats.month_count }}</div>
          <div class="mini-label">Bu Ay</div>
        </div>
        <div class="stat-card mini">
          <div class="mini-val warn">{{ dashboard.pending_tx }}</div>
          <div class="mini-label">Bekleyen</div>
        </div>
        <div class="stat-card mini">
          <div class="mini-val">{{ dashboard.total_customers }}</div>
          <div class="mini-label">Müşteri</div>
        </div>
        <div class="stat-card mini">
          <div class="mini-val accent">₺{{ formatMoney(dashboard.total_dealer_balance) }}</div>
          <div class="mini-label">Bayi Bakiyesi</div>
        </div>
      </div>

      <!-- Bot Status -->
      <div class="section-title">
        <span class="material-symbols-outlined" aria-hidden="true">smart_toy</span>
        Bot Durumu
      </div>
      <div class="bot-grid">
        <div v-for="(bot, name) in botStatus" :key="name" class="bot-card" :class="bot.status">
          <div class="bot-header">
            <div class="bot-indicator" :style="{ background: statusColor(bot.status) }"></div>
            <div class="bot-name">{{ botNames[name as string] || name }}</div>
            <span class="bot-status-badge" :style="{ background: statusColor(bot.status) }">{{ statusLabel(bot.status) }}</span>
          </div>
          <div class="bot-details">
            <div class="bot-stat"><span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">group</span> {{ bot.active_sessions }} oturum</div>
            <div class="bot-stat" v-if="bot.last_heartbeat"><span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">schedule</span> {{ formatDate(bot.last_heartbeat) }}</div>
            <div class="bot-stat" v-if="bot.age_seconds != null"><span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">timer</span> {{ bot.age_seconds < 60 ? `${bot.age_seconds}sn` : `${Math.floor(bot.age_seconds / 60)}dk` }} önce</div>
          </div>
        </div>
      </div>

      <!-- Recent Transactions -->
      <div class="section-title">
        <span class="material-symbols-outlined" aria-hidden="true">history</span>
        Son İşlemler
      </div>
      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead>
            <tr>
              <th>ID</th><th>Müşteri</th><th>Para</th><th>Tutar</th><th>TL</th><th>Kur</th><th>Tür</th><th>Durum</th><th>Bayi</th><th>Tarih</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="tx in dashboard.recent_transactions" :key="tx.transactionId">
              <td>#{{ tx.transactionId }}</td>
              <td>{{ tx.customerName || '—' }}</td>
              <td><span class="currency-chip">{{ tx.currency }}</span></td>
              <td>{{ formatMoney(tx.amount) }}</td>
              <td>₺{{ formatMoney(tx.tlAmount) }}</td>
              <td>{{ tx.exchangeRate }}</td>
              <td><span class="type-chip" :class="tx.isBuy ? 'buy' : 'sell'">{{ tx.isBuy ? 'Alım' : 'Satım' }}</span></td>
              <td><span class="status-badge" :style="{ background: statusColor(tx.status) }">{{ statusLabel(tx.status) }}</span></td>
              <td><code v-if="tx.referralCode">{{ tx.referralCode }}</code><span v-else>—</span></td>
              <td>{{ formatDate(tx.createdAt) }}</td>
            </tr>
            <tr v-if="!dashboard.recent_transactions?.length">
              <td colspan="10" class="empty-msg">Henüz işlem yok</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="refresh-indicator">
        <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">autorenew</span>
        30 saniyede bir otomatik güncellenir
      </div>
    </div>

    <!-- ═══════ TRANSACTIONS ═══════ -->
    <div v-else-if="activeTab === 'transactions'" class="tg-content">
      <div class="toolbar">
        <div class="filter-bar">
          <button v-for="f in [
            { key: 'all', label: 'Tümü' },
            { key: 'pending', label: 'Bekleyen' },
            { key: 'processing', label: 'İşleniyor' },
            { key: 'approved', label: 'Onaylı' },
            { key: 'completed', label: 'Tamamlanan' },
            { key: 'cancelled', label: 'İptal' },
          ]" :key="f.key"
            class="filter-btn" :class="{ active: txFilter === f.key }" @click="txFilter = f.key">
            {{ f.label }}
          </button>
        </div>
        <div class="search-box">
          <span class="material-symbols-outlined" aria-hidden="true" style="font-size:16px;color:var(--color-text-secondary,#9ca3af)">search</span>
          <input v-model="txSearch" placeholder="Ara (ID, müşteri, bayi, para)..." class="search-input" />
        </div>
      </div>
      <div class="result-count">{{ filteredTransactions.length }} işlem</div>
      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead>
            <tr>
              <th>ID</th><th>Müşteri</th><th>Para</th><th>Tutar</th><th>TL</th><th>Kur</th><th>Tür</th><th>Durum</th><th>Bayi</th><th>Tarih</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="tx in filteredTransactions" :key="tx.id">
              <td>#{{ tx.id }}</td>
              <td>{{ tx.customerName || tx.customerUsername || '—' }}</td>
              <td><span class="currency-chip">{{ tx.currency }}</span></td>
              <td>{{ formatMoney(tx.amount) }}</td>
              <td>₺{{ formatMoney(tx.tlAmount) }}</td>
              <td>{{ tx.exchangeRate }}</td>
              <td><span class="type-chip" :class="tx.isBuy ? 'buy' : 'sell'">{{ tx.isBuy ? 'Alım' : 'Satım' }}</span></td>
              <td><span class="status-badge" :style="{ background: statusColor(tx.status) }">{{ statusLabel(tx.status) }}</span></td>
              <td><code v-if="tx.referralCode">{{ tx.referralCode }}</code><span v-else>—</span></td>
              <td>{{ formatDate(tx.createdAt) }}</td>
            </tr>
            <tr v-if="!filteredTransactions.length">
              <td colspan="10" class="empty-msg">İşlem bulunamadı</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ═══════ DEALERS ═══════ -->
    <div v-else-if="activeTab === 'dealers'" class="tg-content">
      <div class="toolbar">
        <div class="section-title" style="margin:0"><span class="material-symbols-outlined" aria-hidden="true">storefront</span> Bayi Yönetimi</div>
        <button class="add-btn" @click="showCreateDealer ? closeCreateDealer() : (showCreateDealer = true, loadVaults())">
          <span class="material-symbols-outlined" aria-hidden="true" style="font-size:16px">{{ showCreateDealer ? 'close' : 'person_add' }}</span>
          {{ showCreateDealer ? 'Kapat' : 'Bayi Ata' }}
        </button>
      </div>

      <!-- Create Dealer Form -->
      <div v-if="showCreateDealer" class="create-form">
        <template v-if="!createdDealerResult">
          <div class="form-row">
            <div class="form-group">
              <label>Sistem Kullanıcı Adı</label>
              <input v-model="newDealer.username" placeholder="Mevcut kullanıcı adı..." class="form-input" />
            </div>
            <div class="form-group">
              <label>Bayi / Şube Adı</label>
              <input v-model="newDealer.name" placeholder="örn. Ankara" class="form-input" />
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Tip</label>
              <select v-model="newDealer.dealerType" class="form-input">
                <option value="External">Harici Bayi (sadece cari hesap)</option>
                <option value="Branch">Şube (Kasa'ya işler)</option>
              </select>
            </div>
            <div class="form-group" v-if="newDealer.dealerType === 'Branch'">
              <label>Kasa</label>
              <select v-model="newDealer.vaultId" class="form-input">
                <option value="">-- Kasa seçin --</option>
                <option v-for="v in vaults" :key="v.id || v.vaultId" :value="v.id || v.vaultId">
                  {{ v.name || v.vaultName }}{{ v.officeName ? ' — ' + v.officeName : '' }}
                </option>
              </select>
            </div>
          </div>
          <div class="form-row">
            <button class="action-sm save" :disabled="createLoading" @click="createDealer">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">check</span>
              {{ createLoading ? 'Kaydediliyor...' : 'Kaydet' }}
            </button>
          </div>
          <div class="form-hint">Mevcut bir sistem kullanıcısını bayi/şube olarak atar; cari hesap ve (Şube ise) Kasa bağlantısı otomatik kurulur. Harici bayiler için oluşturduktan sonra "Kur Ayarla"dan alış/satış kurunu tanımlamanız gerekir.</div>
        </template>

        <!-- Oluşturma sonucu: hazır QR + link -->
        <template v-else>
          <div class="dealer-result">
            <img :src="createdDealerResult.qrImageUrl" alt="QR" width="180" height="180" />
            <div class="dealer-result-info">
              <div><strong>Bayi Kodu:</strong> <code>{{ createdDealerResult.dealerCode }}</code></div>
              <div class="dealer-result-link">{{ createdDealerResult.qrLink }}</div>
              <button class="action-sm save" @click="closeCreateDealer">Tamam</button>
            </div>
          </div>
        </template>
      </div>

      <!-- Dealer Cards -->
      <div v-if="dealers.length" class="dealer-cards">
        <div v-for="d in dealers" :key="d.id" class="dealer-card">
          <div class="dealer-card-header">
            <div class="dealer-card-icon"><span class="material-symbols-outlined" aria-hidden="true">storefront</span></div>
            <div class="dealer-card-info">
              <div class="dealer-card-name">{{ d.dealer_name }}</div>
              <div class="dealer-card-code"><code>{{ d.dealer_code }}</code></div>
            </div>
            <div class="dealer-card-badges">
              <span class="status-badge sm" :style="{ background: d.dealer_type === 'Branch' ? '#f59e0b' : '#6366f1' }">{{ d.dealer_type === 'Branch' ? 'Şube' : 'Bayi' }}</span>
              <span class="status-badge sm" :style="{ background: d.is_active ? '#10b981' : '#ef4444' }">{{ d.is_active ? 'Aktif' : 'Pasif' }}</span>
              <span v-if="rateStalenessDays(d.oldest_rate_update) !== null && rateStalenessDays(d.oldest_rate_update)! >= 3"
                    class="status-badge sm rate-stale-badge" title="Kur bir süredir güncellenmedi">
                ⚠️ {{ rateStalenessDays(d.oldest_rate_update) }} gündür güncellenmedi
              </span>
            </div>
          </div>
          <div class="dealer-card-stats">
            <div class="dealer-stat">
              <div class="dealer-stat-val">₺{{ formatMoney(d.balance) }}</div>
              <div class="dealer-stat-label">Bakiye</div>
            </div>
            <div class="dealer-stat">
              <div class="dealer-stat-val">{{ d.total_tx }}</div>
              <div class="dealer-stat-label">Toplam İşlem</div>
            </div>
            <div class="dealer-stat">
              <div class="dealer-stat-val">{{ d.completed_tx }}</div>
              <div class="dealer-stat-label">Tamamlanan</div>
            </div>
          </div>
          <div class="dealer-card-footer">
            <span class="dealer-card-user"><span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">person</span> {{ d.username }} · {{ d.name }}</span>
            <div style="display:flex;gap:6px">
              <button class="action-sm edit" style="margin:0" @click="openDealerInfoEdit(d)">
                <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">edit</span> Düzenle
              </button>
              <button class="action-sm edit" style="margin:0" @click="openDealerRates(d.dealer_code, d.dealer_type === 'Branch')">
                <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">currency_exchange</span> Kur Ayarla
              </button>
              <button class="icon-btn danger" @click="deleteDealer(d.id)" title="Atamasını kaldır">
                <span class="material-symbols-outlined" aria-hidden="true">person_remove</span>
              </button>
            </div>
          </div>

          <!-- Bayi/Şube Bilgisi Düzenleme -->
          <div v-if="editingDealerInfoFor === d.dealer_code" class="dealer-rate-panel">
            <div class="dealer-info-field">
              <label>Görünen Ad</label>
              <input v-model="dealerInfoForm.dealerName" type="text" class="rate-input" style="width:100%" />
            </div>
            <div class="dealer-info-row">
              <div class="dealer-info-field">
                <label>Şehir</label>
                <input v-model="dealerInfoForm.city" type="text" class="rate-input" style="width:100%" />
              </div>
              <div class="dealer-info-field">
                <label>Adres</label>
                <input v-model="dealerInfoForm.address" type="text" class="rate-input" style="width:100%" />
              </div>
            </div>
            <div class="dealer-info-field">
              <label>Yetki Seviyesi</label>
              <div class="dealer-rank-chips">
                <button
                  v-for="r in DEALER_RANKS" :key="r.value"
                  type="button"
                  :class="['dealer-rank-chip', { active: dealerInfoForm.rank === r.value }]"
                  @click="dealerInfoForm.rank = r.value"
                >{{ r.label }}</button>
              </div>
            </div>
            <div class="dealer-info-actions">
              <button class="action-sm save" :disabled="dealerInfoSaving" @click="saveDealerInfo(d.dealer_code)">
                {{ dealerInfoSaving ? 'Kaydediliyor...' : 'Kaydet' }}
              </button>
            </div>
          </div>

          <!-- Bayi Kur Yönetimi -->
          <div v-if="editingDealerRatesFor === d.dealer_code" class="dealer-rate-panel">
            <div class="dealer-rate-hint">
              {{ d.dealer_type === 'Branch'
                ? 'Bu Şube\'nin gerçek Kasa/Ofis kuru — Manuel Kur Yönetimi\'ndeki ile aynı, buradan da düzenlenebilir.'
                : 'Owner\'ın bu bayiden alış/satış yaparken kullanacağı kur — müşteriye gösterilen kurdan bağımsız, kâr aradaki farktan doğar.' }}
            </div>
            <div v-for="cur in DEALER_RATE_CURRENCIES" :key="cur" class="dealer-rate-row-wrap">
              <div class="dealer-rate-row">
                <span class="dealer-rate-currency">{{ cur }}</span>
                <label>Alış</label>
                <input v-model.number="dealerRateForm[cur].buyRate" type="number" step="0.01" min="0" class="rate-input sm" />
                <label>Satış</label>
                <input v-model.number="dealerRateForm[cur].sellRate" type="number" step="0.01" min="0" class="rate-input sm" />
                <button class="action-sm save" :disabled="dealerRateLoading" @click="saveDealerRate(d.dealer_code, cur)">Kaydet</button>
              </div>
              <div class="dealer-rate-tools">
                <span v-if="marginPreview(cur)" class="dealer-rate-margin">
                  100 {{ cur }} → Alışta ₺{{ marginPreview(cur)!.buyTl }} · Satışta ₺{{ marginPreview(cur)!.sellTl }}
                </span>
                <button class="dealer-rate-link" @click="copyFromMerkez(cur)">Merkez kurunu getir</button>
                <button v-if="d.dealer_type !== 'Branch'" class="dealer-rate-link" :disabled="bulkApplyLoading === cur" @click="applyRateToAllExternal(d.dealer_code, cur)">
                  {{ bulkApplyLoading === cur ? 'Uygulanıyor...' : 'Tüm harici bayilere uygula' }}
                </button>
                <button class="dealer-rate-link" @click="toggleRateHistory(d.dealer_code, cur)">
                  {{ showHistoryFor === cur ? 'Geçmişi gizle' : 'Geçmiş' }}
                </button>
              </div>
              <div v-if="showHistoryFor === cur" class="dealer-rate-history">
                <div v-if="!dealerRateHistory[cur]?.length" class="dealer-rate-history-empty">Henüz kayıtlı değişiklik yok.</div>
                <div v-for="(h, hi) in dealerRateHistory[cur]" :key="hi" class="dealer-rate-history-row">
                  <span>{{ formatDate(h.changedAt) }}</span>
                  <span>Alış {{ h.oldBuyRate }} → {{ h.newBuyRate }}</span>
                  <span>Satış {{ h.oldSellRate }} → {{ h.newSellRate }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div v-else class="empty-state">
        <span class="material-symbols-outlined" aria-hidden="true" style="font-size:48px;color:var(--color-text-secondary,#d1d5db)">storefront</span>
        <div>Henüz bayi atanmamış</div>
      </div>
    </div>

    <!-- ═══════ OPERATORS ═══════ -->
    <div v-else-if="activeTab === 'operators'" class="tg-content">
      <div class="toolbar">
        <div class="section-title" style="margin:0"><span class="material-symbols-outlined" aria-hidden="true">support_agent</span> Operatör Yönetimi</div>
        <button class="add-btn" @click="showCreateOperator = !showCreateOperator">
          <span class="material-symbols-outlined" aria-hidden="true" style="font-size:16px">{{ showCreateOperator ? 'close' : 'person_add' }}</span>
          {{ showCreateOperator ? 'Kapat' : 'Operatör Ata' }}
        </button>
      </div>

      <!-- Create Operator Form -->
      <div v-if="showCreateOperator" class="create-form">
        <div class="form-row">
          <div class="form-group">
            <label>Sistem Kullanıcı Adı</label>
            <input v-model="newOperator.username" placeholder="Mevcut kullanıcı adı..." class="form-input" />
          </div>
          <div class="form-group">
            <label>Telegram ID (opsiyonel)</label>
            <input v-model="newOperator.telegramId" placeholder="Telegram Chat ID..." class="form-input" />
          </div>
          <button class="action-sm save" :disabled="createLoading" @click="createOperator">
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">check</span>
            {{ createLoading ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
        </div>
        <div class="form-hint">Mevcut bir sistem kullanıcısını Telegram operatörü olarak atayın.</div>
      </div>

      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead>
            <tr><th>Kullanıcı</th><th>Ad</th><th>Telegram ID</th><th>Durum</th><th>Kayıt Tarihi</th><th></th></tr>
          </thead>
          <tbody>
            <tr v-for="op in operators" :key="op.id">
              <td><strong>{{ op.username }}</strong></td>
              <td>{{ op.name }}</td>
              <td><code>{{ op.telegram_id }}</code></td>
              <td><span class="status-badge" :style="{ background: op.is_active ? '#10b981' : '#ef4444' }">{{ op.is_active ? 'Aktif' : 'Pasif' }}</span></td>
              <td>{{ formatDate(op.created_at) }}</td>
              <td>
                <button class="icon-btn danger" @click="deleteOperator(op.id)" title="Kaldır">
                  <span class="material-symbols-outlined" aria-hidden="true">person_remove</span>
                </button>
              </td>
            </tr>
            <tr v-if="!operators.length"><td colspan="6" class="empty-msg">Operatör bulunamadı</td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ═══════ CRYPTO ═══════ -->
    <div v-else-if="activeTab === 'crypto'" class="tg-content">
      <div class="stat-grid four" style="margin-bottom:16px">
        <div class="stat-card mini"><div class="mini-val" style="color:#3b82f6">{{ cryptoSummary.total_usdt_transactions }}</div><div class="mini-label">USDT İşlem</div></div>
        <div class="stat-card mini"><div class="mini-val" style="color:#10b981">{{ cryptoSummary.verified_transactions }}</div><div class="mini-label">Doğrulanmış</div></div>
        <div class="stat-card mini"><div class="mini-val warn">{{ cryptoSummary.unverified_transactions }}</div><div class="mini-label">Doğrulanmamış</div></div>
        <div class="stat-card mini"><div class="mini-val" style="color:#8b5cf6">{{ cryptoSummary.confirmed_count }}</div><div class="mini-label">Onaylı Deposit</div></div>
      </div>

      <!-- Exchange Rates -->
      <div class="crypto-section">
        <div class="crypto-section-header">
          <div class="crypto-section-title"><span class="material-symbols-outlined" aria-hidden="true" style="font-size:18px">currency_exchange</span> Döviz Kurları</div>
          <button class="add-btn" @click="showNewRateForm = !showNewRateForm">
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size:16px">{{ showNewRateForm ? 'close' : 'add' }}</span>
            {{ showNewRateForm ? 'Kapat' : 'Kur Ekle' }}
          </button>
        </div>
        <div v-if="showNewRateForm" class="rate-form">
          <input v-model="newRate.currency" placeholder="Para birimi (USDT, RUB...)" class="rate-input" />
          <input v-model.number="newRate.buyRate" type="number" step="0.01" placeholder="Alış" class="rate-input" />
          <input v-model.number="newRate.sellRate" type="number" step="0.01" placeholder="Satış" class="rate-input" />
          <button class="action-sm save" @click="createRate">Kaydet</button>
          <button class="action-sm cancel" @click="showNewRateForm = false">İptal</button>
        </div>
        <div class="rate-cards">
          <div v-for="rate in exchangeRates" :key="rate.id" class="rate-card">
            <div class="rate-currency">{{ rate.currency }}</div>
            <template v-if="editingRate?.id === rate.id">
              <div class="rate-edit-row"><label>Alış</label><input v-model.number="editingRate.buyRate" type="number" step="0.01" class="rate-input sm" /></div>
              <div class="rate-edit-row"><label>Satış</label><input v-model.number="editingRate.sellRate" type="number" step="0.01" class="rate-input sm" /></div>
              <div class="rate-actions">
                <button class="action-sm save" @click="saveRate(editingRate)">Kaydet</button>
                <button class="action-sm cancel" @click="editingRate = null">İptal</button>
              </div>
            </template>
            <template v-else>
              <div class="rate-values">
                <div class="rate-row"><span class="rate-label">Alış</span><span class="rate-val">{{ rate.buyRate }}</span></div>
                <div class="rate-row"><span class="rate-label">Satış</span><span class="rate-val">{{ rate.sellRate }}</span></div>
              </div>
              <div class="rate-updated">Son: {{ formatDate(rate.updatedAt) }}</div>
              <button class="action-sm edit" @click="editingRate = { ...rate }">
                <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">edit</span> Düzenle
              </button>
            </template>
          </div>
          <div v-if="!exchangeRates.length" class="empty-msg" style="padding:20px;text-align:center">Kur tanımlanmamış</div>
        </div>
      </div>

      <!-- Crypto Deposits -->
      <div class="crypto-section" style="margin-top:16px">
        <div class="crypto-section-title"><span class="material-symbols-outlined" aria-hidden="true" style="font-size:18px">account_balance</span> Kripto Deposit Geçmişi</div>
        <div class="tg-table-wrap" style="margin-top:10px">
          <table class="tg-table">
            <thead><tr><th>ID</th><th>İşlem</th><th>TXID</th><th>Tutar</th><th>Ağ</th><th>Onay</th><th>Durum</th><th>Tarih</th></tr></thead>
            <tbody>
              <tr v-for="dep in cryptoDeposits" :key="dep.id">
                <td>#{{ dep.id }}</td>
                <td>#{{ dep.transactionId }}</td>
                <td class="txid-cell" :title="dep.txid">{{ dep.txid ? (dep.txid.substring(0, 12) + '...') : '—' }}</td>
                <td>{{ formatMoney(dep.amount) }}</td>
                <td>{{ dep.network || '—' }}</td>
                <td>{{ dep.confirmations ?? '—' }}</td>
                <td><span class="status-badge" :style="{ background: statusColor(dep.status) }">{{ statusLabel(dep.status) }}</span></td>
                <td>{{ formatDate(dep.createdAt) }}</td>
              </tr>
              <tr v-if="!cryptoDeposits.length"><td colspan="8" class="empty-msg">Kripto deposit bulunamadı</td></tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ═══════ CARİ HESAP ═══════ -->
    <div v-else-if="activeTab === 'cari'" class="tg-content">
      <!-- Özet Kartları -->
      <div class="stat-grid three" style="margin-bottom:16px">
        <div class="stat-card accent-green">
          <div class="stat-icon-wrap green"><span class="material-symbols-outlined" aria-hidden="true">arrow_downward</span></div>
          <div class="stat-body">
            <div class="stat-value">₺{{ formatMoney(cariSummary.totals?.totalReceivable) }}</div>
            <div class="stat-label">Toplam Alacak</div>
          </div>
        </div>
        <div class="stat-card accent-orange">
          <div class="stat-icon-wrap orange"><span class="material-symbols-outlined" aria-hidden="true">arrow_upward</span></div>
          <div class="stat-body">
            <div class="stat-value">₺{{ formatMoney(cariSummary.totals?.totalPayable) }}</div>
            <div class="stat-label">Toplam Borç</div>
          </div>
        </div>
        <div class="stat-card" :class="{ 'accent-blue': (cariSummary.totals?.netPosition ?? 0) >= 0, 'accent-orange': (cariSummary.totals?.netPosition ?? 0) < 0 }">
          <div class="stat-icon-wrap" :class="(cariSummary.totals?.netPosition ?? 0) >= 0 ? 'blue' : 'orange'"><span class="material-symbols-outlined" aria-hidden="true">balance</span></div>
          <div class="stat-body">
            <div class="stat-value">₺{{ formatMoney(Math.abs(cariSummary.totals?.netPosition ?? 0)) }}</div>
            <div class="stat-label">Net Pozisyon ({{ (cariSummary.totals?.netPosition ?? 0) >= 0 ? 'Alacak' : 'Borç' }})</div>
          </div>
        </div>
      </div>

      <!-- Ödeme Formu -->
      <div v-if="showPaymentForm" class="create-form" style="margin-bottom:16px">
        <div class="section-title" style="margin:0 0 10px"><span class="material-symbols-outlined" aria-hidden="true">payments</span> Ödeme Kaydı — {{ paymentForm.dealerCode }}</div>
        <div class="form-row">
          <div class="form-group">
            <label>Tutar (TL)</label>
            <input v-model.number="paymentForm.amount" type="number" step="0.01" class="form-input" />
          </div>
          <div class="form-group">
            <label>Açıklama</label>
            <input v-model="paymentForm.description" placeholder="Ödeme notu..." class="form-input" />
          </div>
          <div class="form-group" style="flex:0.6">
            <label>Referans</label>
            <input v-model="paymentForm.paymentReference" placeholder="Makbuz no..." class="form-input" />
          </div>
          <button class="action-sm save" :disabled="paymentLoading" @click="submitPayment">
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">check</span>
            {{ paymentLoading ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
          <button class="action-sm cancel" @click="showPaymentForm = false">İptal</button>
        </div>
      </div>

      <!-- Bayi Cari Kartları -->
      <div v-if="cariSummary.dealers?.length" class="dealer-cards">
        <div v-for="d in cariSummary.dealers" :key="d.dealerCode" class="dealer-card cari-card">
          <div class="dealer-card-header">
            <div class="dealer-card-icon" :class="{ payable: d.balanceType === 'payable', receivable: d.balanceType === 'receivable' }">
              <span class="material-symbols-outlined" aria-hidden="true">{{ d.balanceType === 'payable' ? 'arrow_upward' : d.balanceType === 'receivable' ? 'arrow_downward' : 'check_circle' }}</span>
            </div>
            <div class="dealer-card-info">
              <div class="dealer-card-name">{{ d.dealerName }}</div>
              <div class="dealer-card-code"><code>{{ d.dealerCode }}</code></div>
            </div>
            <span class="cari-balance-badge" :class="d.balanceType">
              {{ d.balanceType === 'payable' ? 'Borçlu' : d.balanceType === 'receivable' ? 'Alacaklı' : 'Kapalı' }}
            </span>
          </div>
          <div class="dealer-card-stats">
            <div class="dealer-stat">
              <div class="dealer-stat-val" :class="{ 'cari-payable': d.balanceType === 'payable', 'cari-receivable': d.balanceType === 'receivable' }">
                ₺{{ d.displayBalance }}
              </div>
              <div class="dealer-stat-label">Bakiye</div>
            </div>
            <div class="dealer-stat">
              <div class="dealer-stat-val">₺{{ formatMoney(d.totalDebits) }}</div>
              <div class="dealer-stat-label">Toplam Borç</div>
            </div>
            <div class="dealer-stat">
              <div class="dealer-stat-val">₺{{ formatMoney(d.totalCredits) }}</div>
              <div class="dealer-stat-label">Toplam Alacak</div>
            </div>
            <div class="dealer-stat">
              <div class="dealer-stat-val">{{ d.transactionCount }}</div>
              <div class="dealer-stat-label">İşlem</div>
            </div>
          </div>
          <div class="dealer-card-footer">
            <span class="dealer-card-user">
              <template v-if="d.lastTransaction">Son işlem: {{ formatDate(d.lastTransaction) }}</template>
              <template v-else>Henüz işlem yok</template>
            </span>
            <div style="display:flex;gap:6px">
              <button class="action-sm edit" style="margin:0" @click="loadCariEntries(d.dealerCode)">
                <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">receipt_long</span> Ekstre
              </button>
              <button v-if="d.balanceType !== 'settled'" class="action-sm save" style="margin:0" @click="openPaymentForm(d)">
                <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">payments</span> Ödeme
              </button>
            </div>
          </div>
        </div>
      </div>
      <div v-else class="empty-state">
        <span class="material-symbols-outlined" aria-hidden="true" style="font-size:48px;color:var(--color-text-secondary,#d1d5db)">account_balance_wallet</span>
        <div>Cari hesap bağlantısı olan bayi yok</div>
      </div>

      <!-- Ekstre Detayı -->
      <div v-if="selectedCariDealer" class="cari-ekstre" style="margin-top:20px">
        <div class="toolbar">
          <div class="section-title" style="margin:0">
            <span class="material-symbols-outlined" aria-hidden="true">receipt_long</span>
            {{ cariEntries.dealerName }} — Ekstre
            <span class="cari-balance-badge sm" :class="cariEntries.balanceType" style="margin-left:8px">
              ₺{{ formatMoney(Math.abs(cariEntries.balance)) }}
              {{ cariEntries.balanceType === 'payable' ? '(Borç)' : cariEntries.balanceType === 'receivable' ? '(Alacak)' : '' }}
            </span>
          </div>
          <button class="action-sm cancel" @click="selectedCariDealer = null">
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">close</span> Kapat
          </button>
        </div>
        <div class="tg-table-wrap" style="margin-top:10px">
          <table class="tg-table">
            <thead>
              <tr><th>Tarih</th><th>Açıklama</th><th>Borç</th><th>Alacak</th><th>Bakiye</th><th>Durum</th><th>Referans</th></tr>
            </thead>
            <tbody>
              <tr v-for="e in cariEntries.entries" :key="e.id">
                <td>{{ formatDate(e.entryDate) }}</td>
                <td>{{ e.description || '—' }}</td>
                <td class="cari-debit">{{ e.debit != null ? '₺' + formatMoney(e.debit) : '' }}</td>
                <td class="cari-credit">{{ e.credit != null ? '₺' + formatMoney(e.credit) : '' }}</td>
                <td :class="{ 'cari-payable': e.runningBalance < 0, 'cari-receivable': e.runningBalance > 0 }">
                  ₺{{ formatMoney(Math.abs(e.runningBalance)) }}
                  <span style="font-size:10px;opacity:0.7">{{ e.runningBalance < 0 ? '(B)' : e.runningBalance > 0 ? '(A)' : '' }}</span>
                </td>
                <td><span class="status-badge sm" :style="{ background: e.paymentStatus === 'Paid' ? '#10b981' : e.paymentStatus === 'Pending' ? '#f59e0b' : '#6b7280' }">{{ e.paymentStatus === 'Paid' ? 'Ödendi' : e.paymentStatus === 'Pending' ? 'Bekliyor' : e.paymentStatus }}</span></td>
                <td><code v-if="e.referenceNumber">{{ e.referenceNumber }}</code><span v-else>—</span></td>
              </tr>
              <tr v-if="!cariEntries.entries?.length"><td colspan="7" class="empty-msg">Henüz kayıt yok</td></tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ═══════ API QUEUE ═══════ -->
    <div v-else-if="activeTab === 'queue'" class="tg-content">
      <div class="stat-grid three" style="margin-bottom:16px">
        <div class="stat-card mini"><div class="mini-val warn">{{ apiQueue.summary.pending }}</div><div class="mini-label">Bekleyen</div></div>
        <div class="stat-card mini"><div class="mini-val" style="color:#ef4444">{{ apiQueue.summary.failed }}</div><div class="mini-label">Başarısız</div></div>
        <div class="stat-card mini"><div class="mini-val" style="color:#10b981">{{ apiQueue.summary.success }}</div><div class="mini-label">Başarılı</div></div>
      </div>
      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead><tr><th>ID</th><th>İşlem</th><th>Durum</th><th>Deneme</th><th>Hata</th><th>Tarih</th><th></th></tr></thead>
          <tbody>
            <tr v-for="q in apiQueue.items" :key="q.queueId">
              <td>#{{ q.queueId }}</td>
              <td>#{{ q.transactionId }}</td>
              <td><span class="status-badge" :style="{ background: statusColor(q.status) }">{{ statusLabel(q.status) }}</span></td>
              <td>{{ q.attempts }}/{{ q.maxAttempts }}</td>
              <td class="error-cell" :title="q.lastError">{{ q.lastError || '—' }}</td>
              <td>{{ formatDate(q.createdAt) }}</td>
              <td>
                <button v-if="q.status === 'failed'" class="icon-btn" @click="retryQueue(q.queueId)" title="Tekrar Dene">
                  <span class="material-symbols-outlined" aria-hidden="true">refresh</span>
                </button>
              </td>
            </tr>
            <tr v-if="!apiQueue.items?.length"><td colspan="7" class="empty-msg">Kuyruk boş</td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ═══════ LOGS ═══════ -->
    <div v-else-if="activeTab === 'logs'" class="tg-content">
      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead><tr><th>Kullanıcı</th><th>IP</th><th>Panel</th><th>Durum</th><th>Tarih</th></tr></thead>
          <tbody>
            <tr v-for="log in loginLogs" :key="log.id">
              <td><strong>{{ log.username }}</strong></td>
              <td><code>{{ log.ipAddress }}</code></td>
              <td>{{ log.panelType }}</td>
              <td><span class="status-badge" :style="{ background: log.success ? '#10b981' : '#ef4444' }">{{ log.success ? 'Başarılı' : 'Başarısız' }}</span></td>
              <td>{{ formatDate(log.createdAt) }}</td>
            </tr>
            <tr v-if="!loginLogs.length"><td colspan="5" class="empty-msg">Log bulunamadı</td></tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tg-admin { padding: 0; }

/* Tabs */
.tg-tabs {
  display: flex; gap: 4px; padding: 12px 16px;
  border-bottom: 2px solid var(--border-strong, #cbd5e1);
  overflow-x: auto; background: var(--color-bg-card, #fff);
}
.tg-tab {
  display: flex; align-items: center; gap: 6px; padding: 8px 14px;
  border: none; background: transparent; color: var(--color-text-secondary, var(--color-text-secondary));
  border-radius: var(--radius-md); cursor: pointer; font-size: 13px; white-space: nowrap; transition: background-color 0.15s, color 0.15s;
}
.tg-tab .material-symbols-outlined { font-size: 18px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.tg-tab:hover { background: var(--color-hover, #f3f4f6); color: var(--color-text, #1f2937); }
.tg-tab.active { background: var(--color-primary, #2563eb); color: #fff; box-shadow: var(--shadow-glow-primary); border-bottom: 3px solid var(--color-primary, #2563eb); }

.tg-content { padding: 16px; }
.tg-loading { display: flex; align-items: center; justify-content: center; gap: 8px; padding: 60px; color: var(--color-text-secondary, #6b7280); }

/* Stat Grid */
.stat-grid { display: grid; gap: 12px; }
.stat-grid.four { grid-template-columns: repeat(4, 1fr); }
.stat-grid.six { grid-template-columns: repeat(6, 1fr); margin-top: 12px; }
.stat-grid.three { grid-template-columns: repeat(3, 1fr); }

.stat-card {
  padding: 16px; background: var(--color-bg-card, #fff);
  border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-md);
  box-shadow: var(--shadow-bold);
  display: flex; align-items: center; gap: 12px;
}
.stat-card.mini { flex-direction: column; text-align: center; padding: 12px; }
.stat-card.accent-green { border-left: 3px solid var(--color-success); }
.stat-card.accent-blue { border-left: 3px solid var(--color-secondary); }
.stat-card.accent-purple { border-left: 3px solid #8b5cf6; }
.stat-card.accent-orange { border-left: 3px solid var(--color-warning); }

.stat-icon-wrap {
  width: 42px; height: 42px; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center;
  box-shadow: var(--shadow-md);
}
.stat-icon-wrap .material-symbols-outlined { font-size: 24px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.stat-icon-wrap.green { background: var(--color-success); color: #fff; }
.stat-icon-wrap.blue { background: var(--color-secondary); color: #fff; }
.stat-icon-wrap.purple { background: #8b5cf6; color: #fff; }
.stat-icon-wrap.orange { background: var(--color-warning); color: #fff; }

.stat-body { flex: 1; }
.stat-value { font-size: 20px; font-weight: 700; color: var(--color-text, #1f2937); }
.stat-label { font-size: 12px; color: var(--color-text-secondary, #6b7280); margin-top: 2px; }

.mini-val { font-size: 22px; font-weight: 700; color: var(--color-text, #1f2937); }
.mini-val.warn { color: var(--color-warning); }
.mini-val.accent { color: var(--color-success); font-size: 16px; }
.mini-label { font-size: 11px; color: var(--color-text-secondary, #6b7280); margin-top: 2px; }

/* Section Title */
.section-title {
  display: flex; align-items: center; gap: 8px; margin: 20px 0 10px;
  padding: 8px 12px 8px 10px;
  font-size: 14px; font-weight: 600; color: var(--color-text, #1f2937);
  position: relative; border-bottom: 3px solid var(--color-border, #e5e7eb);
  background: linear-gradient(90deg, var(--color-hover, #f3f4f6), transparent);
  border-radius: var(--radius-sm);
}
.section-title::before {
  content: ''; position: absolute; left: 0; top: 6px; bottom: 6px; width: 5px;
  background: var(--color-primary, #2563eb); border-radius: var(--radius-sm);
}
.section-title .material-symbols-outlined { font-size: 20px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

/* Bot Grid */
.bot-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 10px; }
.bot-card {
  padding: 14px; background: var(--color-bg-card, #fff);
  border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-md);
  box-shadow: var(--shadow-md);
}
.bot-card.online { border-left: 3px solid var(--color-success); }
.bot-card.offline { border-left: 3px solid var(--color-danger); }
.bot-header { display: flex; align-items: center; gap: 8px; }
.bot-indicator { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
.bot-name { font-weight: 600; font-size: 14px; color: var(--color-text, #1f2937); flex: 1; }
.bot-status-badge { padding: 2px 8px; border-radius: var(--radius-md); font-size: 10px; font-weight: 600; color: #fff; }
.bot-details { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 10px; }
.bot-stat { display: flex; align-items: center; gap: 4px; font-size: 12px; color: var(--color-text-secondary, #6b7280); }
.bot-stat .material-symbols-outlined { font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

/* Tables */
.tg-table-wrap { overflow-x: auto; border-radius: var(--radius-md); border: 1px solid var(--color-border, var(--color-border)); box-shadow: var(--shadow-md); }
.tg-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.tg-table th { padding: 10px 12px; text-align: left; font-weight: 700; font-size: 11px; text-transform: uppercase; letter-spacing: 0.05em; color: var(--color-text-secondary, #6b7280); background: var(--color-hover, #f9fafb); border-bottom: 2px solid var(--border-strong, #cbd5e1); }
.tg-table td { padding: 10px 12px; border-bottom: 1px solid var(--color-border, #f3f4f6); color: var(--color-text, #1f2937); }
.tg-table tbody tr:hover { background: var(--color-hover, #f9fafb); }

.status-badge { padding: 3px 8px; border-radius: var(--radius-md); font-size: 11px; font-weight: 600; color: #fff; white-space: nowrap; }
.status-badge.sm { font-size: 10px; padding: 2px 6px; }
.empty-msg { text-align: center; color: var(--color-text-secondary, #6b7280); padding: 30px !important; }

.currency-chip { background: var(--color-hover, #f3f4f6); padding: 2px 8px; border-radius: var(--radius-sm); font-weight: 600; font-size: 12px; }
.type-chip { padding: 2px 8px; border-radius: var(--radius-sm); font-size: 11px; font-weight: 600; }
.type-chip.buy { background: #dbeafe; color: #1d4ed8; }
.type-chip.sell { background: #fce7f3; color: #be185d; }

/* Toolbar */
.toolbar { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-bottom: 12px; flex-wrap: wrap; }
.filter-bar { display: flex; gap: 6px; flex-wrap: wrap; }
.filter-btn { padding: 6px 12px; border: 1px solid var(--color-border, var(--color-border)); background: var(--color-bg-card, #fff); border-radius: var(--radius-sm); font-size: 12px; cursor: pointer; color: var(--color-text-secondary, var(--color-text-secondary)); transition: background-color 0.15s, color 0.15s, border-color 0.15s; }
.filter-btn:hover { border-color: var(--color-primary, #2563eb); }
.filter-btn.active { background: var(--color-primary, #2563eb); color: #fff; border-color: var(--color-primary, #2563eb); }

.search-box { display: flex; align-items: center; gap: 6px; padding: 6px 10px; border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-sm); background: var(--color-bg-card, #fff); }
.search-input { border: none; outline: none; font-size: 12px; background: transparent; color: var(--color-text, #1f2937); width: 200px; }
.result-count { font-size: 11px; color: var(--color-text-secondary, #9ca3af); margin-bottom: 8px; }

/* Add Button */
.add-btn { display: flex; align-items: center; gap: 4px; padding: 6px 14px; border: 1px solid var(--color-primary, var(--color-secondary-hover)); background: transparent; color: var(--color-primary, var(--color-secondary-hover)); border-radius: var(--radius-sm); font-size: 12px; font-weight: 600; cursor: pointer; transition: background-color 0.15s, color 0.15s, border-color 0.15s; }
.add-btn:hover { background: var(--color-primary, #2563eb); color: #fff; }
.add-btn .material-symbols-outlined { font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

/* Create Forms */
.create-form { padding: 14px; background: var(--color-hover, #f9fafb); border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-md); box-shadow: var(--shadow-md); margin-bottom: 16px; }
.form-row { display: flex; gap: 10px; align-items: flex-end; flex-wrap: wrap; }
.form-group { flex: 1; min-width: 160px; }
.form-group label { display: block; font-size: 11px; font-weight: 600; color: var(--color-text-secondary, #6b7280); margin-bottom: 4px; text-transform: uppercase; letter-spacing: 0.05em; }
.form-input { width: 100%; padding: 7px 10px; border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-sm); font-size: 13px; background: var(--color-bg-card, #fff); color: var(--color-text, var(--color-text)); }
.form-input:focus { outline: none; border-color: var(--color-primary, #2563eb); }
.form-hint { font-size: 11px; color: var(--color-text-secondary, #9ca3af); margin-top: 8px; }

/* Dealer Onboarding Result */
.dealer-result { display: flex; gap: 16px; align-items: center; }
.dealer-result img { border-radius: var(--radius-sm); border: 1px solid var(--color-border, var(--color-border)); background: #fff; }
.dealer-result-info { display: flex; flex-direction: column; gap: 8px; font-size: 13px; }
.dealer-result-link { font-family: var(--font-mono, monospace); font-size: 12px; color: var(--color-text-secondary, #6b7280); word-break: break-all; max-width: 320px; }

/* Dealer Cards */
.dealer-cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 12px; }
.dealer-card { background: var(--color-bg-card, #fff); border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-md); padding: 16px; box-shadow: var(--shadow-bold); }
.dealer-card-header { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.dealer-card-badges { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.dealer-card-icon { width: 38px; height: 38px; border-radius: var(--radius-md); background: var(--color-secondary); color: #fff; box-shadow: var(--shadow-md); display: flex; align-items: center; justify-content: center; }
.dealer-card-icon .material-symbols-outlined { font-size: 20px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.dealer-card-info { flex: 1 1 120px; min-width: 120px; }
.dealer-card-name { font-weight: 700; font-size: 14px; color: var(--color-text, #1f2937); }
.dealer-card-code code { background: var(--color-hover, #f3f4f6); padding: 1px 6px; border-radius: var(--radius-sm); font-size: 12px; font-weight: 600; }
.dealer-card-stats { display: flex; gap: 1px; margin: 12px -16px; background: var(--color-border, var(--color-border)); }
.dealer-stat { flex: 1; text-align: center; padding: 10px; background: var(--color-bg-card, #fff); }
.dealer-stat:first-child { background: var(--color-hover, #f9fafb); }
.dealer-stat-val { font-size: 16px; font-weight: 700; color: var(--color-text, #1f2937); }
.dealer-stat-label { font-size: 10px; color: var(--color-text-secondary, #6b7280); margin-top: 2px; text-transform: uppercase; letter-spacing: 0.05em; }
.dealer-card-footer { display: flex; justify-content: space-between; align-items: center; margin-top: 12px; }
.dealer-card-user { display: flex; align-items: center; gap: 4px; font-size: 12px; color: var(--color-text-secondary, #6b7280); }
.dealer-card-user .material-symbols-outlined { font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

/* Bayi Kur Yönetimi */
.dealer-rate-panel { margin: 12px -16px -16px; padding: 12px 16px; background: var(--color-hover, #f9fafb); border-top: 1px solid var(--color-border, var(--color-border)); border-radius: 0 0 var(--radius-md) var(--radius-md); display: flex; flex-direction: column; gap: 10px; }
.dealer-rate-hint { font-size: 11px; color: var(--color-text-secondary, #6b7280); }
.dealer-rate-row-wrap { display: flex; flex-direction: column; gap: 4px; padding-bottom: 8px; border-bottom: 1px dashed var(--color-border, #e5e7eb); }
.dealer-rate-row-wrap:last-child { border-bottom: none; padding-bottom: 0; }
.dealer-rate-row { display: flex; align-items: center; gap: 6px; font-size: 12px; flex-wrap: wrap; }
.dealer-rate-currency { font-weight: 700; width: 36px; flex-shrink: 0; }
.dealer-rate-row label { color: var(--color-text-secondary, #6b7280); }
.dealer-rate-row .rate-input.sm { width: 70px; min-width: 0; flex: 1 1 60px; }
.dealer-rate-row .action-sm.save { flex-shrink: 0; }
.dealer-rate-tools { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; padding-left: 50px; }
.dealer-rate-margin { font-size: 11px; color: var(--color-text-secondary, #6b7280); }
.dealer-rate-link { background: none; border: none; padding: 0; font-size: 11px; color: var(--color-primary, #2563eb); cursor: pointer; text-decoration: underline; }
.dealer-rate-link:disabled { opacity: 0.5; cursor: not-allowed; }
.dealer-rate-history { padding-left: 50px; display: flex; flex-direction: column; gap: 3px; }
.dealer-rate-history-empty { font-size: 11px; color: var(--color-text-secondary, #9ca3af); font-style: italic; }
.dealer-rate-history-row { display: flex; gap: 12px; font-size: 11px; color: var(--color-text-secondary, #6b7280); }
.rate-stale-badge { background: var(--color-warning, #f59e0b) !important; }

/* Icon & Action Buttons */
.icon-btn { border: none; background: transparent; cursor: pointer; padding: 4px; border-radius: var(--radius-sm); color: var(--color-text-secondary, var(--color-text-secondary)); }
.icon-btn:hover { background: var(--color-hover, #f3f4f6); }
.icon-btn.danger:hover { color: var(--color-danger); }
.icon-btn .material-symbols-outlined { font-size: 18px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

.action-sm { padding: 7px 14px; border-radius: var(--radius-sm); font-size: 12px; font-weight: 600; cursor: pointer; border: none; display: flex; align-items: center; gap: 4px; transition: opacity 0.15s; white-space: nowrap; }
.action-sm:hover { opacity: 0.85; }
.action-sm:disabled { opacity: 0.5; cursor: not-allowed; }
.action-sm.save { background: var(--color-success); color: #fff; }
.action-sm.cancel { background: var(--color-border, var(--color-border)); color: var(--color-text, var(--color-text)); }
.action-sm.edit { background: transparent; border: 1px solid var(--color-border, var(--color-border)); color: var(--color-text-secondary, var(--color-text-secondary)); margin-top: 8px; }

/* Empty State */
.empty-state { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 48px; color: var(--color-text-secondary, #9ca3af); font-size: 13px; }

/* Crypto */
.crypto-section { background: var(--color-bg-card, #fff); border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-md); padding: 14px; box-shadow: var(--shadow-bold); }
.crypto-section-header { display: flex; justify-content: space-between; align-items: center; }
.crypto-section-title { display: flex; align-items: center; gap: 6px; font-size: 14px; font-weight: 600; color: var(--color-text, #1f2937); }
.crypto-section-title .material-symbols-outlined { font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

.dealer-info-field { display: flex; flex-direction: column; gap: 4px; flex: 1; }
.dealer-info-field label { font-size: 11px; font-weight: 600; color: var(--color-text-secondary, #6b7280); }
.dealer-info-row { display: flex; gap: 10px; }
.dealer-rank-chips { display: flex; flex-wrap: wrap; gap: 6px; }
.dealer-rank-chip {
  padding: 4px 10px; border-radius: 999px; font-size: 11px; font-weight: 600;
  border: 1.5px solid var(--color-border, #e5e7eb); background: var(--color-bg-card, #fff); color: var(--color-text-secondary, #6b7280);
  cursor: pointer; transition: all 0.15s;
}
.dealer-rank-chip:hover { border-color: var(--color-primary, #2563eb); }
.dealer-rank-chip.active { background: var(--color-primary, #2563eb); border-color: var(--color-primary, #2563eb); color: #fff; }
.dealer-info-actions { display: flex; justify-content: flex-end; }

.rate-form { display: flex; gap: 8px; align-items: center; margin-top: 12px; flex-wrap: wrap; }
.rate-input { padding: 6px 10px; border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-sm); font-size: 13px; background: var(--color-bg-card, #fff); color: var(--color-text, var(--color-text)); width: 140px; }
.rate-input.sm { width: 100px; }
.rate-input:focus { outline: none; border-color: var(--color-primary, #2563eb); }

.rate-cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 10px; margin-top: 12px; }
.rate-card { padding: 14px; background: var(--color-hover, #f9fafb); border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-md); box-shadow: var(--shadow-md); }
.rate-currency { font-size: 16px; font-weight: 700; color: var(--color-text, #1f2937); margin-bottom: 8px; }
.rate-values { display: flex; flex-direction: column; gap: 4px; }
.rate-row { display: flex; justify-content: space-between; align-items: center; }
.rate-label { font-size: 12px; color: var(--color-text-secondary, #6b7280); }
.rate-val { font-size: 15px; font-weight: 600; color: var(--color-text, #1f2937); }
.rate-updated { font-size: 10px; color: var(--color-text-secondary, #9ca3af); margin-top: 6px; }
.rate-edit-row { display: flex; align-items: center; gap: 8px; margin-bottom: 6px; }
.rate-edit-row label { font-size: 12px; color: var(--color-text-secondary, #6b7280); width: 40px; }
.rate-actions { display: flex; gap: 6px; margin-top: 6px; }

.txid-cell { max-width: 150px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-family: monospace; font-size: 12px; }
.error-cell { max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: 11px; color: var(--color-danger); }

code { background: var(--color-hover, #f3f4f6); padding: 2px 6px; border-radius: var(--radius-sm); font-size: 12px; }

.refresh-indicator { display: flex; align-items: center; gap: 4px; justify-content: center; margin-top: 16px; font-size: 11px; color: var(--color-text-secondary, #9ca3af); }
.refresh-indicator .material-symbols-outlined { font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

/* Cari Hesap */
.cari-card .dealer-card-stats { display: grid; grid-template-columns: repeat(4, 1fr); }
.cari-card .dealer-card-icon.payable { background: var(--color-warning); color: #fff; box-shadow: 0 3px 8px -2px rgba(0,0,0,.28); }
.cari-card .dealer-card-icon.receivable { background: var(--color-success); color: #fff; box-shadow: 0 3px 8px -2px rgba(0,0,0,.28); }
.cari-balance-badge { padding: 3px 8px; border-radius: var(--radius-md); font-size: 11px; font-weight: 600; color: #fff; }
.cari-balance-badge.sm { font-size: 10px; padding: 2px 6px; }
.cari-balance-badge.payable { background: var(--color-warning); }
.cari-balance-badge.receivable { background: var(--color-success); }
.cari-balance-badge.settled { background: #6b7280; }
.cari-payable { color: var(--color-warning); font-weight: 600; }
.cari-receivable { color: var(--color-success); font-weight: 600; }
.cari-debit { color: var(--color-danger); font-weight: 500; }
.cari-credit { color: var(--color-success); font-weight: 500; }
.cari-ekstre { background: var(--color-bg-card, #fff); border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-md); padding: 14px; box-shadow: var(--shadow-bold); }

.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

@media (max-width: 900px) {
  .stat-grid.four { grid-template-columns: repeat(2, 1fr); }
  .stat-grid.six { grid-template-columns: repeat(3, 1fr); }
  .dealer-cards { grid-template-columns: 1fr; }
}
@media (max-width: 600px) {
  .stat-grid.four, .stat-grid.six, .stat-grid.three { grid-template-columns: repeat(2, 1fr); }
  .tg-tabs { gap: 2px; padding: 8px; }
  .tg-tab { padding: 6px 10px; font-size: 12px; }
  .tab-label { display: none; }
}
</style>
