<template>
  <div class="login-screen">
    <!-- Animated Background -->
    <div class="animated-bg">
      <div class="gradient-circle circle-1"></div>
      <div class="gradient-circle circle-2"></div>
      <div class="gradient-circle circle-3"></div>
    </div>
    
    <!-- Login Container -->
    <div class="login-container">
      <div class="login-card">
        <!-- Logo Section -->
        <div class="logo-section">
          <div class="logo-wrapper">
            <span class="material-symbols-outlined logo-icon">storefront</span>
          </div>
          <h1 class="app-title">Money Transfer Turkey</h1>
          <p class="app-subtitle">Bayi Paneli</p>
        </div>

        <!-- Login Form -->
        <form @submit.prevent="handleSubmit" class="login-form">
          <div class="form-group">
            <div class="input-wrapper">
              <span class="material-symbols-outlined input-icon">person</span>
              <input
                v-model="loginForm.username"
                type="text"
                required
                placeholder="Kullanıcı Adı"
                class="form-input"
                :disabled="authStore.isLoading"
              >
            </div>
          </div>
          
          <div class="form-group">
            <div class="input-wrapper">
              <span class="material-symbols-outlined input-icon">lock</span>
              <input 
                v-model="loginForm.password" 
                type="password" 
                required
                placeholder="Şifre"
                class="form-input"
                :disabled="authStore.isLoading"
              >
            </div>
          </div>

          <!-- Remember Me & Forgot Password -->
          <div class="form-options">
            <label class="remember-me">
              <input type="checkbox" v-model="rememberMe">
              <span>Beni hatırla</span>
            </label>
            <span class="forgot-password disabled-link">Şifremi unuttum</span>
          </div>

          <!-- Error Message -->
          <transition name="fade">
            <div v-if="authStore.error" class="error-message">
              <span class="material-symbols-outlined" aria-hidden="true">error</span>
              {{ authStore.error }}
            </div>
          </transition>

          <!-- Submit Button -->
          <button type="submit" class="submit-btn" :disabled="authStore.isLoading">
            <span v-if="authStore.isLoading" class="loading-spinner">
              <span class="spinner"></span>
              Giriş yapılıyor...
            </span>
            <span v-else class="btn-content">
              <span class="material-symbols-outlined" aria-hidden="true">login</span>
              Giriş Yap
            </span>
          </button>

        </form>

        <!-- Footer -->
        <div class="login-footer">
          <p>&copy; {{ new Date().getFullYear() }} Money Transfer Turkey. Tüm hakları saklıdır.</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const rememberMe = ref(false)

const loginForm = reactive({
  username: '',
  password: ''
})

async function handleSubmit() {
  try {
    const loginData = {
      mail: loginForm.username,
      password: loginForm.password
    }
    
    await authStore.login(loginData)

    if (rememberMe.value) {
      localStorage.setItem('rememberMe', 'true')
    }
  } catch (error) {
    // Error is handled in store
  }
}

onMounted(() => {
  // Check if user chose to be remembered
  if (localStorage.getItem('rememberMe') === 'true') {
    rememberMe.value = true
  }
})
</script>

<style scoped>
/* Base Styles */
.login-screen {
  position: relative;
  width: 100vw;
  height: 100vh;
  overflow: hidden;
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

/* Animated Background */
.animated-bg {
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, #6366f1 0%, #7c3aed 100%);
  overflow: hidden;
}

.gradient-circle {
  position: absolute;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%);
  animation: float 20s infinite ease-in-out;
}

.circle-1 {
  width: 600px;
  height: 600px;
  top: -200px;
  left: -200px;
  animation-delay: 0s;
}

.circle-2 {
  width: 800px;
  height: 800px;
  bottom: -400px;
  right: -400px;
  animation-delay: 7s;
}

.circle-3 {
  width: 400px;
  height: 400px;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  animation-delay: 14s;
}

@keyframes float {
  0%, 100% { transform: translate(0, 0) scale(1); }
  25% { transform: translate(30px, -30px) scale(1.05); }
  50% { transform: translate(-20px, 20px) scale(0.95); }
  75% { transform: translate(20px, 30px) scale(1.02); }
}

