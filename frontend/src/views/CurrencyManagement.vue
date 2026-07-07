<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import apiService from '@/services/apiservice'
import AppKpiCard from '@/components/common/AppKpiCard.vue'
import AppPageHeader from '@/components/common/AppPageHeader.vue'

interface Currency {
  id: string
  createdDate: string
  currencyCode: string
  currencyName: string
  currencySymbol?: string
}

interface CurrencyMeta {
  flagImg: string
  country: string
  symbol: string
  region: string
  status: 'active' | 'discontinued'
}

const CURRENCY_META: Record<string, CurrencyMeta> = {
  USD:  { flagImg: '/flags/us.png', country: 'Amerika Birleşik Devletleri', symbol: '$',   region: 'Kuzey Amerika', status: 'active' },
  EUR:  { flagImg: '/flags/eu.png', country: 'Avrupa Birliği',              symbol: '€',   region: 'Avrupa',        status: 'active' },
  TRY:  { flagImg: '/flags/tr.png', country: 'Türkiye',                     symbol: '₺',   region: 'Avrupa/Asya',   status: 'active' },
  GBP:  { flagImg: '/flags/gb.png', country: 'Birleşik Krallık',            symbol: '£',   region: 'Avrupa',        status: 'active' },
  RUB:  { flagImg: '/flags/ru.png', country: 'Rusya',                       symbol: '₽',   region: 'Avrupa/Asya',   status: 'active' },
  KRUB: { flagImg: '/flags/ru.png', country: 'Rusya (Kart Ruble)',          symbol: '₽',   region: 'Avrupa/Asya',   status: 'active' },
  AED:  { flagImg: '/flags/ae.png', country: 'Birleşik Arap Emirlikleri',   symbol: 'د.إ', region: 'Orta Doğu',     status: 'active' },
  NOK:  { flagImg: '/flags/no.png', country: 'Norveç',                      symbol: 'kr',  region: 'Avrupa',        status: 'active' },
  DKK:  { flagImg: '/flags/dk.png', country: 'Danimarka',                   symbol: 'kr',  region: 'Avrupa',        status: 'active' },
  UAH:  { flagImg: '/flags/ua.png', country: 'Ukrayna',                     symbol: '₴',   region: 'Avrupa',        status: 'active' },
  AZN:  { flagImg: '/flags/az.png', country: 'Azerbaycan',                  symbol: '₼',   region: 'Kafkasya',      status: 'active' },
  AUD:  { flagImg: '/flags/au.png', country: 'Avustralya',                  symbol: 'A$',  region: 'Okyanusya',     status: 'active' },
  CAD:  { flagImg: '/flags/ca.png', country: 'Kanada',                      symbol: 'C$',  region: 'Kuzey Amerika', status: 'active' },
  HKD:  { flagImg: '/flags/hk.png', country: 'Hong Kong',                   symbol: 'HK$', region: 'Asya',          status: 'active' },
  KZT:  { flagImg: '/flags/kz.png', country: 'Kazakistan',                  symbol: '₸',   region: 'Orta Asya',     status: 'active' },
  KGS:  { flagImg: '/flags/kg.png', country: 'Kırgızistan',                 symbol: 'сом', region: 'Orta Asya',     status: 'active' },
  USDT: { flagImg: '',              country: 'Tether (Kripto)',              symbol: '₮',   region: 'Dijital',       status: 'active' },
  MGBP: { flagImg: '/flags/gb.png', country: 'Birleşik Krallık (Metal)',    symbol: '£',   region: 'Avrupa',        status: 'active' },
  CHF:  { flagImg: '/flags/ch.png', country: 'İsviçre',                     symbol: 'Fr',  region: 'Avrupa',        status: 'active' },
  JPY:  { flagImg: '/flags/jp.png', country: 'Japonya',                     symbol: '¥',   region: 'Asya',          status: 'active' },
  CNY:  { flagImg: '/flags/cn.png', country: 'Çin',                         symbol: '¥',   region: 'Asya',          status: 'active' },
  SEK:  { flagImg: '/flags/se.png', country: 'İsveç',                       symbol: 'kr',  region: 'Avrupa',        status: 'active' },
  PLN:  { flagImg: '/flags/pl.png', country: 'Polonya',                     symbol: 'zł',  region: 'Avrupa',        status: 'active' },
  GEL:  { flagImg: '/flags/ge.png', country: 'Gürcistan',                   symbol: '₾',   region: 'Kafkasya',      status: 'active' },
  INR:  { flagImg: '/flags/in.png', country: 'Hindistan',                   symbol: '₹',   region: 'Asya',          status: 'active' },
  BRL:  { flagImg: '/flags/br.png', country: 'Brezilya',                    symbol: 'R$',  region: 'Güney Amerika', status: 'active' },
  ZAR:  { flagImg: '/flags/za.png', country: 'Güney Afrika',                symbol: 'R',   region: 'Afrika',        status: 'active' },
  SAR:  { flagImg: '/flags/sa.png', country: 'Suudi Arabistan',             symbol: '﷼',   region: 'Orta Doğu',     status: 'active' },
  QAR:  { flagImg: '/flags/qa.png', country: 'Katar',                       symbol: '﷼',   region: 'Orta Doğu',     status: 'active' },
  KWD:  { flagImg: '/flags/kw.png', country: 'Kuveyt',                      symbol: 'د.ك', region: 'Orta Doğu',     status: 'active' },
  BGN:  { flagImg: '/flags/bg.png', country: 'Bulgaristan',                  symbol: 'лв',  region: 'Avrupa',        status: 'active' },
  RON:  { flagImg: '/flags/ro.png', country: 'Romanya',                     symbol: 'lei', region: 'Avrupa',        status: 'active' },
  ILS:  { flagImg: '/flags/il.png', country: 'İsrail',                      symbol: '₪',   region: 'Orta Doğu',     status: 'active' },
  CZK:  { flagImg: '/flags/cz.png', country: 'Çekya',                       symbol: 'Kč',  region: 'Avrupa',        status: 'active' },
  MEUR: { flagImg: '/flags/eu.png', country: 'Avrupa Birliği (Metal)',       symbol: '€',   region: 'Avrupa',        status: 'active' },
}

