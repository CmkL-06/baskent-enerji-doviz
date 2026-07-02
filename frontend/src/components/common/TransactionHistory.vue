<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import apiService from '@/services/apiservice'
import { useNotification } from '@/composables/useNotification'
import { useAuthStore } from '@/stores/auth'
import { getCurrencyCountryCode } from '@/utils/currency'

const props = withDefaults(defineProps<{
  officeId: string
  pageSize?: number
  showActions?: boolean
  showRefreshButton?: boolean
  selectedDate?: string
}>(), {
  pageSize: 20,
  showActions: true,
  showRefreshButton: true,
  selectedDate: ''
})

const notification = useNotification()
const authStore = useAuthStore()

const transactions = ref<any[]>([])
const isLoading = ref(false)
const currentPage = ref(1)
const totalPages = ref(1)
const totalCount = ref(0)
const filterType = ref<string>('')
const deleteConfirmId = ref<string | null>(null)
const isDeleting = ref(false)

const formatNumber = (value: number, decimals = 2) => {
  return new Intl.NumberFormat('tr-TR', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals
  }).format(value)
}

const formatTime = (dateStr: string) => {
  const d = new Date(dateStr)
  return d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}

const formatDate = (dateStr: string) => {
  const d = new Date(dateStr)
  return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit' })
}

const getFlagClass = (code: string) => {
  const cc = getCurrencyCountryCode(code)
  return cc ? `fi fi-${cc}` : ''
}

async function loadTransactions() {
  if (!props.officeId) return
  isLoading.value = true
  try {
    const params: any = {
      officeId: props.officeId,
      page: currentPage.value,
      pageSize: props.pageSize
    }
    if (props.selectedDate) {
      params.startDate = props.selectedDate + 'T00:00:00'
      params.endDate = props.selectedDate + 'T23:59:59'
    }
    if (filterType.value) {
      params.type = filterType.value === 'buy' ? 0 : 1
    }

    const result = await apiService.getTransactionHistory(params)
    if (result && result.data) {
      transactions.value = result.data
      totalPages.value = result.pagination?.totalPages || 1
      totalCount.value = result.pagination?.totalCount || 0
    } else {
      transactions.value = []
    }
  } catch (err) {
    console.error('Failed to load transactions:', err)
    transactions.value = []
  } finally {
    isLoading.value = false
  }
}

async function deleteTransaction(id: string) {
  isDeleting.value = true
  try {
    await apiService.removeTransaction(id)
    notification.success('İşlem başarıyla silindi')
    deleteConfirmId.value = null
    await loadTransactions()
  } catch (err: any) {
    notification.error(`Silme hatası: ${err.response?.data?.message || err.message}`)
  } finally {
    isDeleting.value = false
  }
}

function printAllTransactions() {
  if (transactions.value.length === 0) {
    notification.warning('Yazdırılacak işlem bulunamadı')
    return
  }
  const printWindow = window.open('', '_blank', 'width=900,height=700')
  if (!printWindow) return

  let rows = ''
  transactions.value.forEach(tx => {
    const details = tx.details || []
    const source = details.find((d: any) => d.side === 'Source' || d.side === 0)
    const target = details.find((d: any) => d.side === 'Target' || d.side === 1)
    rows += `<tr>
      <td style="padding:8px;border-bottom:1px solid #eee;font-size:12px">${formatTime(tx.transactionDate)}</td>
      <td style="padding:8px;border-bottom:1px solid #eee">${tx.type === 0 || tx.type === 'Buy' ? 'Alış' : 'Satış'}</td>
      <td style="padding:8px;border-bottom:1px solid #eee;font-family:monospace">${source ? formatNumber(source.amount) + ' ' + source.currencyCode : '-'}</td>
      <td style="padding:8px;border-bottom:1px solid #eee;font-family:monospace">${source?.rate ? formatNumber(source.rate, 4) : '-'}</td>
      <td style="padding:8px;border-bottom:1px solid #eee;font-family:monospace">${target ? formatNumber(target.amount) + ' ' + target.currencyCode : '-'}</td>
      <td style="padding:8px;border-bottom:1px solid #eee;font-family:monospace">${tx.profit ? formatNumber(tx.profit) : '-'}</td>
    </tr>`
  })

  printWindow.document.write(`<!DOCTYPE html><html><head><title>İşlem Geçmişi</title>
    <style>body{font-family:'Segoe UI',sans-serif;padding:30px}h2{text-align:center;margin-bottom:20px}
    table{width:100%;border-collapse:collapse}th{text-align:left;padding:10px 8px;border-bottom:2px solid #333;font-size:13px}
    @media print{body{padding:10px}}</style></head><body>
    <h2>İşlem Geçmişi — ${props.selectedDate || new Date().toLocaleDateString('tr-TR')}</h2>
    <table><thead><tr><th>Saat</th><th>Tür</th><th>Kaynak</th><th>Kur</th><th>Hedef</th><th>Kar</th></tr></thead>
    <tbody>${rows}</tbody></table></body></html>`)
  printWindow.document.close()
  setTimeout(() => { printWindow.print(); printWindow.close() }, 300)
}

