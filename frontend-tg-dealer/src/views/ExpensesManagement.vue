<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import AppKpiCard from '@/components/common/AppKpiCard.vue'
import AppPageHeader from '@/components/common/AppPageHeader.vue'
import AppEmptyState from '@/components/common/AppEmptyState.vue'

const authStore = useAuthStore()
const exchangeStore = useExchangeStore()

const loading = ref(true)
const saving = ref(false)
const error = ref('')

// Data
const definitions = ref<any[]>([])
const payments = ref<any[]>([])
const currencies = ref<any[]>([])

// Filters
const filterCategory = ref<number | ''>('')
const filterDateFrom = ref('')
const filterDateTo = ref('')

// Modal
const showDefModal = ref(false)
const showPayModal = ref(false)
const editDef = ref<any>(null)

const defForm = ref({
  code: '',
  name: '',
  category: 10,
  description: '',
  isActive: true,
  isRecurring: false,
  recurrencePeriod: null as number | null,
  defaultAmount: null as number | null,
  defaultCurrencyId: null as string | null,
})

const payForm = ref({
  expenseDefinitionId: '',
  currencyId: '',
  paymentDate: new Date().toISOString().slice(0, 10),
  amount: null as number | null,
  paymentMethod: 1,
  referenceNumber: '',
  description: '',
})

const CATEGORIES: Record<number, string> = {
  1: 'Maaş', 2: 'Kira', 3: 'Fatura', 4: 'Ofis', 5: 'Pazarlama',
  6: 'Seyahat', 7: 'Sigorta', 8: 'Vergi', 9: 'Bakım', 10: 'Diğer',
}

const PAYMENT_METHODS: Record<number, string> = {
  1: 'Nakit', 2: 'Banka Transferi', 3: 'Kredi Kartı', 4: 'Çek', 5: 'Diğer',
}

const officeId = computed(() =>
  exchangeStore.selectedOffice?.officeId ?? exchangeStore.offices[0]?.officeId
)

const filteredPayments = computed(() => {
  let list = payments.value
  if (filterCategory.value !== '') {
    list = list.filter(p => p.category === filterCategory.value)
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
  filteredPayments.value.reduce((sum, p) => sum + (p.amount || 0), 0)
)

const categoryBreakdown = computed(() => {
  const map: Record<string, number> = {}
  filteredPayments.value.forEach(p => {
    const cat = p.categoryName || CATEGORIES[p.category] || 'Diğer'
    map[cat] = (map[cat] || 0) + (p.amount || 0)
  })
  return Object.entries(map).sort((a, b) => b[1] - a[1])
})

function fmt(n: number): string {
  return new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n)
}

function fmtDate(iso: string): string {
  if (!iso) return '-'
  return new Date(iso).toLocaleDateString('tr-TR')
}

// Load
async function loadAll() {
  loading.value = true
  error.value = ''
  try {
    const [defs, pays, curs] = await Promise.all([
      apiService.getExpenseDefinitions(officeId.value),
      apiService.getExpensePayments({ officeId: officeId.value }),
      apiService.getCurrencies(),
    ])
    definitions.value = Array.isArray(defs) ? defs : (defs?.items ?? [])
    payments.value = Array.isArray(pays) ? pays : (pays?.items ?? [])
    currencies.value = Array.isArray(curs) ? curs : (curs?.items ?? [])
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Veriler yüklenemedi'
  } finally {
    loading.value = false
  }
}

onMounted(loadAll)

watch(() => exchangeStore.selectedOffice, () => {
  if (officeId.value) loadAll()
})

// Definition CRUD
function openCreateDef() {
  editDef.value = null
  defForm.value = { code: '', name: '', category: 10, description: '', isActive: true, isRecurring: false, recurrencePeriod: null, defaultAmount: null, defaultCurrencyId: null }
  showDefModal.value = true
}

function openEditDef(d: any) {
  editDef.value = d
  defForm.value = {
    code: d.code || '', name: d.name, category: d.category ?? 10,
    description: d.description || '', isActive: d.isActive !== false,
    isRecurring: d.isRecurring || false, recurrencePeriod: d.recurrencePeriod ?? null,
    defaultAmount: d.defaultAmount ?? null, defaultCurrencyId: d.defaultCurrencyId ?? null,
  }
  showDefModal.value = true
}

async function saveDef() {
  if (!defForm.value.name.trim()) return
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
    error.value = e?.response?.data?.error || e.message || 'Kayıt başarısız'
  } finally {
    saving.value = false
  }
}

