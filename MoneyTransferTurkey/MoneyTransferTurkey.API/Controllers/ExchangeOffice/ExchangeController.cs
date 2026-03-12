using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice;
using MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Expense;
using MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Office;
using MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Party;
using MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Blockchain;
using MoneyTransferTurkey.Business.Services.ExchangeOffice.Office;
using MoneyTransferTurkey.Business.Services.Permission;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Currency;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Office;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Party;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Expense;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Office;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Party;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Blockchain;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOffice.Blockchain;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOffice.Party;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MoneyTransferTurkey.Data.Contexts;

namespace MoneyTransferTurkey.API.Controllers.ExchangeOffice
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ExchangeController : ControllerBase
    {
        private readonly IExchangeServiceCommand _command;
        private readonly IExchangeServiceQuery _query;
        private readonly IVaultService _vaultService;
        private readonly IExchangeTransactionService _transactionService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IExchangeValidationService _validationService;
        private readonly IExchangeReportingService _reportingService;
        private readonly IZReportService _zReportService;
        private readonly IOfficeServiceCommand _officeServiceCommand;
        private readonly IPartyService _partyService;
        private readonly IPartyAccountService _partyAccountService;
        private readonly IPartyCreditService _partyCreditService;
        private readonly IPartyReportingService _partyReportingService;
        private readonly IGhostPartyService _ghostPartyService;
        private readonly IUserOfficeService _userOfficeService;
        private readonly IExpenseDefinitionService _expenseDefinitionService;
        private readonly IExpensePaymentService _expensePaymentService;
        private readonly ITRC20Service _trc20Service;
        private readonly ILogger<ExchangeController> _logger;
        private readonly MoneyTransferTurkeyDbContext _context;
        public ExchangeController(IExchangeServiceCommand command, IExchangeServiceQuery query,
             IVaultService vaultService,
            IExchangeTransactionService transactionService,
            IExchangeRateService exchangeRateService,
            IExchangeValidationService validationService,
            IExchangeReportingService reportingService,
            IZReportService zReportService,
            IOfficeServiceCommand officeServiceCommand,
            IPartyService partyService,
            IPartyAccountService partyAccountService,
            IPartyCreditService partyCreditService,
            IPartyReportingService partyReportingService,
            IGhostPartyService ghostPartyService,
            IUserOfficeService userOfficeService,
            IExpenseDefinitionService expenseDefinitionService,
            IExpensePaymentService expensePaymentService,
            ITRC20Service trc20Service,
            ILogger<ExchangeController> logger,
            MoneyTransferTurkeyDbContext context)
        {
            _command = command;
            _query = query;
            _vaultService = vaultService;
            _transactionService = transactionService;
            _exchangeRateService = exchangeRateService;
            _validationService = validationService;
            _reportingService = reportingService;
            _zReportService = zReportService;
            _officeServiceCommand = officeServiceCommand;
            _partyService = partyService;
            _partyAccountService = partyAccountService;
            _partyCreditService = partyCreditService;
            _partyReportingService = partyReportingService;
            _ghostPartyService = ghostPartyService;
            _userOfficeService = userOfficeService;
            _expenseDefinitionService = expenseDefinitionService;
            _expensePaymentService = expensePaymentService;
            _trc20Service = trc20Service;
            _logger = logger;
            _context = context;
        }


        [HttpGet("")]
        public async Task<ActionResult<List<vm_exchangerate>>> GetAllRates()
        {
            var result = await _query.GetAllRates();
            return Ok(result);
        }


        [HttpPost("convert")]
        public async Task<ActionResult<decimal>> ConvertCurrency([FromBody] rm_convertcurrency data)
        {
            try
            {
                var result = await _query.Convert(data);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("currency")]
        public List<vm_currency> GetAllCurrencies()
        {
            return _query.GetAllCurrencies();
        }

        [HttpGet("currency/{id}")]
        public vm_currency GetCurrencyById(Guid id)
        {
            return _query.GetCurrencyById(id);
        }
        [HttpPost("currency/delete/{id}")]
        public async Task RemoveCurrencyById(Guid id)
        {
            await _command.DeleteCurrency(id);
        }

        [HttpPost("currency")]
        public async Task<IActionResult> SaveCurrency([FromBody] rm_savecurrency data)
        {
            await _command.SaveCurrency(data);
            return Ok(new { message = "Currency saved successfully." });
        }


        [HttpPost("rate")]
        public async Task<IActionResult> SaveRate([FromBody] rm_saveexchangerate data)
        {
            await _command.SaveRate(data);
            return Ok(new { message = "Exchange rate saved successfully." });
        }


        [HttpPost("rate/delete/{id}")]
        public async Task<IActionResult> DeleteRate(Guid id)
        {
            await _command.DeleteRate(id);
            return Ok(new { message = "Exchange rate deleted successfully." });
        }

        /// <summary>
        /// Get all offices with their vault summaries and total assets
        /// </summary>
        [HttpGet("offices/summary")]
        public async Task<ActionResult<List<vm_officesummary>>> GetOfficeSummaries()
        {
            try
            {
                var summaries = await _vaultService.GetOfficeSummariesAsync();
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting office summaries");
                return StatusCode(500, new { error = "An error occurred while retrieving office summaries" });
            }
        }

        /// <summary>
        /// Get total assets across all offices or specific office
        /// </summary>
        [HttpGet("assets/total")]
        public async Task<ActionResult<object>> GetTotalAssets([FromQuery] Guid? officeId = null)
        {
            try
            {
                var totalAssets = await _vaultService.GetTotalAssetsInBaseCurrencyAsync(officeId);
                return Ok(new
                {
                    totalAssetsInBaseCurrency = totalAssets,
                    baseCurrency = "TRY",
                    officeId = officeId,
                    calculatedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total assets");
                return StatusCode(500, new { error = "An error occurred while calculating total assets" });
            }
        }

        /// <summary>
        /// Get all vaults with their balances
        /// </summary>
        [HttpGet("vaults")]
        public async Task<ActionResult<List<vm_vaultsummary>>> GetAllVaults()
        {
            try
            {
                var vaults = await _vaultService.GetAllVaultSummariesAsync();
                return Ok(vaults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vault summaries");
                return StatusCode(500, new { error = "An error occurred while retrieving vault summaries" });
            }
        }

        [HttpPost("vault")]
        public async Task SaveVault([FromBody] rm_savevault data)
        {
            await _vaultService.SaveVault(data);
        }

        [HttpPost("vault/updatebalance")]
        public async Task UpdateVaultBalanceAsync(rm_updatevaultbalance data)
        {
            await _vaultService.UpdateVaultBalanceAsync(data);
        }
        [HttpPost("vault/delete/{id}")]
        public async Task RemoveId(Guid id)
        {
            await _vaultService.RemoveVault(id);
        }

        [HttpPost("office")]
        public async Task SaveOffice(rm_saveoffice data)
        {
            await _officeServiceCommand.SaveOffice(data);
        }
        [HttpPost("office/delete/{id}")]
        public async Task RemoveOffice(Guid id)
        {
            await _officeServiceCommand.RemoveOffice(id);
        }

        /// <summary>
        /// Get vaults by office ID
        /// </summary>
        [HttpGet("office/{officeId}/vaults")]
        public async Task<ActionResult<List<vm_vaultsummary>>> GetVaultsByOfficeId(Guid officeId )
        {
            try
            {
                var allVaults = await _vaultService.GetAllVaultSummariesAsync();
                var officeVaults = allVaults.Where(v => v.OfficeId == officeId).ToList();
                
                if (!officeVaults.Any())
                    return Ok(new List<vm_vaultsummary>()); // Return empty list if no vaults found
                
                // Calculate total value in base currency for each vault
                foreach (var vault in officeVaults)
                {
                    vault.TotalValueInBaseCurrency = vault.Balances
                        .Sum(b => b.Balance * (b.CurrencyCode == "TRY" ? 1 : b.ExchangeRateToBase));
                }
                
                return Ok(officeVaults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vaults for office {OfficeId}", officeId);
                return StatusCode(500, new { error = "An error occurred while retrieving office vaults" });
            }
        }

        /// <summary>
        /// Get specific vault details
        /// </summary>
        [HttpGet("vaults/{vaultId}")]
        public async Task<ActionResult<vm_vaultsummary>> GetVaultById(Guid vaultId)
        {
            try
            {
                var vault = await _vaultService.GetVaultSummaryAsync(vaultId);
                if (vault == null)
                    return NotFound(new { error = $"Vault with ID {vaultId} not found" });

                return Ok(vault);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vault {VaultId}", vaultId);
                return StatusCode(500, new { error = "An error occurred while retrieving vault details" });
            }
        }

        /// <summary>
        /// Check vault balance for a specific currency
        /// </summary>
        [HttpGet("vaults/{vaultId}/balance/{currencyId}")]
        public async Task<ActionResult<object>> CheckVaultBalance(Guid vaultId, Guid currencyId, [FromQuery] decimal requiredAmount)
        {
            try
            {
                var hasBalance = await _vaultService.CheckVaultBalanceAsync(vaultId, currencyId, requiredAmount);
                var vault = await _vaultService.GetVaultSummaryAsync(vaultId);

                var balance = vault?.Balances.FirstOrDefault(b => b.CurrencyId == currencyId);

                return Ok(new
                {
                    hassufficientBalance = hasBalance,
                    requiredAmount = requiredAmount,
                    availableBalance = balance?.AvailableBalance ?? 0,
                    totalBalance = balance?.Balance ?? 0,
                   // reservedAmount = balance?.ReservedAmount ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking vault balance");
                return StatusCode(500, new { error = "An error occurred while checking vault balance" });
            }
        }



        #region Exchange Rates Management

        /// <summary>
        /// Get all active exchange rates for an office
        /// </summary>
        [HttpGet("rates")]
        public async Task<ActionResult<List<vm_exchangerate>>> GetAllExchangeRates([FromQuery] Guid officeId)
        {
            try
            {
                var rates = await _exchangeRateService.GetAllActiveRatesAsync(officeId);
                return Ok(rates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange rates");
                return StatusCode(500, new { error = "An error occurred while retrieving exchange rates" });
            }
        }

        /// <summary>
        /// Get exchange rate for specific currency pair in an office
        /// </summary>
        [HttpGet("rates/{officeId}/{sourceCurrencyId}/{targetCurrencyId}")]
        public async Task<ActionResult<vm_exchangerate>> GetExchangeRate(Guid officeId, Guid sourceCurrencyId, Guid targetCurrencyId)
        {
            try
            {
                var rate = await _exchangeRateService.GetCurrentRateAsync(officeId, sourceCurrencyId, targetCurrencyId);
                if (rate == null)
                    return NotFound(new { error = "Exchange rate not found for this currency pair in the specified office" });

                return Ok(rate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange rate");
                return StatusCode(500, new { error = "An error occurred while retrieving exchange rate" });
            }
        }

        /// <summary>
        /// Create new exchange rate (creates history)
        /// </summary>
        [HttpPost("rates")]
        public async Task<ActionResult<vm_exchangerate>> CreateOrUpdateExchangeRate([FromBody] CreateExchangeRateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var rate = await _exchangeRateService.CreateOrUpdateRateAsync(
                    request.OfficeId,
                    request.SourceCurrencyId,
                    request.TargetCurrencyId,
                    request.BuyRate,
                    request.SellRate
                );

                return Ok(rate);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating exchange rate");
                return StatusCode(500, new { error = "An error occurred while creating/updating exchange rate" });
            }
        }

        /// <summary>
        /// Update current exchange rate without creating history
        /// </summary>
        [HttpPut("rates")]
        public async Task<ActionResult<vm_exchangerate>> UpdateCurrentExchangeRate([FromBody] CreateExchangeRateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

               // var rate = await _exchangeRateService.UpdateCurrentRateAsync(
                var rate = await _exchangeRateService.CreateOrUpdateRateAsync(
                    request.OfficeId,
                    request.SourceCurrencyId,
                    request.TargetCurrencyId,
                    request.BuyRate,
                    request.SellRate
                );

                return Ok(rate);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exchange rate");
                return StatusCode(500, new { error = "An error occurred while updating exchange rate" });
            }
        }

        /// <summary>
        /// Get exchange rate history for a currency pair in an office
        /// </summary>
        [HttpGet("rates/{officeId}/{sourceCurrencyId}/{targetCurrencyId}/history")]
        public async Task<ActionResult<List<vm_exchangerate>>> GetExchangeRateHistory(
            Guid officeId,
            Guid sourceCurrencyId, 
            Guid targetCurrencyId,
            [FromQuery] int limit = 10)
        {
            try
            {
                var history = await _exchangeRateService.GetRateHistoryAsync(
                    officeId,
                    sourceCurrencyId, 
                    targetCurrencyId, 
                    limit);
                    
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange rate history");
                return StatusCode(500, new { error = "An error occurred while retrieving exchange rate history" });
            }
        }

        #endregion

        #region Transaction Processing

        /// <summary>
        /// Process currency exchange transaction
        /// </summary>
        [HttpPost("exchange")]
        public async Task<ActionResult<vm_exchangetransaction>> ProcessExchange(
            [FromBody] List<rm_exchangetransaction> request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate the transaction
                foreach (var item in request)
                {
                    var validationResult = await _validationService.ValidateExchangeTransaction(item);
                    if (!validationResult.IsValid)
                    {
                        return BadRequest(new { errors = validationResult.Errors });
                    }
                }

                var result = await _transactionService.ProcessExchangeAsync(request);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing exchange");
                return StatusCode(500, new { error = "An error occurred while processing the exchange" });
            }
        }

        [HttpPost("exchange/remove")]
        public async Task removeTransaction([FromBody]rm_removetransaction request)
        {
            await _transactionService.RemoveTransaction(request);
        }

        /// <summary>
        /// Transfer funds between vaults
        /// </summary>
        [HttpPost("transfer")]
        public async Task<ActionResult<object>> TransferBetweenVaults(
            [FromBody] rm_transferbetweenvaults request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate the transfer
                var validationResult = await _validationService.ValidateTransfer(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { errors = validationResult.Errors });
                }

                var result = await _transactionService.TransferBetweenVaultsAsync(request);

                return Ok(new
                {
                    transactionId = result.Id,
                    transactionNumber = result.TransactionNumber,
                    status = result.Status.ToString(),
                    amount = request.Amount,
                    currency = request.CurrencyId,
                    processedAt = result.TransactionDate
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transfer");
                return StatusCode(500, new { error = "An error occurred while processing the transfer" });
            }
        }

        #endregion

        #region Transaction History & Reporting

        [HttpGet("transactions/{id}")]

        public async Task<vm_transaction> GetTransaction(Guid id)
        {
            return await _transactionService.GetTransaction(id);

        }


        /// <summary>
        /// Get transaction history with pagination
        /// </summary>
        [HttpGet("transactions")]
        public async Task<ActionResult<object>> GetTransactionHistory(
            [FromQuery] Guid? vaultId = null,
            [FromQuery] Guid? officeId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] TransactionType? type = null,
            [FromQuery] TransactionStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var transactions = await _transactionService.GetTransactionHistoryAsync(
                    vaultId, startDate, endDate);

                // Apply additional filters
                if (officeId.HasValue)
                {
                    transactions = transactions.Where(t => t.OfficeId == officeId.Value).ToList();
                }

                if (type.HasValue)
                {
                    transactions = transactions.Where(t => t.Type == type.Value).ToList();
                }

                if (status.HasValue)
                {
                    transactions = transactions.Where(t => t.Status == status.Value).ToList();
                }

                // Apply pagination
                var totalCount = transactions.Count;
                var pagedTransactions = transactions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.Id,
                        t.TransactionNumber,
                        t.Type,
                        t.Status,
                        t.TransactionDate,
                        VaultName = t.VaultName,
                        t.IsCustomRate,
                        t.Profit,
                        Details = t.Details.Select(d => new
                        {
                            d.CurrencyId,
                            CurrencyCode = d.CurrencyCode,
                            d.Side,
                            d.Amount,
                            d.Rate,
                            d.NetAmount
                        }),
                        t.Notes
                    })
                    .ToList();

                return Ok(new
                {
                    data = pagedTransactions,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction history");
                return StatusCode(500, new { error = "An error occurred while retrieving transaction history" });
            }
        }

        /// <summary>
        /// Get P&L report for specific period
        /// </summary>
        [HttpGet("reports/pnl")]
        public async Task<ActionResult<object>> GetProfitLossReport(
            [FromQuery] Guid officeId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (endDate < startDate)
                    return BadRequest(new { error = "End date must be after start date" });

                var pnl = await _transactionService.CalculateProfitLossAsync(officeId, startDate, endDate);

                // Get office details
                var offices = await _vaultService.GetOfficeSummariesAsync();
                var office = offices.FirstOrDefault(o => o.OfficeId == officeId);

                return Ok(new
                {
                    officeId,
                    officeName = office?.OfficeName,
                    period = new { startDate, endDate },
                    profitLoss = pnl,
                    currency = "TRY",
                    generatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating P&L");
                return StatusCode(500, new { error = "An error occurred while calculating profit/loss" });
            }
        }

        /// <summary>
        /// Get monthly report for an office
        /// </summary>
        [HttpGet("reports/monthly")]
        public async Task<ActionResult<vm_monthlyreport>> GetMonthlyReport(
            [FromQuery] Guid officeId,
            [FromQuery] int year,
            [FromQuery] int month)
        {
            try
            {
                if (month < 1 || month > 12)
                    return BadRequest(new { error = "Invalid month" });

                var report = await _reportingService.GetMonthlyReport(officeId, year, month);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly report");
                return StatusCode(500, new { error = "An error occurred while generating monthly report" });
            }
        }

        /// <summary>
        /// Get currency performance report
        /// </summary>
        [HttpGet("reports/currency-performance")]
        public async Task<ActionResult<List<vm_currencyperformance>>> GetCurrencyPerformance(
            [FromQuery] Guid officeId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var performance = await _reportingService.GetCurrencyPerformance(officeId, startDate, endDate);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting currency performance");
                return StatusCode(500, new { error = "An error occurred while retrieving currency performance" });
            }
        }

        /// <summary>
        /// Get vault utilization report
        /// </summary>
        [HttpGet("reports/vault-utilization")]
        public async Task<ActionResult<List<vm_vaultutilization>>> GetVaultUtilization([FromQuery] Guid? officeId = null)
        {
            try
            {
                var utilization = await _reportingService.GetVaultUtilization(officeId);
                return Ok(utilization);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vault utilization");
                return StatusCode(500, new { error = "An error occurred while retrieving vault utilization" });
            }
        }

        #endregion

        #region Dashboard Endpoints

        /// <summary>
        /// Get dashboard summary data
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetDashboardData([FromQuery] Guid? officeId = null)
        {
            try
            {
                // Get all offices summary
                var offices = await _vaultService.GetOfficeSummariesAsync();
                
                // Filter by officeId if provided
                if (officeId.HasValue)
                {
                    offices = offices.Where(o => o.OfficeId == officeId.Value).ToList();
                }
                
                // Get all vaults with detailed balances
                var allVaults = await _vaultService.GetAllVaultSummariesAsync();
                
                // Filter vaults by officeId if provided
                if (officeId.HasValue)
                {
                    allVaults = allVaults.Where(v => v.OfficeId == officeId.Value).ToList();
                }

                // Get total company assets (filtered by office if specified)
                var totalAssets = await _vaultService.GetTotalAssetsInBaseCurrencyAsync(officeId);

                // Get today's transactions
                var todayTransactions = await _transactionService.GetTransactionHistoryAsync(
                    null, DateTime.Today, DateTime.Today.AddDays(1).AddSeconds(-1));
                
                // Filter transactions by office if specified
                if (officeId.HasValue && allVaults.Any())
                {
                    var officeVaultIds = allVaults.Select(v => v.VaultId).ToHashSet();
                    todayTransactions = todayTransactions.Where(t => officeVaultIds.Contains(t.VaultId)).ToList();
                }

                // Calculate today's profit
                var todayProfit = offices.Sum(o => o.DailyProfitLoss);

                // Weekly profit: daily * 7 approximation (avoids extra 7-day query cost)
                var weeklyProfit = todayProfit * 7;
                
                // Get party account balances
                var partyAccountsQuery = _context.PartyAccounts
                    .AsNoTracking()
                    .Include(pa => pa.Party)
                    .Include(pa => pa.Currency)
                    .Where(pa => pa.Balance != 0);

                if (officeId.HasValue)
                {
                    partyAccountsQuery = partyAccountsQuery.Where(pa => pa.Party.OfficeId == officeId.Value);
                }

                var partyAccounts = await partyAccountsQuery.ToListAsync();
                
                // Calculate party receivables and payables
                var totalPartyReceivables = partyAccounts.Where(pa => pa.Balance > 0).Sum(pa => pa.Balance);
                var totalPartyPayables = partyAccounts.Where(pa => pa.Balance < 0).Sum(pa => Math.Abs(pa.Balance));
                
                // Group party balances by currency
                var partyBalancesByCurrency = partyAccounts
                    .GroupBy(pa => new { pa.CurrencyId, pa.Currency.CurrencyCode })
                    .Select(g => new
                    {
                        currencyCode = g.Key.CurrencyCode,
                        totalReceivables = g.Where(pa => pa.Balance > 0).Sum(pa => pa.Balance),
                        totalPayables = g.Where(pa => pa.Balance < 0).Sum(pa => Math.Abs(pa.Balance)),
                        netBalance = g.Sum(pa => pa.Balance),
                        partyCount = g.Select(pa => pa.PartyId).Distinct().Count()
                    })
                    .Where(pb => pb.netBalance != 0)
                    .OrderBy(pb => pb.currencyCode)
                    .ToList();
                
                // Get active exchange rates (filtered by office if specified)
                List<vm_exchangerate> rates;
                if (officeId.HasValue)
                {
                    rates = await _exchangeRateService.GetAllActiveRatesAsync(officeId.Value);
                }
                else
                {
                    // Get rates for first office only to avoid performance issues
                    var firstOffice = offices.FirstOrDefault();
                    if (firstOffice != null)
                    {
                        rates = await _exchangeRateService.GetAllActiveRatesAsync(firstOffice.OfficeId);
                    }
                    else
                    {
                        rates = new List<vm_exchangerate>();
                    }
                }
                
                // Group currencies across all vaults (excluding base currency TRY)
                var currencySummary = allVaults
                    .SelectMany(v => v.Balances)
                    .Where(b => b.CurrencyCode != "TRY")
                    .GroupBy(b => b.CurrencyCode)
                    .Select(g => new
                    {
                        currencyCode = g.Key,
                        totalAmount = g.Sum(b => b.Balance),
                        totalValueInBaseCurrency = g.Sum(b => b.Balance * b.ExchangeRateToBase),
                        vaultCount = g.Count(),
                        currentRate = rates.FirstOrDefault(r => r.SourceCurrencyCode == g.Key && r.TargetCurrencyCode == "TRY")
                    })
                    .OrderByDescending(c => c.totalValueInBaseCurrency)
                    .ToList();
                
                // Calculate total foreign currency value
                var totalForeignCurrencyValue = currencySummary.Sum(c => c.totalValueInBaseCurrency);

                // Prepare detailed office data with vaults
                var detailedOffices = offices.Select(office => new
                {
                    office.OfficeId,
                    office.OfficeName,
                    office.VaultCount,
                    office.TotalValueInBaseCurrency,
                    office.DailyProfitLoss,
                    office.MonthlyProfitLoss,
                    vaults = allVaults
                        .Where(v => v.OfficeId == office.OfficeId)
                        .Select(vault => new
                        {
                            vault.VaultId,
                            vault.VaultName,
                            vault.IsActive,
                            totalValueInBaseCurrency = vault.Balances.Sum(b => b.Balance * (b.CurrencyCode == "TRY" ? 1 : b.ExchangeRateToBase)),
                            currencies = vault.Balances.Select(b => new
                            {
                                b.CurrencyId,
                                b.CurrencyCode,
                                b.CurrencyName,
                                b.Balance,
                                valueInBaseCurrency = b.Balance * (b.CurrencyCode == "TRY" ? 1 : b.ExchangeRateToBase),
                                b.ExchangeRateToBase,
                                isBaseCurrency = b.CurrencyCode == "TRY"
                            }).OrderByDescending(c => c.valueInBaseCurrency)
                        })
                }).ToList();

                return Ok(new
                {
                    summary = new
                    {
                        totalAssets,
                        totalAssetsInTRY = allVaults.SelectMany(v => v.Balances.Where(b => b.CurrencyCode == "TRY")).Sum(b => b.Balance),
                        totalForeignCurrencyValue,
                        numberOfOffices = offices.Count,
                        totalVaults = offices.Sum(o => o.VaultCount),
                        totalActiveCurrencies = currencySummary.Count + 1, // +1 for TRY
                        todayTransactionCount = todayTransactions.Count,
                        todayProfit,
                        monthlyProfit = offices.Sum(o => o.MonthlyProfitLoss),
                        weeklyProfit,
                        // Party account summary
                        totalPartyReceivables,
                        totalPartyPayables,
                        netPartyBalance = totalPartyReceivables - totalPartyPayables,
                        activePartyAccounts = partyAccounts.Count,
                        // Combined totals (vault + party accounts)
                        combinedTotalAssets = totalAssets + totalPartyReceivables,
                        combinedNetPosition = totalAssets + totalPartyReceivables - totalPartyPayables
                    },
                    currencyDistribution = new
                    {
                        baseCurrency = "TRY",
                        totalForeignCurrencyValue,
                        currencies = currencySummary,
                        message = $"You have {currencySummary.Count} foreign currencies worth {totalForeignCurrencyValue:N2} TRY available to sell"
                    },
                    offices = detailedOffices,
                    partyAccounts = new
                    {
                        summary = new
                        {
                            totalReceivables = totalPartyReceivables,
                            totalPayables = totalPartyPayables,
                            netBalance = totalPartyReceivables - totalPartyPayables,
                            activeAccounts = partyAccounts.Count,
                            affectedParties = partyAccounts.Select(pa => pa.PartyId).Distinct().Count()
                        },
                        byCurrency = partyBalancesByCurrency,
                        topReceivables = partyAccounts
                            .Where(pa => pa.Balance > 0)
                            .OrderByDescending(pa => pa.Balance)
                            .Take(5)
                            .Select(pa => new
                            {
                                partyCode = pa.Party.PartyCode,
                                partyName = pa.Party.Name,
                                currencyCode = pa.Currency.CurrencyCode,
                                balance = pa.Balance,
                                accountNumber = pa.AccountNumber
                            }),
                        topPayables = partyAccounts
                            .Where(pa => pa.Balance < 0)
                            .OrderBy(pa => pa.Balance)
                            .Take(5)
                            .Select(pa => new
                            {
                                partyCode = pa.Party.PartyCode,
                                partyName = pa.Party.Name,
                                currencyCode = pa.Currency.CurrencyCode,
                                balance = Math.Abs(pa.Balance),
                                accountNumber = pa.AccountNumber
                            })
                    },
                    exchangeRates = rates.Select(r => new
                    {
                        sourceCurrencyCode = r.SourceCurrencyCode,
                        targetCurrencyCode = r.TargetCurrencyCode,
                        r.BuyRate,
                        r.SellRate,
                        spread = r.SellRate - r.BuyRate,
                        spreadPercentage = ((r.SellRate - r.BuyRate) / r.BuyRate) * 100,
                        lastUpdated = r.EffectiveFrom
                    }).OrderBy(r => r.sourceCurrencyCode),
                    recentTransactions = todayTransactions.Take(10).Select(t => new
                    {
                        t.TransactionNumber,
                        t.Type,
                        t.TransactionDate,
                        t.Profit,
                        vaultName = allVaults.FirstOrDefault(v => v.VaultId == t.VaultId)?.VaultName
                    }),
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return StatusCode(500, new { error = "An error occurred while retrieving dashboard data" });
            }
        }

        #endregion

        #region Party Account Management

        /// <summary>
        /// Create a new party (customer/supplier)
        /// </summary>
        [HttpPost("party")]
        public async Task<ActionResult<vm_party>> CreateParty([FromBody] rm_party request)
        {
            try
            {
                var party = await _partyService.CreatePartyAsync(request);
                return Ok(party);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating party");
                return StatusCode(500, new { error = "An error occurred while creating party" });
            }
        }

        /// <summary>
        /// Update party details
        /// </summary>
        [HttpPut("party/{partyId}")]
        public async Task<ActionResult<vm_party>> UpdateParty(Guid partyId, [FromBody] rm_party request)
        {
            try
            {
                var party = await _partyService.UpdatePartyAsync(partyId, request);
                return Ok(party);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating party");
                return StatusCode(500, new { error = "An error occurred while updating party" });
            }
        }

        /// <summary>
        /// Delete a party
        /// </summary>
        [HttpPost("party/delete/{partyId}")]
        public async Task<ActionResult> DeleteParty(Guid partyId)
        {
            try
            {
                var result = await _partyService.DeletePartyAsync(partyId);
                if (result)
                {
                    return Ok(new { message = "Party deleted successfully" });
                }
                return BadRequest(new { error = "Failed to delete party" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting party");
                return StatusCode(500, new { error = "An error occurred while deleting party" });
            }
        }

        /// <summary>
        /// Get party by ID
        /// </summary>
        [HttpGet("party/{partyId}")]
        public async Task<ActionResult<vm_party>> GetPartyById(Guid partyId)
        {
            try
            {
                var party = await _partyService.GetPartyByIdAsync(partyId);
                if (party == null)
                    return NotFound(new { error = "Party not found" });
                
                return Ok(party);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting party");
                return StatusCode(500, new { error = "An error occurred while retrieving party" });
            }
        }

        /// <summary>
        /// Get all parties for an office
        /// </summary>
        [HttpGet("parties")]
        public async Task<ActionResult<List<vm_party>>> GetParties(
            [FromQuery] Guid officeId,
            [FromQuery] PartyType? type = null,
            [FromQuery] PartyStatus? status = null)
        {
            try
            {
                var parties = await _partyService.GetPartiesAsync(officeId, type, status);
                return Ok(parties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parties");
                return StatusCode(500, new { error = "An error occurred while retrieving parties" });
            }
        }

        /// <summary>
        /// Get party account balances
        /// </summary>
        [HttpGet("party/{partyId}/accounts")]
        public async Task<ActionResult<List<vm_partyaccount>>> GetPartyAccounts(Guid partyId)
        {
            try
            {
                var accounts = await _partyAccountService.GetPartyAccountsAsync(partyId);
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting party accounts");
                return StatusCode(500, new { error = "An error occurred while retrieving party accounts" });
            }
        }


        /// <summary>
        /// Get party account entries (transactions)
        /// </summary>
        [HttpGet("party/account/{accountId}/entries")]
        public async Task<ActionResult<List<vm_partyaccountentry>>> GetAccountEntries(
            Guid accountId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] PaymentStatus? status = null)
        {
            try
            {
                var entries = await _partyAccountService.GetAccountEntriesAsync(accountId, startDate, endDate, status);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account entries");
                return StatusCode(500, new { error = "An error occurred while retrieving account entries" });
            }
        }


        /// <summary>
        /// Record payment for party account
        /// </summary>
        [HttpPost("party/account/payment")]
        public async Task<ActionResult<vm_partyaccountentry>> RecordPayment([FromBody] rm_partypayment request)
        {
            try
            {
                var payment = await _partyAccountService.RecordPaymentAsync(request);
                return Ok(payment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording payment");
                return StatusCode(500, new { error = "An error occurred while recording payment" });
            }
        }

        /// <summary>
        /// Get party credit limits
        /// </summary>
        [HttpGet("party/{partyId}/credit-limits")]
        public async Task<ActionResult<List<vm_partycreditlimit>>> GetPartyCreditLimits(Guid partyId)
        {
            try
            {
                var limits = await _partyCreditService.GetPartyCreditLimitsAsync(partyId);
                return Ok(limits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit limits");
                return StatusCode(500, new { error = "An error occurred while retrieving credit limits" });
            }
        }

        /// <summary>
        /// Set or update party credit limit
        /// </summary>
        [HttpPost("party/credit-limit")]
        public async Task<ActionResult<vm_partycreditlimit>> SetCreditLimit([FromBody] rm_partycreditlimit request)
        {
            try
            {
                var limit = await _partyCreditService.SetCreditLimitAsync(request);
                return Ok(limit);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting credit limit");
                return StatusCode(500, new { error = "An error occurred while setting credit limit" });
            }
        }

        /// <summary>
        /// Check credit availability for party
        /// </summary>
        [HttpGet("party/{partyId}/credit-check")]
        public async Task<ActionResult<vm_creditavailability>> CheckCreditAvailability(
            Guid partyId,
            [FromQuery] Guid currencyId,
            [FromQuery] decimal amount)
        {
            try
            {
                var availability = await _partyCreditService.CheckCreditAvailabilityAsync(partyId, currencyId, amount);
                return Ok(availability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking credit availability");
                return StatusCode(500, new { error = "An error occurred while checking credit availability" });
            }
        }

        /// <summary>
        /// Get party statement
        /// </summary>
        [HttpGet("party/{partyId}/statement")]
        public async Task<ActionResult<vm_partystatement>> GetPartyStatement(
            Guid partyId,
            [FromQuery] Guid? currencyId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                if (currencyId.HasValue)
                {
                    // Single currency statement
                    var statement = await _partyReportingService.GenerateStatementAsync(
                        partyId, 
                        currencyId.Value, 
                        startDate ?? DateTime.Now.AddMonths(-1), 
                        endDate ?? DateTime.Now);
                    return Ok(statement);
                }
                else
                {
                    // All currencies - generate combined statement
                    var party = await _partyService.GetPartyByIdAsync(partyId);
                    if (party == null)
                        return NotFound(new { error = "Party not found" });

                    var accounts = await _partyAccountService.GetPartyAccountsAsync(partyId);
                    var statements = new List<vm_partystatement>();
                    
                    foreach (var account in accounts.Where(a => a.Balance != 0 || a.TotalDebits > 0 || a.TotalCredits > 0))
                    {
                        var stmt = await _partyReportingService.GenerateStatementAsync(
                            partyId, 
                            account.CurrencyId, 
                            startDate ?? DateTime.Now.AddMonths(-1), 
                            endDate ?? DateTime.Now);
                        statements.Add(stmt);
                    }
                    
                    // Return a combined view or the first statement for now
                    if (statements.Any())
                        return Ok(statements);
                    else
                        return Ok(new vm_partystatement 
                        { 
                            PartyId = partyId,
                            PartyName = party.Name,
                            StatementDate = DateTime.Now,
                            PeriodStart = startDate ?? DateTime.Now.AddMonths(-1),
                            PeriodEnd = endDate ?? DateTime.Now,
                            Entries = new List<vm_partyaccountentry>()
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating statement");
                return StatusCode(500, new { error = "An error occurred while generating statement" });
            }
        }

        /// <summary>
        /// Get aged receivables report
        /// </summary>
        [HttpGet("party/reports/aged-receivables")]
        public async Task<ActionResult<List<vm_agedreceivables>>> GetAgedReceivables(
            [FromQuery] Guid officeId,
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var report = await _partyReportingService.GetAgedReceivablesAsync(officeId, asOfDate ?? DateTime.Today);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting aged receivables");
                return StatusCode(500, new { error = "An error occurred while generating aged receivables report" });
            }
        }

        /// <summary>
        /// Get party balance summary
        /// </summary>
        [HttpGet("party/reports/balance-summary")]
        public async Task<ActionResult<List<vm_partybalance>>> GetPartyBalanceSummary([FromQuery] Guid? officeId = null)
        {
            try
            {
                if (officeId.HasValue)
                {
                    var summary = await _partyReportingService.GetPartyBalanceSummaryAsync(officeId.Value);
                    return Ok(summary);
                }
                else
                {
                    // Get all offices and combine results
                    var allSummaries = new List<vm_partybalance>();
                    var offices = await _context.Offices.Where(o => o.IsActive).ToListAsync();
                    
                    foreach (var office in offices)
                    {
                        var officeSummary = await _partyReportingService.GetPartyBalanceSummaryAsync(office.Id);
                        allSummaries.AddRange(officeSummary);
                    }
                    
                    return Ok(allSummaries.OrderByDescending(p => p.NetBalance).ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting balance summary");
                return StatusCode(500, new { error = "An error occurred while generating balance summary" });
            }
        }

        #endregion

        #region Ghost Party Management

        /// <summary>
        /// Create ghost party account
        /// </summary>
        [HttpPost("ghost-party/account/create")]
        public async Task<ActionResult<vm_ghostpartyaccount>> CreateGhostAccount([FromBody] rm_ghostpartyaccount request)
        {
            try
            {
                var account = await _ghostPartyService.CreateGhostAccountAsync(request);
                return Ok(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ghost account");
                return StatusCode(500, new { error = "An error occurred while creating ghost account" });
            }
        }

        /// <summary>
        /// Get ghost accounts by party
        /// </summary>
        [HttpGet("ghost-party/{partyId}/accounts")]
        public async Task<ActionResult<List<vm_ghostpartyaccount>>> GetGhostAccountsByParty(Guid partyId)
        {
            try
            {
                var accounts = await _ghostPartyService.GetGhostAccountsByPartyAsync(partyId);
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ghost accounts");
                return StatusCode(500, new { error = "An error occurred while retrieving ghost accounts" });
            }
        }

        /// <summary>
        /// Process ghost party payment (money IN to vault)
        /// </summary>
        [HttpPost("ghost-party/payment")]
        public async Task<ActionResult<vm_ghostpartyentry>> ProcessGhostPayment([FromBody] rm_ghostpartypayment request)
        {
            try
            {
                var entry = await _ghostPartyService.ProcessGhostPaymentAsync(request);
                return Ok(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ghost payment");
                return StatusCode(500, new { error = "An error occurred while processing ghost payment" });
            }
        }

        /// <summary>
        /// Process ghost party collection (money OUT from vault)
        /// </summary>
        [HttpPost("ghost-party/collection")]
        public async Task<ActionResult<vm_ghostpartyentry>> ProcessGhostCollection([FromBody] rm_ghostpartycollection request)
        {
            try
            {
                var entry = await _ghostPartyService.ProcessGhostCollectionAsync(request);
                return Ok(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ghost collection");
                return StatusCode(500, new { error = "An error occurred while processing ghost collection" });
            }
        }

        /// <summary>
        /// Get ghost party entries
        /// </summary>
        [HttpGet("ghost-party/account/{accountId}/entries")]
        public async Task<ActionResult<List<vm_ghostpartyentry>>> GetGhostEntries(Guid accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var entries = await _ghostPartyService.GetGhostEntriesAsync(accountId, fromDate, toDate);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ghost entries");
                return StatusCode(500, new { error = "An error occurred while retrieving ghost entries" });
            }
        }

        /// <summary>
        /// Reverse ghost party entry
        /// </summary>
        [HttpPost("ghost-party/entry/{entryId}/reverse")]
        public async Task<ActionResult<vm_ghostpartyentry>> ReverseGhostEntry(Guid entryId, [FromBody] string reason)
        {
            try
            {
                var entry = await _ghostPartyService.ReverseGhostEntryAsync(entryId, reason);
                return Ok(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reversing ghost entry");
                return StatusCode(500, new { error = "An error occurred while reversing ghost entry" });
            }
        }

        /// <summary>
        /// Get ghost party balance summary
        /// </summary>
        [HttpGet("ghost-party/{partyId}/balance")]
        public async Task<ActionResult<vm_ghostpartybalance>> GetGhostBalanceSummary(Guid partyId)
        {
            try
            {
                var balance = await _ghostPartyService.GetGhostBalanceSummaryAsync(partyId);
                return Ok(balance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ghost balance summary");
                return StatusCode(500, new { error = "An error occurred while retrieving ghost balance summary" });
            }
        }

        /// <summary>
        /// Get ghost party statement
        /// </summary>
        [HttpGet("ghost-party/{partyId}/statement")]
        public async Task<ActionResult<List<vm_ghostpartystatement>>> GetGhostStatement(Guid partyId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                var statement = await _ghostPartyService.GetGhostStatementAsync(partyId, fromDate, toDate);
                return Ok(statement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ghost statement");
                return StatusCode(500, new { error = "An error occurred while retrieving ghost statement" });
            }
        }

        /// <summary>
        /// Get ghost party summary
        /// </summary>
        [HttpGet("ghost-party/{partyId}/summary")]
        public async Task<ActionResult<vm_ghostpartysummary>> GetGhostPartySummary(Guid partyId)
        {
            try
            {
                var summary = await _ghostPartyService.GetGhostPartySummaryAsync(partyId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ghost party summary");
                return StatusCode(500, new { error = "An error occurred while retrieving ghost party summary" });
            }
        }

        /// <summary>
        /// Block amount in ghost account
        /// </summary>
        [HttpPost("ghost-party/account/{accountId}/block")]
        public async Task<ActionResult<vm_ghostpartyaccount>> BlockGhostAmount(Guid accountId, [FromBody] decimal amount, [FromQuery] string reason)
        {
            try
            {
                var account = await _ghostPartyService.BlockAmountAsync(accountId, amount, reason);
                return Ok(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking ghost amount");
                return StatusCode(500, new { error = "An error occurred while blocking ghost amount" });
            }
        }

        /// <summary>
        /// Unblock amount in ghost account
        /// </summary>
        [HttpPost("ghost-party/account/{accountId}/unblock")]
        public async Task<ActionResult<vm_ghostpartyaccount>> UnblockGhostAmount(Guid accountId, [FromBody] decimal amount)
        {
            try
            {
                var account = await _ghostPartyService.UnblockAmountAsync(accountId, amount);
                return Ok(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking ghost amount");
                return StatusCode(500, new { error = "An error occurred while unblocking ghost amount" });
            }
        }

        #endregion

        #region Z-Report Endpoints

        /// <summary>
        /// Get daily Z-Report
        /// </summary>
        [HttpGet("reports/z-report/daily")]
        public async Task<ActionResult<vm_zreport>> GetDailyZReport(
            [FromQuery] Guid? officeId = null,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var reportDate = date ?? DateTime.Today;
                var report = await _zReportService.GetDailyZReport(officeId, reportDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily Z-Report");
                return StatusCode(500, new { error = "An error occurred while generating daily Z-Report" });
            }
        }

        [HttpPost("endday/{officeId}")]
        public async Task EndDay (Guid officeId)
        {
          await  _zReportService.EndDay(officeId);
        }

        /// <summary>
        /// Get weekly Z-Report
        /// </summary>
        [HttpGet("reports/z-report/weekly")]
        public async Task<ActionResult<vm_zreport>> GetWeeklyZReport(
            [FromQuery] Guid? officeId = null,
            [FromQuery] DateTime? weekStartDate = null)
        {
            try
            {
                var startDate = weekStartDate ?? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                var report = await _zReportService.GetWeeklyZReport(officeId, startDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating weekly Z-Report");
                return StatusCode(500, new { error = "An error occurred while generating weekly Z-Report" });
            }
        }

        /// <summary>
        /// Get monthly Z-Report
        /// </summary>
        [HttpGet("reports/z-report/monthly")]
        public async Task<ActionResult<vm_zreport>> GetMonthlyZReport(
            [FromQuery] Guid? officeId = null,
            [FromQuery] int? year = null,
            [FromQuery] int? month = null)
        {
            try
            {
                var reportYear = year ?? DateTime.Today.Year;
                var reportMonth = month ?? DateTime.Today.Month;
                var report = await _zReportService.GetMonthlyZReport(officeId, reportYear, reportMonth);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly Z-Report");
                return StatusCode(500, new { error = "An error occurred while generating monthly Z-Report" });
            }
        }

        /// <summary>
        /// Get yearly Z-Report
        /// </summary>
        [HttpGet("reports/z-report/yearly")]
        public async Task<ActionResult<vm_zreport>> GetYearlyZReport(
            [FromQuery] Guid? officeId = null,
            [FromQuery] int? year = null)
        {
            try
            {
                var reportYear = year ?? DateTime.Today.Year;
                var report = await _zReportService.GetYearlyZReport(officeId, reportYear);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating yearly Z-Report");
                return StatusCode(500, new { error = "An error occurred while generating yearly Z-Report" });
            }
        }

        /// <summary>
        /// Get custom period Z-Report
        /// </summary>
        [HttpGet("reports/z-report/custom")]
        public async Task<ActionResult<vm_zreport>> GetCustomPeriodZReport(
            [FromQuery] Guid? officeId = null,
            [FromQuery] DateTime startDate = default,
            [FromQuery] DateTime endDate = default)
        {
            try
            {
                if (startDate == default || endDate == default)
                {
                    return BadRequest(new { error = "Start date and end date are required for custom period report" });
                }

                if (endDate < startDate)
                {
                    return BadRequest(new { error = "End date must be after start date" });
                }

                var report = await _zReportService.GetCustomPeriodZReport(officeId, startDate, endDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom period Z-Report");
                return StatusCode(500, new { error = "An error occurred while generating custom period Z-Report" });
            }
        }

        /// <summary>
        /// Get historical Z-Reports
        /// </summary>
        [HttpGet("reports/z-report/history")]
        public async Task<ActionResult<List<vm_zreport>>> GetHistoricalZReports(
            [FromQuery] Guid? officeId = null,
            [FromQuery] ZReportPeriod period = ZReportPeriod.Daily,
            [FromQuery] int count = 7)
        {
            try
            {
                if (count < 1 || count > 365)
                {
                    return BadRequest(new { error = "Count must be between 1 and 365" });
                }

                var reports = await _zReportService.GetHistoricalZReports(officeId, period, count);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating historical Z-Reports");
                return StatusCode(500, new { error = "An error occurred while generating historical Z-Reports" });
            }
        }

        #endregion

        #region Vault Balance History Endpoints

        /// <summary>
        /// Get vault balance histories by office and optional date
        /// </summary>
        [HttpGet("vault/balance-histories")]
        public async Task<ActionResult<object>> GetVaultBalanceHistories(
            [FromQuery] Guid officeId,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var result = await _vaultService.GetVaultBalanceHistoriesByOfficeAsync(officeId, date);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vault balance histories");
                return StatusCode(500, new { error = "An error occurred while retrieving vault balance histories" });
            }
        }

        /// <summary>
        /// Get vault balance histories for a specific vault
        /// </summary>
        [HttpGet("vault/{vaultId}/balance-histories")]
        public async Task<ActionResult<object>> GetVaultBalanceHistoriesByVault(
            Guid vaultId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var result = await _vaultService.GetVaultBalanceHistoriesByVaultAsync(vaultId, startDate, endDate, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vault balance histories for vault {VaultId}", vaultId);
                return StatusCode(500, new { error = "An error occurred while retrieving vault balance histories" });
            }
        }

        #endregion

        #region User Office Management

        [HttpGet("user/{userId}/offices")]
        public async Task<ActionResult<List<vm_office>>> GetUserOffices(Guid userId)
        {
        try
        {
            var offices = await _userOfficeService.GetOfficesByUserAsync(userId);
            return Ok(offices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user offices for user {UserId}", userId);
            return StatusCode(500, new { error = "An error occurred while retrieving user offices" });
        }
    }

        [HttpGet("user/{userId}/office/{officeId}")]
        public async Task<ActionResult<vm_useroffice>> GetUserOffice(Guid userId, Guid officeId)
    {
        try
        {
            var userOffice = await _userOfficeService.GetUserOfficeAsync(userId, officeId);
            if (userOffice == null)
                return NotFound(new { error = "User office association not found" });
            
            return Ok(userOffice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user office for user {UserId} and office {OfficeId}", userId, officeId);
            return StatusCode(500, new { error = "An error occurred while retrieving user office" });
        }
    }

        [HttpPost("user/office/attach")]
        public async Task<ActionResult<vm_useroffice>> AttachOfficeToUser([FromBody] rm_useroffice model)
    {
        try
        {
            if (model == null || model.UserId == Guid.Empty || model.OfficeId == Guid.Empty)
                return BadRequest(new { error = "Invalid request data" });

            var result = await _userOfficeService.AttachOfficeToUserAsync(model);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching office {OfficeId} to user {UserId}", model?.OfficeId, model?.UserId);
            return StatusCode(500, new { error = "An error occurred while attaching office to user" });
        }
    }

        [HttpPost("user/{userId}/office/{officeId}/remove")]
        public async Task<ActionResult> RemoveOfficeFromUser(Guid userId, Guid officeId)
    {
        try
        {
            var result = await _userOfficeService.RemoveOfficeFromUserAsync(userId, officeId);
            if (!result)
                return NotFound(new { error = "User office association not found" });
            
            return Ok(new { message = "Office removed from user successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing office {OfficeId} from user {UserId}", officeId, userId);
            return StatusCode(500, new { error = "An error occurred while removing office from user" });
        }
    }

        [HttpPost("user/offices/update")]
        public async Task<ActionResult> UpdateUserOffices([FromBody] rm_updateuseroffices model)
    {
        try
        {
            if (model == null || model.UserId == Guid.Empty || model.OfficeIds == null)
                return BadRequest(new { error = "Invalid request data" });

            var result = await _userOfficeService.UpdateUserOfficesAsync(model.UserId, model.OfficeIds);
            if (!result)
                return BadRequest(new { error = "Failed to update user offices" });
            
            return Ok(new { message = "User offices updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating offices for user {UserId}", model?.UserId);
            return StatusCode(500, new { error = "An error occurred while updating user offices" });
        }
    }

        [HttpGet("user/{userId}/office/{officeId}/access")]
        public async Task<ActionResult<bool>> CheckUserOfficeAccess(Guid userId, Guid officeId)
    {
        try
        {
            var hasAccess = await _userOfficeService.HasUserAccessToOfficeAsync(userId, officeId);
            return Ok(new { hasAccess });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking office access for user {UserId} and office {OfficeId}", userId, officeId);
            return StatusCode(500, new { error = "An error occurred while checking office access" });
        }
    }

        [HttpGet("office/{officeId}/users")]
        public async Task<ActionResult<List<MoneyTransferTurkey.Entity.Modals.ViewModals.User.vm_user>>> GetOfficeUsers(Guid officeId)
    {
        try
        {
            var users = await _userOfficeService.GetUsersByOfficeAsync(officeId);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for office {OfficeId}", officeId);
            return StatusCode(500, new { error = "An error occurred while retrieving office users" });
        }
    }

        #endregion

        #region Expense Endpoints

        /// <summary>
        /// Create expense definition
        /// </summary>
        [HttpPost("expense/definitions")]
        public async Task<ActionResult<vm_expensedefinition>> CreateExpenseDefinition([FromBody] rm_expensedefinition request)
        {
            try
            {
                var result = await _expenseDefinitionService.CreateDefinitionAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense definition");
                return StatusCode(500, new { error = "An error occurred while creating expense definition" });
            }
        }

        /// <summary>
        /// Update expense definition
        /// </summary>
        [HttpPost("expense/definitions/{id}/update")]
        public async Task<ActionResult<vm_expensedefinition>> UpdateExpenseDefinition(Guid id, [FromBody] rm_expensedefinition request)
        {
            try
            {
                request.Id = id;
                var result = await _expenseDefinitionService.UpdateDefinitionAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expense definition");
                return StatusCode(500, new { error = "An error occurred while updating expense definition" });
            }
        }

        /// <summary>
        /// Delete expense definition
        /// </summary>
        [HttpPost("expense/definitions/{id}/delete")]
        public async Task<ActionResult> DeleteExpenseDefinition(Guid id)
        {
            try
            {
                var result = await _expenseDefinitionService.DeleteDefinitionAsync(id);
                if (!result)
                    return NotFound(new { error = "Expense definition not found" });
                
                return Ok(new { message = "Expense definition deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense definition");
                return StatusCode(500, new { error = "An error occurred while deleting expense definition" });
            }
        }

        /// <summary>
        /// Get expense definition by ID
        /// </summary>
        [HttpGet("expense/definitions/{id}")]
        public async Task<ActionResult<vm_expensedefinition>> GetExpenseDefinition(Guid id)
        {
            try
            {
                var result = await _expenseDefinitionService.GetDefinitionAsync(id);
                if (result == null)
                    return NotFound(new { error = "Expense definition not found" });
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense definition");
                return StatusCode(500, new { error = "An error occurred while getting expense definition" });
            }
        }

        /// <summary>
        /// Get all expense definitions for an office
        /// </summary>
        [HttpGet("expense/definitions")]
        public async Task<ActionResult<List<vm_expensedefinition>>> GetExpenseDefinitions(
            [FromQuery] Guid officeId,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var result = await _expenseDefinitionService.GetDefinitionsAsync(officeId, isActive);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense definitions");
                return StatusCode(500, new { error = "An error occurred while getting expense definitions" });
            }
        }

        /// <summary>
        /// Get expense definitions by category
        /// </summary>
        [HttpGet("expense/definitions/category/{category}")]
        public async Task<ActionResult<List<vm_expensedefinition>>> GetExpenseDefinitionsByCategory(
            int category,
            [FromQuery] Guid officeId)
        {
            try
            {
                var result = await _expenseDefinitionService.GetDefinitionsByCategoryAsync(officeId, category);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense definitions by category");
                return StatusCode(500, new { error = "An error occurred while getting expense definitions" });
            }
        }

        /// <summary>
        /// Create expense payment
        /// </summary>
        [HttpPost("expense/payments")]
        public async Task<ActionResult<vm_expensepayment>> CreateExpensePayment([FromBody] rm_expensepayment request)
        {
            try
            {
                var result = await _expensePaymentService.CreatePaymentAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense payment");
                return StatusCode(500, new { error = "An error occurred while creating expense payment" });
            }
        }

        /// <summary>
        /// Get expense payment by ID
        /// </summary>
        [HttpGet("expense/payments/{id}")]
        public async Task<ActionResult<vm_expensepayment>> GetExpensePayment(Guid id)
        {
            try
            {
                var result = await _expensePaymentService.GetPaymentAsync(id);
                if (result == null)
                    return NotFound(new { error = "Expense payment not found" });
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense payment");
                return StatusCode(500, new { error = "An error occurred while getting expense payment" });
            }
        }

        /// <summary>
        /// Get expense payments for an office
        /// </summary>
        [HttpGet("expense/payments")]
        public async Task<ActionResult<List<vm_expensepayment>>> GetExpensePayments(
            [FromQuery] Guid officeId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var result = await _expensePaymentService.GetPaymentsAsync(officeId, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense payments");
                return StatusCode(500, new { error = "An error occurred while getting expense payments" });
            }
        }

        /// <summary>
        /// Get expense payments by definition
        /// </summary>
        [HttpGet("expense/payments/definition/{definitionId}")]
        public async Task<ActionResult<List<vm_expensepayment>>> GetExpensePaymentsByDefinition(
            Guid definitionId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var result = await _expensePaymentService.GetPaymentsByDefinitionAsync(definitionId, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense payments by definition");
                return StatusCode(500, new { error = "An error occurred while getting expense payments" });
            }
        }

        /// <summary>
        /// Delete expense payment
        /// </summary>
        [HttpPost("expense/payments/{id}/delete")]
        public async Task<ActionResult> DeleteExpensePayment(Guid id, [FromBody] DeleteExpensePaymentRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Reason))
                    return BadRequest(new { error = "Reason is required for deleting expense payment" });

                var result = await _expensePaymentService.DeletePaymentAsync(id, request.Reason);
                if (!result)
                    return NotFound(new { error = "Expense payment not found or already deleted" });
                
                return Ok(new { message = "Expense payment deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense payment");
                return StatusCode(500, new { error = "An error occurred while deleting expense payment" });
            }
        }

        /// <summary>
        /// Get total expenses for a period
        /// </summary>
        [HttpGet("expense/reports/total")]
        public async Task<ActionResult<decimal>> GetTotalExpenses(
            [FromQuery] Guid officeId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var result = await _expensePaymentService.GetTotalExpensesAsync(officeId, startDate, endDate);
                return Ok(new { totalExpenses = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total expenses");
                return StatusCode(500, new { error = "An error occurred while getting total expenses" });
            }
        }

        /// <summary>
        /// Get expenses by category for a period
        /// </summary>
        [HttpGet("expense/reports/by-category")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetExpensesByCategory(
            [FromQuery] Guid officeId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var result = await _expensePaymentService.GetExpensesByCategoryAsync(officeId, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expenses by category");
                return StatusCode(500, new { error = "An error occurred while getting expenses by category" });
            }
        }

        #endregion

        #region TRC20 USDT Blockchain

        [HttpGet("binance/usdt-deposits")]
        public async Task<ActionResult> GetBinanceUSDTDeposits()
        {
            try
            {
                var deposits = await _trc20Service.GetRecentDepositsAsync();
                return Ok(deposits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Binance USDT deposits");
                return StatusCode(500, new { error = "Failed to fetch deposits" });
            }
        }

        #endregion

        #region Vault Counting

        /// <summary>
        /// Submit vault count
        /// </summary>
        [HttpPost("vaults/count")]
        public async Task<IActionResult> SubmitVaultCount([FromBody] rm_vaultcount request)
        {
        try
        {
            var result = await _vaultService.SubmitVaultCountAsync(request);
            if (result)
                return Ok(new { success = true, message = "Vault count submitted successfully" });
            
            return BadRequest(new { error = "Failed to submit vault count" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting vault count");
            return StatusCode(500, new { error = ex.Message });
        }
    }

        /// <summary>
        /// Get vault count history
        /// </summary>
        [HttpGet("vaults/{vaultId}/counts")]
        public async Task<ActionResult<List<vm_vaultcount>>> GetVaultCounts(
        Guid vaultId, 
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var counts = await _vaultService.GetVaultCountsAsync(vaultId, startDate, endDate);
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vault counts");
            return StatusCode(500, new { error = ex.Message });
        }
    }

        /// <summary>
        /// Reset vault count status
        /// </summary>
        [HttpPost("vaults/{vaultId}/reset-count")]
        public async Task<IActionResult> ResetVaultCountStatus(Guid vaultId)
    {
        try
        {
            var result = await _vaultService.ResetVaultCountStatusAsync(vaultId);
            if (result)
                return Ok(new { success = true, message = "Vault count status reset successfully" });
            
            return BadRequest(new { error = "Failed to reset vault count status" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting vault count status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

        /// <summary>
        /// Manually set vault should count status
        /// </summary>
        [HttpPost("vaults/{vaultId}/set-count-status")]
        public async Task<IActionResult> SetVaultShouldCount(Guid vaultId, [FromBody] bool shouldCount)
        {
            try
            {
                var result = await _vaultService.SetVaultShouldCountAsync(vaultId, shouldCount);
                if (result)
                    return Ok(new { success = true, message = $"Vault count status set to {shouldCount}" });
                
                return BadRequest(new { error = "Failed to set vault count status" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting vault count status");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion
    }

    #region Request DTOs

    public class CreateExchangeRateRequest
    {
        public Guid OfficeId { get; set; }
        public Guid SourceCurrencyId { get; set; }
        public Guid TargetCurrencyId { get; set; }
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
    }

    public class DeleteExpensePaymentRequest
    {
        public string Reason { get; set; }
    }

    #endregion
}

