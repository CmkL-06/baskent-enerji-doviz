<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import { getCurrencyFlagImg } from '@/utils/currency'
import AppKpiCard from '@/components/common/AppKpiCard.vue'
import AppPageHeader from '@/components/common/AppPageHeader.vue'
import AppEmptyState from '@/components/common/AppEmptyState.vue'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()

const loading = ref(true)
const saving = ref(false)
const error = ref('')

// Data
const settings = ref<any>(null)
const pendingApprovals = ref<any[]>([])
const rateHistory = ref<any[]>([])
const externalRates = ref<any[]>([])
const externalSources = ref<any[]>([])

// Tab
const activeTab = ref<'settings' | 'pending' | 'external' | 'history'>('settings')

// Settings form
const settingsForm = ref({
  isAutoUpdateEnabled: false,
  updateIntervalMinutes: 30,
  startHour: 8,
  endHour: 22,
  workDays: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'] as string[],
  tryBasedMarginPercent: 0,
  crossFiatMarginPercent: 0,
  cryptoMarginPercent: 0,
  useTcmb: true,
  useDovizCom: true,
  useBinance: false,
  rateSelectionStrategy: 'AVERAGE',
  maxPriceChangePercent: 5,
  requireApprovalAboveThreshold: true,
  notificationEmails: '',
  sendMobileNotifications: false,
})

const STRATEGIES: Record<string, string> = {
  BEST_BUY: 'En İyi Alış',
  AVERAGE: 'Ortalama',
  COMPETITIVE: 'Rekabetçi',
}

const DAYS: { key: string; label: string }[] = [
  { key: 'Monday', label: 'Pzt' },
  { key: 'Tuesday', label: 'Sal' },
  { key: 'Wednesday', label: 'Çar' },
  { key: 'Thursday', label: 'Per' },
  { key: 'Friday', label: 'Cum' },
  { key: 'Saturday', label: 'Cmt' },
  { key: 'Sunday', label: 'Paz' },
]

const officeId = computed(() =>
  exchangeStore.selectedOffice?.officeId ?? exchangeStore.offices[0]?.officeId
)

const formatCurrency = (amount: number): string =>
  new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 4, maximumFractionDigits: 4 }).format(amount)

const formatDateTime = (d: string | null | undefined): string => {
  if (!d) return '-'
  return new Date(d).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

async function loadSettings() {
  try {
    const data = await apiService.getAutoRateSettings()
    settings.value = data
    settingsForm.value = {
      isAutoUpdateEnabled: data.isAutoUpdateEnabled ?? false,
      updateIntervalMinutes: data.updateIntervalMinutes ?? 30,
      startHour: data.startHour ?? 8,
      endHour: data.endHour ?? 22,
      workDays: data.workDays ?? ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'],
      tryBasedMarginPercent: data.tryBasedMarginPercent ?? 0,
      crossFiatMarginPercent: data.crossFiatMarginPercent ?? 0,
      cryptoMarginPercent: data.cryptoMarginPercent ?? 0,
      useTcmb: data.useTcmb ?? true,
      useDovizCom: data.useDovizCom ?? true,
      useBinance: data.useBinance ?? false,
      rateSelectionStrategy: data.rateSelectionStrategy ?? 'AVERAGE',
      maxPriceChangePercent: data.maxPriceChangePercent ?? 5,
      requireApprovalAboveThreshold: data.requireApprovalAboveThreshold ?? true,
      notificationEmails: Array.isArray(data.notificationEmails) ? data.notificationEmails.join(', ') : (data.notificationEmails ?? ''),
      sendMobileNotifications: data.sendMobileNotifications ?? false,
    }
  } catch (e: any) {
    console.error('Settings load failed', e)
  }
}

async function saveSettings() {
  saving.value = true
  try {
    const emails = settingsForm.value.notificationEmails
      .split(',')
      .map(e => e.trim())
      .filter(Boolean)
    await apiService.updateAutoRateSettings({
      ...settingsForm.value,
      notificationEmails: emails,
    })
    notification.success('Ayarlar kaydedildi')
    await loadSettings()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Ayarlar kaydedilemedi')
  } finally {
    saving.value = false
  }
}

async function loadPending() {
  try {
    const data = await apiService.getPendingApprovals()
    pendingApprovals.value = Array.isArray(data) ? data : (data?.items ?? [])
  } catch {
    pendingApprovals.value = []
  }
}

async function handleApproval(item: any, isApproved: boolean) {
  const notes = isApproved ? '' : (prompt('Red sebebi:') ?? '')
  if (!isApproved && notes === '') return
  try {
    await apiService.approveOrRejectRate(item.id, {
      ApprovalId: item.id,
      IsApproved: isApproved,
      Notes: notes,
      ApprovedBy: authStore.user?.id,
    })
    await loadPending()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'İşlem başarısız')
  }
}