async function deleteDef(d: any) {
  if (!confirm(`"${d.name}" tanımını silmek istediğinize emin misiniz?`)) return
  try {
    await apiService.deleteExpenseDefinition(d.id)
    await loadAll()
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Silme başarısız'
  }
}

// Payment CRUD
function openCreatePay(defId?: string) {
  payForm.value = {
    expenseDefinitionId: defId || '',
    currencyId: currencies.value[0]?.id || currencies.value[0]?.currencyId || '',
    paymentDate: new Date().toISOString().slice(0, 10),
    amount: null, paymentMethod: 1, referenceNumber: '', description: '',
  }
  showPayModal.value = true
}

async function savePay() {
  if (!payForm.value.expenseDefinitionId || !payForm.value.amount) return
  saving.value = true
  try {
    await apiService.createExpensePayment(payForm.value)
    showPayModal.value = false
    await loadAll()
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Ödeme kaydı başarısız'
  } finally {
    saving.value = false
  }
}

async function deletePay(p: any) {
  const reason = prompt('Silme nedeni:')
  if (reason === null) return
  try {
    await apiService.deleteExpensePayment(p.id, reason)
    await loadAll()
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Silme başarısız'
  }
}

const activeTab = ref<'payments' | 'definitions'>('payments')
</script>

