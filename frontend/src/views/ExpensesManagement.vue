<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import { useNotification } from '@/composables/useNotification'
import { useExpandable } from '@/composables/useExpandable'
import AppKpiCard from '@/components/common/AppKpiCard.vue'
import AppPageHeader from '@/components/common/AppPageHeader.vue'
import AppEmptyState from '@/components/common/AppEmptyState.vue'

const authStore = useAuthStore()
const exchangeStore = useExchangeStore()
const notification = useNotification()

const loading = ref(true)
const saving = ref(false)
const error = ref('')

// Data
const definitions = ref<any[]>([])
const payments = ref<any[]>([])
const currencies = ref<any[]>([])
const pendingApprovals = ref<any[]>([])
const budgetStatus = ref<any[]>([])

// Gider kategorileri — artık sabit enum değil, kullanıcı tarafından yönetilebilen (Currency ile
// aynı desende) bir liste. `categoryMap` hızlı isim çözümlemesi için.
const categories = ref<any[]>([])
const activeCategories = computed(() => categories.value.filter((c: any) => c.isActive))
const categoryMap = computed<Record<string, string>>(() => {
  const m: Record<string, string> = {}
  categories.value.forEach((c: any) => { m[c.id] = c.name })
  return m
})

async function loadCategories() {
  try {
    categories.value = await apiService.getExpenseCategories() ?? []
  } catch (e) {
    console.error('Kategoriler yüklenemedi:', e)
  }
}

// Kategori yönetimi (basit liste + ekle + pasifleştir/aktifleştir + sil) — Currency yönetimiyle
// aynı görsel dilde ama küçük ölçekte, ayrı bir sayfa yerine hafif bir modal.
const showCategoryModal = ref(false)
const newCategoryName = ref('')
const categorySaving = ref(false)
const categoryDeleteConfirmId = ref<string | null>(null)

async function openCategoryModal() {
  showCategoryModal.value = true
  await loadCategories()
}

async function addCategory() {
  if (!newCategoryName.value.trim()) return
  categorySaving.value = true
  try {
    await apiService.saveExpenseCategory({ name: newCategoryName.value.trim(), isActive: true })
    newCategoryName.value = ''
    await loadCategories()
    notification.success('Kategori eklendi')
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Kategori eklenemedi')
  } finally {
    categorySaving.value = false
  }
}

async function toggleCategoryActive(c: any) {
  try {
    await apiService.saveExpenseCategory({ id: c.id, name: c.name, isActive: !c.isActive })
    await loadCategories()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Güncellenemedi')
  }
}

async function deleteCategory(c: any) {
  try {
    await apiService.deleteExpenseCategory(c.id)
    notification.success('Kategori silindi')
    categoryDeleteConfirmId.value = null
    await loadCategories()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Kategori silinemedi (kullanımda olabilir)')
  }
}

function categoryUsage(categoryId: string): number {
  return categories.value.find((c: any) => c.id === categoryId)?.usageCount ?? 0
}

// Bütçe kartından hızlı kategori adı düzenleme (ayrı modale gitmeden)
const editCategoryId = ref<string | null>(null)
const editCategoryName = ref('')

function startRenameCategory(row: any) {
  editCategoryId.value = row.categoryId
  editCategoryName.value = row.categoryName
}

async function saveRenameCategory(row: any) {
  if (!editCategoryName.value.trim()) return
  try {
    await apiService.saveExpenseCategory({ id: row.categoryId, name: editCategoryName.value.trim(), isActive: true })
    editCategoryId.value = null
    await loadCategories()
    notification.success('Kategori güncellendi')
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Kategori güncellenemedi')
  }
}

// Filters
const filterCategory = ref<string>('')
const filterDateFrom = ref('')
const filterDateTo = ref('')

// Bütçe ay seçici — varsayılan mevcut ay
const budgetYear = ref(new Date().getFullYear())
const budgetMonth = ref(new Date().getMonth() + 1)
const budgetSaving = ref<string | null>(null)
const budgetDrafts = ref<Record<string, number | null>>({})

// Onay akışı: satır içi red-notu (bkz. OwnerApprovals.vue deseni)
const approvalActingId = ref<string | null>(null)
const approvalRejectFor = ref<string | null>(null)
const approvalRejectNote = ref('')

// Bütçe kartları: hangi kategori kartı açık — tıklanınca o ayki harcama detayı (gün/kişi/amaç/tutar) gösterilir
const { toggle: toggleBudgetCard, isExpanded: isBudgetExpanded } = useExpandable<string>()

// Ödemeler listesi: açılır satır (accordion) + satır içi silme onayı
const { toggle: togglePaymentRow, isExpanded: isPaymentExpanded } = useExpandable<string>()
const deleteConfirmId = ref<string | null>(null)
const deleteReason = ref('')

// Modal
const showDefModal = ref(false)
const showPayModal = ref(false)
const editDef = ref<any>(null)
const defDeleteConfirmId = ref<string | null>(null)

const defForm = ref({
  code: '',
  name: '',
  categoryId: '' as string,
  description: '',
  isActive: true,
  isRecurring: false,
  recurrencePeriod: null as number | null,
  defaultAmount: null as number | null,
  defaultCurrencyId: null as string | null,
  accountReference: '' as string,
  dueDayOfMonth: null as number | null,
})

// Yaklaşan/gecikmiş tekrarlayan gider kalemleri (salt-okunur, hatırlatma)
const upcomingDefinitions = ref<any[]>([])

// Gider kalemi ekstresi — tıklanan satırın altında açılır (accordion)
const { toggle: toggleDefRow, isExpanded: isDefExpanded } = useExpandable<string>()
const definitionStatements = ref<Record<string, any>>({})
const statementLoading = ref<string | null>(null)

const payForm = ref({
  expenseDefinitionId: '',
  currencyId: '',
  paymentDate: new Date().toISOString().slice(0, 10),
  amount: null as number | null,
  paymentMethod: 1,
  referenceNumber: '',
  description: '',
})

// Ödeme modalında sıfırdan tanım oluşturma — kullanıcı önce ayrı bir "Tanım" akışına
// gitmek zorunda kalmasın diye "+ Yeni Gider Türü" seçeneği aynı formda tutuluyor.
const NEW_DEF_OPTION = '__new__'
const quickDefName = ref('')
const quickDefCategoryId = ref('')

// rm_expensedefinition.Code zorunlu — teknik olmayan kullanıcı için sürtünmeyi azaltmak amacıyla
// zaman damgası tabanlı, çakışmaya dayanıklı bir kod otomatik önerilir (yine de düzenlenebilir).
function generateQuickDefCode(): string {
  return `GDR-${Date.now().toString(36).toUpperCase()}`
}

const PAYMENT_METHODS: Record<number, string> = {
  1: 'Nakit', 2: 'Banka Transferi', 3: 'Kredi Kartı', 4: 'Çek', 5: 'Diğer',
}

const RECURRENCE_PERIODS: Record<number, string> = {
  1: 'Günlük', 2: 'Haftalık', 3: 'Aylık', 4: 'Üç Aylık', 5: 'Yıllık',
}

const STATUS_LABELS: Record<number, string> = {
  1: 'Beklemede', 2: 'Ödendi', 3: 'İptal Edildi', 4: 'İade Edildi',
}

const months = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık']

const officeId = computed(() =>
  exchangeStore.selectedOffice?.officeId ?? exchangeStore.offices[0]?.officeId
)

const filteredPayments = computed(() => {
  let list = payments.value
  if (filterCategory.value !== '') {
    list = list.filter(p => p.categoryId === filterCategory.value)
  }
  if (filterDateFrom.value) {
    list = list.filter(p => p.paymentDate >= filterDateFrom.value)
  }
  if (filterDateTo.value) {
    list = list.filter(p => p.paymentDate <= filterDateTo.value + 'T23:59:59')
  }
  return list
})

const totalExpense = computed(() =>
  filteredPayments.value.filter(p => p.status === 2 && !p.isDeleted).reduce((sum, p) => sum + (p.amount || 0), 0)
)

// Bütçe bölümü: servisin döndürdüğü (bütçesi VEYA gerçekleşeni olan) kategorilere ek olarak,
// henüz hiç bütçesi/harcaması olmayan kategoriler de "0" ile gösterilir — kullanıcı herhangi bir
// kategoriye ilk kez bütçe girebilsin diye.
const budgetRows = computed(() => {
  const byCategory = new Map(budgetStatus.value.map((b: any) => [b.categoryId, b]))
  return activeCategories.value.map((c: any) => {
    const existing = byCategory.get(c.id)
    return existing ?? { categoryId: c.id, categoryName: c.name, budgetAmount: 0, actualAmount: 0, percentUsed: 0, statusLevel: 'none' }
  }).sort((a: any, b: any) => b.actualAmount - a.actualAmount)
})

// Hiç bütçesi/harcaması olmayan kategoriler tam boyutlu kart yerine tek satırlık bir özet
// şeridinde toplanır — anlamsız "₺0,00 / ₺0,00" kalabalığını önlemek için.
const activeBudgetRows = computed(() => budgetRows.value.filter((r: any) => r.budgetAmount > 0 || r.actualAmount > 0))
const dormantBudgetRows = computed(() => budgetRows.value.filter((r: any) => r.budgetAmount === 0 && r.actualAmount === 0))
const showDormantCategories = ref(false)

const budgetSummary = computed(() => {
  const withBudget = budgetRows.value.filter((r: any) => r.budgetAmount > 0)
  const totalBudget = withBudget.reduce((s: number, r: any) => s + r.budgetAmount, 0)
  const totalActual = withBudget.reduce((s: number, r: any) => s + r.actualAmount, 0)
  const overCount = budgetRows.value.filter((r: any) => r.statusLevel === 'red').length
  const percentUsed = totalBudget > 0 ? (totalActual / totalBudget) * 100 : 0
  return { percentUsed, overCount }
})

