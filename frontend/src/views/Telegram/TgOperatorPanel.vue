<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import apiService from '@/services/apiservice'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()

const loading = ref(true)
const transactions = ref<any[]>([])
const selectedTx = ref<any>(null)
const messages = ref<any[]>([])
const newMessage = ref('')
const sendingMsg = ref(false)
const activeFilter = ref('active')

let eventSource: EventSource | null = null
let audioCtx: AudioContext | null = null

const filteredTx = computed(() => {
  if (activeFilter.value === 'active')
    return transactions.value.filter(t => ['pending', 'processing', 'approved'].includes(t.status))
  return transactions.value.filter(t => ['completed', 'cancelled', 'rejected'].includes(t.status))
})

const searchQuery = ref('')
const searchedTx = computed(() => {
  if (!searchQuery.value) return filteredTx.value
  const q = searchQuery.value.toLowerCase()
  return filteredTx.value.filter(t =>
    (t.customerName || '').toLowerCase().includes(q) ||
    String(t.id).includes(q)
  )
})

async function loadTransactions() {
  try {
    const res = await apiService.get('/tg/operator/transactions')
    transactions.value = res?.transactions ?? []
  } catch (e) { console.error(e) }
  finally { loading.value = false }
}

async function selectTransaction(tx: any) {
  selectedTx.value = tx
  try {
    const res = await apiService.get(`/tg/operator/chat/${tx.id}`)
    messages.value = res?.messages ?? []
    await nextTick()
    scrollChatBottom()
  } catch (e) { console.error(e) }
}

async function sendChat() {
  if (!newMessage.value.trim() || !selectedTx.value || sendingMsg.value) return
  sendingMsg.value = true
  try {
    await apiService.post(`/tg/operator/chat/${selectedTx.value.id}`, { message: newMessage.value })
    newMessage.value = ''
    const res = await apiService.get(`/tg/operator/chat/${selectedTx.value.id}`)
    messages.value = res?.messages ?? []
    await nextTick()
    scrollChatBottom()
  } catch (e) { console.error(e) }
  finally { sendingMsg.value = false }
}

const verifyingCrypto = ref(false)

async function verifyCrypto() {
  if (!selectedTx.value || verifyingCrypto.value) return
  if (!confirm('Kripto işlemini doğrulamak istediğinize emin misiniz?')) return

  verifyingCrypto.value = true
  try {
    const res = await apiService.post(`/tg/operator/transaction/${selectedTx.value.id}/verify-crypto`)
    if (res?.success) {
      selectedTx.value.cryptoVerified = true
      selectedTx.value.cryptoVerifiedAt = res.cryptoVerifiedAt
    }
    await loadTransactions()
    if (selectedTx.value) {
      selectedTx.value = transactions.value.find((t: any) => t.id === selectedTx.value.id) || null
    }
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Doğrulama başarısız')
  } finally { verifyingCrypto.value = false }
}

async function doAction(action: string) {
  if (!selectedTx.value) return
  const labels: Record<string, string> = {
    approve: 'Kabul etmek', complete: 'Tamamlamak', reject: 'Reddetmek', cancel: 'İptal etmek'
  }
  if (!confirm(`İşlemi ${labels[action]} istediğinize emin misiniz?`)) return

  try {
    const res = await apiService.post(`/tg/operator/transaction/${selectedTx.value.id}/${action}`)
    if (res?.status) selectedTx.value.status = res.status
    await loadTransactions()
    if (selectedTx.value) {
      selectedTx.value = transactions.value.find((t: any) => t.id === selectedTx.value.id) || null
    }
  } catch (e) { console.error(e) }
}

function scrollChatBottom() {
  const el = document.querySelector('.chat-messages')
  if (el) el.scrollTop = el.scrollHeight
}

