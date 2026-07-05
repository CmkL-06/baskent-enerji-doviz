<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'
import { getCurrencyFlagImg } from '@/utils/currency'
import AppKpiCard from '@/components/common/AppKpiCard.vue'

const authStore = useAuthStore()

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
const expandedCurrency = ref<string | null>(null)

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
  0: '#6366f1',
  1: '#8b5cf6',
  2: '#10b981',
  3: '#ef4444',
  4: '#3b82f6',
  5: '#f59e0b',
  6: '#ec4899',
}

const s = computed(() => reportData.value?.summary ?? {})
const hasData = computed(() => !!reportData.value)
const isMultiOffice = computed(() => !selectedOfficeId.value && (reportData.value?.officeBreakdown?.length ?? 0) > 0)

const kpis = computed(() => {
  if (!s.value) return []
  const items = [
    { icon: 'trending_up', label: 'Toplam Kar', value: fmt(s.value.totalProfit ?? s.value.totalProfitInTRY ?? 0), unit: '₺', color: '#10b981', bg: 'rgba(16,185,129,0.10)' },
    { icon: 'swap_horiz', label: 'İşlem Sayısı', value: fmt(s.value.totalTransactions ?? 0, 0), unit: 'adet', color: '#6366f1', bg: 'rgba(99,102,241,0.10)' },
    { icon: 'monitoring', label: 'İşlem Hacmi', value: fmt(s.value.totalForeignCurrencyProcessed ?? 0), unit: '₺', color: '#0ea5e9', bg: 'rgba(14,165,233,0.10)' },
    { icon: 'percent', label: 'Kar Marjı', value: fmt(s.value.profitMargin ?? 0, 1), unit: '%', color: '#f59e0b', bg: 'rgba(245,158,11,0.10)' },
    { icon: 'straighten', label: 'Ort. İşlem', value: fmt(s.value.averageTransactionSize ?? 0), unit: '₺', color: '#8b5cf6', bg: 'rgba(139,92,246,0.10)' },
    { icon: 'account_balance', label: 'Kasa Değeri', value: fmt(s.value.totalValueInBaseCurrency ?? 0), unit: '₺', color: '#3b82f6', bg: 'rgba(59,130,246,0.10)' },
  ]
  return items
})

