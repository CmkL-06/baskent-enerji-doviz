<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import apiService from '@/services/apiservice'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()

const offices = ref<any[]>([])
const selectedOfficeId = ref('')
const vaults = ref<any[]>([])
const selectedVaultId = ref('')
const loading = ref(true)
const error = ref('')

const wacs = ref<Record<string, number>>({})
const history = ref<any[]>([])
const historyLoading = ref(false)
const historyDate = ref(new Date().toISOString().slice(0, 10))

const counts = ref<any[]>([])
const countsLoading = ref(false)

const snapshots = ref<any[]>([])
const snapshotsLoading = ref(false)
const snapshotDetail = ref<any>(null)

const subTab = ref<'overview' | 'history' | 'wac' | 'counts' | 'snapshots'>('overview')

onMounted(async () => {
  try {
    offices.value = await apiService.getOfficeSummaries() ?? []
    if (offices.value.length) {
      selectedOfficeId.value = offices.value[0].officeId
    }
  } catch (e: any) {
    error.value = e?.message || 'Veri yüklenemedi'
  } finally {
    loading.value = false
  }
})

watch(selectedOfficeId, async (id) => {
  vaults.value = []
  selectedVaultId.value = ''
  wacs.value = {}
  history.value = []
  counts.value = []
  snapshots.value = []
  snapshotDetail.value = null
  if (!id) return
  try {
    const v = await apiService.getVaultsByOfficeId(id)
    vaults.value = Array.isArray(v) ? v : (v?.data ?? [])
    if (vaults.value.length) {
      selectedVaultId.value = vaults.value[0].id || vaults.value[0].vaultId
    }
  } catch { vaults.value = [] }
})

watch(selectedVaultId, async (id) => {
  if (!id) return
  loadWacs(id)
  if (subTab.value === 'history') loadHistory()
  if (subTab.value === 'counts') loadCounts()
  if (subTab.value === 'snapshots') loadSnapshots()
})

watch(subTab, (tab) => {
  if (!selectedVaultId.value) return
  if (tab === 'history') loadHistory()
  if (tab === 'counts') loadCounts()
  if (tab === 'snapshots') loadSnapshots()
})

async function loadWacs(vaultId: string) {
  try {
    const w = await apiService.getAllWacs(vaultId)
    wacs.value = w ?? {}
  } catch { wacs.value = {} }
}

async function loadHistory() {
  if (!selectedOfficeId.value) return
  historyLoading.value = true
  try {
    const data = await apiService.getVaultBalanceHistories(selectedOfficeId.value, historyDate.value)
    const all = Array.isArray(data) ? data : (data?.data ?? [])
    const vid = selectedVaultId.value
    history.value = vid ? all.filter((h: any) => h.vaultId === vid) : all
  } catch { history.value = [] }
  finally { historyLoading.value = false }
}

async function loadCounts() {
  if (!selectedVaultId.value) return
  countsLoading.value = true
  try {
    const data = await apiService.getVaultCounts(selectedVaultId.value)
    counts.value = Array.isArray(data) ? data : (data?.data ?? [])
  } catch { counts.value = [] }
  finally { countsLoading.value = false }
}

async function loadSnapshots() {
  if (!selectedOfficeId.value) return
  snapshotsLoading.value = true
  try {
    const data = await apiService.getVaultSnapshots(selectedOfficeId.value)
    snapshots.value = Array.isArray(data) ? data : (data?.data ?? [])
  } catch { snapshots.value = [] }
  finally { snapshotsLoading.value = false }
}

async function viewSnapshot(id: string) {
  try {
    snapshotDetail.value = await apiService.getVaultSnapshotById(id)
  } catch { snapshotDetail.value = null }
}

async function createSnapshot() {
  if (!selectedOfficeId.value) return
  try {
    await apiService.createVaultSnapshot({ officeId: selectedOfficeId.value })
    await loadSnapshots()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Snapshot oluşturulamadı')
  }
}

const selectedVault = computed(() => vaults.value.find((v: any) => (v.id || v.vaultId) === selectedVaultId.value))

