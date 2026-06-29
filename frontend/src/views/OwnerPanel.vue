<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'

const router   = useRouter()
const authStore = useAuthStore()

// ── Guard ──────────────────────────────────────────
onMounted(async () => {
  if (!authStore.isOwner) { router.push('/ihtiyar/dashboard'); return }
  await Promise.all([loadOffices(), loadTransactions()])
})

// ── Tab ───────────────────────────────────────────
type Tab = 'offices' | 'transactions' | 'qr'
const activeTab = ref<Tab>('offices')

// ── Offices ────────────────────────────────────────
const offices        = ref<any[]>([])
const officesLoading = ref(true)
const officesError   = ref('')

async function loadOffices() {
  officesLoading.value = true
  officesError.value   = ''
  try {
    offices.value = await apiService.getOfficeSummaries() ?? []
  } catch (e: any) {
    officesError.value = e?.response?.data?.message || e.message || 'Veri yüklenemedi'
  } finally {
    officesLoading.value = false
  }
}

const totalVaults    = computed(() => offices.value.reduce((a, o) => a + (o.vaultCount ?? 0), 0))
const totalDailyPL   = computed(() => offices.value.reduce((a, o) => a + (o.dailyProfitLoss ?? 0), 0))
const totalMonthlyPL = computed(() => offices.value.reduce((a, o) => a + (o.monthlyProfitLoss ?? 0), 0))

// ── Transactions ───────────────────────────────────
const transactions = ref<any[]>([])
const txLoading    = ref(true)
const txError      = ref('')
const txPage       = ref(1)
const TX_SIZE      = 20

async function loadTransactions() {
  txLoading.value = true
  txError.value   = ''
  try {
    const res = await apiService.getTransactionHistory({ page: txPage.value, pageSize: TX_SIZE })
    transactions.value = Array.isArray(res) ? res : (res?.items ?? res?.data ?? [])
  } catch (e: any) {
    txError.value = e?.response?.data?.message || e.message || 'Veri yüklenemedi'
  } finally {
    txLoading.value = false
  }
}

// ── QR ─────────────────────────────────────────────
const qrInput  = ref('')
const qrLabel  = ref('')
const qrSize   = ref(280)
const qrGenerated = ref(false)
const qrImgUrl    = ref('')

function generateQr() {
  const text = qrInput.value.trim()
  if (!text) return
  qrImgUrl.value   = `https://api.qrserver.com/v1/create-qr-code/?size=${qrSize.value}x${qrSize.value}&data=${encodeURIComponent(text)}&margin=10&ecc=M`
  qrGenerated.value = true
}

function downloadQr() {
  if (!qrImgUrl.value) return
  const a = document.createElement('a')
  a.href     = qrImgUrl.value
  a.download = (qrLabel.value.trim() || 'qrcode') + '.png'
  a.target   = '_blank'
  a.click()
}

function resetQr() {
  qrInput.value     = ''
  qrLabel.value     = ''
  qrGenerated.value = false
  qrImgUrl.value    = ''
}

