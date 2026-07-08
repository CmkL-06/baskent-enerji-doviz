<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'
import AppKpiCard from '@/components/common/AppKpiCard.vue'
import AppPageHeader from '@/components/common/AppPageHeader.vue'
import AppEmptyState from '@/components/common/AppEmptyState.vue'

const authStore = useAuthStore()

const users   = ref<any[]>([])
const offices = ref<any[]>([])
const loading = ref(false)
const error   = ref('')
const search  = ref('')

const showModal   = ref(false)
const selected    = ref<any>(null)
const activeTab   = ref<'info' | 'password' | 'offices'>('info')

const saving    = ref(false)
const saveError = ref('')
const saveOk    = ref(false)

const editForm   = ref({ username: '', mail: '', firstname: '', lastname: '', rank: 1 })
const pwForm     = ref({ newPassword: '', confirm: '' })
const createForm = ref({ username: '', mail: '', password: '', firstname: '', lastname: '' })
const createMode = ref(false)

const userOffices = ref<any[]>([])
const officeSaving = ref(false)

const RANKS = [
  { value: 0,   label: 'Yasaklı',   key: 'Banned',   color: '#ef4444', bg: '#fef2f2', ring: '#fca5a5' },
  { value: 1,   label: 'Kullanıcı', key: 'User',     color: '#6b7280', bg: '#f9fafb', ring: '#d1d5db' },
  { value: 2,   label: 'Müşteri',   key: 'Customer', color: '#0ea5e9', bg: '#f0f9ff', ring: '#7dd3fc' },
  { value: 50,  label: 'Personel',  key: 'Staff',    color: '#8b5cf6', bg: '#f5f3ff', ring: '#c4b5fd' },
  { value: 99,  label: 'Admin',     key: 'Admin',    color: '#dc2626', bg: '#fef2f2', ring: '#fca5a5' },
  { value: 100, label: 'Owner',     key: 'Owner',    color: '#d97706', bg: '#fffbeb', ring: '#fcd34d' },
]

function rankInfo(rank: any) {
  return RANKS.find(r => r.label === rank || r.value === rank || r.key === rank) ?? RANKS[1]
}

function rankValue(rank: any) {
  return typeof rank === 'number' ? rank : (RANKS.find(r => r.label === rank || r.key === rank)?.value ?? 1)
}

function avatarColor(name: string) {
  const colors = ['#6366f1','#8b5cf6','#ec4899','#f97316','#14b8a6','#0ea5e9','#84cc16','#ef4444']
  return colors[(name?.charCodeAt(0) ?? 0) % colors.length]
}

const filteredUsers = computed(() => {
  const q = search.value.toLowerCase()
  if (!q) return users.value
  return users.value.filter(u =>
    u.username?.toLowerCase().includes(q) ||
    u.mail?.toLowerCase().includes(q) ||
    (u.firstname + ' ' + u.lastname).toLowerCase().includes(q)
  )
})

const assignedIds = computed(() => new Set(userOffices.value.map((o: any) => o.officeId ?? o.id)))

const adminCount = computed(() => users.value.filter(u => rankValue(u.rank) >= 99).length)
const staffCount = computed(() => users.value.filter(u => rankValue(u.rank) === 50).length)

async function load() {
  loading.value = true; error.value = ''
  try {
    const [u, o] = await Promise.all([apiService.getUsers(), apiService.getOffices()])
    users.value = u ?? []
    offices.value = o ?? []
  } catch (e: any) {
    error.value = e.response?.data?.message || 'Veriler yüklenemedi'
  } finally { loading.value = false }
}

async function selectUser(user: any) {
  selected.value = user
  createMode.value = false
  activeTab.value = 'info'
  saveError.value = ''; saveOk.value = false
  editForm.value = {
    username:  user.username ?? '',
    mail:      user.mail ?? '',
    firstname: user.firstname ?? '',
    lastname:  user.lastname ?? '',
    rank:      rankValue(user.rank)
  }
  pwForm.value = { newPassword: '', confirm: '' }
  userOffices.value = []
  showModal.value = true
  try {
    const res = await apiService.getUserOffices(user.id)
    userOffices.value = res ?? []
  } catch { userOffices.value = [] }
}

