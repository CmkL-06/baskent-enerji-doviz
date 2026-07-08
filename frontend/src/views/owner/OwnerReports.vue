<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import apiService from '@/services/apiservice'

const reportType = ref<'daily' | 'monthly' | 'consolidated' | 'audit'>('daily')
const offices = ref<any[]>([])
const selectedOfficeId = ref('')
const dateFrom = ref(new Date().toISOString().slice(0, 10))
const dateTo = ref(new Date().toISOString().slice(0, 10))
const loading = ref(false)
const initialLoad = ref(true)
const reportData = ref<any>(null)
const error = ref('')

const auditData = ref<any[]>([])
const auditLoading = ref(false)
const auditOffice = ref('')
const auditDate = ref(new Date().toISOString().slice(0, 10))

onMounted(async () => {
  try {
    offices.value = await apiService.getOfficeSummaries() ?? []
  } catch {}
  initialLoad.value = false
})

async function loadAudit() {
  auditLoading.value = true
  try {
    const officeId = auditOffice.value || offices.value?.[0]?.officeId
    if (!officeId) return
    const data = await apiService.getVaultBalanceHistories(officeId, auditDate.value)
    auditData.value = data ?? []
  } catch { auditData.value = [] }
  finally { auditLoading.value = false }
}

function txTypeLabel(t: string | number) {
  const map: Record<string, string> = { '0': 'Döviz', '1': 'Yatırım', '2': 'Çekim', '3': 'Düzeltme', '4': 'Transfer', Exchange: 'Döviz', Deposit: 'Yatırım', Withdrawal: 'Çekim', Adjustment: 'Düzeltme', Transfer: 'Transfer' }
  return map[String(t)] ?? String(t)
}

function txTypeClass(t: string | number) {
  const s = String(t)
  if (s === '1' || s === 'Deposit') return 'at-deposit'
  if (s === '2' || s === 'Withdrawal') return 'at-withdrawal'
  if (s === '3' || s === 'Adjustment') return 'at-adjustment'
  if (s === '4' || s === 'Transfer') return 'at-transfer'
  return 'at-exchange'
}

async function generate() {
  loading.value = true; error.value = ''; reportData.value = null
  try {
    if (reportType.value === 'consolidated') {
      const summaries = await apiService.getOfficeSummaries()
      reportData.value = { type: 'consolidated', offices: summaries || [] }
    } else {
      const params: any = { startDate: dateFrom.value, endDate: dateTo.value }
      if (selectedOfficeId.value) params.officeId = selectedOfficeId.value
      const data = await apiService.getPnLReport(params)
      reportData.value = { type: reportType.value, data: data || {} }
    }
  } catch (e: any) {
    error.value = e?.response?.data?.error || e?.message || 'Rapor oluşturulamadı'
  } finally {
    loading.value = false
  }
}

