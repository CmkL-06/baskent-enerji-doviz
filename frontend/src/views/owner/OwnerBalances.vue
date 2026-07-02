<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import apiService from '@/services/apiservice'

const offices = ref<any[]>([])
const vaults = ref<any[]>([])
const currencies = ref<any[]>([])
const loading = ref(true)
const saving = ref(false)
const success = ref('')
const error = ref('')

const selectedOfficeId = ref('')
const selectedVaultId = ref('')

const form = ref({
  currencyId: '',
  amount: null as number | null,
  operationType: 'add',
  description: '',
})

onMounted(async () => {
  loading.value = true
  try {
    const [o, c] = await Promise.all([
      apiService.getOfficeSummaries(),
      apiService.getCurrencies().catch(() => []),
    ])
    offices.value = Array.isArray(o) ? o : []
    currencies.value = Array.isArray(c) ? c : (c?.data ?? [])
  } catch (e: any) {
    error.value = e?.message || 'Veri yüklenemedi'
  } finally {
    loading.value = false
  }
})

watch(selectedOfficeId, async (id) => {
  vaults.value = []
  selectedVaultId.value = ''
  if (!id) return
  try {
    const v = await apiService.getVaultsByOfficeId(id)
    vaults.value = Array.isArray(v) ? v : (v?.data ?? [])
  } catch { vaults.value = [] }
})

const selectedVault = computed(() => vaults.value.find((v: any) => (v.id || v.vaultId) === selectedVaultId.value))

const vaultBalances = computed(() => {
  const v = selectedVault.value
  if (!v) return []
  const bals = v.balances || v.vaultBalances || []
  return bals.map((b: any) => ({
    currencyCode: b.currencyCode || b.currency?.currencyCode || '?',
    balance: b.balance ?? b.amount ?? 0,
  }))
})

async function submit() {
  if (!selectedVaultId.value || !form.value.currencyId || !form.value.amount) return
  saving.value = true; error.value = ''; success.value = ''
  try {
    const amount = form.value.operationType === 'subtract' ? -Math.abs(form.value.amount) : Math.abs(form.value.amount)
    await apiService.updateVaultBalance({
      vaultId: selectedVaultId.value,
      currencyId: form.value.currencyId,
      amount,
      description: form.value.description || (form.value.operationType === 'add' ? 'Manuel ekleme' : 'Manuel çıkarma'),
    })
    success.value = `${form.value.operationType === 'add' ? 'Ekleme' : 'Çıkarma'} başarılı!`
    form.value = { currencyId: '', amount: null, operationType: 'add', description: '' }
    // Refresh vault balances
    const v = await apiService.getVaultsByOfficeId(selectedOfficeId.value)
    vaults.value = Array.isArray(v) ? v : (v?.data ?? [])
    setTimeout(() => success.value = '', 3000)
  } catch (e: any) {
    error.value = e?.response?.data?.error || e?.message || 'İşlem başarısız'
  } finally {
    saving.value = false
  }
}

