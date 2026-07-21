<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'
import { getCurrencyFlagImg } from '@/utils/currency'
import { useNotification } from '@/composables/useNotification'
import { useExpandable } from '@/composables/useExpandable'
import AppKpiCard from '@/components/common/AppKpiCard.vue'
import TrendLineChart from '@/components/common/TrendLineChart.vue'
import VolumeDonutChart from '@/components/common/VolumeDonutChart.vue'
import ComparisonBarChart from '@/components/common/ComparisonBarChart.vue'

const authStore = useAuthStore()
const notification = useNotification()
const voidingId = ref<string | null>(null)

async function voidVaultBalanceHistory(vh: any) {
  if (!confirm('Bu kasa hareketini iptal etmek istediğinize emin misiniz? Bu işlem geri alınamaz.')) return
  const reason = prompt('İptal sebebi (opsiyonel):') || 'Owner tarafından iptal edildi'
  voidingId.value = vh.id
  try {
    if (vh.isCombined) {
      await apiService.voidVaultBalanceHistoryGroup(vh.legs.map((l: any) => l.id), reason)
    } else {
      await apiService.voidVaultBalanceHistory(vh.id, reason)
    }
    notification.success('Kasa hareketi iptal edildi')
    await fetchReport()
  } catch (e: any) {
    notification.error(e.response?.data?.message || 'İptal başarısız')
  } finally {
    voidingId.value = null
  }
}

const isLoading = ref(false)
const reportData = ref<any>(null)
const offices = ref<any[]>([])
const selectedOfficeId = ref<any>('')
const reportMode = ref<'daily' | 'weekly' | 'monthly' | 'custom'>('daily')
const selectedDate = ref(new Date().toISOString().slice(0, 10))
const weekStart = ref(getMondayOfCurrentWeek())
const selectedYear = ref(new Date().getFullYear())
const selectedMonth = ref(new Date().getMonth() + 1)
const customStart = ref(new Date().toISOString().slice(0, 10))
const customEnd = ref(new Date().toISOString().slice(0, 10))
const error = ref<string | null>(null)
const { expandedId: expandedCurrency, toggle: toggleCurrency } = useExpandable<string>()
const { expandedId: expandedOfficeRow, toggle: toggleOfficeRow } = useExpandable<string>()
const { expandedId: expandedVaultRow, toggle: toggleVaultRow } = useExpandable<number>()
const { expandedId: expandedTxRow, toggle: toggleTxRow } = useExpandable<number>()
const { expandedId: expandedEmployeeRow, toggle: toggleEmployeeRow } = useExpandable<string>()

const showVolumeChart = ref(false)
const showOfficeChart = ref(false)
const showPartyChart = ref(false)
const showEmployeeChart = ref(false)

const officeChartLabels = computed(() => officeRows.value.map((o: any) => o.officeName))
const officeChartProfit = computed(() => officeRows.value.map((o: any) => o.profit ?? 0))

const employeeRows = computed(() => reportData.value?.employeeBreakdown ?? [])
const employeeChartLabels = computed(() => employeeRows.value.map((e: any) => e.employeeName))
const employeeChartProfit = computed(() => employeeRows.value.map((e: any) => e.totalProfit ?? 0))

const partyChartCurrencies = computed(() => {
  const debts = partyData.value?.totalDebtsByCurrency ?? {}
  const receivables = partyData.value?.totalReceivablesByCurrency ?? {}
  return [...new Set([...Object.keys(receivables), ...Object.keys(debts)])]
})
const partyChartReceivables = computed(() => partyChartCurrencies.value.map(c => partyData.value?.totalReceivablesByCurrency?.[c] ?? 0))
const partyChartDebts = computed(() => partyChartCurrencies.value.map(c => partyData.value?.totalDebtsByCurrency?.[c] ?? 0))

// KPI kartına tıklanınca son N günün kâr/hacim trendini gösteren mini grafik açılır.
const showProfitTrend = ref(false)
const trendLoading = ref(false)
const trendHistory = ref<any[]>([])

// Rapor modu (günlük/haftalık/aylık) trend penceresinin de dönemini belirler — aylık modda
// "son 14 gün" değil "son 12 ay" (yıl bazlı ay karşılaştırması) anlamlı olur.
const trendPeriodConfig = computed(() => {
  if (reportMode.value === 'weekly') return { period: 2, count: 8, label: 'Son 8 Hafta' }
  if (reportMode.value === 'monthly') return { period: 3, count: 12, label: 'Son 12 Ay' }
  return { period: 1, count: 14, label: 'Son 14 Gün' }
})

async function toggleProfitTrend() {
  showProfitTrend.value = !showProfitTrend.value
  if (showProfitTrend.value && !trendHistory.value.length) {
    trendLoading.value = true
    try {
      const oid = selectedOfficeId.value || undefined
      const { period, count } = trendPeriodConfig.value
      const data = await apiService.getZReportHistory({ officeId: oid, period, count })
      trendHistory.value = (data ?? []).slice().reverse()
    } catch {
      trendHistory.value = []
    } finally {
      trendLoading.value = false
    }
  }
}

// Saatlik dağılım: günlük/haftalık modda ve ≤7 günlük özel aralıkta anlamlı (aylık modda
// bir ayın tüm işlemlerini saate göre kırmak aşırı kalabalık ve az bilgilendirici olur).
const showHourlyChart = ref(false)
const hourlyEnabled = computed(() => {
  if (reportMode.value === 'daily' || reportMode.value === 'weekly') return true
  if (reportMode.value === 'custom') {
    const days = (new Date(customEnd.value).getTime() - new Date(customStart.value).getTime()) / 86400000
    return days >= 0 && days <= 7
  }
  return false
})
const hourlyLabels = Array.from({ length: 24 }, (_, i) => `${String(i).padStart(2, '0')}:00`)
const hourlyCounts = computed(() => {
  const buckets = new Array(24).fill(0)
  transactions.value.forEach((tx: any) => {
    if (!tx.transactionDate) return
    buckets[new Date(tx.transactionDate).getHours()]++
  })
  return buckets
})
function toggleHourlyChart() {
  if (!hourlyEnabled.value) return
  showHourlyChart.value = !showHourlyChart.value
}

const trendLabels = computed(() => trendHistory.value.map((r: any) => {
  const iso = r.reportDate ?? r.periodStart
  if (!iso) return '-'
  if (reportMode.value === 'monthly') {
    const d = new Date(iso)
    return `${months[d.getMonth()].slice(0, 3)} ${d.getFullYear()}`
  }
  return fmtDate(iso)
}))
const trendProfit = computed(() => trendHistory.value.map((r: any) => r.summary?.totalProfit ?? 0))
const trendVolume = computed(() => trendHistory.value.map((r: any) => r.summary?.totalForeignCurrencyProcessed ?? 0))

// Önceki döneme göre % değişim: mevcut GetHistoricalZReports'u count:2 ile çağırıp
// index 1'i (bir önceki dönem) kullanıyoruz — backend'de i=0 mevcut dönem, i=count-1
// en eski olacak şekilde dolduruluyor (ZReportService.cs), yani index 1 = bir önceki dönem.
// 'custom' modda temiz bir "önceki dönem" kavramı olmadığından delta hiç hesaplanmaz.
const previousReport = ref<any>(null)

async function fetchPreviousPeriod() {
  previousReport.value = null
  if (reportMode.value === 'custom') return
  const periodMap: Record<string, number> = { daily: 1, weekly: 2, monthly: 3 }
  const period = periodMap[reportMode.value]
  if (period === undefined) return
  try {
    const oid = selectedOfficeId.value || undefined
    const data = await apiService.getZReportHistory({ officeId: oid, period, count: 2 })
    previousReport.value = (data ?? [])[1] ?? null
  } catch {
    previousReport.value = null
  }
}

function pctDelta(current: number | null | undefined, previous: number | null | undefined): number | null {
  if (previous === null || previous === undefined || previous === 0) return null
  if (current === null || current === undefined) return null
  return ((current - previous) / Math.abs(previous)) * 100
}

const kpiDeltas = computed(() => {
  const prevSummary = previousReport.value?.summary
  if (!prevSummary) return {}
  const curSummary = s.value
  const keys = ['totalProfit', 'totalTransactions', 'totalForeignCurrencyProcessed', 'profitMargin', 'averageTransactionSize', 'totalValueInBaseCurrency']
  const result: Record<string, number | null> = {}
  for (const key of keys) {
    result[key] = pctDelta(curSummary?.[key], prevSummary?.[key])
  }
  return result
})

function getMondayOfCurrentWeek() {
  const d = new Date()
  const day = d.getDay()
  const diff = d.getDate() - day + (day === 0 ? -6 : 1)
  d.setDate(diff)
  return d.toISOString().slice(0, 10)
}

const fmt = (n: number | null | undefined, dec = 2) =>
  new Intl.NumberFormat('tr-TR', { minimumFractionDigits: dec, maximumFractionDigits: dec }).format(n ?? 0)

const fmtDate = (iso: string) =>
  iso ? new Date(iso).toLocaleDateString('tr-TR') : '-'

const fmtDateTime = (iso: string) =>
  iso ? new Date(iso).toLocaleString('tr-TR') : '-'

const fmtTime = (iso: string) =>
  iso ? new Date(iso).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }) : '-'

const months = [
  'Ocak','Şubat','Mart','Nisan','Mayıs','Haziran',
  'Temmuz','Ağustos','Eylül','Ekim','Kasım','Aralık'
]

const txTypeLabels: Record<number, string> = {
  0: 'Alış',
  1: 'Döviz',
  2: 'Giriş',
  3: 'Çıkış',
  4: 'Transfer',
  5: 'Düzeltme',
  6: 'Cari',
}

const txTypeColors: Record<number, string> = {
  0: 'var(--color-primary)',
  1: '#8b5cf6',
  2: '#10b981',
  3: '#ef4444',
  4: 'var(--color-secondary)',
  5: '#f59e0b',
  6: '#ec4899',
}

