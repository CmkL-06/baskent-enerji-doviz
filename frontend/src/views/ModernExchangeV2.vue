<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'
import CurrencySelector from '@/components/common/CurrencySelector.vue'
import TransactionHistory from '@/components/common/TransactionHistory.vue'
import USDTPaymentsModal from './USDTPaymentsModal.vue'
import VaultCountingModal from './VaultCountingModal.vue'
import DayClosureModal from './DayClosureModal.vue'
import { useNotification } from '@/composables/useNotification'
import { getCurrencyCountryCode, getCurrencyName } from '@/utils/currency'

const { t } = useI18n()

const route = useRoute()
const exchangeStore = useExchangeStore()
const authStore = useAuthStore()
const notification = useNotification()

// Terminal mode
const terminalMode = ref<'standard' | 'arbitrage' | 'batch'>('standard')

// Transaction type toggle (buy/sell)
const initialType = route.query.type === 'sell' ? 'sell' : 'buy'
const transactionType = ref<'buy' | 'sell'>(initialType)

watch(() => route.query.type, (val) => {
  if (val === 'buy' || val === 'sell') transactionType.value = val
})

// Batch mode queue
const batchQueue = ref<Array<{
  id: string
  type: 'buy' | 'sell' | 'arbitrage'
  sourceCurrencyId: string
  targetCurrencyId: string
  sourceAmount: number
  targetAmount: number
  exchangeRate: number
  customRate: string | number | null
  note: string
}>>([])

const batchSourceCurrencyId = ref('')
const batchTargetCurrencyId = ref('')
const batchAmount = ref(0)
const batchRate = ref(0)
const batchCustomRate = ref<string | number | null>(null)
const batchType = ref<'buy' | 'sell'>('buy')
const batchNote = ref('')

// Popular currencies for quick access
const popularCurrencies = ['USD', 'EUR', 'RUB', 'GBP', 'KRUB', 'USDT']

// Types
interface ExchangeItem {
  id: string
  sourceCurrencyId: string
  targetCurrencyId: string
  sourceAmount: number
  targetAmount: number
  exchangeRate: number
  customRate: string | number | null
  rateManuallySet?: boolean
}

interface Currency {
  id: string
  currencyCode: string
  currencyName: string
  currencySymbol: string
}

// State
const isLoading = ref(false)
const isInitialLoading = ref(true)
const currencies = ref<Currency[]>([])
const selectedOfficeId = ref(localStorage.getItem('selectedOfficeId') || '')
const selectedVaultId = ref(localStorage.getItem('selectedVaultId') || '')
const notes = ref('')
const tryId = ref<string>('')
const balanceCheck = ref<any>(null)
const balanceCheckTimeout = ref<any>(null)
const rateRefreshInterval = ref<any>(null)
const refreshCountdown = ref(60)
const countdownInterval = ref<any>(null)
const vaultCheckInterval = ref<any>(null)
const isPageHidden = ref(false)
const showVaultCountWarning = ref(false)
const currentVault = ref<any>(null)
const showNotes = ref(false)
const dayBlocked = ref(false)
const dayStatus = ref<any>(null)
const showDayClosureModal = ref(false)
const wacData = ref<Record<string, number>>({})
const ownerOverrideLoss = ref(false)

// Receipt state
const lastReceipt = ref<{
  type: 'buy' | 'sell' | 'arbitrage'
  items: Array<{ source: string, target: string, amount: number, rate: number, total: number, customRate: boolean }>
  totals: Array<{ code: string, amount: number }>
  timestamp: string
  notes: string
} | null>(null)
const showReceiptPanel = ref(false)
const arbMarginPercent = ref<number>(0)

// Component refs
const transactionHistoryRef = ref<any>(null)
const usdtModalRef = ref<any>(null)
const vaultCountingModalRef = ref<any>(null)

// Open USDT modal from outside
const openUSDTModal = () => {
  usdtModalRef.value?.open()
}

// Expose for parent component
defineExpose({
  openUSDTModal
})

// Exchange items
const exchangeItems = ref<ExchangeItem[]>([
  {
    id: crypto.randomUUID(),
    sourceCurrencyId: '',
    targetCurrencyId: '',
    sourceAmount: 0,
    targetAmount: 0,
    exchangeRate: 0,
    customRate: null,
    rateManuallySet: false
  }
])

// External market rates for comparison
const externalMarketRates = ref<Record<string, any[]>>({}) // Key: "USD-TRY", Value: [{source: "TCMB", buyRate: 38.5, ...}]
const loadingExternalRates = ref(false)
const allExternalRates = ref<any[]>([]) // All fetched external rates

// Computed
const isBuyingFromCustomer = computed(() => transactionType.value === 'buy')

// Available vaults based on selected office
const availableVaults = computed(() => {
  if (!selectedOfficeId.value) return []
  return exchangeStore.vaults.filter(v => String(v.officeId ?? v.id) === String(selectedOfficeId.value))
})

// Get selected vault's balance data
const selectedVaultBalances = computed(() => {
  if (!selectedVaultId.value) return []
  const vault = availableVaults.value.find(v => (v.vaultId || v.id) === selectedVaultId.value)
  return vault?.balances || []
})

// Filter currencies to exclude TRY
const filteredCurrencies = computed(() => {
  return currencies.value.filter(c => c.currencyCode !== 'TRY')
})

// Computed totals
const grandTotal = computed(() => {
  // Group items by target currency for buying, source currency for selling
  const totals = new Map<string, { amount: number; currencyCode: string }>()
  
  exchangeItems.value.forEach(item => {
    if (transactionType.value === 'buy') {
      // Buying: group by target currency (what we give to customer)
      const currencyId = item.targetCurrencyId
      const currency = getCurrencyById(currencyId)
      if (currency) {
        const existing = totals.get(currencyId) || { amount: 0, currencyCode: currency.currencyCode }
        existing.amount += item.targetAmount || 0
        totals.set(currencyId, existing)
      }
    } else {
      // Selling: group by source currency (what we receive from customer)
      const currencyId = item.sourceCurrencyId
      const currency = getCurrencyById(currencyId)
      if (currency) {
        const existing = totals.get(currencyId) || { amount: 0, currencyCode: currency.currencyCode }
        existing.amount += item.sourceAmount || 0
        totals.set(currencyId, existing)
      }
    }
  })
  
  return Array.from(totals.values())
})

// Calculate total TRY amount to be received from customer
const totalTryAmount = computed(() => {
  let total = 0

  exchangeItems.value.forEach(item => {
    if (transactionType.value === 'buy') {
      // Buying: we receive TRY from customer (only if source is TRY)
      if (item.sourceCurrencyId === tryId.value) {
        total += item.sourceAmount || 0
      }
    } else {
      // Selling: we give TRY to customer (only if target is TRY)
      if (item.targetCurrencyId === tryId.value) {
        total += item.targetAmount || 0
      }
    }
  })

  return total
})

const totalForeignCurrencyNeeded = computed(() => {
  if (transactionType.value === 'sell' && balanceCheck.value) {
    return exchangeItems.value
      .filter(i => i.targetCurrencyId === balanceCheck.value.currencyId)
      .reduce((sum, i) => sum + (i.targetAmount || 0), 0)
  }
  return 0
})

const isBalanceSufficient = computed(() => {
  if (!balanceCheck.value) return true
  return balanceCheck.value.balance >= totalForeignCurrencyNeeded.value
})

// Helper to format numbers with thousand separators
const formatNumber = (value: number, decimals: number = 2): string => {
  return new Intl.NumberFormat('tr-TR', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals
  }).format(value)
}

// Türkçe sayı girişini güvenli parse et.
// "1.234,56" → 1234.56 (binlik ayraçlı), "38,75" → 38.75, "38.75" → 38.75 (sayı toString).
// Yalnızca hem '.' hem ',' varsa '.' binlik sayılır — mevcut tek-ayraçlı davranış korunur.
const parseNum = (value: any): number => {
  if (value === null || value === undefined || value === '') return 0
  let s = String(value).trim()
  if (s.includes(',') && s.includes('.')) {
    s = s.replace(/\./g, '').replace(',', '.')
  } else {
    s = s.replace(',', '.')
  }
  const n = parseFloat(s)
  return isNaN(n) ? 0 : n
}

// Get currency info by code
const getCurrencyByCode = (code: string) => {
  return currencies.value.find(c => c.currencyCode === code)
}

// Get currency info by id
const getCurrencyById = (id: string) => {
  return currencies.value.find(c => c.id === id)
}

// Fetch external market rates
const fetchExternalMarketRates = async () => {
  if (loadingExternalRates.value) return

  try {
    loadingExternalRates.value = true
    const rates = await apiService.getExternalRates(5) // 5 minute cache

    if (rates && rates.length > 0) {
      allExternalRates.value = rates

      // Organize rates by currency pair for easy lookup
      const ratesMap: Record<string, any[]> = {}

      rates.forEach((rate: any) => {
        const key = `${rate.currencyCode}-${rate.targetCurrencyCode || 'TRY'}`
        if (!ratesMap[key]) {
          ratesMap[key] = []
        }
        ratesMap[key].push(rate)
      })

      externalMarketRates.value = ratesMap
    }
  } catch (error) {
    console.error('Failed to fetch external market rates:', error)
  } finally {
    loadingExternalRates.value = false
  }
}

// Clean up source name (remove DOVIZ_COM_ prefix)
const cleanSourceName = (sourceName: string): string => {
  return sourceName.replace('DOVIZ_COM_', '').replace('_', ' ')
}

// Get external rates for a specific item
const getExternalRatesForItem = (item: ExchangeItem) => {
  if (!item.sourceCurrencyId || !item.targetCurrencyId) return []

  const sourceCurrency = getCurrencyById(item.sourceCurrencyId)
  const targetCurrency = getCurrencyById(item.targetCurrencyId)

  if (!sourceCurrency || !targetCurrency) return []

  const sourceCode = sourceCurrency.currencyCode
  const targetCode = targetCurrency.currencyCode

  // Check if we have direct rates
  const directKey = `${sourceCode}-${targetCode}`
  const directRates = externalMarketRates.value[directKey] || []

  if (directRates.length > 0) {
    return directRates.map(rate => ({
      source: cleanSourceName(rate.source),
      buyRate: transactionType.value === 'buy' ? rate.buyRate : rate.sellRate,
      sellRate: transactionType.value === 'buy' ? rate.sellRate : rate.buyRate,
      displayRate: transactionType.value === 'buy' ? rate.buyRate : rate.sellRate
    }))
  }

  // If no direct rates, calculate cross-rate from TRY-based rates
  // Example: USD/EUR = (USD/TRY) ÷ (EUR/TRY)
  const sourceToTryKey = `${sourceCode}-TRY`
  const targetToTryKey = `${targetCode}-TRY`

  const sourceRates = externalMarketRates.value[sourceToTryKey] || []
  const targetRates = externalMarketRates.value[targetToTryKey] || []

  if (sourceRates.length === 0 || targetRates.length === 0) return []

  // Group rates by source
  const sourcesByProvider: Record<string, any> = {}
  sourceRates.forEach(rate => {
    sourcesByProvider[rate.source] = rate
  })

  const targetsByProvider: Record<string, any> = {}
  targetRates.forEach(rate => {
    targetsByProvider[rate.source] = rate
  })

  // Calculate cross-rates for providers that have both rates
  const crossRates: any[] = []
  Object.keys(sourcesByProvider).forEach(provider => {
    if (targetsByProvider[provider]) {
      const sourceRate = sourcesByProvider[provider]
      const targetRate = targetsByProvider[provider]

      // For BUY transaction: customer gives source, receives target
      // We use buyRate for source (we buy source from customer)
      // We use sellRate for target (we sell target to customer)
      // Cross rate = (source/TRY buy rate) ÷ (target/TRY sell rate)

      // For SELL transaction: customer gives target, receives source
      // We use sellRate for source (we sell source to customer)
      // We use buyRate for target (we buy target from customer)
      // Cross rate = (source/TRY sell rate) ÷ (target/TRY buy rate)

      let crossRate
      if (transactionType.value === 'buy') {
        // Buying from customer: they give source, we give target
        crossRate = sourceRate.buyRate && targetRate.sellRate
          ? sourceRate.buyRate / targetRate.sellRate
          : null
      } else {
        // Selling to customer: we give source, they give target
        crossRate = sourceRate.sellRate && targetRate.buyRate
          ? sourceRate.sellRate / targetRate.buyRate
          : null
      }

      if (crossRate) {
        crossRates.push({
          source: cleanSourceName(provider),
          displayRate: crossRate,
          buyRate: crossRate,
          sellRate: crossRate
        })
      }
    }
  })

  return crossRates
}

// Active quick currency (for highlight)
const activeQuickCurrency = computed(() => {
  if (exchangeItems.value.length === 0) return ''
  const firstItem = exchangeItems.value[0]
  const currId = firstItem.sourceCurrencyId
  if (!currId) return ''
  const c = getCurrencyById(currId)
  return c?.currencyCode || ''
})

// Today's profit summary
const todayProfit = ref<number | null>(null)
const todayTxCount = ref(0)

async function loadTodayStats() {
  if (!selectedOfficeId.value) return
  try {
    const today = new Date().toISOString().split('T')[0]
    const result = await apiService.getTransactionHistory({
      officeId: selectedOfficeId.value,
      startDate: today + 'T00:00:00',
      endDate: today + 'T23:59:59',
      page: 1,
      pageSize: 200
    })
    if (result?.data) {
      todayTxCount.value = result.pagination?.totalCount || result.data.length
      todayProfit.value = result.data.reduce((sum: number, tx: any) => sum + (tx.profit || 0), 0)
    }
  } catch { /* ignore */ }
}

// Keyboard shortcut: Ctrl+Enter to submit
function handleKeyDown(e: KeyboardEvent) {
  if (e.ctrlKey && e.key === 'Enter') {
    e.preventDefault()
    if (terminalMode.value === 'batch') {
      submitBatch()
    } else {
      submitExchange()
    }
  }
  if (e.key === 'F2') { e.preventDefault(); transactionType.value = 'buy'; terminalMode.value = 'standard' }
  if (e.key === 'F3') { e.preventDefault(); transactionType.value = 'sell'; terminalMode.value = 'standard' }
  if (e.key === 'F4') { e.preventDefault(); terminalMode.value = 'arbitrage' }
}

// ═══ Position Bar ═══
const positionData = computed(() => {
  if (!selectedVaultBalances.value?.length) return []
  const pinned = new Set(popularCurrencies)
  return selectedVaultBalances.value
    .filter((b: any) => {
      const c = currencies.value.find(c => c.id === b.currencyId)
      if (!c || c.currencyCode === 'TRY') return false
      return pinned.has(c.currencyCode) || b.balance !== 0
    })
    .map((b: any) => {
      const c = currencies.value.find(c => c.id === b.currencyId)
      const code = c?.currencyCode || ''
      const wac = wacData.value[b.currencyId] ?? 0
      const costValue = b.balance * wac
      const systemRate = getSystemRateForCode(code)
      const marketValue = b.balance * systemRate
      const unrealizedPnl = marketValue - costValue
      return {
        currencyId: b.currencyId,
        code,
        balance: b.balance,
        wac,
        systemRate,
        costValue,
        marketValue,
        unrealizedPnl,
        pnlPercent: costValue > 0 ? ((unrealizedPnl / costValue) * 100) : 0
      }
    })
    .sort((a: any, b: any) => {
      const aIdx = popularCurrencies.indexOf(a.code)
      const bIdx = popularCurrencies.indexOf(b.code)
      const aPinned = aIdx >= 0 ? aIdx : 999
      const bPinned = bIdx >= 0 ? bIdx : 999
      if (aPinned !== bPinned) return aPinned - bPinned
      return Math.abs(b.marketValue) - Math.abs(a.marketValue)
    })
})

function getSystemRateForCode(code: string): number {
  const key = `${code}-TRY`
  const rates = externalMarketRates.value[key]
  if (rates?.length) {
    const sysRate = rates.find((r: any) => r.source?.includes('SYSTEM') || r.source?.includes('Sistem'))
    if (sysRate) return sysRate.buyRate || sysRate.sellRate || 0
    return rates[0]?.buyRate || rates[0]?.sellRate || 0
  }
  const c = currencies.value.find(c => c.currencyCode === code)
  if (c) {
    const rate = exchangeStore.getExchangeRate(c.id, tryId.value, 'sell')
    return rate || 0
  }
  return 0
}

