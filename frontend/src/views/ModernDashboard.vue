<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'

const router    = useRouter()
const authStore = useAuthStore()

const isLoading       = ref(true)
const dashboard       = ref<any>(null)
const transactions    = ref<any[]>([])
const pending         = ref<any[]>([])
const processingId    = ref<string | null>(null)
const rejectReason    = ref('')
const rejectTarget    = ref<string | null>(null)
const now             = new Date()
const dateStr         = now.toLocaleDateString('tr-TR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })

const fmtNum = (n: number, dec = 0) =>
  new Intl.NumberFormat('tr-TR', { minimumFractionDigits: dec, maximumFractionDigits: dec }).format(n ?? 0)
const fmtN4  = (n: number) =>
  Number(n ?? 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })
const fmtDate = (d: string) => {
  if (!d) return '—'
  const dt = new Date(d)
  return dt.toLocaleDateString('tr-TR') + ' ' + dt.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}
const plColor = (n: number) => Number(n ?? 0) >= 0 ? '#22c55e' : '#ef4444'
const plSign  = (n: number) => Number(n ?? 0) >= 0 ? '+' : ''
const g = (o: any, k: string) => {
  if (!o) return undefined
  const lk = k[0].toLowerCase() + k.slice(1)
  return o[lk] !== undefined ? o[lk] : o[k]
}

// ── Computed ──────────────────────────────────────────────
const sum      = computed(() => dashboard.value?.summary ?? {})
const offices  = computed(() => (dashboard.value?.offices ?? []).filter((o: any) => o.officeName !== 'ANA KASA'))
const rates    = computed(() => {
  const priority = ['USD','EUR','GBP','CHF','USDT','KRUB','RUB','AED','AZN','DKK','CAD','BON']
  return (dashboard.value?.exchangeRates ?? [])
    .filter((r: any) => r.buyRate > 0)
    .sort((a: any, b: any) => {
      const ai = priority.indexOf(a.sourceCurrencyCode), bi = priority.indexOf(b.sourceCurrencyCode)
      return (ai === -1 ? 99 : ai) - (bi === -1 ? 99 : bi)
    })
    .slice(0, 12)
})
const topCurrencies = computed(() =>
  (dashboard.value?.currencyDistribution?.currencies ?? [])
    .filter((c: any) => c.totalAmount > 0)
    .sort((a: any, b: any) => b.totalValueInBaseCurrency - a.totalValueInBaseCurrency)
    .slice(0, 8)
)
const totalFV = computed(() => dashboard.value?.currencyDistribution?.totalForeignCurrencyValue ?? 1)
const partyByCur = computed(() => dashboard.value?.partyAccounts?.byCurrency ?? [])
const topRecv    = computed(() => (dashboard.value?.partyAccounts?.topReceivables ?? []).slice(0, 5))

const maxPnL = computed(() => {
  const vals = offices.value.map((o: any) => Math.abs(Number(g(o, 'dailyProfitLoss') ?? 0)))
  return Math.max(...vals, 1)
})

const adminKpi = computed(() => {
  const s = sum.value
  const cards = []
  if (authStore.isOwner) cards.push({ icon: 'account_balance', label: 'Toplam Varlık', value: fmtNum(g(s, 'totalAssets')), unit: '₺', color: '#7c3aed', bg: '#f3f0ff' })
  const tp = Number(g(s, 'todayProfit') ?? 0)
  cards.push({ icon: 'trending_up', label: 'Bugün K/Z', value: plSign(tp) + fmtNum(tp), unit: '₺', color: plColor(tp), bg: tp >= 0 ? '#f0fdf4' : '#fef2f2' })
  const mp = Number(g(s, 'monthlyProfit') ?? 0)
  cards.push({ icon: 'calendar_month', label: 'Aylık K/Z', value: plSign(mp) + fmtNum(mp), unit: '₺', color: plColor(mp), bg: mp >= 0 ? '#f0fdf4' : '#fef2f2' })
  cards.push({ icon: 'people', label: 'Cari Net', value: fmtNum(g(s, 'netPartyBalance')), unit: '₺', color: '#d97706', bg: '#fffbeb' })
  if (authStore.isOwner) cards.push({ icon: 'currency_exchange', label: 'Döviz Varlık', value: fmtNum(g(s, 'totalForeignCurrencyValue')), unit: '₺', color: '#2563eb', bg: '#eff6ff' })
  cards.push({ icon: 'receipt_long', label: 'Bugün İşlem', value: String(g(s, 'todayTransactionCount') ?? 0), unit: 'adet', color: '#6366f1', bg: '#eef2ff' })
  cards.push({ icon: 'pending_actions', label: 'Bekleyen', value: String(pending.value.length), unit: '', color: pending.value.length > 0 ? '#dc2626' : '#6b7280', bg: pending.value.length > 0 ? '#fef2f2' : '#f9fafb' })
  return cards
})

