<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import apiService from '@/services/apiservice'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()

const subTab = ref<'pending' | 'all'>('pending')
const pending = ref<any[]>([])
const allTransfers = ref<any[]>([])
const vaults = ref<any[]>([])
const currencies = ref<any[]>([])
const loading = ref(true)
const error = ref('')

const showCreate = ref(false)
const showReject = ref(false)
const rejectId = ref('')
const rejectReason = ref('')
const saving = ref(false)

const form = ref({
  sourceVaultId: '',
  targetVaultId: '',
  currencyId: '',
  amount: null as number | null,
  notes: '',
})

onMounted(loadData)

async function loadData() {
  loading.value = true; error.value = ''
  try {
    const [p, v, c] = await Promise.all([
      apiService.getPendingTransfers().catch(() => []),
      apiService.getVaults().catch(() => []),
      apiService.getCurrencies().catch(() => []),
    ])
    pending.value = Array.isArray(p) ? p : (p?.data ?? [])
    vaults.value = Array.isArray(v) ? v : (v?.data ?? [])
    currencies.value = Array.isArray(c) ? c : (c?.data ?? [])
  } catch (e: any) {
    error.value = e?.message || 'Veri yüklenemedi'
  } finally {
    loading.value = false
  }
}

async function loadAllTransfers() {
  try {
    const offices = await apiService.getOfficeSummaries()
    const all: any[] = []
    for (const o of (offices || [])) {
      const t = await apiService.getTransfersByOffice(o.officeId)
      all.push(...(Array.isArray(t) ? t : (t?.data ?? [])))
    }
    allTransfers.value = all.sort((a: any, b: any) => new Date(b.createdDate).getTime() - new Date(a.createdDate).getTime())
  } catch { allTransfers.value = [] }
}

async function approve(id: string) {
  saving.value = true
  try {
    await apiService.processTransfer(id, { approve: true })
    await loadData()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Onaylama hatası')
  } finally { saving.value = false }
}

function openReject(id: string) {
  rejectId.value = id; rejectReason.value = ''; showReject.value = true
}

async function submitReject() {
  saving.value = true
  try {
    await apiService.processTransfer(rejectId.value, { approve: false, rejectionReason: rejectReason.value })
    showReject.value = false
    await loadData()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Reddetme hatası')
  } finally { saving.value = false }
}

async function submitCreate() {
  if (!form.value.sourceVaultId || !form.value.targetVaultId || !form.value.currencyId || !form.value.amount || form.value.amount <= 0) return
  saving.value = true
  try {
    await apiService.createTransferRequest(form.value)
    showCreate.value = false
    form.value = { sourceVaultId: '', targetVaultId: '', currencyId: '', amount: null, notes: '' }
    await loadData()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Transfer oluşturma hatası')
  } finally { saving.value = false }
}

