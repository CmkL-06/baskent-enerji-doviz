<script setup lang="ts">
import { ref, computed, nextTick, watch } from 'vue'
import apiService from '@/services/apiservice'

const props = defineProps<{
  vaultId: string
  vaultName: string
  vaultBalances: any[]
  isManual: boolean
}>()

const emit = defineEmits<{
  complete: []
  'waiting-customer': []
}>()

const visible = ref(false)
const submitting = ref(false)
const error = ref('')
const success = ref(false)
const countInputs = ref<Record<string, string>>({})
const lastCount = ref<any>(null)
const loadingLastCount = ref(false)
const addedCurrencies = ref<Set<string>>(new Set())
const showAddMenu = ref(false)

const activeCurrencies = computed(() => {
  if (!props.vaultBalances?.length) return []
  return props.vaultBalances
    .filter((b: any) => b.balance !== 0 || addedCurrencies.value.has(b.currencyId))
    .map((b: any) => ({
      currencyId: b.currencyId,
      currencyCode: b.currencyCode || '',
      currencyName: b.currencyName || '',
      systemBalance: b.balance ?? 0,
    }))
    .filter((r: any) => r.currencyCode)
    .sort((a: any, b: any) => {
      if (a.currencyCode === 'TRY') return -1
      if (b.currencyCode === 'TRY') return 1
      return a.currencyCode.localeCompare(b.currencyCode)
    })
})

const inactiveCurrencies = computed(() => {
  if (!props.vaultBalances?.length) return []
  return props.vaultBalances
    .filter((b: any) => b.balance === 0 && !addedCurrencies.value.has(b.currencyId))
    .map((b: any) => ({
      currencyId: b.currencyId,
      currencyCode: b.currencyCode || '',
      currencyName: b.currencyName || '',
    }))
    .filter((r: any) => r.currencyCode)
    .sort((a: any, b: any) => a.currencyCode.localeCompare(b.currencyCode))
})

function getLastCountAmount(currencyId: string): number | null {
  if (!lastCount.value?.countDetails) return null
  const detail = lastCount.value.countDetails.find((d: any) => d.currencyId === currencyId)
  return detail ? detail.actualAmount : null
}

async function open(_force = false) {
  error.value = ''
  success.value = false
  submitting.value = false
  countInputs.value = {}
  addedCurrencies.value = new Set()
  showAddMenu.value = false
  visible.value = true

  loadingLastCount.value = true
  try {
    const counts = await apiService.getVaultCounts(props.vaultId)
    const items = Array.isArray(counts) ? counts : (counts?.items ?? counts?.data ?? [])
    lastCount.value = items.length > 0 ? items[0] : null
  } catch {
    lastCount.value = null
  } finally {
    loadingLastCount.value = false
  }

  nextTick(() => {
    const firstInput = document.querySelector('.vcm-input') as HTMLInputElement
    firstInput?.focus()
  })
}

function close() {
  visible.value = false
  showAddMenu.value = false
}

function addCurrency(currencyId: string) {
  addedCurrencies.value = new Set([...addedCurrencies.value, currencyId])
  showAddMenu.value = false
  nextTick(() => {
    const inputs = document.querySelectorAll('.vcm-input') as NodeListOf<HTMLInputElement>
    inputs[inputs.length - 1]?.focus()
  })
}

function removeCurrency(currencyId: string) {
  const s = new Set(addedCurrencies.value)
  s.delete(currencyId)
  addedCurrencies.value = s
  delete countInputs.value[currencyId]
}

function getInputValue(currencyId: string): number {
  const raw = countInputs.value[currencyId] || ''
  const val = parseFloat(raw.replace(',', '.'))
  return isNaN(val) ? 0 : val
}

const allFilled = computed(() => {
  return activeCurrencies.value.every((row: any) => {
    const raw = countInputs.value[row.currencyId]
    return raw !== undefined && raw !== '' && !isNaN(parseFloat(String(raw).replace(',', '.')))
  })
})