const DEFAULT_META: CurrencyMeta = { flagImg: '', country: 'Bilinmeyen', symbol: '—', region: 'Diğer', status: 'active' }

function getMeta(code: string) {
  return CURRENCY_META[code?.toUpperCase()] ?? DEFAULT_META
}

const currencies      = ref<Currency[]>([])
const loading         = ref(false)
const saving          = ref(false)
const editingCurrency = ref<Currency | null>(null)
const showForm        = ref(false)
const toast           = ref<{ msg: string; type: 'success' | 'error' } | null>(null)
const deleteTarget    = ref<Currency | null>(null)
const deleting        = ref(false)
const expandedId      = ref<string | null>(null)

const form = ref({ currencyCode: '', currencyName: '', currencySymbol: '' })

const formPreview = computed(() => {
  const code = form.value.currencyCode?.toUpperCase()
  if (!code || code.length < 2) return null
  return CURRENCY_META[code] ?? null
})

const activeCount = computed(() =>
  currencies.value.filter(c => getMeta(c.currencyCode).status === 'active').length
)
const discontinuedCount = computed(() =>
  currencies.value.filter(c => getMeta(c.currencyCode).status === 'discontinued').length
)
const regionCount = computed(() =>
  new Set(currencies.value.map(c => getMeta(c.currencyCode).region)).size
)

function showToast(msg: string, type: 'success' | 'error' = 'success') {
  toast.value = { msg, type }
  setTimeout(() => { toast.value = null }, 3500)
}

const loadCurrencies = async () => {
  loading.value = true
  try {
    currencies.value = await apiService.getCurrencies() ?? []
  } catch {
    showToast('Para birimleri yüklenemedi', 'error')
  } finally {
    loading.value = false
  }
}

