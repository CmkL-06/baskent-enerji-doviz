export interface Currency {
  id: number | string
  code: string
  name: string
  symbol?: string
  isActive: boolean
  countryCode?: string
  isCrypto?: boolean
  isMetal?: boolean
}

export interface VaultBalance {
  id?: string | number
  currencyId: string | number
  currencyCode: string
  currencyName?: string
  balance: number
  availableBalance?: number
  valueInBaseCurrency?: number
  exchangeRateToBase?: number
}

export interface VaultBalanceHistory {
  id?: string | number
  currencyId?: string | number
  currencyCode?: string
  currencyName?: string
  description?: string
  user?: string
  balance?: number
  availableBalance?: number
  valueInBaseCurrency?: number
  transactionType?: string | number
  isParty?: boolean
  createdDate?: string
}

export interface Vault {
  // New API shape (vm_vaultsummary)
  vaultId?: string | number
  vaultName?: string
  officeName?: string
  shouldCount?: boolean
  lastCountDate?: string | null
  createdAt?: string
  balances?: VaultBalance[]
  balanceHistories?: VaultBalanceHistory[]
  totalValueInBaseCurrency?: number
  description?: string
  // Legacy / simple shape
  id?: number | string
  name?: string
  officeId?: number | string
  currencyId?: number | string
  currencyCode?: string
  balance?: number
  isActive: boolean
}

export interface OfficeSummary {
  officeId: string | number
  officeName: string
  officeDescription?: string
  isActive?: boolean
  vaults: Vault[]
  children?: OfficeSummary[]
}

export interface TransactionHistoryParams {
  vaultId?: string | number
  officeId?: string | number
  page?: number
  pageSize?: number
  startDate?: string
  endDate?: string
  type?: string | number
  status?: string | number
}

export interface Office {
  id?: number | string
  officeId?: number | string
  name?: string
  officeName?: string
  officeDescription?: string
  officeImageUri?: string
  address?: string
  phone?: string
  isActive: boolean
  officeType?: string
  parentOfficeId?: string | null
  parentOfficeName?: string | null
  dailyTransactionLimit?: number | null
  monthlyTransactionLimit?: number | null
  commissionRate?: number | null
  vaultCount?: number
  userCount?: number
  vaults?: Vault[]
  children?: Office[]
  createdDate?: string
}

export interface TransactionDetail {
  id?: number | string
  transactionId?: number | string
  currencyId?: number | string
  currencyCode?: string
  currencyName?: string
  currencySymbol?: string
  side: number | string
  amount: number
  rate?: number
  actualBuyRate?: number | null
  actualSellRate?: number | null
  customRate?: number | null
  commission?: number
  netAmount?: number
  username?: string
  userFirstName?: string
  userLastName?: string
  transactionDate?: string
  status?: TransactionStatus
}

export interface Transaction {
  id: number | string
  transactionNumber?: string
  type: number | string
  date?: string
  transactionDate?: string
  status: TransactionStatus
  details?: TransactionDetail[]
  notes?: string
  officeId?: number | string
  officeName?: string
  vaultId?: number | string
  vaultName?: string
  userId?: number | string
  customerId?: number | string | null
  isCustomRate?: boolean
  profit?: number
  isDeleted?: boolean
  deletedReason?: string | null
  deletedBy?: string | null
}

export type TransactionType = 'exchange' | 'deposit' | 'withdraw' | 'transfer' | number
export type TransactionStatus = 'pending' | 'completed' | 'cancelled' | number | string

export interface DashboardSummary {
  officeDetails?: Office[]
  offices?: Office[]
  summary?: {
    todayTransactionCount: number
    todayTurnover?: number
  }
}

export interface ExchangeRate {
  id: number | string
  officeId: number | string
  sourceCurrencyId?: string | number
  targetCurrencyId?: string | number
  currencyId?: number | string
  currencyCode?: string
  buyRate: number
  sellRate: number
  updatedAt?: string
}

export interface User {
  id: number | string
  username: string
  firstname?: string
  lastname?: string
  email?: string
  mail?: string
  rank: number | string
  isAdmin?: boolean
  officeId?: number | string
}

export interface OfficeNode {
  id: string | number
  officeName: string
  officeDescription?: string
  officeImageUri?: string
  address?: string
  phone?: string
  isActive: boolean
  officeType: 'Merkez' | 'Sube' | 'Bayi' | string
  parentOfficeId?: string | null
  parentOfficeName?: string | null
  dailyTransactionLimit?: number | null
  monthlyTransactionLimit?: number | null
  commissionRate?: number | null
  vaultCount: number
  userCount: number
  children?: OfficeNode[]
  createdDate: string
}

export interface OfficeTransfer {
  id: string
  sourceVaultId: string
  sourceVaultName: string
  sourceOfficeName: string
  targetVaultId: string
  targetVaultName: string
  targetOfficeName: string
  currencyCode: string
  amount: number
  status: 'Pending' | 'Approved' | 'Rejected' | 'Completed' | 'Cancelled' | string
  notes?: string | null
  rejectionReason?: string | null
  requestedByName: string
  approvedByName?: string | null
  processedAt?: string | null
  createdDate: string
}
