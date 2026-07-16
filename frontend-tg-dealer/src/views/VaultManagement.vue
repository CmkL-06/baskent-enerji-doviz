<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import AppKpiCard from '@/components/common/AppKpiCard.vue'
import AppPageHeader from '@/components/common/AppPageHeader.vue'
import AppEmptyState from '@/components/common/AppEmptyState.vue'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()
const route = useRoute()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()

const loading = ref(true)
const saving = ref(false)
const error = ref('')

const vaults = ref<any[]>([])
const showModal = ref(false)
const editingVault = ref<any>(null)

const vaultForm = ref({
  name: '',
  officeId: '' as string | number,
  description: '',
  shouldCount: true,
  isActive: true,
})

const officeId = computed(() =>
  exchangeStore.selectedOffice?.officeId ?? exchangeStore.offices[0]?.officeId
)

const formatCurrency = (amount: number): string =>
  new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount)

const formatDate = (d: string | null | undefined): string => {
  if (!d) return '-'
  return new Date(d).toLocaleDateString('tr-TR')
}

async function loadVaults() {
  loading.value = true
  error.value = ''
  try {
    const data = officeId.value
      ? await apiService.getVaultsByOfficeId(officeId.value)
      : await apiService.getVaults()
    vaults.value = Array.isArray(data) ? data : (data?.items ?? data?.data ?? [])
  } catch {
    error.value = 'Kasalar yüklenemedi'
  } finally {
    loading.value = false
  }
}

function openCreateModal() {
  editingVault.value = null
  vaultForm.value = {
    name: '',
    officeId: officeId.value ?? '',
    description: '',
    shouldCount: true,
    isActive: true,
  }
  showModal.value = true
}

function openEditModal(vault: any) {
  editingVault.value = vault
  vaultForm.value = {
    name: vault.vaultName ?? vault.name ?? '',
    officeId: vault.officeId ?? officeId.value ?? '',
    description: vault.description ?? '',
    shouldCount: vault.shouldCount ?? true,
    isActive: vault.isActive ?? true,
  }
  showModal.value = true
}

async function saveVault() {
  if (!vaultForm.value.name) {
    notification.warning('Kasa adı zorunludur')
    return
  }
  saving.value = true
  try {
    const payload: any = {
      Name: vaultForm.value.name,
      OfficeId: vaultForm.value.officeId,
      Description: vaultForm.value.description,
      ShouldCount: vaultForm.value.shouldCount,
      IsActive: vaultForm.value.isActive,
    }
    if (editingVault.value) {
      payload.Id = editingVault.value.vaultId ?? editingVault.value.id
    }
    await apiService.saveVault(payload)
    showModal.value = false
    await loadVaults()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Kayıt başarısız')
  } finally {
    saving.value = false
  }
}

async function deleteVault(vault: any) {
  const name = vault.vaultName ?? vault.name
  if (!confirm(`"${name}" kasasını silmek istediğinizden emin misiniz?`)) return
  try {
    await apiService.deleteVault(vault.vaultId ?? vault.id)
    await loadVaults()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Silme başarısız')
  }
}

function getTotalValue(vault: any): number {
  return (vault.balances ?? []).reduce((sum: number, b: any) => sum + (b.valueInBaseCurrency ?? 0), 0)
}

watch(() => officeId.value, () => {
  if (officeId.value) loadVaults()
})

onMounted(async () => {
  if (officeId.value) await loadVaults()
  if (route.query.create === 'true') openCreateModal()
})
</script>

