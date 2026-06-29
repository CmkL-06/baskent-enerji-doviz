<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'

const authStore = useAuthStore()

const users   = ref<any[]>([])
const loading = ref(false)
const error   = ref('')
const search  = ref('')

const selected    = ref<any>(null)
const panelOpen   = ref(false)
const panelMode   = ref<'edit' | 'create'>('edit')
const activeTab   = ref<'info' | 'password'>('info')

const saving    = ref(false)
const saveError = ref('')
const saveOk    = ref(false)

const editForm   = ref({ username: '', mail: '', firstname: '', lastname: '', rank: 1 })
const pwForm     = ref({ newPassword: '', confirm: '' })
const createForm = ref({ username: '', mail: '', password: '', firstname: '', lastname: '' })

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
  const idx = (name?.charCodeAt(0) ?? 0) % colors.length
  return colors[idx]
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

async function load() {
  loading.value = true; error.value = ''
  try { users.value = await apiService.getUsers() }
  catch (e: any) { error.value = e.response?.data?.message || 'Kullanıcılar yüklenemedi' }
  finally { loading.value = false }
}

function openPanel(user: any) {
  selected.value = user
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
  panelOpen.value = true
}

function openCreatePanel() {
  selected.value = null
  panelMode.value = 'create'
  saveError.value = ''; saveOk.value = false
  createForm.value = { username: '', mail: '', password: '', firstname: '', lastname: '' }
  panelOpen.value = true
}

function closePanel() {
  panelOpen.value = false
  setTimeout(() => { selected.value = null; panelMode.value = 'edit' }, 300)
}

