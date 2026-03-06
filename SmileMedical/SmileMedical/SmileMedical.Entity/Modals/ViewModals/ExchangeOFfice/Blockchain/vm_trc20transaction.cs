using System;

namespace SmileMedical.Entity.Modals.ViewModals.ExchangeOffice.Blockchain
{
    public class vm_trc20transaction
    {
        public string TransactionId { get; set; }
        public string BlockNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Type { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenName { get; set; }
        public decimal Amount { get; set; }
        public string AmountDisplay { get; set; }
        public decimal Fee { get; set; }
        public string Result { get; set; }
        public string ContractAddress { get; set; }
        public int Confirmations { get; set; }
        public string Status { get; set; }
    }

    public class vm_trc20transactionlist
    {
        public vm_trc20transactionlist()
        {
            Transactions = new List<vm_trc20transaction>();
        }

        public List<vm_trc20transaction> Transactions { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class vm_trc20balance
    {
        public string Address { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenName { get; set; }
        public decimal Balance { get; set; }
        public string BalanceDisplay { get; set; }
        public int Decimals { get; set; }
        public string ContractAddress { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}