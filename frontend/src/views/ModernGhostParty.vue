<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
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

// Ghost party uses the regular parties list but for "ghost" (anonymous/temp) accounts
const parties = ref<any[]>([])
const selectedParty = ref<any>(null)
const ghostAccounts = ref<any[]>([])
const selectedAccount = ref<any>(null)
const accountEntries = ref<any[]>([])
const ghostSummary = ref<any>(null)
const ghostStatement = ref<any>(null)

const activeTab = ref<'parties' | 'accounts'>('parties')
const showPaymentModal = ref(false)
const showCollectionModal = ref(false)
const showCreateAccountModal = ref(false)
const showStatementModal = ref(false)

const searchQuery = ref('')

const paymentForm = ref({
  partyId: '',
  accountId: '',
  currencyId: '' as string | number,
  amount: null as number | null,
  description: '',
  referenceNumber: '',
})

const createAccountForm = ref({
  partyId: '',
  currencyId: '' as string | number,
  description: '',
})

const officeId = computed(() =>
  exchangeStore.selectedOffice?.officeId ?? exchangeStore.offices[0]?.officeId
)

const filteredParties = computed(() => {
  if (!searchQuery.value) return parties.value
  const q = searchQuery.value.toLowerCase()
  return parties.value.filter(p =>
    p.name?.toLowerCase().includes(q) || p.partyCode?.toLowerCase().includes(q)
  )
})

const formatCurrency = (amount: number): string =>
  new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount)

const formatDateTime = (d: string | null | undefined): string => {
  if (!d) return '-'
  return new Date(d).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

async function loadParties() {
  loading.value = true
  try {
    const data = await apiService.getParties(officeId.value)
    parties.value = Array.isArray(data) ? data : (data?.items ?? [])
  } catch { parties.value = [] }
  finally { loading.value = false }
}

async function selectParty(party: any) {
  selectedParty.value = party
  activeTab.value = 'accounts'
  try {
    const [accounts, summary] = await Promise.all([
      apiService.getGhostAccounts(party.id),
      apiService.getGhostSummary(party.id).catch(() => null),
    ])
    ghostAccounts.value = Array.isArray(accounts) ? accounts : (accounts?.items ?? [])
    ghostSummary.value = summary
  } catch {
    ghostAccounts.value = []
  }
  accountEntries.value = []
  selectedAccount.value = null
}

async function loadEntries(account: any) {
  selectedAccount.value = account
  try {
    const data = await apiService.getGhostEntries(account.id)
    accountEntries.value = Array.isArray(data) ? data : (data?.items ?? [])
  } catch { accountEntries.value = [] }
}

async function loadStatement(party: any) {
  try {
    ghostStatement.value = await apiService.getGhostStatement(party.id)
    showStatementModal.value = true
  } catch { notification.error('Ekstre yüklenemedi') }
}

function openCreateAccount() {
  if (!selectedParty.value) return
  createAccountForm.value = {
    partyId: selectedParty.value.id,
    currencyId: exchangeStore.currencies[0]?.id ?? '',
    description: '',
  }
  showCreateAccountModal.value = true
}

async function createAccount() {
  saving.value = true
  try {
    await apiService.createGhostAccount({
      PartyId: createAccountForm.value.partyId,
      CurrencyId: createAccountForm.value.currencyId,
      Description: createAccountForm.value.description,
    })
    showCreateAccountModal.value = false
    if (selectedParty.value) await selectParty(selectedParty.value)
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Hesap oluşturulamadı')
  } finally { saving.value = false }
}

function openPayment() {
  if (!selectedParty.value) return
  paymentForm.value = {
    partyId: selectedParty.value.id,
    accountId: selectedAccount.value?.id ?? '',
    currencyId: selectedAccount.value?.currencyId ?? exchangeStore.currencies[0]?.id ?? '',
    amount: null,
    description: '',
    referenceNumber: '',
  }
  showPaymentModal.value = true
}

function openCollection() {
  if (!selectedParty.value) return
  paymentForm.value = {
    partyId: selectedParty.value.id,
    accountId: selectedAccount.value?.id ?? '',
    currencyId: selectedAccount.value?.currencyId ?? exchangeStore.currencies[0]?.id ?? '',
    amount: null,
    description: '',
    referenceNumber: '',
  }
  showCollectionModal.value = true
}

async function submitPayment() {
  if (!paymentForm.value.amount || paymentForm.value.amount <= 0) { notification.warning('Tutar giriniz'); return }
  saving.value = true
  try {
    await apiService.createGhostPayment({
      PartyId: paymentForm.value.partyId,
      CurrencyId: paymentForm.value.currencyId,
      Amount: paymentForm.value.amount,
      Description: paymentForm.value.description,
      ReferenceNumber: paymentForm.value.referenceNumber,
    })
    showPaymentModal.value = false
    if (selectedParty.value) await selectParty(selectedParty.value)
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Ödeme kaydedilemedi')
  } finally { saving.value = false }
}

async function submitCollection() {
  if (!paymentForm.value.amount || paymentForm.value.amount <= 0) { notification.warning('Tutar giriniz'); return }
  saving.value = true
  try {
    await apiService.createGhostCollection({
      PartyId: paymentForm.value.partyId,
      CurrencyId: paymentForm.value.currencyId,
      Amount: paymentForm.value.amount,
      Description: paymentForm.value.description,
      ReferenceNumber: paymentForm.value.referenceNumber,
    })
    showCollectionModal.value = false
    if (selectedParty.value) await selectParty(selectedParty.value)
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Tahsilat kaydedilemedi')
  } finally { saving.value = false }
}

async function reverseEntry(entry: any) {
  if (!confirm('Bu hareketi ters kayıt ile iptal etmek istediğinizden emin misiniz?')) return
  try {
    await apiService.reverseGhostEntry(entry.id)
    if (selectedAccount.value) await loadEntries(selectedAccount.value)
    if (selectedParty.value) await selectParty(selectedParty.value)
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'İptal başarısız')
  }
}

