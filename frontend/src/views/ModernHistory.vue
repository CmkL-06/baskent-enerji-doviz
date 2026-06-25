<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useExchangeStore } from '@/stores/exchange'
import type { Transaction, TransactionType, TransactionStatus } from '@/types/api'
import EditTransactionModal from '@/components/EditTransactionModal.vue'
import DeleteConfirmModal from '@/components/DeleteConfirmModal.vue'
import apiService from '@/services/apiservice'

const exchangeStore = useExchangeStore()

// Filter options
const filters = ref({
  all: true,
  dateFrom: '',
  dateTo: '',
  search: '',
  officeId: null as string | null,
  transactionType: null as TransactionType | null
})

const isLoading = ref(false)
const currentPage = ref(1)
const pageSize = ref(50)
const totalTransactions = ref(0)

// Computed property for formatted transactions
const transactions = computed(() => {
  return exchangeStore.transactions.map(t => ({
    ...t,
    formattedDate: new Date(t.transactionDate).toLocaleString('tr-TR'),
    formattedAmount: `${t.sourceCurrencyCode} ${t.sourceAmount.toFixed(2)}`,
    formattedRate: `₺${t.exchangeRate.toFixed(4)}`,
    formattedTotal: `₺${t.targetAmount.toFixed(2)}`,
    hasSpecialRate: t.customRate !== null
  }))
})

const currencyColors: Record<string, string> = {
  USD: '#ff8c5a',
  EUR: '#5a8cff',
  GBP: '#5ac8ff',
  RUB: '#b45aff',
  SEK: '#5aff8c',
  DKK: '#ff5a8c',
  CHF: '#5affc8',
  AUD: '#ffc85a',
  CAD: '#5a8cff',
  UAH: '#b45aff'
}

// Modal states
const showEditModal = ref(false)
const showDeleteModal = ref(false)
const selectedTransaction = ref<any>(null)

const getCurrencyColor = (currency: string) => {
  return currencyColors[currency] || '#888888'
}

const getStatusText = (status: TransactionStatus) => {
  const statusMap: Record<TransactionStatus, string> = {
    0: 'Bekliyor',
    1: 'Onaylandı',
    2: 'İptal',
    3: 'Başarısız'
  }
  return statusMap[status] || 'Bilinmiyor'
}

// Load transactions from API
const loadTransactions = async () => {
  isLoading.value = true
  try {
    const params = {
      pageNumber: currentPage.value,
      pageSize: pageSize.value,
      officeId: filters.value.officeId,
      transactionType: filters.value.transactionType,
      startDate: filters.value.dateFrom ? new Date(filters.value.dateFrom).toISOString() : undefined,
      endDate: filters.value.dateTo ? new Date(filters.value.dateTo).toISOString() : undefined,
      searchTerm: filters.value.search || undefined
    }
    
    const response = await exchangeStore.loadTransactionHistory(params)
    totalTransactions.value = response?.totalCount || 0
  } catch (error) {
    console.error('Failed to load transactions:', error)
  } finally {
    isLoading.value = false
  }
}

const applyFilters = () => {
  currentPage.value = 1
  loadTransactions()
}

const editTransaction = (transaction: any) => {
  selectedTransaction.value = transaction
  showEditModal.value = true
}

const deleteTransaction = (transaction: any) => {
  selectedTransaction.value = transaction
  showDeleteModal.value = true
}

const handleEditSave = async (_data: any) => {
  showEditModal.value = false
  await loadTransactions()
}

const handleDeleteConfirm = async (reason: string) => {
  if (!selectedTransaction.value) return
  try {
    const id = selectedTransaction.value.transactionId ?? selectedTransaction.value.id
    await apiService.removeTransaction(id)
    showDeleteModal.value = false
    await loadTransactions()
  } catch (error) {
    console.error('İşlem silinemedi:', error)
  }
}

// Load offices and initialize
onMounted(async () => {
  await exchangeStore.loadOffices()
  await loadTransactions()
})
</script>

<template>
  <div class="modern-history">
    <!-- Filter Section -->
    <div class="filter-section">
      <h2>HIZLI İŞLEMLER</h2>
      <div class="filter-controls">
        <select 
          v-model="filters.officeId" 
          class="filter-select"
        >
          <option :value="null">Tüm Ofisler</option>
          <option v-for="office in exchangeStore.offices" :key="office.officeId" :value="office.officeId">
            {{ office.officeName }}
          </option>
        </select>
        <input 
          v-model="filters.search" 
          type="text" 
          class="filter-input"
          placeholder="Ara..."
        >
        <div class="filter-buttons">
          <button class="filter-btn" :class="{ active: filters.all }" @click="filters.all = !filters.all">
            TÜMÜ
          </button>
          <input 
            v-model="filters.dateFrom" 
            type="date" 
            class="date-input"
            placeholder="Tarih 1"
          >
          <input 
            v-model="filters.dateTo" 
            type="date" 
            class="date-input"
            placeholder="Tarih 2"
          >
          <button class="apply-btn" @click="applyFilters" :disabled="isLoading">
            <span v-if="isLoading">⏳</span>
            <span v-else>GETİR</span>
          </button>
        </div>
      </div>
    </div>
    
    <!-- Transaction Table -->
    <div class="transaction-table-container">
      <table class="transaction-table">
        <thead>
          <tr>
            <th>DÖVİZ</th>
            <th>TARİH</th>
            <th>OFİS ADI</th>
            <th>İŞLEM NO</th>
            <th>TUTAR</th>
            <th>KUR</th>
            <th>ÖDENEN TL</th>
            <th>ÖZEL KUR</th>
            <th>DURUM</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="isLoading">
            <td colspan="10" class="loading-cell">
              <div class="loading-spinner"></div>
              Yükleniyor...
            </td>
          </tr>
          <tr v-else-if="transactions.length === 0">
            <td colspan="10" class="empty-cell">
              İşlem bulunamadı
            </td>
          </tr>
          <tr v-else v-for="(transaction, index) in transactions" :key="transaction.id">
            <td>
              <div class="currency-badge" :style="{ background: getCurrencyColor(transaction.sourceCurrencyCode) }">
                {{ transaction.sourceCurrencyCode }}
              </div>
            </td>
            <td>{{ transaction.formattedDate }}</td>
            <td>{{ transaction.officeName }}</td>
            <td>{{ transaction.transactionReferenceNo }}</td>
            <td class="amount">{{ transaction.formattedAmount }}</td>
            <td>{{ transaction.formattedRate }}</td>
            <td class="total">{{ transaction.formattedTotal }}</td>
            <td>
              <span v-if="transaction.hasSpecialRate" class="special-rate">✓</span>
            </td>
            <td>
              <span :class="['status-badge', `status-${transaction.transactionStatus}`]">
                {{ getStatusText(transaction.transactionStatus) }}
              </span>
            </td>
            <td class="actions">
              <button class="action-btn edit" @click="editTransaction(transaction)" title="Düzenle [*]">📝</button>
              <button class="action-btn delete" @click="deleteTransaction(transaction)" title="Sil [*]">❌</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    
    <!-- Modals -->
    <EditTransactionModal 
      :visible="showEditModal"
      :transaction="selectedTransaction"
      @close="showEditModal = false"
      @save="handleEditSave"
    />
    
    <DeleteConfirmModal
      :visible="showDeleteModal"
      @close="showDeleteModal = false"
      @confirm="handleDeleteConfirm"
    />
  </div>