const staffKpi = computed(() => {
  const s = sum.value
  const tp = Number(g(s, 'todayProfit') ?? 0)
  return [
    { icon: 'receipt_long',  label: 'Bugünkü İşlem', value: String(g(s, 'todayTransactionCount') ?? 0), unit: 'adet', color: '#6366f1', bg: '#eef2ff' },
    { icon: 'trending_up',   label: 'Bugün K/Z',     value: plSign(tp) + fmtNum(tp),                    unit: '₺',    color: plColor(tp), bg: tp >= 0 ? '#f0fdf4' : '#fef2f2' },
  ]
})

const staffQuickLinks = [
  { p: '/ihtiyar/exchange-v2?type=buy&currency=fiat',  i: 'south_east',         l: 'Döviz Al' },
  { p: '/ihtiyar/exchange-v2?type=sell&currency=fiat', i: 'north_east',         l: 'Döviz Sat' },
  { p: '/ihtiyar/exchange-v2?type=buy&currency=usdt',  i: 'generating_tokens',  l: 'USDT Al' },
  { p: '/ihtiyar/exchange-v2?type=sell&currency=usdt', i: 'token',              l: 'USDT Sat' },
  { p: '/ihtiyar/history',                             i: 'history',            l: 'Geçmiş' },
  { p: '/ihtiyar/parties',                             i: 'contacts',           l: 'Cariler' },
  { p: '/ihtiyar/vaults',                              i: 'account_balance_wallet', l: 'Kasam' },
  { p: '/ihtiyar/expenses',                            i: 'receipt_long',       l: 'Giderler' },
]

const adminQuickLinks = computed(() => {
  const base = [
    { p: '/ihtiyar/exchange-v2',      i: 'paid',                    l: 'İşlem' },
    { p: '/ihtiyar/z-report',         i: 'insert_chart',            l: 'Z Raporu' },
    { p: '/ihtiyar/vaults',           i: 'account_balance_wallet',  l: 'Kasalar' },
    { p: '/ihtiyar/parties',          i: 'contacts',                l: 'Cariler' },
    { p: '/ihtiyar/history',          i: 'history',                 l: 'Geçmiş' },
    { p: '/ihtiyar/expenses',         i: 'receipt_long',            l: 'Giderler' },
    { p: '/ihtiyar/office-transfers', i: 'swap_horiz',              l: 'Transferler' },
    { p: '/ihtiyar/settings',         i: 'tune',                    l: 'Ayarlar' },
  ]
  if (!authStore.isOwner) return base
  return [...base,
    { p: '/ihtiyar/users',            i: 'manage_accounts',         l: 'Kullanıcılar' },
    { p: '/ihtiyar/user-offices',     i: 'admin_panel_settings',    l: 'Yetki Ver' },
    { p: '/ihtiyar/vault-counts',     i: 'inventory_2',             l: 'Kasa Sayım' },
    { p: '/ihtiyar/owner-panel',      i: 'crown',                   l: 'Owner Panel' },
  ]
})

const txType = { 1: 'Döviz', 2: 'Yatırma', 3: 'Çekim', 4: 'Transfer' } as Record<number, string>

