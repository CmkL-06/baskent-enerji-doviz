<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import type { OfficeTransfer } from '@/types/api'

const router        = useRouter()
const authStore     = useAuthStore()
const exchangeStore = useExchangeStore()

type Tab = 'pending' | 'all'
const activeTab  = ref<Tab>('pending')

const pending    = ref<OfficeTransfer[]>([])
const allTx      = ref<OfficeTransfer[]>([])
const loading    = ref(true)
const error      = ref('')

const rejectModal    = ref(false)
const rejectTarget   = ref<string | null>(null)
const rejectReason   = ref('')
const rejectSaving   = ref(false)

const createModal  = ref(false)
const createSaving = ref(false)
const createError  = ref('')
const vaults       = ref<any[]>([])
const currencies   = ref<any[]>([])

const createForm = ref({
  sourceVaultId: '',
  targetVaultId: '',
  currencyId:    '',
  amount:        null as number | null,
  notes:         '',
})

onMounted(async () => {
  if (!authStore.isAdmin) { router.push('/ihtiyar/dashboard'); return }
  await Promise.all([loadPending(), loadVaultsCurrencies()])
})

async function loadPending() {
  loading.value = true
  error.value   = ''
  try {
    if (authStore.isAdmin) {
      pending.value = await apiService.getPendingTransfers() ?? []
    }
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Veri yüklenemedi'
  } finally {
    loading.value = false
  }
}

async function loadAll() {
  loading.value = true
  error.value   = ''
  try {
    const myAccess = await apiService.getMyOfficeAccess() ?? []
    const results: OfficeTransfer[] = []
    for (const uo of myAccess) {
      const txs = await apiService.getTransfersByOffice(uo.officeId) ?? []
      results.push(...txs)
    }
    const seen = new Set<string>()
    allTx.value = results.filter(t => { if (seen.has(t.id)) return false; seen.add(t.id); return true })
      .sort((a, b) => new Date(b.createdDate).getTime() - new Date(a.createdDate).getTime())
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Geçmiş yüklenemedi'
  } finally {
    loading.value = false
  }
}

async function loadVaultsCurrencies() {
  try {
    const [v, c] = await Promise.all([
      apiService.getVaults(),
      apiService.getCurrencies(),
    ])
    vaults.value    = Array.isArray(v) ? v : (v?.items ?? v?.data ?? [])
    currencies.value = Array.isArray(c) ? c : (c?.items ?? c?.data ?? [])
  } catch {}
}

async function switchTab(tab: Tab) {
  activeTab.value = tab
  error.value     = ''
  if (tab === 'pending') await loadPending()
  else await loadAll()
}

async function approve(id: string) {
  try {
    await apiService.processTransfer(id, { approve: true })
    await loadPending()
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Onaylama başarısız'
  }
}

function openReject(id: string) {
  rejectTarget.value = id
  rejectReason.value = ''
  rejectModal.value  = true
}

async function confirmReject() {
  if (!rejectTarget.value) return
  rejectSaving.value = true
  try {
    await apiService.processTransfer(rejectTarget.value, { approve: false, rejectionReason: rejectReason.value })
    rejectModal.value = false
    await loadPending()
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Red işlemi başarısız'
  } finally {
    rejectSaving.value = false
  }
}

function openCreate() {
  createError.value = ''
  createForm.value  = { sourceVaultId: '', targetVaultId: '', currencyId: '', amount: null, notes: '' }
  createModal.value = true
}

async function submitCreate() {
  if (!createForm.value.sourceVaultId || !createForm.value.targetVaultId || !createForm.value.currencyId || !createForm.value.amount) {
    createError.value = 'Tüm zorunlu alanları doldurun.'
    return
  }
  if (createForm.value.sourceVaultId === createForm.value.targetVaultId) {
    createError.value = 'Kaynak ve hedef kasa aynı olamaz.'
    return
  }
  createSaving.value = true
  createError.value  = ''
  try {
    await apiService.createTransferRequest({ ...createForm.value })
    createModal.value = false
    await loadPending()
  } catch (e: any) {
    createError.value = e?.response?.data?.error || e.message || 'Talep oluşturulamadı'
  } finally {
    createSaving.value = false
  }
}

function statusClass(status: string) {
  return {
    Pending:   'status-pending',
    Completed: 'status-completed',
    Rejected:  'status-rejected',
    Approved:  'status-approved',
    Cancelled: 'status-cancelled',
  }[status] ?? 'status-default'
}

