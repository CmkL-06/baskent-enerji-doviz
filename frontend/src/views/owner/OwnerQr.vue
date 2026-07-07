<script setup lang="ts">
import { ref, computed, watch } from 'vue'

const qrText = ref('https://mt-turkey.com')
const qrSize = ref(300)
const qrColor = ref('1e293b')
const qrBg = ref('ffffff')

const qrUrl = computed(() => {
  const text = encodeURIComponent(qrText.value || 'https://mt-turkey.com')
  const color = qrColor.value.replace('#', '')
  const bg = qrBg.value.replace('#', '')
  return `https://api.qrserver.com/v1/create-qr-code/?size=${qrSize.value}x${qrSize.value}&data=${text}&color=${color}&bgcolor=${bg}&format=png`
})

function download() {
  const a = document.createElement('a')
  a.href = qrUrl.value
  a.download = `qr-code-${Date.now()}.png`
  a.target = '_blank'
  a.click()
}
</script>

<template>
  <div>
    <h3 class="qr-title"><span class="material-symbols-outlined" aria-hidden="true">qr_code_2</span> QR Kod Oluşturucu</h3>

    <div class="qr-layout">
      <div class="qr-form-card">
        <div class="qr-field">
          <label>İçerik / URL</label>
          <input v-model="qrText" class="qr-input" placeholder="https://mt-turkey.com" />
        </div>

        <div class="qr-row">
          <div class="qr-field">
            <label>Boyut (px)</label>
            <select v-model.number="qrSize" class="qr-input">
              <option :value="200">200 × 200</option>
              <option :value="300">300 × 300</option>
              <option :value="400">400 × 400</option>
              <option :value="500">500 × 500</option>
            </select>
          </div>
          <div class="qr-field">
            <label>Renk</label>
            <div class="qr-color-row">
              <input type="color" class="qr-color" :value="'#' + qrColor" @input="qrColor = ($event.target as HTMLInputElement).value.replace('#','')" />
              <span class="qr-color-hex">#{{ qrColor }}</span>
            </div>
          </div>
          <div class="qr-field">
            <label>Arka Plan</label>
            <div class="qr-color-row">
              <input type="color" class="qr-color" :value="'#' + qrBg" @input="qrBg = ($event.target as HTMLInputElement).value.replace('#','')" />
              <span class="qr-color-hex">#{{ qrBg }}</span>
            </div>
          </div>
        </div>
      </div>

      <div class="qr-preview-card">
        <div class="qr-preview-box" :style="{ backgroundColor: '#' + qrBg }">
          <img :src="qrUrl" alt="QR Code" :width="Math.min(qrSize, 280)" :height="Math.min(qrSize, 280)" />
        </div>
        <button class="qr-download" @click="download">
          <span class="material-symbols-outlined" aria-hidden="true">download</span> QR Kodunu İndir
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.qr-title { display: flex; align-items: center; gap: 0.5rem; font-size: 1.1rem; font-weight: 700; color: #1e293b; margin: 0 0 1.25rem; }

.qr-layout { display: grid; grid-template-columns: 1fr auto; gap: 1.5rem; align-items: start; }
@media (max-width: 768px) { .qr-layout { grid-template-columns: 1fr; } }

.qr-form-card { background: white; border-radius: 12px; padding: 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,.08); }
.qr-field { margin-bottom: 1rem; }
.qr-field label { display: block; font-size: 0.8rem; font-weight: 600; color: #475569; margin-bottom: 0.3rem; }

.qr-input {
  width: 100%; padding: 0.6rem 0.75rem; border: 2px solid #e2e8f0;
  border-radius: 8px; font-size: 0.9rem; color: #1e293b;
  background: #f8fafc; outline: none; box-sizing: border-box;
  transition: border-color .2s;
}
.qr-input:focus { border-color: #3b82f6; background: white; }

.qr-row { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 1rem; }
.qr-color-row { display: flex; align-items: center; gap: 0.5rem; }
.qr-color { width: 36px; height: 36px; border: 2px solid #e2e8f0; border-radius: 8px; cursor: pointer; padding: 2px; }
.qr-color-hex { font-size: 0.8rem; font-family: monospace; color: #64748b; }

.qr-preview-card {
  background: white; border-radius: 12px; padding: 1.5rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.08);
  display: flex; flex-direction: column; align-items: center; gap: 1rem;
}
.qr-preview-box {
  border-radius: 12px; padding: 1rem;
  display: flex; align-items: center; justify-content: center;
  border: 2px dashed #e2e8f0;
}
.qr-preview-box img { border-radius: 4px; }

.qr-download {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.6rem 1.5rem; background: #3b82f6; color: white;
  border: none; border-radius: 8px; cursor: pointer; font-size: 0.85rem;
  font-weight: 600; transition: background-color 0.2s;
}
.qr-download:hover { background: #2563eb; }
</style>
