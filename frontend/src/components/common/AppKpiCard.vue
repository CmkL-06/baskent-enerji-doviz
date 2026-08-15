<script setup lang="ts">
defineProps<{
  icon: string
  label: string
  value: string | number
  unit?: string
  color?: string
  bg?: string
  delta?: number | null
  deltaLabel?: string
  tooltip?: string
}>()
</script>

<template>
  <div class="app-kpi" :style="{ '--kc': color ?? 'var(--color-primary)', '--kb': bg ?? 'var(--color-primary-light)' }">
    <div class="app-kpi-icon">
      <span class="material-symbols-outlined" aria-hidden="true">{{ icon }}</span>
    </div>
    <div class="app-kpi-body">
      <p class="app-kpi-label">
        {{ label }}
        <span v-if="tooltip" class="app-kpi-info material-symbols-outlined" aria-hidden="true" :title="tooltip">info</span>
      </p>
      <p class="app-kpi-value" :style="{ color: color ?? '#111' }">{{ value }} <span v-if="unit" class="app-kpi-unit">{{ unit }}</span></p>
      <p v-if="delta !== undefined && delta !== null" class="app-kpi-delta" :class="delta >= 0 ? 'pos' : 'neg'">
        <span class="material-symbols-outlined" aria-hidden="true">{{ delta >= 0 ? 'arrow_upward' : 'arrow_downward' }}</span>
        <span>{{ delta >= 0 ? '+' : '' }}{{ delta.toFixed(1) }}%</span>
        <span v-if="deltaLabel" class="app-kpi-delta-label">{{ deltaLabel }}</span>
      </p>
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
  display: flex;
  align-items: center;
  gap: 3px;
}
.app-kpi-info {
  font-size: 13px;
  color: #9ca3af;
  cursor: help;
  text-transform: none;
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
.app-kpi-delta {
  display: flex;
  align-items: center;
  gap: 3px;
  margin: 4px 0 0;
  font-size: 11px;
  font-weight: 700;
  print-color-adjust: exact;
  -webkit-print-color-adjust: exact;
}
.app-kpi-delta .material-symbols-outlined {
  font-size: 13px;
}
.app-kpi-delta.pos { color: var(--color-success, var(--color-success)); }
.app-kpi-delta.neg { color: var(--color-danger, var(--color-danger)); }
.app-kpi-delta-label {
  font-weight: 400;
  color: var(--color-text-muted, #94a3b8);
  margin-left: 2px;
}
</style>
