<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiService from '@/services/apiservice'

interface Currency {
  id: string
  createdDate: string
  currencyCode: string
  currencyName: string
  currencySymbol?: string
}

const currencies     = ref<Currency[]>([])
const loading        = ref(false)
const saving         = ref(false)
const editingCurrency = ref<Currency | null>(null)
const showForm       = ref(false)
const toast          = ref<{ msg: string; type: 'success' | 'error' } | null>(null)
const deleteTarget   = ref<Currency | null>(null)
const deleting       = ref(false)

const form = ref({ currencyCode: '', currencyName: '', currencySymbol: '' })

function showToast(msg: string, type: 'success' | 'error' = 'success') {
  toast.value = { msg, type }
  setTimeout(() => { toast.value = null }, 3500)
}

const loadCurrencies = async () => {
  loading.value = true
  try {
    currencies.value = await apiService.getCurrencies() ?? []
  } catch {
    showToast('Para birimleri yüklenemedi', 'error')
  } finally {
    loading.value = false
  }
}

const openCreate = () => {
  editingCurrency.value = null
  form.value = { currencyCode: '', currencyName: '', currencySymbol: '' }
  showForm.value = true
}

const openEdit = (c: Currency) => {
  editingCurrency.value = c
  form.value = { currencyCode: c.currencyCode, currencyName: c.currencyName, currencySymbol: c.currencySymbol ?? '' }
  showForm.value = true
}

const cancelForm = () => { showForm.value = false }

const saveCurrency = async () => {
  saving.value = true
  try {
    const payload: any = {
      currencyCode:   form.value.currencyCode.toUpperCase(),
      currencyName:   form.value.currencyName,
      currencySymbol: form.value.currencySymbol || form.value.currencyCode.toUpperCase(),
    }
    if (editingCurrency.value) payload.id = editingCurrency.value.id
    await apiService.saveCurrency(payload)
    showToast(editingCurrency.value ? 'Para birimi güncellendi' : 'Para birimi eklendi')
    showForm.value = false
    await loadCurrencies()
  } catch {
    showToast('Kayıt başarısız', 'error')
  } finally {
    saving.value = false
  }
}

const confirmDelete = (c: Currency) => { deleteTarget.value = c }
const cancelDelete  = () => { deleteTarget.value = null }

const doDelete = async () => {
  if (!deleteTarget.value) return
  deleting.value = true
  try {
    await apiService.deleteCurrency(deleteTarget.value.id)
    showToast('Para birimi silindi')
    deleteTarget.value = null
    await loadCurrencies()
  } catch {
    showToast('Silinemedi', 'error')
  } finally {
    deleting.value = false
  }
}

const formatDate = (d: string) => new Date(d).toLocaleDateString('tr-TR')

onMounted(loadCurrencies)
</script>