// ── Data load ─────────────────────────────────────────────
async function load() {
  isLoading.value = true
  try {
    const calls: Promise<any>[] = [
      apiService.getDashboardData(),
      apiService.getTransactionHistory({ pageSize: 10, page: 1 }),
    ]
    if (authStore.isAdmin) calls.push(apiService.getPendingTransfers())
    const [db, txData, pend] = await Promise.all(calls)
    dashboard.value    = db
    transactions.value = Array.isArray(txData) ? txData : (txData?.items ?? txData?.data ?? [])
    pending.value      = pend ?? []
  } catch (e) { console.error(e) }
  finally { isLoading.value = false }
}

async function approveTransfer(id: string) {
  processingId.value = id
  try {
    await apiService.processTransfer(id, { action: 1, rejectReason: '' })
    pending.value = pending.value.filter((t: any) => (g(t, 'transferId') || g(t, 'id')) !== id)
  } catch (e: any) { alert('Hata: ' + (e.response?.data?.message ?? e.message)) }
  finally { processingId.value = null }
}

function promptReject(id: string) { rejectTarget.value = id; rejectReason.value = '' }

async function confirmReject() {
  if (!rejectTarget.value) return
  const id = rejectTarget.value
  processingId.value = id
  rejectTarget.value = null
  try {
    await apiService.processTransfer(id, { action: 2, rejectReason: rejectReason.value })
    pending.value = pending.value.filter((t: any) => (g(t, 'transferId') || g(t, 'id')) !== id)
  } catch (e: any) { alert('Hata: ' + (e.response?.data?.message ?? e.message)) }
  finally { processingId.value = null; rejectReason.value = '' }
}

let timer: ReturnType<typeof setInterval>
onMounted(() => { load(); timer = setInterval(load, 60000) })
onUnmounted(() => clearInterval(timer))
</script>

