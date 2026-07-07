<script setup lang="ts">
import { ref, computed } from 'vue'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()

const props = defineProps<{
  visible: boolean
}>()

const emit = defineEmits(['close', 'confirm'])

const reason = ref('')

const isVisible = computed(() => props.visible)

const closeModal = () => {
  reason.value = ''
  emit('close')
}

const confirmDelete = () => {
  if (!reason.value.trim()) {
    notification.warning('Lütfen silme nedenini açıklayın')
    return
  }
  emit('confirm', reason.value)
  closeModal()
}
</script>

<template>
  <transition name="modal">
    <div v-if="isVisible" class="modal-overlay" @click.self="closeModal">
      <div class="modal-container">
        <div class="modal-icon">❌</div>
        
        <h2>DİKKAT!</h2>
        <p>Silmek istediğiniz işlem ilgili şubenin loglarına eklenecektir. Silme nedeninizi lütfen açıklayınız.</p>
        
        <div class="form-group">
          <textarea 
            v-model="reason" 
            placeholder="Açıklama yazın..."
            rows="4"
            required
          ></textarea>
        </div>
        
        <div class="modal-actions">
          <button class="btn-cancel" @click="closeModal">
            VAZGEÇ
          </button>
          <button class="btn-confirm" @click="confirmDelete">
            ONAYLA
          </button>
        </div>
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
  border-radius: 20px;
  padding: 30px;
  max-width: 500px;
  width: 90%;
  position: relative;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5);
  text-align: center;
}

.modal-icon {
  font-size: 48px;
  margin-bottom: 20px;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.2));
}

h2 {
  color: white;
  margin-bottom: 15px;
  font-size: 24px;
}

p {
  color: rgba(255, 255, 255, 0.8);
  margin-bottom: 25px;
  line-height: 1.5;
}

.form-group {
  margin-bottom: 25px;
}

.form-group textarea {
  width: 100%;
  padding: 12px 15px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 8px;
  color: white;
  font-size: 14px;
  resize: vertical;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.form-group textarea::placeholder {
  color: rgba(255, 255, 255, 0.4);
}

.form-group textarea:focus {
  outline: none;
  background: rgba(255, 255, 255, 0.15);
  border-color: rgba(255, 255, 255, 0.4);
}

.modal-actions {
  display: flex;
  gap: 15px;
}

.btn-cancel,
.btn-confirm {
  flex: 1;
  padding: 12px 20px;
  border: none;
  border-radius: 8px;
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

.btn-confirm {
  background: #ff5a8c;
  color: white;
}

.btn-confirm:hover {
  background: #e54a7c;
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