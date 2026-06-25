import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createI18n } from 'vue-i18n'
import App from './App.vue'
import router from './router'
import tr from './i18n/tr.json'
import en from './i18n/en.json'
import 'flag-icons/css/flag-icons.min.css'

const i18n = createI18n({
  legacy: false,
  locale: localStorage.getItem('lang') || 'tr',
  fallbackLocale: 'en',
  messages: { tr, en }
})

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(i18n)
app.mount('#app')
