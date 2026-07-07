<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import apiService from '@/services/apiservice'
import { useNotification } from '@/composables/useNotification'
import { getCurrencyFlagImg } from '@/utils/currency'

const props = defineProps<{ officeId: string }>()
const emit = defineEmits<{ (e: 'closed'): void; (e: 'done'): void }>()

const notification = useNotification()
const loading = ref(false)
const submitting = ref(false)
const dayStatus = ref<any>(null)
const physicalCounts = ref<Record<string, number>>({})
const discrepancyNotes = ref<Record<string, string>>({})
const closureNotes = ref('')

onMounted(async () => {
  await loadDayStatus()
})

async function loadDayStatus() {
  loading.value = true
  try {
    dayStatus.value = await apiService.getDayStatus(props.officeId)
    if (dayStatus.value?.systemBalances) {
      for (const b of dayStatus.value.systemBalances) {
        physicalCounts.value[b.currencyId] = b.systemBalance
        discrepancyNotes.value[b.currencyId] = ''
      }
    }
  } catch (err: any) {
    notification.error(err.response?.data?.message || 'Gün durumu yüklenemedi')
  } finally {
    loading.value = false
  }
}

const balances = computed(() => dayStatus.value?.systemBalances || [])

function getDiscrepancy(currencyId: string, systemBalance: number): number {
  const physical = physicalCounts.value[currencyId] ?? 0
  return physical - systemBalance
}

function hasAnyDiscrepancy(): boolean {
  return balances.value.some((b: any) => Math.abs(getDiscrepancy(b.currencyId, b.systemBalance)) > 0.0001)
}

const canSubmit = computed(() => {
  if (!dayStatus.value?.hasUnclosedDays && dayStatus.value?.canTransact) return false
  return balances.value.every((b: any) => {
    const disc = getDiscrepancy(b.currencyId, b.systemBalance)
    if (Math.abs(disc) > 0.0001) {
      return (discrepancyNotes.value[b.currencyId] || '').trim().length > 0
    }
    return true
  })
})

async function submitClosure() {
  if (submitting.value) return
  submitting.value = true
  try {
    const details = balances.value.map((b: any) => ({
      currencyId: b.currencyId,
      physicalCount: physicalCounts.value[b.currencyId] ?? 0,
      discrepancyNote: discrepancyNotes.value[b.currencyId] || ''
    }))
    const result = await apiService.closeDay({
      officeId: props.officeId,
      businessDate: dayStatus.value.firstUnclosedDate || new Date().toISOString(),
      notes: closureNotes.value,
      details
    })
    notification.success('Gün kapanışı başarıyla tamamlandı')
    emit('done')
  } catch (err: any) {
    notification.error(err.response?.data?.message || err.response?.data?.Message || 'Kapanış başarısız')
  } finally {
    submitting.value = false
  }
}