// Kart açıldığında: seçili ay/yıl + kategoriye ait, silinmemiş ödemeler (gün/kişi/amaç/tutar için)
function budgetCategoryPayments(categoryId: string) {
  return payments.value
    .filter((p: any) =>
      p.categoryId === categoryId &&
      !p.isDeleted &&
      new Date(p.paymentDate).getFullYear() === budgetYear.value &&
      new Date(p.paymentDate).getMonth() + 1 === budgetMonth.value)
    .sort((a: any, b: any) => new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime())
}

function fmt(n: number): string {
  return new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n ?? 0)
}

function fmtDate(iso: string): string {
  if (!iso) return '-'
  return new Date(iso).toLocaleDateString('tr-TR')
}

function fmtDateTime(iso: string): string {
  if (!iso) return '-'
  return new Date(iso).toLocaleString('tr-TR')
}

function statusBadgeClass(status: number): string {
  if (status === 1) return 'status-badge--pending'
  if (status === 2) return 'status-badge--paid'
  return 'status-badge--cancelled'
}

// Load
async function loadAll() {
  loading.value = true
  error.value = ''
  try {
    // Owner/admin kullanıcılarda ModernLayout ofis listesini önceden yüklemez (bu sadece
    // admin-olmayan kullanıcılar için tetiklenir) — officeId'nin undefined kalıp yanlış/boş
    // sonuçlara (hatta backend'e geçersiz officeId gönderilmesine) yol açmaması için burada
    // da ModernPartyAccounts.vue'daki gibi açıkça garanti ediyoruz.
    if (!exchangeStore.offices?.length) {
      await exchangeStore.loadOffices()
    }
    const [defs, pays, curs] = await Promise.all([
      apiService.getExpenseDefinitions(officeId.value),
      apiService.getExpensePayments({ officeId: officeId.value }),
      apiService.getCurrencies(),
    ])
    definitions.value = Array.isArray(defs) ? defs : (defs?.items ?? [])
    payments.value = Array.isArray(pays) ? pays : (pays?.items ?? [])
    currencies.value = Array.isArray(curs) ? curs : (curs?.items ?? [])
    await Promise.all([loadCategories(), loadPendingApprovals(), loadBudgetStatus(), loadUpcomingDefinitions()])
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Veriler yüklenemedi'
  } finally {
    loading.value = false
  }
}

async function loadUpcomingDefinitions() {
  if (!officeId.value) return
  try {
    upcomingDefinitions.value = await apiService.getUpcomingExpenseDefinitions(officeId.value) ?? []
  } catch (e) {
    console.error('Yaklaşan ödemeler yüklenemedi:', e)
  }
}

async function toggleDefinitionStatement(d: any) {
  toggleDefRow(d.id)
  if (isDefExpanded(d.id) && !definitionStatements.value[d.id]) {
    statementLoading.value = d.id
    try {
      definitionStatements.value[d.id] = await apiService.getExpenseDefinitionStatement(d.id)
    } catch (e: any) {
      notification.error(e?.response?.data?.error || 'Ekstre yüklenemedi')
    } finally {
      statementLoading.value = null
    }
  }
}

const DUE_STATUS_CLASS: Record<string, string> = {
  'Gecikmiş': 'due-overdue',
  'Bu Hafta': 'due-week',
  'Yaklaşıyor': 'due-soon',
}

async function loadPendingApprovals() {
  if (!officeId.value) return
  try {
    pendingApprovals.value = await apiService.getExpensePendingApprovals(officeId.value) ?? []
  } catch (e) {
    console.error('Bekleyen onaylar yüklenemedi:', e)
  }
}

async function loadBudgetStatus() {
  if (!officeId.value) return
  try {
    budgetStatus.value = await apiService.getExpenseBudgetStatus(officeId.value, budgetYear.value, budgetMonth.value) ?? []
    budgetDrafts.value = {}
  } catch (e) {
    console.error('Bütçe durumu yüklenemedi:', e)
  }
}


onMounted(loadAll)

watch(() => exchangeStore.selectedOffice, () => {
  if (officeId.value) loadAll()
})

watch([budgetYear, budgetMonth], () => {
  if (officeId.value) loadBudgetStatus()
})

// Budget
function budgetDraft(categoryId: string, current: number): number {
  return budgetDrafts.value[categoryId] ?? current
}

async function saveBudget(row: any) {
  const amount = budgetDrafts.value[row.categoryId] ?? row.budgetAmount
  budgetSaving.value = row.categoryId
  try {
    await apiService.setExpenseBudget({
      officeId: officeId.value,
      categoryId: row.categoryId,
      year: budgetYear.value,
      month: budgetMonth.value,
      budgetAmount: amount,
    })
    notification.success('Bütçe kaydedildi')
    await loadBudgetStatus()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Bütçe kaydedilemedi')
  } finally {
    budgetSaving.value = null
  }
}

// Approval flow
async function approvePayment(p: any) {
  approvalActingId.value = p.id
  try {
    await apiService.approveExpensePayment(p.id, true)
    notification.success('Ödeme onaylandı, kasa güncellendi')
    await loadAll()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Onaylama başarısız')
  } finally {
    approvalActingId.value = null
  }
}

function openRejectApproval(p: any) {
  approvalRejectFor.value = p.id
  approvalRejectNote.value = ''
}

async function confirmRejectApproval(p: any) {
  approvalActingId.value = p.id
  try {
    await apiService.approveExpensePayment(p.id, false, approvalRejectNote.value)
    notification.success('Ödeme reddedildi')
    approvalRejectFor.value = null
    await loadAll()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || 'Reddetme başarısız')
  } finally {
    approvalActingId.value = null
  }
}

// Definition CRUD
function openCreateDef() {
  editDef.value = null
  defForm.value = {
    code: generateQuickDefCode(), name: '', categoryId: activeCategories.value[0]?.id ?? '',
    description: '', isActive: true, isRecurring: false,
    recurrencePeriod: null, defaultAmount: null, defaultCurrencyId: null,
    accountReference: '', dueDayOfMonth: null,
  }
  showDefModal.value = true
}

function openEditDef(d: any) {
  editDef.value = d
  defForm.value = {
    code: d.code || '', name: d.name, categoryId: d.categoryId ?? activeCategories.value[0]?.id ?? '',
    description: d.description || '', isActive: d.isActive !== false,
    isRecurring: d.isRecurring || false, recurrencePeriod: d.recurrencePeriod ?? null,
    defaultAmount: d.defaultAmount ?? null, defaultCurrencyId: d.defaultCurrencyId ?? null,
    accountReference: d.accountReference || '', dueDayOfMonth: d.dueDayOfMonth ?? null,
  }
  showDefModal.value = true
}

async function saveDef() {
  if (!defForm.value.name.trim()) {
    notification.warning('Gider kalemi adı boş olamaz')
    return
  }
  if (defForm.value.defaultAmount != null && defForm.value.defaultAmount < 0) {
    notification.warning('Varsayılan tutar negatif olamaz')
    return
  }
  if (defForm.value.isRecurring && !defForm.value.recurrencePeriod) {
    notification.warning('Tekrarlayan gider için tekrar periyodu seçiniz')
    return
  }
  saving.value = true
  try {
    const payload = { ...defForm.value, officeId: officeId.value }
    if (editDef.value) {
      await apiService.updateExpenseDefinition(editDef.value.id, payload)
    } else {
      await apiService.createExpenseDefinition(payload)
    }
    showDefModal.value = false
    await loadAll()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || e.message || 'Kayıt başarısız')
  } finally {
    saving.value = false
  }
}

async function deleteDef(d: any) {
  try {
    const res = await apiService.deleteExpenseDefinition(d.id)
    // Ödemesi olan bir tanım gerçekten silinmez, sadece pasife alınır — kullanıcı "silindi"
    // dediği halde listede "Pasif" olarak kalmasının kafa karıştırmaması için ayrı mesaj.
    if (res?.action === 'deactivated') {
      notification.success('Bu kalemin geçmiş ödeme kaydı olduğu için tamamen silinmedi, "Pasif" durumuna alındı')
    } else {
      notification.success('Tanım silindi')
    }
    defDeleteConfirmId.value = null
    await loadAll()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || e.message || 'Silme başarısız')
  }
}

// Payment CRUD
function openCreatePay(defId?: string) {
  payForm.value = {
    // Henüz hiç tanım yoksa kullanıcıyı doğrudan "yeni tür oluştur" moduna al —
    // ayrı bir "Gider Tanımları" adımına gitmeye zorlamadan tek modalda tamamlansın.
    expenseDefinitionId: defId || (definitions.value.length === 0 ? NEW_DEF_OPTION : ''),
    // Gider ödemelerinin neredeyse tamamı TRY cinsinden yapılıyor — para birimi listesinin
    // ham (alfabetik olmayan) sırasındaki ilk öğeye (ör. MGBP) değil, TRY'ye varsayılan yapılır.
    currencyId: (currencies.value.find((c: any) => (c.code || c.currencyCode) === 'TRY')?.id
      ?? currencies.value.find((c: any) => (c.code || c.currencyCode) === 'TRY')?.currencyId)
      || currencies.value[0]?.id || currencies.value[0]?.currencyId || '',
    paymentDate: new Date().toISOString().slice(0, 10),
    amount: null, paymentMethod: 1, referenceNumber: '', description: '',
  }
  quickDefName.value = ''
  quickDefCategoryId.value = activeCategories.value[0]?.id ?? ''
  showPayModal.value = true
}

