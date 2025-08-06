using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaxCalculator.Core.Models;
using TaxCalculator.Data.Interfaces;
using TaxCalculator.Services.Interfaces;

namespace TaxCalculator.Services.Services
{
    public class UserTaxService : IUserTaxService
    {
        private readonly IUserIncomeRepository _userIncomeRepository;
        private readonly ITaxCalculationService _taxCalculationService;
        private readonly ILogger _logger;

        public UserTaxService(
            IUserIncomeRepository userIncomeRepository,
            ITaxCalculationService taxCalculationService,
            ILogger logger)
        {
            _userIncomeRepository = userIncomeRepository;
            _taxCalculationService = taxCalculationService;
            _logger = logger;
        }

        public async Task<UserAnnualTaxSummary> CalculateAnnualTaxAsync(Guid userId, string financialYear)
        {
            _logger.LogInformation($"Calculating annual tax for user {userId}, year {financialYear}");

            // Get monthly income data
            var monthlyIncomes = await _userIncomeRepository.GetMonthlyIncomeAsync(userId, financialYear);

            if (monthlyIncomes.Count != 12)
            {
                throw new InvalidOperationException($"Incomplete monthly income data for user {userId} in {financialYear}. Found {monthlyIncomes.Count} months, expected 12.");
            }

            // Calculate totals
            var totalGrossIncome = monthlyIncomes.Sum(m => m.GrossIncome);
            var totalDeductions = monthlyIncomes.Sum(m => m.DeductionsAmount);
            var totalTaxableIncome = monthlyIncomes.Sum(m => m.TaxableIncome);

            // Calculate tax
            var taxRequest = new TaxCalculationRequest
            {
                FinancialYear = financialYear,
                TaxableIncome = totalTaxableIncome,
                ResidencyStatus = "Resident",
                IncludeMedicareLevy = true,
                IncludeOffsets = true
            };

            var taxResult = await _taxCalculationService.CalculateTaxAsync(taxRequest);

            // Create summary
            var summary = new UserAnnualTaxSummary
            {
                UserId = userId,
                FinancialYear = financialYear,
                TotalGrossIncome = totalGrossIncome,
                TotalDeductions = totalDeductions,
                TotalTaxableIncome = totalTaxableIncome,
                IncomeTaxPayable = taxResult.IncomeTax,
                MedicareLevyPayable = taxResult.MedicareLevy,
                OtherLeviesPayable = taxResult.BudgetRepairLevy,
                TotalTaxOffsets = taxResult.TaxOffsets,
                NetTaxPayable = taxResult.NetTaxPayable,
                EffectiveTaxRate = taxResult.EffectiveRate,
                MarginalTaxRate = taxResult.MarginalRate,
                MonthlyBreakdown = monthlyIncomes.Select(m => new MonthlyIncomeSummary
                {
                    Month = m.Month,
                    GrossIncome = m.GrossIncome,
                    TaxableIncome = m.TaxableIncome,
                    Deductions = m.DeductionsAmount,
                    IncomeType = m.IncomeType
                }).ToList(),
                LastCalculated = DateTime.UtcNow
            };

            // Save summary
            await _userIncomeRepository.SaveAnnualSummaryAsync(summary);

            return summary;
        }

        public async Task<List<UserMonthlyIncome>> GetMonthlyIncomeAsync(Guid userId, string financialYear)
        {
            return await _userIncomeRepository.GetMonthlyIncomeAsync(userId, financialYear);
        }

        public async Task SaveMonthlyIncomeAsync(UserMonthlyIncome income)
        {
            await _userIncomeRepository.SaveMonthlyIncomeAsync(income);
        }

        public async Task<UserAnnualTaxSummary> GetAnnualSummaryAsync(Guid userId, string financialYear)
        {
            return await _userIncomeRepository.GetAnnualSummaryAsync(userId, financialYear);
        }

        public async Task<List<UserAnnualTaxSummary>> GetTaxHistoryAsync(Guid userId, int years = 5)
        {
            return await _userIncomeRepository.GetTaxHistoryAsync(userId, years);
        }
    }
}
