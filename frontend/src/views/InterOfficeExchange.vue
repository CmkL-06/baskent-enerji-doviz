<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import { formatAmount } from '@/utils/currency'
import AppKpiCard from '@/components/common/AppKpiCard.vue'
import AppPageHeader from '@/components/common/AppPageHeader.vue'
import { useNotification } from '@/composables/useNotification'

const authStore = useAuthStore()
const notification = useNotification()
const exchangeStore = useExchangeStore()

const loading = ref(true)
const saving = ref(false)
const processingIds = ref(new Set<string>())

const pendingTransfers = ref<any[]>([])
const officeTransfers = ref<any[]>([])
const selectedTransfer = ref<any>(null)

const activeTab = ref<'pending' | 'history'>(authStore.isAdmin ? 'pending' : 'history')
const showCreateModal = ref(false)
const showDetailModal = ref(false)

const transferForm = ref({
  sourceOfficeId: '' as string | number,
  targetOfficeId: '' as string | number,
  currencyId: '' as string | number,
  amount: null as number | null,
  description: '',
})

const officeId = computed(() =>
  exchangeStore.selectedOffice?.officeId ?? exchangeStore.offices[0]?.officeId
)

const formatCurrency = (amount: number): string => formatAmount(amount, 2)

const formatDateTime = (d: string | null | undefined): string => {
  if (!d) return '-'
  return new Date(d).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

async function loadPending() {
  if (!authStore.isAdmin) return // Bekleyen transferleri onaylama yetkisi sadece Admin/Owner'da
  try {
    const data = await apiService.getPendingTransfers()
    pendingTransfers.value = Array.isArray(data) ? data : (data?.items ?? [])
  } catch { pendingTransfers.value = [] }
}

async function loadHistory() {
  if (!officeId.value) return
  try {
    const data = await apiService.getTransfersByOffice(String(officeId.value))
    officeTransfers.value = Array.isArray(data) ? data : (data?.items ?? [])
  } catch { officeTransfers.value = [] }
}

async function loadDetail(transfer: any) {
  try {
    selectedTransfer.value = await apiService.getTransferById(String(transfer.id))
    showDetailModal.value = true
  } catch { notification.error('Detay yüklenemedi') }
}

function openCreateModal() {
  transferForm.value = {
    sourceOfficeId: officeId.value ?? '',
    targetOfficeId: '',
    currencyId: exchangeStore.currencies[0]?.id ?? '',
    amount: null,
    description: '',
  }
  showCreateModal.value = true
}

async function createTransfer() {
  if (!transferForm.value.amount || transferForm.value.amount <= 0) {
    notification.warning('Tutar giriniz')
    return
  }
  if (!transferForm.value.targetOfficeId) {
    notification.warning('Hedef ofis seçiniz')
    return
  }
  if (transferForm.value.sourceOfficeId === transferForm.value.targetOfficeId) {
    notification.warning('Kaynak ve hedef ofis aynı olamaz')
    return
  }
  saving.value = true
  try {
    await apiService.createTransferRequest({
      SourceOfficeId: transferForm.value.sourceOfficeId,
      TargetOfficeId: transferForm.value.targetOfficeId,
      CurrencyId: transferForm.value.currencyId,
      Amount: transferForm.value.amount,
      Description: transferForm.value.description,
    })
    showCreateModal.value = false
    await Promise.all([loadPending(), loadHistory()])
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Transfer talebi oluşturulamadı')
  } finally {
    saving.value = false
  }
}

async function processAction(transfer: any, isApproved: boolean) {
  const id = String(transfer.id)
  if (processingIds.value.has(id)) return
  const notes = isApproved ? '' : (prompt('Red sebebi:') ?? '')
  if (!isApproved && notes === '') return
  processingIds.value.add(id)
  try {
    await apiService.processTransfer(id, {
      IsApproved: isApproved,
      Notes: notes,
    })
    await loadPending()
    await loadHistory()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'İşlem başarısız')
  } finally {
    processingIds.value.delete(id)
  }
}

const STATUS_MAP: Record<string, { label: string; cls: string }> = {
  Pending: { label: 'Bekliyor', cls: 'st-pending' },
  Approved: { label: 'Onaylandı', cls: 'st-approved' },
  Rejected: { label: 'Reddedildi', cls: 'st-rejected' },
  Completed: { label: 'Tamamlandı', cls: 'st-completed' },
  Cancelled: { label: 'İptal', cls: 'st-cancelled' },
}

