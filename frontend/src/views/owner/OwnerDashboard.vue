<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import apiService from '@/services/apiservice'

const offices = ref<any[]>([])
const loading = ref(true)
const error = ref('')

const perfData = ref<any[]>([])
const perfPeriod = ref('daily')
const perfLoading = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    offices.value = await apiService.getOfficeSummaries() ?? []
  } catch (e: any) {
    error.value = e?.response?.data?.message || e.message || 'Veri yüklenemedi'
  } finally {
    loading.value = false
  }
  loadPerformance()
})

async function loadPerformance() {
  perfLoading.value = true
  try {
    perfData.value = await apiService.getBranchComparison(perfPeriod.value) ?? []
  } catch { perfData.value = [] }
  finally { perfLoading.value = false }
}

function changePerfPeriod(p: string) {
  perfPeriod.value = p
  loadPerformance()
}

function perfPeriodLabel(p: string) {
  if (p === 'daily') return 'Günlük'
  if (p === 'weekly') return 'Haftalık'
  return 'Aylık'
}

function officeTypeIcon(t: number) {
  if (t === 1) return 'hub'
  if (t === 2) return 'store'
  return 'storefront'
}

const merkezOffices = computed(() => offices.value.filter(o => o.officeType === 1))
const subeOffices = computed(() => offices.value.filter(o => o.officeType !== 1))
const totalVaults = computed(() => offices.value.reduce((a, o) => a + (o.vaultCount ?? 0), 0))
const totalDailyPL = computed(() => offices.value.reduce((a, o) => a + (o.dailyProfitLoss ?? 0), 0))
const totalMonthlyPL = computed(() => offices.value.reduce((a, o) => a + (o.monthlyProfitLoss ?? 0), 0))
const totalAssets = computed(() => offices.value.reduce((a, o) => a + (o.totalValueInBaseCurrency ?? 0), 0))