const totalPositionValue = computed(() => positionData.value.reduce((s, p) => s + p.marketValue, 0))
const totalUnrealizedPnl = computed(() => positionData.value.reduce((s, p) => s + p.unrealizedPnl, 0))

// ═══ Rate Matrix ═══
const matrixCurrencies = ['USD', 'EUR', 'RUB', 'GBP']

const rateMatrix = computed(() => {
  const matrix: Record<string, Record<string, number>> = {}
  for (const from of matrixCurrencies) {
    matrix[from] = {}
    for (const to of matrixCurrencies) {
      if (from === to) { matrix[from][to] = 1; continue }
      const fromRate = getSystemRateForCode(from)
      const toRate = getSystemRateForCode(to)
      matrix[from][to] = toRate > 0 ? fromRate / toRate : 0
    }
  }
  return matrix
})

function matrixToArbitrage(from: string, to: string) {
  const fromCurrency = currencies.value.find(c => c.currencyCode === from)
  const toCurrency = currencies.value.find(c => c.currencyCode === to)
  if (!fromCurrency || !toCurrency || from === to) return
  terminalMode.value = 'arbitrage'
  if (exchangeItems.value.length > 0) {
    exchangeItems.value[0].sourceCurrencyId = fromCurrency.id
    exchangeItems.value[0].targetCurrencyId = toCurrency.id
    exchangeItems.value[0].rateManuallySet = false
    exchangeItems.value[0].customRate = null
    updateExchangeRate(exchangeItems.value[0])
  }
}

// ═══ Batch Mode ═══
function addToBatch() {
  if (!batchSourceCurrencyId.value || !batchTargetCurrencyId.value || batchAmount.value <= 0) {
    notification.warning('Lütfen para birimi, miktar ve kur bilgilerini doldurun')
    return
  }
  let rate = batchRate.value
  if (batchCustomRate.value !== null && batchCustomRate.value !== '') {
    const parsed = parseNum(batchCustomRate.value)
    if (parsed > 0) rate = parsed
  }
  batchQueue.value.push({
    id: crypto.randomUUID(),
    type: batchType.value,
    sourceCurrencyId: batchSourceCurrencyId.value,
    targetCurrencyId: batchTargetCurrencyId.value,
    sourceAmount: batchAmount.value,
    targetAmount: batchAmount.value * rate,
    exchangeRate: rate,
    customRate: batchCustomRate.value,
    note: batchNote.value
  })
  batchAmount.value = 0
  batchCustomRate.value = null
  batchNote.value = ''
  notification.success('İşlem kuyruğa eklendi')
}

function removeBatchItem(id: string) {
  batchQueue.value = batchQueue.value.filter(i => i.id !== id)
}

async function submitBatch() {
  if (batchQueue.value.length === 0) {
    notification.warning('Kuyrukta işlem yok')
    return
  }
  isLoading.value = true
  try {
    const transactions = batchQueue.value.map(item => ({
      vaultId: selectedVaultId.value,
      sourceCurrencyId: item.sourceCurrencyId,
      targetCurrencyId: item.targetCurrencyId,
      sourceAmount: item.sourceAmount,
      isBuyingFromCustomer: item.type === 'buy',
      customRate: item.customRate ? parseNum(item.customRate) : undefined,
      notes: item.note || undefined,
      ownerOverrideLoss: ownerOverrideLoss.value
    }))
    buildReceiptFromBatch()
    await apiService.createExchangeTransaction(transactions)
    notification.success(`${batchQueue.value.length} işlem başarıyla tamamlandı`)
    showReceiptPanel.value = true
    batchQueue.value = []
    if (transactionHistoryRef.value) transactionHistoryRef.value.loadTransactions()
    loadTodayStats()
    notification.exchangeSuccess()
  } catch (error: any) {
    notification.error(`Toplu işlem hatası: ${error.response?.data?.message || error.message}`)
  } finally {
    isLoading.value = false
  }
}

const batchTotal = computed(() => {
  const totals = new Map<string, number>()
  batchQueue.value.forEach(item => {
    const sc = getCurrencyById(item.sourceCurrencyId)
    const tc = getCurrencyById(item.targetCurrencyId)
    if (sc) {
      const key = sc.currencyCode
      totals.set(key, (totals.get(key) || 0) + (item.type === 'buy' ? item.sourceAmount : -item.sourceAmount))
    }
    if (tc) {
      const key = tc.currencyCode
      totals.set(key, (totals.get(key) || 0) + (item.type === 'buy' ? -item.targetAmount : item.targetAmount))
    }
  })
  return Array.from(totals.entries()).map(([code, amount]) => ({ code, amount }))
})

// Update batch rate when currencies change
watch([batchSourceCurrencyId, batchTargetCurrencyId, batchType], async () => {
  if (batchSourceCurrencyId.value && batchTargetCurrencyId.value) {
    const rate = exchangeStore.getExchangeRate(batchSourceCurrencyId.value, batchTargetCurrencyId.value, batchType.value)
    batchRate.value = rate || 0
  }
})

// Methods
const selectQuickCurrency = (currencyCode: string) => {
  const currency = getCurrencyByCode(currencyCode)
  if (!currency) return

  if (exchangeItems.value.length > 0) {
    exchangeItems.value[0].sourceCurrencyId = currency.id
    updateExchangeRate(exchangeItems.value[0])
  }
}

const addExchangeItem = () => {
  exchangeItems.value.push({
    id: crypto.randomUUID(),
    sourceCurrencyId: '',
    targetCurrencyId: tryId.value,
    sourceAmount: 0,
    targetAmount: 0,
    exchangeRate: 0,
    customRate: null,
    rateManuallySet: false
  })
}

const removeExchangeItem = (index: number) => {
  if (exchangeItems.value.length > 1) {
    exchangeItems.value.splice(index, 1)
  }
}

const updateAmount = (item: ExchangeItem, value: string) => {
  const amount = parseNum(value)
  item.sourceAmount = amount

  // Calculate target amount using exchange rate
  let rate = item.exchangeRate
  if (item.customRate !== null && item.customRate !== '') {
    const customRateNum = parseNum(item.customRate)
    if (!isNaN(customRateNum) && customRateNum > 0) {
      rate = customRateNum
    }
  }
  
  if (rate > 0) {
    item.targetAmount = amount * rate
  }
  
  // Trigger balance check for selling
  if (transactionType.value === 'sell' && selectedVaultId.value && item.targetCurrencyId && item.targetCurrencyId !== tryId.value) {
    checkBalance(item.targetCurrencyId)
  }
}

const updateSourceCurrency = async (item: ExchangeItem, currencyId: string) => {
  item.sourceCurrencyId = currencyId
  item.rateManuallySet = false
  item.customRate = null
  
  await exchangeStore.loadExchangeRates(selectedOfficeId.value)
  await updateExchangeRate(item)
}

const updateTargetCurrency = async (item: ExchangeItem, currencyId: string) => {
  item.targetCurrencyId = currencyId
  item.rateManuallySet = false
  item.customRate = null
  
  await exchangeStore.loadExchangeRates(selectedOfficeId.value)
  await updateExchangeRate(item)
  
  // Trigger balance check for selling
  if (transactionType.value === 'sell' && selectedVaultId.value && currencyId && currencyId !== tryId.value) {
    checkBalance(currencyId)
  }
}

const refreshRates = async () => {
  try {
    refreshCountdown.value = 60
    const oldItemRates = exchangeItems.value.map(item => ({
      id: item.id,
      rate: item.exchangeRate,
      sourceCurrency: getCurrencyById(item.sourceCurrencyId)?.currencyCode || '',
      targetCurrency: getCurrencyById(item.targetCurrencyId)?.currencyCode || ''
    }))

    // Refresh both system rates and external market rates
    await Promise.all([
      exchangeStore.loadExchangeRates(selectedOfficeId.value),
      fetchExternalMarketRates()
    ])

    for (const item of exchangeItems.value) {
      if (!item.rateManuallySet) {
        const oldItemData = oldItemRates.find(old => old.id === item.id)
        const oldRate = oldItemData?.rate || 0

        await updateExchangeRate(item)

        if (oldRate > 0 && item.exchangeRate > 0 && Math.abs(oldRate - item.exchangeRate) > 0.001) {
          const pairName = `${oldItemData?.sourceCurrency}/${oldItemData?.targetCurrency}`
          notification.warning(`${pairName} kuru değişti: ${formatNumber(oldRate)} → ${formatNumber(item.exchangeRate)}`, {
            duration: 600000,
            title: 'Kur Değişikliği'
          })
        }
      }
    }
  } catch (error) {
    console.error('Failed to refresh rates:', error)
  }
}

const updateExchangeRate = async (item: ExchangeItem) => {
  if (item.rateManuallySet) {
    if (item.sourceAmount > 0) {
      let rate = item.exchangeRate
      if (item.customRate) {
        const customRateNum = parseNum(item.customRate)
        if (customRateNum > 0) {
          rate = customRateNum
        }
      }
      item.targetAmount = item.sourceAmount * rate
    }
    return
  }
  
  if (!selectedOfficeId.value || !item.sourceCurrencyId || !item.targetCurrencyId) {
    item.exchangeRate = 0
    item.targetAmount = 0
    return
  }
  
  if (item.sourceCurrencyId === item.targetCurrencyId) {
    item.exchangeRate = 1
    item.targetAmount = item.sourceAmount
    return
  }
  
  try {
    const rate = exchangeStore.getExchangeRate(
      item.sourceCurrencyId, 
      item.targetCurrencyId, 
      transactionType.value
    )
    
    if (rate !== null && rate > 0) {
      item.exchangeRate = rate
      if (item.sourceAmount > 0) {
        item.targetAmount = item.sourceAmount * rate
      }
    } else {
      item.exchangeRate = 0
      item.targetAmount = 0
    }
  } catch (error) {
    console.error('Failed to get exchange rate:', error)
    item.exchangeRate = 0
    item.targetAmount = 0
  }
}

const handleRateInput = (item: ExchangeItem, value: string) => {
  item.rateManuallySet = true
  item.customRate = value
  
  if (value !== '') {
    const rate = parseNum(value)

    if (rate > 0) {
      item.exchangeRate = rate
      if (item.sourceAmount > 0) {
        item.targetAmount = item.sourceAmount * rate
      }
    }
  } else {
    if (item.sourceAmount > 0 && item.exchangeRate > 0) {
      item.targetAmount = item.sourceAmount * item.exchangeRate
    }
  }
}

const handleRateBlur = (item: ExchangeItem) => {
  if (item.customRate === '') {
    item.customRate = null
    item.rateManuallySet = false
  }
}

const checkBalance = async (currencyId: string) => {
  if (!selectedVaultId.value || !currencyId) {
    balanceCheck.value = null
    return
  }
  
  if (balanceCheckTimeout.value) {
    clearTimeout(balanceCheckTimeout.value)
  }
  
  balanceCheckTimeout.value = setTimeout(async () => {
    try {
      const balance = await apiService.getVaultBalance(selectedVaultId.value, currencyId)
      balanceCheck.value = {
        currencyId,
        balance: balance.balance || 0,
        currencyCode: balance.currencyCode || ''
      }
    } catch (error) {
      console.error('Failed to check balance:', error)
      balanceCheck.value = null
    }
  }, 300)
}

const validateExchange = async () => {
  if (!selectedOfficeId.value) {
    notification.error('Lütfen önce bir ofis seçiniz!')
    return false
  }
  
  if (!selectedVaultId.value) {
    notification.error('Lütfen önce bir kasa seçiniz!')
    return false
  }
  
  for (const item of exchangeItems.value) {
    if (!item.sourceCurrencyId || !item.targetCurrencyId) {
      notification.error('Lütfen para birimlerini seçiniz')
      return false
    }
    
    if (!item.sourceAmount || item.sourceAmount <= 0) {
      notification.error('Lütfen geçerli bir miktar giriniz')
      return false
    }
    
    if (!item.exchangeRate && !item.customRate) {
      notification.error('Kur bilgisi bulunamadı')
      return false
    }
  }
  
  // Satışta bakiye kontrolü — her farklı hedef döviz için AYRI kontrol
  // (backend de doğruluyor; bu erken/kullanıcı-dostu uyarı)
  if (transactionType.value === 'sell') {
    const neededByCurrency = new Map<string, number>()
    for (const i of exchangeItems.value) {
      if (i.targetCurrencyId && i.targetCurrencyId !== tryId.value) {
        neededByCurrency.set(
          i.targetCurrencyId,
          (neededByCurrency.get(i.targetCurrencyId) || 0) + (i.targetAmount || 0)
        )
      }
    }
    for (const [currencyId, needed] of neededByCurrency) {
      try {
        const bal = await apiService.getVaultBalance(selectedVaultId.value, currencyId)
        const available = bal.balance || 0
        if (needed > available) {
          notification.error(`Yetersiz bakiye. Gerekli: ${formatNumber(needed)}, Mevcut: ${formatNumber(available)} ${bal.currencyCode || ''}`)
          return false
        }
      } catch (e) {
        // Bakiye alınamazsa backend doğrulamasına bırak
      }
    }
  }

  return true
}

const submitExchange = async () => {
  if (!(await validateExchange())) return
  
  isLoading.value = true
  try {
    const transactions = exchangeItems.value.map(item => ({
      vaultId: selectedVaultId.value,
      customerId: undefined,
      partyId: undefined,
      sourceCurrencyId: item.sourceCurrencyId,
      targetCurrencyId: item.targetCurrencyId,
      sourceAmount: item.sourceAmount,
      isBuyingFromCustomer: transactionType.value === 'buy',
      customRate: item.customRate ? parseNum(item.customRate) : undefined,
      notes: notes.value || undefined,
      ownerOverrideLoss: ownerOverrideLoss.value
    }))
    
    buildReceiptFromCurrent()
    await apiService.createExchangeTransaction(transactions)

    notification.success(t('exchange.messages.transactionSuccess'))
    showReceiptPanel.value = true

    resetForm()

    if (transactionHistoryRef.value) {
      transactionHistoryRef.value.loadTransactions()
    }

    loadTodayStats()
    notification.exchangeSuccess()
  } catch (error: any) {
    console.error('Exchange failed:', error)
    notification.error(`${t('exchange.messages.transactionFailed')}: ${error.response?.data?.message || error.message || t('exchange.messages.unknownError')}`)
  } finally {
    isLoading.value = false
  }
}

const resetForm = () => {
  notes.value = ''
  ownerOverrideLoss.value = false
  exchangeItems.value = [{
    id: crypto.randomUUID(),
    sourceCurrencyId: '',
    targetCurrencyId: tryId.value,
    sourceAmount: 0,
    targetAmount: 0,
    exchangeRate: 0,
    customRate: null,
    rateManuallySet: false
  }]
}

