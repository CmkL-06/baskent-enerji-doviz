<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import { formatAmount } from '@/utils/currency'
import AppKpiCard from '@/components/common/AppKpiCard.vue'
import AppPageHeader from '@/components/common/AppPageHeader.vue'
import AppEmptyState from '@/components/common/AppEmptyState.vue'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()

const loading = ref(true)
const saving = ref(false)
const error = ref('')

// ── Data
const vaults = ref<any[]>([])
const countHistory = ref<any[]>([])
const snapshots = ref<any[]>([])
const selectedVault = ref<any>(null)
const selectedSnapshot = ref<any>(null)
const comparisonResult = ref<any>(null)

// ── Tabs
const activeTab = ref<'vaults' | 'timeline' | 'compare'>('vaults')
const vaultFilter = ref<'all' | 'pending'>('all')

// ── Count modal
const showCountModal = ref(false)
const countDetails = ref<{ currencyId: string; currencyCode: string; currencyName: string; systemAmount: number; actualAmount: number | null }[]>([])
const countAlsoSnapshot = ref(true)
const countDescription = ref('')

// ── Snapshot modal
const showSnapshotModal = ref(false)
const snapshotDescription = ref('')

// ── Detail modal
const showDetailModal = ref(false)
const detailType = ref<'count' | 'snapshot'>('count')

// ── Compare
const compareId1 = ref('')
const compareId2 = ref('')
const showCompareResult = ref(false)

// ── Timeline filter
const timelineDateFrom = ref('')
const timelineDateTo = ref('')
const timelineVaultId = ref<string>('')

// ── History for a specific vault
const showVaultHistory = ref(false)
const vaultHistoryData = ref<any[]>([])

const officeId = computed(() =>
  exchangeStore.selectedOffice?.officeId ?? exchangeStore.offices[0]?.officeId
)

const pendingVaults = computed(() => vaults.value.filter(v => v.shouldCount))
const pendingCount = computed(() => pendingVaults.value.length)

const countedToday = computed(() => {
  const today = new Date().toISOString().slice(0, 10)
  return vaults.value.filter(v => v.lastCountDate && v.lastCountDate.slice(0, 10) === today).length
})

const displayedVaults = computed(() =>
  vaultFilter.value === 'pending' ? pendingVaults.value : vaults.value
)

const timeline = computed(() => {
  const items: any[] = []

  for (const s of snapshots.value) {
    items.push({ ...s, _type: 'snapshot', _date: s.snapshotDate ?? s.createdDate })
  }
  for (const c of countHistory.value) {
    items.push({ ...c, _type: 'count', _date: c.countDate ?? c.createdDate })
  }

  items.sort((a, b) => new Date(b._date).getTime() - new Date(a._date).getTime())
  return items
})

const formatCurrency = (amount: number): string => formatAmount(amount, 2)

const formatDateTime = (d: string | null | undefined): string => {
  if (!d) return '-'
  return new Date(d).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

// ── Load
async function loadVaults() {
  loading.value = true
  error.value = ''
  try {
    const data = officeId.value
      ? await apiService.getVaultsByOfficeId(officeId.value)
      : await apiService.getVaults()
    vaults.value = Array.isArray(data) ? data : (data?.items ?? data?.data ?? [])
  } catch {
    error.value = 'Kasalar yüklenemedi'
  } finally {
    loading.value = false
  }
}

async function loadSnapshots() {
  if (!officeId.value) return
  try {
    const params: any = {}
    if (timelineDateFrom.value) params.startDate = timelineDateFrom.value
    if (timelineDateTo.value) params.endDate = timelineDateTo.value
    const data = await apiService.getVaultSnapshots(officeId.value, params)
    snapshots.value = Array.isArray(data) ? data : (data?.items ?? [])
  } catch { snapshots.value = [] }
}

async function loadAllCountHistory() {
  try {
    const results: any[] = []
    for (const v of vaults.value) {
      const vid = v.vaultId ?? v.id
      if (!vid) continue
      const params: any = {}
      if (timelineDateFrom.value) params.startDate = timelineDateFrom.value
      if (timelineDateTo.value) params.endDate = timelineDateTo.value
      const data = await apiService.getVaultCounts(vid, params)
      const items = Array.isArray(data) ? data : (data?.items ?? [])
      results.push(...items.map((c: any) => ({ ...c, _vaultName: v.vaultName ?? v.name })))
    }
    countHistory.value = results
  } catch { countHistory.value = [] }
}

async function loadTimeline() {
  await Promise.all([loadSnapshots(), loadAllCountHistory()])
}

async function loadVaultHistory(vault: any) {
  selectedVault.value = vault
  try {
    const params: any = {}
    if (timelineDateFrom.value) params.startDate = timelineDateFrom.value
    if (timelineDateTo.value) params.endDate = timelineDateTo.value
    const data = await apiService.getVaultCounts(vault.vaultId ?? vault.id, params)
    vaultHistoryData.value = Array.isArray(data) ? data : (data?.items ?? [])
    showVaultHistory.value = true
  } catch { vaultHistoryData.value = [] }
}

// ── Count
function openCountModal(vault: any) {
  selectedVault.value = vault
  const balances = vault.balances ?? []
  countDetails.value = balances.map((b: any) => ({
    currencyId: b.currencyId,
    currencyCode: b.currencyCode ?? b.currencyName ?? '',
    currencyName: b.currencyName ?? b.currencyCode ?? '',
    systemAmount: b.balance ?? 0,
    actualAmount: null,
  }))
  countAlsoSnapshot.value = true
  countDescription.value = ''
  showCountModal.value = true
}

async function submitCount() {
  if (!selectedVault.value) return
  const details = countDetails.value
    .filter(d => d.actualAmount !== null && d.actualAmount !== undefined)
    .map(d => ({ CurrencyId: d.currencyId, ActualAmount: d.actualAmount }))

  if (details.length === 0) {
    notification.warning('En az bir para birimi için sayım tutarı giriniz')
    return
  }

  saving.value = true
  try {
    await apiService.submitVaultCount({
      VaultId: selectedVault.value.vaultId ?? selectedVault.value.id,
      IsManual: true,
      CountDetails: details,
    })

    if (countAlsoSnapshot.value && officeId.value) {
      await apiService.createVaultSnapshot({
        OfficeId: officeId.value,
        Description: countDescription.value || `Sayım sonrası — ${selectedVault.value.vaultName ?? selectedVault.value.name}`,
      }).catch(() => {})
    }

    showCountModal.value = false
    await loadVaults()
    if (activeTab.value === 'timeline') await loadTimeline()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Sayım kaydedilemedi')
  } finally {
    saving.value = false
  }
}

function getDiscrepancy(detail: any): number {
  return (detail.actualAmount ?? 0) - (detail.systemAmount ?? 0)
}

// ── Snapshot
function openSnapshotModal() {
  snapshotDescription.value = ''
  showSnapshotModal.value = true
}

async function createSnapshot() {
  if (!officeId.value) return
  saving.value = true
  try {
    await apiService.createVaultSnapshot({
      OfficeId: officeId.value,
      Description: snapshotDescription.value || undefined,
    })
    showSnapshotModal.value = false
    if (activeTab.value === 'timeline') await loadTimeline()
    else await loadSnapshots()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Snapshot oluşturulamadı')
  } finally {
    saving.value = false
  }
}

async function deleteSnapshot(snap: any) {
  if (!confirm(`${formatDateTime(snap.snapshotDate)} snapshot'ını silmek istediğinizden emin misiniz?`)) return
  try {
    await apiService.deleteVaultSnapshot(snap.id)
    await loadTimeline()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Silme başarısız')
  }
}

// ── Detail
async function openSnapshotDetail(snap: any) {
  try {
    selectedSnapshot.value = await apiService.getVaultSnapshotById(snap.id)
    detailType.value = 'snapshot'
    showDetailModal.value = true
  } catch { notification.error('Detay yüklenemedi') }
}

function openCountDetail(count: any) {
  selectedSnapshot.value = count
  detailType.value = 'count'
  showDetailModal.value = true
}

function groupDetailsByVault(details: any[]): Record<string, any[]> {
  const groups: Record<string, any[]> = {}
  for (const d of details) {
    const key = d.vaultName ?? 'Bilinmeyen'
    if (!groups[key]) groups[key] = []
    groups[key].push(d)
  }
  return groups
}

// ── Compare
async function runComparison() {
  if (!compareId1.value || !compareId2.value) {
    notification.warning('Karşılaştırma için 2 snapshot seçiniz')
    return
  }
  try {
    comparisonResult.value = await apiService.compareVaultSnapshots(compareId1.value, compareId2.value)
    showCompareResult.value = true
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Karşılaştırma başarısız')
  }
}

// ── Admin actions
async function resetCountStatus(vault: any) {
  if (!confirm(`"${vault.vaultName ?? vault.name}" sayım durumunu sıfırlamak istiyor musunuz?`)) return
  try {
    await apiService.resetVaultCountStatus(vault.vaultId ?? vault.id)
    await loadVaults()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'İşlem başarısız')
  }
}

