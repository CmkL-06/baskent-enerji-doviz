<template>
  <div class="settings-window">
    <div class="settings-header">
      <h2>Ayarlar</h2>
      <p class="subtitle">Döviz bürosu tercihlerinizi yapılandırın</p>
    </div>

    <div class="settings-content">
      <!-- General Settings -->
      <div class="settings-section">
        <h3>Genel Ayarlar</h3>
        
        <div class="setting-item">
          <label>Varsayılan Kaynak Para Birimi</label>
          <select v-model="localSettings.defaultFromCurrency" class="setting-select">
            <option v-for="currency in exchangeStore.currencies" :key="currency.code" :value="currency.code">
              {{ currency.code }} - {{ currency.name }}
            </option>
          </select>
        </div>

        <div class="setting-item">
          <label>Varsayılan Hedef Para Birimi</label>
          <select v-model="localSettings.defaultToCurrency" class="setting-select">
            <option v-for="currency in exchangeStore.currencies" :key="currency.code" :value="currency.code">
              {{ currency.code }} - {{ currency.name }}
            </option>
          </select>
        </div>

        <div class="setting-item">
          <label>Hizmet Ücreti Yüzdesi</label>
          <div class="input-with-unit">
            <input
              v-model.number="localSettings.feePercentage"
              type="number"
              class="setting-input"
              min="0"
              max="10"
              step="0.1"
            />
            <span class="unit">%</span>
          </div>
        </div>
      </div>

      <!-- Preferences -->
      <div class="settings-section">
        <h3>Tercihler</h3>
        
        <div class="setting-item">
          <label class="checkbox-label">
            <input
              v-model="localSettings.soundEnabled"
              type="checkbox"
              class="setting-checkbox"
            />
            <span>Ses bildirimlerini etkinleştir</span>
          </label>
        </div>

        <div class="setting-item">
          <label>Tema</label>
          <div class="theme-options">
            <label class="radio-label">
              <input
                v-model="localSettings.theme"
                type="radio"
                value="light"
                name="theme"
              />
              <span>Açık</span>
            </label>
            <label class="radio-label">
              <input
                v-model="localSettings.theme"
                type="radio"
                value="dark"
                name="theme"
                disabled
              />
              <span>Koyu (Yakında)</span>
            </label>
          </div>
        </div>
      </div>

      <!-- Actions -->
      <div class="settings-actions">
        <button @click="saveSettings" class="btn-primary">
          Ayarları Kaydet
        </button>
        <button @click="resetSettings" class="btn-secondary">
          Varsayılanlara Dön
        </button>
      </div>
    </div>

    <!-- Success Notification -->
    <Transition name="fade">
      <div v-if="showSuccess" class="success-message">
        ✓ Ayarlar başarıyla kaydedildi!
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useExchangeStore } from '../../stores/exchange';

const exchangeStore = useExchangeStore();

// Local settings state
const localSettings = reactive({
  defaultFromCurrency: '',
  defaultToCurrency: '',
  feePercentage: 0,
  soundEnabled: true,
  theme: 'light',
});

const showSuccess = ref(false);

// Load current settings
const loadSettings = () => {
  Object.assign(localSettings, exchangeStore.settings);
};

// Save settings
const saveSettings = () => {
  exchangeStore.updateSettings(localSettings);
  showSuccess.value = true;
  setTimeout(() => {
    showSuccess.value = false;
  }, 3000);
};

// Reset to defaults
const resetSettings = () => {
  localSettings.defaultFromCurrency = 'USD';
  localSettings.defaultToCurrency = 'EUR';
  localSettings.feePercentage = 1.5;
  localSettings.soundEnabled = true;
  localSettings.theme = 'light';
};

// Load data on mount
onMounted(async () => {
  await exchangeStore.fetchCurrencies();
  loadSettings();
});
</script>

<style scoped>
.settings-window {
  padding: 24px;
  height: 100%;
  overflow-y: auto;
}

.settings-header {
  text-align: center;
  margin-bottom: 32px;
}

.settings-header h2 {
  margin: 0;
  font-size: 24px;
  color: #333;
}

.subtitle {
  margin: 8px 0 0;
  color: #666;
  font-size: 14px;
}

.settings-content {
  max-width: 600px;
  margin: 0 auto;
}

.settings-section {
  background: white;
  border-radius: 8px;
  padding: 24px;
  margin-bottom: 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.settings-section h3 {
  margin: 0 0 20px;
  font-size: 18px;
  color: #333;
  border-bottom: 1px solid #e0e0e0;
  padding-bottom: 12px;
}

.setting-item {
  margin-bottom: 20px;
}

.setting-item:last-child {
  margin-bottom: 0;
}

.setting-item label {
  display: block;
  margin-bottom: 8px;
  font-weight: 500;
  color: #444;
  font-size: 14px;
}

.setting-select,
.setting-input {
  width: 100%;
  padding: 10px 12px;
  border: 2px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  background: white;
  transition: border-color 0.3s;
}

.setting-select:focus,
.setting-input:focus {
  outline: none;
  border-color: #0078d4;
}

.input-with-unit {
  display: flex;
  align-items: center;
  gap: 8px;
}

.input-with-unit .setting-input {
  width: 100px;
}

.unit {
  font-size: 14px;
  color: #666;
}

.checkbox-label,
.radio-label {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 14px;
  color: #444;
  margin-bottom: 8px;
}

.setting-checkbox,
.radio-label input[type="radio"] {
  cursor: pointer;
}

.theme-options {
  display: flex;
  gap: 24px;
}

.settings-actions {
  display: flex;
  gap: 12px;
  margin-top: 32px;
}

.btn-primary,
.btn-secondary {
  flex: 1;
  padding: 12px 24px;
  border: none;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s;
}

.btn-primary {
  background: #0078d4;
  color: white;
}

.btn-primary:hover {
  background: #106ebe;
  transform: translateY(-1px);
  box-shadow: 0 4px 8px rgba(0, 120, 212, 0.3);
}

.btn-secondary {
  background: #e0e0e0;
  color: #333;
}

.btn-secondary:hover {
  background: #d0d0d0;
}

.success-message {
  position: fixed;
  top: 20px;
  right: 20px;
  background: #4caf50;
  color: white;
  padding: 12px 24px;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(76, 175, 80, 0.3);
  font-weight: 500;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>