async function loadExternalRates() {
  try {
    const [rates, sources] = await Promise.all([
      apiService.getExternalRates(60),
      apiService.getExternalSources(),
    ])
    externalRates.value = Array.isArray(rates) ? rates : (rates?.items ?? [])
    externalSources.value = Array.isArray(sources) ? sources : (sources?.items ?? [])
  } catch {
    externalRates.value = []
  }
}

async function loadHistory() {
  try {
    const data = await apiService.getRateHistory()
    rateHistory.value = Array.isArray(data) ? data : (data?.data ?? data?.items ?? [])
  } catch {
    rateHistory.value = []
  }
}

async function triggerUpdate(testMode: boolean) {
  if (!testMode && !confirm('Kurları şimdi güncellemek istediğinizden emin misiniz?')) return
  try {
    await apiService.triggerAutoUpdate({
      OfficeId: officeId.value ?? null,
      ForceUpdate: !testMode,
      TriggeredBy: testMode ? 'TEST' : 'USER',
      TestMode: testMode,
    })
    notification.success(testMode ? 'Test çalıştırıldı — kurlar uygulanmadı' : 'Kurlar güncellendi')
    if (!testMode) await loadPending()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Güncelleme başarısız')
  }
}

function toggleDay(day: string) {
  const idx = settingsForm.value.workDays.indexOf(day)
  if (idx >= 0) settingsForm.value.workDays.splice(idx, 1)
  else settingsForm.value.workDays.push(day)
}

const selectedSource = ref<string | null>(null)

const ratesBySource = computed(() => {
  const groups: Record<string, any[]> = {}
  for (const r of externalRates.value) {
    const key = r.source ?? 'Bilinmeyen'
    if (!groups[key]) groups[key] = []
    groups[key].push(r)
  }
  return groups
})

const filteredRatesBySource = computed(() => {
  if (!selectedSource.value) return ratesBySource.value
  const key = selectedSource.value
  if (ratesBySource.value[key]) return { [key]: ratesBySource.value[key] }
  return {}
})

function toggleSource(sourceKey: string) {
  selectedSource.value = selectedSource.value === sourceKey ? null : sourceKey
}