// ── Helpers ────────────────────────────────────────
function fmtMoney(n: number | null | undefined) {
  if (n == null) return '—'
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtDate(d: string) {
  if (!d) return '—'
  return new Date(d).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function plClass(n: number) {
  if (n > 0) return 'text-green-600'
  if (n < 0) return 'text-red-500'
  return 'text-gray-500'
}

function statusLabel(s: string) {
  const map: Record<string, string> = {
    Completed: 'Tamamlandı', Pending: 'Bekliyor', Cancelled: 'İptal',
    completed: 'Tamamlandı', pending: 'Bekliyor', cancelled: 'İptal',
  }
  return map[s] ?? s
}

function statusClass(s: string) {
  if (/complet/i.test(s)) return 'bg-green-100 text-green-700'
  if (/cancel/i.test(s))  return 'bg-red-100 text-red-600'
  return 'bg-yellow-100 text-yellow-700'
}

const currencyKeys = (obj: Record<string, number> | null | undefined) =>
  obj ? Object.entries(obj).slice(0, 4) : []
</script>

<template>
  <div class="owner-panel">

    <!-- ── Header ─────────────────────────────────── -->
    <div class="op-header">
      <div class="op-header-left">
        <span class="material-symbols-outlined op-crown">crown</span>
        <div>
          <h1 class="op-title">Owner Panel</h1>
          <p class="op-sub">Sistem yönetimi ve genel bakış</p>
        </div>
      </div>
      <button class="op-refresh-btn" @click="loadOffices(); loadTransactions()">
        <span class="material-symbols-outlined">refresh</span>
        Yenile
      </button>
    </div>

    <!-- ── Stats ──────────────────────────────────── -->
    <div class="op-stats">
      <div class="op-stat-card">
        <span class="material-symbols-outlined op-stat-icon blue">store</span>
        <div>
          <div class="op-stat-val">{{ offices.length }}</div>
          <div class="op-stat-lbl">Toplam Şube</div>
        </div>
      </div>
      <div class="op-stat-card">
        <span class="material-symbols-outlined op-stat-icon purple">account_balance_wallet</span>
        <div>
          <div class="op-stat-val">{{ totalVaults }}</div>
          <div class="op-stat-lbl">Toplam Kasa</div>
        </div>
      </div>
      <div class="op-stat-card">
        <span class="material-symbols-outlined op-stat-icon" :class="totalDailyPL >= 0 ? 'green' : 'red'">trending_up</span>
        <div>
          <div class="op-stat-val" :class="plClass(totalDailyPL)">{{ fmtMoney(totalDailyPL) }} ₺</div>
          <div class="op-stat-lbl">Günlük K/Z</div>
        </div>
      </div>
      <div class="op-stat-card">
        <span class="material-symbols-outlined op-stat-icon" :class="totalMonthlyPL >= 0 ? 'green' : 'red'">calendar_month</span>
        <div>
          <div class="op-stat-val" :class="plClass(totalMonthlyPL)">{{ fmtMoney(totalMonthlyPL) }} ₺</div>
          <div class="op-stat-lbl">Aylık K/Z</div>
        </div>
      </div>
    </div>

    <!-- ── Tabs ────────────────────────────────────── -->
    <div class="op-tabs">
      <button class="op-tab" :class="{ active: activeTab === 'offices' }" @click="activeTab = 'offices'">
        <span class="material-symbols-outlined">store</span> Şubeler
      </button>
      <button class="op-tab" :class="{ active: activeTab === 'transactions' }" @click="activeTab = 'transactions'">
        <span class="material-symbols-outlined">receipt_long</span> Son İşlemler
      </button>
      <button class="op-tab" :class="{ active: activeTab === 'qr' }" @click="activeTab = 'qr'">
        <span class="material-symbols-outlined">qr_code_2</span> QR Oluştur
      </button>
    </div>

    <!-- ── Tab: Şubeler ────────────────────────────── -->
    <div v-if="activeTab === 'offices'" class="op-card">
      <div v-if="officesLoading" class="op-loading">
        <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
      </div>
      <div v-else-if="officesError" class="op-error">
        <span class="material-symbols-outlined">error</span> {{ officesError }}
      </div>
      <div v-else-if="offices.length === 0" class="op-empty">
        <span class="material-symbols-outlined">store_off</span>
        <p>Şube bulunamadı</p>
      </div>
      <div v-else>
        <div class="op-table-wrap">
          <table class="op-table">
            <thead>
              <tr>
                <th>Şube Adı</th>
                <th>Kasa</th>
                <th>Günlük K/Z</th>
                <th>Aylık K/Z</th>
                <th>Toplam Varlık (₺)</th>
                <th>Döviz Bakiyeleri</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="o in offices" :key="o.officeId">
                <td><strong>{{ o.officeName }}</strong></td>
                <td>{{ o.vaultCount }}</td>
                <td :class="plClass(o.dailyProfitLoss)">{{ fmtMoney(o.dailyProfitLoss) }} ₺</td>
                <td :class="plClass(o.monthlyProfitLoss)">{{ fmtMoney(o.monthlyProfitLoss) }} ₺</td>
                <td>{{ fmtMoney(o.totalValueInBaseCurrency) }} ₺</td>
                <td>
                  <span
                    v-for="[cur, bal] in currencyKeys(o.totalBalancesByCurrency)"
                    :key="cur"
                    class="op-currency-badge"
                  >{{ cur }}: {{ fmtMoney(bal) }}</span>
                  <span v-if="!o.totalBalancesByCurrency || Object.keys(o.totalBalancesByCurrency).length === 0" class="text-gray-400 text-xs">—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ── Tab: Son İşlemler ───────────────────────── -->
    <div v-if="activeTab === 'transactions'" class="op-card">
      <div v-if="txLoading" class="op-loading">
        <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
      </div>
      <div v-else-if="txError" class="op-error">
        <span class="material-symbols-outlined">error</span> {{ txError }}
      </div>
      <div v-else-if="transactions.length === 0" class="op-empty">
        <span class="material-symbols-outlined">receipt_long</span>
        <p>İşlem bulunamadı</p>
      </div>
      <div v-else>
        <div class="op-table-wrap">
          <table class="op-table">
            <thead>
              <tr>
                <th>İşlem No</th>
                <th>Şube</th>
                <th>Tarih</th>
                <th>Tür</th>
                <th>Durum</th>
                <th>Kâr</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="tx in transactions" :key="tx.id ?? tx.transactionNumber">
                <td><code class="op-code">{{ tx.transactionNumber ?? '—' }}</code></td>
                <td>{{ tx.officeName ?? '—' }}</td>
                <td class="text-sm text-gray-500">{{ fmtDate(tx.transactionDate) }}</td>
                <td>{{ tx.type ?? '—' }}</td>
                <td>
                  <span class="op-badge" :class="statusClass(tx.status)">
                    {{ statusLabel(tx.status) }}
                  </span>
                </td>
                <td :class="plClass(tx.profit)">{{ fmtMoney(tx.profit) }} ₺</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="op-pagination">
          <button class="op-page-btn" :disabled="txPage <= 1" @click="txPage--; loadTransactions()">
            <span class="material-symbols-outlined">chevron_left</span>
          </button>
          <span class="op-page-info">Sayfa {{ txPage }}</span>
          <button class="op-page-btn" :disabled="transactions.length < TX_SIZE" @click="txPage++; loadTransactions()">
            <span class="material-symbols-outlined">chevron_right</span>
          </button>
        </div>
      </div>
    </div>

    <!-- ── Tab: QR Oluştur ─────────────────────────── -->
    <div v-if="activeTab === 'qr'" class="op-card">
      <div class="op-qr-layout">
        <div class="op-qr-form">
          <h3 class="op-section-title">
            <span class="material-symbols-outlined">qr_code_2</span> QR Kod Oluşturucu
          </h3>

          <div class="op-field">
            <label class="op-label">İçerik / URL</label>
            <textarea
              v-model="qrInput"
              class="op-textarea"
              rows="3"
              placeholder="QR içeriğini girin (URL, metin, telefon, vb.)"
              @keydown.enter.prevent="generateQr"
            />
          </div>

          <div class="op-field">
            <label class="op-label">Dosya Adı (opsiyonel)</label>
            <input v-model="qrLabel" type="text" class="op-input" placeholder="örn: bayi-ankara" />
          </div>

          <div class="op-field">
            <label class="op-label">Boyut: {{ qrSize }}×{{ qrSize }} px</label>
            <input v-model.number="qrSize" type="range" min="100" max="500" step="20" class="op-range" />
          </div>

          <div class="op-qr-btns">
            <button class="op-btn primary" :disabled="!qrInput.trim()" @click="generateQr">
              <span class="material-symbols-outlined">qr_code</span> Oluştur
            </button>
            <button class="op-btn secondary" @click="resetQr">
              <span class="material-symbols-outlined">refresh</span> Temizle
            </button>
          </div>
        </div>

        <div class="op-qr-preview">
          <div v-if="!qrGenerated" class="op-qr-empty">
            <span class="material-symbols-outlined">qr_code_2</span>
            <p>Sol taraftan içerik girin ve<br><strong>Oluştur</strong>'a basın</p>
          </div>
          <div v-else class="op-qr-result">
            <img :src="qrImgUrl" :alt="qrLabel || 'QR Kod'" class="op-qr-img" />
            <p class="op-qr-caption">{{ qrLabel || 'QR Kod' }}</p>
            <button class="op-btn primary w-full" @click="downloadQr">
              <span class="material-symbols-outlined">download</span> İndir (.png)
            </button>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<style scoped>
/* ── Layout ───────────────────────────────────────── */
.owner-panel { padding: 1.5rem; max-width: 1400px; margin: 0 auto; }

/* ── Header ───────────────────────────────────────── */
.op-header {
  display: flex; align-items: center; justify-content: space-between;
  margin-bottom: 1.5rem;
}
.op-header-left { display: flex; align-items: center; gap: 1rem; }
.op-crown { font-size: 2.5rem; color: #f59e0b; }
.op-title { font-size: 1.5rem; font-weight: 800; color: #1e293b; margin: 0; }
.op-sub   { font-size: 0.875rem; color: #64748b; margin: 0; }
.op-refresh-btn {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.5rem 1rem; background: #f1f5f9; border: 1px solid #e2e8f0;
  border-radius: 8px; cursor: pointer; font-size: 0.875rem; color: #475569;
  transition: all .2s;
}
.op-refresh-btn:hover { background: #e2e8f0; }

/* ── Stats ────────────────────────────────────────── */
.op-stats {
  display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem; margin-bottom: 1.5rem;
}
.op-stat-card {
  background: white; border-radius: 12px; padding: 1.25rem;
  display: flex; align-items: center; gap: 1rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.08);
}
.op-stat-icon { font-size: 2rem; padding: 0.5rem; border-radius: 10px; }
.op-stat-icon.blue   { background: #eff6ff; color: #3b82f6; }
.op-stat-icon.purple { background: #f5f3ff; color: #8b5cf6; }
.op-stat-icon.green  { background: #f0fdf4; color: #22c55e; }
.op-stat-icon.red    { background: #fef2f2; color: #ef4444; }
.op-stat-val  { font-size: 1.5rem; font-weight: 800; color: #1e293b; }
.op-stat-lbl  { font-size: 0.75rem; color: #64748b; margin-top: 2px; }

/* ── Tabs ─────────────────────────────────────────── */
.op-tabs {
  display: flex; gap: 0.5rem; margin-bottom: 1rem;
  border-bottom: 2px solid #e2e8f0; padding-bottom: 0;
}
.op-tab {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.6rem 1.2rem; background: none; border: none;
  border-bottom: 2px solid transparent; margin-bottom: -2px;
  cursor: pointer; font-size: 0.9rem; color: #64748b;
  transition: all .2s;
}
.op-tab:hover  { color: #3b82f6; }
.op-tab.active { color: #3b82f6; border-bottom-color: #3b82f6; font-weight: 600; }
.op-tab .material-symbols-outlined { font-size: 1.1rem; }

/* ── Card ─────────────────────────────────────────── */
.op-card {
  background: white; border-radius: 12px; padding: 1.5rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.08);
}

/* ── States ───────────────────────────────────────── */
.op-loading, .op-empty, .op-error {
  display: flex; flex-direction: column; align-items: center;
  justify-content: center; gap: 0.75rem; padding: 3rem;
  color: #94a3b8; font-size: 0.95rem;
}
.op-error { color: #ef4444; }
.op-empty .material-symbols-outlined,
.op-loading .material-symbols-outlined { font-size: 3rem; }
@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin 1s linear infinite; }

/* ── Table ────────────────────────────────────────── */
.op-table-wrap { overflow-x: auto; }
.op-table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
.op-table th {
  text-align: left; padding: 0.75rem 1rem;
  background: #f8fafc; color: #64748b; font-weight: 600;
  border-bottom: 1px solid #e2e8f0; white-space: nowrap;
}
.op-table td {
  padding: 0.75rem 1rem; border-bottom: 1px solid #f1f5f9;
  vertical-align: middle;
}
.op-table tr:last-child td { border-bottom: none; }
.op-table tr:hover td { background: #f8fafc; }
.op-code { font-family: monospace; font-size: 0.8rem; background: #f1f5f9; padding: 2px 6px; border-radius: 4px; }
.op-badge { padding: 3px 8px; border-radius: 20px; font-size: 0.75rem; font-weight: 600; white-space: nowrap; }
.op-currency-badge {
  display: inline-block; margin: 2px; padding: 2px 6px;
  background: #eff6ff; color: #3b82f6; border-radius: 4px; font-size: 0.75rem;
}

/* ── Pagination ───────────────────────────────────── */
.op-pagination { display: flex; align-items: center; justify-content: center; gap: 1rem; padding-top: 1rem; }
.op-page-btn {
  display: flex; align-items: center; padding: 0.4rem;
  background: #f1f5f9; border: 1px solid #e2e8f0;
  border-radius: 6px; cursor: pointer; transition: all .2s;
}
.op-page-btn:hover:not(:disabled) { background: #e2e8f0; }
.op-page-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.op-page-info { font-size: 0.875rem; color: #64748b; }

/* ── QR ───────────────────────────────────────────── */
.op-qr-layout   { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; }
@media (max-width: 768px) { .op-qr-layout { grid-template-columns: 1fr; } }

.op-section-title {
  display: flex; align-items: center; gap: 0.5rem;
  font-size: 1.1rem; font-weight: 700; color: #1e293b;
  margin: 0 0 1.5rem;
}
.op-field   { margin-bottom: 1.25rem; }
.op-label   { display: block; font-size: 0.8rem; font-weight: 600; color: #475569; margin-bottom: 0.4rem; }
.op-textarea, .op-input {
  width: 100%; padding: 0.75rem; border: 2px solid #e2e8f0;
  border-radius: 8px; font-size: 0.9rem; color: #1e293b;
  background: #f8fafc; transition: border-color .2s; outline: none; resize: vertical;
  box-sizing: border-box;
}
.op-textarea:focus, .op-input:focus { border-color: #3b82f6; background: white; }
.op-range { width: 100%; accent-color: #3b82f6; }

.op-qr-btns { display: flex; gap: 0.75rem; }
.op-btn {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.6rem 1.2rem; border: none; border-radius: 8px;
  cursor: pointer; font-size: 0.9rem; font-weight: 600; transition: all .2s;
}
.op-btn.primary   { background: #3b82f6; color: white; }
.op-btn.primary:hover:not(:disabled) { background: #2563eb; }
.op-btn.secondary { background: #f1f5f9; color: #475569; border: 1px solid #e2e8f0; }
.op-btn.secondary:hover { background: #e2e8f0; }
.op-btn:disabled  { opacity: 0.5; cursor: not-allowed; }
.op-btn.w-full    { width: 100%; justify-content: center; }

.op-qr-preview {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  background: #f8fafc; border: 2px dashed #e2e8f0; border-radius: 12px;
  padding: 2rem; min-height: 300px;
}
.op-qr-empty {
  display: flex; flex-direction: column; align-items: center; gap: 1rem;
  color: #94a3b8; text-align: center;
}
.op-qr-empty .material-symbols-outlined { font-size: 4rem; opacity: 0.3; }
.op-qr-result { display: flex; flex-direction: column; align-items: center; gap: 1rem; width: 100%; }
.op-qr-img    { border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,.12); }
.op-qr-caption { font-weight: 600; color: #1e293b; }
</style>
