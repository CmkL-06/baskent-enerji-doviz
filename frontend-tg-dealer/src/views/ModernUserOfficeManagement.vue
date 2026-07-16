<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'

const router    = useRouter()
const authStore = useAuthStore()

const users       = ref<any[]>([])
const offices     = ref<any[]>([])
const userOffices = ref<any[]>([])  // selected user's offices
const loading     = ref(false)
const saving      = ref(false)
const error       = ref('')
const search      = ref('')
const selected    = ref<any>(null)

onMounted(async () => {
  if (!authStore.isAdmin) { router.push('/ihtiyar/dashboard'); return }
  loading.value = true
  try {
    const [u, o] = await Promise.all([apiService.getUsers(), apiService.getOffices()])
    users.value   = u ?? []
    offices.value = o ?? []
  } catch (e: any) {
    error.value = e.response?.data?.message || 'Veriler yüklenemedi'
  } finally {
    loading.value = false
  }
})

const filteredUsers = computed(() => {
  const q = search.value.toLowerCase()
  if (!q) return users.value
  return users.value.filter(u =>
    u.username?.toLowerCase().includes(q) ||
    (u.firstname + ' ' + u.lastname).toLowerCase().includes(q)
  )
})

const assignedIds = computed(() => new Set(userOffices.value.map((o: any) => o.officeId ?? o.id)))

async function selectUser(user: any) {
  selected.value  = user
  userOffices.value = []
  try {
    const res = await apiService.getUserOffices(user.id)
    userOffices.value = res ?? []
  } catch { userOffices.value = [] }
}

async function toggleOffice(office: any) {
  if (!selected.value || !authStore.isAdmin) return
  const officeId = office.officeId ?? office.id
  saving.value = true
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
  } finally { saving.value = false }
}

function avatarColor(name: string) {
  const colors = ['#6366f1','#8b5cf6','#ec4899','#f97316','#14b8a6','#0ea5e9','#84cc16','#ef4444']
  return colors[(name?.charCodeAt(0) ?? 0) % colors.length]
}
</script>

<template>
  <div class="uom-page">

    <!-- Header -->
    <div class="uom-header">
      <div>
        <h1 class="uom-title">Kullanıcı - Ofis Yetkilendirme</h1>
        <p class="uom-sub">Hangi kullanıcının hangi ofislere erişebileceğini yönetin</p>
      </div>
    </div>

    <div v-if="error" class="uom-error">{{ error }}</div>

    <div class="uom-layout">

      <!-- Left: User List -->
      <div class="uom-left">
        <div class="uom-search-wrap">
          <span class="material-symbols-outlined uom-si">search</span>
          <input v-model="search" class="uom-search" placeholder="Kullanıcı ara..." />
        </div>

        <div v-if="loading" class="uom-loader">
          <div class="uom-spinner"></div>
        </div>

        <div v-else class="uom-user-list">
          <div
            v-for="u in filteredUsers"
            :key="u.id"
            class="uom-user-row"
            :class="{ active: selected?.id === u.id }"
            @click="selectUser(u)"
          >
            <div class="uom-avatar" :style="{ background: avatarColor(u.firstname || u.username) }">
              {{ (u.firstname || u.username || '?')[0].toUpperCase() }}
            </div>
            <div class="uom-user-info">
              <div class="uom-user-name">{{ u.firstname }} {{ u.lastname }}</div>
              <div class="uom-user-sub">@{{ u.username }}</div>
            </div>
            <span class="material-symbols-outlined uom-chevron">chevron_right</span>
          </div>
          <div v-if="!filteredUsers.length" class="uom-empty">Kullanıcı bulunamadı</div>
        </div>
      </div>

      <!-- Right: Office Assignment -->
      <div class="uom-right">
        <template v-if="!selected">
          <div class="uom-placeholder">
            <span class="material-symbols-outlined" aria-hidden="true">manage_accounts</span>
            <p>Soldaki listeden bir kullanıcı seçin</p>
          </div>
        </template>

        <template v-else>
          <div class="uom-right-header">
            <div class="uom-panel-avatar" :style="{ background: avatarColor(selected.firstname || selected.username) }">
              {{ (selected.firstname || selected.username || '?')[0].toUpperCase() }}
            </div>
            <div>
              <div class="uom-panel-name">{{ selected.firstname }} {{ selected.lastname }}</div>
              <div class="uom-panel-sub">@{{ selected.username }} · {{ assignedIds.size }} ofis atanmış</div>
            </div>
          </div>

          <div class="uom-offices-label">Ofisler</div>

          <div v-if="offices.length === 0" class="uom-empty" style="padding:40px">Ofis bulunamadı</div>

          <div v-else class="uom-offices-list">
            <div
              v-for="office in offices"
              :key="office.officeId ?? office.id"
              class="uom-office-row"
              :class="{ assigned: assignedIds.has(office.officeId ?? office.id) }"
            >
              <div class="uom-office-info">
                <span class="material-symbols-outlined uom-office-icon">store</span>
                <div>
                  <div class="uom-office-name">{{ office.officeName ?? office.name }}</div>
                  <div class="uom-office-sub">{{ office.officeCode ?? office.code ?? '' }}</div>
                </div>
              </div>
              <button
                class="uom-toggle"
                :class="{ on: assignedIds.has(office.officeId ?? office.id) }"
                :disabled="saving || !authStore.isAdmin"
                @click="toggleOffice(office)"
              >
                <span class="uom-toggle-ball"></span>
              </button>
            </div>
          </div>
        </template>
      </div>

    </div>
  </div>
