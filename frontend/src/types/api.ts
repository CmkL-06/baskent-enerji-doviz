export interface Currency {
  id: number
  code: string
  name: string
  symbol?: string
  isActive: boolean
  countryCode?: string
  isCrypto?: boolean
  isMetal?: boolean
}

export interface Vault {
  id: number
  name: string
  officeId: number
  currencyId: number
  currencyCode: string
  balance: number
  isActive: boolean
}

export interface Office {
  id: number
  name?: string
  officeName?: string
  isActive: boolean
  vaultCount?: number
  vaults?: Vault[]
}

export interface TransactionDetail {
  id: number
  transactionId: number
  side: number | string
  currencyId: number
  currencyCode: string
  amount: number
  rate?: number
}

export interface Transaction {
  id: number
  type: number
  date: string
  status: TransactionStatus
  details: TransactionDetail[]
  notes?: string
  officeId?: number
  userId?: number
}

export type TransactionType = 'exchange' | 'deposit' | 'withdraw' | 'transfer' | number
export type TransactionStatus = 'pending' | 'completed' | 'cancelled' | number

export interface DashboardSummary {
  officeDetails?: Office[]
  offices?: Office[]
  summary?: {
    todayTransactionCount: number
    todayTurnover?: number
  }
}

export interface ExchangeRate {
  id: number
  officeId: number
  currencyId: number
  currencyCode: string
  buyRate: number
  sellRate: number
  updatedAt: string
}

export interface User {
  id: number
  username: string
  email?: string
  rank: number | string
  isAdmin?: boolean
  officeId?: number
}

export interface OfficeNode {
  id: string
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
