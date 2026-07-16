<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, useTemplateRef } from 'vue'
import apiService from '@/services/apiservice'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const loading = ref(true)

const dashboard = ref<any>({
  balance: 0, given_tl: 0, usdt: 0, rub: 0,
  dealer_name: '', dealer_code: '', transactions: [], dealer: {}
})

const filter = ref('all')
const filteredTx = computed(() => {
  const txs = dashboard.value.transactions || []
  if (filter.value === 'all') return txs
  if (filter.value === 'today') {
    const today = new Date().toISOString().slice(0, 10)
    return txs.filter((t: any) => t.createdAt?.startsWith(today))
  }
  return txs.filter((t: any) => t.status === filter.value)
})

const BOT_USERNAME = 'MoneyExchangeTurkeyBot'

const telegramDeepLink = computed(() =>
  `https://t.me/${BOT_USERNAME}?start=${dashboard.value.dealer_code || ''}`
)

const qrImageUrl = computed(() => {
  const link = telegramDeepLink.value
  return `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(link)}`
})

const qrImg = useTemplateRef<HTMLImageElement>('qrImg')

async function downloadQR() {
  try {
    const url = `https://api.qrserver.com/v1/create-qr-code/?size=600x600&data=${encodeURIComponent(telegramDeepLink.value)}`
    const res = await fetch(url)
    const blob = await res.blob()
    const a = document.createElement('a')
    a.href = URL.createObjectURL(blob)
    a.download = `qr-${dashboard.value.dealer_code || 'dealer'}.png`
    a.click()
    URL.revokeObjectURL(a.href)
  } catch (e) { console.error('QR download error:', e) }
}

const cryptoDeposits = ref<any[]>([])
const cariData = ref<any>({ balance: 0, balanceType: 'settled', entries: [] })
const cariLoaded = ref(false)

let refreshInterval: number | null = null
let eventSource: EventSource | null = null
const sseConnected = ref(false)

function connectSSE() {
  const token = localStorage.getItem('token')
  if (!token) return

  const baseUrl = import.meta.env.DEV ? 'http://localhost:5093/api/v1' : 'https://api.baskentenerji.com/api/v1'
  eventSource = new EventSource(`${baseUrl}/tg/events?token=${token}`)

  eventSource.onopen = () => { sseConnected.value = true }

  eventSource.addEventListener('transaction_update', () => {
    loadData()
  })

  eventSource.onerror = () => {
    sseConnected.value = false
    if (eventSource && eventSource.readyState === EventSource.CLOSED) {
      eventSource.close()
      setTimeout(connectSSE, 5000)
    }
  }
}

function disconnectSSE() {
  sseConnected.value = false
  if (eventSource) { eventSource.close(); eventSource = null }
}

async function loadData() {
  try {
    const [dashRes, depositRes] = await Promise.all([
      apiService.get('/tg/dealer/dashboard'),
      apiService.get('/tg/dealer/crypto-deposits').catch(() => ({ deposits: [] }))
    ])
    dashboard.value = dashRes
    cryptoDeposits.value = depositRes?.deposits ?? []
    if (dashRes?.dealer_code && !cariLoaded.value) {
      loadCariData(dashRes.dealer_code)
    }
  } catch (e) { console.error('Dealer dashboard error:', e) }
  finally { loading.value = false }
}

async function loadCariData(code: string) {
  try {
    const res = await apiService.getTgCariEntries(code)
    cariData.value = res ?? cariData.value
    cariLoaded.value = true
  } catch { /* cari hesap henüz yoksa sessiz geç */ }
}

