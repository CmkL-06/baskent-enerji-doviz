<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import { getCurrencyFlagImg, formatExchangeRate } from '@/utils/currency'

const exchangeStore = useExchangeStore()

const loading = ref(true)
const saving = ref(false)
const error = ref('')
const success = ref('')

const activeTab = ref<'rates' | 'add' | 'history'>('rates')

// Current rates
const rates = ref<any[]>([])
const rateHistory = ref<any[]>([])
const historyPair = ref<{ source: string; target: string } | null>(null)

// Add/Edit form
const editMode = ref(false)
const editingRate = ref<any>(null)
const form = ref({
  sourceCurrencyId: '',
  targetCurrencyId: '',
  buyRate: '',
  sellRate: '',
})

const officeId = computed(() =>
  exchangeStore.selectedOffice?.officeId ?? exchangeStore.offices[0]?.officeId
)

const currencies = computed(() =>
  exchangeStore.currencies.filter((c: any) => c.isActive !== false)
)

const nonTryCurrencies = computed(() =>
  currencies.value.filter((c: any) => c.currencyCode !== 'TRY')
)

const tryCurrency = computed(() =>
  currencies.value.find((c: any) => c.currencyCode === 'TRY')
)

const spreadPercent = computed(() => {
  const buy = parseFloat(form.value.buyRate.replace(',', '.'))
  const sell = parseFloat(form.value.sellRate.replace(',', '.'))
  if (!buy || !sell || buy <= 0) return null
  return ((sell - buy) / buy * 100).toFixed(2)
})

function getCurrencyCode(id: string): string {
  return currencies.value.find((c: any) => c.id === id)?.currencyCode ?? ''
}

function getCurrencyName(id: string): string {
  return currencies.value.find((c: any) => c.id === id)?.currencyName ?? ''
}

async function loadRates() {
  if (!officeId.value) return
  loading.value = true
  error.value = ''
  try {
    const data = await apiService.getExchangeRates(officeId.value)
    rates.value = Array.isArray(data) ? data : (data?.items ?? data?.data ?? [])
  } catch (e: any) {
    error.value = 'Kurlar yüklenemedi'
    console.error(e)
  } finally {
    loading.value = false
  }
}

async function loadHistory(sourceId: string, targetId: string) {
  if (!officeId.value) return
  try {
    const data = await apiService.getExchangeRateHistory(officeId.value, sourceId, targetId)
    rateHistory.value = Array.isArray(data) ? data : (data?.items ?? data?.data ?? [])
    historyPair.value = {
      source: getCurrencyCode(sourceId),
      target: getCurrencyCode(targetId),
    }
    activeTab.value = 'history'
  } catch (e: any) {
    console.error('History load failed', e)
  }
}

function startEdit(rate: any) {
  editMode.value = true
  editingRate.value = rate
  form.value = {
    sourceCurrencyId: rate.sourceCurrencyId,
    targetCurrencyId: rate.targetCurrencyId,
    buyRate: formatDecimal(rate.buyRate),
    sellRate: formatDecimal(rate.sellRate),
  }
  activeTab.value = 'add'
}

function startAdd() {
  editMode.value = false
  editingRate.value = null
  form.value = {
    sourceCurrencyId: nonTryCurrencies.value[0]?.id ?? '',
    targetCurrencyId: tryCurrency.value?.id ?? '',
    buyRate: '',
    sellRate: '',
  }
  activeTab.value = 'add'
}

function formatDecimal(val: number): string {
  if (!val && val !== 0) return ''
  return val.toLocaleString('tr-TR', { minimumFractionDigits: 4, maximumFractionDigits: 6 })
}

function parseNum(val: string): number {
  if (!val) return 0
  let s = String(val).trim()
  if (/^\d{1,3}(\.\d{3})+(,\d+)?$/.test(s)) {
    s = s.replace(/\./g, '').replace(',', '.')
  } else {
    s = s.replace(',', '.')
  }
  const n = parseFloat(s)
  return isNaN(n) ? 0 : n
}

