<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import BinanceDeposits from './BinanceDeposits.vue'

const { t } = useI18n()

const isOpen = ref(false)

const open = () => {
  isOpen.value = true
}

const close = () => {
  isOpen.value = false
}

// Expose methods for parent component
defineExpose({
  open,
  close
})
</script>

<template>
  <!-- Modal Backdrop -->
  <Teleport to="body">
    <div v-if="isOpen" class="fixed inset-0 z-50 overflow-y-auto">
      <!-- Background overlay -->
      <div 
        class="fixed inset-0 bg-black bg-opacity-50 transition-opacity"
        @click="close"
      ></div>
      
      <!-- Modal Content -->
      <div class="flex min-h-full items-center justify-center p-4">
        <div 
          class="relative transform overflow-hidden rounded-2xl bg-white shadow-2xl transition-all w-full max-w-6xl"
          @click.stop
        >
          <!-- Header -->
          <div class="bg-gradient-to-r from-teal-600 to-cyan-600 px-6 py-4">
            <div class="flex items-center justify-between">
              <h3 class="text-xl font-bold text-white flex items-center gap-2">
                <span class="text-2xl">₮</span>
                {{ t('usdt.title') }}
              </h3>
              <button
                @click="close"
                class="text-white hover:bg-white/20 rounded-lg p-2 transition-colors"
              >
                <span class="material-symbols-outlined" aria-hidden="true">close</span>
              </button>
            </div>
          </div>
          
          <!-- Body -->
          <div class="p-6 max-h-[80vh] overflow-y-auto">
            <BinanceDeposits />
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
/* Smooth modal animations */
.fixed {
  animation: fadeIn 0.2s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}
</style>