<script setup lang="ts">
import { computed } from 'vue'
import { useNotification } from '@/composables/useNotification'
import { TransitionGroup } from 'vue'

const { notifications, remove } = useNotification()

const getPositionClasses = (position: string) => {
  const classes: Record<string, string> = {
    'top-right': 'top-right',
    'top-left': 'top-left',
    'bottom-right': 'bottom-right',
    'bottom-left': 'bottom-left',
    'top-center': 'top-center',
    'bottom-center': 'bottom-center'
  }
  return classes[position] || classes['top-right']
}

const getTypeClasses = (type: string) => {
  const classes: Record<string, string> = {
    'success': 'notification-success',
    'error': 'notification-error',
    'warning': 'notification-warning',
    'info': 'notification-info'
  }
  return classes[type] || classes['info']
}

const getIcon = (type: string) => {
  const icons: Record<string, string> = {
    'success': 'check_circle',
    'error': 'error',
    'warning': 'warning',
    'info': 'info'
  }
  return icons[type] || icons['info']
}

const groupedNotifications = computed(() => {
  const groups: Record<string, typeof notifications.value> = {}
  
  notifications.value.forEach(notification => {
    const position = notification.position
    if (!groups[position]) {
      groups[position] = []
    }
    groups[position].push(notification)
  })
  
  return groups
})
</script>

<template>
  <div class="notification-container">
    <div 
      v-for="(group, position) in groupedNotifications" 
      :key="position"
      :class="['notification-group', getPositionClasses(position as string)]"
    >
      <TransitionGroup name="notification" tag="div" class="notification-list">
        <div
          v-for="notification in group"
          :key="notification.id"
          :class="['notification', getTypeClasses(notification.type)]"
        >
          <div class="notification-content">
            <div class="notification-icon">
              <span class="material-symbols-outlined">
                {{ getIcon(notification.type) }}
              </span>
            </div>
            <div class="notification-text">
              <h3 v-if="notification.title" class="notification-title">
                {{ notification.title }}
              </h3>
              <p class="notification-message">
                {{ notification.message }}
              </p>
            </div>
            <button
              @click="remove(notification.id)"
              class="notification-close"
              aria-label="Zamknij powiadomienie"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>
        </div>
      </TransitionGroup>
    </div>
  </div>
</template>

<style scoped>
.notification-container {
  pointer-events: none;
  z-index: 9999;
}

.notification-group {
  position: fixed;
  pointer-events: auto;
  z-index: 9999;
}

.notification-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

/* Position classes */
.notification-group.top-right {
  top: 20px;
  right: 20px;
}

.notification-group.top-left {
  top: 20px;
  left: 20px;
}

.notification-group.bottom-right {
  bottom: 20px;
  right: 20px;
}

.notification-group.bottom-left {
  bottom: 20px;
  left: 20px;
}

.notification-group.top-center {
  top: 20px;
  left: 50%;
  transform: translateX(-50%);
}

.notification-group.bottom-center {
  bottom: 20px;
  left: 50%;
  transform: translateX(-50%);
}

.notification {
  background: white;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
  border-radius: 8px;
  border: 1px solid;
  padding: 16px;
  min-width: 300px;
  max-width: 400px;
  backdrop-filter: blur(10px);
}

/* Type-specific styles */
.notification-success {
  background-color: #f0fdf4;
  border-color: #bbf7d0;
  color: #166534;
}

.notification-error {
  background-color: #fef2f2;
  border-color: #fecaca;
  color: #991b1b;
}

.notification-warning {
  background-color: #fefce8;
  border-color: #fef08a;
  color: #854d0e;
}

.notification-info {
  background-color: #eff6ff;
  border-color: #bfdbfe;
  color: #1e40af;
}

.notification-content {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.notification-icon {
  flex-shrink: 0;
}

.notification-icon .material-symbols-outlined {
  font-size: 24px;
}

.notification-success .notification-icon .material-symbols-outlined {
  color: #059669;
}

.notification-error .notification-icon .material-symbols-outlined {
  color: #dc2626;
}

.notification-warning .notification-icon .material-symbols-outlined {
  color: #ca8a04;
}

.notification-info .notification-icon .material-symbols-outlined {
  color: #2563eb;
}

.notification-text {
  flex-grow: 1;
}

.notification-title {
  font-weight: 600;
  font-size: 15px;
  margin: 0 0 4px 0;
}

.notification-message {
  font-size: 14px;
  line-height: 1.5;
  margin: 0;
}

.notification-close {
  flex-shrink: 0;
  padding: 4px;
  border-radius: 4px;
  background: transparent;
  border: none;
  cursor: pointer;
  transition: background-color 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.notification-close:hover {
  background-color: rgba(0, 0, 0, 0.1);
}

.notification-close .material-symbols-outlined {
  font-size: 18px;
  color: currentColor;
  opacity: 0.6;
}

/* Animation classes */
.notification-enter-active,
.notification-leave-active {
  transition: all 0.3s ease;
}

.notification-enter-from {
  transform: translateX(100%);
  opacity: 0;
}

.notification-leave-to {
  transform: translateX(100%);
  opacity: 0;
}

/* For left-positioned notifications */
.notification-group.top-left .notification-enter-from,
.notification-group.top-left .notification-leave-to,
.notification-group.bottom-left .notification-enter-from,
.notification-group.bottom-left .notification-leave-to {
  transform: translateX(-100%);
}

/* For center-positioned notifications */
.notification-group.top-center .notification-enter-from,
.notification-group.top-center .notification-leave-to,
.notification-group.bottom-center .notification-enter-from,
.notification-group.bottom-center .notification-leave-to {
  transform: translateX(-50%) translateY(-20px);
}

/* Responsive */
@media (max-width: 640px) {
  .notification {
    min-width: calc(100vw - 40px);
    max-width: calc(100vw - 40px);
  }
  
  .notification-group.top-right,
  .notification-group.top-left,
  .notification-group.bottom-right,
  .notification-group.bottom-left {
    right: 20px;
    left: 20px;
  }
  
  .notification-group.top-center,
  .notification-group.bottom-center {
    left: 50%;
    transform: translateX(-50%);
  }
}
</style>