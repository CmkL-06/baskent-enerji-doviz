<template>
  <div class="page">
    <div class="cards">
      <div class="card"><div class="card-title">Kasa bakiyesi</div><div class="card-value">VaultSnapshot</div></div>
      <div class="card"><div class="card-title">Sayım</div><div class="card-value">VaultCount</div></div>
    </div>
    <div class="table-wrap">
      <table>
        <thead><tr><th>Endpoint</th><th>Açıklama</th></tr></thead>
        <tbody>
          <tr><td>VaultSnapshotController</td><td>Kasa anlık bakiye / snapshot</td></tr>
          <tr><td>VaultCountingBackgroundService</td><td>Arka plan sayım</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
</script>

<style scoped>
.page { max-width: 800px; }
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 1rem; }
.card { background: var(--card); border: 1px solid var(--card-border); border-radius: 10px; padding: 1rem; }
.card-title { font-size: 0.8rem; color: var(--muted); }
.card-value { font-size: 0.95rem; font-weight: 600; }
.table-wrap { background: var(--card); border: 1px solid var(--card-border); border-radius: 10px; overflow: hidden; margin-top: 1rem; }
table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
th, td { padding: 0.75rem 1rem; text-align: left; border-bottom: 1px solid var(--card-border); }
th { background: #f8fafc; font-weight: 600; color: var(--muted); font-size: 0.8rem; }
</style>
