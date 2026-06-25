import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { DesktopIcon, WindowState } from '@/types'

export const useDesktopStore = defineStore('desktop', () => {
  const icons = ref<DesktopIcon[]>([
    { id: 'dashboard',  type: 'app', title: 'Dashboard',        icon: 'dashboard',          position: { x: 20,  y: 20  } },
    { id: 'exchange',   type: 'app', title: 'Döviz İşlemi',     icon: 'currency_exchange',  position: { x: 20,  y: 120 } },
    { id: 'history',    type: 'app', title: 'İşlem Geçmişi',    icon: 'history',            position: { x: 20,  y: 220 } },
    { id: 'vault',      type: 'app', title: 'Kasa Yönetimi',    icon: 'account_balance_wallet', position: { x: 20, y: 320 } },
    { id: 'party',      type: 'app', title: 'Cari Yönetimi',    icon: 'contacts',           position: { x: 20,  y: 420 } },
    { id: 'office',     type: 'app', title: 'Ofis Yönetimi',    icon: 'store',              position: { x: 20,  y: 520 } },
    { id: 'reports',    type: 'app', title: 'Raporlar',         icon: 'insert_chart',       position: { x: 20,  y: 620 } },
    { id: 'calculator', type: 'app', title: 'Hesap Makinesi',   icon: 'calculate',          position: { x: 120, y: 20  } },
    { id: 'settings',   type: 'app', title: 'Ayarlar',          icon: 'settings',           position: { x: 120, y: 120 } },
  ])

  const activeWindows  = ref<WindowState[]>([])
  const selectedIconId = ref<string | null>(null)

  let _zTop = 10

  function openWindow(config: Omit<WindowState, 'zIndex'> & { zIndex?: number }) {
    const existing = activeWindows.value.find(w => w.id === config.id)
    if (existing) {
      existing.isMinimized = false
      focusWindow(config.id)
      return
    }
    activeWindows.value.push({ ...config, zIndex: ++_zTop })
  }

  function closeWindow(id: string) {
    const idx = activeWindows.value.findIndex(w => w.id === id)
    if (idx >= 0) activeWindows.value.splice(idx, 1)
  }

  function minimizeWindow(id: string) {
    const w = activeWindows.value.find(w => w.id === id)
    if (w) w.isMinimized = true
  }

  function maximizeWindow(id: string) {
    const w = activeWindows.value.find(w => w.id === id)
    if (w) { w.isMaximized = !w.isMaximized; focusWindow(id) }
  }

  function focusWindow(id: string) {
    const w = activeWindows.value.find(w => w.id === id)
    if (w) w.zIndex = ++_zTop
  }

  function updateWindowPosition(id: string, position: { x: number; y: number }) {
    const w = activeWindows.value.find(w => w.id === id)
    if (w) w.position = position
  }

  function updateWindowSize(id: string, size: { width: number; height: number }) {
    const w = activeWindows.value.find(w => w.id === id)
    if (w) w.size = size
  }

  function updateIconPosition(id: string, position: { x: number; y: number }) {
    const icon = icons.value.find(i => i.id === id)
    if (icon) icon.position = position
  }

  function selectIcon(id: string | null) {
    selectedIconId.value = id
  }

  return {
    icons, activeWindows, selectedIconId,
    openWindow, closeWindow, minimizeWindow, maximizeWindow, focusWindow,
    updateWindowPosition, updateWindowSize, updateIconPosition, selectIcon
  }
})
