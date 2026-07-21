<script setup lang="ts">
import { computed } from 'vue'
import { Doughnut } from 'vue-chartjs'
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js'

ChartJS.register(ArcElement, Tooltip, Legend)

const props = defineProps<{
  items: { code: string; amount: number }[]
}>()

// Canvas 2D context CSS custom property'lerini ("var(--x)") çözemez — burada geçirilirse
// tarayıcı geçersiz fillStyle'ı siyaha düşürür. Bu yüzden --color-primary'nin ham hex
// karşılığı kullanılıyor (design-tokens.css'teki değerle birebir aynı).
const palette = ['#6366f1', '#0ea5e9', '#10b981', '#f59e0b', '#ec4899', '#8b5cf6', '#ef4444', '#14b8a6']

const chartData = computed(() => ({
  labels: props.items.map(i => i.code),
  datasets: [
    {
      data: props.items.map(i => i.amount),
      backgroundColor: props.items.map((_, i) => palette[i % palette.length]),
      borderWidth: 0,
    },
  ],
}))

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '65%',
  plugins: {
    legend: { position: 'right' as const, labels: { boxWidth: 10, font: { size: 11 } } },
  },
}
</script>

<template>
  <div class="donut-chart-wrap">
    <Doughnut :data="chartData" :options="chartOptions" />
  </div>
</template>

<style scoped>
.donut-chart-wrap { height: 200px; }
</style>