const openCreate = () => {
  editingCurrency.value = null
  form.value = { currencyCode: '', currencyName: '', currencySymbol: '' }
  showForm.value = true
}

const openEdit = (c: Currency) => {
  editingCurrency.value = c
  form.value = { currencyCode: c.currencyCode, currencyName: c.currencyName, currencySymbol: c.currencySymbol ?? '' }
  showForm.value = true
}

const cancelForm = () => { showForm.value = false }

const saveCurrency = async () => {
  saving.value = true
  try {
    const payload: any = {
      currencyCode:   form.value.currencyCode.toUpperCase(),
      currencyName:   form.value.currencyName,
      currencySymbol: form.value.currencySymbol || form.value.currencyCode.toUpperCase(),
    }
    if (editingCurrency.value) payload.id = editingCurrency.value.id
    await apiService.saveCurrency(payload)
    showToast(editingCurrency.value ? 'Para birimi güncellendi' : 'Para birimi eklendi')
    showForm.value = false
    await loadCurrencies()
  } catch {
    showToast('Kayıt başarısız', 'error')
  } finally {
    saving.value = false
  }
}

const confirmDelete = (c: Currency) => { deleteTarget.value = c }
const cancelDelete  = () => { deleteTarget.value = null }

const doDelete = async () => {
  if (!deleteTarget.value) return
  deleting.value = true
  try {
    await apiService.deleteCurrency(deleteTarget.value.id)
    showToast('Para birimi silindi')
    deleteTarget.value = null
    await loadCurrencies()
  } catch {
    showToast('Silinemedi', 'error')
  } finally {
    deleting.value = false
  }
}

function toggleExpand(id: string) {
  expandedId.value = expandedId.value === id ? null : id
}

const formatDate = (d: string) => new Date(d).toLocaleDateString('tr-TR')

onMounted(loadCurrencies)
</script>

