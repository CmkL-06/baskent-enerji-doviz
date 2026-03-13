<template>
  <div class="dashboard">
    <section class="welcome">
      <h2 class="welcome-title">Hoş geldiniz, {{ userName }}</h2>
      <p class="welcome-date">{{ todayLabel }}</p>
    </section>

    <section class="stats">
      <div class="stat-card">
        <div class="stat-icon stat-icon--office">▣</div>
        <div class="stat-body">
          <span class="stat-label">Ofisler</span>
          <span class="stat-value">{{ stats.offices }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--vault">₺</div>
        <div class="stat-body">
          <span class="stat-label">Kasa</span>
          <span class="stat-value">{{ stats.vaults }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--tx">↔</div>
        <div class="stat-body">
          <span class="stat-label">İşlem (bugün)</span>
          <span class="stat-value">{{ stats.todayTransactions }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--party">👥</div>
        <div class="stat-body">
          <span class="stat-label">Taraflar</span>
          <span class="stat-value">{{ stats.parties }}</span>
        </div>
      </div>
    </section>

    <section class="grid-2">
      <div class="panel">
        <div class="panel-head">
          <h3>Son işlemler</h3>
          <router-link :to="{ name: 'Exchange' }" class="panel-link">Tümü →</router-link>
        </div>
        <div class="panel-body">
          <table class="table" v-if="recentTransactions.length">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>İşlem no</th>
                <th>Tür</th>
                <th class="text-right">Tutar</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="tx in recentTransactions" :key="tx.id">
                <td>{{ tx.date }}</td>
                <td>{{ tx.number }}</td>
                <td><span class="badge" :class="tx.typeClass">{{ tx.type }}</span></td>
                <td class="text-right">{{ tx.amount }}</td>
              </tr>
            </tbody>
          </table>
          <div v-else class="empty-state">
            <p>Henüz işlem yok veya API bağlı değil.</p>
            <router-link :to="{ name: 'Exchange' }">Döviz / Ofis</router-link> sayfasından işlem açabilirsiniz.
          </div>
        </div>
      </div>

      <div class="panel">
        <div class="panel-head">
          <h3>Hızlı işlemler</h3>
        </div>
        <div class="panel-body quick-actions">
          <router-link :to="{ name: 'Exchange' }" class="quick-action">
            <span class="quick-action-icon">↔</span>
            <span>Yeni döviz işlemi</span>
          </router-link>
          <router-link :to="{ name: 'Vault' }" class="quick-action">
            <span class="quick-action-icon">₺</span>
            <span>Kasa özeti</span>
          </router-link>
          <router-link :to="{ name: 'Party' }" class="quick-action">
            <span class="quick-action-icon">👥</span>
            <span>Taraflar</span>
          </router-link>
          <router-link :to="{ name: 'Rates' }" class="quick-action">
            <span class="quick-action-icon">📈</span>
            <span>Kurlar</span>
          </router-link>
        </div>
      </div>
    </section>

    <section class="panel">
      <div class="panel-head">
        <h3>Kur özeti (örnek)</h3>
        <router-link :to="{ name: 'Rates' }" class="panel-link">Kur yönetimi →</router-link>
      </div>
      <div class="panel-body">
        <div class="rates-grid">
          <div class="rate-item" v-for="r in sampleRates" :key="r.code">
            <span class="rate-code">{{ r.code }}</span>
            <span class="rate-buy">Alış {{ r.buy }}</span>
            <span class="rate-sell">Satış {{ r.sell }}</span>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '../api/client'
import { useAuth } from '../composables/useAuth'

const { user } = useAuth()
const userName = computed(() => user.value?.userName || user.value?.Firstname || user.value?.email || 'Kullanıcı')

const todayLabel = computed(() => {
  const d = new Date()
  return d.toLocaleDateString('tr-TR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })
})

const stats = ref({
  offices: '—',
  vaults: '—',
  todayTransactions: '—',
  parties: '—',
})

const recentTransactions = ref([])

const sampleRates = ref([
  { code: 'USD', buy: '34,25', sell: '34,85' },
  { code: 'EUR', buy: '36,10', sell: '36,70' },
  { code: 'GBP', buy: '43,50', sell: '44,20' },
])

onMounted(async () => {
  try {
    const [officesRes, vaultsRes, txRes, partiesRes] = await Promise.all([
      api.get('/api/v1/Exchange/offices').catch(() => null),
      api.get('/api/v1/Exchange/vaults').catch(() => null),
      api.get('/api/v1/Exchange/transactions').catch(() => null),
      api.get('/api/v1/Exchange/parties').catch(() => null),
    ])
    if (officesRes?.data?.length !== undefined) stats.value.offices = officesRes.data.length
    if (vaultsRes?.data?.length !== undefined) stats.value.vaults = vaultsRes.data.length
    if (partiesRes?.data?.length !== undefined) stats.value.parties = partiesRes.data.length
    if (txRes?.data?.length !== undefined) {
      stats.value.todayTransactions = typeof txRes.data === 'number' ? txRes.data : (txRes.data.filter(t => isToday(t.transactionDate || t.TransactionDate)).length || txRes.data.length)
      recentTransactions.value = (Array.isArray(txRes.data) ? txRes.data.slice(0, 5) : []).map((t, i) => ({
        id: t.id || i,
        date: formatDate(t.transactionDate || t.TransactionDate),
        number: t.transactionNumber || t.TransactionNumber || '—',
        type: t.type || t.Type || 'İşlem',
        typeClass: (t.type || t.Type || '').toLowerCase().includes('sell') ? 'badge--sell' : 'badge--buy',
        amount: formatAmount(t.totalAmount ?? t.TotalAmount ?? t.amount ?? t.Amount),
      }))
    }
  } catch {
    // Demo / API yok: örnek son işlemler göster
    recentTransactions.value = [
      { id: 1, date: todayLabel.value.split(' ').slice(0, 2).join(' '), number: 'TXN-001', type: 'Satış', typeClass: 'badge--sell', amount: '1.250,00 ₺' },
      { id: 2, date: todayLabel.value.split(' ').slice(0, 2).join(' '), number: 'TXN-002', type: 'Alış', typeClass: 'badge--buy', amount: '3.400,00 ₺' },
    ]
  }
})

function isToday(d) {
  if (!d) return false
  const dt = typeof d === 'string' ? new Date(d) : d
  const today = new Date()
  return dt.getDate() === today.getDate() && dt.getMonth() === today.getMonth() && dt.getFullYear() === today.getFullYear()
}

function formatDate(d) {
  if (!d) return '—'
  const dt = typeof d === 'string' ? new Date(d) : d
  return dt.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
}

function formatAmount(n) {
  if (n == null) return '—'
  return new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2 }).format(n) + ' ₺'
}
</script>

<style scoped>
.dashboard { max-width: 1200px; }
.welcome { margin-bottom: 1.5rem; }
.welcome-title { font-size: 1.35rem; font-weight: 700; color: var(--text); margin-bottom: 0.25rem; }
.welcome-date { font-size: 0.95rem; color: var(--muted); }

.stats {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}
.stat-card {
  background: var(--card);
  border: 1px solid var(--card-border);
  border-radius: 12px;
  padding: 1.25rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: 0 1px 3px rgba(0,0,0,0.06);
}
.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.4rem;
}
.stat-icon--office { background: #e0f2fe; color: #0369a1; }
.stat-icon--vault { background: #dcfce7; color: #166534; }
.stat-icon--tx { background: #fef3c7; color: #92400e; }
.stat-icon--party { background: #f3e8ff; color: #6b21a8; }
.stat-body { display: flex; flex-direction: column; gap: 0.2rem; }
.stat-label { font-size: 0.8rem; color: var(--muted); }
.stat-value { font-size: 1.5rem; font-weight: 700; color: var(--text); }

.grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
  margin-bottom: 1.5rem;
}
@media (max-width: 900px) {
  .grid-2 { grid-template-columns: 1fr; }
}

.panel {
  background: var(--card);
  border: 1px solid var(--card-border);
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0,0,0,0.06);
}
.panel-head {
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--card-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.panel-head h3 { font-size: 1rem; font-weight: 600; margin: 0; }
.panel-link { font-size: 0.875rem; color: var(--sidebar-active); text-decoration: none; }
.panel-link:hover { text-decoration: underline; }
.panel-body { padding: 1.25rem; }

.table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
.table th, .table td { padding: 0.6rem 0.75rem; text-align: left; border-bottom: 1px solid var(--card-border); }
.table th { font-weight: 600; color: var(--muted); font-size: 0.75rem; text-transform: uppercase; }
.table .text-right { text-align: right; }
.badge { display: inline-block; padding: 0.2rem 0.5rem; border-radius: 6px; font-size: 0.75rem; font-weight: 500; }
.badge--buy { background: #dcfce7; color: #166534; }
.badge--sell { background: #fee2e2; color: #991b1b; }

.empty-state { text-align: center; padding: 1.5rem; color: var(--muted); font-size: 0.9rem; }
.empty-state a { color: var(--sidebar-active); }

.quick-actions { display: flex; flex-direction: column; gap: 0.5rem; }
.quick-action {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem;
  border-radius: 8px;
  color: var(--text);
  text-decoration: none;
  border: 1px solid var(--card-border);
  transition: background 0.15s;
}
.quick-action:hover { background: #f8fafc; }
.quick-action-icon { font-size: 1.25rem; }

.rates-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 1rem;
}
.rate-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding: 0.75rem;
  background: #f8fafc;
  border-radius: 8px;
  font-size: 0.9rem;
}
.rate-code { font-weight: 700; color: var(--text); }
.rate-buy { color: #166534; }
.rate-sell { color: #991b1b; }
</style>
