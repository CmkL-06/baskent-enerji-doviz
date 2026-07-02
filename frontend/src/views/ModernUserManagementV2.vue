<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'

const authStore = useAuthStore()

const users   = ref<any[]>([])
const offices = ref<any[]>([])
const loading = ref(false)
const error   = ref('')
const search  = ref('')

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
  { value: 0,   label: 'Yasaklı',   color: '#ef4444', bg: '#fef2f2', ring: '#fca5a5' },
  { value: 1,   label: 'Kullanıcı', color: '#6b7280', bg: '#f9fafb', ring: '#d1d5db' },
  { value: 2,   label: 'Müşteri',   color: '#0ea5e9', bg: '#f0f9ff', ring: '#7dd3fc' },
  { value: 50,  label: 'Personel',  color: '#8b5cf6', bg: '#f5f3ff', ring: '#c4b5fd' },
  { value: 99,  label: 'Admin',     color: '#dc2626', bg: '#fef2f2', ring: '#fca5a5' },
  { value: 100, label: 'Owner',     color: '#d97706', bg: '#fffbeb', ring: '#fcd34d' },
]

function rankInfo(rank: any) {
  return RANKS.find(r => r.label === rank || r.value === rank) ?? RANKS[1]
}

function rankValue(rank: any) {
  return typeof rank === 'number' ? rank : (RANKS.find(r => r.label === rank)?.value ?? 1)
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
    setTimeout(() => { createMode.value = false }, 1200)
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
    <!-- Header -->
    <div class="uy-header">
      <div>
        <h1 class="uy-title">Kullanıcı Yönetimi</h1>
        <p class="uy-sub">{{ users.length }} kullanıcı · {{ offices.length }} ofis</p>
      </div>
      <div class="uy-header-actions">
        <button v-if="authStore.isOwner" class="uy-create-btn" @click="openCreateMode">
          <span class="material-symbols-outlined">person_add</span> Yeni Kullanıcı
        </button>
        <button class="uy-icon-btn" @click="load" :disabled="loading" title="Yenile">
          <span class="material-symbols-outlined" :class="{ spin: loading }">refresh</span>
        </button>
      </div>
    </div>

    <div v-if="error" class="uy-error">{{ error }}</div>

    <div class="uy-layout">
      <!-- Left: User List -->
      <div class="uy-left">
        <div class="uy-search-box">
          <span class="material-symbols-outlined uy-search-icon">search</span>
          <input v-model="search" class="uy-search" placeholder="Kullanıcı ara..." />
        </div>

        <div v-if="loading" class="uy-loader">
          <span class="material-symbols-outlined spin">progress_activity</span>
        </div>

        <div v-else class="uy-user-list">
          <div
            v-for="u in filteredUsers"
            :key="u.id"
            class="uy-user-row"
            :class="{ active: selected?.id === u.id && !createMode }"
            @click="selectUser(u)"
          >
            <div class="uy-avatar" :style="{ background: avatarColor(u.firstname || u.username) }">
              {{ (u.firstname || u.username || '?')[0].toUpperCase() }}
            </div>
            <div class="uy-user-info">
              <div class="uy-user-name">{{ u.firstname }} {{ u.lastname }}</div>
              <div class="uy-user-meta">
                <span>@{{ u.username }}</span>
                <span class="uy-rank-dot"
                  :style="{ background: rankInfo(u.rank).color }"
                  :title="rankInfo(u.rank).label"></span>
              </div>
            </div>
            <span class="material-symbols-outlined uy-chevron">chevron_right</span>
          </div>
          <div v-if="!filteredUsers.length" class="uy-empty-list">
            <span class="material-symbols-outlined">person_search</span>
            Kullanıcı bulunamadı
          </div>
        </div>
      </div>

      <!-- Right: Detail / Create -->
      <div class="uy-right">

        <!-- Placeholder -->
        <div v-if="!selected && !createMode" class="uy-placeholder">
          <span class="material-symbols-outlined">manage_accounts</span>
          <p>Listeden bir kullanıcı seçin</p>
          <p class="uy-placeholder-hint">veya yeni kullanıcı oluşturun</p>
        </div>

        <!-- Create Mode -->
        <template v-if="createMode">
          <div class="uy-detail-header">
            <div class="uy-detail-avatar" style="background:#6366f1">
              <span class="material-symbols-outlined" style="font-size:22px">person_add</span>
            </div>
            <div>
              <div class="uy-detail-name">Yeni Kullanıcı</div>
              <div class="uy-detail-sub">Owner işlemi</div>
            </div>
          </div>

          <div class="uy-detail-body">
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
            <button class="uy-btn uy-btn-primary" @click="createUser" :disabled="saving">
              <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
              {{ saving ? 'Oluşturuluyor...' : 'Kullanıcı Oluştur' }}
            </button>
          </div>
        </template>

        <!-- Edit Mode -->
        <template v-if="selected && !createMode">
          <!-- Detail Header -->
          <div class="uy-detail-header">
            <div class="uy-detail-avatar" :style="{ background: avatarColor(selected.firstname || selected.username) }">
              {{ (selected.firstname || selected.username || '?')[0].toUpperCase() }}
            </div>
            <div class="uy-detail-identity">
              <div class="uy-detail-name">{{ selected.firstname }} {{ selected.lastname }}</div>
              <div class="uy-detail-sub">@{{ selected.username }} · {{ selected.mail }}</div>
            </div>
            <span class="uy-rank-badge"
              :style="{ color: rankInfo(selected.rank).color, background: rankInfo(selected.rank).bg, border: `1px solid ${rankInfo(selected.rank).ring}` }">
              {{ rankInfo(selected.rank).label }}
            </span>
          </div>

          <!-- Tabs -->
          <div class="uy-tabs">
            <button class="uy-tab" :class="{ active: activeTab === 'info' }" @click="switchTab('info')">
              <span class="material-symbols-outlined">edit</span> Bilgiler
            </button>
            <button class="uy-tab" :class="{ active: activeTab === 'password' }" @click="switchTab('password')">
              <span class="material-symbols-outlined">key</span> Şifre
            </button>
            <button v-if="authStore.isAdmin" class="uy-tab" :class="{ active: activeTab === 'offices' }" @click="switchTab('offices')">
              <span class="material-symbols-outlined">store</span> Ofisler
              <span class="uy-tab-count">{{ assignedIds.size }}</span>
            </button>
          </div>

          <!-- Info Tab -->
          <div v-if="activeTab === 'info'" class="uy-detail-body">
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

            <button v-if="authStore.isOwner" class="uy-btn uy-btn-primary" @click="saveInfo" :disabled="saving">
              <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
              {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
            </button>
          </div>

          <!-- Password Tab -->
          <div v-if="activeTab === 'password'" class="uy-detail-body">
            <div class="uy-info-banner">
              <span class="material-symbols-outlined">info</span>
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

            <button v-if="authStore.isOwner" class="uy-btn uy-btn-warn" @click="savePassword" :disabled="saving">
              <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
              {{ saving ? 'Değiştiriliyor...' : 'Şifreyi Değiştir' }}
            </button>
          </div>

          <!-- Offices Tab -->
          <div v-if="activeTab === 'offices'" class="uy-detail-body uy-offices-body">
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

        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.uy-page { padding: 24px; max-width: 1400px; margin: 0 auto; }

/* Header */
.uy-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; }
.uy-title { font-size: 22px; font-weight: 700; color: #111; margin: 0; }
.uy-sub { font-size: 13px; color: #9ca3af; margin: 2px 0 0; }
.uy-header-actions { display: flex; align-items: center; gap: 8px; }
.uy-create-btn {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 16px; border: none; border-radius: 10px;
  background: #6366f1; color: #fff; font-size: 13px; font-weight: 600;
  cursor: pointer; transition: background .15s;
}
.uy-create-btn:hover { background: #4f46e5; }
.uy-create-btn .material-symbols-outlined { font-size: 17px; }
.uy-icon-btn {
  display: flex; align-items: center; padding: 8px;
  border: 1px solid #e5e7eb; border-radius: 10px;
  background: #fff; cursor: pointer; color: #6b7280; transition: all .15s;
}
.uy-icon-btn:hover { background: #f3f4f6; }
.uy-error {
  background: #fef2f2; border: 1px solid #fecaca; color: #dc2626;
  border-radius: 10px; padding: 12px 16px; font-size: 14px; margin-bottom: 16px;
}

/* Layout */
.uy-layout { display: grid; grid-template-columns: 320px 1fr; gap: 20px; min-height: 560px; }

/* Left Panel */
.uy-left {
  background: #fff; border: 1px solid #e5e7eb; border-radius: 16px;
  overflow: hidden; display: flex; flex-direction: column;
}
.uy-search-box { position: relative; padding: 12px; border-bottom: 1px solid #f3f4f6; }
.uy-search-icon {
  position: absolute; left: 22px; top: 50%; transform: translateY(-50%);
  font-size: 17px; color: #9ca3af; pointer-events: none;
}
.uy-search {
  width: 100%; box-sizing: border-box;
  padding: 8px 10px 8px 34px; border: 1px solid #e5e7eb; border-radius: 8px;
  font-size: 13px; outline: none; transition: border-color .15s;
}
.uy-search:focus { border-color: #6366f1; }

.uy-loader { display: flex; justify-content: center; align-items: center; padding: 40px; color: #9ca3af; }

.uy-user-list { overflow-y: auto; flex: 1; }
.uy-user-row {
  display: flex; align-items: center; gap: 10px;
  padding: 12px 14px; cursor: pointer;
  border-bottom: 1px solid #f9fafb; transition: background .12s;
}
.uy-user-row:hover { background: #f9fafb; }
.uy-user-row.active { background: #eef2ff; }
.uy-avatar {
  width: 38px; height: 38px; border-radius: 50%; color: #fff;
  font-weight: 700; font-size: 15px; display: flex; align-items: center;
  justify-content: center; flex-shrink: 0;
}
.uy-user-info { flex: 1; min-width: 0; }
.uy-user-name { font-size: 14px; font-weight: 600; color: #111; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.uy-user-meta { display: flex; align-items: center; gap: 6px; font-size: 12px; color: #9ca3af; }
.uy-rank-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
.uy-chevron { font-size: 18px; color: #d1d5db; }
.uy-user-row.active .uy-chevron { color: #6366f1; }
.uy-empty-list {
  display: flex; flex-direction: column; align-items: center; gap: 8px;
  padding: 40px 20px; color: #9ca3af; font-size: 13px;
}
.uy-empty-list .material-symbols-outlined { font-size: 40px; }

/* Right Panel */
.uy-right {
  background: #fff; border: 1px solid #e5e7eb; border-radius: 16px;
  overflow: hidden; display: flex; flex-direction: column;
}
.uy-placeholder {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  height: 100%; gap: 10px; color: #9ca3af; padding: 60px;
}
.uy-placeholder .material-symbols-outlined { font-size: 56px; }
.uy-placeholder p { font-size: 14px; margin: 0; }
.uy-placeholder-hint { font-size: 12px !important; color: #d1d5db; }

/* Detail Header */
.uy-detail-header {
  display: flex; align-items: center; gap: 14px;
  padding: 20px; border-bottom: 1px solid #f3f4f6;
}
.uy-detail-avatar {
  width: 48px; height: 48px; border-radius: 50%; color: #fff;
  font-weight: 700; font-size: 19px; display: flex; align-items: center;
  justify-content: center; flex-shrink: 0;
}
.uy-detail-identity { flex: 1; min-width: 0; }
.uy-detail-name { font-weight: 700; font-size: 16px; color: #111; }
.uy-detail-sub { font-size: 12px; color: #9ca3af; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.uy-rank-badge {
  display: inline-block; padding: 4px 12px; border-radius: 20px;
  font-size: 12px; font-weight: 600; letter-spacing: .3px; flex-shrink: 0;
}

/* Tabs */
.uy-tabs {
  display: flex; gap: 2px; padding: 0 20px;
  border-bottom: 1px solid #f3f4f6; background: #fafbfc;
}
.uy-tab {
  display: flex; align-items: center; gap: 6px;
  padding: 10px 16px; border: none; background: none; cursor: pointer;
  font-size: 13px; font-weight: 500; color: #6b7280;
  border-bottom: 2px solid transparent; transition: all .15s;
}
.uy-tab .material-symbols-outlined { font-size: 16px; }
.uy-tab.active { color: #6366f1; border-bottom-color: #6366f1; background: #fff; }
.uy-tab:hover:not(.active) { color: #374151; background: #f3f4f6; }
.uy-tab-count {
  font-size: 11px; font-weight: 700; background: #eef2ff; color: #6366f1;
  padding: 1px 7px; border-radius: 10px; margin-left: 2px;
}

/* Detail Body */
.uy-detail-body { padding: 20px; display: flex; flex-direction: column; gap: 16px; flex: 1; overflow-y: auto; }
.uy-field { display: flex; flex-direction: column; gap: 5px; }
.uy-field label { font-size: 12px; font-weight: 600; color: #374151; text-transform: uppercase; letter-spacing: .5px; }
.uy-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.uy-input {
  padding: 10px 12px; border: 1.5px solid #e5e7eb; border-radius: 10px;
  font-size: 14px; outline: none; transition: border-color .15s; background: #fff;
}
.uy-input:focus { border-color: #6366f1; }
.uy-input:disabled { background: #f9fafb; color: #9ca3af; cursor: not-allowed; }

/* Rank Selector */
.uy-rank-grid { display: flex; flex-wrap: wrap; gap: 6px; }
.uy-rank-grid.disabled { opacity: .6; pointer-events: none; }
.uy-rank-chip {
  padding: 5px 12px; border-radius: 20px; border: 1.5px solid #e5e7eb;
  background: #f9fafb; color: #6b7280; font-size: 12px; font-weight: 500;
  cursor: pointer; transition: all .15s;
}
.uy-rank-chip:hover { border-color: #9ca3af; }
.uy-rank-chip.selected { font-weight: 700; }

/* Messages */
.uy-msg { border-radius: 8px; padding: 10px 14px; font-size: 13px; }
.uy-msg-ok { background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0; }
.uy-msg-err { background: #fef2f2; color: #dc2626; border: 1px solid #fecaca; }

/* Buttons */
.uy-btn {
  display: flex; align-items: center; justify-content: center; gap: 8px;
  padding: 12px; border-radius: 10px; border: none;
  font-size: 14px; font-weight: 600; cursor: pointer; transition: background .15s;
}
.uy-btn:disabled { opacity: .6; cursor: not-allowed; }
.uy-btn-primary { background: #6366f1; color: #fff; }
.uy-btn-primary:hover { background: #4f46e5; }
.uy-btn-warn { background: #f59e0b; color: #fff; }
.uy-btn-warn:hover { background: #d97706; }

.uy-info-banner {
  display: flex; align-items: center; gap: 10px;
  background: #eff6ff; border-radius: 8px; padding: 12px 14px;
  color: #3b82f6; font-size: 13px;
}
.uy-info-banner .material-symbols-outlined { font-size: 18px; flex-shrink: 0; }

/* Offices Tab */
.uy-offices-body { padding: 12px; }
.uy-empty-offices { text-align: center; padding: 40px; color: #9ca3af; font-size: 13px; }
.uy-office-list { display: flex; flex-direction: column; gap: 6px; }
.uy-office-row {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 14px; border: 1.5px solid #e5e7eb; border-radius: 12px;
  transition: all .15s;
}
.uy-office-row.assigned { border-color: #a5b4fc; background: #eef2ff; }
.uy-office-info { display: flex; align-items: center; gap: 12px; }
.uy-office-icon { font-size: 20px; color: #9ca3af; }
.uy-office-row.assigned .uy-office-icon { color: #6366f1; }
.uy-office-name { font-size: 14px; font-weight: 600; color: #111; }
.uy-office-code { font-size: 12px; color: #9ca3af; }

/* Toggle */
.uy-toggle {
  position: relative; width: 44px; height: 24px; border-radius: 12px;
  border: none; background: #d1d5db; cursor: pointer;
  transition: background .2s; flex-shrink: 0; padding: 0;
}
.uy-toggle.on { background: #6366f1; }
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
  .uy-layout { grid-template-columns: 1fr; }
  .uy-left { max-height: 300px; }
}
</style>