function playNotificationSound() {
  try {
    if (!audioCtx) audioCtx = new AudioContext()
    const osc = audioCtx.createOscillator()
    const gain = audioCtx.createGain()
    osc.connect(gain)
    gain.connect(audioCtx.destination)
    osc.frequency.value = 800
    gain.gain.value = 0.1
    osc.start()
    osc.stop(audioCtx.currentTime + 0.15)
  } catch { }
}

function connectSSE() {
  const token = localStorage.getItem('token')
  if (!token) return

  const baseUrl = import.meta.env.DEV ? 'http://localhost:5093/api/v1' : 'https://api.baskentenerji.com/api/v1'
  eventSource = new EventSource(`${baseUrl}/tg/events?token=${token}`)

  eventSource.addEventListener('transaction_update', (e: any) => {
    loadTransactions()
    playNotificationSound()
    if (selectedTx.value) {
      try {
        const data = JSON.parse(e.data)
        if (data.transaction_id === selectedTx.value.id)
          selectTransaction(selectedTx.value)
      } catch {}
    }
  })

  eventSource.addEventListener('new_message', (e: any) => {
    try {
      const data = JSON.parse(e.data)
      if (selectedTx.value && data.transaction_id === selectedTx.value.id)
        selectTransaction(selectedTx.value)
    } catch {}
    playNotificationSound()
  })

  eventSource.onerror = () => {
    if (eventSource && eventSource.readyState === EventSource.CLOSED) {
      eventSource.close()
      setTimeout(connectSSE, 5000)
    }
  }
}

function formatDate(d: string | null) {
  if (!d) return '—'
  return new Date(d).toLocaleString('tr-TR')
}

function formatTime(d: string | null) {
  if (!d) return ''
  return new Date(d).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}

