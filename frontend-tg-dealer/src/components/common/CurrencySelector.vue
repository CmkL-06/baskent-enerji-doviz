<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { getCurrencyCountryCode, getCurrencyName } from '@/utils/currency'

interface Currency {
  id: string
  currencyCode: string
  currencyName: string
  currencySymbol: string
}

interface VaultBalance {
  currencyId: string
  currencyCode: string
  balance: number
}

const props = withDefaults(defineProps<{
  modelValue: string
  currencies: Currency[]
  placeholder?: string
  disabled?: boolean
  vaultBalances?: VaultBalance[]
  compact?: boolean
  dropdownMinWidth?: number
}>(), {
  placeholder: 'Para birimi seçin',
  disabled: false,
  compact: false,
  dropdownMinWidth: 280
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const isOpen = ref(false)
const search = ref('')
const containerRef = ref<HTMLElement>()
const searchInputRef = ref<HTMLInputElement>()

const selectedCurrency = computed(() => {
  return props.currencies.find(c => c.id === props.modelValue)
})

const filteredCurrencies = computed(() => {
  if (!search.value) return props.currencies
  const q = search.value.toLowerCase()
  return props.currencies.filter(c =>
    c.currencyCode.toLowerCase().includes(q) ||
    c.currencyName.toLowerCase().includes(q) ||
    getCurrencyName(c.currencyCode).toLowerCase().includes(q)
  )
})

const dropdownStyle = computed(() => {
  if (!containerRef.value) return {}
  const rect = containerRef.value.getBoundingClientRect()
  const spaceBelow = window.innerHeight - rect.bottom
  const openUp = spaceBelow < 300 && rect.top > 300
  return {
    position: 'fixed' as const,
    left: `${rect.left}px`,
    width: `${Math.max(rect.width, props.dropdownMinWidth)}px`,
    zIndex: 9999,
    ...(openUp
      ? { bottom: `${window.innerHeight - rect.top + 4}px` }
      : { top: `${rect.bottom + 4}px` })
  }
})

function getBalance(currencyId: string): number | null {
  if (!props.vaultBalances?.length) return null
  const b = props.vaultBalances.find(vb => vb.currencyId === currencyId)
  return b ? b.balance : null
}

function formatBalance(val: number): string {
  return new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(val)
}

function getFlagClass(code: string): string {
  const cc = getCurrencyCountryCode(code)
  return cc ? `fi fi-${cc}` : ''
}

function toggle() {
  if (props.disabled) return
  isOpen.value = !isOpen.value
  if (isOpen.value) {
    search.value = ''
    setTimeout(() => searchInputRef.value?.focus(), 50)
  }
}

function select(currency: Currency) {
  emit('update:modelValue', currency.id)
  isOpen.value = false
  search.value = ''
}

function onClickOutside(e: MouseEvent) {
  if (!isOpen.value) return
  const target = e.target as Node
  if (containerRef.value && containerRef.value.contains(target)) return
  const dropdown = document.querySelector('.cs-dropdown')
  if (dropdown && dropdown.contains(target)) return
  isOpen.value = false
  search.value = ''
}

onMounted(() => document.addEventListener('click', onClickOutside, true))
onUnmounted(() => document.removeEventListener('click', onClickOutside, true))
</script>

<template>
  <div ref="containerRef" class="cs-root" :class="{ 'cs-compact': compact, 'cs-disabled': disabled }">
    <button
      type="button"
      class="cs-trigger"
      :class="{ 'cs-open': isOpen }"
      @click="toggle"
      :disabled="disabled"
    >
      <template v-if="selectedCurrency">
        <span v-if="selectedCurrency.currencyCode === 'USDT'" class="cs-crypto">₮</span>
        <span v-else-if="selectedCurrency.currencyCode === 'KRUB'" class="material-symbols-outlined cs-icon-sm">credit_card</span>
        <i v-else-if="getFlagClass(selectedCurrency.currencyCode)" :class="getFlagClass(selectedCurrency.currencyCode)" class="cs-flag"></i>
        <span class="cs-code">{{ selectedCurrency.currencyCode }}</span>
        <span v-if="!compact" class="cs-name">{{ getCurrencyName(selectedCurrency.currencyCode) }}</span>
      </template>
      <span v-else class="cs-placeholder">{{ placeholder }}</span>
      <span class="material-symbols-outlined cs-chevron" :class="{ 'cs-chevron-up': isOpen }">expand_more</span>
    </button>

    <Teleport to="body">
      <div
        v-if="isOpen"
        class="cs-dropdown"
        :style="dropdownStyle"
      >
        <div class="cs-search-wrap">
          <span class="material-symbols-outlined cs-search-icon">search</span>
          <input
            ref="searchInputRef"
            v-model="search"
            class="cs-search"
            placeholder="Ara..."
            @keydown.esc="isOpen = false"
          />
        </div>
        <div class="cs-list">
          <button
            v-for="c in filteredCurrencies"
            :key="c.id"
            type="button"
            class="cs-option"
            :class="{ 'cs-selected': c.id === modelValue }"
            @click="select(c)"
          >
            <div class="cs-option-left">
              <span v-if="c.currencyCode === 'USDT'" class="cs-crypto">₮</span>
              <span v-else-if="c.currencyCode === 'KRUB'" class="material-symbols-outlined cs-icon-sm">credit_card</span>
              <i v-else-if="getFlagClass(c.currencyCode)" :class="getFlagClass(c.currencyCode)" class="cs-flag"></i>
              <span v-else class="cs-flag-placeholder"></span>
              <div class="cs-option-text">
                <span class="cs-option-code">{{ c.currencyCode }}</span>
                <span class="cs-option-name">{{ getCurrencyName(c.currencyCode) }}</span>
              </div>
            </div>
            <div class="cs-option-right">
              <span v-if="getBalance(c.id) !== null" class="cs-balance">
                {{ formatBalance(getBalance(c.id)!) }}
              </span>
              <span v-if="c.id === modelValue" class="material-symbols-outlined cs-check">check</span>
            </div>
          </button>
          <div v-if="filteredCurrencies.length === 0" class="cs-empty">Sonuç bulunamadı</div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.cs-root {
  position: relative;
}

.cs-trigger {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 10px 12px;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  cursor: pointer;
  transition: border-color 0.2s, box-shadow 0.2s;
  font-size: 14px;
  text-align: left;
}
.cs-trigger:hover:not(:disabled) {
  border-color: #6366f1;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.08);
}
.cs-trigger:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  background: #f9fafb;
}
.cs-open {
  border-color: #6366f1;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.12);
}

.cs-compact .cs-trigger {
  padding: 6px 10px;
  font-size: 13px;
  border-radius: 8px;
}

.cs-flag {
  font-size: 18px;
  flex-shrink: 0;
}
.cs-compact .cs-flag {
  font-size: 14px;
}
.cs-crypto {
  font-weight: 700;
  font-size: 18px;
  color: #10b981;
  flex-shrink: 0;
}
.cs-icon-sm {
  font-size: 18px;
  color: #6b46c1;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 20;
  flex-shrink: 0;
}
.cs-code {
  font-weight: 600;
  color: #1f2937;
}
.cs-name {
  color: #6b7280;
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.cs-placeholder {
  color: #9ca3af;
}
.cs-chevron {
  margin-left: auto;
  font-size: 20px;
  color: #9ca3af;
  transition: transform 0.2s;
  flex-shrink: 0;
}
.cs-chevron-up {
  transform: rotate(180deg);
}

.cs-dropdown {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.12), 0 2px 8px rgba(0, 0, 0, 0.06);
  overflow: hidden;
  animation: cs-fade-in 0.15s ease;
}
@keyframes cs-fade-in {
  from { opacity: 0; transform: translateY(-4px); }
  to { opacity: 1; transform: translateY(0); }
}

.cs-search-wrap {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  border-bottom: 1px solid #f3f4f6;
}
.cs-search-icon {
  font-size: 18px;
  color: #9ca3af;
}
.cs-search {
  flex: 1;
  border: none;
  outline: none;
  font-size: 14px;
  background: transparent;
}

.cs-list {
  max-height: 260px;
  overflow-y: auto;
  padding: 4px;
}
.cs-list::-webkit-scrollbar { width: 6px; }
.cs-list::-webkit-scrollbar-thumb { background: #d1d5db; border-radius: 3px; }

.cs-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 8px 10px;
  border: none;
  background: transparent;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.15s;
  font-size: 14px;
  text-align: left;
}
.cs-option:hover {
  background: #f5f3ff;
}
.cs-selected {
  background: #ede9fe;
}

.cs-option-left {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}
.cs-flag-placeholder {
  width: 20px;
  flex-shrink: 0;
}
.cs-option-text {
  display: flex;
  align-items: baseline;
  gap: 6px;
  min-width: 0;
}
.cs-option-code {
  font-weight: 600;
  color: #1f2937;
  white-space: nowrap;
}
.cs-option-name {
  color: #6b7280;
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cs-option-right {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
}

.cs-balance {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-size: 12px;
  color: #6366f1;
  font-weight: 500;
}

.cs-check {
  font-size: 18px;
  color: #6366f1;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 20;
}

.cs-empty {
  padding: 16px;
  text-align: center;
  color: #9ca3af;
  font-size: 14px;
}

.cs-disabled {
  pointer-events: none;
}

.material-symbols-outlined {
  font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
</style>