/* Login Container */
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
  backdrop-filter: blur(20px);
  border-radius: var(--radius-xl);
  box-shadow: 0 30px 60px rgba(0, 0, 0, 0.3);
  width: 100%;
  max-width: 480px;
  padding: 3rem 2.5rem 2rem;
  animation: slideUp 0.6s ease-out;
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Logo Section */
.logo-section {
  text-align: center;
  margin-bottom: 2.5rem;
}

.logo-wrapper {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 80px;
  height: 80px;
  background: linear-gradient(135deg, #6366f1 0%, #7c3aed 100%);
  border-radius: var(--radius-xl);
  margin-bottom: 1.5rem;
  box-shadow: 0 10px 30px rgba(102, 126, 234, 0.4);
}

.logo-icon {
  font-size: 40px;
  color: white;
}

.app-title {
  margin: 0;
  font-size: 2rem;
  font-weight: 800;
  color: var(--color-text);
  letter-spacing: -0.02em;
}

.app-subtitle {
  margin: 0.5rem 0 0;
  font-size: 1rem;
  color: var(--color-text-secondary);
}

/* Form Styles */
.login-form {
  margin-top: 2rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

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

.input-icon {
  position: absolute;
  left: 1rem;
  color: var(--color-text-muted);
  font-size: 22px;
  transition: color 0.3s ease;
}

.input-wrapper:focus-within .input-icon {
  color: var(--color-primary);
}

.form-input {
  flex: 1;
  padding: 1rem 1rem 1rem 3rem;
  background: transparent;
  border: none;
  font-size: 1rem;
  color: var(--color-text);
  outline: none;
}

.form-input::placeholder {
  color: var(--color-text-muted);
}

.form-input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* Form Options */
.form-options {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.remember-me {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  color: var(--color-text-secondary);
  font-size: 0.9rem;
}

.remember-me input[type="checkbox"] {
  width: 18px;
  height: 18px;
  accent-color: var(--color-primary);
}

.forgot-password {
  color: var(--color-text-muted);
  font-size: 0.9rem;
  font-weight: 500;
  cursor: default;
}

/* Error Message */
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

.error-message .material-symbols-outlined {
  font-size: 20px;
}

/* Submit Button */
.submit-btn {
  width: 100%;
  padding: 1rem;
  background: linear-gradient(135deg, #6366f1 0%, #7c3aed 100%);
  border: none;
  border-radius: var(--radius-lg);
  color: white;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.3s ease, transform 0.3s ease, box-shadow 0.3s ease;
  box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
}

.submit-btn:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(102, 126, 234, 0.5);
}

.submit-btn:active:not(:disabled) {
  transform: translateY(0);
}

.submit-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.btn-content {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.loading-spinner {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
}

.spinner {
  width: 20px;
  height: 20px;
  border: 3px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* UI Selector */
.ui-selector {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid var(--color-border);
}

.selector-title {
  margin: 0 0 1rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  text-align: center;
}

.style-options {
  display: grid;
  gap: 0.75rem;
}

.style-option {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.875rem 1rem;
  background: var(--color-bg-page);
  border: 2px solid var(--color-border);
  border-radius: var(--radius-md);
  position: relative;
  transition: background-color 0.3s ease, border-color 0.3s ease;
}

.style-option.selected {
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1), rgba(118, 75, 162, 0.1));
  border-color: var(--color-primary);
}

.style-option.disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.style-option .material-symbols-outlined {
  font-size: 24px;
  color: var(--color-primary);
}

.style-option.disabled .material-symbols-outlined {
  color: var(--color-text-muted);
}

.style-option span:nth-child(2) {
  flex: 1;
  font-weight: 500;
  color: var(--color-text);
}

.badge {
  padding: 0.25rem 0.5rem;
  background: var(--color-primary);
  color: white;
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 600;
}

.badge.disabled {
  background: var(--color-border);
  color: var(--color-text-secondary);
}

/* Footer */
.login-footer {
  margin-top: 2rem;
  text-align: center;
}

.login-footer p {
  margin: 0;
  font-size: 0.875rem;
  color: var(--color-text-muted);
}

/* Transitions */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* Responsive */
@media (max-width: 640px) {
  .login-card {
    padding: 2rem 1.5rem 1.5rem;
  }
  
  .app-title {
    font-size: 1.75rem;
  }
  
  .app-subtitle {
    font-size: 0.9rem;
  }
}
</style>