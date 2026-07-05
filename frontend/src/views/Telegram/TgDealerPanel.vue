<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, useTemplateRef } from 'vue'
import apiService from '@/services/apiservice'

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

let refreshInterval: number | null = null

async function loadData() {
  try {
    const [dashRes, depositRes] = await Promise.all([
      apiService.get('/tg/dealer/dashboard'),
      apiService.get('/tg/dealer/crypto-deposits').catch(() => ({ deposits: [] }))
    ])
    dashboard.value = dashRes
    cryptoDeposits.value = depositRes?.deposits ?? []
  } catch (e) { console.error('Dealer dashboard error:', e) }
  finally { loading.value = false }
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

onMounted(() => {
  loadData()
  refreshInterval = window.setInterval(loadData, 30000)
})

onUnmounted(() => {
  if (refreshInterval) clearInterval(refreshInterval)
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
          <span class="material-symbols-outlined" style="font-size: 28px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">storefront</span>
          <div>
            <div class="dealer-name">{{ dashboard.dealer_name }}</div>
            <div class="dealer-code">Bayi Kodu: <code>{{ dashboard.dealer_code || '—' }}</code></div>
            <div v-if="dashboard.staff_name && dashboard.staff_name !== dashboard.dealer_name" class="dealer-staff">Personel: {{ dashboard.staff_name }}</div>
          </div>
        </div>
      </div>

      <!-- Summary Cards -->
      <div class="summary-grid">
        <div class="summary-card">
          <div class="summary-icon" style="color: #10b981">
            <span class="material-symbols-outlined">account_balance_wallet</span>
          </div>
          <div class="summary-value">₺{{ formatMoney(dashboard.balance) }}</div>
          <div class="summary-label">Bakiye TL</div>
        </div>
        <div class="summary-card">
          <div class="summary-icon" style="color: #f59e0b">
            <span class="material-symbols-outlined">payments</span>
          </div>
          <div class="summary-value">₺{{ formatMoney(dashboard.given_tl) }}</div>
          <div class="summary-label">Verilen Toplam TL</div>
        </div>
        <div class="summary-card">
          <div class="summary-icon" style="color: #3b82f6">
            <span class="material-symbols-outlined">token</span>
          </div>
          <div class="summary-value">${{ formatMoney(dashboard.usdt) }}</div>
          <div class="summary-label">Alınan USDT</div>
        </div>
        <div class="summary-card">
          <div class="summary-icon" style="color: #8b5cf6">
            <span class="material-symbols-outlined">currency_ruble</span>
          </div>
          <div class="summary-value">₽{{ formatMoney(dashboard.rub) }}</div>
          <div class="summary-label">Alınan RUB</div>
        </div>
      </div>

      <!-- QR Code -->
      <div v-if="dashboard.dealer_code" class="qr-section">
        <div class="qr-section-header">
          <div class="section-title" style="margin: 0">
            <span class="material-symbols-outlined">qr_code</span>
            Telegram Bot QR Kodu
          </div>
          <button class="qr-download-btn" @click="downloadQR">
            <span class="material-symbols-outlined" style="font-size: 16px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">download</span>
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
          <span class="material-symbols-outlined">currency_bitcoin</span>
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

      <!-- Filter & Transactions -->
      <div class="section-title" style="margin-top: 20px">
        <span class="material-symbols-outlined">receipt_long</span>
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
        <span class="material-symbols-outlined" style="font-size: 14px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">autorenew</span>
        30 saniyede bir otomatik güncellenir
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
.dealer-code code { background: var(--color-hover, #f3f4f6); padding: 2px 8px; border-radius: 4px; font-weight: 600; }
.dealer-staff { font-size: 12px; color: var(--color-text-secondary, #9ca3af); margin-top: 2px; }

.summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; }
.summary-card { padding: 16px; background: var(--color-card, #fff); border: 1px solid var(--color-border, #e5e7eb); border-radius: 10px; text-align: center; }
.summary-icon .material-symbols-outlined { font-size: 28px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.summary-value { font-size: 22px; font-weight: 700; color: var(--color-text, #1f2937); margin: 4px 0; }
.summary-label { font-size: 12px; color: var(--color-text-secondary, #6b7280); }

.qr-section { margin-top: 16px; padding: 16px; background: var(--color-card, #fff); border: 1px solid var(--color-border, #e5e7eb); border-radius: 10px; }
.qr-section-header { display: flex; justify-content: space-between; align-items: center; }
.qr-download-btn { display: flex; align-items: center; gap: 4px; padding: 6px 12px; border: 1px solid var(--color-primary, #2563eb); background: transparent; color: var(--color-primary, #2563eb); border-radius: 6px; font-size: 12px; font-weight: 600; cursor: pointer; transition: all 0.15s; }
.qr-download-btn:hover { background: var(--color-primary, #2563eb); color: #fff; }
.qr-body { display: flex; gap: 20px; margin-top: 14px; align-items: flex-start; }
.qr-image-wrap { flex-shrink: 0; padding: 8px; background: #fff; border: 1px solid var(--color-border, #e5e7eb); border-radius: 8px; }
.qr-image { width: 160px; height: 160px; display: block; }
.qr-details { flex: 1; min-width: 0; }
.qr-link-label { font-size: 11px; font-weight: 600; color: var(--color-text-secondary, #6b7280); text-transform: uppercase; letter-spacing: 0.05em; }
.qr-link-value { font-size: 13px; color: var(--color-text, #1f2937); margin-top: 2px; word-break: break-all; }
.qr-link-value a { color: var(--color-primary, #2563eb); text-decoration: none; }
.qr-link-value a:hover { text-decoration: underline; }
.qr-link-value code { background: var(--color-hover, #f3f4f6); padding: 2px 8px; border-radius: 4px; font-weight: 600; font-size: 14px; }
.qr-hint { margin-top: 12px; font-size: 12px; color: var(--color-text-secondary, #6b7280); line-height: 1.5; }

.section-title { display: flex; align-items: center; gap: 8px; margin: 16px 0 10px; font-size: 14px; font-weight: 600; color: var(--color-text, #1f2937); }
.section-title .material-symbols-outlined { font-size: 20px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

.filter-bar { display: flex; gap: 6px; margin-bottom: 12px; }
.filter-btn { padding: 6px 12px; border: 1px solid var(--color-border, #e5e7eb); background: var(--color-card, #fff); border-radius: 6px; font-size: 12px; cursor: pointer; color: var(--color-text-secondary, #6b7280); transition: all 0.15s; }
.filter-btn:hover { border-color: var(--color-primary, #2563eb); }
.filter-btn.active { background: var(--color-primary, #2563eb); color: #fff; border-color: var(--color-primary, #2563eb); }

.tg-table-wrap { overflow-x: auto; border-radius: 8px; border: 1px solid var(--color-border, #e5e7eb); }
.tg-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.tg-table th { padding: 10px 12px; text-align: left; font-weight: 600; font-size: 11px; text-transform: uppercase; letter-spacing: 0.05em; color: var(--color-text-secondary, #6b7280); background: var(--color-hover, #f9fafb); border-bottom: 1px solid var(--color-border, #e5e7eb); }
.tg-table td { padding: 10px 12px; border-bottom: 1px solid var(--color-border, #f3f4f6); color: var(--color-text, #1f2937); }
.tg-table tbody tr:hover { background: var(--color-hover, #f9fafb); }

.status-badge { padding: 3px 8px; border-radius: 10px; font-size: 11px; font-weight: 600; color: #fff; }
.empty-msg { text-align: center; color: var(--color-text-secondary, #6b7280); padding: 30px !important; }

.crypto-overview { margin-top: 16px; padding: 16px; background: var(--color-card, #fff); border: 1px solid var(--color-border, #e5e7eb); border-radius: 10px; }
.crypto-stats { display: flex; gap: 16px; margin-top: 10px; }
.crypto-stat { flex: 1; text-align: center; padding: 12px; background: var(--color-hover, #f9fafb); border-radius: 8px; }
.crypto-stat-val { display: block; font-size: 18px; font-weight: 700; color: var(--color-text, #1f2937); }
.crypto-stat-label { font-size: 11px; color: var(--color-text-secondary, #6b7280); }
.txid-cell { font-family: monospace; font-size: 12px; }

.refresh-indicator { display: flex; align-items: center; gap: 4px; justify-content: center; margin-top: 16px; font-size: 11px; color: var(--color-text-secondary, #9ca3af); }

.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
</style>