async function saveRate() {
  if (!officeId.value) return
  const buyRate = parseNum(form.value.buyRate)
  const sellRate = parseNum(form.value.sellRate)

  if (!form.value.sourceCurrencyId || !form.value.targetCurrencyId) {
    error.value = 'Kaynak ve hedef para birimi seçiniz'
    return
  }
  if (form.value.sourceCurrencyId === form.value.targetCurrencyId) {
    error.value = 'Kaynak ve hedef para birimi aynı olamaz'
    return
  }
  if (buyRate <= 0 || sellRate <= 0) {
    error.value = 'Alış ve satış kuru 0\'dan büyük olmalıdır'
    return
  }
  if (sellRate < buyRate) {
    error.value = 'Satış kuru alış kurundan küçük olamaz'
    return
  }

  saving.value = true
  error.value = ''
  success.value = ''

  try {
    const payload = {
      officeId: officeId.value,
      sourceCurrencyId: form.value.sourceCurrencyId,
      targetCurrencyId: form.value.targetCurrencyId,
      buyRate,
      sellRate,
    }
    await apiService.createExchangeRate(payload)
    success.value = editMode.value ? 'Kur başarıyla güncellendi' : 'Yeni kur başarıyla eklendi'
    await loadRates()
    await exchangeStore.loadExchangeRates(officeId.value)
    setTimeout(() => { success.value = '' }, 3000)
    activeTab.value = 'rates'
  } catch (e: any) {
    error.value = e.response?.data?.error ?? 'Kur kaydedilemedi'
  } finally {
    saving.value = false
  }
}