function getDiff(row: any): number {
  const actual = getInputValue(row.currencyId)
  return actual - row.systemBalance
}

function getLastDiff(row: any): number | null {
  const lastAmt = getLastCountAmount(row.currencyId)
  if (lastAmt === null) return null
  return row.systemBalance - lastAmt
}

async function submit() {
  if (!allFilled.value) {
    error.value = 'Tüm döviz miktarlarını giriniz'
    return
  }
  submitting.value = true
  error.value = ''
  try {
    await apiService.submitVaultCount({
      vaultId: props.vaultId,
      isManual: props.isManual,
      countDetails: activeCurrencies.value.map((row: any) => ({
        currencyId: row.currencyId,
        actualAmount: getInputValue(row.currencyId),
      })),
    })
    success.value = true
    setTimeout(() => {
      visible.value = false
      emit('complete')
    }, 1500)
  } catch (e: any) {
    error.value = e?.response?.data?.error || e?.message || 'Kasa sayımı gönderilemedi'
  } finally {
    submitting.value = false
  }
}

function fmt(n: number): string {
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtDiff(n: number): string {
  return (n > 0 ? '+' : '') + fmt(n)
}

function formatDate(d: string): string {
  if (!d) return ''
  const dt = new Date(d)
  return dt.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function getRemainingTime(): number {
  return 0
}

defineExpose({ open, close, getRemainingTime })
</script>

<template>
  <Teleport to="body">
    <Transition name="vcm-fade">
      <div v-if="visible" class="vcm-backdrop" @click.self="close">
        <div class="vcm-modal" @click.stop>
          <!-- Header -->
          <div class="vcm-header">
            <div class="vcm-header-left">
              <div class="vcm-header-icon">
                <span class="material-symbols-outlined" aria-hidden="true">inventory</span>
              </div>
              <div>
                <h2 class="vcm-title">Kasa Sayımı</h2>
                <p class="vcm-subtitle">{{ vaultName }}</p>
              </div>
            </div>
            <button class="vcm-close" @click="close">
              <span class="material-symbols-outlined" aria-hidden="true">close</span>
            </button>
          </div>

          <!-- Success State -->
          <div v-if="success" class="vcm-success">
            <span class="material-symbols-outlined vcm-success-icon">check_circle</span>
            <p class="vcm-success-text">Kasa sayımı başarıyla kaydedildi!</p>
          </div>

          <template v-else>
            <!-- Last Count Info -->
            <div v-if="lastCount" class="vcm-last-count">
              <span class="material-symbols-outlined" aria-hidden="true">history</span>
              <span>Son sayım: <strong>{{ formatDate(lastCount.countDate) }}</strong></span>
              <span v-if="lastCount.hasDiscrepancy" class="vcm-badge vcm-badge--red">Farklı</span>
              <span v-else class="vcm-badge vcm-badge--green">Eşleşti</span>
            </div>

            <!-- Body -->
            <div class="vcm-body">
              <!-- Currency Rows -->
              <div v-for="row in activeCurrencies" :key="row.currencyId" class="vcm-row">
                <div class="vcm-row-header">
                  <div class="vcm-currency">
                    <span class="vcm-currency-code">{{ row.currencyCode }}</span>
                    <span class="vcm-currency-name">{{ row.currencyName }}</span>
                  </div>
                  <button
                    v-if="row.systemBalance === 0"
                    class="vcm-remove-btn"
                    @click="removeCurrency(row.currencyId)"
                    title="Kaldır"
                  >
                    <span class="material-symbols-outlined" aria-hidden="true">close</span>
                  </button>
                </div>

                <div class="vcm-row-body">
                  <div class="vcm-col">
                    <label class="vcm-label">Sistem</label>
                    <div class="vcm-system-val">{{ fmt(row.systemBalance) }}</div>
                  </div>

                  <div class="vcm-col vcm-col--input">
                    <label class="vcm-label">Sayılan</label>
                    <input
                      v-model="countInputs[row.currencyId]"
                      type="text"
                      inputmode="decimal"
                      class="vcm-input"
                      :placeholder="fmt(row.systemBalance)"
                      @keydown.enter="submit"
                    />
                  </div>

                  <div class="vcm-col vcm-col--diff">
                    <label class="vcm-label">Fark</label>
                    <div v-if="countInputs[row.currencyId] !== '' && countInputs[row.currencyId] !== undefined" class="vcm-diff-val" :class="{
                      'vcm-pos': getDiff(row) > 0.01,
                      'vcm-neg': getDiff(row) < -0.01,
                      'vcm-ok': Math.abs(getDiff(row)) <= 0.01
                    }">{{ fmtDiff(getDiff(row)) }}</div>
                    <div v-else class="vcm-diff-val vcm-empty">—</div>
                  </div>

                  <div v-if="lastCount" class="vcm-col vcm-col--last">
                    <label class="vcm-label">Son Sayımdan Bu Yana</label>
                    <div v-if="getLastDiff(row) !== null" class="vcm-diff-val" :class="{
                      'vcm-pos': getLastDiff(row)! > 0.01,
                      'vcm-neg': getLastDiff(row)! < -0.01,
                      'vcm-ok': Math.abs(getLastDiff(row)!) <= 0.01
                    }">{{ fmtDiff(getLastDiff(row)!) }}</div>
                    <div v-else class="vcm-diff-val vcm-empty">—</div>
                  </div>
                </div>
              </div>

              <!-- Add Currency Button -->
              <div v-if="inactiveCurrencies.length > 0" class="vcm-add-section">
                <button class="vcm-add-btn" @click="showAddMenu = !showAddMenu">
                  <span class="material-symbols-outlined" aria-hidden="true">add_circle</span>
                  Para Birimi Ekle
                </button>
                <div v-if="showAddMenu" class="vcm-add-menu">
                  <button
                    v-for="curr in inactiveCurrencies"
                    :key="curr.currencyId"
                    class="vcm-add-item"
                    @click="addCurrency(curr.currencyId)"
                  >
                    <span class="vcm-add-item-code">{{ curr.currencyCode }}</span>
                    <span class="vcm-add-item-name">{{ curr.currencyName }}</span>
                  </button>
                </div>
              </div>

              <!-- Error -->
              <div v-if="error" class="vcm-error">
                <span class="material-symbols-outlined" aria-hidden="true">error</span>
                <span>{{ error }}</span>
              </div>
            </div>

            <!-- Footer -->
            <div class="vcm-footer">
              <button class="vcm-btn vcm-btn--ghost" @click="$emit('waiting-customer')">
                <span class="material-symbols-outlined" aria-hidden="true">person</span>
                Müşteri Bekliyor
              </button>
              <div class="vcm-footer-right">
                <button class="vcm-btn vcm-btn--secondary" @click="close">İptal</button>
                <button class="vcm-btn vcm-btn--primary" :disabled="!allFilled || submitting" @click="submit">
                  <svg v-if="submitting" class="vcm-spin" viewBox="0 0 24 24" width="18" height="18">
                    <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" fill="none" opacity=".3"/>
                    <path d="M12 2a10 10 0 019.95 9" stroke="currentColor" stroke-width="3" fill="none" stroke-linecap="round"/>
                  </svg>
                  <span v-else class="material-symbols-outlined" aria-hidden="true">check</span>
                  Sayımı Kaydet
                </button>
              </div>
            </div>
          </template>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.vcm-backdrop {
  position: fixed; inset: 0; z-index: 9999;
  background: rgba(0,0,0,.5); backdrop-filter: blur(6px);
  display: flex; align-items: center; justify-content: center;
  padding: 16px;
}
.vcm-modal {
  background: #fff; border-radius: var(--radius-xl); width: 100%; max-width: 580px;
  max-height: 90vh; display: flex; flex-direction: column;
  box-shadow: 0 25px 80px rgba(0,0,0,.25);
  overflow: hidden;
}