function formatMoney(n: number | null) {
  if (n == null) return '0'
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function statusColor(status: string) {
  const map: Record<string, string> = {
    completed: '#10b981', approved: '#3b82f6', pending: '#f59e0b',
    processing: '#8b5cf6', cancelled: '#ef4444', rejected: '#ef4444'
  }
  return map[status] || '#6b7280'
}

onMounted(() => {
  loadTransactions()
  connectSSE()
})

onUnmounted(() => {
  if (eventSource) eventSource.close()
})
</script>

<template>
  <div class="tg-operator">
    <div v-if="loading" class="tg-loading">
      <span class="material-symbols-outlined spin">progress_activity</span>
      Yükleniyor...
    </div>

    <template v-else>
      <div class="operator-layout">
        <!-- Left: Transaction List -->
        <div class="tx-sidebar">
          <div class="sidebar-tabs">
            <button class="stab" :class="{ active: activeFilter === 'active' }" @click="activeFilter = 'active'">
              Aktif İşlemler
            </button>
            <button class="stab" :class="{ active: activeFilter === 'completed' }" @click="activeFilter = 'completed'">
              Tamamlananlar
            </button>
          </div>

          <div class="search-box">
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size: 16px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">search</span>
            <input v-model="searchQuery" placeholder="Müşteri adı veya işlem no..." />
          </div>

          <div class="tx-list">
            <div v-for="tx in searchedTx" :key="tx.id"
              class="tx-item" :class="{ selected: selectedTx?.id === tx.id }"
              @click="selectTransaction(tx)">
              <div class="tx-item-header">
                <span class="tx-id">#{{ tx.id }}</span>
                <span class="status-dot" :style="{ background: statusColor(tx.status) }"></span>
              </div>
              <div class="tx-item-name">{{ tx.customerName || tx.customerUsername || 'Müşteri' }}</div>
              <div class="tx-item-detail">{{ tx.amount }} {{ tx.currency }} · {{ formatTime(tx.createdAt) }}</div>
            </div>
            <div v-if="!searchedTx.length" class="tx-empty">İşlem yok</div>
          </div>
        </div>

        <!-- Center: Chat -->
        <div class="chat-area">
          <template v-if="selectedTx">
            <div class="chat-header">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">chat</span>
              İşlem #{{ selectedTx.id }} — {{ selectedTx.customerName || 'Müşteri' }}
            </div>

            <div class="chat-messages">
              <div v-for="msg in messages" :key="msg.id"
                class="chat-msg" :class="msg.senderType === 'operator' ? 'sent' : 'received'">
                <div class="msg-bubble">
                  {{ msg.message }}
                  <span class="msg-time">{{ formatTime(msg.createdAt) }}</span>
                </div>
              </div>
              <div v-if="!messages.length" class="chat-empty">Henüz mesaj yok</div>
            </div>

            <div class="chat-input">
              <input v-model="newMessage" placeholder="Mesaj yazın..." @keyup.enter="sendChat" :disabled="sendingMsg" />
              <button class="send-btn" @click="sendChat" :disabled="sendingMsg || !newMessage.trim()">
                <span class="material-symbols-outlined" aria-hidden="true" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">send</span>
              </button>
            </div>
          </template>
          <div v-else class="chat-placeholder">
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size: 48px; color: var(--color-text-secondary, #9ca3af); font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">forum</span>
            <p>Soldaki listeden bir işlem seçin</p>
          </div>
        </div>

        <!-- Right: Transaction Details & Actions -->
        <div class="tx-detail-panel" v-if="selectedTx">
          <div class="detail-title">İşlem Detayları</div>

          <div class="detail-grid">
            <div class="detail-row"><span class="dl">İşlem No</span><span class="dv">#{{ selectedTx.id }}</span></div>
            <div class="detail-row"><span class="dl">Müşteri</span><span class="dv">{{ selectedTx.customerName || '—' }}</span></div>
            <div class="detail-row"><span class="dl">Para Birimi</span><span class="dv">{{ selectedTx.currency }}</span></div>
            <div class="detail-row"><span class="dl">Tutar</span><span class="dv">{{ formatMoney(selectedTx.amount) }}</span></div>
            <div class="detail-row"><span class="dl">TL Tutar</span><span class="dv">₺{{ formatMoney(selectedTx.tlAmount) }}</span></div>
            <div class="detail-row"><span class="dl">Kur</span><span class="dv">{{ selectedTx.exchangeRate }}</span></div>
            <div class="detail-row"><span class="dl">Bayi</span><span class="dv">{{ selectedTx.referralCode || '—' }}</span></div>
            <div class="detail-row"><span class="dl">Tür</span><span class="dv">{{ selectedTx.isBuy ? 'Alım' : 'Satım' }}</span></div>
            <div class="detail-row">
              <span class="dl">Durum</span>
              <span class="status-badge" :style="{ background: statusColor(selectedTx.status) }">{{ selectedTx.status }}</span>
            </div>
            <div class="detail-row"><span class="dl">Tarih</span><span class="dv">{{ formatDate(selectedTx.createdAt) }}</span></div>
            <div class="detail-row" v-if="selectedTx.completedAt"><span class="dl">Tamamlanma</span><span class="dv">{{ formatDate(selectedTx.completedAt) }}</span></div>
          </div>

          <!-- Kripto Doğrulama Bölümü -->
          <div class="crypto-verify-section" v-if="selectedTx.txid || selectedTx.currency === 'USDT'">
            <div class="detail-title" style="margin-top: 16px; padding-top: 12px; border-top: 1px solid var(--color-border, #e5e7eb);">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-size: 18px; font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">currency_bitcoin</span>
              Kripto Bilgileri
            </div>
            <div class="detail-grid">
              <div class="detail-row">
                <span class="dl">TXID</span>
                <span class="dv txid-val" :title="selectedTx.txid">{{ selectedTx.txid ? (selectedTx.txid.substring(0, 12) + '...') : '—' }}</span>
              </div>
              <div class="detail-row">
                <span class="dl">Doğrulama</span>
                <span v-if="selectedTx.cryptoVerified" class="status-badge" style="background: #10b981;">Doğrulanmış</span>
                <span v-else class="status-badge" style="background: #f59e0b;">Bekliyor</span>
              </div>
              <div class="detail-row" v-if="selectedTx.cryptoVerifiedAt">
                <span class="dl">Doğrulama Tarihi</span>
                <span class="dv">{{ formatDate(selectedTx.cryptoVerifiedAt) }}</span>
              </div>
            </div>
            <button
              v-if="selectedTx.txid && !selectedTx.cryptoVerified"
              class="action-btn verify-crypto"
              @click="verifyCrypto"
              :disabled="verifyingCrypto">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">verified</span>
              {{ verifyingCrypto ? 'Doğrulanıyor...' : 'Kripto Doğrula' }}
            </button>
          </div>

          <!-- Action Buttons -->
          <div class="action-buttons" v-if="['pending', 'processing', 'approved'].includes(selectedTx.status)">
            <button class="action-btn approve" @click="doAction('approve')" v-if="selectedTx.status === 'pending'">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">check_circle</span>
              Kabul Et
            </button>
            <button class="action-btn complete" @click="doAction('complete')" v-if="selectedTx.status === 'approved'">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">task_alt</span>
              Tamamla
            </button>
            <button class="action-btn reject" @click="doAction('reject')">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">block</span>
              Reddet
            </button>
            <button class="action-btn cancel" @click="doAction('cancel')">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;">cancel</span>
              İptal Et
            </button>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.tg-operator { height: calc(100vh - 64px); display: flex; flex-direction: column; }
.tg-loading { display: flex; align-items: center; justify-content: center; gap: 8px; padding: 60px; color: var(--color-text-secondary, #6b7280); }

.operator-layout { display: grid; grid-template-columns: 280px 1fr 300px; height: 100%; }

/* Sidebar */
.tx-sidebar { border-right: 1px solid var(--color-border, #e5e7eb); display: flex; flex-direction: column; overflow: hidden; }
.sidebar-tabs { display: flex; border-bottom: 1px solid var(--color-border, #e5e7eb); }
.stab { flex: 1; padding: 10px; border: none; background: transparent; font-size: 12px; font-weight: 600; cursor: pointer; color: var(--color-text-secondary, #6b7280); }
.stab.active { color: var(--color-primary, #2563eb); border-bottom: 2px solid var(--color-primary, #2563eb); }

.search-box { display: flex; align-items: center; gap: 6px; padding: 8px 10px; border-bottom: 1px solid var(--color-border, #e5e7eb); color: var(--color-text-secondary, #9ca3af); }
.search-box input { flex: 1; border: none; outline: none; font-size: 12px; background: transparent; color: var(--color-text, #1f2937); }

.tx-list { flex: 1; overflow-y: auto; }
.tx-item { padding: 10px 12px; border-bottom: 1px solid var(--color-border, #f3f4f6); cursor: pointer; transition: background 0.1s; }
.tx-item:hover { background: var(--color-hover, #f9fafb); }
.tx-item.selected { background: var(--color-primary-light, #eff6ff); border-left: 3px solid var(--color-primary, #2563eb); }
.tx-item-header { display: flex; justify-content: space-between; align-items: center; }
.tx-id { font-size: 12px; font-weight: 600; color: var(--color-text, #1f2937); }
.status-dot { width: 8px; height: 8px; border-radius: 50%; }
.tx-item-name { font-size: 13px; color: var(--color-text, #1f2937); margin: 2px 0; }
.tx-item-detail { font-size: 11px; color: var(--color-text-secondary, #6b7280); }
.tx-empty { text-align: center; padding: 30px; font-size: 13px; color: var(--color-text-secondary, #9ca3af); }

/* Chat */
.chat-area { display: flex; flex-direction: column; overflow: hidden; }
.chat-header { padding: 12px 16px; border-bottom: 1px solid var(--color-border, #e5e7eb); font-weight: 600; font-size: 14px; display: flex; align-items: center; gap: 8px; color: var(--color-text, #1f2937); }

.chat-messages { flex: 1; overflow-y: auto; padding: 16px; display: flex; flex-direction: column; gap: 8px; }
.chat-msg { display: flex; }
.chat-msg.sent { justify-content: flex-end; }
.chat-msg.received { justify-content: flex-start; }
.msg-bubble { max-width: 70%; padding: 8px 12px; border-radius: var(--radius-lg); font-size: 13px; line-height: 1.4; position: relative; }
.sent .msg-bubble { background: var(--color-primary, var(--color-secondary-hover)); color: #fff; border-bottom-right-radius: var(--radius-sm); }
.received .msg-bubble { background: var(--color-hover, #f3f4f6); color: var(--color-text, var(--color-text)); border-bottom-left-radius: var(--radius-sm); }
.msg-time { font-size: 10px; opacity: 0.6; margin-left: 8px; white-space: nowrap; }
.chat-empty { text-align: center; padding: 40px; color: var(--color-text-secondary, #9ca3af); font-size: 13px; }

.chat-input { display: flex; gap: 8px; padding: 12px 16px; border-top: 1px solid var(--color-border, #e5e7eb); }
.chat-input input { flex: 1; padding: 8px 14px; border: 1px solid var(--color-border, var(--color-border)); border-radius: var(--radius-xl); outline: none; font-size: 13px; background: var(--color-card, #fff); color: var(--color-text, var(--color-text)); }
.chat-input input:focus { border-color: var(--color-primary, #2563eb); }
.send-btn { width: 36px; height: 36px; border-radius: 50%; border: none; background: var(--color-primary, #2563eb); color: #fff; cursor: pointer; display: flex; align-items: center; justify-content: center; }
.send-btn:disabled { opacity: 0.5; cursor: not-allowed; }
.send-btn .material-symbols-outlined { font-size: 18px; }

.chat-placeholder { display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; gap: 8px; }
.chat-placeholder p { color: var(--color-text-secondary, #9ca3af); font-size: 14px; }

/* Detail Panel */
.tx-detail-panel { border-left: 1px solid var(--color-border, #e5e7eb); padding: 16px; overflow-y: auto; }
.detail-title { font-size: 14px; font-weight: 600; margin-bottom: 16px; color: var(--color-text, #1f2937); }
.detail-grid { display: flex; flex-direction: column; gap: 8px; }
.detail-row { display: flex; justify-content: space-between; align-items: center; font-size: 13px; }
.dl { color: var(--color-text-secondary, #6b7280); }
.dv { font-weight: 500; color: var(--color-text, #1f2937); }

.status-badge { padding: 3px 8px; border-radius: var(--radius-md); font-size: 11px; font-weight: 600; color: #fff; }

.action-buttons { display: flex; flex-direction: column; gap: 8px; margin-top: 20px; padding-top: 16px; border-top: 1px solid var(--color-border, #e5e7eb); }
.action-btn { display: flex; align-items: center; justify-content: center; gap: 6px; padding: 10px; border: none; border-radius: var(--radius-md); font-size: 13px; font-weight: 600; cursor: pointer; color: #fff; transition: opacity 0.15s; }
.action-btn:hover { opacity: 0.9; }
.action-btn .material-symbols-outlined { font-size: 18px; }
.action-btn.approve { background: var(--color-secondary); }
.action-btn.complete { background: var(--color-success); }
.action-btn.reject { background: var(--color-warning); }
.action-btn.cancel { background: var(--color-danger); }
.action-btn.verify-crypto { background: #8b5cf6; margin-top: 8px; }

.crypto-verify-section .detail-title { display: flex; align-items: center; gap: 6px; }
.txid-val { font-family: monospace; font-size: 12px; }

.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

@media (max-width: 1024px) {
  .operator-layout { grid-template-columns: 240px 1fr; }
  .tx-detail-panel { display: none; }
}
@media (max-width: 768px) {
  .operator-layout { grid-template-columns: 1fr; }
  .tx-sidebar { max-height: 40vh; }
}
</style>