</template>

<style scoped>
.modern-history {
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

/* Filter Section */
.filter-section {
  background: white;
  border-radius: 15px;
  padding: 20px;
  border: 1px solid #e5e7eb;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.filter-section h2 {
  color: #1a1a1a;
  font-size: 16px;
  margin-bottom: 15px;
}

.filter-controls {
  display: flex;
  gap: 10px;
  align-items: center;
}

.filter-input {
  flex: 1;
  padding: 10px 15px;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  color: #1a1a1a;
  font-size: 14px;
}

.filter-input::placeholder {
  color: #9ca3af;
}

.filter-buttons {
  display: flex;
  gap: 10px;
}

.filter-btn {
  padding: 10px 20px;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  color: #1a1a1a;
  cursor: pointer;
  transition: all 0.3s ease;
}

.filter-btn.active {
  background: #6b46c1;
  color: white;
  border-color: #6b46c1;
}

.date-input {
  padding: 10px 15px;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  color: #1a1a1a;
  font-size: 14px;
}

.apply-btn {
  padding: 10px 30px;
  background: #5a8cff;
  border: none;
  border-radius: 8px;
  color: white;
  font-weight: bold;
  cursor: pointer;
  transition: all 0.3s ease;
}

.apply-btn:hover {
  background: #4a7ce5;
  transform: translateY(-2px);
}

/* Transaction Table */
.transaction-table-container {
  flex: 1;
  background: white;
  border-radius: 15px;
  overflow: hidden;
  border: 1px solid #e5e7eb;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.transaction-table {
  width: 100%;
  border-collapse: collapse;
}

.transaction-table thead {
  background: #f9fafb;
}

.transaction-table th {
  padding: 15px;
  text-align: left;
  color: #1a1a1a;
  font-size: 12px;
  font-weight: 600;
  border-bottom: 2px solid #e5e7eb;
}

.transaction-table tbody tr {
  border-bottom: 1px solid #e5e7eb;
  transition: background 0.3s ease;
}

.transaction-table tbody tr:hover {
  background: #f9fafb;
}

.transaction-table td {
  padding: 15px;
  color: #1a1a1a;
  font-size: 14px;
}

.currency-badge {
  display: inline-block;
  padding: 5px 15px;
  border-radius: 5px;
  color: white;
  font-weight: bold;
  font-size: 12px;
}

.amount {
  font-weight: 600;
}

.total {
  font-weight: 600;
  color: #5aff8c;
}

.special-rate {
  color: #5aff8c;
  font-size: 18px;
}

.actions {
  display: flex;
  gap: 5px;
}

.action-btn {
  background: none;
  border: none;
  font-size: 16px;
  cursor: pointer;
  padding: 5px;
  border-radius: 5px;
  transition: all 0.3s ease;
}

.action-btn:hover {
  background: rgba(255, 255, 255, 0.1);
}

.action-btn.edit {
  color: #5ac8ff;
}

.action-btn.delete {
  color: #ff5a8c;
}

/* Filter select */
.filter-select {
  padding: 10px 15px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 8px;
  color: white;
  font-size: 14px;
  min-width: 150px;
}

.filter-select option {
  background: #2a1a4e;
  color: white;
}

/* Loading and empty states */
.loading-cell,
.empty-cell {
  text-align: center;
  padding: 40px;
  color: rgba(255, 255, 255, 0.6);
}

.loading-spinner {
  display: inline-block;
  width: 20px;
  height: 20px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-right: 10px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Status badges */
.status-badge {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
}

.status-badge.status-0 { /* Pending */
  background: rgba(255, 193, 7, 0.2);
  color: #ffc107;
}

.status-badge.status-1 { /* Completed */
  background: rgba(40, 167, 69, 0.2);
  color: #28a745;
}

.status-badge.status-2 { /* Cancelled */
  background: rgba(220, 53, 69, 0.2);
  color: #dc3545;
}

.status-badge.status-3 { /* Failed */
  background: rgba(255, 90, 140, 0.2);
  color: #ff5a8c;
}
</style>