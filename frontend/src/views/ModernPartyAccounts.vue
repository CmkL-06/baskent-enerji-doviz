<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import AppKpiCard from '@/components/common/AppKpiCard.vue'
import { useNotification } from '@/composables/useNotification'

const authStore = useAuthStore()
const notification = useNotification()
const exchangeStore = useExchangeStore()

const loading = ref(true)
const saving = ref(false)
const error = ref('')

// Data
const parties = ref<any[]>([])
const currencies = ref<any[]>([])
const selectedParty = ref<any>(null)
const partyAccounts = ref<any[]>([])
const accountEntries = ref<any[]>([])
const selectedAccount = ref<any>(null)
const partyStatement = ref<any>(null)

// Filters
const searchQuery = ref('')
const filterType = ref<number | ''>('')
const filterBalance = ref<'' | 'receivables' | 'debts' | 'all'>('')

// Tabs
const activeTab = ref<'list' | 'accounts' | 'statement'>('list')

// Modals
const showCreateModal = ref(false)
const showPaymentModal = ref(false)
const showStatementModal = ref(false)
const editingParty = ref<any>(null)

const PARTY_TYPES: Record<number, string> = {
  1: 'Müşteri', 2: 'Tedarikçi', 3: 'Her İkisi',
}

const PARTY_STATUSES: Record<number, string> = {
  1: 'Aktif', 2: 'Pasif', 3: 'Askıda', 4: 'Blokeli',
}

const PAYMENT_METHODS: Record<number, string> = {
  1: 'Nakit', 2: 'Banka Transferi', 3: 'Kredi Kartı', 4: 'Çek', 5: 'Diğer',
}

const partyForm = ref({
  partyCode: '',
  name: '',
  type: 1,
  contactPerson: '',
  email: '',
  phone: '',
  address: '',
  taxNumber: '',
  registrationNumber: '',
  hasCreditLimit: false,
  defaultPaymentTermDays: 0,
  notes: '',
})

const paymentForm = ref({
  partyId: '',
  currencyId: '',
  amount: null as number | null,
  type: 1,
  paymentMethod: 1,
  paymentReference: '',
  paymentDate: new Date().toISOString().slice(0, 10),
  notes: '',
  customExchangeRate: null as number | null,
})

const officeId = computed(() =>
  exchangeStore.selectedOffice?.officeId ?? exchangeStore.offices[0]?.officeId
)

const filteredParties = computed(() => {
  let list = parties.value
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter(p =>
      p.name?.toLowerCase().includes(q) ||
      p.partyCode?.toLowerCase().includes(q) ||
      p.phone?.toLowerCase().includes(q)
    )
  }
  if (filterType.value !== '') {
    list = list.filter(p => p.type === filterType.value)
  }
  if (filterBalance.value === 'receivables') {
    list = list.filter(p => (p.netBalance ?? 0) > 0)
  } else if (filterBalance.value === 'debts') {
    list = list.filter(p => (p.netBalance ?? 0) < 0)
  }
  return list
})

const totals = computed(() => {
  let totalReceivables = 0
  let totalDebts = 0
  parties.value.forEach(p => {
    totalReceivables += p.totalReceivables ?? 0
    totalDebts += p.totalPayables ?? 0
  })
  return {
    totalReceivables,
    totalDebts,
    netBalance: totalReceivables - totalDebts,
    count: parties.value.length,
  }
})

const formatCurrency = (amount: number): string =>
  new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount)

const formatDate = (d: string | null | undefined): string => {
  if (!d) return '-'
  return new Date(d).toLocaleDateString('tr-TR')
}

// API calls
async function loadParties() {
  loading.value = true
  error.value = ''
  try {
    const data = await apiService.getParties(officeId.value)
    parties.value = Array.isArray(data) ? data : (data?.items ?? [])
    emitTotals()
  } catch (e: any) {
    error.value = 'Cariler yüklenemedi'
    console.error(e)
  } finally {
    loading.value = false
  }
}

function emitTotals() {
  window.dispatchEvent(new CustomEvent('updatePartyTotals', {
    detail: {
      totalReceivables: totals.value.totalReceivables,
      totalDebts: totals.value.totalDebts,
      netBalance: totals.value.netBalance,
    }
  }))
}

async function selectParty(party: any) {
  selectedParty.value = party
  activeTab.value = 'accounts'
  try {
    const data = await apiService.getPartyAccounts(party.id)
    partyAccounts.value = Array.isArray(data) ? data : (data?.items ?? [])
  } catch {
    partyAccounts.value = party.accounts ?? []
  }
  accountEntries.value = []
  selectedAccount.value = null
}