function changePage(page: number) {
  if (page < 1 || page > totalPages.value) return
  currentPage.value = page
  loadTransactions()
}

watch(() => props.officeId, () => {
  currentPage.value = 1
  loadTransactions()
})

watch(filterType, () => {
  currentPage.value = 1
  loadTransactions()
})

onMounted(() => loadTransactions())

defineExpose({ loadTransactions, printAllTransactions })
</script>

<template>
  <div class="th-root">
    <!-- Header -->
    <div class="th-header">
      <div class="th-header-left">
        <span class="material-symbols-outlined th-icon-filled" style="font-size:20px;color:#6366f1">history</span>
        <h3 class="th-title">İşlem Geçmişi</h3>
        <span v-if="totalCount > 0" class="th-count">{{ totalCount }}</span>
      </div>
      <div class="th-header-right">
        <div class="th-filter-group">
          <button @click="filterType = ''" class="th-filter-btn" :class="{ 'th-filter-btn--active': filterType === '' }">Tümü</button>
          <button @click="filterType = 'buy'" class="th-filter-btn" :class="{ 'th-filter-btn--active': filterType === 'buy' }">Alış</button>
          <button @click="filterType = 'sell'" class="th-filter-btn" :class="{ 'th-filter-btn--active': filterType === 'sell' }">Satış</button>
        </div>
        <button v-if="showRefreshButton" @click="loadTransactions" :disabled="isLoading" class="th-action-btn" title="Yenile">
          <span class="material-symbols-outlined" :class="{ 'th-spin': isLoading }">sync</span>
        </button>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading && transactions.length === 0" class="th-loading">
      <span class="material-symbols-outlined th-spin" style="font-size:24px;color:#6366f1">refresh</span>
      <span>Yükleniyor...</span>
    </div>

    <!-- Empty -->
    <div v-else-if="transactions.length === 0" class="th-empty">
      <span class="material-symbols-outlined" style="font-size:36px;color:#d1d5db">receipt_long</span>
      <p>Bugün henüz işlem yapılmamış</p>
    </div>

    <!-- Table -->
    <div v-else class="th-table-wrap">
      <table class="th-table">
        <thead>
          <tr>
            <th class="th-col-time">Saat</th>
            <th class="th-col-type">Tür</th>
            <th>Kaynak</th>
            <th class="th-col-rate">Kur</th>
            <th>Hedef</th>
            <th class="th-col-profit">Kar</th>
            <th v-if="showActions" class="th-col-actions"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="tx in transactions" :key="tx.id" class="th-row" :class="{ 'th-row--deleting': deleteConfirmId === tx.id }">
            <td class="th-cell-time">{{ formatDate(tx.transactionDate) }} {{ formatTime(tx.transactionDate) }}</td>
            <td>
              <span class="th-type-badge" :class="tx.type === 0 || tx.type === 'Buy' ? 'th-type-badge--buy' : 'th-type-badge--sell'">
                {{ tx.type === 0 || tx.type === 'Buy' ? 'Alış' : 'Satış' }}
              </span>
            </td>
            <td>
              <div v-for="d in (tx.details || []).filter((d: any) => d.side === 'Source' || d.side === 0)" :key="d.currencyId" class="th-currency-cell">
                <i v-if="getFlagClass(d.currencyCode)" :class="getFlagClass(d.currencyCode)" class="th-flag"></i>
                <span class="th-amount">{{ formatNumber(d.amount) }}</span>
                <span class="th-code">{{ d.currencyCode }}</span>
              </div>
            </td>
            <td class="th-cell-rate">
              <template v-for="d in (tx.details || []).filter((d: any) => d.side === 'Source' || d.side === 0)" :key="d.currencyId">
                <span class="th-rate">{{ d.rate ? formatNumber(d.rate, 4) : '-' }}</span>
                <span v-if="tx.isCustomRate" class="th-custom-badge">Ö</span>
              </template>
            </td>
            <td>
              <div v-for="d in (tx.details || []).filter((d: any) => d.side === 'Target' || d.side === 1)" :key="d.currencyId" class="th-currency-cell">
                <i v-if="getFlagClass(d.currencyCode)" :class="getFlagClass(d.currencyCode)" class="th-flag"></i>
                <span class="th-amount">{{ formatNumber(d.amount) }}</span>
                <span class="th-code">{{ d.currencyCode }}</span>
              </div>
            </td>
            <td class="th-cell-profit">
              <span v-if="tx.profit" :class="tx.profit > 0 ? 'th-profit--pos' : 'th-profit--neg'">
                {{ tx.profit > 0 ? '+' : '' }}{{ formatNumber(tx.profit) }}
              </span>
              <span v-else class="th-profit--zero">-</span>
            </td>
            <td v-if="showActions" class="th-cell-actions">
              <template v-if="deleteConfirmId === tx.id">
                <button @click="deleteTransaction(tx.id)" :disabled="isDeleting" class="th-del-confirm">
                  <span class="material-symbols-outlined" style="font-size:16px">{{ isDeleting ? 'refresh' : 'check' }}</span>
                  Sil
                </button>
                <button @click="deleteConfirmId = null" class="th-del-cancel">İptal</button>
              </template>
              <button v-else-if="authStore.isAdmin || authStore.isOwner" @click="deleteConfirmId = tx.id" class="th-del-btn" title="İşlemi Sil">
                <span class="material-symbols-outlined" style="font-size:16px">delete</span>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="th-pagination">
      <button @click="changePage(currentPage - 1)" :disabled="currentPage <= 1" class="th-page-btn">
        <span class="material-symbols-outlined" style="font-size:18px">chevron_left</span>
      </button>
      <span class="th-page-info">{{ currentPage }} / {{ totalPages }}</span>
      <button @click="changePage(currentPage + 1)" :disabled="currentPage >= totalPages" class="th-page-btn">
        <span class="material-symbols-outlined" style="font-size:18px">chevron_right</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.th-root {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 14px;
  overflow: hidden;
}