function formatDateTime(d: string | null | undefined): string {
  if (!d) return '-'
  return new Date(d).toLocaleString('tr-TR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  })
}

watch(officeId, () => { if (officeId.value) loadRates() })

onMounted(async () => {
  if (!exchangeStore.currencies.length) await exchangeStore.initialize()
  await loadRates()
})
</script>

<template>
  <div class="mr-page">
    <!-- Top Bar -->
    <div class="mr-topbar">
      <div class="mr-topbar-left">
        <div class="mr-topbar-icon">
          <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 22px; color: #6366f1">tune</span>
        </div>
        <h1 class="mr-topbar-title">Manuel Kur Yönetimi</h1>
      </div>
      <div class="mr-topbar-right">
        <button class="mr-btn mr-btn--primary" @click="startAdd">
          <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 18px">add_circle</span>
          Yeni Kur Ekle
        </button>
        <button class="mr-btn mr-btn--ghost" @click="loadRates" :disabled="loading">
          <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 18px" :class="{ 'mr-spin': loading }">refresh</span>
        </button>
      </div>
    </div>

    <!-- Tabs -->
    <div class="mr-tabs">
      <button class="mr-tab" :class="{ 'mr-tab--active': activeTab === 'rates' }" @click="activeTab = 'rates'">
        <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 18px">monitoring</span>
        Mevcut Kurlar
      </button>
      <button class="mr-tab" :class="{ 'mr-tab--active': activeTab === 'add' }" @click="startAdd">
        <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 18px">edit_note</span>
        {{ editMode ? 'Kur Düzenle' : 'Kur Ekle' }}
      </button>
      <button class="mr-tab" :class="{ 'mr-tab--active': activeTab === 'history' }" @click="activeTab = 'history'" v-if="rateHistory.length">
        <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 18px">history</span>
        Geçmiş{{ historyPair ? ` (${historyPair.source}/${historyPair.target})` : '' }}
      </button>
    </div>

    <!-- Messages -->
    <div v-if="error" class="mr-alert mr-alert--error">
      <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 18px">error</span>
      {{ error }}
      <button class="mr-alert-close" @click="error = ''">×</button>
    </div>
    <div v-if="success" class="mr-alert mr-alert--success">
      <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 18px">check_circle</span>
      {{ success }}
    </div>

    <!-- Loading -->
    <div v-if="loading && activeTab === 'rates'" class="mr-loading">
      <span class="material-symbols-outlined mr-spin" style="font-size: 32px; color: #6366f1">progress_activity</span>
      <span>Kurlar yükleniyor...</span>
    </div>

    <!-- ═══ TAB: Mevcut Kurlar ═══ -->
    <div v-else-if="activeTab === 'rates'" class="mr-rates-grid">
      <div v-if="!rates.length" class="mr-empty">
        <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 0, 'wght' 300, 'GRAD' 0, 'opsz' 24; font-size: 48px; color: #cbd5e1">currency_exchange</span>
        <p>Henüz tanımlı kur bulunamadı</p>
        <button class="mr-btn mr-btn--primary" @click="startAdd">İlk Kuru Ekle</button>
      </div>

      <div v-for="rate in rates" :key="rate.id" class="mr-rate-card">
        <div class="mr-rate-header">
          <div class="mr-rate-pair">
            <img v-if="getCurrencyFlagImg(getCurrencyCode(rate.sourceCurrencyId))"
                 :src="getCurrencyFlagImg(getCurrencyCode(rate.sourceCurrencyId))"
                 class="mr-flag" />
            <span class="mr-rate-code">{{ getCurrencyCode(rate.sourceCurrencyId) }}</span>
            <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 16px; color: #94a3b8">arrow_forward</span>
            <img v-if="getCurrencyFlagImg(getCurrencyCode(rate.targetCurrencyId))"
                 :src="getCurrencyFlagImg(getCurrencyCode(rate.targetCurrencyId))"
                 class="mr-flag" />
            <span class="mr-rate-code">{{ getCurrencyCode(rate.targetCurrencyId) }}</span>
          </div>
          <div class="mr-rate-actions">
            <button class="mr-icon-btn" @click="loadHistory(rate.sourceCurrencyId, rate.targetCurrencyId)" title="Geçmiş">
              <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 18px">history</span>
            </button>
            <button class="mr-icon-btn mr-icon-btn--edit" @click="startEdit(rate)" title="Düzenle">
              <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 18px">edit</span>
            </button>
          </div>
        </div>
        <div class="mr-rate-body">
          <div class="mr-rate-col mr-rate-col--buy">
            <span class="mr-rate-label">Alış</span>
            <span class="mr-rate-value mr-rate-value--buy">{{ formatExchangeRate(rate.buyRate) }}</span>
          </div>
          <div class="mr-rate-divider"></div>
          <div class="mr-rate-col mr-rate-col--sell">
            <span class="mr-rate-label">Satış</span>
            <span class="mr-rate-value mr-rate-value--sell">{{ formatExchangeRate(rate.sellRate) }}</span>
          </div>
          <div class="mr-rate-divider"></div>
          <div class="mr-rate-col">
            <span class="mr-rate-label">Spread</span>
            <span class="mr-rate-value mr-rate-value--spread">
              {{ rate.buyRate > 0 ? ((rate.sellRate - rate.buyRate) / rate.buyRate * 100).toFixed(2) : '0' }}%
            </span>
          </div>
        </div>
        <div class="mr-rate-footer">
          <span class="mr-rate-date">
            <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 14px">schedule</span>
            {{ formatDateTime(rate.effectiveFrom) }}
          </span>
          <span class="mr-rate-badge" :class="rate.isActive ? 'mr-rate-badge--active' : 'mr-rate-badge--inactive'">
            {{ rate.isActive ? 'Aktif' : 'Pasif' }}
          </span>
        </div>
      </div>
    </div>

    <!-- ═══ TAB: Kur Ekle/Düzenle ═══ -->
    <div v-else-if="activeTab === 'add'" class="mr-form-wrap">
      <div class="mr-form-card">
        <div class="mr-form-header">
          <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 22px; color: #6366f1">{{ editMode ? 'edit_note' : 'add_circle' }}</span>
          <h2>{{ editMode ? 'Kur Düzenle' : 'Yeni Kur Ekle' }}</h2>
        </div>

        <form @submit.prevent="saveRate" class="mr-form">
          <!-- Currency selectors -->
          <div class="mr-form-row">
            <div class="mr-form-group">
              <label class="mr-label">Kaynak Para Birimi</label>
              <select v-model="form.sourceCurrencyId" class="mr-select" :disabled="editMode">
                <option value="" disabled>Seçiniz...</option>
                <option v-for="c in nonTryCurrencies" :key="c.id" :value="c.id">
                  {{ c.currencyCode }} — {{ c.currencyName }}
                </option>
              </select>
            </div>
            <div class="mr-form-arrow">
              <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 24px; color: #6366f1">swap_horiz</span>
            </div>
            <div class="mr-form-group">
              <label class="mr-label">Hedef Para Birimi</label>
              <select v-model="form.targetCurrencyId" class="mr-select" :disabled="editMode">
                <option value="" disabled>Seçiniz...</option>
                <option v-for="c in currencies" :key="c.id" :value="c.id">
                  {{ c.currencyCode }} — {{ c.currencyName }}
                </option>
              </select>
            </div>
          </div>

          <!-- Rate inputs -->
          <div class="mr-form-row">
            <div class="mr-form-group">
              <label class="mr-label">
                <span class="mr-label-dot mr-label-dot--green"></span>
                Alış Kuru
              </label>
              <input v-model="form.buyRate" type="text" class="mr-input mr-input--buy" placeholder="0,0000" inputmode="decimal" />
            </div>
            <div class="mr-form-group">
              <label class="mr-label">
                <span class="mr-label-dot mr-label-dot--red"></span>
                Satış Kuru
              </label>
              <input v-model="form.sellRate" type="text" class="mr-input mr-input--sell" placeholder="0,0000" inputmode="decimal" />
            </div>
          </div>

          <!-- Spread info -->
          <div v-if="spreadPercent !== null" class="mr-spread-info">
            <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 16px">info</span>
            Spread: <strong>%{{ spreadPercent }}</strong>
            <span v-if="parseFloat(spreadPercent) > 5" class="mr-spread-warn">
              (Yüksek spread — kontrol ediniz)
            </span>
          </div>

          <!-- Actions -->
          <div class="mr-form-actions">
            <button type="button" class="mr-btn mr-btn--ghost" @click="activeTab = 'rates'">İptal</button>
            <button type="submit" class="mr-btn mr-btn--primary" :disabled="saving">
              <span v-if="saving" class="material-symbols-outlined mr-spin" style="font-size: 18px">progress_activity</span>
              {{ saving ? 'Kaydediliyor...' : (editMode ? 'Güncelle' : 'Kaydet') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ═══ TAB: Geçmiş ═══ -->
    <div v-else-if="activeTab === 'history'" class="mr-history-wrap">
      <div class="mr-history-header">
        <h2 v-if="historyPair">
          <img v-if="getCurrencyFlagImg(historyPair.source)" :src="getCurrencyFlagImg(historyPair.source)" class="mr-flag" />
          {{ historyPair.source }} / {{ historyPair.target }} Kur Geçmişi
        </h2>
        <button class="mr-btn mr-btn--ghost" @click="activeTab = 'rates'">
          <span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24; font-size: 18px">arrow_back</span>
          Geri
        </button>
      </div>

      <div v-if="!rateHistory.length" class="mr-empty">
        <p>Bu çift için geçmiş kaydı bulunamadı</p>
      </div>

      <table v-else class="mr-table">
        <thead>
          <tr>
            <th>Tarih</th>
            <th>Alış</th>
            <th>Satış</th>
            <th>Spread</th>
            <th>Durum</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(h, i) in rateHistory" :key="i">
            <td>{{ formatDateTime(h.effectiveFrom) }}</td>
            <td class="mr-td-buy">{{ formatExchangeRate(h.buyRate) }}</td>
            <td class="mr-td-sell">{{ formatExchangeRate(h.sellRate) }}</td>
            <td>{{ h.buyRate > 0 ? ((h.sellRate - h.buyRate) / h.buyRate * 100).toFixed(2) : '0' }}%</td>
            <td>
              <span class="mr-rate-badge" :class="h.isActive ? 'mr-rate-badge--active' : 'mr-rate-badge--inactive'">
                {{ h.isActive ? 'Aktif' : 'Pasif' }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
/* ═══ Design Tokens ═══ */
.mr-page {
  --mr-radius: 16px;
  --mr-border: #e2e8f0;
  --mr-indigo: #6366f1;
  --mr-green: #16a34a;
  --mr-red: #dc2626;
  --mr-amber: #d97706;
  --mr-shadow-sm: 0 1px 3px rgba(0,0,0,0.04), 0 1px 2px rgba(0,0,0,0.06);
  --mr-shadow-md: 0 4px 16px rgba(0,0,0,0.06), 0 1px 3px rgba(0,0,0,0.04);
  --mr-shadow-lg: 0 8px 32px rgba(0,0,0,0.08), 0 2px 6px rgba(0,0,0,0.04);

  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 12px;
}

/* ═══ Top Bar ═══ */
.mr-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 16px 20px;
  background: linear-gradient(135deg, rgba(99,102,241,0.04), rgba(124,58,237,0.02));
  border: 1px solid var(--mr-border);
  border-radius: var(--mr-radius);
  margin-bottom: 16px;
}
.mr-topbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}
.mr-topbar-icon {
  width: 40px;
  height: 40px;
  border-radius: 12px;
  background: rgba(99,102,241,0.08);
  display: flex;
  align-items: center;
  justify-content: center;
}
.mr-topbar-title {
  font-size: 18px;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
}
.mr-topbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* ═══ Tabs ═══ */
.mr-tabs {
  display: flex;
  gap: 4px;
  padding: 4px;
  background: #f1f5f9;
  border-radius: 12px;
  margin-bottom: 20px;
  box-shadow: inset 0 1px 2px rgba(0,0,0,0.06);
}
.mr-tab {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 10px 16px;
  border: none;
  border-radius: 10px;
  background: transparent;
  color: #64748b;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}
.mr-tab:hover {
  color: #334155;
  background: rgba(255,255,255,0.6);
}
.mr-tab--active {
  background: white;
  color: var(--mr-indigo);
  box-shadow: var(--mr-shadow-sm), 0 0 0 1px rgba(99,102,241,0.08);
}

/* ═══ Buttons ═══ */
.mr-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 9px 18px;
  border: none;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}
.mr-btn--primary {
  background: var(--mr-indigo);
  color: white;
  box-shadow: 0 2px 8px rgba(99,102,241,0.25);
}
.mr-btn--primary:hover {
  background: #4f46e5;
  box-shadow: 0 4px 16px rgba(99,102,241,0.35);
  transform: translateY(-1px);
}
.mr-btn--primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}
.mr-btn--ghost {
  background: white;
  color: #475569;
  border: 1px solid var(--mr-border);
}
.mr-btn--ghost:hover {
  background: #f8fafc;
  border-color: #cbd5e1;
}

/* ═══ Alerts ═══ */
.mr-alert {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  border-radius: 12px;
  font-size: 13px;
  font-weight: 500;
  margin-bottom: 16px;
}
.mr-alert--error {
  background: #fef2f2;
  color: #991b1b;
  border: 1px solid #fecaca;
}
.mr-alert--success {
  background: #f0fdf4;
  color: #166534;
  border: 1px solid #bbf7d0;
}
.mr-alert-close {
  margin-left: auto;
  background: none;
  border: none;
  font-size: 18px;
  cursor: pointer;
  color: inherit;
  opacity: 0.6;
}

/* ═══ Loading ═══ */
.mr-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 64px 0;
  color: #94a3b8;
  font-size: 14px;
}

