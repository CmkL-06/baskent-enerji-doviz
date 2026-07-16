<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import apiService from '@/services/apiservice'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()
const route = useRoute()

const users = ref<any[]>([])
const loading = ref(true)
const error = ref('')

async function loadData() {
  loading.value = true
  error.value = ''
  try {
    users.value = await apiService.getUserActivity() ?? []
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Veri yüklenemedi'
  } finally {
    loading.value = false
  }
}

async function openFromRoute() {
  const userId = route.query.userId as string | undefined
  if (!userId) return
  const user = users.value.find(u => u.id === userId)
  if (!user) return
  if (route.query.date) selectedDate.value = route.query.date as string
  await openDetail(user)
}

onMounted(async () => {
  await loadData()
  await openFromRoute()
})

watch(() => [route.query.userId, route.query.date], openFromRoute)

function fullName(u: any) {
  return `${u.firstname || ''} ${u.lastname || ''}`.trim() || u.username
}

function roleLabel(r: string) {
  const map: Record<string, string> = {
    Banned: 'Yasaklı', User: 'Kullanıcı', Customer: 'Müşteri',
    Staff: 'Personel', Admin: 'Admin', Owner: 'Sahip'
  }
  return map[r] || r
}

function roleBadgeClass(r: string) {
  const map: Record<string, string> = {
    Owner: 'role-owner', Admin: 'role-manager', Staff: 'role-cashier',
    Banned: 'role-banned', User: 'role-user'
  }
  return map[r] || 'role-viewer'
}

function formatDate(d: string | null) {
  if (!d) return 'Hiç giriş yapmadı'
  return new Date(d).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

// --- Günlük detay drill-down: oturum çizelgesi, gün sonu, işlem giriş deseni ---
const showDetailModal = ref(false)
const detailLoading = ref(false)
const detailUser = ref<any>(null)
const detail = ref<any>(null)

function todayIso() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}
const selectedDate = ref(todayIso())

async function openDetail(u: any) {
  detailUser.value = u
  showDetailModal.value = true
  await loadDetail()
}

async function loadDetail() {
  if (!detailUser.value) return
  detailLoading.value = true
  try {
    detail.value = await apiService.getUserDailyDetail(detailUser.value.id, selectedDate.value)
  } catch (e: any) {
    notification.error(e.response?.data?.message || 'Günlük detay yüklenemedi')
    detail.value = null
  } finally {
    detailLoading.value = false
  }
}

function formatTime(d: string) {
  return new Date(d).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}

function sessionGaps(sessions: any[]) {
  const rows: { type: 'active' | 'idle'; start: string; end: string; minutes: number }[] = []
  for (let i = 0; i < sessions.length; i++) {
    const s = sessions[i]
    rows.push({ type: 'active', start: s.start, end: s.end, minutes: Math.round((new Date(s.end).getTime() - new Date(s.start).getTime()) / 60000) })
    const next = sessions[i + 1]
    if (next) {
      rows.push({ type: 'idle', start: s.end, end: next.start, minutes: Math.round((new Date(next.start).getTime() - new Date(s.end).getTime()) / 60000) })
    }
  }
  return rows
}
</script>

