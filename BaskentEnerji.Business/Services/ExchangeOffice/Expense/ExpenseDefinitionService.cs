using AutoMapper;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Expense;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Expense
{
    public class ExpenseDefinitionService : IExpenseDefinitionService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly IMapper _mapper;

        public ExpenseDefinitionService(BaskentEnerjiDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<vm_expensedefinition> CreateDefinitionAsync(rm_expensedefinition request)
        {
            // Check if code is unique
            var existingCode = await _context.ExpenseDefinitions
                .AnyAsync(ed => ed.OfficeId == request.OfficeId && ed.Code == request.Code);
            
            if (existingCode)
                throw new InvalidOperationException($"Expense code '{request.Code}' already exists for this office");

            var definition = new ExpenseDefinition
            {
                Id = Guid.NewGuid(),
                OfficeId = request.OfficeId,
                Code = request.Code,
                Name = request.Name,
                Category = request.Category,
                Description = request.Description,
                IsActive = request.IsActive,
                IsRecurring = request.IsRecurring,
                RecurrencePeriod = request.RecurrencePeriod,
                DefaultAmount = request.DefaultAmount,
                DefaultCurrencyId = request.DefaultCurrencyId,
                CreatedDate = DateTime.UtcNow
            };

            _context.ExpenseDefinitions.Add(definition);
            await _context.SaveChangesAsync();

            return await GetDefinitionAsync(definition.Id);
        }

        public async Task<vm_expensedefinition> UpdateDefinitionAsync(rm_expensedefinition request)
        {
            if (!request.Id.HasValue)
                throw new ArgumentException("Id is required for update");

            var definition = await _context.ExpenseDefinitions
                .FirstOrDefaultAsync(ed => ed.Id == request.Id.Value);

            if (definition == null)
                throw new InvalidOperationException("Expense definition not found");

            // Check if code is unique (excluding current record)
            var existingCode = await _context.ExpenseDefinitions
                .AnyAsync(ed => ed.OfficeId == request.OfficeId && 
                               ed.Code == request.Code && 
                               ed.Id != request.Id.Value);
            
            if (existingCode)
                throw new InvalidOperationException($"Expense code '{request.Code}' already exists for this office");

            definition.Code = request.Code;
            definition.Name = request.Name;
            definition.Category = request.Category;
            definition.Description = request.Description;
            definition.IsActive = request.IsActive;
            definition.IsRecurring = request.IsRecurring;
            definition.RecurrencePeriod = request.RecurrencePeriod;
            definition.DefaultAmount = request.DefaultAmount;
            definition.DefaultCurrencyId = request.DefaultCurrencyId;

            await _context.SaveChangesAsync();

            return await GetDefinitionAsync(definition.Id);
        }

        public async Task<bool> DeleteDefinitionAsync(Guid id)
        {
            var definition = await _context.ExpenseDefinitions
                .Include(ed => ed.Payments)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (definition == null)
                return false;

            // Check if there are any payments
            if (definition.Payments != null && definition.Payments.Any())
            {
                // Soft delete - just mark as inactive
                definition.IsActive = false;
                await _context.SaveChangesAsync();
            }
            else
            {
                // Hard delete if no payments
                _context.ExpenseDefinitions.Remove(definition);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<vm_expensedefinition> GetDefinitionAsync(Guid id)
        {
            var definition = await _context.ExpenseDefinitions
                .Include(ed => ed.Office)
                .Include(ed => ed.DefaultCurrency)
                .Include(ed => ed.Payments)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (definition == null)
                return null;

            return MapToViewModel(definition);
        }

        public async Task<List<vm_expensedefinition>> GetDefinitionsAsync(Guid officeId, bool? isActive = null)
        {
            var query = _context.ExpenseDefinitions
                .Include(ed => ed.Office)
                .Include(ed => ed.DefaultCurrency)
                .Include(ed => ed.Payments)
                .Where(ed => ed.OfficeId == officeId);

            if (isActive.HasValue)
                query = query.Where(ed => ed.IsActive == isActive.Value);

            var definitions = await query.OrderBy(ed => ed.Name).ToListAsync();

            return definitions.Select(MapToViewModel).ToList();
        }

        public async Task<List<vm_expensedefinition>> GetDefinitionsByCategoryAsync(Guid officeId, int category)
        {
            var definitions = await _context.ExpenseDefinitions
                .Include(ed => ed.Office)
                .Include(ed => ed.DefaultCurrency)
                .Include(ed => ed.Payments)
                .Where(ed => ed.OfficeId == officeId && (int)ed.Category == category && ed.IsActive)
                .OrderBy(ed => ed.Name)
                .ToListAsync();

            return definitions.Select(MapToViewModel).ToList();
        }

        public async Task<bool> IsCodeUniqueAsync(Guid officeId, string code, Guid? excludeId = null)
        {
            var query = _context.ExpenseDefinitions
                .Where(ed => ed.OfficeId == officeId && ed.Code == code);

            if (excludeId.HasValue)
                query = query.Where(ed => ed.Id != excludeId.Value);

            return !await query.AnyAsync();
        }

        private vm_expensedefinition MapToViewModel(ExpenseDefinition definition)
        {
            return new vm_expensedefinition
            {
                Id = definition.Id,
                OfficeId = definition.OfficeId,
                OfficeName = definition.Office?.OfficeName,
                Code = definition.Code,
                Name = definition.Name,
                Category = definition.Category,
                CategoryName = GetCategoryName(definition.Category),
                Description = definition.Description,
                IsActive = definition.IsActive,
                IsRecurring = definition.IsRecurring,
                RecurrencePeriod = definition.RecurrencePeriod,
                RecurrencePeriodName = definition.RecurrencePeriod.HasValue ? 
                    GetRecurrencePeriodName(definition.RecurrencePeriod.Value) : null,
                DefaultAmount = definition.DefaultAmount,
                DefaultCurrencyId = definition.DefaultCurrencyId,
                DefaultCurrencyCode = definition.DefaultCurrency?.CurrencyCode,
                CreatedDate = definition.CreatedDate,
                TotalPayments = definition.Payments?.Where(p => !p.IsDeleted).Sum(p => p.Amount) ?? 0,
                PaymentCount = definition.Payments?.Count(p => !p.IsDeleted) ?? 0
            };
        }

        private string GetCategoryName(ExpenseCategory category)
        {
            return category switch
            {
                ExpenseCategory.Salary => "Maaş",
                ExpenseCategory.Rent => "Kira",
                ExpenseCategory.Utilities => "Faturalar",
                ExpenseCategory.Office => "Ofis Giderleri",
                ExpenseCategory.Marketing => "Pazarlama",
                ExpenseCategory.Travel => "Seyahat",
                ExpenseCategory.Insurance => "Sigorta",
                ExpenseCategory.Tax => "Vergi",
                ExpenseCategory.Maintenance => "Bakım",
                ExpenseCategory.Other => "Diğer",
                _ => category.ToString()
            };
        }

        private string GetRecurrencePeriodName(RecurrencePeriod period)
        {
            return period switch
            {
                RecurrencePeriod.Daily => "Günlük",
                RecurrencePeriod.Weekly => "Haftalık",
                RecurrencePeriod.Monthly => "Aylık",
                RecurrencePeriod.Quarterly => "3 Aylık",
                RecurrencePeriod.Yearly => "Yıllık",
                _ => period.ToString()
            };
        }
    }
}