/* ═══ Empty ═══ */
.mr-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 64px 0;
  color: #94a3b8;
  font-size: 14px;
}

/* ═══ Rates Grid ═══ */
.mr-rates-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
  gap: 16px;
}

/* ═══ Rate Card ═══ */
.mr-rate-card {
  background: white;
  border: 1px solid var(--mr-border);
  border-radius: var(--mr-radius);
  box-shadow: var(--mr-shadow-sm);
  overflow: hidden;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}
.mr-rate-card:hover {
  box-shadow: var(--mr-shadow-md);
  transform: translateY(-2px);
  border-color: rgba(99,102,241,0.2);
}

.mr-rate-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px;
  background: linear-gradient(135deg, #fafbff, #f5f3ff);
  border-bottom: 1px solid var(--mr-border);
}
.mr-rate-pair {
  display: flex;
  align-items: center;
  gap: 6px;
}
.mr-rate-code {
  font-size: 14px;
  font-weight: 700;
  color: #1e293b;
}
.mr-rate-actions {
  display: flex;
  gap: 4px;
}

.mr-icon-btn {
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 8px;
  background: rgba(99,102,241,0.06);
  color: #64748b;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}
.mr-icon-btn:hover {
  background: rgba(99,102,241,0.12);
  color: var(--mr-indigo);
}
.mr-icon-btn--edit:hover {
  background: rgba(99,102,241,0.12);
  color: var(--mr-indigo);
}

.mr-rate-body {
  display: flex;
  align-items: center;
  padding: 16px;
  gap: 0;
}
.mr-rate-col {
  flex: 1;
  text-align: center;
}
.mr-rate-divider {
  width: 1px;
  height: 36px;
  background: var(--mr-border);
  margin: 0 8px;
}
.mr-rate-label {
  display: block;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: #94a3b8;
  margin-bottom: 4px;
}
.mr-rate-value {
  font-size: 17px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}
.mr-rate-value--buy { color: var(--mr-green); }
.mr-rate-value--sell { color: var(--mr-red); }
.mr-rate-value--spread { color: var(--mr-amber); font-size: 14px; }

.mr-rate-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 16px;
  background: #fafbfc;
  border-top: 1px solid var(--mr-border);
}
.mr-rate-date {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: #94a3b8;
}
.mr-rate-badge {
  font-size: 11px;
  font-weight: 600;
  padding: 2px 10px;
  border-radius: 20px;
}
.mr-rate-badge--active {
  background: #ecfdf5;
  color: #059669;
  border: 1px solid #a7f3d0;
}
.mr-rate-badge--inactive {
  background: #fef2f2;
  color: #dc2626;
  border: 1px solid #fecaca;
}

