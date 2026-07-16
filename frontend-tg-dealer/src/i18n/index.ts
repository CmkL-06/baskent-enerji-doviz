import { useI18n } from 'vue-i18n'

export function changeLanguage(lang: string) {
  const { locale } = useI18n()
  locale.value = lang
  localStorage.setItem('lang', lang)
}