async function loadAccountEntries(account: any) {
  selectedAccount.value = account
  try {
    const data = await apiService.getPartyAccountEntries(account.id)
    accountEntries.value = Array.isArray(data) ? data : (data?.items ?? [])
  } catch {
    accountEntries.value = []
  }
}

async function loadStatement(party: any) {
  try {
    partyStatement.value = await apiService.getPartyStatement(party.id)
    showStatementModal.value = true
  } catch {
    notification.error('Ekstre yüklenemedi')
  }
}

// CRUD
function openCreateModal() {
  editingParty.value = null
  partyForm.value = {
    partyCode: '',
    name: '',
    type: 1,
    contactPerson: '',
    email: '',
    phone: '',
    address: '',
    taxNumber: '',
    registrationNumber: '',
    hasCreditLimit: false,
    defaultPaymentTermDays: 0,
    notes: '',
  }
  showCreateModal.value = true
}

function openEditModal(party: any) {
  editingParty.value = party
  partyForm.value = {
    partyCode: party.partyCode ?? '',
    name: party.name ?? '',
    type: party.type ?? 1,
    contactPerson: party.contactPerson ?? '',
    email: party.email ?? '',
    phone: party.phone ?? '',
    address: party.address ?? '',
    taxNumber: party.taxNumber ?? '',
    registrationNumber: party.registrationNumber ?? '',
    hasCreditLimit: party.hasCreditLimit ?? false,
    defaultPaymentTermDays: party.defaultPaymentTermDays ?? 0,
    notes: party.notes ?? '',
  }
  showCreateModal.value = true
}

async function saveParty() {
  if (!partyForm.value.partyCode || !partyForm.value.name) {
    notification.warning('Cari kodu ve adı zorunludur')
    return
  }
  saving.value = true
  try {
    if (editingParty.value) {
      await apiService.updateParty(editingParty.value.id, {
        ...partyForm.value,
        officeId: officeId.value,
      })
    } else {
      await apiService.createParty({
        ...partyForm.value,
        officeId: officeId.value,
      })
    }
    showCreateModal.value = false
    await loadParties()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Kayıt başarısız')
  } finally {
    saving.value = false
  }
}

async function deleteParty(party: any) {
  if (!confirm(`"${party.name}" carisini silmek istediğinizden emin misiniz?`)) return
  try {
    await apiService.deleteParty(party.id)
    await loadParties()
    if (selectedParty.value?.id === party.id) {
      selectedParty.value = null
      activeTab.value = 'list'
    }
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Silme başarısız')
  }
}

// Payment
function openPaymentModal(party?: any) {
  const p = party || selectedParty.value
  if (!p) return
  paymentForm.value = {
    partyId: p.id,
    currencyId: currencies.value[0]?.id ?? '',
    amount: null,
    type: 1,
    paymentMethod: 1,
    paymentReference: '',
    paymentDate: new Date().toISOString().slice(0, 10),
    notes: '',
    customExchangeRate: null,
  }
  showPaymentModal.value = true
}

async function savePayment() {
  if (!paymentForm.value.amount || paymentForm.value.amount <= 0) {
    notification.warning('Tutar giriniz')
    return
  }
  saving.value = true
  try {
    await apiService.createPartyPayment({
      ...paymentForm.value,
      officeId: officeId.value,
    })
    showPaymentModal.value = false
    await loadParties()
    if (selectedParty.value) {
      await selectParty(selectedParty.value)
    }
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Ödeme kaydedilemedi')
  } finally {
    saving.value = false
  }
}

// CustomEvent listeners from sidebar
function onOpenCreateModal() {
  openCreateModal()
}

function onFilterParties(e: Event) {
  const detail = (e as CustomEvent).detail
  if (detail === 'receivables') filterBalance.value = 'receivables'
  else if (detail === 'debts') filterBalance.value = 'debts'
  else filterBalance.value = ''
  activeTab.value = 'list'
  selectedParty.value = null
}

function backToList() {
  activeTab.value = 'list'
  selectedParty.value = null
  partyAccounts.value = []
  accountEntries.value = []
  selectedAccount.value = null
}

watch(() => officeId.value, () => {
  if (officeId.value) loadParties()
})

onMounted(async () => {
  window.addEventListener('openCreatePartyModal', onOpenCreateModal)
  window.addEventListener('filterParties', onFilterParties)

  if (!exchangeStore.offices?.length) {
    await exchangeStore.loadOffices()
  }

  currencies.value = exchangeStore.currencies?.length
    ? exchangeStore.currencies
    : await apiService.getCurrencies().catch(() => [])

  await loadParties()
})

