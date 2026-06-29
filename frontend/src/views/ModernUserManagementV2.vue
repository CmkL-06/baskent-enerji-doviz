<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'

const authStore = useAuthStore()

const users   = ref<any[]>([])
const loading = ref(false)
const error   = ref('')
const search  = ref('')

const showCreateModal   = ref(false)
const showEditModal     = ref(false)
const showPasswordModal = ref(false)
const selectedUser      = ref<any>(null)
const saving            = ref(false)
const formError         = ref('')

const createForm = ref({ username: '', mail: '', password: '', firstname: '', lastname: '' })
const editForm   = ref({ username: '', mail: '', firstname: '', lastname: '', rank: 1 })
const pwForm     = ref({ newPassword: '', confirm: '' })

const RANKS = [
  { value: 0,   label: 'Yasaklı',   color: '#ef4444', bg: '#fef2f2' },
  { value: 1,   label: 'Kullanıcı', color: '#6b7280', bg: '#f9fafb' },
  { value: 2,   label: 'Müşteri',   color: '#6b7280', bg: '#f9fafb' },
  { value: 50,  label: 'Personel',  color: '#3b82f6', bg: '#eff6ff' },
  { value: 99,  label: 'Admin',     color: '#dc2626', bg: '#fef2f2' },
  { value: 100, label: 'Owner',     color: '#d97706', bg: '#fffbeb' },
]