const printInvoice = () => {
  const printWindow = window.open('', '_blank', 'width=800,height=600')
  if (!printWindow) {
    notification.error(t('exchange.messages.printWindowFailed'))
    return
  }
  
  let tableHTML = `
    <table style="width: 100%; border-collapse: collapse; margin-top: 30px;">
      <thead>
        <tr>
          <th style="text-align: left; padding: 12px 8px; border-bottom: 2px solid #e5e7eb;">Kaynak</th>
          <th style="text-align: left; padding: 12px 8px; border-bottom: 2px solid #e5e7eb;">Hedef</th>
          <th style="text-align: right; padding: 12px 8px; border-bottom: 2px solid #e5e7eb;">${t('exchange.operations.amount')}</th>
          <th style="text-align: right; padding: 12px 8px; border-bottom: 2px solid #e5e7eb;">Kur</th>
          <th style="text-align: right; padding: 12px 8px; border-bottom: 2px solid #e5e7eb;">Tutar</th>
        </tr>
      </thead>
      <tbody>
  `
  
  exchangeItems.value.forEach(item => {
    const sourceCurrency = getCurrencyById(item.sourceCurrencyId)
    const targetCurrency = getCurrencyById(item.targetCurrencyId)
    const amount = formatNumber(item.sourceAmount)
    const rate = formatNumber(item.exchangeRate)
    const total = formatNumber(item.targetAmount)
    const customRateBadge = item.customRate ? '<span style="background: #fef3c7; color: #92400e; padding: 2px 6px; border-radius: 4px; font-size: 10px; margin-left: 6px;">Ö</span>' : ''
    
    tableHTML += `
      <tr>
        <td style="padding: 12px 8px; border-bottom: 1px solid #f3f4f6;">${sourceCurrency?.currencyCode || ''}</td>
        <td style="padding: 12px 8px; border-bottom: 1px solid #f3f4f6;">${targetCurrency?.currencyCode || ''}</td>
        <td style="text-align: right; padding: 12px 8px; border-bottom: 1px solid #f3f4f6; font-family: monospace;">${amount}</td>
        <td style="text-align: right; padding: 12px 8px; border-bottom: 1px solid #f3f4f6; font-family: monospace;">${rate}${customRateBadge}</td>
        <td style="text-align: right; padding: 12px 8px; border-bottom: 1px solid #f3f4f6; font-family: monospace; font-weight: 600;">${total}</td>
      </tr>
    `
  })
  
  // Add totals for each currency
  tableHTML += `
      </tbody>
      <tfoot>
  `
  
  grandTotal.value.forEach(total => {
    tableHTML += `
        <tr style="background: #f9fafb;">
          <td colspan="4" style="padding: 12px 8px; font-weight: bold; border-top: 1px solid #e5e7eb;">TOPLAM (${total.currencyCode})</td>
          <td style="text-align: right; padding: 12px 8px; font-weight: bold; border-top: 1px solid #e5e7eb; font-family: monospace;">${formatNumber(total.amount)} ${total.currencyCode}</td>
        </tr>
    `
  })
  
  tableHTML += `
      </tfoot>
    </table>
  `
  
  printWindow.document.write(`
    <!DOCTYPE html>
    <html>
    <head>
      <title>${transactionType.value === 'buy' ? t('exchange.receipt.buyTitle') : t('exchange.receipt.sellTitle')}</title>
      <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
          font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
          padding: 40px;
          line-height: 1.6;
        }
        .invoice-header { 
          text-align: center; 
          margin-bottom: 40px;
          padding-bottom: 20px;
          border-bottom: 2px solid #e5e7eb;
        }
        .invoice-header h2 { 
          font-size: 24px;
          margin-bottom: 20px;
          letter-spacing: 2px;
          font-weight: 600;
        }
        .invoice-info { 
          display: flex; 
          justify-content: center; 
          gap: 60px;
        }
        .invoice-info p {
          font-size: 14px;
          color: #4b5563;
        }
        @media print { 
          body { 
            margin: 0;
            padding: 20px;
          }
        }
      </style>
    </head>
    <body>
      <div class="invoice-header">
        <h2>${transactionType.value === 'buy' ? 'DÖVİZ ALIM BELGESİ' : 'DÖVİZ SATIM BELGESİ'}</h2>
        <div class="invoice-info">
          <p><strong>Tarih:</strong> ${new Date().toLocaleDateString('tr-TR')}</p>
          <p><strong>Saat:</strong> ${new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}</p>
        </div>
      </div>
      ${tableHTML}
    </body>
    </html>
  `)
  
  printWindow.document.close()
  printWindow.focus()
  
  setTimeout(() => {
    printWindow.print()
    printWindow.close()
    notification.printSuccess()
  }, 250)
}

function buildReceiptFromCurrent() {
  const items = exchangeItems.value.map(item => {
    const sc = getCurrencyById(item.sourceCurrencyId)
    const tc = getCurrencyById(item.targetCurrencyId)
    return {
      source: sc?.currencyCode || '-',
      target: tc?.currencyCode || '-',
      amount: item.sourceAmount,
      rate: item.exchangeRate,
      total: item.targetAmount,
      customRate: item.customRate !== null && item.customRate !== ''
    }
  })
  const totals = grandTotal.value.map(t => ({ code: t.currencyCode, amount: t.amount }))
  lastReceipt.value = {
    type: terminalMode.value === 'arbitrage' ? 'arbitrage' : transactionType.value,
    items,
    totals,
    timestamp: new Date().toISOString(),
    notes: notes.value || ''
  }
}

function buildReceiptFromBatch() {
  const items = batchQueue.value.map(item => {
    const sc = getCurrencyById(item.sourceCurrencyId)
    const tc = getCurrencyById(item.targetCurrencyId)
    return {
      source: sc?.currencyCode || '-',
      target: tc?.currencyCode || '-',
      amount: item.sourceAmount,
      rate: item.exchangeRate,
      total: item.targetAmount,
      customRate: item.customRate !== null && item.customRate !== ''
    }
  })
  lastReceipt.value = {
    type: 'buy',
    items,
    totals: batchTotal.value.map(t => ({ code: t.code, amount: t.amount })),
    timestamp: new Date().toISOString(),
    notes: ''
  }
}

function printReceipt(receipt?: typeof lastReceipt.value) {
  const data = receipt || lastReceipt.value
  if (!data || data.items.length === 0) return

  const printWindow = window.open('', '_blank', 'width=800,height=600')
  if (!printWindow) { notification.error('Yazdırma penceresi açılamadı'); return }

  const typeLabel = data.type === 'buy' ? 'DÖVİZ ALIM FİŞİ' : data.type === 'sell' ? 'DÖVİZ SATIM FİŞİ' : 'ARBİTRAJ İŞLEM FİŞİ'
  const typeBadge = data.type === 'buy' ? '#16a34a' : data.type === 'sell' ? '#dc2626' : '#d97706'
  const ts = new Date(data.timestamp)
  const dateStr = ts.toLocaleDateString('tr-TR')
  const timeStr = ts.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })

  let rows = ''
  data.items.forEach((item, i) => {
    const customBadge = item.customRate ? '<span style="background:#fef3c7;color:#92400e;padding:1px 5px;border-radius:3px;font-size:9px;margin-left:4px">Ö</span>' : ''
    rows += `<tr>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;text-align:center;color:#6b7280;font-size:12px">${i + 1}</td>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;font-weight:600">${item.source}</td>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;text-align:center;color:#9ca3af">→</td>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;font-weight:600">${item.target}</td>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;text-align:right;font-family:monospace">${formatNumber(item.amount)}</td>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;text-align:right;font-family:monospace">${formatNumber(item.rate, 4)}${customBadge}</td>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;text-align:right;font-family:monospace;font-weight:700">${formatNumber(item.total)}</td>
    </tr>`
  })

  let totalsHTML = ''
  data.totals.forEach(t => {
    totalsHTML += `<div style="display:flex;justify-content:space-between;padding:6px 0;font-size:14px">
      <span style="font-weight:600">${t.code}</span>
      <span style="font-family:monospace;font-weight:700">${formatNumber(t.amount)}</span>
    </div>`
  })

  let marginHTML = ''
  if (data.type === 'arbitrage' && arbMarginPercent.value > 0) {
    marginHTML = `<div style="margin-top:16px;padding:12px;background:#fffbeb;border:1px solid #fcd34d;border-radius:8px">
      <div style="font-weight:700;font-size:13px;color:#92400e;margin-bottom:8px">📊 Kar Marjı Hesabı (% ${formatNumber(arbMarginPercent.value, 2)})</div>`
    data.items.forEach(item => {
      const marginAmount = item.total * (arbMarginPercent.value / 100)
      const customerRate = item.rate * (1 + arbMarginPercent.value / 100)
      const customerTotal = item.amount * customerRate
      marginHTML += `<div style="display:flex;justify-content:space-between;padding:4px 0;font-size:12px;font-family:monospace">
        <span>${item.amount} ${item.source} → ${item.target}</span>
        <span>Müşteri Kur: <strong>${formatNumber(customerRate, 4)}</strong> | Müşteri Tutar: <strong>${formatNumber(customerTotal)}</strong> | Kar: <strong style="color:#16a34a">+${formatNumber(marginAmount)}</strong> ${item.target}</span>
      </div>`
    })
    const totalMarginProfit = data.items.reduce((s, item) => s + item.total * (arbMarginPercent.value / 100), 0)
    marginHTML += `<div style="margin-top:8px;padding-top:8px;border-top:1px solid #fcd34d;font-weight:700;text-align:right;font-size:13px;color:#16a34a">Toplam Kar: +${formatNumber(totalMarginProfit)} ${data.items[0]?.target || ''}</div></div>`
  }

  printWindow.document.write(`<!DOCTYPE html><html><head><title>${typeLabel}</title>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;padding:32px;line-height:1.5;color:#1f2937}
@media print{body{padding:16px}@page{margin:10mm}}
</style></head><body>
<div style="text-align:center;margin-bottom:24px;padding-bottom:16px;border-bottom:2px solid #e5e7eb">
  <div style="font-size:11px;color:#9ca3af;letter-spacing:2px;margin-bottom:4px">BAŞKENT ENERJİ DÖVİZ</div>
  <h2 style="font-size:20px;font-weight:700;letter-spacing:1px;margin-bottom:12px">
    <span style="display:inline-block;padding:4px 16px;border-radius:6px;background:${typeBadge};color:white">${typeLabel}</span>
  </h2>
  <div style="display:flex;justify-content:center;gap:40px;font-size:13px;color:#6b7280">
    <span>📅 ${dateStr}</span><span>🕐 ${timeStr}</span>
  </div>
</div>
<table style="width:100%;border-collapse:collapse;margin-bottom:20px">
  <thead><tr style="background:#f9fafb">
    <th style="padding:8px 6px;text-align:center;font-size:11px;color:#9ca3af;border-bottom:2px solid #e5e7eb">#</th>
    <th style="padding:8px 6px;text-align:left;font-size:11px;color:#9ca3af;border-bottom:2px solid #e5e7eb">Kaynak</th>
    <th style="padding:8px 6px;border-bottom:2px solid #e5e7eb"></th>
    <th style="padding:8px 6px;text-align:left;font-size:11px;color:#9ca3af;border-bottom:2px solid #e5e7eb">Hedef</th>
    <th style="padding:8px 6px;text-align:right;font-size:11px;color:#9ca3af;border-bottom:2px solid #e5e7eb">Miktar</th>
    <th style="padding:8px 6px;text-align:right;font-size:11px;color:#9ca3af;border-bottom:2px solid #e5e7eb">Kur</th>
    <th style="padding:8px 6px;text-align:right;font-size:11px;color:#9ca3af;border-bottom:2px solid #e5e7eb">Tutar</th>
  </tr></thead>
  <tbody>${rows}</tbody>
</table>
<div style="max-width:280px;margin-left:auto;padding:12px 16px;background:#f9fafb;border-radius:8px;border:1px solid #e5e7eb">
  <div style="font-size:11px;color:#9ca3af;margin-bottom:6px;text-transform:uppercase;letter-spacing:1px">Toplam</div>
  ${totalsHTML}
</div>
${marginHTML}
${data.notes ? `<div style="margin-top:16px;padding:10px 14px;background:#f3f4f6;border-radius:6px;font-size:12px;color:#6b7280"><strong>Not:</strong> ${data.notes}</div>` : ''}
<div style="margin-top:32px;text-align:center;font-size:10px;color:#d1d5db;border-top:1px solid #f3f4f6;padding-top:12px">Bu belge bilgi amaçlıdır • Başkent Enerji Döviz</div>
</body></html>`)
  printWindow.document.close()
  printWindow.focus()
  setTimeout(() => { printWindow.print(); printWindow.close(); notification.printSuccess() }, 300)
}

// Watch office selection
watch(selectedOfficeId, async (newOfficeId) => {
  if (newOfficeId) {
    // Save to localStorage
    localStorage.setItem('selectedOfficeId', newOfficeId)
    
    const office = exchangeStore.offices.find(o => String(o.officeId ?? o.id) === String(newOfficeId))
    if (office) {
      exchangeStore.selectedOffice = office
    }
    
    await exchangeStore.loadVaults(newOfficeId)
    // Pass the officeId directly to loadExchangeRates
    await exchangeStore.loadExchangeRates(newOfficeId)
    
    // Try to restore saved vault or select first one
    const savedVaultId = localStorage.getItem('selectedVaultId')
    const savedVault = savedVaultId ? availableVaults.value.find(v => (v.vaultId || v.id) === savedVaultId) : null
    
    if (savedVault) {
      selectedVaultId.value = savedVaultId || ''
    } else if (availableVaults.value.length > 0) {
      selectedVaultId.value = String(availableVaults.value[0].vaultId ?? availableVaults.value[0].id ?? '')
      localStorage.setItem('selectedVaultId', selectedVaultId.value)
    }
    
    exchangeItems.value.forEach(item => {
      if (item.sourceCurrencyId && item.targetCurrencyId) {
        updateExchangeRate(item)
      }
    })

    await Promise.all([loadWacData(), fetchExternalMarketRates()])
  }
})

// Watch vault selection
watch(selectedVaultId, async (newVaultId) => {
  if (newVaultId) {
    localStorage.setItem('selectedVaultId', newVaultId)
    // Update current vault reference
    currentVault.value = availableVaults.value.find(v => (v.vaultId || v.id) === newVaultId)
    // Check if vault counting is required when vault changes
    await checkVaultCounting()
  }
})

// Check vault counting requirement
const checkVaultCounting = async () => {
  if (!selectedOfficeId.value || !selectedVaultId.value) return

  try {
    const vaults = await apiService.getVaultsByOfficeId(selectedOfficeId.value)
    exchangeStore.vaults = vaults

    const selectedVault = vaults.find((v: any) => (v.vaultId || v.id) === selectedVaultId.value)

    if (selectedVault && selectedVault.shouldCount) {
      currentVault.value = selectedVault
      currentVault.value.isManual = false

      if (authStore.isAdmin) {
        isPageHidden.value = false
        showVaultCountWarning.value = true
      } else {
        isPageHidden.value = true
        showVaultCountWarning.value = false
      }
    } else {
      isPageHidden.value = false
      showVaultCountWarning.value = false
    }
  } catch (error) {
    console.error('Failed to check vault counting:', error)
  }
}

// Handle vault counting completion
const handleVaultCountComplete = async () => {
  isPageHidden.value = false
  showVaultCountWarning.value = false
  // Refresh vault data
  await checkVaultCounting()
  notification.success('Kasa sayımı tamamlandı')
}

// Handle waiting customer button
const handleWaitingCustomer = () => {
  // Temporarily show the exchange page
  isPageHidden.value = false
  
  // Show countdown notification
  const showCountdown = () => {
    if (vaultCountingModalRef.value) {
      const remaining = vaultCountingModalRef.value.getRemainingTime()
      if (remaining > 0) {
        const mins = Math.floor(remaining / 60)
        const secs = remaining % 60
        notification.warning(
          `Kasa sayımı ${mins}:${secs.toString().padStart(2, '0')} sonra tekrar açılacak`, 
          {
            duration: 3000,
            title: 'Kasa Sayım Hatırlatması'
          }
        )
      }
    }
  }
  
  // Show initial notification
  showCountdown()
  
  // Show periodic reminders
  const reminderInterval = setInterval(() => {
    if (vaultCountingModalRef.value) {
      const remaining = vaultCountingModalRef.value.getRemainingTime()
      if (remaining <= 0) {
        clearInterval(reminderInterval)
      } else if (remaining === 120 || remaining === 60 || remaining === 30) {
        showCountdown()
      }
    }
  }, 1000)
}

// Open vault counting modal
const openVaultCountingModal = async () => {
  // Ensure we have vault data
  if (selectedVaultId.value && availableVaults.value.length > 0) {
    const vault = availableVaults.value.find(v => (v.vaultId || v.id) === selectedVaultId.value)
    if (vault) {
      currentVault.value = { ...vault, isManual: false }
    }
  }
  
  // Wait for next tick then try to open modal
  await nextTick()
  
  if (vaultCountingModalRef.value && vaultCountingModalRef.value.open) {
    // Force open to bypass temporary closed state
    vaultCountingModalRef.value.open(true)
  }
}

