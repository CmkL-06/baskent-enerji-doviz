<script setup lang="ts">
  import { ref, computed, onMounted, watch } from 'vue'
  import { useAuthStore } from '@/stores/auth'
  import apiService from '@/services/apiservice'

  const authStore = useAuthStore()

  // ── State ────────────────────────────────────────────────
  const isLoading = ref(false)
  const reportData = ref<any>(null)
  const offices = ref<any[]>([])
  const selectedOfficeId = ref<any>('')
  const reportMode = ref<'daily' | 'weekly' | 'monthly' | 'custom'>('daily')
  const selectedDate = ref(new Date().toISOString().slice(0, 10))
  const weekStart = ref(getMondayOfCurrentWeek())
  const selectedYear = ref(new Date().getFullYear())
  const selectedMonth = ref(new Date().getMonth() + 1)
  const customStart = ref(new Date().toISOString().slice(0, 10))
  const customEnd = ref(new Date().toISOString().slice(0, 10))
  const error = ref<string | null>(null)

  function getMondayOfCurrentWeek() {
      const d = new Date()
      const day = d.getDay()
      const diff = d.getDate() - day + (day === 0 ? -6 : 1)
      d.setDate(diff)
      return d.toISOString().slice(0, 10)
  }

  // ── Format helpers ────────────────────────────────────────
  const fmt = (n: number, dec = 2) =>
      new Intl.NumberFormat('tr-TR', { minimumFractionDigits: dec, maximumFractionDigits: dec }).format(n ?? 0)

  const fmtDate = (iso: string) =>
      iso ? new Date(iso).toLocaleDateString('tr-TR') : '-'

  const months = [
      'Ocak','Şubat','Mart','Nisan','Mayıs','Haziran',
      'Temmuz','Ağustos','Eylül','Ekim','Kasım','Aralık'
    ]

  // ── KPI cards ─────────────────────────────────────────────
  const kpis = computed(() => {
      const s = reportData.value?.summary
      if (!s) return []
          return [
            { icon: 'trending_up',           label: 'Toplam Kar',       value: fmt(s.totalProfit ?? 0),           unit: '₺', color: '#10b981', bg: 'rgba(16,185,129,0.12)' },
            { icon: 'currency_exchange',     label: 'İşlem Sayısı',     value: fmt(s.totalTransactions ?? 0, 0),  unit: 'adet', color: '#6366f1', bg: 'rgba(99,102,241,0.12)' },
            { icon: 'arrow_downward',        label: 'Toplam Alış',      value: fmt(s.totalBuy ?? 0),              unit: '₺', color: '#3b82f6', bg: 'rgba(59,130,246,0.12)' },
            { icon: 'arrow_upward',          label: 'Toplam Satış',     value: fmt(s.totalSell ?? 0),             unit: '₺', color: '#f59e0b', bg: 'rgba(245,158,11,0.12)' },
            { icon: 'account_balance_wallet',label: 'Net Ciro',         value: fmt(s.totalVolume ?? 0),           unit: '₺', color: '#8b5cf6', bg: 'rgba(139,92,246,0.12)' },
            { icon: 'storefront',            label: 'Aktif Kasa',       value: fmt(s.vaultCount ?? 0, 0),         unit: 'kasa', color: '#ec4899', bg: 'rgba(236,72,153,0.12)' },
              ]
  })

  // ── Computed helpers ──────────────────────────────────────
  const currencyRows = computed(() => reportData.value?.currencyBreakdown ?? [])
  const vaultRows = computed(() => reportData.value?.vaultBreakdown ?? [])
  const transactions = computed(() => reportData.value?.transactions ?? [])

  const reportTitle = computed(() => {
      if (reportMode.value === 'daily')   return `Günlük Z-Raporu — ${fmtDate(selectedDate.value)}`
      if (reportMode.value === 'weekly')  return `Haftalık Z-Raporu — ${fmtDate(weekStart.value)}`
      if (reportMode.value === 'monthly') return `Aylık Z-Raporu — ${months[selectedMonth.value - 1]} ${selectedYear.value}`
      return `Özel Z-Raporu — ${fmtDate(customStart.value)} / ${fmtDate(customEnd.value)}`
  })

  // ── Fetch ─────────────────────────────────────────────────
  async function loadOffices() {
      try {
            const res = await apiService.getVaults()
            // Gather unique offices from vaults
        const map: Record<number, string> = {}
              ;(res ?? []).forEach((v: any) => { if (v.officeId) map[v.officeId] = v.officeName ?? `Ofis ${v.officeId}` })
            offices.value = Object.entries(map).map(([id, name]) => ({ id: Number(id), name }))
      } catch { offices.value = [] }
  }

  async function fetchReport() {
      error.value = null
      isLoading.value = true
      reportData.value = null
      try {
            const oid = selectedOfficeId.value || undefined
            let data: any
            if (reportMode.value === 'daily') {
                    data = await apiService.getZReportDaily(oid, selectedDate.value)
            } else if (reportMode.value === 'weekly') {
                    data = await apiService.getZReportWeekly(oid, weekStart.value)
            } else if (reportMode.value === 'monthly') {
                    data = await apiService.getZReportMonthly(oid, selectedYear.value, selectedMonth.value)
            } else {
                    data = await apiService.getZReportCustom({ officeId: oid, startDate: customStart.value, endDate: customEnd.value })
            }
            reportData.value = data
      } catch (e: any) {
            error.value = e?.response?.data?.message ?? e?.message ?? 'Rapor yüklenemedi'
      } finally {
            isLoading.value = false
      }
  }

  function printReport() { window.print() }

  onMounted(async () => {
      await loadOffices()
      await fetchReport()
  })
    </script>

    <template>
        <div class="zr-wrap">

              <!-- ── Filtre Paneli ── -->
              <div class="zr-filters">
                      <div class="filter-row">

                                <!-- Mod seçici -->
                                <div class="filter-group">
                                            <label>Rapor Türü</label>
                                                      <div class="mode-tabs">
                                                                    <button :class="['mode-btn', { active: reportMode === 'daily' }]"   @click="reportMode = 'daily'">Günlük</button>
                                                                                <button :class="['mode-btn', { active: reportMode === 'weekly' }]"  @click="reportMode = 'weekly'">Haftalık</button>
                                                                                            <button :class="['mode-btn', { active: reportMode === 'monthly' }]" @click="reportMode = 'monthly'">Aylık</button>
                                                                                                        <button :class="['mode-btn', { active: reportMode === 'custom' }]"  @click="reportMode = 'custom'">Özel</button>
                                                                                                                  </div>
                                                                                                                          </div>
                                                                                                                          
                                <!-- Tarih girdileri -->
                                <div class="filter-group" v-if="reportMode === 'daily'">
                                            <label>Tarih</label>
                                                      <input type="date" v-model="selectedDate" class="filter-input" />
                                          </div>

                                <div class="filter-group" v-if="reportMode === 'weekly'">
                                            <label>Hafta Başlangıcı (Pazartesi)</label>
                                                      <input type="date" v-model="weekStart" class="filter-input" />
                                          </div>

                                <template v-if="reportMode === 'monthly'">
                                            <div class="filter-group">
                                                          <label>Yıl</label>
                                                                      <input type="number" v-model.number="selectedYear" min="2020" max="2099" class="filter-input filter-input--sm" />
                                                        </div>
                                                                  <div class="filter-group">
                                                                                <label>Ay</label>
                                                                                            <select v-model.number="selectedMonth" class="filter-input">
                                                                                                            <option v-for="(m, i) in months" :key="i" :value="i + 1">{{ m }}</option>
                                                                                                                        </select>
                                                                                                                                  </div>
                                                                                                                                          </template>
                                                                                                                                          
                                <template v-if="reportMode === 'custom'">
                                            <div class="filter-group">
                                                          <label>Başlangıç</label>
                                                                      <input type="date" v-model="customStart" class="filter-input" />
                                                        </div>
                                                                  <div class="filter-group">
                                                                                <label>Bitiş</label>
                                                                                            <input type="date" v-model="customEnd" class="filter-input" />
                                                                              </div>
                                                                                      </template>

                                <!-- Ofis seçici -->
                                <div class="filter-group" v-if="offices.length > 0">
                                            <label>Şube</label>
                                                      <select v-model="selectedOfficeId" class="filter-input">
                                                                    <option value="">Tüm Şubeler</option>
                                                                                <option v-for="o in offices" :key="o.id" :value="o.id">{{ o.name }}</option>
                                                                                          </select>
                                                                                                  </div>

                                <!-- Getir butonu -->
                                <div class="filter-group filter-group--action">
                                            <label>&nbsp;</label>
                                                      <button class="fetch-btn" @click="fetchReport" :disabled="isLoading">
                                                                    <span class="material-symbols-outlined">{{ isLoading ? 'hourglass_top' : 'search' }}</span>
                                                                                {{ isLoading ? 'Yükleniyor...' : 'Raporu Getir' }}
                                                                  </button>
                                                                          </div>

                              </div>
                                  </div>

              <!-- ── Hata ── -->
              <div class="zr-error" v-if="error">
                      <span class="material-symbols-outlined">error</span> {{ error }}
                    </div>

              <!-- ── Loading ── -->
              <div class="zr-loading" v-if="isLoading">
                      <div class="spinner"></div>
                            <span>Rapor hazırlanıyor...</span>
                                </div>

              <!-- ── Rapor İçeriği ── -->
              <template v-if="!isLoading && reportData">

                      <!-- Başlık -->
                      <div class="zr-header print-only-show">
                                <h2 class="zr-title">{{ reportTitle }}</h2>
                                        <p class="zr-subtitle">Oluşturulma: {{ new Date().toLocaleString('tr-TR') }}</p>
                                              </div>

                      <!-- KPI Kartları -->
                      <div class="kpi-grid">
                                <div v-for="k in kpis" :key="k.label" class="kpi-card" :style="{ '--kc': k.color, '--kb': k.bg }">
                                            <div class="kpi-icon"><span class="material-symbols-outlined">{{ k.icon }}</span></div>
                                                      <div class="kpi-body">
                                                                    <p class="kpi-label">{{ k.label }}</p>
                                                                                <p class="kpi-value">{{ k.value }} <span class="kpi-unit">{{ k.unit }}</span></p>
                                                                                          </div>
                                                                                                  </div>
                                                                                                        </div>
                                                                                                        
                      <!-- İki kolon: Döviz & Kasa -->
                      <div class="zr-columns">

                                <!-- Döviz Bazlı Döküm -->
                                <div class="zr-panel" v-if="currencyRows.length">
                                            <div class="panel-header">
                                                          <span class="material-symbols-outlined">currency_exchange</span>
                                                                      <h3>Döviz Bazlı Özet</h3>
                                                                                </div>
                                                                                          <div class="table-wrap">
                                                                                                        <table class="zr-table">
                                                                                                                        <thead>
                                                                                                                                          <tr>
                                                                                                                                                              <th>Döviz</th>
                                                                                                                                                                                <th>Alış Miktarı</th>
                                                                                                                                                                                                  <th>Satış Miktarı</th>
                                                                                                                                                                                                                    <th>Alış Kuru (Ort.)</th>
                                                                                                                                                                                                                                      <th>Satış Kuru (Ort.)</th>
                                                                                                                                                                                                                                                        <th>Kar (₺)</th>
                                                                                                                                                                                                                                                                        </tr>
                                                                                                                                                                                                                                                                                      </thead>
                                                                                                                                                                                                                                                                                                    <tbody>
                                                                                                                                                                                                                                                                                                                      <tr v-for="row in currencyRows" :key="row.currencyCode">
                                                                                                                                                                                                                                                                                                                                          <td><span class="badge">{{ row.currencyCode }}</span></td>
                                                                                                                                                                                                                                                                                                                                                            <td>{{ fmt(row.buyAmount) }}</td>
                                                                                                                                                                                                                                                                                                                                                                              <td>{{ fmt(row.sellAmount) }}</td>
                                                                                                                                                                                                                                                                                                                                                                                                <td>{{ fmt(row.avgBuyRate, 4) }}</td>
                                                                                                                                                                                                                                                                                                                                                                                                                  <td>{{ fmt(row.avgSellRate, 4) }}</td>
                                                                                                                                                                                                                                                                                                                                                                                                                                    <td :class="row.profit >= 0 ? 'pos' : 'neg'">
                                                                                                                                                                                                                                                                                                                                                                                                                                                          {{ row.profit >= 0 ? '+' : '' }}{{ fmt(row.profit) }}
                                                                                                                                                                                                                                                                                                                                                                                                                                                        </td>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                        </tr>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      </tbody>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  </table>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            </div>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    </div>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    
                                <!-- Kasa Bazlı Döküm -->
                                <div class="zr-panel" v-if="vaultRows.length">
                                            <div class="panel-header">
                                                          <span class="material-symbols-outlined">account_balance_wallet</span>
                                                                      <h3>Kasa Bazlı Özet</h3>
                                                                                </div>
                                                                                          <div class="table-wrap">
                                                                                                        <table class="zr-table">
                                                                                                                        <thead>
                                                                                                                                          <tr>
                                                                                                                                                              <th>Kasa</th>
                                                                                                                                                                                <th>İşlem Sayısı</th>
                                                                                                                                                                                                  <th>Ciro (₺)</th>
                                                                                                                                                                                                                    <th>Kar (₺)</th>
                                                                                                                                                                                                                                    </tr>
                                                                                                                                                                                                                                                  </thead>
                                                                                                                                                                                                                                                                <tbody>
                                                                                                                                                                                                                                                                                  <tr v-for="row in vaultRows" :key="row.vaultId">
                                                                                                                                                                                                                                                                                                      <td>{{ row.vaultName }}</td>
                                                                                                                                                                                                                                                                                                                        <td>{{ fmt(row.transactionCount, 0) }}</td>
                                                                                                                                                                                                                                                                                                                                          <td>{{ fmt(row.volume) }}</td>
                                                                                                                                                                                                                                                                                                                                                            <td :class="row.profit >= 0 ? 'pos' : 'neg'">
                                                                                                                                                                                                                                                                                                                                                                                  {{ row.profit >= 0 ? '+' : '' }}{{ fmt(row.profit) }}
                                                                                                                                                                                                                                                                                                                                                                                </td>
                                                                                                                                                                                                                                                                                                                                                                                                </tr>
                                                                                                                                                                                                                                                                                                                                                                                                              </tbody>
                                                                                                                                                                                                                                                                                                                                                                                                                          </table>
                                                                                                                                                                                                                                                                                                                                                                                                                                    </div>
                                                                                                                                                                                                                                                                                                                                                                                                                                            </div>
                                                                                                                                                                                                                                                                                                                                                                                                                                            
                              </div>

                      <!-- İşlem Listesi -->
                      <div class="zr-panel" v-if="transactions.length">
                                <div class="panel-header">
                                            <span class="material-symbols-outlined">receipt_long</span>
                                                      <h3>İşlem Detayları</h3>
                                                                <span class="badge badge--count">{{ transactions.length }} işlem</span>
                                                                        </div>
                                                                                <div class="table-wrap">
                                                                                            <table class="zr-table">
                                                                                                          <thead>
                                                                                                                          <tr>
                                                                                                                                            <th>İşlem No</th>
                                                                                                                                                            <th>Kasa</th>
                                                                                                                                                                            <th>Tür</th>
                                                                                                                                                                                            <th>Tarih</th>
                                                                                                                                                                                                            <th>Döviz</th>
                                                                                                                                                                                                                            <th>Miktar</th>
                                                                                                                                                                                                                                            <th>Kur</th>
                                                                                                                                                                                                                                                            <th>TRY Karşılığı</th>
                                                                                                                                                                                                                                                                            <th>Kar (₺)</th>
                                                                                                                                                                                                                                                                                            <th>Durum</th>
                                                                                                                                                                                                                                                                                                          </tr>
                                                                                                                                                                                                                                                                                                                      </thead>
                                                                                                                                                                                                                                                                                                                                  <tbody>
                                                                                                                                                                                                                                                                                                                                                  <tr v-for="tx in transactions" :key="tx.id ?? tx.transactionNumber">
                                                                                                                                                                                                                                                                                                                                                                    <td class="mono">{{ tx.transactionNumber }}</td>
                                                                                                                                                                                                                                                                                                                                                                                    <td>{{ tx.vaultName ?? '-' }}</td>
                                                                                                                                                                                                                                                                                                                                                                                                    <td>{{ tx.typeName ?? tx.type }}</td>
                                                                                                                                                                                                                                                                                                                                                                                                                    <td>{{ tx.transactionDate ? new Date(tx.transactionDate).toLocaleString('tr-TR') : '-' }}</td>
                                                                                                                                                                                                                                                                                                                                                                                                                                    <td><span class="badge">{{ tx.currencyCode ?? '-' }}</span></td>
                                                                                                                                                                                                                                                                                                                                                                                                                                                    <td>{{ fmt(tx.amount) }}</td>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                    <td>{{ fmt(tx.rate ?? tx.exchangeRate, 4) }}</td>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    <td>{{ fmt(tx.tryAmount ?? tx.totalTry) }}</td>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    <td :class="(tx.profit ?? 0) >= 0 ? 'pos' : 'neg'">
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        {{ (tx.profit ?? 0) >= 0 ? '+' : '' }}{{ fmt(tx.profit ?? 0) }}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      </td>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      <td>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          <span class="status-chip" :class="tx.status === 2 || tx.statusName === 'Tamamlandı' ? 'done' : 'pend'">
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                {{ tx.statusName ?? (tx.status === 2 ? 'Tamamlandı' : 'Bekliyor') }}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              </span>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              </td>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            </tr>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        </tbody>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  </table>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          </div>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                </div>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                
                      <!-- Boş durum -->
                      <div class="zr-empty" v-if="!currencyRows.length && !vaultRows.length && !transactions.length">
                                <span class="material-symbols-outlined">inbox</span>
                                        <p>Seçilen dönem için işlem bulunamadı.</p>
                                              </div>

                    </template>

              <!-- ── Başlangıç durumu ── -->
              <div class="zr-empty" v-if="!isLoading && !reportData && !error">
                      <span class="material-symbols-outlined">assessment</span>
                            <p>Filtre seçip <strong>Raporu Getir</strong> butonuna tıklayın.</p>
                                </div>

            </div>
            </template>

            <style scoped>
              /* ── Layout ── */
              .zr-wrap { padding: 24px; display: flex; flex-direction: column; gap: 20px; }

              /* ── Filtreler ── */
              .zr-filters { background: #fff; border: 1px solid #e5e7eb; border-radius: 12px; padding: 18px 20px; }
              .filter-row { display: flex; flex-wrap: wrap; gap: 16px; align-items: flex-end; }
              .filter-group { display: flex; flex-direction: column; gap: 6px; }
              .filter-group label { font-size: 12px; font-weight: 600; color: #6b7280; text-transform: uppercase; letter-spacing: .04em; }
              .filter-group--action { margin-left: auto; }
              .filter-input { height: 38px; padding: 0 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; color: #111; background: #f9fafb; outline: none; }
              .filter-input:focus { border-color: #6366f1; box-shadow: 0 0 0 3px rgba(99,102,241,.15); }
              .filter-input--sm { width: 90px; }
              .mode-tabs { display: flex; border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden; }
              .mode-btn { padding: 8px 14px; font-size: 13px; font-weight: 500; background: #f9fafb; border: none; cursor: pointer; color: #6b7280; transition: all .15s; }
              .mode-btn.active { background: #6366f1; color: #fff; }
              .fetch-btn { display: flex; align-items: center; gap: 6px; height: 38px; padding: 0 18px; background: #6366f1; color: #fff; border: none; border-radius: 8px; font-size: 14px; font-weight: 600; cursor: pointer; transition: background .15s; }
              .fetch-btn:hover:not(:disabled) { background: #4f46e5; }
              .fetch-btn:disabled { opacity: .6; cursor: not-allowed; }
              .fetch-btn .material-symbols-outlined { font-size: 18px; }

              /* ── Hata & Loading ── */
              .zr-error { background: #fef2f2; border: 1px solid #fecaca; border-radius: 10px; padding: 14px 18px; color: #dc2626; display: flex; align-items: center; gap: 8px; font-size: 14px; }
              .zr-loading { display: flex; align-items: center; justify-content: center; gap: 12px; padding: 60px; color: #6b7280; font-size: 15px; }
              .spinner { width: 28px; height: 28px; border: 3px solid #e5e7eb; border-top-color: #6366f1; border-radius: 50%; animation: spin .7s linear infinite; }
              @keyframes spin { to { transform: rotate(360deg); } }

              /* ── KPI Grid ── */
              .kpi-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 14px; }
              .kpi-card { background: var(--kb); border: 1px solid rgba(0,0,0,.06); border-radius: 12px; padding: 16px; display: flex; align-items: center; gap: 14px; }
              .kpi-icon { width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; border-radius: 10px; background: var(--kb); }
              .kpi-icon .material-symbols-outlined { font-size: 22px; color: var(--kc); }
              .kpi-label { font-size: 12px; color: #6b7280; margin: 0 0 4px; }
              .kpi-value { font-size: 18px; font-weight: 700; color: #111; margin: 0; }
              .kpi-unit { font-size: 12px; font-weight: 400; color: #9ca3af; }

              /* ── Paneller ── */
              .zr-panel { background: #fff; border: 1px solid #e5e7eb; border-radius: 12px; overflow: hidden; }
              .panel-header { display: flex; align-items: center; gap: 8px; padding: 16px 20px; border-bottom: 1px solid #f3f4f6; }
              .panel-header h3 { margin: 0; font-size: 15px; font-weight: 600; color: #111; flex: 1; }
              .panel-header .material-symbols-outlined { font-size: 20px; color: #6366f1; }

              /* ── İki Kolon ── */
              .zr-columns { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
              @media (max-width: 900px) { .zr-columns { grid-template-columns: 1fr; } }

              /* ── Tablo ── */
              .table-wrap { overflow-x: auto; }
              .zr-table { width: 100%; border-collapse: collapse; font-size: 13px; }
              .zr-table th { background: #f9fafb; padding: 10px 14px; text-align: left; font-weight: 600; color: #374151; white-space: nowrap; border-bottom: 1px solid #e5e7eb; }
              .zr-table td { padding: 10px 14px; border-bottom: 1px solid #f3f4f6; color: #1f2937; }
              .zr-table tr:last-child td { border-bottom: none; }
              .zr-table tr:hover td { background: #f9fafb; }
              .pos { color: #10b981; font-weight: 600; }
              .neg { color: #ef4444; font-weight: 600; }
              .mono { font-family: monospace; font-size: 12px; }

              /* ── Badge ── */
              .badge { display: inline-block; padding: 2px 8px; border-radius: 6px; background: #ede9fe; color: #6366f1; font-size: 12px; font-weight: 600; }
              .badge--count { background: #f3f4f6; color: #374151; margin-left: auto; }

              /* ── Status ── */
              .status-chip { display: inline-block; padding: 2px 8px; border-radius: 6px; font-size: 11px; font-weight: 600; }
              .status-chip.done { background: #d1fae5; color: #065f46; }
              .status-chip.pend { background: #fef3c7; color: #92400e; }

              /* ── Başlık (print) ── */
              .zr-header { display: none; }
              .zr-title { margin: 0 0 4px; font-size: 20px; font-weight: 700; }
              .zr-subtitle { margin: 0; color: #6b7280; font-size: 13px; }

              /* ── Boş durum ── */
              .zr-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 64px; gap: 12px; color: #9ca3af; }
              .zr-empty .material-symbols-outlined { font-size: 48px; }
              .zr-empty p { font-size: 15px; margin: 0; }

              /* ── Print ── */
              @media print {
                  .zr-filters, .fetch-btn { display: none !important; }
                  .zr-header { display: block !important; }
                  .zr-wrap { padding: 0; }
                  .zr-panel { break-inside: avoid; }
              }
              </style>