function rankInfo(rank: any) {
  return RANKS.find(r => r.label === rank || r.value === rank) ?? RANKS[1]
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
  loading.value = true
  error.value   = ''
  try {
    users.value = await apiService.getUsers()
  } catch (e: any) {
    error.value = e.response?.data?.message || 'Kullanıcılar yüklenemedi'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  createForm.value = { username: '', mail: '', password: '', firstname: '', lastname: '' }
  formError.value  = ''
  showCreateModal.value = true
}

function openEdit(user: any) {
  selectedUser.value = user
  const rankVal = rankInfo(user.rank).value
  editForm.value = { username: user.username, mail: user.mail, firstname: user.firstname ?? '', lastname: user.lastname ?? '', rank: rankVal }
  formError.value = ''
  showEditModal.value = true
}

function openPassword(user: any) {
  selectedUser.value = user
  pwForm.value = { newPassword: '', confirm: '' }
  formError.value = ''
  showPasswordModal.value = true
}

async function createUser() {
  formError.value = ''
  if (!createForm.value.username || !createForm.value.mail || !createForm.value.password) {
    formError.value = 'Kullanıcı adı, e-posta ve şifre zorunludur.'
    return
  }
  saving.value = true
  try {
    await apiService.registerUser(createForm.value)
    showCreateModal.value = false
    await load()
  } catch (e: any) {
    formError.value = e.response?.data?.message || 'Kullanıcı oluşturulamadı'
  } finally {
    saving.value = false
  }
}

async function updateUser() {
  formError.value = ''
  saving.value    = true
  try {
    await apiService.updateUser({ Id: selectedUser.value.id, ...editForm.value })
    showEditModal.value = false
    await load()
  } catch (e: any) {
    formError.value = e.response?.data?.message || 'Kullanıcı güncellenemedi'
  } finally {
    saving.value = false
  }
}

async function changePassword() {
  formError.value = ''
  if (!pwForm.value.newPassword) { formError.value = 'Şifre boş olamaz'; return }
  if (pwForm.value.newPassword !== pwForm.value.confirm) { formError.value = 'Şifreler eşleşmiyor'; return }
  saving.value = true
  try {
    await apiService.changeUserPassword(selectedUser.value.id, pwForm.value.newPassword)
    showPasswordModal.value = false
  } catch (e: any) {
    formError.value = e.response?.data?.message || 'Şifre değiştirilemedi'
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="um-page">
    <!-- Header -->
    <div class="um-header">
      <div>
        <h1 class="um-title">Kullanıcı Yönetimi</h1>
        <p class="um-sub">{{ users.length }} kullanıcı</p>
      </div>
      <div class="um-header-actions">
        <div class="um-search-wrap">
          <span class="material-symbols-outlined um-search-icon">search</span>
          <input v-model="search" class="um-search" placeholder="Ara (ad, e-posta...)" />
        </div>
        <button v-if="authStore.isOwner" class="um-btn um-btn-primary" @click="openCreate">
          <span class="material-symbols-outlined">person_add</span> Yeni Kullanıcı
        </button>
        <button class="um-btn um-btn-ghost" @click="load" :disabled="loading">
          <span class="material-symbols-outlined" :class="{ spin: loading }">refresh</span>
        </button>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="um-error">{{ error }}</div>

    <!-- Loading -->
    <div v-if="loading && !users.length" class="um-loading">
      <div class="um-spinner"></div>
      <span>Yükleniyor...</span>
    </div>

    <!-- Table -->
    <div v-else class="um-table-wrap">
      <table class="um-table">
        <thead>
          <tr>
            <th>Kullanıcı</th>
            <th>E-posta</th>
            <th>Yetki</th>
            <th v-if="authStore.isOwner" class="um-actions-col">İşlemler</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!filteredUsers.length">
            <td :colspan="authStore.isOwner ? 4 : 3" class="um-empty">Kullanıcı bulunamadı</td>
          </tr>
          <tr v-for="u in filteredUsers" :key="u.id" class="um-row">
            <td>
              <div class="um-user-cell">
                <div class="um-avatar">{{ (u.firstname || u.username || '?')[0].toUpperCase() }}</div>
                <div>
                  <div class="um-name">{{ u.firstname }} {{ u.lastname }}</div>
                  <div class="um-username">@{{ u.username }}</div>
                </div>
              </div>
            </td>
            <td class="um-mail">{{ u.mail }}</td>
            <td>
              <span class="um-rank-badge" :style="{ color: rankInfo(u.rank).color, background: rankInfo(u.rank).bg }">
                {{ rankInfo(u.rank).label }}
              </span>
            </td>
            <td v-if="authStore.isOwner" class="um-actions-cell">
              <button class="um-icon-btn" title="Düzenle" @click="openEdit(u)">
                <span class="material-symbols-outlined">edit</span>
              </button>
              <button class="um-icon-btn" title="Şifre Değiştir" @click="openPassword(u)">
                <span class="material-symbols-outlined">key</span>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Create Modal -->
    <div v-if="showCreateModal" class="um-modal-backdrop" @click.self="showCreateModal = false">
      <div class="um-modal">
        <div class="um-modal-header">
          <h2>Yeni Kullanıcı</h2>
          <button class="um-modal-close" @click="showCreateModal = false">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>
        <div class="um-modal-body">
          <div class="um-form-row">
            <label>Kullanıcı Adı *</label>
            <input v-model="createForm.username" class="um-input" placeholder="kullanici_adi" />
          </div>
          <div class="um-form-row">
            <label>E-posta *</label>
            <input v-model="createForm.mail" type="email" class="um-input" placeholder="ornek@email.com" />
          </div>
          <div class="um-form-row">
            <label>Şifre *</label>
            <input v-model="createForm.password" type="password" class="um-input" placeholder="••••••••" />
          </div>
          <div class="um-form-2col">
            <div class="um-form-row">
              <label>Ad</label>
              <input v-model="createForm.firstname" class="um-input" placeholder="Ad" />
            </div>
            <div class="um-form-row">
              <label>Soyad</label>
              <input v-model="createForm.lastname" class="um-input" placeholder="Soyad" />
            </div>
          </div>
          <div v-if="formError" class="um-form-error">{{ formError }}</div>
        </div>
        <div class="um-modal-footer">
          <button class="um-btn um-btn-ghost" @click="showCreateModal = false">İptal</button>
          <button class="um-btn um-btn-primary" @click="createUser" :disabled="saving">
            <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
            {{ saving ? 'Kaydediliyor...' : 'Oluştur' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Edit Modal -->
    <div v-if="showEditModal" class="um-modal-backdrop" @click.self="showEditModal = false">
      <div class="um-modal">
        <div class="um-modal-header">
          <h2>Kullanıcıyı Düzenle</h2>
          <button class="um-modal-close" @click="showEditModal = false">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>
        <div class="um-modal-body">
          <div class="um-form-row">
            <label>Kullanıcı Adı</label>
            <input v-model="editForm.username" class="um-input" />
          </div>
          <div class="um-form-row">
            <label>E-posta</label>
            <input v-model="editForm.mail" type="email" class="um-input" />
          </div>
          <div class="um-form-2col">
            <div class="um-form-row">
              <label>Ad</label>
              <input v-model="editForm.firstname" class="um-input" />
            </div>
            <div class="um-form-row">
              <label>Soyad</label>
              <input v-model="editForm.lastname" class="um-input" />
            </div>
          </div>
          <div class="um-form-row">
            <label>Yetki Seviyesi</label>
            <select v-model="editForm.rank" class="um-input">
              <option v-for="r in RANKS" :key="r.value" :value="r.value">{{ r.label }}</option>
            </select>
          </div>
          <div v-if="formError" class="um-form-error">{{ formError }}</div>
        </div>
        <div class="um-modal-footer">
          <button class="um-btn um-btn-ghost" @click="showEditModal = false">İptal</button>
          <button class="um-btn um-btn-primary" @click="updateUser" :disabled="saving">
            <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
            {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Password Modal -->
    <div v-if="showPasswordModal" class="um-modal-backdrop" @click.self="showPasswordModal = false">
      <div class="um-modal um-modal-sm">
        <div class="um-modal-header">
          <h2>Şifre Değiştir</h2>
          <button class="um-modal-close" @click="showPasswordModal = false">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>
        <div class="um-modal-body">
          <p class="um-modal-user">
            <span class="material-symbols-outlined">person</span>
            {{ selectedUser?.firstname || selectedUser?.username }}
          </p>
          <div class="um-form-row">
            <label>Yeni Şifre</label>
            <input v-model="pwForm.newPassword" type="password" class="um-input" placeholder="••••••••" />
          </div>
          <div class="um-form-row">
            <label>Tekrar</label>
            <input v-model="pwForm.confirm" type="password" class="um-input" placeholder="••••••••" />
          </div>
          <div v-if="formError" class="um-form-error">{{ formError }}</div>
        </div>
        <div class="um-modal-footer">
          <button class="um-btn um-btn-ghost" @click="showPasswordModal = false">İptal</button>
          <button class="um-btn um-btn-primary" @click="changePassword" :disabled="saving">
            <span v-if="saving" class="material-symbols-outlined spin">progress_activity</span>
            {{ saving ? 'Değiştiriliyor...' : 'Değiştir' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.um-page { padding: 24px; display: flex; flex-direction: column; gap: 20px; }

.um-header { display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 12px; }
.um-title  { font-size: 22px; font-weight: 700; color: #111; margin: 0; }
.um-sub    { font-size: 13px; color: #6b7280; margin: 2px 0 0; }
.um-header-actions { display: flex; align-items: center; gap: 10px; }

.um-search-wrap { position: relative; }
.um-search-icon { position: absolute; left: 10px; top: 50%; transform: translateY(-50%); font-size: 18px; color: #9ca3af; pointer-events: none; }
.um-search { padding: 8px 12px 8px 36px; border: 1px solid #e5e7eb; border-radius: 8px; font-size: 14px; width: 220px; outline: none; transition: border-color .15s; }
.um-search:focus { border-color: #6366f1; }

.um-btn { display: flex; align-items: center; gap: 6px; padding: 8px 14px; border-radius: 8px; font-size: 14px; font-weight: 500; cursor: pointer; border: none; transition: all .15s; }
.um-btn-primary { background: #6366f1; color: #fff; }
.um-btn-primary:hover { background: #4f46e5; }
.um-btn-primary:disabled { opacity: .6; cursor: not-allowed; }
.um-btn-ghost { background: #f3f4f6; color: #374151; }
.um-btn-ghost:hover { background: #e5e7eb; }
.um-btn-ghost:disabled { opacity: .5; cursor: not-allowed; }

.um-error { background: #fef2f2; border: 1px solid #fecaca; color: #dc2626; border-radius: 8px; padding: 12px 16px; font-size: 14px; }

.um-loading { display: flex; align-items: center; justify-content: center; gap: 12px; padding: 60px; color: #6b7280; }
.um-spinner { width: 24px; height: 24px; border: 2px solid #e5e7eb; border-top-color: #6366f1; border-radius: 50%; animation: spin .7s linear infinite; }

.um-table-wrap { background: #fff; border-radius: 12px; border: 1px solid #e5e7eb; overflow: hidden; }
.um-table { width: 100%; border-collapse: collapse; font-size: 14px; }
.um-table thead th { background: #f9fafb; padding: 11px 16px; text-align: left; font-size: 12px; font-weight: 600; color: #6b7280; text-transform: uppercase; letter-spacing: .5px; border-bottom: 1px solid #e5e7eb; }
.um-table tbody tr { border-bottom: 1px solid #f3f4f6; transition: background .1s; }
.um-table tbody tr:hover { background: #fafafa; }
.um-table tbody tr:last-child { border-bottom: none; }
.um-table td { padding: 12px 16px; }
.um-actions-col { width: 100px; }
.um-actions-cell { display: flex; gap: 4px; }

.um-user-cell { display: flex; align-items: center; gap: 10px; }
.um-avatar { width: 36px; height: 36px; border-radius: 50%; background: #e0e7ff; color: #4f46e5; font-weight: 700; font-size: 14px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.um-name { font-weight: 600; color: #111; }
.um-username { font-size: 12px; color: #9ca3af; }
.um-mail { color: #4b5563; font-size: 13px; }
.um-empty { text-align: center; color: #9ca3af; padding: 40px !important; }

.um-rank-badge { display: inline-block; padding: 3px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }

.um-icon-btn { display: flex; align-items: center; justify-content: center; width: 32px; height: 32px; border: none; border-radius: 6px; background: #f3f4f6; color: #374151; cursor: pointer; transition: all .15s; }
.um-icon-btn:hover { background: #e0e7ff; color: #4f46e5; }
.um-icon-btn .material-symbols-outlined { font-size: 16px; }

/* Modals */
.um-modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,.4); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.um-modal { background: #fff; border-radius: 16px; width: 480px; max-width: 95vw; box-shadow: 0 20px 60px rgba(0,0,0,.15); }
.um-modal-sm { width: 380px; }
.um-modal-header { display: flex; align-items: center; justify-content: space-between; padding: 20px 24px 0; }
.um-modal-header h2 { font-size: 18px; font-weight: 700; color: #111; margin: 0; }
.um-modal-close { background: none; border: none; cursor: pointer; color: #9ca3af; display: flex; }
.um-modal-close:hover { color: #374151; }
.um-modal-body { padding: 20px 24px; display: flex; flex-direction: column; gap: 14px; }
.um-modal-footer { padding: 0 24px 20px; display: flex; justify-content: flex-end; gap: 10px; }

.um-form-row { display: flex; flex-direction: column; gap: 5px; }
.um-form-row label { font-size: 13px; font-weight: 500; color: #374151; }
.um-form-2col { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.um-input { padding: 9px 12px; border: 1px solid #e5e7eb; border-radius: 8px; font-size: 14px; outline: none; transition: border-color .15s; width: 100%; box-sizing: border-box; }
.um-input:focus { border-color: #6366f1; }
.um-form-error { background: #fef2f2; color: #dc2626; border-radius: 8px; padding: 10px 14px; font-size: 13px; }
.um-modal-user { display: flex; align-items: center; gap: 8px; color: #374151; font-weight: 500; margin: 0; }

@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin .7s linear infinite; }
</style>