function fmtMoney(n: number | null | undefined) {
  if (n == null) return '—'
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtDate(d: string) {
  if (!d) return '—'
  return new Date(d).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function statusBadge(s: string) {
  const map: Record<string, { label: string; cls: string }> = {
    Pending: { label: 'Bekliyor', cls: 'st-pending' },
    Approved: { label: 'Onaylandı', cls: 'st-approved' },
    Completed: { label: 'Tamamlandı', cls: 'st-completed' },
    Rejected: { label: 'Reddedildi', cls: 'st-rejected' },
    Cancelled: { label: 'İptal', cls: 'st-cancelled' },
  }
  return map[s] ?? { label: s, cls: '' }
}

const vaultLabel = (id: string) => {
  const v = vaults.value.find((x: any) => (x.id || x.vaultId) === id)
  return v ? `${v.name || v.vaultName} (${v.officeName || ''})` : id
}
</script>

<template>
  <div>
    <div class="ot-header">
      <h3 class="ot-title"><span class="material-symbols-outlined" aria-hidden="true">swap_horiz</span> Şubeler Arası Transferler</h3>
      <button class="ot-btn primary" @click="showCreate = true">
        <span class="material-symbols-outlined" aria-hidden="true">add</span> Yeni Transfer
      </button>
    </div>

    <div class="ot-subtabs">
      <button :class="{ active: subTab === 'pending' }" @click="subTab = 'pending'">
        Bekleyen
        <span v-if="pending.length" class="ot-count">{{ pending.length }}</span>
      </button>
      <button :class="{ active: subTab === 'all' }" @click="subTab = 'all'; loadAllTransfers()">
        Tüm Transferler
      </button>
    </div>

    <div v-if="loading" class="ot-center">
      <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
    </div>

    <!-- Pending -->
    <div v-else-if="subTab === 'pending'">
      <div v-if="pending.length === 0" class="ot-empty">
        <span class="material-symbols-outlined" aria-hidden="true">check_circle</span>
        <p>Bekleyen transfer yok</p>
      </div>
      <div v-else class="ot-card-list">
        <div v-for="t in pending" :key="t.id" class="ot-transfer-card">
          <div class="ot-route">
            <span class="ot-office">{{ t.sourceOfficeName }}</span>
            <span class="material-symbols-outlined ot-arrow">arrow_forward</span>
            <span class="ot-office">{{ t.targetOfficeName }}</span>
          </div>
          <div class="ot-amount">{{ fmtMoney(t.amount) }} <span class="ot-currency">{{ t.currencyCode }}</span></div>
          <div class="ot-meta">
            <span>{{ t.sourceVaultName }} → {{ t.targetVaultName }}</span>
            <span>{{ t.requestedByName }} · {{ fmtDate(t.createdDate) }}</span>
          </div>
          <div v-if="t.notes" class="ot-notes">{{ t.notes }}</div>
          <div class="ot-actions">
            <button class="ot-btn success" :disabled="saving" @click="approve(t.id)">
              <span class="material-symbols-outlined" aria-hidden="true">check</span> Onayla
            </button>
            <button class="ot-btn danger" :disabled="saving" @click="openReject(t.id)">
              <span class="material-symbols-outlined" aria-hidden="true">close</span> Reddet
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- All -->
    <div v-else-if="subTab === 'all'">
      <div v-if="allTransfers.length === 0" class="ot-empty">
        <span class="material-symbols-outlined" aria-hidden="true">swap_horiz</span>
        <p>Transfer geçmişi bulunamadı</p>
      </div>
      <div v-else class="ot-table-wrap">
        <table class="ot-table">
          <thead>
            <tr>
              <th>Tarih</th>
              <th>Kaynak → Hedef</th>
              <th>Para Birimi</th>
              <th>Tutar</th>
              <th>Durum</th>
              <th>Talep Eden</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="t in allTransfers" :key="t.id">
              <td class="ot-date">{{ fmtDate(t.createdDate) }}</td>
              <td>{{ t.sourceOfficeName }} → {{ t.targetOfficeName }}</td>
              <td><span class="ot-cur-badge">{{ t.currencyCode }}</span></td>
              <td class="ot-mono">{{ fmtMoney(t.amount) }}</td>
              <td><span class="ot-status" :class="statusBadge(t.status).cls">{{ statusBadge(t.status).label }}</span></td>
              <td>{{ t.requestedByName }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Create Modal -->
    <Teleport to="body">
      <div v-if="showCreate" class="ot-overlay" @click.self="showCreate = false">
        <div class="ot-modal">
          <div class="ot-modal-header">
            <h3>Yeni Transfer Talebi</h3>
            <button class="ot-close" @click="showCreate = false">&times;</button>
          </div>
          <div class="ot-modal-body">
            <div class="ot-field">
              <label>Kaynak Kasa</label>
              <select v-model="form.sourceVaultId" class="ot-input">
                <option value="">Seçin...</option>
                <option v-for="v in vaults" :key="v.id || v.vaultId" :value="v.id || v.vaultId">
                  {{ v.name || v.vaultName }} ({{ v.officeName || '' }})
                </option>
              </select>
            </div>
            <div class="ot-field">
              <label>Hedef Kasa</label>
              <select v-model="form.targetVaultId" class="ot-input">
                <option value="">Seçin...</option>
                <option v-for="v in vaults" :key="v.id || v.vaultId" :value="v.id || v.vaultId" :disabled="(v.id || v.vaultId) === form.sourceVaultId">
                  {{ v.name || v.vaultName }} ({{ v.officeName || '' }})
                </option>
              </select>
            </div>
            <div class="ot-field-row">
              <div class="ot-field">
                <label>Para Birimi</label>
                <select v-model="form.currencyId" class="ot-input">
                  <option value="">Seçin...</option>
                  <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.currencyCode }}</option>
                </select>
              </div>
              <div class="ot-field">
                <label>Tutar</label>
                <input v-model.number="form.amount" type="number" step="0.01" class="ot-input" placeholder="0.00" />
              </div>
            </div>
            <div class="ot-field">
              <label>Not</label>
              <textarea v-model="form.notes" class="ot-input" rows="2" placeholder="Opsiyonel not"></textarea>
            </div>
          </div>
          <div class="ot-modal-footer">
            <button class="ot-btn secondary" @click="showCreate = false">İptal</button>
            <button class="ot-btn primary" :disabled="saving || !form.sourceVaultId || !form.targetVaultId || !form.currencyId || !form.amount" @click="submitCreate">
              {{ saving ? 'Gönderiliyor...' : 'Transfer Oluştur' }}
            </button>
          </div>
        </div>
      </div>

      <!-- Reject Modal -->
      <div v-if="showReject" class="ot-overlay" @click.self="showReject = false">
        <div class="ot-modal" style="max-width: 400px;">
          <div class="ot-modal-header">
            <h3>Transferi Reddet</h3>
            <button class="ot-close" @click="showReject = false">&times;</button>
          </div>
          <div class="ot-modal-body">
            <div class="ot-field">
              <label>Red Sebebi</label>
              <textarea v-model="rejectReason" class="ot-input" rows="3" placeholder="Neden reddedildiğini yazın..."></textarea>
            </div>
          </div>
          <div class="ot-modal-footer">
            <button class="ot-btn secondary" @click="showReject = false">İptal</button>
            <button class="ot-btn danger" :disabled="saving" @click="submitReject">Reddet</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.ot-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
.ot-title { display: flex; align-items: center; gap: 0.5rem; font-size: 1.1rem; font-weight: 700; color: var(--color-text); margin: 0; }

.ot-subtabs { display: flex; gap: 0.5rem; margin-bottom: 1rem; }
.ot-subtabs button {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.5rem 1rem; background: var(--color-bg-page); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); cursor: pointer; font-size: 0.85rem; color: var(--color-text-secondary);
  transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}