async function toggleCountFlag(vault: any) {
  try {
    await apiService.setVaultCountStatus(vault.vaultId ?? vault.id, !vault.shouldCount)
    await loadVaults()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'İşlem başarısız')
  }
}

watch(() => officeId.value, () => {
  loadVaults()
  if (activeTab.value === 'timeline') loadTimeline()
}, { immediate: true })

function switchTab(tab: 'vaults' | 'timeline' | 'compare') {
  activeTab.value = tab
  if (tab === 'timeline') loadTimeline()
  if (tab === 'compare') loadSnapshots()
}
</script>

<template>
  <div class="vc-wrap">
    <!-- Header -->
    <AppPageHeader icon="fact_check" title="Kasa Kontrol">
      <button class="btn-secondary" @click="loadVaults">
        <span class="material-symbols-outlined" aria-hidden="true">refresh</span>
        Yenile
      </button>
      <button v-if="authStore.isAdmin" class="btn-secondary" @click="openSnapshotModal">
        <span class="material-symbols-outlined" aria-hidden="true">add_a_photo</span>
        Snapshot Al
      </button>
    </AppPageHeader>

    <!-- KPI Cards -->
    <div class="kpi-grid">
      <AppKpiCard icon="account_balance_wallet" label="Toplam Kasa" :value="vaults.length" color="var(--color-secondary)" bg="var(--color-secondary-light)" />
      <AppKpiCard icon="priority_high" label="Sayım Bekleyen" :value="pendingCount" color="var(--color-warning)" bg="#fffbeb" />
      <AppKpiCard icon="task_alt" label="Bugün Sayılan" :value="countedToday" color="var(--color-success)" bg="#ecfdf5" />
      <AppKpiCard icon="photo_camera" label="Snapshot" :value="snapshots.length" color="#8b5cf6" bg="#f5f3ff" />
    </div>

    <!-- Tabs -->
    <div class="tab-bar">
      <button :class="['tab-btn', activeTab === 'vaults' ? 'tab-active' : '']" @click="switchTab('vaults')">
        <span class="material-symbols-outlined" aria-hidden="true">inventory_2</span>
        Kasalar
        <span v-if="pendingCount > 0" class="badge">{{ pendingCount }}</span>
      </button>
      <button :class="['tab-btn', activeTab === 'timeline' ? 'tab-active' : '']" @click="switchTab('timeline')">
        <span class="material-symbols-outlined" aria-hidden="true">timeline</span>
        Zaman Çizelgesi
      </button>
      <button :class="['tab-btn', activeTab === 'compare' ? 'tab-active' : '']" @click="switchTab('compare')">
        <span class="material-symbols-outlined" aria-hidden="true">compare</span>
        Karşılaştır
      </button>
    </div>

    <!-- ═══ VAULTS TAB ═══ -->
    <template v-if="activeTab === 'vaults'">
      <div class="vault-filter-bar">
        <button :class="['chip', vaultFilter === 'all' ? 'chip-active' : '']" @click="vaultFilter = 'all'">
          Tümü ({{ vaults.length }})
        </button>
        <button :class="['chip', vaultFilter === 'pending' ? 'chip-active' : '']" @click="vaultFilter = 'pending'">
          Sayım Bekleyen ({{ pendingCount }})
        </button>
      </div>

      <div v-if="loading" class="loading-state">
        <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
      </div>
      <div v-else-if="error" class="error-state">{{ error }}</div>

      <div v-else class="vault-grid">
        <div v-for="vault in displayedVaults" :key="vault.vaultId ?? vault.id"
             class="vault-card" :class="{ 'vault-pending': vault.shouldCount }">
          <div class="vault-top">
            <div class="vault-info">
              <h3>{{ vault.vaultName ?? vault.name }}</h3>
              <span class="vault-office">{{ vault.officeName ?? '' }}</span>
            </div>
            <div v-if="vault.shouldCount" class="count-badge badge-pending">
              <span class="material-symbols-outlined" aria-hidden="true">schedule</span>
              Sayım Bekliyor
            </div>
            <div v-else class="count-badge badge-ok">
              <span class="material-symbols-outlined" aria-hidden="true">check_circle</span>
              Tamamlandı
            </div>
          </div>

          <!-- Balances -->
          <div class="balance-list">
            <div v-for="bal in (vault.balances ?? []).filter((b: any) => (b.balance ?? 0) !== 0)" :key="bal.currencyId" class="balance-row">
              <span class="bal-currency">{{ bal.currencyCode }}</span>
              <span class="bal-amount">{{ formatCurrency(bal.balance ?? 0) }}</span>
            </div>
            <div v-if="!(vault.balances ?? []).some((b: any) => (b.balance ?? 0) !== 0)" class="no-balance">
              <span class="material-symbols-outlined" aria-hidden="true">account_balance</span>
              Bakiye bilgisi yok
            </div>
          </div>

          <div class="vault-footer">
            <div class="vault-meta">
              <span class="material-symbols-outlined" aria-hidden="true">schedule</span>
              Son Sayım: {{ formatDateTime(vault.lastCountDate) }}
            </div>

            <!-- Actions -->
            <div class="vault-actions">
              <button class="btn-primary btn-sm" @click="openCountModal(vault)">
                <span class="material-symbols-outlined" aria-hidden="true">calculate</span>
                Sayım Yap
              </button>
              <button class="btn-outline btn-sm" @click="loadVaultHistory(vault)">
                <span class="material-symbols-outlined" aria-hidden="true">history</span>
                Geçmiş
              </button>
            </div>
            <div v-if="authStore.isAdmin" class="vault-admin-actions">
              <button v-if="vault.shouldCount" class="admin-btn" @click="resetCountStatus(vault)">
                <span class="material-symbols-outlined" aria-hidden="true">restart_alt</span>
                Sıfırla
              </button>
              <button class="admin-btn" :class="vault.shouldCount ? 'admin-btn-off' : 'admin-btn-on'" @click="toggleCountFlag(vault)">
                <span class="material-symbols-outlined" aria-hidden="true">{{ vault.shouldCount ? 'cancel' : 'notification_add' }}</span>
                {{ vault.shouldCount ? 'Kapat' : 'Sayım İste' }}
              </button>
            </div>
          </div>
        </div>

        <AppEmptyState v-if="displayedVaults.length === 0" icon="inventory_2" :message="vaultFilter === 'pending' ? 'Sayım bekleyen kasa yok' : 'Kasa bulunamadı'" />
      </div>
    </template>

    <!-- ═══ TIMELINE TAB ═══ -->
    <template v-if="activeTab === 'timeline'">
      <div class="timeline-filters">
        <div class="form-group">
          <label>Başlangıç</label>
          <input v-model="timelineDateFrom" type="date" @change="loadTimeline" />
        </div>
        <div class="form-group">
          <label>Bitiş</label>
          <input v-model="timelineDateTo" type="date" @change="loadTimeline" />
        </div>
      </div>

      <div class="timeline-list">
        <AppEmptyState v-if="timeline.length === 0" icon="schedule" message="Kayıt bulunamadı" />

        <div v-for="item in timeline" :key="item.id + item._type" class="tl-item" :class="'tl-' + item._type">
          <div class="tl-marker">
            <span class="material-symbols-outlined" aria-hidden="true">{{ item._type === 'snapshot' ? 'photo_camera' : 'calculate' }}</span>
          </div>
          <div class="tl-content">
            <div class="tl-header">
              <span class="tl-type-badge" :class="'badge-' + item._type">
                {{ item._type === 'snapshot' ? 'Snapshot' : 'Sayım' }}
              </span>
              <span class="tl-date">{{ formatDateTime(item._date) }}</span>
              <span class="tl-user">{{ item.userName ?? item.username ?? '-' }}</span>
            </div>

            <!-- Snapshot row -->
            <template v-if="item._type === 'snapshot'">
              <div class="tl-body">
                <span v-if="item.description">{{ item.description }}</span>
                <span class="tl-meta">{{ item.totalVaults ?? '-' }} kasa &middot; {{ item.totalCurrencies ?? '-' }} para birimi &middot; {{ formatCurrency(item.totalValueInBaseCurrency ?? 0) }} ₺</span>
              </div>
              <div class="tl-actions">
                <button class="icon-btn" @click="openSnapshotDetail(item)" title="Detay">
                  <span class="material-symbols-outlined" aria-hidden="true">visibility</span>
                </button>
                <button v-if="authStore.isAdmin" class="icon-btn danger" @click="deleteSnapshot(item)" title="Sil">
                  <span class="material-symbols-outlined" aria-hidden="true">delete</span>
                </button>
              </div>
            </template>

            <!-- Count row -->
            <template v-if="item._type === 'count'">
              <div class="tl-body">
                <span class="tl-vault-name">{{ item.vaultName ?? item._vaultName ?? '-' }}</span>
                <span v-if="item.hasDiscrepancy" class="tl-discrepancy">Fark tespit edildi</span>
                <span v-else class="tl-ok">Uyumlu</span>
                <span class="tl-source-tag">{{ item.isSystemGenerated ? 'Sistem' : 'Manuel' }}</span>
              </div>
              <div class="tl-actions">
                <button class="icon-btn" @click="openCountDetail(item)" title="Detay">
                  <span class="material-symbols-outlined" aria-hidden="true">visibility</span>
                </button>
              </div>
            </template>
          </div>
        </div>
      </div>
    </template>

    <!-- ═══ COMPARE TAB ═══ -->
    <template v-if="activeTab === 'compare'">
      <div class="compare-panel">
        <p class="compare-hint">İki snapshot seçerek fark analizi yapın.</p>
        <div class="compare-selectors">
          <div class="form-group">
            <label>1. Snapshot (Eski)</label>
            <select v-model="compareId1">
              <option value="">Seçiniz</option>
              <option v-for="s in snapshots" :key="s.id" :value="s.id" :disabled="s.id === compareId2">
                {{ formatDateTime(s.snapshotDate) }} — {{ s.description || 'Snapshot' }}
              </option>
            </select>
          </div>
          <div class="compare-arrow">
            <span class="material-symbols-outlined" aria-hidden="true">arrow_forward</span>
          </div>
          <div class="form-group">
            <label>2. Snapshot (Yeni)</label>
            <select v-model="compareId2">
              <option value="">Seçiniz</option>
              <option v-for="s in snapshots" :key="s.id" :value="s.id" :disabled="s.id === compareId1">
                {{ formatDateTime(s.snapshotDate) }} — {{ s.description || 'Snapshot' }}
              </option>
            </select>
          </div>
          <button class="btn-primary" @click="runComparison" :disabled="!compareId1 || !compareId2">
            <span class="material-symbols-outlined" aria-hidden="true">compare_arrows</span>
            Karşılaştır
          </button>
        </div>

        <!-- Comparison Result -->
        <div v-if="showCompareResult && comparisonResult" class="compare-result">
          <div class="compare-header-info">
            <div class="compare-side">
              <span class="compare-label">Eski</span>
              <span>{{ formatDateTime(comparisonResult.snapshot1?.snapshotDate ?? comparisonResult.oldSnapshot?.snapshotDate) }}</span>
            </div>
            <span class="material-symbols-outlined" aria-hidden="true" style="color:#9ca3af;font-size:24px">arrow_forward</span>
            <div class="compare-side">
              <span class="compare-label">Yeni</span>
              <span>{{ formatDateTime(comparisonResult.snapshot2?.snapshotDate ?? comparisonResult.newSnapshot?.snapshotDate) }}</span>
            </div>
          </div>

          <div class="table-wrap">
            <table class="vc-table">
              <thead>
                <tr>
                  <th>Kasa</th>
                  <th>Para Birimi</th>
                  <th class="text-right">Eski Bakiye</th>
                  <th class="text-right">Yeni Bakiye</th>
                  <th class="text-right">Fark</th>
                  <th class="text-right">Değişim %</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(row, i) in (comparisonResult.differences ?? comparisonResult.details ?? [])" :key="i"
                    :class="{ 'diff-row-pos': (row.difference ?? row.change ?? 0) > 0, 'diff-row-neg': (row.difference ?? row.change ?? 0) < 0 }">
                  <td>{{ row.vaultName ?? '-' }}</td>
                  <td><strong>{{ row.currencyCode }}</strong></td>
                  <td class="text-right font-mono">{{ formatCurrency(row.oldBalance ?? row.balance1 ?? 0) }}</td>
                  <td class="text-right font-mono">{{ formatCurrency(row.newBalance ?? row.balance2 ?? 0) }}</td>
                  <td class="text-right font-mono" :class="(row.difference ?? row.change ?? 0) >= 0 ? 'balance-pos' : 'balance-neg'">
                    {{ (row.difference ?? row.change ?? 0) > 0 ? '+' : '' }}{{ formatCurrency(row.difference ?? row.change ?? 0) }}
                  </td>
                  <td class="text-right">{{ row.changePercentage != null ? row.changePercentage.toFixed(1) + '%' : '-' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </template>

    <!-- ═══ COUNT MODAL ═══ -->
    <div v-if="showCountModal && selectedVault" class="modal-overlay" @click.self="showCountModal = false">
      <div class="modal modal-lg">
        <div class="modal-header">
          <h2>Kasa Sayımı — {{ selectedVault.vaultName ?? selectedVault.name }}</h2>
          <button class="modal-close" @click="showCountModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <p class="modal-hint">Her para birimi için kasadaki gerçek tutarı giriniz.</p>
          <div class="count-form">
            <div v-for="detail in countDetails" :key="detail.currencyId" class="count-row">
              <div class="count-currency">
                <span class="currency-code">{{ detail.currencyCode }}</span>
                <span class="currency-name">{{ detail.currencyName }}</span>
              </div>
              <div class="count-system">
                <span class="count-label">Sistem</span>
                <span class="count-amount">{{ formatCurrency(detail.systemAmount) }}</span>
              </div>
              <div class="count-actual">
                <span class="count-label">Gerçek Tutar</span>
                <input v-model.number="detail.actualAmount" type="number" step="0.01" placeholder="0.00" />
              </div>
              <div class="count-diff" v-if="detail.actualAmount !== null && detail.actualAmount !== undefined">
                <span class="count-label">Fark</span>
                <span :class="getDiscrepancy(detail) === 0 ? 'diff-ok' : 'diff-warn'">
                  {{ getDiscrepancy(detail) > 0 ? '+' : '' }}{{ formatCurrency(getDiscrepancy(detail)) }}
                </span>
              </div>
            </div>
          </div>

          <div class="count-options">
            <label class="checkbox-label">
              <input type="checkbox" v-model="countAlsoSnapshot" />
              Sayım sonrası otomatik snapshot al
            </label>
            <div v-if="countAlsoSnapshot" class="form-group">
              <label>Snapshot açıklaması</label>
              <input v-model="countDescription" placeholder="Opsiyonel" />
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showCountModal = false">İptal</button>
          <button class="btn-primary" :disabled="saving" @click="submitCount">
            {{ saving ? 'Kaydediliyor...' : 'Sayımı Kaydet' }}
          </button>
        </div>
      </div>
    </div>

    <!-- ═══ SNAPSHOT MODAL ═══ -->
    <div v-if="showSnapshotModal" class="modal-overlay" @click.self="showSnapshotModal = false">
      <div class="modal modal-sm">
        <div class="modal-header">
          <h2>Yeni Snapshot</h2>
          <button class="modal-close" @click="showSnapshotModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <p class="modal-hint">Mevcut kasa bakiyelerinin anlık görüntüsünü kaydeder.</p>
          <div class="form-group">
            <label>Açıklama (opsiyonel)</label>
            <input v-model="snapshotDescription" placeholder="Örn: Gün sonu" />
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showSnapshotModal = false">İptal</button>
          <button class="btn-primary" :disabled="saving" @click="createSnapshot">
            {{ saving ? 'Oluşturuluyor...' : 'Snapshot Al' }}
          </button>
        </div>
      </div>
    </div>

    <!-- ═══ DETAIL MODAL ═══ -->
    <div v-if="showDetailModal && selectedSnapshot" class="modal-overlay" @click.self="showDetailModal = false">
      <div class="modal modal-xl">
        <div class="modal-header">
          <h2 v-if="detailType === 'snapshot'">Snapshot Detay — {{ formatDateTime(selectedSnapshot.snapshotDate) }}</h2>
          <h2 v-else>Sayım Detay — {{ formatDateTime(selectedSnapshot.countDate) }}</h2>
          <button class="modal-close" @click="showDetailModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <!-- Snapshot detail -->
          <template v-if="detailType === 'snapshot'">
            <div class="detail-meta">
              <span>Ofis: <strong>{{ selectedSnapshot.officeName }}</strong></span>
              <span>Kullanıcı: <strong>{{ selectedSnapshot.userName || '-' }}</strong></span>
              <span v-if="selectedSnapshot.description">Not: {{ selectedSnapshot.description }}</span>
            </div>
            <div v-for="(details, vaultName) in groupDetailsByVault(selectedSnapshot.details ?? selectedSnapshot.snapshotDetails ?? [])" :key="vaultName" class="vault-group">
              <h4 class="vault-group-title">{{ vaultName }}</h4>
              <table class="vc-table inner-table">
                <thead>
                  <tr>
                    <th>Para Birimi</th>
                    <th class="text-right">Bakiye</th>
                    <th class="text-right">Rezerve</th>
                    <th class="text-right">Kullanılabilir</th>
                    <th class="text-right">₺ Değer</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="d in details" :key="d.currencyCode">
                    <td><strong>{{ d.currencyCode }}</strong> <span class="currency-name-sm">{{ d.currencyName }}</span></td>
                    <td class="text-right font-mono">{{ formatCurrency(d.balance ?? 0) }}</td>
                    <td class="text-right font-mono">{{ formatCurrency(d.reservedAmount ?? 0) }}</td>
                    <td class="text-right font-mono">{{ formatCurrency(d.availableBalance ?? 0) }}</td>
                    <td class="text-right font-mono">{{ formatCurrency(d.valueInBaseCurrency ?? 0) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="total-row">
              <strong>Toplam:</strong>
              <span class="total-value">{{ formatCurrency(selectedSnapshot.totalValueInBaseCurrency ?? 0) }} ₺</span>
            </div>
          </template>

          <!-- Count detail -->
          <template v-if="detailType === 'count'">
            <div class="detail-meta">
              <span>Kasa: <strong>{{ selectedSnapshot.vaultName ?? '-' }}</strong></span>
              <span>Kullanıcı: <strong>{{ selectedSnapshot.username ?? '-' }}</strong></span>
              <span>Kaynak: <strong>{{ selectedSnapshot.isSystemGenerated ? 'Sistem' : 'Manuel' }}</strong></span>
            </div>
            <table class="vc-table">
              <thead>
                <tr>
                  <th>Para Birimi</th>
                  <th class="text-right">Sistem Tutarı</th>
                  <th class="text-right">Sayım Tutarı</th>
                  <th class="text-right">Fark</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="d in (selectedSnapshot.countDetails ?? [])" :key="d.currencyId"
                    :class="{ 'discrepancy-row': (d.discrepancy ?? 0) !== 0 }">
                  <td><strong>{{ d.currencyCode }}</strong></td>
                  <td class="text-right font-mono">{{ formatCurrency(d.systemAmount ?? 0) }}</td>
                  <td class="text-right font-mono">{{ formatCurrency(d.actualAmount ?? 0) }}</td>
                  <td class="text-right font-mono" :class="(d.discrepancy ?? 0) !== 0 ? 'diff-warn' : 'diff-ok'">
                    {{ formatCurrency(d.discrepancy ?? 0) }}
                  </td>
                </tr>
              </tbody>
            </table>
          </template>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showDetailModal = false">Kapat</button>
        </div>
      </div>
    </div>

    <!-- ═══ VAULT HISTORY MODAL ═══ -->
    <div v-if="showVaultHistory && selectedVault" class="modal-overlay" @click.self="showVaultHistory = false">
      <div class="modal modal-lg">
        <div class="modal-header">
          <h2>Sayım Geçmişi — {{ selectedVault.vaultName ?? selectedVault.name }}</h2>
          <button class="modal-close" @click="showVaultHistory = false">&times;</button>
        </div>
        <div class="modal-body">
          <table class="vc-table">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Kullanıcı</th>
                <th>Kaynak</th>
                <th>Fark</th>
                <th>Detay</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="vaultHistoryData.length === 0">
                <td colspan="5" class="empty-row">Sayım kaydı bulunamadı</td>
              </tr>
              <tr v-for="record in vaultHistoryData" :key="record.id" :class="{ 'discrepancy-row': record.hasDiscrepancy }">
                <td>{{ formatDateTime(record.countDate) }}</td>
                <td>{{ record.username || '-' }}</td>
                <td>
                  <span class="source-badge" :class="record.isSystemGenerated ? 'source-system' : 'source-manual'">
                    {{ record.isSystemGenerated ? 'Sistem' : 'Manuel' }}
                  </span>
                </td>
                <td>
                  <span v-if="record.hasDiscrepancy" class="diff-warn">Fark var</span>
                  <span v-else class="diff-ok">Uyumlu</span>
                </td>
                <td>
                  <div v-if="record.countDetails?.length" class="detail-list">
                    <div v-for="d in record.countDetails" :key="d.currencyId" class="detail-item-row">
                      <strong>{{ d.currencyCode }}</strong>
                      Sistem: {{ formatCurrency(d.systemAmount ?? 0) }}
                      Sayım: {{ formatCurrency(d.actualAmount ?? 0) }}
                      <span :class="(d.discrepancy ?? 0) !== 0 ? 'diff-warn' : 'diff-ok'">
                        Fark: {{ formatCurrency(d.discrepancy ?? 0) }}
                      </span>
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="showVaultHistory = false">Kapat</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ── Filled icons globally ── */
.vc-wrap .material-symbols-outlined {
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}

.vc-wrap { padding: 28px 32px; max-width: 1400px; margin: 0 auto; }

/* Header */
.header-actions { display: flex; gap: 10px; }

/* Buttons */
.btn-primary {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 9px 18px; background: linear-gradient(135deg, var(--color-secondary-hover), #1d4ed8); color: #fff;
  border: none; border-radius: var(--radius-md); font-size: 13px; font-weight: 600; cursor: pointer;
  box-shadow: 0 2px 8px rgba(37,99,235,.25); transition: background-color 0.2s, box-shadow 0.2s, transform 0.2s;
}
.btn-primary:hover { background: linear-gradient(135deg, #1d4ed8, #1e40af); box-shadow: 0 4px 12px rgba(37,99,235,.35); transform: translateY(-1px); }
.btn-primary:active { transform: translateY(0); }
.btn-primary:disabled { opacity: .6; cursor: not-allowed; box-shadow: none; transform: none; }
.btn-secondary {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 9px 18px; background: #fff; color: #374151;
  border: 1px solid #d1d5db; border-radius: var(--radius-md); font-size: 13px; font-weight: 500; cursor: pointer;
  transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}
.btn-secondary:hover { background: var(--color-bg-page); border-color: #93c5fd; color: #1d4ed8; }
.btn-outline {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 9px 18px; background: transparent; color: var(--color-text-secondary);
  border: 1px solid var(--color-border); border-radius: var(--radius-md); font-size: 13px; font-weight: 500; cursor: pointer;
  transition: background-color 0.2s, border-color 0.2s;
}
.btn-outline:hover { background: var(--color-bg-page); border-color: var(--color-text-muted); }
.btn-sm { padding: 7px 14px; font-size: 12px; }
.btn-sm .material-symbols-outlined { font-size: 16px; }
.btn-cancel {
  padding: 9px 18px; background: #fff; color: #374151;
  border: 1px solid #d1d5db; border-radius: var(--radius-md); font-size: 13px; cursor: pointer; transition: background-color 0.2s;
}
.btn-cancel:hover { background: #f3f4f6; }

/* KPI */
.kpi-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 24px; }

/* Tabs */
.tab-bar { display: flex; gap: 2px; margin-bottom: 20px; border-bottom: 2px solid var(--color-border); }
.tab-btn {
  padding: 12px 20px; border: none; background: none; font-size: 13px; font-weight: 600;
  color: var(--color-text-muted); cursor: pointer; border-bottom: 2px solid transparent;
  display: flex; align-items: center; gap: 8px; margin-bottom: -2px; transition: color 0.2s, border-bottom-color 0.2s;
}
.tab-btn:hover { color: var(--color-text-secondary); }
.tab-active { color: #1d4ed8; border-bottom-color: var(--color-secondary-hover); }
.tab-btn .material-symbols-outlined { font-size: 18px; }
.badge { background: var(--color-danger); color: #fff; font-size: 10px; padding: 2px 8px; border-radius: var(--radius-md); font-weight: 700; min-width: 20px; text-align: center; }

/* Vault filter */
.vault-filter-bar { display: flex; gap: 8px; margin-bottom: 20px; }
.chip {
  padding: 7px 16px; border-radius: var(--radius-xl); border: 1px solid var(--color-border);
  background: #fff; font-size: 13px; cursor: pointer; color: var(--color-text-secondary); font-weight: 500;
  transition: background-color 0.2s, color 0.2s, border-color 0.2s, box-shadow 0.2s;
}
.chip:hover { background: var(--color-bg-page); border-color: var(--color-border); }
.chip-active { background: linear-gradient(135deg, var(--color-secondary-hover), #1d4ed8); color: #fff; border-color: transparent; box-shadow: 0 2px 8px rgba(37,99,235,.3); }

/* Loading */
.loading-state, .error-state { text-align: center; padding: 60px 20px; color: var(--color-text-muted); font-size: 14px; }
.error-state { color: var(--color-danger); }
.spin { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

/* Vault Grid */
.vault-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 18px; }
.vault-card {
  background: #fff; border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  padding: 0; transition: border-color 0.2s, box-shadow 0.2s, transform 0.2s; overflow: hidden;
  box-shadow: 0 1px 3px rgba(0,0,0,.04);
  display: flex; flex-direction: column;
}
.vault-card:hover { border-color: #93c5fd; box-shadow: 0 4px 16px rgba(0,0,0,.08); transform: translateY(-2px); }
.vault-pending { border-left: 4px solid var(--color-warning); }
.vault-top { display: flex; justify-content: space-between; align-items: flex-start; padding: 18px 20px 0; }
.vault-info h3 { font-size: 16px; font-weight: 700; color: var(--color-text); margin: 0 0 2px; }
.vault-office { font-size: 12px; color: var(--color-text-muted); }
.count-badge {
  display: flex; align-items: center; gap: 4px;
  font-size: 11px; font-weight: 600;
  padding: 5px 12px; border-radius: var(--radius-xl); white-space: nowrap;
}
.count-badge .material-symbols-outlined { font-size: 14px; }
.badge-pending { background: #fef3c7; color: #92400e; }
.badge-ok { background: var(--color-success-bg); color: #065f46; }

/* Balance list */
.balance-list { padding: 12px 20px; flex: 1; max-height: 180px; overflow-y: auto; }
.balance-row {
  display: flex; justify-content: space-between; align-items: center;
  padding: 5px 10px; font-size: 13px; border-radius: var(--radius-sm);
}
.balance-row:nth-child(odd) { background: var(--color-bg-page); }
.bal-currency { font-weight: 600; color: var(--color-text); font-size: 12px; letter-spacing: .3px; }
.bal-amount { font-weight: 700; color: var(--color-text); font-family: 'Consolas', 'Courier New', monospace; font-size: 13px; }
.no-balance {
  display: flex; flex-direction: column; align-items: center; gap: 6px;
  font-size: 13px; color: var(--color-text-muted); text-align: center; padding: 20px 0;
}
.no-balance .material-symbols-outlined { font-size: 28px; opacity: .4; }

/* Vault footer */
.vault-footer {
  padding: 14px 20px; border-top: 1px solid var(--color-bg-page);
  background: #fafbfc; display: flex; flex-direction: column; gap: 10px;
}
.vault-meta {
  font-size: 11px; color: var(--color-text-muted); display: flex; align-items: center; gap: 4px;
}
.vault-meta .material-symbols-outlined { font-size: 14px; }
.vault-actions { display: flex; gap: 8px; }
.vault-admin-actions {
  display: flex; gap: 6px; padding-top: 8px; border-top: 1px solid var(--color-border);
}
.admin-btn {
  display: inline-flex; align-items: center; gap: 4px;
  padding: 5px 10px; border-radius: var(--radius-sm); font-size: 11px; font-weight: 500;
  cursor: pointer; border: 1px solid var(--color-border); background: var(--color-bg-page); color: var(--color-text-secondary);
  transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}
.admin-btn:hover { background: var(--color-bg-page); border-color: var(--color-border); }
.admin-btn .material-symbols-outlined { font-size: 14px; }
.admin-btn-off { color: var(--color-danger); }
.admin-btn-off:hover { background: #fef2f2; border-color: #fca5a5; }
.admin-btn-on { color: var(--color-success); }
.admin-btn-on:hover { background: #f0fdf4; border-color: #86efac; }

/* Timeline */
.timeline-filters { display: flex; gap: 12px; margin-bottom: 20px; }
.timeline-list { display: flex; flex-direction: column; gap: 0; }
.tl-item {
  display: flex; gap: 16px; padding: 16px 0;
  border-bottom: 1px solid var(--color-bg-page); position: relative;
}
.tl-marker {
  width: 40px; height: 40px; border-radius: 50%; display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}
.tl-snapshot .tl-marker { background: linear-gradient(135deg, #dbeafe, #bfdbfe); color: #1d4ed8; }
.tl-count .tl-marker { background: linear-gradient(135deg, #fef3c7, #fde68a); color: #92400e; }
.tl-marker .material-symbols-outlined { font-size: 18px; }
.tl-content { flex: 1; min-width: 0; }
.tl-header { display: flex; align-items: center; gap: 10px; margin-bottom: 6px; flex-wrap: wrap; }
.tl-type-badge { font-size: 11px; padding: 3px 12px; border-radius: var(--radius-xl); font-weight: 600; }
.badge-snapshot { background: #dbeafe; color: #1d4ed8; }
.badge-count { background: #fef3c7; color: #92400e; }
.tl-date { font-size: 12px; color: var(--color-text-secondary); font-weight: 500; }
.tl-user { font-size: 12px; color: var(--color-text-muted); }
.tl-body { font-size: 13px; color: var(--color-text); display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.tl-meta { font-size: 12px; color: var(--color-text-muted); }
.tl-vault-name { font-weight: 600; }
.tl-discrepancy { color: var(--color-danger); font-weight: 600; font-size: 12px; }
.tl-ok { color: var(--color-success); font-weight: 600; font-size: 12px; }
.tl-source-tag { font-size: 10px; padding: 2px 8px; border-radius: var(--radius-md); background: var(--color-bg-page); color: var(--color-text-secondary); font-weight: 500; }
.tl-actions { margin-top: 6px; display: flex; gap: 4px; }

/* Icon btn */
.icon-btn {
  background: none; border: 1px solid transparent; cursor: pointer; padding: 6px; border-radius: var(--radius-md);
  color: var(--color-text-secondary); display: flex; align-items: center; transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}
.icon-btn:hover { background: var(--color-bg-page); color: var(--color-secondary-hover); border-color: var(--color-border); }
.icon-btn.danger:hover { color: var(--color-danger); border-color: #fecaca; background: #fef2f2; }
.icon-btn .material-symbols-outlined { font-size: 18px; }

/* Compare */
.compare-panel { max-width: 1000px; }
.compare-hint { font-size: 13px; color: var(--color-text-secondary); margin: 0 0 16px; }
.compare-selectors { display: flex; align-items: flex-end; gap: 12px; flex-wrap: wrap; margin-bottom: 24px; }
.compare-selectors .form-group { flex: 1; min-width: 200px; }
.compare-arrow { display: flex; align-items: center; padding-bottom: 4px; color: var(--color-text-muted); }
.compare-result { margin-top: 8px; }
.compare-header-info { display: flex; align-items: center; justify-content: center; gap: 20px; margin-bottom: 16px; }
.compare-side { text-align: center; }
.compare-label { display: block; font-size: 11px; text-transform: uppercase; color: var(--color-text-muted); margin-bottom: 2px; font-weight: 600; }
.diff-row-pos { background: #f0fdf4; }
.diff-row-neg { background: #fef2f2; }

/* Table */
.table-wrap { background: #fff; border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow-x: auto; box-shadow: 0 1px 3px rgba(0,0,0,.04); }
.vc-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.vc-table th {
  padding: 12px 16px; text-align: left; font-weight: 600; color: var(--color-text-secondary);
  font-size: 11px; text-transform: uppercase; letter-spacing: .5px;
  border-bottom: 2px solid var(--color-border); background: var(--color-bg-page); white-space: nowrap;
}
.vc-table td { padding: 12px 16px; border-bottom: 1px solid var(--color-bg-page); color: var(--color-text); vertical-align: top; }
.text-right { text-align: right; }
.font-mono { font-family: 'Consolas', 'Courier New', monospace; }
.empty-row { text-align: center; color: var(--color-text-muted); padding: 40px 16px !important; }
.balance-pos { color: var(--color-success); font-weight: 600; }
.balance-neg { color: var(--color-danger); font-weight: 600; }
.discrepancy-row { background: #fef2f2; }
.diff-ok { color: var(--color-success); font-weight: 600; font-size: 13px; }
.diff-warn { color: var(--color-danger); font-weight: 600; font-size: 13px; }

.source-badge { font-size: 11px; padding: 3px 10px; border-radius: var(--radius-xl); font-weight: 600; }
.source-system { background: #e0e7ff; color: #4338ca; }
.source-manual { background: var(--color-success-bg); color: #065f46; }

.detail-list { display: flex; flex-direction: column; gap: 4px; }
.detail-item-row { display: flex; gap: 10px; font-size: 12px; color: var(--color-text-secondary); }

/* Modal */
.modal-overlay {
  position: fixed; inset: 0; background: rgba(15,23,42,.5); backdrop-filter: var(--glass-blur-strong);
  display: flex; align-items: center; justify-content: center; z-index: 1000; padding: 20px;
}
.modal {
  background: #fff; border-radius: var(--radius-xl); width: 100%; max-width: 600px;
  max-height: 90vh; display: flex; flex-direction: column;
  box-shadow: 0 25px 60px rgba(0,0,0,.2);
}
.modal-sm { max-width: 440px; }
.modal-lg { max-width: 720px; }
.modal-xl { max-width: 1000px; }
.modal-header {
  display: flex; justify-content: space-between; align-items: center;
  padding: 22px 28px; border-bottom: 1px solid var(--color-border);
}
.modal-header h2 { font-size: 17px; font-weight: 700; color: var(--color-text); margin: 0; }
.modal-close {
  background: var(--color-bg-page); border: none; width: 32px; height: 32px; border-radius: var(--radius-md);
  font-size: 18px; cursor: pointer; color: var(--color-text-secondary); display: flex; align-items: center; justify-content: center;
  transition: background-color 0.2s, color 0.2s;
}
.modal-close:hover { background: var(--color-border); color: var(--color-text); }
.modal-body { padding: 24px 28px; overflow-y: auto; }
.modal-footer {
  padding: 18px 28px; border-top: 1px solid var(--color-border);
  display: flex; justify-content: flex-end; gap: 10px;
}
.modal-hint { font-size: 13px; color: var(--color-text-secondary); margin: 0 0 16px; }

/* Form */
.form-group { display: flex; flex-direction: column; gap: 6px; }
.form-group label { font-size: 12px; font-weight: 600; color: var(--color-text); }
.form-group input, .form-group select {
  padding: 9px 14px; border: 1px solid var(--color-border); border-radius: var(--radius-md); font-size: 13px; outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-group input:focus, .form-group select:focus { border-color: var(--color-secondary-hover); box-shadow: 0 0 0 3px rgba(37,99,235,.12); }

/* Count form */
.count-form { display: flex; flex-direction: column; gap: 12px; }
.count-row {
  display: grid; grid-template-columns: 1fr 1fr 1fr auto;
  gap: 12px; align-items: end; padding: 14px;
  background: var(--color-bg-page); border-radius: var(--radius-md); border: 1px solid var(--color-border);
}
.count-currency { display: flex; flex-direction: column; }
.currency-code { font-weight: 700; font-size: 15px; color: var(--color-text); }
.currency-name { font-size: 11px; color: var(--color-text-muted); }
.count-label { font-size: 11px; color: var(--color-text-muted); display: block; margin-bottom: 2px; font-weight: 500; }
.count-amount { font-size: 14px; font-weight: 600; color: var(--color-text); font-family: 'Consolas', monospace; }
.count-actual input {
  width: 100%; padding: 7px 12px; border: 1px solid var(--color-border);
  border-radius: var(--radius-md); font-size: 13px; outline: none; transition: border-color 0.2s, box-shadow 0.2s;
}
.count-actual input:focus { border-color: var(--color-secondary-hover); box-shadow: 0 0 0 3px rgba(37,99,235,.12); }
.count-diff { min-width: 80px; }
.count-options { margin-top: 16px; padding-top: 16px; border-top: 1px solid var(--color-border); display: flex; flex-direction: column; gap: 10px; }
.checkbox-label { display: flex; align-items: center; gap: 8px; font-size: 13px; cursor: pointer; color: var(--color-text); }
.checkbox-label input[type="checkbox"] { width: 16px; height: 16px; accent-color: var(--color-secondary-hover); }

/* Detail */
.detail-meta { display: flex; gap: 20px; flex-wrap: wrap; font-size: 13px; color: var(--color-text-secondary); margin-bottom: 20px; }
.vault-group { margin-bottom: 20px; }
.vault-group-title { font-size: 14px; font-weight: 700; color: var(--color-text); margin: 0 0 10px; padding-bottom: 6px; border-bottom: 2px solid var(--color-border); }
.inner-table { border: 1px solid var(--color-border); border-radius: var(--radius-md); overflow: hidden; }
.currency-name-sm { font-size: 11px; color: var(--color-text-muted); margin-left: 4px; }
.total-row { display: flex; justify-content: flex-end; align-items: center; gap: 12px; padding: 16px 0 0; font-size: 15px; color: var(--color-text); }
.total-value { font-size: 22px; font-weight: 800; color: var(--color-text); }

/* Responsive */
@media (max-width: 768px) {
  .vc-wrap { padding: 16px; }
  .vault-grid { grid-template-columns: 1fr; }
  .count-row { grid-template-columns: 1fr 1fr; }
  .kpi-grid { grid-template-columns: repeat(2, 1fr); }
  .compare-selectors { flex-direction: column; }
  .timeline-filters { flex-direction: column; }
}
</style>
