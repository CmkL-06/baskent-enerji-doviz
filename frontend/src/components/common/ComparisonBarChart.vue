<script setup lang="ts">
import { computed } from 'vue'
import { Bar } from 'vue-chartjs'
import {
  Chart as ChartJS, CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend,
} from 'chart.js'

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend)

const props = defineProps<{
  labels: string[]
  datasets: { label: string; data: number[]; color: string }[]
  horizontal?: boolean
}>()

const chartData = computed(() => ({
  labels: props.labels,
  datasets: props.datasets.map(d => ({
    label: d.label,
    data: d.data,
    backgroundColor: d.color,
    borderRadius: 4,
  })),
}))

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  indexAxis: (props.horizontal ? 'y' : 'x') as 'y' | 'x',
  plugins: {
    legend: { position: 'bottom' as const, labels: { boxWidth: 10, font: { size: 11 } } },
  },
  scales: {
    x: { ticks: { font: { size: 10 } } },
    y: { ticks: { font: { size: 10 } } },
  },
}))
</script>

<template>
  <div class="bar-chart-wrap">
    <Bar :data="chartData" :options="chartOptions" />
  </div>
</template>

<style scoped>
.bar-chart-wrap { height: 220px; }
</style>
