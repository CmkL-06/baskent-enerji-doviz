<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiService from '@/services/apiservice'

const offices = ref<any[]>([])
const loading = ref(true)
const error = ref('')
const expandedId = ref<string | null>(null)
const expandedVaults = ref<any[]>([])
const expandedUsers = ref<any[]>([])
const expandedWacs = ref<Record<string, Record<string, number>>>({})

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
const pushResult = ref('')

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
  try {
    const [vaults, users] = await Promise.all([
      apiService.getVaultsByOfficeId(officeId),
      apiService.getOfficeUsers(officeId).catch(() => [])
    ])
    expandedVaults.value = Array.isArray(vaults) ? vaults : (vaults?.data ?? [])
    expandedUsers.value = Array.isArray(users) ? users : (users?.data ?? [])

    // Load WAC data per vault
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
  if (!merkez) { pushResult.value = 'Merkez ofis bulunamadı'; return }
  pushingRates.value = true; pushResult.value = ''
  try {
    const res = await apiService.pushRatesToBranches(merkez.officeId)
    pushResult.value = res.message || `${res.updatedCount} kur güncellendi`
  } catch (e: any) {
    pushResult.value = e?.response?.data?.error || 'Kur dağıtım hatası'
  } finally {
    pushingRates.value = false
    setTimeout(() => { pushResult.value = '' }, 4000)
  }
}

async function saveOffice() {
  saving.value = true
  try {
    await apiService.saveOffice(editForm.value)
    showModal.value = false
    await loadData()
  } catch (e: any) {
    alert(e?.response?.data?.error || e.message || 'Kaydetme hatası')
  } finally {
    saving.value = false
  }
}

