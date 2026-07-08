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
const selectedTx = ref<any>(null)

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

const formatFullDate = (dateStr: string) => {
  const d = new Date(dateStr)
  return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' }) + ' ' +
         d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

const getFlagClass = (code: string) => {
  const cc = getCurrencyCountryCode(code)
  return cc ? `fi fi-${cc}` : ''
}

const txTypeLabel = (tx: any) => {
  return tx.type === 0 || tx.type === 'Buy' ? 'Alış' : 'Satış'
}

const isBuy = (tx: any) => tx.type === 0 || tx.type === 'Buy'

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
    selectedTx.value = null
    await loadTransactions()
  } catch (err: any) {
    notification.error(`Silme hatası: ${err.response?.data?.message || err.message}`)
  } finally {
    isDeleting.value = false
  }
}

function openDetail(tx: any) {
  selectedTx.value = tx
}

function closeDetail() {
  selectedTx.value = null
}

function printReceipt(tx: any) {
  const printWindow = window.open('', '_blank', 'width=400,height=600')
  if (!printWindow) return

  const typeLabel = txTypeLabel(tx)
  const source = tx.sourceCurrencyCode || ''
  const target = tx.targetCurrencyCode || ''
  const rate = tx.exchangeRate || 0

  printWindow.document.write(`<!DOCTYPE html><html><head><title>Fiş — ${tx.transactionReferenceNo || ''}</title>
<style>
  *{margin:0;padding:0;box-sizing:border-box}
  body{font-family:'Segoe UI',sans-serif;width:320px;margin:0 auto;padding:20px;color:#111}
  .header{text-align:center;border-bottom:2px dashed #999;padding-bottom:12px;margin-bottom:12px}
  .header h2{font-size:16px;margin-bottom:4px}
  .header p{font-size:11px;color:#666}
  .row{display:flex;justify-content:space-between;padding:5px 0;font-size:13px}
  .row .label{color:#666}
  .row .value{font-weight:600;text-align:right}
  .divider{border-top:1px dashed #ccc;margin:10px 0}
  .amount-box{background:#f5f5f5;border-radius:8px;padding:12px;margin:10px 0;text-align:center}
  .amount-box .big{font-size:22px;font-weight:700}
  .amount-box .small{font-size:11px;color:#666;margin-top:2px}
  .footer{text-align:center;font-size:10px;color:#999;margin-top:16px;border-top:2px dashed #999;padding-top:12px}
  @media print{body{padding:10px}}
</style></head><body>
<div class="header">
  <h2>BAŞKENT ENERJİ</h2>
  <p>Döviz Alım Satım</p>
  <p style="margin-top:6px;font-size:12px;font-weight:600">${typeLabel} İşlemi</p>
</div>

<div class="row"><span class="label">Fiş No:</span><span class="value">${tx.transactionReferenceNo || '-'}</span></div>
<div class="row"><span class="label">Tarih:</span><span class="value">${formatFullDate(tx.transactionDate)}</span></div>
<div class="row"><span class="label">Personel:</span><span class="value">${tx.username || '-'}</span></div>
<div class="row"><span class="label">Şube:</span><span class="value">${tx.officeName || '-'}</span></div>

<div class="divider"></div>

<div class="row"><span class="label">Kaynak:</span><span class="value">${formatNumber(tx.sourceAmount)} ${source}</span></div>
<div class="row"><span class="label">Kur:</span><span class="value">${formatNumber(rate, 4)}</span></div>
${tx.customRate ? `<div class="row"><span class="label">Özel Kur:</span><span class="value">${formatNumber(tx.customRate, 4)}</span></div>` : ''}
<div class="row"><span class="label">Hedef:</span><span class="value">${formatNumber(tx.targetAmount)} ${target}</span></div>

${tx.profit ? `<div class="divider"></div><div class="row"><span class="label">Kar:</span><span class="value" style="color:${tx.profit > 0 ? '#16a34a' : '#dc2626'}">${tx.profit > 0 ? '+' : ''}${formatNumber(tx.profit)} ₺</span></div>` : ''}

<div class="amount-box">
  <div class="big">${formatNumber(tx.targetAmount)} ${target}</div>
  <div class="small">${formatNumber(tx.sourceAmount)} ${source} × ${formatNumber(rate, 4)}</div>
</div>

${tx.notes ? `<div class="divider"></div><div class="row"><span class="label">Not:</span><span class="value">${tx.notes}</span></div>` : ''}

<div class="footer">
  <p>Başkent Enerji Döviz</p>
  <p style="margin-top:4px">${new Date().toLocaleDateString('tr-TR')} ${new Date().toLocaleTimeString('tr-TR')}</p>
</div>
</body></html>`)
  printWindow.document.close()
  setTimeout(() => { printWindow.print(); printWindow.close() }, 400)
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
    rows += `<tr>
      <td style="padding:8px;border-bottom:1px solid #eee;font-size:12px">${formatDate(tx.transactionDate)} ${formatTime(tx.transactionDate)}</td>
      <td style="padding:8px;border-bottom:1px solid #eee"><span style="padding:2px 6px;border-radius:4px;font-size:11px;font-weight:600;background:${isBuy(tx) ? '#dcfce7' : '#fee2e2'};color:${isBuy(tx) ? '#15803d' : '#b91c1c'}">${txTypeLabel(tx)}</span></td>
      <td style="padding:8px;border-bottom:1px solid #eee;font-family:monospace">${formatNumber(tx.sourceAmount)} ${tx.sourceCurrencyCode || '-'}</td>
      <td style="padding:8px;border-bottom:1px solid #eee;font-family:monospace">${tx.exchangeRate ? formatNumber(tx.exchangeRate, 4) : '-'}</td>
      <td style="padding:8px;border-bottom:1px solid #eee;font-family:monospace">${formatNumber(tx.targetAmount)} ${tx.targetCurrencyCode || '-'}</td>
      <td style="padding:8px;border-bottom:1px solid #eee;font-size:12px">${tx.username || '-'}</td>
      <td style="padding:8px;border-bottom:1px solid #eee;font-family:monospace">${tx.profit ? formatNumber(tx.profit) : '-'}</td>
    </tr>`
  })

  printWindow.document.write(`<!DOCTYPE html><html><head><title>İşlem Geçmişi</title>
    <style>body{font-family:'Segoe UI',sans-serif;padding:30px}h2{text-align:center;margin-bottom:20px}
    table{width:100%;border-collapse:collapse}th{text-align:left;padding:10px 8px;border-bottom:2px solid #333;font-size:13px}
    @media print{body{padding:10px}}</style></head><body>
    <h2>İşlem Geçmişi — ${props.selectedDate || new Date().toLocaleDateString('tr-TR')}</h2>
    <table><thead><tr><th>Saat</th><th>Tür</th><th>Kaynak</th><th>Kur</th><th>Hedef</th><th>Personel</th><th>Kar</th></tr></thead>
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
          <span class="material-symbols-outlined" aria-hidden="true" :class="{ 'th-spin': isLoading }">sync</span>
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
      <span class="material-symbols-outlined" aria-hidden="true" style="font-size:36px;color:#d1d5db">receipt_long</span>
      <p>Bugün henüz işlem yapılmamış</p>
    </div>

    <!-- Table -->
    <div v-else class="th-table-wrap">
      <table class="th-table">
        <thead>
          <tr>
            <th class="th-col-time">SAAT</th>
            <th class="th-col-type">TÜR</th>
            <th>KAYNAK</th>
            <th class="th-col-rate">KUR</th>
            <th>HEDEF</th>
            <th class="th-col-user">PERSONEL</th>
            <th class="th-col-profit">KAR</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="tx in transactions" :key="tx.id" class="th-row" :class="{ 'th-row--deleting': deleteConfirmId === tx.id }" @click="openDetail(tx)">
            <td class="th-cell-time">{{ formatDate(tx.transactionDate) }} {{ formatTime(tx.transactionDate) }}</td>
            <td>
              <span class="th-type-badge" :class="isBuy(tx) ? 'th-type-badge--buy' : 'th-type-badge--sell'">
                {{ txTypeLabel(tx) }}
              </span>
            </td>
            <td>
              <div class="th-currency-cell">
                <i v-if="getFlagClass(tx.sourceCurrencyCode)" :class="getFlagClass(tx.sourceCurrencyCode)" class="th-flag"></i>
                <span class="th-amount">{{ formatNumber(tx.sourceAmount) }}</span>
                <span class="th-code">{{ tx.sourceCurrencyCode }}</span>
              </div>
            </td>
            <td class="th-cell-rate">
              <span class="th-rate">{{ tx.exchangeRate ? formatNumber(tx.exchangeRate, 4) : '-' }}</span>
              <span v-if="tx.isCustomRate" class="th-custom-badge">Ö</span>
            </td>
            <td>
              <div class="th-currency-cell">
                <i v-if="getFlagClass(tx.targetCurrencyCode)" :class="getFlagClass(tx.targetCurrencyCode)" class="th-flag"></i>
                <span class="th-amount">{{ formatNumber(tx.targetAmount) }}</span>
                <span class="th-code">{{ tx.targetCurrencyCode }}</span>
              </div>
            </td>
            <td class="th-cell-user">
              <span class="th-username">{{ tx.username || '-' }}</span>
            </td>
            <td class="th-cell-profit">
              <span v-if="tx.profit" :class="tx.profit > 0 ? 'th-profit--pos' : 'th-profit--neg'">
                {{ tx.profit > 0 ? '+' : '' }}{{ formatNumber(tx.profit) }}
              </span>
              <span v-else class="th-profit--zero">-</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="th-pagination">
      <button @click="changePage(currentPage - 1)" :disabled="currentPage <= 1" class="th-page-btn">
        <span class="material-symbols-outlined" aria-hidden="true" style="font-size:18px">chevron_left</span>
      </button>
      <span class="th-page-info">{{ currentPage }} / {{ totalPages }}</span>
      <button @click="changePage(currentPage + 1)" :disabled="currentPage >= totalPages" class="th-page-btn">
        <span class="material-symbols-outlined" aria-hidden="true" style="font-size:18px">chevron_right</span>
      </button>
    </div>

    <!-- Detail Modal -->
    <Teleport to="body">
      <div v-if="selectedTx" class="th-overlay" @click.self="closeDetail">
        <div class="th-detail-card">
          <div class="th-detail-header">
            <div>
              <h3 class="th-detail-title">İşlem Detayı</h3>
              <span class="th-detail-ref">{{ selectedTx.transactionReferenceNo }}</span>
            </div>
            <div class="th-detail-header-right">
              <button @click="printReceipt(selectedTx)" class="th-print-btn" title="Fiş Yazdır">
                <span class="material-symbols-outlined" aria-hidden="true" style="font-size:18px">receipt_long</span>
                Fiş Yazdır
              </button>
              <button @click="closeDetail" class="th-close-btn">
                <span class="material-symbols-outlined" aria-hidden="true" style="font-size:20px">close</span>
              </button>
            </div>
          </div>

          <div class="th-detail-body">
            <div class="th-detail-type-row">
              <span class="th-type-badge th-type-badge--lg" :class="isBuy(selectedTx) ? 'th-type-badge--buy' : 'th-type-badge--sell'">
                {{ txTypeLabel(selectedTx) }}
              </span>
              <span class="th-detail-date">{{ formatFullDate(selectedTx.transactionDate) }}</span>
            </div>

            <div class="th-detail-amounts">
              <div class="th-detail-amount-box">
                <div class="th-detail-amount-label">Kaynak</div>
                <div class="th-detail-amount-value">
                  <i v-if="getFlagClass(selectedTx.sourceCurrencyCode)" :class="getFlagClass(selectedTx.sourceCurrencyCode)" class="th-flag"></i>
                  {{ formatNumber(selectedTx.sourceAmount) }} {{ selectedTx.sourceCurrencyCode }}
                </div>
              </div>
              <div class="th-detail-arrow">
                <span class="material-symbols-outlined" aria-hidden="true">arrow_forward</span>
              </div>
              <div class="th-detail-amount-box">
                <div class="th-detail-amount-label">Hedef</div>
                <div class="th-detail-amount-value">
                  <i v-if="getFlagClass(selectedTx.targetCurrencyCode)" :class="getFlagClass(selectedTx.targetCurrencyCode)" class="th-flag"></i>
                  {{ formatNumber(selectedTx.targetAmount) }} {{ selectedTx.targetCurrencyCode }}
                </div>
              </div>
            </div>

            <div class="th-detail-info-grid">
              <div class="th-detail-info-item">
                <span class="th-detail-info-label">Kur</span>
                <span class="th-detail-info-value">{{ selectedTx.exchangeRate ? formatNumber(selectedTx.exchangeRate, 4) : '-' }}</span>
              </div>
              <div v-if="selectedTx.customRate" class="th-detail-info-item">
                <span class="th-detail-info-label">Özel Kur</span>
                <span class="th-detail-info-value th-detail-custom">{{ formatNumber(selectedTx.customRate, 4) }}</span>
              </div>
              <div class="th-detail-info-item">
                <span class="th-detail-info-label">Personel</span>
                <span class="th-detail-info-value">{{ selectedTx.username || '-' }}</span>
              </div>
              <div class="th-detail-info-item">
                <span class="th-detail-info-label">Şube</span>
                <span class="th-detail-info-value">{{ selectedTx.officeName || '-' }}</span>
              </div>
              <div v-if="selectedTx.profit" class="th-detail-info-item">
                <span class="th-detail-info-label">Kar</span>
                <span class="th-detail-info-value" :class="selectedTx.profit > 0 ? 'th-profit--pos' : 'th-profit--neg'">
                  {{ selectedTx.profit > 0 ? '+' : '' }}{{ formatNumber(selectedTx.profit) }} ₺
                </span>
              </div>
              <div v-if="selectedTx.notes" class="th-detail-info-item th-detail-info-full">
                <span class="th-detail-info-label">Not</span>
                <span class="th-detail-info-value">{{ selectedTx.notes }}</span>
              </div>
            </div>

            <div v-if="selectedTx.isDeleted" class="th-detail-deleted-banner">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-size:16px">delete</span>
              Silinmiş — {{ selectedTx.deletedBy || '' }} {{ selectedTx.deletedReason ? `(${selectedTx.deletedReason})` : '' }}
            </div>
          </div>

          <div v-if="showActions && !selectedTx.isDeleted && (authStore.isAdmin || authStore.isOwner)" class="th-detail-footer">
            <template v-if="deleteConfirmId === selectedTx.id">
              <span style="font-size:13px;color:#6b7280">Bu işlemi silmek istediğinize emin misiniz?</span>
              <div style="display:flex;gap:8px">
                <button @click="deleteTransaction(selectedTx.id)" :disabled="isDeleting" class="th-detail-del-confirm">
                  <span class="material-symbols-outlined" aria-hidden="true" style="font-size:16px">{{ isDeleting ? 'refresh' : 'check' }}</span>
                  Evet, Sil
                </button>
                <button @click="deleteConfirmId = null" class="th-detail-del-cancel">İptal</button>
              </div>
            </template>
            <button v-else @click="deleteConfirmId = selectedTx.id" class="th-detail-del-btn">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-size:16px">delete</span>
              İşlemi Sil
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.th-root {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-lg);
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
  border-radius: var(--radius-md);
  background: #ede9fe;
  color: var(--color-primary);
}
.th-header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.th-filter-group {
  display: flex;
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-md);
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
  transition: background-color 0.2s, color 0.2s;
}
.th-filter-btn:not(:last-child) { border-right: 1px solid #e5e7eb; }
.th-filter-btn:hover { background: #f9fafb; }
.th-filter-btn--active {
  background: var(--color-primary);
  color: white;
}

.th-action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-md);
  background: white;
  color: #6b7280;
  cursor: pointer;
  transition: border-color 0.2s, color 0.2s;
}
.th-action-btn:hover:not(:disabled) { border-color: var(--color-primary); color: var(--color-primary); }
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
  letter-spacing: 0.5px;
  color: #9ca3af;
  background: #fafafa;
  border-bottom: 1px solid #e5e7eb;
  white-space: nowrap;
}
.th-col-time { width: 110px; }
.th-col-type { width: 60px; }
.th-col-rate { width: 90px; }
.th-col-user { width: 110px; }
.th-col-profit { width: 90px; }

.th-row td {
  padding: 10px 12px;
  border-bottom: 1px solid #f3f4f6;
  vertical-align: middle;
}
.th-row { cursor: pointer; transition: background 0.15s; }
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
  border-radius: var(--radius-sm);
}
.th-type-badge--buy { background: #dcfce7; color: #15803d; }
.th-type-badge--sell { background: var(--color-danger-bg); color: #b91c1c; }
.th-type-badge--lg { font-size: 13px; padding: 5px 14px; }

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

.th-cell-user {
  max-width: 120px;
}
.th-username {
  font-size: 12px;
  color: #374151;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  display: block;
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
  color: var(--color-danger);
}
.th-profit--zero { color: #d1d5db; }

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
  border-radius: var(--radius-md);
  background: white;
  color: #374151;
  cursor: pointer;
  transition: border-color 0.2s, color 0.2s;
}
.th-page-btn:hover:not(:disabled) { border-color: var(--color-primary); color: var(--color-primary); }
.th-page-btn:disabled { opacity: 0.3; cursor: not-allowed; }
.th-page-info {
  font-size: 13px;
  color: #6b7280;
  font-weight: 500;
}

/* Detail Modal */
.th-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 20px;
}
.th-detail-card {
  background: white;
  border-radius: var(--radius-lg);
  width: 100%;
  max-width: 520px;
  box-shadow: 0 20px 60px rgba(0,0,0,0.2);
  overflow: hidden;
}

.th-detail-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 24px;
  border-bottom: 1px solid #f3f4f6;
}
.th-detail-title {
  font-size: 16px;
  font-weight: 700;
  color: #111827;
}
.th-detail-ref {
  font-size: 12px;
  color: #9ca3af;
  font-family: 'JetBrains Mono', monospace;
}
.th-detail-header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}
.th-print-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 6px 14px;
  border: 1px solid var(--color-primary);
  border-radius: var(--radius-md);
  background: white;
  color: var(--color-primary);
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s;
}
.th-print-btn:hover { background: var(--color-primary); color: white; }
.th-close-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: none;
  border-radius: var(--radius-md);
  background: #f3f4f6;
  color: #6b7280;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s;
}
.th-close-btn:hover { background: #e5e7eb; color: #111827; }

.th-detail-body {
  padding: 20px 24px;
}
.th-detail-type-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}
.th-detail-date {
  font-size: 13px;
  color: #6b7280;
}

.th-detail-amounts {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;
}
.th-detail-amount-box {
  flex: 1;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-lg);
  padding: 14px;
  text-align: center;
}
.th-detail-amount-label {
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: #9ca3af;
  margin-bottom: 6px;
}
.th-detail-amount-value {
  font-size: 17px;
  font-weight: 700;
  color: #111827;
  font-family: 'JetBrains Mono', monospace;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}
.th-detail-arrow {
  color: #9ca3af;
  flex-shrink: 0;
}

.th-detail-info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}
.th-detail-info-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.th-detail-info-full { grid-column: span 2; }
.th-detail-info-label {
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: #9ca3af;
}
.th-detail-info-value {
  font-size: 14px;
  font-weight: 500;
  color: #111827;
}
.th-detail-custom {
  color: #92400e;
}

.th-detail-deleted-banner {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 16px;
  padding: 10px 14px;
  border-radius: var(--radius-md);
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #b91c1c;
  font-size: 13px;
  font-weight: 500;
}

.th-detail-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 24px;
  border-top: 1px solid #f3f4f6;
  background: #fafafa;
}
.th-detail-del-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 6px 14px;
  border: 1px solid #fecaca;
  border-radius: var(--radius-md);
  background: white;
  color: var(--color-danger);
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}
.th-detail-del-btn:hover { background: #fef2f2; }
.th-detail-del-confirm {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 6px 14px;
  border: none;
  border-radius: var(--radius-md);
  background: var(--color-danger);
  color: white;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
}
.th-detail-del-cancel {
  padding: 6px 14px;
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-md);
  background: white;
  color: #6b7280;
  font-size: 12px;
  cursor: pointer;
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
  .th-col-user { display: none; }
  .th-cell-user { display: none; }
  .th-col-profit { display: none; }
  .th-cell-profit { display: none; }
  .th-detail-card { max-width: 100%; }
  .th-detail-amounts { flex-direction: column; }
  .th-detail-arrow { transform: rotate(90deg); }
}
</style>
