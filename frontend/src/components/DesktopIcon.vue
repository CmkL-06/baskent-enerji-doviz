<template>
  <div
    class="desktop-icon"
    :class="{ selected }"
    :style="{ left: `${icon.position.x}px`, top: `${icon.position.y}px` }"
    @mousedown="startDrag"
    @dblclick="handleDoubleClick"
    @click="handleClick"
  >
    <div class="icon-image">{{ icon.icon }}</div>
    <div class="icon-label">{{ icon.title }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { DesktopIcon } from '../types';

const props = defineProps<{
  icon: DesktopIcon;
  selected: boolean;
}>();

const emit = defineEmits<{
  open: [icon: DesktopIcon];
  'update-position': [id: string, position: { x: number; y: number }];
  select: [id: string];
}>();

const isDragging = ref(false);
const dragOffset = ref({ x: 0, y: 0 });

const handleClick = (event: MouseEvent) => {
  event.stopPropagation();
  emit('select', props.icon.id);
};

const handleDoubleClick = () => {
  emit('open', props.icon);
};

const startDrag = (event: MouseEvent) => {
  if (event.button !== 0) return; // Only left click
  
  isDragging.value = true;
  const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
  dragOffset.value = {
    x: event.clientX - rect.left,
    y: event.clientY - rect.top,
  };
  
  emit('select', props.icon.id);
  
  document.addEventListener('mousemove', handleDrag);
  document.addEventListener('mouseup', stopDrag);
};

const handleDrag = (event: MouseEvent) => {
  if (!isDragging.value) return;
  
  const newX = event.clientX - dragOffset.value.x;
  const newY = event.clientY - dragOffset.value.y;
  
  // Snap to grid (100px)
  const snappedX = Math.round(newX / 100) * 100;
  const snappedY = Math.round(newY / 100) * 100;
  
  // Keep within desktop bounds
  const maxX = window.innerWidth - 100;
  const maxY = window.innerHeight - 150; // Account for taskbar
  
  emit('update-position', props.icon.id, {
    x: Math.max(0, Math.min(snappedX, maxX)),
    y: Math.max(0, Math.min(snappedY, maxY)),
  });
};

const stopDrag = () => {
  isDragging.value = false;
  document.removeEventListener('mousemove', handleDrag);
  document.removeEventListener('mouseup', stopDrag);
};
</script>

<style scoped>
.desktop-icon {
  position: absolute;
  width: 80px;
  padding: 10px;
  display: flex;
  flex-direction: column;
  align-items: center;
  cursor: pointer;
  border-radius: 4px;
  transition: background-color 0.2s;
}

.desktop-icon:hover {
  background-color: rgba(255, 255, 255, 0.1);
}

.desktop-icon.selected {
  background-color: rgba(255, 255, 255, 0.2);
  outline: 1px solid rgba(255, 255, 255, 0.5);
}

.icon-image {
  font-size: 48px;
  line-height: 1;
  margin-bottom: 5px;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.3));
}

.icon-label {
  font-size: 12px;
  color: white;
  text-align: center;
  text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.8);
  word-break: break-word;
  max-width: 100%;
}
</style>