<template>
  <div class="taskbar">
    <button class="start-button" @click="toggleStartMenu">
      <span class="start-icon">⊞</span>
      <span>Başlat</span>
    </button>

    <button class="show-desktop-button" @click="showDesktop" title="Masaüstünü Göster">
      <span class="desktop-icon">▭</span>
    </button>

    <div class="taskbar-windows">
      <button
        v-for="window in desktopStore.windows"
        :key="window.id"
        class="taskbar-window"
        :class="{ minimized: window.isMinimized }"
        @click="handleWindowClick(window)"
      >
        <span class="window-icon">{{ getWindowIcon(window.type) }}</span>
        <span class="window-title">{{ window.title }}</span>
      </button>
    </div>

    <div class="system-tray">
      <span class="time">{{ currentTime }}</span>
    </div>

    <!-- Start Menu -->
    <div v-if="startMenuOpen" class="start-menu">
      <div class="start-menu-item" @click="openExchange">
        <span class="menu-icon">💱</span>
        <span>Yeni Döviz İşlemi</span>
      </div>
      <div class="start-menu-item" @click="openHistory">
        <span class="menu-icon">📊</span>
        <span>İşlem Geçmişi</span>
      </div>
      <div class="start-menu-item" @click="openCalculator">
        <span class="menu-icon">🧮</span>
        <span>Hesap Makinesi</span>
      </div>
      <div class="start-menu-item" @click="openSettings">
        <span class="menu-icon">⚙️</span>
        <span>Ayarlar</span>
      </div>
      <div class="start-menu-divider"></div>
      <div class="start-menu-item" @click="refresh">
        <span class="menu-icon">🔄</span>
        <span>Yenile</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useDesktopStore } from '../stores/desktop';
import type { Window } from '../types';

const desktopStore = useDesktopStore();

const startMenuOpen = ref(false);
const currentTime = ref('');

// Update time every second
const updateTime = () => {
  const now = new Date();
  currentTime.value = now.toLocaleTimeString('tr-TR', {
    hour: '2-digit',
    minute: '2-digit',
  });
};

onMounted(() => {
  updateTime();
  const interval = setInterval(updateTime, 1000);
  
  onUnmounted(() => {
    clearInterval(interval);
  });
});

// Window icon mapping
const getWindowIcon = (type: string) => {
  const icons: Record<string, string> = {
    exchange: '💱',
    history: '📊',
    settings: '⚙️',
    calculator: '🧮',
    note: '📝',
  };
  return icons[type] || '📄';
};

// Start menu
const toggleStartMenu = () => {
  startMenuOpen.value = !startMenuOpen.value;
};

const closeStartMenu = () => {
  startMenuOpen.value = false;
};

// Window actions
const handleWindowClick = (window: Window) => {
  if (window.isMinimized) {
    desktopStore.minimizeWindow(window.id); // This will toggle minimize state
    window.isMinimized = false;
  }
  desktopStore.focusWindow(window.id);
};

// Start menu actions
const openExchange = () => {
  desktopStore.openWindow({
    id: 'exchange-window',
    title: 'Yeni Döviz İşlemi',
    type: 'exchange',
    position: { x: 100, y: 100 },
    size: { width: 600, height: 500 },
    isMinimized: false,
    isMaximized: false,
  });
  closeStartMenu();
};

const openHistory = () => {
  desktopStore.openWindow({
    id: 'history-window',
    title: 'İşlem Geçmişi',
    type: 'history',
    position: { x: 150, y: 150 },
    size: { width: 800, height: 600 },
    isMinimized: false,
    isMaximized: false,
  });
  closeStartMenu();
};

const openCalculator = () => {
  desktopStore.openWindow({
    id: 'calculator-window',
    title: 'Hesap Makinesi',
    type: 'calculator',
    position: { x: 250, y: 250 },
    size: { width: 350, height: 450 },
    isMinimized: false,
    isMaximized: false,
  });
  closeStartMenu();
};

const openSettings = () => {
  desktopStore.openWindow({
    id: 'settings-window',
    title: 'Ayarlar',
    type: 'settings',
    position: { x: 200, y: 200 },
    size: { width: 500, height: 400 },
    isMinimized: false,
    isMaximized: false,
  });
  closeStartMenu();
};

