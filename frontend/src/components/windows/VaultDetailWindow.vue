<template>
  <div class="vault-detail-window">
    <!-- Premium Header -->
    <div class="premium-header">
      <div class="header-content">
        <div class="vault-identity">
          <div class="vault-icon-wrapper">
            <div class="vault-icon">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M3 7h18v10c0 1.1-.9 2-2 2H5c-1.1 0-2-.9-2-2V7z" fill="currentColor" opacity="0.2"/>
                <path d="M21 7V6c0-2.21-1.79-4-4-4H7C4.79 2 3 3.79 3 6v1h18z" fill="currentColor" opacity="0.3"/>
                <path d="M12 13a2 2 0 100-4 2 2 0 000 4z" fill="currentColor"/>
                <path d="M21 7H3v10c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V7zM12 13c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2z" stroke="currentColor" stroke-width="1.5" fill="none"/>
              </svg>
            </div>
            <div :class="['status-indicator', vault?.isActive ? 'active' : 'inactive']"></div>
          </div>
          <div class="vault-title">
            <h1>{{ vault?.vaultName || 'Kasa' }}</h1>
            <div class="vault-meta">
              <span class="meta-item">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M7 7h10v10H7z" stroke="currentColor" stroke-width="2"/>
                  <path d="M5 3h14M3 5v14m18-14v14M5 21h14" stroke="currentColor" stroke-width="1.5" opacity="0.5"/>
                </svg>
                {{ vault?.vaultId }}
              </span>
              <span class="meta-divider">•</span>
              <span :class="['meta-status', vault?.isActive ? 'active' : 'inactive']">
                {{ vault?.isActive ? 'Aktif' : 'Pasif' }}
              </span>
            </div>
          </div>
        </div>
        <button @click="refresh" class="refresh-button" :disabled="isLoading">
          <svg :class="{ 'rotating': isLoading }" width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M4 12a8 8 0 018-8V2.5L16 6l-4 3.5V8a6 6 0 00-6 6h1.5m14.5 0a8 8 0 01-8 8v1.5L8 18l4-3.5V16a6 6 0 006-6h-1.5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <span>Yenile</span>
        </button>
      </div>
      <div class="header-gradient"></div>
    </div>

    <div v-if="vault && !isLoading" class="vault-content">
      <!-- Quick Actions -->
      <div class="quick-actions">
        <h3>Hızlı İşlemler</h3>
        <div class="actions-grid">
          <button @click="openTransfer" class="action-card transfer">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M7 10L12 5L17 10M17 14L12 19L7 14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <span class="action-label">Transfer</span>
            <span class="action-description">Kasalar arası para transferi</span>
          </button>
          
          <button @click="openDeposit" class="action-card deposit">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 2v20m0-20l-4 4m4-4l4 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M5 19h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </div>
            <span class="action-label">Para Yatır</span>
            <span class="action-description">Kasaya para girişi</span>
          </button>
          
          <button @click="openWithdraw" class="action-card withdraw">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 22V2m0 20l4-4m-4 4l-4-4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M5 5h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </div>
            <span class="action-label">Para Çek</span>
            <span class="action-description">Kasadan para çıkışı</span>
          </button>
          
          <button @click="editVault" class="action-card edit">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <span class="action-label">Düzenle</span>
            <span class="action-description">Kasa bilgilerini güncelle</span>
          </button>
        </div>
      </div>

      <!-- Unified Balance Card -->
      <div class="unified-balance-card">
        <div class="balance-card-header">
          <h2>Kasa Bakiyeleri</h2>
          <div class="total-value">
            <span class="total-label">Toplam Değer</span>
            <span class="total-amount">{{ formatCurrency(vault.totalValueInBaseCurrency || 0, 'TRY') }}</span>
            <span class="foreign-total" v-if="foreignCurrencyTotal > 0">
              Yabancı Para: {{ formatCurrency(foreignCurrencyTotal, 'TRY') }}
            </span>
          </div>
        </div>
        
        <div class="currencies-grid">
          <div 
            v-for="balance in vault.balances" 
            :key="balance.currencyId"
            class="currency-item"
            :class="{ 'is-crypto': isCryptoCurrency(balance.currencyCode) }"
          >
            <div class="currency-header">
              <div class="currency-identity">
                <span class="currency-display">
                  <i v-if="getCurrencyCountryCode(balance.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(balance.currencyCode)}`"></i>
                  <span v-else>{{ balance.currencyCode }}</span>
                </span>
                <div class="currency-info">
                  <span class="currency-code">{{ balance.currencyCode }}</span>
                  <span class="currency-name">{{ balance.currencyName || getCurrencyName(balance.currencyCode) }}</span>
                </div>
              </div>
              <div class="currency-value">
                <span class="balance-amount">{{ formatAmount(balance.balance) }}</span>
                <span class="balance-in-base">{{ formatCurrency(balance.valueInBaseCurrency || 0, 'TRY') }}</span>
              </div>
            </div>
            
            <div class="currency-details">
              <div class="detail-item">
                <span class="detail-label">Kullanılabilir</span>
                <span class="detail-value">{{ formatAmount(balance.availableBalance || balance.balance) }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">Güncel Kur</span>
                <span class="detail-value">{{ formatExchangeRate(balance.exchangeRateToBase) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Balance History Table -->
      <div class="balance-history-section">
        <div class="section-header">
          <h3>Bakiye Geçmişi</h3>
          <div class="time-range-selector">
            <button 
              v-for="range in timeRanges" 
              :key="range.value"
              :class="['range-btn', { active: selectedRange === range.value }]"
              @click="selectedRange = range.value"
            >
              {{ range.label }}
            </button>
          </div>
        </div>
        
        <div class="history-table">
          <table>
            <thead>
              <tr>
                <th>Tarih</th>
                <th>İşlem No</th>
                <th>Tür</th>
                <th>İşlem Detayı</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="transaction in transactions" :key="transaction.id">
                <td>{{ new Date(transaction.transactionDate || '').toLocaleString('tr-TR') }}</td>
                <td class="transaction-number">{{ transaction.transactionNumber }}</td>
                <td>
                  <span :class="['transaction-badge', getTransactionTypeClass(Number(transaction.type))]">
                    {{ getTransactionDescription(transaction) }}
                  </span>
                </td>
                <td>
                  <div class="currency-exchange" v-if="transaction.details && transaction.details.length >= 2">
                    <div class="currency-received">
                      <span class="exchange-sign positive">+</span>
                      <i v-if="getCurrencyCountryCode(transaction.details[1].currencyCode)" :class="`fi fi-${getCurrencyCountryCode(transaction.details[1].currencyCode)}`"></i>
                      <span>{{ formatAmount(transaction.details[1].amount) }}</span>
                      <span class="currency-code">{{ transaction.details[1].currencyCode }}</span>
                    </div>
                    <span class="exchange-separator">⇄</span>
                    <div class="currency-given">
                      <span class="exchange-sign negative">-</span>
                      <i v-if="getCurrencyCountryCode(transaction.details[0].currencyCode)" :class="`fi fi-${getCurrencyCountryCode(transaction.details[0].currencyCode)}`"></i>
                      <span>{{ formatAmount(transaction.details[0].amount) }}</span>
                      <span class="currency-code">{{ transaction.details[0].currencyCode }}</span>
                    </div>
                  </div>
                  <span v-else>-</span>
                </td>
              </tr>
            </tbody>
          </table>
          <div v-if="!transactions.length && !isLoadingHistory" class="empty-history">
            <p>Bu dönem için işlem bulunmuyor.</p>
          </div>
          <div v-if="isLoadingHistory" class="loading-history">
            <div class="spinner-small"></div>
            <span>İşlem geçmişi yükleniyor...</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="loading-state">
      <div class="loading-spinner">
        <div class="spinner-ring"></div>
        <div class="spinner-ring"></div>
        <div class="spinner-ring"></div>
      </div>
      <p>Kasa detayları yükleniyor...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="!vault && !isLoading" class="empty-state">
      <svg width="80" height="80" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M12 2L2 7v10c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V7l-10-5z" stroke="currentColor" stroke-width="1.5" opacity="0.5"/>
        <path d="M12 11v2m0 4h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
      </svg>
      <h3>Kasa Bulunamadı</h3>
      <p>İstenen kasa bilgilerine ulaşılamadı.</p>
      <button @click="refresh" class="retry-button">Tekrar Dene</button>
    </div>

    <!-- Edit Dialog -->
    <Teleport to="body">
      <transition name="dialog-fade">
        <div v-if="showEditDialog && vault" class="dialog-overlay" @click="closeEditDialog">
          <VaultEditDialog
            :vault="vault"
            @close="closeEditDialog"
            @saved="onVaultSaved"
            @click.stop
          />
        </div>
      </transition>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, defineProps, computed, watch } from 'vue'
import apiService from '@/services/apiservice'
import { useDesktopStore } from '@/stores/desktop'
import VaultEditDialog from '@/views/VaultEditDialog.vue'
import type { Vault, Transaction, TransactionHistoryParams } from '@/types/api'
import { getCurrencyCountryCode, getCurrencyName, isCryptoCurrency, formatCurrency, formatAmount, formatExchangeRate } from '@/utils/currency'

const props = defineProps<{
  data?: {
    vaultId?: string
  }
}>()

const desktopStore = useDesktopStore()
const vault = ref<Vault | null>(null)
const isLoading = ref(false)
const showEditDialog = ref(false)
const selectedRange = ref('week')
const transactions = ref<Transaction[]>([])
const isLoadingHistory = ref(false)

// Calculate foreign currency total
const foreignCurrencyTotal = computed(() => {
  if (!vault.value?.balances) return 0
  return vault.value.balances
    .filter(b => b.currencyCode !== 'TRY')
    .reduce((sum, b) => sum + (b.valueInBaseCurrency || 0), 0)
})

const timeRanges = [
  { label: 'Bugün', value: 'today' },
  { label: 'Bu Hafta', value: 'week' },
  { label: 'Bu Ay', value: 'month' },
  { label: 'Bu Yıl', value: 'year' },
  { label: 'Tümü', value: 'all' }
]

// Calculate date ranges
const getDateRange = (range: string) => {
  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  
  switch (range) {
    case 'today':
      return {
        startDate: today.toISOString(),
        endDate: new Date(today.getTime() + 24 * 60 * 60 * 1000).toISOString()
      }
    case 'week':
      const weekStart = new Date(today)
      weekStart.setDate(today.getDate() - today.getDay())
      return {
        startDate: weekStart.toISOString(),
        endDate: new Date().toISOString()
      }
    case 'month':
      return {
        startDate: new Date(now.getFullYear(), now.getMonth(), 1).toISOString(),
        endDate: new Date().toISOString()
      }
    case 'year':
      return {
        startDate: new Date(now.getFullYear(), 0, 1).toISOString(),
        endDate: new Date().toISOString()
      }
    default:
      return {}
  }
}

// Load transaction history
const loadTransactionHistory = async () => {
  if (!vault.value?.vaultId) return
  
  isLoadingHistory.value = true
  try {
    const dateRange = getDateRange(selectedRange.value)
    const params: TransactionHistoryParams = {
      vaultId: vault.value.vaultId,
      page: 1,
      pageSize: 100,
      ...dateRange
    }
    
    const response = await apiService.getTransactionHistory(params)
    transactions.value = response.data || []
  } catch (error) {
    console.error('Failed to load transaction history:', error)
    transactions.value = []
  } finally {
    isLoadingHistory.value = false
  }
}

// Watch for range changes
watch(selectedRange, () => {
  loadTransactionHistory()
})

// Format transaction for display
const getTransactionDisplay = (transaction: Transaction) => {
  const detail = transaction.details?.[0]
  if (!detail) return { currency: '', type: '', amount: 0, description: '' }
  
  return {
    currency: detail.currencyCode,
    type: detail.side === 1 ? 'in' : 'out', // 1 = BUY, 2 = SELL
    amount: detail.amount,
    description: getTransactionDescription(transaction)
  }
}

const getTransactionDescription = (transaction: Transaction) => {
  // TransactionType is an enum with numeric values
  switch (transaction.type) {
    case 1: // Exchange
      return 'Döviz alım/satım'
    case 2: // Deposit
      return 'Para yatırma'
    case 3: // Withdrawal
      return 'Para çekme'
    case 4: // Transfer
      return 'Kasa transferi'
    case 5: // Adjustment
      return 'Düzeltme'
    default:
      return 'İşlem'
  }
}

const getExchangeRate = (transaction: any) => {
  if (!transaction.details || transaction.details.length < 2) return '-'
  
  const fromAmount = transaction.details[1]?.amount ?? 0
  const toAmount = transaction.details[0]?.amount ?? 0

  if (!fromAmount) return '-'

  const rate = toAmount / fromAmount
  return (rate ?? 0).toFixed(2)
}

const getRateTooltip = (transaction: any) => {
  if (!transaction.details || transaction.details.length < 2) return ''
  
  const detail1 = transaction.details[0]
  const detail2 = transaction.details[1]
  
  return `Alış Kuru: ${detail1.rate?.toFixed(4) || '-'}\nSatış Kuru: ${detail2.rate?.toFixed(4) || '-'}`
}

const getTransactionTypeClass = (type: number) => {
  switch (type) {
    case 1: return 'exchange'
    case 2: return 'deposit'
    case 3: return 'withdrawal'
    case 4: return 'transfer'
    case 5: return 'adjustment'
    default: return 'unknown'
  }
}

const getTransactionStatusClass = (status: number) => {
  switch (status) {
    case 1: return 'pending'
    case 2: return 'completed'
    case 3: return 'cancelled'
    case 4: return 'failed'
    default: return 'unknown'
  }
}

const getTransactionStatusText = (status: number) => {
  switch (status) {
    case 1: return 'Beklemede'
    case 2: return 'Tamamlandı'
    case 3: return 'İptal Edildi'
    case 4: return 'Başarısız'
    default: return 'Bilinmiyor'
  }
}

// Actions
const refresh = async () => {
  if (!props.data?.vaultId) return
  
  isLoading.value = true
  try {
    vault.value = await apiService.getVaultById(props.data.vaultId)
    await loadTransactionHistory()
  } catch (error) {
    console.error('Failed to load vault:', error)
  } finally {
    isLoading.value = false
  }
}

const openTransfer = () => {
  desktopStore.openWindowByType('transfer')
}

const openDeposit = () => {
  if (vault.value?.vaultId) {
    desktopStore.openWindowByType('deposit', { vaultId: vault.value.vaultId })
  }
}

const openWithdraw = () => {
  if (vault.value?.vaultId) {
    desktopStore.openWindowByType('withdraw', { vaultId: vault.value.vaultId })
  }
}

const editVault = () => {
  showEditDialog.value = true
}

const closeEditDialog = () => {
  showEditDialog.value = false
}

const onVaultSaved = async (updatedVault: Vault) => {
  vault.value = updatedVault
  await refresh()
}

onMounted(() => {
  if (props.data?.vaultId) {
    refresh()
  }
})
</script>

<style scoped>
.vault-detail-window {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: #f8f9fb;
  position: relative;
  overflow: hidden;
}

/* Premium Header */
.premium-header {
  position: relative;
  background: linear-gradient(135deg, #1a1c3d 0%, #2d3561 100%);
  color: white;
  padding: 2rem 2rem 1.5rem;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
}

.header-gradient {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 40px;
  background: linear-gradient(to bottom, transparent, #f8f9fb);
  pointer-events: none;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 2rem;
  position: relative;
  z-index: 1;
}

.vault-identity {
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.vault-icon-wrapper {
  position: relative;
}

.vault-icon {
  width: 60px;
  height: 60px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);
}

.status-indicator {
  position: absolute;
  bottom: -2px;
  right: -2px;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  border: 3px solid #1a1c3d;
  animation: pulse 2s ease-in-out infinite;
}

.status-indicator.active {
  background: #10b981;
}

.status-indicator.inactive {
  background: #ef4444;
  animation: none;
}

@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.8; transform: scale(1.1); }
}

.vault-title h1 {
  margin: 0;
  font-size: 2rem;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.vault-meta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-top: 0.5rem;
  font-size: 0.9rem;
  opacity: 0.9;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.meta-divider {
  opacity: 0.5;
}

.meta-status {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 500;
  background: rgba(255, 255, 255, 0.2);
}

.meta-status.active {
  background: rgba(16, 185, 129, 0.2);
  color: #86efac;
}

.meta-status.inactive {
  background: rgba(239, 68, 68, 0.2);
  color: #fca5a5;
}

.refresh-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 12px;
  color: white;
  font-size: 0.9rem;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s, border-color 0.2s, transform 0.2s;
}

.refresh-button:hover:not(:disabled) {
  background: rgba(255, 255, 255, 0.2);
  border-color: rgba(255, 255, 255, 0.3);
  transform: translateY(-1px);
}

.refresh-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.rotating {
  animation: rotate 1s linear infinite;
}

@keyframes rotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

/* Content Area */
.vault-content {
  flex: 1;
  overflow-y: auto;
  padding: 2rem;
  padding-top: 1rem;
}

/* Unified Balance Card */
.unified-balance-card {
  background: white;
  border-radius: 20px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
  padding: 2rem;
  margin-bottom: 2rem;
  border: 1px solid rgba(0, 0, 0, 0.05);
}

.balance-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
}

.balance-card-header h2 {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a1c3d;
}

.total-value {
  text-align: right;
}

.total-label {
  display: block;
  font-size: 0.85rem;
  color: #6b7280;
  margin-bottom: 0.25rem;
}

.total-amount {
  display: block;
  font-size: 1.75rem;
  font-weight: 700;
  color: #10b981;
  letter-spacing: -0.02em;
}

.foreign-total {
  display: block;
  font-size: 0.9rem;
  color: #6b7280;
  margin-top: 0.25rem;
  font-weight: 500;
}

.currencies-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1rem;
}

