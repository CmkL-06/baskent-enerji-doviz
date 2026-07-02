<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import apiService from '@/services/apiservice'

const loading = ref(true)
const activeTab = ref('dashboard')

// Dashboard
const dashboard = ref<any>({
  total_tx: 0, total_tl: 0, total_usdt: 0, total_rub: 0,
  total_dealer_balance: 0, recent_transactions: []
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

// Transaction filter
const txFilter = ref('all')
const filteredTransactions = computed(() => {
  if (txFilter.value === 'all') return transactions.value
  return transactions.value.filter(t => t.status === txFilter.value)
})

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
    await apiService.put(`/tg/admin/exchange-rates/${rate.id}`, {
      buyRate: rate.buyRate,
      sellRate: rate.sellRate
    })
    editingRate.value = null
    await loadCryptoDeposits()
  } catch (e) { console.error('Rate update error:', e) }
}

async function createRate() {
  try {
    await apiService.post('/tg/admin/exchange-rates', {
      currency: newRate.value.currency,
      buyRate: newRate.value.buyRate,
      sellRate: newRate.value.sellRate
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

async function retryQueue(queueId: number) {
  try {
    await apiService.post(`/tg/admin/baskent-queue/${queueId}/retry`)
    await loadApiQueue()
  } catch (e) { console.error(e) }
}

async function deleteDealer(userId: string) {
  if (!confirm('Bayiyi silmek istediğinize emin misiniz?')) return
  try {
    await apiService.delete(`/tg/admin/dealers/${userId}`)
    await loadDealers()
  } catch (e) { console.error(e) }
}

async function deleteOperator(userId: string) {
  if (!confirm('Operatörü silmek istediğinize emin misiniz?')) return
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
  else if (tab === 'queue') loadApiQueue()
  else if (tab === 'logs') loadLoginLogs()
}

function formatDate(d: string | null) {
  if (!d) return '—'
  return new Date(d).toLocaleString('tr-TR')
}

function formatMoney(n: number | null) {
  if (n == null) return '0'
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function statusColor(status: string) {
  const map: Record<string, string> = {
    completed: '#10b981', approved: '#3b82f6', pending: '#f59e0b',
    processing: '#8b5cf6', cancelled: '#ef4444', rejected: '#ef4444',
    online: '#10b981', offline: '#ef4444', unknown: '#6b7280'
  }
  return map[status] || '#6b7280'
}

onMounted(async () => {
  await loadDashboard()
  loading.value = false
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
        { key: 'queue', icon: 'queue', label: 'API Kuyruk' },
        { key: 'logs', icon: 'history', label: 'Log\'lar' },
      ]" :key="tab.key"
        class="tg-tab" :class="{ active: activeTab === tab.key }"
        @click="switchTab(tab.key)">
        <span class="material-symbols-outlined">{{ tab.icon }}</span>
        <span class="tab-label">{{ tab.label }}</span>
      </button>
    </div>

    <div v-if="loading" class="tg-loading">
      <span class="material-symbols-outlined spin">progress_activity</span>
      Yükleniyor...
    </div>

    <!-- Dashboard Tab -->
    <div v-else-if="activeTab === 'dashboard'" class="tg-content">
      <!-- Stats Cards -->
      <div class="stat-grid">
        <div class="stat-card">
          <span class="material-symbols-outlined stat-icon">swap_horiz</span>
          <div class="stat-info">
            <div class="stat-value">{{ dashboard.total_tx }}</div>
            <div class="stat-label">Toplam İşlem</div>
          </div>
        </div>
        <div class="stat-card">
          <span class="material-symbols-outlined stat-icon" style="color: #10b981">payments</span>
          <div class="stat-info">
            <div class="stat-value">₺{{ formatMoney(dashboard.total_tl) }}</div>
            <div class="stat-label">Toplam TL</div>
          </div>
        </div>
        <div class="stat-card">
          <span class="material-symbols-outlined stat-icon" style="color: #3b82f6">token</span>
          <div class="stat-info">
            <div class="stat-value">${{ formatMoney(dashboard.total_usdt) }}</div>
            <div class="stat-label">Toplam USDT</div>
          </div>
        </div>
        <div class="stat-card">
          <span class="material-symbols-outlined stat-icon" style="color: #8b5cf6">currency_ruble</span>
          <div class="stat-info">
            <div class="stat-value">₽{{ formatMoney(dashboard.total_rub) }}</div>
            <div class="stat-label">Toplam RUB</div>
          </div>
        </div>
      </div>

      <!-- Period Stats -->
      <div class="stat-grid" style="margin-top: 12px">
        <div class="stat-card mini">
          <div class="stat-value">{{ stats.today_count }}</div>
          <div class="stat-label">Bugün</div>
        </div>
        <div class="stat-card mini">
          <div class="stat-value">{{ stats.week_count }}</div>
          <div class="stat-label">Bu Hafta</div>
        </div>
        <div class="stat-card mini">
          <div class="stat-value">{{ stats.month_count }}</div>
          <div class="stat-label">Bu Ay</div>
        </div>
        <div class="stat-card mini">
          <div class="stat-value">{{ stats.active_dealers }}</div>
          <div class="stat-label">Aktif Bayi</div>
        </div>
      </div>

      <!-- Bot Status -->
      <div class="section-title">
        <span class="material-symbols-outlined">smart_toy</span>
        Bot Durumu
      </div>
      <div class="bot-grid">
        <div v-for="(bot, name) in botStatus" :key="name" class="bot-card">
          <div class="bot-indicator" :style="{ background: statusColor(bot.status) }"></div>
          <div class="bot-info">
            <div class="bot-name">{{ name }}</div>
            <div class="bot-detail">{{ bot.status }} · {{ bot.active_sessions }} oturum</div>
            <div class="bot-detail" v-if="bot.last_heartbeat">Son: {{ formatDate(bot.last_heartbeat) }}</div>
          </div>
        </div>
      </div>

      <!-- Recent Transactions -->
      <div class="section-title">
        <span class="material-symbols-outlined">history</span>
        Son İşlemler
      </div>
      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead>
            <tr>
              <th>ID</th><th>Para</th><th>Tutar</th><th>Kur</th><th>Durum</th><th>Tarih</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="tx in dashboard.recent_transactions" :key="tx.transactionId">
              <td>#{{ tx.transactionId }}</td>
              <td>{{ tx.currency }}</td>
              <td>{{ formatMoney(tx.amount) }}</td>
              <td>{{ tx.exchangeRate }}</td>
              <td><span class="status-badge" :style="{ background: statusColor(tx.status) }">{{ tx.status }}</span></td>
              <td>{{ formatDate(tx.createdAt) }}</td>
            </tr>
            <tr v-if="!dashboard.recent_transactions?.length">
              <td colspan="6" class="empty-msg">Henüz işlem yok</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Transactions Tab -->
    <div v-else-if="activeTab === 'transactions'" class="tg-content">
      <div class="filter-bar">
        <button v-for="f in ['all','pending','processing','approved','completed','cancelled','rejected']" :key="f"
          class="filter-btn" :class="{ active: txFilter === f }" @click="txFilter = f">
          {{ f === 'all' ? 'Tümü' : f }}
        </button>
      </div>
      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead>
            <tr>
              <th>ID</th><th>Müşteri</th><th>Para</th><th>Tutar</th><th>TL</th><th>Kur</th><th>Durum</th><th>Bayi</th><th>Tarih</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="tx in filteredTransactions" :key="tx.id">
              <td>#{{ tx.id }}</td>
              <td>{{ tx.customerName || tx.customerUsername || '—' }}</td>
              <td>{{ tx.currency }}</td>
              <td>{{ formatMoney(tx.amount) }}</td>
              <td>₺{{ formatMoney(tx.tlAmount) }}</td>
              <td>{{ tx.exchangeRate }}</td>
              <td><span class="status-badge" :style="{ background: statusColor(tx.status) }">{{ tx.status }}</span></td>
              <td>{{ tx.referralCode || '—' }}</td>
              <td>{{ formatDate(tx.createdAt) }}</td>
            </tr>
            <tr v-if="!filteredTransactions.length">
              <td colspan="9" class="empty-msg">İşlem bulunamadı</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Dealers Tab -->
    <div v-else-if="activeTab === 'dealers'" class="tg-content">
      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead>
            <tr><th>Kullanıcı</th><th>Ad</th><th>Bayi Kodu</th><th>Durum</th><th>Tarih</th><th></th></tr>
          </thead>
          <tbody>
            <tr v-for="d in dealers" :key="d.id">
              <td>{{ d.username }}</td>
              <td>{{ d.name }}</td>
              <td><code>{{ d.dealer_code }}</code></td>
              <td><span class="status-badge" :style="{ background: d.is_active ? '#10b981' : '#ef4444' }">{{ d.is_active ? 'Aktif' : 'Pasif' }}</span></td>
              <td>{{ formatDate(d.created_at) }}</td>
              <td>
                <button class="icon-btn danger" @click="deleteDealer(d.id)" title="Sil">
                  <span class="material-symbols-outlined">delete</span>
                </button>
              </td>
            </tr>
            <tr v-if="!dealers.length"><td colspan="6" class="empty-msg">Bayi bulunamadı</td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Operators Tab -->
    <div v-else-if="activeTab === 'operators'" class="tg-content">
      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead>
            <tr><th>Kullanıcı</th><th>Ad</th><th>Telegram ID</th><th>Durum</th><th>Tarih</th><th></th></tr>
          </thead>
          <tbody>
            <tr v-for="op in operators" :key="op.id">
              <td>{{ op.username }}</td>
              <td>{{ op.name }}</td>
              <td><code>{{ op.telegram_id }}</code></td>
              <td><span class="status-badge" :style="{ background: op.is_active ? '#10b981' : '#ef4444' }">{{ op.is_active ? 'Aktif' : 'Pasif' }}</span></td>
              <td>{{ formatDate(op.created_at) }}</td>
              <td>
                <button class="icon-btn danger" @click="deleteOperator(op.id)" title="Sil">
                  <span class="material-symbols-outlined">delete</span>
                </button>
              </td>
            </tr>
            <tr v-if="!operators.length"><td colspan="6" class="empty-msg">Operatör bulunamadı</td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Crypto Terminal Tab -->
    <div v-else-if="activeTab === 'crypto'" class="tg-content">
      <!-- Crypto Summary Cards -->
      <div class="stat-grid" style="margin-bottom: 16px">
        <div class="stat-card mini">
          <div class="stat-value" style="color:#3b82f6">{{ cryptoSummary.total_usdt_transactions }}</div>
          <div class="stat-label">USDT İşlem</div>
        </div>
        <div class="stat-card mini">
          <div class="stat-value" style="color:#10b981">{{ cryptoSummary.verified_transactions }}</div>
          <div class="stat-label">Doğrulanmış</div>
        </div>
        <div class="stat-card mini">
          <div class="stat-value" style="color:#f59e0b">{{ cryptoSummary.unverified_transactions }}</div>
          <div class="stat-label">Doğrulanmamış</div>
        </div>
        <div class="stat-card mini">
          <div class="stat-value" style="color:#8b5cf6">{{ cryptoSummary.confirmed_count }}</div>
          <div class="stat-label">Onaylı Deposit</div>
        </div>
      </div>

      <!-- Exchange Rates Management -->
      <div class="crypto-section">
        <div class="crypto-section-header">
          <div class="crypto-section-title">
            <span class="material-symbols-outlined" style="font-size: 18px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">currency_exchange</span>
            Döviz Kurları
          </div>
          <button class="add-btn" @click="showNewRateForm = !showNewRateForm">
            <span class="material-symbols-outlined" style="font-size: 16px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">add</span>
            Kur Ekle
          </button>
        </div>

        <!-- New Rate Form -->
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
              <div class="rate-edit-row">
                <label>Alış</label>
                <input v-model.number="editingRate.buyRate" type="number" step="0.01" class="rate-input sm" />
              </div>
              <div class="rate-edit-row">
                <label>Satış</label>
                <input v-model.number="editingRate.sellRate" type="number" step="0.01" class="rate-input sm" />
              </div>
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
                <span class="material-symbols-outlined" style="font-size: 14px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">edit</span>
                Düzenle
              </button>
            </template>
          </div>
          <div v-if="!exchangeRates.length" class="empty-msg" style="padding: 20px; text-align: center">Kur tanımlanmamış</div>
        </div>
      </div>

      <!-- Crypto Deposits Table -->
      <div class="crypto-section" style="margin-top: 16px">
        <div class="crypto-section-title">
          <span class="material-symbols-outlined" style="font-size: 18px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">account_balance</span>
          Kripto Deposit Geçmişi
        </div>
        <div class="tg-table-wrap" style="margin-top: 10px">
          <table class="tg-table">
            <thead>
              <tr><th>ID</th><th>İşlem</th><th>TXID</th><th>Tutar</th><th>Ağ</th><th>Onay</th><th>Durum</th><th>Tarih</th></tr>
            </thead>
            <tbody>
              <tr v-for="dep in cryptoDeposits" :key="dep.id">
                <td>#{{ dep.id }}</td>
                <td>#{{ dep.transactionId }}</td>
                <td class="txid-cell">{{ dep.txid || '—' }}</td>
                <td>{{ formatMoney(dep.amount) }}</td>
                <td>{{ dep.network || '—' }}</td>
                <td>{{ dep.confirmations ?? '—' }}</td>
                <td><span class="status-badge" :style="{ background: statusColor(dep.status) }">{{ dep.status }}</span></td>
                <td>{{ formatDate(dep.createdAt) }}</td>
              </tr>
              <tr v-if="!cryptoDeposits.length"><td colspan="8" class="empty-msg">Kripto deposit bulunamadı</td></tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- API Queue Tab -->
    <div v-else-if="activeTab === 'queue'" class="tg-content">
      <div class="stat-grid" style="margin-bottom: 16px">
        <div class="stat-card mini"><div class="stat-value" style="color:#f59e0b">{{ apiQueue.summary.pending }}</div><div class="stat-label">Bekleyen</div></div>
        <div class="stat-card mini"><div class="stat-value" style="color:#ef4444">{{ apiQueue.summary.failed }}</div><div class="stat-label">Başarısız</div></div>
        <div class="stat-card mini"><div class="stat-value" style="color:#10b981">{{ apiQueue.summary.success }}</div><div class="stat-label">Başarılı</div></div>
      </div>
      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead>
            <tr><th>ID</th><th>İşlem</th><th>Durum</th><th>Deneme</th><th>Hata</th><th>Tarih</th><th></th></tr>
          </thead>
          <tbody>
            <tr v-for="q in apiQueue.items" :key="q.queueId">
              <td>#{{ q.queueId }}</td>
              <td>#{{ q.transactionId }}</td>
              <td><span class="status-badge" :style="{ background: statusColor(q.status) }">{{ q.status }}</span></td>
              <td>{{ q.attempts }}/{{ q.maxAttempts }}</td>
              <td class="error-cell">{{ q.lastError || '—' }}</td>
              <td>{{ formatDate(q.createdAt) }}</td>
              <td>
                <button v-if="q.status === 'failed'" class="icon-btn" @click="retryQueue(q.queueId)" title="Tekrar Dene">
                  <span class="material-symbols-outlined">refresh</span>
                </button>
              </td>
            </tr>
            <tr v-if="!apiQueue.items?.length"><td colspan="7" class="empty-msg">Kuyruk boş</td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Login Logs Tab -->
    <div v-else-if="activeTab === 'logs'" class="tg-content">
      <div class="tg-table-wrap">
        <table class="tg-table">
          <thead>
            <tr><th>Kullanıcı</th><th>IP</th><th>Panel</th><th>Durum</th><th>Tarih</th></tr>
          </thead>
          <tbody>
            <tr v-for="log in loginLogs" :key="log.id">
              <td>{{ log.username }}</td>
              <td>{{ log.ipAddress }}</td>
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

.tg-tabs {
  display: flex;
  gap: 4px;
  padding: 12px 16px;
  border-bottom: 1px solid var(--color-border, #e5e7eb);
  overflow-x: auto;
  background: var(--color-card, #fff);
}
.tg-tab {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border: none;
  background: transparent;
  color: var(--color-text-secondary, #6b7280);
  border-radius: 8px;
  cursor: pointer;
  font-size: 13px;
  white-space: nowrap;
  transition: all 0.15s;
}
.tg-tab .material-symbols-outlined { font-size: 18px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.tg-tab:hover { background: var(--color-hover, #f3f4f6); color: var(--color-text, #1f2937); }
.tg-tab.active { background: var(--color-primary, #2563eb); color: #fff; }

.tg-content { padding: 16px; }
.tg-loading { display: flex; align-items: center; justify-content: center; gap: 8px; padding: 60px; color: var(--color-text-secondary, #6b7280); }

.stat-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 12px; }
.stat-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
  background: var(--color-card, #fff);
  border: 1px solid var(--color-border, #e5e7eb);
  border-radius: 10px;
}
.stat-card.mini { flex-direction: column; text-align: center; padding: 12px; }
.stat-icon { font-size: 32px; color: var(--color-primary, #2563eb); font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }
.stat-value { font-size: 20px; font-weight: 700; color: var(--color-text, #1f2937); }
.stat-card.mini .stat-value { font-size: 24px; }
.stat-label { font-size: 12px; color: var(--color-text-secondary, #6b7280); }

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 20px 0 10px;
  font-size: 14px;
  font-weight: 600;
  color: var(--color-text, #1f2937);
}
.section-title .material-symbols-outlined { font-size: 20px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

.bot-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 10px; }
.bot-card { display: flex; gap: 10px; align-items: center; padding: 12px; background: var(--color-card, #fff); border: 1px solid var(--color-border, #e5e7eb); border-radius: 8px; }
.bot-indicator { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
.bot-name { font-weight: 600; font-size: 13px; color: var(--color-text, #1f2937); }
.bot-detail { font-size: 11px; color: var(--color-text-secondary, #6b7280); }

.tg-table-wrap { overflow-x: auto; border-radius: 8px; border: 1px solid var(--color-border, #e5e7eb); }
.tg-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.tg-table th { padding: 10px 12px; text-align: left; font-weight: 600; font-size: 11px; text-transform: uppercase; letter-spacing: 0.05em; color: var(--color-text-secondary, #6b7280); background: var(--color-hover, #f9fafb); border-bottom: 1px solid var(--color-border, #e5e7eb); }
.tg-table td { padding: 10px 12px; border-bottom: 1px solid var(--color-border, #f3f4f6); color: var(--color-text, #1f2937); }
.tg-table tbody tr:hover { background: var(--color-hover, #f9fafb); }

.status-badge { padding: 3px 8px; border-radius: 10px; font-size: 11px; font-weight: 600; color: #fff; }
.empty-msg { text-align: center; color: var(--color-text-secondary, #6b7280); padding: 30px !important; }

.filter-bar { display: flex; gap: 6px; margin-bottom: 12px; flex-wrap: wrap; }
.filter-btn { padding: 6px 12px; border: 1px solid var(--color-border, #e5e7eb); background: var(--color-card, #fff); border-radius: 6px; font-size: 12px; cursor: pointer; color: var(--color-text-secondary, #6b7280); transition: all 0.15s; }
.filter-btn:hover { border-color: var(--color-primary, #2563eb); }
.filter-btn.active { background: var(--color-primary, #2563eb); color: #fff; border-color: var(--color-primary, #2563eb); }

.icon-btn { border: none; background: transparent; cursor: pointer; padding: 4px; border-radius: 6px; color: var(--color-text-secondary, #6b7280); }
.icon-btn:hover { background: var(--color-hover, #f3f4f6); }
.icon-btn.danger:hover { color: #ef4444; }
.icon-btn .material-symbols-outlined { font-size: 18px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; }

.txid-cell { max-width: 150px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.error-cell { max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: 11px; color: #ef4444; }

.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

code { background: var(--color-hover, #f3f4f6); padding: 2px 6px; border-radius: 4px; font-size: 12px; }

.crypto-section { background: var(--color-card, #fff); border: 1px solid var(--color-border, #e5e7eb); border-radius: 10px; padding: 14px; }
.crypto-section-header { display: flex; justify-content: space-between; align-items: center; }
.crypto-section-title { display: flex; align-items: center; gap: 6px; font-size: 14px; font-weight: 600; color: var(--color-text, #1f2937); }
.add-btn { display: flex; align-items: center; gap: 4px; padding: 6px 12px; border: 1px solid var(--color-primary, #2563eb); background: transparent; color: var(--color-primary, #2563eb); border-radius: 6px; font-size: 12px; font-weight: 600; cursor: pointer; transition: all 0.15s; }
.add-btn:hover { background: var(--color-primary, #2563eb); color: #fff; }

.rate-form { display: flex; gap: 8px; align-items: center; margin-top: 12px; flex-wrap: wrap; }
.rate-input { padding: 6px 10px; border: 1px solid var(--color-border, #e5e7eb); border-radius: 6px; font-size: 13px; background: var(--color-card, #fff); color: var(--color-text, #1f2937); width: 140px; }
.rate-input.sm { width: 100px; }
.rate-input:focus { outline: none; border-color: var(--color-primary, #2563eb); }

.rate-cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 10px; margin-top: 12px; }
.rate-card { padding: 14px; background: var(--color-hover, #f9fafb); border: 1px solid var(--color-border, #e5e7eb); border-radius: 8px; }
.rate-currency { font-size: 16px; font-weight: 700; color: var(--color-text, #1f2937); margin-bottom: 8px; }
.rate-values { display: flex; flex-direction: column; gap: 4px; }
.rate-row { display: flex; justify-content: space-between; align-items: center; }
.rate-label { font-size: 12px; color: var(--color-text-secondary, #6b7280); }
.rate-val { font-size: 15px; font-weight: 600; color: var(--color-text, #1f2937); }
.rate-updated { font-size: 10px; color: var(--color-text-secondary, #9ca3af); margin-top: 6px; }
.rate-edit-row { display: flex; align-items: center; gap: 8px; margin-bottom: 6px; }
.rate-edit-row label { font-size: 12px; color: var(--color-text-secondary, #6b7280); width: 40px; }
.rate-actions { display: flex; gap: 6px; margin-top: 6px; }

.action-sm { padding: 5px 10px; border-radius: 6px; font-size: 11px; font-weight: 600; cursor: pointer; border: none; display: flex; align-items: center; gap: 4px; transition: opacity 0.15s; }
.action-sm:hover { opacity: 0.85; }
.action-sm.save { background: #10b981; color: #fff; }
.action-sm.cancel { background: var(--color-border, #e5e7eb); color: var(--color-text, #1f2937); }
.action-sm.edit { background: transparent; border: 1px solid var(--color-border, #d1d5db); color: var(--color-text-secondary, #6b7280); margin-top: 8px; }
</style>
