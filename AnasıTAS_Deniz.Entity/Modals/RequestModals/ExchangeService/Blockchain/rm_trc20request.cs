using System;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Blockchain
{
    public class rm_trc20transactionrequest
    {
        public string WalletAddress { get; set; }
        public string ContractAddress { get; set; } = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t"; // USDT TRC20 Contract
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string TransactionType { get; set; } // "all", "sent", "received"
    }

    public class rm_trc20balancerequest
    {
        public string WalletAddress { get; set; }
        public string ContractAddress { get; set; } = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t"; // USDT TRC20 Contract
    }
}