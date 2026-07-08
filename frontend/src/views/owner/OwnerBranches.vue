<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import apiService from '@/services/apiservice'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()

const offices = ref<any[]>([])
const loading = ref(true)
const error = ref('')
const expandedId = ref<string | null>(null)
const expandedVaults = ref<any[]>([])
const expandedUsers = ref<any[]>([])
const expandedWacs = ref<Record<string, Record<string, number>>>({})
const expandedLoading = ref(false)

const showModal = ref(false)
const editForm = ref({
  id: null as string | null,
  officeName: '',
  officeDescription: '',
  address: '',
  phone: '',
  officeType: 2,
  parentOfficeId: null as string | null,
  dailyTransactionLimit: null as number | null,
  monthlyTransactionLimit: null as number | null,
  commissionRate: null as number | null,
  rateInheritanceMode: 0,
  transferApprovalThreshold: null as number | null,
})
const saving = ref(false)
const pushingRates = ref(false)
const pushResult = ref<{ type: string; text: string } | null>(null)

const totalAssets = computed(() => offices.value.reduce((a, o) => a + (o.totalValueInBaseCurrency ?? 0), 0))
const totalVaults = computed(() => offices.value.reduce((a, o) => a + (o.vaultCount ?? 0), 0))
const totalUsers = computed(() => offices.value.reduce((a, o) => a + (o.userCount ?? 0), 0))
const totalDailyPL = computed(() => offices.value.reduce((a, o) => a + (o.dailyProfitLoss ?? 0), 0))

const sortedOffices = computed(() =>
  [...offices.value].sort((a, b) => (a.officeType === 1 ? -1 : b.officeType === 1 ? 1 : 0))
)

function getCurrencyBreakdown(o: any): { code: string; amount: number }[] {
  const bal = o.totalBalancesByCurrency
  if (!bal || typeof bal !== 'object') return []
  return Object.entries(bal)
    .filter(([, v]) => (v as number) > 0)
    .map(([code, amount]) => ({ code, amount: amount as number }))
    .sort((a, b) => b.amount - a.amount)
}

onMounted(loadData)

async function loadData() {
  loading.value = true; error.value = ''
  try {
    offices.value = await apiService.getOfficeSummaries() ?? []
  } catch (e: any) {
    error.value = e?.response?.data?.message || e.message || 'Veri yüklenemedi'
  } finally {
    loading.value = false
  }
}

async function toggleExpand(officeId: string) {
  if (expandedId.value === officeId) { expandedId.value = null; return }
  expandedId.value = officeId
  expandedLoading.value = true
  expandedVaults.value = []
  expandedUsers.value = []
  expandedWacs.value = {}
  try {
    const [vaults, users] = await Promise.all([
      apiService.getVaultsByOfficeId(officeId),
      apiService.getOfficeUsers(officeId).catch(() => [])
    ])
    expandedVaults.value = Array.isArray(vaults) ? vaults : (vaults?.data ?? [])
    expandedUsers.value = Array.isArray(users) ? users : (users?.data ?? [])

    const wacMap: Record<string, Record<string, number>> = {}
    for (const v of expandedVaults.value) {
      const vid = v.id || v.vaultId
      try {
        const wacs = await apiService.getAllWacs(vid)
        wacMap[vid] = wacs && typeof wacs === 'object' ? wacs : {}
      } catch { wacMap[vid] = {} }
    }
    expandedWacs.value = wacMap
  } catch { expandedVaults.value = []; expandedUsers.value = [] }
  finally { expandedLoading.value = false }
}

function openCreate() {
  const merkez = offices.value.find(o => o.officeType === 1)
  editForm.value = {
    id: null, officeName: '', officeDescription: '', address: '', phone: '',
    officeType: 2, parentOfficeId: merkez?.officeId ?? null,
    dailyTransactionLimit: null, monthlyTransactionLimit: null, commissionRate: null,
    rateInheritanceMode: 0, transferApprovalThreshold: null,
  }
  showModal.value = true
}

function openEdit(o: any) {
  editForm.value = {
    id: o.officeId, officeName: o.officeName, officeDescription: o.officeDescription ?? '',
    address: o.address ?? '', phone: o.phone ?? '',
    officeType: o.officeType ?? 2, parentOfficeId: o.parentOfficeId ?? null,
    dailyTransactionLimit: o.dailyTransactionLimit, monthlyTransactionLimit: o.monthlyTransactionLimit,
    commissionRate: o.commissionRate,
    rateInheritanceMode: o.rateInheritanceMode ?? 0,
    transferApprovalThreshold: o.transferApprovalThreshold ?? null,
  }
  showModal.value = true
}

async function pushRates() {
  const merkez = offices.value.find(o => o.officeType === 1)
  if (!merkez) { pushResult.value = { type: 'error', text: 'Merkez ofis bulunamadı' }; return }
  pushingRates.value = true; pushResult.value = null
  try {
    const res = await apiService.pushRatesToBranches(merkez.officeId)
    pushResult.value = { type: 'success', text: res.message || `${res.updatedCount} kur güncellendi` }
  } catch (e: any) {
    pushResult.value = { type: 'error', text: e?.response?.data?.error || 'Kur dağıtım hatası' }
  } finally {
    pushingRates.value = false
    setTimeout(() => { pushResult.value = null }, 5000)
  }
}

