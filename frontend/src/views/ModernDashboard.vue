<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'

const router = useRouter()
const authStore = useAuthStore()

const isLoading   = ref(true)
const dashboard   = ref<any>(null)
const offices     = ref<any[]>([])
const transactions = ref<any[]>([])

const fmt = (n: number, dec = 0) =>
  new Intl.NumberFormat('tr-TR', { minimumFractionDigits: dec, maximumFractionDigits: dec }).format(n ?? 0)

const now = new Date()
const dateStr = now.toLocaleDateString('tr-TR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })

const kpiCards = computed(() => {
  const d = dashboard.value?.summary
  if (!d) return []
  return [
    { icon: 'account_balance_wallet', label: 'Toplam Varlık',      value: fmt(d.combinedTotalAssets), unit: 'TRY', color: '#6366f1', bg: 'rgba(99,102,241,0.12)' },
    { icon: 'trending_up',           label: 'Bugünkü Kar',         value: fmt(d.todayProfit),          unit: 'TRY', color: '#10b981', bg: 'rgba(16,185,129,0.12)' },
    { icon: 'calendar_month',        label: 'Aylık Kar',           value: fmt(d.monthlyProfit),         unit: 'TRY', color: '#f59e0b', bg: 'rgba(245,158,11,0.12)' },
    { icon: 'receipt_long',          label: 'Bugün İşlem',         value: fmt(d.todayTransactionCount), unit: 'adet', color: '#3b82f6', bg: 'rgba(59,130,246,0.12)' },
    { icon: 'payments',              label: 'Net Cari Bakiye',     value: fmt(d.netPartyBalance),        unit: 'TRY', color: '#8b5cf6', bg: 'rgba(139,92,246,0.12)' },
    { icon: 'storefront',            label: 'Aktif Şube',          value: fmt(d.numberOfOffices),        unit: 'şube', color: '#ec4899', bg: 'rgba(236,72,153,0.12)' },
  ]
})

const topCurrencies = computed(() => {
  const dist = dashboard.value?.currencyDistribution?.currencies ?? []
  return dist
    .filter((c: any) => c.totalAmount > 0)
    .sort((a: any, b: any) => b.totalValueInBaseCurrency - a.totalValueInBaseCurrency)
    .slice(0, 8)
})

const totalForeignValue = computed(() =>
  dashboard.value?.currencyDistribution?.totalForeignCurrencyValue ?? 1
)

const partyStats = computed(() => {
  const d = dashboard.value?.summary
  return {
    receivables: d?.totalPartyReceivables ?? 0,
    payables:    d?.totalPartyPayables    ?? 0,
    net:         d?.netPartyBalance       ?? 0,
    accounts:    d?.activePartyAccounts   ?? 0,
  }
})

const quickNav = [
  { icon: 'currency_exchange', label: 'Döviz İşlemi',  path: '/ihtiyar/exchange-v2',    color: '#6366f1' },
  { icon: 'assessment',        label: 'Z-Raporu',       path: '/ihtiyar/z-report',        color: '#10b981' },
  { icon: 'groups',            label: 'Cariler',        path: '/ihtiyar/parties',         color: '#f59e0b' },
  { icon: 'account_balance',   label: 'Kasalar',        path: '/ihtiyar/vaults',          color: '#3b82f6' },
  { icon: 'payments',          label: 'Giderler',       path: '/ihtiyar/expenses',        color: '#8b5cf6' },
  { icon: 'manage_accounts',   label: 'Kullanıcılar',   path: '/ihtiyar/users',           color: '#ec4899' },
  { icon: 'history',           label: 'İşlem Geçmişi',  path: '/ihtiyar/history',         color: '#14b8a6' },
  { icon: 'auto_graph',        label: 'Oto Kur',        path: '/ihtiyar/auto-rate-management', color: '#f97316' },
]

const txTypeLabel: Record<number, string> = { 1: 'Döviz', 2: 'Para Yatırma', 3: 'Para Çekme' }
const txStatusLabel: Record<number, { text: string; color: string }> = {
  1: { text: 'Bekliyor', color: '#f59e0b' },
  2: { text: 'Tamamlandı', color: '#10b981' },
  3: { text: 'İptal', color: '#ef4444' },
}

const load = async () => {
  isLoading.value = true
  try {
    const [dash, offs, txs] = await Promise.all([
      apiService.getDashboardData(),
      apiService.getOfficeSummaries(),
      apiService.getTransactionHistory({ pageSize: 8, page: 1 }),
    ])
    dashboard.value    = dash
    offices.value      = offs ?? []
    transactions.value = Array.isArray(txs) ? txs : (txs?.items ?? txs?.data ?? [])
  } catch (e) {
    console.error(e)
  } finally {
    isLoading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="db">
    <!-- Loading -->
    <div v-if="isLoading" class="db-loading">
      <div class="spinner"></div>
      <span>Yükleniyor…</span>
    </div>

    <template v-else>
      <!-- ── Header ─────────────────────────────────────── -->
      <div class="db-header">
        <div>
          <h1 class="db-title">Hoş Geldiniz, {{ authStore.user?.firstname ?? authStore.user?.username }} 👋</h1>
          <p class="db-date">{{ dateStr }}</p>
        </div>
        <button class="refresh-btn" @click="load">
          <span class="material-symbols-outlined">refresh</span> Yenile
        </button>
      </div>

      <!-- ── KPI Cards ──────────────────────────────────── -->
      <div class="kpi-grid">
        <div v-for="k in kpiCards" :key="k.label" class="kpi-card" :style="{ '--kc': k.color, '--kb': k.bg }">
          <div class="kpi-icon"><span class="material-symbols-outlined">{{ k.icon }}</span></div>
          <div class="kpi-body">
            <p class="kpi-label">{{ k.label }}</p>
            <p class="kpi-value">{{ k.value }} <span class="kpi-unit">{{ k.unit }}</span></p>
          </div>
        </div>
      </div>

      <!-- ── Main Row ───────────────────────────────────── -->
      <div class="main-row">
        <!-- Döviz Dağılımı -->
        <div class="panel currency-panel">
          <div class="panel-header">
            <span class="material-symbols-outlined">pie_chart</span>
            <h3>Döviz Varlık Dağılımı</h3>
          </div>
          <div class="currency-list">
            <div v-for="c in topCurrencies" :key="c.currencyCode" class="currency-row">
              <div class="cr-left">
                <span class="cr-code">{{ c.currencyCode }}</span>
                <span class="cr-amount">{{ fmt(c.totalAmount, 2) }}</span>
              </div>
              <div class="cr-bar-wrap">
                <div class="cr-bar" :style="{ width: Math.min(100, (c.totalValueInBaseCurrency / totalForeignValue) * 100) + '%' }"></div>
              </div>
              <span class="cr-try">{{ fmt(c.totalValueInBaseCurrency) }} ₺</span>
            </div>
            <div v-if="topCurrencies.length === 0" class="empty-state">Varlık bulunamadı</div>
          </div>
        </div>

        <!-- Cari Hesaplar -->
        <div class="panel party-panel">
          <div class="panel-header">
            <span class="material-symbols-outlined">account_balance</span>
            <h3>Cari Hesap Özeti</h3>
          </div>
          <div class="party-stats">
            <div class="ps-row green">
              <span class="material-symbols-outlined">arrow_downward</span>
              <div>
                <p class="ps-label">Alacaklar</p>
                <p class="ps-value">{{ fmt(partyStats.receivables) }} ₺</p>
              </div>
            </div>
            <div class="ps-row red">
              <span class="material-symbols-outlined">arrow_upward</span>
              <div>
                <p class="ps-label">Borçlar</p>
                <p class="ps-value">{{ fmt(partyStats.payables) }} ₺</p>
              </div>
            </div>
            <div class="ps-divider"></div>
            <div class="ps-row net">
              <span class="material-symbols-outlined">balance</span>
              <div>
                <p class="ps-label">Net Pozisyon</p>
                <p class="ps-value net-val">{{ fmt(partyStats.net) }} ₺</p>
              </div>
            </div>
            <div class="ps-row blue">
              <span class="material-symbols-outlined">groups</span>
              <div>
                <p class="ps-label">Aktif Hesap</p>
                <p class="ps-value">{{ partyStats.accounts }} cari</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Şube Özetleri -->
        <div class="panel office-panel">
          <div class="panel-header">
            <span class="material-symbols-outlined">storefront</span>
            <h3>Şube Varlıkları</h3>
          </div>
          <div class="office-list">
            <div v-for="o in offices" :key="o.officeId" class="office-card">
              <div class="oc-header">
                <span class="oc-name">{{ o.officeName }}</span>
                <span class="oc-total">{{ fmt(o.totalValueInBaseCurrency) }} ₺</span>
              </div>
              <div class="oc-currencies">
                <template v-for="(amt, code) in o.totalBalancesByCurrency" :key="code">
                  <span v-if="amt > 0" class="oc-tag">{{ code }}: {{ fmt(amt, 2) }}</span>
                </template>
              </div>
              <div class="oc-meta">
                <span class="material-symbols-outlined" style="font-size:14px">inbox</span>
                {{ o.vaultCount }} kasa
              </div>
            </div>
            <div v-if="offices.length === 0" class="empty-state">Şube bulunamadı</div>
          </div>
        </div>
      </div>

      <!-- ── Son İşlemler ───────────────────────────────── -->
      <div class="panel tx-panel">
        <div class="panel-header">
          <span class="material-symbols-outlined">receipt_long</span>
          <h3>Son İşlemler</h3>
          <button class="see-all" @click="router.push('/ihtiyar/history')">
            Tümünü Gör <span class="material-symbols-outlined">chevron_right</span>
          </button>
        </div>
        <div class="tx-table-wrap">
          <table class="tx-table" v-if="transactions.length">
            <thead>
              <tr>
                <th>İşlem No</th>
                <th>Kasa</th>
                <th>Tür</th>
                <th>Tarih</th>
                <th>Durum</th>
                <th>Kar</th>
                <th>Detay</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="tx in transactions" :key="tx.id">
                <td class="tx-num">{{ tx.transactionNumber }}</td>
                <td>{{ tx.vaultName ?? '—' }}</td>
                <td>{{ txTypeLabel[tx.type] ?? tx.type }}</td>
                <td>{{ new Date(tx.transactionDate).toLocaleString('tr-TR') }}</td>
                <td>
                  <span class="tx-status" :style="{ color: txStatusLabel[tx.status]?.color }">
                    {{ txStatusLabel[tx.status]?.text ?? tx.status }}
                  </span>
                </td>
                <td class="tx-profit" :class="{ positive: tx.profit > 0 }">
                  {{ tx.profit > 0 ? '+' : '' }}{{ fmt(tx.profit) }} ₺
                </td>
                <td class="tx-detail">
                  <span v-for="d in tx.details" :key="d.currencyCode" class="detail-chip">
                    {{ d.side === 2 ? '↑' : '↓' }} {{ fmt(d.amount, 2) }} {{ d.currencyCode }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
          <div v-else class="empty-state">Henüz işlem yok</div>
        </div>
      </div>

      <!-- ── Hızlı Erişim ───────────────────────────────── -->
      <div class="quick-nav" v-if="authStore.isAdmin">
        <div class="panel-header" style="margin-bottom:16px">
          <span class="material-symbols-outlined">grid_view</span>
          <h3>Hızlı Erişim</h3>
        </div>
        <div class="qn-grid">
          <button v-for="n in quickNav" :key="n.path"
                  class="qn-btn" @click="router.push(n.path)"
                  :style="{ '--qc': n.color }">
            <span class="material-symbols-outlined">{{ n.icon }}</span>
            <span>{{ n.label }}</span>
          </button>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.db {
  padding: 24px 28px;
  min-height: 100%;
  background: #f1f5f9;
  font-family: 'Inter', sans-serif;
  color: #1e293b;
}

/* Loading */
.db-loading {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  height: 60vh; gap: 16px; color: #64748b;
}
.spinner {
  width: 40px; height: 40px;
  border: 3px solid #e2e8f0; border-top-color: #6366f1;
  border-radius: 50%; animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

/* Header */
.db-header {
  display: flex; align-items: center; justify-content: space-between;
  margin-bottom: 24px;
}
.db-title { font-size: 1.5rem; font-weight: 700; margin: 0; }
.db-date  { font-size: 0.875rem; color: #64748b; margin: 4px 0 0; }
.refresh-btn {
  display: flex; align-items: center; gap: 6px;
  padding: 8px 16px; border: 1px solid #e2e8f0; border-radius: 10px;
  background: white; color: #475569; font-size: 0.875rem; font-weight: 500;
  cursor: pointer; transition: all 0.2s;
}
.refresh-btn:hover { background: #f8fafc; border-color: #6366f1; color: #6366f1; }

/* KPI Grid */
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px; margin-bottom: 20px;
}
.kpi-card {
  background: white; border-radius: 14px; padding: 18px 20px;
  display: flex; align-items: center; gap: 14px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.06);
  border: 1px solid #f1f5f9;
  transition: transform 0.2s, box-shadow 0.2s;
}
.kpi-card:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,0.08); }
.kpi-icon {
  width: 48px; height: 48px; border-radius: 12px;
  background: var(--kb); display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.kpi-icon .material-symbols-outlined { font-size: 24px; color: var(--kc); }
.kpi-label { font-size: 0.75rem; color: #64748b; margin: 0 0 4px; font-weight: 500; }
.kpi-value { font-size: 1.2rem; font-weight: 700; margin: 0; color: #0f172a; }
.kpi-unit  { font-size: 0.7rem; font-weight: 400; color: #94a3b8; }

/* Panel */
.panel {
  background: white; border-radius: 16px; padding: 20px 22px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.06); border: 1px solid #f1f5f9;
  margin-bottom: 20px;
}
.panel-header {
  display: flex; align-items: center; gap: 10px; margin-bottom: 18px;
}
.panel-header h3 { font-size: 1rem; font-weight: 600; margin: 0; flex: 1; }
.panel-header .material-symbols-outlined { font-size: 20px; color: #6366f1; }
.see-all {
  display: flex; align-items: center; gap: 2px;
  font-size: 0.8rem; color: #6366f1; font-weight: 500;
  background: none; border: none; cursor: pointer; padding: 4px 8px;
  border-radius: 6px; transition: background 0.2s;
}
.see-all:hover { background: rgba(99,102,241,0.08); }
.see-all .material-symbols-outlined { font-size: 16px; }

/* Main Row */
.main-row {
  display: grid;
  grid-template-columns: 2fr 1fr 1.5fr;
  gap: 20px; margin-bottom: 0;
}
@media (max-width: 1100px) { .main-row { grid-template-columns: 1fr 1fr; } }
@media (max-width: 700px)  { .main-row { grid-template-columns: 1fr; } }

/* Currency Panel */
.currency-list { display: flex; flex-direction: column; gap: 10px; }
.currency-row {
  display: grid; grid-template-columns: 140px 1fr 120px;
  align-items: center; gap: 10px;
}
.cr-left { display: flex; align-items: center; gap: 8px; }
.cr-code {
  font-size: 0.8rem; font-weight: 700; color: white;
  background: #6366f1; padding: 2px 8px; border-radius: 6px; min-width: 46px; text-align: center;
}
.cr-amount { font-size: 0.8rem; color: #475569; font-weight: 500; }
.cr-bar-wrap { background: #f1f5f9; border-radius: 4px; height: 8px; overflow: hidden; }
.cr-bar { height: 100%; background: linear-gradient(90deg, #6366f1, #8b5cf6); border-radius: 4px; transition: width 0.6s ease; }
.cr-try { font-size: 0.8rem; font-weight: 600; color: #0f172a; text-align: right; }

/* Party Panel */
.party-stats { display: flex; flex-direction: column; gap: 12px; }
.ps-row {
  display: flex; align-items: center; gap: 12px;
  padding: 12px 14px; border-radius: 10px;
}
.ps-row.green  { background: rgba(16,185,129,0.08);  }
.ps-row.green .material-symbols-outlined { color: #10b981; }
.ps-row.red    { background: rgba(239,68,68,0.08);   }
.ps-row.red    .material-symbols-outlined { color: #ef4444; }
.ps-row.net    { background: rgba(99,102,241,0.08);  }
.ps-row.net    .material-symbols-outlined { color: #6366f1; }
.ps-row.blue   { background: rgba(59,130,246,0.08);  }
.ps-row.blue   .material-symbols-outlined { color: #3b82f6; }
.ps-label { font-size: 0.75rem; color: #64748b; margin: 0 0 2px; }
.ps-value { font-size: 1rem; font-weight: 700; margin: 0; }
.net-val  { color: #6366f1; }
.ps-divider { height: 1px; background: #f1f5f9; margin: 2px 0; }

/* Office Panel */
.office-list { display: flex; flex-direction: column; gap: 10px; }
.office-card {
  border: 1px solid #e2e8f0; border-radius: 10px;
  padding: 12px 14px; transition: border-color 0.2s;
}
.office-card:hover { border-color: #6366f1; }
.oc-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
.oc-name  { font-weight: 600; font-size: 0.9rem; }
.oc-total { font-weight: 700; color: #6366f1; font-size: 0.9rem; }
.oc-currencies { display: flex; flex-wrap: wrap; gap: 4px; margin-bottom: 6px; }
.oc-tag {
  font-size: 0.7rem; padding: 2px 6px; background: #f1f5f9;
  border-radius: 4px; color: #475569; font-weight: 500;
}
.oc-meta { font-size: 0.7rem; color: #94a3b8; display: flex; align-items: center; gap: 4px; }

/* Transactions Table */
.tx-panel { overflow: hidden; }
.tx-table-wrap { overflow-x: auto; }
.tx-table { width: 100%; border-collapse: collapse; font-size: 0.82rem; }
.tx-table thead tr { background: #f8fafc; }
.tx-table th {
  text-align: left; padding: 10px 14px;
  font-size: 0.72rem; font-weight: 600; color: #64748b;
  text-transform: uppercase; letter-spacing: 0.05em;
  border-bottom: 1px solid #f1f5f9;
}
.tx-table td { padding: 10px 14px; border-bottom: 1px solid #f8fafc; color: #334155; }
.tx-table tbody tr:hover { background: #f8fafc; }
.tx-num { font-family: monospace; font-size: 0.75rem; color: #64748b; }
.tx-status { font-weight: 600; font-size: 0.75rem; }
.tx-profit { font-weight: 700; color: #94a3b8; }
.tx-profit.positive { color: #10b981; }
.tx-detail { display: flex; flex-wrap: wrap; gap: 4px; }
.detail-chip {
  font-size: 0.7rem; padding: 2px 6px;
  background: #f1f5f9; border-radius: 4px; color: #475569; font-weight: 500; white-space: nowrap;
}

/* Quick Nav */
.quick-nav { background: white; border-radius: 16px; padding: 20px 22px; border: 1px solid #f1f5f9; }
.qn-grid {
  display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 10px;
}
.qn-btn {
  display: flex; flex-direction: column; align-items: center; gap: 8px;
  padding: 16px 10px; border-radius: 12px; border: 1px solid #e2e8f0;
  background: white; cursor: pointer; font-size: 0.78rem; font-weight: 500;
  color: #475569; transition: all 0.2s;
}
.qn-btn:hover {
  border-color: var(--qc); color: var(--qc);
  background: color-mix(in srgb, var(--qc) 8%, white);
  transform: translateY(-2px);
}
.qn-btn .material-symbols-outlined { font-size: 26px; color: var(--qc); }

.empty-state { text-align: center; color: #94a3b8; font-size: 0.875rem; padding: 20px 0; }
</style>
