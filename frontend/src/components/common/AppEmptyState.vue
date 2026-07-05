<script setup lang="ts">
import { useRouter } from 'vue-router'

const props = defineProps<{
  icon: string
  message: string
  actionLabel?: string
  actionRoute?: string
}>()

const router = useRouter()

function go() {
  if (props.actionRoute) router.push(props.actionRoute)
}
</script>

<template>
  <div class="app-empty">
    <span class="material-symbols-outlined">{{ icon }}</span>
    <p>{{ message }}</p>
    <button v-if="actionLabel" class="app-empty-btn" @click="go">{{ actionLabel }}</button>
    <slot />
  </div>
</template>

<style scoped>
.app-empty {
  text-align: center;
  padding: 48px 20px;
  color: #94a3b8;
}
.app-empty .material-symbols-outlined {
  font-size: 48px;
  display: block;
  margin-bottom: 8px;
}
.app-empty p {
  font-size: 14px;
  margin: 0 0 12px;
}
.app-empty-btn {
  background: #6366f1;
  color: #fff;
  border: none;
  border-radius: 8px;
  padding: 8px 20px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: background .2s;
}
.app-empty-btn:hover {
  background: #4f46e5;
}
</style>