onUnmounted(() => {
  window.removeEventListener('openCreatePartyModal', onOpenCreateModal)
  window.removeEventListener('filterParties', onFilterParties)
})
</script>

<template>
  <div class="party-wrap">
    <!-- Header -->
    <div class="party-header">
      <div class="header-left">
        <button v-if="activeTab !== 'list'" class="btn-back" @click="backToList">
          <span class="material-symbols-outlined" aria-hidden="true">arrow_back</span>
        </button>
        <h1 v-if="activeTab === 'list'">Cariler</h1>
        <h1 v-else-if="activeTab === 'accounts' && selectedParty">
          {{ selectedParty.name }}
          <span class="party-code">{{ selectedParty.partyCode }}</span>
        </h1>
      </div>
      <div class="header-actions">
        <button v-if="activeTab === 'list'" class="btn-primary" @click="openCreateModal">
          <span class="material-symbols-outlined" aria-hidden="true">person_add</span>
          Yeni Cari
        </button>
        <button v-if="activeTab === 'accounts' && selectedParty" class="btn-primary" @click="openPaymentModal()">
          <span class="material-symbols-outlined" aria-hidden="true">payments</span>
          Ödeme Ekle
        </button>
        <button v-if="activeTab === 'accounts' && selectedParty" class="btn-secondary" @click="loadStatement(selectedParty)">
          <span class="material-symbols-outlined" aria-hidden="true">receipt_long</span>
          Ekstre
        </button>
      </div>
    </div>

    <!-- KPI Cards -->
    <div v-if="activeTab === 'list'" class="kpi-grid">
      <AppKpiCard icon="people" label="Toplam Cari" :value="totals.count" color="#6366f1" bg="#eef2ff" />
      <AppKpiCard icon="arrow_downward" label="Toplam Alacak" :value="formatCurrency(totals.totalReceivables) + ' ₺'" color="#059669" bg="#ecfdf5" />
      <AppKpiCard icon="arrow_upward" label="Toplam Borç" :value="formatCurrency(totals.totalDebts) + ' ₺'" color="#ef4444" bg="#fef2f2" />
      <AppKpiCard icon="account_balance" label="Net Bakiye" :value="formatCurrency(totals.netBalance) + ' ₺'" :color="totals.netBalance >= 0 ? '#059669' : '#ef4444'" :bg="totals.netBalance >= 0 ? '#ecfdf5' : '#fef2f2'" />
    </div>

    <!-- Filters -->
    <div v-if="activeTab === 'list'" class="filter-bar">
      <div class="search-box">
        <span class="material-symbols-outlined" aria-hidden="true">search</span>
        <input v-model="searchQuery" placeholder="Cari ara (ad, kod, telefon)..." />
      </div>
      <select v-model="filterType" class="filter-select">
        <option value="">Tüm Tipler</option>
        <option :value="1">Müşteri</option>
        <option :value="2">Tedarikçi</option>
        <option :value="3">Her İkisi</option>
      </select>
      <div class="filter-chips">
        <button :class="['chip', filterBalance === '' ? 'chip-active' : '']" @click="filterBalance = ''">Tümü</button>
        <button :class="['chip', filterBalance === 'receivables' ? 'chip-active' : '']" @click="filterBalance = 'receivables'">Alacaklılar</button>
        <button :class="['chip', filterBalance === 'debts' ? 'chip-active' : '']" @click="filterBalance = 'debts'">Borçlular</button>
      </div>
    </div>

    <!-- Loading / Error -->
    <div v-if="loading" class="loading-state">
      <span class="material-symbols-outlined spin">progress_activity</span>
      Yükleniyor...
    </div>
    <div v-else-if="error" class="error-state">{{ error }}</div>

    <!-- Party List Tab -->
    <div v-else-if="activeTab === 'list'" class="party-table-wrap">
      <table class="party-table">
        <thead>
          <tr>
            <th>Kod</th>
            <th>Ad</th>
            <th>Tip</th>
            <th>Telefon</th>
            <th>Durum</th>
            <th class="text-right">Net Bakiye</th>
            <th>Son İşlem</th>
            <th class="text-center">İşlem</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="filteredParties.length === 0">
            <td colspan="8" class="empty-row">Cari bulunamadı</td>
          </tr>
          <tr v-for="party in filteredParties" :key="party.id" class="clickable-row" @click="selectParty(party)">
            <td class="code-cell">{{ party.partyCode }}</td>
            <td class="name-cell">
              <strong>{{ party.name }}</strong>
              <small v-if="party.contactPerson">{{ party.contactPerson }}</small>
            </td>
            <td>
              <span class="type-badge" :class="'type-' + party.type">
                {{ PARTY_TYPES[party.type] || party.typeName || '-' }}
              </span>
            </td>
            <td>{{ party.phone || '-' }}</td>
            <td>
              <span class="status-dot" :class="'status-' + party.status"></span>
              {{ PARTY_STATUSES[party.status] || party.statusName || '-' }}
            </td>
            <td class="text-right" :class="(party.netBalance ?? 0) >= 0 ? 'balance-pos' : 'balance-neg'">
              {{ formatCurrency(party.netBalance ?? 0) }} ₺
            </td>
            <td>{{ formatDate(party.lastTransactionDate) }}</td>
            <td class="text-center actions-cell" @click.stop>
              <button class="icon-btn" title="Düzenle" @click="openEditModal(party)">
                <span class="material-symbols-outlined" aria-hidden="true">edit</span>
              </button>
              <button class="icon-btn" title="Ödeme" @click="openPaymentModal(party)">
                <span class="material-symbols-outlined" aria-hidden="true">payments</span>
              </button>
              <button class="icon-btn" title="Ekstre" @click="loadStatement(party)">
                <span class="material-symbols-outlined" aria-hidden="true">receipt_long</span>
              </button>
              <button class="icon-btn danger" title="Sil" @click="deleteParty(party)">
                <span class="material-symbols-outlined" aria-hidden="true">delete</span>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Accounts Tab -->
    <div v-else-if="activeTab === 'accounts' && selectedParty" class="accounts-section">
      <!-- Party Info Card -->
      <div class="party-info-card">
        <div class="info-grid">
          <div class="info-item">
            <span class="info-label">Tip</span>
            <span>{{ PARTY_TYPES[selectedParty.type] || '-' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Telefon</span>
            <span>{{ selectedParty.phone || '-' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">E-posta</span>
            <span>{{ selectedParty.email || '-' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Vergi No</span>
            <span>{{ selectedParty.taxNumber || '-' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Adres</span>
            <span>{{ selectedParty.address || '-' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Durum</span>
            <span class="status-dot" :class="'status-' + selectedParty.status"></span>
            {{ PARTY_STATUSES[selectedParty.status] || '-' }}
          </div>
        </div>
      </div>

      <!-- Accounts List -->
      <h3 class="section-title">Hesaplar</h3>
      <div class="accounts-grid">
        <div v-for="acc in partyAccounts" :key="acc.id"
             class="account-card" :class="{ 'account-selected': selectedAccount?.id === acc.id }"
             @click="loadAccountEntries(acc)">
          <div class="acc-header">
            <span class="acc-currency">{{ acc.currencyCode }}</span>
            <span class="acc-number">{{ acc.accountNumber }}</span>
          </div>
          <div class="acc-balance" :class="acc.balance >= 0 ? 'balance-pos' : 'balance-neg'">
            {{ formatCurrency(acc.balance ?? 0) }}
          </div>
          <div class="acc-meta">
            <span>Borç: {{ formatCurrency(acc.totalDebits ?? 0) }}</span>
            <span>Alacak: {{ formatCurrency(acc.totalCredits ?? 0) }}</span>
          </div>
          <div class="acc-meta">
            <span>{{ acc.transactionCount ?? 0 }} işlem</span>
            <span v-if="acc.hasCreditLimit">Limit: {{ formatCurrency(acc.creditLimit ?? 0) }}</span>
          </div>
        </div>
        <div v-if="partyAccounts.length === 0" class="empty-accounts">
          Bu cari için hesap bulunamadı
        </div>
      </div>

      <!-- Account Entries -->
      <div v-if="selectedAccount" class="entries-section">
        <h3 class="section-title">
          {{ selectedAccount.currencyCode }} Hesap Hareketleri
        </h3>
        <table class="party-table">
          <thead>
            <tr>
              <th>Tarih</th>
              <th>Fiş No</th>
              <th>Açıklama</th>
              <th class="text-right">Borç</th>
              <th class="text-right">Alacak</th>
              <th class="text-right">Bakiye</th>
              <th>Durum</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="accountEntries.length === 0">
              <td colspan="7" class="empty-row">Hareket bulunamadı</td>
            </tr>
            <tr v-for="entry in accountEntries" :key="entry.id">
              <td>{{ formatDate(entry.entryDate) }}</td>
              <td class="code-cell">{{ entry.entryNumber || '-' }}</td>
              <td>{{ entry.description || '-' }}</td>
              <td class="text-right balance-neg">{{ entry.debitAmount ? formatCurrency(entry.debitAmount) : '' }}</td>
              <td class="text-right balance-pos">{{ entry.creditAmount ? formatCurrency(entry.creditAmount) : '' }}</td>
              <td class="text-right" :class="(entry.runningBalance ?? 0) >= 0 ? 'balance-pos' : 'balance-neg'">
                {{ formatCurrency(entry.runningBalance ?? 0) }}
              </td>
              <td>
                <span class="payment-status" :class="'ps-' + entry.paymentStatus">
                  {{ entry.paymentStatusName || '-' }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Create/Edit Party Modal -->
    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div class="modal modal-lg">
        <div class="modal-header">
          <h2>{{ editingParty ? 'Cari Düzenle' : 'Yeni Cari' }}</h2>
          <button class="modal-close" @click="showCreateModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-grid-2">
            <div class="form-group">
              <label>Cari Kodu *</label>
              <input v-model="partyForm.partyCode" maxlength="20" placeholder="Örn: C001" />
            </div>
            <div class="form-group">
              <label>Cari Adı *</label>
              <input v-model="partyForm.name" maxlength="200" placeholder="Ad Soyad / Firma Adı" />
            </div>
            <div class="form-group">
              <label>Tip</label>
              <select v-model="partyForm.type">
                <option :value="1">Müşteri</option>
                <option :value="2">Tedarikçi</option>
                <option :value="3">Her İkisi</option>
              </select>
            </div>
            <div class="form-group">
              <label>Yetkili Kişi</label>
              <input v-model="partyForm.contactPerson" placeholder="İletişim kişisi" />
            </div>
            <div class="form-group">
              <label>Telefon</label>
              <input v-model="partyForm.phone" placeholder="05xx xxx xx xx" />
            </div>
            <div class="form-group">
              <label>E-posta</label>
              <input v-model="partyForm.email" type="email" placeholder="email@example.com" />
            </div>
            <div class="form-group">
              <label>Vergi Numarası</label>
              <input v-model="partyForm.taxNumber" placeholder="Vergi No" />
            </div>
            <div class="form-group">
              <label>Sicil No</label>
              <input v-model="partyForm.registrationNumber" placeholder="Ticaret Sicil No" />
            </div>
            <div class="form-group full-width">
              <label>Adres</label>
              <input v-model="partyForm.address" placeholder="Adres" />
            </div>
            <div class="form-group">
              <label>Vade (Gün)</label>
              <input v-model.number="partyForm.defaultPaymentTermDays" type="number" min="0" max="365" />
            </div>
            <div class="form-group checkbox-group">
              <label>
                <input type="checkbox" v-model="partyForm.hasCreditLimit" />
                Kredi limiti var
              </label>
            </div>
            <div class="form-group full-width">
              <label>Notlar</label>
              <textarea v-model="partyForm.notes" rows="2" placeholder="Notlar..."></textarea>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showCreateModal = false">İptal</button>
          <button class="btn-primary" :disabled="saving" @click="saveParty">
            {{ saving ? 'Kaydediliyor...' : (editingParty ? 'Güncelle' : 'Kaydet') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Payment Modal -->
    <div v-if="showPaymentModal" class="modal-overlay" @click.self="showPaymentModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Ödeme Ekle</h2>
          <button class="modal-close" @click="showPaymentModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-grid-2">
            <div class="form-group">
              <label>İşlem Tipi</label>
              <select v-model="paymentForm.type">
                <option :value="1">Borç (Debit)</option>
                <option :value="2">Alacak (Credit)</option>
              </select>
            </div>
            <div class="form-group">
              <label>Para Birimi</label>
              <select v-model="paymentForm.currencyId">
                <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.code }} - {{ c.name }}</option>
              </select>
            </div>
            <div class="form-group">
              <label>Tutar *</label>
              <input v-model.number="paymentForm.amount" type="number" step="0.01" min="0" placeholder="0.00" />
            </div>
            <div class="form-group">
              <label>Ödeme Yöntemi</label>
              <select v-model="paymentForm.paymentMethod">
                <option v-for="(label, key) in PAYMENT_METHODS" :key="key" :value="Number(key)">{{ label }}</option>
              </select>
            </div>
            <div class="form-group">
              <label>Tarih</label>
              <input v-model="paymentForm.paymentDate" type="date" />
            </div>
            <div class="form-group">
              <label>Referans No</label>
              <input v-model="paymentForm.paymentReference" placeholder="Dekont / referans" />
            </div>
            <div class="form-group">
              <label>Özel Kur</label>
              <input v-model.number="paymentForm.customExchangeRate" type="number" step="0.0001" placeholder="Boş bırakılırsa güncel kur" />
            </div>
            <div class="form-group full-width">
              <label>Açıklama</label>
              <textarea v-model="paymentForm.notes" rows="2" placeholder="Açıklama..."></textarea>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showPaymentModal = false">İptal</button>
          <button class="btn-primary" :disabled="saving" @click="savePayment">
            {{ saving ? 'Kaydediliyor...' : 'Ödeme Kaydet' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Statement Modal -->
    <div v-if="showStatementModal && partyStatement" class="modal-overlay" @click.self="showStatementModal = false">
      <div class="modal modal-xl">
        <div class="modal-header">
          <h2>Cari Ekstre — {{ partyStatement.partyName || selectedParty?.name }}</h2>
          <button class="modal-close" @click="showStatementModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <!-- Aging Summary -->
          <div class="aging-grid">
            <div class="aging-card">
              <span class="aging-label">Açılış Bakiye</span>
              <span class="aging-value">{{ formatCurrency(partyStatement.openingBalance ?? 0) }}</span>
            </div>
            <div class="aging-card">
              <span class="aging-label">Toplam Borç</span>
              <span class="aging-value balance-neg">{{ formatCurrency(partyStatement.totalDebits ?? 0) }}</span>
            </div>
            <div class="aging-card">
              <span class="aging-label">Toplam Alacak</span>
              <span class="aging-value balance-pos">{{ formatCurrency(partyStatement.totalCredits ?? 0) }}</span>
            </div>
            <div class="aging-card">
              <span class="aging-label">Kapanış Bakiye</span>
              <span class="aging-value" :class="(partyStatement.closingBalance ?? 0) >= 0 ? 'balance-pos' : 'balance-neg'">
                {{ formatCurrency(partyStatement.closingBalance ?? 0) }}
              </span>
            </div>
          </div>

          <!-- Aging Buckets -->
          <div v-if="partyStatement.currentAmount != null" class="aging-buckets">
            <h4>Yaşlandırma</h4>
            <div class="aging-grid">
              <div class="aging-card small">
                <span class="aging-label">Güncel</span>
                <span>{{ formatCurrency(partyStatement.currentAmount ?? 0) }}</span>
              </div>
              <div class="aging-card small">
                <span class="aging-label">30 Gün</span>
                <span>{{ formatCurrency(partyStatement.amount30Days ?? 0) }}</span>
              </div>
              <div class="aging-card small">
                <span class="aging-label">60 Gün</span>
                <span>{{ formatCurrency(partyStatement.amount60Days ?? 0) }}</span>
              </div>
              <div class="aging-card small">
                <span class="aging-label">90 Gün</span>
                <span>{{ formatCurrency(partyStatement.amount90Days ?? 0) }}</span>
              </div>
              <div class="aging-card small">
                <span class="aging-label">90+ Gün</span>
                <span>{{ formatCurrency(partyStatement.amountOver90Days ?? 0) }}</span>
              </div>
            </div>
          </div>

          <!-- Statement Lines -->
          <table class="party-table stmt-table">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Fiş No</th>
                <th>Açıklama</th>
                <th>Referans</th>
                <th class="text-right">Borç</th>
                <th class="text-right">Alacak</th>
                <th class="text-right">Bakiye</th>
                <th>Vade</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(line, i) in (partyStatement.lines ?? partyStatement.entries ?? [])" :key="i"
                  :class="{ 'overdue-row': line.isOverdue }">
                <td>{{ formatDate(line.date ?? line.entryDate) }}</td>
                <td class="code-cell">{{ line.entryNumber || '-' }}</td>
                <td>{{ line.description || '-' }}</td>
                <td>{{ line.referenceNumber || line.transactionNumber || '-' }}</td>
                <td class="text-right balance-neg">{{ line.debit ? formatCurrency(line.debit) : (line.debitAmount ? formatCurrency(line.debitAmount) : '') }}</td>
                <td class="text-right balance-pos">{{ line.credit ? formatCurrency(line.credit) : (line.creditAmount ? formatCurrency(line.creditAmount) : '') }}</td>
                <td class="text-right" :class="(line.balance ?? line.runningBalance ?? 0) >= 0 ? 'balance-pos' : 'balance-neg'">
                  {{ formatCurrency(line.balance ?? line.runningBalance ?? 0) }}
                </td>
                <td :class="{ 'overdue-text': line.isOverdue }">{{ formatDate(line.dueDate) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showStatementModal = false">Kapat</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.party-wrap {
  padding: 24px;
  max-width: 1400px;
  margin: 0 auto;
}

/* Header */
.party-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}
.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}
.header-left h1 {
  font-size: 22px;
  font-weight: 700;
  color: #1a1a2e;
  margin: 0;
}
.party-code {
  font-size: 13px;
  font-weight: 400;
  color: #6b7280;
  margin-left: 8px;
}
.header-actions {
  display: flex;
  gap: 8px;
}
.btn-back {
  background: none;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 6px 8px;
  cursor: pointer;
  color: #374151;
  display: flex;
  align-items: center;
}
.btn-back:hover { background: #f3f4f6; }

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: #2563eb;
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
}
.btn-primary:hover { background: #1d4ed8; }
.btn-primary:disabled { opacity: .6; cursor: not-allowed; }

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: #fff;
  color: #374151;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
}
.btn-secondary:hover { background: #f9fafb; }

.btn-cancel {
  padding: 8px 16px;
  background: #fff;
  color: #374151;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 13px;
  cursor: pointer;
}
.btn-cancel:hover { background: #f3f4f6; }

/* KPI Grid */
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
  margin-bottom: 20px;
}
/* Filter Bar */
.filter-bar {
  display: flex;
  gap: 12px;
  align-items: center;
  margin-bottom: 16px;
  flex-wrap: wrap;
}
.search-box {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #fff;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  padding: 6px 12px;
  flex: 1;
  min-width: 220px;
}
.search-box input {
  border: none;
  outline: none;
  font-size: 13px;
  width: 100%;
  background: transparent;
}
.search-box .material-symbols-outlined {
  font-size: 18px;
  color: #9ca3af;
}
.filter-select {
  padding: 8px 12px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 13px;
  background: #fff;
  color: #374151;
}
.filter-chips {
  display: flex;
  gap: 6px;
}
.chip {
  padding: 6px 14px;
  border-radius: 20px;
  border: 1px solid #d1d5db;
  background: #fff;
  font-size: 12px;
  cursor: pointer;
  color: #374151;
}
.chip:hover { background: #f3f4f6; }
.chip-active {
  background: #2563eb;
  color: #fff;
  border-color: #2563eb;
}

/* Loading / Error */
.loading-state, .error-state {
  text-align: center;
  padding: 60px 20px;
  color: #6b7280;
  font-size: 14px;
}
.error-state { color: #dc2626; }
.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

/* Table */
.party-table-wrap {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  overflow-x: auto;
}
.party-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}
.party-table th {
  padding: 10px 14px;
  text-align: left;
  font-weight: 600;
  color: #6b7280;
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: .5px;
  border-bottom: 1px solid #e5e7eb;
  background: #f9fafb;
  white-space: nowrap;
}
.party-table td {
  padding: 10px 14px;
  border-bottom: 1px solid #f3f4f6;
  color: #374151;
}
.clickable-row { cursor: pointer; }
.clickable-row:hover { background: #f0f7ff; }
.text-right { text-align: right; }
.text-center { text-align: center; }
.empty-row {
  text-align: center;
  color: #9ca3af;
  padding: 40px 14px !important;
}
.code-cell {
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 12px;
  color: #6b7280;
}
.name-cell strong {
  display: block;
  color: #1a1a2e;
}
.name-cell small {
  color: #9ca3af;
  font-size: 11px;
}
.balance-pos { color: #059669; font-weight: 600; }
.balance-neg { color: #dc2626; font-weight: 600; }

/* Type badges */
.type-badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 500;
}
.type-1 { background: #dbeafe; color: #1d4ed8; }
.type-2 { background: #fef3c7; color: #92400e; }
.type-3 { background: #e0e7ff; color: #4338ca; }

/* Status */
.status-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  margin-right: 4px;
}
.status-1 { background: #10b981; }
.status-2 { background: #9ca3af; }
.status-3 { background: #f59e0b; }
.status-4 { background: #ef4444; }

/* Action buttons */
.actions-cell {
  display: flex;
  gap: 2px;
  justify-content: center;
}
.icon-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: 4px;
  border-radius: 6px;
  color: #6b7280;
  display: flex;
  align-items: center;
}
.icon-btn:hover { background: #f3f4f6; color: #2563eb; }
.icon-btn.danger:hover { color: #dc2626; }
.icon-btn .material-symbols-outlined { font-size: 18px; }

/* Party Info Card */
.party-info-card {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 20px;
}
.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
}
.info-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.info-label {
  font-size: 11px;
  color: #9ca3af;
  text-transform: uppercase;
  letter-spacing: .5px;
}

/* Section title */
.section-title {
  font-size: 15px;
  font-weight: 600;
  color: #1a1a2e;
  margin: 20px 0 12px;
}

/* Accounts Grid */
.accounts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 12px;
  margin-bottom: 20px;
}
.account-card {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  padding: 16px;
  cursor: pointer;
  transition: border-color .15s, box-shadow .15s;
}
.account-card:hover { border-color: #93c5fd; }
.account-selected { border-color: #2563eb; box-shadow: 0 0 0 2px rgba(37, 99, 235, .15); }
.acc-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}
.acc-currency {
  font-weight: 700;
  font-size: 16px;
  color: #1a1a2e;
}
.acc-number {
  font-size: 11px;
  color: #9ca3af;
  font-family: monospace;
}
.acc-balance {
  font-size: 22px;
  font-weight: 700;
  margin-bottom: 8px;
}
.acc-meta {
  display: flex;
  justify-content: space-between;
  font-size: 11px;
  color: #6b7280;
  margin-top: 4px;
}
.empty-accounts {
  grid-column: 1 / -1;
  text-align: center;
  color: #9ca3af;
  padding: 40px;
}

/* Entries Section */
.entries-section {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  overflow-x: auto;
}
.entries-section .section-title {
  padding: 16px 16px 0;
}

/* Payment status */
.payment-status {
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 10px;
}
.ps-1 { background: #fef3c7; color: #92400e; }
.ps-2 { background: #d1fae5; color: #065f46; }
.ps-3 { background: #e0e7ff; color: #4338ca; }
.ps-4 { background: #fee2e2; color: #991b1b; }
.ps-5 { background: #f3f4f6; color: #6b7280; }

/* Modal */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, .4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 20px;
}
.modal {
  background: #fff;
  border-radius: 16px;
  width: 100%;
  max-width: 560px;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 60px rgba(0, 0, 0, .15);
}
.modal-lg { max-width: 680px; }
.modal-xl { max-width: 960px; }
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
  border-bottom: 1px solid #e5e7eb;
}
.modal-header h2 {
  font-size: 17px;
  font-weight: 600;
  color: #1a1a2e;
  margin: 0;
}
.modal-close {
  background: none;
  border: none;
  font-size: 22px;
  cursor: pointer;
  color: #6b7280;
  padding: 0 4px;
}
.modal-body {
  padding: 24px;
  overflow-y: auto;
}
.modal-footer {
  padding: 16px 24px;
  border-top: 1px solid #e5e7eb;
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

/* Form */
.form-grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}
.form-group {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.form-group.full-width { grid-column: 1 / -1; }
.form-group label {
  font-size: 12px;
  font-weight: 500;
  color: #374151;
}
.form-group input,
.form-group select,
.form-group textarea {
  padding: 8px 12px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 13px;
  outline: none;
  transition: border-color .15s;
}
.form-group input:focus,
.form-group select:focus,
.form-group textarea:focus {
  border-color: #2563eb;
  box-shadow: 0 0 0 2px rgba(37, 99, 235, .1);
}
.form-group textarea { resize: vertical; }
.checkbox-group label {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding-top: 20px;
}
.checkbox-group input[type="checkbox"] {
  width: 16px;
  height: 16px;
}

/* Aging */
.aging-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 12px;
  margin-bottom: 16px;
}
.aging-card {
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 12px;
  text-align: center;
}
.aging-card.small { padding: 8px; }
.aging-label {
  display: block;
  font-size: 11px;
  color: #6b7280;
  margin-bottom: 4px;
}
.aging-value { font-size: 16px; font-weight: 700; }
.aging-buckets { margin-bottom: 16px; }
.aging-buckets h4 {
  font-size: 13px;
  font-weight: 600;
  color: #374151;
  margin: 0 0 8px;
}

/* Statement table */
.stmt-table { margin-top: 8px; }
.overdue-row { background: #fef2f2; }
.overdue-text { color: #dc2626; font-weight: 500; }

/* Responsive */
@media (max-width: 768px) {
  .party-wrap { padding: 12px; }
  .party-header { flex-direction: column; gap: 12px; align-items: flex-start; }
  .filter-bar { flex-direction: column; }
  .form-grid-2 { grid-template-columns: 1fr; }
  .kpi-grid { grid-template-columns: repeat(2, 1fr); }
  .accounts-grid { grid-template-columns: 1fr; }
  .aging-grid { grid-template-columns: repeat(2, 1fr); }
}
</style>
