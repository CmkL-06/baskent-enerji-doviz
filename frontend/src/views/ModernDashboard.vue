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
const dateStr = ref('')

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

// ── Owner data
const offices = ref<any[]>([])
const dealerSummary = ref<any>(null)
const pendingTransfers = ref<any[]>([])
const ownerAlerts = ref<any[]>([])
const liveRates = ref<any[]>([])
const recentTx = ref<any[]>([])
const recentTxLoading = ref(false)
const zHistory = ref<any[]>([])

const MAIN_CURRENCIES = ['USD', 'EUR', 'GBP', 'CHF', 'RUB', 'KRUB', 'USDT']

// ── Staff data
const myOfficeIds = ref<Set<string>>(new Set())
const zReport = ref<any>(null)
const zReportLoading = ref(false)
const partyTotals = ref<any>(null)
const expenseTotals = ref<{ total: number; count: number }>({ total: 0, count: 0 })

// ── Helper functions
function txTypeLabel(t: any) {
  const type = (t?.transactionType ?? t?.type ?? '').toString().toLowerCase()
  if (type.includes('buy') || type === '0') return { label: 'Alış', cls: 'tx-buy' }
  if (type.includes('sell') || type === '1') return { label: 'Satış', cls: 'tx-sell' }
  return { label: type || '—', cls: '' }
}

function txTime(t: any) {
  const d = t?.createdAt ?? t?.transactionDate
  if (!d) return '—'
  const dt = new Date(d)
  return dt.toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
}

function txUser(t: any) {
  return t?.userName ?? t?.userFullName ?? t?.createdBy ?? ''
}

// ── Loaders
async function loadOffices() {
  try { offices.value = await apiService.getOfficeSummaries() ?? [] } catch { offices.value = [] }
}

async function loadDealerSummary() {
  try { dealerSummary.value = await apiService.getTgCariSummary() } catch { dealerSummary.value = null }
}

async function loadLiveRates() {
  try {
    const officeId = myOfficeIds.value.size ? [...myOfficeIds.value][0] : undefined
    const data = await apiService.getExchangeRates(officeId)
    liveRates.value = Array.isArray(data) ? data : []
  } catch { liveRates.value = [] }
}

async function loadRecentTx() {
  recentTxLoading.value = true
  try {
    const res = await apiService.getTransactionHistory({ pageSize: 15, page: 1 })
    recentTx.value = (res?.items ?? res?.data ?? res ?? []).slice(0, 15)
  } catch { recentTx.value = [] }
  finally { recentTxLoading.value = false }
}

async function loadPendingTransfers() {
  try { pendingTransfers.value = await apiService.getPendingTransfers() ?? [] } catch { pendingTransfers.value = [] }
}

async function loadAlerts() {
  try { ownerAlerts.value = await apiService.getUnreadAlerts() ?? [] } catch { ownerAlerts.value = [] }
}

async function loadZHistory() {
  try {
    const data = await apiService.getZReportHistory({ count: 7 })
    zHistory.value = Array.isArray(data) ? data : []
  } catch { zHistory.value = [] }
}

async function loadMyAccess() {
  if (authStore.isAdmin) return
  try {
    const res = await apiService.getMyOfficeAccess()
    const ids = (res ?? []).map((r: any) => r.officeId ?? r.office?.id).filter(Boolean)
    myOfficeIds.value = new Set(ids)
  } catch {}
}

// Staff-specific loaders
async function loadStaffZReport() {
  zReportLoading.value = true
  try {
    const officeId = myOfficeIds.value.size ? [...myOfficeIds.value][0] : undefined
    const today = new Date().toISOString().slice(0, 10)
    zReport.value = await apiService.getZReportDaily(officeId, today)
  } catch { zReport.value = null }
  finally { zReportLoading.value = false }
}

async function loadStaffParties() {
  try {
    const officeId = myOfficeIds.value.size ? [...myOfficeIds.value][0] : undefined
    if (!officeId) return
    const data = await apiService.getParties(officeId)
    const list = Array.isArray(data) ? data : (data?.items ?? [])
    const recv = list.reduce((a: number, p: any) => a + (p.totalReceivables ?? 0), 0)
    const payb = list.reduce((a: number, p: any) => a + (p.totalPayables ?? 0), 0)
    partyTotals.value = { count: list.length, receivables: recv, payables: payb, net: recv - payb }
  } catch { partyTotals.value = null }
}

async function loadStaffExpenses() {
  try {
    const officeId = myOfficeIds.value.size ? [...myOfficeIds.value][0] : undefined
    if (!officeId) return
    const data = await apiService.getExpensePayments({ officeId })
    const list = Array.isArray(data) ? data : (data?.items ?? [])
    const total = list.reduce((a: number, p: any) => a + (p.amount ?? 0), 0)
    expenseTotals.value = { total, count: list.length }
  } catch { expenseTotals.value = { total: 0, count: 0 } }
}

// ── Computed: Owner
const sum = computed(() => dashboard.value?.summary ?? {})

const mainRates = computed(() =>
  liveRates.value
    .filter(r => r.targetCurrencyCode === 'TRY' && MAIN_CURRENCIES.includes(r.sourceCurrencyCode))
    .sort((a: any, b: any) => MAIN_CURRENCIES.indexOf(a.sourceCurrencyCode) - MAIN_CURRENCIES.indexOf(b.sourceCurrencyCode))
)

const ownerKpi = computed(() => {
  const s = sum.value
  const totalAssets = Number(g(s, 'totalAssets') ?? 0)
  const tp = Number(g(s, 'todayProfit') ?? 0)
  const mp = Number(g(s, 'monthlyProfit') ?? 0)
  const txCount = Number(g(s, 'todayTransactionCount') ?? 0)
  const fxValue = Number(g(s, 'totalForeignCurrencyValue') ?? 0)
  const cariNet = Number(g(s, 'netPartyBalance') ?? 0)
  const ch = kpiChanges.value
  return [
    { icon: 'account_balance', label: 'Toplam Varlık', value: fmtNum(totalAssets), unit: '₺', color: '#7c3aed', bg: '#f3f0ff', spark: null, change: null },
    { icon: 'trending_up', label: 'Günlük K/Z', value: plSign(tp) + fmtNum(tp), unit: '₺', color: plColor(tp), bg: tp >= 0 ? '#f0fdf4' : '#fef2f2', spark: profitSpark.value, change: ch.profit },
    { icon: 'calendar_month', label: 'Aylık K/Z', value: plSign(mp) + fmtNum(mp), unit: '₺', color: plColor(mp), bg: mp >= 0 ? '#f0fdf4' : '#fef2f2', spark: null, change: null },
    { icon: 'swap_horiz', label: 'İşlem Sayısı', value: String(txCount), unit: 'adet', color: 'var(--color-primary)', bg: 'var(--color-primary-light)', spark: txSpark.value, change: ch.txCount },
    { icon: 'currency_exchange', label: 'Döviz Varlık', value: fmtNum(fxValue), unit: '₺', color: 'var(--color-secondary-hover)', bg: 'var(--color-secondary-light)', spark: null, change: null },
    { icon: 'people', label: 'Cari Net', value: fmtNum(cariNet), unit: '₺', color: 'var(--color-warning)', bg: '#fffbeb', spark: null, change: null },
  ]
})

const topCurrencies = computed(() =>
  (dashboard.value?.currencyDistribution?.currencies ?? [])
    .filter((c: any) => c.totalAmount > 0)
    .sort((a: any, b: any) => b.totalValueInBaseCurrency - a.totalValueInBaseCurrency)
)

const totalFxValue = computed(() => {
  const v = topCurrencies.value.reduce((a: number, c: any) => a + (c.totalValueInBaseCurrency ?? 0), 0)
  return v > 0 ? v : 1
})

const riskLevel = computed(() => {
  if (!topCurrencies.value.length) return { level: 'low', label: 'Düşük', color: '#22c55e' }
  const top = topCurrencies.value[0]
  const pct = (top.totalValueInBaseCurrency / totalFxValue.value) * 100
  if (pct > 70) return { level: 'high', label: 'Yüksek', color: '#ef4444' }
  if (pct > 50) return { level: 'medium', label: 'Orta', color: '#f59e0b' }
  return { level: 'low', label: 'Düşük', color: '#22c55e' }
})

const isMerkez = (o: any) => /merkez/i.test(o?.officeName ?? '')
const sortedOffices = computed(() =>
  [...offices.value].sort((a, b) => (isMerkez(b) ? 1 : 0) - (isMerkez(a) ? 1 : 0))
)

// ── Merkez / Şubeler / Bayiler kartları
const merkezOffice = computed(() => offices.value.find(o => o.officeType === 1))
const subeOfficeList = computed(() => offices.value.filter(o => o.officeType === 2))
const subeAggregate = computed(() => {
  const list = subeOfficeList.value
  return {
    count: list.length,
    vaultCount: list.reduce((s, o) => s + (o.vaultCount ?? 0), 0),
    userCount: list.reduce((s, o) => s + (o.userCount ?? 0), 0),
    dailyProfitLoss: list.reduce((s, o) => s + (o.dailyProfitLoss ?? 0), 0),
    monthlyProfitLoss: list.reduce((s, o) => s + (o.monthlyProfitLoss ?? 0), 0),
    totalValueInBaseCurrency: list.reduce((s, o) => s + (o.totalValueInBaseCurrency ?? 0), 0),
    netDebtToMerkez: list.reduce((s, o) => s + (o.netDebtToMerkez ?? 0), 0),
  }
})
const dealerCards = computed(() => dealerSummary.value?.dealers ?? [])
const dealerTotals = computed(() => dealerSummary.value?.totals ?? { totalPayable: 0, totalReceivable: 0, netPosition: 0 })

const pendingActionCount = computed(() => pendingTransfers.value.length + ownerAlerts.value.length)

// ── Z-Report history → sparklines + bugün vs dün
const sortedZHistory = computed(() =>
  [...zHistory.value].sort((a, b) =>
    new Date(a.reportDate ?? a.startDate ?? 0).getTime() - new Date(b.reportDate ?? b.startDate ?? 0).getTime()
  )
)

const profitSpark = computed(() => sortedZHistory.value.map(z => z?.summary?.totalProfit ?? z?.summary?.totalProfitInTRY ?? 0))
const txSpark = computed(() => sortedZHistory.value.map(z => z?.summary?.totalTransactions ?? 0))