<template>
  <div class="currency-page">
    <!-- Toast -->
    <transition name="fade">
      <div v-if="toast" :class="['toast', toast.type]">{{ toast.msg }}</div>
    </transition>

    <!-- Header -->
    <div class="page-header">
      <h2>Para Birimi Yönetimi</h2>
      <button class="btn-primary" @click="openCreate">+ Yeni Ekle</button>
    </div>

    <!-- Form Modal -->
    <div v-if="showForm" class="modal-overlay" @click.self="cancelForm">
      <div class="modal">
        <h3>{{ editingCurrency ? 'Para Birimini Düzenle' : 'Yeni Para Birimi' }}</h3>
        <form @submit.prevent="saveCurrency" class="form-grid">
          <label>
            Kod <span class="req">*</span>
            <input v-model="form.currencyCode" placeholder="USD" maxlength="10" required :disabled="!!editingCurrency" />
          </label>
          <label>
            Ad <span class="req">*</span>
            <input v-model="form.currencyName" placeholder="Amerikan Doları" required />
          </label>
          <label>
            Sembol
            <input v-model="form.currencySymbol" placeholder="$" maxlength="5" />
          </label>
          <div class="modal-actions">
            <button type="button" class="btn-ghost" @click="cancelForm">İptal</button>
            <button type="submit" class="btn-primary" :disabled="saving">
              {{ saving ? 'Kaydediliyor…' : (editingCurrency ? 'Güncelle' : 'Ekle') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Delete Confirm Modal -->
    <div v-if="deleteTarget" class="modal-overlay" @click.self="cancelDelete">
      <div class="modal modal-sm">
        <h3>Emin misiniz?</h3>
        <p><strong>{{ deleteTarget.currencyCode }} — {{ deleteTarget.currencyName }}</strong> silinecek.<br>Bu para birimine ait işlemler etkilenebilir.</p>
        <div class="modal-actions">
          <button class="btn-ghost" @click="cancelDelete">Vazgeç</button>
          <button class="btn-danger" :disabled="deleting" @click="doDelete">
            {{ deleting ? 'Siliniyor…' : 'Sil' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="card">
      <div v-if="loading" class="empty">Yükleniyor…</div>
      <table v-else class="data-table">
        <thead>
          <tr>
            <th>Kod</th>
            <th>Ad</th>
            <th>Sembol</th>
            <th>Eklenme</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="currencies.length === 0">
            <td colspan="5" class="empty">Henüz para birimi yok</td>
          </tr>
          <tr v-for="c in currencies" :key="c.id">
            <td><span class="badge">{{ c.currencyCode }}</span></td>
            <td>{{ c.currencyName }}</td>
            <td>{{ c.currencySymbol || '—' }}</td>
            <td>{{ formatDate(c.createdDate) }}</td>
            <td class="actions">
              <button class="btn-icon" @click="openEdit(c)" title="Düzenle">✏️</button>
              <button class="btn-icon danger" @click="confirmDelete(c)" title="Sil">🗑️</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.currency-page { padding: 20px; display: flex; flex-direction: column; gap: 16px; }

.page-header { display: flex; align-items: center; justify-content: space-between; }
.page-header h2 { font-size: 18px; font-weight: 600; color: #1a1a1a; margin: 0; }

.card { background: white; border-radius: 12px; border: 1px solid #e5e7eb; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,.06); }

.data-table { width: 100%; border-collapse: collapse; font-size: 14px; }
.data-table thead { background: #f9fafb; }
.data-table th { padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; font-size: 12px; border-bottom: 2px solid #e5e7eb; }
.data-table td { padding: 12px 16px; color: #1a1a1a; border-bottom: 1px solid #f3f4f6; }
.data-table tbody tr:hover { background: #f9fafb; }

.badge { background: #ede9fe; color: #6b46c1; font-weight: 700; font-size: 12px; padding: 3px 8px; border-radius: 4px; }

.actions { display: flex; gap: 4px; }
.btn-icon { background: none; border: none; cursor: pointer; font-size: 16px; padding: 4px 6px; border-radius: 6px; transition: background .2s; }
.btn-icon:hover { background: #f3f4f6; }
.btn-icon.danger:hover { background: #fee2e2; }

.empty { text-align: center; padding: 40px; color: #9ca3af; }

/* Buttons */
.btn-primary { background: #6b46c1; color: white; border: none; padding: 9px 18px; border-radius: 8px; font-size: 14px; font-weight: 600; cursor: pointer; transition: background .2s; }
.btn-primary:hover:not(:disabled) { background: #553c9a; }
.btn-primary:disabled { opacity: .6; cursor: not-allowed; }
.btn-ghost { background: #f3f4f6; color: #374151; border: none; padding: 9px 18px; border-radius: 8px; font-size: 14px; cursor: pointer; }
.btn-ghost:hover { background: #e5e7eb; }
.btn-danger { background: #dc2626; color: white; border: none; padding: 9px 18px; border-radius: 8px; font-size: 14px; cursor: pointer; }
.btn-danger:hover:not(:disabled) { background: #b91c1c; }
.btn-danger:disabled { opacity: .6; cursor: not-allowed; }

/* Modal */
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.45); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.modal { background: white; border-radius: 14px; padding: 28px; width: 420px; max-width: 95vw; }
.modal.modal-sm { width: 340px; }
.modal h3 { margin: 0 0 20px; font-size: 16px; font-weight: 600; color: #1a1a1a; }
.modal p { color: #374151; font-size: 14px; line-height: 1.6; margin: 0 0 20px; }

.form-grid { display: flex; flex-direction: column; gap: 14px; }
.form-grid label { display: flex; flex-direction: column; gap: 5px; font-size: 13px; font-weight: 500; color: #374151; }
.form-grid input { padding: 9px 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; }
.form-grid input:focus { outline: none; border-color: #6b46c1; box-shadow: 0 0 0 2px rgba(107,70,193,.15); }
.form-grid input:disabled { background: #f9fafb; color: #9ca3af; }
.req { color: #dc2626; }

.modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 8px; }

/* Toast */
.toast { position: fixed; top: 20px; right: 20px; padding: 12px 20px; border-radius: 10px; font-size: 14px; font-weight: 500; z-index: 2000; box-shadow: 0 4px 12px rgba(0,0,0,.15); }
.toast.success { background: #d1fae5; color: #065f46; border: 1px solid #a7f3d0; }
.toast.error   { background: #fee2e2; color: #991b1b; border: 1px solid #fca5a5; }

.fade-enter-active, .fade-leave-active { transition: opacity .3s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