function fmtMoney(n: number | null | undefined) {
  if (n == null) return '—'
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function officeTypeLabel(t: number) {
  if (t === 1) return 'Merkez'
  if (t === 2) return 'Şube'
  if (t === 3) return 'Bayi'
  return '—'
}

function officeTypeBadge(t: number) {
  if (t === 1) return 'badge-merkez'
  if (t === 2) return 'badge-sube'
  return 'badge-bayi'
}

function roleLabel(r: number | string) {
  const map: Record<string, string> = { '1': 'Yönetici', '2': 'Kasiyer', '3': 'İzleyici', Manager: 'Yönetici', Cashier: 'Kasiyer', Viewer: 'İzleyici' }
  return map[String(r)] ?? String(r)
}
</script>

<template>
  <div>
    <div class="ob-header">
      <h3 class="ob-title"><span class="material-symbols-outlined">store</span> Şube Yönetimi</h3>
      <div class="ob-header-actions">
        <button class="ob-btn accent" :disabled="pushingRates" @click="pushRates">
          <span class="material-symbols-outlined" :class="{ spin: pushingRates }">{{ pushingRates ? 'progress_activity' : 'sync' }}</span>
          Kurları Şubelere Dağıt
        </button>
        <button class="ob-btn primary" @click="openCreate">
          <span class="material-symbols-outlined">add</span> Yeni Şube
        </button>
      </div>
    </div>

    <div v-if="pushResult" class="ob-push-result">
      <span class="material-symbols-outlined">info</span> {{ pushResult }}
    </div>

    <div v-if="loading" class="ob-center">
      <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
    </div>
    <div v-else-if="error" class="ob-center ob-error">
      <span class="material-symbols-outlined">error</span> {{ error }}
    </div>
    <div v-else>
      <div class="ob-table-wrap">
        <table class="ob-table">
          <thead>
            <tr>
              <th></th>
              <th>Ofis Adı</th>
              <th>Tip</th>
              <th>Kasa</th>
              <th>Kullanıcı</th>
              <th>Toplam Varlık (₺)</th>
              <th>Günlük K/Z</th>
              <th>İşlem</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="o in offices" :key="o.officeId">
              <tr :class="{ 'ob-expanded-row': expandedId === o.officeId }">
                <td>
                  <button class="ob-expand-btn" @click="toggleExpand(o.officeId)">
                    <span class="material-symbols-outlined">{{ expandedId === o.officeId ? 'expand_less' : 'expand_more' }}</span>
                  </button>
                </td>
                <td>
                  <div class="ob-name-cell">
                    <span class="material-symbols-outlined" :style="{ color: o.officeType === 1 ? '#f59e0b' : '#3b82f6' }">
                      {{ o.officeType === 1 ? 'hub' : 'store' }}
                    </span>
                    <strong>{{ o.officeName }}</strong>
                  </div>
                </td>
                <td><span class="ob-badge" :class="officeTypeBadge(o.officeType)">{{ officeTypeLabel(o.officeType) }}</span></td>
                <td>{{ o.vaultCount ?? 0 }}</td>
                <td>{{ o.userCount ?? 0 }}</td>
                <td class="ob-mono">{{ fmtMoney(o.totalValueInBaseCurrency) }} ₺</td>
                <td :style="{ color: (o.dailyProfitLoss ?? 0) >= 0 ? '#16a34a' : '#ef4444' }">{{ fmtMoney(o.dailyProfitLoss) }} ₺</td>
                <td>
                  <button v-if="o.officeType !== 1" class="ob-icon-btn" title="Düzenle" @click="openEdit(o)">
                    <span class="material-symbols-outlined">edit</span>
                  </button>
                </td>
              </tr>
              <tr v-if="expandedId === o.officeId" class="ob-detail-row">
                <td colspan="8">
                  <div class="ob-detail">
                    <div class="ob-detail-section">
                      <h4><span class="material-symbols-outlined">account_balance_wallet</span> Kasalar</h4>
                      <div v-if="expandedVaults.length === 0" class="ob-detail-empty">Kasa bulunamadı</div>
                      <div v-else class="ob-vault-grid">
                        <div v-for="v in expandedVaults" :key="v.id || v.vaultId" class="ob-vault-card">
                          <strong>{{ v.name || v.vaultName }}</strong>
                          <div v-for="b in (v.balances || [])" :key="b.currencyCode" class="ob-vault-bal">
                            <span>{{ b.currencyCode }}</span>
                            <span class="ob-mono">{{ fmtMoney(b.balance) }}</span>
                            <span v-if="expandedWacs[v.id || v.vaultId]?.[b.currencyId] > 0" class="ob-wac-tag" title="WAC (Ağırlıklı Ort. Maliyet)">
                              WAC: {{ fmtMoney(expandedWacs[v.id || v.vaultId][b.currencyId]) }}
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                    <div class="ob-detail-section">
                      <h4><span class="material-symbols-outlined">group</span> Personel</h4>
                      <div v-if="expandedUsers.length === 0" class="ob-detail-empty">Personel bulunamadı</div>
                      <div v-else>
                        <div v-for="u in expandedUsers" :key="u.id || u.userId" class="ob-user-row">
                          <span class="material-symbols-outlined ob-user-icon">person</span>
                          <span>{{ u.firstname || u.userName || u.user?.firstname || '—' }}</span>
                          <span class="ob-badge badge-sube">{{ roleLabel(u.role ?? u.officeRole ?? '') }}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal -->
    <Teleport to="body">
      <div v-if="showModal" class="ob-overlay" @click.self="showModal = false">
        <div class="ob-modal">
          <div class="ob-modal-header">
            <h3>{{ editForm.id ? 'Şube Düzenle' : 'Yeni Şube Oluştur' }}</h3>
            <button class="ob-close" @click="showModal = false">&times;</button>
          </div>
          <div class="ob-modal-body">
            <div class="ob-field">
              <label>Ofis Adı *</label>
              <input v-model="editForm.officeName" type="text" class="ob-input" placeholder="Şube adı" />
            </div>
            <div class="ob-field">
              <label>Açıklama</label>
              <input v-model="editForm.officeDescription" type="text" class="ob-input" placeholder="Kısa açıklama" />
            </div>
            <div class="ob-field-row">
              <div class="ob-field">
                <label>Adres</label>
                <input v-model="editForm.address" type="text" class="ob-input" placeholder="Adres" />
              </div>
              <div class="ob-field">
                <label>Telefon</label>
                <input v-model="editForm.phone" type="text" class="ob-input" placeholder="Telefon" />
              </div>
            </div>
            <div class="ob-field-row">
              <div class="ob-field">
                <label>Tip</label>
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
                <input v-model.number="editForm.dailyTransactionLimit" type="number" class="ob-input" placeholder="Limit yok" />
              </div>
              <div class="ob-field">
                <label>Aylık İşlem Limiti (₺)</label>
                <input v-model.number="editForm.monthlyTransactionLimit" type="number" class="ob-input" placeholder="Limit yok" />
              </div>
            </div>
            <div class="ob-field-row">
              <div class="ob-field">
                <label>Kur Modu</label>
                <select v-model.number="editForm.rateInheritanceMode" class="ob-input">
                  <option :value="0">Merkez Kurunu Kullan</option>
                  <option :value="1">Özel Kur Belirle</option>
                </select>
              </div>
              <div class="ob-field">
                <label>Oto-Onay Eşiği (₺)</label>
                <input v-model.number="editForm.transferApprovalThreshold" type="number" step="100" class="ob-input" placeholder="Yok — hepsi onay gerektirir" />
              </div>
            </div>
          </div>
          <div class="ob-modal-footer">
            <button class="ob-btn secondary" @click="showModal = false">İptal</button>
            <button class="ob-btn primary" :disabled="saving || !editForm.officeName.trim()" @click="saveOffice">
              <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
              {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.ob-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
.ob-title { display: flex; align-items: center; gap: 0.5rem; font-size: 1.1rem; font-weight: 700; color: #1e293b; margin: 0; }

.ob-center { display: flex; flex-direction: column; align-items: center; gap: 0.75rem; padding: 3rem; color: #94a3b8; }
.ob-error { color: #ef4444; }
@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin 1s linear infinite; }

.ob-table-wrap { overflow-x: auto; background: white; border-radius: 12px; box-shadow: 0 1px 3px rgba(0,0,0,.08); }
.ob-table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
.ob-table th { text-align: left; padding: 0.75rem 1rem; background: #f8fafc; color: #64748b; font-weight: 600; border-bottom: 1px solid #e2e8f0; white-space: nowrap; }
.ob-table td { padding: 0.75rem 1rem; border-bottom: 1px solid #f1f5f9; vertical-align: middle; }
.ob-table tr:hover td { background: #fafbfc; }
.ob-expanded-row td { background: #f8fafc; }
.ob-mono { font-family: monospace; }

.ob-name-cell { display: flex; align-items: center; gap: 0.5rem; }
.ob-badge { padding: 2px 10px; border-radius: 20px; font-size: 0.7rem; font-weight: 600; }
.badge-merkez { background: #fffbeb; color: #b45309; }
.badge-sube { background: #eff6ff; color: #2563eb; }
.badge-bayi { background: #f0fdf4; color: #16a34a; }

.ob-expand-btn { background: none; border: none; cursor: pointer; color: #64748b; padding: 0.25rem; border-radius: 4px; }
.ob-expand-btn:hover { background: #f1f5f9; }
.ob-icon-btn { background: none; border: none; cursor: pointer; color: #64748b; padding: 0.25rem; border-radius: 4px; }
.ob-icon-btn:hover { background: #eff6ff; color: #3b82f6; }

.ob-detail-row td { padding: 0 !important; }
.ob-detail { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; padding: 1.25rem; background: #f8fafc; }
@media (max-width: 768px) { .ob-detail { grid-template-columns: 1fr; } }
.ob-detail-section h4 { display: flex; align-items: center; gap: 0.4rem; font-size: 0.9rem; font-weight: 600; color: #475569; margin: 0 0 0.75rem; }
.ob-detail-section h4 .material-symbols-outlined { font-size: 1rem; }
.ob-detail-empty { font-size: 0.8rem; color: #94a3b8; }

.ob-vault-grid { display: flex; flex-wrap: wrap; gap: 0.5rem; }
.ob-vault-card { background: white; border-radius: 8px; padding: 0.75rem; border: 1px solid #e2e8f0; min-width: 140px; font-size: 0.8rem; }
.ob-vault-card strong { display: block; margin-bottom: 0.4rem; color: #1e293b; }
.ob-vault-bal { display: flex; justify-content: space-between; gap: 0.5rem; padding: 1px 0; }
.ob-vault-bal span:first-child { color: #3b82f6; font-weight: 600; }

.ob-user-row { display: flex; align-items: center; gap: 0.5rem; padding: 0.4rem 0; font-size: 0.85rem; }
.ob-user-icon { font-size: 1rem; color: #64748b; }

/* Modal */
.ob-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.4); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.ob-modal { background: white; border-radius: 16px; width: 95%; max-width: 560px; max-height: 90vh; overflow-y: auto; }
.ob-modal-header { display: flex; align-items: center; justify-content: space-between; padding: 1.25rem 1.5rem; border-bottom: 1px solid #e2e8f0; }
.ob-modal-header h3 { margin: 0; font-size: 1.1rem; }
.ob-close { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: #64748b; }
.ob-modal-body { padding: 1.5rem; }
.ob-modal-footer { display: flex; justify-content: flex-end; gap: 0.75rem; padding: 1rem 1.5rem; border-top: 1px solid #e2e8f0; }

.ob-field { margin-bottom: 1rem; }
.ob-field label { display: block; font-size: 0.8rem; font-weight: 600; color: #475569; margin-bottom: 0.3rem; }
.ob-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.ob-input {
  width: 100%; padding: 0.6rem 0.75rem; border: 2px solid #e2e8f0;
  border-radius: 8px; font-size: 0.9rem; color: #1e293b;
  background: #f8fafc; outline: none; box-sizing: border-box;
  transition: border-color .2s;
}
.ob-input:focus { border-color: #3b82f6; background: white; }

.ob-btn {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.6rem 1.2rem; border: none; border-radius: 8px;
  cursor: pointer; font-size: 0.9rem; font-weight: 600; transition: all .2s;
}
.ob-btn.primary { background: #3b82f6; color: white; }
.ob-btn.primary:hover:not(:disabled) { background: #2563eb; }
.ob-btn.secondary { background: #f1f5f9; color: #475569; border: 1px solid #e2e8f0; }
.ob-btn.secondary:hover { background: #e2e8f0; }
.ob-btn:disabled { opacity: 0.5; cursor: not-allowed; }
.ob-btn.accent { background: #f59e0b; color: white; }
.ob-btn.accent:hover:not(:disabled) { background: #d97706; }

.ob-header-actions { display: flex; gap: 0.5rem; align-items: center; }

.ob-wac-tag {
  font-size: 0.65rem; color: #8b5cf6; background: #f5f3ff;
  padding: 1px 6px; border-radius: 4px; font-family: monospace;
  white-space: nowrap;
}

.ob-push-result {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.6rem 1rem; background: #eff6ff; color: #2563eb;
  border-radius: 8px; font-size: 0.85rem; margin-bottom: 1rem;
  border: 1px solid #bfdbfe;
}
</style>
