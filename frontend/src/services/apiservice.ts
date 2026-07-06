import axios from 'axios'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ??
  ((typeof window !== 'undefined' &&
    (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'))
    ? 'http://localhost:5093/api/v1'
    : 'https://api.baskentenerji.com/api/v1')

const apiClient = axios.create({ baseURL: BASE_URL })

// JWT header
apiClient.interceptors.request.use(cfg => {
  const token = localStorage.getItem('token')
  if (token) cfg.headers.Authorization = `Bearer ${token}`
  return cfg
})

// 401 → login yönlendir (cascade korumalı)
let _redirecting = false
apiClient.interceptors.response.use(
  r => r,
  err => {
    if (err.response?.status === 401 && !_redirecting) {
      const hadToken = !!localStorage.getItem('token')
      if (hadToken) {
        _redirecting = true
        localStorage.removeItem('token')
        localStorage.removeItem('user')
        if (window.location.pathname !== '/login') {
          import('@/router').then(m => {
            m.default.push('/login').finally(() => { _redirecting = false })
          })
        } else {
          _redirecting = false
        }
      }
    }
    return Promise.reject(err)
  }
)

const get   = (url: string, config?: any) => apiClient.get(url, config).then(r => r.data)
const post  = (url: string, data?: any, config?: any) => apiClient.post(url, data, config).then(r => r.data)
const put   = (url: string, data?: any) => apiClient.put(url, data).then(r => r.data)
const del   = (url: string) => apiClient.delete(url).then(r => r.data)
const patch = (url: string, data?: any) => apiClient.patch(url, data).then(r => r.data)

const apiService = {
  // ── Ham axios (DashboardWindow.vue apiService.apiClient.get(...) kullanıyor)
  apiClient,

  // ── Auth  [UserController → /api/v1/user/*]
  login:              (data: any)                        => post('/user/login', data),
  registerUser:       (data: any)                        => post('/user/register', data),
  updateUser:         (data: any)                        => post('/user/update', data),
  getUsers:           (params?: any)                     => get('/user/Users', { params }),
  getUser:            (params?: any)                     => get('/user/User', { params }),
  changeUserPassword: (userId: any, newPassword: string) => post('/user/change-password', { UserId: userId, NewPassword: newPassword }),
  logoutAllUsers:     ()                                 => post('/user/logout-all'),

  // ── Currencies  [ExchangeController → /api/v1/exchange/currency]
  getCurrencies:  ()           => get('/exchange/currency'),
  saveCurrency:   (data: any)  => post('/exchange/currency', data),
  deleteCurrency: (id: any)    => post(`/exchange/currency/delete/${id}`),

  // ── Vaults  [ExchangeController → /api/v1/exchange/vault(s)]
  getVaults:           ()                                     => get('/exchange/vaults'),
  getVaultById:        (id: any)                              => get(`/exchange/vaults/${id}`),
  getVaultsByOfficeId: (officeId: any)                        => get(`/exchange/office/${officeId}/vaults`),
  getVaultBalance:     (vaultId: any, currencyId: any)        => get(`/exchange/vaults/${vaultId}/balance/${currencyId}`),
  updateVaultBalance:  (data: any)                            => post('/exchange/vault/updatebalance', data),
  checkVaultBalance:   (vaultId: any, currencyId: any)        => get(`/exchange/vaults/${vaultId}/balance/${currencyId}`),
  saveVault:           (data: any)                            => post('/exchange/vault', data),
  deleteVault:         (id: any)                              => post(`/exchange/vault/delete/${id}`),

  // ── Vault Counts  [VaultService → /api/v1/exchange/vaults/count]
  submitVaultCount:      (data: any)                           => post('/exchange/vaults/count', data),
  getVaultCounts:        (vaultId: any, params?: any)          => get(`/exchange/vaults/${vaultId}/counts`, { params }),
  resetVaultCountStatus: (vaultId: any)                        => post(`/exchange/vaults/${vaultId}/reset-count`),
  setVaultCountStatus:   (vaultId: any, shouldCount: boolean)  => apiClient.post(`/exchange/vaults/${vaultId}/set-count-status`, JSON.stringify(shouldCount), { headers: { 'Content-Type': 'application/json' } }).then(r => r.data),

  // ── Offices  [ExchangeController → /api/v1/exchange/office(s)]
  getOffices:             ()             => get('/exchange/offices/summary'),
  getOfficeSummaries:     ()             => get('/exchange/offices/summary'),
  saveOffice:             (data: any)    => post('/exchange/office', data),
  deleteOffice:           (id: any)      => post(`/exchange/office/delete/${id}`),
  getOfficeUsers:         (officeId: any) => get(`/exchange/office/${officeId}/users`),

  // ── Ofis hiyerarşi
  getOfficeHierarchy:     ()                   => get('/exchange/offices/hierarchy'),
  getMerkezOffice:        ()                   => get('/exchange/offices/merkez'),
  getMyOfficeAccess:      ()                   => get('/exchange/offices/my-access'),

  // ── Ofislerarası Transfer  [ExchangeHierarchyController → /api/v1/exchange/office-transfer/...]
  createTransferRequest:  (data: any)          => post('/exchange/office-transfer/request', data),
  getPendingTransfers:    ()                   => get('/exchange/office-transfer/pending'),
  getTransfersByOffice:   (officeId: string)   => get(`/exchange/office-transfer/office/${officeId}`),
  processTransfer:        (id: string, data: any) => post(`/exchange/office-transfer/${id}/action`, data),
  getTransferById:        (id: string)         => get(`/exchange/office-transfer/${id}`),

  // ── User ↔ Office  [ExchangeController → /api/v1/exchange/user/...]
  getUserOffices:       (userId: any)                  => get(`/exchange/user/${userId}/offices`),
  attachOfficeToUser:   (data: any)                    => post('/exchange/user/office/attach', data),
  removeOfficeFromUser: (userId: any, officeId: any)   => post(`/exchange/user/${userId}/office/${officeId}/remove`),
  updateUserOffices:    (data: any)                    => post('/exchange/user/offices/update', data),

  // ── Exchange rates  [ExchangeController → /api/v1/exchange/rates]
  getExchangeRates:         (officeId?: any)  => get(officeId ? `/exchange/rates?officeId=${officeId}` : '/exchange/rates'),
  getExchangeRatesByOffice: (officeId: any)   => get(`/exchange/rates?officeId=${officeId}`),
  getSpecificExchangeRate:  (officeId: any, sourceCurrencyId: any, targetCurrencyId: any) =>
                              get(`/exchange/rates/${officeId}/${sourceCurrencyId}/${targetCurrencyId}`),
  getExchangeRateHistory:   (officeId: any, sourceCurrencyId: any, targetCurrencyId: any) =>
                              get(`/exchange/rates/${officeId}/${sourceCurrencyId}/${targetCurrencyId}/history`),
  updateExchangeRate:       (data: any)       => put('/exchange/rates', data),
  createExchangeRate:       (data: any)       => post('/exchange/rates', data),
  deleteExchangeRate:       (id: any)         => post(`/exchange/rate/delete/${id}`),

  // ── External rates  [ExchangeAutoRateController → /api/v1/exchange/auto-rate]
  getExternalRates:   (cacheMinutes?: number) => get(`/exchange/auto-rate/external-rates${cacheMinutes ? `?cache=${cacheMinutes}` : ''}`),
  getExternalSources: ()                      => get('/exchange/auto-rate/sources'),

  // ── Transactions  [ExchangeController → /api/v1/exchange/exchange & /transactions]
  createExchangeTransaction: (data: any)   => post('/exchange/exchange', data),
  getTransactionHistory:     (params?: any) => get('/exchange/transactions', { params }),
  getTransactionById:        (id: any)     => get(`/exchange/transactions/${id}`),
  removeTransaction:         (id: any)     => post('/exchange/exchange/remove', { transactionId: id }),
  transferBetweenVaults:     (data: any)   => post('/exchange/transfer', data),

  // ── Dashboard  [ExchangeController → /api/v1/exchange/dashboard]
  getDashboardData: (officeId?: any) => get(officeId ? `/exchange/dashboard?officeId=${officeId}` : '/exchange/dashboard'),

  // ── Binance  [ExchangeController → /api/v1/exchange/binance]
  getBinanceUSDTDeposits: () => get('/exchange/binance/usdt-deposits'),

  // ── Expenses  [ExchangeController → /api/v1/exchange/expense]
  getExpensePayments:               (params?: any)              => get('/exchange/expense/payments', { params }),
  getPaymentsByDefinition:          (definitionId: any)         => get(`/exchange/expense/payments/definition/${definitionId}`),
  getExpenseDefinitions:            (officeId?: any)            => get(officeId ? `/exchange/expense/definitions?officeId=${officeId}` : '/exchange/expense/definitions'),
  getExpenseDefinitionsByCategory:  (categoryId: any)           => get(`/exchange/expense/definitions/category/${categoryId}`),
  getExpenseDefinitionById:         (id: any)                   => get(`/exchange/expense/definitions/${id}`),
  createExpensePayment:             (data: any)                 => post('/exchange/expense/payments', data),
  createExpenseDefinition:          (data: any)                 => post('/exchange/expense/definitions', data),
  updateExpenseDefinition:          (id: any, data: any)        => post(`/exchange/expense/definitions/${id}/update`, data),
  deleteExpensePayment:             (id: any, reason?: string)  => post(`/exchange/expense/payments/${id}/delete`, { reason: reason ?? '' }),
  deleteExpenseDefinition:          (id: any)                   => post(`/exchange/expense/definitions/${id}/delete`),

  // ── Auto rate  [ExchangeAutoRateController → /api/v1/exchange/auto-rate]
  getAutoRateSettings:   ()            => get('/exchange/auto-rate/settings'),
  updateAutoRateSettings: (data: any)  => put('/exchange/auto-rate/settings', data),
  triggerAutoUpdate:      (data: any)  => post('/exchange/auto-rate/update', data),
  getPendingApprovals:    ()           => get('/exchange/auto-rate/pending'),
  getRateHistory:         ()           => get('/exchange/auto-rate/history'),
  approveOrRejectRate:    (id: any, data: any) => post(`/exchange/auto-rate/pending/${id}/action`, data),

  // ── Party  [ExchangeController → /api/v1/exchange/party]
  getParties:               (officeId?: any)      => get(officeId ? `/exchange/parties?officeId=${officeId}` : '/exchange/parties'),
  getPartyById:             (partyId: any)        => get(`/exchange/party/${partyId}`),
  createParty:              (data: any)           => post('/exchange/party', data),
  updateParty:              (partyId: any, data: any) => put(`/exchange/party/${partyId}`, data),
  deleteParty:              (partyId: any)        => post(`/exchange/party/delete/${partyId}`),
  getPartyAccounts:         (partyId: any)        => get(`/exchange/party/${partyId}/accounts`),
  getPartyAccountEntries:   (accountId: any)      => get(`/exchange/party/account/${accountId}/entries`),
  createPartyPayment:       (data: any)           => post('/exchange/party/account/payment', data),
  getPartyStatement:        (partyId: any)        => get(`/exchange/party/${partyId}/statement`),
  getPartyBalanceSummary:   (officeId?: any)       => get(`/exchange/party/reports/balance-summary${officeId ? `?officeId=${officeId}` : ''}`),
  getPartyAgedReceivables:  (officeId?: any)      => get(`/exchange/party/reports/aged-receivables${officeId ? `?officeId=${officeId}` : ''}`),

  // ── Ghost Party  [ExchangeController → /api/v1/exchange/ghost-party]
  createGhostAccount:     (data: any)             => post('/exchange/ghost-party/account/create', data),
  getGhostAccounts:       (partyId: any)          => get(`/exchange/ghost-party/${partyId}/accounts`),
  getGhostEntries:        (accountId: any)        => get(`/exchange/ghost-party/account/${accountId}/entries`),
  createGhostPayment:     (data: any)             => post('/exchange/ghost-party/payment', data),
  createGhostCollection:  (data: any)             => post('/exchange/ghost-party/collection', data),
  reverseGhostEntry:      (entryId: any)          => post(`/exchange/ghost-party/entry/${entryId}/reverse`),
  getGhostBalance:        (partyId: any)          => get(`/exchange/ghost-party/${partyId}/balance`),
  getGhostStatement:      (partyId: any)          => get(`/exchange/ghost-party/${partyId}/statement`),
  getGhostSummary:        (partyId: any)          => get(`/exchange/ghost-party/${partyId}/summary`),
  blockGhostAccount:      (accountId: any)        => post(`/exchange/ghost-party/account/${accountId}/block`),
  unblockGhostAccount:    (accountId: any)        => post(`/exchange/ghost-party/account/${accountId}/unblock`),

  // ── Reports  [ExchangeController → /api/v1/exchange/reports]
  getZReportDaily:   (officeId?: any, date?: string) => {
    const p: any = {}
    if (officeId != null) p.officeId = officeId
    if (date != null) p.date = date
    return get('/exchange/reports/z-report/daily', { params: p })
  },
  getZReportWeekly:  (officeId?: any, weekStart?: string) => {
    const p: any = {}
    if (officeId != null) p.officeId = officeId
    if (weekStart != null) p.weekStart = weekStart
    return get('/exchange/reports/z-report/weekly', { params: p })
  },
  getZReportMonthly: (officeId?: any, year?: number, month?: number) => {
    const p: any = {}
    if (officeId != null) p.officeId = officeId
    if (year != null) p.year = year
    if (month != null) p.month = month
    return get('/exchange/reports/z-report/monthly', { params: p })
  },
  getZReportYearly:  (officeId?: any, year?: number) => {
    const p: any = {}
    if (officeId != null) p.officeId = officeId
    if (year != null) p.year = year
    return get('/exchange/reports/z-report/yearly', { params: p })
  },
  getZReportCustom:  (params?: any) => get('/exchange/reports/z-report/custom', { params }),
  getZReportHistory: (params?: any) => get('/exchange/reports/z-report/history', { params }),
  endDay:            (officeId: any) => post(`/exchange/endday/${officeId}`),
  getPnLReport:      (params?: any) => get('/exchange/reports/pnl', { params }),
  getMonthlyReport:  (params?: any) => get('/exchange/reports/monthly', { params }),

  // ── WAC (Weighted Average Cost)
  getWac:            (vaultId: any, currencyId: any) => get(`/exchange/wac/${vaultId}/${currencyId}`),
  getAllWacs:         (vaultId: any) => get(`/exchange/wac/${vaultId}`),
  getWacHistory:     (vaultId: any, currencyId?: any, limit = 50) => {
    const p: any = { limit }
    if (currencyId != null) p.currencyId = currencyId
    return get(`/exchange/wac/${vaultId}/history`, { params: p })
  },

  // ── Day Closure (Gün Kapanışı)
  getDayStatus:      (officeId: any) => get(`/exchange/day-status/${officeId}`),
  closeDay:          (data: any) => post('/exchange/day-close', data),
  getDayClosure:     (officeId: any, date: string) => get(`/exchange/day-closure/${officeId}/${date}`),
  getClosureHistory: (officeId: any, params?: any) => get(`/exchange/day-closure/${officeId}/history`, { params }),
  getConsolidatedDayClosure: (date?: string) => get('/exchange/day-closure/consolidated', { params: date ? { date } : {} }),

  // ── Vault balance histories  [ExchangeController → /api/v1/exchange/vault/balance-histories]
  getVaultBalanceHistories: (officeId: any, date?: string) => {
    const p: any = {}
    if (officeId != null) p.officeId = officeId
    if (date != null) p.date = date
    return get('/exchange/vault/balance-histories', { params: p })
  },

  // ── Vault Snapshots  [VaultSnapshotController → /api/v1/exchange/vault-snapshots]
  createVaultSnapshot:     (data: any)                              => post('/exchange/vault-snapshots', data),
  getVaultSnapshots:       (officeId: any, params?: any)            => get(`/exchange/vault-snapshots/office/${officeId}`, { params }),
  getVaultSnapshotsByDate: (officeId: any, date: string)            => get(`/exchange/vault-snapshots/office/${officeId}/date/${date}`),
  getVaultSnapshotById:    (snapshotId: any)                        => get(`/exchange/vault-snapshots/${snapshotId}`),
  deleteVaultSnapshot:     (snapshotId: any)                        => del(`/exchange/vault-snapshots/${snapshotId}`),
  compareVaultSnapshots:   (id1: any, id2: any)                     => get(`/exchange/vault-snapshots/compare`, { params: { snapshotId1: id1, snapshotId2: id2 } }),

  // ── Expense aggregates  [ExchangeController → /api/v1/exchange/expense/reports/...]
  getTotalExpenses:      (officeId: any, startDate?: string, endDate?: string) => {
    const p: any = { officeId }
    if (startDate != null) p.startDate = startDate
    if (endDate != null) p.endDate = endDate
    return get('/exchange/expense/reports/total', { params: p })
  },
  getExpensesByCategory: (officeId: any, startDate?: string, endDate?: string) => {
    const p: any = { officeId }
    if (startDate != null) p.startDate = startDate
    if (endDate != null) p.endDate = endDate
    return get('/exchange/expense/reports/by-category', { params: p })
  },

  // ── Alerts  [ExchangeHierarchyController → /api/v1/exchange/alerts/...]
  getUnreadAlerts:      ()                    => get('/exchange/alerts/unread'),
  getAlertsByOffice:    (officeId: string, limit = 50) => get(`/exchange/alerts/office/${officeId}`, { params: { limit } }),
  markAlertRead:        (alertId: string)     => post(`/exchange/alerts/${alertId}/read`),
  markAllAlertsRead:    ()                    => post('/exchange/alerts/read-all'),
  resolveAlert:         (alertId: string)     => post(`/exchange/alerts/${alertId}/resolve`),

  // ── Merkezi Kur Yönetimi  [ExchangeHierarchyController → /api/v1/exchange/rates/...]
  pushRatesToBranches:  (merkezOfficeId: string) => post('/exchange/rates/push-to-branches', { merkezOfficeId }),
  getEffectiveRate:     (officeId: string, sourceCurrencyId: string, targetCurrencyId: string) =>
                          get(`/exchange/rates/effective/${officeId}/${sourceCurrencyId}/${targetCurrencyId}`),

  // ── Performans  [ExchangeHierarchyController → /api/v1/exchange/reports/branch-comparison]
  getBranchComparison:  (period = 'daily')    => get('/exchange/reports/branch-comparison', { params: { period } }),

  // ── TG Bayi Cari Hesap  [TelegramDealerController → /api/v1/tg/dealer/...]
  getTgCariSummary:     ()                             => get('/tg/dealer/cari-summary'),
  getTgCariEntries:     (code: string)                 => get(`/tg/dealer/${code}/cari-entries`),
  recordTgPayment:      (data: any)                    => post('/tg/dealer/record-payment', data),

  // ── Generic
  get, post, put, delete: del, patch,
}

export default apiService