.currency-item {
  background: #f8f9fb;
  border-radius: 12px;
  padding: 1.25rem;
  transition: background-color 0.2s, border-color 0.2s, box-shadow 0.2s, transform 0.2s;
  border: 1px solid transparent;
}

.currency-item:hover {
  background: white;
  border-color: #e5e7eb;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  transform: translateY(-2px);
}

.currency-item.is-crypto {
  background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
}

.currency-item.is-crypto:hover {
  background: linear-gradient(135deg, #fde68a 0%, #fbbf24 100%);
}

.currency-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
}

.currency-identity {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.currency-display {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 40px;
  height: 40px;
  padding: 0 0.5rem;
  background: white;
  border-radius: 10px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  font-weight: 600;
  font-size: 0.9rem;
  color: #1a1c3d;
}

.currency-display .fi {
  font-size: 1.5em;
  margin: 0;
}

.currency-cell .fi {
  font-size: 1.2em;
  margin-right: 0.5rem;
}

.is-crypto .currency-display {
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  color: white;
  font-weight: 700;
}

.currency-info {
  display: flex;
  flex-direction: column;
}

.currency-code {
  font-size: 1.1rem;
  font-weight: 700;
  color: #1a1c3d;
}

.currency-name {
  font-size: 0.85rem;
  color: #6b7280;
  margin-top: 0.1rem;
}

.currency-value {
  text-align: right;
}

.balance-amount {
  display: block;
  font-size: 1.25rem;
  font-weight: 700;
  color: #1a1c3d;
}

.balance-in-base {
  display: block;
  font-size: 0.85rem;
  color: #6b7280;
  margin-top: 0.25rem;
}

.currency-details {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
  padding-top: 1rem;
  border-top: 1px solid rgba(0, 0, 0, 0.05);
}

.detail-item {
  display: flex;
  flex-direction: column;
}

.detail-label {
  font-size: 0.75rem;
  color: #9ca3af;
  margin-bottom: 0.25rem;
}

.detail-value {
  font-size: 0.9rem;
  font-weight: 600;
  color: #4b5563;
}

/* Balance History Section */
.balance-history-section {
  background: white;
  border-radius: 20px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
  padding: 2rem;
  margin-bottom: 2rem;
  border: 1px solid rgba(0, 0, 0, 0.05);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.section-header h3 {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 700;
  color: #1a1c3d;
}

.time-range-selector {
  display: flex;
  gap: 0.5rem;
  background: #f3f4f6;
  padding: 0.25rem;
  border-radius: 10px;
}

.range-btn {
  padding: 0.5rem 1rem;
  border: none;
  background: transparent;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 500;
  color: #6b7280;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s;
}

.range-btn:hover {
  color: #1a1c3d;
}

.range-btn.active {
  background: white;
  color: #1a1c3d;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

/* History Table */
.history-table {
  overflow-x: auto;
}

.history-table table {
  width: 100%;
  border-collapse: collapse;
}

.history-table th,
.history-table td {
  padding: 0.875rem;
  text-align: left;
  border-bottom: 1px solid #e5e7eb;
}

.history-table th {
  background: #f8f9fb;
  font-weight: 600;
  color: #6b7280;
  font-size: 0.85rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.history-table tbody tr:hover {
  background: #f8f9fb;
}

.currency-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.currency-code-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 2.5em;
  padding: 0.2em 0.4em;
  background: #f0f0f0;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.8em;
  font-weight: 600;
  color: #333;
}

.currency-cell .fi {
  font-size: 1.2em;
  margin-right: 0.25rem;
}

.currency-identity .fi {
  font-size: 1.8em;
}

.transaction-type {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 500;
}

.transaction-type.in {
  background: #d1e7dd;
  color: #0f5132;
}

.transaction-type.out {
  background: #f8d7da;
  color: #842029;
}

.amount-in {
  color: #10b981;
  font-weight: 600;
}

.amount-out {
  color: #ef4444;
  font-weight: 600;
}

/* Currency exchange display */
.currency-exchange {
  display: flex;
  align-items: center;
  gap: 1rem;
  font-weight: 500;
}

.currency-received,
.currency-given {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.3rem 0.6rem;
  border-radius: 6px;
}

.currency-received {
  background-color: #d1fae5 !important;
  border: 1px solid #6ee7b7 !important;
  color: #059669 !important;
}

.currency-given {
  background-color: #fee2e2 !important;
  border: 1px solid #fca5a5 !important;
  color: #dc2626 !important;
}

.exchange-sign {
  font-weight: bold;
  font-size: 1.1em;
}

.exchange-sign.positive {
  color: #059669 !important;
}

.exchange-sign.negative {
  color: #dc2626 !important;
}

.currency-received i,
.currency-given i {
  font-size: 1.2em;
  margin: 0 0.2rem;
}

.currency-code {
  font-weight: 600;
  margin-left: 0.2rem;
}

.exchange-separator {
  color: #9ca3af;
  font-size: 1.2em;
}

/* Rate cell styles */
.rate-cell {
  text-align: center;
}

.rate-display {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  cursor: help;
}

.rate-value {
  font-weight: 500;
  color: #1a1c3d;
  text-decoration: underline;
  text-decoration-style: dotted;
  text-underline-offset: 2px;
}

.custom-rate-indicator {
  color: #f59e0b;
  font-size: 1.1em;
  cursor: help;
}

.balance-cell {
  font-weight: 600;
  color: #1a1c3d;
}

.description-cell {
  color: #6b7280;
  font-size: 0.9rem;
}

.transaction-number {
  font-family: monospace;
  font-size: 0.85rem;
  color: #6b7280;
}

.transaction-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 500;
  display: inline-block;
}

.transaction-badge.exchange {
  background: #e0e7ff;
  color: #4338ca;
}

.transaction-badge.deposit {
  background: #d1fae5;
  color: #065f46;
}

.transaction-badge.withdrawal {
  background: #fee2e2;
  color: #991b1b;
}

.transaction-badge.transfer {
  background: #f3e8ff;
  color: #6b21a8;
}

.transaction-badge.adjustment {
  background: #fef3c7;
  color: #92400e;
}

.transaction-badge.unknown {
  background: #f3f4f6;
  color: #6b7280;
}

.amount-cell {
  font-weight: 600;
}

.status-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 500;
  display: inline-block;
}

.status-badge.completed {
  background: #d1fae5;
  color: #065f46;
}

.status-badge.pending {
  background: #fef3c7;
  color: #92400e;
}

.status-badge.cancelled {
  background: #f3f4f6;
  color: #6b7280;
}

.status-badge.failed {
  background: #fee2e2;
  color: #991b1b;
}

.status-badge.unknown {
  background: #e5e7eb;
  color: #4b5563;
}

.loading-history {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 2rem;
  color: #6b7280;
}

.spinner-small {
  width: 20px;
  height: 20px;
  border: 2px solid #e5e7eb;
  border-top-color: #3b82f6;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.empty-history {
  text-align: center;
  padding: 3rem;
  color: #9ca3af;
}

/* Quick Actions */
.quick-actions {
  margin-bottom: 2rem;
  background: white;
  border-radius: 20px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
  padding: 2rem;
  border: 1px solid rgba(0, 0, 0, 0.05);
}

.quick-actions h3 {
  margin: 0 0 1rem;
  font-size: 1.25rem;
  font-weight: 700;
  color: #1a1c3d;
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.action-card {
  background: white;
  border-radius: 16px;
  padding: 1.5rem;
  border: 2px solid transparent;
  cursor: pointer;
  transition: box-shadow 0.2s, transform 0.2s;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.75rem;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.05);
}

.action-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
}