// Bir tanım seçildiğinde varsayılan tutar/para birimi varsa formu önceden doldur.
watch(() => payForm.value.expenseDefinitionId, (defId) => {
  const def = definitions.value.find(d => d.id === defId)
  if (!def) return
  if (def.defaultAmount != null) payForm.value.amount = def.defaultAmount
  if (def.defaultCurrencyId) payForm.value.currencyId = def.defaultCurrencyId
})

async function savePay() {
  const isNewDef = payForm.value.expenseDefinitionId === NEW_DEF_OPTION
  if (isNewDef && !quickDefName.value.trim()) {
    notification.warning('Gider türü adı giriniz')
    return
  }
  if (!isNewDef && !payForm.value.expenseDefinitionId) {
    notification.warning('Gider türü seçiniz')
    return
  }
  if (!payForm.value.amount || payForm.value.amount <= 0) {
    notification.warning('Tutar sıfırdan büyük olmalıdır')
    return
  }
  saving.value = true
  try {
    let expenseDefinitionId = payForm.value.expenseDefinitionId
    if (isNewDef) {
      const newDef = await apiService.createExpenseDefinition({
        code: generateQuickDefCode(),
        name: quickDefName.value.trim(),
        categoryId: quickDefCategoryId.value,
        officeId: officeId.value,
        isActive: true,
        isRecurring: false,
      })
      expenseDefinitionId = newDef.id
    }

    const result = await apiService.createExpensePayment({ ...payForm.value, expenseDefinitionId })
    showPayModal.value = false
    if (result?.status === 1) {
      notification.success('Ödeme kaydedildi — tutar eşiği aştığından Owner onayı bekliyor')
    } else {
      notification.success('Ödeme kaydedildi')
    }
    await loadAll()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || e.message || 'Ödeme kaydı başarısız')
  } finally {
    saving.value = false
  }
}

async function deletePay(p: any) {
  try {
    await apiService.deleteExpensePayment(p.id, deleteReason.value)
    notification.success('Ödeme silindi')
    deleteConfirmId.value = null
    deleteReason.value = ''
    await loadAll()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || e.message || 'Silme başarısız')
  }
}