<template>
  <div class="ob-panel">
    <div class="ob-toolbar">
      <h2 class="ob-panel-title">
        <span class="material-symbols-outlined" aria-hidden="true">badge</span>
        Personel Takibi
      </h2>
      <button class="ob-btn ob-btn-outline" @click="loadData">
        <span class="material-symbols-outlined" aria-hidden="true">refresh</span>
        <span class="ob-btn-text">Yenile</span>
      </button>
    </div>

    <div v-if="loading" class="ob-loading">Yükleniyor...</div>
    <div v-else-if="error" class="ob-error">
      {{ error }}
      <button class="ob-btn ob-btn-outline" @click="loadData">Tekrar Dene</button>
    </div>
    <div v-else-if="users.length === 0" class="ob-empty">Personel bulunamadı.</div>

    <div v-else class="sa-table-wrap">
      <table class="sa-table">
        <thead>
          <tr>
            <th>Ad Soyad</th>
            <th>Şube</th>
            <th>Rol</th>
            <th>Son Giriş</th>
            <th>Durum</th>
            <th>Bu Ay Giriş</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in users" :key="u.id" class="sa-row" @click="openDetail(u)">
            <td class="sa-name">{{ fullName(u) }}</td>
            <td>{{ (u.offices || []).join(', ') || '—' }}</td>
            <td><span class="role-badge" :class="roleBadgeClass(u.rank)">{{ roleLabel(u.rank) }}</span></td>
            <td>{{ formatDate(u.lastLoginDate) }}</td>
            <td>
              <span class="sa-status" :class="u.isOnline ? 'online' : 'offline'">
                <span class="sa-status-dot"></span>
                {{ u.isOnline ? 'Aktif' : 'Çevrimdışı' }}
              </span>
            </td>
            <td>{{ u.loginCountThisMonth }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <Teleport to="body">
      <Transition name="modal">
        <div v-if="showDetailModal" class="ob-overlay" @click.self="showDetailModal = false">
          <div class="ob-modal">
            <div class="ob-modal-header">
              <div class="ob-modal-title-block">
                <strong>{{ detailUser ? fullName(detailUser) : '' }} — Günlük Detay</strong>
              </div>
              <button class="ob-modal-close" @click="showDetailModal = false">
                <span class="material-symbols-outlined" aria-hidden="true">close</span>
              </button>
            </div>
            <div class="ob-modal-body">
              <div class="ob-field">
                <label>Tarih</label>
                <input type="date" v-model="selectedDate" class="ob-input" @change="loadDetail" />
              </div>

              <div v-if="detailLoading" class="ob-loading">Yükleniyor...</div>
              <template v-else-if="detail">
                <div class="sa-section">
                  <h4 class="sa-section-title">Oturum Zaman Çizelgesi</h4>
                  <div v-if="detail.sessions.length === 0" class="ob-empty">Bu gün için sistem aktivitesi yok.</div>
                  <div v-else class="sa-timeline">
                    <div v-for="(row, i) in sessionGaps(detail.sessions)" :key="i" class="sa-timeline-row" :class="row.type">
                      <span class="material-symbols-outlined" aria-hidden="true">{{ row.type === 'active' ? 'bolt' : 'pause_circle' }}</span>
                      <span class="sa-timeline-range">{{ formatTime(row.start) }} – {{ formatTime(row.end) }}</span>
                      <span class="sa-timeline-note">{{ row.type === 'active' ? 'aktif' : `${row.minutes} dk hareketsiz` }}</span>
                    </div>
                  </div>
                </div>

                <div class="sa-section">
                  <h4 class="sa-section-title">Gün Sonu</h4>
                  <div v-if="detail.dayClosures.length === 0" class="sa-badge warn">
                    <span class="material-symbols-outlined" aria-hidden="true">warning</span> Alınmadı
                  </div>
                  <div v-else v-for="(dc, i) in detail.dayClosures" :key="i"
                       class="sa-badge" :class="dc.status === 'Closed' || dc.status === 'AutoClosed' ? 'ok' : 'warn'">
                    <span class="material-symbols-outlined" aria-hidden="true">
                      {{ dc.status === 'Closed' || dc.status === 'AutoClosed' ? 'check_circle' : 'warning' }}
                    </span>
                    {{ dc.officeName }} — {{ dc.status === 'Closed' || dc.status === 'AutoClosed' ? 'Alındı' : 'Alınmadı' }}
                    <span v-if="dc.closedAt"> ({{ formatTime(dc.closedAt) }}, {{ dc.closedByUser }})</span>
                  </div>
                </div>

                <div class="sa-section">
                  <h4 class="sa-section-title">İşlem Girişi</h4>
                  <div v-if="detail.transactionCount === 0" class="ob-empty">Bu gün işlem girilmemiş.</div>
                  <template v-else>
                    <div class="sa-badge" :class="detail.isBulkEntrySuspected ? 'warn' : 'ok'">
                      <span class="material-symbols-outlined" aria-hidden="true">{{ detail.isBulkEntrySuspected ? 'warning' : 'check_circle' }}</span>
                      {{ detail.transactionCount }} işlem, {{ formatTime(detail.firstTransactionAt) }}–{{ formatTime(detail.lastTransactionAt) }} arası
                      {{ detail.isBulkEntrySuspected ? '(toplu girilmiş şüphesi)' : '(yayılmış)' }}
                    </div>
                    <div v-if="detail.baselineRatio != null" class="sa-baseline-hint">
                      Kişisel ortalama: %{{ Math.round(detail.baselineRatio * 100) }} yayılım — bugün: %{{ Math.round((detail.todayRatio ?? 0) * 100) }}
                    </div>
                    <div v-if="detail.backdatedCount > 0" class="sa-badge warn" style="margin-top: 0.4rem">
                      <span class="material-symbols-outlined" aria-hidden="true">history_toggle_off</span>
                      {{ detail.backdatedCount }} işlem geriye tarihli girilmiş (giriş anı ile işlem tarihi arasında 2+ saat fark)
                    </div>
                  </template>
                </div>
              </template>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.ob-panel { background: var(--color-bg-card, #fff); border-radius: var(--radius-lg); padding: 1.25rem; }
.ob-toolbar { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
.ob-panel-title { display: flex; align-items: center; gap: 0.5rem; font-size: 1.1rem; font-weight: 700; margin: 0; }
.ob-loading, .ob-empty { padding: 2rem; text-align: center; color: var(--color-text-secondary); }
.ob-error { padding: 2rem; text-align: center; color: var(--color-danger); display: flex; flex-direction: column; gap: 0.75rem; align-items: center; }

.sa-table-wrap { overflow-x: auto; }
.sa-table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
.sa-table th {
  text-align: left; padding: 0.6rem 0.75rem; font-size: 0.75rem; font-weight: 700;
  color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: 0.03em;
  border-bottom: 2px solid var(--color-border);
}
.sa-table td { padding: 0.6rem 0.75rem; border-bottom: 1px solid var(--color-border); }
.sa-row { cursor: pointer; transition: background-color .15s; }
.sa-row:hover { background: var(--color-bg-page); }
.sa-name { font-weight: 600; }

.sa-status { display: inline-flex; align-items: center; gap: 0.35rem; font-size: 0.8rem; font-weight: 600; }
.sa-status.online { color: var(--color-success); }
.sa-status.offline { color: var(--color-text-muted); }
.sa-status-dot { width: 8px; height: 8px; border-radius: 50%; background: currentColor; }

.role-badge { padding: 2px 8px; border-radius: var(--radius-md); font-size: 0.7rem; font-weight: 700; }
.role-owner { background: #fce7f3; color: #be185d; }
.role-manager { background: #fef3c7; color: #b45309; }
.role-cashier { background: var(--color-secondary-light); color: var(--color-secondary-hover); }
.role-banned { background: #fef2f2; color: var(--color-danger); }
.role-user { background: #f0fdf4; color: #15803d; }
.role-viewer { background: var(--color-bg-page); color: var(--color-text-secondary); }

.sa-section { margin-top: 1.25rem; }
.sa-section:first-of-type { margin-top: 1rem; }
.sa-section-title { font-size: 0.8rem; font-weight: 700; color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: 0.03em; margin: 0 0 0.5rem; }

.sa-timeline { display: flex; flex-direction: column; gap: 0.35rem; max-height: 260px; overflow-y: auto; }
.sa-timeline-row {
  display: flex; align-items: center; gap: 0.5rem; padding: 0.4rem 0.6rem;
  border-radius: var(--radius-md); font-size: 0.8rem;
}
.sa-timeline-row.active { background: #f0fdf4; color: #15803d; }
.sa-timeline-row.idle { background: var(--color-bg-page); color: var(--color-text-muted); }
.sa-timeline-row .material-symbols-outlined { font-size: 1rem; }
.sa-timeline-range { font-weight: 700; }
.sa-timeline-note { color: inherit; opacity: 0.85; }

.sa-badge {
  display: flex; align-items: center; gap: 0.4rem; padding: 0.5rem 0.7rem;
  border-radius: var(--radius-md); font-size: 0.82rem; font-weight: 600;
}
.sa-badge .material-symbols-outlined { font-size: 1.1rem; }
.sa-badge.ok { background: #f0fdf4; color: #15803d; }
.sa-badge.warn { background: #fffbeb; color: #b45309; }

.sa-baseline-hint { font-size: 0.75rem; color: var(--color-text-secondary); margin-top: 0.3rem; }

.ob-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,0.45);
  display: flex; align-items: center; justify-content: center; z-index: 200;
}
.ob-modal { background: #fff; border-radius: var(--radius-lg); width: min(480px, calc(100vw - 2rem)); max-height: 85vh; overflow-y: auto; box-shadow: var(--shadow-bold); }
.ob-modal--sm { width: min(420px, calc(100vw - 2rem)); }
.ob-modal-header { display: flex; align-items: center; justify-content: space-between; padding: 1rem 1.25rem; border-bottom: 1px solid var(--color-border); }
.ob-modal-close { background: none; border: none; cursor: pointer; color: var(--color-text-secondary); }
.ob-modal-body { padding: 1.25rem; }
.ob-field-row { display: flex; gap: 0.75rem; margin-bottom: 0.75rem; }
.ob-field { display: flex; flex-direction: column; gap: 0.25rem; flex: 1; }
.ob-field label { font-size: 0.75rem; font-weight: 600; color: var(--color-text-secondary); }
.ob-input { padding: 0.5rem 0.6rem; border: 1px solid var(--color-border); border-radius: var(--radius-md); font-size: 0.875rem; }

.modal-enter-active, .modal-leave-active { transition: opacity .2s; }
.modal-enter-from, .modal-leave-to { opacity: 0; }
</style>
