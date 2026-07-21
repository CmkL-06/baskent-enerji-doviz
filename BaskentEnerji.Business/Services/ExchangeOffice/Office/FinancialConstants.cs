namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    /// <summary>
    /// Sertleştirme: DayClosureService (0.0001) ve VaultService.SubmitVaultCountAsync (0.01) farklı
    /// sayım farkı eşikleri kullanıyordu — aynı kavram (ihmal edilebilir yuvarlama farkı) iki farklı
    /// hassasiyetle ele alınıyordu. Artık tek, paylaşılan bir sabit kullanılıyor.
    /// </summary>
    public static class FinancialConstants
    {
        public const decimal DiscrepancyThreshold = 0.0001m;

        // Gider ödemesi onay eşiği: Owner/Admin dışındaki roller bu TRY tutarının üzerindeki
        // ödemeleri oluşturduğunda kayıt Pending olarak açılır ve kasa hiç düşülmez — Owner
        // onayladığında düşülür (bkz. ExpensePaymentService.CreatePaymentAsync/ApproveExpensePaymentAsync).
        public const decimal ExpensePaymentApprovalThresholdTRY = 10000m;
    }
}