async function toggleAccountBlock(account: any) {
  try {
    if (account.isBlocked || account.status === 'Blocked') {
      await apiService.unblockGhostAccount(account.id)
    } else {
      await apiService.blockGhostAccount(account.id)
    }
    if (selectedParty.value) await selectParty(selectedParty.value)
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'İşlem başarısız')
  }
}

function backToList() {
  activeTab.value = 'parties'
  selectedParty.value = null
  ghostAccounts.value = []
  accountEntries.value = []
  selectedAccount.value = null
  ghostSummary.value = null
}

onMounted(() => {
  if (officeId.value) loadParties()
})
</script>

<template>
  <div class="gp-wrap">
    <div class="gp-header">
      <div class="header-left">
        <button v-if="activeTab === 'accounts'" class="btn-back" @click="backToList">
          <span class="material-symbols-outlined" aria-hidden="true">arrow_back</span>
        </button>
        <h1 v-if="activeTab === 'parties'">Ghost Party</h1>
        <h1 v-else>{{ selectedParty?.name }} <span class="party-code">{{ selectedParty?.partyCode }}</span></h1>
      </div>
      <div class="header-actions" v-if="activeTab === 'accounts' && selectedParty">
        <button class="btn-primary" @click="openPayment">
          <span class="material-symbols-outlined" aria-hidden="true">arrow_upward</span> Ödeme
        </button>
        <button class="btn-secondary" @click="openCollection">
          <span class="material-symbols-outlined" aria-hidden="true">arrow_downward</span> Tahsilat
        </button>
        <button class="btn-secondary" @click="openCreateAccount">
          <span class="material-symbols-outlined" aria-hidden="true">add</span> Hesap Ekle
        </button>
        <button class="btn-secondary" @click="loadStatement(selectedParty)">
          <span class="material-symbols-outlined" aria-hidden="true">receipt_long</span> Ekstre
        </button>
      </div>
    </div>

    <!-- Summary -->
    <div v-if="activeTab === 'accounts' && ghostSummary" class="kpi-grid">
      <AppKpiCard icon="account_circle" label="Hesap Sayısı" :value="ghostAccounts.length" color="#6366f1" bg="#eef2ff" />
      <AppKpiCard icon="arrow_downward" label="Toplam Alacak" :value="formatCurrency(ghostSummary.totalReceivables ?? ghostSummary.totalCredits ?? 0)" color="#059669" bg="#ecfdf5" />
      <AppKpiCard icon="arrow_upward" label="Toplam Borç" :value="formatCurrency(ghostSummary.totalPayables ?? ghostSummary.totalDebits ?? 0)" color="#ef4444" bg="#fef2f2" />
    </div>

    <!-- Search (parties tab) -->
    <div v-if="activeTab === 'parties'" class="search-bar">
      <span class="material-symbols-outlined" aria-hidden="true">search</span>
      <input v-model="searchQuery" placeholder="Cari ara..." />
    </div>

    <div v-if="loading" class="loading-state">
      <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
    </div>

    <!-- Parties List -->
    <div v-else-if="activeTab === 'parties'" class="table-wrap">
      <table class="gp-table">
        <thead>
          <tr>
            <th>Kod</th>
            <th>Ad</th>
            <th>Telefon</th>
            <th class="text-right">Net Bakiye</th>
            <th>Son İşlem</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="filteredParties.length === 0">
            <td colspan="5" class="empty-row">Cari bulunamadı</td>
          </tr>
          <tr v-for="p in filteredParties" :key="p.id" class="clickable-row" @click="selectParty(p)">
            <td class="code-cell">{{ p.partyCode }}</td>
            <td><strong>{{ p.name }}</strong></td>
            <td>{{ p.phone || '-' }}</td>
            <td class="text-right" :class="(p.netBalance ?? 0) >= 0 ? 'balance-pos' : 'balance-neg'">
              {{ formatCurrency(p.netBalance ?? 0) }}
            </td>
            <td>{{ formatDateTime(p.lastTransactionDate) }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Ghost Accounts -->
    <div v-else-if="activeTab === 'accounts'">
      <div class="accounts-grid">
        <div v-for="acc in ghostAccounts" :key="acc.id"
             class="account-card" :class="{ 'account-selected': selectedAccount?.id === acc.id, 'account-blocked': acc.isBlocked }"
             @click="loadEntries(acc)">
          <div class="acc-top">
            <span class="acc-currency">{{ acc.currencyCode }}</span>
            <button v-if="authStore.isAdmin" class="icon-btn-sm" @click.stop="toggleAccountBlock(acc)"
                    :title="acc.isBlocked ? 'Blok Kaldır' : 'Blokla'">
              <span class="material-symbols-outlined" aria-hidden="true">{{ acc.isBlocked ? 'lock_open' : 'lock' }}</span>
            </button>
          </div>
          <div class="acc-balance" :class="(acc.balance ?? 0) >= 0 ? 'balance-pos' : 'balance-neg'">
            {{ formatCurrency(acc.balance ?? 0) }}
          </div>
          <div v-if="acc.isBlocked" class="blocked-badge">Blokeli</div>
        </div>
        <div v-if="ghostAccounts.length === 0" class="empty-accounts">Ghost hesap bulunamadı</div>
      </div>

      <!-- Entries -->
      <div v-if="selectedAccount" class="entries-section">
        <h3>{{ selectedAccount.currencyCode }} Hareketleri</h3>
        <table class="gp-table">
          <thead>
            <tr>
              <th>Tarih</th>
              <th>Açıklama</th>
              <th>Referans</th>
              <th class="text-right">Borç</th>
              <th class="text-right">Alacak</th>
              <th class="text-right">Bakiye</th>
              <th class="text-center">İşlem</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="accountEntries.length === 0">
              <td colspan="7" class="empty-row">Hareket bulunamadı</td>
            </tr>
            <tr v-for="e in accountEntries" :key="e.id" :class="{ 'reversed-row': e.isReversed }">
              <td>{{ formatDateTime(e.entryDate) }}</td>
              <td>{{ e.description || '-' }}</td>
              <td class="code-cell">{{ e.referenceNumber || '-' }}</td>
              <td class="text-right balance-neg">{{ e.debitAmount ? formatCurrency(e.debitAmount) : '' }}</td>
              <td class="text-right balance-pos">{{ e.creditAmount ? formatCurrency(e.creditAmount) : '' }}</td>
              <td class="text-right" :class="(e.runningBalance ?? 0) >= 0 ? 'balance-pos' : 'balance-neg'">
                {{ formatCurrency(e.runningBalance ?? 0) }}
              </td>
              <td class="text-center">
                <button v-if="!e.isReversed" class="icon-btn-sm danger" title="İptal (ters kayıt)" @click="reverseEntry(e)">
                  <span class="material-symbols-outlined" aria-hidden="true">undo</span>
                </button>
                <span v-else class="reversed-label">İptal</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Payment Modal -->
    <div v-if="showPaymentModal" class="modal-overlay" @click.self="showPaymentModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Ghost Ödeme</h2>
          <button class="modal-close" @click="showPaymentModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-stack">
            <div class="form-group">
              <label>Para Birimi</label>
              <select v-model="paymentForm.currencyId">
                <option v-for="c in exchangeStore.currencies" :key="c.id" :value="c.id">{{ c.code }}</option>
              </select>
            </div>
            <div class="form-group">
              <label>Tutar *</label>
              <input v-model.number="paymentForm.amount" type="number" step="0.01" min="0" placeholder="0.00" />
            </div>
            <div class="form-group">
              <label>Referans No</label>
              <input v-model="paymentForm.referenceNumber" placeholder="Referans" />
            </div>
            <div class="form-group">
              <label>Açıklama</label>
              <input v-model="paymentForm.description" placeholder="Açıklama" />
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showPaymentModal = false">İptal</button>
          <button class="btn-primary" :disabled="saving" @click="submitPayment">
            {{ saving ? 'Kaydediliyor...' : 'Ödeme Kaydet' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Collection Modal -->
    <div v-if="showCollectionModal" class="modal-overlay" @click.self="showCollectionModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Ghost Tahsilat</h2>
          <button class="modal-close" @click="showCollectionModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-stack">
            <div class="form-group">
              <label>Para Birimi</label>
              <select v-model="paymentForm.currencyId">
                <option v-for="c in exchangeStore.currencies" :key="c.id" :value="c.id">{{ c.code }}</option>
              </select>
            </div>
            <div class="form-group">
              <label>Tutar *</label>
              <input v-model.number="paymentForm.amount" type="number" step="0.01" min="0" placeholder="0.00" />
            </div>
            <div class="form-group">
              <label>Referans No</label>
              <input v-model="paymentForm.referenceNumber" placeholder="Referans" />
            </div>
            <div class="form-group">
              <label>Açıklama</label>
              <input v-model="paymentForm.description" placeholder="Açıklama" />
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showCollectionModal = false">İptal</button>
          <button class="btn-primary" :disabled="saving" @click="submitCollection">
            {{ saving ? 'Kaydediliyor...' : 'Tahsilat Kaydet' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Create Account Modal -->
    <div v-if="showCreateAccountModal" class="modal-overlay" @click.self="showCreateAccountModal = false">
      <div class="modal modal-sm">
        <div class="modal-header">
          <h2>Ghost Hesap Ekle</h2>
          <button class="modal-close" @click="showCreateAccountModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-stack">
            <div class="form-group">
              <label>Para Birimi</label>
              <select v-model="createAccountForm.currencyId">
                <option v-for="c in exchangeStore.currencies" :key="c.id" :value="c.id">{{ c.code }} - {{ c.name }}</option>
              </select>
            </div>
            <div class="form-group">
              <label>Açıklama</label>
              <input v-model="createAccountForm.description" placeholder="Opsiyonel" />
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showCreateAccountModal = false">İptal</button>
          <button class="btn-primary" :disabled="saving" @click="createAccount">
            {{ saving ? 'Oluşturuluyor...' : 'Hesap Oluştur' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Statement Modal -->
    <div v-if="showStatementModal && ghostStatement" class="modal-overlay" @click.self="showStatementModal = false">
      <div class="modal modal-xl">
        <div class="modal-header">
          <h2>Ghost Ekstre</h2>
          <button class="modal-close" @click="showStatementModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="stmt-summary">
            <span>Açılış: <strong>{{ formatCurrency(ghostStatement.openingBalance ?? 0) }}</strong></span>
            <span>Borç: <strong class="balance-neg">{{ formatCurrency(ghostStatement.totalDebits ?? 0) }}</strong></span>
            <span>Alacak: <strong class="balance-pos">{{ formatCurrency(ghostStatement.totalCredits ?? 0) }}</strong></span>
            <span>Kapanış: <strong>{{ formatCurrency(ghostStatement.closingBalance ?? 0) }}</strong></span>
          </div>
          <table class="gp-table">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Açıklama</th>
                <th class="text-right">Borç</th>
                <th class="text-right">Alacak</th>
                <th class="text-right">Bakiye</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(line, i) in (ghostStatement.lines ?? ghostStatement.entries ?? [])" :key="i">
                <td>{{ formatDateTime(line.date ?? line.entryDate) }}</td>
                <td>{{ line.description || '-' }}</td>
                <td class="text-right balance-neg">{{ line.debit ?? line.debitAmount ? formatCurrency(line.debit ?? line.debitAmount) : '' }}</td>
                <td class="text-right balance-pos">{{ line.credit ?? line.creditAmount ? formatCurrency(line.credit ?? line.creditAmount) : '' }}</td>
                <td class="text-right">{{ formatCurrency(line.balance ?? line.runningBalance ?? 0) }}</td>
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
.gp-wrap { padding: 24px; max-width: 1400px; margin: 0 auto; }
.gp-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
.header-left { display: flex; align-items: center; gap: 12px; }
.header-left h1 { font-size: 22px; font-weight: 700; color: #1a1a2e; margin: 0; }
.party-code { font-size: 13px; font-weight: 400; color: #6b7280; margin-left: 8px; }
.header-actions { display: flex; gap: 8px; flex-wrap: wrap; }
.btn-back {
  background: none; border: 1px solid #e5e7eb; border-radius: 8px;
  padding: 6px 8px; cursor: pointer; color: #374151; display: flex; align-items: center;
}
.btn-back:hover { background: #f3f4f6; }

.btn-primary {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 16px; background: #2563eb; color: #fff;
  border: none; border-radius: 8px; font-size: 13px; font-weight: 600; cursor: pointer;
}
.btn-primary:hover { background: #1d4ed8; }
.btn-primary:disabled { opacity: .6; cursor: not-allowed; }
.btn-secondary {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 16px; background: #fff; color: #374151;
  border: 1px solid #d1d5db; border-radius: 8px; font-size: 13px; font-weight: 500; cursor: pointer;
}
.btn-secondary:hover { background: #f9fafb; }
.btn-cancel {
  padding: 8px 16px; background: #fff; color: #374151;
  border: 1px solid #d1d5db; border-radius: 8px; font-size: 13px; cursor: pointer;
}
.btn-cancel:hover { background: #f3f4f6; }

.kpi-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin-bottom: 20px; }

.search-bar {
  display: flex; align-items: center; gap: 8px;
  background: #fff; border: 1px solid #d1d5db; border-radius: 8px;
  padding: 6px 12px; margin-bottom: 16px; max-width: 400px;
}
.search-bar input { border: none; outline: none; font-size: 13px; width: 100%; background: transparent; }
.search-bar .material-symbols-outlined { font-size: 18px; color: #9ca3af; }

.loading-state { text-align: center; padding: 60px 20px; color: #6b7280; font-size: 14px; }
.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

.table-wrap { background: #fff; border: 1px solid #e5e7eb; border-radius: 12px; overflow-x: auto; }
.gp-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.gp-table th {
  padding: 10px 14px; text-align: left; font-weight: 600; color: #6b7280;
  font-size: 11px; text-transform: uppercase; letter-spacing: .5px;
  border-bottom: 1px solid #e5e7eb; background: #f9fafb; white-space: nowrap;
}
.gp-table td { padding: 10px 14px; border-bottom: 1px solid #f3f4f6; color: #374151; }
.text-right { text-align: right; }
.text-center { text-align: center; }
.empty-row { text-align: center; color: #9ca3af; padding: 40px 14px !important; }
.clickable-row { cursor: pointer; }
.clickable-row:hover { background: #f0f7ff; }
.code-cell { font-family: 'Consolas', monospace; font-size: 12px; color: #6b7280; }
.balance-pos { color: #059669; font-weight: 600; }
.balance-neg { color: #dc2626; font-weight: 600; }

.accounts-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 12px; margin-bottom: 20px; }
.account-card {
  background: #fff; border: 1px solid #e5e7eb; border-radius: 10px; padding: 16px;
  cursor: pointer; transition: border-color .15s;
}
.account-card:hover { border-color: #93c5fd; }
.account-selected { border-color: #2563eb; box-shadow: 0 0 0 2px rgba(37,99,235,.15); }
.account-blocked { opacity: .6; border-color: #fca5a5; }
.acc-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
.acc-currency { font-weight: 700; font-size: 16px; color: #1a1a2e; }
.acc-balance { font-size: 22px; font-weight: 700; }
.blocked-badge { font-size: 10px; color: #991b1b; background: #fee2e2; padding: 2px 8px; border-radius: 8px; margin-top: 6px; display: inline-block; }
.empty-accounts { grid-column: 1 / -1; text-align: center; color: #9ca3af; padding: 40px; }

.icon-btn-sm {
  background: none; border: none; cursor: pointer; padding: 2px; border-radius: 4px; color: #6b7280;
}
.icon-btn-sm:hover { color: #2563eb; }
.icon-btn-sm.danger:hover { color: #dc2626; }
.icon-btn-sm .material-symbols-outlined { font-size: 16px; }
.reversed-row { opacity: .5; text-decoration: line-through; }
.reversed-label { font-size: 10px; color: #9ca3af; }

.entries-section {
  background: #fff; border: 1px solid #e5e7eb; border-radius: 12px; overflow-x: auto;
}
.entries-section h3 {
  font-size: 15px; font-weight: 600; color: #1a1a2e; padding: 16px 16px 0; margin: 0;
}

.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.4);
  display: flex; align-items: center; justify-content: center; z-index: 1000; padding: 20px;
}
.modal {
  background: #fff; border-radius: 16px; width: 100%; max-width: 520px;
  max-height: 90vh; display: flex; flex-direction: column;
  box-shadow: 0 20px 60px rgba(0,0,0,.15);
}
.modal-sm { max-width: 400px; }
.modal-xl { max-width: 900px; }
.modal-header {
  display: flex; justify-content: space-between; align-items: center;
  padding: 20px 24px; border-bottom: 1px solid #e5e7eb;
}
.modal-header h2 { font-size: 17px; font-weight: 600; color: #1a1a2e; margin: 0; }
.modal-close { background: none; border: none; font-size: 22px; cursor: pointer; color: #6b7280; }
.modal-body { padding: 24px; overflow-y: auto; }
.modal-footer { padding: 16px 24px; border-top: 1px solid #e5e7eb; display: flex; justify-content: flex-end; gap: 8px; }

.form-stack { display: flex; flex-direction: column; gap: 14px; }
.form-group { display: flex; flex-direction: column; gap: 4px; }
.form-group label { font-size: 12px; font-weight: 500; color: #374151; }
.form-group input, .form-group select {
  padding: 8px 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 13px; outline: none;
}
.form-group input:focus, .form-group select:focus {
  border-color: #2563eb; box-shadow: 0 0 0 2px rgba(37,99,235,.1);
}

.stmt-summary {
  display: flex; gap: 20px; flex-wrap: wrap; font-size: 13px; color: #6b7280; margin-bottom: 16px;
}

@media (max-width: 768px) {
  .gp-wrap { padding: 12px; }
  .gp-header { flex-direction: column; gap: 12px; align-items: flex-start; }
  .accounts-grid { grid-template-columns: 1fr 1fr; }
}
</style>
