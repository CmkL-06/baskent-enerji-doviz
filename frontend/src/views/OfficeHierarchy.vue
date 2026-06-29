<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import apiService from '@/services/apiservice'
import type { OfficeNode } from '@/types/api'

const router    = useRouter()
const authStore = useAuthStore()

// ── State ──────────────────────────────────────────
const hierarchy = ref<OfficeNode[]>([])
const loading   = ref(true)
const error     = ref('')

// Modal
const showModal   = ref(false)
const saving      = ref(false)
const saveError   = ref('')
const editTarget  = ref<OfficeNode | null>(null)

const form = ref({
  id:                     '',
  officeName:             '',
  officeDescription:      '',
  address:                '',
  phone:                  '',
  officeType:             'Sube' as 'Merkez' | 'Sube' | 'Bayi',
  parentOfficeId:         null as string | null,
  dailyTransactionLimit:  null as number | null,
  monthlyTransactionLimit:null as number | null,
  commissionRate:         null as number | null,
})

// ── Load ───────────────────────────────────────────
onMounted(async () => {
  if (!authStore.isOwner) { router.push('/ihtiyar/dashboard'); return }
  await load()
})

async function load() {
  loading.value = true
  error.value   = ''
  try {
    const data = await apiService.getOfficeHierarchy()
    hierarchy.value = Array.isArray(data) ? data : []
  } catch (e: any) {
    error.value = e?.response?.data?.error || e.message || 'Hiyerarşi yüklenemedi'
  } finally {
    loading.value = false
  }
}

// ── Modal helpers ──────────────────────────────────
function openCreate(parentId?: string | number, parentType?: string) {
  const parentIdStr = parentId !== undefined ? String(parentId) : undefined
  editTarget.value  = null
  saveError.value   = ''
  form.value = {
    id: '', officeName: '', officeDescription: '', address: '', phone: '',
    officeType: parentType === 'Merkez' ? 'Sube' : parentType === 'Sube' ? 'Bayi' : 'Sube',
    parentOfficeId: parentIdStr ?? null,
    dailyTransactionLimit: null, monthlyTransactionLimit: null, commissionRate: null,
  }
  showModal.value = true
}

function openEdit(office: OfficeNode) {
  editTarget.value  = office
  saveError.value   = ''
  form.value = {
    id:                     String(office.id),
    officeName:             office.officeName,
    officeDescription:      office.officeDescription ?? '',
    address:                office.address ?? '',
    phone:                  office.phone ?? '',
    officeType:             office.officeType as any,
    parentOfficeId:         office.parentOfficeId ?? null,
    dailyTransactionLimit:  office.dailyTransactionLimit ?? null,
    monthlyTransactionLimit:office.monthlyTransactionLimit ?? null,
    commissionRate:         office.commissionRate ?? null,
  }
  showModal.value = true
}

async function save() {
  if (!form.value.officeName.trim()) { saveError.value = 'Ofis adı zorunludur.'; return }
  saving.value    = true
  saveError.value = ''
  try {
    await apiService.saveOfficeHierarchy({ ...form.value })
    showModal.value = false
    await load()
  } catch (e: any) {
    saveError.value = e?.response?.data?.error || e.message || 'Kayıt başarısız'
  } finally {
    saving.value = false
  }
}

// ── Stats ──────────────────────────────────────────
const merkez = computed(() => hierarchy.value.find(o => o.officeType === 'Merkez'))
const totalSube = computed(() => countAll(hierarchy.value, 'Sube'))
const totalBayi = computed(() => countAll(hierarchy.value, 'Bayi'))

function countAll(nodes: OfficeNode[], type: string): number {
  return nodes.reduce((n, o) => {
    const match = o.officeType === type ? 1 : 0
    return n + match + countAll(o.children ?? [], type)
  }, 0)
}

// ── Type badge ─────────────────────────────────────
const typeColor: Record<string, string> = {
  Merkez: 'bg-purple-500/20 text-purple-300 border-purple-500/30',
  Sube:   'bg-blue-500/20  text-blue-300  border-blue-500/30',
  Bayi:   'bg-green-500/20 text-green-300 border-green-500/30',
}
</script>

