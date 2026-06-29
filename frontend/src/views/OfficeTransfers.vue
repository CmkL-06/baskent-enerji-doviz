<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useExchangeStore } from '@/stores/exchange'
import apiService from '@/services/apiservice'
import type { OfficeTransfer } from '@/types/api'

const router        = useRouter()
const authStore     = useAuthStore()
const exchangeStore = useExchangeStore()

// ── State ──────────────────────────────────────────
type Tab = 'pending' | 'all'
const activeTab  = ref<Tab>('pending')

const pending    = ref<OfficeTransfer[]>([])
const allTx      = ref<OfficeTransfer[]>([])
const loading    = ref(true)
const error      = ref('')

// Reject modal
const rejectModal    = ref(false)
const rejectTarget   = ref<string | null>(null)
const rejectReason   = ref('')
const rejectSaving   = ref(false)

// Create request modal
const createModal  = ref(false)
const createSaving = ref(false)
const createError  = ref('')
const vaults       = ref<any[]>([])
const currencies   = ref<any[]>([])

const createForm = ref({
  sourceVaultId: '',
  targetVaultId: '',
  currencyId:    '',
  amount:        null as number | null,
  notes:         '',
})

// ── Load ───────────────────────────────────────────
onMounted(async () => {
  if (!authStore.isAdmin) { router.push('/ihtiyar/dashboard'); return }
  await Promise.all([loadPending(), loadVaultsCurrencies()])
})

async function loadPending() {
  loading.value = true
  error.value   = ''
  try {
    if (authStore.isAdmin) {
      pending.value = await apiService.getPendingTransfers() ?? []
    }
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Veri yüklenemedi'
  } finally {
    loading.value = false
  }
}

async function loadAll() {
  loading.value = true
  error.value   = ''
  try {
    // load transfers for all accessible offices (fetch my-access offices first)
    const myAccess = await apiService.getMyOfficeAccess() ?? []
    const results: OfficeTransfer[] = []
    for (const uo of myAccess) {
      const txs = await apiService.getTransfersByOffice(uo.officeId) ?? []
      results.push(...txs)
    }
    // deduplicate by id
    const seen = new Set<string>()
    allTx.value = results.filter(t => { if (seen.has(t.id)) return false; seen.add(t.id); return true })
      .sort((a, b) => new Date(b.createdDate).getTime() - new Date(a.createdDate).getTime())
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Geçmiş yüklenemedi'
  } finally {
    loading.value = false
  }
}

async function loadVaultsCurrencies() {
  try {
    const [v, c] = await Promise.all([
      apiService.getVaults(),
      apiService.getCurrencies(),
    ])
    vaults.value    = Array.isArray(v) ? v : (v?.items ?? v?.data ?? [])
    currencies.value = Array.isArray(c) ? c : (c?.items ?? c?.data ?? [])
  } catch {}
}

async function switchTab(tab: Tab) {
  activeTab.value = tab
  error.value     = ''
  if (tab === 'pending') await loadPending()
  else await loadAll()
}

// ── Approve ────────────────────────────────────────
async function approve(id: string) {
  try {
    await apiService.processTransfer(id, { approve: true })
    await loadPending()
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Onaylama başarısız'
  }
}

// ── Reject ─────────────────────────────────────────
function openReject(id: string) {
  rejectTarget.value = id
  rejectReason.value = ''
  rejectModal.value  = true
}

async function confirmReject() {
  if (!rejectTarget.value) return
  rejectSaving.value = true
  try {
    await apiService.processTransfer(rejectTarget.value, { approve: false, rejectionReason: rejectReason.value })
    rejectModal.value = false
    await loadPending()
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Red işlemi başarısız'
  } finally {
    rejectSaving.value = false
  }
}

// ── Create request ─────────────────────────────────
function openCreate() {
  createError.value = ''
  createForm.value  = { sourceVaultId: '', targetVaultId: '', currencyId: '', amount: null, notes: '' }
  createModal.value = true
}

async function submitCreate() {
  if (!createForm.value.sourceVaultId || !createForm.value.targetVaultId || !createForm.value.currencyId || !createForm.value.amount) {
    createError.value = 'Tüm zorunlu alanları doldurun.'
    return
  }
  if (createForm.value.sourceVaultId === createForm.value.targetVaultId) {
    createError.value = 'Kaynak ve hedef kasa aynı olamaz.'
    return
  }
  createSaving.value = true
  createError.value  = ''
  try {
    await apiService.createTransferRequest({ ...createForm.value })
    createModal.value = false
    await loadPending()
  } catch (e: any) {
    createError.value = e?.response?.data?.error || e.message || 'Talep oluşturulamadı'
  } finally {
    createSaving.value = false
  }
}

// ── Helpers ────────────────────────────────────────
function statusBadge(status: string) {
  return {
    Pending:   'bg-yellow-500/20 text-yellow-300 border-yellow-500/30',
    Completed: 'bg-green-500/20  text-green-300  border-green-500/30',
    Rejected:  'bg-red-500/20    text-red-300    border-red-500/30',
    Approved:  'bg-blue-500/20   text-blue-300   border-blue-500/30',
    Cancelled: 'bg-gray-500/20   text-gray-400   border-gray-500/30',
  }[status] ?? 'bg-gray-700 text-gray-300 border-gray-600'
}

