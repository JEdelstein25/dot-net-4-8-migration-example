using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaxCalculator.Core.Models;
using TaxCalculator.Data.Interfaces;
using TaxCalculator.Services.Interfaces;

namespace TaxCalculator.Services.Services
{
    public class TaxCalculationService : ITaxCalculationService
    {
        private readonly ITaxBracketRepository _taxBracketRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger _logger;

        public TaxCalculationService(
            ITaxBracketRepository taxBracketRepository,
            ICacheService cacheService,
            ILogger logger)
        {
            _taxBracketRepository = taxBracketRepository;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request)
        {
            if (request.TaxableIncome < 0)
                throw new ArgumentException("Taxable income cannot be negative");

            _logger.LogInformation($"Calculating tax for {request.FinancialYear}, income: {request.TaxableIncome}");

            // Get cached tax brackets
            var cacheKey = $"tax_brackets_{request.FinancialYear}";
            var brackets = await _cacheService.GetAsync<List<TaxBracket>>(cacheKey);

            if (brackets == null)
            {
                brackets = await _taxBracketRepository.GetTaxBracketsAsync(request.FinancialYear);
                await _cacheService.SetAsync(cacheKey, brackets, TimeSpan.FromHours(24));
            }

            // Calculate progressive income tax
            var incomeTaxResult = CalculateProgressiveIncomeTax(request.TaxableIncome, brackets);

            // Calculate Medicare levy
            var medicareLevyResult = await CalculateMedicareLevyAsync(request.TaxableIncome, request.FinancialYear);

            // Calculate other levies (Budget Repair, etc.)
            var otherLeviesResult = await CalculateOtherLeviesAsync(request.TaxableIncome, request.FinancialYear);

            // Calculate tax offsets
            var offsetsResult = await CalculateTaxOffsetsAsync(request.TaxableIncome, request.FinancialYear);

            // Combine results
            var result = new TaxCalculationResult
            {
                TaxableIncome = request.TaxableIncome,
                IncomeTax = incomeTaxResult.TotalTax,
                MedicareLevy = medicareLevyResult.Amount,
                BudgetRepairLevy = otherLeviesResult.BudgetRepairLevy,
                TotalLevies = medicareLevyResult.Amount + otherLeviesResult.BudgetRepairLevy,
                TaxOffsets = offsetsResult.TotalOffsets,
                BracketBreakdown = incomeTaxResult.BracketBreakdown
            };

            // Calculate final amounts
            var grossTax = result.IncomeTax + result.TotalLevies;
            result.NetTaxPayable = Math.Max(0, grossTax - result.TaxOffsets);
            result.NetIncome = result.TaxableIncome - result.NetTaxPayable;
            result.EffectiveRate = result.TaxableIncome > 0 ? result.NetTaxPayable / result.TaxableIncome : 0;
            result.MarginalRate = GetMarginalTaxRate(request.TaxableIncome, brackets);

            return result;
        }

        private ProgressiveIncomeTaxResult CalculateProgressiveIncomeTax(decimal income, List<TaxBracket> brackets)
        {
            var result = new ProgressiveIncomeTaxResult
            {
                BracketBreakdown = new List<TaxBracketCalculation>()
            };

            decimal totalTax = 0;
            decimal remainingIncome = income;

            foreach (var bracket in brackets.OrderBy(b => b.BracketOrder))
            {
                if (remainingIncome <= 0) break;

                var bracketMin = bracket.MinIncome;
                var bracketMax = bracket.MaxIncome ?? decimal.MaxValue;

                if (income <= bracketMin) continue;

                decimal taxableInBracket;
                if (income > bracketMax)
                    taxableInBracket = bracketMax - bracketMin + 1;
                else
                    taxableInBracket = income - bracketMin + 1;

                if (taxableInBracket <= 0) continue;

                var taxInBracket = bracket.FixedAmount + (taxableInBracket * bracket.TaxRate);
                totalTax += taxInBracket;

                result.BracketBreakdown.Add(new TaxBracketCalculation
                {
                    MinIncome = bracketMin,
                    MaxIncome = bracket.MaxIncome,
                    TaxRate = bracket.TaxRate,
                    TaxableInBracket = taxableInBracket,
                    TaxPayable = taxInBracket
                });

                remainingIncome -= taxableInBracket;
            }

            result.TotalTax = totalTax;
            return result;
        }

