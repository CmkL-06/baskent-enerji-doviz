// Döviz kodu → ISO 3166-1 alpha-2 ülke kodu (flag-icons için)
const CURRENCY_COUNTRY: Record<string, string> = {
  USD: 'us', EUR: 'eu', GBP: 'gb', JPY: 'jp', CHF: 'ch', CAD: 'ca',
  AUD: 'au', NZD: 'nz', SEK: 'se', NOK: 'no', DKK: 'dk', HUF: 'hu',
  CZK: 'cz', PLN: 'pl', RON: 'ro', BGN: 'bg', HRK: 'hr', RSD: 'rs',
  RUB: 'ru', UAH: 'ua', KZT: 'kz', GEL: 'ge', AZN: 'az', BYN: 'by',
  TRY: 'tr', SAR: 'sa', AED: 'ae', QAR: 'qa', KWD: 'kw', BHD: 'bh',
  OMR: 'om', EGP: 'eg', MAD: 'ma', TND: 'tn', DZD: 'dz', LYD: 'ly',
  CNY: 'cn', HKD: 'hk', SGD: 'sg', KRW: 'kr', INR: 'in', IDR: 'id',
  MYR: 'my', THB: 'th', PHP: 'ph', VND: 'vn', BRL: 'br', MXN: 'mx',
  ARS: 'ar', CLP: 'cl', COP: 'co', PEN: 'pe', ILS: 'il', ZAR: 'za',
  NGN: 'ng', KES: 'ke', GHS: 'gh', UGX: 'ug', ETB: 'et', TZS: 'tz',
}

const CRYPTO_CODES = new Set(['BTC','ETH','USDT','USDC','BNB','XRP','SOL','ADA','DOGE','DOT',
  'AVAX','MATIC','LINK','UNI','LTC','BCH','XLM','ALGO','VET','TRX','XMR'])

const METAL_CODES = new Set(['XAU','XAG','XPT','XPD','GOLD','SILVER','PLATINUM'])

const SPECIAL_FLAG_MAP: Record<string, string> = {
  KRUB: 'ru', MGBP: 'gb', MEUR: 'eu', KGS: 'kg',
}

export function getCurrencyFlagImg(code?: string | null): string {
  if (!code) return ''
  const upper = code.toUpperCase()
  const cc = SPECIAL_FLAG_MAP[upper] || CURRENCY_COUNTRY[upper]
  return cc ? `/flags/${cc}.png` : ''
}

export function getCurrencyCountryCode(code?: string | null): string {
  if (!code) return ''
  return CURRENCY_COUNTRY[code.toUpperCase()] || ''
}

export function getCurrencyFlag(code: string): string {
  const cc = getCurrencyCountryCode(code)
  return cc ? `fi fi-${cc}` : ''
}

export function getCurrencyFlagAlt(code: string): string {
  return getCurrencyFlag(code)
}

export function getCurrencyName(code: string): string {
  const names: Record<string, string> = {
    USD:'Amerikan Doları', EUR:'Euro', GBP:'İngiliz Sterlini',
    TRY:'Türk Lirası', RUB:'Rus Rublesi', JPY:'Japon Yeni',
    CHF:'İsviçre Frangı', CAD:'Kanada Doları', AUD:'Avustralya Doları',
    CNY:'Çin Yuanı', SAR:'Suudi Riyali', AED:'BAE Dirhemi',
    USDT:'Tether (USD)', BTC:'Bitcoin', ETH:'Ethereum',
    XAU:'Altın', XAG:'Gümüş',
  }
  return names[code?.toUpperCase()] || code
}

export function isCryptoCurrency(code: string): boolean {
  return CRYPTO_CODES.has(code?.toUpperCase())
}

export function isMetalCurrency(code: string): boolean {
  return METAL_CODES.has(code?.toUpperCase())
}

export function formatAmount(value: number | null | undefined, decimals = 0): string {
  if (value === null || value === undefined) return '0'
  const num = Number(value)
  if (isNaN(num)) return '0'
  return num.toLocaleString('tr-TR', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals
  })
}

export function formatExchangeRate(value: number | null | undefined, decimals = 4): string {
  if (value === null || value === undefined) return '0'
  const num = Number(value)
  if (isNaN(num)) return '0'
  return num.toLocaleString('tr-TR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: decimals
  })
}

// Türkçe klavye/numpad girişini güvenli parse eder.
// "1.234,56" -> 1234.56 ("." binlik, "," ondalık)
// "38,75" -> 38.75 (yalnızca "," varsa ondalık ayraç)
// "1.234" -> 1234 (yalnızca "." varsa VE tam binlik gruplama deseniyle eşleşiyorsa binlik ayraç sayılır)
// "12.5" / "0.5" -> 12.5 / 0.5 (yalnızca "." varsa ve binlik deseniyle eşleşmiyorsa ondalık nokta sayılır)
// Önceden bazı sayfalar tek-ayraçlı "." girişini her zaman ondalık nokta sayıyordu, bazıları ise
// her zaman binlik ayraç — aynı "1.234" girişi sayfaya göre 1.234 ya da 1234 olarak parse ediliyordu.
export function parseDecimalInput(value: unknown): number {
  if (value === null || value === undefined || value === '') return 0
  let s = String(value).trim()
  if (s.includes(',') && s.includes('.')) {
    s = s.replace(/\./g, '').replace(',', '.')
  } else if (s.includes(',')) {
    s = s.replace(',', '.')
  } else if (/^\d{1,3}(\.\d{3})+$/.test(s)) {
    s = s.replace(/\./g, '')
  }
  const n = parseFloat(s)
  return isNaN(n) ? 0 : n
}

export function formatCurrency(value: number | null | undefined, currencyCode = 'TRY'): string {
  if (value === null || value === undefined) return '0'
  const num = Number(value)
  if (isNaN(num)) return '0'
  try {
    return num.toLocaleString('tr-TR', {
      style: 'currency',
      currency: currencyCode,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    })
  } catch {
    return `${num.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currencyCode}`
  }
}