const kpiChanges = computed(() => {
  const h = sortedZHistory.value
  if (h.length < 2) return { profit: null, txCount: null }
  const cur = h[h.length - 1]?.summary ?? {}
  const prev = h[h.length - 2]?.summary ?? {}
  const cp = cur.totalProfit ?? cur.totalProfitInTRY ?? 0
  const pp = prev.totalProfit ?? prev.totalProfitInTRY ?? 0
  const ct = cur.totalTransactions ?? 0
  const pt = prev.totalTransactions ?? 0
  const pct = (c: number, p: number) => p === 0 ? (c > 0 ? 100 : c < 0 ? -100 : 0) : ((c - p) / Math.abs(p)) * 100
  return { profit: pct(cp, pp), txCount: pct(ct, pt) }
})

// ── Active staff from recent transactions
const activeStaffList = computed(() => {
  const m = new Map<string, { name: string; last: string; count: number }>()
  for (const tx of recentTx.value) {
    const n = txUser(tx)
    if (!n) continue
    if (!m.has(n)) m.set(n, { name: n, last: tx.createdAt ?? tx.transactionDate ?? '', count: 0 })
    m.get(n)!.count++
  }
  return [...m.values()].sort((a, b) => new Date(b.last).getTime() - new Date(a.last).getTime()).slice(0, 8)
})

function sparkPath(vals: number[], w = 80, h = 24): string {
  if (vals.length < 2) return ''
  const mx = Math.max(...vals, 1), mn = Math.min(...vals, 0), rng = mx - mn || 1
  const step = w / (vals.length - 1)
  return vals.map((v, i) => `${i ? 'L' : 'M'}${(i * step).toFixed(1)},${(h - ((v - mn) / rng) * h * 0.7 - h * 0.15).toFixed(1)}`).join(' ')
}