</template>

<style scoped>
.uom-page { padding: 24px; }
.uom-header { margin-bottom: 20px; }
.uom-title { font-size: 22px; font-weight: 700; color: var(--color-text); margin: 0; }
.uom-sub   { font-size: 13px; color: var(--color-text-muted); margin: 3px 0 0; }
.uom-error { background: var(--color-danger-bg); border: 1px solid #fecaca; color: var(--color-danger); border-radius: var(--radius-md); padding: 12px 16px; font-size: 14px; margin-bottom: 16px; }

.uom-layout { display: grid; grid-template-columns: 300px 1fr; gap: 20px; min-height: 500px; }

/* Left */
.uom-left { background: #fff; border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow: hidden; display: flex; flex-direction: column; }

.uom-search-wrap { position: relative; padding: 12px; border-bottom: 1px solid var(--color-bg-page); }
.uom-si { position: absolute; left: 22px; top: 50%; transform: translateY(-50%); font-size: 17px; color: var(--color-text-muted); pointer-events: none; }
.uom-search { width: 100%; box-sizing: border-box; padding: 8px 10px 8px 34px; border: 1px solid var(--color-border); border-radius: var(--radius-md); font-size: 13px; outline: none; }
.uom-search:focus { border-color: var(--color-primary); }

.uom-loader { display: flex; justify-content: center; padding: 40px; }
.uom-spinner { width: 24px; height: 24px; border: 2px solid var(--color-border); border-top-color: var(--color-primary); border-radius: 50%; animation: spin .7s linear infinite; }

.uom-user-list { overflow-y: auto; flex: 1; }
.uom-user-row { display: flex; align-items: center; gap: 10px; padding: 12px 14px; cursor: pointer; border-bottom: 1px solid var(--color-bg-page); transition: background .12s; }
.uom-user-row:hover { background: var(--color-bg-page); }
.uom-user-row.active { background: var(--color-primary-light); }
.uom-avatar { width: 36px; height: 36px; border-radius: 50%; color: #fff; font-weight: 700; font-size: 14px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.uom-user-info { flex: 1; min-width: 0; }
.uom-user-name { font-size: 14px; font-weight: 600; color: var(--color-text); }
.uom-user-sub  { font-size: 12px; color: var(--color-text-muted); }
.uom-chevron   { font-size: 18px; color: var(--color-border); }
.uom-user-row.active .uom-chevron { color: var(--color-primary); }
.uom-empty { text-align: center; padding: 30px; color: var(--color-text-muted); font-size: 13px; }

/* Right */
.uom-right { background: #fff; border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow: hidden; }

.uom-placeholder { display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; gap: 12px; color: var(--color-text-muted); padding: 60px; }
.uom-placeholder .material-symbols-outlined { font-size: 52px; }
.uom-placeholder p { font-size: 14px; margin: 0; }

.uom-right-header { display: flex; align-items: center; gap: 14px; padding: 20px; border-bottom: 1px solid var(--color-bg-page); }
.uom-panel-avatar { width: 44px; height: 44px; border-radius: 50%; color: #fff; font-weight: 700; font-size: 17px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.uom-panel-name { font-weight: 700; font-size: 16px; color: var(--color-text); }
.uom-panel-sub  { font-size: 12px; color: var(--color-text-muted); }

.uom-offices-label { padding: 12px 20px 8px; font-size: 11px; font-weight: 700; color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: .6px; }

.uom-offices-list { padding: 0 12px 12px; display: flex; flex-direction: column; gap: 6px; }
.uom-office-row { display: flex; align-items: center; justify-content: space-between; padding: 12px 14px; border: 1.5px solid var(--color-border); border-radius: var(--radius-lg); transition: border-color 0.2s, background-color 0.2s; }
.uom-office-row.assigned { border-color: #a5b4fc; background: var(--color-primary-light); }
.uom-office-info { display: flex; align-items: center; gap: 12px; }
.uom-office-icon { font-size: 20px; color: var(--color-text-muted); }
.uom-office-row.assigned .uom-office-icon { color: var(--color-primary); }
.uom-office-name { font-size: 14px; font-weight: 600; color: var(--color-text); }
.uom-office-sub  { font-size: 12px; color: var(--color-text-muted); }

/* Toggle Switch */
.uom-toggle { position: relative; width: 44px; height: 24px; border-radius: var(--radius-lg); border: none; background: var(--color-border); cursor: pointer; transition: background .2s; flex-shrink: 0; padding: 0; }
.uom-toggle.on { background: var(--color-primary); }
.uom-toggle:disabled { opacity: .5; cursor: not-allowed; }
.uom-toggle-ball { position: absolute; top: 3px; left: 3px; width: 18px; height: 18px; border-radius: 50%; background: #fff; transition: transform .2s; box-shadow: 0 1px 3px rgba(0,0,0,.2); }
.uom-toggle.on .uom-toggle-ball { transform: translateX(20px); }

@keyframes spin { to { transform: rotate(360deg); } }
</style>