.action-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  transition: background-color 0.2s, transform 0.2s;
}

.action-card.transfer .action-icon {
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
  color: white;
}

.action-card.deposit .action-icon {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: white;
}

.action-card.withdraw .action-icon {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: white;
}

.action-card.edit .action-icon {
  background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
  color: white;
}

.action-card:hover .action-icon {
  transform: scale(1.1);
}

.action-label {
  font-size: 1.1rem;
  font-weight: 600;
  color: #1a1c3d;
}

.action-description {
  font-size: 0.85rem;
  color: #6b7280;
}

/* Loading State */
.loading-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1.5rem;
}

.loading-spinner {
  position: relative;
  width: 60px;
  height: 60px;
}

.spinner-ring {
  position: absolute;
  width: 100%;
  height: 100%;
  border: 3px solid transparent;
  border-top-color: #3b82f6;
  border-radius: 50%;
  animation: spin 1.2s cubic-bezier(0.5, 0, 0.5, 1) infinite;
}

.spinner-ring:nth-child(2) {
  animation-delay: -0.3s;
  border-top-color: #10b981;
}

.spinner-ring:nth-child(3) {
  animation-delay: -0.6s;
  border-top-color: #f59e0b;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.loading-state p {
  color: #6b7280;
  font-size: 1rem;
}

/* Empty State */
.empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 3rem;
  gap: 1rem;
}

.empty-state svg {
  color: #d1d5db;
}

.empty-state h3 {
  margin: 0;
  font-size: 1.5rem;
  color: #4b5563;
}

.empty-state p {
  color: #9ca3af;
  margin: 0;
}

.retry-button {
  margin-top: 1rem;
  padding: 0.75rem 1.5rem;
  background: #3b82f6;
  color: white;
  border: none;
  border-radius: 10px;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s, transform 0.2s;
}

.retry-button:hover {
  background: #2563eb;
  transform: translateY(-1px);
}

/* Dialog Overlay */
.dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 2rem;
}

/* Dialog Transition */
.dialog-fade-enter-active,
.dialog-fade-leave-active {
  transition: opacity 0.3s ease;
}

.dialog-fade-enter-from,
.dialog-fade-leave-to {
  opacity: 0;
}

/* Responsive */
@media (max-width: 768px) {
  .vault-content {
    padding: 1rem;
  }
  
  .currencies-grid {
    grid-template-columns: 1fr;
  }
  
  .actions-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  
  .chart-stats {
    grid-template-columns: 1fr;
  }
}
</style>