<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'
import OwnerDashboard from './owner/OwnerDashboard.vue'
import OwnerBranches from './owner/OwnerBranches.vue'
import OwnerTransfers from './owner/OwnerTransfers.vue'
import OwnerBalances from './owner/OwnerBalances.vue'
import OwnerReports from './owner/OwnerReports.vue'
import OwnerQr from './owner/OwnerQr.vue'
import OwnerVaultInspect from './owner/OwnerVaultInspect.vue'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

onMounted(() => {
  if (!authStore.isOwner) { router.push('/ihtiyar/dashboard'); return }
  const tab = route.query.tab as Tab | undefined
  if (tab && tabs.some(t => t.id === tab)) activeTab.value = tab
  if (route.query.alerts === '1') showAlertPanel.value = true
  loadAlerts()
})

type Tab = 'dashboard' | 'branches' | 'transfers' | 'balances' | 'inspect' | 'reports' | 'qr'
const activeTab = ref<Tab>('dashboard')
const refreshKey = ref(0)

const tabs: { id: Tab; icon: string; label: string }[] = [
  { id: 'dashboard',  icon: 'monitoring',       label: 'Genel Bakış' },
  { id: 'branches',   icon: 'store',            label: 'Şubeler' },
  { id: 'transfers',  icon: 'swap_horiz',       label: 'Transferler' },
  { id: 'balances',   icon: 'account_balance',   label: 'Bakiye İşlemleri' },
  { id: 'inspect',    icon: 'manage_search',     label: 'Kasa İnceleme' },
  { id: 'reports',    icon: 'insert_chart',      label: 'Raporlar' },
  { id: 'qr',         icon: 'qr_code_2',        label: 'QR Oluştur' },
]

const alerts = ref<any[]>([])
const showAlertPanel = ref(false)
const alertLoading = ref(false)

async function loadAlerts() {
  try {
    alerts.value = await apiService.getUnreadAlerts() ?? []
  } catch { alerts.value = [] }
}

async function markRead(alertId: string) {
  try {
    await apiService.markAlertRead(alertId)
    alerts.value = alerts.value.filter(a => a.id !== alertId)
  } catch {}
}

async function markAllRead() {
  try {
    await apiService.markAllAlertsRead()
    alerts.value = []
    showAlertPanel.value = false
  } catch {}
}

function severityColor(s: string) {
  if (s === 'Critical') return '#ef4444'
  if (s === 'Warning') return '#f59e0b'
  return '#3b82f6'
}

function severityIcon(s: string) {
  if (s === 'Critical') return 'error'
  if (s === 'Warning') return 'warning'
  return 'info'
}

function refresh() {
  refreshKey.value++
  loadAlerts()
}
</script>

<template>
  <div class="owner-panel">
    <div class="op-header">
      <div class="op-header-left">
        <span class="material-symbols-outlined op-crown">shield_person</span>
        <div>
          <h1 class="op-title">Yönetim Paneli</h1>
          <p class="op-sub">Çok şubeli döviz ofisi yönetim merkezi</p>
        </div>
      </div>
      <div class="op-header-actions">
        <div class="op-bell-wrap">
          <button class="op-bell-btn" @click="showAlertPanel = !showAlertPanel">
            <span class="material-symbols-outlined" aria-hidden="true">notifications</span>
            <span v-if="alerts.length" class="op-bell-badge">{{ alerts.length }}</span>
          </button>
          <div v-if="showAlertPanel" class="op-alert-panel">
            <div class="op-alert-header">
              <strong>Uyarılar</strong>
              <button v-if="alerts.length" class="op-alert-clear" @click="markAllRead">Tümünü Oku</button>
            </div>
            <div v-if="alerts.length === 0" class="op-alert-empty">
              <span class="material-symbols-outlined" aria-hidden="true">check_circle</span> Okunmamış uyarı yok
            </div>
            <div v-else class="op-alert-list">
              <div v-for="a in alerts" :key="a.id" class="op-alert-item">
                <span class="material-symbols-outlined op-alert-icon" :style="{ color: severityColor(a.severity) }">
                  {{ severityIcon(a.severity) }}
                </span>
                <div class="op-alert-body">
                  <div class="op-alert-title">{{ a.title }}</div>
                  <div class="op-alert-msg">{{ a.message }}</div>
                  <div class="op-alert-meta">{{ a.officeName }}</div>
                </div>
                <button class="op-alert-dismiss" @click="markRead(a.id)" title="Okundu">
                  <span class="material-symbols-outlined" aria-hidden="true">close</span>
                </button>
              </div>
            </div>
          </div>
        </div>
        <button class="op-refresh-btn" @click="refresh">
          <span class="material-symbols-outlined" aria-hidden="true">refresh</span>
          Yenile
        </button>
      </div>
    </div>

    <div class="op-tabs">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        class="op-tab"
        :class="{ active: activeTab === tab.id }"
        @click="activeTab = tab.id"
      >
        <span class="material-symbols-outlined" aria-hidden="true">{{ tab.icon }}</span>
        {{ tab.label }}
      </button>
    </div>

    <OwnerDashboard   v-if="activeTab === 'dashboard'"  :key="'d-' + refreshKey" />
    <OwnerBranches    v-if="activeTab === 'branches'"   :key="'b-' + refreshKey" />
    <OwnerTransfers   v-if="activeTab === 'transfers'"  :key="'t-' + refreshKey" />
    <OwnerBalances    v-if="activeTab === 'balances'"   :key="'bl-' + refreshKey" />
    <OwnerVaultInspect v-if="activeTab === 'inspect'"   :key="'vi-' + refreshKey" />
    <OwnerReports     v-if="activeTab === 'reports'"    :key="'r-' + refreshKey" />
    <OwnerQr          v-if="activeTab === 'qr'" />
  </div>