function getStatus(s: string) {
  return STATUS_MAP[s] ?? { label: s || '-', cls: '' }
}

watch(() => officeId.value, () => {
  if (officeId.value) { loadPending(); loadHistory() }
})

onMounted(async () => {
  loading.value = true
  try {
    await Promise.all([loadPending(), loadHistory()])
  } finally { loading.value = false }
})
</script>

<template>
  <div class="io-wrap">
    <AppPageHeader icon="swap_horiz" title="Ofislerarası Transfer">
      <button class="btn-primary" @click="openCreateModal">
        <span class="material-symbols-outlined" aria-hidden="true">swap_horiz</span>
        Yeni Transfer
      </button>
    </AppPageHeader>

    <!-- KPI -->
    <div class="kpi-grid">
      <AppKpiCard v-if="authStore.isAdmin" icon="pending_actions" label="Bekleyen" :value="pendingTransfers.length" color="#d97706" bg="#fffbeb" />
      <AppKpiCard icon="sync_alt" label="Toplam Transfer" :value="officeTransfers.length" color="#6366f1" bg="#eef2ff" />
    </div>

    <!-- Tabs -->
    <div class="tab-bar">
      <button v-if="authStore.isAdmin" :class="['tab-btn', activeTab === 'pending' ? 'tab-active' : '']" @click="activeTab = 'pending'">
        Bekleyenler
        <span v-if="pendingTransfers.length" class="badge">{{ pendingTransfers.length }}</span>
      </button>
      <button :class="['tab-btn', activeTab === 'history' ? 'tab-active' : '']" @click="activeTab = 'history'; loadHistory()">
        Geçmiş
      </button>
    </div>

    <div v-if="loading" class="loading-state">
      <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
    </div>

    <!-- Pending -->
    <div v-else-if="activeTab === 'pending' && authStore.isAdmin" class="table-wrap">
      <table class="io-table">
        <thead>
          <tr>
            <th>Kaynak Ofis</th>
            <th>Hedef Ofis</th>
            <th>Döviz</th>
            <th class="text-right">Tutar</th>
            <th>Açıklama</th>
            <th>Tarih</th>
            <th class="text-center">İşlem</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="pendingTransfers.length === 0">
            <td colspan="7" class="empty-row">Bekleyen transfer yok</td>
          </tr>
          <tr v-for="t in pendingTransfers" :key="t.id">
            <td>{{ t.sourceOfficeName || '-' }}</td>
            <td>{{ t.targetOfficeName || '-' }}</td>
            <td><strong>{{ t.currencyCode || '-' }}</strong></td>
            <td class="text-right font-mono">{{ formatCurrency(t.amount ?? 0) }}</td>
            <td>{{ t.description || '-' }}</td>
            <td>{{ formatDateTime(t.createdAt ?? t.requestDate) }}</td>
            <td class="text-center actions-cell">
              <button class="btn-approve" @click="processAction(t, true)" :disabled="processingIds.has(String(t.id))" title="Onayla">
                <span class="material-symbols-outlined" aria-hidden="true">check_circle</span>
              </button>
              <button class="btn-reject" @click="processAction(t, false)" :disabled="processingIds.has(String(t.id))" title="Reddet">
                <span class="material-symbols-outlined" aria-hidden="true">cancel</span>
              </button>
              <button class="icon-btn" title="Detay" @click="loadDetail(t)">
                <span class="material-symbols-outlined" aria-hidden="true">visibility</span>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- History -->
    <div v-else-if="activeTab === 'history'" class="table-wrap">
      <table class="io-table">
        <thead>
          <tr>
            <th>Kaynak</th>
            <th>Hedef</th>
            <th>Döviz</th>
            <th class="text-right">Tutar</th>
            <th>Durum</th>
            <th>Tarih</th>
            <th class="text-center">Detay</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="officeTransfers.length === 0">
            <td colspan="7" class="empty-row">Transfer geçmişi bulunamadı</td>
          </tr>
          <tr v-for="t in officeTransfers" :key="t.id">
            <td>{{ t.sourceOfficeName || '-' }}</td>
            <td>{{ t.targetOfficeName || '-' }}</td>
            <td><strong>{{ t.currencyCode || '-' }}</strong></td>
            <td class="text-right font-mono">{{ formatCurrency(t.amount ?? 0) }}</td>
            <td>
              <span class="status-badge" :class="getStatus(t.status).cls">
                {{ getStatus(t.status).label }}
              </span>
            </td>
            <td>{{ formatDateTime(t.createdAt ?? t.requestDate) }}</td>
            <td class="text-center">
              <button class="icon-btn" @click="loadDetail(t)">
                <span class="material-symbols-outlined" aria-hidden="true">visibility</span>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Create Modal -->
    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Yeni Transfer Talebi</h2>
          <button class="modal-close" @click="showCreateModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-stack">
            <div class="form-group">
              <label>Kaynak Ofis</label>
              <select v-model="transferForm.sourceOfficeId">
                <option v-for="o in exchangeStore.offices" :key="o.officeId ?? o.id" :value="o.officeId ?? o.id">
                  {{ o.officeName ?? o.name }}
                </option>
              </select>
            </div>
            <div class="form-group">
              <label>Hedef Ofis</label>
              <select v-model="transferForm.targetOfficeId">
                <option value="" disabled>Seçiniz</option>
                <option v-for="o in exchangeStore.offices.filter(o => (o.officeId ?? o.id) !== transferForm.sourceOfficeId)"
                        :key="o.officeId ?? o.id" :value="o.officeId ?? o.id">
                  {{ o.officeName ?? o.name }}
                </option>
              </select>
            </div>
            <div class="form-group">
              <label>Para Birimi</label>
              <select v-model="transferForm.currencyId">
                <option v-for="c in exchangeStore.currencies" :key="c.id" :value="c.id">{{ c.code }} - {{ c.name }}</option>
              </select>
            </div>
            <div class="form-group">
              <label>Tutar *</label>
              <input v-model.number="transferForm.amount" type="number" step="0.01" min="0" placeholder="0.00" />
            </div>
            <div class="form-group">
              <label>Açıklama</label>
              <textarea v-model="transferForm.description" rows="2" placeholder="Açıklama..."></textarea>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showCreateModal = false">İptal</button>
          <button class="btn-primary" :disabled="saving" @click="createTransfer">
            {{ saving ? 'Gönderiliyor...' : 'Talep Oluştur' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Detail Modal -->
    <div v-if="showDetailModal && selectedTransfer" class="modal-overlay" @click.self="showDetailModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Transfer Detay</h2>
          <button class="modal-close" @click="showDetailModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="detail-grid">
            <div class="detail-item">
              <span class="detail-label">Kaynak</span>
              <span>{{ selectedTransfer.sourceOfficeName || '-' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Hedef</span>
              <span>{{ selectedTransfer.targetOfficeName || '-' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Para Birimi</span>
              <span>{{ selectedTransfer.currencyCode || '-' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Tutar</span>
              <span class="font-mono">{{ formatCurrency(selectedTransfer.amount ?? 0) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Durum</span>
              <span class="status-badge" :class="getStatus(selectedTransfer.status).cls">
                {{ getStatus(selectedTransfer.status).label }}
              </span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Talep Tarihi</span>
              <span>{{ formatDateTime(selectedTransfer.createdAt ?? selectedTransfer.requestDate) }}</span>
            </div>
            <div v-if="selectedTransfer.description" class="detail-item full">
              <span class="detail-label">Açıklama</span>
              <span>{{ selectedTransfer.description }}</span>
            </div>
            <div v-if="selectedTransfer.notes" class="detail-item full">
              <span class="detail-label">Notlar</span>
              <span>{{ selectedTransfer.notes }}</span>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showDetailModal = false">Kapat</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.io-wrap { padding: 24px; max-width: 1400px; margin: 0 auto; }

.btn-primary {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 16px; background: var(--color-secondary-hover); color: #fff;
  border: none; border-radius: var(--radius-md); font-size: 13px; font-weight: 600; cursor: pointer;
}
.btn-primary:hover { background: #1d4ed8; }
.btn-primary:disabled { opacity: .6; cursor: not-allowed; }
.btn-cancel {
  padding: 8px 16px; background: #fff; color: #374151;
  border: 1px solid #d1d5db; border-radius: var(--radius-md); font-size: 13px; cursor: pointer;
}
.btn-cancel:hover { background: #f3f4f6; }
.btn-approve { background: none; border: none; cursor: pointer; color: var(--color-success); padding: 4px; border-radius: var(--radius-sm); }
.btn-approve:hover { background: var(--color-success-bg); }
.btn-reject { background: none; border: none; cursor: pointer; color: var(--color-danger); padding: 4px; border-radius: var(--radius-sm); }
.btn-reject:hover { background: var(--color-danger-bg); }
.btn-approve .material-symbols-outlined, .btn-reject .material-symbols-outlined { font-size: 22px; }
.icon-btn {
  background: none; border: none; cursor: pointer; padding: 4px; border-radius: var(--radius-sm);
  color: #6b7280; display: flex; align-items: center;
}
.icon-btn:hover { background: #f3f4f6; color: var(--color-secondary-hover); }
.icon-btn .material-symbols-outlined { font-size: 18px; }

.kpi-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin-bottom: 20px; }

.tab-bar { display: flex; gap: 4px; margin-bottom: 16px; border-bottom: 1px solid #e5e7eb; }
.tab-btn {
  padding: 10px 18px; border: none; background: none; font-size: 13px; font-weight: 500;
  color: #6b7280; cursor: pointer; border-bottom: 2px solid transparent;
  display: flex; align-items: center; gap: 6px;
}
.tab-btn:hover { color: #374151; }
.tab-active { color: var(--color-secondary-hover); border-bottom-color: var(--color-secondary-hover); }
.badge { background: var(--color-danger); color: #fff; font-size: 11px; padding: 1px 7px; border-radius: var(--radius-md); font-weight: 600; }

.loading-state { text-align: center; padding: 60px 20px; color: #6b7280; font-size: 14px; }
.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

.table-wrap { background: #fff; border: 1px solid #e5e7eb; border-radius: var(--radius-lg); overflow-x: auto; }
.io-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.io-table th {
  padding: 10px 14px; text-align: left; font-weight: 600; color: #6b7280;
  font-size: 11px; text-transform: uppercase; letter-spacing: .5px;
  border-bottom: 1px solid #e5e7eb; background: #f9fafb; white-space: nowrap;
}
.io-table td { padding: 10px 14px; border-bottom: 1px solid #f3f4f6; color: #374151; }
.text-right { text-align: right; }
.text-center { text-align: center; }
.font-mono { font-family: 'Consolas', monospace; }
.empty-row { text-align: center; color: #9ca3af; padding: 40px 14px !important; }
.actions-cell { display: flex; gap: 4px; justify-content: center; }

.status-badge { font-size: 11px; padding: 2px 10px; border-radius: var(--radius-md); font-weight: 500; }
.st-pending { background: #fef3c7; color: #92400e; }
.st-approved { background: var(--color-success-bg); color: #065f46; }
.st-rejected { background: var(--color-danger-bg); color: #991b1b; }
.st-completed { background: #dbeafe; color: #1d4ed8; }
.st-cancelled { background: #f3f4f6; color: #6b7280; }

.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.4);
  display: flex; align-items: center; justify-content: center; z-index: 1000; padding: 20px;
}
.modal {
  background: #fff; border-radius: var(--radius-lg); width: 100%; max-width: 520px;
  max-height: 90vh; display: flex; flex-direction: column;
  box-shadow: 0 20px 60px rgba(0,0,0,.15);
}
.modal-header {
  display: flex; justify-content: space-between; align-items: center;
  padding: 20px 24px; border-bottom: 1px solid #e5e7eb;
}
.modal-header h2 { font-size: 17px; font-weight: 600; color: #1a1a2e; margin: 0; }
.modal-close { background: none; border: none; font-size: 22px; cursor: pointer; color: #6b7280; }
.modal-body { padding: 24px; overflow-y: auto; }
.modal-footer { padding: 16px 24px; border-top: 1px solid #e5e7eb; display: flex; justify-content: flex-end; gap: 8px; }

.form-stack { display: flex; flex-direction: column; gap: 14px; }
.form-group { display: flex; flex-direction: column; gap: 4px; }
.form-group label { font-size: 12px; font-weight: 500; color: #374151; }
.form-group input, .form-group select, .form-group textarea {
  padding: 8px 12px; border: 1px solid #d1d5db; border-radius: var(--radius-md); font-size: 13px; outline: none;
}
.form-group input:focus, .form-group select:focus, .form-group textarea:focus {
  border-color: var(--color-secondary-hover); box-shadow: 0 0 0 2px rgba(37,99,235,.1);
}
.form-group textarea { resize: vertical; }

.detail-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.detail-item { display: flex; flex-direction: column; gap: 2px; }
.detail-item.full { grid-column: 1 / -1; }
.detail-label { font-size: 11px; color: #9ca3af; text-transform: uppercase; letter-spacing: .5px; }

@media (max-width: 768px) {
  .io-wrap { padding: 12px; }
  .detail-grid { grid-template-columns: 1fr; }
}
</style>