.ot-subtabs button.active { background: var(--color-secondary); color: white; border-color: var(--color-secondary); }
.ot-count { background: var(--color-danger); color: white; font-size: 0.7rem; padding: 1px 6px; border-radius: var(--radius-md); font-weight: 700; }

.ot-center, .ot-empty { display: flex; flex-direction: column; align-items: center; gap: 0.75rem; padding: 3rem; color: var(--color-text-muted); }
@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin 1s linear infinite; }

.ot-card-list { display: flex; flex-direction: column; gap: 0.75rem; }
.ot-transfer-card {
  background: white; border-radius: var(--radius-lg); padding: 1.25rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.08); border-left: 4px solid var(--color-warning);
}
.ot-route { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.5rem; }
.ot-office { font-weight: 600; color: var(--color-text); }
.ot-arrow { color: var(--color-warning); font-size: 1.25rem; }
.ot-amount { font-size: 1.25rem; font-weight: 800; color: var(--color-text); font-family: monospace; margin-bottom: 0.5rem; }
.ot-currency { font-size: 0.9rem; color: var(--color-warning); font-weight: 600; }
.ot-meta { display: flex; flex-direction: column; gap: 0.15rem; font-size: 0.8rem; color: var(--color-text-secondary); margin-bottom: 0.5rem; }
.ot-notes { font-size: 0.8rem; color: var(--color-text-secondary); font-style: italic; padding: 0.4rem 0.6rem; background: var(--color-bg-page); border-radius: var(--radius-sm); margin-bottom: 0.75rem; }
.ot-actions { display: flex; gap: 0.5rem; }