<template>
  <div class="db">

    <!-- Loading -->
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

      <!-- ═══════════════════════════════════════ STAFF VIEW ═══ -->
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

        <div class="main-row two-col">
          <!-- Rates -->
          <div class="panel">
            <div class="panel-header"><span class="material-symbols-outlined">currency_exchange</span><h3>Güncel Kurlar</h3></div>
            <div class="rates-table">
              <div class="rates-header"><span>Döviz</span><span>Alış</span><span>Satış</span></div>
              <div v-for="r in rates" :key="r.sourceCurrencyCode" class="rate-row three-col">
                <span class="rate-code">{{ r.sourceCurrencyCode }}</span>
                <span class="rate-buy">{{ fmtN4(r.buyRate) }}</span>
                <span class="rate-sell">{{ fmtN4(r.sellRate) }}</span>
              </div>
              <p v-if="!rates.length" class="empty-msg">Kur verisi yok</p>
            </div>
          </div>

          <!-- Staff Transactions -->
          <div class="panel">
            <div class="panel-header">
              <span class="material-symbols-outlined">receipt_long</span><h3>Bugünkü İşlemlerim</h3>
              <button class="see-all" @click="router.push('/ihtiyar/history')">Tümü <span class="material-symbols-outlined">chevron_right</span></button>
            </div>
            <div class="tx-wrap">
              <table v-if="transactions.length" class="tx-table">
                <thead><tr><th>Saat</th><th>Tür</th><th>K/Z</th></tr></thead>
                <tbody>
                  <tr v-for="tx in transactions" :key="tx.id">
                    <td>{{ new Date(tx.transactionDate).toLocaleTimeString('tr-TR', {hour:'2-digit',minute:'2-digit'}) }}</td>
                    <td>{{ txType[tx.type] ?? tx.type }}</td>
                    <td :style="{color:plColor(tx.profit),fontWeight:'600'}">{{ plSign(tx.profit) }}{{ fmtNum(tx.profit) }} ₺</td>
                  </tr>
                </tbody>
              </table>
              <p v-else class="empty-state">Henüz işlem yok</p>
            </div>
          </div>
        </div>

        <!-- Staff Quick Links -->
        <div class="panel quick-nav">
          <div class="panel-header"><span class="material-symbols-outlined">bolt</span><h3>Hızlı İşlem</h3></div>
          <div class="qn-grid">
            <button v-for="l in staffQuickLinks" :key="l.p" class="qn-btn" @click="router.push(l.p)">
              <span class="material-symbols-outlined">{{ l.i }}</span><span>{{ l.l }}</span>
            </button>
          </div>
        </div>
      </template>

      <!-- ══════════════════════════════════ ADMIN / OWNER VIEW ═══ -->
      <template v-else>
        <!-- KPI -->
        <div class="kpi-grid">
          <div v-for="k in adminKpi" :key="k.label" class="kpi-card" :style="{'--kc':k.color,'--kb':k.bg}">
            <div class="kpi-icon"><span class="material-symbols-outlined">{{ k.icon }}</span></div>
            <div class="kpi-body">
              <p class="kpi-label">{{ k.label }}</p>
              <p class="kpi-value" :style="{color:k.color}">{{ k.value }} <span class="kpi-unit">{{ k.unit }}</span></p>
            </div>
          </div>
        </div>

        <!-- Row 1: Rates | Office P&L bars | Pending transfers -->
        <div class="main-row three-col">

          <!-- Live rates -->
          <div class="panel">
            <div class="panel-header">
              <span class="material-symbols-outlined">currency_exchange</span>
              <h3>Canlı Kurlar</h3>
              <span class="panel-badge">{{ rates.length }} döviz</span>
            </div>
            <div class="rates-table">
              <div class="rates-header"><span>Döviz</span><span>Alış</span><span>Satış</span><span>Fark</span></div>
              <div v-for="r in rates" :key="r.sourceCurrencyCode" class="rate-row">
                <span class="rate-code">{{ r.sourceCurrencyCode }}</span>
                <span class="rate-buy">{{ fmtN4(r.buyRate) }}</span>
                <span class="rate-sell">{{ fmtN4(r.sellRate) }}</span>
                <span class="rate-spread">+{{ fmtN4((r.sellRate ?? 0) - (r.buyRate ?? 0)) }}</span>
              </div>
              <p v-if="!rates.length" class="empty-msg">Kur verisi yok</p>
            </div>
          </div>

          <!-- Office daily P&L bars -->
          <div class="panel">
            <div class="panel-header"><span class="material-symbols-outlined">leaderboard</span><h3>Şube Günlük K/Z</h3></div>
            <div class="pnl-list">
              <div v-for="o in offices" :key="o.officeId" class="pnl-row">
                <div class="pnl-name" :title="g(o,'officeName')">{{ g(o,'officeName') }}</div>
                <div class="pnl-bar-track">
                  <div class="pnl-bar-fill"
                    :style="{
                      width: Math.min(100, Math.abs(Number(g(o,'dailyProfitLoss')??0)) / maxPnL * 100) + '%',
                      background: plColor(Number(g(o,'dailyProfitLoss')??0))
                    }"
                  ></div>
                </div>
                <div class="pnl-val" :style="{color:plColor(Number(g(o,'dailyProfitLoss')??0))}">
                  {{ plSign(Number(g(o,'dailyProfitLoss')??0)) }}{{ fmtNum(Number(g(o,'dailyProfitLoss')??0)) }} ₺
                </div>
              </div>
              <p v-if="!offices.length" class="empty-msg">Şube verisi yok</p>
            </div>
          </div>

          <!-- Pending transfers -->
          <div class="panel">
            <div class="panel-header">
              <span class="material-symbols-outlined">pending_actions</span>
              <h3>Bekleyen Transferler</h3>
              <span v-if="pending.length" class="panel-badge red">{{ pending.length }}</span>
            </div>
            <div class="pending-list">
              <template v-if="pending.length">
                <div v-for="t in pending.slice(0, 6)" :key="g(t,'transferId')||g(t,'id')" class="pending-item">
                  <div class="pi-info">
                    <div class="pi-route">{{ g(t,'sourceOfficeName') ?? g(t,'sourceName') ?? 'Kaynak' }} → {{ g(t,'targetOfficeName') ?? g(t,'targetName') ?? 'Hedef' }}</div>
                    <div class="pi-meta">{{ fmtDate(g(t,'createdAt') ?? g(t,'requestDate')) }}</div>
                  </div>
                  <div class="pi-right">
                    <div class="pi-amt">{{ fmtNum(g(t,'amount')??0) }} {{ g(t,'currencyCode') }}</div>
                    <div class="pi-acts">
                      <button class="pi-ok"
                        :disabled="processingId === (g(t,'transferId')||g(t,'id'))"
                        @click="approveTransfer(g(t,'transferId')||g(t,'id'))">
                        ✓
                      </button>
                      <button class="pi-no"
                        :disabled="processingId === (g(t,'transferId')||g(t,'id'))"
                        @click="promptReject(g(t,'transferId')||g(t,'id'))">
                        ✗
                      </button>
                    </div>
                  </div>
                </div>
                <div v-if="pending.length > 6" class="see-more" @click="router.push('/ihtiyar/office-transfers')">
                  +{{ pending.length - 6 }} daha →
                </div>
              </template>
              <p v-else class="empty-msg">Bekleyen transfer yok</p>
            </div>
          </div>
        </div>

        <!-- Row 2: Transactions | Currency dist (owner) / Party accounts (admin) -->
        <div class="second-row">
          <!-- Transactions -->
          <div class="panel">
            <div class="panel-header">
              <span class="material-symbols-outlined">receipt_long</span>
              <h3>Son İşlemler</h3>
              <button class="see-all" @click="router.push('/ihtiyar/history')">Tümü <span class="material-symbols-outlined">chevron_right</span></button>
            </div>
            <div class="tx-wrap">
              <table v-if="transactions.length" class="tx-table">
                <thead><tr><th>Saat</th><th>Kasa</th><th>Tür</th><th class="r">K/Z</th></tr></thead>
                <tbody>
                  <tr v-for="tx in transactions" :key="tx.id">
                    <td class="nowrap">{{ new Date(tx.transactionDate).toLocaleTimeString('tr-TR',{hour:'2-digit',minute:'2-digit'}) }}</td>
                    <td class="dimmed">{{ g(tx,'vaultName') ?? g(tx,'officeName') ?? '—' }}</td>
                    <td>{{ txType[tx.type] ?? tx.type }}</td>
                    <td class="r" :style="{color:plColor(tx.profit),fontWeight:'600'}">{{ plSign(tx.profit) }}{{ fmtNum(tx.profit) }} ₺</td>
                  </tr>
                </tbody>
              </table>
              <p v-else class="empty-state">Henüz işlem yok</p>
            </div>
          </div>

          <!-- Owner: currency distribution | Admin: party by currency -->
          <div class="panel" v-if="authStore.isOwner && topCurrencies.length">
            <div class="panel-header"><span class="material-symbols-outlined">pie_chart</span><h3>Döviz Dağılımı</h3></div>
            <div class="curr-list">
              <div v-for="c in topCurrencies" :key="c.currencyCode" class="curr-row">
                <span class="curr-code">{{ c.currencyCode }}</span>
                <div class="cr-bar-wrap"><div class="cr-bar" :style="{width:Math.min(100,c.totalValueInBaseCurrency/totalFV*100)+'%'}"></div></div>
                <span class="curr-try">{{ fmtNum(c.totalValueInBaseCurrency) }} ₺</span>
              </div>
            </div>
          </div>
          <div class="panel" v-else-if="!authStore.isOwner && partyByCur.length">
            <div class="panel-header"><span class="material-symbols-outlined">people</span><h3>Cari Hesap Özeti</h3></div>
            <div class="tx-wrap">
              <table class="tx-table">
                <thead><tr><th>Döviz</th><th class="r">Alacak</th><th class="r">Borç</th><th class="r">Net</th></tr></thead>
                <tbody>
                  <tr v-for="c in partyByCur" :key="c.currencyCode">
                    <td><strong>{{ g(c,'currencyCode') }}</strong></td>
                    <td class="r" style="color:#16a34a">{{ fmtNum(g(c,'totalReceivables')??0) }}</td>
                    <td class="r" style="color:#dc2626">{{ fmtNum(g(c,'totalPayables')??0) }}</td>
                    <td class="r" :style="{color:plColor(Number(g(c,'netBalance')??0)),fontWeight:'700'}">
                      {{ plSign(Number(g(c,'netBalance')??0)) }}{{ fmtNum(Number(g(c,'netBalance')??0)) }}
                    </td>
                  </tr>
                </tbody>
              </table>
              <div v-if="topRecv.length" class="top-recv">
                <p class="recv-title">En Yüksek Alacaklar</p>
                <div v-for="p in topRecv" :key="p.partyCode" class="recv-row">
                  <span class="recv-name">{{ p.partyName }}</span>
                  <span style="color:#22c55e;font-weight:700">{{ fmtNum(p.balance) }} ₺</span>
                </div>
              </div>
            </div>
          </div>
          <div class="panel" v-else>
            <div class="panel-header"><span class="material-symbols-outlined">people</span><h3>Cari Hesap Özeti</h3></div>
            <p class="empty-state">Cari hesap verisi yok</p>
          </div>
        </div>

        <!-- Quick nav -->
        <div class="panel quick-nav">
          <div class="panel-header"><span class="material-symbols-outlined">grid_view</span><h3>Hızlı Erişim</h3></div>
          <div class="qn-grid">
            <button v-for="l in adminQuickLinks" :key="l.p" class="qn-btn" @click="router.push(l.p)">
              <span class="material-symbols-outlined">{{ l.i }}</span><span>{{ l.l }}</span>
            </button>
          </div>
        </div>
      </template>
    </template>

    <!-- Reject reason dialog -->
    <div v-if="rejectTarget" class="dialog-overlay" @click.self="rejectTarget = null">
      <div class="dialog">
        <h3>Transferi Reddet</h3>
        <p>Red nedeni (opsiyonel):</p>
        <input v-model="rejectReason" class="dialog-input" placeholder="Açıklama..." @keyup.enter="confirmReject" />
        <div class="dialog-acts">
          <button class="dialog-cancel" @click="rejectTarget = null">Vazgeç</button>
          <button class="dialog-confirm" @click="confirmReject">Reddet</button>
        </div>
      </div>
    </div>

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

