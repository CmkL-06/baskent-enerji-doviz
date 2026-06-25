<template>
  <span class="currency-icon-wrapper">
    <!-- For fiat currencies, use flag-icons -->
    <span 
      v-if="countryCode && !isCrypto && !isMetal" 
      :class="`fi fi-${countryCode}`"
      :title="currencyName"
    ></span>
    <!-- For cryptocurrencies with SVG icons -->
    <img
      v-else-if="isCrypto && hasCryptoIcon"
      :src="cryptoIconUrl"
      :alt="currencyCode"
      :title="currencyName"
      class="crypto-icon"
    />
    <!-- For other currencies and metals, show symbol/icon -->
    <span 
      v-else 
      :class="['currency-symbol', { crypto: isCrypto, metal: isMetal }]"
      :title="currencyName"
    >
      {{ symbol }}
    </span>
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { getCurrencyCountryCode, getCurrencyFlag, getCurrencyName, isCryptoCurrency, isMetalCurrency } from '@/utils/currency'

const props = defineProps<{
  currencyCode: string
}>()

const countryCode = computed(() => getCurrencyCountryCode(props.currencyCode))
const isCrypto = computed(() => isCryptoCurrency(props.currencyCode))
const isMetal = computed(() => isMetalCurrency(props.currencyCode))
const currencyName = computed(() => getCurrencyName(props.currencyCode))

const symbol = computed(() => {
  // Get the emoji/symbol for crypto and metals
  const flag = getCurrencyFlag(props.currencyCode)
  // If it's a crypto/metal symbol, use it; otherwise show code
  if ((isCrypto.value || isMetal.value) && flag !== '💱') {
    return flag
  }
  return props.currencyCode?.substring(0, 3).toUpperCase()
})

// List of available crypto icons
const availableCryptoIcons = ['btc', 'eth', 'usdt', 'bnb', 'xrp', 'ada', 'doge', 'sol', 'dot', 'matic', 'ltc', 'avax', 'link', 'uni', 'xlm', 'atom', 'etc', 'bch', 'algo', 'vet', 'ftm', 'mana', 'xtz', 'theta', 'aave', 'eos', 'axs', 'cake', 'mkr', 'comp', 'bat', 'dash', 'zec', 'dcr', 'nano', 'xmr', 'trx', 'neo', 'waves', 'zrx']

const hasCryptoIcon = computed(() => {
  return isCrypto.value && availableCryptoIcons.includes(props.currencyCode?.toLowerCase())
})

const cryptoIconUrl = computed(() => {
  if (!hasCryptoIcon.value) return ''
  const iconName = props.currencyCode.toLowerCase()
  // Using jsdelivr CDN for crypto icons
  return `https://cdn.jsdelivr.net/gh/spothq/cryptocurrency-icons@latest/svg/color/${iconName}.svg`
})
</script>

<style scoped>
.currency-icon-wrapper {
  display: inline-flex;
  align-items: center;
  margin-right: 0.25rem;
}

.fi {
  font-size: 1.2em;
  line-height: 1;
  border-radius: 2px;
  overflow: hidden;
}

.currency-symbol {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.8em;
  height: 1.4em;
  padding: 0 0.3em;
  background: #f0f0f0;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.85em;
  font-weight: 600;
  color: #333;
  font-family: 'Segoe UI', system-ui, sans-serif;
}

.currency-symbol.crypto {
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  border-color: #f59e0b;
  color: white;
  font-weight: 700;
}

.currency-symbol.metal {
  background: linear-gradient(135deg, #d1d5db 0%, #9ca3af 100%);
  border-color: #9ca3af;
  color: #111827;
  font-weight: 700;
}

.crypto-icon {
  width: 1.4em;
  height: 1.4em;
  object-fit: contain;
}
</style>