function timeAgo(d: string): string {
  if (!d) return '—'
  const diff = Date.now() - new Date(d).getTime()
  const mins = Math.floor(diff / 60000)
  if (mins < 1) return 'az önce'
  if (mins < 60) return `${mins} dk önce`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs} sa önce`
  return `${Math.floor(hrs / 24)} gün önce`
}

// ── Computed: Staff
const staffOffices = computed(() => {
  const all = dashboard.value?.offices ?? []
  if (!myOfficeIds.value.size) return all
  return all.filter((o: any) => myOfficeIds.value.has(o.officeId))
})
const staffVaults = computed(() => staffOffices.value.flatMap((o: any) => (o.vaults ?? []).map((v: any) => ({ ...v, officeName: o.officeName }))))

const VAULT_CURRENCIES = ['TRY', ...MAIN_CURRENCIES]

function vaultBalanceFor(code: string): number {
  const found = vaultMainCurrencies.value.find((c: any) => c.currencyCode === code)
  return found?.balance ?? 0
}

const vaultMainCurrencies = computed(() => {
  const currencies: any[] = []
  for (const v of staffVaults.value) {
    for (const c of (v.currencies ?? [])) {
      if (c.balance > 0 && MAIN_CURRENCIES.includes(c.currencyCode)) currencies.push(c)
    }
  }
  return currencies.sort((a: any, b: any) => MAIN_CURRENCIES.indexOf(a.currencyCode) - MAIN_CURRENCIES.indexOf(b.currencyCode))
})

function filteredVaultCurrencies(vault: any): any[] {
  return (vault.currencies ?? []).filter((c: any) => c.balance > 0 && VAULT_CURRENCIES.includes(c.currencyCode))
}

const zSummary = computed(() => zReport.value?.summary ?? {})

const staffZKpi = computed(() => {
  const myOff = staffOffices.value
  const totalAssets = myOff.reduce((a: number, o: any) => a + (o.totalValueInBaseCurrency ?? 0), 0)
  const zs = zSummary.value
  const profit = zs.totalProfit ?? zs.totalProfitInTRY ?? 0
  const txCount = zs.totalTransactions ?? 0
  const volume = zs.totalForeignCurrencyProcessed ?? 0
  const margin = zs.profitMargin ?? 0
  const vaultValue = zs.totalValueInBaseCurrency ?? totalAssets
  return [
    { icon: 'account_balance_wallet', label: 'Kasa Değeri', value: fmtNum(vaultValue), unit: '₺', color: '#7c3aed', bg: '#f3f0ff' },
    { icon: 'trending_up', label: 'Günlük Kar', value: plSign(profit) + fmtNum(profit), unit: '₺', color: plColor(profit), bg: profit >= 0 ? '#f0fdf4' : '#fef2f2' },
    { icon: 'swap_horiz', label: 'İşlem Sayısı', value: String(txCount), unit: 'adet', color: 'var(--color-primary)', bg: 'var(--color-primary-light)' },
    { icon: 'monitoring', label: 'İşlem Hacmi', value: fmtNum(volume), unit: '₺', color: '#0ea5e9', bg: 'rgba(14,165,233,0.10)' },
    { icon: 'percent', label: 'Kar Marjı', value: fmtNum(margin, 1), unit: '%', color: '#f59e0b', bg: 'rgba(245,158,11,0.10)' },
    { icon: 'receipt_long', label: 'Giderler', value: fmtNum(expenseTotals.value.total), unit: '₺', color: '#ef4444', bg: '#fef2f2' },
  ]
})

// ── Main loader
async function load() {
  isLoading.value = true
  dateStr.value = new Date().toLocaleDateString('tr-TR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })
  try {
    const calls: Promise<any>[] = [apiService.getDashboardData(), loadRecentTx(), loadMyAccess()]
    if (authStore.isOwner) calls.push(loadOffices(), loadDealerSummary(), loadPendingTransfers(), loadAlerts(), loadZHistory())
    const [db] = await Promise.all(calls)
    dashboard.value = db
    // Use rates from dashboard response for owner; separate call for staff
    if (authStore.isAdmin) {
      const dbRates = db?.exchangeRates ?? []
      if (dbRates.length) liveRates.value = dbRates
      else await loadLiveRates()
    } else {
      await Promise.all([loadStaffZReport(), loadStaffParties(), loadStaffExpenses(), loadLiveRates()])
    }
  } catch (e) { console.error(e) }
  finally { isLoading.value = false }
}

// ── Owner Quick Actions (modals)
const activeModal = ref<string | null>(null)
const modalLoading = ref(false)
const modalMsg = ref({ type: '', text: '' })
const modalUsers = ref<any[]>([])
const modalCurrencies = ref<any[]>([])
const modalVaultList = ref<any[]>([])

const modalTitles: Record<string, string> = {
  addUser: 'Kullanıcı Ekle', removeUser: 'Kullanıcı Çıkar',
  addVault: 'Kasa Ekle', removeVault: 'Kasa Çıkar',
  loadBalance: 'Bakiye Yükle', transfer: 'Transfer Yap',
}

const formUser = ref({ firstname: '', lastname: '', mail: '', username: '', password: '', rank: 1 })
const formVault = ref({ name: '', officeId: '' })
const formBalance = ref({ vaultId: '', currencyId: '', amount: 0, description: '' })
const formTransfer = ref({ sourceVaultId: '', targetVaultId: '', currencyId: '', amount: 0, notes: '' })
const selectedUserId = ref('')
const selectedVaultId = ref('')

const modalVaults = computed(() => {
  return modalVaultList.value.map(v => ({
    id: v.vaultId ?? v.id,
    name: v.vaultName ?? v.name ?? 'Kasa',
    officeName: v.officeName ?? '',
    officeId: v.officeId ?? ''
  }))
})

async function openModal(type: string) {
  activeModal.value = type
  modalMsg.value = { type: '', text: '' }
  if (type === 'removeUser') {
    try { const d = await apiService.getUsers(); modalUsers.value = Array.isArray(d) ? d : (d?.items ?? []) } catch { modalUsers.value = [] }
  }
  if (['loadBalance', 'transfer', 'removeVault'].includes(type)) {
    try { const d = await apiService.getVaults(); modalVaultList.value = Array.isArray(d) ? d : [] } catch { modalVaultList.value = [] }
  }
  if (['loadBalance', 'transfer'].includes(type)) {
    try { const d = await apiService.getCurrencies(); modalCurrencies.value = Array.isArray(d) ? d : [] } catch { modalCurrencies.value = [] }
  }
}

function closeModal() {
  activeModal.value = null
  formUser.value = { firstname: '', lastname: '', mail: '', username: '', password: '', rank: 1 }
  formVault.value = { name: '', officeId: '' }
  formBalance.value = { vaultId: '', currencyId: '', amount: 0, description: '' }
  formTransfer.value = { sourceVaultId: '', targetVaultId: '', currencyId: '', amount: 0, notes: '' }
  selectedUserId.value = ''
  selectedVaultId.value = ''
}

async function submitModal() {
  modalLoading.value = true
  modalMsg.value = { type: '', text: '' }
  try {
    switch (activeModal.value) {
      case 'addUser': {
        if (!formUser.value.mail || !formUser.value.username || !formUser.value.password) throw new Error('Tüm alanları doldurun')
        const regRes = await apiService.registerUser(formUser.value)
        const newId = regRes?.userInfo?.id
        if (newId && formUser.value.rank !== 1) {
          await apiService.updateUser({ id: newId, username: formUser.value.username, mail: formUser.value.mail, firstname: formUser.value.firstname, lastname: formUser.value.lastname, rank: formUser.value.rank, isEmailVerified: false })
        }
        modalMsg.value = { type: 'ok', text: 'Kullanıcı başarıyla eklendi' }
      }
        break
      case 'removeUser': {
        if (!selectedUserId.value) throw new Error('Kullanıcı seçin')
        const usr = modalUsers.value.find((u: any) => u.id === selectedUserId.value)
        if (!usr) throw new Error('Kullanıcı bulunamadı')
        await apiService.updateUser({ id: usr.id, username: usr.username ?? '', mail: usr.mail ?? '', firstname: usr.firstname ?? '', lastname: usr.lastname ?? '', rank: 0, isEmailVerified: usr.isEmailVerified ?? false })
        modalMsg.value = { type: 'ok', text: 'Kullanıcı banlandı' }
      }
        break
      case 'addVault':
        if (!formVault.value.name || !formVault.value.officeId) throw new Error('Kasa adı ve şube seçin')
        await apiService.saveVault({ name: formVault.value.name, officeId: formVault.value.officeId, isActive: true })
        modalMsg.value = { type: 'ok', text: 'Kasa oluşturuldu' }
        break
      case 'removeVault':
        if (!selectedVaultId.value) throw new Error('Kasa seçin')
        await apiService.deleteVault(selectedVaultId.value)
        modalMsg.value = { type: 'ok', text: 'Kasa silindi' }
        break
      case 'loadBalance':
        if (!formBalance.value.vaultId || !formBalance.value.currencyId || !formBalance.value.amount) throw new Error('Tüm alanları doldurun')
        await apiService.updateVaultBalance({
          vaultId: formBalance.value.vaultId, currencyId: formBalance.value.currencyId,
          amount: Number(formBalance.value.amount), description: formBalance.value.description || 'Dashboard bakiye yükleme',
          isEntireBalance: false
        })
        modalMsg.value = { type: 'ok', text: 'Bakiye güncellendi' }
        break
      case 'transfer':
        if (!formTransfer.value.sourceVaultId || !formTransfer.value.targetVaultId || !formTransfer.value.currencyId || !formTransfer.value.amount) throw new Error('Tüm alanları doldurun')
        await apiService.createTransferRequest({
          sourceVaultId: formTransfer.value.sourceVaultId, targetVaultId: formTransfer.value.targetVaultId,
          currencyId: formTransfer.value.currencyId, amount: Number(formTransfer.value.amount),
          notes: formTransfer.value.notes || ''
        })
        modalMsg.value = { type: 'ok', text: 'Transfer talebi oluşturuldu' }
        break
    }
    setTimeout(() => { closeModal(); load() }, 1200)
  } catch (e: any) {
    modalMsg.value = { type: 'err', text: e?.response?.data?.error ?? e?.response?.data?.message ?? e?.message ?? 'Hata oluştu' }
  } finally { modalLoading.value = false }
}

let timer: ReturnType<typeof setInterval> | null = null

const handleVisibilityChange = () => {
  if (document.hidden) {
    if (timer) { clearInterval(timer); timer = null }
  } else {
    load()
    timer = setInterval(load, 60000)
  }
}

onMounted(() => {
  load()
  timer = setInterval(load, 60000)
  document.addEventListener('visibilitychange', handleVisibilityChange)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
  document.removeEventListener('visibilitychange', handleVisibilityChange)
})
</script>

<template>
  <div class="db">

    <div v-if="isLoading" class="db-loading">
      <div class="spinner"></div><span>Yükleniyor...</span>
    </div>

    <template v-else>
      <!-- Header -->
      <div class="db-hero">
        <div class="db-hero-left">
          <p class="db-hero-hello">Hoş Geldiniz</p>
          <h1 class="db-hero-name">{{ authStore.user?.firstname ?? authStore.user?.username }}</h1>
          <p class="db-hero-date">{{ dateStr }}</p>
        </div>
        <div class="db-hero-right">
          <div v-if="authStore.isOwner && pendingActionCount > 0" class="db-hero-badge" @click="router.push('/ihtiyar/owner-panel')">
            <span class="material-symbols-outlined" aria-hidden="true">notifications_active</span>
            {{ pendingActionCount }} bekleyen
          </div>
          <button class="db-hero-refresh" @click="load" title="Yenile">
            <span class="material-symbols-outlined" aria-hidden="true">refresh</span>
          </button>
        </div>
      </div>

      <!-- ═══════════ STAFF VIEW ═══════════ -->
      <template v-if="!authStore.isAdmin">
        <div class="sf-kpi-row">
          <div v-for="k in staffZKpi.slice(0, 3)" :key="k.label" class="sf-kpi" :style="{ '--kpi-accent': k.color }">
            <div class="sf-kpi-icon" :style="{ background: k.bg }">
              <span class="material-symbols-outlined" aria-hidden="true" :style="{ color: k.color }">{{ k.icon }}</span>
            </div>
            <div class="sf-kpi-body">
              <span class="sf-kpi-label">{{ k.label }}</span>
              <span class="sf-kpi-value">{{ k.value }} <small>{{ k.unit }}</small></span>
            </div>
          </div>
        </div>
        <div class="sf-kpi-row">
          <div v-for="k in staffZKpi.slice(3)" :key="k.label" class="sf-kpi" :style="{ '--kpi-accent': k.color }">
            <div class="sf-kpi-icon" :style="{ background: k.bg }">
              <span class="material-symbols-outlined" aria-hidden="true" :style="{ color: k.color }">{{ k.icon }}</span>
            </div>
            <div class="sf-kpi-body">
              <span class="sf-kpi-label">{{ k.label }}</span>
              <span class="sf-kpi-value">{{ k.value }} <small>{{ k.unit }}</small></span>
            </div>
          </div>
        </div>

        <div class="sf-main-grid">
          <div class="sf-panel">
            <div class="sf-panel-head">
              <div class="sf-panel-title">
                <span class="material-symbols-outlined" aria-hidden="true">currency_exchange</span>Döviz Kurları
              </div>
              <button class="sf-link-btn" @click="router.push('/ihtiyar/exchange-v2')">İşlem Yap <span class="material-symbols-outlined" aria-hidden="true">arrow_forward</span></button>
            </div>
            <div v-if="!mainRates.length" class="state-msg"><span class="material-symbols-outlined" aria-hidden="true">info</span> Kur verisi bulunamadı</div>
            <table v-else class="sf-rates">
              <thead><tr><th>Döviz</th><th>Alış</th><th>Satış</th><th>Kasada</th></tr></thead>
              <tbody>
                <tr v-for="r in mainRates" :key="r.id">
                  <td class="sf-rate-cur">
                    <span class="sf-rate-code">{{ r.sourceCurrencyCode }}</span>
                    <span class="sf-rate-name">{{ r.sourceCurrencyName }}</span>
                  </td>
                  <td class="sf-rate-buy">{{ fmtNum(r.buyRate, r.buyRate < 1 ? 6 : 2) }} <small>₺</small></td>
                  <td class="sf-rate-sell">{{ fmtNum(r.sellRate, r.sellRate < 1 ? 6 : 2) }} <small>₺</small></td>
                  <td class="sf-rate-vault">{{ fmtNum(vaultBalanceFor(r.sourceCurrencyCode), 2) }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="sf-panel sf-vault-panel" v-for="vault in staffVaults" :key="vault.vaultId">
            <div class="sf-panel-head sf-vault-head">
              <div class="sf-panel-title">
                <span class="material-symbols-outlined" aria-hidden="true">account_balance_wallet</span>{{ vault.vaultName }}
              </div>
              <div class="sf-vault-total">₺{{ fmtMoney(vault.totalValueInBaseCurrency) }}</div>
            </div>
            <div class="sf-vault-grid">
              <div v-for="cur in filteredVaultCurrencies(vault)" :key="cur.currencyCode" class="sf-vault-item">
                <span class="sf-vault-code">{{ cur.currencyCode }}</span>
                <span class="sf-vault-bal">{{ fmtMoney(cur.balance) }}</span>
                <span class="sf-vault-val" v-if="!cur.isBaseCurrency">≈ ₺{{ fmtMoney(cur.valueInBaseCurrency) }}</span>
              </div>
            </div>
          </div>
        </div>

        <div class="sf-info-card">
          <div class="sf-info-head">
            <span class="material-symbols-outlined" aria-hidden="true" style="color:var(--color-primary)">contacts</span>
            <span class="sf-info-title">Cari Hesaplar</span>
            <button class="sf-link-btn" @click="router.push('/ihtiyar/parties')">Detay <span class="material-symbols-outlined" aria-hidden="true">arrow_forward</span></button>
          </div>
          <div v-if="partyTotals" class="sf-info-body sf-info-inline">
            <div class="sf-info-row"><span>Toplam Cari</span><strong>{{ partyTotals.count }}</strong></div>
            <div class="sf-info-row"><span>Alacak</span><strong style="color:#16a34a">{{ fmtMoney(partyTotals.receivables) }} ₺</strong></div>
            <div class="sf-info-row"><span>Borç</span><strong style="color:var(--color-danger)">{{ fmtMoney(partyTotals.payables) }} ₺</strong></div>
            <div class="sf-info-row sf-info-highlight"><span>Net Bakiye</span><strong :style="{ color: plColor(partyTotals.net) }">{{ plSign(partyTotals.net) }}{{ fmtMoney(partyTotals.net) }} ₺</strong></div>
          </div>
          <div v-else class="state-msg"><span class="material-symbols-outlined" aria-hidden="true">info</span> Veri yüklenemedi</div>
        </div>

        <div class="sf-panel">
          <div class="sf-panel-head">
            <div class="sf-panel-title"><span class="material-symbols-outlined" aria-hidden="true">history</span>Son İşlemler</div>
            <button class="sf-link-btn" @click="router.push('/ihtiyar/exchange-v2')">Tümü <span class="material-symbols-outlined" aria-hidden="true">arrow_forward</span></button>
          </div>
          <div v-if="recentTxLoading" class="state-msg"><span class="material-symbols-outlined spin">progress_activity</span></div>
          <div v-else-if="!recentTx.length" class="state-msg"><span class="material-symbols-outlined" aria-hidden="true">receipt_long</span> Henüz işlem yok</div>
          <div v-else class="sf-tx-list">
            <div v-for="tx in recentTx.slice(0, 10)" :key="tx.id" class="sf-tx-row">
              <span class="sf-tx-badge" :class="txTypeLabel(tx).cls">{{ txTypeLabel(tx).label }}</span>
              <span class="sf-tx-cur">{{ tx.sourceCurrencyCode ?? tx.currencyCode ?? '—' }}</span>
              <span class="sf-tx-amount">{{ fmtMoney(tx.sourceAmount ?? tx.amount) }}</span>
              <span class="sf-tx-rate">@ {{ fmtNum(tx.exchangeRate ?? tx.rate, 4) }}</span>
              <span class="sf-tx-total">{{ fmtMoney(tx.targetAmount ?? tx.totalTry) }} ₺</span>
              <span class="sf-tx-time">{{ txTime(tx) }}</span>
            </div>
          </div>
        </div>
      </template>

      <!-- ═══════════ OWNER / ADMIN VIEW ═══════════ -->
      <template v-else>

        <!-- 1. Kur Bandı -->
        <div class="rate-band" v-if="mainRates.length">
          <div class="rate-band-inner">
            <div v-for="r in mainRates" :key="r.id" class="rate-chip">
              <span class="rate-chip-code">{{ r.sourceCurrencyCode }}</span>
              <span class="rate-chip-buy">{{ fmtNum(r.buyRate, r.buyRate < 1 ? 4 : 2) }}</span>
              <span class="rate-chip-sep">/</span>
              <span class="rate-chip-sell">{{ fmtNum(r.sellRate, r.sellRate < 1 ? 4 : 2) }}</span>
            </div>
          </div>
        </div>

        <!-- 2. KPI Kartları -->
        <div class="ok-grid">
          <div v-for="k in ownerKpi" :key="k.label" class="ok-card" :style="{ borderLeftColor: k.color, background: `linear-gradient(135deg, ${k.bg} 0%, #ffffff 60%)` }">
            <div class="ok-icon" :style="{ background: k.color }">
              <span class="material-symbols-outlined" aria-hidden="true" style="color:#fff">{{ k.icon }}</span>
            </div>
            <div class="ok-body">
              <span class="ok-label">{{ k.label }}</span>
              <span class="ok-value" :style="{ color: k.label.includes('K/Z') ? k.color : undefined }">{{ k.value }} <small>{{ k.unit }}</small></span>
              <span v-if="k.change != null && k.change !== 0" class="ok-change" :class="k.change > 0 ? 'up' : 'down'">
                <span class="material-symbols-outlined" aria-hidden="true">{{ k.change > 0 ? 'trending_up' : 'trending_down' }}</span>
                %{{ Math.abs(k.change).toFixed(1) }} <span class="ok-change-label">düne göre</span>
              </span>
            </div>
            <svg v-if="k.spark?.length >= 2" class="ok-spark" :viewBox="'0 0 80 24'" preserveAspectRatio="none">
              <path :d="sparkPath(k.spark)" fill="none" :stroke="k.color" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" opacity="0.35" />
            </svg>
          </div>
        </div>

        <!-- 2.5 Hızlı İşlemler -->
        <div class="qa-section">
          <div class="qa-head"><span class="material-symbols-outlined" aria-hidden="true">bolt</span> Hızlı İşlemler</div>
          <div class="qa-grid">
            <button class="qa-btn" @click="openModal('addUser')"><span class="material-symbols-outlined" aria-hidden="true" style="color:#22c55e">person_add</span><span>Kullanıcı Ekle</span></button>
            <button class="qa-btn" @click="openModal('removeUser')"><span class="material-symbols-outlined" aria-hidden="true" style="color:#ef4444">person_remove</span><span>Kullanıcı Çıkar</span></button>
            <button class="qa-btn" @click="openModal('addVault')"><span class="material-symbols-outlined" aria-hidden="true" style="color:var(--color-primary)">add_card</span><span>Kasa Ekle</span></button>
            <button class="qa-btn" @click="openModal('removeVault')"><span class="material-symbols-outlined" aria-hidden="true" style="color:#f59e0b">credit_card_off</span><span>Kasa Çıkar</span></button>
            <button class="qa-btn" @click="openModal('loadBalance')"><span class="material-symbols-outlined" aria-hidden="true" style="color:#0ea5e9">account_balance_wallet</span><span>Bakiye Yükle</span></button>
            <button class="qa-btn" @click="openModal('transfer')"><span class="material-symbols-outlined" aria-hidden="true" style="color:#8b5cf6">swap_horiz</span><span>Transfer Yap</span></button>
          </div>
        </div>

        <!-- 3. Pozisyon + Bekleyen Aksiyonlar -->
        <div class="ow-two-col">
          <!-- Pozisyon Özeti -->
          <div class="ow-card">
            <div class="ow-card-head">
              <span class="material-symbols-outlined" aria-hidden="true">donut_large</span>
              <h3>Döviz Pozisyonu</h3>
              <div class="ow-risk-badge" :style="{ color: riskLevel.color, background: riskLevel.color + '18', borderColor: riskLevel.color + '40' }">
                <span class="material-symbols-outlined" aria-hidden="true">{{ riskLevel.level === 'high' ? 'warning' : riskLevel.level === 'medium' ? 'info' : 'check_circle' }}</span>
                Yoğunlaşma: {{ riskLevel.label }}
              </div>
            </div>
            <div v-if="!topCurrencies.length" class="state-msg"><span class="material-symbols-outlined" aria-hidden="true">account_balance_wallet</span> Döviz pozisyonu yok</div>
            <div v-else class="pos-list">
              <div v-for="c in topCurrencies.slice(0, 8)" :key="c.currencyCode" class="pos-row">
                <span class="pos-code">{{ c.currencyCode }}</span>
                <span class="pos-amount">{{ fmtNum(c.totalAmount, 2) }}</span>
                <div class="pos-bar-wrap">
                  <div class="pos-bar" :style="{ width: Math.min(100, (c.totalValueInBaseCurrency / totalFxValue) * 100) + '%' }">
                    <span class="pos-pct">%{{ fmtNum((c.totalValueInBaseCurrency / totalFxValue) * 100, 0) }}</span>
                  </div>
                </div>
                <span class="pos-try">{{ fmtNum(c.totalValueInBaseCurrency) }} ₺</span>
              </div>
            </div>
          </div>

          <!-- Bekleyen Aksiyonlar -->
          <div class="ow-card">
            <div class="ow-card-head">
              <span class="material-symbols-outlined" aria-hidden="true">pending_actions</span>
              <h3>Bekleyen Aksiyonlar</h3>
            </div>
            <div class="action-list">
              <div class="action-item" @click="router.push('/ihtiyar/owner-panel?tab=transfers')" style="cursor:pointer">
                <div class="action-icon" :class="pendingTransfers.length ? 'warn' : 'ok'">
                  <span class="material-symbols-outlined" aria-hidden="true">swap_horiz</span>
                </div>
                <div class="action-body">
                  <span class="action-title">Bekleyen Transferler</span>
                  <span class="action-desc">Onay bekleyen şubelerarası transferler</span>
                </div>
                <span class="action-count" :class="pendingTransfers.length ? 'warn' : 'ok'">{{ pendingTransfers.length }}</span>
              </div>
              <div class="action-item" @click="router.push('/ihtiyar/owner-panel?alerts=1')" style="cursor:pointer">
                <div class="action-icon" :class="ownerAlerts.length ? 'warn' : 'ok'">
                  <span class="material-symbols-outlined" aria-hidden="true">notifications</span>
                </div>
                <div class="action-body">
                  <span class="action-title">Sistem Uyarıları</span>
                  <span class="action-desc">Okunmamış uyarılar ve bildirimler</span>
                </div>
                <span class="action-count" :class="ownerAlerts.length ? 'warn' : 'ok'">{{ ownerAlerts.length }}</span>
              </div>
              <div class="action-item" @click="router.push('/ihtiyar/owner-panel?tab=branches')" style="cursor:pointer">
                <div class="action-icon info">
                  <span class="material-symbols-outlined" aria-hidden="true">store</span>
                </div>
                <div class="action-body">
                  <span class="action-title">Aktif Şubeler</span>
                  <span class="action-desc">Toplam kasa sayısı ve operasyon durumu</span>
                </div>
                <span class="action-count info">{{ offices.length }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- 4. Merkez / Şubeler / Bayiler -->
        <div class="msb-grid" v-if="offices.length || dealerSummary">
          <!-- Başkent Ana Kasa -->
          <div class="msb-card msb-card--merkez" v-if="merkezOffice" @click="router.push('/ihtiyar/owner-panel?tab=branches')">
            <div class="msb-card-head">
              <span class="material-symbols-outlined" aria-hidden="true">hub</span>
              <h3>Başkent Ana Kasa</h3>
            </div>
            <div class="msb-card-name">{{ merkezOffice.officeName }}</div>
            <div class="msb-stats">
              <div class="msb-stat"><span class="msb-stat-label">Kasalar</span><span class="msb-stat-value">{{ merkezOffice.vaultCount ?? 0 }}</span></div>
              <div class="msb-stat"><span class="msb-stat-label">Personel</span><span class="msb-stat-value">{{ merkezOffice.userCount ?? 0 }}</span></div>
              <div class="msb-stat"><span class="msb-stat-label">Günlük K/Z</span><span class="msb-stat-value" :style="{ color: plColor(merkezOffice.dailyProfitLoss ?? 0) }">{{ plSign(merkezOffice.dailyProfitLoss ?? 0) }}{{ fmtNum(merkezOffice.dailyProfitLoss ?? 0) }} ₺</span></div>
              <div class="msb-stat"><span class="msb-stat-label">Aylık K/Z</span><span class="msb-stat-value" :style="{ color: plColor(merkezOffice.monthlyProfitLoss ?? 0) }">{{ plSign(merkezOffice.monthlyProfitLoss ?? 0) }}{{ fmtNum(merkezOffice.monthlyProfitLoss ?? 0) }} ₺</span></div>
            </div>
            <div class="msb-total">Toplam Varlık <strong>{{ fmtNum(merkezOffice.totalValueInBaseCurrency ?? 0) }} ₺</strong></div>
            <div class="msb-debt" :class="subeAggregate.netDebtToMerkez >= 0 ? 'msb-debt--credit' : 'msb-debt--owe'">
              {{ subeAggregate.netDebtToMerkez >= 0 ? 'Şubelerden Alacağı' : 'Şubelere Borçlu' }}
              <strong>{{ fmtNum(Math.abs(subeAggregate.netDebtToMerkez)) }} ₺</strong>
            </div>
          </div>

          <!-- Şubeler -->
          <div class="msb-card msb-card--sube" @click="router.push('/ihtiyar/owner-panel?tab=branches')">
            <div class="msb-card-head">
              <span class="material-symbols-outlined" aria-hidden="true">store</span>
              <h3>Şubeler</h3>
            </div>
            <div class="msb-card-name">{{ subeAggregate.count }} şube</div>
            <div class="msb-stats">
              <div class="msb-stat"><span class="msb-stat-label">Kasalar</span><span class="msb-stat-value">{{ subeAggregate.vaultCount }}</span></div>
              <div class="msb-stat"><span class="msb-stat-label">Personel</span><span class="msb-stat-value">{{ subeAggregate.userCount }}</span></div>
              <div class="msb-stat"><span class="msb-stat-label">Günlük K/Z</span><span class="msb-stat-value" :style="{ color: plColor(subeAggregate.dailyProfitLoss) }">{{ plSign(subeAggregate.dailyProfitLoss) }}{{ fmtNum(subeAggregate.dailyProfitLoss) }} ₺</span></div>
              <div class="msb-stat"><span class="msb-stat-label">Aylık K/Z</span><span class="msb-stat-value" :style="{ color: plColor(subeAggregate.monthlyProfitLoss) }">{{ plSign(subeAggregate.monthlyProfitLoss) }}{{ fmtNum(subeAggregate.monthlyProfitLoss) }} ₺</span></div>
            </div>
            <div class="msb-total">Toplam Varlık <strong>{{ fmtNum(subeAggregate.totalValueInBaseCurrency) }} ₺</strong></div>
            <div class="msb-debt" :class="subeAggregate.netDebtToMerkez >= 0 ? 'msb-debt--owe' : 'msb-debt--credit'">
              {{ subeAggregate.netDebtToMerkez >= 0 ? "Merkez'e Toplam Borç" : "Merkez'den Alacaklı" }}
              <strong>{{ fmtNum(Math.abs(subeAggregate.netDebtToMerkez)) }} ₺</strong>
            </div>
          </div>

          <!-- Bayiler -->
          <div class="msb-card msb-card--bayi" v-if="dealerSummary" @click="router.push('/ihtiyar/tg-admin')">
            <div class="msb-card-head">
              <span class="material-symbols-outlined" aria-hidden="true">storefront</span>
              <h3>Bayiler</h3>
              <span class="msb-badge-light">Cari Özet</span>
            </div>
            <div class="msb-card-name">{{ dealerCards.length }} bayi</div>
            <div class="msb-stats msb-stats--bayi">
              <div class="msb-stat"><span class="msb-stat-label">Alacak</span><span class="msb-stat-value" style="color:#22c55e">{{ fmtNum(dealerTotals.totalReceivable) }} ₺</span></div>
              <div class="msb-stat"><span class="msb-stat-label">Borç</span><span class="msb-stat-value" style="color:#ef4444">{{ fmtNum(dealerTotals.totalPayable) }} ₺</span></div>
            </div>
            <div class="msb-total">Net Pozisyon <strong :style="{ color: plColor(dealerTotals.netPosition) }">{{ plSign(dealerTotals.netPosition) }}{{ fmtNum(dealerTotals.netPosition) }} ₺</strong></div>
          </div>
        </div>

        <!-- 5. Son İşlemler + Kasa Dağılımı -->
        <div class="ow-two-col">
          <div class="ow-card">
            <div class="ow-card-head">
              <span class="material-symbols-outlined" aria-hidden="true">history</span>
              <h3>Son İşlemler</h3>
              <button class="ow-link" @click="router.push('/ihtiyar/exchange-v2')">Tümü <span class="material-symbols-outlined" aria-hidden="true">arrow_forward</span></button>
            </div>
            <div v-if="recentTxLoading" class="state-msg"><span class="material-symbols-outlined spin">progress_activity</span></div>
            <div v-else-if="!recentTx.length" class="state-msg"><span class="material-symbols-outlined" aria-hidden="true">receipt_long</span> Henüz işlem yok</div>
            <div v-else class="tx-feed">
              <div v-for="tx in recentTx" :key="tx.id" class="tx-item">
                <span class="tx-badge" :class="txTypeLabel(tx).cls">{{ txTypeLabel(tx).label }}</span>
                <div class="tx-detail">
                  <span class="tx-cur">{{ tx.sourceCurrencyCode ?? tx.currencyCode ?? '—' }}</span>
                  <span class="tx-amt">{{ fmtMoney(tx.sourceAmount ?? tx.amount) }}</span>
                </div>
                <span class="tx-rate-val">@ {{ fmtNum(tx.exchangeRate ?? tx.rate, 4) }}</span>
                <span class="tx-total-val">{{ fmtMoney(tx.targetAmount ?? tx.totalTry) }} ₺</span>
                <div class="tx-meta">
                  <span class="tx-user" v-if="txUser(tx)">{{ txUser(tx) }}</span>
                  <span class="tx-time-val">{{ txTime(tx) }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Kasa Doluluk -->
          <div class="ow-card">
            <div class="ow-card-head">
              <span class="material-symbols-outlined" aria-hidden="true">inventory_2</span>
              <h3>Kasa Doluluk Durumu</h3>
              <button class="ow-link" @click="router.push('/ihtiyar/vaults')">Kasalar <span class="material-symbols-outlined" aria-hidden="true">arrow_forward</span></button>
            </div>
            <div v-if="!sortedOffices.length" class="state-msg"><span class="material-symbols-outlined" aria-hidden="true">info</span> Kasa verisi yok</div>
            <div v-else class="vault-fill-list">
              <div v-for="o in sortedOffices" :key="o.officeId" class="vf-office">
                <div class="vf-office-head">
                  <span class="vf-office-name">{{ o.officeName }}</span>
                  <span class="vf-office-total">₺{{ fmtNum(o.totalValueInBaseCurrency ?? 0) }}</span>
                </div>
                <div class="vf-currencies">
                  <div v-for="[code, val] in Object.entries(o.currencies ?? {})" :key="code" class="vf-cur" v-show="val as number > 0">
                    <span class="vf-code">{{ code }}</span>
                    <span class="vf-bal">{{ fmtNum(val as number, 2) }}</span>
                  </div>
                  <div v-if="!Object.entries(o.currencies ?? {}).some(([, v]) => (v as number) > 0)" class="vf-empty">Bakiye yok</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 6. Personel Aktivitesi -->
        <div class="ow-card" v-if="activeStaffList.length">
          <div class="ow-card-head">
            <span class="material-symbols-outlined" aria-hidden="true">group</span>
            <h3>Personel Aktivitesi</h3>
            <button class="ow-link" @click="router.push('/ihtiyar/users')">Tümü <span class="material-symbols-outlined" aria-hidden="true">arrow_forward</span></button>
          </div>
          <div class="staff-grid">
            <div v-for="s in activeStaffList" :key="s.name" class="staff-item">
              <div class="staff-avatar">{{ s.name.charAt(0).toUpperCase() }}</div>
              <div class="staff-body">
                <span class="staff-name">{{ s.name }}</span>
                <span class="staff-meta">{{ s.count }} işlem · {{ timeAgo(s.last) }}</span>
              </div>
              <div class="staff-indicator active"></div>
            </div>
          </div>
        </div>

      </template>
    </template>

    <!-- ═══ Owner Quick-Action Modals ═══ -->
    <Teleport to="body">
      <div v-if="activeModal" class="qm-overlay" @click.self="closeModal">
        <div class="qm-box">
          <div class="qm-header">
            <h3>{{ modalTitles[activeModal!] }}</h3>
            <button class="qm-close" @click="closeModal"><span class="material-symbols-outlined" aria-hidden="true">close</span></button>
          </div>

          <div class="qm-body">
            <!-- Kullanıcı Ekle -->
            <template v-if="activeModal === 'addUser'">
              <div class="qm-row"><label>Ad</label><input v-model="formUser.firstname" placeholder="Ad" /></div>
              <div class="qm-row"><label>Soyad</label><input v-model="formUser.lastname" placeholder="Soyad" /></div>
              <div class="qm-row"><label>E-posta</label><input v-model="formUser.mail" type="email" placeholder="mail@example.com" /></div>
              <div class="qm-row"><label>Kullanıcı Adı</label><input v-model="formUser.username" placeholder="kullanici_adi" /></div>
              <div class="qm-row"><label>Şifre</label><input v-model="formUser.password" type="password" placeholder="••••••" /></div>
              <div class="qm-row">
                <label>Yetki</label>
                <select v-model.number="formUser.rank">
                  <option :value="1">Kullanıcı</option>
                  <option :value="2">Müşteri</option>
                  <option :value="50">Personel</option>
                  <option :value="99">Admin</option>
                </select>
              </div>
            </template>

            <!-- Kullanıcı Çıkar -->
            <template v-if="activeModal === 'removeUser'">
              <div class="qm-row">
                <label>Kullanıcı</label>
                <select v-model="selectedUserId">
                  <option value="">Seçiniz...</option>
                  <option v-for="u in modalUsers.filter((u: any) => u.id !== authStore.user?.id)" :key="u.id" :value="u.id">{{ u.fullName || ((u.firstname ?? '') + ' ' + (u.lastname ?? '')).trim() }} ({{ u.mail }})</option>
                </select>
              </div>
              <p v-if="selectedUserId" class="qm-warn">Bu kullanıcı banlanacak ve sisteme erişimi kapatılacak.</p>
            </template>

            <!-- Kasa Ekle -->
            <template v-if="activeModal === 'addVault'">
              <div class="qm-row"><label>Kasa Adı</label><input v-model="formVault.name" placeholder="Ana Kasa" /></div>
              <div class="qm-row">
                <label>Şube</label>
                <select v-model="formVault.officeId">
                  <option value="">Seçiniz...</option>
                  <option v-for="o in offices" :key="o.officeId" :value="o.officeId">{{ o.officeName }}</option>
                </select>
              </div>
            </template>

            <!-- Kasa Çıkar -->
            <template v-if="activeModal === 'removeVault'">
              <div class="qm-row">
                <label>Kasa</label>
                <select v-model="selectedVaultId">
                  <option value="">Seçiniz...</option>
                  <option v-for="v in modalVaults" :key="v.id" :value="v.id">{{ v.name }} ({{ v.officeName }})</option>
                </select>
              </div>
              <p v-if="selectedVaultId" class="qm-warn qm-warn-red">Bu kasa kalıcı olarak silinecek!</p>
            </template>

            <!-- Bakiye Yükle -->
            <template v-if="activeModal === 'loadBalance'">
              <div class="qm-row">
                <label>Kasa</label>
                <select v-model="formBalance.vaultId">
                  <option value="">Seçiniz...</option>
                  <option v-for="v in modalVaults" :key="v.id" :value="v.id">{{ v.name }} ({{ v.officeName }})</option>
                </select>
              </div>
              <div class="qm-row">
                <label>Para Birimi</label>
                <select v-model="formBalance.currencyId">
                  <option value="">Seçiniz...</option>
                  <option v-for="c in modalCurrencies" :key="c.id" :value="c.id">{{ c.currencyCode }} — {{ c.currencyName }}</option>
                </select>
              </div>
              <div class="qm-row"><label>Miktar</label><input v-model.number="formBalance.amount" type="number" step="0.01" placeholder="0.00" /></div>
              <div class="qm-row"><label>Açıklama</label><input v-model="formBalance.description" placeholder="Opsiyonel" /></div>
            </template>

            <!-- Transfer Yap -->
            <template v-if="activeModal === 'transfer'">
              <div class="qm-row">
                <label>Kaynak Kasa</label>
                <select v-model="formTransfer.sourceVaultId">
                  <option value="">Seçiniz...</option>
                  <option v-for="v in modalVaults" :key="'s'+v.id" :value="v.id">{{ v.name }} ({{ v.officeName }})</option>
                </select>
              </div>
              <div class="qm-row">
                <label>Hedef Kasa</label>
                <select v-model="formTransfer.targetVaultId">
                  <option value="">Seçiniz...</option>
                  <option v-for="v in modalVaults.filter(x => x.id !== formTransfer.sourceVaultId)" :key="'t'+v.id" :value="v.id">{{ v.name }} ({{ v.officeName }})</option>
                </select>
              </div>
              <div class="qm-row">
                <label>Para Birimi</label>
                <select v-model="formTransfer.currencyId">
                  <option value="">Seçiniz...</option>
                  <option v-for="c in modalCurrencies" :key="c.id" :value="c.id">{{ c.currencyCode }} — {{ c.currencyName }}</option>
                </select>
              </div>
              <div class="qm-row"><label>Miktar</label><input v-model.number="formTransfer.amount" type="number" step="0.01" placeholder="0.00" /></div>
              <div class="qm-row"><label>Not</label><input v-model="formTransfer.notes" placeholder="Opsiyonel" /></div>
            </template>
          </div>

          <div v-if="modalMsg.text" class="qm-msg" :class="modalMsg.type">
            <span class="material-symbols-outlined" aria-hidden="true">{{ modalMsg.type === 'ok' ? 'check_circle' : 'error' }}</span>
            {{ modalMsg.text }}
          </div>

          <div class="qm-footer">
            <button class="qm-cancel" @click="closeModal">İptal</button>
            <button class="qm-submit" @click="submitModal" :disabled="modalLoading">
              <span v-if="modalLoading" class="material-symbols-outlined spin">progress_activity</span>
              {{ modalLoading ? 'İşleniyor...' : 'Onayla' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.db { padding: 24px; display: flex; flex-direction: column; gap: 18px; min-height: 100vh; }
.db-loading { display: flex; flex-direction: column; align-items: center; justify-content: center; height: 60vh; gap: 16px; color: var(--color-text-secondary); }
.spinner { width: 40px; height: 40px; border: 3px solid var(--color-border); border-top-color: var(--color-primary); border-radius: 50%; animation: spin .8s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

/* ═══ Hero ═══ */
.db-hero { display: flex; align-items: center; justify-content: space-between; }
.db-hero-left { display: flex; align-items: baseline; gap: 8px; flex-wrap: wrap; }
.db-hero-hello { margin: 0; font-size: 14px; font-weight: 500; color: var(--color-text-secondary); }
.db-hero-name { font-size: 1.35rem; font-weight: 800; margin: 0; color: var(--color-text); letter-spacing: -0.02em; }
.db-hero-date { font-size: 13px; color: var(--color-text-muted); margin: 0; font-weight: 500; }
.db-hero-right { display: flex; align-items: center; gap: 10px; }
.db-hero-badge {
  display: flex; align-items: center; gap: 6px;
  padding: 6px 14px; background: #fef3c7; border: 1px solid #fcd34d;
  border-radius: var(--radius-xl); font-size: 12px; font-weight: 700; color: #92400e;
  cursor: pointer; transition: background-color .2s;
}
.db-hero-badge:hover { background: #fde68a; }
.db-hero-badge .material-symbols-outlined { font-size: 16px; }
.db-hero-refresh {
  display: flex; align-items: center; justify-content: center;
  width: 38px; height: 38px; background: var(--color-bg-page); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); cursor: pointer; color: var(--color-text-secondary); transition: background-color .2s, color .2s, transform .2s;
}
.db-hero-refresh:hover { background: var(--color-border); color: #4338ca; transform: rotate(90deg); }
.db-hero-refresh .material-symbols-outlined { font-size: 20px; }

/* ═══ Rate Band ═══ */
.rate-band {
  background: linear-gradient(135deg, #1e1b4b, #312e81);
  border-radius: var(--radius-lg); padding: 14px 20px; overflow-x: auto;
}
.rate-band-inner { display: flex; gap: 6px; min-width: max-content; }
.rate-chip {
  display: flex; align-items: center; gap: 6px;
  padding: 8px 16px; background: rgba(255,255,255,0.08);
  border-radius: var(--radius-md); white-space: nowrap;
  transition: background .15s;
}
.rate-chip:hover { background: rgba(255,255,255,0.14); }
.rate-chip-code { font-size: 13px; font-weight: 800; color: #c7d2fe; letter-spacing: 0.3px; }
.rate-chip-buy { font-size: 13px; font-weight: 700; color: #4ade80; font-variant-numeric: tabular-nums; }
.rate-chip-sep { color: rgba(255,255,255,0.25); font-size: 12px; }
.rate-chip-sell { font-size: 13px; font-weight: 700; color: #f87171; font-variant-numeric: tabular-nums; }

/* ═══ Owner KPI ═══ */
.ok-grid { display: grid; grid-template-columns: repeat(6, 1fr); gap: 12px; }
.ok-card {
  display: flex; align-items: center; gap: 14px; position: relative;
  border: 2px solid #e4e7f0; border-left-width: 7px; border-radius: var(--radius-lg);
  padding: 16px; transition: border-color .2s, box-shadow .2s, transform .2s; overflow: hidden;
  box-shadow: var(--shadow-sm);
}
.ok-card:hover { border-color: #d4d8e8; box-shadow: 0 8px 22px -6px rgba(0,0,0,0.14); transform: translateY(-3px); }
.ok-icon {
  width: 46px; height: 46px; border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
  box-shadow: 0 3px 8px -2px rgba(0,0,0,0.25);
}
.ok-icon .material-symbols-outlined { font-size: 24px; }
.ok-body { display: flex; flex-direction: column; gap: 2px; min-width: 0; flex: 1; z-index: 1; }
.ok-label { font-size: 11px; font-weight: 600; color: var(--color-text-muted); text-transform: uppercase; letter-spacing: 0.03em; }
.ok-value { font-size: 1.1rem; font-weight: 800; color: var(--color-text); letter-spacing: -0.02em; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.ok-value small { font-size: 12px; font-weight: 600; color: var(--color-text-muted); margin-left: 2px; }
.ok-change {
  display: inline-flex; align-items: center; gap: 3px;
  font-size: 11px; font-weight: 700; margin-top: 1px;
}
.ok-change .material-symbols-outlined { font-size: 14px; }
.ok-change.up { color: #16a34a; }
.ok-change.down { color: var(--color-danger); }
.ok-change-label { font-weight: 500; color: var(--color-text-muted); font-size: 10px; }
.ok-spark {
  position: absolute; right: 8px; bottom: 8px;
  width: 80px; height: 24px; z-index: 0;
}

/* ═══ Quick Actions ═══ */
.qa-section {
  background: var(--color-bg-card); border: 2px solid #e4e7f0; border-radius: var(--radius-lg);
  padding: 16px 20px; overflow: hidden; box-shadow: var(--shadow-sm);
}
.qa-head {
  display: flex; align-items: center; gap: 10px; position: relative;
  font-size: 16px; font-weight: 800; color: #1e1b4b; margin-bottom: 14px;
  padding-left: 14px; padding-bottom: 12px; border-bottom: 3px solid var(--color-warning);
}
.qa-head::before {
  content: ''; position: absolute; left: 0; top: 0; bottom: 10px; width: 5px;
  background: var(--color-warning); border-radius: 2px;
}
.qa-head > .material-symbols-outlined {
  font-size: 18px; color: #fff; background: var(--color-warning);
  width: 32px; height: 32px; border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center;
  box-shadow: 0 3px 8px -2px rgba(217,119,6,0.4);
}
.qa-grid { display: grid; grid-template-columns: repeat(6, 1fr); gap: 10px; }
.qa-btn {
  display: flex; flex-direction: column; align-items: center; gap: 8px;
  padding: 16px 8px; background: var(--color-bg-page); border: 2px solid #eef0f4;
  border-radius: var(--radius-lg); cursor: pointer; transition: background-color .2s, border-color .2s, transform .2s, box-shadow .2s;
}
.qa-btn:hover { background: var(--color-primary-light); border-color: var(--color-primary); transform: translateY(-3px); box-shadow: 0 6px 16px rgba(99,102,241,0.15); }
.qa-btn .material-symbols-outlined { font-size: 28px; }
.qa-btn span:last-child { font-size: 12px; font-weight: 700; color: var(--color-text-secondary); white-space: nowrap; }

/* ═══ Quick-Action Modal ═══ */
.qm-overlay {
  position: fixed; inset: 0; z-index: 9999;
  background: rgba(15,23,42,0.5); backdrop-filter: var(--glass-blur-strong);
  display: flex; align-items: center; justify-content: center;
}
.qm-box {
  background: var(--color-bg-card); border-radius: var(--radius-xl); width: 460px; max-width: 95vw;
  box-shadow: 0 20px 60px rgba(0,0,0,0.2); overflow: hidden;
  animation: qmIn .2s ease-out;
}
@keyframes qmIn { from { opacity: 0; transform: scale(0.95) translateY(10px); } }
.qm-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 18px 24px; border-bottom: 1px solid var(--color-bg-page);
}
.qm-header h3 { margin: 0; font-size: 16px; font-weight: 800; color: #1e1b4b; }
.qm-close {
  display: flex; align-items: center; justify-content: center;
  width: 32px; height: 32px; border-radius: var(--radius-md); background: none;
  border: none; cursor: pointer; color: var(--color-text-muted); transition: background-color .15s, color .15s;
}
.qm-close:hover { background: var(--color-bg-page); color: var(--color-text-secondary); }
.qm-body { padding: 20px 24px; display: flex; flex-direction: column; gap: 14px; }
.qm-row { display: flex; flex-direction: column; gap: 5px; }
.qm-row label { font-size: 12px; font-weight: 700; color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: 0.03em; }
.qm-row input, .qm-row select {
  padding: 10px 14px; border: 1px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 14px; color: var(--color-text); background: var(--color-bg-page);
  transition: border-color .15s, box-shadow .15s; outline: none;
}
.qm-row input:focus, .qm-row select:focus { border-color: #818cf8; box-shadow: 0 0 0 3px rgba(129,140,248,0.15); background: var(--color-bg-card); }
.qm-warn {
  margin: 0; padding: 10px 14px; background: #fef3c7; border: 1px solid #fcd34d;
  border-radius: var(--radius-md); font-size: 12px; color: #92400e; font-weight: 600;
}
.qm-warn-red { background: #fef2f2; border-color: #fecaca; color: #991b1b; }
.qm-msg {
  display: flex; align-items: center; gap: 8px;
  margin: 0 24px; padding: 10px 14px; border-radius: var(--radius-md); font-size: 13px; font-weight: 600;
}
.qm-msg .material-symbols-outlined { font-size: 18px; }
.qm-msg.ok { background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0; }
.qm-msg.err { background: #fef2f2; color: var(--color-danger); border: 1px solid #fecaca; }
.qm-footer {
  display: flex; justify-content: flex-end; gap: 10px;
  padding: 16px 24px; border-top: 1px solid var(--color-bg-page);
}
.qm-cancel {
  padding: 9px 20px; background: var(--color-bg-page); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); cursor: pointer; font-size: 13px; font-weight: 600;
  color: var(--color-text-secondary); transition: background-color .15s;
}
.qm-cancel:hover { background: var(--color-border); }
.qm-submit {
  display: flex; align-items: center; gap: 6px;
  padding: 9px 24px; background: linear-gradient(135deg, var(--color-primary), var(--color-primary-hover));
  border: none; border-radius: var(--radius-md); cursor: pointer;
  font-size: 13px; font-weight: 700; color: #fff; transition: box-shadow .15s, opacity .15s;
}
.qm-submit:hover { box-shadow: 0 4px 12px rgba(99,102,241,0.3); }
.qm-submit:disabled { opacity: 0.6; cursor: not-allowed; }
.qm-submit .material-symbols-outlined { font-size: 16px; }

/* ═══ Two column layout ═══ */
.ow-two-col { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }

/* ═══ Owner Cards ═══ */
.ow-card {
  background: var(--color-bg-card); border: 2px solid #e4e7f0; border-radius: var(--radius-lg);
  overflow: hidden; transition: box-shadow .2s; box-shadow: var(--shadow-sm);
}
.ow-card:hover { box-shadow: 0 6px 24px rgba(99,102,241,0.1); }
.ow-card-head {
  display: flex; align-items: center; gap: 12px; position: relative;
  padding: 16px 20px 14px 26px; border-bottom: 3px solid var(--color-primary);
  background: linear-gradient(180deg, var(--color-primary-light) 0%, #ffffff 100%);
}
.ow-card-head::before {
  content: ''; position: absolute; left: 0; top: 0; bottom: 0; width: 6px;
  background: var(--color-primary);
}
.ow-card-head > .material-symbols-outlined {
  font-size: 20px; color: #fff; background: var(--color-primary);
  width: 34px; height: 34px; border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center;
  box-shadow: 0 3px 8px -2px rgba(99,102,241,0.4);
}
.ow-card-head h3 { margin: 0; font-size: 16px; font-weight: 800; color: #1e1b4b; flex: 1; letter-spacing: -0.01em; }
.ow-link {
  display: flex; align-items: center; gap: 3px;
  background: none; border: none; cursor: pointer;
  color: var(--color-primary); font-size: 13px; font-weight: 700;
  padding: 5px 10px; border-radius: var(--radius-md); transition: background .2s;
}
.ow-link:hover { background: var(--color-primary-light); }
.ow-link .material-symbols-outlined { font-size: 16px; }

.ow-risk-badge {
  display: flex; align-items: center; gap: 4px;
  padding: 4px 12px; border-radius: var(--radius-xl);
  font-size: 11px; font-weight: 700; border: 1px solid;
}
.ow-risk-badge .material-symbols-outlined { font-size: 14px; }

/* ═══ Position Overview ═══ */
.pos-list { padding: 8px 0; }
.pos-row {
  display: grid; grid-template-columns: 60px 100px 1fr 110px;
  align-items: center; gap: 12px; padding: 10px 20px;
  transition: background .12s;
}
.pos-row:hover { background: #fafbfe; }
.pos-code { font-weight: 800; font-size: 14px; color: #1e1b4b; letter-spacing: 0.3px; }
.pos-amount { font-size: 13px; font-weight: 600; color: var(--color-text-secondary); font-variant-numeric: tabular-nums; text-align: right; }
.pos-bar-wrap { height: 22px; background: var(--color-bg-page); border-radius: var(--radius-sm); overflow: hidden; position: relative; }
.pos-bar {
  height: 100%; border-radius: var(--radius-sm);
  background: linear-gradient(90deg, var(--color-primary), #818cf8);
  display: flex; align-items: center; justify-content: flex-end;
  padding-right: 6px; min-width: 32px;
  transition: width .6s cubic-bezier(.4,0,.2,1);
}
.pos-pct { font-size: 10px; font-weight: 700; color: #fff; }
.pos-try { font-size: 13px; font-weight: 700; color: var(--color-text); text-align: right; font-variant-numeric: tabular-nums; }

/* ═══ Pending Actions ═══ */
.action-list { padding: 8px 0; }
.action-item {
  display: flex; align-items: center; gap: 14px;
  padding: 14px 20px; transition: background .12s;
  border-bottom: 1px solid #f8f8f8;
}
.action-item:last-child { border-bottom: none; }
.action-item:hover { background: #fafbfe; }
.action-icon {
  width: 42px; height: 42px; border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.action-icon .material-symbols-outlined { font-size: 20px; }
.action-icon.warn { background: #fef3c7; }
.action-icon.warn .material-symbols-outlined { color: var(--color-warning); }
.action-icon.ok { background: #f0fdf4; }
.action-icon.ok .material-symbols-outlined { color: #22c55e; }
.action-icon.info { background: var(--color-primary-light); }
.action-icon.info .material-symbols-outlined { color: var(--color-primary); }
.action-body { flex: 1; min-width: 0; }
.action-title { display: block; font-size: 14px; font-weight: 700; color: var(--color-text); }
.action-desc { display: block; font-size: 12px; color: var(--color-text-muted); margin-top: 2px; }
.action-count {
  font-size: 1.3rem; font-weight: 800; min-width: 36px; height: 36px;
  display: flex; align-items: center; justify-content: center;
  border-radius: var(--radius-md); flex-shrink: 0;
}
.action-count.warn { background: #fef3c7; color: var(--color-warning); }
.action-count.ok { background: #f0fdf4; color: #22c55e; }
.action-count.info { background: var(--color-primary-light); color: var(--color-primary); }

/* ═══ Merkez / Şubeler / Bayiler kartları ═══ */
.msb-grid {
  display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px;
}
@media (max-width: 900px) { .msb-grid { grid-template-columns: 1fr; } }
.msb-card {
  background: var(--color-bg-card); border: 2px solid #e4e7f0; border-top-width: 8px; border-radius: var(--radius-lg);
  padding: 20px 22px; cursor: pointer; box-shadow: var(--shadow-sm);
  transition: box-shadow .2s, border-color .2s, transform .2s;
}
.msb-card:hover { box-shadow: 0 10px 28px -8px rgba(99,102,241,0.18); transform: translateY(-3px); }
.msb-card-head { display: flex; align-items: center; gap: 10px; margin-bottom: 8px; position: relative; padding-left: 13px; }
.msb-card-head::before {
  content: ''; position: absolute; left: 0; top: 0; bottom: 0; width: 4px;
  background: currentColor; border-radius: 2px; opacity: 0.7;
}
.msb-card-head h3 { margin: 0; font-size: 15px; font-weight: 800; color: #1e1b4b; flex: 1; letter-spacing: -0.01em; }
.msb-card-head .material-symbols-outlined {
  font-size: 18px; color: #fff; background: var(--msb-accent, var(--color-primary));
  width: 32px; height: 32px; border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
  box-shadow: 0 3px 8px -2px rgba(0,0,0,0.28);
}
.msb-card-name { font-size: 12px; color: var(--color-text-secondary); font-weight: 600; margin-bottom: 14px; padding-left: 13px; }
.msb-stats { display: grid; grid-template-columns: 1fr 1fr; gap: 10px 16px; margin-bottom: 12px; }
.msb-stats--bayi { grid-template-columns: 1fr 1fr; }
.msb-stat { display: flex; flex-direction: column; gap: 2px; }
.msb-stat-label { font-size: 10px; text-transform: uppercase; letter-spacing: 0.04em; color: var(--color-text-muted); }
.msb-stat-value { font-size: 15px; font-weight: 800; color: var(--color-text); font-variant-numeric: tabular-nums; letter-spacing: -0.01em; }
.msb-total {
  font-size: 12px; color: var(--color-text-secondary); padding-top: 12px;
  border-top: 2px solid var(--color-border); font-variant-numeric: tabular-nums;
}
.msb-total strong { color: #1e1b4b; font-weight: 800; margin-left: 4px; font-size: 15px; }
.msb-debt {
  margin-top: 8px; font-size: 11px; font-weight: 600; padding: 6px 10px;
  border-radius: var(--radius-sm); font-variant-numeric: tabular-nums;
}
.msb-debt strong { margin-left: 4px; font-weight: 800; }
.msb-debt--owe { background: #fef2f2; color: #b91c1c; }
.msb-debt--credit { background: #f0fdf4; color: #15803d; }

.msb-card--merkez {
  --msb-accent: var(--color-warning);
  border-top-color: var(--msb-accent); border-color: #fde68a;
  background: linear-gradient(135deg, #fffbeb, var(--color-warning-bg));
}
.msb-card--merkez .msb-card-head { color: #92400e; }

.msb-card--sube {
  --msb-accent: var(--color-primary);
  border-top-color: var(--msb-accent); border-color: #dfe3fc;
  background: linear-gradient(135deg, var(--color-primary-light), #ffffff 55%);
}
.msb-card--sube .msb-card-head { color: var(--color-primary); }

.msb-card--bayi {
  border-style: dashed; border-color: #cbd5e1; border-top-width: 1.5px;
  background: #fafbfc; opacity: 0.92;
}
.msb-card--bayi:hover { opacity: 1; border-color: #94a3b8; }
.msb-card--bayi .msb-card-head { color: #64748b; }
.msb-card--bayi .msb-card-head .material-symbols-outlined {
  background: transparent; color: #64748b; box-shadow: none; width: auto; height: auto;
}
.msb-badge-light {
  font-size: 10px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.3px;
  background: #f1f5f9; color: #64748b; padding: 2px 8px; border-radius: var(--radius-sm);
}

/* ═══ Transaction Feed ═══ */
.tx-feed { max-height: 460px; overflow-y: auto; }
.tx-item {
  display: grid; grid-template-columns: 52px 1fr 85px 95px auto;
  align-items: center; gap: 8px; padding: 11px 18px;
  border-bottom: 1px solid #f5f5f5; font-size: 13px; transition: background .12s;
}
.tx-item:last-child { border-bottom: none; }
.tx-item:hover { background: #fafbfe; }
.tx-badge {
  padding: 3px 10px; border-radius: var(--radius-sm);
  font-size: 11px; font-weight: 700; text-align: center; letter-spacing: 0.3px;
}
.tx-buy { background: linear-gradient(135deg, #ecfdf5, var(--color-success-bg)); color: var(--color-success); }
.tx-sell { background: linear-gradient(135deg, #fef2f2, var(--color-danger-bg)); color: var(--color-danger); }
.tx-detail { display: flex; align-items: baseline; gap: 6px; }
.tx-cur { font-weight: 800; color: #1e1b4b; letter-spacing: 0.3px; }
.tx-amt { font-weight: 600; color: #374151; font-variant-numeric: tabular-nums; }
.tx-rate-val { font-size: 11px; color: var(--color-text-muted); font-variant-numeric: tabular-nums; }
.tx-total-val { font-weight: 700; color: #4338ca; text-align: right; font-variant-numeric: tabular-nums; }
.tx-meta { display: flex; flex-direction: column; align-items: flex-end; gap: 1px; }
.tx-user { font-size: 11px; font-weight: 600; color: var(--color-text-secondary); }
.tx-time-val { font-size: 10px; color: var(--color-text-muted); }

/* ═══ Vault Fill ═══ */
.vault-fill-list { padding: 8px 0; }
.vf-office { padding: 12px 20px; border-bottom: 1px solid #f5f5f5; }
.vf-office:last-child { border-bottom: none; }
.vf-office-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
.vf-office-name { font-size: 14px; font-weight: 700; color: var(--color-text); }
.vf-office-total { font-size: 13px; font-weight: 800; color: #4338ca; font-variant-numeric: tabular-nums; }
.vf-currencies { display: flex; flex-wrap: wrap; gap: 6px; }
.vf-cur {
  display: flex; align-items: center; gap: 6px;
  padding: 5px 12px; background: var(--color-bg-page); border: 1px solid #eef0f4;
  border-radius: var(--radius-md); font-size: 12px;
}
.vf-code { font-weight: 800; color: var(--color-primary-hover); }
.vf-bal { font-weight: 600; color: #374151; font-variant-numeric: tabular-nums; }
.vf-empty { font-size: 12px; color: var(--color-text-muted); padding: 4px 0; }

/* ═══ Staff Activity ═══ */
.staff-grid { display: grid; grid-template-columns: repeat(2, 1fr); }
.staff-item {
  display: flex; align-items: center; gap: 12px;
  padding: 12px 20px; border-bottom: 1px solid #f5f5f5;
  border-right: 1px solid #f5f5f5; transition: background .12s;
}
.staff-item:nth-child(2n) { border-right: none; }
.staff-item:nth-last-child(-n+2) { border-bottom: none; }
.staff-item:hover { background: #fafbfe; }
.staff-avatar {
  width: 36px; height: 36px; border-radius: var(--radius-md);
  background: linear-gradient(135deg, var(--color-primary), #818cf8);
  color: #fff; font-size: 14px; font-weight: 800;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.staff-body { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 1px; }
.staff-name { font-size: 13px; font-weight: 700; color: var(--color-text); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.staff-meta { font-size: 11px; color: var(--color-text-muted); }
.staff-indicator {
  width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0;
}
.staff-indicator.active { background: #22c55e; box-shadow: 0 0 6px rgba(34,197,94,0.4); }

/* ═══ Shared ═══ */
.state-msg { display: flex; align-items: center; justify-content: center; gap: 10px; padding: 48px 20px; color: var(--color-text-muted); font-size: 14px; font-weight: 500; }
.state-msg .material-symbols-outlined { font-size: 24px; opacity: 0.6; }
.spin { animation: spin 1s linear infinite; }

/* ═══ Staff View Styles ═══ */
.sf-kpi-row { display: grid; grid-template-columns: repeat(3, 1fr); gap: 14px; }
.sf-kpi {
  display: flex; align-items: center; gap: 14px;
  background: var(--color-bg-card); border: 1px solid #eef0f4; border-radius: var(--radius-lg); padding: 16px 18px;
  transition: border-color .2s, box-shadow .2s;
}
.sf-kpi:hover { border-color: #d4d8e8; box-shadow: 0 4px 16px -6px rgba(0,0,0,0.08); }
.sf-kpi-icon {
  width: 42px; height: 42px; border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.sf-kpi-icon .material-symbols-outlined { font-size: 22px; }
.sf-kpi-body { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.sf-kpi-label { font-size: 11px; font-weight: 600; color: var(--color-text-muted); text-transform: uppercase; letter-spacing: 0.03em; }
.sf-kpi-value { font-size: 1.15rem; font-weight: 800; color: var(--color-text); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.sf-kpi-value small { font-size: 12px; font-weight: 600; color: var(--color-text-muted); margin-left: 2px; }

.sf-panel {
  background: var(--color-bg-card); border: 1px solid #eef0f4; border-radius: var(--radius-lg); overflow: hidden;
  transition: border-color .2s, box-shadow .2s;
}
.sf-panel:hover { border-color: #d4d8e8; box-shadow: 0 4px 16px -6px rgba(0,0,0,0.08); }
.sf-panel-head {
  display: flex; align-items: center; justify-content: space-between;
  padding: 14px 18px; border-bottom: 1px solid var(--color-bg-page);
}
.sf-panel-title { display: flex; align-items: center; gap: 8px; font-size: 14px; font-weight: 700; color: var(--color-text); }
.sf-panel-title .material-symbols-outlined { font-size: 20px; color: var(--color-primary); }
.sf-link-btn {
  display: inline-flex; align-items: center; gap: 3px;
  background: none; border: none; cursor: pointer;
  font-size: 12px; font-weight: 600; color: var(--color-primary);
  padding: 4px 8px; border-radius: var(--radius-sm); transition: background .15s;
}
.sf-link-btn:hover { background: var(--color-primary-light); }
.sf-link-btn .material-symbols-outlined { font-size: 15px; }

.sf-main-grid { display: grid; grid-template-columns: 1.4fr 1fr; gap: 14px; }

.sf-rates { width: 100%; border-collapse: collapse; font-variant-numeric: tabular-nums; }
.sf-rates thead th {
  padding: 10px 18px; font-size: 11px; font-weight: 700;
  color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: 0.04em;
  text-align: left; background: var(--color-bg-page); border-bottom: 1px solid var(--color-bg-page);
}
.sf-rates thead th:nth-child(n+2) { text-align: right; }
.sf-rates tbody tr { border-bottom: 1px solid #f5f5f5; transition: background .12s; }
.sf-rates tbody tr:last-child { border-bottom: none; }
.sf-rates tbody tr:hover { background: #fafbfe; }
.sf-rates tbody td { padding: 11px 18px; font-size: 13px; }
.sf-rates tbody td:nth-child(n+2) { text-align: right; }
.sf-rate-cur { display: flex; flex-direction: column; gap: 1px; }
.sf-rate-code { font-weight: 800; font-size: 14px; color: #1e1b4b; letter-spacing: 0.3px; }
.sf-rate-name { font-size: 11px; color: var(--color-text-muted); font-weight: 500; }
.sf-rate-buy { font-weight: 700; color: #16a34a; }
.sf-rate-buy small { font-weight: 500; color: #6b7280; }
.sf-rate-sell { font-weight: 700; color: var(--color-danger); }
.sf-rate-sell small { font-weight: 500; color: #6b7280; }
.sf-rate-vault { font-weight: 600; color: #374151; }

.sf-vault-panel { display: flex; flex-direction: column; }
.sf-vault-head { background: linear-gradient(135deg, #1e1b4b, #312e81); border-bottom: none; }
.sf-vault-head .sf-panel-title { color: #e0e7ff; }
.sf-vault-head .sf-panel-title .material-symbols-outlined { color: #a5b4fc; }
.sf-vault-total { font-size: 1.1rem; font-weight: 800; color: #a5f3fc; }
.sf-vault-grid {
  display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 1px; background: var(--color-bg-page); flex: 1;
}
.sf-vault-item { display: flex; flex-direction: column; gap: 2px; padding: 12px 14px; background: var(--color-bg-card); }
.sf-vault-code { font-size: 11px; font-weight: 700; color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: 0.4px; }
.sf-vault-bal { font-size: 14px; font-weight: 700; color: var(--color-text); font-variant-numeric: tabular-nums; }
.sf-vault-val { font-size: 11px; color: var(--color-text-muted); }

.sf-info-card {
  background: var(--color-bg-card); border: 1px solid #eef0f4; border-radius: var(--radius-lg); overflow: hidden;
  transition: border-color .2s, box-shadow .2s;
}
.sf-info-card:hover { border-color: #d4d8e8; box-shadow: 0 4px 16px -6px rgba(0,0,0,0.08); }
.sf-info-head {
  display: flex; align-items: center; gap: 8px;
  padding: 14px 18px; border-bottom: 1px solid var(--color-bg-page);
}
.sf-info-head .material-symbols-outlined { font-size: 20px; }
.sf-info-title { flex: 1; font-size: 14px; font-weight: 700; color: var(--color-text); }
.sf-info-body { display: flex; flex-direction: column; }
.sf-info-row {
  display: flex; justify-content: space-between; align-items: center;
  padding: 12px 18px; border-bottom: 1px solid #f8f8f8; font-size: 13px; color: var(--color-text-secondary);
}
.sf-info-row:last-child { border-bottom: none; }
.sf-info-row strong { font-weight: 700; color: var(--color-text); font-variant-numeric: tabular-nums; }
.sf-info-highlight { background: #fafbfe; }

.sf-tx-list { display: flex; flex-direction: column; }
.sf-tx-row {
  display: grid; grid-template-columns: 56px 52px 1fr 90px 1fr 80px;
  align-items: center; gap: 8px; padding: 11px 18px;
  border-bottom: 1px solid #f5f5f5; font-size: 13px; transition: background .12s;
}
.sf-tx-row:last-child { border-bottom: none; }
.sf-tx-row:hover { background: #fafbfe; }
.sf-tx-badge {
  padding: 3px 10px; border-radius: var(--radius-sm);
  font-size: 11px; font-weight: 700; text-align: center; letter-spacing: 0.3px;
}
.sf-tx-cur { font-weight: 800; color: #1e1b4b; letter-spacing: 0.3px; }
.sf-tx-amount { font-weight: 700; color: var(--color-text); text-align: right; font-variant-numeric: tabular-nums; }
.sf-tx-rate { font-size: 11px; color: var(--color-text-muted); font-variant-numeric: tabular-nums; }
.sf-tx-total { font-weight: 700; color: #4338ca; text-align: right; font-variant-numeric: tabular-nums; }
.sf-tx-time { font-size: 11px; color: var(--color-text-muted); text-align: right; }

/* ═══ Responsive ═══ */
@media(max-width: 1200px) {
  .ok-grid { grid-template-columns: repeat(3, 1fr); }
}
@media(max-width: 900px) {
  .ok-grid { grid-template-columns: repeat(2, 1fr); }
  .qa-grid { grid-template-columns: repeat(3, 1fr); }
  .ow-two-col { grid-template-columns: 1fr; }
  .sf-main-grid { grid-template-columns: 1fr; }
  .pos-row { grid-template-columns: 50px 80px 1fr 90px; }
}
@media(max-width: 600px) {
  .db { padding: 16px; gap: 14px; }
  .ok-grid { grid-template-columns: 1fr 1fr; }
  .qa-grid { grid-template-columns: repeat(3, 1fr); }
  .sf-kpi-row { grid-template-columns: 1fr 1fr; }
  .sf-kpi-row .sf-kpi:last-child:nth-child(odd) { grid-column: 1 / -1; }
  .sf-tx-row { grid-template-columns: 56px 48px 1fr 80px; }
  .sf-tx-rate, .sf-tx-time { display: none; }
  .tx-item { grid-template-columns: 52px 1fr 85px; }
  .staff-grid { grid-template-columns: 1fr; }
  .tx-rate-val, .tx-meta { display: none; }
  .rate-band-inner { gap: 4px; }
  .rate-chip { padding: 6px 10px; }
  .db-hero-name { font-size: 1.15rem; }
}
</style>
