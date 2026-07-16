<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
onMounted(() => router.replace({ name: 'VaultCounts' }))
</script>

<template>
  <div style="padding:40px;text-align:center;color:#9ca3af">Yönlendiriliyor...</div>
</template>
