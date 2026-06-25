<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { changeLanguage } from '@/i18n'

const { locale } = useI18n()
const isOpen = ref(false)

const languages = [
  { code: 'tr', name: 'Türkçe', flag: 'tr' },
  { code: 'en', name: 'English', flag: 'gb' },
  { code: 'ru', name: 'Русский', flag: 'ru' }
]

const currentLanguage = computed(() => {
  return languages.find(lang => lang.code === locale.value) || languages[0]
})

const selectLanguage = (lang: string) => {
  changeLanguage(lang)
  isOpen.value = false
}
</script>

<template>
  <div class="relative">
    <button
      @click="isOpen = !isOpen"
      class="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-gray-100 transition-colors"
    >
      <i :class="`fi fi-${currentLanguage.flag} text-lg`"></i>
      <span class="text-sm font-medium hidden sm:inline">{{ currentLanguage.name }}</span>
      <span class="material-symbols-outlined text-sm">
        {{ isOpen ? 'expand_less' : 'expand_more' }}
      </span>
    </button>
    
    <!-- Dropdown Menu -->
    <div
      v-if="isOpen"
      class="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 z-50 overflow-hidden"
    >
      <button
        v-for="lang in languages"
        :key="lang.code"
        @click="selectLanguage(lang.code)"
        class="w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors"
        :class="{ 'bg-purple-50': lang.code === locale }"
      >
        <i :class="`fi fi-${lang.flag} text-lg`"></i>
        <span class="font-medium">{{ lang.name }}</span>
        <span v-if="lang.code === locale" class="ml-auto material-symbols-outlined text-purple-600 text-sm">
          check
        </span>
      </button>
    </div>
  </div>
</template>