.th-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 20px;
  border-bottom: 1px solid #f3f4f6;
  flex-wrap: wrap;
  gap: 10px;
}
.th-header-left {
  display: flex;
  align-items: center;
  gap: 8px;
}
.th-title {
  font-size: 15px;
  font-weight: 600;
  color: #111827;
}
.th-count {
  font-size: 11px;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 10px;
  background: #ede9fe;
  color: #6366f1;
}
.th-header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.th-filter-group {
  display: flex;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  overflow: hidden;
}
.th-filter-btn {
  padding: 5px 12px;
  border: none;
  background: white;
  font-size: 12px;
  font-weight: 500;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.15s;
}
.th-filter-btn:not(:last-child) { border-right: 1px solid #e5e7eb; }
.th-filter-btn:hover { background: #f9fafb; }
.th-filter-btn--active {
  background: #6366f1;
  color: white;
}

.th-action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: white;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.15s;
}
.th-action-btn:hover:not(:disabled) { border-color: #6366f1; color: #6366f1; }
.th-action-btn:disabled { opacity: 0.4; }

.th-loading, .th-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 40px 20px;
  color: #9ca3af;
  font-size: 14px;
}

.th-table-wrap {
  overflow-x: auto;
}
.th-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}
.th-table thead th {
  text-align: left;
  padding: 10px 12px;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: #9ca3af;
  background: #fafafa;
  border-bottom: 1px solid #e5e7eb;
  white-space: nowrap;
}
.th-col-time { width: 110px; }
.th-col-type { width: 60px; }
.th-col-rate { width: 90px; }
.th-col-profit { width: 90px; }
.th-col-actions { width: 80px; }