function openCreateMode() {
  selected.value = null
  createMode.value = true
  activeTab.value = 'info'
  saveError.value = ''; saveOk.value = false
  createForm.value = { username: '', mail: '', password: '', firstname: '', lastname: '' }
  showModal.value = true
}

function closeModal() {
  showModal.value = false
  selected.value = null
  createMode.value = false
}

function switchTab(tab: 'info' | 'password' | 'offices') {
  activeTab.value = tab
  saveError.value = ''; saveOk.value = false
}

async function saveInfo() {
  saveError.value = ''; saveOk.value = false; saving.value = true
  try {
    await apiService.updateUser({ Id: selected.value.id, ...editForm.value })
    saveOk.value = true
    await load()
    const updated = users.value.find(u => u.id === selected.value.id)
    if (updated) { selected.value = updated }
  } catch (e: any) {
    saveError.value = e.response?.data?.message || 'Kaydedilemedi'
  } finally { saving.value = false }
}

async function createUser() {
  saveError.value = ''; saveOk.value = false
  const f = createForm.value
  if (!f.username || !f.mail || !f.password) { saveError.value = 'Kullanıcı adı, e-posta ve şifre zorunludur.'; return }
  saving.value = true
  try {
    await apiService.registerUser(f)
    saveOk.value = true
    await load()
    setTimeout(() => { closeModal() }, 1200)
  } catch (e: any) {
    saveError.value = e.response?.data?.message || 'Kullanıcı oluşturulamadı'
  } finally { saving.value = false }
}

async function savePassword() {
  saveError.value = ''; saveOk.value = false
  if (!pwForm.value.newPassword) { saveError.value = 'Şifre boş olamaz'; return }
  if (pwForm.value.newPassword !== pwForm.value.confirm) { saveError.value = 'Şifreler eşleşmiyor'; return }
  saving.value = true
  try {
    await apiService.changeUserPassword(selected.value.id, pwForm.value.newPassword)
    saveOk.value = true
    pwForm.value = { newPassword: '', confirm: '' }
  } catch (e: any) {
    saveError.value = e.response?.data?.message || 'Şifre değiştirilemedi'
  } finally { saving.value = false }
}

async function toggleOffice(office: any) {
  if (!selected.value || !authStore.isAdmin) return
  const officeId = office.officeId ?? office.id
  officeSaving.value = true
  try {
    if (assignedIds.value.has(officeId)) {
      await apiService.removeOfficeFromUser(selected.value.id, officeId)
      userOffices.value = userOffices.value.filter((o: any) => (o.officeId ?? o.id) !== officeId)
    } else {
      await apiService.attachOfficeToUser({ userId: selected.value.id, officeId })
      userOffices.value = [...userOffices.value, { officeId, ...office }]
    }
  } catch (e: any) {
    error.value = e.response?.data?.message || 'İşlem başarısız'
    setTimeout(() => error.value = '', 3000)
  } finally { officeSaving.value = false }
}

onMounted(load)
</script>