function fmtMoney(n: number | null | undefined) {
  if (n == null) return '—'
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function plClass(n: number) {
  if (n > 0) return 'text-green'
  if (n < 0) return 'text-red'
  return ''
}
</script>

<template>
  <div>
    <h3 class="or-title"><span class="material-symbols-outlined" aria-hidden="true">insert_chart</span> Raporlar</h3>

    <div class="or-controls">
      <div class="or-type-tabs">
        <button :class="{ active: reportType === 'daily' }" @click="reportType = 'daily'">
          <span class="material-symbols-outlined" aria-hidden="true">today</span> Günlük
        </button>
        <button :class="{ active: reportType === 'monthly' }" @click="reportType = 'monthly'">
          <span class="material-symbols-outlined" aria-hidden="true">calendar_month</span> Aylık
        </button>
        <button :class="{ active: reportType === 'consolidated' }" @click="reportType = 'consolidated'">
          <span class="material-symbols-outlined" aria-hidden="true">hub</span> Konsolide
        </button>
        <button :class="{ active: reportType === 'audit' }" @click="reportType = 'audit'">
          <span class="material-symbols-outlined" aria-hidden="true">history</span> Denetim İzi
        </button>
      </div>

      <div v-if="reportType !== 'consolidated' && reportType !== 'audit'" class="or-filters">
        <div class="or-field">
          <label>Başlangıç</label>
          <input v-model="dateFrom" type="date" class="or-input" />
        </div>
        <div class="or-field">
          <label>Bitiş</label>
          <input v-model="dateTo" type="date" class="or-input" />
        </div>
        <div class="or-field">
          <label>Ofis</label>
          <select v-model="selectedOfficeId" class="or-input">
            <option value="">Tümü</option>
            <option v-for="o in offices" :key="o.officeId" :value="o.officeId">{{ o.officeName }}</option>
          </select>
        </div>
      </div>

      <button v-if="reportType !== 'audit'" class="or-generate" :disabled="loading" @click="generate">
        <span class="material-symbols-outlined" aria-hidden="true">{{ loading ? 'progress_activity' : 'play_arrow' }}</span>
        {{ loading ? 'Oluşturuluyor...' : 'Rapor Oluştur' }}
      </button>
    </div>

    <div v-if="error" class="or-alert error">
      <span class="material-symbols-outlined" aria-hidden="true">error</span> {{ error }}
    </div>

    <!-- Consolidated Report -->
    <div v-if="reportData?.type === 'consolidated'" class="or-result">
      <h4 class="or-result-title">Konsolide Durum Raporu</h4>
      <div class="or-table-wrap">
        <table class="or-table">
          <thead>
            <tr>
              <th>Ofis</th>
              <th>Tip</th>
              <th>Kasa</th>
              <th>Günlük K/Z</th>
              <th>Aylık K/Z</th>
              <th>Toplam Varlık</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="o in reportData.offices" :key="o.officeId">
              <td><strong>{{ o.officeName }}</strong></td>
              <td>
                <span class="or-type-badge" :class="o.officeType === 1 ? 'merkez' : 'sube'">
                  {{ o.officeType === 1 ? 'Merkez' : 'Şube' }}
                </span>
              </td>
              <td>{{ o.vaultCount ?? 0 }}</td>
              <td :class="plClass(o.dailyProfitLoss)">{{ fmtMoney(o.dailyProfitLoss) }} ₺</td>
              <td :class="plClass(o.monthlyProfitLoss)">{{ fmtMoney(o.monthlyProfitLoss) }} ₺</td>
              <td class="or-mono"><strong>{{ fmtMoney(o.totalValueInBaseCurrency) }} ₺</strong></td>
            </tr>
          </tbody>
          <tfoot>
            <tr>
              <td colspan="3"><strong>TOPLAM</strong></td>
              <td :class="plClass(reportData.offices.reduce((a: number, o: any) => a + (o.dailyProfitLoss ?? 0), 0))">
                <strong>{{ fmtMoney(reportData.offices.reduce((a: number, o: any) => a + (o.dailyProfitLoss ?? 0), 0)) }} ₺</strong>
              </td>
              <td :class="plClass(reportData.offices.reduce((a: number, o: any) => a + (o.monthlyProfitLoss ?? 0), 0))">
                <strong>{{ fmtMoney(reportData.offices.reduce((a: number, o: any) => a + (o.monthlyProfitLoss ?? 0), 0)) }} ₺</strong>
              </td>
              <td class="or-mono">
                <strong>{{ fmtMoney(reportData.offices.reduce((a: number, o: any) => a + (o.totalValueInBaseCurrency ?? 0), 0)) }} ₺</strong>
              </td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>

    <!-- PnL Report -->
    <div v-else-if="reportData?.type && reportData.data" class="or-result">
      <h4 class="or-result-title">K/Z Raporu ({{ dateFrom }} — {{ dateTo }})</h4>
      <div class="or-pnl-cards">
        <div class="or-pnl-card">
          <div class="or-pnl-label">Toplam Gelir</div>
          <div class="or-pnl-val text-green">{{ fmtMoney(reportData.data.totalRevenue ?? reportData.data.totalIncome) }} ₺</div>
        </div>
        <div class="or-pnl-card">
          <div class="or-pnl-label">Toplam Gider</div>
          <div class="or-pnl-val text-red">{{ fmtMoney(reportData.data.totalExpense ?? reportData.data.totalCost) }} ₺</div>
        </div>
        <div class="or-pnl-card highlight">
          <div class="or-pnl-label">Net Kâr/Zarar</div>
          <div class="or-pnl-val" :class="plClass(reportData.data.netProfit ?? reportData.data.profitLoss)">
            {{ fmtMoney(reportData.data.netProfit ?? reportData.data.profitLoss) }} ₺
          </div>
        </div>
      </div>

      <div v-if="reportData.data.transactions?.length || reportData.data.details?.length" class="or-table-wrap">
        <table class="or-table">
          <thead>
            <tr>
              <th>Tarih</th>
              <th>İşlem</th>
              <th>Para Birimi</th>
              <th>Miktar</th>
              <th>Kur</th>
              <th>K/Z</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(t, i) in (reportData.data.transactions || reportData.data.details || []).slice(0, 50)" :key="i">
              <td class="or-date-col">{{ t.date ? new Date(t.date).toLocaleDateString('tr-TR') : '—' }}</td>
              <td>{{ t.type || t.transactionType || '—' }}</td>
              <td><span class="or-cur-tag">{{ t.currencyCode || '—' }}</span></td>
              <td class="or-mono">{{ fmtMoney(t.amount ?? t.quantity) }}</td>
              <td class="or-mono">{{ fmtMoney(t.rate ?? t.exchangeRate) }}</td>
              <td :class="plClass(t.profit ?? t.profitLoss)">{{ fmtMoney(t.profit ?? t.profitLoss) }} ₺</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <div v-else-if="reportType !== 'audit' && !loading && !initialLoad && !reportData" class="or-empty">
      <span class="material-symbols-outlined" aria-hidden="true">assessment</span>
      <p>Rapor tipi seçin ve "Rapor Oluştur" butonuna tıklayın</p>
    </div>

    <!-- Audit Trail -->
    <div v-if="reportType === 'audit'" class="or-result">
      <div class="at-controls">
        <div class="or-field">
          <label>Ofis</label>
          <select v-model="auditOffice" class="or-input">
            <option v-for="o in offices" :key="o.officeId" :value="o.officeId">{{ o.officeName }}</option>
          </select>
        </div>
        <div class="or-field">
          <label>Tarih</label>
          <input v-model="auditDate" type="date" class="or-input" />
        </div>
        <button class="or-generate" :disabled="auditLoading" @click="loadAudit">
          <span class="material-symbols-outlined" aria-hidden="true">{{ auditLoading ? 'progress_activity' : 'search' }}</span>
          {{ auditLoading ? 'Yükleniyor...' : 'Sorgula' }}
        </button>
      </div>

      <div v-if="auditData.length" class="or-table-wrap" style="margin-top: 1rem">
        <table class="or-table">
          <thead>
            <tr>
              <th>Saat</th>
              <th>Tür</th>
              <th>Döviz</th>
              <th>Miktar</th>
              <th>Açıklama</th>
              <th>Kullanıcı</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="h in auditData" :key="h.id">
              <td class="or-date-col">{{ h.createdDate ? new Date(h.createdDate).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '—' }}</td>
              <td><span class="at-type-badge" :class="txTypeClass(h.transactionType)">{{ txTypeLabel(h.transactionType) }}</span></td>
              <td><span class="or-cur-tag">{{ h.currencyCode || '—' }}</span></td>
              <td class="or-mono" :class="(h.balance ?? 0) >= 0 ? 'text-green' : 'text-red'">{{ fmtMoney(h.balance) }}</td>
              <td class="at-desc">{{ h.description || '—' }}</td>
              <td>{{ h.user || 'System' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-else-if="!auditLoading && auditData.length === 0" class="or-empty" style="padding: 2rem">
        <span class="material-symbols-outlined" aria-hidden="true">history</span>
        <p>Denetim izi sorgulamak için ofis ve tarih seçin</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.or-title { display: flex; align-items: center; gap: 0.5rem; font-size: 1.1rem; font-weight: 700; color: var(--color-text); margin: 0 0 1.25rem; }

.or-controls {
  background: white; border-radius: var(--radius-lg); padding: 1.25rem;
  box-shadow: 0 1px 3px rgba(0,0,0,.08); margin-bottom: 1.25rem;
}

.or-type-tabs { display: flex; gap: 0.5rem; margin-bottom: 1rem; }
.or-type-tabs button {
  display: flex; align-items: center; gap: 0.3rem;
  padding: 0.5rem 1rem; background: var(--color-bg-page); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); cursor: pointer; font-size: 0.85rem; color: var(--color-text-secondary); transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}
.or-type-tabs button.active { background: var(--color-secondary); color: white; border-color: var(--color-secondary); }

.or-filters { display: flex; gap: 1rem; margin-bottom: 1rem; flex-wrap: wrap; }
.or-field label { display: block; font-size: 0.75rem; font-weight: 600; color: var(--color-text-secondary); margin-bottom: 0.25rem; }
.or-input {
  padding: 0.5rem 0.75rem; border: 2px solid var(--color-border); border-radius: var(--radius-md);
  font-size: 0.85rem; color: var(--color-text); background: var(--color-bg-page); outline: none;
  transition: border-color .2s;
}
.or-input:focus { border-color: var(--color-secondary); background: white; }

.or-generate {
  display: flex; align-items: center; gap: 0.4rem;
  padding: 0.6rem 1.5rem; background: var(--color-secondary); color: white;
  border: none; border-radius: var(--radius-md); cursor: pointer; font-size: 0.9rem; font-weight: 600;
  transition: background-color 0.2s;
}
.or-generate:hover:not(:disabled) { background: var(--color-secondary-hover); }
.or-generate:disabled { opacity: 0.5; cursor: not-allowed; }
@keyframes spin { to { transform: rotate(360deg); } }

.or-alert { display: flex; align-items: center; gap: 0.4rem; padding: 0.6rem 1rem; border-radius: var(--radius-md); font-size: 0.85rem; margin-bottom: 1rem; }
.or-alert.error { background: #fef2f2; color: var(--color-danger); }

.or-result { margin-top: 0.5rem; }
.or-result-title { font-size: 1rem; font-weight: 700; color: var(--color-text); margin: 0 0 1rem; }

.or-table-wrap { overflow-x: auto; background: white; border-radius: var(--radius-lg); box-shadow: 0 1px 3px rgba(0,0,0,.08); }
.or-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
.or-table th { text-align: left; padding: 0.75rem 1rem; background: var(--color-bg-page); color: var(--color-text-secondary); font-weight: 600; border-bottom: 1px solid var(--color-border); }
.or-table td { padding: 0.75rem 1rem; border-bottom: 1px solid var(--color-bg-page); }
.or-table tfoot td { background: var(--color-bg-page); font-weight: 700; border-top: 2px solid var(--color-border); }
.or-date-col { font-size: 0.8rem; color: var(--color-text-secondary); }
.or-mono { font-family: monospace; }
.or-cur-tag { background: var(--color-secondary-light); color: var(--color-secondary); padding: 2px 8px; border-radius: var(--radius-sm); font-size: 0.75rem; font-weight: 600; }

.or-type-badge { padding: 2px 10px; border-radius: var(--radius-xl); font-size: 0.7rem; font-weight: 600; }
.or-type-badge.merkez { background: #fffbeb; color: #b45309; }
.or-type-badge.sube { background: var(--color-secondary-light); color: var(--color-secondary-hover); }

.or-pnl-cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 1rem; margin-bottom: 1.25rem; }
.or-pnl-card { background: white; border-radius: var(--radius-lg); padding: 1rem; box-shadow: 0 1px 3px rgba(0,0,0,.08); }
.or-pnl-card.highlight { border: 2px solid var(--color-secondary); }
.or-pnl-label { font-size: 0.75rem; color: var(--color-text-secondary); margin-bottom: 0.25rem; }
.or-pnl-val { font-size: 1.25rem; font-weight: 800; font-family: monospace; }

.text-green { color: #16a34a; }
.text-red { color: var(--color-danger); }

.or-empty { display: flex; flex-direction: column; align-items: center; gap: 0.75rem; padding: 3rem; color: var(--color-text-muted); }

.at-controls { display: flex; gap: 1rem; align-items: flex-end; flex-wrap: wrap; }
.at-type-badge { padding: 2px 8px; border-radius: var(--radius-sm); font-size: 0.7rem; font-weight: 600; }
.at-exchange { background: var(--color-secondary-light); color: var(--color-secondary-hover); }
.at-deposit { background: #f0fdf4; color: #16a34a; }
.at-withdrawal { background: #fef2f2; color: var(--color-danger); }
.at-adjustment { background: #fefce8; color: #ca8a04; }
.at-transfer { background: #f5f3ff; color: #7c3aed; }
.at-desc { font-size: 0.8rem; color: var(--color-text-secondary); max-width: 300px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