.th-row td {
  padding: 10px 12px;
  border-bottom: 1px solid #f3f4f6;
  vertical-align: middle;
}
.th-row:hover { background: #fafafe; }
.th-row--deleting { background: #fef2f2; }
.th-row:last-child td { border-bottom: none; }

.th-cell-time {
  font-size: 12px;
  color: #6b7280;
  white-space: nowrap;
}

.th-type-badge {
  display: inline-block;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.3px;
  padding: 3px 8px;
  border-radius: 6px;
}
.th-type-badge--buy { background: #dcfce7; color: #15803d; }
.th-type-badge--sell { background: #fee2e2; color: #b91c1c; }

.th-currency-cell {
  display: flex;
  align-items: center;
  gap: 6px;
}
.th-flag { font-size: 14px; }
.th-amount {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-weight: 600;
  color: #111827;
}
.th-code {
  font-size: 11px;
  color: #6b7280;
  font-weight: 500;
}

.th-cell-rate {
  display: flex;
  align-items: center;
  gap: 4px;
}
.th-rate {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  color: #374151;
}
.th-custom-badge {
  font-size: 9px;
  font-weight: 700;
  padding: 1px 4px;
  border-radius: 3px;
  background: #fef3c7;
  color: #92400e;
}

.th-cell-profit { text-align: right; }
.th-profit--pos {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-weight: 600;
  color: #16a34a;
}
.th-profit--neg {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-weight: 600;
  color: #dc2626;
}
.th-profit--zero { color: #d1d5db; }

.th-cell-actions {
  text-align: center;
  white-space: nowrap;
}
.th-del-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: #9ca3af;
  cursor: pointer;
  transition: all 0.15s;
}
.th-del-btn:hover { background: #fee2e2; color: #ef4444; }
.th-del-confirm {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  padding: 3px 8px;
  border: none;
  border-radius: 5px;
  background: #ef4444;
  color: white;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
}
.th-del-cancel {
  padding: 3px 8px;
  border: none;
  border-radius: 5px;
  background: #f3f4f6;
  color: #6b7280;
  font-size: 11px;
  cursor: pointer;
  margin-left: 4px;
}

.th-pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 12px;
  border-top: 1px solid #f3f4f6;
}
.th-page-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: white;
  color: #374151;
  cursor: pointer;
  transition: all 0.15s;
}
.th-page-btn:hover:not(:disabled) { border-color: #6366f1; color: #6366f1; }
.th-page-btn:disabled { opacity: 0.3; cursor: not-allowed; }
.th-page-info {
  font-size: 13px;
  color: #6b7280;
  font-weight: 500;
}

.material-symbols-outlined {
  font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
.th-icon-filled {
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
.th-spin { animation: spin 1s linear infinite; }

@media (max-width: 640px) {
  .th-table { font-size: 12px; }
  .th-table thead th { padding: 8px; }
  .th-row td { padding: 8px; }
  .th-col-profit, .th-col-actions { display: none; }
  .th-cell-profit, .th-cell-actions { display: none; }
}
</style>