// CSV dışa aktarma — ModernZReport.vue'nun exportCsv() deseni (UTF-8 BOM + noktalı virgül)
function csvEscape(value: any): string {
  const str = String(value ?? '')
  return /[";\n]/.test(str) ? `"${str.replace(/"/g, '""')}"` : str
}

function exportCsv() {
  if (!filteredPayments.value.length) {
    notification.warning('Dışa aktarılacak ödeme bulunamadı')
    return
  }
  const headers = ['Tarih', 'Gider Tanımı', 'Kategori', 'Tutar', 'Para Birimi', 'Ödeme Yöntemi', 'Durum', 'Referans No', 'Açıklama', 'Oluşturan']
  const rows = filteredPayments.value.map((p: any) => [
    fmtDate(p.paymentDate),
    p.expenseDefinitionName ?? '-',
    p.categoryName || categoryMap.value[p.categoryId] || '-',
    fmt(p.amount),
    p.currencyCode ?? '-',
    p.paymentMethodName || PAYMENT_METHODS[p.paymentMethod] || '-',
    STATUS_LABELS[p.status] ?? '-',
    p.referenceNumber ?? '-',
    p.description ?? '-',
    p.createdByUserName ?? '-',
  ])
  const csvContent = [headers, ...rows].map(r => r.map(csvEscape).join(';')).join('\r\n')
  const blob = new Blob(['﻿' + csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `giderler_${new Date().toISOString().slice(0, 10)}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

const activeTab = ref<'payments' | 'definitions'>('payments')
</script>

<template>
  <div class="exp-wrap">
    <!-- Header -->
    <AppPageHeader icon="payments" title="Gider Yönetimi" subtitle="Gider tanımları, ödemeleri ve bütçenizi yönetin">
      <button class="exp-btn secondary" @click="exportCsv" title="CSV Olarak İndir">
        <span class="material-symbols-outlined" aria-hidden="true">download</span>CSV
      </button>
      <button v-if="!authStore.isViewerForOffice(officeId)" class="exp-btn secondary" @click="openCreateDef">
        <span class="material-symbols-outlined" aria-hidden="true">category</span>Yeni Tanım
      </button>
      <button v-if="!authStore.isViewerForOffice(officeId)" class="exp-btn primary" @click="openCreatePay()">
        <span class="material-symbols-outlined" aria-hidden="true">add_circle</span>Yeni Ödeme
      </button>
    </AppPageHeader>

    <!-- Bekleyen Onaylar -->
    <div v-if="pendingApprovals.length" class="panel approvals-panel">
      <div class="panel-hd approvals-hd">
        <span class="material-symbols-outlined" aria-hidden="true">fact_check</span>
        <h3>Onay Bekleyen Ödemeler</h3>
        <span class="approvals-count">{{ pendingApprovals.length }}</span>
      </div>
      <div class="approvals-list">
        <div v-for="p in pendingApprovals" :key="p.id" class="approval-card">
          <div class="approval-info">
            <div class="approval-main">
              <strong>{{ p.expenseDefinitionName }}</strong>
              <span class="approval-amount">{{ fmt(p.amount) }} {{ p.currencyCode }}</span>
            </div>
            <div class="approval-meta">
              {{ p.categoryName }} · {{ fmtDate(p.paymentDate) }} · Talep eden: {{ p.createdByUserName || '-' }}
            </div>
          </div>

          <template v-if="authStore.isOwner">
            <div v-if="approvalRejectFor === p.id" class="approval-reject-box">
              <input v-model="approvalRejectNote" class="filter-input" placeholder="Red nedeni (opsiyonel)" />
              <div class="approval-actions">
                <button class="exp-btn secondary" @click="approvalRejectFor = null">Vazgeç</button>
                <button class="exp-btn danger" :disabled="approvalActingId === p.id" @click="confirmRejectApproval(p)">Reddi Onayla</button>
              </div>
            </div>
            <div v-else class="approval-actions">
              <button class="exp-btn secondary" :disabled="approvalActingId === p.id" @click="openRejectApproval(p)">Reddet</button>
              <button class="exp-btn success" :disabled="approvalActingId === p.id" @click="approvePayment(p)">
                <span class="material-symbols-outlined" aria-hidden="true">check_circle</span>Onayla
              </button>
            </div>
          </template>
          <span v-else class="approval-waiting-badge">Owner onayı bekleniyor</span>
        </div>
      </div>
    </div>

    <!-- Yaklaşan Ödemeler (salt-okunur hatırlatma — hiçbir kayda dokunmaz) -->
    <div v-if="upcomingDefinitions.length" class="panel upcoming-panel">
      <div class="panel-hd">
        <span class="material-symbols-outlined" aria-hidden="true">event_upcoming</span>
        <h3>Yaklaşan Ödemeler</h3>
        <span class="upcoming-count">{{ upcomingDefinitions.length }}</span>
      </div>
      <div class="upcoming-list">
        <div v-for="d in upcomingDefinitions" :key="d.id" class="upcoming-card">
          <div class="upcoming-info">
            <div class="upcoming-main">
              <strong>{{ d.name }}</strong>
              <span class="due-badge" :class="DUE_STATUS_CLASS[d.dueStatus]">{{ d.dueStatus }}</span>
            </div>
            <div class="upcoming-meta">
              {{ d.categoryName }} · Vade: {{ fmtDate(d.nextDueDate) }}
              <span v-if="d.accountReference"> · Abone No: {{ d.accountReference }}</span>
            </div>
          </div>
          <button v-if="!authStore.isViewerForOffice(officeId)" class="exp-btn primary" @click="openCreatePay(d.id)">
            <span class="material-symbols-outlined" aria-hidden="true">payments</span>Öde
          </button>
        </div>
      </div>
    </div>

    <!-- KPIs -->
    <div class="kpi-grid">
      <AppKpiCard icon="payments" label="Toplam Gider" :value="'₺' + fmt(totalExpense)" color="var(--color-primary)" bg="var(--color-primary-light)" />
      <AppKpiCard icon="fact_check" label="Bekleyen Onay" :value="pendingApprovals.length" color="var(--color-warning)" bg="var(--color-warning-bg)" />
      <AppKpiCard icon="pie_chart" label="Bütçe Kullanımı" :value="'%' + fmt(budgetSummary.percentUsed)" color="#8b5cf6" bg="#f5f3ff" />
      <AppKpiCard icon="warning" label="Bütçe Aşan Kategori" :value="budgetSummary.overCount" color="var(--color-danger)" bg="var(--color-danger-bg)" />
    </div>

    <!-- Bütçe Durumu -->
    <div class="panel budget-panel">
      <div class="panel-hd">
        <span class="material-symbols-outlined" aria-hidden="true">account_balance_wallet</span>
        <h3>Bütçe Durumu</h3>
        <div class="budget-month-picker">
          <select v-model.number="budgetMonth" class="filter-input">
            <option v-for="(m, i) in months" :key="i" :value="i + 1">{{ m }}</option>
          </select>
          <select v-model.number="budgetYear" class="filter-input">
            <option v-for="y in [budgetYear - 1, budgetYear, budgetYear + 1]" :key="y" :value="y">{{ y }}</option>
          </select>
        </div>
      </div>
      <div class="budget-cards">
        <div
          v-for="row in (showDormantCategories ? budgetRows : activeBudgetRows)" :key="row.categoryId"
          class="budget-card" :class="['budget-card--' + row.statusLevel, { 'budget-card--open': isBudgetExpanded(row.categoryId) }]"
        >
          <div class="budget-card-head">
            <span class="budget-card-icon material-symbols-outlined" aria-hidden="true">category</span>
            <template v-if="editCategoryId === row.categoryId">
              <input
                class="budget-cat-input" v-model="editCategoryName" @click.stop
                @keyup.enter="saveRenameCategory(row)" @keyup.esc="editCategoryId = null"
              />
              <button class="icon-btn budget-card-action" @click.stop="saveRenameCategory(row)" title="Kaydet">
                <span class="material-symbols-outlined" aria-hidden="true">check</span>
              </button>
              <button class="icon-btn budget-card-action" @click.stop="editCategoryId = null" title="Vazgeç">
                <span class="material-symbols-outlined" aria-hidden="true">close</span>
              </button>
            </template>
            <template v-else>
              <span class="budget-cat">{{ row.categoryName }}</span>
              <button class="icon-btn budget-card-action" @click.stop="startRenameCategory(row)" title="Kategoriyi Düzenle" aria-label="Kategoriyi Düzenle">
                <span class="material-symbols-outlined" aria-hidden="true">edit</span>
              </button>
              <template v-if="categoryDeleteConfirmId === row.categoryId">
                <button class="icon-btn budget-card-action" @click.stop="categoryDeleteConfirmId = null" title="Vazgeç">
                  <span class="material-symbols-outlined" aria-hidden="true">close</span>
                </button>
                <button class="icon-btn danger budget-card-action" @click.stop="deleteCategory({ id: row.categoryId })" title="Onayla">
                  <span class="material-symbols-outlined" aria-hidden="true">check</span>
                </button>
              </template>
              <button
                v-else class="icon-btn danger budget-card-action" :disabled="categoryUsage(row.categoryId) > 0"
                :title="categoryUsage(row.categoryId) > 0 ? 'Kullanımda olduğu için silinemez' : 'Kategoriyi Sil'"
                aria-label="Kategoriyi Sil"
                @click.stop="categoryDeleteConfirmId = row.categoryId"
              >
                <span class="material-symbols-outlined" aria-hidden="true">delete</span>
              </button>
            </template>
            <button
              type="button" class="icon-btn budget-card-chevron-btn" @click="toggleBudgetCard(row.categoryId)"
              :aria-label="row.categoryName + ' bütçe detayını ' + (isBudgetExpanded(row.categoryId) ? 'kapat' : 'aç')"
            >
              <span class="material-symbols-outlined budget-card-chevron" :class="{ 'budget-card-chevron--open': isBudgetExpanded(row.categoryId) }" aria-hidden="true">expand_more</span>
            </button>
          </div>
          <button
            type="button" class="budget-card-top" @click="toggleBudgetCard(row.categoryId)"
            :aria-label="row.categoryName + ' bütçe detayını ' + (isBudgetExpanded(row.categoryId) ? 'kapat' : 'aç')"
          >
            <div class="budget-card-amounts">
              <span class="budget-actual">₺{{ fmt(row.actualAmount) }}</span>
              <span v-if="row.budgetAmount > 0" class="budget-sep">/ ₺{{ fmt(row.budgetAmount) }}</span>
              <span v-else class="budget-no-limit">Bütçe belirlenmedi</span>
              <span v-if="row.budgetAmount > 0" class="budget-percent" :class="'budget-percent--' + row.statusLevel">%{{ fmt(row.percentUsed) }}</span>
            </div>
            <div v-if="row.budgetAmount > 0" class="budget-bar-wrap">
              <div class="budget-bar" :class="'budget-bar--' + row.statusLevel" :style="{ width: Math.min(100, row.percentUsed) + '%' }"></div>
            </div>
            <div v-if="row.budgetAmount > 0" class="budget-remaining">
              Kalan: ₺{{ fmt(Math.max(0, row.budgetAmount - row.actualAmount)) }}
            </div>
          </button>

          <div v-if="isBudgetExpanded(row.categoryId)" class="budget-card-detail">
            <div class="budget-card-edit">
              <label class="budget-edit-label">Aylık bütçe</label>
              <input
                class="filter-input budget-input"
                type="number" step="100" min="0"
                :value="budgetDraft(row.categoryId, row.budgetAmount)"
                @input="budgetDrafts[row.categoryId] = ($event.target as HTMLInputElement).valueAsNumber || 0"
              />
              <button class="icon-btn" :disabled="budgetSaving === row.categoryId" @click="saveBudget(row)" title="Bütçeyi Kaydet">
                <span class="material-symbols-outlined" aria-hidden="true">save</span>
              </button>
            </div>
            <div v-if="budgetCategoryPayments(row.categoryId).length" class="budget-detail-list">
              <div v-for="p in budgetCategoryPayments(row.categoryId)" :key="p.id" class="budget-detail-item">
                <span class="budget-detail-date">{{ fmtDate(p.paymentDate) }}</span>
                <span class="budget-detail-desc">
                  <strong>{{ p.expenseDefinitionName || '-' }}</strong>
                  <span v-if="p.description"> — {{ p.description }}</span>
                </span>
                <span class="budget-detail-user">{{ p.createdByUserName || '-' }}</span>
                <span class="status-badge" :class="statusBadgeClass(p.status)">{{ STATUS_LABELS[p.status] ?? '-' }}</span>
                <span class="budget-detail-amount">{{ fmt(p.amount) }} {{ p.currencyCode }}</span>
              </div>
            </div>
            <div v-else class="budget-detail-empty">Bu kategoride {{ months[budgetMonth - 1] }} {{ budgetYear }} için kayıtlı harcama yok.</div>
          </div>
        </div>
      </div>
      <button
        v-if="dormantBudgetRows.length" type="button" class="dormant-toggle"
        @click="showDormantCategories = !showDormantCategories"
      >
        <span class="material-symbols-outlined" aria-hidden="true">{{ showDormantCategories ? 'expand_less' : 'expand_more' }}</span>
        {{ showDormantCategories ? 'Kullanılmayan kategorileri gizle' : `Kullanılmayan ${dormantBudgetRows.length} kategoriyi göster` }}
      </button>
    </div>

    <!-- Gider Kayıtları: filtre + tab + liste/tablo tek panelde -->
    <div class="panel records-panel">
      <div class="panel-hd records-hd">
        <span class="material-symbols-outlined" aria-hidden="true">receipt_long</span>
        <h3>Gider Kayıtları</h3>
        <div class="tab-bar">
          <button class="tab-btn" :class="{ active: activeTab === 'payments' }" @click="activeTab = 'payments'">
            <span class="material-symbols-outlined" aria-hidden="true">receipt_long</span>Ödemeler
          </button>
          <button class="tab-btn" :class="{ active: activeTab === 'definitions' }" @click="activeTab = 'definitions'">
            <span class="material-symbols-outlined" aria-hidden="true">category</span>Gider Kalemleri
          </button>
        </div>
      </div>

      <div class="panel-body">
        <!-- Filters -->
        <div class="filter-bar">
          <div class="filter-group">
            <label>
              Kategori
              <button class="cat-manage-link" type="button" @click="openCategoryModal" title="Kategorileri Yönet" aria-label="Kategorileri Yönet">
                <span class="material-symbols-outlined" aria-hidden="true">settings</span>
              </button>
            </label>
            <select v-model="filterCategory" class="filter-input">
              <option value="">Tümü</option>
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
          <div class="filter-group">
            <label>Başlangıç</label>
            <input type="date" v-model="filterDateFrom" class="filter-input" />
          </div>
          <div class="filter-group">
            <label>Bitiş</label>
            <input type="date" v-model="filterDateTo" class="filter-input" />
          </div>
        </div>

        <!-- Loading / Error -->
        <div v-if="loading" class="exp-loading"><div class="spinner"></div>Yükleniyor...</div>
        <div v-else-if="error" class="exp-error">
          <span class="material-symbols-outlined" aria-hidden="true">error</span>{{ error }}
          <button class="exp-btn secondary" @click="loadAll" style="margin-left:12px">Tekrar Dene</button>
        </div>

        <!-- Payments: açılır liste -->
        <div v-else-if="activeTab === 'payments'" class="table-wrap">
          <div v-if="filteredPayments.length" class="pay-list">
        <div v-for="p in filteredPayments" :key="p.id" class="pay-item-wrap">
          <div class="pay-item" :class="{ deleted: p.isDeleted, open: isPaymentExpanded(p.id) }" @click="togglePaymentRow(p.id)">
            <span class="material-symbols-outlined pay-chevron" :class="{ 'pay-chevron--open': isPaymentExpanded(p.id) }" aria-hidden="true">chevron_right</span>
            <span class="pay-date">{{ fmtDate(p.paymentDate) }}</span>
            <span class="fw600 pay-name">{{ p.expenseDefinitionName || '-' }}</span>
            <span class="cat-badge">{{ p.categoryName || categoryMap[p.categoryId] || '-' }}</span>
            <span class="status-badge" :class="statusBadgeClass(p.status)">{{ STATUS_LABELS[p.status] ?? '-' }}</span>
            <span class="pay-amount">{{ fmt(p.amount) }} {{ p.currencyCode }}</span>
            <span v-if="p.isDeleted" class="deleted-tag">İptal</span>
          </div>

          <div v-if="isPaymentExpanded(p.id)" class="pay-detail">
            <div class="pay-detail-grid">
              <div class="pay-detail-item"><span class="pay-detail-label">Ödeme Yöntemi</span><span>{{ p.paymentMethodName || PAYMENT_METHODS[p.paymentMethod] || '-' }}</span></div>
              <div class="pay-detail-item"><span class="pay-detail-label">Fatura No</span><span>{{ p.referenceNumber || '-' }}</span></div>
              <div class="pay-detail-item"><span class="pay-detail-label">Oluşturan</span><span>{{ p.createdByUserName || '-' }}</span></div>
              <div class="pay-detail-item" v-if="p.approvedByUserName"><span class="pay-detail-label">Onaylayan/Reddeden</span><span>{{ p.approvedByUserName }} — {{ fmtDateTime(p.approvedAt) }}</span></div>
              <div class="pay-detail-item pay-detail-full" v-if="p.description"><span class="pay-detail-label">Açıklama</span><span>{{ p.description }}</span></div>
              <div class="pay-detail-item pay-detail-full" v-if="p.rejectionNote"><span class="pay-detail-label">Red Notu</span><span>{{ p.rejectionNote }}</span></div>
            </div>

            <div v-if="p.isDeleted" class="pay-deleted-banner">
              <span class="material-symbols-outlined" aria-hidden="true" style="font-size:16px">delete</span>
              Silinmiş — {{ p.deletedByUserName || '' }} {{ p.deletedReason ? `(${p.deletedReason})` : '' }}
            </div>

            <div v-if="!p.isDeleted && authStore.isAdmin" class="pay-detail-footer">
              <template v-if="deleteConfirmId === p.id">
                <input v-model="deleteReason" class="filter-input" placeholder="Silme nedeni (opsiyonel)" @click.stop />
                <div class="approval-actions">
                  <button class="exp-btn secondary" @click.stop="deleteConfirmId = null">İptal</button>
                  <button class="exp-btn danger" @click.stop="deletePay(p)">Evet, Sil</button>
                </div>
              </template>
              <button v-else class="exp-btn danger" @click.stop="deleteConfirmId = p.id; deleteReason = ''">
                <span class="material-symbols-outlined" aria-hidden="true">delete</span>İşlemi Sil
              </button>
            </div>
          </div>
        </div>
      </div>
      <AppEmptyState v-else icon="receipt_long" message="Henüz gider ödemesi kaydedilmemiş. Kira, fatura, maaş gibi bir gideri kaydetmek için başlayın — gider türünü de aynı adımda oluşturabilirsiniz.">
        <button v-if="!authStore.isViewerForOffice(officeId)" class="exp-btn primary" @click="openCreatePay()">İlk Gideri Ekle</button>
      </AppEmptyState>
    </div>

    <!-- Definitions Table -->
    <div v-else-if="activeTab === 'definitions'" class="table-wrap">
      <table class="exp-table" v-if="definitions.length">
        <thead>
          <tr>
            <th>Kod</th>
            <th>Ad</th>
            <th>Kategori</th>
            <th>Varsayılan Tutar</th>
            <th>Tekrarlayan</th>
            <th>Durum</th>
            <th>Toplam Ödeme</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="d in definitions" :key="d.id">
            <tr class="clickable-row" @click="toggleDefinitionStatement(d)">
              <td class="mono">
                <span class="material-symbols-outlined def-chevron" :class="{ 'def-chevron--open': isDefExpanded(d.id) }" aria-hidden="true">chevron_right</span>
                {{ d.code }}
              </td>
              <td class="fw600">{{ d.name }}</td>
              <td><span class="cat-badge">{{ d.categoryName || categoryMap[d.categoryId] || '-' }}</span></td>
              <td>{{ d.defaultAmount ? fmt(d.defaultAmount) : '-' }}</td>
              <td>
                <span v-if="d.isRecurring" class="recurring-badge">{{ RECURRENCE_PERIODS[d.recurrencePeriod] || 'Evet' }}</span>
                <span v-else class="muted">Hayır</span>
              </td>
              <td>
                <span class="status-dot" :class="d.isActive ? 'active' : 'inactive'"></span>
                {{ d.isActive ? 'Aktif' : 'Pasif' }}
              </td>
              <td>{{ d.paymentCount ?? 0 }} adet / ₺{{ fmt(d.totalPayments ?? 0) }}</td>
              <td class="action-cell" @click.stop>
                <template v-if="defDeleteConfirmId === d.id">
                  <button class="icon-btn" @click="defDeleteConfirmId = null" title="Vazgeç">
                    <span class="material-symbols-outlined" aria-hidden="true">close</span>
                  </button>
                  <button class="icon-btn danger" @click="deleteDef(d)" title="Onayla">
                    <span class="material-symbols-outlined" aria-hidden="true">check</span>
                  </button>
                </template>
                <template v-else>
                  <button v-if="!authStore.isViewerForOffice(officeId)" class="icon-btn" @click="openCreatePay(d.id)" title="Ödeme Ekle">
                    <span class="material-symbols-outlined" aria-hidden="true">add_circle</span>
                  </button>
                  <button v-if="!authStore.isViewerForOffice(officeId)" class="icon-btn" @click="openEditDef(d)" title="Düzenle">
                    <span class="material-symbols-outlined" aria-hidden="true">edit</span>
                  </button>
                  <button v-if="authStore.isAdmin" class="icon-btn danger" @click="defDeleteConfirmId = d.id" title="Sil">
                    <span class="material-symbols-outlined" aria-hidden="true">delete</span>
                  </button>
                </template>
              </td>
            </tr>
            <tr v-if="isDefExpanded(d.id)" class="def-statement-row">
              <td colspan="8">
                <div v-if="statementLoading === d.id" class="def-statement-loading">Ekstre yükleniyor...</div>
                <div v-else-if="definitionStatements[d.id]" class="def-statement">
                  <div class="def-statement-summary">
                    <div class="def-statement-item">
                      <span class="pay-detail-label">Abone/Sözleşme No</span>
                      <span>{{ definitionStatements[d.id].accountReference || '-' }}</span>
                    </div>
                    <div class="def-statement-item" v-if="definitionStatements[d.id].nextDueDate">
                      <span class="pay-detail-label">Sıradaki Vade</span>
                      <span>
                        {{ fmtDate(definitionStatements[d.id].nextDueDate) }}
                        <span v-if="definitionStatements[d.id].dueStatus" class="due-badge" :class="DUE_STATUS_CLASS[definitionStatements[d.id].dueStatus]">{{ definitionStatements[d.id].dueStatus }}</span>
                      </span>
                    </div>
                    <div class="def-statement-item">
                      <span class="pay-detail-label">Toplam Ödenen</span>
                      <span>₺{{ fmt(definitionStatements[d.id].totalPaid) }} ({{ definitionStatements[d.id].paymentCount }} ödeme)</span>
                    </div>
                    <div class="def-statement-item">
                      <span class="pay-detail-label">Ortalama Tutar</span>
                      <span>₺{{ fmt(definitionStatements[d.id].averageAmount) }}</span>
                    </div>
                  </div>
                  <table class="exp-table" v-if="definitionStatements[d.id].lines?.length">
                    <thead>
                      <tr>
                        <th>Tarih</th>
                        <th>Fatura No</th>
                        <th class="text-right">Tutar</th>
                        <th class="text-right">Kümülatif</th>
                        <th>Durum</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="line in definitionStatements[d.id].lines" :key="line.paymentId">
                        <td>{{ fmtDate(line.paymentDate) }}</td>
                        <td class="mono">{{ line.referenceNumber || '-' }}</td>
                        <td class="text-right">{{ fmt(line.amount) }} {{ line.currencyCode }}</td>
                        <td class="text-right">{{ fmt(line.runningTotal) }}</td>
                        <td>{{ line.statusName }}</td>
                      </tr>
                    </tbody>
                  </table>
                  <div v-else class="def-statement-empty">Bu kalem için henüz ödeme kaydı yok.</div>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
      <AppEmptyState v-else icon="category" message="Henüz gider türü tanımlanmamış. Gider türleri (Kira, Maaş, Fatura vb.) tekrarlayan giderlerde varsayılan tutar/para birimi hatırlar — yeni bir gider eklerken de otomatik oluşturulabilir, ayrıca buradan da yönetilebilir.">
        <button v-if="!authStore.isViewerForOffice(officeId)" class="exp-btn primary" @click="openCreateDef">İlk Gider Türünü Ekle</button>
      </AppEmptyState>
        </div>
      </div>
    </div>

    <!-- Definition Modal -->
    <div v-if="showDefModal" class="modal-overlay" @click.self="showDefModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>{{ editDef ? 'Tanım Düzenle' : 'Yeni Gider Tanımı' }}</h2>
          <button class="icon-btn" @click="showDefModal = false"><span class="material-symbols-outlined" aria-hidden="true">close</span></button>
        </div>
        <div class="modal-body">
          <div class="form-row">
            <div class="form-group">
              <label>Kod</label>
              <input v-model="defForm.code" class="form-input" placeholder="GDR-001" />
            </div>
            <div class="form-group">
              <label>Ad *</label>
              <input v-model="defForm.name" class="form-input" placeholder="Gider adı" />
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>
                Kategori
                <button class="cat-manage-link" type="button" @click="openCategoryModal" title="Kategorileri Yönet" aria-label="Kategorileri Yönet">
                  <span class="material-symbols-outlined" aria-hidden="true">settings</span>
                </button>
              </label>
              <select v-model="defForm.categoryId" class="form-input">
                <option v-for="c in activeCategories" :key="c.id" :value="c.id">{{ c.name }}</option>
              </select>
            </div>
            <div class="form-group">
              <label>Varsayılan Tutar</label>
              <input v-model.number="defForm.defaultAmount" type="number" step="0.01" class="form-input" placeholder="0.00" />
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Varsayılan Para Birimi</label>
              <select v-model="defForm.defaultCurrencyId" class="form-input">
                <option :value="null">Seçiniz</option>
                <option v-for="c in currencies" :key="c.id || c.currencyId" :value="c.id || c.currencyId">{{ c.code || c.currencyCode }}</option>
              </select>
            </div>
            <div class="form-group" v-if="defForm.isRecurring">
              <label>Tekrar Periyodu</label>
              <select v-model.number="defForm.recurrencePeriod" class="form-input">
                <option :value="null">Seçiniz</option>
                <option v-for="(label, key) in RECURRENCE_PERIODS" :key="key" :value="Number(key)">{{ label }}</option>
              </select>
            </div>
          </div>
          <div class="form-row" v-if="defForm.isRecurring">
            <div class="form-group">
              <label>Abone/Sözleşme No</label>
              <input v-model="defForm.accountReference" class="form-input" placeholder="Örn: elektrik abone no" />
            </div>
            <div class="form-group" v-if="defForm.recurrencePeriod === 3">
              <label>Vade Günü (Ayın Kaçı)</label>
              <input v-model.number="defForm.dueDayOfMonth" type="number" min="1" max="31" class="form-input" placeholder="Örn: 5" />
            </div>
          </div>
          <div class="form-group">
            <label>Açıklama</label>
            <textarea v-model="defForm.description" class="form-input" rows="2" placeholder="İsteğe bağlı"></textarea>
          </div>
          <div class="form-row">
            <label class="checkbox-label">
              <input type="checkbox" v-model="defForm.isActive" /> Aktif
            </label>
            <label class="checkbox-label">
              <input type="checkbox" v-model="defForm.isRecurring" /> Tekrarlayan
            </label>
          </div>
        </div>
        <div class="modal-footer">
          <button class="exp-btn secondary" @click="showDefModal = false">İptal</button>
          <button class="exp-btn primary" @click="saveDef" :disabled="saving">
            {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Kategori Yönetimi Modal -->
    <div v-if="showCategoryModal" class="modal-overlay" @click.self="showCategoryModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Gider Kategorilerini Yönet</h2>
          <button class="icon-btn" @click="showCategoryModal = false"><span class="material-symbols-outlined" aria-hidden="true">close</span></button>
        </div>
        <div class="modal-body">
          <div class="form-row">
            <div class="form-group" style="flex:2">
              <input v-model="newCategoryName" class="form-input" placeholder="Yeni kategori adı (örn. Bakım Sözleşmeleri)" @keyup.enter="addCategory" />
            </div>
            <button class="exp-btn primary" :disabled="categorySaving || !newCategoryName.trim()" @click="addCategory">
              <span class="material-symbols-outlined" aria-hidden="true">add</span>Ekle
            </button>
          </div>
          <div class="category-list">
            <div v-for="c in categories" :key="c.id" class="category-row">
              <span class="category-name" :class="{ 'category-name--inactive': !c.isActive }">{{ c.name }}</span>
              <span v-if="c.usageCount" class="category-usage">{{ c.usageCount }} kayıtta kullanılıyor</span>
              <div class="category-actions">
                <button class="icon-btn" @click="toggleCategoryActive(c)" :title="c.isActive ? 'Pasifleştir' : 'Aktifleştir'">
                  <span class="material-symbols-outlined" aria-hidden="true">{{ c.isActive ? 'visibility_off' : 'visibility' }}</span>
                </button>
                <template v-if="categoryDeleteConfirmId === c.id">
                  <button class="icon-btn" @click="categoryDeleteConfirmId = null" title="Vazgeç"><span class="material-symbols-outlined" aria-hidden="true">close</span></button>
                  <button class="icon-btn danger" @click="deleteCategory(c)" title="Onayla"><span class="material-symbols-outlined" aria-hidden="true">check</span></button>
                </template>
                <button v-else class="icon-btn danger" :disabled="c.usageCount > 0" :title="c.usageCount > 0 ? 'Kullanımda olduğu için silinemez' : 'Sil'" @click="categoryDeleteConfirmId = c.id">
                  <span class="material-symbols-outlined" aria-hidden="true">delete</span>
                </button>
              </div>
            </div>
            <div v-if="!categories.length" class="category-empty">Henüz kategori yok.</div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="exp-btn secondary" @click="showCategoryModal = false">Kapat</button>
        </div>
      </div>
    </div>

    <!-- Payment Modal -->
    <div v-if="showPayModal" class="modal-overlay" @click.self="showPayModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Yeni Gider Ödemesi</h2>
          <button class="icon-btn" @click="showPayModal = false"><span class="material-symbols-outlined" aria-hidden="true">close</span></button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Gider Türü *</label>
            <select v-model="payForm.expenseDefinitionId" class="form-input">
              <option value="" disabled>Seçiniz</option>
              <option v-for="d in definitions.filter(x => x.isActive !== false)" :key="d.id" :value="d.id">{{ d.name }}</option>
              <option :value="NEW_DEF_OPTION">+ Yeni Gider Türü Ekle...</option>
            </select>
          </div>
          <div v-if="payForm.expenseDefinitionId === NEW_DEF_OPTION" class="quick-def-box">
            <div class="form-row">
              <div class="form-group">
                <label>Yeni Gider Türü Adı *</label>
                <input v-model="quickDefName" class="form-input" placeholder="Örn: Ofis Kirası" />
              </div>
              <div class="form-group">
                <label>Kategori</label>
                <select v-model="quickDefCategoryId" class="form-input">
                  <option v-for="c in activeCategories" :key="c.id" :value="c.id">{{ c.name }}</option>
                </select>
              </div>
            </div>
            <p class="quick-def-hint">Bu gider türü kaydedilecek ve ileride tekrar seçilebilecek.</p>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Tutar *</label>
              <input v-model.number="payForm.amount" type="number" step="0.01" min="0.01" class="form-input" placeholder="0.00" />
            </div>
            <div class="form-group">
              <label>Para Birimi</label>
              <select v-model="payForm.currencyId" class="form-input">
                <option v-for="c in currencies" :key="c.id || c.currencyId" :value="c.id || c.currencyId">{{ c.code || c.currencyCode }}</option>
              </select>
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Tarih *</label>
              <input v-model="payForm.paymentDate" type="date" class="form-input" />
            </div>
            <div class="form-group">
              <label>Ödeme Yöntemi</label>
              <select v-model="payForm.paymentMethod" class="form-input">
                <option v-for="(label, key) in PAYMENT_METHODS" :key="key" :value="Number(key)">{{ label }}</option>
              </select>
            </div>
          </div>
          <div class="form-group">
            <label>Referans No</label>
            <input v-model="payForm.referenceNumber" class="form-input" placeholder="İsteğe bağlı" />
          </div>
          <div class="form-group">
            <label>Açıklama</label>
            <textarea v-model="payForm.description" class="form-input" rows="2" placeholder="İsteğe bağlı"></textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button class="exp-btn secondary" @click="showPayModal = false">İptal</button>
          <button class="exp-btn primary" @click="savePay" :disabled="saving">
            {{ saving ? 'Kaydediliyor...' : 'Ödemeyi Kaydet' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.exp-wrap { max-width: 1200px; margin: 0 auto; }

.exp-actions { display: flex; gap: 8px; }

/* Buttons */
.exp-btn {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 16px; border: none; border-radius: var(--radius-md);
  font-size: 13px; font-weight: 600; cursor: pointer; transition: background-color 0.2s, color 0.2s;
}
.exp-btn .material-symbols-outlined { font-size: 18px; }
.exp-btn.primary { background: var(--color-primary); color: #fff; }
.exp-btn.primary:hover { background: var(--color-primary-hover); }
.exp-btn.primary:disabled { opacity: .6; cursor: not-allowed; }
.exp-btn.secondary { background: #f3f4f6; color: var(--color-text); }
.exp-btn.secondary:hover { background: #e5e7eb; }
.exp-btn.success { background: var(--color-success); color: #fff; }
.exp-btn.success:hover { opacity: .9; }
.exp-btn.danger { background: var(--color-danger); color: #fff; }
.exp-btn.danger:hover { opacity: .9; }
.exp-btn:disabled { opacity: .6; cursor: not-allowed; }
.exp-btn.primary:not(:disabled) { box-shadow: var(--shadow-glow-primary); }

/* Kart/modül reçetesi — ModernZReport.vue ile birebir aynı */
.panel {
  background: var(--color-bg-card); border: 1px solid var(--color-border);
  border-radius: var(--radius-lg); overflow: hidden; box-shadow: var(--shadow-bold);
  margin-bottom: 16px;
}
.panel-hd {
  display: flex; align-items: center; gap: 8px; padding: 14px 18px;
  border-bottom: 1px solid #f3f4f6; border-left: 3px solid var(--color-primary);
}
.panel-hd h3 { margin: 0; font-size: 14px; font-weight: 600; color: var(--color-text); flex: 1; }
.panel-hd .material-symbols-outlined { font-size: 19px; color: var(--color-primary); }
.panel-body { padding: 16px 18px; }

/* Bekleyen Onaylar */
.approvals-panel .panel-hd { border-left-color: var(--color-warning); }
.approvals-panel .panel-hd .material-symbols-outlined {
  color: var(--color-warning); font-variation-settings: 'FILL' 1, 'wght' 500, 'GRAD' 0, 'opsz' 24;
}
.approvals-count {
  font-size: 11px; font-weight: 700; background: var(--color-warning); color: #fff;
  padding: 1px 8px; border-radius: var(--radius-sm);
}
.approvals-list { display: flex; flex-direction: column; gap: 10px; padding: 0 18px 16px; }
.approval-card {
  display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap;
  background: var(--color-warning-bg); border: 1px solid var(--color-warning-bg); border-radius: var(--radius-md); padding: 10px 14px;
  transition: box-shadow .2s, transform .2s;
}
.approval-card:hover { box-shadow: var(--shadow-md); transform: translateY(-1px); }
.approval-main { display: flex; align-items: center; gap: 8px; font-size: 13px; }
.approval-amount { font-weight: 700; color: var(--color-text); font-variant-numeric: tabular-nums; }
.approval-meta { font-size: 12px; color: var(--color-text-secondary); margin-top: 2px; }
.approval-actions { display: flex; gap: 8px; }
.approval-reject-box { display: flex; align-items: center; gap: 8px; }
.approval-waiting-badge { font-size: 12px; font-weight: 600; color: var(--color-warning); }

/* Yaklaşan Ödemeler */
.upcoming-count {
  font-size: 11px; font-weight: 700; background: var(--color-primary); color: #fff;
  padding: 1px 8px; border-radius: var(--radius-sm);
}
.upcoming-list { display: flex; flex-direction: column; gap: 10px; padding: 0 18px 16px; }
.upcoming-card {
  display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap;
  background: #f9fafb; border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: 10px 14px;
  transition: box-shadow .2s, transform .2s;
}
.upcoming-card:hover { box-shadow: var(--shadow-md); transform: translateY(-1px); }
.panel.upcoming-panel .panel-hd .material-symbols-outlined { font-variation-settings: 'FILL' 1, 'wght' 500, 'GRAD' 0, 'opsz' 24; }
.upcoming-main { display: flex; align-items: center; gap: 8px; font-size: 13px; }
.upcoming-meta { font-size: 12px; color: var(--color-text-secondary); margin-top: 2px; }

/* Vade durum rozetleri */
.due-badge {
  display: inline-block; padding: 2px 8px; border-radius: var(--radius-sm);
  font-size: 11px; font-weight: 700; margin-left: 6px;
}
.due-overdue { background: var(--color-danger-bg); color: var(--color-danger); }
.due-week { background: var(--color-warning-bg); color: var(--color-warning); }
.due-soon { background: #eff6ff; color: #1d4ed8; }

/* KPI */
.kpi-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin-bottom: 16px; }

/* Bütçe — AppKpiCard diliyle ızgara (grid) kartlar */
.panel.budget-panel .panel-hd { border-left-color: var(--color-primary); }
.budget-month-picker { display: flex; gap: 8px; }
.budget-cards {
  display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 14px; padding: 14px 18px 16px; align-items: start;
}
.budget-card {
  border: 2px solid #e4e7f0; border-top-width: 6px; border-top-color: #e4e7f0;
  border-radius: var(--radius-lg); background: var(--color-bg-card);
  box-shadow: var(--shadow-sm);
  transition: border-color .2s, box-shadow .2s, transform .2s, grid-column .15s;
}
.budget-card:hover { border-color: #d4d8e8; box-shadow: 0 8px 22px -6px rgba(0,0,0,0.14); transform: translateY(-3px); }
.budget-card--open:hover { transform: none; }
.budget-card--green { border-top-color: var(--color-success); background: linear-gradient(135deg, var(--color-success-bg) 0%, #ffffff 60%); }
.budget-card--amber { border-top-color: var(--color-warning); background: linear-gradient(135deg, var(--color-warning-bg) 0%, #ffffff 60%); }
.budget-card--red { border-top-color: var(--color-danger); background: linear-gradient(135deg, var(--color-danger-bg) 0%, #ffffff 60%); }
.budget-card--none { border-top-color: #e4e7f0; }
.budget-card--open { grid-column: 1 / -1; }

.budget-card-top {
  padding: 8px 20px 16px; cursor: pointer; display: flex; flex-direction: column; gap: 12px;
  width: 100%; background: none; border: none; text-align: left; font: inherit; color: inherit;
}
.budget-card-head { display: flex; align-items: center; gap: 8px; padding: 14px 20px 0; }
.budget-card-icon {
  width: 26px; height: 26px; border-radius: var(--radius-sm); flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  font-size: 16px; color: var(--color-text-muted); background: var(--color-bg-page);
  box-shadow: 0 2px 6px -2px rgba(0,0,0,0.18);
}
.budget-card--green .budget-card-icon { color: var(--color-success); background: var(--color-success-bg); }
.budget-card--amber .budget-card-icon { color: var(--color-warning); background: var(--color-warning-bg); }
.budget-card--red .budget-card-icon { color: var(--color-danger); background: var(--color-danger-bg); }
.budget-cat { font-size: 14px; color: var(--color-text); font-weight: 600; flex: 1; }
.budget-cat-input {
  flex: 1; min-width: 0; font-size: 14px; font-weight: 600; color: var(--color-text);
  padding: 3px 6px; border: 1px solid var(--color-primary); border-radius: var(--radius-sm);
}
.budget-card-action {
  width: 24px; height: 24px; padding: 0; flex-shrink: 0; color: var(--color-text-muted);
}
.budget-card-action:hover:not(:disabled):not(.danger) { color: var(--color-primary); background: var(--color-primary-light); }
.budget-card-action .material-symbols-outlined { font-size: 16px; }
.budget-card-chevron-btn {
  width: 24px; height: 24px; padding: 0; flex-shrink: 0; margin-left: auto;
  background: none; border: none; display: flex; align-items: center; justify-content: center; cursor: pointer;
}
.budget-card-chevron { font-size: 20px; color: var(--color-text-muted); transition: transform .15s; }
.budget-card-chevron--open { transform: rotate(180deg); color: var(--color-primary); }
.budget-card-amounts { display: flex; align-items: baseline; gap: 6px; flex-wrap: wrap; }
.budget-actual { font-size: 24px; font-weight: 700; color: var(--color-text); font-variant-numeric: tabular-nums; }
.budget-percent { margin-left: auto; font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: var(--radius-sm); }
.budget-percent--green { background: color-mix(in srgb, var(--color-success) 12%, transparent); color: var(--color-success); }
.budget-percent--amber { background: color-mix(in srgb, var(--color-warning) 12%, transparent); color: var(--color-warning); }
.budget-percent--red { background: color-mix(in srgb, var(--color-danger) 12%, transparent); color: var(--color-danger); }
.budget-percent--none { background: color-mix(in srgb, var(--color-text-secondary) 12%, transparent); color: var(--color-text-secondary); }
.budget-remaining { font-size: 12px; color: var(--color-text-secondary); }
.budget-sep { color: var(--color-text-muted); font-size: 12px; font-variant-numeric: tabular-nums; }
.budget-no-limit { color: var(--color-text-muted); font-size: 11px; font-style: italic; }
.dormant-toggle {
  display: flex; align-items: center; gap: 6px; margin: 12px 16px 4px;
  padding: 8px 12px; background: none; border: 1px dashed var(--color-border); border-radius: var(--radius-md);
  color: var(--color-text-secondary); font-size: 12px; font-weight: 600; cursor: pointer;
}
.dormant-toggle:hover { background: var(--color-bg-hover, rgba(0,0,0,.03)); }
.dormant-toggle .material-symbols-outlined { font-size: 16px; }
.budget-bar-wrap { height: 8px; background: var(--color-bg-page); border-radius: var(--radius-sm); overflow: hidden; }
.budget-bar { height: 100%; border-radius: var(--radius-sm); transition: width .3s; }
.budget-bar--green { background: linear-gradient(90deg, var(--color-success) 0%, color-mix(in srgb, var(--color-success) 75%, white) 100%); }
.budget-bar--amber { background: linear-gradient(90deg, var(--color-warning) 0%, color-mix(in srgb, var(--color-warning) 75%, white) 100%); }
.budget-bar--red { background: linear-gradient(90deg, var(--color-danger) 0%, color-mix(in srgb, var(--color-danger) 75%, white) 100%); }
.budget-bar--none { background: #d1d5db; }

.budget-input { width: 100%; min-width: 0; text-align: right; flex: 1; }

/* Bütçe kartı — açılır harcama detayı (gün/kişi/amaç/tutar) */
.budget-card-detail { padding: 12px 16px 14px; background: #faf5ff; border-top: 1px solid var(--color-border); }
.budget-card-edit { display: flex; gap: 8px; align-items: center; margin-bottom: 12px; }
.budget-edit-label { font-size: 11px; font-weight: 600; color: var(--color-text-secondary); white-space: nowrap; }
.budget-detail-list { display: flex; flex-direction: column; gap: 6px; }
.budget-detail-item { display: flex; align-items: center; gap: 10px; font-size: 12px; flex-wrap: wrap; }
.budget-detail-date { color: var(--color-text-secondary); min-width: 70px; }
.budget-detail-desc { flex: 1; min-width: 160px; color: var(--color-text); }
.budget-detail-user { color: var(--color-text-secondary); }
.budget-detail-amount { font-weight: 700; color: var(--color-text); margin-left: auto; font-variant-numeric: tabular-nums; }
.budget-detail-empty { color: var(--color-text-muted); font-size: 12px; padding: 10px 0; }

/* Gider Kayıtları paneli */
.records-panel .panel-hd { flex-wrap: wrap; row-gap: 8px; }
.records-hd h3 { flex: none; margin-right: 8px; }

/* Filters */
.filter-bar { display: flex; gap: 12px; margin-bottom: 12px; flex-wrap: wrap; }
.filter-group { display: flex; flex-direction: column; gap: 4px; }
.filter-group label { font-size: 11px; color: var(--color-text-secondary); font-weight: 600; }
.filter-input {
  padding: 7px 10px; border: 1px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 13px; color: var(--color-text); background: var(--color-bg-card); min-width: 140px;
}
.filter-input:focus { border-color: var(--color-primary); outline: none; box-shadow: 0 0 0 2px rgba(99,102,241,.15); }
.filter-input:focus-visible { outline: 2px solid var(--color-primary); outline-offset: 1px; }

/* Tabs (panel-hd içinde) */
.tab-bar { display: flex; gap: 4px; }
.tab-btn {
  display: flex; align-items: center; gap: 6px;
  padding: 6px 12px; border: none; background: transparent; border-radius: var(--radius-md);
  font-size: 13px; font-weight: 500; color: var(--color-text-secondary);
  cursor: pointer; transition: background-color 0.2s, color 0.2s;
}
.tab-btn .material-symbols-outlined { font-size: 18px; }
.tab-btn.active { color: var(--color-primary); background: var(--color-primary-light); font-weight: 600; }
.tab-btn:hover:not(.active) { color: var(--color-text); background: #f3f4f6; }
.tab-btn:focus-visible { outline: 2px solid var(--color-primary); outline-offset: 1px; }
.budget-card-top:focus-visible { outline: 2px solid var(--color-primary); outline-offset: -2px; }

/* Table (Definitions) */
.table-wrap { overflow-x: auto; }
.exp-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.exp-table th {
  text-align: left; padding: 10px 12px; font-size: 11px; font-weight: 700;
  color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: .4px;
  border-bottom: 2px solid var(--border-strong); background: #f9fafb;
}
.exp-table td { padding: 10px 12px; border-bottom: 1px solid #f3f4f6; color: var(--color-text); }
.exp-table tr:hover { background: #f9fafb; }
.fw600 { font-weight: 600; color: var(--color-text); }
.mono { font-family: monospace; font-size: 12px; color: var(--color-primary); }
.muted { color: var(--color-text-muted); font-size: 12px; }
.text-right { text-align: right; }
.clickable-row { cursor: pointer; }

/* Gider Kalemi Ekstresi (accordion) */
.def-chevron { font-size: 16px; color: var(--color-text-muted); vertical-align: middle; margin-right: 4px; transition: transform .15s; display: inline-block; }
.def-chevron--open { transform: rotate(90deg); color: var(--color-primary); }
.def-statement-row td { background: #faf5ff; border-top: 2px solid var(--border-strong); padding: 16px 20px !important; }
.def-statement-loading { color: var(--color-text-secondary); font-size: 13px; padding: 8px 0; }
.def-statement-summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 12px; margin-bottom: 12px; }
.def-statement-item { display: flex; flex-direction: column; gap: 2px; font-size: 13px; color: var(--color-text); }
.def-statement-empty { color: var(--color-text-muted); font-size: 13px; padding: 8px 0; }

/* Ödemeler: açılır liste */
.pay-list { display: flex; flex-direction: column; }
.pay-item-wrap { border-bottom: 1px solid #f3f4f6; }
.pay-item-wrap:last-child { border-bottom: none; }
.pay-item {
  display: flex; align-items: center; gap: 12px; padding: 10px 14px; cursor: pointer; transition: background .15s ease;
  flex-wrap: wrap;
}
.pay-item:hover { background: var(--color-primary-light); }
.pay-item.open { background: #ede9fe; }
.pay-item.deleted { opacity: .5; }
.pay-chevron { font-size: 16px; color: var(--color-text-muted); transition: transform .15s; }
.pay-chevron--open { transform: rotate(90deg); color: var(--color-primary); }
.pay-date { font-size: 12px; color: var(--color-text-secondary); min-width: 70px; }
.pay-name { flex: 1; min-width: 140px; }
.pay-amount { font-weight: 700; color: var(--color-text); font-variant-numeric: tabular-nums; margin-left: auto; }
.deleted-tag {
  display: inline-block; padding: 2px 6px; border-radius: var(--radius-sm);
  font-size: 10px; font-weight: 700; background: var(--color-danger-bg); color: var(--color-danger);
}

.pay-detail { padding: 14px 20px 16px 42px; background: #faf5ff; border-top: 2px solid var(--border-strong); }
.pay-detail-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px 20px; margin-bottom: 10px; }
.pay-detail-item { display: flex; flex-direction: column; gap: 2px; font-size: 13px; color: var(--color-text); }
.pay-detail-full { grid-column: span 2; }
.pay-detail-label { font-size: 10px; font-weight: 700; text-transform: uppercase; letter-spacing: .4px; color: var(--color-text-muted); }
.pay-deleted-banner {
  display: flex; align-items: center; gap: 6px; margin-top: 8px; padding: 8px 12px;
  border-radius: var(--radius-md); background: var(--color-danger-bg); border: 1px solid var(--color-danger-bg); color: var(--color-danger); font-size: 12px; font-weight: 500;
}
.pay-detail-footer { margin-top: 10px; }

/* Badges */
.cat-badge {
  display: inline-block; padding: 3px 9px; border-radius: var(--radius-sm);
  font-size: 11px; font-weight: 600; background: #f3f4f6; color: var(--color-text);
}
.status-badge { display: inline-block; padding: 3px 9px; border-radius: var(--radius-sm); font-size: 11px; font-weight: 700; }
.status-badge--pending { background: var(--color-warning-bg); color: var(--color-warning); }
.status-badge--paid { background: var(--color-success-bg); color: var(--color-success); }
.status-badge--cancelled { background: #f3f4f6; color: var(--color-text-secondary); }
.recurring-badge {
  display: inline-block; padding: 2px 8px; border-radius: var(--radius-sm);
  font-size: 11px; font-weight: 600; background: #ede9fe; color: #7c3aed;
}
.status-dot {
  display: inline-block; width: 7px; height: 7px; border-radius: 50%; margin-right: 4px;
}
.status-dot.active { background: var(--color-success); }
.status-dot.inactive { background: #d1d5db; }

/* Action buttons */
.action-cell { display: flex; gap: 4px; }
.icon-btn {
  width: 30px; height: 30px; display: flex; align-items: center; justify-content: center;
  border: none; background: transparent; border-radius: var(--radius-sm); cursor: pointer; color: var(--color-text-secondary);
}
.icon-btn:hover:not(:disabled) { background: #f3f4f6; color: var(--color-text); }
.icon-btn.danger:hover:not(:disabled) { background: var(--color-danger-bg); color: var(--color-danger); }
.icon-btn:disabled { opacity: .5; cursor: not-allowed; }
.icon-btn .material-symbols-outlined { font-size: 18px; }

/* Loading / Error */
.exp-loading { display: flex; align-items: center; justify-content: center; gap: 10px; padding: 48px; color: var(--color-text-secondary); font-size: 14px; }
.spinner { width: 20px; height: 20px; border: 2px solid #e5e7eb; border-top-color: var(--color-primary); border-radius: 50%; animation: spin .6s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
.exp-error { display: flex; align-items: center; gap: 8px; padding: 16px; background: var(--color-danger-bg); color: var(--color-danger); border-radius: var(--radius-md); font-size: 13px; }
.exp-error .material-symbols-outlined { font-size: 20px; }

/* Modal — ModernPartyAccounts.vue ile aynı token/ritim (z-index, padding, border, gölge) */
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.4); z-index: 1000;
  display: flex; align-items: center; justify-content: center;
  backdrop-filter: var(--glass-blur-strong); padding: 20px;
}
.modal {
  background: var(--color-bg-card); border-radius: var(--radius-lg); width: 520px; max-width: 95vw;
  max-height: 90vh; overflow-y: auto; box-shadow: 0 20px 60px rgba(0,0,0,.2);
}
.modal-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 20px 24px; border-bottom: 1px solid var(--color-border);
}
.modal-header h2 { font-size: 17px; font-weight: 600; color: #1a1a2e; margin: 0; }
.modal-body { padding: 24px; display: flex; flex-direction: column; gap: 14px; }
.modal-footer {
  display: flex; justify-content: flex-end; gap: 8px;
  padding: 16px 24px; border-top: 1px solid var(--color-border);
}

/* Form */
.form-row { display: flex; gap: 12px; }
.form-row > * { flex: 1; }
.form-group { display: flex; flex-direction: column; gap: 4px; }
.form-group label { display: flex; align-items: center; gap: 4px; font-size: 12px; font-weight: 600; color: var(--color-text); }
.form-input {
  padding: 8px 12px; border: 1px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 13px; color: var(--color-text); background: var(--color-bg-card); width: 100%;
}
.form-input:focus { border-color: var(--color-secondary-hover); outline: none; box-shadow: 0 0 0 2px rgba(37,99,235,.1); }
textarea.form-input { resize: vertical; }
.checkbox-label {
  display: flex; align-items: center; gap: 6px; font-size: 13px; color: var(--color-text); cursor: pointer;
}
.checkbox-label input[type="checkbox"] { width: 15px; height: 15px; accent-color: var(--color-primary); cursor: pointer; }

/* Kategori yönet linki (form etiketinin yanında) */
.cat-manage-link {
  display: inline-flex; align-items: center; justify-content: center;
  width: 18px; height: 18px; border: none; background: transparent; color: var(--color-text-muted);
  cursor: pointer; padding: 0; border-radius: var(--radius-sm);
}
.cat-manage-link:hover { color: var(--color-primary); background: var(--color-primary-light); }
.cat-manage-link .material-symbols-outlined { font-size: 15px; }

/* Kategori yönetim listesi */
.category-list { display: flex; flex-direction: column; gap: 6px; max-height: 320px; overflow-y: auto; }
.category-row {
  display: flex; align-items: center; gap: 10px; padding: 8px 10px;
  border: 1px solid var(--color-border); border-radius: var(--radius-md);
}
.category-name { flex: 1; font-size: 13px; font-weight: 600; color: var(--color-text); }
.category-name--inactive { color: var(--color-text-muted); text-decoration: line-through; }
.category-usage { font-size: 11px; color: var(--color-text-secondary); }
.category-actions { display: flex; gap: 2px; }
.category-empty { color: var(--color-text-muted); font-size: 13px; padding: 12px 0; text-align: center; }

/* Ödeme modalı içinde hızlı gider türü oluşturma */
.quick-def-box {
  background: var(--color-primary-light);
  border: 1px dashed var(--color-primary);
  border-radius: var(--radius-md);
  padding: 12px;
  margin-top: -4px;
}
.quick-def-hint { font-size: 11px; color: var(--color-text-secondary); margin: 6px 0 0; }

/* Responsive */
@media (max-width: 768px) {
  .kpi-grid { grid-template-columns: repeat(2, 1fr); }
  .filter-bar { flex-direction: column; }
  .form-row { flex-direction: column; }
}
</style>