/* KPI */
.kpi-grid       { display:grid; grid-template-columns:repeat(auto-fill,minmax(140px,1fr)); gap:12px; }
.kpi-grid-small { display:grid; grid-template-columns:repeat(2,1fr); gap:12px; max-width:400px; }
.kpi-card { background:var(--kb); border:1px solid rgba(0,0,0,.06); border-radius:14px; padding:16px; display:flex; align-items:center; gap:12px; transition:.15s; }
.kpi-card:hover { transform:translateY(-2px); box-shadow:0 4px 16px rgba(0,0,0,.08); }
.kpi-icon { width:42px; height:42px; display:flex; align-items:center; justify-content:center; border-radius:10px; background:white; flex-shrink:0; }
.kpi-icon .material-symbols-outlined { font-size:22px; color:var(--kc); }
.kpi-label { font-size:10px; color:#6b7280; margin:0 0 3px; font-weight:600; text-transform:uppercase; letter-spacing:.04em; }
.kpi-value { font-size:17px; font-weight:800; color:#111; margin:0; }
.kpi-unit  { font-size:11px; font-weight:400; color:#9ca3af; }

/* Panels */
.panel { background:#fff; border:1px solid #e5e7eb; border-radius:14px; overflow:hidden; }
.panel-header { display:flex; align-items:center; gap:8px; padding:14px 18px; border-bottom:1px solid #f3f4f6; }
.panel-header h3 { margin:0; font-size:14px; font-weight:700; color:#111; flex:1; }
.panel-header .material-symbols-outlined { font-size:20px; color:#6366f1; }
.panel-badge     { font-size:11px; background:#ede9fe; color:#6366f1; padding:2px 8px; border-radius:12px; font-weight:600; }
.panel-badge.red { background:#fef2f2; color:#dc2626; }

/* Grids */
.main-row { display:grid; gap:16px; }
.main-row.two-col   { grid-template-columns:1fr 2fr; }
.main-row.three-col { grid-template-columns:1fr 1fr 1fr; }
@media(max-width:1100px){.main-row.three-col{grid-template-columns:1fr 1fr}.main-row.two-col{grid-template-columns:1fr}}
@media(max-width:700px){.main-row.three-col,.main-row.two-col{grid-template-columns:1fr}}
.second-row { display:grid; grid-template-columns:1fr 1fr; gap:16px; }
@media(max-width:900px){.second-row{grid-template-columns:1fr}}

/* Rates */
.rates-table { max-height:300px; overflow-y:auto; }
.rates-header { display:grid; grid-template-columns:56px 1fr 1fr 1fr; padding:8px 16px; background:#f9fafb; font-size:11px; font-weight:700; color:#6b7280; text-transform:uppercase; position:sticky; top:0; }
.rate-row { display:grid; grid-template-columns:56px 1fr 1fr 1fr; padding:7px 16px; border-bottom:1px solid #f9fafb; font-size:13px; }
.rate-row.three-col { grid-template-columns:56px 1fr 1fr; }
.rate-row:hover { background:#f9fafb; }
.rate-code   { font-weight:700; color:#374151; }
.rate-buy    { color:#3b82f6; font-weight:600; }
.rate-sell   { color:#ef4444; font-weight:600; }
.rate-spread { color:#10b981; font-size:11px; }

/* Office P&L bars */
.pnl-list { padding:12px 16px; display:flex; flex-direction:column; gap:8px; }
.pnl-row  { display:flex; align-items:center; gap:8px; }
.pnl-name { font-size:12px; color:#475569; font-weight:500; min-width:80px; max-width:100px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
.pnl-bar-track { flex:1; height:7px; background:#f1f5f9; border-radius:4px; overflow:hidden; }
.pnl-bar-fill  { height:100%; border-radius:4px; transition:width .4s; }
.pnl-val { font-size:11px; font-weight:700; min-width:80px; text-align:right; }

/* Pending transfers */
.pending-list { display:flex; flex-direction:column; padding:4px 0; max-height:300px; overflow-y:auto; }
.pending-item { display:flex; align-items:flex-start; justify-content:space-between; gap:8px; padding:10px 16px; border-bottom:1px solid #f9fafb; }
.pi-info { flex:1; min-width:0; }
.pi-route { font-size:13px; font-weight:600; color:#1e293b; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
.pi-meta  { font-size:11px; color:#94a3b8; margin-top:2px; }
.pi-right { display:flex; flex-direction:column; align-items:flex-end; gap:4px; flex-shrink:0; }
.pi-amt   { font-size:13px; font-weight:700; color:#1e293b; white-space:nowrap; }
.pi-acts  { display:flex; gap:4px; }
.pi-ok, .pi-no { width:28px; height:22px; border:none; border-radius:5px; font-size:12px; font-weight:700; cursor:pointer; }
.pi-ok { background:#22c55e; color:#fff; }
.pi-no { background:#ef4444; color:#fff; }
.pi-ok:disabled, .pi-no:disabled { opacity:.4; cursor:not-allowed; }
.see-more { text-align:center; padding:8px; font-size:12px; color:#6366f1; cursor:pointer; font-weight:600; }
.see-more:hover { text-decoration:underline; }

/* Transactions */
.tx-wrap { overflow-x:auto; }
.tx-table { width:100%; border-collapse:collapse; font-size:12px; }
.tx-table th { background:#f9fafb; padding:9px 12px; text-align:left; font-weight:700; color:#374151; border-bottom:1px solid #e5e7eb; white-space:nowrap; }
.tx-table td { padding:9px 12px; border-bottom:1px solid #f3f4f6; }
.tx-table tr:hover td { background:#f9fafb; }
.r { text-align:right; }
.nowrap { white-space:nowrap; }
.dimmed { color:#6b7280; }

/* Currency distribution */
.curr-list { padding:12px 16px; display:flex; flex-direction:column; gap:8px; }
.curr-row  { display:flex; align-items:center; gap:8px; }
.curr-code { font-weight:700; font-size:13px; color:#374151; width:48px; flex-shrink:0; }
.cr-bar-wrap { flex:1; background:#f3f4f6; border-radius:4px; height:6px; }
.cr-bar { height:6px; border-radius:4px; background:linear-gradient(90deg,#6366f1,#8b5cf6); transition:width .4s; }
.curr-try { font-size:12px; color:#374151; font-weight:600; width:100px; text-align:right; }

/* Party top receivables */
.top-recv { padding:8px 16px 12px; border-top:1px solid #f3f4f6; }
.recv-title { font-size:11px; font-weight:700; color:#6b7280; text-transform:uppercase; margin:0 0 6px; }
.recv-row { display:flex; justify-content:space-between; align-items:center; padding:5px 0; border-bottom:1px solid #f3f4f6; font-size:12px; }
.recv-row:last-child { border-bottom:none; }
.recv-name { color:#374151; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; max-width:180px; }

/* Quick nav */
.quick-nav .panel-header { border-bottom:1px solid #f3f4f6; }
.qn-grid { display:grid; grid-template-columns:repeat(auto-fill,minmax(80px,1fr)); gap:10px; padding:14px 18px; }
.qn-btn { display:flex; flex-direction:column; align-items:center; gap:6px; padding:12px 6px; background:#fafafa; border:1px solid #e5e7eb; border-radius:12px; cursor:pointer; font-size:11px; font-weight:600; color:#374151; transition:.15s; }
.qn-btn:hover { transform:translateY(-2px); box-shadow:0 4px 12px rgba(0,0,0,.1); background:white; border-color:#a5b4fc; }
.qn-btn .material-symbols-outlined { font-size:22px; color:#6366f1; }

/* Dialog */
.dialog-overlay { position:fixed; inset:0; background:rgba(0,0,0,.4); display:flex; align-items:center; justify-content:center; z-index:9999; }
.dialog { background:#fff; border-radius:14px; padding:24px; width:360px; box-shadow:0 20px 40px rgba(0,0,0,.2); }
.dialog h3 { margin:0 0 12px; font-size:16px; font-weight:700; color:#111; }
.dialog p  { margin:0 0 8px; font-size:13px; color:#6b7280; }
.dialog-input { width:100%; box-sizing:border-box; padding:10px 12px; border:1px solid #e5e7eb; border-radius:8px; font-size:14px; outline:none; margin-bottom:16px; }
.dialog-input:focus { border-color:#6366f1; }
.dialog-acts { display:flex; gap:10px; justify-content:flex-end; }
.dialog-cancel  { padding:8px 20px; background:#f3f4f6; border:none; border-radius:8px; cursor:pointer; font-weight:600; font-size:13px; }
.dialog-confirm { padding:8px 20px; background:#ef4444; color:#fff; border:none; border-radius:8px; cursor:pointer; font-weight:600; font-size:13px; }

.empty-msg   { padding:24px; text-align:center; color:#9ca3af; font-size:13px; margin:0; }
.empty-state { padding:40px; text-align:center; color:#9ca3af; font-size:14px; }
.see-all { display:flex; align-items:center; gap:2px; background:none; border:none; cursor:pointer; color:#6366f1; font-size:13px; font-weight:600; padding:4px 8px; border-radius:6px; margin-left:auto; }
.see-all:hover { background:#ede9fe; }
.see-all .material-symbols-outlined { font-size:16px; }
</style>