const s = computed(() => reportData.value?.summary ?? {})
const hasData = computed(() => !!reportData.value)
const isMultiOffice = computed(() => !selectedOfficeId.value && (reportData.value?.officeBreakdown?.length ?? 0) > 0)

const kpis = computed(() => {
  if (!s.value) return []
  const items = [
    { icon: 'trending_up', label: 'Toplam Kar', value: fmt(s.value.totalProfit ?? s.value.totalProfitInTRY ?? 0), unit: '₺', color: '#10b981', bg: 'rgba(16,185,129,0.10)', deltaKey: 'totalProfit' },
    { icon: 'swap_horiz', label: 'İşlem Sayısı', value: fmt(s.value.totalTransactions ?? 0, 0), unit: 'adet', color: 'var(--color-primary)', bg: 'rgba(99,102,241,0.10)', deltaKey: 'totalTransactions' },
    { icon: 'monitoring', label: 'İşlem Hacmi', value: fmt(s.value.totalForeignCurrencyProcessed ?? 0), unit: '₺', color: '#0ea5e9', bg: 'rgba(14,165,233,0.10)', deltaKey: 'totalForeignCurrencyProcessed' },
    { icon: 'percent', label: 'Kar Marjı', value: fmt(s.value.profitMargin ?? 0, 1), unit: '%', color: '#f59e0b', bg: 'rgba(245,158,11,0.10)', deltaKey: 'profitMargin' },
    { icon: 'straighten', label: 'Ort. İşlem', value: fmt(s.value.averageTransactionSize ?? 0), unit: '₺', color: '#8b5cf6', bg: 'rgba(139,92,246,0.10)', deltaKey: 'averageTransactionSize' },
    { icon: 'account_balance', label: 'Kasa Değeri', value: fmt(s.value.totalValueInBaseCurrency ?? 0), unit: '₺', color: 'var(--color-secondary)', bg: 'rgba(59,130,246,0.10)', deltaKey: 'totalValueInBaseCurrency' },
  ]
  return items
})

const txBreakdown = computed(() => {
  const st = s.value
  if (!st) return []
  return [
    { label: 'Döviz İşlemi', count: st.totalExchangeTransactions ?? 0, icon: 'currency_exchange', color: 'var(--color-primary)' },
    { label: 'Kasa Giriş', count: st.totalDepositTransactions ?? 0, icon: 'arrow_downward', color: '#10b981' },
    { label: 'Kasa Çıkış', count: st.totalWithdrawalTransactions ?? 0, icon: 'arrow_upward', color: '#ef4444' },
  ]
})

const volumesByCurrency = computed(() => {
  const vols = s.value?.totalVolumesByCurrency
  if (!vols || typeof vols !== 'object') return []
  return Object.entries(vols).map(([code, amount]) => ({ code, amount: Number(amount) })).filter(v => v.amount > 0)
})

const currencyRows = computed(() => reportData.value?.currencyDetails ?? [])

// Alış/Satış/Döviz işlemlerinde her işlem kasada iki ayrı satır oluşturur (alınan döviz +
// karşılığında verilen TL/döviz). Aynı açıklamaya (Exchange transaction EX-...) ve kasaya
// sahip, biri giriş biri çıkış olan tam 2 satırlık çiftleri "X alındı, karşılığında Y verildi"
// şeklinde TEK satırda göstermek için birleştiriyoruz — ham kayıtlar (iptal/void için) korunur.
const EXCHANGE_VAULT_TX_TYPES = new Set([0, 1])

const vaultRows = computed(() => {
  const histories = reportData.value?.vaultBalanceHistories ?? []
  const mapped = histories.map((h: any) => ({
    ...h,
    amount: Math.abs(h.balance ?? 0),
    isDeposit: (h.balance ?? 0) >= 0,
    typeLabel: txTypeLabels[h.transactionType] ?? 'Bilinmeyen',
    typeColor: txTypeColors[h.transactionType] ?? '#6b7280',
  }))

  const groups = new Map<string, any[]>()
  const singles: any[] = []
  for (const row of mapped) {
    if (!EXCHANGE_VAULT_TX_TYPES.has(row.transactionType) || !row.description) {
      singles.push(row)
      continue
    }
    const key = `${row.description}|${row.vaultId}`
    if (!groups.has(key)) groups.set(key, [])
    groups.get(key)!.push(row)
  }

  const rows: any[] = [...singles]
  for (const group of groups.values()) {
    if (group.length === 2 && group[0].isDeposit !== group[1].isDeposit) {
      const received = group.find((r: any) => r.isDeposit)
      const given = group.find((r: any) => !r.isDeposit)
      rows.push({
        ...received,
        isCombined: true,
        legs: group,
        receivedAmount: received.amount,
        receivedCurrencyCode: received.currencyCode,
        receivedBalance: received.runningBalance,
        givenAmount: given.amount,
        givenCurrencyCode: given.currencyCode,
        givenBalance: given.runningBalance,
        valueInBaseCurrency: received.valueInBaseCurrency || given.valueInBaseCurrency,
      })
    } else {
      rows.push(...group)
    }
  }

  return rows.sort((a: any, b: any) => new Date(b.createdDate ?? b.date).getTime() - new Date(a.createdDate ?? a.date).getTime())
})

const transactions = computed(() => reportData.value?.transactions ?? [])
const officeRows = computed(() => reportData.value?.officeBreakdown ?? [])
const partyData = computed(() => reportData.value?.partyAccountsSummary ?? null)
const cashData = computed(() => reportData.value?.cashOnlySummary ?? null)

const cashVolumesByCurrency = computed(() => {
  const vols = cashData.value?.cashVolumesByCurrency
  if (!vols || typeof vols !== 'object') return []
  return Object.entries(vols).map(([code, amount]) => ({ code, amount: Number(amount) })).filter(v => v.amount > 0)
})

const reportTitle = computed(() => {
  if (reportMode.value === 'daily') return `Günlük Z-Raporu — ${fmtDate(selectedDate.value)}`
  if (reportMode.value === 'weekly') return `Haftalık Z-Raporu — ${fmtDate(weekStart.value)}`
  if (reportMode.value === 'monthly') return `Aylık Z-Raporu — ${months[selectedMonth.value - 1]} ${selectedYear.value}`
  return `Özel Z-Raporu — ${fmtDate(customStart.value)} / ${fmtDate(customEnd.value)}`
})

const periodLabel = computed(() => {
  const r = reportData.value
  if (!r) return ''
  return `${fmtDate(r.periodStart)} — ${fmtDate(r.periodEnd)}`
})

async function loadOffices() {
  try {
    if (!authStore.isAdmin) {
      const access = await apiService.getMyOfficeAccess()
      const list = (access ?? []).map((a: any) => ({ id: a.officeId, name: a.office?.officeName ?? 'Ofis' })).filter((o: any) => o.id)
      offices.value = list
      if (list.length === 1) selectedOfficeId.value = list[0].id
      return
    }
    const res = await apiService.getVaults()
    const map: Record<string, string> = {}
    ;(res ?? []).forEach((v: any) => { if (v.officeId) map[String(v.officeId)] = v.officeName ?? `Ofis ${v.officeId}` })
    offices.value = Object.entries(map).map(([id, name]) => ({ id, name }))
  } catch { offices.value = [] }
}

async function fetchReport() {
  error.value = null
  isLoading.value = true
  reportData.value = null
  showProfitTrend.value = false
  trendHistory.value = []
  showHourlyChart.value = false
  try {
    const oid = selectedOfficeId.value || undefined
    let data: any
    if (reportMode.value === 'daily') {
      data = await apiService.getZReportDaily(oid, selectedDate.value)
    } else if (reportMode.value === 'weekly') {
      data = await apiService.getZReportWeekly(oid, weekStart.value)
    } else if (reportMode.value === 'monthly') {
      data = await apiService.getZReportMonthly(oid, selectedYear.value, selectedMonth.value)
    } else {
      data = await apiService.getZReportCustom({ officeId: oid, startDate: customStart.value, endDate: customEnd.value })
    }
    reportData.value = data
    await fetchPreviousPeriod()
  } catch (e: any) {
    error.value = e?.response?.data?.message ?? e?.message ?? 'Rapor yüklenemedi'
  } finally {
    isLoading.value = false
  }
}

function printReport() { window.print() }

