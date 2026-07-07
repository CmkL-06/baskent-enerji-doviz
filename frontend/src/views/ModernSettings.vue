<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import AppPageHeader from '@/components/common/AppPageHeader.vue'

const router = useRouter()

// Settings state
const tickerEnabled = ref(false)
const showSuccessMessage = ref(false)

// Load settings from localStorage on mount
onMounted(() => {
  const savedTickerPreference = localStorage.getItem('showTicker')
  tickerEnabled.value = savedTickerPreference === 'true'
})

// Save ticker setting
const saveTickerSetting = () => {
  localStorage.setItem('showTicker', tickerEnabled.value.toString())
  showSuccessMessage.value = true
  
  // Hide success message after 3 seconds
  setTimeout(() => {
    showSuccessMessage.value = false
  }, 3000)
  
  // Reload the page to apply the ticker visibility change
  setTimeout(() => {
    window.location.reload()
  }, 1000)
}

// Toggle ticker setting
const toggleTicker = () => {
  tickerEnabled.value = !tickerEnabled.value
  saveTickerSetting()
}
</script>

<template>
  <div class="modern-settings">
    <!-- Header -->
    <AppPageHeader icon="settings" title="Ayarlar" subtitle="Uygulama ayarlarını buradan yönetebilirsiniz" />

    <!-- Success Message -->
    <div v-if="showSuccessMessage" class="success-alert">
      <span class="material-symbols-outlined" aria-hidden="true">check_circle</span>
      <span>Ayarlar başarıyla kaydedildi!</span>
    </div>

    <!-- Settings Sections -->
    <div class="settings-container">
      <!-- Display Settings -->
      <div class="settings-section">
        <div class="section-header">
          <h2 class="section-title">
            <span class="material-symbols-outlined" aria-hidden="true">display_settings</span>
            Görüntü Ayarları
          </h2>
        </div>
        
        <div class="settings-grid">
          <!-- Ticker Setting -->
          <div class="setting-item">
            <div class="setting-content">
              <div class="setting-info">
                <h3 class="setting-title">Kur Ticker'ı</h3>
                <p class="setting-description">
                  Üst kısımda kayan döviz kurları gösterimini açıp kapatabilirsiniz
                </p>
              </div>
              <div class="setting-control">
                <label class="toggle-switch">
                  <input 
                    type="checkbox" 
                    v-model="tickerEnabled"
                    @change="saveTickerSetting"
                  >
                  <span class="toggle-slider"></span>
                </label>
              </div>
            </div>
            <div class="setting-preview" v-if="tickerEnabled">
              <div class="preview-ticker">
                <span class="ticker-sample">
                  USD: A:30.50 / S:31.00 ₺ • EUR: A:33.20 / S:33.80 ₺ • GBP: A:38.50 / S:39.20 ₺
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Other Settings Sections (placeholder for future settings) -->
      <div class="settings-section">
        <div class="section-header">
          <h2 class="section-title">
            <span class="material-symbols-outlined" aria-hidden="true">notifications</span>
            Bildirim Ayarları
          </h2>
        </div>
        
        <div class="settings-grid">
          <div class="setting-item disabled">
            <div class="setting-content">
              <div class="setting-info">
                <h3 class="setting-title">Bildirimler</h3>
                <p class="setting-description">
                  Yakında eklenecek
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- System Settings -->
      <div class="settings-section">
        <div class="section-header">
          <h2 class="section-title">
            <span class="material-symbols-outlined" aria-hidden="true">tune</span>
            Sistem Ayarları
          </h2>
        </div>
        
        <div class="settings-grid">
          <div class="setting-item disabled">
            <div class="setting-content">
              <div class="setting-info">
                <h3 class="setting-title">Dil Seçimi</h3>
                <p class="setting-description">
                  Yakında eklenecek
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modern-settings {
  max-width: 1200px;
  margin: 0 auto;
}

/* Success Alert */
.success-alert {
  background: #10b981;
  color: white;
  padding: 1rem 1.5rem;
  border-radius: 12px;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 2rem;
  animation: slideDown 0.3s ease;
}

@keyframes slideDown {
  from {
    transform: translateY(-20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

/* Settings Container */
.settings-container {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

/* Settings Section */
.settings-section {
  background: white;
  border-radius: 16px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  overflow: hidden;
}

.section-header {
  padding: 1.5rem;
  border-bottom: 1px solid #f3f4f6;
  background: #fafbfc;
}

.section-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: #1f2937;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.section-title .material-symbols-outlined {
  font-size: 1.5rem;
  color: #6b7280;
}

/* Settings Grid */
.settings-grid {
  padding: 1.5rem;
}

/* Setting Item */
.setting-item {
  padding: 1.5rem;
  border-radius: 12px;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  transition: border-color 0.2s, box-shadow 0.2s, background-color 0.2s;
}

.setting-item:hover:not(.disabled) {
  background: white;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

.setting-item.disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.setting-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 2rem;
}

.setting-info {
  flex: 1;
}

.setting-title {
  font-size: 1.1rem;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 0.5rem;
}

.setting-description {
  font-size: 0.95rem;
  color: #6b7280;
  line-height: 1.5;
}

.setting-control {
  flex-shrink: 0;
}

/* Toggle Switch */
.toggle-switch {
  position: relative;
  display: inline-block;
  width: 60px;
  height: 32px;
}

.toggle-switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle-slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #cbd5e1;
  transition: 0.4s;
  border-radius: 34px;
}

.toggle-slider:before {
  position: absolute;
  content: "";
  height: 24px;
  width: 24px;
  left: 4px;
  bottom: 4px;
  background-color: white;
  transition: 0.4s;
  border-radius: 50%;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

input:checked + .toggle-slider {
  background: linear-gradient(135deg, #6366f1 0%, #7c3aed 100%);
}

input:focus + .toggle-slider {
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.2);
}

input:checked + .toggle-slider:before {
  transform: translateX(28px);
}

/* Setting Preview */
.setting-preview {
  margin-top: 1rem;
  padding: 1rem;
  background: #1f2937;
  border-radius: 8px;
  overflow: hidden;
}

.preview-ticker {
  color: #fbbf24;
  font-size: 0.9rem;
  white-space: nowrap;
  animation: ticker-preview 10s linear infinite;
}

@keyframes ticker-preview {
  0% { transform: translateX(0); }
  100% { transform: translateX(-50%); }
}

.ticker-sample {
  display: inline-block;
  padding-right: 2rem;
}

/* Responsive */
@media (max-width: 768px) {
  .setting-content {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .setting-control {
    margin-top: 1rem;
  }
}
</style>