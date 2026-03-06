<template>
  <div class="page">
    <div class="cards">
      <div class="card"><div class="card-title">Sayfa</div><div class="card-value">PageController</div></div>
      <div class="card"><div class="card-title">Menü / Slider</div><div class="card-value">Menu, Slider</div></div>
      <div class="card"><div class="card-title">Tema / Form</div><div class="card-value">Theme, Form</div></div>
      <div class="card"><div class="card-title">SEO / Analytics</div><div class="card-value">Seo, Analytics</div></div>
    </div>
    <div class="table-wrap">
      <table>
        <thead><tr><th>Controller</th><th>Kullanım</th></tr></thead>
        <tbody>
          <tr><td>Page, Site, Menu, Slider</td><td>Site içeriği</td></tr>
          <tr><td>Theme, GlobalColor</td><td>Görünüm / renk</td></tr>
          <tr><td>Form, Form_Submit</td><td>İletişim formları</td></tr>
          <tr><td>Analytics, Seo, Media</td><td>Analitik, medya</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
</script>

<style scoped>
.page { max-width: 800px; }
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 1rem; margin-bottom: 1rem; }
.card { background: var(--card); border: 1px solid var(--card-border); border-radius: 10px; padding: 1rem; }
.card-title { font-size: 0.8rem; color: var(--muted); }
.card-value { font-size: 0.95rem; font-weight: 600; }
.table-wrap { background: var(--card); border: 1px solid var(--card-border); border-radius: 10px; overflow: hidden; margin-top: 1rem; }
table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
th, td { padding: 0.75rem 1rem; text-align: left; border-bottom: 1px solid var(--card-border); }
th { background: #f8fafc; font-weight: 600; color: var(--muted); font-size: 0.8rem; }
</style>
