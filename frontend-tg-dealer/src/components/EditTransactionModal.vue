<script setup lang="ts">
import { ref, computed } from 'vue'

interface Transaction {
  currency: string
  office: string
  amount: string
  rate: string
}

const props = defineProps<{
  visible: boolean
  transaction: Transaction | null
}>()

const emit = defineEmits(['close', 'save'])

const offices = ['KARGICAK', 'YATMAZ', 'MIGROS', 'MERKEZ KASA']
const currencies = ['USD', 'EUR', 'GBP', 'RUB', 'SEK', 'DKK', 'CHF', 'AUD', 'CAD', 'UAH']

const formData = ref({
  office: '',
  currency: '',
  amount: '',
  rate: ''
})

const isVisible = computed(() => props.visible)

const closeModal = () => {
  emit('close')
}

const saveChanges = () => {
  emit('save', formData.value)
  closeModal()
}

// Watch for transaction changes to update form
const updateForm = () => {
  if (props.transaction) {
    formData.value = {
      office: props.transaction.office,
      currency: props.transaction.currency,
      amount: props.transaction.amount.replace(/[^\d,.-]/g, ''),
      rate: props.transaction.rate.replace(/[^\d,.-]/g, '')
    }
  }
}

// Update form when transaction changes
updateForm()
</script>

<template>
  <transition name="modal">
    <div v-if="isVisible" class="modal-overlay" @click.self="closeModal">
      <div class="modal-container">
        <button class="close-btn" @click="closeModal">✕</button>
        
        <h2>İşlem Düzenle</h2>
        
        <form @submit.prevent="saveChanges">
          <div class="form-group">
            <label>OFİS ADI</label>
            <select v-model="formData.office" required>
              <option value="" disabled>Ofis seçin</option>
              <option v-for="office in offices" :key="office" :value="office">
                {{ office }}
              </option>
            </select>
          </div>
          
          <div class="form-group">
            <label>DÖVİZ TÜRÜ</label>
            <select v-model="formData.currency" required>
              <option value="" disabled>Döviz seçin</option>
              <option v-for="currency in currencies" :key="currency" :value="currency">
                {{ currency }}
              </option>
            </select>
          </div>
          
          <div class="form-group">
            <label>TUTARI</label>
            <input 
              v-model="formData.amount" 
              type="number" 
              step="0.01"
              placeholder="100,00"
              required
            >
          </div>
          
          <div class="form-group">
            <label>KUR</label>
            <input 
              v-model="formData.rate" 
              type="number" 
              step="0.01"
              placeholder="39,25"
              required
            >
          </div>
          
          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="closeModal">
              VAZGEÇ
            </button>
            <button type="submit" class="btn-save">
              GÜNCELLE
            </button>
          </div>
        </form>
      </div>
    </div>
  </transition>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-container {
  background: rgba(30, 20, 50, 0.95);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: var(--radius-xl);
  padding: 30px;
  min-width: 400px;
  max-width: 90vw;
  position: relative;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5);
}

.close-btn {
  position: absolute;
  top: 15px;
  right: 15px;
  background: rgba(255, 255, 255, 0.1);
  border: none;
  color: white;
  font-size: 20px;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.close-btn:hover {
  background: rgba(255, 255, 255, 0.2);
}

h2 {
  color: white;
  margin-bottom: 25px;
  font-size: 24px;
  text-align: center;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  color: rgba(255, 255, 255, 0.8);
  font-size: 12px;
  font-weight: 600;
  margin-bottom: 8px;
}

.form-group input,
.form-group select {
  width: 100%;
  padding: 12px 15px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: var(--radius-md);
  color: white;
  font-size: 16px;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.form-group input::placeholder {
  color: rgba(255, 255, 255, 0.4);
}

.form-group input:focus,
.form-group select:focus {
  outline: none;
  background: rgba(255, 255, 255, 0.15);
  border-color: rgba(255, 255, 255, 0.4);
}

.form-group select option {
  background: #2a1a4e;
}

.modal-actions {
  display: flex;
  gap: 15px;
  margin-top: 30px;
}

.btn-cancel,
.btn-save {
  flex: 1;
  padding: 12px 20px;
  border: none;
  border-radius: var(--radius-md);
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}

.btn-cancel {
  background: rgba(255, 255, 255, 0.1);
  color: white;
  border: 1px solid rgba(255, 255, 255, 0.2);
}

.btn-cancel:hover {
  background: rgba(255, 255, 255, 0.2);
}

.btn-save {
  background: #5a8cff;
  color: white;
}

.btn-save:hover {
  background: #4a7ce5;
  transform: translateY(-2px);
}

/* Modal transition */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.3s ease, transform 0.3s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-from .modal-container,
.modal-leave-to .modal-container {
  transform: scale(0.9);
}
</style>