.ot-table-wrap { overflow-x: auto; background: white; border-radius: var(--radius-lg); box-shadow: 0 1px 3px rgba(0,0,0,.08); }
.ot-table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
.ot-table th { text-align: left; padding: 0.75rem 1rem; background: var(--color-bg-page); color: var(--color-text-secondary); font-weight: 600; border-bottom: 1px solid var(--color-border); }
.ot-table td { padding: 0.75rem 1rem; border-bottom: 1px solid var(--color-bg-page); }
.ot-date { font-size: 0.8rem; color: var(--color-text-secondary); }
.ot-mono { font-family: monospace; }
.ot-cur-badge { background: var(--color-secondary-light); color: var(--color-secondary); padding: 2px 8px; border-radius: var(--radius-sm); font-size: 0.75rem; font-weight: 600; }
.ot-status { padding: 3px 10px; border-radius: var(--radius-xl); font-size: 0.7rem; font-weight: 600; }
.st-pending { background: #fffbeb; color: #b45309; }
.st-approved, .st-completed { background: #f0fdf4; color: #16a34a; }
.st-rejected { background: #fef2f2; color: var(--color-danger); }
.st-cancelled { background: var(--color-bg-page); color: var(--color-text-secondary); }

/* Modal */
.ot-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.4); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.ot-modal { background: white; border-radius: var(--radius-lg); width: 95%; max-width: 520px; max-height: 90vh; overflow-y: auto; }
.ot-modal-header { display: flex; align-items: center; justify-content: space-between; padding: 1.25rem 1.5rem; border-bottom: 1px solid var(--color-border); }
.ot-modal-header h3 { margin: 0; font-size: 1.1rem; }
.ot-close { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: var(--color-text-secondary); }
.ot-modal-body { padding: 1.5rem; }
.ot-modal-footer { display: flex; justify-content: flex-end; gap: 0.75rem; padding: 1rem 1.5rem; border-top: 1px solid var(--color-border); }

.ot-field { margin-bottom: 1rem; }
.ot-field label { display: block; font-size: 0.8rem; font-weight: 600; color: var(--color-text-secondary); margin-bottom: 0.3rem; }
.ot-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.ot-input {
  width: 100%; padding: 0.6rem 0.75rem; border: 2px solid var(--color-border);
  border-radius: var(--radius-md); font-size: 0.9rem; color: var(--color-text);
  background: var(--color-bg-page); outline: none; box-sizing: border-box;
  transition: border-color .2s;
}
.ot-input:focus { border-color: var(--color-secondary); background: white; }

.ot-btn {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.5rem 1rem; border: none; border-radius: var(--radius-md);
  cursor: pointer; font-size: 0.85rem; font-weight: 600; transition: background-color 0.2s, color 0.2s;
}
.ot-btn.primary { background: var(--color-secondary); color: white; }
.ot-btn.primary:hover:not(:disabled) { background: var(--color-secondary-hover); }
.ot-btn.secondary { background: var(--color-bg-page); color: var(--color-text-secondary); border: 1px solid var(--color-border); }
.ot-btn.success { background: #22c55e; color: white; }
.ot-btn.success:hover:not(:disabled) { background: #16a34a; }
.ot-btn.danger { background: var(--color-danger); color: white; }
.ot-btn.danger:hover:not(:disabled) { background: var(--color-danger); }
.ot-btn:disabled { opacity: 0.5; cursor: not-allowed; }
</style>