const txBreakdown = computed(() => {
  const st = s.value
  if (!st) return []
  return [
    { label: 'Döviz İşlemi', count: st.totalExchangeTransactions ?? 0, icon: 'currency_exchange', color: '#6366f1' },
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
const totalUnrealized = computed(() => currencyRows.value.reduce((sum: number, r: any) => sum + (r.unrealizedProfit ?? 0), 0))
const totalRealized = computed(() => currencyRows.value.reduce((sum: number, r: any) => sum + (r.realizedProfit ?? 0), 0))

const vaultRows = computed(() => {
  const histories = reportData.value?.vaultBalanceHistories ?? []
  return histories.map((h: any) => ({
    ...h,
    amount: Math.abs(h.balance ?? 0),
    isDeposit: (h.balance ?? 0) >= 0,
    typeLabel: txTypeLabels[h.transactionType] ?? 'Bilinmeyen',
    typeColor: txTypeColors[h.transactionType] ?? '#6b7280',
  }))
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

function toggleCurrency(code: string) {
  expandedCurrency.value = expandedCurrency.value === code ? null : code
}

async function loadOffices() {
  try {
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
  } catch (e: any) {
    error.value = e?.response?.data?.message ?? e?.message ?? 'Rapor yüklenemedi'
  } finally {
    isLoading.value = false
  }
}

function printReport() { window.print() }

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

        <div class="filter-group" v-if="offices.length > 0">
          <label>Şube</label>
          <select v-model="selectedOfficeId" class="zr-input">
            <option value="">Tüm Şubeler</option>
            <option v-for="o in offices" :key="o.id" :value="o.id">{{ o.name }}</option>
          </select>
        </div>

        <div class="filter-group filter-group--actions">
          <label>&nbsp;</label>
          <div class="btn-row">
            <button class="btn btn--primary" @click="fetchReport" :disabled="isLoading">
              <span class="material-symbols-outlined">{{ isLoading ? 'hourglass_top' : 'search' }}</span>
              {{ isLoading ? 'Yükleniyor...' : 'Raporu Getir' }}
            </button>
            <button class="btn btn--ghost" @click="printReport" v-if="hasData" title="Yazdır">
              <span class="material-symbols-outlined">print</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Hata ── -->
    <div class="zr-error" v-if="error">
      <span class="material-symbols-outlined">error</span> {{ error }}
    </div>

    <!-- ── Loading ── -->
    <div class="zr-loading" v-if="isLoading">
      <div class="spinner"></div>
      <span>Rapor hazırlanıyor...</span>
    </div>

    <!-- ── Rapor İçeriği ── -->
    <template v-if="!isLoading && hasData">

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
        <p class="zr-timestamp print-only">Oluşturulma: {{ new Date().toLocaleString('tr-TR') }}</p>
      </div>

      <!-- ── KPI Kartları ── -->
      <div class="kpi-grid">
        <AppKpiCard v-for="k in kpis" :key="k.label" :icon="k.icon" :label="k.label" :value="k.value" :unit="k.unit" :color="k.color" :bg="k.bg" />
      </div>

      <!-- ── İşlem Dağılımı + Devir Bakiye ── -->
      <div class="breakdown-row">
        <div class="tx-breakdown" v-if="txBreakdown.some(t => t.count > 0)">
          <div v-for="t in txBreakdown" :key="t.label" class="tx-chip" :style="{ '--tc': t.color }">
            <span class="material-symbols-outlined">{{ t.icon }}</span>
            <span class="tx-count">{{ t.count }}</span>
            <span class="tx-label">{{ t.label }}</span>
          </div>
        </div>
        <div class="opening-balance" v-if="s.hasInheritedBalance">
          <span class="material-symbols-outlined">history</span>
          <div>
            <p class="ob-label">Devir Bakiye</p>
            <p class="ob-val">{{ fmt(s.openingBalanceTRY) }} ₺</p>
          </div>
        </div>
      </div>

      <!-- ── Döviz Bazlı Hacim ── -->
      <div class="volume-chips" v-if="volumesByCurrency.length">
        <span class="vc-title">
          <span class="material-symbols-outlined">bar_chart</span>
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
      </div>

      <!-- ── Kasa Giriş/Çıkış Özeti ── -->
      <div class="vault-ops" v-if="(s.vaultDeposits ?? 0) > 0 || (s.vaultWithdrawals ?? 0) > 0">
        <div class="vo-item">
          <span class="material-symbols-outlined" style="color: #10b981">arrow_downward</span>
          <div>
            <p class="vo-label">Kasa Giriş</p>
            <p class="vo-val">{{ fmt(s.vaultDeposits) }} ₺</p>
          </div>
        </div>
        <div class="vo-divider"></div>
        <div class="vo-item">
          <span class="material-symbols-outlined" style="color: #ef4444">arrow_upward</span>
          <div>
            <p class="vo-label">Kasa Çıkış</p>
            <p class="vo-val">{{ fmt(s.vaultWithdrawals) }} ₺</p>
          </div>
        </div>
        <div class="vo-divider"></div>
        <div class="vo-item">
          <span class="material-symbols-outlined" style="color: #3b82f6">sync_alt</span>
          <div>
            <p class="vo-label">Net Hareket</p>
            <p class="vo-val" :class="(s.netVaultChange ?? 0) >= 0 ? 'pos' : 'neg'">{{ (s.netVaultChange ?? 0) >= 0 ? '+' : '' }}{{ fmt(s.netVaultChange) }} ₺</p>
          </div>
        </div>
        <div class="vo-divider"></div>
        <div class="vo-item">
          <span class="material-symbols-outlined" style="color: #6366f1">balance</span>
          <div>
            <p class="vo-label">Kasa Sonrası Kar</p>
            <p class="vo-val" :class="(s.profitAfterVaultOperations ?? 0) >= 0 ? 'pos' : 'neg'">{{ fmt(s.profitAfterVaultOperations) }} ₺</p>
          </div>
        </div>
      </div>

      <!-- ── Şube Karşılaştırma ── -->
      <div class="panel" v-if="isMultiOffice">
        <div class="panel-hd">
          <span class="material-symbols-outlined">store</span>
          <h3>Şube Karşılaştırması</h3>
          <span class="badge">{{ officeRows.length }} şube</span>
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
              <tr v-for="o in officeRows" :key="o.officeId">
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
            </tbody>
          </table>
        </div>
      </div>

      <!-- ── Döviz Bazlı Özet ── -->
      <div class="panel" v-if="currencyRows.length">
        <div class="panel-hd">
          <span class="material-symbols-outlined">currency_exchange</span>
          <h3>Döviz Bazlı Özet</h3>
          <span class="badge">{{ currencyRows.length }} döviz</span>
        </div>
        <div class="table-wrap">
          <table class="tbl">
            <thead>
              <tr>
                <th>Döviz</th>
                <th>Alış Miktarı</th>
                <th>Alış (₺)</th>
                <th>Ort. Alış Kuru</th>
                <th>Satış Miktarı</th>
                <th>Satış (₺)</th>
                <th>Ort. Satış Kuru</th>
                <th>WAC</th>
                <th>Net Pozisyon</th>
                <th>Kar (₺)</th>
                <th>G.leşen K/Z</th>
                <th>G.leşmemiş K/Z</th>
                <th>Marj</th>
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
                <td>{{ fmt(row.totalBuyCost) }}</td>
                <td class="mono">{{ fmt(row.averageBuyRate, 4) }}</td>
                <td>{{ fmt(row.totalSoldAmount) }} <small class="text-muted">({{ row.sellTransactionCount ?? 0 }})</small></td>
                <td>{{ fmt(row.totalSellRevenue) }}</td>
                <td class="mono">{{ fmt(row.averageSellRate, 4) }}</td>
                <td class="mono wac-cell">{{ row.wac ? fmt(row.wac, 4) : '—' }}</td>
                <td :class="(row.netPosition ?? 0) >= 0 ? 'pos' : 'neg'">{{ fmt(row.netPosition) }}</td>
                <td :class="(row.profit ?? 0) >= 0 ? 'pos' : 'neg'" class="fw-600">{{ (row.profit ?? 0) >= 0 ? '+' : '' }}{{ fmt(row.profit) }}</td>
                <td :class="(row.realizedProfit ?? 0) >= 0 ? 'pos' : 'neg'">{{ row.realizedProfit != null ? ((row.realizedProfit >= 0 ? '+' : '') + fmt(row.realizedProfit)) : '—' }}</td>
                <td :class="(row.unrealizedProfit ?? 0) >= 0 ? 'pos' : 'neg'">{{ row.unrealizedProfit ? ((row.unrealizedProfit >= 0 ? '+' : '') + fmt(row.unrealizedProfit)) : '—' }}</td>
                <td>%{{ fmt(row.profitMargin ?? 0, 1) }}</td>
              </tr>
            </tbody>
            <tfoot v-if="currencyRows.length > 1">
              <tr>
                <td class="fw-600">TOPLAM</td>
                <td colspan="8"></td>
                <td :class="(s.totalProfit ?? 0) >= 0 ? 'pos' : 'neg'" class="fw-600">{{ fmt(s.totalProfit ?? s.totalProfitInTRY) }}</td>
                <td :class="totalRealized >= 0 ? 'pos' : 'neg'">{{ totalRealized ? ((totalRealized >= 0 ? '+' : '') + fmt(totalRealized)) : '—' }}</td>
                <td :class="totalUnrealized >= 0 ? 'pos' : 'neg'">{{ totalUnrealized ? ((totalUnrealized >= 0 ? '+' : '') + fmt(totalUnrealized)) : '—' }}</td>
                <td>%{{ fmt(s.profitMargin ?? 0, 1) }}</td>
              </tr>
            </tfoot>
          </table>
        </div>
        <!-- Genişletilmiş Döviz Detay -->
        <div class="cur-detail" v-if="expandedCurrency">
          <template v-for="row in currencyRows" :key="'det-' + row.currencyCode">
            <div v-if="row.currencyCode === expandedCurrency" class="cur-detail-inner">
              <div class="cd-grid">
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
                <div class="cd-item">
                  <span class="cd-label">Alış İşlem</span>
                  <span class="cd-val">{{ row.buyTransactionCount ?? 0 }} adet</span>
                </div>
                <div class="cd-item">
                  <span class="cd-label">Satış İşlem</span>
                  <span class="cd-val">{{ row.sellTransactionCount ?? 0 }} adet</span>
                </div>
                <div class="cd-item">
                  <span class="cd-label">Net Pozisyon</span>
                  <span class="cd-val" :class="(row.netPosition ?? 0) >= 0 ? 'pos' : 'neg'">{{ fmt(row.netPosition) }}</span>
                </div>
                <div class="cd-item" v-if="row.wac">
                  <span class="cd-label">WAC</span>
                  <span class="cd-val mono wac-val">{{ fmt(row.wac, 4) }}</span>
                </div>
                <div class="cd-item" v-if="row.currentBalance">
                  <span class="cd-label">Mevcut Bakiye</span>
                  <span class="cd-val">{{ fmt(row.currentBalance) }}</span>
                </div>
                <div class="cd-item" v-if="row.realizedProfit !== undefined">
                  <span class="cd-label">Gerçekleşen K/Z</span>
                  <span class="cd-val" :class="(row.realizedProfit ?? 0) >= 0 ? 'pos' : 'neg'">{{ fmt(row.realizedProfit) }} ₺</span>
                </div>
                <div class="cd-item" v-if="row.unrealizedProfit">
                  <span class="cd-label">Gerçekleşmemiş K/Z</span>
                  <span class="cd-val" :class="(row.unrealizedProfit ?? 0) >= 0 ? 'pos' : 'neg'">{{ fmt(row.unrealizedProfit) }} ₺</span>
                </div>
              </div>
            </div>
          </template>
        </div>
      </div>

      <!-- ── İki Kolon: Cari Hesap + Kasa Bakiye ── -->
      <div class="two-col">

        <!-- Cari Hesap Özeti -->
        <div class="panel" v-if="partyData">
          <div class="panel-hd">
            <span class="material-symbols-outlined">group</span>
            <h3>Cari Hesap Özeti</h3>
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

        <!-- Kasa Bakiyeleri -->
        <div class="panel" v-if="cashData">
          <div class="panel-hd">
            <span class="material-symbols-outlined">account_balance_wallet</span>
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
      </div>

      <!-- ── Kasa Hareketleri ── -->
      <div class="panel" v-if="vaultRows.length">
        <div class="panel-hd">
          <span class="material-symbols-outlined">history</span>
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
                <th>Döviz</th>
                <th>Miktar</th>
                <th>TRY Karşılığı</th>
                <th>Personel</th>
                <th>Açıklama</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(vh, i) in vaultRows" :key="i">
                <td class="no-wrap">{{ fmtDateTime(vh.createdDate ?? vh.date) }}</td>
                <td>
                  <span class="status-chip" :class="vh.isDeposit ? 'done' : 'pend'">
                    {{ vh.isDeposit ? 'Giriş' : 'Çıkış' }}
                  </span>
                </td>
                <td>
                  <span class="type-tag" :style="{ '--tag-color': vh.typeColor }">
                    {{ vh.typeLabel }}
                  </span>
                  <span v-if="vh.isParty" class="party-indicator" title="Cari İşlem">
                    <span class="material-symbols-outlined">person</span>
                  </span>
                </td>
                <td>
                  <div class="cur-cell">
                    <img v-if="getCurrencyFlagImg(vh.currencyCode)" :src="getCurrencyFlagImg(vh.currencyCode)" class="cur-flag cur-flag--sm" />
                    <span>{{ vh.currencyCode }}</span>
                  </div>
                </td>
                <td :class="vh.isDeposit ? 'pos' : 'neg'">{{ vh.isDeposit ? '+' : '-' }}{{ fmt(Math.abs(vh.amount ?? 0)) }}</td>
                <td class="text-muted mono">{{ vh.valueInBaseCurrency ? fmt(vh.valueInBaseCurrency) + ' ₺' : '-' }}</td>
                <td>
                  <span v-if="vh.user" class="user-tag">
                    <span class="material-symbols-outlined">person</span>
                    {{ vh.user }}
                  </span>
                  <span v-else class="text-muted">-</span>
                </td>
                <td class="text-muted desc-cell">{{ vh.description ?? '-' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ── İşlem Detayları ── -->
      <div class="panel" v-if="transactions.length">
        <div class="panel-hd">
          <span class="material-symbols-outlined">receipt_long</span>
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
              <tr v-for="tx in transactions" :key="tx.id ?? tx.transactionNumber">
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
            </tbody>
          </table>
        </div>
      </div>

      <!-- ── Boş ── -->
      <div class="zr-empty" v-if="!currencyRows.length && !transactions.length && !partyData && !cashData">
        <span class="material-symbols-outlined">inbox</span>
        <p>Seçilen dönem için işlem bulunamadı.</p>
      </div>

    </template>

    <!-- ── Başlangıç ── -->
    <div class="zr-empty" v-if="!isLoading && !hasData && !error">
      <span class="material-symbols-outlined">assessment</span>
      <p>Filtre seçip <strong>Raporu Getir</strong> butonuna tıklayın.</p>
    </div>

  </div>
</template>

<style scoped>
/* ── Foundation ── */
.zr { padding: 24px; display: flex; flex-direction: column; gap: 18px; }

/* ── Filters ── */
.zr-filters { background: #fff; border: 1px solid #e5e7eb; border-radius: 12px; padding: 16px 20px; }
.filter-row { display: flex; flex-wrap: wrap; gap: 14px; align-items: flex-end; }
.filter-group { display: flex; flex-direction: column; gap: 5px; }
.filter-group label { font-size: 11px; font-weight: 600; color: #6b7280; text-transform: uppercase; letter-spacing: .04em; }
.filter-group--actions { margin-left: auto; }
.btn-row { display: flex; gap: 8px; }
.zr-input { height: 36px; padding: 0 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 13px; color: #111; background: #f9fafb; outline: none; transition: border .15s; }
.zr-input:focus { border-color: #6366f1; box-shadow: 0 0 0 3px rgba(99,102,241,.12); }
.zr-input-sm { width: 90px; }
.mode-tabs { display: flex; border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden; }
.mt-btn { padding: 7px 14px; font-size: 12px; font-weight: 500; background: #f9fafb; border: none; cursor: pointer; color: #6b7280; transition: all .15s; }
.mt-btn.active { background: #6366f1; color: #fff; }
.btn { display: flex; align-items: center; gap: 6px; height: 36px; padding: 0 16px; border: none; border-radius: 8px; font-size: 13px; font-weight: 600; cursor: pointer; transition: all .15s; }
.btn--primary { background: #6366f1; color: #fff; }
.btn--primary:hover:not(:disabled) { background: #4f46e5; }
.btn--primary:disabled { opacity: .6; cursor: not-allowed; }
.btn--ghost { background: #f3f4f6; color: #374151; padding: 0 10px; }
.btn--ghost:hover { background: #e5e7eb; }
.btn .material-symbols-outlined { font-size: 17px; }

/* ── Error / Loading ── */
.zr-error { background: #fef2f2; border: 1px solid #fecaca; border-radius: 10px; padding: 12px 16px; color: #dc2626; display: flex; align-items: center; gap: 8px; font-size: 13px; }
.zr-loading { display: flex; align-items: center; justify-content: center; gap: 12px; padding: 60px; color: #6b7280; font-size: 14px; }
.spinner { width: 26px; height: 26px; border: 3px solid #e5e7eb; border-top-color: #6366f1; border-radius: 50%; animation: spin .7s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

/* ── Header ── */
.zr-header { display: flex; align-items: flex-start; justify-content: space-between; }
.zr-title { margin: 0; font-size: 18px; font-weight: 700; color: #111; }
.zr-subtitle { margin: 4px 0 0; color: #6b7280; font-size: 13px; display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.zr-timestamp { margin: 0; color: #9ca3af; font-size: 12px; display: none; }
.vault-badge { display: inline-block; padding: 2px 8px; border-radius: 6px; font-size: 11px; font-weight: 600; }
.vault-badge.open { background: #d1fae5; color: #065f46; }
.vault-badge.closed { background: #fee2e2; color: #991b1b; }

/* ── KPI ── */
.kpi-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(175px, 1fr)); gap: 12px; }

/* ── Breakdown Row ── */
.breakdown-row { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; }

/* ── TX Breakdown ── */
.tx-breakdown { display: flex; gap: 10px; flex-wrap: wrap; }
.tx-chip { display: flex; align-items: center; gap: 6px; padding: 6px 12px; background: #fff; border: 1px solid #e5e7eb; border-radius: 8px; font-size: 12px; }
.tx-chip .material-symbols-outlined { font-size: 16px; color: var(--tc); font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.tx-count { font-weight: 700; color: #111; }
.tx-label { color: #6b7280; }

/* ── Opening Balance ── */
.opening-balance { display: flex; align-items: center; gap: 8px; background: #fffbeb; border: 1px solid #fde68a; border-radius: 8px; padding: 6px 14px; margin-left: auto; }
.opening-balance .material-symbols-outlined { font-size: 18px; color: #f59e0b; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.ob-label { font-size: 10px; color: #92400e; margin: 0; text-transform: uppercase; letter-spacing: .03em; }
.ob-val { font-size: 14px; font-weight: 700; color: #92400e; margin: 0; }

/* ── Volume Chips ── */
.volume-chips { display: flex; align-items: center; gap: 12px; background: #fff; border: 1px solid #e5e7eb; border-radius: 10px; padding: 10px 16px; flex-wrap: wrap; }
.vc-title { display: flex; align-items: center; gap: 6px; font-size: 12px; font-weight: 600; color: #374151; white-space: nowrap; }
.vc-title .material-symbols-outlined { font-size: 16px; color: #6366f1; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.vc-list { display: flex; gap: 8px; flex-wrap: wrap; }
.vc-item { display: flex; align-items: center; gap: 8px; background: #f9fafb; border-radius: 6px; padding: 4px 10px; }
.vc-amount { font-weight: 600; font-size: 12px; color: #111; font-family: 'JetBrains Mono', 'Cascadia Code', monospace; }

/* ── Vault Ops Bar ── */
.vault-ops { display: flex; align-items: center; gap: 16px; background: #fff; border: 1px solid #e5e7eb; border-radius: 10px; padding: 14px 20px; flex-wrap: wrap; }
.vo-item { display: flex; align-items: center; gap: 10px; }
.vo-item .material-symbols-outlined { font-size: 22px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.vo-label { font-size: 11px; color: #6b7280; margin: 0; }
.vo-val { font-size: 15px; font-weight: 700; margin: 0; color: #111; }
.vo-divider { width: 1px; height: 32px; background: #e5e7eb; }

/* ── Panels ── */
.panel { background: #fff; border: 1px solid #e5e7eb; border-radius: 12px; overflow: hidden; }
.panel-hd { display: flex; align-items: center; gap: 8px; padding: 14px 18px; border-bottom: 1px solid #f3f4f6; }
.panel-hd h3 { margin: 0; font-size: 14px; font-weight: 600; color: #111; flex: 1; }
.panel-hd .material-symbols-outlined { font-size: 19px; color: #6366f1; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

/* ── Two Column ── */
.two-col { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; }
@media (max-width: 900px) { .two-col { grid-template-columns: 1fr; } }

/* ── Table ── */
.table-wrap { overflow-x: auto; }
.tbl { width: 100%; border-collapse: collapse; font-size: 12px; }
.tbl th { background: #f9fafb; padding: 9px 14px; text-align: left; font-weight: 600; color: #374151; white-space: nowrap; border-bottom: 1px solid #e5e7eb; font-size: 11px; text-transform: uppercase; letter-spacing: .03em; }
.tbl td { padding: 9px 14px; border-bottom: 1px solid #f3f4f6; color: #1f2937; }
.tbl tr:last-child td { border-bottom: none; }
.tbl tr:hover td { background: #fafafa; }
.tbl tfoot td { background: #f9fafb; border-top: 2px solid #e5e7eb; font-size: 13px; }
.tbl--compact { font-size: 12px; }
.tbl--compact td, .tbl--compact th { padding: 7px 14px; }

/* ── Currency Cell ── */
.cur-cell { display: flex; align-items: center; gap: 6px; }
.cur-flag { width: 22px; height: 16px; object-fit: cover; border-radius: 2px; border: 1px solid rgba(0,0,0,.08); }
.cur-flag--sm { width: 18px; height: 13px; }
.cur-code { font-weight: 600; color: #6366f1; font-size: 12px; }
.cur-name { color: #9ca3af; font-size: 11px; }
.cur-row { cursor: pointer; transition: background .1s; }
.cur-row:hover td { background: #f5f3ff !important; }
.cur-row.expanded td { background: #ede9fe; }

/* ── Currency Detail ── */
.cur-detail { border-top: 1px solid #e5e7eb; }
.cur-detail-inner { padding: 14px 18px; background: #faf5ff; }
.cd-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 12px; }
.cd-item { display: flex; flex-direction: column; gap: 2px; }
.cd-label { font-size: 10px; color: #6b7280; text-transform: uppercase; letter-spacing: .04em; }
.cd-val { font-size: 14px; font-weight: 600; color: #111; }

/* ── Party Cards ── */
.party-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(130px, 1fr)); gap: 10px; padding: 14px 18px; }
.party-card { background: #f9fafb; border-radius: 8px; padding: 10px 12px; }
.pc-label { font-size: 10px; color: #6b7280; margin: 0 0 4px; text-transform: uppercase; letter-spacing: .04em; }
.pc-val { font-size: 16px; font-weight: 700; margin: 0; color: #111; }
.pc-val small { font-size: 11px; font-weight: 400; color: #9ca3af; }
.party-volume { display: flex; justify-content: space-between; align-items: center; padding: 8px 18px; background: #f0fdf4; border-top: 1px solid #dcfce7; }
.pv-label { font-size: 11px; color: #166534; font-weight: 500; }
.pv-val { font-size: 13px; font-weight: 700; color: #166534; }
.party-currencies { border-top: 1px solid #f3f4f6; padding: 0; }

/* ── Vault Balances ── */
.vault-balances { display: flex; flex-wrap: wrap; gap: 8px; padding: 14px 18px; }
.vb-item { display: flex; align-items: center; justify-content: space-between; gap: 12px; background: #f9fafb; border-radius: 8px; padding: 8px 12px; min-width: 140px; flex: 1; }
.vb-amount { font-weight: 700; font-size: 14px; color: #111; }
.cash-summary { border-top: 1px solid #f3f4f6; padding: 12px 18px; display: flex; flex-direction: column; gap: 6px; }
.cs-row { display: flex; justify-content: space-between; font-size: 13px; color: #374151; }

/* ── Cash Volumes ── */
.cash-volumes { border-top: 1px solid #f3f4f6; padding: 10px 18px; }
.cv-title { font-size: 10px; color: #6b7280; text-transform: uppercase; letter-spacing: .04em; margin-bottom: 6px; font-weight: 600; }
.cv-list { display: flex; flex-wrap: wrap; gap: 6px; }
.cv-item { display: flex; align-items: center; gap: 6px; background: #f9fafb; border-radius: 6px; padding: 4px 8px; font-size: 12px; font-weight: 500; color: #374151; }

/* ── Type Tags & Chips ── */
.type-tag { display: inline-block; padding: 2px 8px; border-radius: 4px; font-size: 10px; font-weight: 600; background: color-mix(in srgb, var(--tag-color) 12%, transparent); color: var(--tag-color); }
.type-chip { display: inline-block; padding: 2px 8px; border-radius: 6px; font-size: 10px; font-weight: 600; }
.chip-exchange { background: #ede9fe; color: #6d28d9; }
.chip-deposit { background: #d1fae5; color: #065f46; }
.chip-withdrawal { background: #fee2e2; color: #991b1b; }
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
.bar-wrap { position: relative; width: 80px; height: 20px; background: #f3f4f6; border-radius: 4px; overflow: hidden; }
.bar-fill { position: absolute; left: 0; top: 0; height: 100%; background: #6366f1; border-radius: 4px; opacity: .2; }
.bar-text { position: relative; z-index: 1; font-size: 11px; font-weight: 600; color: #374151; display: flex; align-items: center; justify-content: center; height: 100%; }

/* ── Status ── */
.status-chip { display: inline-block; padding: 2px 8px; border-radius: 6px; font-size: 10px; font-weight: 600; }
.status-chip.done { background: #d1fae5; color: #065f46; }
.status-chip.pend { background: #fef3c7; color: #92400e; }
.status-dot { display: inline-block; width: 7px; height: 7px; border-radius: 50%; margin-right: 4px; }
.status-dot.on { background: #10b981; }
.status-dot.off { background: #d1d5db; }

/* ── Badge ── */
.badge { display: inline-block; padding: 2px 8px; border-radius: 6px; background: #f3f4f6; color: #374151; font-size: 11px; font-weight: 600; margin-left: auto; }

/* ── Common ── */
.pos { color: #10b981; font-weight: 600; }
.neg { color: #ef4444; font-weight: 600; }
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
  .zr-filters, .btn-row, .btn--ghost { display: none !important; }
  .zr-timestamp { display: block !important; }
  .zr { padding: 0; gap: 12px; }
  .panel { break-inside: avoid; }
  .cur-row:hover td { background: transparent !important; }
  .volume-chips { border: none; padding: 6px 0; }
  .vault-ops { border: none; padding: 8px 0; }
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
</style>