function fmtMoney(n: number) {
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
</script>

<template>
  <div>
    <h3 class="ob-title"><span class="material-symbols-outlined">account_balance</span> Bakiye İşlemleri</h3>

    <div v-if="loading" class="ob-center">
      <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
    </div>

    <template v-else>
      <div class="ob-form-card">
        <!-- Step 1: Office -->
        <div class="ob-step">
          <div class="ob-step-num">1</div>
          <div class="ob-step-content">
            <label>Ofis Seçin</label>
            <select v-model="selectedOfficeId" class="ob-input">
              <option value="">Ofis seçin...</option>
              <option v-for="o in offices" :key="o.officeId" :value="o.officeId">
                {{ o.officeName }}
              </option>
            </select>
          </div>
        </div>

        <!-- Step 2: Vault -->
        <div class="ob-step" :class="{ disabled: !selectedOfficeId }">
          <div class="ob-step-num">2</div>
          <div class="ob-step-content">
            <label>Kasa Seçin</label>
            <select v-model="selectedVaultId" class="ob-input" :disabled="!selectedOfficeId">
              <option value="">Kasa seçin...</option>
              <option v-for="v in vaults" :key="v.id || v.vaultId" :value="v.id || v.vaultId">
                {{ v.name || v.vaultName }}
              </option>
            </select>

            <div v-if="selectedVaultId && vaultBalances.length" class="ob-current-bals">
              <div class="ob-bals-title">Mevcut Bakiyeler</div>
              <div class="ob-bals-grid">
                <div v-for="b in vaultBalances" :key="b.currencyCode" class="ob-bal-chip">
                  <span class="ob-bal-cur">{{ b.currencyCode }}</span>
                  <span class="ob-bal-amt">{{ fmtMoney(b.balance) }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Step 3: Operation -->
        <div class="ob-step" :class="{ disabled: !selectedVaultId }">
          <div class="ob-step-num">3</div>
          <div class="ob-step-content">
            <label>İşlem Detayları</label>

            <div class="ob-op-toggle">
              <button :class="{ active: form.operationType === 'add' }" @click="form.operationType = 'add'">
                <span class="material-symbols-outlined">add_circle</span> Ekle
              </button>
              <button :class="{ active: form.operationType === 'subtract', sub: true }" @click="form.operationType = 'subtract'">
                <span class="material-symbols-outlined">remove_circle</span> Çıkar
              </button>
            </div>

            <div class="ob-field-row">
              <div class="ob-field">
                <label>Para Birimi</label>
                <select v-model="form.currencyId" class="ob-input" :disabled="!selectedVaultId">
                  <option value="">Seçin...</option>
                  <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.currencyCode }}</option>
                </select>
              </div>
              <div class="ob-field">
                <label>Tutar</label>
                <input v-model.number="form.amount" type="number" step="0.01" class="ob-input" placeholder="0.00" :disabled="!selectedVaultId" />
              </div>
            </div>

            <div class="ob-field">
              <label>Açıklama</label>
              <input v-model="form.description" class="ob-input" placeholder="İşlem açıklaması (opsiyonel)" :disabled="!selectedVaultId" />
            </div>

            <div v-if="success" class="ob-alert success">
              <span class="material-symbols-outlined">check_circle</span> {{ success }}
            </div>
            <div v-if="error" class="ob-alert error">
              <span class="material-symbols-outlined">error</span> {{ error }}
            </div>

            <button class="ob-submit" :disabled="saving || !selectedVaultId || !form.currencyId || !form.amount" @click="submit">
              <span class="material-symbols-outlined">{{ saving ? 'progress_activity' : (form.operationType === 'add' ? 'add_circle' : 'remove_circle') }}</span>
              {{ saving ? 'İşleniyor...' : (form.operationType === 'add' ? 'Bakiye Ekle' : 'Bakiye Çıkar') }}
            </button>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.ob-title { display: flex; align-items: center; gap: 0.5rem; font-size: 1.1rem; font-weight: 700; color: #1e293b; margin: 0 0 1.25rem; }
.ob-center { display: flex; align-items: center; gap: 0.75rem; padding: 3rem; justify-content: center; color: #94a3b8; }
@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin 1s linear infinite; }

.ob-form-card { background: white; border-radius: 16px; padding: 1.5rem; box-shadow: 0 1px 4px rgba(0,0,0,.08); max-width: 640px; }

.ob-step {
  display: flex; gap: 1rem; padding: 1.25rem 0;
  border-bottom: 1px solid #f1f5f9; transition: opacity .2s;
}
.ob-step:last-child { border-bottom: none; }
.ob-step.disabled { opacity: 0.4; pointer-events: none; }
.ob-step-num {
  width: 28px; height: 28px; border-radius: 50%; background: #3b82f6;
  color: white; display: flex; align-items: center; justify-content: center;
  font-size: 0.8rem; font-weight: 700; flex-shrink: 0; margin-top: 0.15rem;
}
.ob-step-content { flex: 1; }
.ob-step-content > label { font-size: 0.85rem; font-weight: 600; color: #475569; margin-bottom: 0.5rem; display: block; }

.ob-input {
  width: 100%; padding: 0.6rem 0.75rem; border: 2px solid #e2e8f0;
  border-radius: 8px; font-size: 0.9rem; color: #1e293b;
  background: #f8fafc; outline: none; box-sizing: border-box;
  transition: border-color .2s;
}
.ob-input:focus { border-color: #3b82f6; background: white; }

.ob-current-bals { margin-top: 0.75rem; padding: 0.75rem; background: #f8fafc; border-radius: 10px; }
.ob-bals-title { font-size: 0.75rem; font-weight: 600; color: #64748b; margin-bottom: 0.4rem; }
.ob-bals-grid { display: flex; flex-wrap: wrap; gap: 0.4rem; }
.ob-bal-chip {
  display: flex; gap: 0.5rem; padding: 0.3rem 0.6rem; background: white;
  border-radius: 6px; font-size: 0.8rem; border: 1px solid #e2e8f0;
}
.ob-bal-cur { font-weight: 600; color: #3b82f6; }
.ob-bal-amt { font-family: monospace; color: #1e293b; }

.ob-op-toggle { display: flex; gap: 0.5rem; margin-bottom: 1rem; }
.ob-op-toggle button {
  display: flex; align-items: center; gap: 0.3rem; flex: 1;
  padding: 0.6rem; border: 2px solid #e2e8f0; border-radius: 10px;
  background: white; cursor: pointer; font-size: 0.85rem; font-weight: 600;
  color: #64748b; transition: all .2s;
}
.ob-op-toggle button.active { border-color: #22c55e; color: #16a34a; background: #f0fdf4; }
.ob-op-toggle button.sub.active { border-color: #ef4444; color: #dc2626; background: #fef2f2; }

.ob-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1rem; }
.ob-field { margin-bottom: 0.75rem; }
.ob-field label { display: block; font-size: 0.75rem; font-weight: 600; color: #64748b; margin-bottom: 0.3rem; }

.ob-alert {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.6rem 1rem; border-radius: 8px; font-size: 0.85rem; margin-bottom: 0.75rem;
}
.ob-alert.success { background: #f0fdf4; color: #16a34a; }
.ob-alert.error { background: #fef2f2; color: #ef4444; }

.ob-submit {
  display: flex; align-items: center; justify-content: center; gap: 0.4rem;
  width: 100%; padding: 0.75rem; background: #3b82f6; color: white;
  border: none; border-radius: 10px; cursor: pointer; font-size: 0.9rem;
  font-weight: 600; transition: all .2s;
}
.ob-submit:hover:not(:disabled) { background: #2563eb; }
.ob-submit:disabled { opacity: 0.5; cursor: not-allowed; }
</style>