function formatMoney(n: number | null) {
  if (n == null) return '0.00'
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(d: string | null) {
  if (!d) return '—'
  return new Date(d).toLocaleString('tr-TR')
}

function statusColor(status: string) {
  const map: Record<string, string> = {
    completed: '#10b981', approved: '#3b82f6', pending: '#f59e0b',
    cancelled: '#ef4444', rejected: '#ef4444'
  }
  return map[status] || '#6b7280'
}

const handleVisibilityChange = () => {
  if (document.hidden) {
    if (refreshInterval) { clearInterval(refreshInterval); refreshInterval = null }
    disconnectSSE()
  } else {
    loadData()
    refreshInterval = window.setInterval(loadData, 30000)
    connectSSE()
  }
}

onMounted(() => {
  loadData()
  refreshInterval = window.setInterval(loadData, 30000)
  connectSSE()
  document.addEventListener('visibilitychange', handleVisibilityChange)
})

onUnmounted(() => {
  if (refreshInterval) clearInterval(refreshInterval)
  disconnectSSE()
  document.removeEventListener('visibilitychange', handleVisibilityChange)
})
</script>

<template>
  <div class="tg-dealer">
    <div v-if="loading" class="tg-loading">
      <span class="material-symbols-outlined spin">progress_activity</span>
      Yükleniyor...
    </div>

    <template v-else>
      <!-- Header -->
      <div class="dealer-header">
        <div class="dealer-info">
          <span class="material-symbols-outlined" aria-hidden="true" style="font-size: 28px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">storefront</span>
          <div>
            <div class="dealer-name">{{ dashboard.dealer_name }}</div>
            <div class="dealer-code">Bayi Kodu: <code>{{ dashboard.dealer_code || '—' }}</code></div>
            <div v-if="dashboard.staff_name && dashboard.staff_name !== dashboard.dealer_name" class="dealer-staff">Personel: {{ dashboard.staff_name }}</div>
          </div>
        </div>
        <button type="button" class="logout-btn" @click="authStore.logout()">
          <span class="material-symbols-outlined" aria-hidden="true">logout</span>
          Çıkış Yap
        </button>
      </div>

      <!-- Summary Cards -->
      <div class="summary-grid">
        <div class="summary-card">
          <div class="summary-icon" style="color: #10b981">
            <span class="material-symbols-outlined" aria-hidden="true">account_balance_wallet</span>
          </div>
          <div class="summary-value">₺{{ formatMoney(dashboard.balance) }}</div>
          <div class="summary-label">Bakiye TL</div>
        </div>
        <div class="summary-card">
          <div class="summary-icon" style="color: #f59e0b">
            <span class="material-symbols-outlined" aria-hidden="true">payments</span>
          </div>
          <div class="summary-value">₺{{ formatMoney(dashboard.given_tl) }}</div>
          <div class="summary-label">Verilen Toplam TL</div>
        </div>
        <div class="summary-card">
          <div class="summary-icon" style="color: #3b82f6">
            <span class="material-symbols-outlined" aria-hidden="true">token</span>
          </div>
          <div class="summary-value">${{ formatMoney(dashboard.usdt) }}</div>
          <div class="summary-label">Alınan USDT</div>
        </div>
        <div class="summary-card">
          <div class="summary-icon" style="color: #8b5cf6">
            <span class="material-symbols-outlined" aria-hidden="true">currency_ruble</span>
          </div>
          <div class="summary-value">₽{{ formatMoney(dashboard.rub) }}</div>
          <div class="summary-label">Alınan RUB</div>
        </div>
      </div>

      <!-- QR Code -->
      <div v-if="dashboard.dealer_code" class="qr-section">
        <div class="qr-section-header">
          <div class="section-title" style="margin: 0">
            <span class="material-symbols-outlined" aria-hidden="true">qr_code</span>
            Telegram Bot QR Kodu
          </div>
          <button class="qr-download-btn" @click="downloadQR">
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size: 16px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">download</span>
            İndir
          </button>
        </div>
        <div class="qr-body">
          <div class="qr-image-wrap">
            <img :src="qrImageUrl" alt="Telegram Bot QR" class="qr-image" crossorigin="anonymous" ref="qrImg" />
          </div>
          <div class="qr-details">
            <div class="qr-link-label">Deep Link</div>
            <div class="qr-link-value">
              <a :href="telegramDeepLink" target="_blank" rel="noopener">{{ telegramDeepLink }}</a>
            </div>
            <div class="qr-link-label" style="margin-top: 8px">Referral Kodu</div>
            <div class="qr-link-value"><code>{{ dashboard.dealer_code }}</code></div>
            <div class="qr-hint">Müşterileriniz bu QR kodu okutarak bot'a yönlendirilir ve işlemleri sizin bayinize bağlanır.</div>
          </div>
        </div>
      </div>

      <!-- Kripto Özeti -->
      <div v-if="dashboard.crypto_summary?.total_usdt_tx > 0 || cryptoDeposits.length > 0" class="crypto-overview">
        <div class="section-title">
          <span class="material-symbols-outlined" aria-hidden="true">currency_bitcoin</span>
          Kripto Özeti
        </div>
        <div class="crypto-stats">
          <div class="crypto-stat">
            <span class="crypto-stat-val">{{ dashboard.crypto_summary?.total_usdt_tx || 0 }}</span>
            <span class="crypto-stat-label">USDT İşlem</span>
          </div>
          <div class="crypto-stat">
            <span class="crypto-stat-val">{{ dashboard.crypto_summary?.completed_usdt_tx || 0 }}</span>
            <span class="crypto-stat-label">Tamamlanan</span>
          </div>
          <div class="crypto-stat">
            <span class="crypto-stat-val">${{ formatMoney(dashboard.crypto_summary?.total_usdt_amount || 0) }}</span>
            <span class="crypto-stat-label">Toplam USDT</span>
          </div>
        </div>

        <div v-if="cryptoDeposits.length" class="tg-table-wrap" style="margin-top: 10px">
          <table class="tg-table">
            <thead>
              <tr>
                <th>ID</th><th>TXID</th><th>Tutar</th><th>Ağ</th><th>Onay</th><th>Durum</th><th>Tarih</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="d in cryptoDeposits" :key="d.id">
                <td>#{{ d.id }}</td>
                <td class="txid-cell" :title="d.txid">{{ d.txid ? (d.txid.substring(0, 10) + '...') : '—' }}</td>
                <td>{{ formatMoney(d.amount) }}</td>
                <td>{{ d.network || '—' }}</td>
                <td>{{ d.confirmations ?? 0 }}</td>
                <td><span class="status-badge" :style="{ background: statusColor(d.status) }">{{ d.status }}</span></td>
                <td>{{ formatDate(d.createdAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Cari Hesap -->
      <div v-if="cariLoaded && cariData.entries?.length > 0" class="cari-section">
        <div class="section-title" style="margin-top: 0">
          <span class="material-symbols-outlined" aria-hidden="true">account_balance_wallet</span>
          Cari Hesap
          <span class="cari-badge" :class="cariData.balanceType">
            ₺{{ formatMoney(Math.abs(cariData.balance)) }}
            {{ cariData.balanceType === 'payable' ? '(Borçlu)' : cariData.balanceType === 'receivable' ? '(Alacaklı)' : '(Kapalı)' }}
          </span>
        </div>
        <div class="tg-table-wrap" style="margin-top: 10px">
          <table class="tg-table">
            <thead>
              <tr><th>Tarih</th><th>Açıklama</th><th>Borç</th><th>Alacak</th><th>Bakiye</th><th>Durum</th></tr>
            </thead>
            <tbody>
              <tr v-for="e in cariData.entries.slice(0, 20)" :key="e.id">
                <td>{{ formatDate(e.entryDate) }}</td>
                <td>{{ e.description || '—' }}</td>
                <td class="cari-debit">{{ e.debit != null ? '₺' + formatMoney(e.debit) : '' }}</td>
                <td class="cari-credit">{{ e.credit != null ? '₺' + formatMoney(e.credit) : '' }}</td>
                <td :class="{ 'cari-neg': e.runningBalance < 0, 'cari-pos': e.runningBalance > 0 }">
                  ₺{{ formatMoney(Math.abs(e.runningBalance)) }}
                  <span style="font-size:10px;opacity:0.7">{{ e.runningBalance < 0 ? '(B)' : e.runningBalance > 0 ? '(A)' : '' }}</span>
                </td>
                <td><span class="status-badge" :style="{ background: e.paymentStatus === 'Paid' ? '#10b981' : e.paymentStatus === 'Pending' ? '#f59e0b' : '#6b7280' }">{{ e.paymentStatus === 'Paid' ? 'Ödendi' : e.paymentStatus === 'Pending' ? 'Bekliyor' : e.paymentStatus }}</span></td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Filter & Transactions -->
      <div class="section-title" style="margin-top: 20px">
        <span class="material-symbols-outlined" aria-hidden="true">receipt_long</span>
        İşlemler
      </div>
      <div class="filter-bar">
        <button v-for="f in [
          { key: 'all', label: 'Tümü' },
          { key: 'completed', label: 'Tamamlanan' },
          { key: 'cancelled', label: 'İptal Edilen' },
          { key: 'today', label: 'Bugün' },
        ]" :key="f.key"
          class="filter-btn" :class="{ active: filter === f.key }" @click="filter = f.key">
          {{ f.label }}
        </button>
      </div>

      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead>
            <tr>
              <th>ID</th><th>Para</th><th>Tutar</th><th>TL Tutar</th><th>Kur</th><th>Tür</th><th>Durum</th><th>Tarih</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="tx in filteredTx" :key="tx.id">
              <td>#{{ tx.id }}</td>
              <td>{{ tx.currency }}</td>
              <td>{{ formatMoney(tx.amount) }}</td>
              <td>₺{{ formatMoney(tx.tlAmount) }}</td>
              <td>{{ tx.exchangeRate }}</td>
              <td>{{ tx.isBuy ? 'Alım' : 'Satım' }}</td>
              <td><span class="status-badge" :style="{ background: statusColor(tx.status) }">{{ tx.status }}</span></td>
              <td>{{ formatDate(tx.createdAt) }}</td>
            </tr>
            <tr v-if="!filteredTx.length">
              <td colspan="8" class="empty-msg">İşlem bulunamadı</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Auto refresh indicator -->
      <div class="refresh-indicator">
        <span class="live-dot" :class="{ on: sseConnected }"></span>
        {{ sseConnected ? 'Canlı bağlantı aktif — yeni işlemler anında görünür' : '30 saniyede bir otomatik güncellenir' }}
      </div>
    </template>
  </div>
</template>

<style scoped>
.tg-dealer { padding: 16px; }
.tg-loading { display: flex; align-items: center; justify-content: center; gap: 8px; padding: 60px; color: var(--color-text-secondary, #6b7280); }

.dealer-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.dealer-info { display: flex; align-items: center; gap: 12px; }
.dealer-name { font-size: 18px; font-weight: 700; color: var(--color-text, #1f2937); }
.dealer-code { font-size: 13px; color: var(--color-text-secondary, #6b7280); }
.dealer-code code { background: var(--color-hover, #f3f4f6); padding: 2px 8px; border-radius: var(--radius-sm); font-weight: 600; }
.dealer-staff { font-size: 12px; color: var(--color-text-secondary, #9ca3af); margin-top: 2px; }
.logout-btn { display: flex; align-items: center; gap: 6px; padding: 8px 14px; background: var(--color-bg-card, #fff); border: 1px solid var(--color-border); border-radius: var(--radius-md); color: var(--color-text-secondary, #6b7280); font-size: 13px; font-weight: 600; cursor: pointer; transition: background-color 0.2s ease, color 0.2s ease; }
.logout-btn:hover { background: var(--color-danger-bg, #fee2e2); color: var(--color-danger, #dc2626); }
.logout-btn .material-symbols-outlined { font-size: 18px; }

.summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; }
.summary-card { padding: 16px; background: var(--color-bg-card, #fff); border: 1px solid var(--color-border); border-radius: var(--radius-md); text-align: center; box-shadow: var(--shadow-bold); }
.summary-icon { display: inline-flex; align-items: center; justify-content: center; width: 44px; height: 44px; margin: 0 auto; border-radius: var(--radius-md); background: currentColor; box-shadow: 0 3px 8px -2px rgba(0,0,0,.28); }
.summary-icon .material-symbols-outlined { font-size: 28px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; color: #fff; }
.summary-value { font-size: 22px; font-weight: 700; color: var(--color-text, #1f2937); margin: 4px 0; }
.summary-label { font-size: 12px; color: var(--color-text-secondary, #6b7280); }

.qr-section { margin-top: 16px; padding: 16px; background: var(--color-bg-card, #fff); border: 1px solid var(--color-border); border-radius: var(--radius-md); box-shadow: var(--shadow-md); }
.qr-section-header { display: flex; justify-content: space-between; align-items: center; }
.qr-download-btn { display: flex; align-items: center; gap: 4px; padding: 6px 12px; border: 1px solid var(--color-primary, var(--color-secondary-hover)); background: transparent; color: var(--color-primary, var(--color-secondary-hover)); border-radius: var(--radius-sm); font-size: 12px; font-weight: 600; cursor: pointer; transition: background-color 0.15s, color 0.15s, border-color 0.15s; }
.qr-download-btn:hover { background: var(--color-primary, #2563eb); color: #fff; }
.qr-body { display: flex; gap: 20px; margin-top: 14px; align-items: flex-start; }
.qr-image-wrap { flex-shrink: 0; padding: 8px; background: var(--color-bg-card, #fff); border: 1px solid var(--color-border); border-radius: var(--radius-md); box-shadow: var(--shadow-md); }
.qr-image { width: 160px; height: 160px; display: block; }
.qr-details { flex: 1; min-width: 0; }
.qr-link-label { font-size: 11px; font-weight: 600; color: var(--color-text-secondary, #6b7280); text-transform: uppercase; letter-spacing: 0.05em; }
.qr-link-value { font-size: 13px; color: var(--color-text, #1f2937); margin-top: 2px; word-break: break-all; }
.qr-link-value a { color: var(--color-primary, #2563eb); text-decoration: none; }
.qr-link-value a:hover { text-decoration: underline; }
.qr-link-value code { background: var(--color-hover, #f3f4f6); padding: 2px 8px; border-radius: var(--radius-sm); font-weight: 600; font-size: 14px; }
.qr-hint { margin-top: 12px; font-size: 12px; color: var(--color-text-secondary, #6b7280); line-height: 1.5; }

.section-title { display: flex; align-items: center; gap: 8px; margin: 16px 0 10px; padding: 6px 10px 6px 12px; font-size: 14px; font-weight: 600; color: var(--color-text, #1f2937); position: relative; background: linear-gradient(90deg, var(--color-hover, #f3f4f6), transparent); border-bottom: 3px solid var(--border-strong); border-radius: var(--radius-sm) var(--radius-sm) 0 0; }
.section-title::before { content: ''; position: absolute; left: 0; top: 4px; bottom: 4px; width: 5px; border-radius: var(--radius-sm); background: var(--color-primary, #2563eb); }
.section-title .material-symbols-outlined { font-size: 20px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

.filter-bar { display: flex; gap: 6px; margin-bottom: 12px; }
.filter-btn { padding: 6px 12px; border: 1px solid var(--color-border); background: var(--color-bg-card, #fff); border-radius: var(--radius-sm); font-size: 12px; cursor: pointer; color: var(--color-text-secondary, var(--color-text-secondary)); transition: background-color 0.15s, color 0.15s, border-color 0.15s; }
.filter-btn:hover { border-color: var(--color-primary, #2563eb); }
.filter-btn.active { background: var(--color-primary, #2563eb); color: #fff; border-color: var(--color-primary, #2563eb); }

.tg-table-wrap { overflow-x: auto; border-radius: var(--radius-md); border: 1px solid var(--color-border); box-shadow: var(--shadow-md); }
.tg-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.tg-table th { padding: 10px 12px; text-align: left; font-weight: 700; font-size: 11px; text-transform: uppercase; letter-spacing: 0.05em; color: var(--color-text-secondary, #6b7280); background: var(--color-hover, #f9fafb); border-bottom: 2px solid var(--border-strong); }
.tg-table td { padding: 10px 12px; border-bottom: 1px solid var(--color-border, #f3f4f6); color: var(--color-text, #1f2937); }
.tg-table tbody tr:hover { background: var(--color-hover, #f9fafb); }

.status-badge { padding: 3px 8px; border-radius: var(--radius-md); font-size: 11px; font-weight: 600; color: #fff; }
.empty-msg { text-align: center; color: var(--color-text-secondary, #6b7280); padding: 30px !important; }

.crypto-overview { margin-top: 16px; padding: 16px; background: var(--color-bg-card, #fff); border: 1px solid var(--color-border); border-radius: var(--radius-md); box-shadow: var(--shadow-md); }
.crypto-stats { display: flex; gap: 16px; margin-top: 10px; }
.crypto-stat { flex: 1; text-align: center; padding: 12px; background: var(--color-hover, #f9fafb); border-radius: var(--radius-md); }
.crypto-stat-val { display: block; font-size: 18px; font-weight: 700; color: var(--color-text, #1f2937); }
.crypto-stat-label { font-size: 11px; color: var(--color-text-secondary, #6b7280); }
.txid-cell { font-family: monospace; font-size: 12px; }

.cari-section { margin-top: 16px; padding: 16px; background: var(--color-bg-card, #fff); border: 1px solid var(--color-border); border-radius: var(--radius-md); box-shadow: var(--shadow-bold); }
.cari-badge { padding: 3px 8px; border-radius: var(--radius-md); font-size: 11px; font-weight: 600; color: #fff; margin-left: 8px; }
.cari-badge.payable { background: var(--color-warning); }
.cari-badge.receivable { background: var(--color-success); }
.cari-badge.settled { background: #6b7280; }
.cari-debit { color: var(--color-danger); font-weight: 500; }
.cari-credit { color: var(--color-success); font-weight: 500; }
.cari-neg { color: var(--color-warning); font-weight: 600; }
.cari-pos { color: var(--color-success); font-weight: 600; }

.refresh-indicator { display: flex; align-items: center; gap: 6px; justify-content: center; margin-top: 16px; font-size: 11px; color: var(--color-text-secondary, #9ca3af); }
.live-dot { width: 8px; height: 8px; border-radius: 50%; background: #9ca3af; flex-shrink: 0; }
.live-dot.on { background: var(--color-success, #10b981); box-shadow: 0 0 0 3px rgba(16,185,129,0.25); animation: live-pulse 2s ease-in-out infinite; }
@keyframes live-pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }

.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
</style>
