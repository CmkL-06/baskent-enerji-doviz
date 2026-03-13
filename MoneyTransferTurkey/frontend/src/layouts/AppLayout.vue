<template>
  <div class="app-layout">
    <aside class="sidebar">
      <div class="sidebar-logo">Başkent Enerji Döviz</div>
      <nav class="sidebar-nav">
        <div class="nav-section">
          <div class="nav-section-title">Genel</div>
          <router-link to="/" class="nav-item" active-class="active">
            <span class="nav-icon">▣</span> Gösterge Paneli
          </router-link>
          <router-link to="/user" class="nav-item" active-class="active">
            <span class="nav-icon">👤</span> Kullanıcı
          </router-link>
        </div>
        <div class="nav-section">
          <div class="nav-section-title">Döviz Bürosu</div>
          <router-link to="/exchange" class="nav-item" active-class="active">▣ Döviz / Ofis</router-link>
          <router-link to="/vault" class="nav-item" active-class="active">▣ Kasa</router-link>
          <router-link to="/party" class="nav-item" active-class="active">▣ Taraflar</router-link>
          <router-link to="/rates" class="nav-item" active-class="active">▣ Kur</router-link>
        </div>
        <div class="nav-section">
          <div class="nav-section-title">Site & İçerik</div>
          <router-link to="/site" class="nav-item" active-class="active">▣ Site</router-link>
          <router-link to="/blog" class="nav-item" active-class="active">▣ Blog</router-link>
          <router-link to="/coin" class="nav-item" active-class="active">▣ Coin</router-link>
        </div>
      </nav>
    </aside>
    <main class="main">
      <header class="header">
        <h1 class="page-title">{{ pageTitle }}</h1>
        <div class="header-user">
          <span>{{ user?.email || user?.userName || 'Kullanıcı' }}</span>
          <button type="button" class="btn-logout" @click="logout">Çıkış</button>
        </div>
      </header>
      <div class="content">
        <router-view />
      </div>
    </main>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuth } from '../composables/useAuth'

const route = useRoute()
const router = useRouter()
const { user, logout: doLogout } = useAuth()

const titles = {
  Dashboard: 'Dashboard',
  User: 'Kullanıcı',
  Exchange: 'Döviz / Ofis',
  Vault: 'Kasa',
  Party: 'Taraflar',
  Rates: 'Kur / Auto Rate',
  Site: 'Site & İçerik',
  Blog: 'Blog',
  Coin: 'Coin',
}

const pageTitle = computed(() => titles[route.name] || 'Başkent Enerji Döviz')

function logout() {
  doLogout()
  router.push({ name: 'Login' })
}
</script>

<style scoped>
.app-layout { display: flex; min-height: 100vh; }
.sidebar {
  width: 260px;
  background: var(--sidebar);
  color: var(--sidebar-text);
  padding: 1rem 0;
  flex-shrink: 0;
}
.sidebar-logo {
  padding: 0 1.25rem 1.25rem;
  font-weight: 700;
  font-size: 1.1rem;
  color: #fff;
  border-bottom: 1px solid rgba(255,255,255,0.08);
  margin-bottom: 1rem;
}
.sidebar-nav { padding: 0 0.5rem; }
.nav-section { margin-bottom: 0.5rem; }
.nav-section-title {
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--muted);
  padding: 0.5rem 0.75rem;
}
.nav-item {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.6rem 0.75rem;
  border-radius: 8px;
  color: inherit;
  text-decoration: none;
  font-size: 0.9rem;
  transition: background 0.15s, color 0.15s;
}
.nav-item:hover { background: rgba(255,255,255,0.06); color: #fff; }
.nav-item.active { background: rgba(14,165,233,0.15); color: var(--sidebar-active); }
.nav-icon { opacity: 0.85; }
.main { flex: 1; display: flex; flex-direction: column; min-width: 0; }
.header {
  background: var(--header);
  border-bottom: 1px solid var(--header-border);
  padding: 0.75rem 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.page-title { font-size: 1.15rem; font-weight: 600; }
.header-user { display: flex; align-items: center; gap: 0.75rem; font-size: 0.9rem; color: var(--muted); }
.header-user span { color: var(--text); font-weight: 500; }
.btn-logout {
  padding: 0.35rem 0.75rem;
  border: 1px solid var(--card-border);
  border-radius: 6px;
  background: var(--card);
  cursor: pointer;
  font-size: 0.85rem;
}
.btn-logout:hover { background: #f1f5f9; }
.content { padding: 1.5rem; overflow: auto; flex: 1; }
</style>
