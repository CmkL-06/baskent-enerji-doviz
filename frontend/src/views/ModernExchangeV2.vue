<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'
import CurrencySelector from '@/components/common/CurrencySelector.vue'
import TransactionHistory from '@/components/common/TransactionHistory.vue'
import USDTPaymentsModal from './USDTPaymentsModal.vue'
import VaultCountingModal from './VaultCountingModal.vue'
import { useNotification } from '@/composables/useNotification'
import { getCurrencyCountryCode } from '@/utils/currency'

const { t } = useI18n()

const exchangeStore = useExchangeStore()
const authStore = useAuthStore()
const notification = useNotification()

// Transaction type toggle (buy/sell)
const transactionType = ref<'buy' | 'sell'>('buy')

// Popular currencies for quick access
const popularCurrencies = ['USD', 'EUR', 'RUB', 'KRUB', 'USDT']

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

// Methods
const selectQuickCurrency = (currencyCode: string) => {
  const currency = getCurrencyByCode(currencyCode)
  if (!currency) return
  
  // Update the first item's source currency
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
  const cleanValue = value.replace(',', '.')
  const amount = parseFloat(cleanValue) || 0
  item.sourceAmount = amount
  
  // Calculate target amount using exchange rate
  let rate = item.exchangeRate
  if (item.customRate !== null && item.customRate !== '') {
    const cleanValue = item.customRate.toString().replace(',', '.')
    const customRateNum = parseFloat(cleanValue)
    if (!isNaN(customRateNum)) {
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
        const cleanValue = item.customRate.toString().replace(',', '.')
        const customRateNum = parseFloat(cleanValue)
        if (!isNaN(customRateNum)) {
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
    const cleanValue = value.toString().replace(',', '.')
    const rate = parseFloat(cleanValue)
    
    if (!isNaN(rate)) {
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
  
  // Check balance when selling
  if (transactionType.value === 'sell' && balanceCheck.value) {
    const totalNeeded = exchangeItems.value
      .filter(i => i.targetCurrencyId === balanceCheck.value.currencyId)
      .reduce((sum, i) => sum + (i.targetAmount || 0), 0)
    
    if (totalNeeded > balanceCheck.value.balance) {
      notification.error(`Yetersiz bakiye. Gerekli: ${formatNumber(totalNeeded)}, Mevcut: ${formatNumber(balanceCheck.value.balance)} ${balanceCheck.value.currencyCode}`)
      return false
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
      customRate: item.customRate ? parseFloat(item.customRate.toString().replace(',', '.')) : undefined,
      notes: notes.value || undefined
    }))
    
    await apiService.createExchangeTransaction(transactions)
    
    // Show success message
    notification.success(t('exchange.messages.transactionSuccess'))
    
    resetForm()
    
    if (transactionHistoryRef.value) {
      transactionHistoryRef.value.loadTransactions()
    }
    
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

// Listen for USDT modal open event
const handleOpenUSDTModal = () => {
  openUSDTModal()
}

// Initialize
onMounted(async () => {
  // Add event listener for USDT modal
  window.addEventListener('open-usdt-modal', handleOpenUSDTModal)
  
  // Start vault check interval (every 5 minutes)
  vaultCheckInterval.value = setInterval(() => {
    checkVaultCounting()
  }, 5 * 60 * 1000) // 5 minutes
  
  isInitialLoading.value = true
  try {
    // Load user offices if not already loaded
    if (!authStore.isAdmin && authStore.user?.id && authStore.userOffices.length === 0) {
      try {
        const offices = await apiService.getUserOffices(authStore.user.id)
        authStore.userOffices = offices
      } catch (err) {
        console.error('Failed to load user offices:', err)
      }
    }
    
    // Load initial data
    let officesData
    
    if (!authStore.isAdmin && authStore.userOffices.length > 0) {
      officesData = authStore.userOffices.map(office => ({
        id: office.id || office.officeId,
        officeId: office.officeId || office.id,
        officeName: office.officeName
      }))
    } else {
      officesData = await apiService.getOffices()
    }
    
    const [currenciesData, vaultsDataRes, ratesData] = await Promise.all([
      apiService.getCurrencies(),
      apiService.getVaults(),
      apiService.getExchangeRates()
    ])
    
    currencies.value = currenciesData
    exchangeStore.offices = officesData
    exchangeStore.vaults = vaultsDataRes
    exchangeStore.exchangeRates = ratesData
    
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
  // Remove event listener
  window.removeEventListener('open-usdt-modal', handleOpenUSDTModal)
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
  <div v-if="isInitialLoading" class="w-full max-w-7xl mx-auto">
    <div class="flex items-center justify-center h-64 mt-8">
      <div class="text-center">
        <div class="inline-flex items-center justify-center w-16 h-16 mb-4">
          <svg class="animate-spin h-12 w-12 text-purple-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        </div>
        <p class="text-lg text-gray-600">{{ t('exchange.messages.loadingData') }}</p>
      </div>
    </div>
  </div>
  
  <!-- Page Hidden Overlay -->
  <div v-if="isPageHidden && !isInitialLoading" class="w-full max-w-7xl mx-auto">
    <div class="flex items-center justify-center h-[70vh]">
      <div class="text-center">
        <div class="inline-flex items-center justify-center w-24 h-24 mb-6 bg-gradient-to-br from-red-500 to-red-600 rounded-full shadow-lg">
          <span class="material-symbols-outlined text-white text-5xl">inventory</span>
        </div>
        <h2 class="text-3xl font-bold text-gray-900 mb-3">Kasa Sayımı Zorunlu</h2>
        <p class="text-lg text-gray-600 mb-8 max-w-md mx-auto">
          Devam etmek için kasanızı saymanız gerekmektedir.
          Bu işlem ihtiyarın mental ve ruh sağlığı açısından zorunludur.
        </p>
        <button 
          @click="openVaultCountingModal"
          class="px-8 py-3 bg-gradient-to-r from-red-600 to-red-700 text-white font-semibold rounded-xl hover:from-red-700 hover:to-red-800 transition-all shadow-lg hover:shadow-xl transform hover:-translate-y-0.5"
        >
          <span class="material-symbols-outlined mr-2 align-middle">calculate</span>
          Kasa Sayımına Başla
        </button>
      </div>
    </div>
  </div>
  
  <!-- Main Content -->
  <div v-else-if="!isPageHidden" class="w-full max-w-7xl mx-auto">
    <!-- Transaction Type Toggle - VERY PROMINENT -->
    <div class="mb-6">
      <div :class="[
        'rounded-xl shadow-lg p-1 transition-all',
        transactionType === 'buy' 
          ? 'bg-gradient-to-r from-green-500 to-green-600' 
          : 'bg-gradient-to-r from-red-500 to-red-600'
      ]">
        <div class="bg-white rounded-lg p-6">
          <h3 class="text-xl font-bold text-gray-900 mb-4 text-center">{{ t('exchange.transactionType.selectTitle') }}</h3>
          <div class="grid grid-cols-2 gap-4">
            <button
              @click="transactionType = 'buy'"
              :class="[
                'relative py-3 px-4 rounded-lg font-semibold transition-all transform',
                transactionType === 'buy' 
                  ? 'bg-gradient-to-r from-green-500 to-green-600 text-white shadow-lg scale-105 ring-2 ring-green-300' 
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200 hover:scale-102'
              ]"
            >
              <div class="flex flex-col items-center gap-1">
                <span class="material-symbols-outlined text-2xl">download</span>
                <span class="text-base">{{ t('exchange.transactionType.buy') }}</span>
                <span class="text-xs opacity-90">{{ t('exchange.transactionType.buyDesc') }}</span>
              </div>
              <div v-if="transactionType === 'buy'" 
                   class="absolute -top-2 -right-2 bg-white text-green-600 rounded-full p-0.5 shadow-lg">
                <span class="material-symbols-outlined text-lg">check_circle</span>
              </div>
            </button>
            <button
              @click="transactionType = 'sell'"
              :class="[
                'relative py-3 px-4 rounded-lg font-semibold transition-all transform',
                transactionType === 'sell' 
                  ? 'bg-gradient-to-r from-red-500 to-red-600 text-white shadow-lg scale-105 ring-2 ring-red-300' 
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200 hover:scale-102'
              ]"
            >
              <div class="flex flex-col items-center gap-1">
                <span class="material-symbols-outlined text-2xl">upload</span>
                <span class="text-base">{{ t('exchange.transactionType.sell') }}</span>
                <span class="text-xs opacity-90">{{ t('exchange.transactionType.sellDesc') }}</span>
              </div>
              <div v-if="transactionType === 'sell'" 
                   class="absolute -top-2 -right-2 bg-white text-red-600 rounded-full p-0.5 shadow-lg">
                <span class="material-symbols-outlined text-lg">check_circle</span>
              </div>
            </button>
          </div>
        </div>
      </div>
    </div>


    <!-- Main Layout -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Left Column: Process -->
      <div class="lg:col-span-2 space-y-6">
        <!-- Warning Message -->
        <div v-if="!selectedOfficeId || !selectedVaultId" class="bg-red-50 border-2 border-red-300 rounded-lg p-4 mb-6">
          <div class="flex items-center gap-3">
            <span class="material-symbols-outlined text-red-600 text-3xl">warning</span>
            <div>
              <p class="font-semibold text-red-900">{{ t('exchange.officeVault.warningTitle') }}</p>
              <p class="text-sm text-red-700 mt-1">
                {{ !selectedOfficeId ? t('exchange.officeVault.warningOffice') : '' }}
                {{ !selectedVaultId ? t('exchange.officeVault.warningVault') : '' }}
              </p>
            </div>
          </div>
        </div>
        

        <!-- Quick Currency Selection -->
        <div class="bg-white border border-gray-200 rounded-lg p-6 shadow-sm">
          <h3 class="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
            <span class="material-symbols-outlined text-purple-600">flash_on</span>
            {{ t('exchange.quickSelect.title') }}
          </h3>
          <div class="flex flex-wrap gap-3">
            <button
              v-for="currency in popularCurrencies"
              :key="currency"
              @click="selectQuickCurrency(currency)"
              class="relative px-4 py-2 bg-white border-2 border-gray-200 rounded-lg hover:shadow-lg transition-all flex items-center gap-2 overflow-hidden"
              :class="{
                'hover:border-green-400': currency === 'USD',
                'hover:border-blue-400': currency === 'EUR',
                'hover:border-red-400': currency === 'RUB',
                'hover:border-purple-400': currency === 'KRUB',
                'hover:border-teal-400': currency === 'USDT'
              }"
            >
              <div class="absolute left-0 top-1/2 w-1 h-[70%] -translate-y-1/2"
                :class="{
                  'bg-gradient-to-b from-green-400 to-green-600': currency === 'USD',
                  'bg-gradient-to-b from-blue-400 to-blue-600': currency === 'EUR',
                  'bg-gradient-to-b from-red-400 to-red-600': currency === 'RUB',
                  'bg-gradient-to-b from-purple-400 to-purple-600': currency === 'KRUB',
                  'bg-gradient-to-b from-teal-400 to-teal-600': currency === 'USDT'
                }"
              ></div>
              <span v-if="currency === 'KRUB'" class="material-symbols-outlined"
                :class="{
                  'text-purple-600': currency === 'KRUB'
                }"
              >credit_card</span>
              <span v-else-if="currency === 'USDT'" class="font-bold text-lg text-teal-600">₮</span>
              <img 
                v-else
                :src="`https://flagcdn.com/24x18/${currency === 'USD' ? 'us' : currency === 'EUR' ? 'eu' : currency === 'RUB' ? 'ru' : 'xx'}.png`" 
                :alt="currency"
                class="w-5 h-4"
                onerror="this.style.display='none'"
              >
              <span class="font-semibold"
                :class="{
                  'text-green-700': currency === 'USD',
                  'text-blue-700': currency === 'EUR',
                  'text-red-700': currency === 'RUB',
                  'text-purple-700': currency === 'KRUB',
                  'text-teal-700': currency === 'USDT'
                }"
              >{{ currency }}</span>
            </button>
          </div>
        </div>

        <!-- Exchange Items -->
        <div class="bg-white border border-gray-200 rounded-lg shadow-sm">
          <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
            <h3 class="text-lg font-semibold text-gray-900">{{ t('exchange.operations.title') }}</h3>
            <button 
              @click="addExchangeItem" 
              class="flex items-center gap-1 px-3 py-1.5 bg-purple-600 hover:bg-purple-700 text-white rounded-lg text-sm transition-colors"
            >
              <span class="material-symbols-outlined text-lg">add</span>
              <span class="hidden sm:inline">{{ t('exchange.operations.addButton') }}</span>
            </button>
          </div>

          <div class="p-6 space-y-4">
            <!-- All Items - Compact when multiple, full when single -->
            <div
              v-for="(item, index) in exchangeItems"
              :key="item.id"
            >
              <!-- Full Card (Only when single item) -->
              <div
                v-if="exchangeItems.length === 1"
                class="border-2 rounded-xl p-5 space-y-4 shadow-lg transition-all"
                :class="{
                  'border-green-400 bg-gradient-to-br from-green-50 via-emerald-50 to-green-100': transactionType === 'buy',
                  'border-red-400 bg-gradient-to-br from-red-50 via-rose-50 to-red-100': transactionType === 'sell'
                }"
              >
                <div class="flex justify-between items-center mb-3">
                  <div class="flex items-center gap-2">
                    <span class="text-sm font-semibold"
                      :class="{
                        'text-green-900': transactionType === 'buy',
                        'text-red-900': transactionType === 'sell'
                      }"
                    >{{ t('exchange.operations.transactionNumber') }}1</span>
                    <span class="px-3 py-1.5 rounded-full text-sm font-bold"
                      :class="{
                        'bg-green-600 text-white shadow-md': transactionType === 'buy',
                        'bg-red-600 text-white shadow-md': transactionType === 'sell'
                      }"
                    >
                      {{ transactionType === 'buy' ? 'ALIŞ' : 'SATIŞ' }}
                    </span>
                  </div>
                </div>

                <!-- Currency Selection Row -->
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">
                      {{ t('exchange.operations.receivedCurrency') }}
                      <span class="text-xs text-gray-500 ml-1">({{ transactionType === 'buy' ? t('exchange.operations.fromCustomer') : t('exchange.operations.toVault') }})</span>
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
                  <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">
                      {{ t('exchange.operations.givenCurrency') }}
                      <span class="text-xs text-gray-500 ml-1">({{ transactionType === 'buy' ? t('exchange.operations.fromVault') : t('exchange.operations.toCustomer') }})</span>
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

                <!-- Amount and Rate Row -->
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('exchange.operations.amount') }}</label>
                    <input
                      type="text"
                      :value="item.sourceAmount ? item.sourceAmount.toString().replace('.', ',') : ''"
                      @input="updateAmount(item, ($event.target as HTMLInputElement).value)"
                      class="amount-input font-mono"
                      placeholder="0,00"
                      inputmode="decimal"
                      :disabled="!selectedOfficeId || !selectedVaultId"
                    />
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">
                      {{ t('exchange.operations.rate') }}
                      <span v-if="item.customRate" class="ml-1 text-xs bg-yellow-100 text-yellow-800 px-1.5 py-0.5 rounded">{{ t('exchange.operations.customRate') }}</span>
                    </label>
                    <input
                      type="text"
                      :value="item.customRate !== null && item.customRate !== '' ? item.customRate : (item.rateManuallySet ? '' : item.exchangeRate.toString())"
                      @input="handleRateInput(item, ($event.target as HTMLInputElement).value)"
                      @blur="handleRateBlur(item)"
                      @focus="($event.target as HTMLInputElement).select()"
                      class="rate-input font-mono"
                      :class="{ 'custom-rate': item.customRate !== null && item.customRate !== '' }"
                      placeholder="0,00"
                      inputmode="decimal"
                      :disabled="!selectedOfficeId || !selectedVaultId"
                    />
                  </div>
                </div>

                <!-- External Market Rates - Full Width at Bottom -->
                <div v-if="getExternalRatesForItem(item).length > 0" class="mt-3 pt-3 border-t border-gray-300">
                  <div class="text-xs font-medium text-gray-600 mb-2 flex items-center gap-1">
                    <span class="material-symbols-outlined text-sm">trending_up</span>
                    Piyasa Kurları Karşılaştırması
                  </div>
                  <div class="flex flex-wrap gap-2">
                    <div class="flex items-center gap-1.5 px-3 py-2 bg-gradient-to-r from-purple-50 to-blue-50 border-2 border-purple-300 rounded-lg shadow-sm">
                      <span class="font-bold text-purple-700 text-sm">Bizim Sistem:</span>
                      <span class="font-mono text-purple-900 font-bold text-base">{{ formatNumber(item.exchangeRate, 4) }}</span>
                    </div>
                    <div
                      v-for="rate in getExternalRatesForItem(item)"
                      :key="rate.source"
                      class="flex items-center gap-1.5 px-3 py-2 bg-white rounded-lg border-2 shadow-sm transition-all hover:shadow-md"
                      :class="{
                        'border-green-400 bg-green-50': rate.displayRate && item.exchangeRate && rate.displayRate < item.exchangeRate,
                        'border-red-400 bg-red-50': rate.displayRate && item.exchangeRate && rate.displayRate > item.exchangeRate,
                        'border-gray-300': !rate.displayRate || !item.exchangeRate || rate.displayRate === item.exchangeRate
                      }"
                    >
                      <span class="font-bold text-gray-700 text-sm">{{ rate.source }}:</span>
                      <span class="font-mono font-semibold text-base"
                        :class="{
                          'text-green-700': rate.displayRate && item.exchangeRate && rate.displayRate < item.exchangeRate,
                          'text-red-700': rate.displayRate && item.exchangeRate && rate.displayRate > item.exchangeRate,
                          'text-gray-900': !rate.displayRate || !item.exchangeRate || rate.displayRate === item.exchangeRate
                        }"
                      >{{ rate.displayRate ? formatNumber(rate.displayRate, 4) : '-' }}</span>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Compact Card (When multiple items) -->
              <div
                v-else
                class="border-2 rounded-lg p-3 hover:shadow-md transition-all relative"
                :class="{
                  'border-green-300 bg-gradient-to-r from-green-50 to-emerald-50': transactionType === 'buy',
                  'border-red-300 bg-gradient-to-r from-red-50 to-rose-50': transactionType === 'sell'
                }"
                :style="{ zIndex: 100 - index }"
              >
                <div class="flex items-center gap-2">
                  <!-- Transaction Number & Type Badge -->
                  <div class="flex items-center gap-1.5 w-24 flex-shrink-0">
                    <span class="text-xs font-semibold text-gray-600 whitespace-nowrap">İşlem {{ index + 1 }}</span>
                    <span class="px-1.5 py-0.5 rounded text-[10px] font-bold whitespace-nowrap"
                      :class="{
                        'bg-green-600 text-white': transactionType === 'buy',
                        'bg-red-600 text-white': transactionType === 'sell'
                      }"
                    >
                      {{ transactionType === 'buy' ? 'ALIŞ' : 'SATIŞ' }}
                    </span>
                  </div>

                  <!-- Received Currency -->
                  <div class="flex-1 min-w-[200px]">
                    <CurrencySelector
                      :modelValue="transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId"
                      @update:modelValue="transactionType === 'buy' ? updateSourceCurrency(item, $event) : updateTargetCurrency(item, $event)"
                      :currencies="transactionType === 'buy' ? filteredCurrencies : currencies"
                      :placeholder="t('exchange.operations.selectCurrency')"
                      :disabled="!selectedOfficeId || !selectedVaultId"
                      :vaultBalances="selectedVaultBalances"
                      compact
                      :dropdownMinWidth="300"
                    />
                  </div>

                  <!-- Arrow Icon -->
                  <span class="material-symbols-outlined text-gray-400 text-sm flex-shrink-0">arrow_forward</span>

                  <!-- Given Currency -->
                  <div class="flex-1 min-w-[200px]">
                    <CurrencySelector
                      :modelValue="transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId"
                      @update:modelValue="transactionType === 'buy' ? updateTargetCurrency(item, $event) : updateSourceCurrency(item, $event)"
                      :currencies="transactionType === 'buy' ? currencies : filteredCurrencies"
                      :placeholder="t('exchange.operations.selectCurrency')"
                      :disabled="!selectedOfficeId || !selectedVaultId"
                      :vaultBalances="selectedVaultBalances"
                      compact
                      :dropdownMinWidth="300"
                    />
                  </div>

                  <!-- Amount -->
                  <div class="w-28">
                    <input
                      type="text"
                      :value="item.sourceAmount ? item.sourceAmount.toString().replace('.', ',') : ''"
                      @input="updateAmount(item, ($event.target as HTMLInputElement).value)"
                      class="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent font-mono"
                      placeholder="Miktar"
                      inputmode="decimal"
                      :disabled="!selectedOfficeId || !selectedVaultId"
                    />
                  </div>

                  <!-- Rate -->
                  <div class="w-28">
                    <input
                      type="text"
                      :value="item.customRate !== null && item.customRate !== '' ? item.customRate : (item.rateManuallySet ? '' : item.exchangeRate.toString())"
                      @input="handleRateInput(item, ($event.target as HTMLInputElement).value)"
                      @blur="handleRateBlur(item)"
                      @focus="($event.target as HTMLInputElement).select()"
                      class="w-full px-2 py-1.5 text-sm border rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent font-mono"
                      :class="{
                        'border-yellow-300 bg-yellow-50': item.customRate !== null && item.customRate !== '',
                        'border-gray-300': !(item.customRate !== null && item.customRate !== '')
                      }"
                      placeholder="Kur"
                      inputmode="decimal"
                      :disabled="!selectedOfficeId || !selectedVaultId"
                    />
                  </div>

                  <!-- Delete Button -->
                  <button
                    v-if="exchangeItems.length > 1"
                    @click="removeExchangeItem(index)"
                    class="text-red-600 hover:text-red-700 hover:bg-red-50 p-1.5 rounded flex-shrink-0"
                  >
                    <span class="material-symbols-outlined text-lg">delete</span>
                  </button>
                </div>

                <!-- External Market Rates - Full Width at Bottom -->
                <div v-if="getExternalRatesForItem(item).length > 0" class="mt-2 pt-2 border-t border-gray-300">
                  <div class="text-xs font-medium text-gray-600 mb-1.5 flex items-center gap-1">
                    <span class="material-symbols-outlined text-xs">trending_up</span>
                    Piyasa Kurları
                  </div>
                  <div class="flex flex-wrap gap-1.5">
                    <div class="flex items-center gap-1 px-2 py-1 bg-gradient-to-r from-purple-50 to-blue-50 border-2 border-purple-300 rounded shadow-sm">
                      <span class="font-bold text-purple-700 text-xs">Sistem:</span>
                      <span class="font-mono text-purple-900 font-bold text-sm">{{ formatNumber(item.exchangeRate, 4) }}</span>
                    </div>
                    <div
                      v-for="rate in getExternalRatesForItem(item)"
                      :key="rate.source"
                      class="flex items-center gap-1 px-2 py-1 bg-white rounded border-2 shadow-sm transition-all hover:shadow-md"
                      :class="{
                        'border-green-400 bg-green-50': rate.displayRate && item.exchangeRate && rate.displayRate < item.exchangeRate,
                        'border-red-400 bg-red-50': rate.displayRate && item.exchangeRate && rate.displayRate > item.exchangeRate,
                        'border-gray-300': !rate.displayRate || !item.exchangeRate || rate.displayRate === item.exchangeRate
                      }"
                    >
                      <span class="font-bold text-gray-700 text-xs">{{ rate.source }}:</span>
                      <span class="font-mono font-semibold text-sm"
                        :class="{
                          'text-green-700': rate.displayRate && item.exchangeRate && rate.displayRate < item.exchangeRate,
                          'text-red-700': rate.displayRate && item.exchangeRate && rate.displayRate > item.exchangeRate,
                          'text-gray-900': !rate.displayRate || !item.exchangeRate || rate.displayRate === item.exchangeRate
                        }"
                      >{{ rate.displayRate ? formatNumber(rate.displayRate, 4) : '-' }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Notes -->
        <div class="bg-white border border-gray-200 rounded-lg shadow-sm overflow-hidden">
          <button
            @click="showNotes = !showNotes"
            class="w-full px-6 py-4 flex items-center justify-between hover:bg-gray-50 transition-colors"
          >
            <div class="flex items-center gap-2">
              <span class="material-symbols-outlined text-gray-600">{{ showNotes ? 'expand_less' : 'expand_more' }}</span>
              <span class="text-sm font-medium text-gray-700">{{ t('exchange.notes.label') }}</span>
              <span v-if="notes" class="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs rounded-full">Not var</span>
            </div>
            <span class="material-symbols-outlined text-gray-400">{{ showNotes ? 'remove' : 'add' }}</span>
          </button>

          <div v-if="showNotes" class="px-6 pb-6 pt-2">
            <textarea
              v-model="notes"
              rows="3"
              class="notes-textarea"
              :placeholder="t('exchange.notes.placeholder')"
              :disabled="!selectedOfficeId || !selectedVaultId"
            ></textarea>
          </div>
        </div>
      </div>

      <!-- Right Column: Summary & USDT -->
      <div class="space-y-6">
        <!-- Transaction Summary -->
        <div class="bg-gradient-to-br from-white to-gray-50 border border-gray-200 rounded-xl shadow-lg overflow-hidden">
          <div class="bg-gradient-to-r from-purple-600 to-blue-600 px-6 py-4">
            <h3 class="text-lg font-semibold text-white flex items-center gap-2">
              <span class="material-symbols-outlined">receipt_long</span>
              {{ t('exchange.summary.title') }}
            </h3>
          </div>
          <div class="p-6">
            
            <!-- Balance Check for Selling -->
            <div v-if="transactionType === 'sell' && balanceCheck" class="mb-4">
              <div 
                class="p-3 rounded-lg border"
                :class="{
                  'bg-blue-50 border-blue-200': isBalanceSufficient,
                  'bg-red-50 border-red-200': !isBalanceSufficient
                }"
              >
                <div class="flex items-center gap-2 text-sm">
                  <span 
                    class="material-symbols-outlined"
                    :class="{
                      'text-blue-600': isBalanceSufficient,
                      'text-red-600': !isBalanceSufficient
                    }"
                  >
                    {{ isBalanceSufficient ? 'account_balance' : 'error' }}
                  </span>
                  <div>
                    <p class="font-medium" :class="{
                      'text-blue-900': isBalanceSufficient,
                      'text-red-900': !isBalanceSufficient
                    }">
                      {{ t('exchange.summary.vaultBalance') }} {{ !isBalanceSufficient ? '(' + t('exchange.summary.insufficient') + ')' : '' }}
                    </p>
                    <p :class="{
                      'text-blue-700': isBalanceSufficient,
                      'text-red-700': !isBalanceSufficient
                    }">
                      {{ t('exchange.summary.available') }}: {{ formatNumber(balanceCheck.balance) }} {{ balanceCheck.currencyCode }}
                      <span v-if="totalForeignCurrencyNeeded > 0">
                        / {{ t('exchange.summary.required') }}: {{ formatNumber(totalForeignCurrencyNeeded) }}
                      </span>
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Summary Table -->
            <div class="mb-4">
              <div class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">{{ t('exchange.summary.details') }}</div>

              <!-- Single Item - Full Card -->
              <div v-if="exchangeItems.length === 1" class="space-y-3">
                <div
                  v-for="item in exchangeItems"
                  :key="item.id"
                  class="bg-gradient-to-r from-gray-50 to-white rounded-lg p-4 border border-gray-200"
                >
                  <!-- Received Amount (Prominent) -->
                  <div class="mb-3 pb-3 border-b border-gray-200">
                    <div class="text-xs text-gray-500 mb-1">
                      {{ t('exchange.summary.receivedFromCustomer') }}
                    </div>
                    <div class="flex items-center gap-2">
                      <div class="flex items-center gap-1">
                        <span v-if="getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode === 'USDT'"
                              class="text-green-600 font-bold text-xl">₮</span>
                        <span v-else-if="getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode === 'KRUB'"
                              class="text-lg">💳</span>
                        <i v-else-if="getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode || '')"
                           :class="`fi fi-${getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode || '')}`"
                           class="text-xl"></i>
                      </div>
                      <div>
                        <span class="text-green-600 text-2xl font-bold">
                          +{{ formatNumber(transactionType === 'buy' ? item.sourceAmount : item.targetAmount) }}
                        </span>
                        <span class="text-lg font-semibold text-gray-700 ml-1">
                          {{ getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode || '-' }}
                        </span>
                      </div>
                    </div>
                  </div>

                  <!-- Exchange Details -->
                  <div class="flex items-center justify-between">
                    <div class="text-xs text-gray-500">
                      {{ t('exchange.summary.exchangeRate') }}: {{ formatNumber(item.exchangeRate) }}
                      <span v-if="item.customRate" class="ml-1 bg-yellow-100 text-yellow-700 px-1.5 py-0.5 rounded text-xs font-medium">{{ t('exchange.operations.customRate') }}</span>
                    </div>
                    <div class="flex items-center gap-2">
                      <span class="material-symbols-outlined text-gray-400 text-lg">swap_horiz</span>
                      <div class="text-right">
                        <div class="text-xs text-gray-500">
                          {{ transactionType === 'buy' ? t('exchange.summary.givenFromVault') : t('exchange.summary.givenToCustomer') }}
                        </div>
                        <div class="flex items-center gap-1">
                          <span v-if="getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode === 'USDT'"
                                class="text-green-600 font-bold">₮</span>
                          <span v-else-if="getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode === 'KRUB'"
                                class="text-sm">💳</span>
                          <i v-else-if="getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode || '')"
                             :class="`fi fi-${getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode || '')}`"
                             class="text-sm"></i>
                          <span class="text-red-600 font-bold">
                            -{{ formatNumber(transactionType === 'buy' ? item.targetAmount : item.sourceAmount) }}
                          </span>
                          <span class="font-medium text-gray-700 text-sm ml-0.5">
                            {{ getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode }}
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Multiple Items - Compact List -->
              <div v-else class="bg-gradient-to-r from-gray-50 to-white rounded-lg p-3 border border-gray-200 space-y-1.5">
                <div
                  v-for="(item, index) in exchangeItems"
                  :key="item.id"
                  class="flex items-center justify-between py-1.5 border-b border-gray-100 last:border-0"
                >
                  <span class="text-xs text-gray-500 w-8">#{{ index + 1 }}</span>
                  <div class="flex items-center gap-1.5 flex-1">
                    <i v-if="getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode || '')"
                       :class="`fi fi-${getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode || '')}`"
                       class="text-sm"></i>
                    <span class="text-green-600 font-bold text-sm">
                      +{{ formatNumber(transactionType === 'buy' ? item.sourceAmount : item.targetAmount) }}
                    </span>
                    <span class="text-xs font-medium text-gray-700">
                      {{ getCurrencyById(transactionType === 'buy' ? item.sourceCurrencyId : item.targetCurrencyId)?.currencyCode }}
                    </span>
                  </div>
                  <span class="material-symbols-outlined text-gray-300 text-sm">arrow_forward</span>
                  <div class="flex items-center gap-1.5 flex-1 justify-end">
                    <i v-if="getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode || '')"
                       :class="`fi fi-${getCurrencyCountryCode(getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode || '')}`"
                       class="text-sm"></i>
                    <span class="text-red-600 font-bold text-sm">
                      -{{ formatNumber(transactionType === 'buy' ? item.targetAmount : item.sourceAmount) }}
                    </span>
                    <span class="text-xs font-medium text-gray-700">
                      {{ getCurrencyById(transactionType === 'buy' ? item.targetCurrencyId : item.sourceCurrencyId)?.currencyCode }}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Grand Total -->
            <div class="bg-gradient-to-r from-purple-50 to-blue-50 rounded-lg p-4 border border-purple-200">
              <div class="flex items-center justify-between mb-2">
                <span class="text-sm font-medium text-gray-700 flex items-center gap-1">
                  <span class="material-symbols-outlined text-purple-600 text-lg">payments</span>
                  {{ t('exchange.summary.paymentAmount') }}
                </span>
              </div>
              <div class="space-y-2">
                <div v-for="total in grandTotal" :key="total.currencyCode" 
                     class="flex justify-between items-center">
                  <div class="flex items-center gap-2">
                    <span v-if="total.currencyCode === 'USDT'" 
                          class="text-green-600 font-bold">₮</span>
                    <span v-else-if="total.currencyCode === 'KRUB'" 
                          class="text-sm">💳</span>
                    <i v-else-if="getCurrencyCountryCode(total.currencyCode)" 
                       :class="`fi fi-${getCurrencyCountryCode(total.currencyCode)}`"
                       class="text-lg"></i>
                    <span class="font-medium text-gray-700">{{ total.currencyCode }}</span>
                  </div>
                  <span class="text-xl font-bold text-purple-700 font-mono">
                    {{ formatNumber(total.amount) }}
                  </span>
                </div>
                <div v-if="grandTotal.length === 0" class="text-center text-gray-500 text-sm py-2">
                  {{ t('exchange.summary.noTransaction') }}
                </div>
                
                <!-- Total TRY Amount - Only show for SELL transactions -->
                <div v-if="transactionType === 'sell' && totalTryAmount > 0" class="mt-3 pt-3 border-t border-purple-200">
                  <div class="flex justify-between items-center">
                    <span class="text-sm font-medium text-gray-700">
                      Müşteriden Alınacak Toplam:
                    </span>
                    <div class="flex items-center gap-1">
                      <span class="text-xl font-bold text-purple-700 font-mono">
                        {{ formatNumber(totalTryAmount) }}
                      </span>
                      <i class="fi fi-tr text-lg"></i>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Submit Button -->
            <button
              @click="submitExchange"
              :disabled="isLoading || exchangeItems.length === 0 || !selectedOfficeId || !selectedVaultId"
              class="w-full mt-6 px-6 py-3 bg-gradient-to-r from-green-600 to-green-700 text-white font-semibold rounded-xl hover:from-green-700 hover:to-green-800 disabled:opacity-50 disabled:cursor-not-allowed transition-all flex items-center justify-center gap-2 shadow-lg hover:shadow-xl transform hover:-translate-y-0.5"
            >
              <span v-if="isLoading" class="material-symbols-outlined animate-spin">refresh</span>
              <span v-else class="material-symbols-outlined">check_circle</span>
              <span>{{ isLoading ? t('exchange.summary.processing') : t('exchange.summary.submit') }}</span>
            </button>
          </div>
        </div>

        <!-- Vault Count Warning (if admin) -->
        <div v-if="showVaultCountWarning" class="bg-gradient-to-br from-white to-gray-50 border-2 border-amber-400 rounded-xl shadow-lg overflow-hidden">
          <div class="bg-gradient-to-r from-amber-500 to-orange-500 px-4 py-3">
            <h3 class="text-base font-bold text-white flex items-center gap-2">
              <span class="material-symbols-outlined">inventory</span>
              Kasa Sayımı Gerekli
            </h3>
          </div>
          <div class="p-4">
            <p class="text-sm text-amber-900 mb-3">
              Bu kasa için sayım henüz yapılmamış. Lütfen en kısa sürede kasa sayımı yapın.
            </p>
            <button
              @click="openManualVaultCounting"
              class="w-full px-4 py-2.5 bg-gradient-to-r from-amber-600 to-orange-600 text-white font-semibold rounded-lg hover:from-amber-700 hover:to-orange-700 transition-all shadow-md hover:shadow-lg flex items-center justify-center gap-2"
            >
              <span class="material-symbols-outlined text-lg">calculate</span>
              <span>Sayım Yap</span>
            </button>
          </div>
        </div>

        <!-- Control Panel -->
        <div class="bg-gradient-to-br from-white to-gray-50 border border-gray-200 rounded-xl shadow-lg overflow-hidden">
          <div class="bg-gradient-to-r from-gray-700 to-gray-800 px-4 py-3">
            <h3 class="text-base font-bold text-white flex items-center gap-2">
              <span class="material-symbols-outlined">settings</span>
              Kontrol Paneli
            </h3>
          </div>
          <div class="p-4 space-y-3">
            <!-- Office Selection -->
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1.5">Ofis</label>
              <select
                v-model="selectedOfficeId"
                class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent bg-white"
              >
                <option value="">Ofis Seçin</option>
                <option v-for="office in exchangeStore.offices" :key="office.officeId || office.id" :value="office.officeId || office.id">
                  {{ office.officeName }}
                </option>
              </select>
            </div>

            <!-- Vault Selection -->
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1.5">Kasa</label>
              <select
                v-model="selectedVaultId"
                :disabled="!selectedOfficeId"
                class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent bg-white disabled:bg-gray-100 disabled:cursor-not-allowed"
              >
                <option value="">Kasa Seçin</option>
                <option v-for="vault in availableVaults" :key="vault.vaultId || vault.id" :value="vault.vaultId || vault.id">
                  {{ vault.vaultName }}
                </option>
              </select>
            </div>

            <!-- Action Buttons -->
            <div class="pt-2 space-y-2">
              <!-- Refresh Rates Button -->
              <button
                @click="refreshRates"
                :disabled="!selectedOfficeId || loadingExternalRates"
                class="w-full px-3 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-300 text-white text-sm font-medium rounded-lg transition-colors flex items-center justify-center gap-2 disabled:cursor-not-allowed"
              >
                <span class="material-symbols-outlined text-base" :class="{ 'animate-spin': loadingExternalRates }">sync</span>
                <span>Tüm Kurları Yenile</span>
                <span v-if="refreshCountdown < 60" class="text-xs opacity-90">({{ refreshCountdown }}s)</span>
              </button>

              <!-- Vault Count Button -->
              <button
                @click="openManualVaultCounting"
                :disabled="!selectedVaultId"
                class="w-full px-3 py-2 bg-amber-600 hover:bg-amber-700 disabled:bg-gray-300 text-white text-sm font-medium rounded-lg transition-colors flex items-center justify-center gap-2 disabled:cursor-not-allowed"
              >
                <span class="material-symbols-outlined text-base">calculate</span>
                <span>Kasa Sayımı Yap</span>
              </button>

              <!-- Print Button -->
              <button
                @click="transactionHistoryRef?.printAllTransactions?.()"
                :disabled="!selectedOfficeId"
                class="w-full px-3 py-2 bg-purple-600 hover:bg-purple-700 disabled:bg-gray-300 text-white text-sm font-medium rounded-lg transition-colors flex items-center justify-center gap-2 disabled:cursor-not-allowed"
              >
                <span class="material-symbols-outlined text-base">print</span>
                <span>Yazdır</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <!-- Transaction History -->
    <div class="mt-8 mb-6">
      <TransactionHistory 
        ref="transactionHistoryRef"
        :office-id="selectedOfficeId"
        :page-size="50"
        :show-actions="true"
        :show-refresh-button="true"
        :selected-date="new Date().toISOString().split('T')[0]"
      />
    </div>
    
    <!-- USDT Payments Modal -->
    <USDTPaymentsModal ref="usdtModalRef" />
  </div>
  
  <!-- Vault Counting Modal - OUTSIDE of conditional blocks so it's always mounted -->
  <VaultCountingModal 
    ref="vaultCountingModalRef"
    :vault-id="currentVault?.vaultId || currentVault?.id || selectedVaultId"
    :vault-name="currentVault?.vaultName || availableVaults.find(v => (v.vaultId || v.id) === selectedVaultId)?.vaultName || ''"
    :vault-balances="currentVault?.balances || selectedVaultBalances"
    :is-manual="currentVault?.isManual || false"
    @complete="handleVaultCountComplete"
    @waiting-customer="handleWaitingCustomer"
  />
</template>

<style scoped>
/* Form Inputs */
.amount-input,
.rate-input,
.notes-textarea,
.form-select {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  font-size: 1rem;
  transition: all 0.3s ease;
  outline: none;
}

.form-select {
  background-color: white;
  cursor: pointer;
  appearance: none;
  -webkit-appearance: none;
  -moz-appearance: none;
  background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%239333ea' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
  background-position: right 0.5rem center;
  background-repeat: no-repeat;
  background-size: 1.5em 1.5em;
  padding-right: 2.5rem;
}

.amount-input:focus,
.rate-input:focus,
.notes-textarea:focus,
.form-select:focus {
  border-color: #9333ea;
  box-shadow: 0 0 0 3px rgba(147, 51, 234, 0.1);
}

.rate-input.custom-rate {
  background-color: #fef3c7;
}

.notes-textarea {
  resize: none;
  min-height: 80px;
}

/* Material Symbols */
.material-symbols-outlined {
  font-variation-settings: 
    'FILL' 0,
    'wght' 400,
    'GRAD' 0,
    'opsz' 24;
}

/* Animation */
@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.animate-spin {
  animation: spin 1s linear infinite;
}
</style>