<template>
  <div
    class="context-menu"
    :style="{ left: `${position.x}px`, top: `${position.y}px` }"
    @click.stop
  >
    <div
      v-for="item in items"
      :key="item.id"
      class="menu-item"
      :class="{ divider: item.divider }"
      @click="handleItemClick(item)"
    >
      <span v-if="item.icon" class="menu-icon">{{ item.icon }}</span>
      <span v-if="!item.divider" class="menu-label">{{ item.label }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import type { ContextMenuItem } from '../types';

const props = defineProps<{
  items: ContextMenuItem[];
  position: { x: number; y: number };
}>();

const emit = defineEmits<{
  close: [];
}>();

const handleItemClick = (item: ContextMenuItem) => {
  if (!item.divider) {
    item.action();
    emit('close');
  }
};

const handleClickOutside = () => {
  emit('close');
};

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});
</script>

<style scoped>
.context-menu {
  position: fixed;
  background: white;
  border: 1px solid #ccc;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  padding: 4px 0;
  min-width: 200px;
  z-index: 10000;
}

.menu-item {
  display: flex;
  align-items: center;
  padding: 8px 16px;
  cursor: pointer;
  transition: background-color 0.2s;
  font-size: 14px;
  color: #333;
}

.menu-item:hover:not(.divider) {
  background-color: #e3f2fd;
}

.menu-item.divider {
  height: 1px;
  background-color: #e0e0e0;
  margin: 4px 0;
  padding: 0;
  cursor: default;
}

.menu-icon {
  margin-right: 12px;
  font-size: 18px;
  width: 24px;
  text-align: center;
}

.menu-label {
  flex: 1;
}
</style>