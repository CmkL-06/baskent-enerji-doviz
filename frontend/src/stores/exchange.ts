import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import apiService from '@/services/apiservice'
import type { Currency, Vault, Office } from '@/types/api'

const OFFICE_ID_KEY = 'selectedOfficeId'

// Deploy sırasındaki app-pool restart gibi kısa süreli ağ kesintilerinde ilk deneme başarısız
// olabilir (AxiosError: Network Error) — bu durumda tek seferlik bir tekrar deneme, kullanıcının
// sayfayı manuel yenilemesine gerek kalmadan store'un doğru veriyle kendini toparlamasını sağlar.
async function withRetry<T>(fn: () => Promise<T>, delayMs = 1000): Promise<T> {
  try {
    return await fn()
  } catch (e) {
    await new Promise(resolve => setTimeout(resolve, delayMs))
    return await fn()
  }
}

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
    try { currencies.value = await withRetry(() => apiService.getCurrencies()) }
    catch (e) { console.error('Para birimleri yüklenemedi:', e) }
  }

  async function fetchVaults() {
    try { vaults.value = await withRetry(() => apiService.getVaults()) }
    catch (e) { console.error('Kasalar yüklenemedi:', e) }
  }

  // Load vaults for a specific office (used by ExchangeV2)
  async function loadVaults(officeId: any) {
    try {
      const data = await apiService.getVaultsByOfficeId(officeId)
      vaults.value = Array.isArray(data) ? data : (data?.items ?? data?.data ?? [])
    } catch (e) {
      console.error('Ofis kasaları yüklenemedi, tüm kasalara geri dönülüyor:', e)
      // fallback: load all vaults
      await fetchVaults()
    }
  }

  async function fetchOffices() {
    try {
      const data = await withRetry(() => apiService.getOffices())
      offices.value = Array.isArray(data) ? data : (data?.items ?? data?.data ?? [])

      if (offices.value.length === 0) {
        selectedOffice.value = null
        return
      }

      // selectedOffice zaten set olsa bile, güncel ofis listesinde hâlâ var mı diye HER
      // fetchOffices() çağrısında yeniden doğrulanır — aksi halde bir ofis reorganizasyonu
      // sonrası bayat/silinmiş bir ofis seçili kalabilir ve o ofise bağlı her create/update
      // isteği FK ihlaliyle 500 döner (yalnızca tam sayfa yenilemesi sorunu çözerdi).
      const selectedId = (selectedOffice.value as any)?.officeId ?? (selectedOffice.value as any)?.id
      const stillValid = selectedId != null &&
        offices.value.some((o: any) => (o.officeId ?? o.id) === selectedId)

      if (!stillValid) {
        // localStorage.getItem() her zaman string döner; backend officeId'yi number olarak
        // dönerse (Office.officeId tipi number|string union) strict === hiç eşleşmez ve
        // kullanıcının seçtiği ofis her sayfa yenilemesinde sessizce ilk ofise düşerdi.
        const savedId = localStorage.getItem(OFFICE_ID_KEY)
        const match = savedId
          ? offices.value.find((o: any) => String(o.officeId ?? o.id) === savedId)
          : null
        selectedOffice.value = match ?? offices.value[0]

        const newId = (selectedOffice.value as any)?.officeId ?? (selectedOffice.value as any)?.id
        if (newId) localStorage.setItem(OFFICE_ID_KEY, String(newId))
      }
    } catch (e) { console.error('Ofisler yüklenemedi:', e) }
  }

  // Alias used by ModernLayout.vue
  const loadOffices = fetchOffices

  async function loadExchangeRates(officeId?: string | number) {
    try {
      const id = officeId ?? selectedOffice.value?.officeId ?? currentOfficeId.value
      const data = await apiService.getExchangeRates(id ?? undefined)
      exchangeRates.value = Array.isArray(data) ? data : (data?.items ?? data?.data ?? [])
    } catch (e) { console.error('Döviz kurları yüklenemedi:', e) }
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

  const settings = ref({
    defaultFromCurrency: 'USD',
    defaultToCurrency: 'TRY',
    autoRefreshRates: true,
    refreshInterval: 60,
    theme: 'light',
    language: 'tr'
  })

  async function updateSettings(newSettings: Partial<typeof settings.value>) {
    Object.assign(settings.value, newSettings)
  }

  return {
    currencies, vaults, offices, exchangeRates, currentOfficeId, selectedOffice, currentOffice,
    transactions, settings,
    initialize,
    fetchCurrencies, fetchVaults, fetchOffices,
    loadOffices, loadExchangeRates, loadVaults,
    getExchangeRate,
    setSelectedOffice, getSelectedOfficeId, setSelectedOfficeId,
    loadTransactionHistory, updateSettings
  }
})
