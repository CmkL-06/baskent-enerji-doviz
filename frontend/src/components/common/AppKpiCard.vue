<script setup lang="ts">
defineProps<{
  icon: string
  label: string
  value: string | number
  unit?: string
  color?: string
  bg?: string
}>()
</script>

<template>
  <div class="app-kpi" :style="{ '--kc': color ?? '#6366f1', '--kb': bg ?? '#eef2ff' }">
    <div class="app-kpi-icon">
      <span class="material-symbols-outlined">{{ icon }}</span>
    </div>
    <div class="app-kpi-body">
      <p class="app-kpi-label">{{ label }}</p>
      <p class="app-kpi-value" :style="{ color: color ?? '#111' }">{{ value }} <span v-if="unit" class="app-kpi-unit">{{ unit }}</span></p>
    </div>
  </div>
</template>

<style scoped>
.app-kpi {
  background: var(--kb);
  border: 1px solid rgba(0,0,0,.06);
  border-radius: 14px;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 12px;
  transition: transform .2s, box-shadow .2s;
}
.app-kpi:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 16px rgba(0,0,0,.08);
}
.app-kpi-icon {
  width: 42px;
  height: 42px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 10px;
  background: white;
  flex-shrink: 0;
}
.app-kpi-icon .material-symbols-outlined {
  font-size: 22px;
  color: var(--kc);
}
.app-kpi-label {
  font-size: 10px;
  color: #6b7280;
  margin: 0 0 3px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: .04em;
}
.app-kpi-value {
  font-size: 17px;
  font-weight: 800;
  margin: 0;
}
.app-kpi-unit {
  font-size: 11px;
  font-weight: 400;
  color: #9ca3af;
}
</style>
