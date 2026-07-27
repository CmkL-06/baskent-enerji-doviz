<template>
  <div class="vault-view">
    <!-- Premium Header -->
    <div class="vault-header">
      <div class="header-background">
        <div class="header-pattern"></div>
      </div>
      <div class="header-content">
        <button @click="goBack" class="back-button">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M19 12H5m0 0l7 7m-7-7l7-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          Geri
        </button>
        
        <div v-if="vault" class="vault-identity">
          <div class="vault-icon-wrapper">
            <div class="vault-icon">
              <svg width="40" height="40" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M3 7h18v10c0 1.1-.9 2-2 2H5c-1.1 0-2-.9-2-2V7z" fill="currentColor" opacity="0.2"/>
                <path d="M21 7V6c0-2.21-1.79-4-4-4H7C4.79 2 3 3.79 3 6v1h18z" fill="currentColor" opacity="0.3"/>
                <path d="M12 13a2 2 0 100-4 2 2 0 000 4z" fill="currentColor"/>
              </svg>
            </div>
            <div :class="['status-badge', vault.isActive ? 'active' : 'inactive']">
              {{ vault.isActive ? 'Active' : 'Inactive' }}
            </div>
          </div>
          
          <div class="vault-details">
            <h1 class="vault-name">{{ vault.vaultName }}</h1>
            <div class="vault-meta">
              <span class="meta-item">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M3 21h18M3 10h18M5 21V10l7-7 7 7v11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                {{ officeName }}
              </span>
              <span class="meta-divider">•</span>
              <span class="meta-item">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                Created {{ vault.createdAt ? formatDate(vault.createdAt) : '' }}
              </span>
            </div>
          </div>
        </div>
        
        <button @click="refresh" class="refresh-button" :disabled="isLoading">
          <svg :class="{ 'rotating': isLoading }" width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M4 12a8 8 0 018-8V2.5L16 6l-4 3.5V8a6 6 0 00-6 6h1.5m14.5 0a8 8 0 01-8 8v1.5L8 18l4-3.5V16a6 6 0 006-6h-1.5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>

      <div v-if="vaultSwitcherOptions.length > 1" class="vault-switcher">
        <button
          v-for="opt in vaultSwitcherOptions"
          :key="opt.id"
          type="button"
          class="vault-switcher-tab"
          :class="{ active: opt.isActive }"
          @click="switchVault(opt.id)"
        >
          {{ opt.name }}
        </button>
      </div>
    </div>

    <div v-if="vault && !isLoading" class="vault-content">
      <!-- Quick Actions -->
      <div class="quick-actions-card">
        <h2 class="section-title">Hızlı İşlemler</h2>
        <div class="actions-grid">
          <button v-if="!authStore.isViewerForOffice(vault?.officeId)" type="button" @click="showDepositDialog = true" class="action-button deposit">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 2v20m0-20l-4 4m4-4l4 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M5 19h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </div>
            <span class="action-label">Para Yatır</span>
            <span class="action-description">Kasaya para ekle</span>
          </button>

          <button v-if="!authStore.isViewerForOffice(vault?.officeId)" type="button" @click="showWithdrawDialog = true" class="action-button withdraw">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 22V2m0 20l4-4m-4 4l-4-4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M5 5h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </div>
            <span class="action-label">Para Çek</span>
            <span class="action-description">Kasadan para çıkar</span>
          </button>
          
          <!-- Transfer butonu gizlendi
          <button type="button" @click="showTransferDialog = true" class="action-button transfer">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M7 10L12 5L17 10M17 14L12 19L7 14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <span class="action-label">Transfer</span>
            <span class="action-description">Kasalar arası transfer</span>
          </button>
          -->
          
          <button type="button" @click="router.push('/ihtiyar/exchange-v2')" class="action-button exchange">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M7 10L12 5L17 10M17 14L12 19L7 14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <span class="action-label">Döviz İşlemi</span>
            <span class="action-description">Alış / satış işlemi yap</span>
          </button>
          
          <!-- Düzenleme butonu gizlendi
          <button v-if="authStore.isAdmin" type="button" @click="editVault" class="action-button edit">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <span class="action-label">Düzenle</span>
            <span class="action-description">Kasa bilgilerini güncelle</span>
          </button>
          -->
          
          <button v-if="authStore.isAdmin" type="button" @click="confirmDelete" class="action-button delete">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M3 6h18m-2 0v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <span class="action-label">Sil</span>
            <span class="action-description">Bu kasayı kaldır</span>
          </button>
        </div>
      </div>

      <!-- Section Tabs -->
      <div class="section-tabs">
        <button :class="['section-tab', { active: activeSection === 'balances' }]" @click="activeSection = 'balances'">
          <span class="material-symbols-outlined" aria-hidden="true" style="font-size:18px">account_balance_wallet</span>
          Bakiyeler
        </button>
        <button :class="['section-tab', { active: activeSection === 'history' }]" @click="activeSection = 'history'">
          <span class="material-symbols-outlined" aria-hidden="true" style="font-size:18px">history</span>
          Geçmiş
        </button>
        <button v-if="authStore.isAdmin" :class="['section-tab', { active: activeSection === 'count' }]" @click="activeSection = 'count'; loadCountHistory()">
          <span class="material-symbols-outlined" aria-hidden="true" style="font-size:18px">inventory_2</span>
          Sayım
        </button>
      </div>

      <!-- Balances Overview -->
      <div v-if="activeSection === 'balances'" class="balances-card">
        <div class="balances-header">
          <h2 class="section-title">Kasa Bakiyeleri</h2>
          <div class="header-controls">
            <div class="view-toggle">
              <button 
                @click="viewMode = 'list'" 
                :class="['view-btn', { active: viewMode === 'list' }]"
                title="Liste Görünümü"
              >
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
              <button 
                @click="viewMode = 'grid'" 
                :class="['view-btn', { active: viewMode === 'grid' }]"
                title="Kart Görünümü"
              >
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <rect x="3" y="3" width="7" height="7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <rect x="14" y="3" width="7" height="7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <rect x="14" y="14" width="7" height="7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <rect x="3" y="14" width="7" height="7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
            <div class="total-section">
              <span class="total-label">Toplam Değer</span>
              <span class="total-amount">{{ formatCurrency(vault.totalValueInBaseCurrency || 0, 'TRY') }}</span>
            </div>
          </div>
        </div>
        
        <!-- List View -->
        <div v-if="viewMode === 'list'" class="balances-table">
          <table>
            <thead>
              <tr>
                <th>Para Birimi</th>
                <th>Bakiye</th>
                <th>Kullanılabilir</th>
                <th>TRY Değeri</th>
                <th v-if="authStore.isAdmin">İşlem</th>
              </tr>
            </thead>
            <tbody>
              <tr 
                v-for="balance in vault.balances" 
                :key="balance.currencyId"
                :class="{ 'is-negative': balance.balance < 0 }"
              >
                <td>
                  <div class="currency-info">
                    <i v-if="getCurrencyCountryCode(balance.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(balance.currencyCode)}`"></i>
                    <span v-else class="currency-badge">{{ balance.currencyCode.substring(0, 2) }}</span>
                    <div class="currency-text">
                      <span class="currency-code">{{ balance.currencyCode }}</span>
                      <span class="currency-name">{{ balance.currencyName }}</span>
                    </div>
                  </div>
                </td>
                <td>
                  <span :class="['amount', { 'negative': balance.balance < 0 }]">
                    {{ formatAmount(balance.balance) }}
                  </span>
                </td>
                <td>
                  <span class="amount available">
                    {{ formatAmount(balance.availableBalance || balance.balance) }}
                  </span>
                </td>
                <td>
                  <span class="amount in-base">
                    {{ formatCurrency(balance.valueInBaseCurrency || 0, 'TRY') }}
                  </span>
                  <span v-if="balance.unrealizedProfit" class="amount" :class="balance.unrealizedProfit < 0 ? 'unrealized-neg' : 'unrealized-pos'" style="display:block;font-size:11px">
                    {{ balance.unrealizedProfit > 0 ? '+' : '' }}{{ formatCurrency(balance.unrealizedProfit, 'TRY') }} (anlık fark)
                  </span>
                </td>
                <td v-if="authStore.isAdmin">
                  <button @click="updateBalance(balance)" class="balance-update-btn" title="Bakiye Güncelle">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                      <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
          <div v-if="!vault.balances || vault.balances.length === 0" class="empty-balances">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 2v20M2 12h20" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <p>Henüz bakiye bulunmuyor</p>
          </div>
        </div>

        <!-- Grid View -->
        <div v-if="viewMode === 'grid'" class="currencies-grid">
          <div 
            v-for="balance in vault.balances" 
            :key="balance.currencyId"
            class="currency-card"
            :class="{ 'is-negative': balance.balance < 0 }"
          >
            <div class="currency-header">
              <div class="currency-identity">
                <i v-if="getCurrencyCountryCode(balance.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(balance.currencyCode)}`"></i>
                <span v-else class="currency-badge">{{ balance.currencyCode }}</span>
                <span class="currency-code">{{ balance.currencyCode }}</span>
              </div>
              <button v-if="authStore.isAdmin" @click="updateBalance(balance)" class="balance-update-btn" title="Update Balance">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
            
            <div class="balance-details">
              <div class="balance-row primary">
                <span class="balance-label">Bakiye</span>
                <span class="balance-value">{{ formatAmount(balance.balance) }}</span>
              </div>
              <div class="balance-row">
                <span class="balance-label">Kullanılabilir</span>
                <span class="balance-value available">{{ formatAmount(balance.availableBalance || balance.balance) }}</span>
              </div>
              <div class="balance-row">
                <span class="balance-label">Değer</span>
                <span class="balance-value in-base">{{ formatCurrency(balance.valueInBaseCurrency || 0, 'TRY') }}</span>
              </div>
              <div class="balance-row" v-if="balance.unrealizedProfit">
                <span class="balance-label">Anlık Piyasa Farkı</span>
                <span class="balance-value" :class="balance.unrealizedProfit < 0 ? 'unrealized-neg' : 'unrealized-pos'">
                  {{ balance.unrealizedProfit > 0 ? '+' : '' }}{{ formatCurrency(balance.unrealizedProfit, 'TRY') }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Balance History -->
      <div v-if="activeSection === 'history'" class="history-card">
        <div class="history-header">
          <h2 class="section-title">Bakiye Geçmişi</h2>
          <div class="history-controls">
            <div class="type-filter">
              <button 
                v-for="filter in balanceTypeFilters" 
                :key="filter.value"
                :class="['filter-btn', { active: selectedTypeFilter === filter.value }]"
                @click="selectedTypeFilter = filter.value"
              >
                {{ filter.label }}
              </button>
            </div>
            <div class="time-filter">
              <button 
                v-for="range in balanceHistoryRanges" 
                :key="range.value"
                :class="['time-btn', { active: selectedBalanceRange === range.value }]"
                @click="selectedBalanceRange = range.value"
              >
                {{ range.label }}
              </button>
            </div>
            <label class="show-exchanges-check">
              <input v-model="showExchangeHistories" type="checkbox" class="form-checkbox">
              <span>Döviz İşlemlerini Göster</span>
            </label>
          </div>
        </div>
        
        <div class="balance-history-table">
          <table>
            <thead>
              <tr>
                <th>Tarih & Saat</th>
                <th>İşlem Türü</th>
                <th>Para Birimi</th>
                <th>Bakiye Değişimi</th>
                <th>Yeni Bakiye</th>
                <th>TRY Değeri</th>
                <th>Açıklama</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="history in filteredBalanceHistory" :key="history.id">
                <td class="date-cell">
                  <div class="date-time">
                    <span class="date">{{ formatDate(history.createdDate || '') }}</span>
                    <span class="time">{{ formatTime(history.createdDate || '') }}</span>
                  </div>
                </td>
                <td>
                  <span :class="['type-badge', getTransactionTypeClass(Number(history.transactionType))]">
                    {{ getTransactionTypeName(Number(history.transactionType)) }}
                  </span>
                </td>
                <td>
                  <div class="currency-identity">
                    <i v-if="getCurrencyCountryCode(history.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(history.currencyCode)}`"></i>
                    <span v-else class="currency-badge">{{ history.currencyCode }}</span>
                    <span class="currency-code">{{ history.currencyCode }}</span>
                  </div>
                </td>
                <td>
                  <span :class="['balance-change', (history.balance ?? 0) >= 0 ? 'positive' : 'negative']">
                    {{ (history.balance ?? 0) >= 0 ? '+' : '' }}{{ formatAmount(history.balance ?? 0) }}
                  </span>
                </td>
                <td>
                  <span class="balance-value">{{ formatAmount(history.availableBalance) }}</span>
                </td>
                <td>
                  <span class="balance-value in-base">{{ formatCurrency(history.valueInBaseCurrency || 0, 'TRY') }}</span>
                </td>
                <td>
                  <span v-if="history.description" class="description">{{ history.description }}</span>
                  <span v-else class="no-details">-</span>
                </td>
              </tr>
            </tbody>
          </table>
          <div v-if="!filteredBalanceHistory.length" class="empty-transactions">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M9 14h6m-6 4h3" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
            </svg>
            <p>Bu dönemde bakiye geçmişi bulunmuyor</p>
          </div>
        </div>
      </div>

      <!-- ═══ Count Section ═══ -->
      <div v-if="activeSection === 'count'" class="count-section">
        <div class="count-section-header">
          <h2 class="section-title">
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size:20px;font-variation-settings:'FILL' 1,'wght' 400,'GRAD' 0,'opsz' 24">inventory_2</span>
            Kasa Sayımı
          </h2>
          <button @click="openCountForm" class="count-start-btn">
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size:18px;font-variation-settings:'FILL' 1,'wght' 400,'GRAD' 0,'opsz' 24">add_circle</span>
            Yeni Sayım
          </button>
        </div>

        <!-- Count Form -->
        <div v-if="showCountForm" class="count-form-card">
          <div class="count-form-header">
            <h3>Fiziksel Sayım</h3>
            <p class="count-form-hint">Her para birimi için kasadaki gerçek tutarı giriniz.</p>
          </div>
          <div class="count-form-body">
            <div v-for="detail in countDetails" :key="detail.currencyId" class="count-row">
              <div class="count-currency-info">
                <i v-if="getCurrencyCountryCode(detail.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(detail.currencyCode)}`" style="font-size:16px"></i>
                <span class="count-currency-code">{{ detail.currencyCode }}</span>
              </div>
              <div class="count-field">
                <span class="count-field-label">Sistem</span>
                <span class="count-field-value">{{ formatAmount(detail.systemAmount) }}</span>
              </div>
              <div class="count-field">
                <span class="count-field-label">Gerçek Tutar</span>
                <input v-model.number="detail.actualAmount" type="number" step="0.01" placeholder="0.00" class="count-input" />
              </div>
              <div v-if="detail.actualAmount !== null && detail.actualAmount !== undefined" class="count-field">
                <span class="count-field-label">Fark</span>
                <span :class="['count-diff-val', getCountDiscrepancy(detail) === 0 ? 'count-diff--ok' : 'count-diff--warn']">
                  {{ getCountDiscrepancy(detail) > 0 ? '+' : '' }}{{ formatAmount(getCountDiscrepancy(detail)) }}
                </span>
              </div>
            </div>
          </div>
          <div class="count-form-footer">
            <label class="count-snapshot-check">
              <input type="checkbox" v-model="countAlsoSnapshot" />
              <span>Sayım sonrası snapshot al</span>
            </label>
            <input v-if="countAlsoSnapshot" v-model="countSnapshotDesc" placeholder="Snapshot açıklaması (opsiyonel)" class="count-snapshot-input" />
            <div class="count-form-actions">
              <button @click="showCountForm = false" class="count-cancel-btn">İptal</button>
              <button @click="submitVaultCount" :disabled="isSavingCount" class="count-save-btn">
                <span class="material-symbols-outlined" aria-hidden="true" style="font-size:16px;font-variation-settings:'FILL' 1,'wght' 400,'GRAD' 0,'opsz' 24">{{ isSavingCount ? 'refresh' : 'save' }}</span>
                {{ isSavingCount ? 'Kaydediliyor...' : 'Sayımı Kaydet' }}
              </button>
            </div>
          </div>
        </div>

        <!-- Count History -->
        <div class="count-history-card">
          <h3 class="count-history-title">Sayım Geçmişi</h3>
          <div v-if="isLoadingCounts" class="count-loading">
            <span class="material-symbols-outlined animate-spin" style="font-size:24px">refresh</span>
            Yükleniyor...
          </div>
          <table v-else-if="countHistory.length > 0" class="count-history-table">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Para Birimi</th>
                <th>Sistem</th>
                <th>Sayım</th>
                <th>Fark</th>
                <th>Sayan</th>
              </tr>
            </thead>
            <tbody>
              <template v-for="count in countHistory" :key="count.id">
                <tr v-for="(detail, di) in (count.details || count.countDetails || [])" :key="count.id + '-' + di">
                  <td v-if="di === 0" :rowspan="(count.details || count.countDetails || []).length" class="count-date-cell">
                    <div class="count-date">{{ formatDate(count.countDate || count.createdDate || '') }}</div>
                    <div class="count-time">{{ formatTime(count.countDate || count.createdDate || '') }}</div>
                  </td>
                  <td>
                    <span class="count-curr-badge">{{ detail.currencyCode || '' }}</span>
                  </td>
                  <td class="count-mono">{{ formatAmount(detail.systemAmount ?? detail.expectedAmount ?? 0) }}</td>
                  <td class="count-mono">{{ formatAmount(detail.actualAmount ?? detail.countedAmount ?? 0) }}</td>
                  <td>
                    <span :class="['count-diff-badge', (detail.discrepancy ?? ((detail.actualAmount ?? detail.countedAmount ?? 0) - (detail.systemAmount ?? detail.expectedAmount ?? 0))) === 0 ? 'count-diff--ok' : 'count-diff--warn']">
                      {{ formatAmount(detail.discrepancy ?? ((detail.actualAmount ?? detail.countedAmount ?? 0) - (detail.systemAmount ?? detail.expectedAmount ?? 0))) }}
                    </span>
                  </td>
                  <td v-if="di === 0" :rowspan="(count.details || count.countDetails || []).length" class="count-user-cell">
                    {{ count.countedBy || count.userName || '-' }}
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
          <div v-else class="count-empty">
            <span class="material-symbols-outlined" aria-hidden="true" style="font-size:40px;color:#d1d5db;font-variation-settings:'FILL' 1,'wght' 400,'GRAD' 0,'opsz' 24">inventory_2</span>
            <p>Henüz sayım kaydı bulunmuyor</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="loading-container">
      <div class="loading-content">
        <div class="loading-spinner">
          <div class="spinner-ring"></div>
          <div class="spinner-ring"></div>
          <div class="spinner-ring"></div>
        </div>
        <p>Kasa detayları yükleniyor...</p>
      </div>
    </div>

    <!-- Deposit Dialog -->
    <Teleport to="body">
      <transition name="modal">
        <div v-if="showDepositDialog" class="modal-overlay" @click="showDepositDialog = false">
          <div class="modal-container" @click.stop>
            <div class="modal-header">
              <h2>Para Yatır</h2>
              <button @click="showDepositDialog = false" class="modal-close">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
            <form @submit.prevent="processDeposit" class="modal-form">
              <div class="form-group">
                <label class="form-label">Para Birimi <span class="required">*</span></label>
                <div class="custom-currency-select" @click.stop>
                  <div 
                    class="currency-select-trigger"
                    @click.stop="depositDropdownOpen = !depositDropdownOpen"
                    :class="{ 'disabled': currencies.length === 0 }"
                  >
                    <div v-if="depositForm.currencyId" class="selected-currency">
                      <template v-if="getSelectedCurrency(depositForm.currencyId)">
                        <i v-if="getCurrencyCountryCode(getSelectedCurrency(depositForm.currencyId).currencyCode)" :class="`fi fi-${getCurrencyCountryCode(getSelectedCurrency(depositForm.currencyId).currencyCode)}`"></i>
                        <span v-else-if="getSelectedCurrency(depositForm.currencyId).currencyCode === 'USDT'" class="currency-badge usdt">₮</span>
                        <span v-else class="currency-badge">{{ getSelectedCurrency(depositForm.currencyId).currencyCode.substring(0, 2) }}</span>
                      </template>
                      <span>{{ getSelectedCurrency(depositForm.currencyId)?.currencyCode }} - {{ getSelectedCurrency(depositForm.currencyId)?.currencyName }}</span>
                    </div>
                    <span v-else class="placeholder">Para birimi seçin</span>
                    <span class="material-symbols-outlined" aria-hidden="true">{{ depositDropdownOpen ? 'expand_less' : 'expand_more' }}</span>
                  </div>
                  <div v-if="depositDropdownOpen" class="currency-dropdown-list">
                    <div 
                      v-for="currency in currencies" 
                      :key="currency.currencyId || currency.id"
                      class="currency-dropdown-item"
                      @click.stop="selectDepositCurrency(currency)"
                    >
                      <i v-if="getCurrencyCountryCode(currency.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(currency.currencyCode)}`"></i>
                      <span v-else-if="currency.currencyCode === 'USDT'" class="currency-badge usdt">₮</span>
                      <span v-else class="currency-badge">{{ currency.currencyCode.substring(0, 2) }}</span>
                      <span class="currency-code">{{ currency.currencyCode }}</span>
                      <span class="currency-name">{{ currency.currencyName }}</span>
                    </div>
                  </div>
                </div>
              </div>
              <div class="form-group">
                <label class="form-label">Tutar <span class="required">*</span></label>
                <input v-model.number="depositForm.amount" type="number" min="0.01" step="0.01" class="form-input" placeholder="0.00">
              </div>
              <div class="form-group">
                <label class="form-label">Açıklama <span class="required">*</span></label>
                <textarea v-model="depositForm.notes" rows="3" class="form-textarea" placeholder="Bu para yatırma işleminin nedeni (zorunlu)" required></textarea>
              </div>
              <div class="form-actions">
                <button type="button" @click="showDepositDialog = false" class="btn-cancel">İptal</button>
                <button type="submit" class="btn-save" :disabled="isProcessing">
                  {{ isProcessing ? 'İşleniyor...' : 'Para Yatır' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      </transition>
    </Teleport>

    <!-- Withdraw Dialog -->
    <Teleport to="body">
      <transition name="modal">
        <div v-if="showWithdrawDialog" class="modal-overlay" @click="showWithdrawDialog = false">
          <div class="modal-container" @click.stop>
            <div class="modal-header">
              <h2>Para Çek</h2>
              <button @click="showWithdrawDialog = false" class="modal-close">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
            <form @submit.prevent="processWithdraw" class="modal-form">
              <div class="form-group">
                <label class="form-label">Para Birimi <span class="required">*</span></label>
                <div class="custom-currency-select" @click.stop>
                  <div 
                    class="currency-select-trigger"
                    @click.stop="withdrawDropdownOpen = !withdrawDropdownOpen"
                  >
                    <div v-if="withdrawForm.currencyId" class="selected-currency">
                      <template v-if="selectedWithdrawBalance">
                        <i v-if="getCurrencyCountryCode(selectedWithdrawBalance.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(selectedWithdrawBalance.currencyCode)}`"></i>
                        <span v-else-if="selectedWithdrawBalance.currencyCode === 'USDT'" class="currency-badge usdt">₮</span>
                        <span v-else class="currency-badge">{{ selectedWithdrawBalance.currencyCode?.substring(0, 2) }}</span>
                      </template>
                      <span>{{ selectedWithdrawBalance?.currencyCode }} - Kullanılabilir: {{ formatAmount(selectedWithdrawBalance?.availableBalance || 0) }}</span>
                    </div>
                    <span v-else class="placeholder">Para birimi seçin</span>
                    <span class="material-symbols-outlined" aria-hidden="true">{{ withdrawDropdownOpen ? 'expand_less' : 'expand_more' }}</span>
                  </div>
                  <div v-if="withdrawDropdownOpen" class="currency-dropdown-list">
                    <div 
                      v-for="balance in availableBalances" 
                      :key="balance.currencyId"
                      class="currency-dropdown-item"
                      @click.stop="selectWithdrawCurrency(balance)"
                    >
                      <i v-if="getCurrencyCountryCode(balance.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(balance.currencyCode)}`"></i>
                      <span v-else-if="balance.currencyCode === 'USDT'" class="currency-badge usdt">₮</span>
                      <span v-else class="currency-badge">{{ balance.currencyCode.substring(0, 2) }}</span>
                      <span class="currency-code">{{ balance.currencyCode }}</span>
                      <span class="currency-name">Kullanılabilir: {{ formatAmount(balance.availableBalance || balance.balance) }}</span>
                    </div>
                  </div>
                </div>
              </div>
              <div class="form-group">
                <label class="form-label">Tutar <span class="required">*</span></label>
                <input v-model.number="withdrawForm.amount" type="number" min="0.01" step="0.01" class="form-input" placeholder="0.00">
              </div>
              <div class="form-group">
                <label class="form-label">Açıklama <span class="required">*</span></label>
                <textarea v-model="withdrawForm.notes" rows="3" class="form-textarea" placeholder="Bu para çekme işleminin nedeni (zorunlu)" required></textarea>
              </div>
              <div class="form-actions">
                <button type="button" @click="showWithdrawDialog = false" class="btn-cancel">İptal</button>
                <button type="submit" class="btn-save" :disabled="isProcessing">
                  {{ isProcessing ? 'İşleniyor...' : 'Para Çek' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      </transition>
    </Teleport>

    <!-- Balance Update Dialog -->
    <Teleport to="body">
      <transition name="modal">
        <div v-if="updatingBalance" class="modal-overlay" @click="updatingBalance = null">
          <div class="modal-container small" @click.stop>
            <div class="modal-header">
              <h2>Bakiye Güncelle</h2>
              <button @click="updatingBalance = null" class="modal-close">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
            <form @submit.prevent="saveBalanceUpdate" class="modal-form">
              <div class="balance-update-info">
                <div class="info-item">
                  <span class="info-label">Para Birimi</span>
                  <span class="info-value currency">
                    <i v-if="getCurrencyCountryCode(updatingBalance.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(updatingBalance.currencyCode)}`"></i>
                    <span v-else class="currency-badge">{{ updatingBalance.currencyCode }}</span>
                    {{ updatingBalance.currencyCode }}
                  </span>
                </div>
                <div class="info-item">
                  <span class="info-label">Mevcut Bakiye</span>
                  <span class="info-value amount">{{ formatAmount(updatingBalance.balance) }}</span>
                </div>
              </div>
              <div class="form-group">
                <label class="form-label">Yeni Bakiye <span class="required">*</span></label>
                <input v-model.number="balanceUpdateAmount" type="number" min="0" step="0.01" class="form-input large" placeholder="0.00" required autofocus>
              </div>
              <div class="form-actions">
                <button type="button" @click="updatingBalance = null" class="btn-cancel">Cancel</button>
                <button type="submit" class="btn-save">Update Balance</button>
              </div>
            </form>
          </div>
        </div>
      </transition>
    </Teleport>

    <!-- Edit Vault Dialog -->
    <Teleport to="body">
      <transition name="modal">
        <div v-if="showEditDialog" class="modal-overlay" @click="showEditDialog = false">
          <div class="modal-container" @click.stop>
            <div class="modal-header">
              <h2>Kasa Düzenle</h2>
              <button @click="showEditDialog = false" class="modal-close">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
            <form @submit.prevent="saveVault" class="modal-form">
              <div class="form-group">
                <label class="form-label">Kasa Adı <span class="required">*</span></label>
                <input v-model="editForm.vaultName" type="text" class="form-input" placeholder="Kasa adını girin" required>
              </div>
              <div class="form-group">
                <label class="form-label">Ofis <span class="required">*</span></label>
                <select v-model="editForm.officeId" class="form-select" required>
                  <option value="">Ofis seçin</option>
                  <option v-for="office in offices" :key="office.officeId" :value="office.officeId">
                    {{ office.officeName }}
                  </option>
                </select>
              </div>
              <div class="form-group">
                <label class="form-label">Açıklama</label>
                <textarea v-model="editForm.description" rows="3" class="form-textarea" placeholder="İsteğe bağlı açıklama"></textarea>
              </div>
              <div class="form-group">
                <label class="form-check">
                  <input v-model="editForm.isActive" type="checkbox" class="form-checkbox">
                  <span>Aktif</span>
                </label>
              </div>
              <div class="form-actions">
                <button type="button" @click="showEditDialog = false" class="btn-cancel">Cancel</button>
                <button type="submit" class="btn-save">Kasayı Güncelle</button>
              </div>
            </form>
          </div>
        </div>
      </transition>
    </Teleport>

    <!-- Transfer Dialog -->
    <Teleport to="body">
      <transition name="modal">
        <div v-if="showTransferDialog" class="modal-overlay" @click="showTransferDialog = false">
          <div class="modal-container" @click.stop>
            <div class="modal-header">
              <h2>Başka Kasaya Transfer</h2>
              <button @click="showTransferDialog = false" class="modal-close">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
            <form @submit.prevent="processTransfer" class="modal-form">
              <div class="form-group">
                <label class="form-label">Hedef Kasa <span class="required">*</span></label>
                <select v-model="transferForm.targetVaultId" class="form-select" required>
                  <option value="">Hedef kasa seçin</option>
                  <option 
                    v-for="v in otherVaults" 
                    :key="v.vaultId || v.id" 
                    :value="v.vaultId || v.id"
                  >
                    {{ v.vaultName }}
                  </option>
                </select>
              </div>
              <div class="form-group">
                <label class="form-label">Para Birimi <span class="required">*</span></label>
                <div class="custom-currency-select" @click.stop>
                  <div 
                    class="currency-select-trigger"
                    @click.stop="transferDropdownOpen = !transferDropdownOpen"
                  >
                    <div v-if="transferForm.currencyId" class="selected-currency">
                      <template v-if="selectedTransferBalance">
                        <i v-if="getCurrencyCountryCode(selectedTransferBalance.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(selectedTransferBalance.currencyCode)}`"></i>
                        <span v-else-if="selectedTransferBalance.currencyCode === 'USDT'" class="currency-badge usdt">₮</span>
                        <span v-else class="currency-badge">{{ selectedTransferBalance.currencyCode?.substring(0, 2) }}</span>
                      </template>
                      <span>{{ selectedTransferBalance?.currencyCode }} - Kullanılabilir: {{ formatAmount(selectedTransferBalance?.availableBalance || 0) }}</span>
                    </div>
                    <span v-else class="placeholder">Para birimi seçin</span>
                    <span class="material-symbols-outlined" aria-hidden="true">{{ transferDropdownOpen ? 'expand_less' : 'expand_more' }}</span>
                  </div>
                  <div v-if="transferDropdownOpen" class="currency-dropdown-list">
                    <div 
                      v-for="balance in availableBalances" 
                      :key="balance.currencyId"
                      class="currency-dropdown-item"
                      @click.stop="selectTransferCurrency(balance)"
                    >
                      <i v-if="getCurrencyCountryCode(balance.currencyCode)" :class="`fi fi-${getCurrencyCountryCode(balance.currencyCode)}`"></i>
                      <span v-else-if="balance.currencyCode === 'USDT'" class="currency-badge usdt">₮</span>
                      <span v-else class="currency-badge">{{ balance.currencyCode.substring(0, 2) }}</span>
                      <span class="currency-code">{{ balance.currencyCode }}</span>
                      <span class="currency-name">Kullanılabilir: {{ formatAmount(balance.availableBalance || balance.balance) }}</span>
                    </div>
                  </div>
                </div>
              </div>
              <div class="form-group">
                <label class="form-label">Amount <span class="required">*</span></label>
                <input v-model.number="transferForm.amount" type="number" min="0.01" step="0.01" class="form-input" placeholder="0.00">
              </div>
              <div class="form-group">
                <label class="form-label">Notes</label>
                <textarea v-model="transferForm.notes" rows="3" class="form-textarea" placeholder="Optional transfer notes"></textarea>
              </div>
              <div class="form-actions">
                <button type="button" @click="showTransferDialog = false" class="btn-cancel">Cancel</button>
                <button type="submit" class="btn-save" :disabled="isProcessing">
                  {{ isProcessing ? 'Processing...' : 'Transfer' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      </transition>
    </Teleport>

    <!-- Delete Confirmation Dialog -->
    <Teleport to="body">
      <transition name="modal">
        <div v-if="showDeleteDialog" class="modal-overlay" @click="showDeleteDialog = false">
          <div class="modal-container small" @click.stop>
            <div class="modal-header delete-header">
              <h2>Kasayı Sil</h2>
              <button @click="showDeleteDialog = false; deleteConfirmText = ''" class="modal-close">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
            <div class="modal-form">
              <div class="delete-warning">
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                <h3>Bu işlem geri alınamaz!</h3>
                <p>{{ vault?.vaultName }} kasasını ve tüm işlem geçmişini kalıcı olarak silmek üzeresiniz.</p>
                <p class="confirm-instruction">Onaylamak için <strong>{{ vault?.vaultName }}</strong> yazın:</p>
              </div>
              <div class="form-group">
                <input 
                  v-model="deleteConfirmText" 
                  type="text" 
                  class="form-input delete-confirm-input" 
                  :placeholder="vault?.vaultName"
                  @keyup.enter="deleteVault"
                >
              </div>
              <div class="form-actions">
                <button type="button" @click="showDeleteDialog = false; deleteConfirmText = ''" class="btn-cancel">İptal</button>
                <button 
                  type="button" 
                  @click="deleteVault" 
                  class="btn-delete"
                  :disabled="deleteConfirmText !== vault?.vaultName"
                >
                  Kasayı Sil
                </button>
              </div>
            </div>
          </div>
        </div>
      </transition>
    </Teleport>
    
    <!-- Inter-Office Exchange Modal -->
    <InterOfficeExchange
      v-if="vault"
      ref="interOfficeExchangeRef"
      :source-vault-id="vault.vaultId || vault.id"
      :source-vault-name="vault.vaultName"
      :source-office-id="vault.officeId"
      :source-office-name="officeName"
      @complete="loadVaultData"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import apiService from '@/services/apiservice'
import { useAuthStore } from '@/stores/auth'
import type { Vault, OfficeSummary } from '@/types/api'
import { getCurrencyCountryCode, formatCurrency, formatAmount } from '@/utils/currency'
import { useNotification } from '@/composables/useNotification'
import InterOfficeExchange from '@/views/InterOfficeExchange.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const notification = useNotification()

// State
const vault = ref<Vault | null>(null)
const offices = ref<OfficeSummary[]>([])
const currencies = ref<any[]>([])
const otherVaults = ref<Vault[]>([])
const isLoading = ref(false)
const isLoadingHistory = ref(false)
const isProcessing = ref(false)
const selectedBalanceRange = ref('all')
const selectedTypeFilter = ref('all')
const showExchangeHistories = ref(false)
const viewMode = ref<'list' | 'grid'>('grid')
const activeSection = ref<'balances' | 'history' | 'count'>('balances')

// Count state
const countDetails = ref<Array<{ currencyId: string, currencyCode: string, currencyName: string, systemAmount: number, actualAmount: number | null }>>([])
const countHistory = ref<any[]>([])
const isLoadingCounts = ref(false)
const isSavingCount = ref(false)
const showCountForm = ref(false)
const countAlsoSnapshot = ref(true)
const countSnapshotDesc = ref('')

// Dialogs
const showDepositDialog = ref(false)
const showWithdrawDialog = ref(false)
const showTransferDialog = ref(false)
const showInterOfficeDialog = ref(false)
const interOfficeExchangeRef = ref<any>(null)
const showEditDialog = ref(false)
const showDeleteDialog = ref(false)
const deleteConfirmText = ref('')
const updatingBalance = ref<any>(null)
const balanceUpdateAmount = ref(0)

// Dropdown states
const depositDropdownOpen = ref(false)
const withdrawDropdownOpen = ref(false)
const transferDropdownOpen = ref(false)

// Forms
const depositForm = ref({
  currencyId: null as string | null,
  amount: 0,
  notes: ''
})

const withdrawForm = ref({
  currencyId: null as string | null,
  amount: 0,
  notes: ''
})

const transferForm = ref({
  targetVaultId: '',
  currencyId: null as string | null,
  amount: 0,
  notes: ''
})

const editForm = ref({
  vaultName: '',
  officeId: '',
  description: '',
  isActive: true
})

// Time ranges
const balanceHistoryRanges = [
  { label: 'Bugün', value: 'today' },
  { label: 'Bu Hafta', value: 'week' },
  { label: 'Bu Ay', value: 'month' },
  { label: 'Bu Yıl', value: 'year' },
  { label: 'Tümü', value: 'all' }
]

const balanceTypeFilters = [
  { label: 'Tümü', value: 'all' },
  { label: 'Para Yatırma', value: 'deposit' },
  { label: 'Para Çekme', value: 'withdrawal' }
]

// Computed
const officeName = computed(() => {
  if (!vault.value) return ''
  if ('officeName' in vault.value && vault.value.officeName) return vault.value.officeName
  
  const office = offices.value.find(o => 
    o.vaults.some(v => (v.vaultId || v.id) === (vault.value?.vaultId || vault.value?.id))
  )
  return office?.officeName || 'Bilinmeyen Ofis'
})

const availableBalances = computed(() => {
  if (!vault.value?.balances) return []
  return vault.value.balances.filter(b => (b.availableBalance || b.balance) > 0)
})

// Kasa seçici: aktif kasa + diğer kasalar tek listede, isme göre sıralı.
const vaultSwitcherOptions = computed(() => {
  const current = vault.value
  const activeId = current ? (current.vaultId || current.id) : null
  const list = current ? [current, ...otherVaults.value] : otherVaults.value
  return list
    .filter((v: any) => v)
    .sort((a: any, b: any) => (a.vaultName || '').localeCompare(b.vaultName || ''))
    .map((v: any) => ({ id: v.vaultId || v.id, name: v.vaultName, isActive: (v.vaultId || v.id) === activeId }))
})

function switchVault(vaultId: string) {
  const currentId = vault.value ? (vault.value.vaultId || vault.value.id) : null
  if (!vaultId || vaultId === currentId) return
  localStorage.setItem('selectedVaultId', vaultId)
  router.push(`/ihtiyar/vaults/${vaultId}`)
}

const selectedWithdrawBalance = computed(() =>
  availableBalances.value.find(b => b.currencyId === withdrawForm.value.currencyId) ?? null
)

const selectedTransferBalance = computed(() =>
  availableBalances.value.find(b => b.currencyId === transferForm.value.currencyId) ?? null
)

const filteredBalanceHistory = computed(() => {
  if (!vault.value?.balanceHistories) return []
  
  let histories = vault.value.balanceHistories
  
  // Filter by transaction type
  if (!showExchangeHistories.value) {
    histories = histories.filter(h => h.transactionType !== 1) // Exclude Exchange histories
  }
  
  if (selectedTypeFilter.value !== 'all') {
    if (selectedTypeFilter.value === 'deposit') {
      histories = histories.filter(h => h.transactionType === 2) // Only Deposits
    } else if (selectedTypeFilter.value === 'withdrawal') {
      histories = histories.filter(h => h.transactionType === 3) // Only Withdrawals
    }
  }
  
  // Filter by date range
  if (selectedBalanceRange.value !== 'all') {
    const dateRange = getDateRange(selectedBalanceRange.value)
    const startDate = new Date(dateRange.startDate)
    const endDate = new Date(dateRange.endDate || new Date())
    
    histories = histories.filter(history => {
      const historyDate = new Date(history.createdDate || '')
      return historyDate >= startDate && historyDate <= endDate
    })
  }
  
  return histories.sort((a, b) => new Date(b.createdDate || '').getTime() - new Date(a.createdDate || '').getTime())
})

// Methods
const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleDateString('tr-TR', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

const formatTime = (dateString: string) => {
  return new Date(dateString).toLocaleTimeString('tr-TR', {
    hour: '2-digit',
    minute: '2-digit'
  })
}

const getDateRange = (range: string) => {
  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  
  switch (range) {
    case 'today':
      return {
        startDate: today.toISOString(),
        endDate: new Date(today.getTime() + 24 * 60 * 60 * 1000).toISOString()
      }
    case 'week':
      const weekStart = new Date(today)
      weekStart.setDate(today.getDate() - today.getDay())
      return {
        startDate: weekStart.toISOString(),
        endDate: new Date().toISOString()
      }
    case 'month':
      return {
        startDate: new Date(now.getFullYear(), now.getMonth(), 1).toISOString(),
        endDate: new Date().toISOString()
      }
    case 'year':
      return {
        startDate: new Date(now.getFullYear(), 0, 1).toISOString(),
        endDate: new Date().toISOString()
      }
    default:
      return { startDate: new Date(0).toISOString(), endDate: new Date().toISOString() }
  }
}

const getTransactionTypeClass = (type: number) => {
  const types: Record<number, string> = {
    1: 'exchange',
    2: 'deposit',
    3: 'withdrawal',
    4: 'transfer',
    5: 'adjustment'
  }
  return types[type] || 'unknown'
}

const getTransactionTypeName = (type: number) => {
  const types: Record<number, string> = {
    1: 'Döviz',
    2: 'Para Yatırma',
    3: 'Para Çekme',
    4: 'Transfer',
    5: 'Düzeltme'
  }
  return types[type] || 'Bilinmeyen'
}

const getTransactionStatusClass = (status: number) => {
  const statuses: Record<number, string> = {
    1: 'pending',
    2: 'completed',
    3: 'cancelled',
    4: 'failed'
  }
  return statuses[status] || 'unknown'
}

const getTransactionStatusName = (status: number) => {
  const statuses: Record<number, string> = {
    1: 'Beklemede',
    2: 'Tamamlandı',
    3: 'İptal Edildi',
    4: 'Başarısız'
  }
  return statuses[status] || 'Bilinmeyen'
}

const loadVaultData = async (overrideId?: string) => {
  const vaultId = overrideId || (route.params.id as string)
  if (!vaultId) return

  isLoading.value = true
  try {
    const [vaultData, officesData, currenciesData, vaultsData] = await Promise.all([
      apiService.getVaultById(vaultId),
      apiService.getOfficeSummaries(),
      apiService.getCurrencies(),
      apiService.getVaults()
    ])

    vault.value = vaultData.data || vaultData
    offices.value = officesData.data || officesData
    currencies.value = currenciesData.data || currenciesData
    const allVaults = vaultsData.data || vaultsData
    otherVaults.value = allVaults.filter((v: any) => (v.vaultId || v.id) !== vaultId)
  } catch (error) {
    console.error('Failed to load vault data:', error)
    notification.error('Kasa verileri yüklenemedi')
  } finally {
    isLoading.value = false
  }
}


const refresh = () => {
  loadVaultData()
  if (activeSection.value === 'count') loadCountHistory()
}

// ═══ Vault Count Methods ═══
async function loadCountHistory() {
  if (!vault.value) return
  isLoadingCounts.value = true
  try {
    const vaultId = (vault.value as any).vaultId || vault.value.id
    const data = await apiService.getVaultCounts(vaultId)
    countHistory.value = Array.isArray(data) ? data : (data?.items ?? [])
  } catch {
    countHistory.value = []
  } finally {
    isLoadingCounts.value = false
  }
}

function openCountForm() {
  if (!vault.value?.balances) return
  countDetails.value = vault.value.balances.map((b: any) => ({
    currencyId: b.currencyId,
    currencyCode: b.currencyCode ?? b.currencyName ?? '',
    currencyName: b.currencyName ?? b.currencyCode ?? '',
    systemAmount: b.balance ?? 0,
    actualAmount: null,
  }))
  countAlsoSnapshot.value = true
  countSnapshotDesc.value = ''
  showCountForm.value = true
}

function getCountDiscrepancy(detail: any): number {
  return (detail.actualAmount ?? 0) - (detail.systemAmount ?? 0)
}

async function submitVaultCount() {
  if (!vault.value) return
  const details = countDetails.value
    .filter(d => d.actualAmount !== null && d.actualAmount !== undefined)
    .map(d => ({ CurrencyId: d.currencyId, ActualAmount: d.actualAmount }))

  if (details.length === 0) {
    notification.warning('En az bir para birimi için sayım tutarı giriniz')
    return
  }

  isSavingCount.value = true
  try {
    const vaultId = (vault.value as any).vaultId || vault.value.id
    await apiService.submitVaultCount({
      VaultId: vaultId,
      IsManual: true,
      CountDetails: details,
    })

    if (countAlsoSnapshot.value) {
      const office = offices.value.find(o =>
        o.vaults.some(v => (v.vaultId || v.id) === vaultId)
      )
      if (office) {
        await apiService.createVaultSnapshot({
          OfficeId: (office as any).officeId || office.id,
          Description: countSnapshotDesc.value || `Sayım — ${vault.value.vaultName}`,
        }).catch(() => {})
      }
    }

    showCountForm.value = false
    notification.success('Sayım başarıyla kaydedildi')
    await loadCountHistory()
    await loadVaultData()
  } catch (e: any) {
    notification.error(e?.response?.data?.message || 'Sayım kaydedilemedi')
  } finally {
    isSavingCount.value = false
  }
}

const goBack = () => {
  router.push({ name: 'Vaults' })
}

const editVault = () => {
  if (!vault.value) return
  
  editForm.value = {
    vaultName: vault.value.vaultName || '',
    description: vault.value.description || '',
    officeId: '',
    isActive: vault.value.isActive
  }
  
  const office = offices.value.find(o => 
    o.vaults.some(v => (v.vaultId || v.id) === (vault.value?.vaultId || vault.value?.id))
  )
  if (office) {
    editForm.value.officeId = String(office.officeId)
  }
  
  showEditDialog.value = true
}

const saveVault = async () => {
  if (!vault.value) return
  
  try {
    const data = {
      id: vault.value.vaultId || vault.value.id,
      name: editForm.value.vaultName,
      description: editForm.value.description,
      officeId: editForm.value.officeId,
      isActive: editForm.value.isActive
    }
    
    await apiService.saveVault(data)
    showEditDialog.value = false
    await loadVaultData()
    notification.success('Kasa başarıyla güncellendi')
  } catch (error) {
    notification.error('Kasa güncellenemedi')
  }
}

const confirmDelete = () => {
  if (!vault.value) return
  showDeleteDialog.value = true
}

const openInterOfficeExchange = () => {
  if (!vault.value) return
  if (interOfficeExchangeRef.value) {
    interOfficeExchangeRef.value.open()
  }
}

const deleteVault = async () => {
  if (!vault.value || deleteConfirmText.value !== vault.value.vaultName) return
  
  try {
    const vaultId = vault.value.vaultId || vault.value.id
    await apiService.deleteVault(vaultId)
    notification.success('Kasa başarıyla silindi')
    router.push({ name: 'VaultManagement' })
  } catch (error) {
    notification.error('Kasa silinemedi')
  }
}

const updateBalance = (balance: any) => {
  updatingBalance.value = balance
  balanceUpdateAmount.value = balance.balance
}

const saveBalanceUpdate = async () => {
  if (!updatingBalance.value || !vault.value) return
  
  try {
    const vaultId = vault.value.vaultId || vault.value.id
    
    await apiService.updateVaultBalance({
      vaultId: vaultId,
      currencyId: updatingBalance.value.currencyId,
      amount: balanceUpdateAmount.value,
      isEntireBalance: true
    })
    
    updatingBalance.value = null
    await loadVaultData()
    notification.success('Bakiye başarıyla güncellendi')
  } catch (error) {
    notification.error('Bakiye güncellenemedi')
  }
}

const processDeposit = async () => {
  if (isProcessing.value || !vault.value) return

  // Manual validation
  if (!depositForm.value.currencyId) {
    notification.error('Lütfen bir para birimi seçin')
    return
  }

  if (!depositForm.value.amount || depositForm.value.amount <= 0) {
    notification.error('Lütfen geçerli bir tutar girin')
    return
  }

  if (!depositForm.value.notes || !depositForm.value.notes.trim()) {
    notification.error('Lütfen bu para yatırma işlemi için bir açıklama girin')
    return
  }

  isProcessing.value = true
  try {
    // API endpoint for deposit would be implemented here
    // For now, we'll use the balance update endpoint
    await apiService.updateVaultBalance({
      vaultId: vault.value.vaultId || vault.value.id,
      currencyId: depositForm.value.currencyId,
      amount: depositForm.value.amount,
      isEntireBalance: false,
      description: depositForm.value.notes.trim()
    })
    
    showDepositDialog.value = false
    depositForm.value = { currencyId: null, amount: 0, notes: '' }
    await loadVaultData()
    notification.success('Para yatırma işlemi başarıyla tamamlandı')
  } catch (error) {
    notification.error('Para yatırma işlemi başarısız')
  } finally {
    isProcessing.value = false
  }
}

const processWithdraw = async () => {
  if (isProcessing.value || !vault.value) return

  // Manual validation
  if (!withdrawForm.value.currencyId) {
    notification.error('Lütfen bir para birimi seçin')
    return
  }

  if (!withdrawForm.value.amount || withdrawForm.value.amount <= 0) {
    notification.error('Lütfen geçerli bir tutar girin')
    return
  }

  const withdrawAvailable = selectedWithdrawBalance.value?.availableBalance ?? selectedWithdrawBalance.value?.balance ?? 0
  if (withdrawForm.value.amount > withdrawAvailable) {
    notification.error(`Yetersiz bakiye: kasada ${formatAmount(withdrawAvailable)} ${selectedWithdrawBalance.value?.currencyCode ?? ''} var`)
    return
  }

  if (!withdrawForm.value.notes || !withdrawForm.value.notes.trim()) {
    notification.error('Lütfen bu para çekme işlemi için bir açıklama girin')
    return
  }

  isProcessing.value = true
  try {
    // API endpoint for withdrawal would be implemented here
    // For now, we'll use the balance update endpoint with negative amount
    await apiService.updateVaultBalance({
      vaultId: vault.value.vaultId || vault.value.id,
      currencyId: withdrawForm.value.currencyId,
      amount: -withdrawForm.value.amount,
      isEntireBalance: false,
      description: withdrawForm.value.notes.trim()
    })
    
    showWithdrawDialog.value = false
    withdrawForm.value = { currencyId: null, amount: 0, notes: '' }
    await loadVaultData()
    notification.success('Para çekme işlemi başarıyla tamamlandı')
  } catch (error) {
    notification.error('Para çekme işlemi başarısız')
  } finally {
    isProcessing.value = false
  }
}

const processTransfer = async () => {
  if (isProcessing.value || !vault.value) return

  // Manual validation
  if (!transferForm.value.targetVaultId) {
    notification.error('Lütfen hedef kasa seçin')
    return
  }

  if (!transferForm.value.currencyId) {
    notification.error('Lütfen bir para birimi seçin')
    return
  }

  if (!transferForm.value.amount || transferForm.value.amount <= 0) {
    notification.error('Lütfen geçerli bir tutar girin')
    return
  }

  const transferAvailable = selectedTransferBalance.value?.availableBalance ?? selectedTransferBalance.value?.balance ?? 0
  if (transferForm.value.amount > transferAvailable) {
    notification.error(`Yetersiz bakiye: kasada ${formatAmount(transferAvailable)} ${selectedTransferBalance.value?.currencyCode ?? ''} var`)
    return
  }

  isProcessing.value = true
  try {
    await apiService.transferBetweenVaults({
      sourceVaultId: vault.value.vaultId || vault.value.id,
      targetVaultId: transferForm.value.targetVaultId,
      currencyId: transferForm.value.currencyId,
      amount: transferForm.value.amount,
      notes: transferForm.value.notes
    })
    
    showTransferDialog.value = false
    transferForm.value = { targetVaultId: '', currencyId: null, amount: 0, notes: '' }
    await loadVaultData()
    notification.success('Transfer başarıyla tamamlandı')
  } catch (error: any) {
    notification.error(error.response?.data?.error || 'Transfer başarısız')
  } finally {
    isProcessing.value = false
  }
}


// Helper methods
const getSelectedCurrency = (currencyId: string | null) => {
  if (!currencyId) return null
  return currencies.value.find(c => (c.currencyId || c.id) === currencyId)
}

const selectDepositCurrency = (currency: any) => {
  depositForm.value.currencyId = currency.currencyId || currency.id
  depositDropdownOpen.value = false
}

const selectWithdrawCurrency = (balance: any) => {
  withdrawForm.value.currencyId = balance.currencyId
  withdrawDropdownOpen.value = false
}

const selectTransferCurrency = (balance: any) => {
  transferForm.value.currencyId = balance.currencyId
  transferDropdownOpen.value = false
}

// Close dropdowns when clicking outside
const closeDropdowns = () => {
  depositDropdownOpen.value = false
  withdrawDropdownOpen.value = false
  transferDropdownOpen.value = false
}

async function loadForCurrentRoute() {
  if (!route.params.id) {
    try {
      // Kullanıcı daha önce kasa sekmesinden bir kasa seçtiyse, o kasayı hatırla —
      // yalnızca hiç seçim yokken (ilk giriş) ofis/ilk-kasa fallback'ine düş.
      const rememberedVaultId = localStorage.getItem('selectedVaultId')
      if (rememberedVaultId) {
        router.replace(`/ihtiyar/vaults/${rememberedVaultId}`)
        await loadVaultData(rememberedVaultId)
        return
      }

      const userOfficeId = localStorage.getItem('selectedOfficeId')
      let allVaults: any[] = []
      if (userOfficeId) {
        try {
          const officeVaults = await apiService.getVaultsByOfficeId(userOfficeId)
          allVaults = officeVaults.data || officeVaults
          if (!Array.isArray(allVaults)) allVaults = []
        } catch { allVaults = [] }
      }
      if (allVaults.length === 0) {
        const vaultsData = await apiService.getVaults()
        allVaults = vaultsData.data || vaultsData
      }
      if (allVaults && allVaults.length > 0) {
        const firstId = allVaults[0].vaultId || allVaults[0].id
        if (!firstId) return
        router.replace(`/ihtiyar/vaults/${firstId}`)
        await loadVaultData(firstId)
      }
    } catch (e) {
      console.error('Failed to load vaults list', e)
    }
  } else {
    loadVaultData()
  }
}

// Lifecycle
onMounted(async () => {
  document.addEventListener('click', closeDropdowns)
  await loadForCurrentRoute()
})

// Router aynı bileşeni yeniden kullandığından (vaults/:id?), "Geri" gibi
// sadece param'ı değiştiren navigasyonlarda onMounted tekrar tetiklenmez —
// bu yüzden route.params.id'yi ayrıca izlememiz gerekiyor.
watch(() => route.params.id, () => {
  loadForCurrentRoute()
})

onUnmounted(() => {
  document.removeEventListener('click', closeDropdowns)
})
</script>

<style scoped>
.vault-view {
  min-height: 100vh;
  background: #f8f9fb;
}

/* Vault Header */
.vault-header {
  position: sticky;
  top: 0;
  z-index: 100;
  background: linear-gradient(135deg, #1e293b 0%, #334155 100%);
  color: white;
  padding: 1.5rem 0 2rem;
  overflow: hidden;
  box-shadow: var(--shadow-bold);
  transition: box-shadow 0.3s ease;
}

.header-background {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  overflow: hidden;
}

.header-pattern {
  position: absolute;
  top: -50%;
  right: -10%;
  width: 60%;
  height: 200%;
  background: radial-gradient(circle, rgba(255,255,255,0.05) 1px, transparent 1px);
  background-size: 50px 50px;
  transform: rotate(30deg);
}

.header-content {
  position: relative;
  max-width: 1400px;
  margin: 0 auto;
  padding: 0 2rem;
  display: flex;
  align-items: flex-start;
  gap: 2rem;
}

.vault-switcher {
  position: relative;
  max-width: 1400px;
  margin: 1rem auto 0;
  padding: 0 2rem;
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.vault-switcher-tab {
  padding: 0.5rem 1.125rem;
  background: rgba(255, 255, 255, 0.12);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: var(--radius-md);
  color: rgba(255, 255, 255, 0.85);
  font-weight: 500;
  font-size: 0.875rem;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
}

.vault-switcher-tab:hover {
  background: rgba(255, 255, 255, 0.2);
}

.vault-switcher-tab.active {
  background: white;
  color: var(--color-primary, #4f46e5);
  border-color: white;
}

.back-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  background: rgba(255, 255, 255, 0.15);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: var(--radius-md);
  color: white;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s ease, transform 0.2s ease;
}

.back-button:hover {
  background: rgba(255, 255, 255, 0.2);
  transform: translateX(-4px);
}

.vault-identity {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 2rem;
}

.vault-icon-wrapper {
  position: relative;
}

.vault-icon {
  width: 80px;
  height: 80px;
  background: rgba(255, 255, 255, 0.15);
  border-radius: var(--radius-xl);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
}

.status-badge {
  position: absolute;
  bottom: -8px;
  right: -8px;
  padding: 0.375rem 0.875rem;
  border-radius: var(--radius-xl);
  font-size: 0.75rem;
  font-weight: 600;
}

.status-badge.active {
  background: rgba(16, 185, 129, 0.9);
  color: white;
}

.status-badge.inactive {
  background: rgba(239, 68, 68, 0.9);
  color: white;
}

.vault-details {
  flex: 1;
}

.vault-name {
  margin: 0;
  font-size: 2.5rem;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.vault-meta {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-top: 0.75rem;
  opacity: 0.9;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
}

.meta-divider {
  opacity: 0.5;
}

.refresh-button {
  padding: 0.75rem;
  background: rgba(255, 255, 255, 0.15);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: var(--radius-md);
  color: white;
  cursor: pointer;
  transition: background-color 0.2s ease, transform 0.2s ease;
}

.refresh-button:hover:not(:disabled) {
  background: rgba(255, 255, 255, 0.2);
  transform: rotate(90deg);
}

.refresh-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.rotating {
  animation: rotate 1s linear infinite;
}

@keyframes rotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

/* Content */
.vault-content {
  max-width: 1400px;
  margin: -2rem auto 0;
  padding: 0 2rem 3rem;
  position: relative;
  z-index: 10;
}

/* Section Cards */
.quick-actions-card,
.balances-card,
.history-card {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  padding: 2rem;
  box-shadow: var(--shadow-bold);
  margin-bottom: 2rem;
}

.section-title {
  margin: 0 0 1.5rem;
  padding-left: 0.75rem;
  border-left: 5px solid var(--color-primary);
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-text);
}

/* Quick Actions */
.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 1rem;
}

.action-button {
  background: var(--color-bg-card);
  border: 2px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: 1.5rem 1rem;
  cursor: pointer;
  transition: background-color 0.3s ease, border-color 0.3s ease, box-shadow 0.3s ease, transform 0.3s ease;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.75rem;
  width: 100%;
  min-height: 140px;
  font-family: inherit;
  font-size: inherit;
  color: inherit;
  -webkit-appearance: none;
  -moz-appearance: none;
  appearance: none;
  outline: none;
}

.action-button:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-bold);
}

.action-icon {
  width: 56px;
  height: 56px;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background-color 0.3s ease, transform 0.3s ease;
  padding: 12px;
}

.action-button.deposit .action-icon {
  background: linear-gradient(135deg, #10b981 0%, var(--color-success) 100%);
  color: white;
}

.action-button.withdraw .action-icon {
  background: linear-gradient(135deg, #f59e0b 0%, var(--color-warning) 100%);
  color: white;
}

.action-button.transfer .action-icon {
  background: linear-gradient(135deg, var(--color-secondary) 0%, var(--color-secondary-hover) 100%);
  color: white;
}

.action-button.exchange .action-icon {
  background: linear-gradient(135deg, #14b8a6 0%, #0d9488 100%);
  color: white;
}

.action-button.edit .action-icon {
  background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
  color: white;
}

.action-button.delete .action-icon {
  background: linear-gradient(135deg, #ef4444 0%, var(--color-danger) 100%);
  color: white;
}

.action-button:hover .action-icon {
  transform: scale(1.1);
}

.action-label {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--color-text);
}

.action-description {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

/* Balances */
.balances-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.header-controls {
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.view-toggle {
  display: flex;
  gap: 0.25rem;
  background: var(--color-bg-page);
  padding: 0.25rem;
  border-radius: var(--radius-md);
}

.view-btn {
  padding: 0.5rem;
  border: none;
  background: transparent;
  border-radius: var(--radius-md);
  color: var(--color-text-muted);
  cursor: pointer;
  transition: background-color 0.2s ease, color 0.2s ease, box-shadow 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.view-btn:hover {
  color: var(--color-text-secondary);
}

.view-btn.active {
  background: var(--color-bg-card);
  color: var(--color-secondary);
  box-shadow: var(--shadow-glow-primary);
}

.total-section {
  text-align: right;
}

.total-label {
  display: block;
  font-size: 0.875rem;
  color: var(--color-text-secondary);
  margin-bottom: 0.25rem;
}

.total-amount {
  display: block;
  font-size: 2rem;
  font-weight: 700;
  color: #16a34a;
  letter-spacing: -0.02em;
}

/* Balances Table View */
.balances-table {
  overflow-x: auto;
  border-radius: var(--radius-lg);
  border: 1px solid var(--color-border);
  background: var(--color-bg-card);
  box-shadow: var(--shadow-md);
}

.balances-table table {
  width: 100%;
  border-collapse: collapse;
}

.balances-table th {
  background: linear-gradient(to bottom, #f8fafc, #f1f5f9);
  padding: 1rem 1.5rem;
  text-align: left;
  font-weight: 700;
  color: var(--color-text-secondary);
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border-bottom: 2px solid var(--border-strong);
  white-space: nowrap;
}

.balances-table th:first-child {
  border-top-left-radius: var(--radius-lg);
}

.balances-table th:last-child {
  border-top-right-radius: var(--radius-lg);
}

.balances-table td {
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid var(--color-bg-page);
  vertical-align: middle;
}

.balances-table tbody tr {
  transition: background-color 0.2s ease, transform 0.2s ease;
}

.balances-table tbody tr:hover {
  background: linear-gradient(to right, rgba(59, 130, 246, 0.02), rgba(59, 130, 246, 0.05));
  transform: translateX(2px);
}

.balances-table tbody tr.is-negative {
  background: linear-gradient(to right, rgba(239, 68, 68, 0.02), rgba(239, 68, 68, 0.04));
}

.balances-table tbody tr.is-negative:hover {
  background: linear-gradient(to right, rgba(239, 68, 68, 0.04), rgba(239, 68, 68, 0.08));
}

.currency-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.currency-info .fi {
  font-size: 1.5rem;
  line-height: 1;
}

.currency-text {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
}

.currency-text .currency-code {
  font-weight: 700;
  color: var(--color-text);
  font-size: 1rem;
}

.currency-text .currency-name {
  font-size: 0.75rem;
  color: var(--color-text-muted);
}

.balances-table .amount {
  font-family: 'SF Mono', Monaco, Consolas, monospace;
  font-weight: 600;
  font-size: 1rem;
  color: var(--color-text);
}

.balances-table .amount.negative {
  color: var(--color-danger);
}

.balances-table .amount.available {
  color: var(--color-success);
}

.balances-table .amount.in-base {
  color: var(--color-primary);
}

.empty-balances {
  text-align: center;
  padding: 4rem 2rem;
  color: var(--color-text-muted);
}

.empty-balances svg {
  margin-bottom: 1rem;
  opacity: 0.5;
}

.empty-balances p {
  font-size: 1rem;
  font-weight: 500;
}

/* Grid View */
.currencies-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1rem;
}

.currency-card {
  background: var(--color-bg-page);
  border-radius: var(--radius-lg);
  padding: 1.5rem;
  transition: background-color 0.3s ease, border-color 0.3s ease, box-shadow 0.3s ease;
  border: 1px solid transparent;
  border-left: 6px solid transparent;
}

.currency-card:hover {
  background: var(--color-bg-card);
  border-color: var(--color-border);
  border-left-color: var(--color-primary);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.currency-card.is-negative {
  background: #fef2f2;
  border-color: var(--color-danger-bg);
}

.currency-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.currency-identity {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.currency-identity .fi {
  font-size: 1.5rem;
}

.currency-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 2.5em;
  padding: 0.375em 0.625em;
  background: var(--color-border);
  border-radius: var(--radius-md);
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-text-secondary);
}

.currency-code {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--color-text);
}

.balance-update-btn {
  padding: 0.5rem;
  background: var(--color-bg-page);
  border: none;
  border-radius: var(--radius-md);
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: background-color 0.2s ease, color 0.2s ease;
}

.balance-update-btn:hover {
  background: var(--color-secondary);
  color: white;
  transform: scale(1.05);
}

.balance-details {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.balance-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.balance-row.primary {
  padding-bottom: 0.5rem;
  border-bottom: 1px solid rgba(0, 0, 0, 0.05);
  margin-bottom: 0.25rem;
}

.balance-label {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

.balance-value {
  font-weight: 600;
  color: var(--color-text);
  font-family: 'SF Mono', Monaco, Consolas, monospace;
}

.balance-value.available {
  color: #16a34a;
}

.balance-value.in-base {
  color: var(--color-text-secondary);
  font-size: 0.875rem;
}

.unrealized-pos { color: #16a34a; }
.unrealized-neg { color: var(--color-danger, #dc2626); }

/* Balance History */
.history-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.history-controls {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.time-filter,
.type-filter {
  display: flex;
  gap: 0.5rem;
  background: var(--color-bg-page);
  padding: 0.25rem;
  border-radius: var(--radius-lg);
}

.time-btn,
.filter-btn {
  padding: 0.5rem 1rem;
  border: none;
  background: transparent;
  border-radius: var(--radius-md);
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: background-color 0.2s ease, color 0.2s ease;
}

.time-btn:hover,
.filter-btn:hover {
  color: var(--color-text);
}

.time-btn.active,
.filter-btn.active {
  background: var(--color-bg-card);
  color: var(--color-text);
  box-shadow: var(--shadow-md);
}

.show-exchanges-check {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  font-size: 0.875rem;
  color: var(--color-text-secondary);
  font-weight: 500;
}

.show-exchanges-check:hover {
  color: var(--color-text);
}

.show-exchanges-check .form-checkbox {
  width: 1.125rem;
  height: 1.125rem;
  border: 2px solid var(--color-border);
  border-radius: var(--radius-sm);
  cursor: pointer;
  accent-color: var(--color-secondary);
}

/* Balance History Table */
.balance-history-table {
  overflow-x: auto;
}

.balance-history-table table {
  width: 100%;
  border-collapse: collapse;
}

.balance-history-table th,
.balance-history-table td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid var(--color-border);
}

.balance-history-table th {
  background: var(--color-bg-page);
  font-weight: 700;
  color: var(--color-text-secondary);
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border-bottom: 2px solid var(--border-strong);
}

.balance-history-table tbody tr:hover {
  background: var(--color-bg-page);
}

.date-cell .date-time {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.date-cell .date {
  font-weight: 600;
  color: var(--color-text);
}

.date-cell .time {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

.transaction-number {
  font-family: 'SF Mono', Monaco, Consolas, monospace;
  font-size: 0.875rem;
  color: var(--color-text-secondary);
  background: var(--color-bg-page);
  padding: 0.25rem 0.5rem;
  border-radius: var(--radius-sm);
}

.type-badge,
.status-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.375rem 0.875rem;
  border-radius: var(--radius-xl);
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.type-badge.exchange {
  background: #e0e7ff;
  color: #4338ca;
}

.type-badge.deposit {
  background: var(--color-success-bg);
  color: #065f46;
}

.type-badge.withdrawal {
  background: var(--color-danger-bg);
  color: #991b1b;
}

.type-badge.transfer {
  background: #f3e8ff;
  color: #6b21a8;
}

.type-badge.adjustment {
  background: #fef3c7;
  color: #92400e;
}

.status-badge.completed {
  background: var(--color-success-bg);
  color: #065f46;
}

.status-badge.pending {
  background: #fef3c7;
  color: #92400e;
}

.status-badge.cancelled {
  background: #f3f4f6;
  color: #6b7280;
}

.status-badge.failed {
  background: var(--color-danger-bg);
  color: #991b1b;
}

.exchange-details {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.exchange-item {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.375rem 0.75rem;
  border-radius: var(--radius-md);
  font-size: 0.875rem;
  font-weight: 500;
}

.exchange-item.received {
  background: var(--color-success-bg);
  color: #065f46;
}

.exchange-item.given {
  background: var(--color-danger-bg);
  color: #991b1b;
}

.exchange-item .fi {
  font-size: 1.125rem;
}

.exchange-item .sign {
  font-weight: 700;
}

.exchange-arrow {
  color: var(--color-text-muted);
}

.no-details {
  color: var(--color-text-muted);
}

.balance-change {
  font-weight: 600;
  font-family: 'SF Mono', Monaco, Consolas, monospace;
}

.balance-change.positive {
  color: #16a34a;
}

.balance-change.negative {
  color: var(--color-danger);
}

.balance-history-table .description {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

.empty-transactions {
  text-align: center;
  padding: 3rem;
  color: var(--color-text-muted);
}

.empty-transactions svg {
  margin-bottom: 1rem;
  opacity: 0.5;
}


/* Loading State */
.loading-container {
  min-height: 60vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.loading-content {
  text-align: center;
}

.loading-spinner {
  position: relative;
  width: 60px;
  height: 60px;
  margin: 0 auto 1.5rem;
}

.spinner-ring {
  position: absolute;
  width: 100%;
  height: 100%;
  border: 3px solid transparent;
  border-top-color: var(--color-secondary);
  border-radius: 50%;
  animation: spin 1.2s cubic-bezier(0.5, 0, 0.5, 1) infinite;
}

.spinner-ring:nth-child(2) {
  animation-delay: -0.3s;
  border-top-color: var(--color-success);
}

.spinner-ring:nth-child(3) {
  animation-delay: -0.6s;
  border-top-color: var(--color-warning);
}

.spinner {
  width: 24px;
  height: 24px;
  border: 2px solid var(--color-border);
  border-top-color: var(--color-secondary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(15, 23, 42, 0.5);
  backdrop-filter: var(--glass-blur-strong);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  z-index: 1000;
}

.modal-container {
  background: var(--color-bg-card);
  border-radius: var(--radius-xl);
  width: 100%;
  max-width: 500px;
  max-height: 90vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
  position: relative;
  z-index: 1001;
}

.modal-container.small {
  max-width: 400px;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem 2rem;
  border-bottom: 1px solid var(--color-border);
  flex-shrink: 0;
}

.modal-header h2 {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-text);
}

.modal-close {
  padding: 0.5rem;
  background: none;
  border: none;
  color: var(--color-text-secondary);
  cursor: pointer;
  border-radius: var(--radius-md);
  transition: background-color 0.2s ease, color 0.2s ease;
}

.modal-close:hover {
  background: var(--color-bg-page);
  color: var(--color-text);
}

.modal-form {
  padding: 2rem;
  overflow-y: auto;
  position: relative;
  flex: 1;
}

.form-group {
  margin-bottom: 1.5rem;
  position: relative;
  z-index: auto;
}

.form-label {
  display: block;
  margin-bottom: 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-text-secondary);
}

.required {
  color: var(--color-danger);
}

.form-input,
.form-select,
.form-textarea {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 2px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: 1rem;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
  background: var(--color-bg-card);
}

.form-input:focus,
.form-select:focus,
.form-textarea:focus {
  outline: none;
  border-color: var(--color-secondary);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.form-input.large {
  font-size: 1.25rem;
  padding: 1rem 1.25rem;
  font-weight: 600;
}

.form-textarea {
  resize: vertical;
  min-height: 80px;
}

.form-check {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
}

.form-checkbox {
  width: 1.25rem;
  height: 1.25rem;
  border: 2px solid var(--color-border);
  border-radius: var(--radius-sm);
  cursor: pointer;
}

.balance-update-info {
  background: var(--color-bg-page);
  border-radius: var(--radius-lg);
  padding: 1rem;
  margin-bottom: 1.5rem;
}

.info-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.5rem 0;
}

.info-item:not(:last-child) {
  border-bottom: 1px solid var(--color-border);
}

.info-label {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

.info-value {
  font-weight: 600;
  color: var(--color-text);
}

.info-value.currency {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.info-value.amount {
  font-size: 1.125rem;
  color: #16a34a;
}

.form-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 2rem;
}

.btn-cancel,
.btn-save {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: var(--radius-md);
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s ease, transform 0.2s ease, opacity 0.2s ease;
}

.btn-cancel {
  background: var(--color-bg-page);
  color: var(--color-text-secondary);
}

.btn-cancel:hover {
  background: var(--color-border);
}

.btn-save {
  background: var(--color-secondary);
  color: white;
}

.btn-save:hover:not(:disabled) {
  background: var(--color-secondary-hover);
  transform: translateY(-1px);
}

.btn-save:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* Modal Transitions - Simplified for better performance */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.15s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

/* Delete Confirmation Styles */
.delete-header {
  background: var(--color-danger-bg);
  border-bottom: 1px solid #fecaca;
}

.delete-header h2 {
  color: var(--color-danger);
}

.delete-warning {
  text-align: center;
  padding: 2rem 0;
}

.delete-warning svg {
  color: var(--color-warning);
  margin-bottom: 1rem;
}

.delete-warning h3 {
  margin: 0 0 0.5rem;
  font-size: 1.25rem;
  color: var(--color-danger);
}

.delete-warning p {
  margin: 0.5rem 0;
  color: var(--color-text-secondary);
}

.confirm-instruction {
  margin-top: 1.5rem !important;
  font-size: 1rem;
}

.confirm-instruction strong {
  color: var(--color-text);
  font-family: 'SF Mono', Monaco, Consolas, monospace;
  background: var(--color-bg-page);
  padding: 0.25rem 0.5rem;
  border-radius: var(--radius-sm);
}

.delete-confirm-input {
  text-align: center;
  font-weight: 600;
}

.btn-delete {
  background: var(--color-danger);
  color: white;
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: var(--radius-md);
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s ease, transform 0.2s ease;
}

.btn-delete:hover:not(:disabled) {
  background: #b91c1c;
  transform: translateY(-1px);
}

.btn-delete:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Custom Currency Select */
.custom-currency-select {
  position: relative;
  width: 100%;
}

.currency-select-trigger {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 2px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: 1rem;
  background: var(--color-bg-card);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: space-between;
  transition: border-color 0.2s ease;
}

.currency-select-trigger:hover:not(.disabled) {
  border-color: var(--color-border);
}

.currency-select-trigger:focus {
  outline: none;
  border-color: var(--color-secondary);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.currency-select-trigger.disabled {
  background: #f9fafb;
  cursor: not-allowed;
  opacity: 0.6;
}

.selected-currency {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.selected-currency i {
  font-size: 1.25rem;
  width: 24px;
  height: 18px;
  display: inline-block;
  border-radius: 2px;
  overflow: hidden;
}

.placeholder {
  color: #9ca3af;
}

.currency-dropdown-list {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-bold);
  max-height: 240px;
  overflow-y: auto;
  z-index: 9999;
}

.currency-dropdown-item {
  padding: 0.75rem 1rem;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  cursor: pointer;
  transition: background 0.15s ease;
  border-bottom: 1px solid #f3f4f6;
}

.currency-dropdown-item:last-child {
  border-bottom: none;
}

.currency-dropdown-item:hover {
  background: #f9fafb;
}

.currency-dropdown-item i {
  font-size: 1.25rem;
  width: 24px;
  height: 18px;
  display: inline-block;
  border-radius: 2px;
  overflow: hidden;
  flex-shrink: 0;
}

.currency-dropdown-item .currency-code {
  font-weight: 600;
  color: var(--color-text);
  min-width: 3rem;
}

.currency-dropdown-item .currency-name {
  color: var(--color-text-secondary);
  font-size: 0.875rem;
  flex: 1;
}

/* Scrollbar styling for dropdown */
.currency-dropdown-list::-webkit-scrollbar {
  width: 6px;
}

.currency-dropdown-list::-webkit-scrollbar-track {
  background: #f3f4f6;
  border-radius: 3px;
}

.currency-dropdown-list::-webkit-scrollbar-thumb {
  background: #d1d5db;
  border-radius: 3px;
}

.currency-dropdown-list::-webkit-scrollbar-thumb:hover {
  background: #9ca3af;
}

/* Currency badge for currencies without flags */
.currency-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 18px;
  background: var(--color-border);
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--color-text-secondary);
  text-transform: uppercase;
}

.currency-badge.usdt {
  background: #26A17B;
  color: white;
  font-size: 1rem;
}

/* Ensure all action buttons work */
.action-button {
  position: relative;
  z-index: 1;
}

/* Responsive */
@media (max-width: 768px) {
  .vault-header {
    padding: 1.5rem 0 2rem;
  }

  .header-content {
    flex-direction: column;
    gap: 1rem;
  }

  .vault-identity {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }

  .vault-name {
    font-size: 2rem;
  }

  .actions-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .currencies-grid {
    grid-template-columns: 1fr;
  }

  .time-filter {
    overflow-x: auto;
  }

  .balances-table,
  .balance-history-table {
    font-size: 0.875rem;
  }

  .modal-container {
    margin: 1rem;
  }
}

/* ═══ Section Tabs ═══ */
.section-tabs {
  display: flex;
  gap: 4px;
  padding: 4px;
  background: #f3f4f6;
  border-radius: var(--radius-lg);
  margin-bottom: 20px;
}
.section-tab {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 10px 16px;
  border: none;
  background: transparent;
  border-radius: var(--radius-md);
  font-size: 14px;
  font-weight: 500;
  color: #6b7280;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s;
}
.section-tab:hover {
  color: #374151;
  background: rgba(255,255,255,0.5);
}
.section-tab.active {
  background: var(--color-bg-card);
  color: var(--color-primary);
  box-shadow: var(--shadow-md);
}

/* ═══ Count Section ═══ */
.count-section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}
.count-section-header .section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
  margin: 0;
}
.count-start-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 20px;
  background: linear-gradient(135deg, var(--color-primary), #818cf8);
  color: white;
  border: none;
  border-radius: var(--radius-md);
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s, transform 0.2s, box-shadow 0.2s;
}
.count-start-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(99,102,241,0.3);
}
.count-form-card {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  margin-bottom: 24px;
  box-shadow: var(--shadow-md);
}
.count-form-header {
  padding: 20px 24px 12px;
  border-bottom: 1px solid #f3f4f6;
}
.count-form-header h3 {
  font-size: 16px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 4px;
}
.count-form-hint {
  font-size: 13px;
  color: #9ca3af;
  margin: 0;
}
.count-form-body {
  padding: 16px 24px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.count-row {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 10px 14px;
  background: #f9fafb;
  border-radius: var(--radius-md);
}
.count-currency-info {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 80px;
}
.count-currency-code {
  font-weight: 600;
  font-size: 14px;
  color: #1f2937;
}
.count-field {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 100px;
}
.count-field-label {
  font-size: 11px;
  color: #9ca3af;
  font-weight: 500;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.count-field-value {
  font-family: 'JetBrains Mono', monospace;
  font-size: 14px;
  color: #374151;
  font-weight: 500;
}
.count-input {
  width: 120px;
  padding: 6px 10px;
  border: 1px solid #d1d5db;
  border-radius: var(--radius-md);
  font-size: 14px;
  font-family: 'JetBrains Mono', monospace;
  outline: none;
  transition: border-color 0.2s;
}
.count-input:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(99,102,241,0.1);
}
.count-diff-val {
  font-family: 'JetBrains Mono', monospace;
  font-size: 14px;
  font-weight: 600;
}
.count-diff--ok { color: var(--color-success); }
.count-diff--warn { color: var(--color-danger); }
.count-form-footer {
  padding: 16px 24px;
  border-top: 1px solid #f3f4f6;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}
.count-snapshot-check {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: #4b5563;
  cursor: pointer;
}
.count-snapshot-check input[type="checkbox"] {
  accent-color: var(--color-primary);
}
.count-snapshot-input {
  flex: 1;
  min-width: 180px;
  padding: 6px 12px;
  border: 1px solid #d1d5db;
  border-radius: var(--radius-md);
  font-size: 13px;
  outline: none;
}
.count-snapshot-input:focus {
  border-color: var(--color-primary);
}
.count-form-actions {
  display: flex;
  gap: 8px;
  margin-left: auto;
}
.count-cancel-btn {
  padding: 8px 18px;
  background: #f3f4f6;
  border: none;
  border-radius: var(--radius-md);
  font-size: 14px;
  color: #6b7280;
  cursor: pointer;
  transition: background 0.2s;
}
.count-cancel-btn:hover { background: #e5e7eb; }
.count-save-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 20px;
  background: linear-gradient(135deg, #10b981, #34d399);
  color: white;
  border: none;
  border-radius: var(--radius-md);
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s, transform 0.2s, box-shadow 0.2s;
}
.count-save-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(16,185,129,0.3);
}
.count-save-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* Count History */
.count-history-card {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: 20px 24px;
  box-shadow: var(--shadow-md);
}
.count-history-title {
  font-size: 16px;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 16px;
  padding-left: 0.6rem;
  border-left: 5px solid var(--color-primary);
}
.count-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 40px;
  color: #9ca3af;
  font-size: 14px;
}
.count-history-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  font-size: 14px;
}
.count-history-table thead th {
  text-align: left;
  padding: 10px 12px;
  color: #6b7280;
  font-weight: 700;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  border-bottom: 2px solid var(--border-strong);
}
.count-history-table tbody td {
  padding: 10px 12px;
  border-bottom: 1px solid #f3f4f6;
  color: #374151;
}
.count-date-cell {
  vertical-align: top;
}
.count-date {
  font-weight: 500;
  font-size: 13px;
}
.count-time {
  font-size: 12px;
  color: #9ca3af;
}
.count-curr-badge {
  display: inline-block;
  padding: 2px 8px;
  background: #ede9fe;
  color: var(--color-primary);
  border-radius: var(--radius-sm);
  font-size: 12px;
  font-weight: 600;
}
.count-mono {
  font-family: 'JetBrains Mono', monospace;
  font-size: 13px;
}
.count-diff-badge {
  font-family: 'JetBrains Mono', monospace;
  font-size: 13px;
  font-weight: 600;
}
.count-user-cell {
  vertical-align: top;
  color: #6b7280;
  font-size: 13px;
}
.count-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 40px;
  color: #9ca3af;
  font-size: 14px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
.animate-spin {
  animation: spin 1s linear infinite;
}
@media (max-width: 480px) {
  .count-row {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>