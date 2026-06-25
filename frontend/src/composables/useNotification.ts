import { ref } from 'vue'

export type NotificationType = 'success' | 'error' | 'warning' | 'info'

export interface Notification {
  id: string
  type: NotificationType
  message: string
  duration?: number
}

const notifications = ref<Notification[]>([])

export function useNotification() {
  function notify(n: Omit<Notification, 'id'>) {
    const id = Math.random().toString(36).slice(2)
    notifications.value.push({ id, duration: 4000, ...n })
    setTimeout(() => dismiss(id), n.duration ?? 4000)
  }

  function dismiss(id: string) {
    const idx = notifications.value.findIndex(n => n.id === id)
    if (idx >= 0) notifications.value.splice(idx, 1)
  }

  const success = (message: string) => notify({ type: 'success', message })
  const error   = (message: string) => notify({ type: 'error',   message })
  const warning = (message: string) => notify({ type: 'warning', message })
  const info    = (message: string) => notify({ type: 'info',    message })

  return { notifications, notify, dismiss, success, error, warning, info }
}
