<template>
  <div class="login-page">
    <div class="login-card">
      <h1>SmileMedical</h1>
      <p class="subtitle">API: api/v1/User/login</p>
      <form @submit.prevent="submit" class="form">
        <div class="field">
          <label>E-posta</label>
          <input v-model="email" type="email" required placeholder="ornek@email.com" />
        </div>
        <div class="field">
          <label>Şifre</label>
          <input v-model="password" type="password" required placeholder="••••••••" />
        </div>
        <p v-if="error" class="error">{{ error }}</p>
        <p v-if="success" class="success">Giriş başarılı, yönlendiriliyorsunuz...</p>
        <button type="submit" class="btn" :disabled="loading">
          {{ loading ? 'Giriş yapılıyor...' : 'Giriş' }}
        </button>
      </form>
      <p class="hint">Backend API çalışmıyorsa demo giriş ile layout’a girebilirsiniz.</p>
      <button type="button" class="btn btn-secondary" @click="demoLogin">Demo giriş (API yok)</button>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import api from '../api/client'

const router = useRouter()
const route = useRoute()
const { setAuth } = useAuth()

const email = ref('')
const password = ref('')
const loading = ref(false)
const error = ref('')
const success = ref(false)

async function submit() {
  error.value = ''
  loading.value = true
  try {
    const { data } = await api.post('/api/v1/User/login', {
      Mail: email.value,
      Password: password.value,
    })
    const token = data?.apiToken ?? data?.token ?? data?.accessToken ?? data?.jwt
    const user = data?.userInfo ?? data?.user ?? { email: email.value, userName: data?.userName ?? email.value }
    if (token) {
      setAuth(token, user)
      success.value = true
      const redirect = route.query.redirect || '/'
      setTimeout(() => router.push(redirect), 500)
    } else {
      setAuth(JSON.stringify(data), user)
      success.value = true
      setTimeout(() => router.push('/'), 500)
    }
  } catch (e) {
    error.value = e.response?.data?.message || e.message || 'Giriş başarısız. API çalışıyor mu?'
  } finally {
    loading.value = false
  }
}

function demoLogin() {
  setAuth('demo-token', { email: email.value || 'demo@test.com', userName: 'Demo' })
  router.push(route.query.redirect || '/')
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
}
.login-card {
  background: #fff;
  border-radius: 12px;
  padding: 2rem;
  width: 100%;
  max-width: 380px;
  box-shadow: 0 25px 50px -12px rgba(0,0,0,0.25);
}
.login-card h1 { font-size: 1.5rem; margin-bottom: 0.25rem; }
.subtitle { font-size: 0.85rem; color: var(--muted); margin-bottom: 1.5rem; }
.form { display: flex; flex-direction: column; gap: 1rem; }
.field { display: flex; flex-direction: column; gap: 0.35rem; }
.field label { font-size: 0.9rem; font-weight: 500; }
.field input {
  padding: 0.6rem 0.75rem;
  border: 1px solid var(--card-border);
  border-radius: 8px;
  font-size: 1rem;
}
.field input:focus { outline: none; border-color: var(--sidebar-active); }
.error { color: #dc2626; font-size: 0.9rem; }
.success { color: #16a34a; font-size: 0.9rem; }
.btn {
  padding: 0.65rem 1rem;
  background: var(--sidebar-active);
  color: #fff;
  border: none;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  font-size: 1rem;
}
.btn:disabled { opacity: 0.7; cursor: not-allowed; }
.btn-secondary { margin-top: 1rem; background: #64748b; }
.hint { font-size: 0.8rem; color: var(--muted); margin-top: 1rem; }
</style>
