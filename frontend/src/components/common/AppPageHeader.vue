<script setup lang="ts">
defineProps<{
  icon: string
  title: string
  subtitle?: string
}>()
</script>

<template>
  <div class="app-page-header">
    <div class="aph-left">
      <div class="aph-icon">
        <span class="material-symbols-outlined">{{ icon }}</span>
      </div>
      <div>
        <h1 class="aph-title">{{ title }}</h1>
        <p v-if="subtitle" class="aph-subtitle">{{ subtitle }}</p>
      </div>
    </div>
    <div class="aph-actions">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.app-page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 20px 24px;
  background: linear-gradient(135deg, #1e293b 0%, #334155 100%);
  border-radius: 16px;
  margin-bottom: 20px;
  flex-wrap: wrap;
}
.aph-left {
  display: flex;
  align-items: center;
  gap: 14px;
}
.aph-icon {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
  background: rgba(255,255,255,.12);
}
.aph-icon .material-symbols-outlined {
  font-size: 24px;
  color: #fff;
}
.aph-title {
  font-size: 20px;
  font-weight: 700;
  color: #fff;
  margin: 0;
}
.aph-subtitle {
  font-size: 12px;
  color: rgba(255,255,255,.65);
  margin: 3px 0 0;
}
.aph-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