function switchTab(tab: 'info' | 'password') {
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
    if (updated) selected.value = updated
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
    setTimeout(closePanel, 1200)
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

onMounted(load)
</script>

<template>
  <div class="um-page">

    <!-- Header -->
    <div class="um-header">
      <div>
        <h1 class="um-title">Kullanıcılar</h1>
        <p class="um-sub">{{ users.length }} kullanıcı</p>
      </div>
      <div class="um-header-right">
        <div class="um-search-wrap">
          <span class="material-symbols-outlined um-si">search</span>
          <input v-model="search" class="um-search" placeholder="Ara..." />
        </div>
        <button v-if="authStore.isOwner" class="um-create-btn" @click="openCreatePanel">
          <span class="material-symbols-outlined">person_add</span> Yeni Kullanıcı
        </button>
        <button class="um-refresh" @click="load" :disabled="loading">
          <span class="material-symbols-outlined" :class="{ spin: loading }">refresh</span>
        </button>
      </div>
    </div>

    <div v-if="error" class="um-error">{{ error }}</div>

    <!-- Skeleton -->
    <div v-if="loading && !users.length" class="um-grid">
      <div v-for="i in 6" :key="i" class="um-card um-skeleton"></div>
    </div>

    <!-- Cards -->
    <div v-else class="um-grid">
      <div
        v-for="u in filteredUsers"
        :key="u.id"
        class="um-card"
        :class="{ 'um-card-active': selected?.id === u.id && panelOpen }"
        @click="openPanel(u)"
      >
        <div class="um-card-top">
          <div class="um-avatar" :style="{ background: avatarColor(u.firstname || u.username) }">
            {{ (u.firstname || u.username || '?')[0].toUpperCase() }}
          </div>
          <span class="um-rank-badge"
            :style="{ color: rankInfo(u.rank).color, background: rankInfo(u.rank).bg, border: `1px solid ${rankInfo(u.rank).ring}` }">
            {{ rankInfo(u.rank).label }}
          </span>
        </div>
        <div class="um-card-name">{{ u.firstname }} {{ u.lastname }}</div>
        <div class="um-card-user">@{{ u.username }}</div>
        <div class="um-card-mail">{{ u.mail }}</div>
        <div class="um-card-arrow">
          <span class="material-symbols-outlined">chevron_right</span>
        </div>
      </div>

      <div v-if="!filteredUsers.length && !loading" class="um-empty-card">
        <span class="material-symbols-outlined">person_search</span>
        <p>Kullanıcı bulunamadı</p>
      </div>
    </div>

    <!-- Overlay -->
    <div v-if="panelOpen" class="um-overlay" @click="closePanel"></div>

    <!-- Detail Panel -->
    <div class="um-panel" :class="{ 'um-panel-open': panelOpen }">

      <!-- CREATE MODE -->
      <template v-if="panelMode === 'create'">
        <div class="um-panel-header">
          <div class="um-panel-avatar" style="background:#6366f1">
            <span class="material-symbols-outlined" style="font-size:22px">person_add</span>
          </div>
          <div class="um-panel-identity">
            <div class="um-panel-name">Yeni Kullanıcı</div>
            <div class="um-panel-uname">Owner işlemi</div>
          </div>
          <button class="um-panel-close" @click="closePanel">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>
        <div class="um-panel-body" style="margin-top:16px">
          <div class="um-field-row">
            <div class="um-field">
              <label>Ad</label>
              <input v-model="createForm.firstname" class="um-input" placeholder="Ad" />
            </div>
            <div class="um-field">
              <label>Soyad</label>
              <input v-model="createForm.lastname" class="um-input" placeholder="Soyad" />
            </div>
          </div>
          <div class="um-field">
            <label>Kullanıcı Adı *</label>
            <input v-model="createForm.username" class="um-input" placeholder="kullanici_adi" />
          </div>
          <div class="um-field">
            <label>E-posta *</label>
            <input v-model="createForm.mail" type="email" class="um-input" placeholder="ornek@email.com" />
          </div>
          <div class="um-field">
            <label>Şifre *</label>
            <input v-model="createForm.password" type="password" class="um-input" placeholder="••••••••" />
          </div>
          <div v-if="saveOk" class="um-ok">Kullanıcı oluşturuldu ✓</div>
          <div v-if="saveError" class="um-err">{{ saveError }}</div>
          <button class="um-save-btn" @click="createUser" :disabled="saving">
            <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
            {{ saving ? 'Oluşturuluyor...' : 'Kullanıcı Oluştur' }}
          </button>
        </div>
      </template>

      <!-- EDIT MODE -->
      <template v-if="panelMode === 'edit' && selected">

        <!-- Panel Header -->
        <div class="um-panel-header">
          <div class="um-panel-avatar" :style="{ background: avatarColor(selected.firstname || selected.username) }">
            {{ (selected.firstname || selected.username || '?')[0].toUpperCase() }}
          </div>
          <div class="um-panel-identity">
            <div class="um-panel-name">{{ selected.firstname }} {{ selected.lastname }}</div>
            <div class="um-panel-uname">@{{ selected.username }}</div>
          </div>
          <button class="um-panel-close" @click="closePanel">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>

        <!-- Rank + mail row -->
        <div class="um-panel-rank-row">
          <span class="um-rank-badge um-rank-lg"
            :style="{ color: rankInfo(selected.rank).color, background: rankInfo(selected.rank).bg, border: `1px solid ${rankInfo(selected.rank).ring}` }">
            {{ rankInfo(selected.rank).label }}
          </span>
          <span class="um-panel-mail">{{ selected.mail }}</span>
        </div>

        <!-- Tabs -->
        <div class="um-tabs">
          <button class="um-tab" :class="{ active: activeTab === 'info' }" @click="switchTab('info')">
            <span class="material-symbols-outlined">edit</span> Bilgileri Düzenle
          </button>
          <button class="um-tab" :class="{ active: activeTab === 'password' }" @click="switchTab('password')">
            <span class="material-symbols-outlined">key</span> Şifre
          </button>
        </div>

        <!-- Info Tab -->
        <div v-if="activeTab === 'info'" class="um-panel-body">
          <div class="um-field-row">
            <div class="um-field">
              <label>Ad</label>
              <input v-model="editForm.firstname" class="um-input" placeholder="Ad" :disabled="!authStore.isOwner" />
            </div>
            <div class="um-field">
              <label>Soyad</label>
              <input v-model="editForm.lastname" class="um-input" placeholder="Soyad" :disabled="!authStore.isOwner" />
            </div>
          </div>
          <div class="um-field">
            <label>Kullanıcı Adı</label>
            <input v-model="editForm.username" class="um-input" :disabled="!authStore.isOwner" />
          </div>
          <div class="um-field">
            <label>E-posta</label>
            <input v-model="editForm.mail" type="email" class="um-input" :disabled="!authStore.isOwner" />
          </div>
          <div class="um-field">
            <label>Yetki Seviyesi</label>
            <div class="um-rank-select-wrap" :class="{ disabled: !authStore.isOwner }">
              <button
                v-for="r in RANKS"
                :key="r.value"
                class="um-rank-option"
                :class="{ selected: editForm.rank === r.value }"
                :style="editForm.rank === r.value ? { background: r.bg, color: r.color, borderColor: r.ring } : {}"
                @click="authStore.isOwner && (editForm.rank = r.value)"
              >{{ r.label }}</button>
            </div>
          </div>

          <div v-if="saveOk" class="um-ok">Kaydedildi ✓</div>
          <div v-if="saveError" class="um-err">{{ saveError }}</div>

          <button v-if="authStore.isOwner" class="um-save-btn" @click="saveInfo" :disabled="saving">
            <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
            {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
        </div>

        <!-- Password Tab -->
        <div v-if="activeTab === 'password'" class="um-panel-body">
          <div class="um-pw-info">
            <span class="material-symbols-outlined">info</span>
            <span>{{ selected.firstname || selected.username }} kullanıcısının şifresini değiştiriyorsunuz.</span>
          </div>
          <div class="um-field">
            <label>Yeni Şifre</label>
            <input v-model="pwForm.newPassword" type="password" class="um-input" placeholder="••••••••" :disabled="!authStore.isOwner" />
          </div>
          <div class="um-field">
            <label>Şifre Tekrar</label>
            <input v-model="pwForm.confirm" type="password" class="um-input" placeholder="••••••••" :disabled="!authStore.isOwner" />
          </div>

          <div v-if="saveOk" class="um-ok">Şifre değiştirildi ✓</div>
          <div v-if="saveError" class="um-err">{{ saveError }}</div>

          <button v-if="authStore.isOwner" class="um-save-btn um-save-btn-warn" @click="savePassword" :disabled="saving">
            <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
            {{ saving ? 'Değiştiriliyor...' : 'Şifreyi Değiştir' }}
          </button>
        </div>

      </template>
    </div>

  </div>
</template>

<style scoped>
.um-page { padding: 24px; position: relative; }

.um-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; }
.um-title  { font-size: 22px; font-weight: 700; color: #111; margin: 0; }
.um-sub    { font-size: 13px; color: #9ca3af; margin: 2px 0 0; }
.um-header-right { display: flex; align-items: center; gap: 8px; }
.um-search-wrap  { position: relative; }
.um-si { position: absolute; left: 10px; top: 50%; transform: translateY(-50%); font-size: 17px; color: #9ca3af; pointer-events: none; }
.um-search { padding: 8px 12px 8px 34px; border: 1px solid #e5e7eb; border-radius: 10px; font-size: 14px; width: 200px; outline: none; transition: border-color .15s; }
.um-search:focus { border-color: #6366f1; }
.um-refresh { display: flex; align-items: center; padding: 8px; border: 1px solid #e5e7eb; border-radius: 10px; background: #fff; cursor: pointer; color: #6b7280; transition: all .15s; }
.um-refresh:hover { background: #f3f4f6; }
.um-create-btn { display: flex; align-items: center; gap: 6px; padding: 8px 14px; border: none; border-radius: 10px; background: #6366f1; color: #fff; font-size: 13px; font-weight: 600; cursor: pointer; transition: background .15s; }
.um-create-btn:hover { background: #4f46e5; }
.um-create-btn .material-symbols-outlined { font-size: 17px; }
.um-error { background: #fef2f2; border: 1px solid #fecaca; color: #dc2626; border-radius: 10px; padding: 12px 16px; font-size: 14px; margin-bottom: 16px; }

.um-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 14px; }

.um-card {
  background: #fff;
  border: 1.5px solid #e5e7eb;
  border-radius: 16px;
  padding: 20px 18px 16px;
  cursor: pointer;
  transition: all .18s;
  position: relative;
  overflow: hidden;
}
.um-card:hover { border-color: #6366f1; box-shadow: 0 4px 16px rgba(99,102,241,.12); transform: translateY(-2px); }
.um-card-active { border-color: #6366f1 !important; box-shadow: 0 4px 20px rgba(99,102,241,.18) !important; }

.um-card-top { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 14px; }
.um-avatar { width: 46px; height: 46px; border-radius: 50%; color: #fff; font-weight: 700; font-size: 18px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }

.um-rank-badge { display: inline-block; padding: 3px 10px; border-radius: 20px; font-size: 11px; font-weight: 600; letter-spacing: .3px; }
.um-rank-lg { font-size: 13px; padding: 4px 14px; }

.um-card-name { font-weight: 600; font-size: 15px; color: #111; line-height: 1.3; }
.um-card-user { font-size: 12px; color: #9ca3af; margin: 2px 0 8px; }
.um-card-mail { font-size: 12px; color: #6b7280; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.um-card-arrow { position: absolute; bottom: 14px; right: 14px; color: #d1d5db; transition: color .15s; }
.um-card:hover .um-card-arrow { color: #6366f1; }

.um-skeleton { min-height: 140px; background: linear-gradient(90deg, #f3f4f6 25%, #e5e7eb 50%, #f3f4f6 75%); background-size: 200% 100%; animation: shimmer 1.4s infinite; }
@keyframes shimmer { 0% { background-position: 200% 0 } 100% { background-position: -200% 0 } }

.um-empty-card { grid-column: 1/-1; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 12px; padding: 60px; color: #9ca3af; }
.um-empty-card .material-symbols-outlined { font-size: 48px; }

.um-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.3); z-index: 40; backdrop-filter: blur(1px); }

.um-panel {
  position: fixed;
  top: 0; right: 0;
  width: 380px; max-width: 95vw;
  height: 100vh;
  background: #fff;
  box-shadow: -8px 0 40px rgba(0,0,0,.12);
  z-index: 50;
  display: flex;
  flex-direction: column;
  transform: translateX(100%);
  transition: transform .28s cubic-bezier(.4,0,.2,1);
  overflow-y: auto;
}
.um-panel-open { transform: translateX(0); }

.um-panel-header { display: flex; align-items: center; gap: 12px; padding: 20px 20px 0; flex-shrink: 0; }
.um-panel-avatar { width: 50px; height: 50px; border-radius: 50%; color: #fff; font-weight: 700; font-size: 20px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.um-panel-identity { flex: 1; min-width: 0; }
.um-panel-name { font-weight: 700; font-size: 16px; color: #111; }
.um-panel-uname { font-size: 13px; color: #9ca3af; }
.um-panel-close { margin-left: auto; background: none; border: none; cursor: pointer; color: #9ca3af; display: flex; padding: 4px; border-radius: 6px; }
.um-panel-close:hover { background: #f3f4f6; color: #374151; }

.um-panel-rank-row { display: flex; align-items: center; gap: 10px; padding: 14px 20px; border-bottom: 1px solid #f3f4f6; }
.um-panel-mail { font-size: 12px; color: #6b7280; flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.um-tabs { display: flex; gap: 4px; padding: 12px 20px 0; border-bottom: 1px solid #f3f4f6; }
.um-tab { display: flex; align-items: center; gap: 6px; padding: 8px 14px; border: none; background: none; cursor: pointer; font-size: 13px; color: #6b7280; border-radius: 8px 8px 0 0; border-bottom: 2px solid transparent; transition: all .15s; }
.um-tab .material-symbols-outlined { font-size: 16px; }
.um-tab.active { color: #6366f1; border-bottom-color: #6366f1; background: #eef2ff; }
.um-tab:hover:not(.active) { background: #f9fafb; color: #374151; }

.um-panel-body { padding: 20px; display: flex; flex-direction: column; gap: 16px; }
.um-field { display: flex; flex-direction: column; gap: 5px; }
.um-field label { font-size: 12px; font-weight: 600; color: #374151; text-transform: uppercase; letter-spacing: .5px; }
.um-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.um-input { padding: 10px 12px; border: 1.5px solid #e5e7eb; border-radius: 10px; font-size: 14px; outline: none; transition: border-color .15s; background: #fff; }
.um-input:focus { border-color: #6366f1; }
.um-input:disabled { background: #f9fafb; color: #9ca3af; cursor: not-allowed; }

.um-rank-select-wrap { display: flex; flex-wrap: wrap; gap: 6px; }
.um-rank-select-wrap.disabled { opacity: .6; pointer-events: none; }
.um-rank-option { padding: 5px 12px; border-radius: 20px; border: 1.5px solid #e5e7eb; background: #f9fafb; color: #6b7280; font-size: 12px; font-weight: 500; cursor: pointer; transition: all .15s; }
.um-rank-option:hover { border-color: #9ca3af; }
.um-rank-option.selected { font-weight: 700; }

.um-ok  { background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0; border-radius: 8px; padding: 10px 14px; font-size: 13px; }
.um-err { background: #fef2f2; color: #dc2626; border: 1px solid #fecaca; border-radius: 8px; padding: 10px 14px; font-size: 13px; }

.um-save-btn { display: flex; align-items: center; justify-content: center; gap: 8px; padding: 12px; border-radius: 10px; border: none; background: #6366f1; color: #fff; font-size: 14px; font-weight: 600; cursor: pointer; transition: background .15s; }
.um-save-btn:hover { background: #4f46e5; }
.um-save-btn:disabled { opacity: .6; cursor: not-allowed; }
.um-save-btn-warn { background: #f59e0b; }
.um-save-btn-warn:hover { background: #d97706; }

.um-pw-info { display: flex; align-items: center; gap: 10px; background: #eff6ff; border-radius: 8px; padding: 12px 14px; color: #3b82f6; font-size: 13px; }
.um-pw-info .material-symbols-outlined { font-size: 18px; flex-shrink: 0; }

@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin .7s linear infinite; display: inline-block; }
</style>
