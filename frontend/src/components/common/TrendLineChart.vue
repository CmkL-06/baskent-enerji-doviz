<script setup lang="ts">
import { computed } from 'vue'
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS, CategoryScale, LinearScale, PointElement, LineElement,
  Title, Tooltip, Legend, Filler,
} from 'chart.js'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend, Filler)

const props = defineProps<{
  labels: string[]
  profitSeries: number[]
  volumeSeries: number[]
}>()

const chartData = computed(() => ({
  labels: props.labels,
  datasets: [
    {
      label: 'Kâr (₺)',
      data: props.profitSeries,
      borderColor: '#10b981',
      backgroundColor: 'rgba(16,185,129,0.12)',
      fill: true,
      tension: 0.35,
      pointRadius: 3,
      yAxisID: 'y',
    },
    {
      label: 'İşlem Hacmi (₺)',
      data: props.volumeSeries,
      borderColor: '#0ea5e9',
      backgroundColor: 'rgba(14,165,233,0.08)',
      fill: true,
      tension: 0.35,
      pointRadius: 3,
      yAxisID: 'y1',
    },
  ],
}))

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index' as const, intersect: false },
  plugins: {
    legend: { position: 'bottom' as const, labels: { boxWidth: 10, font: { size: 11 } } },
  },
  scales: {
    y: { position: 'left' as const, ticks: { font: { size: 10 } } },
    y1: { position: 'right' as const, grid: { drawOnChartArea: false }, ticks: { font: { size: 10 } } },
    x: { ticks: { font: { size: 10 } } },
  },
}
</script>

<template>
  <div class="trend-chart-wrap">
    <Line :data="chartData" :options="chartOptions" />
  </div>
</template>

<style scoped>
.trend-chart-wrap { height: 220px; }
</style>