<template>
  <div class="uy-page">
    <AppPageHeader icon="manage_accounts" title="Kullanıcı Yönetimi" :subtitle="`${users.length} kullanıcı · ${offices.length} ofis`">
      <button v-if="authStore.isOwner" class="btn-primary" @click="openCreateMode">
        <span class="material-symbols-outlined" aria-hidden="true">person_add</span> Yeni Kullanıcı
      </button>
    </AppPageHeader>

    <!-- KPIs -->
    <div class="kpi-grid">
      <AppKpiCard icon="group" label="Toplam Kullanıcı" :value="users.length" color="#6366f1" bg="#eef2ff" />
      <AppKpiCard icon="admin_panel_settings" label="Admin / Owner" :value="adminCount" color="#dc2626" bg="#fef2f2" />
      <AppKpiCard icon="badge" label="Personel" :value="staffCount" color="#8b5cf6" bg="#f5f3ff" />
      <AppKpiCard icon="apartment" label="Ofis Sayısı" :value="offices.length" color="#059669" bg="#ecfdf5" />
    </div>

    <!-- Search -->
    <div class="uy-search-bar">
      <span class="material-symbols-outlined uy-search-icon">search</span>
      <input v-model="search" class="uy-search" placeholder="Kullanıcı ara (ad, e-posta, kullanıcı adı)..." />
    </div>

    <div v-if="error" class="uy-error">{{ error }}</div>

    <div v-if="loading" class="uy-loader">
      <span class="material-symbols-outlined spin">progress_activity</span>
      Yükleniyor...
    </div>

    <!-- User Cards Grid -->
    <div v-else class="uy-card-grid">
      <div
        v-for="u in filteredUsers"
        :key="u.id"
        class="uy-card"
        @click="selectUser(u)"
      >
        <div class="uy-card-top">
          <div class="uy-avatar" :style="{ background: avatarColor(u.firstname || u.username) }">
            {{ (u.firstname || u.username || '?')[0].toUpperCase() }}
          </div>
          <span class="uy-rank-badge"
            :style="{ color: rankInfo(u.rank).color, background: rankInfo(u.rank).bg, border: `1px solid ${rankInfo(u.rank).ring}` }">
            {{ rankInfo(u.rank).label }}
          </span>
        </div>
        <div class="uy-card-name">{{ u.firstname }} {{ u.lastname }}</div>
        <div class="uy-card-meta">@{{ u.username }}</div>
        <div class="uy-card-mail">{{ u.mail }}</div>
      </div>

      <AppEmptyState v-if="!filteredUsers.length && !loading" icon="person_search" message="Kullanıcı bulunamadı" />
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-header-left">
            <template v-if="createMode">
              <div class="uy-detail-avatar" style="background:#6366f1">
                <span class="material-symbols-outlined" aria-hidden="true" style="font-size:20px">person_add</span>
              </div>
              <div>
                <div class="modal-title">Yeni Kullanıcı</div>
                <div class="modal-subtitle">Owner işlemi</div>
              </div>
            </template>
            <template v-else-if="selected">
              <div class="uy-detail-avatar" :style="{ background: avatarColor(selected.firstname || selected.username) }">
                {{ (selected.firstname || selected.username || '?')[0].toUpperCase() }}
              </div>
              <div>
                <div class="modal-title">{{ selected.firstname }} {{ selected.lastname }}</div>
                <div class="modal-subtitle">@{{ selected.username }} · {{ selected.mail }}</div>
              </div>
              <span class="uy-rank-badge"
                :style="{ color: rankInfo(selected.rank).color, background: rankInfo(selected.rank).bg, border: `1px solid ${rankInfo(selected.rank).ring}` }">
                {{ rankInfo(selected.rank).label }}
              </span>
            </template>
          </div>
          <button class="modal-close" @click="closeModal">&times;</button>
        </div>

        <!-- Create Mode Body -->
        <template v-if="createMode">
          <div class="modal-body">
            <div class="uy-field-row">
              <div class="uy-field">
                <label>Ad</label>
                <input v-model="createForm.firstname" class="uy-input" placeholder="Ad" />
              </div>
              <div class="uy-field">
                <label>Soyad</label>
                <input v-model="createForm.lastname" class="uy-input" placeholder="Soyad" />
              </div>
            </div>
            <div class="uy-field">
              <label>Kullanıcı Adı *</label>
              <input v-model="createForm.username" class="uy-input" placeholder="kullanici_adi" />
            </div>
            <div class="uy-field">
              <label>E-posta *</label>
              <input v-model="createForm.mail" type="email" class="uy-input" placeholder="ornek@email.com" />
            </div>
            <div class="uy-field">
              <label>Şifre *</label>
              <input v-model="createForm.password" type="password" class="uy-input" placeholder="••••••••" />
            </div>
            <div v-if="saveOk" class="uy-msg uy-msg-ok">Kullanıcı oluşturuldu</div>
            <div v-if="saveError" class="uy-msg uy-msg-err">{{ saveError }}</div>
          </div>
          <div class="modal-footer">
            <button class="btn-cancel" @click="closeModal">İptal</button>
            <button class="btn-primary" @click="createUser" :disabled="saving">
              <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
              {{ saving ? 'Oluşturuluyor...' : 'Kullanıcı Oluştur' }}
            </button>
          </div>
        </template>

        <!-- Edit Mode Body -->
        <template v-if="selected && !createMode">
          <!-- Tabs -->
          <div class="uy-tabs">
            <button class="uy-tab" :class="{ active: activeTab === 'info' }" @click="switchTab('info')">
              <span class="material-symbols-outlined" aria-hidden="true">edit</span> Bilgiler
            </button>
            <button class="uy-tab" :class="{ active: activeTab === 'password' }" @click="switchTab('password')">
              <span class="material-symbols-outlined" aria-hidden="true">key</span> Şifre
            </button>
            <button v-if="authStore.isAdmin" class="uy-tab" :class="{ active: activeTab === 'offices' }" @click="switchTab('offices')">
              <span class="material-symbols-outlined" aria-hidden="true">store</span> Ofisler
              <span class="uy-tab-count">{{ assignedIds.size }}</span>
            </button>
          </div>

          <!-- Info Tab -->
          <div v-if="activeTab === 'info'" class="modal-body">
            <div class="uy-field-row">
              <div class="uy-field">
                <label>Ad</label>
                <input v-model="editForm.firstname" class="uy-input" placeholder="Ad" :disabled="!authStore.isOwner" />
              </div>
              <div class="uy-field">
                <label>Soyad</label>
                <input v-model="editForm.lastname" class="uy-input" placeholder="Soyad" :disabled="!authStore.isOwner" />
              </div>
            </div>
            <div class="uy-field">
              <label>Kullanıcı Adı</label>
              <input v-model="editForm.username" class="uy-input" :disabled="!authStore.isOwner" />
            </div>
            <div class="uy-field">
              <label>E-posta</label>
              <input v-model="editForm.mail" type="email" class="uy-input" :disabled="!authStore.isOwner" />
            </div>
            <div class="uy-field">
              <label>Yetki Seviyesi</label>
              <div class="uy-rank-grid" :class="{ disabled: !authStore.isOwner }">
                <button
                  v-for="r in RANKS"
                  :key="r.value"
                  class="uy-rank-chip"
                  :class="{ selected: editForm.rank === r.value }"
                  :style="editForm.rank === r.value ? { background: r.bg, color: r.color, borderColor: r.ring } : {}"
                  @click="authStore.isOwner && (editForm.rank = r.value)"
                >{{ r.label }}</button>
              </div>
            </div>
            <div v-if="saveOk" class="uy-msg uy-msg-ok">Kaydedildi</div>
            <div v-if="saveError" class="uy-msg uy-msg-err">{{ saveError }}</div>
          </div>
          <div v-if="activeTab === 'info'" class="modal-footer">
            <button class="btn-cancel" @click="closeModal">Kapat</button>
            <button v-if="authStore.isOwner" class="btn-primary" @click="saveInfo" :disabled="saving">
              <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
              {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
            </button>
          </div>

          <!-- Password Tab -->
          <div v-if="activeTab === 'password'" class="modal-body">
            <div class="uy-info-banner">
              <span class="material-symbols-outlined" aria-hidden="true">info</span>
              <span>{{ selected.firstname || selected.username }} kullanıcısının şifresini değiştiriyorsunuz.</span>
            </div>
            <div class="uy-field">
              <label>Yeni Şifre</label>
              <input v-model="pwForm.newPassword" type="password" class="uy-input" placeholder="••••••••" :disabled="!authStore.isOwner" />
            </div>
            <div class="uy-field">
              <label>Şifre Tekrar</label>
              <input v-model="pwForm.confirm" type="password" class="uy-input" placeholder="••••••••" :disabled="!authStore.isOwner" />
            </div>
            <div v-if="saveOk" class="uy-msg uy-msg-ok">Şifre değiştirildi</div>
            <div v-if="saveError" class="uy-msg uy-msg-err">{{ saveError }}</div>
          </div>
          <div v-if="activeTab === 'password'" class="modal-footer">
            <button class="btn-cancel" @click="closeModal">Kapat</button>
            <button v-if="authStore.isOwner" class="btn-warn" @click="savePassword" :disabled="saving">
              <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
              {{ saving ? 'Değiştiriliyor...' : 'Şifreyi Değiştir' }}
            </button>
          </div>

          <!-- Offices Tab -->
          <div v-if="activeTab === 'offices'" class="modal-body">
            <div v-if="offices.length === 0" class="uy-empty-offices">Ofis bulunamadı</div>
            <div v-else class="uy-office-list">
              <div
                v-for="office in offices"
                :key="office.officeId ?? office.id"
                class="uy-office-row"
                :class="{ assigned: assignedIds.has(office.officeId ?? office.id) }"
              >
                <div class="uy-office-info">
                  <span class="material-symbols-outlined uy-office-icon">store</span>
                  <div>
                    <div class="uy-office-name">{{ office.officeName ?? office.name }}</div>
                    <div class="uy-office-code">{{ office.officeCode ?? office.code ?? '' }}</div>
                  </div>
                </div>
                <button
                  class="uy-toggle"
                  :class="{ on: assignedIds.has(office.officeId ?? office.id) }"
                  :disabled="officeSaving || !authStore.isAdmin"
                  @click="toggleOffice(office)"
                >
                  <span class="uy-toggle-ball"></span>
                </button>
              </div>
            </div>
          </div>
          <div v-if="activeTab === 'offices'" class="modal-footer">
            <button class="btn-cancel" @click="closeModal">Kapat</button>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.uy-page { padding: 24px; max-width: 1400px; margin: 0 auto; }

/* KPIs */
.kpi-grid {
  display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px; margin-bottom: 20px;
}

/* Search */
.uy-search-bar {
  position: relative; margin-bottom: 20px;
}
.uy-search-icon {
  position: absolute; left: 14px; top: 50%; transform: translateY(-50%);
  font-size: 20px; color: var(--color-text-muted); pointer-events: none;
}
.uy-search {
  width: 100%; box-sizing: border-box;
  padding: 12px 14px 12px 44px; border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  font-size: 14px; outline: none; background: #fff; transition: border-color .15s;
}
.uy-search:focus { border-color: var(--color-primary); box-shadow: 0 0 0 3px rgba(99,102,241,.1); }

.uy-error {
  background: var(--color-danger-bg); border: 1px solid #fecaca; color: var(--color-danger);
  border-radius: var(--radius-md); padding: 12px 16px; font-size: 14px; margin-bottom: 16px;
}
.uy-loader {
  display: flex; align-items: center; justify-content: center; gap: 8px;
  padding: 60px 20px; color: var(--color-text-muted); font-size: 14px;
}

/* Card Grid */
.uy-card-grid {
  display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 16px;
}
.uy-card {
  background: #fff; border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  padding: 20px; cursor: pointer; transition: border-color 0.2s, box-shadow 0.2s, transform 0.2s;
}
.uy-card:hover { border-color: #a5b4fc; box-shadow: 0 4px 16px rgba(99,102,241,.1); transform: translateY(-2px); }
.uy-card-top {
  display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px;
}
.uy-avatar {
  width: 44px; height: 44px; border-radius: 50%; color: #fff;
  font-weight: 700; font-size: 17px; display: flex; align-items: center;
  justify-content: center; flex-shrink: 0;
}
.uy-rank-badge {
  display: inline-block; padding: 4px 12px; border-radius: var(--radius-xl);
  font-size: 11px; font-weight: 600; letter-spacing: .3px; flex-shrink: 0;
}
.uy-card-name { font-size: 15px; font-weight: 700; color: #111; margin-bottom: 2px; }
.uy-card-meta { font-size: 12px; color: var(--color-text-muted); margin-bottom: 4px; }
.uy-card-mail { font-size: 12px; color: #6b7280; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

/* Modal */
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.4);
  display: flex; align-items: center; justify-content: center; z-index: 1000; padding: 20px;
}
.modal {
  background: #fff; border-radius: var(--radius-lg); width: 100%; max-width: 540px;
  box-shadow: 0 20px 60px rgba(0,0,0,.15); max-height: 90vh; display: flex; flex-direction: column;
}
.modal-header {
  display: flex; justify-content: space-between; align-items: center;
  padding: 20px 24px; border-bottom: 1px solid var(--color-border);
}
.modal-header-left { display: flex; align-items: center; gap: 12px; flex: 1; min-width: 0; }
.modal-title { font-size: 16px; font-weight: 700; color: #111; }
.modal-subtitle { font-size: 12px; color: var(--color-text-muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.modal-close { background: none; border: none; font-size: 24px; cursor: pointer; color: #6b7280; padding: 4px; }
.modal-close:hover { color: #111; }
.modal-body { padding: 24px; display: flex; flex-direction: column; gap: 16px; overflow-y: auto; flex: 1; }
.modal-footer {
  padding: 16px 24px; border-top: 1px solid var(--color-border);
  display: flex; justify-content: flex-end; gap: 8px;
}

.uy-detail-avatar {
  width: 42px; height: 42px; border-radius: 50%; color: #fff;
  font-weight: 700; font-size: 17px; display: flex; align-items: center;
  justify-content: center; flex-shrink: 0;
}

/* Buttons */
.btn-primary {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 10px 18px; background: var(--color-primary); color: #fff;
  border: none; border-radius: var(--radius-md); font-size: 13px; font-weight: 600; cursor: pointer;
}
.btn-primary:hover { background: var(--color-primary-hover); }
.btn-primary:disabled { opacity: .6; cursor: not-allowed; }
.btn-warn {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 10px 18px; background: var(--color-warning); color: #fff;
  border: none; border-radius: var(--radius-md); font-size: 13px; font-weight: 600; cursor: pointer;
}
.btn-warn:hover { background: var(--color-warning); }
.btn-warn:disabled { opacity: .6; cursor: not-allowed; }
.btn-cancel {
  padding: 10px 18px; background: #fff; color: #374151;
  border: 1px solid var(--color-border); border-radius: var(--radius-md); font-size: 13px; cursor: pointer;
}
.btn-cancel:hover { background: #f3f4f6; }

/* Tabs */
.uy-tabs {
  display: flex; gap: 2px; padding: 0 24px;
  border-bottom: 1px solid #f3f4f6; background: #fafbfc;
}
.uy-tab {
  display: flex; align-items: center; gap: 6px;
  padding: 10px 16px; border: none; background: none; cursor: pointer;
  font-size: 13px; font-weight: 500; color: #6b7280;
  border-bottom: 2px solid transparent; transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}
.uy-tab .material-symbols-outlined { font-size: 16px; }
.uy-tab.active { color: var(--color-primary); border-bottom-color: var(--color-primary); background: #fff; }
.uy-tab:hover:not(.active) { color: #374151; background: #f3f4f6; }
.uy-tab-count {
  font-size: 11px; font-weight: 700; background: var(--color-primary-light); color: var(--color-primary);
  padding: 1px 7px; border-radius: var(--radius-md); margin-left: 2px;
}

/* Form fields */
.uy-field { display: flex; flex-direction: column; gap: 5px; }
.uy-field label { font-size: 12px; font-weight: 600; color: #374151; text-transform: uppercase; letter-spacing: .5px; }
.uy-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.uy-input {
  padding: 10px 12px; border: 1.5px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 14px; outline: none; transition: border-color .15s; background: #fff;
}
.uy-input:focus { border-color: var(--color-primary); }
.uy-input:disabled { background: #f9fafb; color: var(--color-text-muted); cursor: not-allowed; }

/* Rank Selector */
.uy-rank-grid { display: flex; flex-wrap: wrap; gap: 6px; }
.uy-rank-grid.disabled { opacity: .6; pointer-events: none; }
.uy-rank-chip {
  padding: 5px 12px; border-radius: var(--radius-xl); border: 1.5px solid var(--color-border);
  background: #f9fafb; color: #6b7280; font-size: 12px; font-weight: 500;
  cursor: pointer; transition: border-color 0.2s, background-color 0.2s, color 0.2s;
}
.uy-rank-chip:hover { border-color: var(--color-text-muted); }
.uy-rank-chip.selected { font-weight: 700; }

/* Messages */
.uy-msg { border-radius: var(--radius-md); padding: 10px 14px; font-size: 13px; }
.uy-msg-ok { background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0; }
.uy-msg-err { background: var(--color-danger-bg); color: var(--color-danger); border: 1px solid #fecaca; }

.uy-info-banner {
  display: flex; align-items: center; gap: 10px;
  background: var(--color-secondary-light); border-radius: var(--radius-md); padding: 12px 14px;
  color: var(--color-secondary); font-size: 13px;
}
.uy-info-banner .material-symbols-outlined { font-size: 18px; flex-shrink: 0; }

/* Offices */
.uy-empty-offices { text-align: center; padding: 40px; color: var(--color-text-muted); font-size: 13px; }
.uy-office-list { display: flex; flex-direction: column; gap: 6px; }
.uy-office-row {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 14px; border: 1.5px solid var(--color-border); border-radius: var(--radius-lg);
  transition: border-color 0.2s, background-color 0.2s;
}
.uy-office-row.assigned { border-color: #a5b4fc; background: var(--color-primary-light); }
.uy-office-info { display: flex; align-items: center; gap: 12px; }
.uy-office-icon { font-size: 20px; color: var(--color-text-muted); }
.uy-office-row.assigned .uy-office-icon { color: var(--color-primary); }
.uy-office-name { font-size: 14px; font-weight: 600; color: #111; }
.uy-office-code { font-size: 12px; color: var(--color-text-muted); }

/* Toggle */
.uy-toggle {
  position: relative; width: 44px; height: 24px; border-radius: var(--radius-lg);
  border: none; background: #d1d5db; cursor: pointer;
  transition: background .2s; flex-shrink: 0; padding: 0;
}
.uy-toggle.on { background: var(--color-primary); }
.uy-toggle:disabled { opacity: .5; cursor: not-allowed; }
.uy-toggle-ball {
  position: absolute; top: 3px; left: 3px;
  width: 18px; height: 18px; border-radius: 50%; background: #fff;
  transition: transform .2s; box-shadow: 0 1px 3px rgba(0,0,0,.2);
}
.uy-toggle.on .uy-toggle-ball { transform: translateX(20px); }

/* Spinner */
@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin .7s linear infinite; display: inline-block; }

/* Responsive */
@media (max-width: 768px) {
  .uy-page { padding: 12px; }
  .uy-card-grid { grid-template-columns: 1fr 1fr; }
  .uy-field-row { grid-template-columns: 1fr; }
  .modal { max-width: 100%; margin: 10px; }
}
@media (max-width: 480px) {
  .uy-card-grid { grid-template-columns: 1fr; }
}
</style>
