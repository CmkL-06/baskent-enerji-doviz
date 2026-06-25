<template>
  <div
    class="window"
    :class="{ maximized: window.isMaximized }"
    :style="windowStyle"
    @mousedown="handleFocus"
  >
    <!-- Title Bar -->
    <div class="window-titlebar" @mousedown="startDrag" @dblclick="handleDoubleClick">
      <span class="window-title">{{ window.title }}</span>
      <div class="window-controls">
        <button class="control-button minimize" @click="handleMinimize" title="Minimize">
          <svg width="14" height="14" viewBox="0 0 14 14" fill="currentColor">
            <rect x="2" y="11" width="10" height="1" />
          </svg>
        </button>
        <button class="control-button maximize" @click="handleMaximize" title="Maximize">
          <svg v-if="!window.isMaximized" width="14" height="14" viewBox="0 0 14 14" fill="currentColor">
            <rect x="2" y="2" width="10" height="10" fill="none" stroke="currentColor" stroke-width="1.5" />
          </svg>
          <svg v-else width="14" height="14" viewBox="0 0 14 14" fill="currentColor">
            <path d="M4 2h6v2H6v4H4V2z" fill="none" stroke="currentColor" stroke-width="1.5" />
            <rect x="2" y="4" width="8" height="8" fill="none" stroke="currentColor" stroke-width="1.5" />
          </svg>
        </button>
        <button class="control-button close" @click="handleClose" title="Close">
          <svg width="14" height="14" viewBox="0 0 14 14" fill="currentColor">
            <path d="M3.5 3.5l7 7m0-7l-7 7" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Window Content -->
    <div class="window-content">
      <slot></slot>
    </div>

    <!-- Resize Handle -->
    <div
      v-if="!window.isMaximized"
      class="resize-handle"
      @mousedown="startResize"
    ></div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import type { Window } from '../types';

const props = defineProps<{
  window: Window;
}>();

const emit = defineEmits<{
  close: [id: string];
  minimize: [id: string];
  maximize: [id: string];
  focus: [id: string];
  'update-position': [id: string, position: { x: number; y: number }];
  'update-size': [id: string, size: { width: number; height: number }];
}>();

// Dragging state
const isDragging = ref(false);
const dragOffset = ref({ x: 0, y: 0 });

// Resizing state
const isResizing = ref(false);
const resizeStart = ref({ x: 0, y: 0, width: 0, height: 0 });

// Computed styles
const windowStyle = computed(() => {
  if (props.window.isMaximized) {
    return {
      left: '0px',
      top: '0px',
      width: '100vw',
      height: 'calc(100vh - 40px)', // Account for taskbar
      zIndex: props.window.zIndex,
    };
  }
  
  return {
    left: `${props.window.position.x}px`,
    top: `${props.window.position.y}px`,
    width: `${props.window.size.width}px`,
    height: `${props.window.size.height}px`,
    zIndex: props.window.zIndex,
  };
});

// Window controls
const handleClose = () => {
  emit('close', props.window.id);
};

const handleMinimize = () => {
  emit('minimize', props.window.id);
};

const handleMaximize = () => {
  emit('maximize', props.window.id);
};

const handleFocus = () => {
  emit('focus', props.window.id);
};

// Double-click on title bar to maximize/restore
const handleDoubleClick = (event: MouseEvent) => {
  // Prevent double-click on control buttons
  if ((event.target as HTMLElement).closest('.window-controls')) return;
  
  handleMaximize();
};

// Dragging functionality
const startDrag = (event: MouseEvent) => {
  if (props.window.isMaximized) return;
  if (event.button !== 0) return;
  // Prevent dragging when clicking on control buttons
  if ((event.target as HTMLElement).closest('.window-controls')) return;
  
  isDragging.value = true;
  dragOffset.value = {
    x: event.clientX - props.window.position.x,
    y: event.clientY - props.window.position.y,
  };
  
  document.addEventListener('mousemove', handleDrag);
  document.addEventListener('mouseup', stopDrag);
  
  event.preventDefault();
};

const handleDrag = (event: MouseEvent) => {
  if (!isDragging.value) return;
  
  const newX = event.clientX - dragOffset.value.x;
  const newY = event.clientY - dragOffset.value.y;
  
  // Keep window within bounds
  const maxX = window.innerWidth - props.window.size.width;
  const maxY = window.innerHeight - props.window.size.height - 40; // Account for taskbar
  
  emit('update-position', props.window.id, {
    x: Math.max(0, Math.min(newX, maxX)),
    y: Math.max(0, Math.min(newY, maxY)),
  });
};