function formatNumber(val: number): string {
  if (val === 0) return '0'
  return val.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function formatDate(d: string): string {
  if (!d) return ''
  return new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}
</script>

<template>
  <div class="dc-overlay" @click.self="emit('closed')">
    <div class="dc-modal">
      <div class="dc-header">
        <span class="material-symbols-outlined dc-header-icon">lock_clock</span>
        <div>
          <h2>Gün Kapanışı</h2>
          <p v-if="dayStatus?.firstUnclosedDate" class="dc-subtitle">
            {{ formatDate(dayStatus.firstUnclosedDate) }} tarihli kapanış
          </p>
        </div>
        <button class="dc-close-btn" @click="emit('closed')">
          <span class="material-symbols-outlined" aria-hidden="true">close</span>
        </button>
      </div>

      <div v-if="loading" class="dc-loading">
        <span class="material-symbols-outlined spin">sync</span>
        Yükleniyor...
      </div>

      <template v-else-if="dayStatus">
        <!-- Already open, no action needed -->
        <div v-if="dayStatus.canTransact && !dayStatus.hasUnclosedDays" class="dc-status-ok">
          <span class="material-symbols-outlined" aria-hidden="true">check_circle</span>
          <p>Gün açık — tüm kapanışlar tamamlanmış, işlem yapabilirsiniz.</p>
          <p v-if="dayStatus.lastClosedDate" class="dc-meta">Son kapanış: {{ formatDate(dayStatus.lastClosedDate) }}</p>
        </div>

        <!-- Closure required -->
        <template v-else>
          <div class="dc-warning-bar">
            <span class="material-symbols-outlined" aria-hidden="true">warning</span>
            <span>{{ dayStatus.blockReason }}</span>
          </div>

          <div v-if="dayStatus.unclosedDayCount > 1" class="dc-info-bar">
            <span class="material-symbols-outlined" aria-hidden="true">info</span>
            <span>{{ dayStatus.unclosedDayCount }} gün kapanmamış. Ara günler otomatik kapatılacak.</span>
          </div>

          <div class="dc-table-wrapper">
            <table class="dc-table">
              <thead>
                <tr>
                  <th>Döviz</th>
                  <th class="text-right">Sistem Bakiyesi</th>
                  <th class="text-right">Fiziksel Sayım</th>
                  <th class="text-right">Fark</th>
                  <th class="text-right">WAC (₺)</th>
                  <th>Açıklama</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="b in balances" :key="b.currencyId"
                    :class="{ 'has-discrepancy': Math.abs(getDiscrepancy(b.currencyId, b.systemBalance)) > 0.0001 }">
                  <td class="dc-currency-cell">
                    <img :src="getCurrencyFlagImg(b.currencyCode)" :alt="b.currencyCode" class="dc-flag" />
                    <span class="dc-code">{{ b.currencyCode }}</span>
                  </td>
                  <td class="text-right mono">{{ formatNumber(b.systemBalance) }}</td>
                  <td class="text-right">
                    <input type="number" class="dc-input"
                           v-model.number="physicalCounts[b.currencyId]"
                           step="any" min="0" />
                  </td>
                  <td class="text-right mono" :class="{
                    'text-red': getDiscrepancy(b.currencyId, b.systemBalance) < -0.0001,
                    'text-green': getDiscrepancy(b.currencyId, b.systemBalance) > 0.0001
                  }">
                    {{ formatNumber(getDiscrepancy(b.currencyId, b.systemBalance)) }}
                  </td>
                  <td class="text-right mono">{{ formatNumber(b.currentWac) }}</td>
                  <td>
                    <input type="text" class="dc-input dc-note-input"
                           v-model="discrepancyNotes[b.currencyId]"
                           :placeholder="Math.abs(getDiscrepancy(b.currencyId, b.systemBalance)) > 0.0001 ? 'Fark açıklaması (zorunlu)' : ''"
                           :class="{ 'dc-required': Math.abs(getDiscrepancy(b.currencyId, b.systemBalance)) > 0.0001 && !discrepancyNotes[b.currencyId]?.trim() }" />
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="dc-notes-section">
            <label>Genel Not (opsiyonel)</label>
            <textarea v-model="closureNotes" class="dc-textarea" rows="2" placeholder="Gün kapanışı hakkında genel not..."></textarea>
          </div>

          <div class="dc-footer">
            <div v-if="hasAnyDiscrepancy()" class="dc-disc-warn">
              <span class="material-symbols-outlined" aria-hidden="true">error</span>
              Sayım farkları tespit edildi — açıklama zorunludur
            </div>
            <div class="dc-actions">
              <button class="dc-btn dc-btn-cancel" @click="emit('closed')">İptal</button>
              <button class="dc-btn dc-btn-submit" :disabled="!canSubmit || submitting" @click="submitClosure">
                <span v-if="submitting" class="material-symbols-outlined spin">sync</span>
                <span v-else class="material-symbols-outlined" aria-hidden="true">lock</span>
                {{ submitting ? 'Kapatılıyor...' : 'Günü Kapat' }}
              </button>
            </div>
          </div>
        </template>
      </template>
    </div>
  </div>
</template>