function fmtMoney(n: number | null | undefined) {
  if (n == null) return '—'
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function plClass(n: number) {
  if (n > 0) return 'text-green'
  if (n < 0) return 'text-red'
  return 'text-gray'
}

function officeTypeLabel(t: number) {
  if (t === 1) return 'Merkez'
  if (t === 2) return 'Şube'
  if (t === 3) return 'Bayi'
  return '—'
}

function officeTypeBadgeClass(t: number) {
  if (t === 1) return 'badge-merkez'
  if (t === 2) return 'badge-sube'
  return 'badge-bayi'
}

const topCurrencies = (obj: Record<string, number> | null | undefined) => {
  if (!obj) return []
  return Object.entries(obj)
    .sort((a, b) => Math.abs(b[1]) - Math.abs(a[1]))
    .slice(0, 5)
}
</script>

<template>
  <div>
    <div v-if="loading" class="od-loading">
      <span class="material-symbols-outlined spin">progress_activity</span> Yükleniyor...
    </div>
    <div v-else-if="error" class="od-error">
      <span class="material-symbols-outlined">error</span> {{ error }}
    </div>
    <template v-else>
      <!-- Stats -->
      <div class="od-stats">
        <div class="od-stat">
          <span class="material-symbols-outlined od-stat-icon blue">store</span>
          <div>
            <div class="od-stat-val">{{ subeOffices.length }}</div>
            <div class="od-stat-lbl">Şube</div>
          </div>
        </div>
        <div class="od-stat">
          <span class="material-symbols-outlined od-stat-icon purple">account_balance_wallet</span>
          <div>
            <div class="od-stat-val">{{ totalVaults }}</div>
            <div class="od-stat-lbl">Toplam Kasa</div>
          </div>
        </div>
        <div class="od-stat">
          <span class="material-symbols-outlined od-stat-icon" :class="totalDailyPL >= 0 ? 'green' : 'red'">trending_up</span>
          <div>
            <div class="od-stat-val" :class="plClass(totalDailyPL)">{{ fmtMoney(totalDailyPL) }} ₺</div>
            <div class="od-stat-lbl">Günlük K/Z</div>
          </div>
        </div>
        <div class="od-stat">
          <span class="material-symbols-outlined od-stat-icon" :class="totalMonthlyPL >= 0 ? 'green' : 'red'">calendar_month</span>
          <div>
            <div class="od-stat-val" :class="plClass(totalMonthlyPL)">{{ fmtMoney(totalMonthlyPL) }} ₺</div>
            <div class="od-stat-lbl">Aylık K/Z</div>
          </div>
        </div>
        <div class="od-stat">
          <span class="material-symbols-outlined od-stat-icon amber">savings</span>
          <div>
            <div class="od-stat-val">{{ fmtMoney(totalAssets) }} ₺</div>
            <div class="od-stat-lbl">Toplam Varlık</div>
          </div>
        </div>
      </div>

      <!-- Office Cards -->
      <h3 class="od-section-title">
        <span class="material-symbols-outlined">hub</span> Merkez & Şubeler
      </h3>
      <div class="od-office-grid">
        <div
          v-for="o in [...merkezOffices, ...subeOffices]"
          :key="o.officeId"
          class="od-office-card"
          :class="{ 'od-merkez': o.officeType === 1 }"
        >
          <div class="od-office-header">
            <div class="od-office-name-row">
              <span class="material-symbols-outlined" :class="o.officeType === 1 ? 'od-icon-merkez' : 'od-icon-sube'">
                {{ o.officeType === 1 ? 'hub' : 'store' }}
              </span>
              <strong>{{ o.officeName }}</strong>
            </div>
            <span class="od-type-badge" :class="officeTypeBadgeClass(o.officeType)">
              {{ officeTypeLabel(o.officeType) }}
            </span>
          </div>

          <div class="od-office-meta">
            <span><span class="material-symbols-outlined od-meta-icon">account_balance_wallet</span> {{ o.vaultCount ?? 0 }} kasa</span>
            <span><span class="material-symbols-outlined od-meta-icon">group</span> {{ o.userCount ?? 0 }} kullanıcı</span>
          </div>

          <div class="od-office-balances">
            <div v-for="[cur, bal] in topCurrencies(o.totalBalancesByCurrency)" :key="cur" class="od-bal-item">
              <span class="od-bal-cur">{{ cur }}</span>
              <span class="od-bal-amt">{{ fmtMoney(bal) }}</span>
            </div>
            <div v-if="!o.totalBalancesByCurrency || Object.keys(o.totalBalancesByCurrency).length === 0" class="od-bal-empty">
              Bakiye yok
            </div>
          </div>

          <div class="od-office-footer">
            <div>
              <div class="od-footer-label">Günlük K/Z</div>
              <div :class="plClass(o.dailyProfitLoss)">{{ fmtMoney(o.dailyProfitLoss) }} ₺</div>
            </div>
            <div>
              <div class="od-footer-label">Aylık K/Z</div>
              <div :class="plClass(o.monthlyProfitLoss)">{{ fmtMoney(o.monthlyProfitLoss) }} ₺</div>
            </div>
            <div>
              <div class="od-footer-label">Toplam Varlık</div>
              <div class="od-footer-total">{{ fmtMoney(o.totalValueInBaseCurrency) }} ₺</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Performance Ranking -->
      <div class="od-perf-section">
        <div class="od-perf-header">
          <h3 class="od-section-title">
            <span class="material-symbols-outlined">leaderboard</span> Şube Performansı
          </h3>
          <div class="od-perf-periods">
            <button
              v-for="p in ['daily', 'weekly', 'monthly']"
              :key="p"
              class="od-perf-period-btn"
              :class="{ active: perfPeriod === p }"
              @click="changePerfPeriod(p)"
            >{{ perfPeriodLabel(p) }}</button>
          </div>
        </div>

        <div v-if="perfLoading" class="od-loading" style="padding:1.5rem">
          <span class="material-symbols-outlined spin">progress_activity</span>
        </div>
        <div v-else-if="perfData.length === 0" class="od-perf-empty">Veri bulunamadı</div>
        <div v-else class="od-perf-table-wrap">
          <table class="od-perf-table">
            <thead>
              <tr>
                <th>#</th>
                <th>Şube</th>
                <th>İşlem</th>
                <th>Hacim (₺)</th>
                <th>Kâr (₺)</th>
                <th>Transfer</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, idx) in perfData" :key="row.officeId">
                <td>
                  <span v-if="idx === 0" class="od-rank gold">1</span>
                  <span v-else-if="idx === 1" class="od-rank silver">2</span>
                  <span v-else-if="idx === 2" class="od-rank bronze">3</span>
                  <span v-else class="od-rank">{{ idx + 1 }}</span>
                </td>
                <td>
                  <div class="od-perf-name">
                    <span class="material-symbols-outlined" style="font-size:1rem" :style="{ color: row.officeType === 1 ? '#f59e0b' : '#3b82f6' }">{{ officeTypeIcon(row.officeType) }}</span>
                    {{ row.officeName }}
                  </div>
                </td>
                <td class="od-perf-num">{{ row.transactionCount }}</td>
                <td class="od-perf-num">{{ fmtMoney(row.totalVolume) }}</td>
                <td class="od-perf-num" :class="plClass(row.totalProfit)">{{ fmtMoney(row.totalProfit) }}</td>
                <td class="od-perf-num">{{ row.transferCount }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.od-loading, .od-error {
  display: flex; flex-direction: column; align-items: center;
  justify-content: center; gap: 0.75rem; padding: 3rem;
  color: #94a3b8; font-size: 0.95rem;
}
.od-error { color: #ef4444; }
@keyframes spin { to { transform: rotate(360deg); } }
.spin { animation: spin 1s linear infinite; }

.od-stats {
  display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 1rem; margin-bottom: 1.5rem;
}
.od-stat {
  background: white; border-radius: 12px; padding: 1rem;
  display: flex; align-items: center; gap: 0.75rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.08);
}
.od-stat-icon { font-size: 1.75rem; padding: 0.4rem; border-radius: 10px; }
.od-stat-icon.blue   { background: #eff6ff; color: #3b82f6; }
.od-stat-icon.purple { background: #f5f3ff; color: #8b5cf6; }
.od-stat-icon.green  { background: #f0fdf4; color: #22c55e; }
.od-stat-icon.red    { background: #fef2f2; color: #ef4444; }
.od-stat-icon.amber  { background: #fffbeb; color: #f59e0b; }
.od-stat-val { font-size: 1.25rem; font-weight: 800; color: #1e293b; }
.od-stat-lbl { font-size: 0.7rem; color: #64748b; margin-top: 2px; }

.text-green { color: #16a34a; }
.text-red   { color: #ef4444; }
.text-gray  { color: #94a3b8; }

.od-section-title {
  display: flex; align-items: center; gap: 0.5rem;
  font-size: 1.1rem; font-weight: 700; color: #1e293b;
  margin: 0 0 1rem;
}

.od-office-grid {
  display: grid; grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
  gap: 1rem; margin-bottom: 1.5rem;
}

.od-office-card {
  background: white; border-radius: 12px; padding: 1.25rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.08);
  border-left: 4px solid #3b82f6;
  transition: box-shadow .2s;
}
.od-office-card:hover { box-shadow: 0 4px 12px rgba(0,0,0,.12); }
.od-office-card.od-merkez { border-left-color: #f59e0b; }

.od-office-header {
  display: flex; align-items: center; justify-content: space-between;
  margin-bottom: 0.75rem;
}
.od-office-name-row { display: flex; align-items: center; gap: 0.5rem; font-size: 1rem; }
.od-icon-merkez { color: #f59e0b; font-size: 1.25rem; }
.od-icon-sube   { color: #3b82f6; font-size: 1.25rem; }

.od-type-badge {
  padding: 2px 10px; border-radius: 20px; font-size: 0.7rem; font-weight: 600;
}
.badge-merkez { background: #fffbeb; color: #b45309; }
.badge-sube   { background: #eff6ff; color: #2563eb; }
.badge-bayi   { background: #f0fdf4; color: #16a34a; }

.od-office-meta {
  display: flex; gap: 1rem; margin-bottom: 0.75rem;
  font-size: 0.8rem; color: #64748b;
}
.od-office-meta span { display: flex; align-items: center; gap: 0.25rem; }
.od-meta-icon { font-size: 0.9rem; }

.od-office-balances {
  display: flex; flex-wrap: wrap; gap: 0.5rem;
  margin-bottom: 0.75rem; padding: 0.5rem;
  background: #f8fafc; border-radius: 8px;
}
.od-bal-item {
  display: flex; justify-content: space-between; gap: 0.5rem;
  padding: 0.25rem 0.5rem; background: white;
  border-radius: 6px; font-size: 0.8rem; min-width: 120px;
  border: 1px solid #f1f5f9;
}
.od-bal-cur { font-weight: 600; color: #3b82f6; }
.od-bal-amt { font-family: monospace; color: #1e293b; }
.od-bal-empty { font-size: 0.8rem; color: #94a3b8; padding: 0.25rem; }

.od-office-footer {
  display: grid; grid-template-columns: 1fr 1fr 1fr;
  gap: 0.5rem; padding-top: 0.75rem;
  border-top: 1px solid #f1f5f9; font-size: 0.85rem;
}
.od-footer-label { font-size: 0.7rem; color: #94a3b8; margin-bottom: 2px; }
.od-footer-total { font-weight: 700; color: #1e293b; }

/* Performance */
.od-perf-section { margin-top: 0.5rem; }
.od-perf-header {
  display: flex; align-items: center; justify-content: space-between;
  margin-bottom: 1rem; flex-wrap: wrap; gap: 0.5rem;
}
.od-perf-periods { display: flex; gap: 0.25rem; }
.od-perf-period-btn {
  padding: 0.35rem 0.75rem; border: 1px solid #e2e8f0; background: white;
  border-radius: 6px; font-size: 0.78rem; cursor: pointer; color: #64748b;
  transition: all .2s;
}
.od-perf-period-btn:hover { border-color: #3b82f6; color: #3b82f6; }
.od-perf-period-btn.active { background: #3b82f6; color: white; border-color: #3b82f6; }

.od-perf-empty { text-align: center; color: #94a3b8; padding: 2rem; font-size: 0.85rem; }
.od-perf-table-wrap {
  background: white; border-radius: 12px; overflow-x: auto;
  box-shadow: 0 1px 3px rgba(0,0,0,.08);
}
.od-perf-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
.od-perf-table th {
  text-align: left; padding: 0.65rem 1rem; background: #f8fafc;
  color: #64748b; font-weight: 600; border-bottom: 1px solid #e2e8f0;
  white-space: nowrap;
}
.od-perf-table td { padding: 0.65rem 1rem; border-bottom: 1px solid #f1f5f9; }
.od-perf-table tr:hover td { background: #fafbfc; }
.od-perf-name { display: flex; align-items: center; gap: 0.4rem; font-weight: 500; }
.od-perf-num { font-family: monospace; text-align: right; }

.od-rank {
  display: inline-flex; align-items: center; justify-content: center;
  width: 24px; height: 24px; border-radius: 6px; font-size: 0.75rem;
  font-weight: 700; background: #f1f5f9; color: #64748b;
}
.od-rank.gold   { background: #fffbeb; color: #b45309; }
.od-rank.silver { background: #f1f5f9; color: #475569; }
.od-rank.bronze { background: #fff7ed; color: #c2410c; }
</style>