</template>

<style scoped>
.owner-panel { padding: 1.5rem; max-width: 1400px; margin: 0 auto; }

.op-header {
  display: flex; align-items: center; justify-content: space-between;
  margin-bottom: 1.5rem;
}
.op-header-left { display: flex; align-items: center; gap: 1rem; }
.op-crown { font-size: 2.5rem; color: var(--color-secondary); }
.op-title { font-size: 1.5rem; font-weight: 800; color: var(--color-text); margin: 0; }
.op-sub   { font-size: 0.875rem; color: var(--color-text-secondary); margin: 0; }
.op-refresh-btn {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.5rem 1rem; background: var(--color-bg-page); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); cursor: pointer; font-size: 0.875rem; color: var(--color-text-secondary);
  transition: background-color .2s;
}
.op-refresh-btn:hover { background: var(--color-border); }

.op-tabs {
  display: flex; gap: 0.25rem; margin-bottom: 1.5rem;
  border-bottom: 2px solid var(--color-border); padding-bottom: 0;
  overflow-x: auto;
}
.op-tab {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.6rem 1rem; background: none; border: none;
  border-bottom: 2px solid transparent; margin-bottom: -2px;
  cursor: pointer; font-size: 0.85rem; color: var(--color-text-secondary);
  transition: color .2s, border-bottom-color .2s; white-space: nowrap;
}
.op-tab:hover  { color: var(--color-secondary); }
.op-tab.active { color: var(--color-secondary); border-bottom-color: var(--color-secondary); font-weight: 600; }
.op-tab .material-symbols-outlined { font-size: 1.1rem; }

.op-header-actions { display: flex; align-items: center; gap: 0.75rem; }

.op-bell-wrap { position: relative; }
.op-bell-btn {
  position: relative; display: flex; align-items: center; justify-content: center;
  width: 40px; height: 40px; background: var(--color-bg-page); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); cursor: pointer; transition: background-color .2s;
}
.op-bell-btn:hover { background: var(--color-border); }
.op-bell-btn .material-symbols-outlined { font-size: 1.25rem; color: var(--color-text-secondary); }
.op-bell-badge {
  position: absolute; top: -4px; right: -4px; background: var(--color-danger); color: white;
  font-size: 0.65rem; font-weight: 700; min-width: 18px; height: 18px;
  border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center;
  padding: 0 4px; line-height: 1;
}

.op-alert-panel {
  position: absolute; top: 48px; right: 0; width: min(360px, calc(100vw - 2rem)); max-height: 420px;
  background: white; border-radius: var(--radius-lg); box-shadow: 0 8px 32px rgba(0,0,0,.15);
  z-index: 100; overflow: hidden;
}
.op-alert-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 0.75rem 1rem; border-bottom: 1px solid var(--color-border);
  font-size: 0.9rem;
}
.op-alert-clear {
  background: none; border: none; color: var(--color-secondary); cursor: pointer;
  font-size: 0.8rem; font-weight: 600;
}
.op-alert-clear:hover { text-decoration: underline; }
.op-alert-empty {
  display: flex; align-items: center; gap: 0.5rem; padding: 2rem;
  justify-content: center; color: var(--color-text-muted); font-size: 0.85rem;
}
.op-alert-list { max-height: 360px; overflow-y: auto; }
.op-alert-item {
  display: flex; align-items: flex-start; gap: 0.5rem; padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--color-bg-page); transition: background .15s;
}
.op-alert-item:hover { background: var(--color-bg-page); }
.op-alert-icon { font-size: 1.1rem; margin-top: 2px; flex-shrink: 0; }
.op-alert-body { flex: 1; min-width: 0; }
.op-alert-title { font-size: 0.8rem; font-weight: 600; color: var(--color-text); }
.op-alert-msg { font-size: 0.75rem; color: var(--color-text-secondary); margin-top: 2px; line-height: 1.3; }
.op-alert-meta { font-size: 0.7rem; color: var(--color-text-muted); margin-top: 3px; }
.op-alert-dismiss {
  background: none; border: none; cursor: pointer; color: var(--color-text-muted);
  padding: 2px; border-radius: var(--radius-sm); flex-shrink: 0;
}
.op-alert-dismiss:hover { color: var(--color-danger); background: #fef2f2; }
.op-alert-dismiss .material-symbols-outlined { font-size: 0.9rem; }
</style>
