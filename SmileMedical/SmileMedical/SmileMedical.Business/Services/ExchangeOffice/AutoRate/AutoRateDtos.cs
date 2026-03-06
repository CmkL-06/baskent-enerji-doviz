using System;
using System.Collections.Generic;

namespace SmileMedical.Business.Services.ExchangeOffice.AutoRate
{
    /// <summary>
    /// Otomatik güncelleme isteği
    /// </summary>
    public class AutoUpdateRequestDto
    {
        public Guid? OfficeId { get; set; } // null = tüm ofisler
        public bool ForceUpdate { get; set; } = false;
        public string? TriggeredBy { get; set; } // "SYSTEM", "USER", "SCHEDULE", "TEST"
        public bool TestMode { get; set; } = false; // For testing only
        public bool SkipUpdate { get; set; } = false; // For test mode
        public List<Guid>? TargetOfficeIds { get; set; } // Specific offices to update
    }

    /// <summary>
    /// Otomatik güncelleme sonucu
    /// </summary>
    public class AutoUpdateResultDto
    {
        public bool Success { get; set; }
        public int TotalRatesProcessed { get; set; }
        public int RatesUpdated { get; set; }
        public int RatesPendingApproval { get; set; }
        public int RatesSkipped { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? Summary { get; set; }
        public List<ExternalRateDto>? ExternalRates { get; set; } // For test mode - shows fetched rates
    }

    /// <summary>
    /// Rate hesaplama sonucu
    /// </summary>
    public class RateCalculationResultDto
    {
        public string? CurrencyPair { get; set; } // "USD/TRY"
        public decimal CalculatedBuyRate { get; set; }
        public decimal CalculatedSellRate { get; set; }
        public string? StrategyUsed { get; set; } // "BEST_BUY", "AVERAGE", "COMPETITIVE"
        public decimal ProfitMarginPercent { get; set; }
        public List<ExternalRateDto> SourceRates { get; set; } = new();
        public string? Calculation { get; set; } // Hesaplama detayı (UI'da gösterilecek)
        public bool RequiresApproval { get; set; }
        public string? ApprovalReason { get; set; }
    }

    /// <summary>
    /// Ayarları kaydetme isteği
    /// </summary>
    public class SaveSettingsRequestDto
    {
        public bool IsAutoUpdateEnabled { get; set; }
        public int UpdateIntervalMinutes { get; set; }
        public int StartHour { get; set; }
        public int EndHour { get; set; }
        public List<string>? WorkDays { get; set; }

        public decimal TryBasedMarginPercent { get; set; }
        public decimal CrossFiatMarginPercent { get; set; }
        public decimal CryptoMarginPercent { get; set; }

        public bool UseTcmb { get; set; }
        public bool UseDovizCom { get; set; }
        public bool UseBinance { get; set; }

        public string? RateSelectionStrategy { get; set; }

        public decimal MaxPriceChangePercent { get; set; }
        public bool RequireApprovalAboveThreshold { get; set; }

        public List<string>? NotificationEmails { get; set; }
        public bool SendMobileNotifications { get; set; }
    }

    /// <summary>
    /// Onay istenen rate değişikliği
    /// </summary>
    public class PendingApprovalDto
    {
        public Guid Id { get; set; }
        public string? OfficeName { get; set; }
        public string? CurrencyPair { get; set; }
        public decimal CurrentBuyRate { get; set; }
        public decimal CurrentSellRate { get; set; }
        public decimal ProposedBuyRate { get; set; }
        public decimal ProposedSellRate { get; set; }
        public decimal ChangePercent { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ExternalRateDto>? SourceData { get; set; }
    }

    /// <summary>
    /// Onay/Red işlemi
    /// </summary>
    public class ApprovalActionDto
    {
        public Guid ApprovalId { get; set; }
        public bool IsApproved { get; set; }
        public string? Notes { get; set; }
        public Guid ApprovedBy { get; set; }
    }

    /// <summary>
    /// Rate geçmişi
    /// </summary>
    public class RateHistoryDto
    {
        public Guid Id { get; set; }
        public string? OfficeName { get; set; }
        public string? CurrencyPair { get; set; }
        public decimal? OldBuyRate { get; set; }
        public decimal? OldSellRate { get; set; }
        public decimal NewBuyRate { get; set; }
        public decimal NewSellRate { get; set; }
        public decimal ChangePercent { get; set; }
        public string? UpdateSource { get; set; }
        public List<string>? DataSources { get; set; }
        public string? UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Dış kaynak durumu
    /// </summary>
    public class ExternalSourceStatusDto
    {
        public string? SourceName { get; set; }
        public string? SourceKey { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsHealthy { get; set; }
        public DateTime? LastSuccessfulFetch { get; set; }
        public string? LastError { get; set; }
        public int Priority { get; set; }
        public int RatesAvailable { get; set; }
    }
}