// Open manual vault counting
const openManualVaultCounting = async () => {
  // Ensure we have vault data
  if (selectedVaultId.value && availableVaults.value.length > 0) {
    const vault = availableVaults.value.find(v => (v.vaultId || v.id) === selectedVaultId.value)
    if (vault) {
      currentVault.value = { ...vault, isManual: true }
    }
  }
  
  // Wait for next tick then try to open modal
  await nextTick()
  
  if (vaultCountingModalRef.value && vaultCountingModalRef.value.open) {
    // Force open for manual counting - bypasses any temporary closed state
    vaultCountingModalRef.value.open(true)
  }
}

// Watch transaction type changes
watch(transactionType, () => {
  // Reset and recalculate all rates when switching between buy/sell
  exchangeItems.value.forEach(item => {
    if (item.sourceCurrencyId && item.targetCurrencyId) {
      item.rateManuallySet = false
      item.customRate = null
      updateExchangeRate(item)
    }
  })

  // Re-fetch external rates to show appropriate buy/sell rates
  if (allExternalRates.value.length > 0) {
    // Reorganize existing rates instead of re-fetching
    const ratesMap: Record<string, any[]> = {}
    allExternalRates.value.forEach((rate: any) => {
      const key = `${rate.currencyCode}-${rate.targetCurrencyCode || 'TRY'}`
      if (!ratesMap[key]) {
        ratesMap[key] = []
      }
      ratesMap[key].push(rate)
    })
    externalMarketRates.value = ratesMap
  }
})

// WAC data
async function loadWacData() {
  if (!selectedVaultId.value) return
  try {
    const data = await apiService.getAllWacs(selectedVaultId.value)
    wacData.value = data && typeof data === 'object' ? data : {}
  } catch (err) {
    console.error('WAC data load failed:', err)
  }
}

function getWacForCurrency(currencyId: string): number {
  return wacData.value[currencyId] ?? 0
}

function isSellBelowWac(item: ExchangeItem): boolean {
  if (transactionType.value !== 'sell') return false
  const foreignId = item.sourceCurrencyId
  if (!foreignId || foreignId === tryId.value) return false
  const wac = getWacForCurrency(foreignId)
  if (wac <= 0) return false
  let rate = item.exchangeRate
  if (item.customRate !== null && item.customRate !== '') {
    const parsed = parseNum(item.customRate)
    if (parsed > 0) rate = parsed
  }
  return rate > 0 && rate < wac
}

// Day closure check
async function checkDayStatus() {
  if (!selectedOfficeId.value) return
  try {
    dayStatus.value = await apiService.getDayStatus(selectedOfficeId.value)
    dayBlocked.value = dayStatus.value?.canTransact === false
  } catch (err) {
    console.error('Day status check failed:', err)
  }
}

function openDayClosureModal() {
  showDayClosureModal.value = true
}

async function onDayClosureDone() {
  showDayClosureModal.value = false
  await checkDayStatus()
}

// Listen for USDT modal open event
const handleOpenUSDTModal = () => {
  openUSDTModal()
}

// Initialize
onMounted(async () => {
  // Add event listeners
  window.addEventListener('open-usdt-modal', handleOpenUSDTModal)
  window.addEventListener('keydown', handleKeyDown)
  
  // Start vault check interval (every 5 minutes)
  vaultCheckInterval.value = setInterval(() => {
    checkVaultCounting()
  }, 5 * 60 * 1000) // 5 minutes
  
  isInitialLoading.value = true
  try {
    // Load offices — non-admin/non-owner users see only assigned offices
    const isFullAccess = authStore.isAdmin || authStore.isOwner
    let officesData: any

    if (!isFullAccess) {
      try {
        const myOffices = await apiService.getMyOfficeAccess()
        const list = Array.isArray(myOffices) ? myOffices : []
        authStore.userOffices = list
        if (list.length > 0) {
          officesData = list.map((o: any) => ({
            id: o.officeId || o.id,
            officeId: o.officeId || o.id,
            officeName: o.officeName
          }))
        } else {
          officesData = await apiService.getOffices()
        }
      } catch {
        officesData = await apiService.getOffices()
      }
    } else {
      officesData = await apiService.getOffices()
    }
    
    const [currenciesData, vaultsDataRes] = await Promise.all([
      apiService.getCurrencies(),
      apiService.getVaults()
    ])

    currencies.value = currenciesData
    exchangeStore.offices = officesData
    exchangeStore.vaults = vaultsDataRes
    
    // Find TRY currency
    const tryCurrency = currencies.value.find(c => c.currencyCode === 'TRY')
    if (tryCurrency) {
      tryId.value = tryCurrency.id
      // Set TRY as default target for all items
      exchangeItems.value[0].targetCurrencyId = tryId.value
    }
    
    // Auto-select office
    if (exchangeStore.offices.length > 0) {
      // Try to use saved office first
      const savedOfficeId = localStorage.getItem('selectedOfficeId')
      const savedOffice = savedOfficeId
        ? exchangeStore.offices.find(o => String(o.officeId ?? o.id) === String(savedOfficeId))
        : null

      if (savedOffice) {
        selectedOfficeId.value = String(savedOffice.officeId ?? savedOffice.id)
      } else {
        // Fall back to first office
        const firstOffice = exchangeStore.offices[0]
        selectedOfficeId.value = String(firstOffice.officeId ?? firstOffice.id)
      }

      const office = exchangeStore.offices.find(o => String(o.officeId ?? o.id) === String(selectedOfficeId.value))
      if (office) {
        exchangeStore.selectedOffice = office
      }

      await exchangeStore.loadVaults(selectedOfficeId.value)

      // Try to restore saved vault or select first one
      const savedVaultId = localStorage.getItem('selectedVaultId')
      const savedVault = savedVaultId
        ? availableVaults.value.find(v => String(v.vaultId ?? v.id) === String(savedVaultId))
        : null

      if (savedVault) {
        selectedVaultId.value = String(savedVault.vaultId ?? savedVault.id)
      } else if (availableVaults.value.length > 0) {
        selectedVaultId.value = String(availableVaults.value[0].vaultId ?? availableVaults.value[0].id)
        localStorage.setItem('selectedVaultId', selectedVaultId.value)
      }
    }
    
    await exchangeStore.loadExchangeRates(selectedOfficeId.value)
    
    // Start auto-refresh interval
    rateRefreshInterval.value = setInterval(() => {
      refreshRates()
    }, 60000)
    
    // Start countdown interval
    countdownInterval.value = setInterval(() => {
      refreshCountdown.value--
      if (refreshCountdown.value <= 0) {
        refreshCountdown.value = 60
      }
    }, 1000)
    
    // Delay initial vault count check to ensure everything is loaded
    setTimeout(() => {
      checkVaultCounting()
    }, 1000)

    // Check day closure status, load WAC data, and today's stats
    await Promise.all([checkDayStatus(), loadWacData(), loadTodayStats()])

    // Fetch external market rates
    fetchExternalMarketRates()

  } catch (error) {
    console.error('Failed to initialize:', error)
    notification.error('Veriler yüklenirken hata oluştu. Lütfen sayfayı yenileyin.')
  } finally {
    isInitialLoading.value = false
  }
})

// Cleanup on unmount
onUnmounted(() => {
  if (rateRefreshInterval.value) {
    clearInterval(rateRefreshInterval.value)
  }
  if (countdownInterval.value) {
    clearInterval(countdownInterval.value)
  }
  if (balanceCheckTimeout.value) {
    clearTimeout(balanceCheckTimeout.value)
  }
  if (vaultCheckInterval.value) {
    clearInterval(vaultCheckInterval.value)
  }
  // Remove event listeners
  window.removeEventListener('open-usdt-modal', handleOpenUSDTModal)
  window.removeEventListener('keydown', handleKeyDown)
})

// Watch for currency changes
watch(() => exchangeItems.value.map(item => ({
  sourceCurrencyId: item.sourceCurrencyId,
  targetCurrencyId: item.targetCurrencyId
})), async (newValue, oldValue) => {
  for (let i = 0; i < exchangeItems.value.length; i++) {
    const item = exchangeItems.value[i]
    const oldItem = oldValue?.[i]
    
    if (!oldItem || 
        item.sourceCurrencyId !== oldItem.sourceCurrencyId || 
        item.targetCurrencyId !== oldItem.targetCurrencyId) {
      if (!item.rateManuallySet) {
        await updateExchangeRate(item)
      }
    }
  }
}, { deep: true })
</script>

