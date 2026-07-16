<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiService from '@/services/apiservice'
import { useNotification } from '@/composables/useNotification'

const notification = useNotification()

const closures = ref<any[]>([])
const loading = ref(true)
const error = ref('')
const actingId = ref<string | null>(null)
const rejectNoteFor = ref<string | null>(null)
const rejectNote = ref('')

async function loadData() {
  loading.value = true
  error.value = ''
  try {
    closures.value = await apiService.getPendingDayClosureApprovals() ?? []
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Veri yüklenemedi'
  } finally {
    loading.value = false
  }
}

onMounted(loadData)

function formatDate(d: string) {
  return new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function discrepancyRows(closure: any) {
  return (closure.details || []).filter((d: any) => Math.abs(d.discrepancy) > 0.0001)
}

function fmt(n: number) {
  return (n ?? 0).toLocaleString('tr-TR', { minimumFractionDigits: 0, maximumFractionDigits: 0 })
}

async function approve(closure: any) {
  if (!confirm(`${closure.officeName} — ${formatDate(closure.businessDate)} kapanışını onaylıyor musunuz? Bakiye düzeltmeleri hemen uygulanacak.`)) return
  actingId.value = closure.id
  try {
    await apiService.approveDayClosure(closure.id, true)
    notification.success('Kapanış onaylandı, bakiyeler güncellendi')
    await loadData()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Onaylama başarısız')
  } finally {
    actingId.value = null
  }
}

function openReject(closure: any) {
  rejectNoteFor.value = closure.id
  rejectNote.value = ''
}

async function confirmReject(closure: any) {
  actingId.value = closure.id
  try {
    await apiService.approveDayClosure(closure.id, false, rejectNote.value)
    notification.success('Kapanış reddedildi, personel tekrar kapanış yapmalı')
    rejectNoteFor.value = null
    await loadData()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Reddetme başarısız')
  } finally {
    actingId.value = null
  }
}
</script>

<template>
  <div class="ob-panel">
    <div class="ob-toolbar">
      <h2 class="ob-panel-title">
        <span class="material-symbols-outlined" aria-hidden="true">fact_check</span>
        Onay Bekleyen Kapanışlar
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
    <div v-else-if="closures.length === 0" class="ob-empty">
      Onay bekleyen kapanış yok — sayım farkı 500 TL eşiğini aşan bir kapanış olduğunda burada görünecek.
    </div>

    <div v-else class="pa-list">
      <div v-for="c in closures" :key="c.id" class="pa-card">
        <div class="pa-card-header">
          <div>
            <strong>{{ c.officeName }}</strong>
            <span class="pa-date">{{ formatDate(c.businessDate) }}</span>
          </div>
          <span class="sa-badge warn">
            <span class="material-symbols-outlined" aria-hidden="true">warning</span>
            Owner onayı bekliyor
          </span>
        </div>

        <table class="pa-table">
          <thead>
            <tr>
              <th>Döviz</th>
              <th>Sistem</th>
              <th>Sayılan</th>
              <th>Fark</th>
              <th>Açıklama</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="d in discrepancyRows(c)" :key="d.currencyId">
              <td>{{ d.currencyCode }}</td>
              <td>{{ fmt(d.systemBalance) }}</td>
              <td>{{ fmt(d.physicalCount) }}</td>
              <td :class="d.discrepancy > 0 ? 'pa-pos' : 'pa-neg'">
                {{ d.discrepancy > 0 ? '+' : '' }}{{ fmt(d.discrepancy) }}
              </td>
              <td class="pa-note">{{ d.discrepancyNote || '—' }}</td>
            </tr>
          </tbody>
        </table>

        <div v-if="rejectNoteFor === c.id" class="pa-reject-box">
          <label>Reddetme notu (opsiyonel)</label>
          <input v-model="rejectNote" class="ob-input" placeholder="Neden reddedildi?" />
          <div class="pa-actions">
            <button class="ob-btn ob-btn-outline" @click="rejectNoteFor = null">Vazgeç</button>
            <button class="ob-btn pa-btn-danger" :disabled="actingId === c.id" @click="confirmReject(c)">Reddi Onayla</button>
          </div>
        </div>
        <div v-else class="pa-actions">
          <button class="ob-btn ob-btn-outline" :disabled="actingId === c.id" @click="openReject(c)">Reddet</button>
          <button class="ob-btn pa-btn-approve" :disabled="actingId === c.id" @click="approve(c)">
            <span class="material-symbols-outlined" aria-hidden="true">check_circle</span>
            Onayla
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.ob-panel { background: var(--color-bg-card, #fff); border-radius: var(--radius-lg); padding: 1.25rem; }
.ob-toolbar { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
.ob-panel-title { display: flex; align-items: center; gap: 0.5rem; font-size: 1.1rem; font-weight: 700; margin: 0; }
.ob-loading, .ob-empty { padding: 2rem; text-align: center; color: var(--color-text-secondary); }
.ob-error { padding: 2rem; text-align: center; color: var(--color-danger); display: flex; flex-direction: column; gap: 0.75rem; align-items: center; }
.ob-btn { display: inline-flex; align-items: center; gap: 0.35rem; padding: 0.45rem 0.8rem; border-radius: var(--radius-md); font-size: 0.85rem; font-weight: 600; cursor: pointer; border: 1px solid transparent; }
.ob-btn-outline { background: transparent; border-color: var(--color-border); color: var(--color-text-primary); }
.ob-btn:disabled { opacity: 0.6; cursor: not-allowed; }
.ob-input { padding: 0.5rem 0.6rem; border: 1px solid var(--color-border); border-radius: var(--radius-md); font-size: 0.875rem; width: 100%; }

.sa-badge { display: inline-flex; align-items: center; gap: 0.4rem; padding: 0.4rem 0.65rem; border-radius: var(--radius-md); font-size: 0.8rem; font-weight: 600; }
.sa-badge .material-symbols-outlined { font-size: 1.05rem; }
.sa-badge.warn { background: #fffbeb; color: #b45309; }

.pa-list { display: flex; flex-direction: column; gap: 1rem; }
.pa-card { border: 1px solid var(--color-border); border-radius: var(--radius-lg); padding: 1rem; }
.pa-card-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.75rem; }
.pa-date { margin-left: 0.6rem; color: var(--color-text-secondary); font-size: 0.85rem; }

.pa-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; margin-bottom: 0.75rem; }
.pa-table th { text-align: left; padding: 0.4rem 0.6rem; font-size: 0.72rem; font-weight: 700; color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: 0.03em; border-bottom: 2px solid var(--color-border); }
.pa-table td { padding: 0.4rem 0.6rem; border-bottom: 1px solid var(--color-border); }
.pa-pos { color: #15803d; font-weight: 700; }
.pa-neg { color: var(--color-danger); font-weight: 700; }
.pa-note { color: var(--color-text-secondary); font-size: 0.8rem; }

.pa-actions { display: flex; justify-content: flex-end; gap: 0.5rem; }
.pa-btn-approve { background: #15803d; color: #fff; }
.pa-btn-danger { background: var(--color-danger); color: #fff; }

.pa-reject-box { display: flex; flex-direction: column; gap: 0.4rem; padding: 0.75rem; background: var(--color-bg-page); border-radius: var(--radius-md); margin-top: 0.5rem; }
.pa-reject-box label { font-size: 0.75rem; font-weight: 600; color: var(--color-text-secondary); }
</style>