// CSV dışa aktarma: işlem geçmişi tablosundaki aynı kolonlar, Excel'de Türkçe karakterlerin
// bozulmaması için UTF-8 BOM ile ve noktalı virgülle ayrılmış (tr-TR Excel'in varsayılan ayıracı).
function csvEscape(value: any): string {
  const str = String(value ?? '')
  return /[";\n]/.test(str) ? `"${str.replace(/"/g, '""')}"` : str
}

function exportCsv() {
  if (!transactions.value.length) {
    notification.warning('Dışa aktarılacak işlem bulunamadı')
    return
  }
  const headers = ['İşlem No', 'Kasa', 'Tür', 'Tarih', 'Döviz', 'Miktar', 'Kur', 'TRY Karşılığı', 'Kar (₺)', 'Durum']
  const rows = transactions.value.map((tx: any) => [
    tx.transactionNumber,
    tx.vaultName ?? '-',
    getTxTypeLabel(tx),
    fmtDateTime(tx.transactionDate),
    tx.currencyCode ?? '-',
    fmt(tx.amount),
    fmt(tx.rate ?? tx.exchangeRate, 4),
    fmt(tx.tryAmount ?? tx.totalTry),
    fmt(tx.profit ?? 0),
    tx.statusName ?? (tx.status === 2 ? 'Tamamlandı' : 'Bekliyor'),
  ])

  const csvContent = [headers, ...rows].map(r => r.map(csvEscape).join(';')).join('\r\n')
  const blob = new Blob(['﻿' + csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  const dateSuffix = reportMode.value === 'daily' ? selectedDate.value
    : reportMode.value === 'weekly' ? weekStart.value
    : reportMode.value === 'monthly' ? `${selectedYear.value}-${String(selectedMonth.value).padStart(2, '0')}`
    : `${customStart.value}_${customEnd.value}`
  link.href = url
  link.download = `z-raporu_${dateSuffix}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

function getTxTypeChipClass(tx: any): string {
  const t = tx.type ?? tx.transactionType
  if (t === 0 || t === 1) return 'chip-exchange'
  if (t === 2) return 'chip-deposit'
  if (t === 3) return 'chip-withdrawal'
  if (t === 4) return 'chip-transfer'
  if (t === 6) return 'chip-party'
  return 'chip-default'
}

function getTxTypeLabel(tx: any): string {
  if (tx.typeName) return tx.typeName
  const t = tx.type ?? tx.transactionType
  return txTypeLabels[t] ?? `Tür ${t}`
}

onMounted(async () => {
  await loadOffices()
  await fetchReport()
})
</script>

<template>
  <div class="zr">

    <!-- ── Filtre Paneli ── -->
    <div class="zr-filters">
      <div class="filter-row">
        <div class="filter-group">
          <label>Rapor Türü</label>
          <div class="mode-tabs">
            <button :class="['mt-btn', { active: reportMode === 'daily' }]" @click="reportMode = 'daily'">Günlük</button>
            <button :class="['mt-btn', { active: reportMode === 'weekly' }]" @click="reportMode = 'weekly'">Haftalık</button>
            <button :class="['mt-btn', { active: reportMode === 'monthly' }]" @click="reportMode = 'monthly'">Aylık</button>
            <button :class="['mt-btn', { active: reportMode === 'custom' }]" @click="reportMode = 'custom'">Özel</button>
          </div>
        </div>

        <div class="filter-group" v-if="reportMode === 'daily'">
          <label>Tarih</label>
          <input type="date" v-model="selectedDate" class="zr-input" />
        </div>

        <div class="filter-group" v-if="reportMode === 'weekly'">
          <label>Hafta Başlangıcı</label>
          <input type="date" v-model="weekStart" class="zr-input" />
        </div>

        <template v-if="reportMode === 'monthly'">
          <div class="filter-group">
            <label>Yıl</label>
            <input type="number" v-model.number="selectedYear" min="2020" max="2099" class="zr-input zr-input-sm" />
          </div>
          <div class="filter-group">
            <label>Ay</label>
            <select v-model.number="selectedMonth" class="zr-input">
              <option v-for="(m, i) in months" :key="i" :value="i + 1">{{ m }}</option>
            </select>
          </div>
        </template>

        <template v-if="reportMode === 'custom'">
          <div class="filter-group">
            <label>Başlangıç</label>
            <input type="date" v-model="customStart" class="zr-input" />
          </div>
          <div class="filter-group">
            <label>Bitiş</label>
            <input type="date" v-model="customEnd" class="zr-input" />
          </div>
        </template>

        <div class="filter-group" v-if="offices.length > 0 && authStore.isAdmin">
          <label>Şube</label>
          <select v-model="selectedOfficeId" class="zr-input">
            <option value="">Tüm Şubeler</option>
            <option v-for="o in offices" :key="o.id" :value="o.id">{{ o.name }}</option>
          </select>
        </div>
        <div class="filter-group" v-else-if="offices.length === 1 && !authStore.isAdmin">
          <label>Şube</label>
          <div class="zr-input zr-office-fixed">{{ offices[0].name }}</div>
        </div>

        <div class="filter-group filter-group--actions">
          <label>&nbsp;</label>
          <div class="btn-row">
            <button class="btn btn--primary" @click="fetchReport" :disabled="isLoading">
              <span class="material-symbols-outlined" aria-hidden="true">{{ isLoading ? 'hourglass_top' : 'search' }}</span>
              {{ isLoading ? 'Yükleniyor...' : 'Raporu Getir' }}
            </button>
            <button class="btn btn--ghost" @click="printReport" v-if="hasData" title="Yazdır">
              <span class="material-symbols-outlined" aria-hidden="true">print</span>
            </button>
            <button class="btn btn--ghost" @click="exportCsv" v-if="hasData && transactions.length" title="CSV Olarak İndir">
              <span class="material-symbols-outlined" aria-hidden="true">download</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Hata ── -->
    <div class="zr-error" v-if="error">
      <span class="material-symbols-outlined" aria-hidden="true">error</span> {{ error }}
    </div>

    <!-- ── Loading ── -->
    <div class="zr-loading" v-if="isLoading">
      <div class="spinner"></div>
      <span>Rapor hazırlanıyor...</span>
    </div>

    <!-- ── Rapor İçeriği ── -->
    <template v-if="!isLoading && hasData">

      <!-- ══ ZONE 1: Genel Bakış ══ -->
      <div class="zr-zone" style="--zone-color: var(--color-primary)">
        <div class="zr-zone-title">
          <span class="material-symbols-outlined" aria-hidden="true">dashboard</span>
          <span>Genel Bakış</span>
        </div>

      <!-- Başlık -->
      <div class="zr-header">
        <div>
          <h2 class="zr-title">{{ reportTitle }}</h2>
          <p class="zr-subtitle">
            <span v-if="periodLabel">{{ periodLabel }}</span>
            <span v-if="reportData?.officeName"> · {{ reportData.officeName }}</span>
            <span v-if="reportData?.isVaultOpen" class="vault-badge open">Kasa Açık</span>
            <span v-else class="vault-badge closed">Kasa Kapalı</span>
          </p>
        </div>
        <div class="zr-header-actions no-print">
          <p class="zr-timestamp print-only">Oluşturulma: {{ new Date().toLocaleString('tr-TR') }}</p>
        </div>
      </div>

      <!-- ── KPI Kartları ── -->
      <div class="kpi-grid">
        <div v-for="k in kpis" :key="k.label"
             class="kpi-slot" :class="{ clickable: k.label === 'Toplam Kar' || (k.label === 'İşlem Sayısı' && hourlyEnabled) }"
             @click="k.label === 'Toplam Kar' ? toggleProfitTrend() : (k.label === 'İşlem Sayısı' && toggleHourlyChart())">
          <AppKpiCard :icon="k.icon" :label="k.label" :value="k.value" :unit="k.unit" :color="k.color" :bg="k.bg"
                      :delta="kpiDeltas[k.deltaKey]" delta-label="önceki döneme göre" />
        </div>
      </div>

      <!-- ── Kâr & Hacim Trendi (akıllı kart — "Toplam Kar" tıklanınca açılır) ── -->
      <div class="panel trend-panel" v-if="showProfitTrend">
        <div class="panel-hd">
          <span class="material-symbols-outlined" aria-hidden="true">show_chart</span>
          <h3>Kâr &amp; Hacim Trendi — {{ trendPeriodConfig.label }}</h3>
        </div>
        <div class="trend-body">
          <div class="trend-loading" v-if="trendLoading">Yükleniyor...</div>
          <TrendLineChart v-else-if="trendHistory.length" :labels="trendLabels" :profit-series="trendProfit" :volume-series="trendVolume" />
          <div class="trend-empty" v-else>Geçmiş veri bulunamadı.</div>
        </div>
      </div>

      <!-- ── Saatlik Dağılım (akıllı kart — "İşlem Sayısı" tıklanınca açılır) ── -->
      <div class="panel trend-panel" v-if="showHourlyChart && hourlyEnabled">
        <div class="panel-hd">
          <span class="material-symbols-outlined" aria-hidden="true">schedule</span>
          <h3>Saatlik İşlem Dağılımı</h3>
        </div>
        <div class="trend-body">
          <ComparisonBarChart :labels="hourlyLabels" :datasets="[{ label: 'İşlem Sayısı', data: hourlyCounts, color: 'var(--color-primary)' }]" />
        </div>
      </div>

      <!-- ── İşlem Dağılımı + Devir Bakiye ── -->
      <div class="breakdown-row">
        <div class="tx-breakdown" v-if="txBreakdown.some(t => t.count > 0)">
          <div v-for="t in txBreakdown" :key="t.label" class="tx-chip" :style="{ '--tc': t.color }">
            <span class="material-symbols-outlined" aria-hidden="true">{{ t.icon }}</span>
            <span class="tx-count">{{ t.count }}</span>
            <span class="tx-label">{{ t.label }}</span>
          </div>
        </div>
        <div class="opening-balance" v-if="s.hasInheritedBalance">
          <span class="material-symbols-outlined" aria-hidden="true">history</span>
          <div>
            <p class="ob-label">Devir Bakiye</p>
            <p class="ob-val">{{ fmt(s.openingBalanceTRY) }} ₺</p>
          </div>
        </div>
      </div>

      <!-- ── Şube Karşılaştırma ── -->
      <div class="panel" v-if="isMultiOffice">
        <div class="panel-hd">
          <span class="material-symbols-outlined" aria-hidden="true">store</span>
          <h3>Şube Karşılaştırması</h3>
          <span class="badge">{{ officeRows.length }} şube</span>
          <button class="chart-toggle-btn" @click="showOfficeChart = !showOfficeChart" title="Grafik göster/gizle">
            <span class="material-symbols-outlined" aria-hidden="true">bar_chart</span>
          </button>
        </div>
        <div class="chart-panel-inline" v-if="showOfficeChart">
          <ComparisonBarChart :labels="officeChartLabels" :datasets="[{ label: 'Kâr (₺)', data: officeChartProfit, color: '#10b981' }]" horizontal />
        </div>
        <div class="table-wrap">
          <table class="tbl">
            <thead>
              <tr>
                <th>Şube</th>
                <th>İşlem</th>
                <th>Ciro (₺)</th>
                <th>Kar (₺)</th>
                <th>Katkı</th>
                <th>Kasa</th>
              </tr>
            </thead>
            <tbody>
              <template v-for="o in officeRows" :key="o.officeId">
                <tr class="expandable-row" :class="{ expanded: expandedOfficeRow === o.officeId }" @click="toggleOfficeRow(o.officeId)">
                  <td class="fw-600">{{ o.officeName }}</td>
                  <td>{{ fmt(o.transactionCount, 0) }}</td>
                  <td>{{ fmt(o.volumeInTRY) }}</td>
                  <td :class="o.profit >= 0 ? 'pos' : 'neg'">{{ o.profit >= 0 ? '+' : '' }}{{ fmt(o.profit) }}</td>
                  <td>
                    <div class="bar-wrap">
                      <div class="bar-fill" :style="{ width: Math.min(o.contributionPercentage ?? 0, 100) + '%' }"></div>
                      <span class="bar-text">%{{ fmt(o.contributionPercentage ?? 0, 1) }}</span>
                    </div>
                  </td>
                  <td>
                    <span class="status-dot" :class="o.isVaultOpen ? 'on' : 'off'"></span>
                    {{ o.isVaultOpen ? 'Açık' : 'Kapalı' }}
                  </td>
                </tr>
                <tr class="detail-row" v-if="expandedOfficeRow === o.officeId">
                  <td colspan="6">
                    <div class="row-detail">
                      <button class="btn btn--ghost btn--sm" @click.stop="selectedOfficeId = o.officeId; fetchReport()">
                        <span class="material-symbols-outlined" aria-hidden="true">open_in_new</span>
                        Bu şubenin tam raporunu aç
                      </button>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>

      </div>
      <!-- ══ ZONE 2: Döviz Detayı ══ -->
      <div class="zr-zone" style="--zone-color: var(--color-secondary)">
        <div class="zr-zone-title">
          <span class="material-symbols-outlined" aria-hidden="true">currency_exchange</span>
          <span>Döviz Detayı</span>
        </div>

      <!-- ── Döviz Bazlı Hacim (akıllı kart — tıklanınca dağılım grafiği açılır) ── -->
      <div class="volume-chips clickable" v-if="volumesByCurrency.length" @click="showVolumeChart = !showVolumeChart">
        <span class="vc-title">
          <span class="material-symbols-outlined" aria-hidden="true">bar_chart</span>
          Döviz Bazlı Hacim
        </span>
        <div class="vc-list">
          <div v-for="v in volumesByCurrency" :key="v.code" class="vc-item">
            <div class="cur-cell">
              <img v-if="getCurrencyFlagImg(v.code)" :src="getCurrencyFlagImg(v.code)" class="cur-flag cur-flag--sm" />
              <span class="cur-code">{{ v.code }}</span>
            </div>
            <span class="vc-amount">{{ fmt(v.amount) }}</span>
          </div>
        </div>
        <span class="material-symbols-outlined vc-chevron" aria-hidden="true">{{ showVolumeChart ? 'expand_less' : 'expand_more' }}</span>
      </div>
      <div class="panel volume-chart-panel" v-if="showVolumeChart && volumesByCurrency.length">
        <VolumeDonutChart :items="volumesByCurrency" />
      </div>

      <!-- ── Döviz Bazlı Özet ── -->
      <div class="panel" v-if="currencyRows.length">
        <div class="panel-hd">
          <span class="material-symbols-outlined" aria-hidden="true">currency_exchange</span>
          <h3>Döviz Bazlı Özet</h3>
          <span class="badge">{{ currencyRows.length }} döviz</span>
        </div>
        <div class="table-wrap">
          <table class="tbl">
            <thead>
              <tr>
                <th>Döviz</th>
                <th>Alış Miktarı</th>
                <th>Satış Miktarı</th>
                <th title="O gün alınan miktar eksi satılan miktar">Net Pozisyon</th>
                <th title="O gün satılan miktardan gerçekleşen kâr">Gerçekleşen Kâr (₺)</th>
                <th title="Gerçekleşen Kâr ÷ Satış Hasılatı">Marj</th>
                <th class="cur-chevron-col"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in currencyRows" :key="row.currencyCode"
                  class="cur-row" :class="{ expanded: expandedCurrency === row.currencyCode }"
                  @click="toggleCurrency(row.currencyCode)">
                <td>
                  <div class="cur-cell">
                    <img v-if="getCurrencyFlagImg(row.currencyCode)" :src="getCurrencyFlagImg(row.currencyCode)" class="cur-flag" />
                    <span class="cur-code">{{ row.currencyCode }}</span>
                    <span class="cur-name">{{ row.currencyName }}</span>
                  </div>
                </td>
                <td>{{ fmt(row.totalBoughtAmount) }} <small class="text-muted">({{ row.buyTransactionCount ?? 0 }})</small></td>
                <td>{{ fmt(row.totalSoldAmount) }} <small class="text-muted">({{ row.sellTransactionCount ?? 0 }})</small></td>
                <td :class="(row.netPosition ?? 0) >= 0 ? 'pos' : 'neg'">{{ fmt(row.netPosition) }}</td>
                <td :class="(row.profit ?? 0) >= 0 ? 'pos' : 'neg'" class="fw-600">{{ (row.profit ?? 0) >= 0 ? '+' : '' }}{{ fmt(row.profit) }}</td>
                <td>%{{ fmt(row.profitMargin ?? 0, 1) }}</td>
                <td class="cur-chevron-col">
                  <span class="material-symbols-outlined cur-chevron" aria-hidden="true">{{ expandedCurrency === row.currencyCode ? 'expand_less' : 'expand_more' }}</span>
                </td>
              </tr>
            </tbody>
            <tfoot v-if="currencyRows.length > 1">
              <tr>
                <td class="fw-600">TOPLAM</td>
                <td colspan="3"></td>
                <td :class="(s.totalProfit ?? 0) >= 0 ? 'pos' : 'neg'" class="fw-600">{{ fmt(s.totalProfit ?? s.totalProfitInTRY) }}</td>
                <td>%{{ fmt(s.profitMargin ?? 0, 1) }}</td>
                <td></td>
              </tr>
            </tfoot>
          </table>
        </div>
        <!-- Genişletilmiş Döviz Detay (yumuşak accordion geçişi) -->
        <div class="cur-detail" :class="{ open: !!expandedCurrency }">
          <div class="cur-detail-clip">
          <template v-for="row in currencyRows" :key="'det-' + row.currencyCode">
            <div v-if="row.currencyCode === expandedCurrency" class="cur-detail-inner">
              <div class="cd-grid">
                <div class="cd-item">
                  <span class="cd-label">Alış Maliyeti (₺)</span>
                  <span class="cd-val">{{ fmt(row.totalBuyCost) }}</span>
                </div>
                <div class="cd-item">
                  <span class="cd-label">Ort. Alış Kuru</span>
                  <span class="cd-val mono">{{ fmt(row.averageBuyRate, 4) }}</span>
                </div>
                <div class="cd-item">
                  <span class="cd-label">Satış Hasılatı (₺)</span>
                  <span class="cd-val">{{ fmt(row.totalSellRevenue) }}</span>
                </div>
                <div class="cd-item">
                  <span class="cd-label">Ort. Satış Kuru</span>
                  <span class="cd-val mono">{{ fmt(row.averageSellRate, 4) }}</span>
                </div>
                <div class="cd-item">
                  <span class="cd-label">Güncel Alış</span>
                  <span class="cd-val mono">{{ fmt(row.currentBuyRate, 4) }}</span>
                </div>
                <div class="cd-item">
                  <span class="cd-label">Güncel Satış</span>
                  <span class="cd-val mono">{{ fmt(row.currentSellRate, 4) }}</span>
                </div>
                <div class="cd-item">
                  <span class="cd-label">Spread</span>
                  <span class="cd-val mono">{{ fmt(row.spread, 4) }}</span>
                </div>
                <div class="cd-item" v-if="row.wac">
                  <span class="cd-label">WAC</span>
                  <span class="cd-val mono wac-val">{{ fmt(row.wac, 4) }}</span>
                </div>
                <div class="cd-item" v-if="row.currentBalance">
                  <span class="cd-label">Mevcut Bakiye</span>
                  <span class="cd-val">{{ fmt(row.currentBalance) }}</span>
                </div>
                <div class="cd-item" v-if="row.unrealizedProfit">
                  <span class="cd-label">Kasadaki Stok K/Z (Anlık)</span>
                  <span class="cd-val" :class="(row.unrealizedProfit ?? 0) >= 0 ? 'pos' : 'neg'">{{ fmt(row.unrealizedProfit) }} ₺</span>
                </div>
              </div>
            </div>
          </template>
          </div>
        </div>
      </div>

      </div>
      <!-- ══ ZONE 3: Kasa ve Cari ══ -->
      <div class="zr-zone" style="--zone-color: var(--color-success)">
        <div class="zr-zone-title">
          <span class="material-symbols-outlined" aria-hidden="true">account_balance_wallet</span>
          <span>Kasa ve Cari</span>
        </div>

      <!-- ── Kasa Giriş/Çıkış Özeti ── -->
      <div class="vault-ops" v-if="(s.vaultDeposits ?? 0) > 0 || (s.vaultWithdrawals ?? 0) > 0">
        <div class="vo-item">
          <span class="material-symbols-outlined" aria-hidden="true" style="color: #10b981">arrow_downward</span>
          <div>
            <p class="vo-label">Kasa Giriş</p>
            <p class="vo-val">{{ fmt(s.vaultDeposits) }} ₺</p>
          </div>
        </div>
        <div class="vo-divider"></div>
        <div class="vo-item">
          <span class="material-symbols-outlined" aria-hidden="true" style="color: #ef4444">arrow_upward</span>
          <div>
            <p class="vo-label">Kasa Çıkış</p>
            <p class="vo-val">{{ fmt(s.vaultWithdrawals) }} ₺</p>
          </div>
        </div>
        <div class="vo-divider"></div>
        <div class="vo-item">
          <span class="material-symbols-outlined" aria-hidden="true" style="color: var(--color-secondary)">sync_alt</span>
          <div>
            <p class="vo-label">Net Hareket</p>
            <p class="vo-val" :class="(s.netVaultChange ?? 0) >= 0 ? 'pos' : 'neg'">{{ (s.netVaultChange ?? 0) >= 0 ? '+' : '' }}{{ fmt(s.netVaultChange) }} ₺</p>
          </div>
        </div>
        <div class="vo-divider"></div>
        <div class="vo-item">
          <span class="material-symbols-outlined" aria-hidden="true" style="color: var(--color-primary)">balance</span>
          <div>
            <p class="vo-label">Kasa Sonrası Kar</p>
            <p class="vo-val" :class="(s.profitAfterVaultOperations ?? 0) >= 0 ? 'pos' : 'neg'">{{ fmt(s.profitAfterVaultOperations) }} ₺</p>
          </div>
        </div>
      </div>

        <!-- Kasa Bakiyeleri -->
        <div class="panel" v-if="cashData">
          <div class="panel-hd">
            <span class="material-symbols-outlined" aria-hidden="true">account_balance_wallet</span>
            <h3>Kasa Bakiyeleri</h3>
            <span class="badge" v-if="cashData.totalVaultValueInTRY">{{ fmt(cashData.totalVaultValueInTRY) }} ₺</span>
          </div>
          <div class="vault-balances" v-if="cashData.vaultBalancesByCurrency">
            <div v-for="(amount, code) in cashData.vaultBalancesByCurrency" :key="code" class="vb-item">
              <div class="cur-cell">
                <img v-if="getCurrencyFlagImg(String(code))" :src="getCurrencyFlagImg(String(code))" class="cur-flag" />
                <span class="cur-code">{{ code }}</span>
              </div>
              <span class="vb-amount" :class="Number(amount) >= 0 ? '' : 'neg'">{{ fmt(Number(amount)) }}</span>
            </div>
          </div>
          <div class="cash-summary">
            <div class="cs-row">
              <span>Nakit Kar</span>
              <span class="fw-600" :class="(cashData.cashProfit ?? 0) >= 0 ? 'pos' : 'neg'">{{ fmt(cashData.cashProfit) }} ₺</span>
            </div>
            <div class="cs-row">
              <span>Nakit İşlem</span>
              <span>{{ cashData.cashTransactionCount ?? 0 }} adet</span>
            </div>
            <div class="cs-row">
              <span>Nakit Hacim</span>
              <span>{{ fmt(cashData.cashVolumeInTRY ?? 0) }} ₺</span>
            </div>
          </div>
          <!-- Nakit Döviz Bazlı Hacim -->
          <div class="cash-volumes" v-if="cashVolumesByCurrency.length">
            <div class="cv-title">Döviz Bazlı Nakit Hacim</div>
            <div class="cv-list">
              <div v-for="v in cashVolumesByCurrency" :key="v.code" class="cv-item">
                <div class="cur-cell">
                  <img v-if="getCurrencyFlagImg(v.code)" :src="getCurrencyFlagImg(v.code)" class="cur-flag cur-flag--sm" />
                  <span class="cur-code">{{ v.code }}</span>
                </div>
                <span>{{ fmt(v.amount) }}</span>
              </div>
            </div>
          </div>
        </div>

      <!-- ── Kasa Hareketleri ── -->
      <div class="panel" v-if="vaultRows.length">
        <div class="panel-hd">
          <span class="material-symbols-outlined" aria-hidden="true">history</span>
          <h3>Kasa Hareketleri</h3>
          <span class="badge">{{ vaultRows.length }} hareket</span>
        </div>
        <div class="table-wrap">
          <table class="tbl">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Tür</th>
                <th>İşlem Tipi</th>
                <th>Alınan</th>
                <th>Verilen</th>
                <th>Bakiye</th>
                <th>Personel</th>
                <th>Açıklama</th>
                <th v-if="authStore.isOwner"></th>
              </tr>
            </thead>
            <tbody>
              <template v-for="(vh, i) in vaultRows" :key="i">
                <tr class="expandable-row" :class="{ expanded: expandedVaultRow === i }" @click="toggleVaultRow(i)">
                  <td class="no-wrap">{{ fmtDateTime(vh.createdDate ?? vh.date) }}</td>
                  <td>
                    <span v-if="vh.isCombined" class="status-chip" style="--tag-color:#8b5cf6">Döviz</span>
                    <span v-else class="status-chip" :class="vh.isDeposit ? 'done' : 'pend'">
                      {{ vh.isDeposit ? 'Giriş' : 'Çıkış' }}
                    </span>
                  </td>
                  <td>
                    <span class="type-tag" :style="{ '--tag-color': vh.typeColor }">
                      {{ vh.typeLabel }}
                    </span>
                    <span v-if="vh.isParty" class="party-indicator" title="Cari İşlem">
                      <span class="material-symbols-outlined" aria-hidden="true">person</span>
                    </span>
                  </td>
                  <td class="mono">
                    <template v-if="vh.isCombined">
                      <div class="cur-cell pos">
                        <img v-if="getCurrencyFlagImg(vh.receivedCurrencyCode)" :src="getCurrencyFlagImg(vh.receivedCurrencyCode)" class="cur-flag cur-flag--sm" />
                        <span>{{ fmt(vh.receivedAmount) }} {{ vh.receivedCurrencyCode }}</span>
                      </div>
                    </template>
                    <template v-else-if="vh.isDeposit">
                      <div class="cur-cell pos">
                        <img v-if="getCurrencyFlagImg(vh.currencyCode)" :src="getCurrencyFlagImg(vh.currencyCode)" class="cur-flag cur-flag--sm" />
                        <span>{{ fmt(Math.abs(vh.amount ?? 0)) }} {{ vh.currencyCode }}</span>
                      </div>
                    </template>
                    <span v-else class="text-muted">—</span>
                  </td>
                  <td class="mono">
                    <template v-if="vh.isCombined">
                      <div class="cur-cell neg">
                        <img v-if="getCurrencyFlagImg(vh.givenCurrencyCode)" :src="getCurrencyFlagImg(vh.givenCurrencyCode)" class="cur-flag cur-flag--sm" />
                        <span>{{ fmt(vh.givenAmount) }} {{ vh.givenCurrencyCode }}</span>
                      </div>
                    </template>
                    <template v-else-if="!vh.isDeposit">
                      <div class="cur-cell neg">
                        <img v-if="getCurrencyFlagImg(vh.currencyCode)" :src="getCurrencyFlagImg(vh.currencyCode)" class="cur-flag cur-flag--sm" />
                        <span>{{ fmt(Math.abs(vh.amount ?? 0)) }} {{ vh.currencyCode }}</span>
                      </div>
                    </template>
                    <span v-else class="text-muted">—</span>
                  </td>
                  <td class="mono text-muted balance-cell">
                    <template v-if="vh.isCombined">
                      <span>{{ fmt(vh.receivedBalance) }} {{ vh.receivedCurrencyCode }}</span>
                      <span>{{ fmt(vh.givenBalance) }} {{ vh.givenCurrencyCode }}</span>
                    </template>
                    <span v-else>{{ fmt(vh.runningBalance) }} {{ vh.currencyCode }}</span>
                  </td>
                  <td>
                    <span v-if="vh.user" class="user-tag">
                      <span class="material-symbols-outlined" aria-hidden="true">person</span>
                      {{ vh.user }}
                    </span>
                    <span v-else class="text-muted">-</span>
                  </td>
                  <td class="text-muted desc-cell">{{ vh.description ?? '-' }}</td>
                  <td v-if="authStore.isOwner">
                    <button class="void-btn" :disabled="voidingId === vh.id" title="Bu hareketi iptal et" @click.stop="voidVaultBalanceHistory(vh)">
                      <span class="material-symbols-outlined" aria-hidden="true">delete</span>
                    </button>
                  </td>
                </tr>
                <tr class="detail-row" v-if="expandedVaultRow === i">
                  <td :colspan="authStore.isOwner ? 9 : 8">
                    <div class="row-detail">
                      <div class="cd-item">
                        <span class="cd-label">Tam Açıklama</span>
                        <span class="cd-val">{{ vh.description ?? '—' }}</span>
                      </div>
                      <div class="cd-item" v-if="vh.valueInBaseCurrency">
                        <span class="cd-label">Değerleme (günlük ort. kur)</span>
                        <span class="cd-val">{{ fmt(vh.valueInBaseCurrency) }} ₺</span>
                      </div>
                      <div class="cd-item" v-if="vh.isGhost">
                        <span class="cd-label">Not</span>
                        <span class="cd-val">Ghost (gölge) hesap hareketi</span>
                      </div>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ── Cari Hesap Özeti (Kasa'dan ayrı, net etiketli alt-bölüm) ── -->
      <div class="zr-subsection-title" v-if="partyData">
        <span class="material-symbols-outlined" aria-hidden="true">group</span>
        <span>Cari Hesap Özeti</span>
      </div>
      <div class="panel" v-if="partyData">
        <div class="panel-hd">
          <span class="material-symbols-outlined" aria-hidden="true">group</span>
          <h3>Cari Hesap Özeti</h3>
          <button class="chart-toggle-btn" v-if="partyChartCurrencies.length" @click="showPartyChart = !showPartyChart" title="Grafik göster/gizle">
            <span class="material-symbols-outlined" aria-hidden="true">bar_chart</span>
          </button>
        </div>
        <div class="chart-panel-inline" v-if="showPartyChart && partyChartCurrencies.length">
          <ComparisonBarChart :labels="partyChartCurrencies" :datasets="[
            { label: 'Alacak', data: partyChartReceivables, color: '#10b981' },
            { label: 'Borç', data: partyChartDebts, color: '#ef4444' },
          ]" />
        </div>
        <div class="party-grid">
          <div class="party-card">
            <p class="pc-label">Toplam Cari</p>
            <p class="pc-val">{{ partyData.totalPartyAccounts ?? 0 }} <small>hesap</small></p>
          </div>
          <div class="party-card">
            <p class="pc-label">Aktif Cari</p>
            <p class="pc-val">{{ partyData.activePartyAccounts ?? 0 }} <small>hesap</small></p>
          </div>
          <div class="party-card alacak">
            <p class="pc-label">Alacaklarımız</p>
            <p class="pc-val pos">{{ fmt(partyData.totalReceivablesInTRY ?? 0) }} <small>₺</small></p>
          </div>
          <div class="party-card borc">
            <p class="pc-label">Borçlarımız</p>
            <p class="pc-val neg">{{ fmt(partyData.totalDebtsInTRY ?? 0) }} <small>₺</small></p>
          </div>
          <div class="party-card net">
            <p class="pc-label">Net Pozisyon</p>
            <p class="pc-val" :class="(partyData.netPositionInTRY ?? 0) >= 0 ? 'pos' : 'neg'">{{ fmt(partyData.netPositionInTRY ?? 0) }} <small>₺</small></p>
          </div>
          <div class="party-card">
            <p class="pc-label">Cari İşlem</p>
            <p class="pc-val">{{ partyData.partyTransactionCount ?? 0 }} <small>adet</small></p>
          </div>
        </div>
        <!-- Cari İşlem Hacmi -->
        <div class="party-volume" v-if="(partyData.partyTransactionVolume ?? 0) > 0">
          <span class="pv-label">Cari İşlem Hacmi</span>
          <span class="pv-val">{{ fmt(partyData.partyTransactionVolume) }} ₺</span>
        </div>
        <!-- Döviz bazlı alacak/borç -->
        <div class="party-currencies" v-if="partyData.totalReceivablesByCurrency || partyData.totalDebtsByCurrency">
          <table class="tbl tbl--compact">
            <thead>
              <tr><th>Döviz</th><th>Alacak</th><th>Borç</th></tr>
            </thead>
            <tbody>
              <tr v-for="code in [...new Set([...Object.keys(partyData.totalReceivablesByCurrency || {}), ...Object.keys(partyData.totalDebtsByCurrency || {})])]" :key="code">
                <td>
                  <div class="cur-cell">
                    <img v-if="getCurrencyFlagImg(code)" :src="getCurrencyFlagImg(code)" class="cur-flag cur-flag--sm" />
                    <span class="cur-code">{{ code }}</span>
                  </div>
                </td>
                <td class="pos">{{ fmt(partyData.totalReceivablesByCurrency?.[code] ?? 0) }}</td>
                <td class="neg">{{ fmt(partyData.totalDebtsByCurrency?.[code] ?? 0) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      </div>
      <!-- ══ ZONE 4: İşlem Geçmişi ══ -->
      <div class="zr-zone" style="--zone-color: var(--color-warning)">
        <div class="zr-zone-title">
          <span class="material-symbols-outlined" aria-hidden="true">receipt_long</span>
          <span>İşlem Geçmişi</span>
        </div>

      <!-- ── Personel Bazlı Kırılım — sadece tek şube görünümünde (Tüm Şubeler'de tamamen gizli) ── -->
      <div class="panel" v-if="selectedOfficeId && employeeRows.length">
        <div class="panel-hd">
          <span class="material-symbols-outlined" aria-hidden="true">badge</span>
          <h3>Personel Bazlı Kırılım</h3>
          <span class="badge">{{ employeeRows.length }} personel</span>
          <button class="chart-toggle-btn" @click="showEmployeeChart = !showEmployeeChart" title="Grafik göster/gizle">
            <span class="material-symbols-outlined" aria-hidden="true">bar_chart</span>
          </button>
        </div>
        <div class="chart-panel-inline" v-if="showEmployeeChart">
          <ComparisonBarChart :labels="employeeChartLabels" :datasets="[{ label: 'Kâr (₺)', data: employeeChartProfit, color: 'var(--color-warning)' }]" horizontal />
        </div>
        <div class="table-wrap">
          <table class="tbl">
            <thead>
              <tr>
                <th>Personel</th>
                <th>İşlem Sayısı</th>
                <th>Hacim (₺)</th>
                <th>Kâr (₺)</th>
                <th>Ort. İşlem (₺)</th>
                <th class="cur-chevron-col"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="e in employeeRows" :key="e.userId"
                  class="expandable-row" :class="{ expanded: expandedEmployeeRow === e.userId }"
                  @click="toggleEmployeeRow(e.userId)">
                <td class="fw-600">{{ e.employeeName }}</td>
                <td>{{ fmt(e.transactionCount, 0) }}</td>
                <td>{{ fmt(e.totalVolumeInTRY) }}</td>
                <td :class="(e.totalProfit ?? 0) >= 0 ? 'pos' : 'neg'" class="fw-600">{{ (e.totalProfit ?? 0) >= 0 ? '+' : '' }}{{ fmt(e.totalProfit) }}</td>
                <td>{{ fmt(e.averageTransactionSize) }}</td>
                <td class="cur-chevron-col">
                  <span class="material-symbols-outlined cur-chevron" aria-hidden="true">{{ expandedEmployeeRow === e.userId ? 'expand_less' : 'expand_more' }}</span>
                </td>
              </tr>
              <tr class="detail-row" v-for="e in employeeRows.filter(x => expandedEmployeeRow === x.userId)" :key="'det-' + e.userId">
                <td colspan="6">
                  <div class="row-detail">
                    <div class="cd-item">
                      <span class="cd-label">Döviz İşlemi Sayısı</span>
                      <span class="cd-val">{{ e.exchangeTransactionCount ?? 0 }} adet</span>
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ── İşlem Detayları ── -->
      <div class="panel" v-if="transactions.length">
        <div class="panel-hd">
          <span class="material-symbols-outlined" aria-hidden="true">receipt_long</span>
          <h3>İşlem Detayları</h3>
          <span class="badge">{{ transactions.length }} işlem</span>
        </div>
        <div class="table-wrap">
          <table class="tbl">
            <thead>
              <tr>
                <th>İşlem No</th>
                <th>Kasa</th>
                <th>Tür</th>
                <th>Tarih</th>
                <th>Döviz</th>
                <th>Miktar</th>
                <th>Kur</th>
                <th>TRY Karşılığı</th>
                <th>Kar (₺)</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              <template v-for="(tx, i) in transactions" :key="tx.id ?? tx.transactionNumber">
                <tr class="expandable-row" :class="{ expanded: expandedTxRow === i }" @click="toggleTxRow(i)">
                  <td class="mono">{{ tx.transactionNumber }}</td>
                  <td>{{ tx.vaultName ?? '-' }}</td>
                  <td>
                    <span class="type-chip" :class="getTxTypeChipClass(tx)">
                      {{ getTxTypeLabel(tx) }}
                    </span>
                  </td>
                  <td class="no-wrap">{{ fmtDateTime(tx.transactionDate) }}</td>
                  <td>
                    <div class="cur-cell">
                      <img v-if="getCurrencyFlagImg(tx.currencyCode)" :src="getCurrencyFlagImg(tx.currencyCode)" class="cur-flag cur-flag--sm" />
                      <span class="cur-code">{{ tx.currencyCode ?? '-' }}</span>
                    </div>
                  </td>
                  <td>{{ fmt(tx.amount) }}</td>
                  <td class="mono">{{ fmt(tx.rate ?? tx.exchangeRate, 4) }}</td>
                  <td>{{ fmt(tx.tryAmount ?? tx.totalTry) }}</td>
                  <td :class="(tx.profit ?? 0) >= 0 ? 'pos' : 'neg'">
                    {{ (tx.profit ?? 0) >= 0 ? '+' : '' }}{{ fmt(tx.profit ?? 0) }}
                  </td>
                  <td>
                    <span class="status-chip" :class="tx.status === 2 || tx.statusName === 'Tamamlandı' ? 'done' : 'pend'">
                      {{ tx.statusName ?? (tx.status === 2 ? 'Tamamlandı' : 'Bekliyor') }}
                    </span>
                  </td>
                </tr>
                <tr class="detail-row" v-if="expandedTxRow === i">
                  <td colspan="10">
                    <div class="row-detail cd-grid">
                      <div class="cd-item" v-if="tx.commission">
                        <span class="cd-label">Komisyon</span>
                        <span class="cd-val mono">{{ fmt(tx.commission) }}</span>
                      </div>
                      <div class="cd-item" v-if="tx.netAmount">
                        <span class="cd-label">Net Tutar</span>
                        <span class="cd-val mono">{{ fmt(tx.netAmount) }}</span>
                      </div>
                      <div class="cd-item" v-if="tx.actualBuyRate">
                        <span class="cd-label">Gerçek Alış Kuru</span>
                        <span class="cd-val mono">{{ fmt(tx.actualBuyRate, 4) }}</span>
                      </div>
                      <div class="cd-item" v-if="tx.actualSellRate">
                        <span class="cd-label">Gerçek Satış Kuru</span>
                        <span class="cd-val mono">{{ fmt(tx.actualSellRate, 4) }}</span>
                      </div>
                      <div class="cd-item" v-if="tx.customRate">
                        <span class="cd-label">Özel Kur</span>
                        <span class="cd-val mono">{{ fmt(tx.customRate, 4) }}</span>
                      </div>
                      <div class="cd-item" v-if="tx.notes">
                        <span class="cd-label">Not</span>
                        <span class="cd-val">{{ tx.notes }}</span>
                      </div>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>

      </div>
      <!-- ══ /ZONE 4 ══ -->

      <!-- ── Boş ── -->
      <div class="zr-empty" v-if="!currencyRows.length && !transactions.length && !partyData && !cashData">
        <span class="material-symbols-outlined" aria-hidden="true">inbox</span>
        <p>Seçilen dönem için işlem bulunamadı.</p>
      </div>

    </template>

    <!-- ── Başlangıç ── -->
    <div class="zr-empty" v-if="!isLoading && !hasData && !error">
      <span class="material-symbols-outlined" aria-hidden="true">assessment</span>
      <p>Filtre seçip <strong>Raporu Getir</strong> butonuna tıklayın.</p>
    </div>

  </div>
</template>

<style scoped>
/* ── Foundation ── */
.zr { padding: 24px; display: flex; flex-direction: column; gap: 18px; }

/* ── Filters ── */
.zr-filters { background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-lg); padding: 16px 20px; box-shadow: var(--shadow-md); }
.filter-row { display: flex; flex-wrap: wrap; gap: 14px; align-items: flex-end; }
.filter-group { display: flex; flex-direction: column; gap: 5px; }
.filter-group label { font-size: 11px; font-weight: 600; color: #6b7280; text-transform: uppercase; letter-spacing: .04em; }
.filter-group--actions { margin-left: auto; }
.btn-row { display: flex; gap: 8px; }
.zr-input { height: 36px; padding: 0 12px; border: 1px solid #d1d5db; border-radius: var(--radius-md); font-size: 13px; color: #111; background: #f9fafb; outline: none; transition: border .15s; }
.zr-input:focus { border-color: var(--color-primary); box-shadow: 0 0 0 3px rgba(99,102,241,.12); }
.zr-input-sm { width: 90px; }
.zr-office-fixed { display: flex; align-items: center; font-weight: 600; color: var(--color-primary-hover); background: var(--color-primary-light); border-color: #c7d2fe; cursor: default; }
.mode-tabs { display: flex; border: 1px solid var(--color-border); border-radius: var(--radius-md); overflow: hidden; }
.mt-btn { padding: 7px 14px; font-size: 12px; font-weight: 500; background: #f9fafb; border: none; cursor: pointer; color: #6b7280; transition: background-color 0.2s, color 0.2s; }
.mt-btn.active { background: var(--color-primary); color: #fff; }
.btn { display: flex; align-items: center; gap: 6px; height: 36px; padding: 0 16px; border: none; border-radius: var(--radius-md); font-size: 13px; font-weight: 600; cursor: pointer; transition: background-color 0.2s, color 0.2s; }
.btn--primary { background: var(--color-primary); color: #fff; }
.btn--primary:hover:not(:disabled) { background: var(--color-primary-hover); }
.btn--primary:disabled { opacity: .6; cursor: not-allowed; }
.btn--ghost { background: #f3f4f6; color: #374151; padding: 0 10px; }
.btn--ghost:hover { background: #e5e7eb; }
.btn .material-symbols-outlined { font-size: 17px; }

/* ── Error / Loading ── */
.zr-error { background: #fef2f2; border: 1px solid #fecaca; border-radius: var(--radius-md); padding: 12px 16px; color: var(--color-danger); display: flex; align-items: center; gap: 8px; font-size: 13px; }
.zr-loading { display: flex; align-items: center; justify-content: center; gap: 12px; padding: 60px; color: #6b7280; font-size: 14px; }
.spinner { width: 26px; height: 26px; border: 3px solid #e5e7eb; border-top-color: var(--color-primary); border-radius: 50%; animation: spin .7s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

/* ── Header ── */
.zr-header { display: flex; align-items: flex-start; justify-content: space-between; background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-lg); padding: 16px 20px; box-shadow: var(--shadow-glow-primary); }
.zr-header-actions { display: flex; align-items: center; gap: 10px; flex-shrink: 0; }
.zr-title { margin: 0; font-size: 18px; font-weight: 700; color: #111; }
.zr-subtitle { margin: 4px 0 0; color: #6b7280; font-size: 13px; display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.zr-timestamp { margin: 0; color: #9ca3af; font-size: 12px; display: none; }
.vault-badge { display: inline-block; padding: 2px 8px; border-radius: var(--radius-sm); font-size: 11px; font-weight: 600; }
.vault-badge.open { background: var(--color-success-bg); color: #065f46; }
.vault-badge.closed { background: var(--color-danger-bg); color: #991b1b; }

/* ── KPI ── */
.kpi-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(175px, 1fr)); gap: 12px; }
.kpi-slot.clickable { cursor: pointer; }

/* ── Trend & Volume Chart Panels (akıllı kartlar) ── */
.trend-panel, .volume-chart-panel { padding: 14px 18px; }
.trend-loading, .trend-empty { padding: 40px; text-align: center; color: #9ca3af; font-size: 13px; }
.volume-chips.clickable { cursor: pointer; }
.vc-chevron { margin-left: auto; font-size: 18px; color: #9ca3af; }
.chart-toggle-btn { display: inline-flex; align-items: center; justify-content: center; width: 26px; height: 26px; border: none; background: #f3f4f6; border-radius: var(--radius-sm); cursor: pointer; color: #6b7280; transition: color .15s, background-color .15s; margin-left: 8px; }
.chart-toggle-btn:hover { background: var(--color-primary-light); color: var(--color-primary); }
.chart-toggle-btn .material-symbols-outlined { font-size: 15px; }
.chart-panel-inline { padding: 12px 18px; border-bottom: 1px solid #f3f4f6; }

/* ── Expandable Table Rows (genel açılır satır deseni) ── */
.expandable-row { cursor: pointer; transition: background .1s; }
.expandable-row:hover td { background: #f5f3ff !important; }
.expandable-row.expanded td { background: #ede9fe; }
.detail-row td { padding: 0; border-bottom: 1px solid #f3f4f6; }
.row-detail { padding: 12px 18px; background: #faf5ff; border-top: 2px solid var(--border-strong); display: flex; flex-wrap: wrap; gap: 16px; }
.btn--sm { height: 28px; padding: 0 10px; font-size: 12px; }
.btn--sm .material-symbols-outlined { font-size: 15px; }

/* ── Breakdown Row ── */
.breakdown-row { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; }

/* ── TX Breakdown ── */
.tx-breakdown { display: flex; gap: 10px; flex-wrap: wrap; }
.tx-chip { display: flex; align-items: center; gap: 6px; padding: 6px 12px; background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-md); font-size: 12px; box-shadow: var(--shadow-sm); }
.tx-chip .material-symbols-outlined { font-size: 16px; color: var(--tc); font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.tx-count { font-weight: 700; color: #111; }
.tx-label { color: #6b7280; }

/* ── Opening Balance ── */
.opening-balance { display: flex; align-items: center; gap: 8px; background: #fffbeb; border: 1px solid #fde68a; border-left: 6px solid var(--color-warning); border-radius: var(--radius-md); padding: 6px 14px; margin-left: auto; box-shadow: var(--shadow-sm); }
.opening-balance .material-symbols-outlined { font-size: 18px; color: var(--color-warning); font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.ob-label { font-size: 10px; color: #92400e; margin: 0; text-transform: uppercase; letter-spacing: .03em; }
.ob-val { font-size: 14px; font-weight: 700; color: #92400e; margin: 0; }

/* ── Volume Chips ── */
.volume-chips { display: flex; align-items: center; gap: 12px; background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: 10px 16px; flex-wrap: wrap; box-shadow: var(--shadow-sm); }
.vc-title { display: flex; align-items: center; gap: 6px; font-size: 12px; font-weight: 600; color: #374151; white-space: nowrap; }
.vc-title .material-symbols-outlined { font-size: 16px; color: var(--color-primary); font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.vc-list { display: flex; gap: 8px; flex-wrap: wrap; }
.vc-item { display: flex; align-items: center; gap: 8px; background: #f9fafb; border-radius: var(--radius-sm); padding: 4px 10px; }
.vc-amount { font-weight: 600; font-size: 12px; color: #111; font-family: 'JetBrains Mono', 'Cascadia Code', monospace; }

/* ── Vault Ops Bar ── */
.vault-ops { display: flex; align-items: center; gap: 16px; background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: 14px 20px; flex-wrap: wrap; box-shadow: var(--shadow-md); }
.vo-item { display: flex; align-items: center; gap: 10px; }
.vo-item .material-symbols-outlined { font-size: 22px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.vo-label { font-size: 11px; color: #6b7280; margin: 0; }
.vo-val { font-size: 15px; font-weight: 700; margin: 0; color: #111; }
.vo-divider { width: 1px; height: 32px; background: #e5e7eb; }

/* ── Panels ── */
.panel { background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow: hidden; box-shadow: var(--shadow-bold); }
.panel-hd { display: flex; align-items: center; gap: 8px; padding: 14px 18px; border-bottom: 1px solid #f3f4f6; }
.panel-hd h3 { margin: 0; font-size: 14px; font-weight: 600; color: #111; flex: 1; }
.panel-hd .material-symbols-outlined { font-size: 19px; color: var(--color-primary); font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

/* ── Zone (bölge) yapısı — sayfayı 4 görsel olarak ayrışan bölgeye grupluyor ── */
.zr-zone {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 14px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: rgba(0,0,0,.012);
}
.zr-zone-title {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 4px 10px 14px;
  border-left: 4px solid var(--zone-color, var(--color-primary));
  font-size: 16px;
  font-weight: 800;
  color: var(--color-text, #1e293b);
  break-after: avoid;
  print-color-adjust: exact;
  -webkit-print-color-adjust: exact;
}
.zr-zone-title .material-symbols-outlined {
  font-size: 22px;
  color: var(--zone-color, var(--color-primary));
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
/* ── Alt-bölüm başlığı (zone içi, panel'den daha üst bir etiket — ör. Kasa ve Cari içinde Cari Hesap) ── */
.zr-subsection-title {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 0;
  font-size: 13px;
  font-weight: 700;
  color: var(--color-text-secondary, #64748b);
  text-transform: uppercase;
  letter-spacing: .03em;
  break-after: avoid;
}
.zr-subsection-title .material-symbols-outlined {
  font-size: 16px;
  color: var(--color-success);
}

/* ── Table ── */
.table-wrap { overflow-x: auto; }
.tbl { width: 100%; border-collapse: collapse; font-size: 12px; }
.tbl th { background: #f9fafb; padding: 9px 14px; text-align: left; font-weight: 700; color: #374151; white-space: nowrap; border-bottom: 2px solid var(--border-strong); font-size: 11px; text-transform: uppercase; letter-spacing: .03em; }
.tbl td { padding: 9px 14px; border-bottom: 1px solid #f3f4f6; color: #1f2937; }
.tbl tr:last-child td { border-bottom: none; }
.tbl tr:hover td { background: #fafafa; }
.tbl tfoot td { background: #f9fafb; border-top: 2px solid var(--border-strong); font-size: 13px; font-weight: 700; }
.tbl--compact { font-size: 12px; }
.tbl--compact td, .tbl--compact th { padding: 7px 14px; }

/* ── Currency Cell ── */
.cur-cell { display: flex; align-items: center; gap: 6px; }
.balance-cell { display: flex; flex-direction: column; gap: 2px; font-size: 12px; white-space: nowrap; }
.cur-flag { width: 22px; height: 16px; object-fit: cover; border-radius: 2px; border: 1px solid rgba(0,0,0,.08); }
.cur-flag--sm { width: 18px; height: 13px; }
.cur-code { font-weight: 600; color: var(--color-primary); font-size: 12px; }
.cur-name { color: #9ca3af; font-size: 11px; }
.cur-row { cursor: pointer; transition: background .1s; }
.cur-row:hover td { background: #f5f3ff !important; }
.cur-row.expanded td { background: #ede9fe; }

/* ── Currency Detail ── */
.cur-detail { display: grid; grid-template-rows: 0fr; transition: grid-template-rows .25s ease; }
.cur-detail.open { grid-template-rows: 1fr; }
.cur-detail-clip { overflow: hidden; min-height: 0; }
.cur-detail-inner { padding: 14px 18px; background: #faf5ff; border-top: 2px solid var(--border-strong); }
.cd-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 12px; }
.cd-item { display: flex; flex-direction: column; gap: 2px; }
.cd-label { font-size: 10px; color: #6b7280; text-transform: uppercase; letter-spacing: .04em; }
.cd-val { font-size: 14px; font-weight: 600; color: #111; }

/* ── Party Cards ── */
.party-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(130px, 1fr)); gap: 10px; padding: 14px 18px; }
.party-card { background: #f9fafb; border-radius: var(--radius-md); padding: 10px 12px; box-shadow: var(--shadow-sm); }
.pc-label { font-size: 10px; color: #6b7280; margin: 0 0 4px; text-transform: uppercase; letter-spacing: .04em; }
.pc-val { font-size: 16px; font-weight: 700; margin: 0; color: #111; }
.pc-val small { font-size: 11px; font-weight: 400; color: #9ca3af; }
.party-volume { display: flex; justify-content: space-between; align-items: center; padding: 8px 18px; background: #f0fdf4; border-top: 2px solid #dcfce7; }
.pv-label { font-size: 11px; color: #166534; font-weight: 500; }
.pv-val { font-size: 13px; font-weight: 700; color: #166534; }
.party-currencies { border-top: 1px solid #f3f4f6; padding: 0; }

/* ── Vault Balances ── */
.vault-balances { display: flex; flex-wrap: wrap; gap: 8px; padding: 14px 18px; }
.vb-item { display: flex; align-items: center; justify-content: space-between; gap: 12px; background: #f9fafb; border-radius: var(--radius-md); padding: 8px 12px; min-width: 140px; flex: 1; box-shadow: var(--shadow-sm); }
.vb-amount { font-weight: 700; font-size: 14px; color: #111; }
.cash-summary { border-top: 1px solid #f3f4f6; padding: 12px 18px; display: flex; flex-direction: column; gap: 6px; }
.cs-row { display: flex; justify-content: space-between; font-size: 13px; color: #374151; }

/* ── Cash Volumes ── */
.cash-volumes { border-top: 1px solid #f3f4f6; padding: 10px 18px; }
.cv-title { font-size: 10px; color: #6b7280; text-transform: uppercase; letter-spacing: .04em; margin-bottom: 6px; font-weight: 600; }
.cv-list { display: flex; flex-wrap: wrap; gap: 6px; }
.cv-item { display: flex; align-items: center; gap: 6px; background: #f9fafb; border-radius: var(--radius-sm); padding: 4px 8px; font-size: 12px; font-weight: 500; color: #374151; }

/* ── Type Tags & Chips ── */
.type-tag { display: inline-block; padding: 2px 8px; border-radius: var(--radius-sm); font-size: 10px; font-weight: 600; background: color-mix(in srgb, var(--tag-color) 12%, transparent); color: var(--tag-color); }
.void-btn {
  display: inline-flex; align-items: center; justify-content: center;
  width: 26px; height: 26px; border: none; background: none; cursor: pointer;
  color: var(--color-text-muted, #9ca3af); border-radius: var(--radius-sm); transition: color .15s, background-color .15s;
}
.void-btn:hover:not(:disabled) { color: var(--color-danger, #ef4444); background: #fef2f2; }
.void-btn:disabled { opacity: .4; cursor: default; }
.void-btn .material-symbols-outlined { font-size: 16px; }
.type-chip { display: inline-block; padding: 2px 8px; border-radius: var(--radius-sm); font-size: 10px; font-weight: 600; }
.chip-exchange { background: #ede9fe; color: #6d28d9; }
.chip-deposit { background: var(--color-success-bg); color: #065f46; }
.chip-withdrawal { background: var(--color-danger-bg); color: #991b1b; }
.chip-transfer { background: #dbeafe; color: #1e40af; }
.chip-party { background: #fce7f3; color: #9d174d; }
.chip-default { background: #f3f4f6; color: #374151; }

/* ── User Tag ── */
.user-tag { display: inline-flex; align-items: center; gap: 3px; font-size: 11px; color: #6b7280; }
.user-tag .material-symbols-outlined { font-size: 13px; }

/* ── Party Indicator ── */
.party-indicator { display: inline-flex; align-items: center; margin-left: 4px; }
.party-indicator .material-symbols-outlined { font-size: 13px; color: #ec4899; }

/* ── Progress Bar ── */
.bar-wrap { position: relative; width: 80px; height: 20px; background: #f3f4f6; border-radius: var(--radius-sm); overflow: hidden; }
.bar-fill { position: absolute; left: 0; top: 0; height: 100%; background: var(--color-primary); border-radius: var(--radius-sm); opacity: .2; }
.bar-text { position: relative; z-index: 1; font-size: 11px; font-weight: 600; color: #374151; display: flex; align-items: center; justify-content: center; height: 100%; }

/* ── Status ── */
.status-chip { display: inline-block; padding: 2px 8px; border-radius: var(--radius-sm); font-size: 10px; font-weight: 600; }
.status-chip.done { background: var(--color-success-bg); color: #065f46; }
.status-chip.pend { background: #fef3c7; color: #92400e; }
.status-dot { display: inline-block; width: 7px; height: 7px; border-radius: 50%; margin-right: 4px; }
.status-dot.on { background: var(--color-success); }
.status-dot.off { background: #d1d5db; }

/* ── Badge ── */
.badge { display: inline-block; padding: 2px 8px; border-radius: var(--radius-sm); background: #f3f4f6; color: #374151; font-size: 11px; font-weight: 600; margin-left: auto; }

/* ── Common ── */
.pos { color: var(--color-success); font-weight: 600; }
.neg { color: var(--color-danger); font-weight: 600; }
.fw-600 { font-weight: 600; }
.mono { font-family: 'JetBrains Mono', 'Cascadia Code', monospace; font-size: 12px; }
.text-muted { color: #9ca3af; }
.no-wrap { white-space: nowrap; }
.desc-cell { max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

/* ── Empty ── */
.zr-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 60px; gap: 10px; color: #9ca3af; }
.zr-empty .material-symbols-outlined { font-size: 44px; }
.zr-empty p { font-size: 14px; margin: 0; }

/* ── Print ── */
@media print {
  .zr-filters, .btn-row, .btn--ghost, .no-print { display: none !important; }
  .zr-timestamp { display: block !important; }
  .zr { padding: 0; gap: 12px; }
  .panel { break-inside: avoid; }
  .cur-row:hover td { background: transparent !important; }
  .volume-chips { border: none; padding: 6px 0; }
  .vault-ops { border: none; padding: 8px 0; }
  /* Zone sarmalayıcısına break-inside: avoid UYGULANMAZ — bir zone birden fazla
     sayfaya yayılabilir. Sadece zone başlığının içerikten öksüz kalması engellenir
     (zaten .zr-zone-title kuralında break-after: avoid var). */
  .zr-zone { border: none; padding: 8px 0; background: none; }
}

/* ── Responsive ── */
@media (max-width: 768px) {
  .zr { padding: 14px; gap: 14px; }
  .filter-row { gap: 10px; }
  .kpi-grid { grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 8px; }
  .tbl { font-size: 11px; }
  .tbl th, .tbl td { padding: 7px 10px; }
  .breakdown-row { flex-direction: column; align-items: flex-start; }
  .opening-balance { margin-left: 0; }
}
.wac-cell { color: #7c3aed; font-weight: 500; }
.wac-val { color: #7c3aed; font-weight: 600; }

/* ── Döviz Tablosu Genişletme Chevron'u ── */
.cur-chevron-col { width: 28px; text-align: center; }
.cur-chevron { font-size: 18px; color: #9ca3af; transition: color .15s; }
.cur-row.expanded .cur-chevron { color: var(--color-primary); }
</style>