const stopDrag = () => {
  isDragging.value = false;
  document.removeEventListener('mousemove', handleDrag);
  document.removeEventListener('mouseup', stopDrag);
};

// Resizing functionality
const startResize = (event: MouseEvent) => {
  if (event.button !== 0) return;
  
  isResizing.value = true;
  resizeStart.value = {
    x: event.clientX,
    y: event.clientY,
    width: props.window.size.width,
    height: props.window.size.height,
  };
  
  document.addEventListener('mousemove', handleResize);
  document.addEventListener('mouseup', stopResize);
  
  event.preventDefault();
};

const handleResize = (event: MouseEvent) => {
  if (!isResizing.value) return;
  
  const deltaX = event.clientX - resizeStart.value.x;
  const deltaY = event.clientY - resizeStart.value.y;
  
  const newWidth = Math.max(300, resizeStart.value.width + deltaX);
  const newHeight = Math.max(200, resizeStart.value.height + deltaY);
  
  emit('update-size', props.window.id, {
    width: newWidth,
    height: newHeight,
  });
};

const stopResize = () => {
  isResizing.value = false;
  document.removeEventListener('mousemove', handleResize);
  document.removeEventListener('mouseup', stopResize);
};
</script>

<style scoped>
.window {
  position: absolute;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1), 0 0 0 1px rgba(0, 0, 0, 0.05);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: none;
}

.window.maximized {
  border-radius: 0;
  border: none;
}

/* Add focus state for active window */
.window:has(.window-titlebar:focus-within) {
  box-shadow: 0 15px 35px rgba(0, 0, 0, 0.15), 0 0 0 1px rgba(102, 126, 234, 0.5);
}

.window-titlebar {
  height: 40px;
  background: linear-gradient(180deg, #ffffff 0%, #f8f9fa 100%);
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  padding: 0 12px;
  cursor: move;
  user-select: none;
  backdrop-filter: blur(10px);
}

.window.maximized .window-titlebar {
  cursor: default;
}

.window-title {
  flex: 1;
  font-size: 14px;
  font-weight: 600;
  color: #2d3748;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  letter-spacing: -0.01em;
}

.window-controls {
  display: flex;
  gap: 8px;
  margin-left: 16px;
}

.control-button {
  width: 46px;
  height: 30px;
  border: none;
  background: rgba(0, 0, 0, 0.04);
  cursor: pointer;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  color: #4a5568;
  transition: all 0.2s ease;
  position: relative;
  font-weight: 600;
}

.control-button:hover {
  background-color: rgba(0, 0, 0, 0.08);
  transform: translateY(-1px);
}

.control-button.minimize:hover {
  background-color: #fbbf24;
  color: white;
}

.control-button.maximize:hover {
  background-color: #34d399;
  color: white;
}

.control-button.close:hover {
  background-color: #ef4444;
  color: white;
}

.control-button svg {
  width: 14px;
  height: 14px;
  pointer-events: none;
}

.control-button:active {
  transform: scale(0.95);
}

/* Add subtle animations for each button type */
.control-button.minimize {
  background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
  color: #92400e;
}

.control-button.maximize {
  background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
  color: #065f46;
}

.control-button.close {
  background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);
  color: #991b1b;
}

/* Add tooltips styling */
.control-button::after {
  content: attr(title);
  position: absolute;
  bottom: -30px;
  left: 50%;
  transform: translateX(-50%) scale(0.8);
  background: rgba(0, 0, 0, 0.8);
  color: white;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 500;
  white-space: nowrap;
  opacity: 0;
  pointer-events: none;
  transition: all 0.2s ease;
}

.control-button:hover::after {
  opacity: 1;
  transform: translateX(-50%) scale(1);
}

.window-content {
  flex: 1;
  overflow: auto;
  background: #f5f5f5;
}

.resize-handle {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 20px;
  height: 20px;
  cursor: nwse-resize;
  background: transparent;
  border-radius: 0 0 12px 0;
}

.resize-handle::before {
  content: '';
  position: absolute;
  bottom: 4px;
  right: 4px;
  width: 10px;
  height: 10px;
  background: linear-gradient(135deg, transparent 50%, #cbd5e0 50%);
  border-radius: 0 0 8px 0;
  transition: background 0.2s ease;
}

.resize-handle:hover::before {
  background: linear-gradient(135deg, transparent 50%, #94a3b8 50%);
}
</style>