<template>
  <div class="exp-wrap">
    <!-- Header -->
    <AppPageHeader icon="payments" title="Gider Yönetimi" subtitle="Gider tanımları ve ödemelerinizi yönetin">
      <button class="exp-btn secondary" @click="openCreateDef">
        <span class="material-symbols-outlined" aria-hidden="true">category</span>Yeni Tanım
      </button>
      <button class="exp-btn primary" @click="openCreatePay()">
        <span class="material-symbols-outlined" aria-hidden="true">add_circle</span>Yeni Ödeme
      </button>
    </AppPageHeader>

    <!-- KPIs -->
    <div class="kpi-grid">
      <AppKpiCard icon="payments" label="Toplam Gider" :value="'₺' + fmt(totalExpense)" color="#6366f1" bg="#eef2ff" />
      <AppKpiCard icon="receipt_long" label="Ödeme Sayısı" :value="filteredPayments.length" color="#8b5cf6" bg="#f5f3ff" />
      <AppKpiCard icon="category" label="Tanım Sayısı" :value="definitions.length" color="#059669" bg="#ecfdf5" />
      <AppKpiCard icon="trending_up" label="En Yüksek Kategori" :value="categoryBreakdown[0]?.[0] || '-'" color="#d97706" bg="#fffbeb" />
    </div>

    <!-- Filters -->
    <div class="filter-bar">
      <div class="filter-group">
        <label>Kategori</label>
        <select v-model="filterCategory" class="filter-input">
          <option value="">Tümü</option>
          <option v-for="(label, key) in CATEGORIES" :key="key" :value="Number(key)">{{ label }}</option>
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

    <!-- Tabs -->
    <div class="tab-bar">
      <button class="tab-btn" :class="{ active: activeTab === 'payments' }" @click="activeTab = 'payments'">
        <span class="material-symbols-outlined" aria-hidden="true">receipt_long</span>Ödemeler
      </button>
      <button class="tab-btn" :class="{ active: activeTab === 'definitions' }" @click="activeTab = 'definitions'">
        <span class="material-symbols-outlined" aria-hidden="true">category</span>Gider Tanımları
      </button>
    </div>

    <!-- Loading / Error -->
    <div v-if="loading" class="exp-loading"><div class="spinner"></div>Yükleniyor...</div>
    <div v-else-if="error" class="exp-error">
      <span class="material-symbols-outlined" aria-hidden="true">error</span>{{ error }}
      <button class="exp-btn secondary" @click="loadAll" style="margin-left:12px">Tekrar Dene</button>
    </div>

    <!-- Payments Table -->
    <div v-else-if="activeTab === 'payments'" class="table-wrap">
      <table class="exp-table" v-if="filteredPayments.length">
        <thead>
          <tr>
            <th>Tarih</th>
            <th>Gider Tanımı</th>
            <th>Kategori</th>
            <th>Tutar</th>
            <th>Para Birimi</th>
            <th>Ödeme Yöntemi</th>
            <th>Açıklama</th>
            <th>Oluşturan</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in filteredPayments" :key="p.id" :class="{ deleted: p.isDeleted }">
            <td>{{ fmtDate(p.paymentDate) }}</td>
            <td class="fw600">{{ p.expenseDefinitionName || '-' }}</td>
            <td><span class="cat-badge" :data-cat="p.category">{{ p.categoryName || CATEGORIES[p.category] || '-' }}</span></td>
            <td class="amount">{{ fmt(p.amount) }}</td>
            <td>{{ p.currencyCode || '-' }}</td>
            <td>{{ p.paymentMethodName || PAYMENT_METHODS[p.paymentMethod] || '-' }}</td>
            <td class="desc-cell">{{ p.description || '-' }}</td>
            <td class="muted">{{ p.createdByUserName || '-' }}</td>
            <td>
              <button v-if="!p.isDeleted && authStore.isAdmin" class="icon-btn danger" @click="deletePay(p)" title="Sil">
                <span class="material-symbols-outlined" aria-hidden="true">delete</span>
              </button>
              <span v-if="p.isDeleted" class="deleted-tag">İptal</span>
            </td>
          </tr>
        </tbody>
      </table>
      <AppEmptyState v-else icon="receipt_long" message="Henüz gider ödemesi bulunmuyor.">
        <button class="exp-btn primary" @click="openCreatePay()">İlk Ödemeyi Ekle</button>
      </AppEmptyState>

      <!-- Category breakdown -->
      <div v-if="categoryBreakdown.length" class="breakdown">
        <h3>Kategori Dağılımı</h3>
        <div class="breakdown-list">
          <div v-for="[cat, total] in categoryBreakdown" :key="cat" class="breakdown-row">
            <span class="breakdown-label">{{ cat }}</span>
            <div class="breakdown-bar-wrap">
              <div class="breakdown-bar" :style="{ width: Math.min(100, (total / totalExpense) * 100) + '%' }"></div>
            </div>
            <span class="breakdown-val">₺{{ fmt(total) }}</span>
          </div>
        </div>
      </div>
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
          <tr v-for="d in definitions" :key="d.id">
            <td class="mono">{{ d.code }}</td>
            <td class="fw600">{{ d.name }}</td>
            <td><span class="cat-badge" :data-cat="d.category">{{ d.categoryName || CATEGORIES[d.category] || '-' }}</span></td>
            <td>{{ d.defaultAmount ? fmt(d.defaultAmount) : '-' }}</td>
            <td>
              <span v-if="d.isRecurring" class="recurring-badge">{{ d.recurrencePeriodName || 'Evet' }}</span>
              <span v-else class="muted">Hayır</span>
            </td>
            <td>
              <span class="status-dot" :class="d.isActive ? 'active' : 'inactive'"></span>
              {{ d.isActive ? 'Aktif' : 'Pasif' }}
            </td>
            <td>{{ d.paymentCount ?? 0 }} adet / ₺{{ fmt(d.totalPayments ?? 0) }}</td>
            <td class="action-cell">
              <button class="icon-btn" @click="openCreatePay(d.id)" title="Ödeme Ekle">
                <span class="material-symbols-outlined" aria-hidden="true">add_circle</span>
              </button>
              <button class="icon-btn" @click="openEditDef(d)" title="Düzenle">
                <span class="material-symbols-outlined" aria-hidden="true">edit</span>
              </button>
              <button v-if="authStore.isAdmin" class="icon-btn danger" @click="deleteDef(d)" title="Sil">
                <span class="material-symbols-outlined" aria-hidden="true">delete</span>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
      <AppEmptyState v-else icon="category" message="Henüz gider tanımı bulunmuyor.">
        <button class="exp-btn primary" @click="openCreateDef">İlk Tanımı Ekle</button>
      </AppEmptyState>
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
              <label>Kategori</label>
              <select v-model="defForm.category" class="form-input">
                <option v-for="(label, key) in CATEGORIES" :key="key" :value="Number(key)">{{ label }}</option>
              </select>
            </div>
            <div class="form-group">
              <label>Varsayılan Tutar</label>
              <input v-model.number="defForm.defaultAmount" type="number" step="0.01" class="form-input" placeholder="0.00" />
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

    <!-- Payment Modal -->
    <div v-if="showPayModal" class="modal-overlay" @click.self="showPayModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Yeni Gider Ödemesi</h2>
          <button class="icon-btn" @click="showPayModal = false"><span class="material-symbols-outlined" aria-hidden="true">close</span></button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Gider Tanımı *</label>
            <select v-model="payForm.expenseDefinitionId" class="form-input">
              <option value="">Seçiniz</option>
              <option v-for="d in definitions.filter(x => x.isActive !== false)" :key="d.id" :value="d.id">{{ d.name }}</option>
            </select>
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
.exp-btn.secondary { background: #f3f4f6; color: #374151; }
.exp-btn.secondary:hover { background: #e5e7eb; }
.exp-btn.primary:not(:disabled) { box-shadow: var(--shadow-glow-primary); }

/* KPI */
.kpi-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-bottom: 16px; }

/* Filters */
.filter-bar { display: flex; gap: 12px; margin-bottom: 12px; flex-wrap: wrap; }
.filter-group { display: flex; flex-direction: column; gap: 4px; }
.filter-group label { font-size: 11px; color: #6b7280; font-weight: 600; }
.filter-input {
  padding: 7px 10px; border: 1px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 13px; color: #111; background: var(--color-bg-card); min-width: 140px;
}
.filter-input:focus { border-color: var(--color-primary); outline: none; box-shadow: 0 0 0 2px rgba(99,102,241,.15); }

/* Tabs */
.tab-bar { display: flex; gap: 4px; margin-bottom: 16px; border-bottom: 1px solid var(--color-border); }
.tab-btn {
  display: flex; align-items: center; gap: 6px;
  padding: 10px 16px; border: none; background: transparent;
  font-size: 13px; font-weight: 500; color: #6b7280;
  cursor: pointer; border-bottom: 2px solid transparent; margin-bottom: -1px; transition: color 0.2s, border-color 0.2s;
}
.tab-btn .material-symbols-outlined { font-size: 18px; }
.tab-btn.active { color: var(--color-primary); border-bottom-color: var(--color-primary); font-weight: 600; }
.tab-btn:hover { color: #374151; }

/* Table */
.table-wrap { overflow-x: auto; background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-lg); box-shadow: var(--shadow-md); padding: 4px; }
.exp-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.exp-table th {
  text-align: left; padding: 10px 12px; font-size: 11px; font-weight: 700;
  color: #6b7280; text-transform: uppercase; letter-spacing: .4px;
  border-bottom: 2px solid var(--border-strong); background: #f9fafb;
}
.exp-table td { padding: 10px 12px; border-bottom: 1px solid #f3f4f6; color: #374151; }
.exp-table tr:hover { background: #f9fafb; }
.exp-table tr.deleted { opacity: .5; }
.fw600 { font-weight: 600; color: #111; }
.mono { font-family: monospace; font-size: 12px; color: var(--color-primary); }
.amount { font-weight: 700; color: #111; font-variant-numeric: tabular-nums; }
.muted { color: #9ca3af; font-size: 12px; }
.desc-cell { max-width: 180px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

/* Badges */
.cat-badge {
  display: inline-block; padding: 2px 8px; border-radius: var(--radius-sm);
  font-size: 11px; font-weight: 600; background: #f3f4f6; color: #374151;
}
.recurring-badge {
  display: inline-block; padding: 2px 8px; border-radius: var(--radius-sm);
  font-size: 11px; font-weight: 600; background: #ede9fe; color: #7c3aed;
}
.deleted-tag {
  display: inline-block; padding: 2px 6px; border-radius: var(--radius-sm);
  font-size: 10px; font-weight: 700; background: #fef2f2; color: var(--color-danger);
}
.status-dot {
  display: inline-block; width: 7px; height: 7px; border-radius: 50%; margin-right: 4px;
}
.status-dot.active { background: #22c55e; }
.status-dot.inactive { background: #d1d5db; }

/* Action buttons */
.action-cell { display: flex; gap: 4px; }
.icon-btn {
  width: 30px; height: 30px; display: flex; align-items: center; justify-content: center;
  border: none; background: transparent; border-radius: var(--radius-sm); cursor: pointer; color: #6b7280;
}
.icon-btn:hover { background: #f3f4f6; color: #111; }
.icon-btn.danger:hover { background: #fef2f2; color: var(--color-danger); }
.icon-btn .material-symbols-outlined { font-size: 18px; }

/* Breakdown */
.breakdown { margin-top: 20px; padding: 16px; background: var(--color-bg-card); border: 1px solid var(--color-border); border-radius: var(--radius-lg); box-shadow: var(--shadow-bold); }
.breakdown h3 {
  font-size: 13px; font-weight: 700; color: #111; margin: 0 0 12px;
  position: relative; padding: 4px 0 8px 12px;
  background: linear-gradient(90deg, rgba(99,102,241,.08), transparent);
  border-bottom: 3px solid var(--color-border);
}
.breakdown h3::before {
  content: ''; position: absolute; left: 0; top: 0; bottom: 8px; width: 5px;
  background: var(--color-primary); border-radius: 3px;
}
.breakdown-list { display: flex; flex-direction: column; gap: 8px; }
.breakdown-row { display: flex; align-items: center; gap: 10px; }
.breakdown-label { font-size: 12px; color: #374151; min-width: 100px; font-weight: 500; }
.breakdown-bar-wrap { flex: 1; height: 8px; background: #f3f4f6; border-radius: var(--radius-sm); overflow: hidden; }
.breakdown-bar { height: 100%; background: linear-gradient(90deg, #6366f1, #8b5cf6); border-radius: var(--radius-sm); transition: width .3s; }
.breakdown-val { font-size: 12px; font-weight: 700; color: #111; min-width: 90px; text-align: right; }

/* Loading / Error */
.exp-loading { display: flex; align-items: center; justify-content: center; gap: 10px; padding: 48px; color: #6b7280; font-size: 14px; }
.spinner { width: 20px; height: 20px; border: 2px solid #e5e7eb; border-top-color: var(--color-primary); border-radius: 50%; animation: spin .6s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
.exp-error { display: flex; align-items: center; gap: 8px; padding: 16px; background: #fef2f2; color: var(--color-danger); border-radius: var(--radius-md); font-size: 13px; }
.exp-error .material-symbols-outlined { font-size: 20px; }

/* Modal */
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.4); z-index: 200;
  display: flex; align-items: center; justify-content: center;
  backdrop-filter: blur(2px);
}
.modal {
  background: var(--color-bg-card); border-radius: var(--radius-lg); width: 520px; max-width: 95vw;
  max-height: 90vh; overflow-y: auto; box-shadow: 0 20px 60px rgba(0,0,0,.2);
}
.modal-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 18px 20px; border-bottom: 1px solid #f3f4f6;
}
.modal-header h2 { font-size: 16px; font-weight: 700; color: #111; margin: 0; }
.modal-body { padding: 20px; display: flex; flex-direction: column; gap: 14px; }
.modal-footer {
  display: flex; justify-content: flex-end; gap: 8px;
  padding: 14px 20px; border-top: 1px solid #f3f4f6;
}

/* Form */
.form-row { display: flex; gap: 12px; }
.form-row > * { flex: 1; }
.form-group { display: flex; flex-direction: column; gap: 4px; }
.form-group label { font-size: 12px; font-weight: 600; color: #374151; }
.form-input {
  padding: 8px 10px; border: 1px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 13px; color: #111; background: var(--color-bg-card); width: 100%;
}
.form-input:focus { border-color: var(--color-primary); outline: none; box-shadow: 0 0 0 2px rgba(99,102,241,.15); }
textarea.form-input { resize: vertical; }
.checkbox-label {
  display: flex; align-items: center; gap: 6px; font-size: 13px; color: #374151; cursor: pointer;
}

/* Responsive */
@media (max-width: 768px) {
  .kpi-grid { grid-template-columns: repeat(2, 1fr); }
  .filter-bar { flex-direction: column; }
  .form-row { flex-direction: column; }
}
</style>