.mr-flag {
  width: 20px;
  height: 14px;
  border-radius: 2px;
  object-fit: cover;
}

/* ═══ Form ═══ */
.mr-form-wrap {
  display: flex;
  justify-content: center;
}
.mr-form-card {
  width: 100%;
  max-width: 640px;
  background: white;
  border: 1px solid var(--mr-border);
  border-radius: var(--mr-radius);
  box-shadow: var(--mr-shadow-md);
  overflow: hidden;
}
.mr-form-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 18px 24px;
  background: linear-gradient(135deg, #fafbff, #f5f3ff);
  border-bottom: 1px solid var(--mr-border);
}
.mr-form-header h2 {
  font-size: 16px;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
}
.mr-form {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
}
.mr-form-row {
  display: flex;
  gap: 16px;
  align-items: flex-end;
}
.mr-form-arrow {
  padding-bottom: 8px;
}
.mr-form-group {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.mr-label {
  font-size: 12px;
  font-weight: 600;
  color: #475569;
  display: flex;
  align-items: center;
  gap: 6px;
}
.mr-label-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}
.mr-label-dot--green { background: var(--mr-green); }
.mr-label-dot--red { background: var(--mr-red); }

.mr-select, .mr-input {
  padding: 10px 14px;
  border: 1.5px solid var(--mr-border);
  border-radius: 10px;
  font-size: 14px;
  font-weight: 500;
  color: #1e293b;
  background: white;
  transition: all 0.2s;
  outline: none;
  font-family: inherit;
}
.mr-select:focus, .mr-input:focus {
  border-color: var(--mr-indigo);
  box-shadow: 0 0 0 3px rgba(99,102,241,0.1);
}
.mr-select:disabled {
  background: #f8fafc;
  color: #94a3b8;
}
.mr-input--buy:focus {
  border-color: var(--mr-green);
  box-shadow: 0 0 0 3px rgba(22,163,74,0.1);
}
.mr-input--sell:focus {
  border-color: var(--mr-red);
  box-shadow: 0 0 0 3px rgba(220,38,38,0.1);
}

