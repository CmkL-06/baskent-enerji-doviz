<template>
  <div class="vault-edit-dialog">
    <div class="dialog-header">
      <h2>Kasa Düzenle</h2>
      <button @click="$emit('close')" class="close-btn">×</button>
    </div>

    <form @submit.prevent="saveChanges" class="edit-form">
      <!-- Basic Info -->
      <div class="form-section">
        <h3>Temel Bilgiler</h3>
        <div class="form-group">
          <label>Kasa Adı *</label>
          <input 
            v-model="editForm.vaultName" 
            type="text" 
            required
            placeholder="Kasa adını girin"
          >
        </div>
        
        <div class="form-group">
          <label>Ofis</label>
          <select v-model="editForm.officeId" disabled>
            <option v-for="office in offices" :key="office.officeId" :value="office.officeId">
              {{ office.officeName }}
            </option>
          </select>
          <small class="helper-text">Ofis değiştirilemez</small>
        </div>
        
        <div class="form-group">
          <label>Durum</label>
          <div class="radio-group">
            <label class="radio-label">
              <input type="radio" v-model="editForm.isActive" :value="true">
              <span>Aktif</span>
            </label>
            <label class="radio-label">
              <input type="radio" v-model="editForm.isActive" :value="false">
              <span>Pasif</span>
            </label>
          </div>
        </div>

        <div class="form-group">
          <label>Açıklama</label>
          <textarea 
            v-model="editForm.description" 
            rows="3" 
            placeholder="Kasa açıklaması (isteğe bağlı)"
          ></textarea>
        </div>
      </div>

      <!-- Warning for inactive vault -->
      <div v-if="!editForm.isActive" class="warning-message">
        <span class="icon">⚠️</span>
        <p>Pasif kasalarda işlem yapılamaz. Kasayı pasif yapmadan önce bakiyeleri kontrol edin.</p>
      </div>

      <!-- Actions -->
      <div class="form-actions">
        <button type="button" @click="$emit('close')" class="btn-secondary">
          İptal
        </button>
        <button type="submit" class="btn-primary" :disabled="isProcessing">
          <span v-if="isProcessing">Kaydediliyor...</span>
          <span v-else>Kaydet</span>
        </button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiService from '@/services/apiservice'
import { useExchangeStore } from '@/stores/exchange'
import { useNotification } from '@/composables/useNotification'
import type { Vault, Office } from '@/types/api'

const notification = useNotification()

const props = defineProps<{
  vault: Vault
}>()

const emit = defineEmits(['close', 'saved'])
const exchangeStore = useExchangeStore()

// State
const offices = ref<Office[]>([])
const isProcessing = ref(false)
const editForm = ref({
  vaultName: props.vault.vaultName,
  officeId: props.vault.officeId,
  isActive: props.vault.isActive ?? true,
  description: props.vault.description || ''
})

// Methods
const loadOffices = async () => {
  try {
    offices.value = await apiService.getOffices()
  } catch (error) {
    console.error('Failed to load offices:', error)
  }
}

const saveChanges = async () => {
  if (!(editForm.value.vaultName || '').trim()) {
    notification.warning('Kasa adı boş olamaz')
    return
  }
  
  isProcessing.value = true
  
  try {
    // Use saveVault with correct data structure
    const updatedVault = await apiService.saveVault({
      id: props.vault.vaultId || props.vault.id,
      name: editForm.value.vaultName,
      description: editForm.value.description,
      officeId: editForm.value.officeId,
      isActive: editForm.value.isActive
    })
    
    // Refresh vault data in store
    if (editForm.value.officeId) {
      await exchangeStore.loadVaults(editForm.value.officeId)
    } else {
      await exchangeStore.fetchVaults()
    }
    
    emit('saved', updatedVault)
    emit('close')
  } catch (error) {
    console.error('Failed to update vault:', error)
    notification.error('Kasa güncellenirken bir hata oluştu')
  } finally {
    isProcessing.value = false
  }
}

// Initialize
onMounted(() => {
  loadOffices()
})
</script>

<style scoped>
.vault-edit-dialog {
  background: white;
  border-radius: var(--radius-md);
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
}

.dialog-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #e9ecef;
}

.dialog-header h2 {
  margin: 0;
  color: #2c3e50;
  font-size: 1.3rem;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  color: #6c757d;
  cursor: pointer;
  padding: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-sm);
  transition: background-color 0.2s, color 0.2s;
}

.close-btn:hover {
  background: #f8f9fa;
  color: #495057;
}

.edit-form {
  padding: 1.5rem;
}

.form-section {
  margin-bottom: 2rem;
}

.form-section h3 {
  margin: 0 0 1rem;
  color: #495057;
  font-size: 1.1rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  color: #495057;
  font-weight: 500;
}

.form-group input,
.form-group select,
.form-group textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ced4da;
  border-radius: var(--radius-sm);
  font-size: 1rem;
  transition: border-color 0.2s;
}

.form-group input:focus,
.form-group select:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #007bff;
}

.form-group select:disabled {
  background: #e9ecef;
  cursor: not-allowed;
}

.helper-text {
  display: block;
  margin-top: 0.25rem;
  color: #6c757d;
  font-size: 0.875rem;
}

.radio-group {
  display: flex;
  gap: 1.5rem;
  margin-top: 0.5rem;
}

.radio-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  font-weight: normal;
}

.radio-label input[type="radio"] {
  width: auto;
  margin: 0;
}

.warning-message {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: #fff3cd;
  color: #856404;
  padding: 1rem;
  border-radius: var(--radius-sm);
  margin-bottom: 1.5rem;
}

.warning-message .icon {
  font-size: 1.5rem;
}

.warning-message p {
  margin: 0;
  flex: 1;
}

.form-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  padding-top: 1.5rem;
  border-top: 1px solid #e9ecef;
}

.btn-primary,
.btn-secondary {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: var(--radius-sm);
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s;
}

.btn-primary {
  background: #007bff;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #0056b3;
}

.btn-primary:disabled {
  background: #6c757d;
  cursor: not-allowed;
  opacity: 0.6;
}

.btn-secondary {
  background: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background: #545b62;
}
</style>