function statusLabel(s: string) {
  return { Pending:'Bekliyor', Completed:'Tamamlandı', Rejected:'Reddedildi', Approved:'Onaylandı', Cancelled:'İptal' }[s] ?? s
}

function fmt(n: number) { return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function fmtDate(d: string) { return new Date(d).toLocaleString('tr-TR', { day:'2-digit', month:'short', hour:'2-digit', minute:'2-digit' }) }
</script>

<template>
  <div class="office-transfers">
    <!-- Header -->
    <div class="ot-header">
      <div>
        <h1 class="ot-title">Ofislerarası Transferler</h1>
        <p class="ot-subtitle">Merkez ↔ Şube/Bayi para transferleri</p>
      </div>
      <button class="btn-primary" @click="openCreate">
        <span class="material-symbols-outlined" aria-hidden="true">send</span>
        Transfer Talebi
      </button>
    </div>

    <!-- Tabs -->
    <div class="tab-group">
      <button
        v-for="t in [{ id: 'pending', label: 'Bekleyen', icon: 'hourglass_empty' }, { id: 'all', label: 'Tümü', icon: 'history' }]"
        :key="t.id"
        @click="switchTab(t.id as Tab)"
        :class="['tab-btn', { active: activeTab === t.id }]"
      >
        <span class="material-symbols-outlined tab-icon">{{ t.icon }}</span>
        {{ t.label }}
        <span v-if="t.id === 'pending' && pending.length > 0" class="badge-count">{{ pending.length }}</span>
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="state-center">
      <div class="spinner"></div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="error-box">{{ error }}</div>

    <!-- Pending list -->
    <div v-else-if="activeTab === 'pending'">
      <div v-if="!authStore.isAdmin" class="state-center">
        <span class="material-symbols-outlined state-icon">lock</span>
        <p>Bekleyen transferleri görmek için Admin yetkisi gereklidir.</p>
      </div>
      <div v-else-if="pending.length === 0" class="state-center">
        <span class="material-symbols-outlined state-icon">check_circle</span>
        <p>Bekleyen transfer yok.</p>
      </div>
      <div v-else class="transfer-list">
        <div v-for="t in pending" :key="t.id" class="transfer-card">
          <div class="transfer-body">
            <div class="transfer-info">
              <div class="transfer-route">
                <span class="office-name">{{ t.sourceOfficeName }}</span>
                <span class="material-symbols-outlined route-arrow">arrow_forward</span>
                <span class="office-name">{{ t.targetOfficeName }}</span>
                <span :class="['status-badge', statusClass(t.status)]">{{ statusLabel(t.status) }}</span>
              </div>
              <div class="transfer-details">
                <span class="amount-highlight">{{ fmt(t.amount) }} {{ t.currencyCode }}</span>
                <span class="vault-info">{{ t.sourceVaultName }} → {{ t.targetVaultName }}</span>
              </div>
              <div class="transfer-meta">
                <span>Talep: <strong>{{ t.requestedByName }}</strong></span>
                <span>{{ fmtDate(t.createdDate) }}</span>
                <span v-if="t.notes" class="transfer-note">"{{ t.notes }}"</span>
              </div>
            </div>
            <div class="transfer-actions">
              <button class="btn-approve" @click="approve(t.id)">
                <span class="material-symbols-outlined" aria-hidden="true">check</span>
                Onayla
              </button>
              <button class="btn-reject" @click="openReject(t.id)">
                <span class="material-symbols-outlined" aria-hidden="true">close</span>
                Reddet
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- All transfers list -->
    <div v-else>
      <div v-if="allTx.length === 0" class="state-center">
        <span class="material-symbols-outlined state-icon">history</span>
        <p>Transfer geçmişi bulunamadı.</p>
      </div>
      <div v-else class="transfer-list">
        <div v-for="t in allTx" :key="t.id" class="transfer-row">
          <span :class="['status-badge', statusClass(t.status)]">{{ statusLabel(t.status) }}</span>
          <span class="transfer-route-inline">
            <strong>{{ t.sourceOfficeName }}</strong>
            <span class="route-sep">→</span>
            <strong>{{ t.targetOfficeName }}</strong>
          </span>
          <span class="amount-highlight">{{ fmt(t.amount) }} {{ t.currencyCode }}</span>
          <span class="date-text">{{ fmtDate(t.createdDate) }}</span>
          <span v-if="t.rejectionReason" class="rejection-text">Red: {{ t.rejectionReason }}</span>
        </div>
      </div>
    </div>

    <!-- Reject Modal -->
    <Teleport to="body">
      <div v-if="rejectModal" class="modal-overlay">
        <div class="modal-card">
          <h3 class="modal-title modal-title-danger">Transferi Reddet</h3>
          <label class="form-label">Red Gerekçesi (opsiyonel)</label>
          <input
            v-model="rejectReason"
            type="text"
            placeholder="Bakiye yetersiz, yanlış hesap vb."
            class="form-input"
          />
          <div class="modal-actions">
            <button class="btn-cancel" @click="rejectModal = false">İptal</button>
            <button class="btn-danger" @click="confirmReject" :disabled="rejectSaving">
              {{ rejectSaving ? 'Reddetiliyor…' : 'Reddet' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Create Transfer Modal -->
    <Teleport to="body">
      <div v-if="createModal" class="modal-overlay">
        <div class="modal-card modal-wide">
          <div class="modal-header">
            <h2 class="modal-title">Transfer Talebi Oluştur</h2>
            <button class="btn-icon" @click="createModal = false">
              <span class="material-symbols-outlined" aria-hidden="true">close</span>
            </button>
          </div>
          <div class="modal-body">
            <div class="form-group">
              <label class="form-label">Kaynak Kasa *</label>
              <select v-model="createForm.sourceVaultId" class="form-input">
                <option value="">Seçin…</option>
                <option v-for="v in vaults" :key="v.id" :value="v.id">{{ v.name }}</option>
              </select>
            </div>
            <div class="form-group">
              <label class="form-label">Hedef Kasa *</label>
              <select v-model="createForm.targetVaultId" class="form-input">
                <option value="">Seçin…</option>
                <option v-for="v in vaults" :key="v.id" :value="v.id">{{ v.name }}</option>
              </select>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Para Birimi *</label>
                <select v-model="createForm.currencyId" class="form-input">
                  <option value="">Seçin…</option>
                  <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.currencyCode }}</option>
                </select>
              </div>
              <div class="form-group">
                <label class="form-label">Miktar *</label>
                <input
                  v-model.number="createForm.amount"
                  type="number"
                  min="0.01"
                  step="0.01"
                  placeholder="0.00"
                  class="form-input"
                />
              </div>
            </div>
            <div class="form-group">
              <label class="form-label">Not</label>
              <input
                v-model="createForm.notes"
                type="text"
                placeholder="İsteğe bağlı açıklama"
                class="form-input"
              />
            </div>
            <div v-if="createError" class="error-box">{{ createError }}</div>
          </div>
          <div class="modal-footer">
            <button class="btn-cancel" @click="createModal = false">İptal</button>
            <button class="btn-primary" @click="submitCreate" :disabled="createSaving">
              <div v-if="createSaving" class="spinner-sm"></div>
              {{ createSaving ? 'Gönderiliyor…' : 'Gönder' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.office-transfers {
  padding: 24px;
}

.ot-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.ot-title {
  font-size: 22px;
  font-weight: 700;
  color: #1a1a1a;
  margin: 0;
}

.ot-subtitle {
  font-size: 13px;
  color: #6b7280;
  margin: 4px 0 0;
}

/* Buttons */
.btn-primary {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 20px;
  background: #5a8cff;
  color: white;
  border: none;
  border-radius: 10px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-primary:hover { background: #4a7ce5; }
.btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }

.btn-primary .material-symbols-outlined,
.btn-approve .material-symbols-outlined,
.btn-reject .material-symbols-outlined {
  font-size: 18px;
}

.btn-approve {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 6px 14px;
  background: #22c55e;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-approve:hover { background: #16a34a; }

.btn-reject {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 6px 14px;
  background: #ef4444;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-reject:hover { background: #dc2626; }

.btn-cancel {
  padding: 8px 16px;
  background: none;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  color: #6b7280;
  font-size: 14px;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}

.btn-cancel:hover { color: #1a1a1a; border-color: #d1d5db; }

.btn-danger {
  padding: 8px 20px;
  background: #ef4444;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-danger:hover { background: #dc2626; }
.btn-danger:disabled { opacity: 0.5; cursor: not-allowed; }

.btn-icon {
  background: none;
  border: none;
  color: #9ca3af;
  cursor: pointer;
  padding: 4px;
  border-radius: 6px;
  transition: color 0.2s;
}

.btn-icon:hover { color: #1a1a1a; }

/* Tabs */
.tab-group {
  display: flex;
  gap: 4px;
  padding: 4px;
  background: #f3f4f6;
  border-radius: 12px;
  margin-bottom: 24px;
  width: fit-content;
}

.tab-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  color: #6b7280;
  background: transparent;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s, box-shadow 0.2s;
}

.tab-btn.active {
  background: white;
  color: #1a1a1a;
  font-weight: 500;
  box-shadow: 0 1px 3px rgba(0,0,0,0.1);
}

.tab-icon {
  font-size: 18px;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}

.badge-count {
  background: #f59e0b;
  color: #1a1a1a;
  font-size: 11px;
  font-weight: 700;
  padding: 1px 7px;
  border-radius: 10px;
  line-height: 1.4;
}

/* States */
.state-center {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: #9ca3af;
  text-align: center;
}

.state-icon {
  font-size: 48px;
  margin-bottom: 12px;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}

.error-box {
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 12px;
  padding: 12px 16px;
  color: #b91c1c;
  font-size: 14px;
  margin-bottom: 16px;
}

.spinner {
  width: 32px;
  height: 32px;
  border: 3px solid #e5e7eb;
  border-top-color: #5a8cff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.spinner-sm {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

/* Transfer Cards */
.transfer-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.transfer-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 14px;
  padding: 16px 20px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.04);
}

.transfer-body {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.transfer-info {
  flex: 1;
  min-width: 0;
}

.transfer-route {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  margin-bottom: 8px;
}

.office-name {
  font-weight: 600;
  color: #1a1a1a;
  font-size: 15px;
}

.route-arrow {
  font-size: 16px;
  color: #9ca3af;
}

.transfer-details {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
  font-size: 14px;
  margin-bottom: 6px;
}

.amount-highlight {
  font-family: monospace;
  font-weight: 700;
  color: #d97706;
  font-size: 15px;
}

.vault-info {
  color: #9ca3af;
  font-size: 13px;
}

.transfer-meta {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
  font-size: 12px;
  color: #9ca3af;
}

.transfer-meta strong {
  color: #6b7280;
}

.transfer-note {
  font-style: italic;
  color: #9ca3af;
}

.transfer-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

/* Transfer row (all tab) */
.transfer-row {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 12px 16px;
}

.transfer-route-inline {
  font-size: 14px;
  color: #374151;
}

.route-sep {
  color: #9ca3af;
  margin: 0 4px;
}

.date-text {
  font-size: 12px;
  color: #9ca3af;
  margin-left: auto;
}

.rejection-text {
  font-size: 12px;
  color: #ef4444;
}

/* Status badges */
.status-badge {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 600;
  border: 1px solid;
  white-space: nowrap;
}

.status-pending {
  background: #fef3c7;
  color: #92400e;
  border-color: #fcd34d;
}

.status-completed {
  background: #d1fae5;
  color: #065f46;
  border-color: #6ee7b7;
}

.status-rejected {
  background: #fee2e2;
  color: #991b1b;
  border-color: #fca5a5;
}

.status-approved {
  background: #dbeafe;
  color: #1e40af;
  border-color: #93c5fd;
}

.status-cancelled {
  background: #f3f4f6;
  color: #6b7280;
  border-color: #d1d5db;
}

.status-default {
  background: #f3f4f6;
  color: #6b7280;
  border-color: #d1d5db;
}

/* Modal */
.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
  background: rgba(0,0,0,0.5);
}

.modal-card {
  background: white;
  border-radius: 16px;
  width: 100%;
  max-width: 420px;
  box-shadow: 0 20px 60px rgba(0,0,0,0.2);
  padding: 24px;
}

.modal-wide {
  max-width: 480px;
  padding: 0;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid #f3f4f6;
}

.modal-title {
  font-size: 18px;
  font-weight: 600;
  color: #1a1a1a;
  margin: 0 0 16px;
}

.modal-title-danger {
  color: #ef4444;
}

.modal-body {
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 24px;
  border-top: 1px solid #f3f4f6;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 8px;
}

/* Form */
.form-group {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.form-label {
  font-size: 12px;
  color: #6b7280;
  font-weight: 500;
}

.form-input {
  width: 100%;
  padding: 10px 12px;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  font-size: 14px;
  color: #1a1a1a;
  outline: none;
  transition: border-color 0.2s;
  box-sizing: border-box;
}

.form-input:focus {
  border-color: #5a8cff;
}

.form-input::placeholder {
  color: #9ca3af;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.material-symbols-outlined {
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
</style>