.mr-spread-info {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 14px;
  background: #fffbeb;
  border: 1px solid #fde68a;
  border-radius: 10px;
  font-size: 13px;
  color: #92400e;
}
.mr-spread-warn {
  color: var(--mr-red);
  font-weight: 600;
}

.mr-form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  padding-top: 8px;
  border-top: 1px solid var(--mr-border);
}

/* ═══ History ═══ */
.mr-history-wrap {
  background: white;
  border: 1px solid var(--mr-border);
  border-radius: var(--mr-radius);
  box-shadow: var(--mr-shadow-sm);
  overflow: hidden;
}
.mr-history-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  background: linear-gradient(135deg, #fafbff, #f5f3ff);
  border-bottom: 1px solid var(--mr-border);
}
.mr-history-header h2 {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
}

.mr-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
}
.mr-table th {
  text-align: left;
  padding: 12px 16px;
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: #64748b;
  background: #f8fafc;
  border-bottom: 1px solid var(--mr-border);
}
.mr-table td {
  padding: 12px 16px;
  font-size: 13px;
  color: #334155;
  border-bottom: 1px solid #f1f5f9;
  font-variant-numeric: tabular-nums;
}
.mr-table tr:hover td {
  background: rgba(99,102,241,0.02);
}
.mr-td-buy { color: var(--mr-green); font-weight: 600; }
.mr-td-sell { color: var(--mr-red); font-weight: 600; }

/* ═══ Spin ═══ */
.mr-spin { animation: mr-rotate 1s linear infinite; }
@keyframes mr-rotate { to { transform: rotate(360deg); } }

/* ═══ Responsive ═══ */
@media (max-width: 640px) {
  .mr-rates-grid { grid-template-columns: 1fr; }
  .mr-form-row { flex-direction: column; gap: 12px; }
  .mr-form-arrow { display: none; }
  .mr-topbar { flex-direction: column; align-items: flex-start; gap: 10px; }
}
</style>
