<script setup lang="ts">
import NotificationContainer from '@/components/NotificationContainer.vue'
</script>

<template>
  <router-view />
  <NotificationContainer />
</template>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  overflow: hidden;
  background: #ffffff;
  min-height: 100vh;
  color: #1a1a1a;
}

/* Global flag icon styles */
.fi {
  display: inline-block;
  width: 1.33333em;
  line-height: 1em;
  vertical-align: middle;
  margin-right: 0.25rem;
}

.fi:before {
  display: inline-block;
  width: 1.33333em;
  line-height: 1em;
}

/* Currency badge for crypto/unknown currencies */
.currency-badge,
.currency-code-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 2em;
  height: 1.5em;
  margin-right: 0.25rem;
  padding: 0.1em 0.3em;
  background: #f0f0f0;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.85em;
  font-weight: 600;
  color: #333;
  font-family: 'Segoe UI', system-ui, sans-serif;
  vertical-align: middle;
}

/* Crypto currency badge */
.currency-badge.crypto,
.currency-code-badge.crypto {
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  border-color: #f59e0b;
  color: white;
}

/* Metal currency badge */
.currency-badge.metal,
.currency-code-badge.metal {
  background: linear-gradient(135deg, #d1d5db 0%, #9ca3af 100%);
  border-color: #9ca3af;
  color: #111827;
}

#app {
  width: 100vw;
  height: 100vh;
}

/* Global positive/negative amount styles */
.amount-positive {
  color: var(--color-success) !important;
}

.amount-negative {
  color: var(--color-danger) !important;
}

.amount-badge-positive {
  background-color: var(--color-success-bg) !important;
  border: 1px solid #6ee7b7 !important;
  color: var(--color-success) !important;
  padding: 0.3rem 0.6rem;
  border-radius: 6px;
  font-weight: 500;
}

.amount-badge-negative {
  background-color: var(--color-danger-bg) !important;
  border: 1px solid #fca5a5 !important;
  color: var(--color-danger) !important;
  padding: 0.3rem 0.6rem;
  border-radius: 6px;
  font-weight: 500;
}
</style>