onMounted(async () => {
  loading.value = true
  try {
    await Promise.all([loadSettings(), loadPending(), loadExternalRates(), loadHistory()])
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="ar-wrap">
    <!-- Header -->
    <AppPageHeader icon="auto_fix_high" title="Otomatik Kur Yönetimi">
      <button class="btn-secondary" @click="triggerUpdate(true)">
        <span class="material-symbols-outlined" aria-hidden="true">science</span>
        Test Et
      </button>
      <button v-if="authStore.isAdmin" class="btn-primary" @click="triggerUpdate(false)">
        <span class="material-symbols-outlined" aria-hidden="true">sync</span>
        Şimdi Güncelle
      </button>
    </AppPageHeader>

    <!-- KPI -->
    <div class="kpi-grid">
      <AppKpiCard icon="toggle_on" label="Otomatik Güncelleme" :value="settingsForm.isAutoUpdateEnabled ? 'Açık' : 'Kapalı'" :color="settingsForm.isAutoUpdateEnabled ? '#059669' : '#ef4444'" :bg="settingsForm.isAutoUpdateEnabled ? '#ecfdf5' : '#fef2f2'" />
      <AppKpiCard icon="pending_actions" label="Onay Bekleyen" :value="pendingApprovals.length" color="#d97706" bg="#fffbeb" />
      <AppKpiCard icon="public" label="Dış Kaynak" :value="externalRates.length + ' kur'" color="#6366f1" bg="#eef2ff" />
      <AppKpiCard icon="schedule" label="Güncelleme Aralığı" :value="settingsForm.updateIntervalMinutes + ' dk'" color="#6366f1" bg="#eef2ff" />
    </div>

    <!-- Tabs -->
    <div class="tab-bar">
      <button :class="['tab-btn', activeTab === 'settings' ? 'tab-active' : '']" @click="activeTab = 'settings'">
        <span class="material-symbols-outlined" aria-hidden="true">settings</span> Ayarlar
      </button>
      <button :class="['tab-btn', activeTab === 'pending' ? 'tab-active' : '']" @click="activeTab = 'pending'; loadPending()">
        <span class="material-symbols-outlined" aria-hidden="true">pending_actions</span> Onay Bekleyenler
        <span v-if="pendingApprovals.length" class="badge">{{ pendingApprovals.length }}</span>
      </button>
      <button :class="['tab-btn', activeTab === 'external' ? 'tab-active' : '']" @click="activeTab = 'external'; loadExternalRates()">
        <span class="material-symbols-outlined" aria-hidden="true">public</span> Dış Kurlar
      </button>
      <button :class="['tab-btn', activeTab === 'history' ? 'tab-active' : '']" @click="activeTab = 'history'; loadHistory()">
        <span class="material-symbols-outlined" aria-hidden="true">history</span> Geçmiş
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="loading-state">
      <span class="material-symbols-outlined spin">progress_activity</span>
      Yükleniyor...
    </div>

    <!-- Settings Tab -->
    <div v-else-if="activeTab === 'settings'" class="settings-panel">
      <div class="settings-section">
        <h3>Genel</h3>
        <div class="form-grid-2">
          <div class="form-group checkbox-group">
            <label>
              <input type="checkbox" v-model="settingsForm.isAutoUpdateEnabled" />
              Otomatik güncelleme aktif
            </label>
          </div>
          <div class="form-group">
            <label>Güncelleme aralığı (dk)</label>
            <input v-model.number="settingsForm.updateIntervalMinutes" type="number" min="5" max="1440" />
          </div>
          <div class="form-group">
            <label>Başlangıç saati</label>
            <input v-model.number="settingsForm.startHour" type="number" min="0" max="23" />
          </div>
          <div class="form-group">
            <label>Bitiş saati</label>
            <input v-model.number="settingsForm.endHour" type="number" min="0" max="23" />
          </div>
        </div>

        <div class="day-selector">
          <label>Çalışma günleri</label>
          <div class="day-chips">
            <button v-for="day in DAYS" :key="day.key"
                    :class="['chip', settingsForm.workDays.includes(day.key) ? 'chip-active' : '']"
                    @click="toggleDay(day.key)">
              {{ day.label }}
            </button>
          </div>
        </div>
      </div>

      <div class="settings-section">
        <h3>Kaynaklar</h3>
        <div class="form-grid-3">
          <div class="form-group checkbox-group">
            <label><input type="checkbox" v-model="settingsForm.useTcmb" /> TCMB</label>
          </div>
          <div class="form-group checkbox-group">
            <label><input type="checkbox" v-model="settingsForm.useDovizCom" /> Doviz.com</label>
          </div>
          <div class="form-group checkbox-group">
            <label><input type="checkbox" v-model="settingsForm.useBinance" /> Binance</label>
          </div>
        </div>
        <div class="form-group" style="max-width: 300px;">
          <label>Kur seçim stratejisi</label>
          <select v-model="settingsForm.rateSelectionStrategy">
            <option v-for="(label, key) in STRATEGIES" :key="key" :value="key">{{ label }}</option>
          </select>
        </div>
      </div>

      <div class="settings-section">
        <h3>Marjlar (%)</h3>
        <div class="form-grid-3">
          <div class="form-group">
            <label>TRY bazlı</label>
            <input v-model.number="settingsForm.tryBasedMarginPercent" type="number" step="0.01" min="0" />
          </div>
          <div class="form-group">
            <label>Çapraz fiat</label>
            <input v-model.number="settingsForm.crossFiatMarginPercent" type="number" step="0.01" min="0" />
          </div>
          <div class="form-group">
            <label>Kripto</label>
            <input v-model.number="settingsForm.cryptoMarginPercent" type="number" step="0.01" min="0" />
          </div>
        </div>
      </div>

      <div class="settings-section">
        <h3>Güvenlik</h3>
        <div class="form-grid-2">
          <div class="form-group">
            <label>Maks. fiyat değişim eşiği (%)</label>
            <input v-model.number="settingsForm.maxPriceChangePercent" type="number" step="0.1" min="0" />
          </div>
          <div class="form-group checkbox-group">
            <label>
              <input type="checkbox" v-model="settingsForm.requireApprovalAboveThreshold" />
              Eşik üzeri onay gerektir
            </label>
          </div>
        </div>
      </div>

      <div class="settings-section">
        <h3>Bildirimler</h3>
        <div class="form-grid-2">
          <div class="form-group">
            <label>Bildirim e-postaları (virgülle ayırın)</label>
            <input v-model="settingsForm.notificationEmails" placeholder="admin@example.com, ..." />
          </div>
          <div class="form-group checkbox-group">
            <label>
              <input type="checkbox" v-model="settingsForm.sendMobileNotifications" />
              Mobil bildirim gönder
            </label>
          </div>
        </div>
      </div>

      <div class="settings-footer">
        <button class="btn-primary" :disabled="saving" @click="saveSettings">
          {{ saving ? 'Kaydediliyor...' : 'Ayarları Kaydet' }}
        </button>
      </div>
    </div>

    <!-- Pending Approvals Tab -->
    <div v-else-if="activeTab === 'pending'" class="table-wrap">
      <table class="ar-table">
        <thead>
          <tr>
            <th>Ofis</th>
            <th>Döviz Çifti</th>
            <th class="text-right">Mevcut Alış</th>
            <th class="text-right">Mevcut Satış</th>
            <th class="text-right">Önerilen Alış</th>
            <th class="text-right">Önerilen Satış</th>
            <th class="text-right">Değişim %</th>
            <th>Sebep</th>
            <th>Tarih</th>
            <th class="text-center">İşlem</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="pendingApprovals.length === 0">
            <td colspan="10" class="empty-row">Onay bekleyen kur yok</td>
          </tr>
          <tr v-for="item in pendingApprovals" :key="item.id">
            <td>{{ item.officeName || '-' }}</td>
            <td class="ar-pair-cell">
              <img v-if="getCurrencyFlagImg(item.currencyPair?.split('/')[0])" :src="getCurrencyFlagImg(item.currencyPair?.split('/')[0])" class="ar-flag" />
              <strong>{{ item.currencyPair }}</strong>
            </td>
            <td class="text-right font-mono">{{ formatCurrency(item.currentBuyRate ?? 0) }}</td>
            <td class="text-right font-mono">{{ formatCurrency(item.currentSellRate ?? 0) }}</td>
            <td class="text-right font-mono proposed">{{ formatCurrency(item.proposedBuyRate ?? 0) }}</td>
            <td class="text-right font-mono proposed">{{ formatCurrency(item.proposedSellRate ?? 0) }}</td>
            <td class="text-right" :class="Math.abs(item.changePercent ?? 0) > 3 ? 'change-high' : 'change-normal'">
              {{ (item.changePercent ?? 0).toFixed(2) }}%
            </td>
            <td>{{ item.reason || '-' }}</td>
            <td>{{ formatDateTime(item.createdAt) }}</td>
            <td class="text-center actions-cell">
              <button class="btn-approve" @click="handleApproval(item, true)" title="Onayla">
                <span class="material-symbols-outlined" aria-hidden="true">check_circle</span>
              </button>
              <button class="btn-reject" @click="handleApproval(item, false)" title="Reddet">
                <span class="material-symbols-outlined" aria-hidden="true">cancel</span>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- External Rates Tab -->
    <div v-else-if="activeTab === 'external'" class="external-section">
      <div class="ext-header">
        <button class="btn-secondary" @click="loadExternalRates">
          <span class="material-symbols-outlined" aria-hidden="true">refresh</span>
          Yenile
        </button>
      </div>

      <!-- Sources -->
      <div v-if="externalSources.length" class="sources-bar">
        <button v-for="src in externalSources" :key="src.sourceKey"
                class="source-chip"
                :class="{ 'source-selected': selectedSource === src.sourceKey, 'source-dimmed': selectedSource && selectedSource !== src.sourceKey }"
                @click="toggleSource(src.sourceKey)">
          <span class="material-symbols-outlined" aria-hidden="true" style="font-size:14px">{{ selectedSource === src.sourceKey ? 'check_circle' : 'radio_button_unchecked' }}</span>
          {{ src.sourceName }}
          <small>{{ src.sourceType }}</small>
        </button>
      </div>

      <!-- Rates by source -->
      <div v-for="(rates, source) in filteredRatesBySource" :key="source" class="source-group">
        <h4>{{ source }}</h4>
        <table class="ar-table ar-ext-table">
          <thead>
            <tr>
              <th class="col-pair">Döviz</th>
              <th class="col-rate">Alış</th>
              <th class="col-rate">Satış</th>
              <th class="col-spread">Spread %</th>
              <th class="col-date">Güncelleme</th>
              <th class="col-status">Durum</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="r in rates" :key="r.currencyCode">
              <td class="ar-pair-cell">
                <img v-if="getCurrencyFlagImg(r.currencyCode)" :src="getCurrencyFlagImg(r.currencyCode)" class="ar-flag" />
                <strong>{{ r.currencyCode }}</strong>/{{ r.targetCurrencyCode || 'TRY' }}
              </td>
              <td class="col-rate font-mono">{{ formatCurrency(r.buyRate ?? 0) }}</td>
              <td class="col-rate font-mono">{{ formatCurrency(r.sellRate ?? 0) }}</td>
              <td class="text-right">{{ (r.spreadPercent ?? 0).toFixed(2) }}%</td>
              <td>{{ formatDateTime(r.fetchedAt) }}</td>
              <td>
                <span v-if="r.isValid" class="status-ok">Geçerli</span>
                <span v-else class="status-err" :title="r.errorMessage">Hata</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <AppEmptyState v-if="externalRates.length === 0" icon="public" message="Dış kaynak verisi bulunamadı" />
    </div>

    <!-- History Tab -->
    <div v-else-if="activeTab === 'history'" class="table-wrap">
      <table class="ar-table">
        <thead>
          <tr>
            <th>Tarih</th>
            <th>Ofis</th>
            <th>Döviz Çifti</th>
            <th class="text-right">Eski Alış</th>
            <th class="text-right">Yeni Alış</th>
            <th class="text-right">Eski Satış</th>
            <th class="text-right">Yeni Satış</th>
            <th class="text-right">Değişim %</th>
            <th>Kaynak</th>
            <th>Kullanıcı</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="rateHistory.length === 0">
            <td colspan="10" class="empty-row">Geçmiş kaydı bulunamadı</td>
          </tr>
          <tr v-for="h in rateHistory" :key="h.id">
            <td>{{ formatDateTime(h.createdAt) }}</td>
            <td>{{ h.officeName || '-' }}</td>
            <td class="ar-pair-cell">
              <img v-if="getCurrencyFlagImg(h.currencyPair?.split('/')[0])" :src="getCurrencyFlagImg(h.currencyPair?.split('/')[0])" class="ar-flag" />
              <strong>{{ h.currencyPair }}</strong>
            </td>
            <td class="text-right font-mono">{{ formatCurrency(h.oldBuyRate ?? 0) }}</td>
            <td class="text-right font-mono">{{ formatCurrency(h.newBuyRate ?? 0) }}</td>
            <td class="text-right font-mono">{{ formatCurrency(h.oldSellRate ?? 0) }}</td>
            <td class="text-right font-mono">{{ formatCurrency(h.newSellRate ?? 0) }}</td>
            <td class="text-right" :class="Math.abs(h.changePercent ?? 0) > 3 ? 'change-high' : 'change-normal'">
              {{ (h.changePercent ?? 0).toFixed(2) }}%
            </td>
            <td>
              <span class="source-tag">{{ h.updateSource || '-' }}</span>
            </td>
            <td>{{ h.userName || '-' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.ar-wrap {
  padding: 24px;
  max-width: 1400px;
  margin: 0 auto;
}

.header-actions { display: flex; gap: 8px; }

/* Buttons */
.btn-primary {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 16px; background: var(--color-secondary-hover); color: #fff;
  border: none; border-radius: var(--radius-md); font-size: 13px; font-weight: 600; cursor: pointer;
}
.btn-primary:hover { background: var(--color-secondary-hover); }
.btn-primary:disabled { opacity: .6; cursor: not-allowed; }
.btn-secondary {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 16px; background: var(--color-bg-card); color: var(--color-text);
  border: 1px solid var(--color-border); border-radius: var(--radius-md); font-size: 13px; font-weight: 500; cursor: pointer;
}
.btn-secondary:hover { background: var(--color-bg-page); }
.btn-approve {
  background: none; border: none; cursor: pointer; color: var(--color-success); padding: 4px; border-radius: var(--radius-sm);
}
.btn-approve:hover { background: var(--color-success-bg); }
.btn-reject {
  background: none; border: none; cursor: pointer; color: var(--color-danger); padding: 4px; border-radius: var(--radius-sm);
}
.btn-reject:hover { background: var(--color-danger-bg); }
.btn-approve .material-symbols-outlined,
.btn-reject .material-symbols-outlined { font-size: 22px; }

/* KPI */
.kpi-grid {
  display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px; margin-bottom: 20px;
}
/* Tabs */
.tab-bar {
  display: flex; gap: 4px; margin-bottom: 16px; border-bottom: 1px solid var(--color-border); flex-wrap: wrap;
}
.tab-btn {
  padding: 10px 16px; border: none; background: none; font-size: 13px; font-weight: 500;
  color: var(--color-text-secondary); cursor: pointer; border-bottom: 2px solid transparent;
  display: flex; align-items: center; gap: 6px;
}
.tab-btn:hover { color: var(--color-text); }
.tab-active { color: var(--color-secondary-hover); border-bottom-color: var(--color-secondary-hover); }
.tab-btn .material-symbols-outlined { font-size: 18px; }
.badge {
  background: var(--color-danger); color: #fff; font-size: 11px; padding: 1px 7px;
  border-radius: var(--radius-md); font-weight: 600;
}

/* Loading */
.loading-state { text-align: center; padding: 60px 20px; color: var(--color-text-secondary); font-size: 14px; }
.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

/* Settings */
.settings-panel { max-width: 800px; }
.settings-section {
  background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  box-shadow: var(--shadow-bold);
  padding: 20px; margin-bottom: 16px;
}
.settings-section h3 {
  font-size: 14px; font-weight: 700; color: var(--color-text); margin: 0 0 14px;
  padding-bottom: 8px; border-bottom: 2px solid var(--border-strong);
}
.settings-footer { padding: 16px 0; }

.form-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.form-grid-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 14px; }
.form-group { display: flex; flex-direction: column; gap: 4px; }
.form-group label { font-size: 12px; font-weight: 500; color: var(--color-text); }
.form-group input, .form-group select {
  padding: 8px 12px; border: 1px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 13px; outline: none;
}
.form-group input:focus, .form-group select:focus {
  border-color: var(--color-secondary-hover); box-shadow: 0 0 0 2px rgba(37,99,235,.1);
}
.checkbox-group label {
  display: flex; align-items: center; gap: 8px; cursor: pointer; padding-top: 4px;
}
.checkbox-group input[type="checkbox"] { width: 16px; height: 16px; }

.day-selector { margin-top: 14px; }
.day-selector > label { font-size: 12px; font-weight: 500; color: var(--color-text); display: block; margin-bottom: 6px; }
.day-chips { display: flex; gap: 6px; }
.chip {
  padding: 6px 14px; border-radius: var(--radius-xl); border: 1px solid var(--color-border);
  background: var(--color-bg-card); font-size: 12px; cursor: pointer; color: var(--color-text);
}
.chip:hover { background: var(--color-bg-page); }
.chip-active { background: var(--color-secondary-hover); color: #fff; border-color: var(--color-secondary-hover); }

/* Table */
.table-wrap {
  background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow-x: auto;
  box-shadow: var(--shadow-md);
}
.ar-table {
  width: 100%; border-collapse: collapse; font-size: 13px;
}
.ar-table th {
  padding: 10px 14px; text-align: left; font-weight: 700; color: var(--color-text-secondary);
  font-size: 11px; text-transform: uppercase; letter-spacing: .5px;
  border-bottom: 2px solid var(--border-strong); background: var(--color-bg-page); white-space: nowrap;
}
.ar-table td {
  padding: 10px 14px; border-bottom: 1px solid var(--color-bg-page); color: var(--color-text);
}
.text-right { text-align: right; }
.text-center { text-align: center; }
.font-mono { font-family: 'Consolas', monospace; }
.empty-row { text-align: center; color: var(--color-text-muted); padding: 40px 14px !important; }
.actions-cell { display: flex; gap: 4px; justify-content: center; }
.proposed { color: var(--color-secondary-hover); font-weight: 600; }
.change-high { color: var(--color-danger); font-weight: 600; }
.change-normal { color: var(--color-success); }

.source-tag {
  font-size: 11px; padding: 2px 8px; border-radius: var(--radius-md);
  background: #e0e7ff; color: #4338ca;
}

/* External */
.external-section {}
.ext-header { margin-bottom: 12px; }
.sources-bar {
  display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 16px;
}
.source-chip {
  display: flex; align-items: center; gap: 4px;
  padding: 6px 12px; border-radius: var(--radius-md); border: 1px solid var(--color-border);
  background: var(--color-bg-card); font-size: 12px; font-weight: 500; cursor: pointer;
  transition: border-color 0.2s, background-color 0.2s, color 0.2s;
}
.source-chip:hover { border-color: var(--color-secondary-hover); background: var(--color-secondary-light); }
.source-chip small { font-size: 10px; color: var(--color-text-muted); margin-left: 2px; }
.source-selected { background: var(--color-secondary-hover); color: #fff; border-color: var(--color-secondary-hover); }
.source-selected small { color: #bfdbfe; }
.source-selected .material-symbols-outlined { color: #fff; }
.source-dimmed { opacity: .45; }
.source-group { margin-bottom: 20px; }
.source-group h4 { font-size: 14px; font-weight: 600; color: var(--color-text); margin: 0 0 8px; }
.source-group .ar-table { border: 1px solid var(--color-border); border-radius: var(--radius-md); overflow: hidden; box-shadow: var(--shadow-md); }
.status-ok { color: var(--color-success); font-size: 12px; font-weight: 500; }
.status-err { color: var(--color-danger); font-size: 12px; font-weight: 500; cursor: help; }

.ar-pair-cell { display: flex; align-items: center; gap: 8px; }
.ar-flag { width: 24px; height: 16px; object-fit: cover; border-radius: 2px; border: 1px solid var(--color-border); flex-shrink: 0; }

.ar-ext-table { table-layout: fixed; }
.ar-ext-table .col-pair { width: 15%; }
.ar-ext-table .col-rate { width: 13%; text-align: right; }
.ar-ext-table .col-spread { width: 10%; text-align: right; }
.ar-ext-table .col-date { width: 18%; }
.ar-ext-table .col-status { width: 10%; text-align: center; }

/* Responsive */
@media (max-width: 768px) {
  .ar-wrap { padding: 12px; }
  .form-grid-2, .form-grid-3 { grid-template-columns: 1fr; }
  .kpi-grid { grid-template-columns: repeat(2, 1fr); }
  .day-chips { flex-wrap: wrap; }
}
</style>