        private async Task<LevyCalculation> CalculateMedicareLevyAsync(decimal income, string financialYear)
        {
            var levies = await _taxBracketRepository.GetTaxLeviesAsync(financialYear);
            var medicareLevy = levies.FirstOrDefault(l => l.LevyType == "Medicare");

            if (medicareLevy == null || income <= medicareLevy.ThresholdIncome)
            {
                return new LevyCalculation
                {
                    LevyType = "Medicare",
                    ThresholdIncome = 0,
                    LevyRate = 0,
                    Amount = 0
                };
            }

            var amount = income * medicareLevy.LevyRate;
            return new LevyCalculation
            {
                LevyType = "Medicare",
                ThresholdIncome = medicareLevy.ThresholdIncome,
                LevyRate = medicareLevy.LevyRate,
                Amount = amount
            };
        }

        private async Task<LevyCalculation> CalculateOtherLeviesAsync(decimal income, string financialYear)
        {
            var levies = await _taxBracketRepository.GetTaxLeviesAsync(financialYear);
            var budgetRepairLevy = levies.FirstOrDefault(l => l.LevyType == "BudgetRepair");

            decimal budgetRepairAmount = 0;
            if (budgetRepairLevy != null && income > budgetRepairLevy.ThresholdIncome)
            {
                budgetRepairAmount = income * budgetRepairLevy.LevyRate;
            }

            return new LevyCalculation
            {
                LevyType = "Other",
                BudgetRepairLevy = budgetRepairAmount,
                Amount = 0
            };
        }

        private async Task<OffsetCalculation> CalculateTaxOffsetsAsync(decimal income, string financialYear)
        {
            var offsets = await _taxBracketRepository.GetTaxOffsetsAsync(financialYear);
            var lito = offsets.FirstOrDefault(o => o.OffsetType == "LITO");

            decimal totalOffsets = 0;

            if (lito != null && (lito.MaxIncome == null || income <= lito.MaxIncome))
            {
                if (lito.PhaseOutStart != null && income > lito.PhaseOutStart)
                {
                    var phaseOutAmount = (income - lito.PhaseOutStart.Value) * (lito.PhaseOutRate ?? 0);
                    totalOffsets = Math.Max(0, lito.MaxOffset - phaseOutAmount);
                }
                else
                {
                    totalOffsets = lito.MaxOffset;
                }
            }

            return new OffsetCalculation
            {
                OffsetType = "Total",
                TotalOffsets = totalOffsets
            };
        }

        private decimal GetMarginalTaxRate(decimal income, List<TaxBracket> brackets)
        {
            var applicableBracket = brackets
                .Where(b => income >= b.MinIncome && (b.MaxIncome == null || income <= b.MaxIncome))
                .OrderBy(b => b.BracketOrder)
                .FirstOrDefault();

            return applicableBracket?.TaxRate ?? 0;
        }

        public async Task<List<TaxBracket>> GetTaxBracketsAsync(string financialYear)
        {
            return await _taxBracketRepository.GetTaxBracketsAsync(financialYear);
        }

        public async Task<TaxCalculationResult> CompareTaxAcrossYearsAsync(decimal income, List<string> years)
        {
            // Implementation for comparing tax across years
            throw new NotImplementedException();
        }

        public async Task<List<TaxCalculationResult>> GetTaxHistoryAsync(decimal income, int years = 10)
        {
            // Implementation for getting tax history
            throw new NotImplementedException();
        }
    }

    public class ProgressiveIncomeTaxResult
    {
        public decimal TotalTax { get; set; }
        public List<TaxBracketCalculation> BracketBreakdown { get; set; }
    }
}
