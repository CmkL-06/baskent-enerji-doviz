<template>
  <router-view />
</template>

<script setup>
</script>

<style>
  :root {
    --sidebar: #1e293b;
    --sidebar-text: #94a3b8;
    --sidebar-active: #0ea5e9;
    --header: #ffffff;
    --header-border: #e2e8f0;
    --body: #f8fafc;
    --card: #ffffff;
    --card-border: #e2e8f0;
    --text: #0f172a;
    --muted: #64748b;
  }
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: 'Inter', sans-serif; background: var(--body); color: var(--text); min-height: 100vh; }
  #app { min-height: 100vh; }
</style>
