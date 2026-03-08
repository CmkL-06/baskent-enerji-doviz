using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Party;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Party;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Party
{
    public interface IPartyService
    {
        // Party Management
        Task<vm_party> CreatePartyAsync(rm_party request);
        Task<vm_party> UpdatePartyAsync(Guid partyId, rm_party request);
        Task<vm_party> GetPartyByIdAsync(Guid partyId);
        Task<vm_party> GetPartyByCodeAsync(string partyCode, Guid officeId);
        Task<List<vm_party>> GetPartiesAsync(Guid officeId, PartyType? type = null, PartyStatus? status = null);
        Task<bool> DeactivatePartyAsync(Guid partyId, string reason);
        Task<bool> ActivatePartyAsync(Guid partyId);
        Task<bool> DeletePartyAsync(Guid partyId);
        
        // Contact Management
        Task<vm_partycontact> AddContactAsync(Guid partyId, rm_partycontact request);
        Task<vm_partycontact> UpdateContactAsync(Guid contactId, rm_partycontact request);
        Task<bool> RemoveContactAsync(Guid contactId);
        Task<List<vm_partycontact>> GetPartyContactsAsync(Guid partyId);
        
        // Validation
        Task<bool> ValidatePartyCodeAsync(string partyCode, Guid officeId, Guid? excludePartyId = null);
        Task<bool> ValidateEmailAsync(string email, Guid? excludePartyId = null);
        Task<bool> ValidateTaxNumberAsync(string taxNumber, Guid? excludePartyId = null);
    }
}