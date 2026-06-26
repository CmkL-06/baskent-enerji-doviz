<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'

const router = useRouter()
const authStore = useAuthStore()

const isLoading   = ref(true)
const dashboard   = ref<any>(null)
const offices     = ref<any[]>([])
const transactions = ref<any[]>([])
const now = new Date()
const dateStr = now.toLocaleDateString('tr-TR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })

const fmt = (n: number, dec = 0) =>
  new Intl.NumberFormat('tr-TR', { minimumFractionDigits: dec, maximumFractionDigits: dec }).format(n ?? 0)

const kpiCards = computed(() => {
  const d = dashboard.value?.summary
  if (!d) return []
  return [
    { icon: 'account_balance_wallet', label: 'Toplam Varlık',   value: fmt(d.combinedTotalAssets),   unit: 'TRY', color: '#6366f1', bg: 'rgba(99,102,241,0.12)',  trend: null },
    { icon: 'trending_up',            label: 'Bugünkü Kar',     value: fmt(d.todayProfit),           unit: 'TRY', color: '#10b981', bg: 'rgba(16,185,129,0.12)', trend: d.todayProfit >= 0 ? 'up' : 'down' },
    { icon: 'calendar_month',         label: 'Haftalık Kar',    value: fmt(d.weeklyProfit),          unit: 'TRY', color: '#f59e0b', bg: 'rgba(245,158,11,0.12)',  trend: d.weeklyProfit >= 0 ? 'up' : 'down' },
    { icon: 'bar_chart',              label: 'Aylık Kar',       value: fmt(d.monthlyProfit),         unit: 'TRY', color: '#8b5cf6', bg: 'rgba(139,92,246,0.12)',  trend: d.monthlyProfit >= 0 ? 'up' : 'down' },
    { icon: 'receipt_long',           label: 'Bugün İşlem',     value: fmt(d.todayTransactionCount), unit: 'adet',color: '#3b82f6', bg: 'rgba(59,130,246,0.12)', trend: null },
    { icon: 'payments',               label: 'Net Cari Bakiye', value: fmt(d.netPartyBalance),       unit: 'TRY', color: '#ec4899', bg: 'rgba(236,72,153,0.12)', trend: d.netPartyBalance >= 0 ? 'up' : 'down' },
  ]
})

const topCurrencies = computed(() => {
  return (dashboard.value?.currencyDistribution?.currencies ?? [])
    .filter((c: any) => c.totalAmount > 0)
    .sort((a: any, b: any) => b.totalValueInBaseCurrency - a.totalValueInBaseCurrency)
    .slice(0, 8)
})
const totalForeignValue = computed(() => dashboard.value?.currencyDistribution?.totalForeignCurrencyValue ?? 1)

const exchangeRates = computed(() => {
  const rates = dashboard.value?.exchangeRates ?? []
  const priority = ['USD','EUR','GBP','CHF','USDT','KRUB','RUB','AED','AZN','DKK','CAD','BON']
  return rates
    .filter((r: any) => r.buyRate > 0)
    .sort((a: any, b: any) => {
      const ai = priority.indexOf(a.sourceCurrencyCode)
      const bi = priority.indexOf(b.sourceCurrencyCode)
      return (ai === -1 ? 99 : ai) - (bi === -1 ? 99 : bi)
    })
    .slice(0, 12)
})

const partyStats = computed(() => {
  const d = dashboard.value?.summary
  return { receivables: d?.totalPartyReceivables ?? 0, payables: d?.totalPartyPayables ?? 0, net: d?.netPartyBalance ?? 0, accounts: d?.activePartyAccounts ?? 0 }
})

const topReceivables = computed(() => (dashboard.value?.partyAccounts?.topReceivables ?? []).slice(0, 5))
const officeList = computed(() => offices.value ?? [])

const quickNav = [
  { icon: 'currency_exchange', label: 'Döviz İşlemi', path: '/ihtiyar/exchange-v2', color: '#6366f1' },
  { icon: 'assessment',        label: 'Z-Raporu',      path: '/ihtiyar/z-report',   color: '#10b981' },
  { icon: 'groups',            label: 'Cariler',       path: '/ihtiyar/parties',    color: '#f59e0b' },
  { icon: 'account_balance',   label: 'Kasalar',       path: '/ihtiyar/vaults',     color: '#3b82f6' },
  { icon: 'receipt_long',      label: 'Giderler',      path: '/ihtiyar/expenses',   color: '#8b5cf6' },
  { icon: 'manage_accounts',   label: 'Kullanıcılar',  path: '/ihtiyar/users',      color: '#ec4899' },
  { icon: 'history',           label: 'Geçmiş',        path: '/ihtiyar/history',    color: '#14b8a6' },
  { icon: 'inventory_2',       label: 'Kasa Sayımı',   path: '/ihtiyar/vault-counts', color: '#f97316' },
]

const txType   = { 1: 'Döviz', 2: 'Para Yatırma', 3: 'Para Çekme' } as Record<number,string>
const txStatus = { 1: { text: 'Bekliyor', color: '#f59e0b' }, 2: { text: 'Tamamlandı', color: '#10b981' }, 3: { text: 'İptal', color: '#ef4444' } } as Record<number,{text:string;color:string}>

async function load() {
  isLoading.value = true
  try {
    const [db, offSummaries, txData] = await Promise.all([
      apiService.getDashboardData(),
      apiService.getOfficeSummaries(),
      apiService.getTransactionHistory({ pageSize: 8, page: 1 }),
    ])
    dashboard.value    = db
    offices.value      = offSummaries ?? []
    transactions.value = Array.isArray(txData) ? txData : (txData?.items ?? txData?.data ?? [])
  } catch(e) { console.error(e) }
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
      <div class="db-header">
        <div>
          <h1 class="db-title">Hoş Geldiniz, {{ authStore.user?.firstname ?? authStore.user?.username }} 👋</h1>
          <p class="db-date">{{ dateStr }}</p>
        </div>
        <button class="refresh-btn" @click="load"><span class="material-symbols-outlined">refresh</span> Yenile</button>
      </div>
      <div class="kpi-grid">
        <div v-for="k in kpiCards" :key="k.label" class="kpi-card" :style="{'--kc':k.color,'--kb':k.bg}">
          <div class="kpi-icon"><span class="material-symbols-outlined">{{ k.icon }}</span></div>
          <div class="kpi-body">
            <p class="kpi-label">{{ k.label }}</p>
            <p class="kpi-value">{{ k.value }} <span class="kpi-unit">{{ k.unit }}</span></p>
            <span v-if="k.trend==='up'" class="kpi-trend up">↑</span>
            <span v-if="k.trend==='down'" class="kpi-trend down">↓</span>
          </div>
        </div>
      </div>
      <div class="main-row">
        <div class="panel">
          <div class="panel-header"><span class="material-symbols-outlined">currency_exchange</span><h3>Canlı Kurlar</h3><span class="panel-badge">{{ exchangeRates.length }} döviz</span></div>
          <div class="rates-table">
            <div class="rates-header"><span>Döviz</span><span>Alış</span><span>Satış</span><span>Fark</span></div>
            <div v-for="r in exchangeRates" :key="r.sourceCurrencyCode" class="rate-row">
              <span class="rate-code">{{ r.sourceCurrencyCode }}</span>
              <span class="rate-buy">{{ r.buyRate?.toFixed(4) }}</span>
              <span class="rate-sell">{{ r.sellRate?.toFixed(4) }}</span>
              <span class="rate-spread">+{{ r.spread?.toFixed(4) }}</span>
            </div>
            <p v-if="!exchangeRates.length" class="empty-msg">Kur verisi yok</p>
          </div>
        </div>
        <div class="panel">
          <div class="panel-header"><span class="material-symbols-outlined">pie_chart</span><h3>Döviz Varlık Dağılımı</h3></div>
          <div class="currency-list">
            <div v-for="c in topCurrencies" :key="c.currencyCode" class="currency-row">
              <div class="cr-left"><span class="cr-code">{{ c.currencyCode }}</span><span class="cr-amount">{{ c.totalAmount.toLocaleString('tr-TR',{minimumFractionDigits:2}) }}</span></div>
              <div class="cr-bar-wrap"><div class="cr-bar" :style="{width:Math.min(100,c.totalValueInBaseCurrency/totalForeignValue*100)+'%'}"></div></div>
              <span class="cr-try">{{ fmt(c.totalValueInBaseCurrency) }} ₺</span>
            </div>
            <p v-if="!topCurrencies.length" class="empty-msg">Varlık bulunamadı</p>
          </div>
        </div>
        <div class="panel">
          <div class="panel-header"><span class="material-symbols-outlined">storefront</span><h3>Şube Varlıkları</h3></div>
          <div class="office-list">
            <div v-for="o in officeList" :key="o.officeId" class="office-card">
              <div class="oc-header"><span class="oc-name">{{ o.officeName }}</span><span class="oc-total">{{ fmt(o.totalValueInBaseCurrency) }} ₺</span></div>
              <div class="oc-pnl">
                <span class="lbl">Günlük:</span><span :class="o.dailyProfitLoss>=0?'pos':'neg'">{{ o.dailyProfitLoss>=0?'+':'' }}{{ fmt(o.dailyProfitLoss) }} ₺</span>
                <span class="lbl ml">Aylık:</span><span :class="o.monthlyProfitLoss>=0?'pos':'neg'">{{ o.monthlyProfitLoss>=0?'+':'' }}{{ fmt(o.monthlyProfitLoss) }} ₺</span>
              </div>
              <div class="oc-currencies">
                <template v-for="(amt,code) in o.totalBalancesByCurrency" :key="code">
                  <span v-if="amt>0" class="oc-tag">{{ code }}: {{ Number(amt).toLocaleString('tr-TR',{minimumFractionDigits:2}) }}</span>
                </template>
              </div>
              <div class="oc-meta"><span class="material-symbols-outlined" style="font-size:14px">inbox</span> {{ o.vaultCount }} kasa</div>
            </div>
            <p v-if="!officeList.length" class="empty-msg">Şube bulunamadı</p>
          </div>
        </div>
      </div>
      <div class="second-row">
        <div class="panel">
          <div class="panel-header"><span class="material-symbols-outlined">account_balance</span><h3>Cari Hesap Özeti</h3></div>
          <div class="party-stats">
            <div class="ps-row green"><span class="material-symbols-outlined">arrow_downward</span><div><p class="ps-label">Alacaklar</p><p class="ps-value">{{ fmt(partyStats.receivables) }} ₺</p></div></div>
            <div class="ps-row red"><span class="material-symbols-outlined">arrow_upward</span><div><p class="ps-label">Borçlar</p><p class="ps-value">{{ fmt(partyStats.payables) }} ₺</p></div></div>
            <div class="ps-divider"></div>
            <div class="ps-row net"><span class="material-symbols-outlined">balance</span><div><p class="ps-label">Net Pozisyon</p><p class="ps-value net-val">{{ fmt(partyStats.net) }} ₺</p></div></div>
            <div class="ps-row blue"><span class="material-symbols-outlined">groups</span><div><p class="ps-label">Aktif Hesap</p><p class="ps-value">{{ partyStats.accounts }} cari</p></div></div>
          </div>
          <div v-if="topReceivables.length" class="top-recv">
            <p class="recv-title">En Yüksek Alacaklar</p>
            <div v-for="p in topReceivables" :key="p.partyCode" class="recv-row">
              <span class="recv-name">{{ p.partyName }}</span>
              <span class="pos recv-val">{{ fmt(p.balance) }} ₺</span>
            </div>
          </div>
        </div>
        <div class="panel">
          <div class="panel-header"><span class="material-symbols-outlined">receipt_long</span><h3>Son İşlemler</h3>
            <button class="see-all" @click="router.push('/ihtiyar/history')">Tümünü Gör <span class="material-symbols-outlined">chevron_right</span></button>
          </div>
          <div class="tx-wrap">
            <table v-if="transactions.length" class="tx-table">
              <thead><tr><th>İşlem No</th><th>Kasa</th><th>Tür</th><th>Tarih</th><th>Durum</th><th>Kar</th><th>Detay</th></tr></thead>
              <tbody>
                <tr v-for="tx in transactions" :key="tx.id">
                  <td class="mono">{{ tx.transactionNumber }}</td>
                  <td>{{ tx.vaultName??'—' }}</td>
                  <td>{{ txType[tx.type]??tx.type }}</td>
                  <td>{{ new Date(tx.transactionDate).toLocaleString('tr-TR') }}</td>
                  <td><span :style="{color:txStatus[tx.status]?.color,fontWeight:'600'}">{{ txStatus[tx.status]?.text??tx.status }}</span></td>
                  <td :class="['profit',{pos:tx.profit>0}]">{{ tx.profit>0?'+':'' }}{{ fmt(tx.profit) }} ₺</td>
                  <td class="detail-cell">
                    <span v-for="d in tx.details" :key="d.currencyCode" class="chip">{{ d.side===2?'↑':'↓' }} {{ (d.amount??0).toLocaleString('tr-TR',{minimumFractionDigits:2}) }} {{ d.currencyCode }}</span>
                  </td>
                </tr>
              </tbody>
            </table>
            <p v-else class="empty-state">Henüz işlem yok</p>
          </div>
        </div>
      </div>
      <div v-if="authStore.isAdmin" class="quick-nav">
        <div class="qn-header"><span class="material-symbols-outlined">grid_view</span><h3>Hızlı Erişim</h3></div>
        <div class="qn-grid">
          <button v-for="item in quickNav" :key="item.path" class="qn-btn" @click="router.push(item.path)" :style="{'--qc':item.color}">
            <span class="material-symbols-outlined">{{ item.icon }}</span><span>{{ item.label }}</span>
          </button>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.db { padding: 24px; display: flex; flex-direction: column; gap: 20px; min-height: 0; }
.db-loading { display:flex; flex-direction:column; align-items:center; justify-content:center; height:60vh; gap:16px; color:#64748b; }
.spinner { width:40px; height:40px; border:3px solid #e2e8f0; border-top-color:#6366f1; border-radius:50%; animation:spin .8s linear infinite; }
@keyframes spin { to { transform:rotate(360deg); } }
.db-header { display:flex; align-items:center; justify-content:space-between; }
.db-title { font-size:1.5rem; font-weight:700; margin:0; color:#0f172a; }
.db-date  { font-size:.875rem; color:#64748b; margin:4px 0 0; }
.refresh-btn { display:flex; align-items:center; gap:6px; padding:8px 16px; background:#f8fafc; border:1px solid #e2e8f0; border-radius:8px; cursor:pointer; font-size:14px; font-weight:500; transition:.15s; }
.refresh-btn:hover { background:#e2e8f0; }
.kpi-grid { display:grid; grid-template-columns:repeat(6,1fr); gap:12px; }
@media(max-width:1200px){.kpi-grid{grid-template-columns:repeat(3,1fr);}}
@media(max-width:700px){.kpi-grid{grid-template-columns:repeat(2,1fr);}}
.kpi-card { background:var(--kb); border:1px solid rgba(0,0,0,.06); border-radius:14px; padding:16px; display:flex; align-items:center; gap:12px; position:relative; transition:.15s; }
.kpi-card:hover { transform:translateY(-2px); box-shadow:0 4px 16px rgba(0,0,0,.08); }
.kpi-icon { width:42px; height:42px; display:flex; align-items:center; justify-content:center; border-radius:10px; background:white; flex-shrink:0; }
.kpi-icon .material-symbols-outlined { font-size:22px; color:var(--kc); }
.kpi-label { font-size:11px; color:#6b7280; margin:0 0 3px; font-weight:500; text-transform:uppercase; letter-spacing:.04em; }
.kpi-value { font-size:18px; font-weight:800; color:#111; margin:0; }
.kpi-unit  { font-size:11px; font-weight:400; color:#9ca3af; }
.kpi-trend { position:absolute; top:10px; right:12px; font-size:16px; font-weight:700; }
.kpi-trend.up   { color:#10b981; }
.kpi-trend.down { color:#ef4444; }
.panel { background:#fff; border:1px solid #e5e7eb; border-radius:14px; overflow:hidden; }
.panel-header { display:flex; align-items:center; gap:8px; padding:14px 18px; border-bottom:1px solid #f3f4f6; }
.panel-header h3 { margin:0; font-size:14px; font-weight:700; color:#111; flex:1; }
.panel-header .material-symbols-outlined { font-size:20px; color:#6366f1; }
.panel-badge { font-size:11px; background:#ede9fe; color:#6366f1; padding:2px 8px; border-radius:12px; font-weight:600; }
.main-row { display:grid; grid-template-columns:1fr 1.2fr 1fr; gap:16px; }
@media(max-width:1100px){.main-row{grid-template-columns:1fr 1fr;}}
@media(max-width:700px){.main-row{grid-template-columns:1fr;}}
.rates-table { padding:0; max-height:260px; overflow-y:auto; }
.rates-header { display:grid; grid-template-columns:60px 1fr 1fr 1fr; padding:8px 16px; background:#f9fafb; font-size:11px; font-weight:700; color:#6b7280; text-transform:uppercase; position:sticky; top:0; }
.rate-row { display:grid; grid-template-columns:60px 1fr 1fr 1fr; padding:7px 16px; border-bottom:1px solid #f9fafb; font-size:13px; transition:.1s; }
.rate-row:hover { background:#f9fafb; }
.rate-code  { font-weight:700; color:#374151; }
.rate-buy   { color:#3b82f6; font-weight:600; }
.rate-sell  { color:#ef4444; font-weight:600; }
.rate-spread { color:#10b981; font-size:11px; }
.currency-list { padding:12px 16px; display:flex; flex-direction:column; gap:8px; }
.currency-row { display:flex; align-items:center; gap:8px; }
.cr-left { display:flex; align-items:center; gap:8px; width:140px; flex-shrink:0; }
.cr-code   { font-weight:700; font-size:13px; color:#374151; width:50px; }
.cr-amount { font-size:12px; color:#6b7280; }
.cr-bar-wrap { flex:1; background:#f3f4f6; border-radius:4px; height:6px; }
.cr-bar { height:6px; border-radius:4px; background:linear-gradient(90deg,#6366f1,#8b5cf6); transition:width .4s; }
.cr-try { font-size:12px; color:#374151; font-weight:600; width:90px; text-align:right; }
.office-list { padding:12px; display:flex; flex-direction:column; gap:10px; }
.office-card { background:#f9fafb; border:1px solid #e5e7eb; border-radius:10px; padding:12px; }
.oc-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:6px; }
.oc-name  { font-weight:700; font-size:13px; color:#111; }
.oc-total { font-size:13px; font-weight:700; color:#6366f1; }
.oc-pnl { display:flex; align-items:center; gap:4px; font-size:11px; margin-bottom:6px; flex-wrap:wrap; }
.lbl { color:#9ca3af; }
.ml  { margin-left:8px; }
.oc-currencies { display:flex; flex-wrap:wrap; gap:4px; margin-bottom:6px; }
.oc-tag { font-size:11px; background:#ede9fe; color:#6366f1; padding:2px 6px; border-radius:6px; font-weight:600; }
.oc-meta { font-size:11px; color:#9ca3af; display:flex; align-items:center; gap:4px; }
.second-row { display:grid; grid-template-columns:320px 1fr; gap:16px; }
@media(max-width:1000px){.second-row{grid-template-columns:1fr;}}
.party-stats { padding:12px 16px; display:flex; flex-direction:column; gap:8px; }
.ps-row { display:flex; align-items:center; gap:12px; padding:10px 12px; border-radius:10px; }
.ps-row.green { background:rgba(16,185,129,.08); }
.ps-row.red   { background:rgba(239,68,68,.08); }
.ps-row.net   { background:rgba(99,102,241,.08); }
.ps-row.blue  { background:rgba(59,130,246,.08); }
.ps-row .material-symbols-outlined { font-size:20px; }
.ps-row.green .material-symbols-outlined { color:#10b981; }
.ps-row.red   .material-symbols-outlined { color:#ef4444; }
.ps-row.net   .material-symbols-outlined { color:#6366f1; }
.ps-row.blue  .material-symbols-outlined { color:#3b82f6; }
.ps-label { font-size:11px; color:#6b7280; margin:0 0 2px; }
.ps-value { font-size:16px; font-weight:700; margin:0; color:#111; }
.net-val  { color:#6366f1; }
.ps-divider { height:1px; background:#f3f4f6; }
.top-recv { padding:0 16px 12px; }
.recv-title { font-size:12px; font-weight:700; color:#6b7280; text-transform:uppercase; margin:0 0 8px; }
.recv-row { display:flex; justify-content:space-between; align-items:center; padding:6px 0; border-bottom:1px solid #f3f4f6; font-size:13px; }
.recv-row:last-child { border-bottom:none; }
.recv-name { color:#374151; font-weight:500; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; max-width:180px; }
.recv-val  { font-weight:700; }
.tx-wrap { overflow-x:auto; }
.tx-table { width:100%; border-collapse:collapse; font-size:12px; }
.tx-table th { background:#f9fafb; padding:9px 12px; text-align:left; font-weight:700; color:#374151; white-space:nowrap; border-bottom:1px solid #e5e7eb; }
.tx-table td { padding:9px 12px; border-bottom:1px solid #f3f4f6; }
.tx-table tr:hover td { background:#f9fafb; }
.mono { font-family:monospace; font-size:11px; }
.profit { font-weight:700; color:#6b7280; }
.profit.pos { color:#10b981; }
.detail-cell { display:flex; flex-wrap:wrap; gap:4px; }
.chip { font-size:11px; background:#f3f4f6; padding:2px 6px; border-radius:5px; white-space:nowrap; }
.empty-state { padding:40px; text-align:center; color:#9ca3af; font-size:14px; }
.quick-nav { background:#fff; border:1px solid #e5e7eb; border-radius:14px; padding:16px 18px; }
.qn-header { display:flex; align-items:center; gap:8px; margin-bottom:16px; }
.qn-header h3 { margin:0; font-size:14px; font-weight:700; color:#111; }
.qn-header .material-symbols-outlined { font-size:20px; color:#6366f1; }
.qn-grid { display:grid; grid-template-columns:repeat(8,1fr); gap:10px; }
@media(max-width:1100px){.qn-grid{grid-template-columns:repeat(4,1fr);}}
.qn-btn { display:flex; flex-direction:column; align-items:center; gap:6px; padding:14px 8px; background:#fafafa; border:1px solid #e5e7eb; border-radius:12px; cursor:pointer; font-size:11px; font-weight:600; color:#374151; transition:.15s; }
.qn-btn:hover { transform:translateY(-2px); box-shadow:0 4px 12px rgba(0,0,0,.1); background:white; border-color:var(--qc); }
.qn-btn .material-symbols-outlined { font-size:24px; color:var(--qc); }
.pos { color:#10b981; }
.neg { color:#ef4444; }
.empty-msg { padding:20px; text-align:center; color:#9ca3af; font-size:13px; }
.see-all { display:flex; align-items:center; gap:2px; background:none; border:none; cursor:pointer; color:#6366f1; font-size:13px; font-weight:600; padding:4px 8px; border-radius:6px; margin-left:auto; }
.see-all:hover { background:#ede9fe; }
.see-all .material-symbols-outlined { font-size:16px; }
</style>