function statusLabel(s: string) {
  return { Pending:'Bekliyor', Completed:'Tamamlandı', Rejected:'Reddedildi', Approved:'Onaylandı', Cancelled:'İptal' }[s] ?? s
}

function fmt(n: number) { return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function fmtDate(d: string) { return new Date(d).toLocaleString('tr-TR', { day:'2-digit', month:'short', hour:'2-digit', minute:'2-digit' }) }
</script>

<template>
  <div class="min-h-screen bg-gray-950 text-white p-6">

    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-white">Ofislerarası Transferler</h1>
        <p class="text-gray-400 text-sm mt-1">Merkez ↔ Şube/Bayi para transferleri</p>
      </div>
      <button
        @click="openCreate"
        class="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-lg text-sm font-medium transition-colors"
      >
        <span class="material-icons text-base">send</span>
        Transfer Talebi
      </button>
    </div>

    <!-- Tabs -->
    <div class="flex gap-1 p-1 bg-gray-900 rounded-xl border border-gray-800 mb-6 w-fit">
      <button
        v-for="t in [{ id: 'pending', label: 'Bekleyen', icon: 'hourglass_empty' }, { id: 'all', label: 'Tümü', icon: 'history' }]"
        :key="t.id"
        @click="switchTab(t.id as Tab)"
        :class="['flex items-center gap-2 px-4 py-2 rounded-lg text-sm transition-colors',
          activeTab === t.id ? 'bg-gray-700 text-white font-medium' : 'text-gray-400 hover:text-white']"
      >
        <span class="material-icons text-base">{{ t.icon }}</span>
        {{ t.label }}
        <span
          v-if="t.id === 'pending' && pending.length > 0"
          class="bg-yellow-500 text-black text-xs font-bold px-1.5 py-0.5 rounded-full leading-none"
        >{{ pending.length }}</span>
      </button>
    </div>

    <!-- Loading / Error -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
    </div>
    <div v-else-if="error" class="bg-red-500/10 border border-red-500/30 rounded-xl p-4 text-red-300 text-sm mb-4">
      {{ error }}
    </div>

    <!-- Pending list -->
    <div v-else-if="activeTab === 'pending'">
      <div v-if="!authStore.isAdmin" class="text-center py-16 text-gray-500">
        <span class="material-icons text-4xl block mb-2">lock</span>
        Bekleyen transferleri görmek için Admin yetkisi gereklidir.
      </div>
      <div v-else-if="pending.length === 0" class="text-center py-16 text-gray-500">
        <span class="material-icons text-5xl block mb-3">check_circle_outline</span>
        Bekleyen transfer yok.
      </div>
      <div v-else class="space-y-3">
        <div
          v-for="t in pending"
          :key="t.id"
          class="bg-gray-900 border border-gray-800 rounded-xl p-4"
        >
          <div class="flex items-start justify-between gap-4">
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-2 flex-wrap">
                <span class="font-semibold text-white">{{ t.sourceOfficeName }}</span>
                <span class="material-icons text-gray-500 text-base">arrow_forward</span>
                <span class="font-semibold text-white">{{ t.targetOfficeName }}</span>
                <span :class="['text-xs px-2 py-0.5 rounded border', statusBadge(t.status)]">{{ statusLabel(t.status) }}</span>
              </div>
              <div class="flex items-center gap-4 text-sm text-gray-300 flex-wrap">
                <span class="font-mono font-bold text-yellow-400">{{ fmt(t.amount) }} {{ t.currencyCode }}</span>
                <span class="text-gray-500 text-xs">{{ t.sourceVaultName }} → {{ t.targetVaultName }}</span>
              </div>
              <div class="flex items-center gap-4 mt-2 text-xs text-gray-500 flex-wrap">
                <span>Talep: <strong class="text-gray-400">{{ t.requestedByName }}</strong></span>
                <span>{{ fmtDate(t.createdDate) }}</span>
                <span v-if="t.notes" class="italic text-gray-600">"{{ t.notes }}"</span>
              </div>
            </div>
            <div class="flex items-center gap-2 shrink-0">
              <button
                @click="approve(t.id)"
                class="flex items-center gap-1 px-3 py-1.5 bg-green-600 hover:bg-green-500 rounded-lg text-xs font-medium transition-colors"
              >
                <span class="material-icons text-sm">check</span>
                Onayla
              </button>
              <button
                @click="openReject(t.id)"
                class="flex items-center gap-1 px-3 py-1.5 bg-red-600/80 hover:bg-red-500 rounded-lg text-xs font-medium transition-colors"
              >
                <span class="material-icons text-sm">close</span>
                Reddet
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- All transfers list -->
    <div v-else>
      <div v-if="allTx.length === 0" class="text-center py-16 text-gray-500">
        <span class="material-icons text-5xl block mb-3">history</span>
        Transfer geçmişi bulunamadı.
      </div>
      <div v-else class="space-y-2">
        <div
          v-for="t in allTx"
          :key="t.id"
          class="bg-gray-900 border border-gray-800 rounded-xl p-3 flex items-center gap-3 flex-wrap"
        >
          <span :class="['text-xs px-2 py-0.5 rounded border shrink-0', statusBadge(t.status)]">{{ statusLabel(t.status) }}</span>
          <span class="text-sm text-gray-300 min-w-0">
            <strong>{{ t.sourceOfficeName }}</strong>
            <span class="text-gray-500 mx-1">→</span>
            <strong>{{ t.targetOfficeName }}</strong>
          </span>
          <span class="font-mono font-bold text-yellow-400 ml-auto shrink-0">{{ fmt(t.amount) }} {{ t.currencyCode }}</span>
          <span class="text-xs text-gray-500 shrink-0">{{ fmtDate(t.createdDate) }}</span>
          <span v-if="t.rejectionReason" class="text-xs text-red-400 truncate max-w-xs">Red: {{ t.rejectionReason }}</span>
        </div>
      </div>
    </div>

    <!-- ── Reject Modal ──────────────────────────────── -->
    <Teleport to="body">
      <div v-if="rejectModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70">
        <div class="bg-gray-900 border border-gray-700 rounded-2xl w-full max-w-md shadow-2xl p-6">
          <h3 class="text-lg font-semibold mb-4 text-red-400">Transferi Reddet</h3>
          <label class="block text-xs text-gray-400 mb-1">Red Gerekçesi (opsiyonel)</label>
          <input
            v-model="rejectReason"
            type="text"
            placeholder="Bakiye yetersiz, yanlış hesap vb."
            class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:outline-none focus:border-red-500 mb-5"
          />
          <div class="flex justify-end gap-3">
            <button @click="rejectModal = false" class="px-4 py-2 text-sm text-gray-400 hover:text-white transition-colors">İptal</button>
            <button
              @click="confirmReject"
              :disabled="rejectSaving"
              class="px-5 py-2 bg-red-600 hover:bg-red-500 disabled:opacity-50 rounded-lg text-sm font-medium transition-colors"
            >
              {{ rejectSaving ? 'Reddetiliyor…' : 'Reddet' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ── Create Transfer Modal ──────────────────────── -->
    <Teleport to="body">
      <div v-if="createModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
        <div class="bg-gray-900 border border-gray-700 rounded-2xl w-full max-w-md shadow-2xl">
          <div class="flex items-center justify-between p-5 border-b border-gray-800">
            <h2 class="text-lg font-semibold">Transfer Talebi Oluştur</h2>
            <button @click="createModal = false"><span class="material-icons text-gray-400">close</span></button>
          </div>
          <div class="p-5 space-y-4">
            <!-- Kaynak kasa -->
            <div>
              <label class="block text-xs text-gray-400 mb-1">Kaynak Kasa *</label>
              <select
                v-model="createForm.sourceVaultId"
                class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-blue-500"
              >
                <option value="">Seçin…</option>
                <option v-for="v in vaults" :key="v.id" :value="v.id">{{ v.name }}</option>
              </select>
            </div>
            <!-- Hedef kasa -->
            <div>
              <label class="block text-xs text-gray-400 mb-1">Hedef Kasa *</label>
              <select
                v-model="createForm.targetVaultId"
                class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-blue-500"
              >
                <option value="">Seçin…</option>
                <option v-for="v in vaults" :key="v.id" :value="v.id">{{ v.name }}</option>
              </select>
            </div>
            <!-- Para birimi + miktar -->
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs text-gray-400 mb-1">Para Birimi *</label>
                <select
                  v-model="createForm.currencyId"
                  class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-blue-500"
                >
                  <option value="">Seçin…</option>
                  <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.currencyCode }}</option>
                </select>
              </div>
              <div>
                <label class="block text-xs text-gray-400 mb-1">Miktar *</label>
                <input
                  v-model.number="createForm.amount"
                  type="number"
                  min="0.01"
                  step="0.01"
                  placeholder="0.00"
                  class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:outline-none focus:border-blue-500"
                />
              </div>
            </div>
            <!-- Not -->
            <div>
              <label class="block text-xs text-gray-400 mb-1">Not</label>
              <input
                v-model="createForm.notes"
                type="text"
                placeholder="İsteğe bağlı açıklama"
                class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:outline-none focus:border-blue-500"
              />
            </div>
            <!-- Error -->
            <div v-if="createError" class="bg-red-500/10 border border-red-500/30 rounded-lg p-3 text-sm text-red-300">
              {{ createError }}
            </div>
          </div>
          <div class="flex justify-end gap-3 p-5 border-t border-gray-800">
            <button @click="createModal = false" class="px-4 py-2 text-sm text-gray-400 hover:text-white transition-colors">İptal</button>
            <button
              @click="submitCreate"
              :disabled="createSaving"
              class="flex items-center gap-2 px-5 py-2 bg-blue-600 hover:bg-blue-500 disabled:opacity-50 rounded-lg text-sm font-medium transition-colors"
            >
              <span v-if="createSaving" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
              {{ createSaving ? 'Gönderiliyor…' : 'Gönder' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
