import { ref } from 'vue'

export type NotificationType = 'success' | 'error' | 'warning' | 'info'

export interface Notification {
  id: string
  type: NotificationType
  message: string
  duration?: number
  title?: string
  position?: string
}

export interface NotifyOptions {
  duration?: number
  title?: string
  position?: string
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

  const remove = dismiss

  const success = (message: string, opts?: NotifyOptions) => notify({ type: 'success', message, ...opts })
  const error   = (message: string, opts?: NotifyOptions) => notify({ type: 'error',   message, ...opts })
  const warning = (message: string, opts?: NotifyOptions) => notify({ type: 'warning', message, ...opts })
  const info    = (message: string, opts?: NotifyOptions) => notify({ type: 'info',    message, ...opts })

  const exchangeSuccess = (message?: string) => success(message || 'İşlem başarıyla tamamlandı')
  const printSuccess    = (message?: string) => success(message || 'Yazdırma işlemi başlatıldı')

  return { notifications, notify, dismiss, remove, success, error, warning, info, exchangeSuccess, printSuccess }
}