const balances = computed(() => {
  const v = selectedVault.value
  if (!v) return []
  return (v.balances || []).map((b: any) => ({
    ...b,
    wac: wacs.value[b.currencyId] ?? 0,
  }))
})

const totalTryValue = computed(() =>
  balances.value.reduce((sum: number, b: any) => {
    const rate = b.exchangeRateToBase ?? 1
    return sum + (b.balance ?? 0) * (b.currencyCode === 'TRY' ? 1 : rate)
  }, 0)
)

function fmtMoney(n: number | null | undefined) {
  if (n == null) return '—'
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtDate(d: string) {
  if (!d) return '—'
  return new Date(d).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function fmtTime(d: string) {
  if (!d) return '—'
  return new Date(d).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

function txTypeLabel(t: string | number) {
  const map: Record<string, string> = {
    '0': 'Döviz', '1': 'Yatırım', '2': 'Çekim', '3': 'Düzeltme', '4': 'Transfer',
    Exchange: 'Döviz', Deposit: 'Yatırım', Withdrawal: 'Çekim', Adjustment: 'Düzeltme', Transfer: 'Transfer', Buy: 'Alış', Sell: 'Satış'
  }
  return map[String(t)] ?? String(t)
}

function txTypeClass(t: string | number) {
  const s = String(t)
  if (['1', 'Deposit', 'Buy'].includes(s)) return 'vi-in'
  if (['2', 'Withdrawal', 'Sell'].includes(s)) return 'vi-out'
  if (['3', 'Adjustment'].includes(s)) return 'vi-adj'
  if (['4', 'Transfer'].includes(s)) return 'vi-xfer'
  return 'vi-ex'
}
</script>

<template>
  <div>
    <h3 class="vi-title"><span class="material-symbols-outlined" aria-hidden="true">manage_search</span> Kasa İnceleme</h3>

    <div v-if="loading" class="vi-center">
      <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
    </div>

    <template v-else>
      <!-- Selectors -->
      <div class="vi-selectors">
        <div class="vi-field">
          <label>Ofis</label>
          <select v-model="selectedOfficeId" class="vi-input">
            <option v-for="o in offices" :key="o.officeId" :value="o.officeId">{{ o.officeName }}</option>
          </select>
        </div>
        <div class="vi-field">
          <label>Kasa</label>
          <select v-model="selectedVaultId" class="vi-input" :disabled="!vaults.length">
            <option v-for="v in vaults" :key="v.id || v.vaultId" :value="v.id || v.vaultId">
              {{ v.name || v.vaultName }}
            </option>
          </select>
        </div>
      </div>

      <!-- Sub tabs -->
      <div class="vi-subtabs">
        <button :class="{ active: subTab === 'overview' }" @click="subTab = 'overview'">
          <span class="material-symbols-outlined" aria-hidden="true">dashboard</span> Genel Bakış
        </button>
        <button :class="{ active: subTab === 'history' }" @click="subTab = 'history'">
          <span class="material-symbols-outlined" aria-hidden="true">history</span> Hareket Geçmişi
        </button>
        <button :class="{ active: subTab === 'wac' }" @click="subTab = 'wac'">
          <span class="material-symbols-outlined" aria-hidden="true">analytics</span> WAC Analizi
        </button>
        <button :class="{ active: subTab === 'counts' }" @click="subTab = 'counts'">
          <span class="material-symbols-outlined" aria-hidden="true">inventory_2</span> Sayımlar
        </button>
        <button :class="{ active: subTab === 'snapshots' }" @click="subTab = 'snapshots'">
          <span class="material-symbols-outlined" aria-hidden="true">photo_camera</span> Anlık Görüntüler
        </button>
      </div>

      <!-- Overview -->
      <div v-if="subTab === 'overview' && selectedVault" class="vi-section">
        <div class="vi-total-bar">
          <div class="vi-total-label">Toplam Kasa Değeri</div>
          <div class="vi-total-amount">{{ fmtMoney(totalTryValue) }} <small>₺</small></div>
        </div>

        <div class="vi-bal-grid">
          <div v-for="b in balances" :key="b.currencyId" class="vi-bal-card">
            <div class="vi-bal-header">
              <span class="vi-cur-code">{{ b.currencyCode }}</span>
              <span v-if="b.currencyCode !== 'TRY'" class="vi-cur-rate">Kur: {{ fmtMoney(b.exchangeRateToBase) }}</span>
            </div>
            <div class="vi-bal-amount">{{ fmtMoney(b.balance) }}</div>
            <div v-if="b.currencyCode !== 'TRY'" class="vi-bal-try">≈ {{ fmtMoney((b.balance ?? 0) * (b.exchangeRateToBase ?? 1)) }} ₺</div>
            <div v-if="b.wac > 0" class="vi-bal-wac">
              WAC: {{ fmtMoney(b.wac) }} ₺
            </div>
          </div>
        </div>

        <div v-if="!balances.length" class="vi-empty">
          <span class="material-symbols-outlined" aria-hidden="true">account_balance_wallet</span>
          <p>Bu kasada henüz bakiye bulunmuyor</p>
        </div>
      </div>

      <!-- History -->
      <div v-if="subTab === 'history'" class="vi-section">
        <div class="vi-hist-controls">
          <div class="vi-field">
            <label>Tarih</label>
            <input v-model="historyDate" type="date" class="vi-input" @change="loadHistory" />
          </div>
          <button class="vi-btn primary" :disabled="historyLoading" @click="loadHistory">
            <span class="material-symbols-outlined" aria-hidden="true">{{ historyLoading ? 'progress_activity' : 'search' }}</span>
            Sorgula
          </button>
        </div>

        <div v-if="historyLoading" class="vi-center"><span class="material-symbols-outlined spin">progress_activity</span></div>

        <div v-else-if="history.length" class="vi-table-wrap">
          <table class="vi-table">
            <thead>
              <tr>
                <th>Saat</th>
                <th>Tür</th>
                <th>Döviz</th>
                <th>Miktar</th>
                <th>Açıklama</th>
                <th>Kullanıcı</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="h in history" :key="h.id">
                <td class="vi-time">{{ fmtTime(h.createdDate) }}</td>
                <td><span class="vi-type-badge" :class="txTypeClass(h.transactionType)">{{ txTypeLabel(h.transactionType) }}</span></td>
                <td><span class="vi-cur-tag">{{ h.currencyCode || '—' }}</span></td>
                <td class="vi-mono" :class="(h.balance ?? 0) >= 0 ? 'vi-positive' : 'vi-negative'">
                  {{ (h.balance ?? 0) >= 0 ? '+' : '' }}{{ fmtMoney(h.balance) }}
                </td>
                <td class="vi-desc">{{ h.description || '—' }}</td>
                <td>{{ h.user || 'Sistem' }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-else class="vi-empty">
          <span class="material-symbols-outlined" aria-hidden="true">history</span>
          <p>Seçili tarihte hareket bulunamadı</p>
        </div>
      </div>

      <!-- WAC -->
      <div v-if="subTab === 'wac'" class="vi-section">
        <div v-if="balances.filter(b => b.currencyCode !== 'TRY').length" class="vi-wac-grid">
          <div v-for="b in balances.filter(b => b.currencyCode !== 'TRY')" :key="b.currencyId" class="vi-wac-card">
            <div class="vi-wac-header">
              <span class="vi-cur-code">{{ b.currencyCode }}</span>
            </div>
            <div class="vi-wac-row">
              <span class="vi-wac-label">Miktar</span>
              <span class="vi-wac-val">{{ fmtMoney(b.balance) }}</span>
            </div>
            <div class="vi-wac-row">
              <span class="vi-wac-label">WAC (Maliyet)</span>
              <span class="vi-wac-val vi-mono">{{ b.wac > 0 ? fmtMoney(b.wac) + ' ₺' : '—' }}</span>
            </div>
            <div class="vi-wac-row">
              <span class="vi-wac-label">Güncel Kur</span>
              <span class="vi-wac-val vi-mono">{{ fmtMoney(b.exchangeRateToBase) }} ₺</span>
            </div>
            <div v-if="b.wac > 0 && b.balance > 0" class="vi-wac-row vi-wac-pnl">
              <span class="vi-wac-label">Potansiyel K/Z</span>
              <span class="vi-wac-val vi-mono" :class="(b.exchangeRateToBase - b.wac) >= 0 ? 'vi-positive' : 'vi-negative'">
                {{ fmtMoney(((b.exchangeRateToBase ?? 0) - b.wac) * (b.balance ?? 0)) }} ₺
              </span>
            </div>
          </div>
        </div>
        <div v-else class="vi-empty">
          <span class="material-symbols-outlined" aria-hidden="true">analytics</span>
          <p>WAC analizi için döviz bakiyesi gerekli</p>
        </div>
      </div>

      <!-- Counts -->
      <div v-if="subTab === 'counts'" class="vi-section">
        <div v-if="countsLoading" class="vi-center"><span class="material-symbols-outlined spin">progress_activity</span></div>

        <div v-else-if="counts.length" class="vi-table-wrap">
          <table class="vi-table">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Durum</th>
                <th>Fark</th>
                <th>Not</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in counts" :key="c.id || c.countId">
                <td>{{ fmtDate(c.countDate || c.createdDate) }}</td>
                <td>
                  <span class="vi-count-status" :class="c.hasDiscrepancy ? 'discrepancy' : 'ok'">
                    {{ c.hasDiscrepancy ? 'Fark Var' : 'Uyumlu' }}
                  </span>
                </td>
                <td class="vi-desc">{{ c.discrepancyDetails || c.discrepancySummary || '—' }}</td>
                <td class="vi-desc">{{ c.notes || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-else class="vi-empty">
          <span class="material-symbols-outlined" aria-hidden="true">inventory_2</span>
          <p>Sayım kaydı bulunamadı</p>
        </div>
      </div>

      <!-- Snapshots -->
      <div v-if="subTab === 'snapshots'" class="vi-section">
        <div class="vi-snap-header">
          <button class="vi-btn primary" @click="createSnapshot">
            <span class="material-symbols-outlined" aria-hidden="true">add_a_photo</span> Anlık Görüntü Al
          </button>
        </div>

        <div v-if="snapshotsLoading" class="vi-center"><span class="material-symbols-outlined spin">progress_activity</span></div>

        <div v-else-if="snapshots.length" class="vi-snap-list">
          <div v-for="s in snapshots" :key="s.id || s.snapshotId" class="vi-snap-card" @click="viewSnapshot(s.id || s.snapshotId)">
            <div class="vi-snap-date">
              <span class="material-symbols-outlined" aria-hidden="true">photo_camera</span>
              {{ fmtDate(s.snapshotDate || s.createdDate) }}
            </div>
            <div v-if="s.description" class="vi-snap-desc">{{ s.description }}</div>
          </div>
        </div>

        <div v-else class="vi-empty">
          <span class="material-symbols-outlined" aria-hidden="true">photo_camera</span>
          <p>Henüz anlık görüntü alınmamış</p>
        </div>

        <!-- Snapshot Detail Modal -->
        <Teleport to="body">
          <div v-if="snapshotDetail" class="vi-overlay" @click.self="snapshotDetail = null">
            <div class="vi-modal">
              <div class="vi-modal-header">
                <h3>Anlık Görüntü Detayı</h3>
                <button class="vi-close" @click="snapshotDetail = null">&times;</button>
              </div>
              <div class="vi-modal-body">
                <p class="vi-snap-info">{{ fmtDate(snapshotDetail.snapshotDate || snapshotDetail.createdDate) }}</p>
                <div class="vi-table-wrap">
                  <table class="vi-table">
                    <thead>
                      <tr>
                        <th>Kasa</th>
                        <th>Döviz</th>
                        <th>Bakiye</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="d in (snapshotDetail.details || snapshotDetail.balances || [])" :key="d.id">
                        <td>{{ d.vaultName || '—' }}</td>
                        <td><span class="vi-cur-tag">{{ d.currencyCode || '—' }}</span></td>
                        <td class="vi-mono">{{ fmtMoney(d.balance) }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        </Teleport>
      </div>
    </template>
  </div>
</template>

<style scoped>
.vi-title { display: flex; align-items: center; gap: 0.5rem; font-size: 1.1rem; font-weight: 700; color: var(--color-text); margin: 0 0 1.25rem; }
.vi-center { display: flex; align-items: center; gap: 0.75rem; padding: 3rem; justify-content: center; color: var(--color-text-muted); }
@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin 1s linear infinite; }

.vi-selectors { display: flex; gap: 1rem; margin-bottom: 1rem; }
.vi-field label { display: block; font-size: 0.75rem; font-weight: 600; color: var(--color-text-secondary); margin-bottom: 0.25rem; }
.vi-input {
  padding: 0.5rem 0.75rem; border: 2px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 0.85rem; color: var(--color-text); background: var(--color-bg-page); outline: none;
  transition: border-color .2s; min-width: 180px;
}
.vi-input:focus { border-color: var(--color-secondary); background: white; }

.vi-subtabs { display: flex; gap: 0.25rem; margin-bottom: 1.25rem; border-bottom: 2px solid var(--color-border); overflow-x: auto; }
.vi-subtabs button {
  display: flex; align-items: center; gap: 0.35rem;
  padding: 0.55rem 0.9rem; background: none; border: none;
  border-bottom: 2px solid transparent; margin-bottom: -2px;
  cursor: pointer; font-size: 0.82rem; color: var(--color-text-secondary); transition: color 0.2s, border-color 0.2s; white-space: nowrap;
}
.vi-subtabs button:hover { color: var(--color-secondary); }
.vi-subtabs button.active { color: var(--color-secondary); border-bottom-color: var(--color-secondary); font-weight: 600; }
.vi-subtabs .material-symbols-outlined { font-size: 1rem; }

.vi-section { min-height: 200px; }

/* Overview */
.vi-total-bar {
  background: linear-gradient(135deg, #1e40af, #3b82f6); color: white;
  border-radius: var(--radius-lg); padding: 1.25rem 1.5rem; display: flex;
  align-items: center; justify-content: space-between; margin-bottom: 1.25rem;
}
.vi-total-label { font-size: 0.85rem; opacity: 0.85; }
.vi-total-amount { font-size: 1.5rem; font-weight: 800; font-family: monospace; }
.vi-total-amount small { font-size: 0.9rem; opacity: 0.8; }

.vi-bal-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 0.75rem; }
.vi-bal-card {
  background: white; border-radius: var(--radius-lg); padding: 1rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.08); border-left: 4px solid var(--color-secondary);
}
.vi-bal-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.5rem; }
.vi-cur-code { font-weight: 700; font-size: 0.95rem; color: var(--color-text); }
.vi-cur-rate { font-size: 0.7rem; color: var(--color-text-muted); }
.vi-bal-amount { font-size: 1.3rem; font-weight: 800; font-family: monospace; color: var(--color-text); }
.vi-bal-try { font-size: 0.75rem; color: var(--color-text-secondary); margin-top: 0.2rem; }
.vi-bal-wac { font-size: 0.7rem; color: var(--color-warning); margin-top: 0.35rem; font-weight: 600; }

/* History */
.vi-hist-controls { display: flex; gap: 1rem; align-items: flex-end; margin-bottom: 1rem; }
.vi-btn {
  display: flex; align-items: center; gap: 0.35rem;
  padding: 0.5rem 1rem; border: none; border-radius: var(--radius-md);
  cursor: pointer; font-size: 0.85rem; font-weight: 600; transition: background-color 0.2s, color 0.2s;
}
.vi-btn.primary { background: var(--color-secondary); color: white; }
.vi-btn.primary:hover:not(:disabled) { background: var(--color-secondary-hover); }
.vi-btn:disabled { opacity: 0.5; cursor: not-allowed; }

.vi-table-wrap { overflow-x: auto; background: white; border-radius: var(--radius-lg); box-shadow: 0 1px 3px rgba(0,0,0,.08); }
.vi-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
.vi-table th { text-align: left; padding: 0.7rem 1rem; background: var(--color-bg-page); color: var(--color-text-secondary); font-weight: 600; border-bottom: 1px solid var(--color-border); }
.vi-table td { padding: 0.7rem 1rem; border-bottom: 1px solid var(--color-bg-page); }
.vi-time { font-size: 0.8rem; color: var(--color-text-secondary); font-family: monospace; }
.vi-mono { font-family: monospace; }
.vi-positive { color: #16a34a; }
.vi-negative { color: var(--color-danger); }
.vi-desc { font-size: 0.8rem; color: var(--color-text-secondary); max-width: 250px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.vi-type-badge { padding: 2px 8px; border-radius: var(--radius-sm); font-size: 0.7rem; font-weight: 600; }
.vi-in { background: #f0fdf4; color: #16a34a; }
.vi-out { background: #fef2f2; color: var(--color-danger); }
.vi-adj { background: var(--color-warning-bg); color: #ca8a04; }
.vi-xfer { background: #f5f3ff; color: #7c3aed; }
.vi-ex { background: var(--color-secondary-light); color: var(--color-secondary-hover); }

.vi-cur-tag { background: var(--color-secondary-light); color: var(--color-secondary); padding: 2px 8px; border-radius: var(--radius-sm); font-size: 0.75rem; font-weight: 600; }

/* WAC */
.vi-wac-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 0.75rem; }
.vi-wac-card {
  background: white; border-radius: var(--radius-lg); padding: 1.25rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.08);
}
.vi-wac-header { margin-bottom: 0.75rem; }
.vi-wac-row {
  display: flex; justify-content: space-between; align-items: center;
  padding: 0.35rem 0; border-bottom: 1px solid var(--color-bg-page);
}
.vi-wac-row:last-child { border-bottom: none; }
.vi-wac-label { font-size: 0.8rem; color: var(--color-text-secondary); }
.vi-wac-val { font-size: 0.9rem; font-weight: 600; color: var(--color-text); }
.vi-wac-pnl { background: var(--color-bg-page); margin: 0.5rem -0.5rem 0; padding: 0.5rem; border-radius: var(--radius-md); border-bottom: none; }

/* Counts */
.vi-count-status { padding: 2px 10px; border-radius: var(--radius-xl); font-size: 0.7rem; font-weight: 600; }
.vi-count-status.ok { background: #f0fdf4; color: #16a34a; }
.vi-count-status.discrepancy { background: #fef2f2; color: var(--color-danger); }

/* Snapshots */
.vi-snap-header { margin-bottom: 1rem; }
.vi-snap-list { display: flex; flex-direction: column; gap: 0.5rem; }
.vi-snap-card {
  background: white; border-radius: var(--radius-md); padding: 0.75rem 1rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.06); cursor: pointer; transition: box-shadow 0.15s;
  display: flex; align-items: center; gap: 0.75rem;
}
.vi-snap-card:hover { box-shadow: 0 2px 8px rgba(0,0,0,.12); }
.vi-snap-date { display: flex; align-items: center; gap: 0.4rem; font-size: 0.85rem; color: var(--color-text); font-weight: 600; }
.vi-snap-date .material-symbols-outlined { font-size: 1rem; color: var(--color-secondary); }
.vi-snap-desc { font-size: 0.8rem; color: var(--color-text-secondary); }
.vi-snap-info { font-size: 0.85rem; color: var(--color-text-secondary); margin-bottom: 1rem; }

.vi-empty { display: flex; flex-direction: column; align-items: center; gap: 0.75rem; padding: 3rem; color: var(--color-text-muted); }

/* Modal */
.vi-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.4); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.vi-modal { background: white; border-radius: var(--radius-lg); width: 95%; max-width: 600px; max-height: 85vh; overflow-y: auto; }
.vi-modal-header { display: flex; align-items: center; justify-content: space-between; padding: 1.25rem 1.5rem; border-bottom: 1px solid var(--color-border); }
.vi-modal-header h3 { margin: 0; font-size: 1.1rem; }
.vi-close { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: var(--color-text-secondary); }
.vi-modal-body { padding: 1.5rem; }

@media (max-width: 640px) {
  .vi-selectors { flex-direction: column; }
  .vi-input { min-width: 100%; }
  .vi-bal-grid { grid-template-columns: 1fr; }
  .vi-wac-grid { grid-template-columns: 1fr; }
  .vi-hist-controls { flex-direction: column; align-items: stretch; }
}
</style>