<template>
  <div class="vm-wrap">
    <AppPageHeader icon="account_balance_wallet" title="Kasa Yönetimi">
      <button class="btn-primary" @click="openCreateModal">
        <span class="material-symbols-outlined" aria-hidden="true">add</span>
        Yeni Kasa
      </button>
    </AppPageHeader>

    <!-- KPI -->
    <div class="kpi-grid">
      <AppKpiCard icon="account_balance_wallet" label="Toplam Kasa" :value="vaults.length" color="#3b82f6" bg="#eff6ff" />
      <AppKpiCard icon="check_circle" label="Aktif" :value="vaults.filter(v => v.isActive !== false).length" color="#059669" bg="#ecfdf5" />
      <AppKpiCard icon="fact_check" label="Sayım Gerektiren" :value="vaults.filter(v => v.shouldCount).length" color="#d97706" bg="#fffbeb" />
    </div>

    <div v-if="loading" class="loading-state">
      <span class="material-symbols-outlined spin">progress_activity</span>
      Yükleniyor...
    </div>
    <div v-else-if="error" class="error-state">{{ error }}</div>

    <div v-else class="vault-grid">
      <div v-for="vault in vaults" :key="vault.vaultId ?? vault.id" class="vault-card" :class="{ 'vault-inactive': vault.isActive === false }">
        <div class="vault-top">
          <div>
            <h3>{{ vault.vaultName ?? vault.name }}</h3>
            <span class="vault-office">{{ vault.officeName ?? '' }}</span>
          </div>
          <div class="vault-badges">
            <span v-if="vault.isActive === false" class="badge-inactive">Pasif</span>
            <span v-if="vault.shouldCount" class="badge-count">Sayım</span>
          </div>
        </div>

        <div v-if="vault.description" class="vault-desc">{{ vault.description }}</div>

        <!-- Balances -->
        <div class="balance-list">
          <div v-for="bal in (vault.balances ?? [])" :key="bal.currencyId" class="balance-row">
            <span class="bal-currency">{{ bal.currencyCode }}</span>
            <span class="bal-amount" :class="(bal.balance ?? 0) >= 0 ? '' : 'bal-neg'">{{ formatCurrency(bal.balance ?? 0) }}</span>
          </div>
          <div v-if="!vault.balances?.length" class="no-balance">Bakiye yok</div>
        </div>

        <div class="vault-meta">
          <span v-if="vault.totalValueInBaseCurrency != null">
            Toplam: <strong>{{ formatCurrency(vault.totalValueInBaseCurrency) }} ₺</strong>
          </span>
          <span>Oluşturma: {{ formatDate(vault.createdAt) }}</span>
        </div>

        <div class="vault-actions">
          <button class="icon-btn" title="Düzenle" @click="openEditModal(vault)">
            <span class="material-symbols-outlined" aria-hidden="true">edit</span>
          </button>
          <button v-if="authStore.isAdmin" class="icon-btn danger" title="Sil" @click="deleteVault(vault)">
            <span class="material-symbols-outlined" aria-hidden="true">delete</span>
          </button>
        </div>
      </div>

      <AppEmptyState v-if="vaults.length === 0" icon="account_balance_wallet" message="Kasa bulunamadı" />
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>{{ editingVault ? 'Kasa Düzenle' : 'Yeni Kasa' }}</h2>
          <button class="modal-close" @click="showModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-stack">
            <div class="form-group">
              <label>Kasa Adı *</label>
              <input v-model="vaultForm.name" placeholder="Örn: Ana Kasa" />
            </div>
            <div class="form-group">
              <label>Ofis</label>
              <select v-model="vaultForm.officeId">
                <option v-for="o in exchangeStore.offices" :key="o.officeId ?? o.id" :value="o.officeId ?? o.id">
                  {{ o.officeName ?? o.name }}
                </option>
              </select>
            </div>
            <div class="form-group">
              <label>Açıklama</label>
              <input v-model="vaultForm.description" placeholder="Opsiyonel açıklama" />
            </div>
            <div class="form-row">
              <label class="checkbox-label">
                <input type="checkbox" v-model="vaultForm.shouldCount" />
                Sayım gerektir
              </label>
              <label class="checkbox-label">
                <input type="checkbox" v-model="vaultForm.isActive" />
                Aktif
              </label>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showModal = false">İptal</button>
          <button class="btn-primary" :disabled="saving" @click="saveVault">
            {{ saving ? 'Kaydediliyor...' : (editingVault ? 'Güncelle' : 'Oluştur') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.vm-wrap { padding: 24px; max-width: 1400px; margin: 0 auto; }
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

/* KPI */
.kpi-grid {
  display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px; margin-bottom: 20px;
}
/* Loading */
.loading-state, .error-state { text-align: center; padding: 60px 20px; color: #6b7280; font-size: 14px; }
.error-state { color: var(--color-danger); }
.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

/* Vault Grid */
.vault-grid {
  display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 16px;
}
.vault-card {
  background: #fff; border: 1px solid #e5e7eb; border-radius: var(--radius-lg); padding: 20px;
  transition: border-color .15s;
}
.vault-card:hover { border-color: #93c5fd; }
.vault-inactive { opacity: .6; }
.vault-top { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 8px; }
.vault-top h3 { font-size: 16px; font-weight: 600; color: #1a1a2e; margin: 0 0 2px; }
.vault-office { font-size: 12px; color: #9ca3af; }
.vault-badges { display: flex; gap: 4px; }
.badge-inactive {
  font-size: 10px; padding: 2px 8px; border-radius: var(--radius-md); background: var(--color-danger-bg); color: #991b1b;
}
.badge-count {
  font-size: 10px; padding: 2px 8px; border-radius: var(--radius-md); background: #fef3c7; color: #92400e;
}
.vault-desc { font-size: 12px; color: #6b7280; margin-bottom: 8px; }
.balance-list { margin-bottom: 8px; }
.balance-row { display: flex; justify-content: space-between; padding: 3px 0; font-size: 13px; }
.bal-currency { font-weight: 600; color: #374151; }
.bal-amount { font-weight: 600; color: #1a1a2e; font-family: 'Consolas', monospace; }
.bal-neg { color: var(--color-danger); }
.no-balance { font-size: 12px; color: #9ca3af; text-align: center; padding: 8px 0; }
.vault-meta {
  display: flex; justify-content: space-between; font-size: 11px; color: #9ca3af; margin-bottom: 10px;
}
.vault-actions { display: flex; gap: 4px; }
.icon-btn {
  background: none; border: none; cursor: pointer; padding: 4px; border-radius: var(--radius-sm);
  color: #6b7280; display: flex; align-items: center;
}
.icon-btn:hover { background: #f3f4f6; color: var(--color-secondary-hover); }
.icon-btn.danger:hover { color: var(--color-danger); }
.icon-btn .material-symbols-outlined { font-size: 18px; }
/* Modal */
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.4);
  display: flex; align-items: center; justify-content: center; z-index: 1000; padding: 20px;
}
.modal {
  background: #fff; border-radius: var(--radius-lg); width: 100%; max-width: 480px;
  box-shadow: 0 20px 60px rgba(0,0,0,.15);
}
.modal-header {
  display: flex; justify-content: space-between; align-items: center;
  padding: 20px 24px; border-bottom: 1px solid #e5e7eb;
}
.modal-header h2 { font-size: 17px; font-weight: 600; color: #1a1a2e; margin: 0; }
.modal-close { background: none; border: none; font-size: 22px; cursor: pointer; color: #6b7280; }
.modal-body { padding: 24px; }
.modal-footer {
  padding: 16px 24px; border-top: 1px solid #e5e7eb;
  display: flex; justify-content: flex-end; gap: 8px;
}
.form-stack { display: flex; flex-direction: column; gap: 14px; }
.form-group { display: flex; flex-direction: column; gap: 4px; }
.form-group label { font-size: 12px; font-weight: 500; color: #374151; }
.form-group input, .form-group select {
  padding: 8px 12px; border: 1px solid #d1d5db; border-radius: var(--radius-md); font-size: 13px; outline: none;
}
.form-group input:focus, .form-group select:focus {
  border-color: var(--color-secondary-hover); box-shadow: 0 0 0 2px rgba(37,99,235,.1);
}
.form-row { display: flex; gap: 20px; }
.checkbox-label {
  display: flex; align-items: center; gap: 8px; font-size: 13px; cursor: pointer; color: #374151;
}
.checkbox-label input[type="checkbox"] { width: 16px; height: 16px; }

@media (max-width: 768px) {
  .vm-wrap { padding: 12px; }
  .vault-grid { grid-template-columns: 1fr; }
}
</style>