<template>
  <div class="min-h-screen bg-gray-950 text-white p-6">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-white">Ofis Hiyerarşisi</h1>
        <p class="text-gray-400 text-sm mt-1">Merkez, Şube ve Bayi yapısını yönetin</p>
      </div>
      <button
        @click="openCreate()"
        class="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-lg text-sm font-medium transition-colors"
      >
        <span class="material-icons text-base">add</span>
        Yeni Ofis
      </button>
    </div>

    <!-- Stat cards -->
    <div class="grid grid-cols-3 gap-4 mb-6">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4">
        <p class="text-xs text-gray-400 uppercase tracking-wider mb-1">Merkez</p>
        <p class="text-2xl font-bold text-purple-400">{{ merkez ? 1 : 0 }}</p>
        <p class="text-xs text-gray-500 mt-1">{{ merkez?.officeName ?? '—' }}</p>
      </div>
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4">
        <p class="text-xs text-gray-400 uppercase tracking-wider mb-1">Şube</p>
        <p class="text-2xl font-bold text-blue-400">{{ totalSube }}</p>
        <p class="text-xs text-gray-500 mt-1">aktif şube</p>
      </div>
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4">
        <p class="text-xs text-gray-400 uppercase tracking-wider mb-1">Bayi</p>
        <p class="text-2xl font-bold text-green-400">{{ totalBayi }}</p>
        <p class="text-xs text-gray-500 mt-1">bağlı bayi</p>
      </div>
    </div>

    <!-- Loading / Error -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
    </div>
    <div v-else-if="error" class="bg-red-500/10 border border-red-500/30 rounded-xl p-4 text-red-300 text-sm">
      {{ error }}
    </div>

    <!-- Tree -->
    <div v-else class="space-y-4">
      <template v-for="office in hierarchy" :key="office.id">
        <!-- Root office card -->
        <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
          <!-- Office row -->
          <div class="flex items-center gap-4 p-4">
            <span class="material-icons text-purple-400">corporate_fare</span>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 flex-wrap">
                <span class="font-semibold text-white truncate">{{ office.officeName }}</span>
                <span :class="['text-xs px-2 py-0.5 rounded border font-medium', typeColor[office.officeType] ?? 'bg-gray-700 text-gray-300 border-gray-600']">
                  {{ office.officeType }}
                </span>
                <span v-if="!office.isActive" class="text-xs px-2 py-0.5 rounded border bg-red-500/10 text-red-400 border-red-500/20">Pasif</span>
              </div>
              <p v-if="office.address" class="text-xs text-gray-500 truncate mt-0.5">{{ office.address }}</p>
            </div>
            <div class="flex items-center gap-4 text-xs text-gray-400 shrink-0">
              <span title="Kasa"><span class="material-icons text-sm align-middle mr-0.5">account_balance_wallet</span>{{ office.vaultCount }}</span>
              <span title="Kullanıcı"><span class="material-icons text-sm align-middle mr-0.5">group</span>{{ office.userCount }}</span>
            </div>
            <div class="flex items-center gap-2 shrink-0">
              <button
                @click="openCreate(office.id, office.officeType)"
                class="p-1.5 hover:bg-gray-700 rounded-lg transition-colors text-gray-400 hover:text-blue-400"
                title="Alt birim ekle"
              >
                <span class="material-icons text-base">add_circle_outline</span>
              </button>
              <button
                @click="openEdit(office)"
                class="p-1.5 hover:bg-gray-700 rounded-lg transition-colors text-gray-400 hover:text-yellow-400"
                title="Düzenle"
              >
                <span class="material-icons text-base">edit</span>
              </button>
            </div>
          </div>

          <!-- Children -->
          <div v-if="office.children && office.children.length > 0" class="border-t border-gray-800">
            <div
              v-for="child in office.children"
              :key="child.id"
              class="flex items-center gap-4 px-4 py-3 border-b border-gray-800/50 last:border-0 hover:bg-gray-800/30 transition-colors"
            >
              <span class="material-icons text-gray-600 ml-6">subdirectory_arrow_right</span>
              <span :class="['material-icons text-sm', child.officeType === 'Sube' ? 'text-blue-400' : 'text-green-400']">
                {{ child.officeType === 'Sube' ? 'storefront' : 'local_convenience_store' }}
              </span>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 flex-wrap">
                  <span class="text-sm font-medium text-gray-200 truncate">{{ child.officeName }}</span>
                  <span :class="['text-xs px-2 py-0.5 rounded border font-medium', typeColor[child.officeType] ?? 'bg-gray-700 text-gray-300 border-gray-600']">
                    {{ child.officeType }}
                  </span>
                  <span v-if="!child.isActive" class="text-xs px-2 py-0.5 rounded border bg-red-500/10 text-red-400 border-red-500/20">Pasif</span>
                </div>
                <div class="flex items-center gap-3 mt-0.5 text-xs text-gray-500">
                  <span v-if="child.address">{{ child.address }}</span>
                  <span v-if="child.commissionRate != null">Komisyon: %{{ child.commissionRate }}</span>
                  <span v-if="child.dailyTransactionLimit != null">Günlük: {{ child.dailyTransactionLimit.toLocaleString('tr') }}</span>
                </div>
              </div>
              <div class="flex items-center gap-3 text-xs text-gray-400 shrink-0">
                <span title="Kasa"><span class="material-icons text-sm align-middle mr-0.5">account_balance_wallet</span>{{ child.vaultCount }}</span>
                <span title="Kullanıcı"><span class="material-icons text-sm align-middle mr-0.5">group</span>{{ child.userCount }}</span>
              </div>
              <div class="flex items-center gap-2 shrink-0">
                <button
                  v-if="child.officeType === 'Sube'"
                  @click="openCreate(child.id, child.officeType)"
                  class="p-1.5 hover:bg-gray-700 rounded-lg transition-colors text-gray-400 hover:text-blue-400"
                  title="Bayi ekle"
                >
                  <span class="material-icons text-base">add_circle_outline</span>
                </button>
                <button
                  @click="openEdit(child)"
                  class="p-1.5 hover:bg-gray-700 rounded-lg transition-colors text-gray-400 hover:text-yellow-400"
                  title="Düzenle"
                >
                  <span class="material-icons text-base">edit</span>
                </button>
              </div>
            </div>
          </div>
          <div v-else class="border-t border-gray-800 px-4 py-3 text-xs text-gray-600 italic ml-6">
            Henüz bağlı şube/bayi yok.
            <button @click="openCreate(office.id, office.officeType)" class="text-blue-500 hover:text-blue-400 ml-1">Ekle</button>
          </div>
        </div>
      </template>

      <div v-if="hierarchy.length === 0" class="text-center py-16 text-gray-500">
        <span class="material-icons text-5xl block mb-3">corporate_fare</span>
        Henüz ofis tanımlanmamış.
        <button @click="openCreate()" class="text-blue-500 hover:text-blue-400 ml-1">İlk ofisinizi oluşturun.</button>
      </div>
    </div>

    <!-- ── Modal ──────────────────────────────────── -->
    <Teleport to="body">
      <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
        <div class="bg-gray-900 border border-gray-700 rounded-2xl w-full max-w-lg shadow-2xl">
          <!-- Modal header -->
          <div class="flex items-center justify-between p-5 border-b border-gray-800">
            <h2 class="text-lg font-semibold text-white">
              {{ editTarget ? 'Ofisi Düzenle' : 'Yeni Ofis Ekle' }}
            </h2>
            <button @click="showModal = false" class="p-1 hover:bg-gray-700 rounded-lg transition-colors">
              <span class="material-icons text-gray-400">close</span>
            </button>
          </div>

          <!-- Modal body -->
          <div class="p-5 space-y-4 max-h-[65vh] overflow-y-auto">
            <!-- Ofis tipi -->
            <div>
              <label class="block text-xs text-gray-400 mb-1">Ofis Tipi</label>
              <div class="flex gap-2">
                <button
                  v-for="t in ['Merkez', 'Sube', 'Bayi']"
                  :key="t"
                  @click="form.officeType = t as any"
                  :class="['flex-1 py-2 text-sm rounded-lg border transition-colors', form.officeType === t
                    ? typeColor[t] + ' font-medium'
                    : 'border-gray-700 text-gray-400 hover:bg-gray-800']"
                >
                  {{ t === 'Sube' ? 'Şube' : t }}
                </button>
              </div>
            </div>

            <!-- Ofis adı -->
            <div>
              <label class="block text-xs text-gray-400 mb-1">Ofis Adı *</label>
              <input
                v-model="form.officeName"
                type="text"
                placeholder="örn: Ankara Merkez"
                class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:outline-none focus:border-blue-500"
              />
            </div>

            <!-- Açıklama -->
            <div>
              <label class="block text-xs text-gray-400 mb-1">Açıklama</label>
              <input
                v-model="form.officeDescription"
                type="text"
                placeholder="Kısa açıklama"
                class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:outline-none focus:border-blue-500"
              />
            </div>

            <!-- Adres / Telefon -->
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs text-gray-400 mb-1">Adres</label>
                <input
                  v-model="form.address"
                  type="text"
                  placeholder="Şehir / İlçe"
                  class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:outline-none focus:border-blue-500"
                />
              </div>
              <div>
                <label class="block text-xs text-gray-400 mb-1">Telefon</label>
                <input
                  v-model="form.phone"
                  type="text"
                  placeholder="+90 xxx xxx xx xx"
                  class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:outline-none focus:border-blue-500"
                />
              </div>
            </div>

            <!-- Bayi limitleri (sadece Bayi tipinde göster) -->
            <template v-if="form.officeType === 'Bayi'">
              <div class="border-t border-gray-800 pt-4">
                <p class="text-xs text-gray-400 mb-3 font-medium uppercase tracking-wider">Bayi Limitleri</p>
                <div class="space-y-3">
                  <div>
                    <label class="block text-xs text-gray-400 mb-1">Günlük İşlem Limiti</label>
                    <input
                      v-model.number="form.dailyTransactionLimit"
                      type="number"
                      placeholder="0 = limitsiz"
                      class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:outline-none focus:border-blue-500"
                    />
                  </div>
                  <div>
                    <label class="block text-xs text-gray-400 mb-1">Aylık İşlem Limiti</label>
                    <input
                      v-model.number="form.monthlyTransactionLimit"
                      type="number"
                      placeholder="0 = limitsiz"
                      class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:outline-none focus:border-blue-500"
                    />
                  </div>
                  <div>
                    <label class="block text-xs text-gray-400 mb-1">Komisyon Oranı (%)</label>
                    <input
                      v-model.number="form.commissionRate"
                      type="number"
                      step="0.01"
                      placeholder="0.00"
                      class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:outline-none focus:border-blue-500"
                    />
                  </div>
                </div>
              </div>
            </template>

            <!-- Error -->
            <div v-if="saveError" class="bg-red-500/10 border border-red-500/30 rounded-lg p-3 text-sm text-red-300">
              {{ saveError }}
            </div>
          </div>

          <!-- Modal footer -->
          <div class="flex items-center justify-end gap-3 p-5 border-t border-gray-800">
            <button
              @click="showModal = false"
              class="px-4 py-2 text-sm text-gray-400 hover:text-white transition-colors"
            >
              İptal
            </button>
            <button
              @click="save"
              :disabled="saving"
              class="flex items-center gap-2 px-5 py-2 bg-blue-600 hover:bg-blue-500 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg text-sm font-medium transition-colors"
            >
              <span v-if="saving" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
              {{ saving ? 'Kaydediliyor…' : 'Kaydet' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