<template>
  <div class="cm-page">
    <!-- Toast -->
    <transition name="fade">
      <div v-if="toast" :class="['cm-toast', toast.type]">{{ toast.msg }}</div>
    </transition>

    <!-- Header -->
    <AppPageHeader icon="currency_exchange" title="Para Birimi Yönetimi" :subtitle="currencies.length + ' para birimi kayıtlı'">
      <button class="cm-btn cm-btn-primary" @click="openCreate">
        <span class="material-symbols-outlined" aria-hidden="true">add_circle</span> Yeni Ekle
      </button>
    </AppPageHeader>

    <!-- KPI Cards -->
    <div class="kpi-grid">
      <AppKpiCard icon="payments" label="Toplam Para Birimi" :value="currencies.length" color="#3b82f6" bg="#eff6ff" />
      <AppKpiCard icon="check_circle" label="Tedavüldeki" :value="activeCount" color="#059669" bg="#ecfdf5" />
      <AppKpiCard icon="history" label="Tedavülden Kalkan" :value="discontinuedCount" color="#ef4444" bg="#fef2f2" />
      <AppKpiCard icon="public" label="Bölge" :value="regionCount" color="#8b5cf6" bg="#f5f3ff" />
    </div>

    <!-- Loading -->
    <div v-if="loading" class="cm-loading">
      <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
    </div>

    <!-- Currency Table -->
    <div v-else class="cm-card">
      <table class="cm-table">
        <thead>
          <tr>
            <th>Para Birimi</th>
            <th>Sembol</th>
            <th>Bölge</th>
            <th>Durum</th>
            <th>Eklenme</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="currencies.length === 0">
            <td colspan="6" class="cm-empty">Henüz para birimi yok</td>
          </tr>
          <template v-for="c in currencies" :key="c.id">
            <tr class="cm-row" :class="{ expanded: expandedId === c.id }" @click="toggleExpand(c.id)">
              <td class="cm-cell-currency">
                <img v-if="getMeta(c.currencyCode).flagImg" :src="getMeta(c.currencyCode).flagImg" class="cm-flag" :alt="c.currencyCode" />
                <span v-else class="cm-flag cm-flag-placeholder"><span class="material-symbols-outlined" aria-hidden="true">currency_exchange</span></span>
                <div>
                  <div class="cm-currency-code">
                    <span class="cm-badge">{{ c.currencyCode }}</span>
                    <span class="cm-currency-name">{{ c.currencyName }}</span>
                  </div>
                  <div class="cm-currency-country">{{ getMeta(c.currencyCode).country }}</div>
                </div>
              </td>
              <td class="cm-cell-symbol">{{ getMeta(c.currencyCode).symbol }}</td>
              <td>
                <span class="cm-region-pill">{{ getMeta(c.currencyCode).region }}</span>
              </td>
              <td>
                <span class="cm-status" :class="getMeta(c.currencyCode).status === 'active' ? 'cm-status-active' : 'cm-status-disc'">
                  <span class="material-symbols-outlined" aria-hidden="true">{{ getMeta(c.currencyCode).status === 'active' ? 'check_circle' : 'cancel' }}</span>
                  {{ getMeta(c.currencyCode).status === 'active' ? 'Aktif' : 'Tedavülden Kalkmış' }}
                </span>
              </td>
              <td class="cm-cell-date">{{ formatDate(c.createdDate) }}</td>
              <td class="cm-cell-actions" @click.stop>
                <button class="cm-icon-btn" @click="openEdit(c)" title="Düzenle">
                  <span class="material-symbols-outlined" aria-hidden="true">edit</span>
                </button>
                <button class="cm-icon-btn cm-icon-danger" @click="confirmDelete(c)" title="Sil">
                  <span class="material-symbols-outlined" aria-hidden="true">delete</span>
                </button>
              </td>
            </tr>
            <!-- Expanded Detail -->
            <tr v-if="expandedId === c.id" class="cm-detail-row">
              <td colspan="6">
                <div class="cm-detail">
                  <img v-if="getMeta(c.currencyCode).flagImg" :src="getMeta(c.currencyCode).flagImg" class="cm-detail-flag" :alt="c.currencyCode" />
                  <span v-else class="cm-detail-flag cm-detail-flag-placeholder"><span class="material-symbols-outlined" aria-hidden="true">currency_exchange</span></span>
                  <div class="cm-detail-grid">
                    <div class="cm-detail-item">
                      <span class="cm-detail-label">ISO Kodu</span>
                      <span class="cm-detail-value">{{ c.currencyCode }}</span>
                    </div>
                    <div class="cm-detail-item">
                      <span class="cm-detail-label">Tam Ad</span>
                      <span class="cm-detail-value">{{ c.currencyName }}</span>
                    </div>
                    <div class="cm-detail-item">
                      <span class="cm-detail-label">Sembol</span>
                      <span class="cm-detail-value cm-detail-symbol">{{ getMeta(c.currencyCode).symbol }}</span>
                    </div>
                    <div class="cm-detail-item">
                      <span class="cm-detail-label">Ülke</span>
                      <span class="cm-detail-value">{{ getMeta(c.currencyCode).country }}</span>
                    </div>
                    <div class="cm-detail-item">
                      <span class="cm-detail-label">Bölge</span>
                      <span class="cm-detail-value">{{ getMeta(c.currencyCode).region }}</span>
                    </div>
                    <div class="cm-detail-item">
                      <span class="cm-detail-label">Sisteme Eklenme</span>
                      <span class="cm-detail-value">{{ formatDate(c.createdDate) }}</span>
                    </div>
                  </div>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <!-- Form Modal -->
    <div v-if="showForm" class="cm-overlay" @click.self="cancelForm">
      <div class="cm-modal">
        <div class="cm-modal-header">
          <h3>{{ editingCurrency ? 'Para Birimini Düzenle' : 'Yeni Para Birimi' }}</h3>
          <button class="cm-modal-close" @click="cancelForm">
            <span class="material-symbols-outlined" aria-hidden="true">close</span>
          </button>
        </div>

        <!-- Preview -->
        <div v-if="formPreview" class="cm-form-preview">
          <img v-if="formPreview.flagImg" :src="formPreview.flagImg" class="cm-form-preview-flag" alt="" />
          <span v-else class="cm-form-preview-flag cm-flag-placeholder"><span class="material-symbols-outlined" aria-hidden="true">currency_exchange</span></span>
          <div>
            <div class="cm-form-preview-country">{{ formPreview.country }}</div>
            <div class="cm-form-preview-region">{{ formPreview.region }} · {{ formPreview.symbol }}</div>
          </div>
        </div>

        <form @submit.prevent="saveCurrency" class="cm-form">
          <div class="cm-field">
            <label>Kod <span class="cm-req">*</span></label>
            <input v-model="form.currencyCode" placeholder="USD" maxlength="10" required :disabled="!!editingCurrency" />
          </div>
          <div class="cm-field">
            <label>Ad <span class="cm-req">*</span></label>
            <input v-model="form.currencyName" placeholder="Amerikan Doları" required />
          </div>
          <div class="cm-field">
            <label>Sembol</label>
            <input v-model="form.currencySymbol" placeholder="$" maxlength="5" />
          </div>
          <div class="cm-modal-actions">
            <button type="button" class="cm-btn cm-btn-ghost" @click="cancelForm">İptal</button>
            <button type="submit" class="cm-btn cm-btn-primary" :disabled="saving">
              <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
              {{ saving ? 'Kaydediliyor...' : (editingCurrency ? 'Güncelle' : 'Ekle') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Delete Confirm Modal -->
    <div v-if="deleteTarget" class="cm-overlay" @click.self="cancelDelete">
      <div class="cm-modal cm-modal-sm">
        <h3>Emin misiniz?</h3>
        <p class="cm-delete-text">
          <img v-if="getMeta(deleteTarget.currencyCode).flagImg" :src="getMeta(deleteTarget.currencyCode).flagImg" class="cm-flag-sm" :alt="deleteTarget.currencyCode" />
          <span v-else class="material-symbols-outlined" aria-hidden="true" style="font-size:20px;vertical-align:middle;margin-right:6px">currency_exchange</span>
          <strong>{{ deleteTarget.currencyCode }} — {{ deleteTarget.currencyName }}</strong> silinecek.
          <br>Bu para birimine ait işlemler etkilenebilir.
        </p>
        <div class="cm-modal-actions">
          <button class="cm-btn cm-btn-ghost" @click="cancelDelete">Vazgeç</button>
          <button class="cm-btn cm-btn-danger" :disabled="deleting" @click="doDelete">
            <span v-if="deleting" class="material-symbols-outlined spin">progress_activity</span>
            {{ deleting ? 'Siliniyor...' : 'Sil' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.cm-page {
  padding: 24px; max-width: 1400px; margin: 0 auto;
}
.cm-page .material-symbols-outlined {
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}

/* Header */

/* KPI Grid */
.kpi-grid {
  display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 14px; margin-bottom: 20px;
}

/* Loading */
.cm-loading {
  display: flex; align-items: center; justify-content: center; gap: 8px;
  padding: 60px; color: #9ca3af; font-size: 14px;
}

/* Card & Table */
.cm-card {
  background: #fff; border: 1px solid #e5e7eb; border-radius: 14px;
  overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,.04);
}
.cm-table { width: 100%; border-collapse: collapse; font-size: 14px; }
.cm-table thead { background: #f9fafb; }
.cm-table th {
  padding: 12px 16px; text-align: left; font-weight: 600; color: #374151;
  font-size: 12px; text-transform: uppercase; letter-spacing: .4px;
  border-bottom: 2px solid #e5e7eb;
}
.cm-table td { padding: 14px 16px; border-bottom: 1px solid #f3f4f6; }
.cm-row { cursor: pointer; transition: background .12s; }
.cm-row:hover { background: #f9fafb; }
.cm-row.expanded { background: #faf5ff; }

/* Currency cell */
.cm-cell-currency { display: flex; align-items: center; gap: 12px; }
.cm-flag { width: 36px; height: 24px; object-fit: cover; border-radius: 4px; flex-shrink: 0; box-shadow: 0 1px 3px rgba(0,0,0,.12); }
.cm-flag-placeholder {
  width: 36px; height: 24px; border-radius: 4px; flex-shrink: 0;
  background: #f3f4f6; display: inline-flex; align-items: center; justify-content: center;
}
.cm-flag-placeholder .material-symbols-outlined { font-size: 16px; color: #9ca3af; }
.cm-flag-sm { width: 24px; height: 16px; object-fit: cover; border-radius: 3px; vertical-align: middle; margin-right: 6px; box-shadow: 0 1px 2px rgba(0,0,0,.1); }
.cm-currency-code { display: flex; align-items: center; gap: 8px; }
.cm-badge {
  background: #ede9fe; color: #6b46c1; font-weight: 700; font-size: 12px;
  padding: 3px 8px; border-radius: 6px; letter-spacing: .5px;
}
.cm-currency-name { font-weight: 600; color: #111; font-size: 14px; }
.cm-currency-country { font-size: 12px; color: #9ca3af; margin-top: 2px; }

/* Symbol */
.cm-cell-symbol { font-size: 20px; font-weight: 700; color: #374151; font-family: 'Consolas', monospace; }

/* Region pill */
.cm-region-pill {
  display: inline-block; padding: 3px 10px; border-radius: 20px;
  background: #f3f4f6; color: #4b5563; font-size: 11px; font-weight: 500;
}

/* Status */
.cm-status {
  display: inline-flex; align-items: center; gap: 4px;
  padding: 3px 10px; border-radius: 20px; font-size: 12px; font-weight: 600;
}
.cm-status .material-symbols-outlined { font-size: 14px; }
.cm-status-active { background: #f0fdf4; color: #16a34a; }
.cm-status-disc { background: #fef2f2; color: #dc2626; }

/* Date */
.cm-cell-date { font-size: 13px; color: #6b7280; white-space: nowrap; }

/* Actions */
.cm-cell-actions { display: flex; gap: 4px; }
.cm-icon-btn {
  background: none; border: none; cursor: pointer; padding: 6px; border-radius: 8px;
  color: #6b7280; display: flex; align-items: center; transition: background-color 0.2s, color 0.2s;
}
.cm-icon-btn:hover { background: #f3f4f6; color: #6b46c1; }
.cm-icon-btn .material-symbols-outlined { font-size: 18px; }
.cm-icon-danger:hover { background: #fef2f2; color: #dc2626; }

/* Expanded Detail Row */
.cm-detail-row td { padding: 0 !important; border-bottom: 2px solid #e9d5ff; }
.cm-detail {
  display: flex; align-items: flex-start; gap: 20px;
  padding: 20px 24px; background: linear-gradient(135deg, #faf5ff 0%, #f5f3ff 100%);
}
.cm-detail-flag { width: 64px; height: 44px; object-fit: cover; border-radius: 6px; box-shadow: 0 2px 8px rgba(0,0,0,.15); flex-shrink: 0; }
.cm-detail-flag-placeholder {
  width: 64px; height: 44px; border-radius: 6px; flex-shrink: 0;
  background: #ede9fe; display: inline-flex; align-items: center; justify-content: center;
}
.cm-detail-flag-placeholder .material-symbols-outlined { font-size: 28px; color: #7c3aed; }
.cm-detail-grid {
  display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; flex: 1;
}
.cm-detail-item { display: flex; flex-direction: column; gap: 2px; }
.cm-detail-label { font-size: 11px; font-weight: 600; color: #7c3aed; text-transform: uppercase; letter-spacing: .5px; }
.cm-detail-value { font-size: 14px; font-weight: 500; color: #111; }
.cm-detail-symbol { font-size: 20px; font-weight: 700; font-family: 'Consolas', monospace; }

.cm-empty { text-align: center; padding: 40px; color: #9ca3af; }

/* Buttons */
.cm-btn {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 9px 18px; border-radius: 10px; font-size: 14px; font-weight: 600;
  cursor: pointer; border: none; transition: background .15s;
}
.cm-btn:disabled { opacity: .6; cursor: not-allowed; }
.cm-btn-primary { background: #6b46c1; color: #fff; }
.cm-btn-primary:hover:not(:disabled) { background: #553c9a; }
.cm-btn-primary .material-symbols-outlined { font-size: 18px; }
.cm-btn-ghost { background: #f3f4f6; color: #374151; }
.cm-btn-ghost:hover { background: #e5e7eb; }
.cm-btn-danger { background: #dc2626; color: #fff; }
.cm-btn-danger:hover:not(:disabled) { background: #b91c1c; }

/* Modal */
.cm-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.4);
  display: flex; align-items: center; justify-content: center; z-index: 1000;
  backdrop-filter: blur(2px);
}
.cm-modal {
  background: #fff; border-radius: 16px; padding: 0; width: 440px; max-width: 95vw;
  box-shadow: 0 20px 60px rgba(0,0,0,.15); overflow: hidden;
}
.cm-modal-sm { width: 380px; padding: 24px; }
.cm-modal-sm h3 { margin: 0 0 14px; font-size: 17px; font-weight: 700; color: #111; }
.cm-modal-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 20px 24px; border-bottom: 1px solid #f3f4f6;
}
.cm-modal-header h3 { margin: 0; font-size: 17px; font-weight: 700; color: #111; }
.cm-modal-close {
  background: none; border: none; cursor: pointer; color: #9ca3af;
  display: flex; padding: 4px; border-radius: 6px;
}
.cm-modal-close:hover { background: #f3f4f6; color: #374151; }

/* Form Preview */
.cm-form-preview {
  display: flex; align-items: center; gap: 12px;
  padding: 14px 24px; background: #f5f3ff; border-bottom: 1px solid #ede9fe;
}
.cm-form-preview-flag { width: 40px; height: 28px; object-fit: cover; border-radius: 4px; box-shadow: 0 1px 4px rgba(0,0,0,.12); flex-shrink: 0; }
.cm-form-preview-country { font-size: 14px; font-weight: 600; color: #6b46c1; }
.cm-form-preview-region { font-size: 12px; color: #9ca3af; }

.cm-form { display: flex; flex-direction: column; gap: 14px; padding: 24px; }
.cm-field { display: flex; flex-direction: column; gap: 5px; }
.cm-field label { font-size: 13px; font-weight: 500; color: #374151; }
.cm-req { color: #dc2626; }
.cm-field input {
  padding: 10px 12px; border: 1.5px solid #e5e7eb; border-radius: 10px;
  font-size: 14px; outline: none; transition: border-color .15s;
}
.cm-field input:focus { border-color: #6b46c1; box-shadow: 0 0 0 2px rgba(107,70,193,.12); }
.cm-field input:disabled { background: #f9fafb; color: #9ca3af; }

.cm-modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 4px; }

.cm-delete-text { color: #374151; font-size: 14px; line-height: 1.6; margin: 0 0 20px; }

/* Toast */
.cm-toast {
  position: fixed; top: 20px; right: 20px; padding: 12px 20px; border-radius: 10px;
  font-size: 14px; font-weight: 500; z-index: 2000; box-shadow: 0 4px 12px rgba(0,0,0,.15);
}
.cm-toast.success { background: #d1fae5; color: #065f46; border: 1px solid #a7f3d0; }
.cm-toast.error   { background: #fee2e2; color: #991b1b; border: 1px solid #fca5a5; }

/* Animations */
@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin .7s linear infinite; display: inline-block; }
.fade-enter-active, .fade-leave-active { transition: opacity .3s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

/* Responsive */
@media (max-width: 768px) {
  .cm-page { padding: 12px; }
  .kpi-grid { grid-template-columns: repeat(2, 1fr); }
  .cm-card { overflow-x: auto; }
  .cm-detail-grid { grid-template-columns: repeat(2, 1fr); }
}
</style>