<style scoped>
.dc-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  background: rgba(0,0,0,.55);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}
.dc-modal {
  background: #1a1a2e;
  border-radius: 16px;
  width: 100%;
  max-width: 900px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 25px 50px rgba(0,0,0,.5);
  border: 1px solid rgba(255,255,255,.08);
}
.dc-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 20px 24px;
  border-bottom: 1px solid rgba(255,255,255,.08);
}
.dc-header h2 {
  margin: 0;
  font-size: 1.25rem;
  color: #fff;
}
.dc-subtitle {
  margin: 2px 0 0;
  font-size: .85rem;
  color: rgba(255,255,255,.5);
}
.dc-header-icon {
  font-size: 28px;
  color: #f59e0b;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
.dc-close-btn {
  margin-left: auto;
  background: none;
  border: none;
  color: rgba(255,255,255,.5);
  cursor: pointer;
  padding: 4px;
  border-radius: 8px;
  transition: background-color 0.2s, color 0.2s;
}
.dc-close-btn:hover {
  background: rgba(255,255,255,.1);
  color: #fff;
}
.dc-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 60px;
  color: rgba(255,255,255,.5);
}
.dc-status-ok {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 40px 24px;
  text-align: center;
}
.dc-status-ok .material-symbols-outlined {
  font-size: 48px;
  color: #10b981;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 48;
}
.dc-status-ok p { color: rgba(255,255,255,.8); margin: 0; }
.dc-meta { font-size: .8rem; color: rgba(255,255,255,.4) !important; }
.dc-warning-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 24px;
  background: rgba(245,158,11,.12);
  color: #f59e0b;
  font-size: .9rem;
}
.dc-warning-bar .material-symbols-outlined {
  font-size: 20px;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 20;
}
.dc-info-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 24px;
  background: rgba(59,130,246,.1);
  color: #60a5fa;
  font-size: .85rem;
}
.dc-info-bar .material-symbols-outlined {
  font-size: 18px;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 20;
}
.dc-table-wrapper {
  padding: 16px 24px;
  overflow-x: auto;
}
.dc-table {
  width: 100%;
  border-collapse: collapse;
  font-size: .9rem;
}
.dc-table th {
  padding: 8px 10px;
  text-align: left;
  color: rgba(255,255,255,.5);
  font-weight: 500;
  font-size: .8rem;
  text-transform: uppercase;
  letter-spacing: .5px;
  border-bottom: 1px solid rgba(255,255,255,.1);
}
.dc-table td {
  padding: 10px;
  color: #fff;
  border-bottom: 1px solid rgba(255,255,255,.05);
}
.dc-table tr.has-discrepancy {
  background: rgba(239,68,68,.08);
}
.dc-currency-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}
.dc-flag {
  width: 24px;
  height: 18px;
  object-fit: cover;
  border-radius: 2px;
}
.dc-code {
  font-weight: 600;
  font-size: .95rem;
}
.text-right { text-align: right; }
.mono { font-family: 'JetBrains Mono', monospace; font-size: .85rem; }
.text-red { color: #ef4444; }
.text-green { color: #10b981; }
.dc-input {
  background: rgba(255,255,255,.06);
  border: 1px solid rgba(255,255,255,.12);
  border-radius: 8px;
  color: #fff;
  padding: 6px 10px;
  font-size: .9rem;
  width: 120px;
  text-align: right;
  outline: none;
  transition: border-color .2s;
}
.dc-input:focus {
  border-color: #6b46c1;
}
.dc-note-input {
  width: 180px;
  text-align: left;
}
.dc-required {
  border-color: #ef4444 !important;
}
.dc-notes-section {
  padding: 0 24px 16px;
}
.dc-notes-section label {
  display: block;
  font-size: .8rem;
  color: rgba(255,255,255,.5);
  margin-bottom: 6px;
}
.dc-textarea {
  width: 100%;
  background: rgba(255,255,255,.06);
  border: 1px solid rgba(255,255,255,.12);
  border-radius: 8px;
  color: #fff;
  padding: 10px 12px;
  font-size: .9rem;
  resize: vertical;
  outline: none;
}
.dc-textarea:focus { border-color: #6b46c1; }
.dc-footer {
  padding: 16px 24px;
  border-top: 1px solid rgba(255,255,255,.08);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
}
.dc-disc-warn {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: .85rem;
  color: #ef4444;
}
.dc-disc-warn .material-symbols-outlined {
  font-size: 18px;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 20;
}
.dc-actions {
  display: flex;
  gap: 10px;
  margin-left: auto;
}
.dc-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 20px;
  border-radius: 10px;
  font-size: .9rem;
  font-weight: 500;
  cursor: pointer;
  border: none;
  transition: background-color 0.2s, color 0.2s;
}
.dc-btn-cancel {
  background: rgba(255,255,255,.08);
  color: rgba(255,255,255,.7);
}
.dc-btn-cancel:hover {
  background: rgba(255,255,255,.12);
}
.dc-btn-submit {
  background: linear-gradient(135deg, #6b46c1, #7c3aed);
  color: #fff;
}
.dc-btn-submit:hover:not(:disabled) {
  background: linear-gradient(135deg, #7c3aed, #8b5cf6);
  transform: translateY(-1px);
}
.dc-btn-submit:disabled {
  opacity: .5;
  cursor: not-allowed;
}
.dc-btn .material-symbols-outlined {
  font-size: 18px;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 20;
}
@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin 1s linear infinite; }
</style>
