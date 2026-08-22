using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BaskentEnerji.Business.Infrastructure.Cache;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Party;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Party;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Party;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Party
{
    public class PartyService : IPartyService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly ILogger<PartyService> _logger;
        private readonly ValidationService _validationService;
        private readonly ICacheClearService _cacheClearService;

        public PartyService(BaskentEnerjiDbContext context, ILogger<PartyService> logger, ValidationService validationService, ICacheClearService cacheClearService)
        {
            _context = context;
            _logger = logger;
            _validationService = validationService;
            _cacheClearService = cacheClearService;
        }

        public async Task<vm_party> CreatePartyAsync(rm_party request)
        {
            //_cacheClearService.ClearAllCaches();
            try
            {
                await _validationService.EnsureNotViewerAsync(request.OfficeId);

                // Validate party code uniqueness within the office
                var exists = await _context.Parties
                    .AnyAsync(p => p.PartyCode == request.PartyCode && p.OfficeId == request.OfficeId);

                if (exists)
                {
                    throw new InvalidOperationException($"Party code '{request.PartyCode}' already exists for this office.");
                }

                // Check email uniqueness only if email is provided
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var emailExists = await _context.Parties
                        .AnyAsync(p => p.Email == request.Email);

                    if (emailExists)
                    {
                        throw new InvalidOperationException($"Email '{request.Email}' is already registered.");
                    }
                }

                var party = new Entity.Entities.ExchangeOffice.Party.Party
                {
                    PartyCode = request.PartyCode,
                    Name = request.Name,
                    Type = request.Type,
                    ContactPerson = request.ContactPerson,
                    Email = request.Email,
                    Phone = request.Phone,
                    Address = request.Address,
                    TaxNumber = request.TaxNumber,
                    RegistrationNumber = request.RegistrationNumber,
                    HasCreditLimit = request.HasCreditLimit,
                    DefaultPaymentTermDays = request.DefaultPaymentTermDays,
                    Notes = request.Notes,
                    OfficeId = request.OfficeId,
                    CreatedByUserId = Guid.Parse(_validationService.GetUserID()),
                    Status = PartyStatus.Active,
                    Id = Guid.NewGuid(),
                };

                _context.Parties.Add(party);
                await _context.SaveChangesAsync();

                // Sadece PRIMARY TRY hesabını aç — diğer para birimleri işlem geldiğinde
                // RecordPaymentAsync/PartyTransactionIntegration tarafından otomatik açılır.
                // Eskiden tüm 26 para birimi için hesap açılıyordu (Aleyna/Damat/Murat vb. cari
                // detayında 25 sıfır bakiyeli boş kart görünmesine yol açıyordu — 08.2026 temizliği).
                var tryCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
                if (tryCurrency != null)
                {
                    var primaryAccount = new PartyAccount
                    {
                        PartyId = party.Id,
                        CurrencyId = tryCurrency.Id,
                        AccountNumber = $"PA{DateTime.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                        Balance = 0,
                        BlockedAmount = 0,
                        Status = AccountStatus.Active
                    };
                    _context.PartyAccounts.Add(primaryAccount);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Party created: {party.PartyCode} - {party.Name}");

                return await GetPartyByIdAsync(party.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating party");
                throw;
            }
        }

        public async Task<vm_party> UpdatePartyAsync(Guid partyId, rm_party request)
        {
            try
            {
                var party = await _context.Parties
                    .FirstOrDefaultAsync(p => p.Id == partyId);

                if (party == null)
                {
                    throw new InvalidOperationException("Party not found.");
                }

                await _validationService.EnsureNotViewerAsync(party.OfficeId);

                // Validate party code uniqueness if changed
                if (party.PartyCode != request.PartyCode)
                {
                    var exists = await _context.Parties
                        .AnyAsync(p => p.PartyCode == request.PartyCode && 
                                     p.OfficeId == request.OfficeId && 
                                     p.Id != partyId);
                    
                    if (exists)
                    {
                        throw new InvalidOperationException($"Party code '{request.PartyCode}' already exists for this office.");
                    }
                }

                // Validate email uniqueness if changed and not empty
                if (!string.IsNullOrWhiteSpace(request.Email) && party.Email != request.Email)
                {
                    var emailExists = await _context.Parties
                        .AnyAsync(p => p.Email == request.Email && p.Id != partyId);
                    
                    if (emailExists)
                    {
                        throw new InvalidOperationException($"Email '{request.Email}' is already registered.");
                    }
                }

                // Update party
                party.PartyCode = request.PartyCode;
                party.Name = request.Name;
                party.Type = request.Type;
                party.ContactPerson = request.ContactPerson;
                party.Email = request.Email;
                party.Phone = request.Phone;
                party.Address = request.Address;
                party.TaxNumber = request.TaxNumber;
                party.RegistrationNumber = request.RegistrationNumber;
                party.HasCreditLimit = request.HasCreditLimit;
                party.DefaultPaymentTermDays = request.DefaultPaymentTermDays;
                party.Notes = request.Notes;
                party.ModifiedDate = DateTime.UtcNow;
                party.ModifiedByUserId = Guid.Parse(_validationService.GetUserID()); 

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Party updated: {party.PartyCode} - {party.Name}");

                return await GetPartyByIdAsync(party.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating party {partyId}");
                throw;
            }
        }

        public async Task<vm_party> GetPartyByIdAsync(Guid partyId)
        {
            try
            {
                var party = await _context.Parties
                    .Include(p => p.Office)
                    .Include(p => p.CreatedBy)
                    .Include(p => p.ModifiedBy)
                    .Include(p => p.Accounts)
                        .ThenInclude(a => a.Currency)
                    .Include(p => p.CreditLimits.Where(cl => cl.IsActive))
                        .ThenInclude(cl => cl.Currency)
                    .Include(p => p.Contacts)
                    .FirstOrDefaultAsync(p => p.Id == partyId);

                if (party == null)
                {
                    return null;
                }

                await _validationService.ValidateOfficeAccessAsync(party.OfficeId);

                // Calculate summary values
                var totalReceivables = party.Accounts.Sum(a => a.Balance > 0 ? a.Balance : 0);
                var totalPayables = party.Accounts.Sum(a => a.Balance < 0 ? Math.Abs(a.Balance) : 0);

                return new vm_party
                {
                    Id = party.Id,
                    PartyCode = party.PartyCode,
                    Name = party.Name,
                    Type = party.Type,
                    TypeName = party.Type.ToString(),
                    ContactPerson = party.ContactPerson,
                    Email = party.Email,
                    Phone = party.Phone,
                    Address = party.Address,
                    TaxNumber = party.TaxNumber,
                    RegistrationNumber = party.RegistrationNumber,
                    HasCreditLimit = party.HasCreditLimit,
                    DefaultPaymentTermDays = party.DefaultPaymentTermDays,
                    Status = party.Status,
                    StatusName = party.Status.ToString(),
                    LastTransactionDate = party.LastTransactionDate,
                    Notes = party.Notes,
                    OfficeId = party.OfficeId,
                    OfficeName = party.Office?.OfficeName,
                    CreatedDate = party.CreatedDate,
                    CreatedByUserName = $"{party.CreatedBy?.Firstname} {party.CreatedBy?.Lastname}".Trim(),
                    ModifiedDate = party.ModifiedDate,
                    ModifiedByUserName = $"{party.ModifiedBy?.Firstname} {party.ModifiedBy?.Lastname}".Trim(),
                    AccountCount = party.Accounts.Count(a => a.Balance != 0),
                    ActiveCreditLimitCount = party.CreditLimits.Count(cl => cl.IsActive),
                    TotalReceivables = totalReceivables,
                    TotalPayables = totalPayables,
                    NetBalance = totalReceivables - totalPayables,
                    Accounts = party.Accounts.Select(a => new vm_partyaccount
                    {
                        Id = a.Id,
                        PartyId = a.PartyId,
                        CurrencyId = a.CurrencyId,
                        CurrencyCode = a.Currency.CurrencyCode,
                        CurrencyName = a.Currency.CurrencyName,
                        AccountNumber = a.AccountNumber,
                        Balance = a.Balance,
                        BlockedAmount = a.BlockedAmount,
                        AvailableBalance = a.AvailableBalance,
                        Status = a.Status,
                        StatusName = a.Status.ToString(),
                        LastTransactionDate = a.LastTransactionDate
                    }).ToList(),
                    CreditLimits = party.CreditLimits.Where(cl => cl.IsActive).Select(cl => new vm_partycreditlimit
                    {
                        Id = cl.Id,
                        CurrencyId = cl.CurrencyId,
                        CurrencyCode = cl.Currency.CurrencyCode,
                        CurrencyName = cl.Currency.CurrencyName,
                        CreditLimit = cl.CreditLimit,
                        UtilizedAmount = cl.UtilizedAmount,
                        AvailableCredit = cl.AvailableCredit,
                        PaymentTermDays = cl.PaymentTermDays,
                        InterestRate = cl.InterestRate,
                        EffectiveFrom = cl.EffectiveFrom,
                        EffectiveTo = cl.EffectiveTo,
                        IsActive = cl.IsActive
                    }).ToList(),
                    Contacts = party.Contacts.Select(c => new vm_partycontact
                    {
                        Id = c.Id,
                        ContactName = c.ContactName,
                        Position = c.Position,
                        Email = c.Email,
                        Phone = c.Phone,
                        Mobile = c.Mobile,
                        IsPrimary = c.IsPrimary,
                        IsActive = c.IsActive,
                        Notes = c.Notes
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting party {partyId}");
                throw;
            }
        }

        public async Task<vm_party> GetPartyByCodeAsync(string partyCode, Guid officeId)
        {
            try
            {
                var party = await _context.Parties
                    .Where(p => p.PartyCode == partyCode && p.OfficeId == officeId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();

                if (party == Guid.Empty)
                {
                    return null;
                }

                return await GetPartyByIdAsync(party);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting party by code {partyCode}");
                throw;
            }
        }

        public async Task<List<vm_party>> GetPartiesAsync(Guid officeId, PartyType? type = null, PartyStatus? status = null)
        {
            try
            {
                var query = _context.Parties
                    .Include(p => p.Office)
                    .Include(p => p.Accounts)
                    .Where(p => p.OfficeId == officeId);

                if (type.HasValue)
                {
                    query = query.Where(p => p.Type == type.Value || p.Type == PartyType.Both);
                }

                if (status.HasValue)
                {
                    query = query.Where(p => p.Status == status.Value);
                }

                var parties = await query
                    .OrderBy(p => p.PartyCode)
                    .Select(p => new vm_party
                    {
                        Id = p.Id,
                        PartyCode = p.PartyCode,
                        Name = p.Name,
                        Type = p.Type,
                        TypeName = p.Type.ToString(),
                        ContactPerson = p.ContactPerson,
                        Email = p.Email,
                        Phone = p.Phone,
                        Status = p.Status,
                        StatusName = p.Status.ToString(),
                        LastTransactionDate = p.LastTransactionDate,
                        AccountCount = p.Accounts.Count(a => a.Balance != 0),
                        TotalReceivables = p.Accounts.Sum(a => a.Balance > 0 ? a.Balance : 0),
                        TotalPayables = p.Accounts.Sum(a => a.Balance < 0 ? Math.Abs(a.Balance) : 0),
                        NetBalance = p.Accounts.Sum(a => a.Balance),
                        OfficeId = p.Office.Id,
                        OfficeName = p.Office.OfficeName,
                        CreatedByUserName = _context.Users.FirstOrDefault(x=> x.Id == p.CreatedByUserId).Username.ToString(),
                        CreatedDate = p.CreatedDate,
                        ModifiedDate = p.ModifiedDate
                    })
                    .ToListAsync();

                return parties;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting parties for office {officeId}");
                throw;
            }
        }

        public async Task<bool> DeactivatePartyAsync(Guid partyId, string reason)
        {
            try
            {
                var party = await _context.Parties.FindAsync(partyId);
                if (party == null)
                {
                    throw new InvalidOperationException("Party not found.");
                }

                await _validationService.EnsureNotViewerAsync(party.OfficeId);

                party.Status = PartyStatus.Inactive;
                party.Notes = $"{party.Notes}\n[{DateTime.UtcNow:yyyy-MM-dd}] Deactivated: {reason}";
                party.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Party deactivated: {party.PartyCode} - Reason: {reason}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating party {partyId}");
                throw;
            }
        }

        public async Task<bool> ActivatePartyAsync(Guid partyId)
        {
            try
            {
                var party = await _context.Parties.FindAsync(partyId);
                if (party == null)
                {
                    throw new InvalidOperationException("Party not found.");
                }

                await _validationService.EnsureNotViewerAsync(party.OfficeId);

                party.Status = PartyStatus.Active;
                party.Notes = $"{party.Notes}\n[{DateTime.UtcNow:yyyy-MM-dd}] Reactivated";
                party.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Party activated: {party.PartyCode}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error activating party {partyId}");
                throw;
            }
        }

        public async Task<bool> DeletePartyAsync(Guid partyId)
        {
            try
            {
                var party = await _context.Parties
                    .Include(p => p.Accounts)
                        .ThenInclude(a => a.Entries)
                    .Include(p => p.Transactions)
                    .Include(p => p.Contacts)
                    .Include(p => p.CreditLimits)
                    .FirstOrDefaultAsync(p => p.Id == partyId);

                if (party == null)
                {
                    throw new InvalidOperationException("Party not found.");
                }

                await _validationService.EnsureNotViewerAsync(party.OfficeId);

                var nonZeroAccount = party.Accounts?.FirstOrDefault(a => a.Balance != 0);
                if (nonZeroAccount != null)
                    throw new InvalidOperationException(
                        $"Bu cari hesapta bakiye bulunduğu için silinemez. Önce bakiyeyi sıfırlayın.");

                // Remove all account entries first
                if (party.Accounts != null)
                {
                    foreach (var account in party.Accounts)
                    {
                        if (account.Entries != null && account.Entries.Any())
                        {
                            _context.PartyAccountEntries.RemoveRange(account.Entries);
                        }
                    }
                }

                // Remove party contacts
                if (party.Contacts != null && party.Contacts.Any())
                {
                    _context.PartyContacts.RemoveRange(party.Contacts);
                }

                // Remove party credit limits
                if (party.CreditLimits != null && party.CreditLimits.Any())
                {
                    _context.PartyCreditLimits.RemoveRange(party.CreditLimits);
                }

                // Remove party accounts
                if (party.Accounts != null && party.Accounts.Any())
                {
                    _context.PartyAccounts.RemoveRange(party.Accounts);
                }

                // Note: Transactions are not deleted as they affect vault balances and financial records
                // If you want to delete transactions too, uncomment the following:
                /*
                if (party.Transactions != null && party.Transactions.Any())
                {
                    // First remove transaction details
                    foreach (var transaction in party.Transactions)
                    {
                        if (transaction.Details != null && transaction.Details.Any())
                        {
                            _context.TransactionDetails.RemoveRange(transaction.Details);
                        }
                    }
                    // Then remove transactions
                    _context.Transactions.RemoveRange(party.Transactions);
                }
                */

                // Remove the party
                _context.Parties.Remove(party);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Party deleted: {party.PartyCode} (ID: {partyId})");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting party {partyId}");
                throw;
            }
        }

        public async Task<vm_partycontact> AddContactAsync(Guid partyId, rm_partycontact request)
        {
            try
            {
                var party = await _context.Parties.FindAsync(partyId);
                if (party == null)
                {
                    throw new InvalidOperationException("Party not found.");
                }

                await _validationService.EnsureNotViewerAsync(party.OfficeId);

                var contact = new PartyContact
                {
                    PartyId = partyId,
                    ContactName = request.ContactName,
                    Position = request.Position,
                    Email = request.Email,
                    Phone = request.Phone,
                    Mobile = request.Mobile,
                    IsPrimary = request.IsPrimary,
                    IsActive = request.IsActive,
                    Notes = request.Notes
                };

                _context.PartyContacts.Add(contact);
                await _context.SaveChangesAsync();

                return new vm_partycontact
                {
                    Id = contact.Id,
                    PartyId = contact.PartyId,
                    ContactName = contact.ContactName,
                    Position = contact.Position,
                    Email = contact.Email,
                    Phone = contact.Phone,
                    Mobile = contact.Mobile,
                    IsPrimary = contact.IsPrimary,
                    IsActive = contact.IsActive,
                    Notes = contact.Notes,
                    CreatedDate = contact.CreatedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding contact for party {partyId}");
                throw;
            }
        }

        public async Task<vm_partycontact> UpdateContactAsync(Guid contactId, rm_partycontact request)
        {
            try
            {
                var contact = await _context.PartyContacts.FindAsync(contactId);
                if (contact == null)
                {
                    throw new InvalidOperationException("Contact not found.");
                }

                var contactPartyOfficeId = await _context.Parties
                    .Where(p => p.Id == contact.PartyId)
                    .Select(p => p.OfficeId)
                    .FirstOrDefaultAsync();
                await _validationService.EnsureNotViewerAsync(contactPartyOfficeId);

                contact.ContactName = request.ContactName;
                contact.Position = request.Position;
                contact.Email = request.Email;
                contact.Phone = request.Phone;
                contact.Mobile = request.Mobile;
                contact.IsPrimary = request.IsPrimary;
                contact.IsActive = request.IsActive;
                contact.Notes = request.Notes;

                await _context.SaveChangesAsync();

                return new vm_partycontact
                {
                    Id = contact.Id,
                    PartyId = contact.PartyId,
                    ContactName = contact.ContactName,
                    Position = contact.Position,
                    Email = contact.Email,
                    Phone = contact.Phone,
                    Mobile = contact.Mobile,
                    IsPrimary = contact.IsPrimary,
                    IsActive = contact.IsActive,
                    Notes = contact.Notes,
                    CreatedDate = contact.CreatedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating contact {contactId}");
                throw;
            }
        }

        public async Task<bool> RemoveContactAsync(Guid contactId)
        {
            try
            {
                var contact = await _context.PartyContacts.FindAsync(contactId);
                if (contact == null)
                {
                    throw new InvalidOperationException("Contact not found.");
                }

                var contactPartyOfficeId = await _context.Parties
                    .Where(p => p.Id == contact.PartyId)
                    .Select(p => p.OfficeId)
                    .FirstOrDefaultAsync();
                await _validationService.EnsureNotViewerAsync(contactPartyOfficeId);

                _context.PartyContacts.Remove(contact);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing contact {contactId}");
                throw;
            }
        }

        public async Task<List<vm_partycontact>> GetPartyContactsAsync(Guid partyId)
        {
            try
            {
                var contacts = await _context.PartyContacts
                    .Where(c => c.PartyId == partyId)
                    .OrderByDescending(c => c.IsPrimary)
                    .ThenBy(c => c.ContactName)
                    .Select(c => new vm_partycontact
                    {
                        Id = c.Id,
                        PartyId = c.PartyId,
                        ContactName = c.ContactName,
                        Position = c.Position,
                        Email = c.Email,
                        Phone = c.Phone,
                        Mobile = c.Mobile,
                        IsPrimary = c.IsPrimary,
                        IsActive = c.IsActive,
                        Notes = c.Notes,
                        CreatedDate = c.CreatedDate
                    })
                    .ToListAsync();

                return contacts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting contacts for party {partyId}");
                throw;
            }
        }

        public async Task<bool> ValidatePartyCodeAsync(string partyCode, Guid officeId, Guid? excludePartyId = null)
        {
            try
            {
                var query = _context.Parties
                    .Where(p => p.PartyCode == partyCode && p.OfficeId == officeId);

                if (excludePartyId.HasValue)
                {
                    query = query.Where(p => p.Id != excludePartyId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating party code {partyCode}");
                throw;
            }
        }

        public async Task<bool> ValidateEmailAsync(string email, Guid? excludePartyId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return true;

                var query = _context.Parties.Where(p => p.Email == email);

                if (excludePartyId.HasValue)
                {
                    query = query.Where(p => p.Id != excludePartyId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating email {email}");
                throw;
            }
        }

        public async Task<bool> ValidateTaxNumberAsync(string taxNumber, Guid? excludePartyId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(taxNumber))
                    return true;

                var query = _context.Parties.Where(p => p.TaxNumber == taxNumber);

                if (excludePartyId.HasValue)
                {
                    query = query.Where(p => p.Id != excludePartyId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating tax number {taxNumber}");
                throw;
            }
        }
    }
}