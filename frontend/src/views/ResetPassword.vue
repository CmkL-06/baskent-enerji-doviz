<template>
  <div class="login-screen">
    <div class="animated-bg">
      <div class="gradient-circle circle-1"></div>
      <div class="gradient-circle circle-2"></div>
      <div class="gradient-circle circle-3"></div>
    </div>

    <div class="login-container">
      <div class="login-card">
        <div class="logo-section">
          <div class="logo-wrapper">
            <span class="material-symbols-outlined logo-icon">lock_reset</span>
          </div>
          <h1 class="app-title">Şifre Sıfırlama</h1>
          <p class="app-subtitle">Yeni şifrenizi belirleyin</p>
        </div>

        <form v-if="!missingParams && !success" @submit.prevent="handleSubmit" class="login-form">
          <div class="form-group">
            <div class="input-wrapper">
              <span class="material-symbols-outlined input-icon">lock</span>
              <input
                v-model="newPassword"
                type="password"
                required
                minlength="8"
                placeholder="Yeni Şifre"
                class="form-input"
                :disabled="loading"
              >
            </div>
          </div>

          <div class="form-group">
            <div class="input-wrapper">
              <span class="material-symbols-outlined input-icon">lock</span>
              <input
                v-model="confirmPassword"
                type="password"
                required
                minlength="8"
                placeholder="Yeni Şifre (Tekrar)"
                class="form-input"
                :disabled="loading"
              >
            </div>
          </div>

          <transition name="fade">
            <div v-if="error" class="error-message">
              <span class="material-symbols-outlined" aria-hidden="true">error</span>
              {{ error }}
            </div>
          </transition>

          <button type="submit" class="submit-btn" :disabled="loading">
            <span v-if="loading" class="loading-spinner">
              <span class="spinner"></span>
              Kaydediliyor...
            </span>
            <span v-else class="btn-content">
              <span class="material-symbols-outlined" aria-hidden="true">check</span>
              Şifreyi Güncelle
            </span>
          </button>
        </form>

        <div v-else-if="success" class="success-message success-block">
          <span class="material-symbols-outlined" aria-hidden="true">check_circle</span>
          Şifreniz güncellendi. Giriş sayfasına yönlendiriliyorsunuz...
        </div>

        <div v-else class="error-message">
          <span class="material-symbols-outlined" aria-hidden="true">error</span>
          Geçersiz sıfırlama bağlantısı. Lütfen "Şifremi unuttum" ile yeni bir bağlantı isteyin.
        </div>

        <div class="login-footer">
          <p>&copy; {{ new Date().getFullYear() }} Exchange Office. Tüm hakları saklıdır.</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import apiService from '@/services/apiservice'

const route = useRoute()
const router = useRouter()

const token = computed(() => String(route.query.token || ''))
const email = computed(() => String(route.query.email || ''))
const missingParams = computed(() => !token.value || !email.value)

const newPassword = ref('')
const confirmPassword = ref('')
const loading = ref(false)
const error = ref('')
const success = ref(false)

async function handleSubmit() {
  error.value = ''
  if (newPassword.value.length < 8) {
    error.value = 'Şifre en az 8 karakter olmalı.'
    return
  }
  if (newPassword.value !== confirmPassword.value) {
    error.value = 'Şifreler eşleşmiyor.'
    return
  }

  loading.value = true
  try {
    await apiService.resetPassword({
      email: email.value,
      token: token.value,
      newPassword: newPassword.value
    })
    success.value = true
    setTimeout(() => router.push('/login'), 2500)
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Sıfırlama başarısız. Bağlantının süresi dolmuş olabilir.'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  if (missingParams.value) return
})
</script>

<style scoped>
.login-screen {
  position: relative;
  width: 100vw;
  height: 100vh;
  overflow: hidden;
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

.animated-bg {
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, var(--color-primary) 0%, #7c3aed 100%);
  overflow: hidden;
}

.gradient-circle {
  position: absolute;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%);
  animation: float 20s infinite ease-in-out;
}

.circle-1 { width: 600px; height: 600px; top: -200px; left: -200px; animation-delay: 0s; }
.circle-2 { width: 800px; height: 800px; bottom: -400px; right: -400px; animation-delay: 7s; }
.circle-3 { width: 400px; height: 400px; top: 50%; left: 50%; transform: translate(-50%, -50%); animation-delay: 14s; }