<template>
  <!-- Loading State -->
  <div v-if="isInitialLoading" class="ex-page">
    <div class="ex-overlay-center">
      <div class="ex-spinner-wrap">
        <svg class="ex-spinner" viewBox="0 0 24 24" fill="none">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
        </svg>
      </div>
      <p class="ex-overlay-text">{{ t('exchange.messages.loadingData') }}</p>
    </div>
  </div>

  <!-- Vault Counting Required -->
  <div v-else-if="isPageHidden" class="ex-page">
    <div class="ex-overlay-center">
      <div class="ex-overlay-icon ex-overlay-icon--red">
        <span class="material-symbols-outlined ex-icon-filled" style="font-size:40px">inventory</span>
      </div>
      <h2 class="ex-overlay-title">Kasa Sayımı Zorunlu</h2>
      <p class="ex-overlay-desc">Devam etmek için kasanızı saymanız gerekmektedir.</p>
      <button @click="openVaultCountingModal" class="ex-btn ex-btn--red">
        <span class="material-symbols-outlined ex-icon-filled">calculate</span>
        Kasa Sayımına Başla
      </button>
    </div>
  </div>

  <!-- Day Closure Required -->
  <div v-else-if="dayBlocked" class="ex-page">
    <div class="ex-overlay-center">
      <div class="ex-overlay-icon ex-overlay-icon--amber">
        <span class="material-symbols-outlined ex-icon-filled" style="font-size:40px">lock_clock</span>
      </div>
      <h2 class="ex-overlay-title">Gün Kapanışı Gerekli</h2>
      <p class="ex-overlay-desc">{{ dayStatus?.blockReason || 'Önceki günün kapanışı yapılmadan işlem yapılamaz.' }}</p>
      <p v-if="dayStatus?.unclosedDayCount > 1" class="ex-overlay-hint">
        {{ dayStatus.unclosedDayCount }} gün kapanmamış — ara günler otomatik kapatılacak.
      </p>
      <button @click="openDayClosureModal" class="ex-btn ex-btn--amber">
        <span class="material-symbols-outlined ex-icon-filled">lock</span>
        Gün Kapanışını Yap
      </button>
    </div>
  </div>

  <!-- Main Content -->
  <div v-else class="ex-page">

    <!-- Top Bar: Office/Vault + Actions -->
    <div class="ex-topbar">
      <div class="ex-topbar-left">
        <div class="ex-topbar-icon">
          <span class="material-symbols-outlined ex-icon-filled">currency_exchange</span>
        </div>
        <div>
          <h2 class="ex-topbar-title">Döviz İşlem Terminali</h2>
          <p class="ex-topbar-subtitle">Alış · Satış · Arbitraj · Toplu İşlem</p>
        </div>
      </div>
      <div class="ex-topbar-right">
        <button @click="refreshRates" :disabled="!selectedOfficeId || loadingExternalRates" class="ex-topbar-btn" :class="{ 'ex-topbar-btn--syncing': loadingExternalRates }" title="Kurları Yenile">
          <span class="material-symbols-outlined" :class="{ 'animate-spin': loadingExternalRates }">sync</span>
          <span v-if="refreshCountdown < 60" class="ex-countdown">{{ refreshCountdown }}s</span>
        </button>
        <button @click="openManualVaultCounting" :disabled="!selectedVaultId" class="ex-topbar-btn" title="Kasa Sayımı">
          <span class="material-symbols-outlined">calculate</span>
        </button>
        <button @click="transactionHistoryRef?.printAllTransactions?.()" :disabled="!selectedOfficeId" class="ex-topbar-btn" title="Yazdır">
          <span class="material-symbols-outlined">print</span>
        </button>
      </div>
    </div>

    <!-- Warning: No office/vault -->
    <div v-if="!selectedOfficeId || !selectedVaultId" class="ex-warning-banner">
      <span class="material-symbols-outlined ex-icon-filled">warning</span>
      <div>
        <p class="ex-warning-title">{{ t('exchange.officeVault.warningTitle') }}</p>
        <p class="ex-warning-desc">
          {{ !selectedOfficeId ? t('exchange.officeVault.warningOffice') : '' }}
          {{ !selectedVaultId ? t('exchange.officeVault.warningVault') : '' }}
        </p>
      </div>
    </div>

    <!-- Vault Count Warning (admin) -->
    <div v-if="showVaultCountWarning" class="ex-warning-banner ex-warning-banner--amber">
      <span class="material-symbols-outlined ex-icon-filled">inventory</span>
      <div class="flex-1">
        <p class="ex-warning-title">Kasa Sayımı Gerekli</p>
        <p class="ex-warning-desc">Bu kasa için sayım henüz yapılmamış.</p>
      </div>
      <button @click="openManualVaultCounting" class="ex-btn ex-btn--amber ex-btn--sm">
        <span class="material-symbols-outlined">calculate</span>
        Sayım Yap
      </button>
    </div>

    <!-- ═══ Position Bar ═══ -->
    <div v-if="positionData.length > 0" class="ex-pos-bar">
      <div class="ex-pos-bar-strip">
        <div class="ex-pos-bar-label">
          <span class="material-symbols-outlined ex-icon-filled">account_balance_wallet</span>
          <span>Pozisyonlar</span>
        </div>
        <div class="ex-pos-bar-items">
          <div v-for="pos in positionData" :key="pos.currencyId" class="ex-pos-item" @click="selectQuickCurrency(pos.code)">
            <i v-if="getCurrencyCountryCode(pos.code)" :class="`fi fi-${getCurrencyCountryCode(pos.code)}`" class="ex-pos-flag"></i>
            <span class="ex-pos-code">{{ pos.code }}</span>
            <span class="ex-pos-balance">{{ formatNumber(pos.balance) }}</span>
            <span class="ex-pos-sep">·</span>
            <span class="ex-pos-wac">{{ formatNumber(pos.wac, 2) }}</span>
            <span :class="pos.unrealizedPnl >= 0 ? 'ex-pnl--pos' : 'ex-pnl--neg'" class="ex-pos-pnl">
              {{ pos.unrealizedPnl >= 0 ? '+' : '' }}{{ formatNumber(pos.unrealizedPnl, 0) }}₺
            </span>
          </div>
        </div>
        <div class="ex-pos-bar-total">
          <span class="ex-pos-bar-total-label">Σ</span>
          <span class="ex-pos-bar-total-val">{{ formatNumber(totalPositionValue) }} ₺</span>
          <span :class="totalUnrealizedPnl >= 0 ? 'ex-pnl--pos' : 'ex-pnl--neg'" class="ex-pos-pnl">
            {{ totalUnrealizedPnl >= 0 ? '+' : '' }}{{ formatNumber(totalUnrealizedPnl) }}₺
          </span>
        </div>
      </div>
    </div>

    <!-- ═══ Terminal Mode Tabs ═══ -->
    <div class="ex-terminal-tabs">
      <button @click="terminalMode = 'standard'" class="ex-tab" :class="{ 'ex-tab--active': terminalMode === 'standard' }">
        <span class="material-symbols-outlined ex-icon-filled" style="font-size:18px">swap_vert</span>
        Alış / Satış
        <span class="ex-tab-key">F2/F3</span>
      </button>
      <button @click="terminalMode = 'arbitrage'" class="ex-tab" :class="{ 'ex-tab--active-arb': terminalMode === 'arbitrage' }">
        <span class="material-symbols-outlined ex-icon-filled" style="font-size:18px">currency_exchange</span>
        Arbitraj
        <span class="ex-tab-key">F4</span>
      </button>
      <button @click="terminalMode = 'batch'" class="ex-tab" :class="{ 'ex-tab--active-batch': terminalMode === 'batch' }">
        <span class="material-symbols-outlined ex-icon-filled" style="font-size:18px">playlist_add</span>
        Toplu İşlem
      </button>
    </div>

    <!-- ═══ Standard Mode: Buy/Sell Toggle + Quick Chips ═══ -->
    <template v-if="terminalMode === 'standard'">
      <div class="ex-type-toggle">
        <button
          @click="transactionType = 'buy'"
          class="ex-type-btn"
          :class="transactionType === 'buy' ? 'ex-type-btn--buy-active' : 'ex-type-btn--inactive'"
        >
          <span class="material-symbols-outlined ex-icon-filled">download</span>
          <span class="ex-type-label">{{ t('exchange.transactionType.buy') }}</span>
          <span class="ex-type-desc">{{ t('exchange.transactionType.buyDesc') }}</span>
        </button>
        <button
          @click="transactionType = 'sell'"
          class="ex-type-btn"
          :class="transactionType === 'sell' ? 'ex-type-btn--sell-active' : 'ex-type-btn--inactive'"
        >
          <span class="material-symbols-outlined ex-icon-filled">upload</span>
          <span class="ex-type-label">{{ t('exchange.transactionType.sell') }}</span>
          <span class="ex-type-desc">{{ t('exchange.transactionType.sellDesc') }}</span>
        </button>
      </div>

    </template>

    <!-- ═══ Arbitrage Mode Header ═══ -->
    <div v-if="terminalMode === 'arbitrage'" class="ex-arb-header">
      <div class="ex-arb-info">
        <span class="material-symbols-outlined ex-icon-filled" style="font-size:20px;color:#f59e0b">currency_exchange</span>
        <div>
          <p class="ex-arb-title">Arbitraj Modu</p>
          <p class="ex-arb-desc">Döviz↔Döviz direkt çevrim. Çapraz kur otomatik hesaplanır.</p>
        </div>
      </div>
      <div class="ex-arb-margin">
        <label class="ex-arb-margin-label">
          <span class="material-symbols-outlined ex-icon-filled" style="font-size:14px">percent</span>
          Kar Marjı
        </label>
        <input
          type="text"
          :value="arbMarginPercent || ''"
          @input="arbMarginPercent = parseNum(($event.target as HTMLInputElement).value)"
          class="ex-input ex-input--mono ex-arb-margin-input"
          placeholder="0"
          inputmode="decimal"
        />
        <button
          @click="buildReceiptFromCurrent(); printReceipt()"
          :disabled="!exchangeItems.some(i => i.sourceCurrencyId && i.targetCurrencyId && i.sourceAmount > 0)"
          class="ex-btn ex-btn--amber ex-btn--sm"
          title="Arbitraj fişi oluştur ve yazdır"
        >
          <span class="material-symbols-outlined ex-icon-filled" style="font-size:16px">receipt_long</span>
          Fiş Al
        </button>
      </div>
    </div>

    <!-- ═══ Batch Mode Header + Form ═══ -->
    <div v-if="terminalMode === 'batch'" class="ex-batch-section">
      <div class="ex-batch-form">
        <div class="ex-batch-form-row">
          <div class="ex-batch-toggle">
            <button @click="batchType = 'buy'" class="ex-batch-type-btn" :class="{ 'ex-batch-type--buy': batchType === 'buy' }">Alış</button>
            <button @click="batchType = 'sell'" class="ex-batch-type-btn" :class="{ 'ex-batch-type--sell': batchType === 'sell' }">Satış</button>
          </div>
          <CurrencySelector
            :modelValue="batchSourceCurrencyId"
            @update:modelValue="batchSourceCurrencyId = $event"
            :currencies="filteredCurrencies"
            placeholder="Döviz"
            :compact="true"
            :vaultBalances="selectedVaultBalances"
          />
          <div class="ex-batch-arrow">→</div>
          <CurrencySelector
            :modelValue="batchTargetCurrencyId"
            @update:modelValue="batchTargetCurrencyId = $event"
            :currencies="currencies"
            placeholder="Hedef"
            :compact="true"
          />
          <input type="text" :value="batchAmount || ''" @input="batchAmount = parseNum(($event.target as HTMLInputElement).value)" class="ex-input ex-input--mono ex-batch-input" placeholder="Miktar" inputmode="decimal" />
          <input type="text" :value="batchCustomRate !== null && batchCustomRate !== '' ? batchCustomRate : batchRate || ''" @input="batchCustomRate = ($event.target as HTMLInputElement).value" class="ex-input ex-input--mono ex-batch-input ex-batch-input--rate" placeholder="Kur" inputmode="decimal" :class="{ 'ex-input--custom': batchCustomRate !== null && batchCustomRate !== '' }" />
          <input type="text" v-model="batchNote" class="ex-input ex-batch-input ex-batch-input--note" placeholder="Not..." />
          <button @click="addToBatch" class="ex-btn ex-btn--indigo ex-btn--sm">
            <span class="material-symbols-outlined" style="font-size:18px">add</span>
            Ekle
          </button>
        </div>
      </div>

      <!-- Batch Queue -->
      <div v-if="batchQueue.length > 0" class="ex-batch-queue">
        <div class="ex-batch-queue-header">
          <span>Kuyruk ({{ batchQueue.length }} işlem)</span>
          <button @click="submitBatch" :disabled="isLoading" class="ex-btn ex-btn--indigo ex-btn--sm">
            <span class="material-symbols-outlined ex-icon-filled" style="font-size:16px">{{ isLoading ? 'refresh' : 'send' }}</span>
            Tümünü Onayla
          </button>
        </div>
        <div v-for="item in batchQueue" :key="item.id" class="ex-batch-item">
          <span class="ex-batch-item-type" :class="item.type === 'buy' ? 'ex-item-badge--buy' : 'ex-item-badge--sell'">{{ item.type === 'buy' ? 'A' : 'S' }}</span>
          <span class="ex-batch-item-detail">
            {{ formatNumber(item.sourceAmount) }} {{ getCurrencyById(item.sourceCurrencyId)?.currencyCode }}
            → {{ formatNumber(item.targetAmount) }} {{ getCurrencyById(item.targetCurrencyId)?.currencyCode }}
            <span class="ex-batch-item-rate">@ {{ formatNumber(item.exchangeRate, 4) }}</span>
          </span>
          <span v-if="item.note" class="ex-batch-item-note">{{ item.note }}</span>
          <button @click="removeBatchItem(item.id)" class="ex-item-delete">
            <span class="material-symbols-outlined" style="font-size:16px">close</span>
          </button>
        </div>
        <div class="ex-batch-totals">
          <span v-for="total in batchTotal" :key="total.code" class="ex-batch-total-item" :class="total.amount >= 0 ? 'ex-pnl--pos' : 'ex-pnl--neg'">
            {{ total.amount >= 0 ? '+' : '' }}{{ formatNumber(total.amount) }} {{ total.code }}
          </span>
        </div>
      </div>
    </div>

    <!-- Main 2-Column Layout (Standard + Arbitrage modes) -->
    <div v-if="terminalMode !== 'batch'" class="ex-layout">

      <!-- Left: Exchange Form -->
      <div class="ex-form-col">

        <!-- Exchange Card -->
        <div class="ex-card">
          <div class="ex-card-header">
            <h3 class="ex-card-title">{{ t('exchange.operations.title') }}</h3>
            <button @click="addExchangeItem" class="ex-btn ex-btn--indigo ex-btn--sm">
              <span class="material-symbols-outlined" style="font-size:18px">add</span>
              {{ t('exchange.operations.addButton') }}
            </button>
          </div>

          <div class="ex-card-body">
            <div v-for="(item, index) in exchangeItems" :key="item.id" class="ex-item" :class="transactionType === 'buy' ? 'ex-item--buy' : 'ex-item--sell'">

              <!-- Item Header -->
              <div class="ex-item-header">
                <div class="ex-item-badge" :class="transactionType === 'buy' ? 'ex-item-badge--buy' : 'ex-item-badge--sell'">
                  {{ transactionType === 'buy' ? 'ALIŞ' : 'SATIŞ' }} #{{ index + 1 }}
                </div>
                <button v-if="exchangeItems.length > 1" @click="removeExchangeItem(index)" class="ex-item-delete">
                  <span class="material-symbols-outlined">close</span>
                </button>
              </div>

              <!-- Currency Row -->
              <div class="ex-item-currencies">
                <div class="ex-field">
                  <label class="ex-field-label">
                    {{ t('exchange.operations.receivedCurrency') }}
                    <span class="ex-field-hint">({{ transactionType === 'buy' ? t('exchange.operations.fromCustomer') : t('exchange.operations.toVault') }})</span>
                  </label>
                  <CurrencySelector
                    :modelValue="transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId"
                    @update:modelValue="transactionType === 'buy' ? updateSourceCurrency(item, $event) : updateTargetCurrency(item, $event)"
                    :currencies="transactionType === 'buy' ? filteredCurrencies : currencies"
                    :placeholder="t('exchange.operations.selectCurrency')"
                    :disabled="!selectedOfficeId || !selectedVaultId"
                    :vaultBalances="selectedVaultBalances"
                  />
                </div>

                <div class="ex-arrow-divider">
                  <span class="material-symbols-outlined">swap_horiz</span>
                </div>

                <div class="ex-field">
                  <label class="ex-field-label">
                    {{ t('exchange.operations.givenCurrency') }}
                    <span class="ex-field-hint">({{ transactionType === 'buy' ? t('exchange.operations.fromVault') : t('exchange.operations.toCustomer') }})</span>
                  </label>
                  <CurrencySelector
                    :modelValue="transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId"
                    @update:modelValue="transactionType === 'buy' ? updateTargetCurrency(item, $event) : updateSourceCurrency(item, $event)"
                    :currencies="transactionType === 'buy' ? currencies : filteredCurrencies"
                    :placeholder="t('exchange.operations.selectCurrency')"
                    :disabled="!selectedOfficeId || !selectedVaultId"
                    :vaultBalances="selectedVaultBalances"
                  />
                </div>
              </div>

              <!-- Amount + Rate Row -->
              <div class="ex-item-numbers">
                <div class="ex-field ex-field--grow">
                  <label class="ex-field-label">{{ t('exchange.operations.amount') }}</label>
                  <input
                    type="text"
                    :value="item.sourceAmount ? item.sourceAmount.toString().replace('.', ',') : ''"
                    @input="updateAmount(item, ($event.target as HTMLInputElement).value)"
                    class="ex-input ex-input--mono"
                    placeholder="0,00"
                    inputmode="decimal"
                    :disabled="!selectedOfficeId || !selectedVaultId"
                  />
                </div>
                <div class="ex-field ex-field--grow">
                  <label class="ex-field-label">
                    {{ t('exchange.operations.rate') }}
                    <span v-if="item.customRate !== null && item.customRate !== ''" class="ex-custom-badge">{{ t('exchange.operations.customRate') }}</span>
                  </label>
                  <input
                    type="text"
                    :value="item.customRate !== null && item.customRate !== '' ? item.customRate : (item.rateManuallySet ? '' : item.exchangeRate.toString())"
                    @input="handleRateInput(item, ($event.target as HTMLInputElement).value)"
                    @blur="handleRateBlur(item)"
                    @focus="($event.target as HTMLInputElement).select()"
                    class="ex-input ex-input--mono"
                    :class="{ 'ex-input--custom': item.customRate !== null && item.customRate !== '' }"
                    placeholder="0,00"
                    inputmode="decimal"
                    :disabled="!selectedOfficeId || !selectedVaultId"
                  />
                </div>
                <div class="ex-field ex-field--result">
                  <label class="ex-field-label">Tutar</label>
                  <div class="ex-result-value">
                    {{ formatNumber(item.targetAmount) }}
                    <span class="ex-result-code">{{ getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode || '' }}</span>
                  </div>
                </div>
              </div>

              <!-- WAC Info -->
              <div v-if="transactionType === 'sell' && item.sourceCurrencyId && item.sourceCurrencyId !== tryId && getWacForCurrency(item.sourceCurrencyId) > 0" class="ex-wac-row">
                <span class="ex-wac-label">WAC Maliyet:</span>
                <span class="ex-wac-value" :class="isSellBelowWac(item) ? 'ex-wac-value--loss' : 'ex-wac-value--ok'">
                  {{ formatNumber(getWacForCurrency(item.sourceCurrencyId), 4) }} ₺
                </span>
                <span v-if="isSellBelowWac(item)" class="ex-wac-warn">
                  <span class="material-symbols-outlined ex-icon-filled" style="font-size:16px">warning</span>
                  Maliyetin altında!
                </span>
              </div>



            </div>
          </div>
        </div>

        <!-- Notes -->
        <div class="ex-card">
          <button @click="showNotes = !showNotes" class="ex-notes-toggle">
            <div class="ex-notes-toggle-left">
              <span class="material-symbols-outlined" style="font-size:18px">edit_note</span>
              <span>{{ t('exchange.notes.label') }}</span>
              <span v-if="notes" class="ex-notes-badge">Not var</span>
            </div>
            <span class="material-symbols-outlined" style="font-size:18px">{{ showNotes ? 'expand_less' : 'expand_more' }}</span>
          </button>
          <div v-if="showNotes" class="ex-notes-body">
            <textarea
              v-model="notes"
              rows="3"
              class="ex-textarea"
              :placeholder="t('exchange.notes.placeholder')"
              :disabled="!selectedOfficeId || !selectedVaultId"
            ></textarea>
          </div>
        </div>
      </div>

      <!-- Right: Summary -->
      <div class="ex-summary-col">
        <div class="ex-card ex-summary-card">
          <div class="ex-summary-header">
            <span class="material-symbols-outlined ex-icon-filled">receipt_long</span>
            {{ t('exchange.summary.title') }}
          </div>

          <div class="ex-summary-body">
            <!-- Balance Check -->
            <div v-if="transactionType === 'sell' && balanceCheck" class="ex-balance-check" :class="isBalanceSufficient ? 'ex-balance-check--ok' : 'ex-balance-check--low'">
              <span class="material-symbols-outlined ex-icon-filled">{{ isBalanceSufficient ? 'account_balance' : 'error' }}</span>
              <div>
                <p class="ex-balance-title">
                  {{ t('exchange.summary.vaultBalance') }}
                  <span v-if="!isBalanceSufficient"> ({{ t('exchange.summary.insufficient') }})</span>
                </p>
                <p class="ex-balance-detail">
                  {{ t('exchange.summary.available') }}: {{ formatNumber(balanceCheck.balance) }} {{ balanceCheck.currencyCode }}
                  <span v-if="totalForeignCurrencyNeeded > 0"> / {{ t('exchange.summary.required') }}: {{ formatNumber(totalForeignCurrencyNeeded) }}</span>
                </p>
              </div>
            </div>

            <!-- Items Summary -->
            <div class="ex-summary-section">
              <div class="ex-summary-section-title">{{ t('exchange.summary.details') }}</div>
              <div class="ex-summary-items">
                <div v-for="(item, index) in exchangeItems" :key="item.id" class="ex-summary-row">
                  <span class="ex-summary-num">#{{ index + 1 }}</span>
                  <div class="ex-summary-from">
                    <i v-if="getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode || '')"
                       :class="`fi fi-${getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode || '')}`"
                       style="font-size:14px"></i>
                    <span class="ex-summary-amount ex-summary-amount--in">
                      +{{ formatNumber(transactionType === 'buy' ? item.sourceAmount : item.targetAmount) }}
                    </span>
                    <span class="ex-summary-code">
                      {{ getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode || '-' }}
                    </span>
                  </div>
                  <span class="material-symbols-outlined" style="font-size:14px;color:#d1d5db">arrow_forward</span>
                  <div class="ex-summary-to">
                    <i v-if="getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode || '')"
                       :class="`fi fi-${getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode || '')}`"
                       style="font-size:14px"></i>
                    <span class="ex-summary-amount ex-summary-amount--out">
                      -{{ formatNumber(transactionType === 'buy' ? item.targetAmount : item.sourceAmount) }}
                    </span>
                    <span class="ex-summary-code">
                      {{ getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode || '-' }}
                    </span>
                  </div>
                  <span v-if="item.customRate !== null && item.customRate !== ''" class="ex-custom-badge" style="margin-left:4px">Ö</span>
                </div>
              </div>
            </div>

            <!-- Grand Total -->
            <div class="ex-grand-total">
              <div class="ex-grand-total-label">
                <span class="material-symbols-outlined ex-icon-filled" style="font-size:18px">payments</span>
                {{ t('exchange.summary.paymentAmount') }}
              </div>
              <div v-for="total in grandTotal" :key="total.currencyCode" class="ex-grand-total-row">
                <div class="ex-grand-total-left">
                  <span v-if="total.currencyCode === 'USDT'" class="ex-chip-crypto" style="font-size:16px">₮</span>
                  <span v-else-if="total.currencyCode === 'KRUB'" class="material-symbols-outlined ex-icon-filled" style="font-size:16px">credit_card</span>
                  <i v-else-if="getCurrencyCountryCode(total.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(total.currencyCode)}`" style="font-size:16px"></i>
                  <span>{{ total.currencyCode }}</span>
                </div>
                <span class="ex-grand-total-val">{{ formatNumber(total.amount) }}</span>
              </div>
              <div v-if="grandTotal.length === 0" class="ex-grand-total-empty">{{ t('exchange.summary.noTransaction') }}</div>

              <!-- TRY total for sell -->
              <div v-if="transactionType === 'sell' && totalTryAmount > 0" class="ex-try-total">
                <span>Müşteriden Alınacak:</span>
                <span class="ex-grand-total-val">{{ formatNumber(totalTryAmount) }} <i class="fi fi-tr" style="font-size:14px"></i></span>
              </div>
            </div>

            <!-- WAC Warning + Owner Override -->
            <div v-if="transactionType === 'sell' && exchangeItems.some(item => isSellBelowWac(item))" class="ex-wac-block">
              <div class="ex-wac-block-title">
                <span class="material-symbols-outlined ex-icon-filled" style="font-size:18px">error</span>
                Satış kuru WAC maliyetinin altında — zarar edilecek!
              </div>
              <label v-if="authStore.isOwner" class="ex-wac-override">
                <input type="checkbox" v-model="ownerOverrideLoss" class="ex-checkbox" />
                <span>Patron Onayı: Zararına satışa izin ver</span>
              </label>
              <p v-else class="ex-wac-block-info">Bu satış yalnızca Owner yetkisiyle yapılabilir.</p>
            </div>

            <!-- Today Stats -->
            <div v-if="todayTxCount > 0" class="ex-today-stats">
              <div class="ex-today-row">
                <span class="ex-today-label">
                  <span class="material-symbols-outlined ex-icon-filled" style="font-size:14px">today</span>
                  Bugünkü İşlemler
                </span>
                <span class="ex-today-val">{{ todayTxCount }}</span>
              </div>
              <div v-if="todayProfit !== null" class="ex-today-row">
                <span class="ex-today-label">Toplam Kar</span>
                <span class="ex-today-val" :class="todayProfit >= 0 ? 'ex-today-val--pos' : 'ex-today-val--neg'">
                  {{ todayProfit >= 0 ? '+' : '' }}{{ formatNumber(todayProfit) }} ₺
                </span>
              </div>
            </div>

            <!-- Submit -->
            <button
              @click="submitExchange"
              :disabled="isLoading || exchangeItems.length === 0 || !selectedOfficeId || !selectedVaultId || (transactionType === 'sell' && exchangeItems.some(item => isSellBelowWac(item)) && !ownerOverrideLoss)"
              class="ex-submit-btn"
              :class="transactionType === 'buy' ? 'ex-submit-btn--buy' : 'ex-submit-btn--sell'"
            >
              <span v-if="isLoading" class="material-symbols-outlined animate-spin">refresh</span>
              <span v-else class="material-symbols-outlined ex-icon-filled">check_circle</span>
              {{ isLoading ? t('exchange.summary.processing') : t('exchange.summary.submit') }}
              <span v-if="!isLoading" class="ex-submit-hint">Ctrl+Enter</span>
            </button>

            <!-- Receipt Panel -->
            <div v-if="showReceiptPanel && lastReceipt" class="ex-receipt-panel">
              <div class="ex-receipt-panel-header">
                <span class="material-symbols-outlined ex-icon-filled" style="font-size:18px;color:#16a34a">check_circle</span>
                <span class="ex-receipt-panel-title">İşlem Başarılı</span>
                <button @click="showReceiptPanel = false" class="ex-receipt-panel-close">
                  <span class="material-symbols-outlined" style="font-size:16px">close</span>
                </button>
              </div>
              <div class="ex-receipt-panel-body">
                <div v-for="(item, i) in lastReceipt.items" :key="i" class="ex-receipt-line">
                  <span>{{ formatNumber(item.amount) }} {{ item.source }}</span>
                  <span style="color:#9ca3af">→</span>
                  <span>{{ formatNumber(item.total) }} {{ item.target }}</span>
                  <span class="ex-receipt-rate">@ {{ formatNumber(item.rate, 4) }}</span>
                </div>
              </div>
              <div class="ex-receipt-panel-actions">
                <button @click="printReceipt()" class="ex-btn ex-btn--indigo ex-btn--sm">
                  <span class="material-symbols-outlined ex-icon-filled" style="font-size:16px">print</span>
                  Fiş Yazdır
                </button>
                <button @click="showReceiptPanel = false" class="ex-btn ex-btn--ghost ex-btn--sm">Kapat</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ═══ Rate Matrix ═══ -->
    <div class="ex-matrix-section">
      <div class="ex-card">
        <div class="ex-card-header">
          <h3 class="ex-card-title">
            <span class="material-symbols-outlined ex-icon-filled" style="font-size:18px;color:#6366f1">grid_view</span>
            Çapraz Kur Matrisi
          </h3>
        </div>
        <div class="ex-matrix-wrap">
          <table class="ex-matrix">
            <thead>
              <tr>
                <th></th>
                <th v-for="to in matrixCurrencies" :key="to">
                  <i v-if="getCurrencyCountryCode(to)" :class="`fi fi-${getCurrencyCountryCode(to)}`" style="font-size:12px"></i>
                  {{ to }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="from in matrixCurrencies" :key="from">
                <td class="ex-matrix-label">
                  <i v-if="getCurrencyCountryCode(from)" :class="`fi fi-${getCurrencyCountryCode(from)}`" style="font-size:12px"></i>
                  {{ from }}
                </td>
                <td v-for="to in matrixCurrencies" :key="to"
                    :class="{ 'ex-matrix-self': from === to, 'ex-matrix-cell': from !== to }"
                    @click="from !== to && matrixToArbitrage(from, to)">
                  <span v-if="from === to" class="ex-matrix-dash">—</span>
                  <span v-else class="ex-matrix-rate">{{ rateMatrix[from]?.[to] ? formatNumber(rateMatrix[from][to], 4) : '-' }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Transaction History -->
    <div class="ex-history">
      <TransactionHistory
        ref="transactionHistoryRef"
        :office-id="selectedOfficeId"
        :page-size="50"
        :show-actions="true"
        :show-refresh-button="true"
        :selected-date="new Date().toISOString().split('T')[0]"
      />
    </div>

    <USDTPaymentsModal ref="usdtModalRef" />
  </div>

  <!-- Global Modals -->
  <VaultCountingModal
    ref="vaultCountingModalRef"
    :vault-id="currentVault?.vaultId || currentVault?.id || selectedVaultId"
    :vault-name="currentVault?.vaultName || availableVaults.find(v => (v.vaultId || v.id) === selectedVaultId)?.vaultName || ''"
    :vault-balances="currentVault?.balances || selectedVaultBalances"
    :is-manual="currentVault?.isManual || false"
    @complete="handleVaultCountComplete"
    @waiting-customer="handleWaitingCustomer"
  />

  <DayClosureModal
    v-if="showDayClosureModal"
    :office-id="selectedOfficeId"
    @closed="showDayClosureModal = false"
    @done="onDayClosureDone"
  />
</template>

<style scoped>
/* ═══ Design Tokens ═══ */
:root {
  --ex-radius: 16px;
  --ex-border: #e2e8f0;
  --ex-indigo: #6366f1;
  --ex-purple: #7c3aed;
  --ex-green: #16a34a;
  --ex-red: #dc2626;
  --ex-amber: #d97706;
  --ex-shadow-sm: 0 1px 3px rgba(0,0,0,0.04), 0 1px 2px rgba(0,0,0,0.06);
  --ex-shadow-md: 0 4px 16px rgba(0,0,0,0.06), 0 1px 3px rgba(0,0,0,0.04);
  --ex-shadow-lg: 0 8px 32px rgba(0,0,0,0.08), 0 2px 6px rgba(0,0,0,0.04);
  --ex-shadow-glow-indigo: 0 0 20px rgba(99,102,241,0.15);
  --ex-shadow-glow-green: 0 0 20px rgba(22,163,74,0.12);
  --ex-shadow-glow-red: 0 0 20px rgba(220,38,38,0.12);
}

/* ═══ Page ═══ */
.ex-page {
  width: 100%;
  max-width: 1320px;
  margin: 0 auto;
  padding: 0 16px;
  min-height: 100vh;
}

/* ═══ Overlays ═══ */
.ex-overlay-center {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  text-align: center;
  gap: 16px;
}
.ex-overlay-icon {
  width: 88px;
  height: 88px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
}
.ex-overlay-icon--red { background: linear-gradient(135deg, #ef4444, #dc2626); }
.ex-overlay-icon--amber { background: linear-gradient(135deg, #f59e0b, #d97706); }
.ex-overlay-title { font-size: 26px; font-weight: 700; color: #111827; }
.ex-overlay-desc { font-size: 16px; color: #6b7280; max-width: 400px; }
.ex-overlay-hint { font-size: 14px; color: #d97706; }
.ex-overlay-text { font-size: 16px; color: #6b7280; }
.ex-spinner-wrap { width: 56px; height: 56px; }
.ex-spinner { width: 100%; height: 100%; color: #6366f1; animation: spin 1s linear infinite; }

/* ═══ Top Bar ═══ */
.ex-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 24px;
  flex-wrap: wrap;
  padding: 18px 24px;
  background: linear-gradient(135deg, #1e1b4b 0%, #312e81 40%, #4338ca 100%);
  border: 1px solid rgba(99,102,241,0.2);
  border-radius: var(--ex-radius);
  box-shadow: 0 4px 24px rgba(30,27,75,0.15), inset 0 1px 0 rgba(255,255,255,0.08);
}
.ex-topbar-left {
  display: flex;
  gap: 14px;
  flex-wrap: wrap;
  align-items: center;
}
.ex-topbar-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: rgba(255,255,255,0.12);
  backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #a5b4fc;
  font-size: 24px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.15);
}
.ex-topbar-title {
  font-size: 1.15rem;
  font-weight: 800;
  color: #fff;
  margin: 0;
  letter-spacing: -0.02em;
}
.ex-topbar-subtitle {
  font-size: 0.78rem;
  color: #a5b4fc;
  margin: 2px 0 0;
  font-weight: 500;
  letter-spacing: 0.3px;
}
.ex-topbar-right {
  display: flex;
  gap: 6px;
}
.ex-select-group {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.ex-select-label {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  font-weight: 500;
  color: #6b7280;
}
.ex-select {
  padding: 8px 32px 8px 12px;
  border: 1px solid var(--ex-border);
  border-radius: 10px;
  font-size: 14px;
  background: white;
  cursor: pointer;
  appearance: none;
  background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236366f1' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
  background-position: right 8px center;
  background-repeat: no-repeat;
  background-size: 1.2em;
  min-width: 160px;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.ex-select:focus { border-color: var(--ex-indigo); box-shadow: 0 0 0 3px rgba(99,102,241,0.1); outline: none; }
.ex-select:disabled { opacity: 0.5; cursor: not-allowed; background-color: #f9fafb; }

.ex-topbar-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  width: 40px;
  height: 40px;
  padding: 0;
  border: 1px solid rgba(255,255,255,0.15);
  border-radius: 12px;
  background: rgba(255,255,255,0.1);
  backdrop-filter: blur(8px);
  color: #c7d2fe;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.25s;
  position: relative;
}
.ex-topbar-btn:hover:not(:disabled) {
  background: rgba(255,255,255,0.2);
  color: #fff;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0,0,0,0.2);
}
.ex-topbar-btn--syncing {
  background: rgba(99,102,241,0.3);
  color: #a5b4fc;
}
.ex-topbar-btn:disabled { opacity: 0.3; cursor: not-allowed; }
.ex-countdown {
  font-size: 9px;
  color: #1e1b4b;
  background: #a5b4fc;
  border-radius: 8px;
  padding: 1px 5px;
  position: absolute;
  top: -6px;
  right: -6px;
  font-weight: 700;
  line-height: 1.3;
  box-shadow: 0 2px 6px rgba(0,0,0,0.2);
}

/* ═══ Warning Banners ═══ */
.ex-warning-banner {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px 22px;
  border-radius: var(--ex-radius);
  border: 1px solid rgba(239,68,68,0.25);
  background: linear-gradient(135deg, #fef2f2 0%, #fff1f2 50%, #fce7f3 100%);
  margin-bottom: 20px;
  color: #991b1b;
  box-shadow: 0 4px 16px rgba(239,68,68,0.1);
  animation: ex-fade-in 0.4s ease;
}
.ex-warning-banner--amber {
  border-color: rgba(245,158,11,0.25);
  background: linear-gradient(135deg, #fffbeb 0%, #fef3c7 50%, #fde68a33 100%);
  color: #92400e;
  box-shadow: 0 4px 16px rgba(245,158,11,0.1);
}
.ex-warning-title { font-weight: 800; font-size: 14px; letter-spacing: -0.01em; }
.ex-warning-desc { font-size: 13px; opacity: 0.8; margin-top: 3px; line-height: 1.4; }

@keyframes ex-fade-in {
  from { opacity: 0; transform: translateY(-6px); }
  to { opacity: 1; transform: translateY(0); }
}

/* ═══ Buy/Sell Toggle ═══ */
.ex-type-toggle {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
  margin-bottom: 22px;
}
.ex-type-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 24px 16px;
  border-radius: var(--ex-radius);
  border: 2px solid transparent;
  cursor: pointer;
  transition: all 0.35s cubic-bezier(0.4, 0, 0.2, 1);
  font-size: 14px;
  position: relative;
  overflow: hidden;
}
.ex-type-btn::before {
  content: '';
  position: absolute;
  inset: 0;
  opacity: 0;
  transition: opacity 0.5s;
}
.ex-type-btn--buy-active {
  background: linear-gradient(145deg, #dcfce7, #bbf7d0);
  border-color: #22c55e;
  color: #15803d;
  box-shadow: 0 6px 24px rgba(34,197,94,0.25), inset 0 1px 0 rgba(255,255,255,0.6);
  animation: ex-pulse-green 2s ease-in-out infinite;
}
.ex-type-btn--buy-active .material-symbols-outlined {
  font-size: 28px;
}
.ex-type-btn--sell-active {
  background: linear-gradient(145deg, #fee2e2, #fecaca);
  border-color: #ef4444;
  color: #b91c1c;
  box-shadow: 0 6px 24px rgba(239,68,68,0.25), inset 0 1px 0 rgba(255,255,255,0.6);
  animation: ex-pulse-red 2s ease-in-out infinite;
}
.ex-type-btn--sell-active .material-symbols-outlined {
  font-size: 28px;
}
.ex-type-btn--inactive {
  background: white;
  border-color: var(--ex-border);
  color: #94a3b8;
  box-shadow: var(--ex-shadow-sm);
}
.ex-type-btn--inactive:hover {
  background: #f8fafc;
  border-color: #cbd5e1;
  transform: translateY(-2px);
  box-shadow: var(--ex-shadow-md);
}
.ex-type-label { font-weight: 800; font-size: 19px; letter-spacing: -0.01em; }
.ex-type-desc { font-size: 12px; opacity: 0.75; }

@keyframes ex-pulse-green {
  0%, 100% { box-shadow: 0 6px 24px rgba(34,197,94,0.25), inset 0 1px 0 rgba(255,255,255,0.6); }
  50% { box-shadow: 0 6px 32px rgba(34,197,94,0.35), inset 0 1px 0 rgba(255,255,255,0.6); }
}
@keyframes ex-pulse-red {
  0%, 100% { box-shadow: 0 6px 24px rgba(239,68,68,0.25), inset 0 1px 0 rgba(255,255,255,0.6); }
  50% { box-shadow: 0 6px 32px rgba(239,68,68,0.35), inset 0 1px 0 rgba(255,255,255,0.6); }
}

/* ═══ Quick Currency Bar ═══ */
.ex-quick-bar {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-bottom: 22px;
  padding: 14px 20px;
  background: linear-gradient(135deg, #f8fafc, #f1f5f9);
  border: 1px solid #e2e8f0;
  border-radius: var(--ex-radius);
  flex-wrap: wrap;
}
.ex-quick-title {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 11px;
  font-weight: 800;
  color: #6366f1;
  white-space: nowrap;
  text-transform: uppercase;
  letter-spacing: 0.8px;
}
.ex-quick-chips { display: flex; gap: 8px; flex-wrap: wrap; }
.ex-chip {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 18px;
  border: 2px solid #e2e8f0;
  border-radius: 28px;
  background: white;
  font-weight: 700;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: #475569;
  box-shadow: 0 2px 6px rgba(0,0,0,0.04);
}
.ex-chip:hover { transform: translateY(-3px); box-shadow: 0 6px 20px rgba(0,0,0,0.1); }
.ex-chip--usd { border-color: rgba(34,197,94,0.3); }
.ex-chip--usd:hover { border-color: #22c55e; color: #15803d; background: linear-gradient(135deg, #f0fdf4, #dcfce7); box-shadow: 0 6px 20px rgba(34,197,94,0.2); }
.ex-chip--eur { border-color: rgba(59,130,246,0.3); }
.ex-chip--eur:hover { border-color: #3b82f6; color: #1d4ed8; background: linear-gradient(135deg, #eff6ff, #dbeafe); box-shadow: 0 6px 20px rgba(59,130,246,0.2); }
.ex-chip--rub { border-color: rgba(239,68,68,0.3); }
.ex-chip--rub:hover { border-color: #ef4444; color: #b91c1c; background: linear-gradient(135deg, #fef2f2, #fee2e2); box-shadow: 0 6px 20px rgba(239,68,68,0.2); }
.ex-chip--krub { border-color: rgba(139,92,246,0.3); }
.ex-chip--krub:hover { border-color: #8b5cf6; color: #6b46c1; background: linear-gradient(135deg, #f5f3ff, #ede9fe); box-shadow: 0 6px 20px rgba(139,92,246,0.2); }
.ex-chip--usdt { border-color: rgba(20,184,166,0.3); }
.ex-chip--usdt:hover { border-color: #14b8a6; color: #0f766e; background: linear-gradient(135deg, #f0fdfa, #ccfbf1); box-shadow: 0 6px 20px rgba(20,184,166,0.2); }
.ex-chip--active {
  box-shadow: 0 0 0 2px #6366f1, 0 4px 16px rgba(99,102,241,0.25);
  border-color: #6366f1;
  background: linear-gradient(135deg, #eef2ff, #e0e7ff);
  color: #4338ca;
  transform: translateY(-2px);
}
.ex-chip--active.ex-chip--usd { box-shadow: 0 0 0 2px #22c55e, 0 4px 16px rgba(34,197,94,0.25); border-color: #22c55e; background: linear-gradient(135deg, #f0fdf4, #dcfce7); color: #15803d; }
.ex-chip--active.ex-chip--eur { box-shadow: 0 0 0 2px #3b82f6, 0 4px 16px rgba(59,130,246,0.25); border-color: #3b82f6; background: linear-gradient(135deg, #eff6ff, #dbeafe); color: #1d4ed8; }
.ex-chip--active.ex-chip--rub { box-shadow: 0 0 0 2px #ef4444, 0 4px 16px rgba(239,68,68,0.25); border-color: #ef4444; background: linear-gradient(135deg, #fef2f2, #fee2e2); color: #b91c1c; }
.ex-chip--active.ex-chip--krub { box-shadow: 0 0 0 2px #8b5cf6, 0 4px 16px rgba(139,92,246,0.25); border-color: #8b5cf6; background: linear-gradient(135deg, #f5f3ff, #ede9fe); color: #6b46c1; }
.ex-chip--active.ex-chip--usdt { box-shadow: 0 0 0 2px #14b8a6, 0 4px 16px rgba(20,184,166,0.25); border-color: #14b8a6; background: linear-gradient(135deg, #f0fdfa, #ccfbf1); color: #0f766e; }
.ex-chip-flag { font-size: 20px; }
.ex-chip-crypto { font-weight: 800; font-size: 20px; color: #10b981; }

/* ═══ Layout ═══ */
.ex-layout {
  display: grid;
  grid-template-columns: 1fr 400px;
  gap: 28px;
  align-items: start;
}
@media (max-width: 1024px) {
  .ex-layout { grid-template-columns: 1fr; }
}

.ex-form-col { display: flex; flex-direction: column; gap: 18px; }
.ex-summary-col { position: sticky; top: 20px; }

/* ═══ Cards ═══ */
.ex-card {
  background: white;
  border: 1px solid var(--ex-border);
  border-radius: var(--ex-radius);
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04), 0 1px 2px rgba(0,0,0,0.06);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
.ex-card:hover { box-shadow: 0 8px 28px rgba(0,0,0,0.08), 0 2px 6px rgba(0,0,0,0.04); }
.ex-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 22px;
  border-bottom: 1px solid #f1f5f9;
  background: linear-gradient(180deg, #fafbff 0%, #fff 100%);
}
.ex-card-title {
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
  display: flex;
  align-items: center;
  gap: 8px;
  letter-spacing: -0.02em;
}
.ex-card-body { padding: 20px 22px; display: flex; flex-direction: column; gap: 18px; background: #fafbfe; }

/* ═══ Exchange Item ═══ */
.ex-item {
  border: 1px solid var(--ex-border);
  border-radius: 14px;
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
}
.ex-item--buy {
  border-left: 4px solid #22c55e;
  background: linear-gradient(135deg, #fafffe, #f0fdf4);
}
.ex-item--sell {
  border-left: 4px solid #ef4444;
  background: linear-gradient(135deg, #fffafa, #fef2f2);
}
.ex-item:hover {
  box-shadow: var(--ex-shadow-md);
  transform: translateY(-1px);
}

.ex-item-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.ex-item-badge {
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.8px;
  padding: 5px 12px;
  border-radius: 8px;
}
.ex-item-badge--buy {
  background: linear-gradient(135deg, #dcfce7, #bbf7d0);
  color: #15803d;
  box-shadow: 0 1px 4px rgba(22,163,74,0.12);
}
.ex-item-badge--sell {
  background: linear-gradient(135deg, #fee2e2, #fecaca);
  color: #b91c1c;
  box-shadow: 0 1px 4px rgba(220,38,38,0.12);
}
.ex-item-delete {
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  border: none;
  background: transparent;
  color: #9ca3af;
  cursor: pointer;
  transition: all 0.15s;
}
.ex-item-delete:hover { background: #fee2e2; color: #ef4444; }

.ex-item-currencies {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  gap: 8px;
  align-items: end;
}
@media (max-width: 640px) {
  .ex-item-currencies { grid-template-columns: 1fr; }
  .ex-arrow-divider { display: none; }
}
.ex-arrow-divider {
  display: flex;
  align-items: center;
  justify-content: center;
  padding-bottom: 4px;
  color: white;
  width: 38px;
  height: 38px;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  border-radius: 50%;
  box-shadow: 0 3px 10px rgba(99,102,241,0.3);
  align-self: end;
  margin-bottom: 4px;
  flex-shrink: 0;
}

.ex-item-numbers {
  display: flex;
  gap: 12px;
  align-items: end;
}
@media (max-width: 640px) {
  .ex-item-numbers { flex-wrap: wrap; }
}

/* ═══ Fields ═══ */
.ex-field { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
.ex-field--grow { flex: 1; }
.ex-field--result { flex: 0 0 auto; min-width: 120px; }
.ex-field-label {
  font-size: 11px;
  font-weight: 700;
  color: #475569;
  display: flex;
  align-items: center;
  gap: 4px;
  text-transform: uppercase;
  letter-spacing: 0.4px;
}
.ex-field-hint { font-size: 10px; color: #94a3b8; text-transform: none; letter-spacing: 0; }

.ex-input {
  width: 100%;
  padding: 12px 16px;
  border: 1.5px solid #dde1e8;
  border-radius: 12px;
  font-size: 15px;
  transition: all 0.25s;
  outline: none;
  background: white;
  box-shadow: inset 0 1px 3px rgba(0,0,0,0.04);
}
.ex-input:focus {
  border-color: var(--ex-indigo);
  box-shadow: 0 0 0 4px rgba(99,102,241,0.1), inset 0 1px 2px rgba(0,0,0,0.02);
}
.ex-input:disabled { opacity: 0.5; background: #f1f5f9; }
.ex-input--mono { font-family: 'JetBrains Mono', ui-monospace, monospace; font-weight: 600; }
.ex-input--custom {
  background: linear-gradient(135deg, #fffbeb, #fef3c7);
  border-color: #fbbf24;
  box-shadow: 0 0 0 3px rgba(251,191,36,0.1);
}

.ex-result-value {
  padding: 11px 14px;
  background: linear-gradient(135deg, #eef2ff, #e0e7ff);
  border: 1.5px solid #c7d2fe;
  border-radius: 12px;
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-size: 16px;
  font-weight: 700;
  color: #312e81;
  white-space: nowrap;
}
.ex-result-code { font-size: 11px; color: #64748b; font-weight: 600; margin-left: 4px; }

.ex-custom-badge {
  font-size: 10px;
  font-weight: 600;
  padding: 1px 6px;
  border-radius: 4px;
  background: #fef3c7;
  color: #92400e;
}

/* ═══ WAC ═══ */
.ex-wac-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  padding: 6px 0 0;
}
.ex-wac-label { color: #6b7280; }
.ex-wac-value { font-family: 'JetBrains Mono', ui-monospace, monospace; font-weight: 600; }
.ex-wac-value--ok { color: #16a34a; }
.ex-wac-value--loss { color: #dc2626; }
.ex-wac-warn {
  display: flex;
  align-items: center;
  gap: 2px;
  color: #dc2626;
  font-weight: 600;
}

.ex-wac-block {
  padding: 12px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 10px;
}
.ex-wac-block-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 600;
  color: #b91c1c;
  margin-bottom: 8px;
}
.ex-wac-override {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #dc2626;
  font-weight: 500;
  cursor: pointer;
}
.ex-wac-block-info { font-size: 12px; color: #dc2626; }
.ex-checkbox { width: 16px; height: 16px; accent-color: #dc2626; }

/* ═══ Market Rates ═══ */
.ex-market-rates {
  padding-top: 12px;
  border-top: 1px solid #f3f4f6;
}
.ex-market-header {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  font-weight: 600;
  color: #6b7280;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 8px;
}
.ex-market-chips { display: flex; flex-wrap: wrap; gap: 6px; }
.ex-market-chip {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 8px;
  border: 1px solid var(--ex-border);
  background: white;
  font-size: 12px;
}
.ex-market-chip--system { background: #ede9fe; border-color: #c4b5fd; }
.ex-market-chip--low { background: #f0fdf4; border-color: #86efac; }
.ex-market-chip--high { background: #fef2f2; border-color: #fca5a5; }
.ex-market-source { font-weight: 600; color: #374151; }
.ex-market-val { font-family: 'JetBrains Mono', ui-monospace, monospace; font-weight: 500; color: #111827; }

/* ═══ Notes ═══ */
.ex-notes-toggle {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 14px 20px;
  border: none;
  background: transparent;
  cursor: pointer;
  color: #374151;
  font-size: 14px;
  font-weight: 600;
  transition: background 0.2s;
}
.ex-notes-toggle:hover { background: linear-gradient(135deg, #f8fafc, #f1f5f9); }
.ex-notes-toggle-left { display: flex; align-items: center; gap: 8px; }
.ex-notes-badge {
  font-size: 10px;
  font-weight: 700;
  padding: 3px 10px;
  border-radius: 12px;
  background: linear-gradient(135deg, #dbeafe, #bfdbfe);
  color: #1d4ed8;
  letter-spacing: 0.3px;
}
.ex-notes-body { padding: 0 20px 16px; }
.ex-textarea {
  width: 100%;
  padding: 12px 14px;
  border: 1.5px solid #dde1e8;
  border-radius: 12px;
  font-size: 14px;
  resize: none;
  min-height: 72px;
  outline: none;
  transition: all 0.25s;
  box-shadow: inset 0 1px 3px rgba(0,0,0,0.04);
}
.ex-textarea:focus { border-color: var(--ex-indigo); box-shadow: 0 0 0 4px rgba(99,102,241,0.1), inset 0 1px 2px rgba(0,0,0,0.02); }

/* ═══ Summary Card ═══ */
.ex-summary-card {
  border: 1px solid rgba(99,102,241,0.25);
  box-shadow: 0 8px 32px rgba(99,102,241,0.12), 0 2px 6px rgba(0,0,0,0.04);
  border-radius: 20px;
}
.ex-summary-card:hover { box-shadow: 0 12px 40px rgba(99,102,241,0.18), 0 2px 8px rgba(0,0,0,0.06); }
.ex-summary-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 18px 22px;
  background: linear-gradient(135deg, #4338ca 0%, #6366f1 40%, #7c3aed 100%);
  color: white;
  font-size: 16px;
  font-weight: 800;
  letter-spacing: -0.01em;
  box-shadow: inset 0 -1px 0 rgba(0,0,0,0.1);
}
.ex-summary-body { padding: 18px 22px; display: flex; flex-direction: column; gap: 16px; }

/* Balance Check */
.ex-balance-check {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 10px 14px;
  border-radius: 10px;
  font-size: 13px;
}
.ex-balance-check--ok { background: #eff6ff; border: 1px solid #bfdbfe; color: #1e40af; }
.ex-balance-check--low { background: #fef2f2; border: 1px solid #fecaca; color: #991b1b; }
.ex-balance-title { font-weight: 600; }
.ex-balance-detail { font-size: 12px; margin-top: 2px; }

/* Summary Section */
.ex-summary-section-title {
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.8px;
  color: #9ca3af;
  margin-bottom: 8px;
}
.ex-summary-items {
  display: flex;
  flex-direction: column;
  gap: 4px;
  background: linear-gradient(135deg, #f8fafc, #f1f5f9);
  border-radius: 12px;
  padding: 12px 14px;
  border: 1px solid #e2e8f0;
}
.ex-summary-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  padding: 6px 4px;
  border-bottom: 1px solid rgba(0,0,0,0.04);
  border-radius: 6px;
  transition: background 0.15s;
}
.ex-summary-row:hover { background: rgba(99,102,241,0.04); }
.ex-summary-row:last-child { border-bottom: none; }
.ex-summary-num {
  font-size: 10px;
  color: white;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  width: 22px;
  height: 22px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 7px;
  font-weight: 800;
  box-shadow: 0 2px 4px rgba(99,102,241,0.3);
}
.ex-summary-from, .ex-summary-to { display: flex; align-items: center; gap: 4px; flex: 1; }
.ex-summary-to { justify-content: flex-end; }
.ex-summary-amount { font-weight: 700; font-family: 'JetBrains Mono', ui-monospace, monospace; font-size: 13px; }
.ex-summary-amount--in { color: #16a34a; }
.ex-summary-amount--out { color: #dc2626; }
.ex-summary-code { font-size: 11px; font-weight: 600; color: #64748b; }

/* Grand Total */
.ex-grand-total {
  background: linear-gradient(145deg, #f5f3ff, #eef2ff, #faf5ff);
  border: 1px solid rgba(99,102,241,0.15);
  border-radius: 14px;
  padding: 16px;
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.8);
}
.ex-grand-total-label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 600;
  color: #4338ca;
  margin-bottom: 10px;
}
.ex-grand-total-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 4px 0;
}
.ex-grand-total-left {
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 500;
  color: #374151;
  font-size: 14px;
}
.ex-grand-total-val {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-size: 20px;
  font-weight: 800;
  color: #312e81;
  text-shadow: 0 1px 2px rgba(49,46,129,0.1);
}
.ex-grand-total-empty { text-align: center; font-size: 13px; color: #9ca3af; padding: 8px 0; }
.ex-try-total {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 10px;
  padding-top: 10px;
  border-top: 1px solid #c4b5fd;
  font-size: 13px;
  font-weight: 500;
  color: #374151;
}

/* ═══ Submit Button ═══ */
.ex-submit-btn {
  width: 100%;
  padding: 18px;
  border: none;
  border-radius: 16px;
  font-size: 16px;
  font-weight: 800;
  color: white;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  letter-spacing: 0.02em;
  position: relative;
  overflow: hidden;
  text-transform: uppercase;
}
.ex-submit-btn::after {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, rgba(255,255,255,0.15) 0%, transparent 60%);
  pointer-events: none;
}
.ex-submit-btn:hover:not(:disabled) {
  transform: translateY(-2px);
}
.ex-submit-btn:active:not(:disabled) { transform: translateY(0); }
.ex-submit-btn:disabled { opacity: 0.5; cursor: not-allowed; transform: none; }
.ex-submit-btn--buy {
  background: linear-gradient(135deg, #22c55e, #16a34a);
  box-shadow: 0 4px 16px rgba(22,163,74,0.35);
}
.ex-submit-btn--buy:hover:not(:disabled) {
  box-shadow: 0 8px 32px rgba(22,163,74,0.45);
  background: linear-gradient(135deg, #16a34a, #15803d);
}
.ex-submit-btn--sell {
  background: linear-gradient(135deg, #ef4444, #dc2626);
  box-shadow: 0 4px 16px rgba(220,38,38,0.35);
}
.ex-submit-btn--sell:hover:not(:disabled) {
  box-shadow: 0 8px 32px rgba(220,38,38,0.45);
  background: linear-gradient(135deg, #dc2626, #b91c1c);
}

/* ═══ Buttons ═══ */
.ex-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 10px 20px;
  border: none;
  border-radius: 12px;
  font-size: 14px;
  font-weight: 700;
  color: white;
  cursor: pointer;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  letter-spacing: 0.01em;
}
.ex-btn:hover { transform: translateY(-1px); }
.ex-btn:active { transform: translateY(0); }
.ex-btn--sm { padding: 8px 16px; font-size: 13px; border-radius: 10px; }
.ex-btn--red { background: linear-gradient(135deg, #ef4444, #dc2626); box-shadow: 0 3px 10px rgba(220,38,38,0.25); }
.ex-btn--red:hover { background: linear-gradient(135deg, #dc2626, #b91c1c); box-shadow: 0 6px 16px rgba(220,38,38,0.3); }
.ex-btn--amber { background: linear-gradient(135deg, #f59e0b, #d97706); box-shadow: 0 3px 10px rgba(217,119,6,0.25); }
.ex-btn--amber:hover { background: linear-gradient(135deg, #d97706, #b45309); box-shadow: 0 6px 16px rgba(217,119,6,0.3); }
.ex-btn--indigo { background: linear-gradient(135deg, #6366f1, #4f46e5); box-shadow: 0 3px 10px rgba(79,70,229,0.25); }
.ex-btn--indigo:hover { background: linear-gradient(135deg, #4f46e5, #4338ca); box-shadow: 0 6px 16px rgba(79,70,229,0.3); }

/* ═══ Today Stats ═══ */
.ex-today-stats {
  background: linear-gradient(135deg, #f0fdf4 0%, #ecfdf5 50%, #d1fae5 100%);
  border: 1px solid rgba(22,163,74,0.2);
  border-radius: 14px;
  padding: 14px 18px;
  box-shadow: 0 4px 12px rgba(22,163,74,0.08);
}
.ex-today-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 3px 0;
}
.ex-today-label {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: #374151;
  font-weight: 500;
}
.ex-today-val {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-size: 13px;
  font-weight: 700;
  color: #15803d;
}
.ex-today-val--pos { color: #16a34a; }
.ex-today-val--neg { color: #dc2626; }

.ex-submit-hint {
  font-size: 10px;
  font-weight: 500;
  opacity: 0.7;
  padding: 2px 6px;
  border-radius: 4px;
  background: rgba(255,255,255,0.2);
  margin-left: 6px;
}

/* ═══ Receipt Panel ═══ */
.ex-receipt-panel {
  margin-top: 12px;
  background: linear-gradient(135deg, #f0fdf4, #dcfce7);
  border: 1px solid #86efac;
  border-radius: var(--ex-radius);
  overflow: hidden;
  animation: ex-slide-down 0.25s ease;
}
@keyframes ex-slide-down {
  from { opacity: 0; transform: translateY(-8px); }
  to { opacity: 1; transform: translateY(0); }
}
.ex-receipt-panel-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  border-bottom: 1px solid #bbf7d0;
}
.ex-receipt-panel-title {
  font-size: 13px;
  font-weight: 700;
  color: #15803d;
}
.ex-receipt-panel-close {
  margin-left: auto;
  background: none;
  border: none;
  cursor: pointer;
  color: #6b7280;
  padding: 2px;
  border-radius: 4px;
  display: flex;
}
.ex-receipt-panel-close:hover { background: rgba(0,0,0,0.06); }
.ex-receipt-panel-body { padding: 8px 14px; }
.ex-receipt-line {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 0;
  font-size: 13px;
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  color: #1f2937;
}
.ex-receipt-rate {
  margin-left: auto;
  font-size: 11px;
  color: #6b7280;
}
.ex-receipt-panel-actions {
  display: flex;
  gap: 8px;
  padding: 10px 14px;
  border-top: 1px solid #bbf7d0;
  justify-content: flex-end;
}
.ex-btn--ghost {
  background: transparent;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  color: #6b7280;
  cursor: pointer;
  font-size: 12px;
  font-weight: 600;
  padding: 6px 14px;
  transition: all 0.15s;
}
.ex-btn--ghost:hover { background: #f3f4f6; color: #374151; }

/* ═══ History ═══ */
.ex-history { margin-top: 36px; margin-bottom: 28px; }

/* ═══ Icons ═══ */
.material-symbols-outlined {
  font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
.ex-icon-filled {
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
.ex-icon-xs { font-size: 16px; }

/* ═══ Position Bar ═══ */
.ex-pos-bar {
  background: linear-gradient(135deg, #0f172a 0%, #1e1b4b 50%, #312e81 100%);
  border: 1px solid rgba(99,102,241,0.2);
  border-radius: var(--ex-radius);
  margin-bottom: 20px;
  box-shadow: 0 4px 20px rgba(15,23,42,0.2);
}
.ex-pos-bar-strip {
  display: flex;
  align-items: center;
  gap: 0;
  padding: 0;
  overflow: hidden;
}
.ex-pos-bar-label {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 14px 18px;
  font-size: 12px;
  font-weight: 700;
  color: #a5b4fc;
  white-space: nowrap;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  border-right: 1px solid rgba(255,255,255,0.06);
  flex-shrink: 0;
}
.ex-pos-bar-label .material-symbols-outlined { font-size: 18px; }
.ex-pos-bar-items {
  display: flex;
  gap: 2px;
  overflow-x: auto;
  flex: 1;
  padding: 6px 4px;
}
.ex-pos-bar-items::-webkit-scrollbar { height: 3px; }
.ex-pos-bar-items::-webkit-scrollbar-thumb { background: #6366f1; border-radius: 2px; }
.ex-pos-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  white-space: nowrap;
  background: rgba(255,255,255,0.03);
  border: 1px solid transparent;
  flex-shrink: 0;
}
.ex-pos-item:hover {
  background: rgba(255,255,255,0.1);
  border-color: rgba(99,102,241,0.3);
  box-shadow: 0 4px 16px rgba(0,0,0,0.25);
  transform: translateY(-1px);
}
.ex-pos-flag { font-size: 18px; flex-shrink: 0; }
.ex-pos-code {
  font-size: 11px;
  font-weight: 800;
  color: #a5b4fc;
  letter-spacing: 0.5px;
  min-width: 32px;
}
.ex-pos-balance {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-size: 14px;
  font-weight: 700;
  color: #f1f5f9;
}
.ex-pos-sep { color: rgba(255,255,255,0.15); font-size: 14px; }
.ex-pos-wac {
  font-size: 10px;
  color: #64748b;
  font-family: 'JetBrains Mono', ui-monospace, monospace;
}
.ex-pos-pnl {
  font-size: 10px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 6px;
}
.ex-pos-bar-total {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 14px 18px;
  border-left: 1px solid rgba(255,255,255,0.06);
  flex-shrink: 0;
  white-space: nowrap;
}
.ex-pos-bar-total-label {
  font-size: 14px;
  font-weight: 800;
  color: #818cf8;
}
.ex-pos-bar-total-val {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-size: 14px;
  font-weight: 700;
  color: #e0e7ff;
}
.ex-pnl--pos { color: #4ade80; background: rgba(74,222,128,0.12); }
.ex-pnl--neg { color: #f87171; background: rgba(248,113,113,0.12); }

/* ═══ Terminal Mode Tabs ═══ */
.ex-terminal-tabs {
  display: flex;
  gap: 4px;
  margin-bottom: 22px;
  background: linear-gradient(135deg, #e2e8f0, #f1f5f9);
  border-radius: 16px;
  padding: 5px;
  box-shadow: inset 0 2px 4px rgba(0,0,0,0.06);
  border: 1px solid #e2e8f0;
}
.ex-tab {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 14px 20px;
  border: none;
  border-radius: 13px;
  background: transparent;
  font-size: 13px;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
.ex-tab:hover { color: #334155; background: rgba(255,255,255,0.7); }
.ex-tab--active {
  background: white;
  color: var(--ex-indigo);
  box-shadow: 0 2px 12px rgba(99,102,241,0.2), 0 1px 3px rgba(0,0,0,0.06);
  font-weight: 700;
}
.ex-tab--active-arb {
  background: linear-gradient(135deg, #fffbeb, #fef3c7);
  color: #92400e;
  box-shadow: 0 2px 12px rgba(217,119,6,0.2), 0 1px 3px rgba(0,0,0,0.06);
  font-weight: 700;
}
.ex-tab--active-batch {
  background: linear-gradient(135deg, #eff6ff, #dbeafe);
  color: #1e40af;
  box-shadow: 0 2px 12px rgba(59,130,246,0.2), 0 1px 3px rgba(0,0,0,0.06);
  font-weight: 700;
}
.ex-tab-key {
  font-size: 9px;
  font-weight: 600;
  padding: 2px 6px;
  border-radius: 4px;
  background: rgba(0,0,0,0.06);
  color: #94a3b8;
  letter-spacing: 0.3px;
}

/* ═══ Arbitrage Header ═══ */
.ex-arb-header {
  display: flex;
  align-items: center;
  margin-bottom: 16px;
  padding: 14px 18px;
  background: linear-gradient(135deg, #fffbeb, #fef3c7);
  border: 1px solid #fcd34d;
  border-radius: var(--ex-radius);
  gap: 12px;
}
.ex-arb-info { display: flex; align-items: center; gap: 12px; }
.ex-arb-title { font-size: 15px; font-weight: 700; color: #92400e; }
.ex-arb-desc { font-size: 12px; color: #a16207; margin-top: 2px; }
.ex-arb-margin {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-left: auto;
  flex-shrink: 0;
}
.ex-arb-margin-label {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  font-weight: 600;
  color: #92400e;
  white-space: nowrap;
}
.ex-arb-margin-input {
  width: 70px;
  padding: 6px 8px;
  font-size: 13px;
  border-radius: 8px;
  text-align: center;
}
.ex-btn--amber {
  background: linear-gradient(135deg, #f59e0b, #d97706);
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 4px;
  font-weight: 600;
  transition: all 0.2s;
}
.ex-btn--amber:hover { background: linear-gradient(135deg, #d97706, #b45309); }
.ex-btn--amber:disabled { opacity: 0.5; cursor: not-allowed; }

/* ═══ Batch Mode ═══ */
.ex-batch-section { margin-bottom: 20px; }
.ex-batch-form {
  background: white;
  border: 1px solid var(--ex-border);
  border-radius: var(--ex-radius);
  padding: 16px;
}
.ex-batch-form-row {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}
.ex-batch-toggle {
  display: flex;
  border: 1px solid var(--ex-border);
  border-radius: 8px;
  overflow: hidden;
}
.ex-batch-type-btn {
  padding: 6px 12px;
  border: none;
  background: white;
  font-size: 12px;
  font-weight: 600;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.15s;
}
.ex-batch-type-btn:first-child { border-right: 1px solid var(--ex-border); }
.ex-batch-type--buy { background: #dcfce7; color: #15803d; }
.ex-batch-type--sell { background: #fee2e2; color: #b91c1c; }
.ex-batch-arrow { color: #d1d5db; font-weight: 700; }
.ex-batch-input { width: 100px; padding: 7px 10px; font-size: 13px; }
.ex-batch-input--rate { width: 90px; }
.ex-batch-input--note { width: 120px; flex: 1; min-width: 80px; }

.ex-batch-queue {
  margin-top: 12px;
  background: white;
  border: 1px solid var(--ex-border);
  border-radius: var(--ex-radius);
  overflow: hidden;
}
.ex-batch-queue-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 16px;
  border-bottom: 1px solid #f3f4f6;
  font-size: 13px;
  font-weight: 600;
  color: #374151;
}
.ex-batch-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  border-bottom: 1px solid #f3f4f6;
  font-size: 13px;
}
.ex-batch-item:last-child { border-bottom: none; }
.ex-batch-item-type {
  font-size: 10px;
  font-weight: 700;
  padding: 3px 6px;
  border-radius: 4px;
}
.ex-batch-item-detail {
  flex: 1;
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-weight: 500;
  color: #111827;
}
.ex-batch-item-rate { color: #6b7280; font-size: 11px; }
.ex-batch-item-note { font-size: 11px; color: #9ca3af; max-width: 120px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.ex-batch-totals {
  display: flex;
  gap: 12px;
  padding: 10px 16px;
  background: #f9fafb;
  border-top: 1px solid #e5e7eb;
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-size: 12px;
  font-weight: 600;
}
.ex-batch-total-item { white-space: nowrap; }

/* ═══ Rate Matrix ═══ */
.ex-matrix-section { margin-top: 28px; margin-bottom: 8px; }
.ex-matrix-wrap { overflow-x: auto; padding: 12px 20px 16px; }
.ex-matrix {
  width: 100%;
  border-collapse: separate;
  border-spacing: 3px;
  font-size: 13px;
}
.ex-matrix th {
  padding: 12px 14px;
  text-align: center;
  font-size: 11px;
  font-weight: 800;
  color: #fff;
  background: linear-gradient(135deg, #4338ca, #6366f1);
  border-radius: 8px;
  letter-spacing: 0.5px;
}
.ex-matrix th:first-child { background: transparent; }
.ex-matrix td {
  padding: 10px 14px;
  text-align: center;
  border-radius: 8px;
  transition: all 0.2s;
}
.ex-matrix tbody tr { transition: background 0.15s; }
.ex-matrix tbody tr:hover td { background: #f5f3ff; }
.ex-matrix tbody tr:nth-child(even) td { background: #fafbff; }
.ex-matrix tbody tr:nth-child(even):hover td { background: #ede9fe; }
.ex-matrix-label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 800;
  color: #fff;
  font-size: 12px;
  text-align: left !important;
  letter-spacing: 0.3px;
  background: linear-gradient(135deg, #312e81, #4338ca) !important;
  border-radius: 8px;
  padding: 10px 14px;
}
.ex-matrix-self { background: #e2e8f0 !important; }
.ex-matrix-dash { color: #94a3b8; font-size: 16px; }
.ex-matrix-cell {
  cursor: pointer;
  transition: all 0.2s;
  border-radius: 8px;
  background: #fff;
  border: 1px solid #f1f5f9;
}
.ex-matrix-cell:hover {
  background: #4338ca !important;
  box-shadow: 0 4px 12px rgba(67,56,202,0.3);
  transform: scale(1.05);
}
.ex-matrix-cell:hover .ex-matrix-rate { color: #fff; }
.ex-matrix-rate {
  font-family: 'JetBrains Mono', ui-monospace, monospace;
  font-weight: 600;
  color: #0f172a;
  font-size: 12px;
  transition: color 0.2s;
}

/* ═══ Animation ═══ */
@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
.animate-spin { animation: spin 1s linear infinite; }

@media (max-width: 768px) {
  .ex-pos-bar-items { flex-wrap: nowrap; }
  .ex-terminal-tabs { overflow-x: auto; }
  .ex-batch-form-row { flex-direction: column; align-items: stretch; }
  .ex-batch-input { width: 100%; }
  .ex-batch-input--rate { width: 100%; }
  .ex-batch-input--note { width: 100%; }
  .ex-tab-key { display: none; }
}
</style>