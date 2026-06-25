export interface ContextMenuItem {
  id: string
  label: string
  icon?: string
  divider?: boolean
  action: () => void
}

export interface DesktopIcon {
  id: string
  type: 'app' | 'file' | 'folder'
  title: string
  icon: string
  position: { x: number; y: number }
}

export interface WindowState {
  id: string
  title: string
  type: string
  position: { x: number; y: number }
  size: { width: number; height: number }
  isMinimized: boolean
  isMaximized: boolean
  zIndex?: number
  data?: any
}