const refresh = () => {
  window.location.reload();
};

// Show desktop functionality
const showDesktop = () => {
  // Minimize all windows
  desktopStore.windows.forEach(window => {
    if (!window.isMinimized) {
      desktopStore.minimizeWindow(window.id);
    }
  });
};

// Close start menu when clicking outside
onMounted(() => {
  const handleClickOutside = (event: MouseEvent) => {
    const target = event.target as HTMLElement;
    if (!target.closest('.start-button') && !target.closest('.start-menu')) {
      closeStartMenu();
    }
  };
  
  document.addEventListener('click', handleClickOutside);
  
  onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside);
  });
});
</script>

<style scoped>
.taskbar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  height: 40px;
  background: linear-gradient(180deg, #f0f0f0 0%, #d0d0d0 100%);
    background: linear-gradient(90deg, #1b1b1ad0 80%, #000000 100%);
  border-top: 4px solid #585858;
  display: flex;
  align-items: center;
  padding: 0 4px;
  z-index: 9999;
}

.start-button {
  height: 32px;
  padding: 0 16px;
  border: none;
 background: linear-gradient(180deg, #000000 85%, #d9ff00 100%);
  border-radius: 4px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 17px;
  font-weight: 600;
  color: #ffffff;
  transition: all 0.2s;
}

.start-button:hover {
  background: linear-gradient(180deg, #e5ff00d0 80%, #000000 100%);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  color:black;
}

.start-button:active {
  background: linear-gradient(180deg, #e0e0e0 0%, #c0c0c0 100%);
}
.start-button:hover .start-icon {
  color:black;
}

.start-icon {
  font-size: 18px;
  color: #ffffff;
}

.show-desktop-button {
  height: 32px;
  width: 40px;
  border: none;
  background: linear-gradient(180deg, #000000 85%, #d9ff00 100%);
  border-radius: 4px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  color: white;
  transition: all 0.2s;
  margin-left: 4px;
}

.show-desktop-button:hover {
   background: linear-gradient(180deg, #e5ff00d0 80%, #000000 100%);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  color: black;
}

.show-desktop-button:active {
  background: linear-gradient(180deg, #e0e0e0 0%, #c0c0c0 100%);
}

.desktop-icon {
  font-size: 24px;
  line-height: 1;
}

.taskbar-windows {
  flex: 1;
  display: flex;
  gap: 2px;
  margin: 0 8px;
  overflow-x: auto;
}

.taskbar-window {
  height: 32px;
  min-width: 150px;
  max-width: 200px;
  padding: 0 12px;
  border: none;
 background: linear-gradient(180deg, #000000 85%, #d9ff00 100%);
  border-radius: 4px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: white;
  transition: all 0.2s;
  font-weight: 600;
}

.taskbar-window:hover {
   background: linear-gradient(180deg, #e5ff00d0 80%, #000000 100%);
   color:black;
}

.taskbar-window.minimized {
  background: linear-gradient(180deg, #e5ff00d0 80%, #000000 100%);
  color:black;
}

.window-icon {
  font-size: 16px;
}

.window-title {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.system-tray {
  display: flex;
  align-items: center;
  padding: 0 12px;
  height: 100%;
  border-left: 1px solid #ccc;
}

.time {
  font-size: 13px;
  color: #ffffff;
}

.start-menu {
  position: absolute;
  bottom: 100%;
  left: 4px;
  width: 300px;
  background: white;
  border: 1px solid #ccc;
  border-radius: 4px 4px 0 0;
  box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.15);
  padding: 8px 0;
  margin-bottom: -1px;
}

.start-menu-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 16px;
  cursor: pointer;
  transition: background-color 0.2s;
  font-size: 14px;
  color: #242121;
}

.start-menu-item:hover {
  background-color: #288fd8;
}

.menu-icon {
  font-size: 20px;
  width: 24px;
  text-align: center;
}

.start-menu-divider {
  height: 1px;
  background-color: #e0e0e0;
  margin: 8px 0;
}
</style>