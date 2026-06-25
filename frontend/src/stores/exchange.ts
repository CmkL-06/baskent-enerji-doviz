import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import apiService from '@/services/apiservice'
import type { Currency, Vault, Office } from '@/types/api'

const OFFICE_ID_KEY = 'selectedOfficeId'

export const useExchangeStore = defineStore('exchange', () => {
  const currencies      = ref<Currency[]>([])
  const vaults          = ref<Vault[]>([])
  const offices         = ref<Office[]>([])
  const exchangeRates   = ref<any[]>([])
  const currentOfficeId = ref<number | null>(null)
  const selectedOffice  = ref<Office | null>(null)
  const transactions    = ref<any[]>([])

  const currentOffice = computed(() =>
    selectedOffice.value ?? offices.value[0] ?? null
  )

  async function initialize() {
    await Promise.all([fetchCurrencies(), fetchVaults(), fetchOffices()])
  }

  async function fetchCurrencies() {
    try { currencies.value = await apiService.getCurrencies() } catch {}
  }

  async function fetchVaults() {
    try { vaults.value = await apiService.getVaults() } catch {}
  }

  // Load vaults for a specific office (used by ExchangeV2)
  async function loadVaults(officeId: any) {
    try {
      const data = await apiService.getVaultsByOfficeId(officeId)
      vaults.value = Array.isArray(data) ? data : (data?.items ?? data?.data ?? [])
    } catch {
      // fallback: load all vaults
      await fetchVaults()
    }
  }

  async function fetchOffices() {
    try {
      const data = await apiService.getOffices()
      offices.value = Array.isArray(data) ? data : (data?.items ?? data?.data ?? [])
      // Pick first office as selected if none selected yet
      if (!selectedOffice.value && offices.value.length > 0) {
        selectedOffice.value = offices.value[0]
      }
    } catch {}
  }

  // Alias used by ModernLayout.vue
  const loadOffices = fetchOffices

  async function loadExchangeRates(officeId?: number) {
    try {
      const id = officeId ?? selectedOffice.value?.officeId ?? currentOfficeId.value
      const data = await apiService.getExchangeRates(id ?? undefined)
      exchangeRates.value = Array.isArray(data) ? data : (data?.items ?? data?.data ?? [])
    } catch {}
  }

  function getExchangeRate(sourceCurrencyId: string, targetCurrencyId: string, transactionType: string): number | null {
    const rate = exchangeRates.value.find(
      r => r.sourceCurrencyId === sourceCurrencyId && r.targetCurrencyId === targetCurrencyId
    )
    if (rate) {
      return transactionType === 'buy' ? (rate.buyRate ?? null) : (rate.sellRate ?? null)
    }
    // Ters yön: source/target takas edilmiş, oranı tersine çevir
    const rev = exchangeRates.value.find(
      r => r.sourceCurrencyId === targetCurrencyId && r.targetCurrencyId === sourceCurrencyId
    )
    if (rev) {
      const base = transactionType === 'buy' ? rev.sellRate : rev.buyRate
      return base > 0 ? 1 / base : null
    }
    return null
  }

  function setSelectedOffice(office: Office | null) {
    selectedOffice.value = office
    if (office) currentOfficeId.value = (office as any).officeId ?? (office as any).id ?? null
  }

  function getSelectedOfficeId(): string | null {
    return localStorage.getItem(OFFICE_ID_KEY)
  }

  function setSelectedOfficeId(id: string | number | null) {
    if (id !== null && id !== undefined) {
      localStorage.setItem(OFFICE_ID_KEY, String(id))
      currentOfficeId.value = Number(id) || null
    } else {
      localStorage.removeItem(OFFICE_ID_KEY)
      currentOfficeId.value = null
    }
  }

  async function loadTransactionHistory(params?: any): Promise<any> {
    try {
      const data = await apiService.getTransactionHistory(params)
      const items = Array.isArray(data) ? data : (data?.items ?? data?.data ?? data?.transactions ?? [])
      transactions.value = items
      return data
    } catch {
      transactions.value = []
      return { totalCount: 0 }
    }
  }

  return {
    currencies, vaults, offices, exchangeRates, currentOfficeId, selectedOffice, currentOffice,
    transactions,
    initialize,
    fetchCurrencies, fetchVaults, fetchOffices,
    loadOffices, loadExchangeRates, loadVaults,
    getExchangeRate,
    setSelectedOffice, getSelectedOfficeId, setSelectedOfficeId,
    loadTransactionHistory,
    apiService
  }
})