async function saveOffice() {
  saving.value = true
  try {
    await apiService.saveOffice(editForm.value)
    showModal.value = false
    await loadData()
  } catch (e: any) {
    notification.error(e?.response?.data?.error || e.message || 'Kaydetme hatası')
  } finally {
    saving.value = false
  }
}

function fmtMoney(n: number | null | undefined) {
  if (n == null) return '—'
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function fmtCompact(n: number | null | undefined) {
  if (n == null || n === 0) return '0'
  if (n >= 1_000_000) return (n / 1_000_000).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + 'M'
  if (n >= 1_000) return (n / 1_000).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + 'K'
  return n.toLocaleString('tr-TR', { maximumFractionDigits: 0 })
}

function officeTypeLabel(t: number) {
  if (t === 1) return 'Merkez'
  if (t === 2) return 'Şube'
  if (t === 3) return 'Bayi'
  return '—'
}

function roleLabel(r: number | string) {
  const map: Record<string, string> = {
    '1': 'Yönetici', '2': 'Kasiyer', '3': 'İzleyici',
    'Manager': 'Yönetici', 'Cashier': 'Kasiyer', 'Viewer': 'İzleyici',
    'Owner': 'Sahip', 'Admin': 'Admin', 'Staff': 'Personel',
    '100': 'Sahip', '90': 'Admin', '50': 'Personel',
    'Banned': 'Yasaklı', 'User': 'Kullanıcı',
    '0': 'Yasaklı', '10': 'Kullanıcı',
  }
  return map[String(r)] ?? String(r)
}

function roleBadgeClass(r: number | string) {
  const s = String(r)
  if (s === '100' || s === 'Owner') return 'role-owner'
  if (s === '90' || s === 'Admin' || s === '1' || s === 'Manager') return 'role-manager'
  if (s === '50' || s === 'Staff' || s === '2' || s === 'Cashier') return 'role-cashier'
  if (s === '0' || s === 'Banned') return 'role-banned'
  if (s === '10' || s === 'User') return 'role-user'
  return 'role-viewer'
}

function assetPercent(o: any) {
  if (!totalAssets.value || !o.totalValueInBaseCurrency) return 0
  return Math.round((o.totalValueInBaseCurrency / totalAssets.value) * 100)
}
</script>

<template>
  <div class="ob-root">
    <!-- Summary Stats -->
    <div class="ob-stats-row">
      <div class="ob-stat-card">
        <div class="ob-stat-icon" style="background: rgba(59, 130, 246, 0.1); color: #3b82f6;">
          <span class="material-symbols-outlined" aria-hidden="true">domain</span>
        </div>
        <div class="ob-stat-info">
          <span class="ob-stat-value">{{ offices.length }}</span>
          <span class="ob-stat-label">Toplam Şube</span>
        </div>
      </div>
      <div class="ob-stat-card">
        <div class="ob-stat-icon" style="background: rgba(168, 85, 247, 0.1); color: #a855f7;">
          <span class="material-symbols-outlined" aria-hidden="true">account_balance_wallet</span>
        </div>
        <div class="ob-stat-info">
          <span class="ob-stat-value">{{ totalVaults }}</span>
          <span class="ob-stat-label">Toplam Kasa</span>
        </div>
      </div>
      <div class="ob-stat-card">
        <div class="ob-stat-icon" style="background: rgba(34, 197, 94, 0.1); color: #22c55e;">
          <span class="material-symbols-outlined" aria-hidden="true">groups</span>
        </div>
        <div class="ob-stat-info">
          <span class="ob-stat-value">{{ totalUsers }}</span>
          <span class="ob-stat-label">Toplam Personel</span>
        </div>
      </div>
      <div class="ob-stat-card">
        <div class="ob-stat-icon" style="background: rgba(245, 158, 11, 0.1); color: #f59e0b;">
          <span class="material-symbols-outlined" aria-hidden="true">account_balance</span>
        </div>
        <div class="ob-stat-info">
          <span class="ob-stat-value ob-stat-money">{{ fmtCompact(totalAssets) }} <small>₺</small></span>
          <span class="ob-stat-label">Toplam Varlık</span>
        </div>
      </div>
    </div>

    <!-- Action Bar -->
    <div class="ob-action-bar">
      <div class="ob-action-left">
        <h3 class="ob-section-title">
          <span class="material-symbols-outlined" aria-hidden="true">store</span>
          Şube Yönetimi
        </h3>
        <span class="ob-subtitle">{{ offices.length }} ofis kayıtlı</span>
      </div>
      <div class="ob-action-right">
        <button class="ob-btn ob-btn-outline" :disabled="pushingRates" @click="pushRates">
          <span class="material-symbols-outlined" aria-hidden="true" :class="{ spin: pushingRates }">{{ pushingRates ? 'progress_activity' : 'sync' }}</span>
          <span class="ob-btn-text">Kurları Dağıt</span>
        </button>
        <button class="ob-btn ob-btn-primary" @click="openCreate">
          <span class="material-symbols-outlined" aria-hidden="true">add</span>
          <span class="ob-btn-text">Yeni Şube</span>
        </button>
      </div>
    </div>

    <!-- Push Result Toast -->
    <Transition name="toast">
      <div v-if="pushResult" :class="['ob-toast', pushResult.type]">
        <span class="material-symbols-outlined" aria-hidden="true">{{ pushResult.type === 'success' ? 'check_circle' : 'error' }}</span>
        {{ pushResult.text }}
      </div>
    </Transition>

    <!-- Loading -->
    <div v-if="loading" class="ob-loading">
      <span class="material-symbols-outlined spin">progress_activity</span>
      <span>Şube verileri yükleniyor...</span>
    </div>
    <div v-else-if="error" class="ob-loading ob-err">
      <span class="material-symbols-outlined" aria-hidden="true">error</span>
      <span>{{ error }}</span>
      <button class="ob-btn ob-btn-outline" @click="loadData">Tekrar Dene</button>
    </div>

    <!-- Office Cards -->
    <div v-else class="ob-office-list">
      <div
        v-for="o in sortedOffices"
        :key="o.officeId"
        :class="['ob-office-card', { expanded: expandedId === o.officeId, merkez: o.officeType === 1 }]"
      >
        <!-- Card Main Row -->
        <div class="ob-office-main" @click="toggleExpand(o.officeId)">
          <!-- Office Identity -->
          <div class="ob-office-identity">
            <div :class="['ob-office-icon', o.officeType === 1 ? 'icon-merkez' : 'icon-sube']">
              <span class="material-symbols-outlined" aria-hidden="true">{{ o.officeType === 1 ? 'hub' : 'store' }}</span>
            </div>
            <div class="ob-office-name-block">
              <div class="ob-office-name">{{ o.officeName }}</div>
              <div class="ob-office-meta">
                <span :class="['ob-type-badge', 'type-' + o.officeType]">{{ officeTypeLabel(o.officeType) }}</span>
                <span class="ob-active-dot" :class="o.isActive !== false ? 'active' : 'inactive'"></span>
                <span class="ob-active-text">{{ o.isActive !== false ? 'Aktif' : 'Pasif' }}</span>
              </div>
            </div>
          </div>

          <!-- Stats Grid -->
          <div class="ob-office-stats">
            <div class="ob-office-stat">
              <span class="ob-stat-num">{{ o.vaultCount ?? 0 }}</span>
              <span class="ob-stat-lbl">Kasa</span>
            </div>
            <div class="ob-office-stat">
              <span class="ob-stat-num">{{ o.userCount ?? 0 }}</span>
              <span class="ob-stat-lbl">Personel</span>
            </div>
            <div class="ob-office-stat stat-wide">
              <span class="ob-stat-num ob-mono">{{ fmtMoney(o.totalValueInBaseCurrency) }} <small>₺</small></span>
              <span class="ob-stat-lbl">Toplam Varlık</span>
            </div>
            <div class="ob-office-stat">
              <span
                class="ob-stat-num ob-mono"
                :style="{ color: (o.dailyProfitLoss ?? 0) >= 0 ? '#22c55e' : '#ef4444' }"
              >
                {{ (o.dailyProfitLoss ?? 0) >= 0 ? '+' : '' }}{{ fmtMoney(o.dailyProfitLoss) }} ₺
              </span>
              <span class="ob-stat-lbl">Günlük K/Z</span>
            </div>
          </div>

          <!-- Actions -->
          <div class="ob-office-actions" @click.stop>
            <button
              v-if="o.officeType !== 1"
              class="ob-icon-btn"
              title="Düzenle"
              @click="openEdit(o)"
            >
              <span class="material-symbols-outlined" aria-hidden="true">edit</span>
            </button>
            <button
              class="ob-icon-btn ob-expand-toggle"
              :class="{ rotated: expandedId === o.officeId }"
              @click="toggleExpand(o.officeId)"
            >
              <span class="material-symbols-outlined" aria-hidden="true">expand_more</span>
            </button>
          </div>
        </div>

        <!-- Asset Bar -->
        <div v-if="totalAssets > 0" class="ob-asset-bar-wrap">
          <div class="ob-asset-bar">
            <div
              class="ob-asset-fill"
              :style="{ width: assetPercent(o) + '%' }"
            ></div>
          </div>
          <span class="ob-asset-pct">%{{ assetPercent(o) }}</span>
        </div>

        <!-- Currency Chips -->
        <div v-if="getCurrencyBreakdown(o).length" class="ob-currency-chips">
          <div
            v-for="c in getCurrencyBreakdown(o)"
            :key="c.code"
            class="ob-currency-chip"
          >
            <span class="ob-chip-code">{{ c.code }}</span>
            <span class="ob-chip-val ob-mono">{{ fmtMoney(c.amount) }}</span>
          </div>
        </div>

        <!-- Expanded Detail -->
        <Transition name="detail">
          <div v-if="expandedId === o.officeId" class="ob-office-detail">
            <div v-if="expandedLoading" class="ob-detail-loading">
              <span class="material-symbols-outlined spin">progress_activity</span>
              Detaylar yükleniyor...
            </div>
            <template v-else>
              <div class="ob-detail-grid">
                <!-- Kasalar -->
                <div class="ob-detail-section">
                  <div class="ob-detail-header">
                    <span class="material-symbols-outlined" aria-hidden="true">account_balance_wallet</span>
                    <h4>Kasalar</h4>
                    <span class="ob-detail-count">{{ expandedVaults.length }}</span>
                  </div>
                  <div v-if="expandedVaults.length === 0" class="ob-detail-empty">
                    <span class="material-symbols-outlined" aria-hidden="true">inbox</span>
                    Bu ofise ait kasa bulunamadı
                  </div>
                  <div v-else class="ob-vault-list">
                    <div v-for="v in expandedVaults" :key="v.id || v.vaultId" class="ob-vault-item">
                      <div class="ob-vault-name">
                        <span class="material-symbols-outlined" aria-hidden="true">lock</span>
                        {{ v.name || v.vaultName }}
                      </div>
                      <div class="ob-vault-balances">
                        <div
                          v-for="b in (v.balances || []).filter((x: any) => x.balance > 0)"
                          :key="b.currencyCode"
                          class="ob-bal-row"
                        >
                          <span class="ob-bal-code">{{ b.currencyCode }}</span>
                          <span class="ob-bal-amount ob-mono">{{ fmtMoney(b.balance) }}</span>
                          <span
                            v-if="expandedWacs[v.id || v.vaultId]?.[b.currencyId] > 0"
                            class="ob-wac-chip"
                            title="Ağırlıklı Ortalama Maliyet"
                          >
                            WAC {{ fmtMoney(expandedWacs[v.id || v.vaultId][b.currencyId]) }}
                          </span>
                        </div>
                        <div
                          v-if="!(v.balances || []).some((x: any) => x.balance > 0)"
                          class="ob-bal-empty"
                        >
                          Bakiye bulunmuyor
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Personel -->
                <div class="ob-detail-section">
                  <div class="ob-detail-header">
                    <span class="material-symbols-outlined" aria-hidden="true">group</span>
                    <h4>Personel</h4>
                    <span class="ob-detail-count">{{ expandedUsers.length }}</span>
                  </div>
                  <div v-if="expandedUsers.length === 0" class="ob-detail-empty">
                    <span class="material-symbols-outlined" aria-hidden="true">person_off</span>
                    Bu ofise atanmış personel yok
                  </div>
                  <div v-else class="ob-user-list">
                    <div v-for="u in expandedUsers" :key="u.id || u.userId" class="ob-user-item">
                      <div class="ob-user-avatar">
                        {{ (u.firstname || u.userName || u.user?.firstname || '?')[0].toUpperCase() }}
                      </div>
                      <div class="ob-user-info">
                        <span class="ob-user-name">{{ u.firstname || u.userName || u.user?.firstname || '—' }}</span>
                        <span class="ob-user-mail">{{ u.mail || u.email || u.user?.mail || '' }}</span>
                      </div>
                      <span :class="['ob-role-badge', roleBadgeClass(u.rank ?? u.role ?? u.officeRole ?? '')]">
                        {{ roleLabel(u.rank ?? u.role ?? u.officeRole ?? '') }}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </template>
          </div>
        </Transition>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <Teleport to="body">
      <Transition name="modal">
        <div v-if="showModal" class="ob-overlay" @click.self="showModal = false">
          <div class="ob-modal">
            <div class="ob-modal-header">
              <div class="ob-modal-title-block">
                <span class="material-symbols-outlined" aria-hidden="true">{{ editForm.id ? 'edit' : 'add_business' }}</span>
                <h3>{{ editForm.id ? 'Şube Düzenle' : 'Yeni Şube Oluştur' }}</h3>
              </div>
              <button class="ob-modal-close" @click="showModal = false">
                <span class="material-symbols-outlined" aria-hidden="true">close</span>
              </button>
            </div>
            <div class="ob-modal-body">
              <div class="ob-form-section">
                <div class="ob-form-section-title">Temel Bilgiler</div>
                <div class="ob-field">
                  <label>Ofis Adı <span class="req">*</span></label>
                  <input v-model="editForm.officeName" type="text" class="ob-input" placeholder="Şube adı giriniz" />
                </div>
                <div class="ob-field">
                  <label>Açıklama</label>
                  <input v-model="editForm.officeDescription" type="text" class="ob-input" placeholder="Kısa açıklama (opsiyonel)" />
                </div>
                <div class="ob-field-row">
                  <div class="ob-field">
                    <label>Adres</label>
                    <input v-model="editForm.address" type="text" class="ob-input" placeholder="Adres giriniz" />
                  </div>
                  <div class="ob-field">
                    <label>Telefon</label>
                    <input v-model="editForm.phone" type="text" class="ob-input" placeholder="+90 xxx xxx xx xx" />
                  </div>
                </div>
              </div>
              <div class="ob-form-section">
                <div class="ob-form-section-title">Yapılandırma</div>
                <div class="ob-field-row">
                  <div class="ob-field">
                    <label>Ofis Tipi</label>
                    <select v-model.number="editForm.officeType" class="ob-input">
                      <option :value="2">Şube</option>
                      <option :value="3">Bayi</option>
                    </select>
                  </div>
                  <div class="ob-field">
                    <label>Komisyon Oranı (%)</label>
                    <input v-model.number="editForm.commissionRate" type="number" step="0.01" class="ob-input" placeholder="0.00" />
                  </div>
                </div>
                <div class="ob-field-row">
                  <div class="ob-field">
                    <label>Günlük İşlem Limiti (₺)</label>
                    <input v-model.number="editForm.dailyTransactionLimit" type="number" class="ob-input" placeholder="Boş = sınırsız" min="0" />
                  </div>
                  <div class="ob-field">
                    <label>Aylık İşlem Limiti (₺)</label>
                    <input v-model.number="editForm.monthlyTransactionLimit" type="number" class="ob-input" placeholder="Boş = sınırsız" min="0" />
                  </div>
                </div>
              </div>
              <div class="ob-form-section">
                <div class="ob-form-section-title">Kur & Onay Ayarları</div>
                <div class="ob-field-row">
                  <div class="ob-field">
                    <label>Kur Modu</label>
                    <select v-model.number="editForm.rateInheritanceMode" class="ob-input">
                      <option :value="0">Merkez Kurunu Kullan</option>
                      <option :value="1">Özel Kur Belirle</option>
                    </select>
                  </div>
                  <div class="ob-field">
                    <label>Transfer Modu</label>
                    <input type="text" class="ob-input" value="Otomatik (anında tamamlanır)" disabled style="background:#f0fdf4;color:#16a34a;font-weight:500;" />
                  </div>
                </div>
              </div>
            </div>
            <div class="ob-modal-footer">
              <button class="ob-btn ob-btn-ghost" @click="showModal = false">İptal</button>
              <button class="ob-btn ob-btn-primary" :disabled="saving || !editForm.officeName.trim()" @click="saveOffice">
                <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
                <span class="material-symbols-outlined" aria-hidden="true" v-else>save</span>
                {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
/* ── Base ────────────────────────────────── */
.ob-root { display: flex; flex-direction: column; gap: 1rem; }

/* ── Summary Stats ───────────────────────── */
.ob-stats-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.75rem;
}
@media (max-width: 900px) { .ob-stats-row { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 500px) { .ob-stats-row { grid-template-columns: 1fr; } }

.ob-stat-card {
  display: flex; align-items: center; gap: 0.75rem;
  background: #fff; border-radius: var(--radius-lg); padding: 1rem 1.25rem;
  border: 1px solid var(--color-border);
  transition: box-shadow .2s;
}
.ob-stat-card:hover { box-shadow: 0 2px 8px rgba(0,0,0,.06); }
.ob-stat-icon {
  width: 44px; height: 44px; border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}
.ob-stat-icon .material-symbols-outlined { font-size: 22px; }
.ob-stat-info { display: flex; flex-direction: column; min-width: 0; }
.ob-stat-value { font-size: 1.35rem; font-weight: 700; color: var(--color-text); line-height: 1.2; }
.ob-stat-money small { font-size: 0.7em; font-weight: 500; color: var(--color-text-secondary); }
.ob-stat-label { font-size: 0.75rem; color: var(--color-text-muted); font-weight: 500; margin-top: 2px; }

/* ── Action Bar ──────────────────────────── */
.ob-action-bar {
  display: flex; align-items: center; justify-content: space-between;
  gap: 1rem; flex-wrap: wrap;
}
.ob-action-left { display: flex; align-items: baseline; gap: 0.75rem; }
.ob-section-title {
  display: flex; align-items: center; gap: 0.4rem;
  font-size: 1.05rem; font-weight: 700; color: var(--color-text); margin: 0;
}
.ob-section-title .material-symbols-outlined { font-size: 20px; color: var(--color-secondary); }
.ob-subtitle { font-size: 0.8rem; color: var(--color-text-muted); }
.ob-action-right { display: flex; gap: 0.5rem; }

/* ── Buttons ─────────────────────────────── */
.ob-btn {
  display: inline-flex; align-items: center; gap: 0.35rem;
  padding: 0.55rem 1rem; border: none; border-radius: var(--radius-md);
  cursor: pointer; font-size: 0.85rem; font-weight: 600;
  transition: background-color 0.2s, color 0.2s, border-color 0.2s; white-space: nowrap;
}
.ob-btn .material-symbols-outlined { font-size: 18px; }
.ob-btn:disabled { opacity: 0.5; cursor: not-allowed; }

.ob-btn-primary { background: var(--color-secondary); color: #fff; }
.ob-btn-primary:hover:not(:disabled) { background: var(--color-secondary-hover); }
.ob-btn-outline {
  background: #fff; color: var(--color-text-secondary);
  border: 1px solid #d1d5db;
}
.ob-btn-outline:hover:not(:disabled) { background: var(--color-bg-page); border-color: var(--color-text-muted); }
.ob-btn-ghost { background: transparent; color: var(--color-text-secondary); }
.ob-btn-ghost:hover { background: var(--color-bg-page); }

@media (max-width: 600px) {
  .ob-btn-text { display: none; }
  .ob-btn { padding: 0.55rem 0.7rem; }
}

/* ── Toast ────────────────────────────────── */
.ob-toast {
  display: flex; align-items: center; gap: 0.5rem;
  padding: 0.7rem 1rem; border-radius: var(--radius-md);
  font-size: 0.85rem; font-weight: 500;
}
.ob-toast.success { background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0; }
.ob-toast.error { background: #fef2f2; color: var(--color-danger); border: 1px solid #fecaca; }
.ob-toast .material-symbols-outlined { font-size: 18px; }
.toast-enter-active, .toast-leave-active { transition: opacity 0.3s ease, transform 0.3s ease; }
.toast-enter-from, .toast-leave-to { opacity: 0; transform: translateY(-8px); }

/* ── Loading ─────────────────────────────── */
.ob-loading {
  display: flex; flex-direction: column; align-items: center; gap: 0.75rem;
  padding: 3rem; color: var(--color-text-muted); font-size: 0.9rem;
}
.ob-err { color: var(--color-danger); }
@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin .8s linear infinite; }

/* ── Office Card ─────────────────────────── */
.ob-office-list { display: flex; flex-direction: column; gap: 0.5rem; }

.ob-office-card {
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  transition: box-shadow .2s, border-color .2s;
}
.ob-office-card:hover { box-shadow: 0 2px 12px rgba(0,0,0,.05); }
.ob-office-card.expanded { border-color: #bfdbfe; box-shadow: 0 2px 12px rgba(59,130,246,.08); }
.ob-office-card.merkez { border-left: 3px solid var(--color-warning); }

.ob-office-main {
  display: flex; align-items: center; gap: 1rem;
  padding: 1rem 1.25rem;
  cursor: pointer;
  transition: background .15s;
}
.ob-office-main:hover { background: #fafbfc; }

/* Identity */
.ob-office-identity { display: flex; align-items: center; gap: 0.75rem; min-width: 200px; flex-shrink: 0; }
.ob-office-icon {
  width: 40px; height: 40px; border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}
.ob-office-icon .material-symbols-outlined { font-size: 20px; }
.icon-merkez { background: #fffbeb; color: var(--color-warning); }
.icon-sube { background: var(--color-secondary-light); color: var(--color-secondary); }

.ob-office-name-block { min-width: 0; }
.ob-office-name { font-weight: 600; color: var(--color-text); font-size: 0.95rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.ob-office-meta { display: flex; align-items: center; gap: 0.5rem; margin-top: 3px; }

.ob-type-badge {
  font-size: 0.65rem; font-weight: 700; padding: 1px 8px;
  border-radius: var(--radius-sm); text-transform: uppercase; letter-spacing: 0.5px;
}
.type-1 { background: #fffbeb; color: #b45309; }
.type-2 { background: var(--color-secondary-light); color: var(--color-secondary-hover); }
.type-3 { background: #f0fdf4; color: #16a34a; }

.ob-active-dot {
  width: 6px; height: 6px; border-radius: 50%;
}
.ob-active-dot.active { background: #22c55e; box-shadow: 0 0 0 2px rgba(34,197,94,.2); }
.ob-active-dot.inactive { background: var(--color-danger); }
.ob-active-text { font-size: 0.7rem; color: var(--color-text-muted); }

/* Stats */
.ob-office-stats {
  display: flex; align-items: center; gap: 1.5rem;
  flex: 1; justify-content: flex-end;
}
.ob-office-stat { display: flex; flex-direction: column; align-items: center; min-width: 60px; }
.ob-office-stat.stat-wide { min-width: 120px; align-items: flex-end; }
.ob-stat-num { font-size: 0.95rem; font-weight: 700; color: var(--color-text); }
.ob-stat-num small { font-size: 0.7em; font-weight: 500; color: var(--color-text-muted); }
.ob-stat-lbl { font-size: 0.65rem; color: var(--color-text-muted); font-weight: 500; margin-top: 1px; }
.ob-mono { font-variant-numeric: tabular-nums; }

/* Actions */
.ob-office-actions { display: flex; align-items: center; gap: 0.25rem; margin-left: 0.5rem; }
.ob-icon-btn {
  width: 32px; height: 32px; display: flex; align-items: center; justify-content: center;
  background: none; border: none; cursor: pointer; color: var(--color-text-muted);
  border-radius: var(--radius-sm); transition: background-color 0.15s, color 0.15s;
}
.ob-icon-btn:hover { background: var(--color-bg-page); color: var(--color-secondary); }
.ob-icon-btn .material-symbols-outlined { font-size: 18px; }
.ob-expand-toggle .material-symbols-outlined { transition: transform .25s ease; }
.ob-expand-toggle.rotated .material-symbols-outlined { transform: rotate(180deg); }

/* Asset Bar */
.ob-asset-bar-wrap {
  display: flex; align-items: center; gap: 0.5rem;
  padding: 0 1.25rem 0.5rem;
}
.ob-asset-bar {
  flex: 1; height: 4px; background: var(--color-bg-page); border-radius: 2px; overflow: hidden;
}
.ob-asset-fill {
  height: 100%; border-radius: 2px;
  background: linear-gradient(90deg, #3b82f6, #6366f1);
  transition: width .5s ease;
}
.ob-asset-pct { font-size: 0.65rem; color: var(--color-text-muted); font-weight: 600; min-width: 28px; text-align: right; }

/* Currency Chips */
.ob-currency-chips {
  display: flex; flex-wrap: wrap; gap: 0.35rem;
  padding: 0 1.25rem 0.75rem;
}
.ob-currency-chip {
  display: inline-flex; align-items: center; gap: 0.35rem;
  background: var(--color-bg-page); border: 1px solid var(--color-border);
  padding: 3px 8px; border-radius: var(--radius-sm);
  font-size: 0.72rem;
}
.ob-chip-code { font-weight: 700; color: var(--color-secondary); }
.ob-chip-val { color: var(--color-text-secondary); }

/* ── Expanded Detail ─────────────────────── */
.detail-enter-active, .detail-leave-active { transition: opacity 0.25s ease, max-height 0.25s ease; }
.detail-enter-from, .detail-leave-to { opacity: 0; max-height: 0; }
.detail-enter-to, .detail-leave-from { opacity: 1; max-height: 600px; }

.ob-office-detail {
  border-top: 1px solid var(--color-border);
  background: #f9fafb;
  overflow: hidden;
}
.ob-detail-loading {
  display: flex; align-items: center; justify-content: center; gap: 0.5rem;
  padding: 2rem; color: var(--color-text-muted); font-size: 0.85rem;
}
.ob-detail-grid {
  display: grid; grid-template-columns: 1fr 1fr; gap: 1px;
  background: var(--color-border);
}
@media (max-width: 768px) { .ob-detail-grid { grid-template-columns: 1fr; } }

.ob-detail-section { background: #f9fafb; padding: 1.25rem; }
.ob-detail-header {
  display: flex; align-items: center; gap: 0.4rem;
  margin-bottom: 0.75rem;
}
.ob-detail-header .material-symbols-outlined { font-size: 18px; color: var(--color-text-secondary); }
.ob-detail-header h4 { margin: 0; font-size: 0.85rem; font-weight: 700; color: var(--color-text-secondary); }
.ob-detail-count {
  margin-left: auto; font-size: 0.65rem; font-weight: 700;
  background: var(--color-border); color: var(--color-text-secondary); padding: 1px 6px; border-radius: var(--radius-md);
}
.ob-detail-empty {
  display: flex; align-items: center; gap: 0.5rem;
  font-size: 0.8rem; color: var(--color-text-muted); padding: 1rem 0;
}
.ob-detail-empty .material-symbols-outlined { font-size: 18px; }

/* Vault Items */
.ob-vault-list { display: flex; flex-direction: column; gap: 0.5rem; }
.ob-vault-item {
  background: #fff; border-radius: var(--radius-md); padding: 0.75rem;
  border: 1px solid var(--color-border);
}
.ob-vault-name {
  display: flex; align-items: center; gap: 0.35rem;
  font-weight: 600; font-size: 0.82rem; color: var(--color-text); margin-bottom: 0.5rem;
}
.ob-vault-name .material-symbols-outlined { font-size: 16px; color: var(--color-text-secondary); }
.ob-vault-balances { display: flex; flex-direction: column; gap: 3px; }
.ob-bal-row {
  display: flex; align-items: center; gap: 0.5rem;
  font-size: 0.78rem; padding: 2px 0;
}
.ob-bal-code { font-weight: 700; color: var(--color-secondary); min-width: 40px; }
.ob-bal-amount { color: var(--color-text); }
.ob-wac-chip {
  font-size: 0.6rem; color: #7c3aed; background: #f5f3ff;
  padding: 1px 5px; border-radius: 3px; white-space: nowrap;
}
.ob-bal-empty { font-size: 0.75rem; color: var(--color-border); font-style: italic; }

/* User Items */
.ob-user-list { display: flex; flex-direction: column; gap: 0.35rem; }
.ob-user-item {
  display: flex; align-items: center; gap: 0.6rem;
  padding: 0.5rem 0.6rem; background: #fff; border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
}
.ob-user-avatar {
  width: 32px; height: 32px; border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #3b82f6);
  color: #fff; font-size: 0.75rem; font-weight: 700;
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}
.ob-user-info { flex: 1; min-width: 0; display: flex; flex-direction: column; }
.ob-user-name { font-size: 0.82rem; font-weight: 600; color: var(--color-text); }
.ob-user-mail { font-size: 0.7rem; color: var(--color-text-muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.ob-role-badge {
  font-size: 0.65rem; font-weight: 700; padding: 2px 8px;
  border-radius: var(--radius-sm); white-space: nowrap;
}
.role-owner { background: #fce7f3; color: #be185d; }
.role-manager { background: #fef3c7; color: #b45309; }
.role-cashier { background: var(--color-secondary-light); color: var(--color-secondary-hover); }
.role-banned { background: #fef2f2; color: var(--color-danger); }
.role-user { background: #f0fdf4; color: #15803d; }
.role-viewer { background: var(--color-bg-page); color: var(--color-text-secondary); }

/* ── Modal ───────────────────────────────── */
.ob-overlay {
  position: fixed; inset: 0; background: rgba(15,23,42,.5);
  display: flex; align-items: center; justify-content: center;
  z-index: 1000; backdrop-filter: blur(2px);
}
.ob-modal {
  background: #fff; border-radius: var(--radius-lg);
  width: 95%; max-width: 580px; max-height: 90vh;
  overflow: hidden; display: flex; flex-direction: column;
  box-shadow: 0 20px 60px rgba(0,0,0,.15);
}
.ob-modal-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 1.25rem 1.5rem; border-bottom: 1px solid var(--color-border);
}
.ob-modal-title-block { display: flex; align-items: center; gap: 0.5rem; }
.ob-modal-title-block .material-symbols-outlined { font-size: 22px; color: var(--color-secondary); }
.ob-modal-title-block h3 { margin: 0; font-size: 1.05rem; font-weight: 700; color: var(--color-text); }
.ob-modal-close {
  width: 32px; height: 32px; display: flex; align-items: center; justify-content: center;
  background: none; border: none; cursor: pointer; color: var(--color-text-muted);
  border-radius: var(--radius-sm); transition: background-color 0.15s, color 0.15s;
}
.ob-modal-close:hover { background: var(--color-bg-page); color: var(--color-text-secondary); }
.ob-modal-close .material-symbols-outlined { font-size: 20px; }
.ob-modal-body { padding: 1.5rem; overflow-y: auto; flex: 1; }
.ob-modal-footer {
  display: flex; justify-content: flex-end; gap: 0.5rem;
  padding: 1rem 1.5rem; border-top: 1px solid var(--color-border);
  background: #f9fafb;
}

/* Form */
.ob-form-section { margin-bottom: 1.25rem; }
.ob-form-section:last-child { margin-bottom: 0; }
.ob-form-section-title {
  font-size: 0.7rem; font-weight: 700; color: var(--color-text-muted);
  text-transform: uppercase; letter-spacing: 0.8px;
  margin-bottom: 0.75rem;
  padding-bottom: 0.4rem;
  border-bottom: 1px solid var(--color-bg-page);
}
.ob-field { margin-bottom: 0.75rem; }
.ob-field:last-child { margin-bottom: 0; }
.ob-field label {
  display: block; font-size: 0.78rem; font-weight: 600;
  color: var(--color-text-secondary); margin-bottom: 0.3rem;
}
.ob-field label .req { color: var(--color-danger); }
.ob-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; margin-bottom: 0.75rem; }
@media (max-width: 500px) { .ob-field-row { grid-template-columns: 1fr; } }

.ob-input {
  width: 100%; padding: 0.55rem 0.75rem;
  border: 1.5px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 0.85rem; color: var(--color-text); background: #fff;
  outline: none; box-sizing: border-box;
  transition: border-color .2s, box-shadow .2s;
}
.ob-input:focus { border-color: var(--color-secondary); box-shadow: 0 0 0 3px rgba(59,130,246,.1); }
.ob-input::placeholder { color: var(--color-border); }

/* Modal animation */
.modal-enter-active, .modal-leave-active { transition: opacity .2s ease; }
.modal-enter-active .ob-modal, .modal-leave-active .ob-modal { transition: transform .2s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; }
.modal-enter-from .ob-modal { transform: scale(0.95) translateY(10px); }
.modal-leave-to .ob-modal { transform: scale(0.95) translateY(10px); }

/* ── Responsive: Hide stat columns on small screens ── */
@media (max-width: 768px) {
  .ob-office-main { flex-wrap: wrap; }
  .ob-office-identity { width: 100%; }
  .ob-office-stats { width: 100%; justify-content: flex-start; gap: 1rem; flex-wrap: wrap; }
  .ob-office-stat.stat-wide { align-items: flex-start; }
  .ob-office-actions { position: absolute; top: 1rem; right: 1rem; }
  .ob-office-main { position: relative; }
}
</style>