/* Header */
.vcm-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 20px 24px; border-bottom: 1px solid var(--color-bg-page);
  flex-shrink: 0;
}
.vcm-header-left { display: flex; align-items: center; gap: 12px; }
.vcm-header-icon {
  width: 44px; height: 44px; border-radius: var(--radius-lg);
  background: linear-gradient(135deg, #fee2e2, #fecaca); color: var(--color-danger);
  display: flex; align-items: center; justify-content: center;
  font-size: 24px;
}
.vcm-title { font-size: 17px; font-weight: 700; color: var(--color-text); margin: 0; }
.vcm-subtitle { font-size: 12px; color: var(--color-text-muted); margin: 2px 0 0; }
.vcm-close {
  background: none; border: none; cursor: pointer;
  color: var(--color-text-muted); padding: 6px; border-radius: var(--radius-md); transition: background-color 0.15s, color 0.15s;
}
.vcm-close:hover { background: var(--color-bg-page); color: var(--color-text); }

/* Last Count Banner */
.vcm-last-count {
  display: flex; align-items: center; gap: 8px;
  padding: 10px 24px; background: var(--color-bg-page); border-bottom: 1px solid var(--color-bg-page);
  font-size: 12px; color: var(--color-text-secondary); flex-shrink: 0;
}
.vcm-last-count .material-symbols-outlined { font-size: 16px; }
.vcm-badge {
  font-size: 10px; font-weight: 700; padding: 2px 8px; border-radius: 99px;
  text-transform: uppercase; letter-spacing: .3px;
}
.vcm-badge--green { background: #dcfce7; color: #15803d; }
.vcm-badge--red { background: var(--color-danger-bg); color: var(--color-danger); }

/* Body */
.vcm-body {
  flex: 1; overflow-y: auto; padding: 16px 24px;
  min-height: 0;
}

/* Currency Row */
.vcm-row {
  padding: 14px 0; border-bottom: 1px solid var(--color-bg-page);
}
.vcm-row:last-of-type { border-bottom: none; }

.vcm-row-header {
  display: flex; align-items: center; justify-content: space-between;
  margin-bottom: 10px;
}
.vcm-currency { display: flex; align-items: baseline; gap: 8px; }
.vcm-currency-code { font-size: 15px; font-weight: 800; color: var(--color-text); }
.vcm-currency-name { font-size: 11px; color: var(--color-text-muted); text-transform: uppercase; letter-spacing: .3px; }

.vcm-remove-btn {
  background: none; border: none; cursor: pointer;
  color: var(--color-border); padding: 2px; border-radius: var(--radius-sm); transition: background-color 0.15s, color 0.15s;
}
.vcm-remove-btn .material-symbols-outlined { font-size: 16px; }
.vcm-remove-btn:hover { color: var(--color-danger); background: #fef2f2; }

.vcm-row-body {
  display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 8px;
  align-items: end;
}
.vcm-row-body:has(.vcm-col--last) {
  grid-template-columns: 1fr 1.2fr .8fr .8fr;
}

.vcm-label {
  display: block; font-size: 10px; font-weight: 600; color: var(--color-text-muted);
  text-transform: uppercase; letter-spacing: .5px; margin-bottom: 4px;
}

.vcm-system-val {
  font-family: 'SF Mono', 'Cascadia Code', 'Consolas', monospace;
  font-size: 14px; font-weight: 600; color: var(--color-text);
  padding: 8px 0;
}

.vcm-input {
  width: 100%; padding: 8px 10px;
  border: 1.5px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 14px; font-family: 'SF Mono', 'Cascadia Code', 'Consolas', monospace;
  text-align: right; outline: none; transition: border-color 0.15s, background-color 0.15s, box-shadow 0.15s;
  background: var(--color-bg-page); font-weight: 600;
}
.vcm-input:focus { border-color: var(--color-primary); background: #fff; box-shadow: 0 0 0 3px rgba(99,102,241,.1); }

.vcm-diff-val {
  font-family: 'SF Mono', 'Cascadia Code', 'Consolas', monospace;
  font-size: 13px; font-weight: 700; padding: 8px 0; text-align: right;
}
.vcm-pos { color: var(--color-success); }
.vcm-neg { color: var(--color-danger); }
.vcm-ok { color: var(--color-success); }
.vcm-empty { color: var(--color-border); }

/* Add Currency */
.vcm-add-section { padding-top: 12px; position: relative; }
.vcm-add-btn {
  display: inline-flex; align-items: center; gap: 6px;
  background: none; border: 1.5px dashed var(--color-border); border-radius: var(--radius-md);
  padding: 8px 16px; color: var(--color-text-secondary); font-size: 13px; font-weight: 600;
  cursor: pointer; transition: border-color 0.15s, color 0.15s, background-color 0.15s; width: 100%; justify-content: center;
}
.vcm-add-btn:hover { border-color: var(--color-primary); color: var(--color-primary); background: var(--color-primary-light); }
.vcm-add-btn .material-symbols-outlined { font-size: 18px; }

.vcm-add-menu {
  position: absolute; left: 0; right: 0; top: 100%; margin-top: 4px;
  background: #fff; border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  box-shadow: 0 8px 30px rgba(0,0,0,.12); z-index: 10;
  max-height: 200px; overflow-y: auto; padding: 4px;
}
.vcm-add-item {
  display: flex; align-items: center; gap: 8px; width: 100%;
  padding: 8px 12px; border: none; background: none;
  cursor: pointer; border-radius: var(--radius-md); transition: background .1s;
  text-align: left;
}
.vcm-add-item:hover { background: var(--color-bg-page); }
.vcm-add-item-code { font-weight: 700; font-size: 13px; color: var(--color-text); min-width: 48px; }
.vcm-add-item-name { font-size: 12px; color: var(--color-text-muted); }

/* Error */
.vcm-error {
  display: flex; align-items: center; gap: 8px; margin-top: 12px;
  padding: 10px 14px; background: #fef2f2; border: 1px solid #fca5a5;
  border-radius: var(--radius-md); font-size: 13px; color: var(--color-danger);
}

/* Footer */
.vcm-footer {
  display: flex; align-items: center; justify-content: space-between;
  padding: 14px 24px; border-top: 1px solid var(--color-bg-page); background: #fafbfc;
  flex-shrink: 0;
}
.vcm-footer-right { display: flex; gap: 8px; }

.vcm-btn {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 9px 16px; border-radius: var(--radius-md); font-size: 13px; font-weight: 600;
  border: none; cursor: pointer; transition: background-color 0.15s, color 0.15s, opacity 0.15s;
}
.vcm-btn:disabled { opacity: .45; cursor: not-allowed; }
.vcm-btn--primary { background: var(--color-primary); color: #fff; }
.vcm-btn--primary:hover:not(:disabled) { background: var(--color-primary-hover); }
.vcm-btn--secondary { background: var(--color-bg-page); color: var(--color-text-secondary); }
.vcm-btn--secondary:hover { background: var(--color-border); }
.vcm-btn--ghost { background: none; color: var(--color-text-secondary); font-size: 12px; }
.vcm-btn--ghost:hover { background: var(--color-bg-page); color: var(--color-text); }
.vcm-btn .material-symbols-outlined { font-size: 18px; }

/* Success */
.vcm-success {
  display: flex; flex-direction: column; align-items: center; gap: 12px;
  padding: 56px 24px; text-align: center;
}
.vcm-success-icon { font-size: 56px; color: var(--color-success); }
.vcm-success-text { font-size: 16px; font-weight: 700; color: var(--color-success); margin: 0; }

/* Animations */
@keyframes vcm-spin { to { transform: rotate(360deg); } }
.vcm-spin { animation: vcm-spin .7s linear infinite; }
.vcm-fade-enter-active, .vcm-fade-leave-active { transition: opacity .2s ease; }
.vcm-fade-enter-from, .vcm-fade-leave-to { opacity: 0; }
</style>