@keyframes float {
  0%, 100% { transform: translate(0, 0) scale(1); }
  25% { transform: translate(30px, -30px) scale(1.05); }
  50% { transform: translate(-20px, 20px) scale(0.95); }
  75% { transform: translate(20px, 30px) scale(1.02); }
}

.login-container {
  position: relative;
  z-index: 10;
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.login-card {
  background: rgba(255, 255, 255, 0.98);
  backdrop-filter: var(--glass-blur-strong);
  border-radius: var(--radius-xl);
  box-shadow: 0 30px 60px rgba(0, 0, 0, 0.3);
  width: 100%;
  max-width: 480px;
  padding: 3rem 2.5rem 2rem;
  animation: slideUp 0.6s ease-out;
}

@keyframes slideUp {
  from { opacity: 0; transform: translateY(30px); }
  to { opacity: 1; transform: translateY(0); }
}

.logo-section { text-align: center; margin-bottom: 2.5rem; }

.logo-wrapper {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 80px;
  height: 80px;
  background: linear-gradient(135deg, var(--color-primary) 0%, #7c3aed 100%);
  border-radius: var(--radius-xl);
  margin-bottom: 1.5rem;
  box-shadow: 0 10px 30px rgba(102, 126, 234, 0.4);
}

.logo-icon { font-size: 40px; color: white; }
.app-title { margin: 0; font-size: 2rem; font-weight: 800; color: var(--color-text); letter-spacing: -0.02em; }
.app-subtitle { margin: 0.5rem 0 0; font-size: 1rem; color: var(--color-text-secondary); }

.login-form { margin-top: 1rem; }
.form-group { margin-bottom: 1.5rem; }

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  background: var(--color-bg-page);
  border: 2px solid var(--color-border);
  border-radius: var(--radius-lg);
  transition: border-color 0.3s ease, background-color 0.3s ease;
}

.input-wrapper:focus-within {
  background: white;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.input-icon { position: absolute; left: 1rem; color: var(--color-text-muted); font-size: 22px; }
.input-wrapper:focus-within .input-icon { color: var(--color-primary); }

.form-input {
  flex: 1;
  padding: 1rem 1rem 1rem 3rem;
  background: transparent;
  border: none;
  font-size: 1rem;
  color: var(--color-text);
  outline: none;
}

.form-input:disabled { opacity: 0.6; cursor: not-allowed; }

.error-message {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.875rem 1rem;
  background: var(--color-danger-bg);
  border: 1px solid var(--color-danger);
  border-radius: var(--radius-md);
  color: var(--color-danger);
  font-size: 0.9rem;
  margin-bottom: 1.5rem;
}

.success-message {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.875rem 1rem;
  background: var(--color-success-bg, #e6f9ee);
  border: 1px solid var(--color-success, #10b981);
  border-radius: var(--radius-md);
  color: var(--color-success, #10b981);
  font-size: 0.9rem;
}

.success-block { justify-content: center; text-align: center; }

.error-message .material-symbols-outlined,
.success-message .material-symbols-outlined { font-size: 20px; }

.submit-btn {
  width: 100%;
  padding: 1rem;
  background: linear-gradient(135deg, var(--color-primary) 0%, #7c3aed 100%);
  border: none;
  border-radius: var(--radius-lg);
  color: white;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.3s ease, transform 0.3s ease, box-shadow 0.3s ease;
  box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
}

.submit-btn:hover:not(:disabled) { transform: translateY(-2px); box-shadow: 0 6px 20px rgba(102, 126, 234, 0.5); }
.submit-btn:disabled { opacity: 0.7; cursor: not-allowed; }

.btn-content { display: flex; align-items: center; justify-content: center; gap: 0.5rem; }
.loading-spinner { display: flex; align-items: center; justify-content: center; gap: 0.75rem; }

.spinner {
  width: 20px;
  height: 20px;
  border: 3px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

.login-footer { margin-top: 2rem; text-align: center; }
.login-footer p { margin: 0; font-size: 0.875rem; color: var(--color-text-muted); }

.fade-enter-active, .fade-leave-active { transition: opacity 0.3s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

@media (max-width: 640px) {
  .login-card { padding: 2rem 1.5rem 1.5rem; }
  .app-title { font-size: 1.75rem; }
  .app-subtitle { font-size: 0.9rem; }
}
</style>
