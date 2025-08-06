using System;
using System.Collections.Generic;
using System.Linq;

namespace TaxCalculator.TestClient
{
    public class TaxCalculationEngine
    {
        private readonly Dictionary<string, List<TaxBracket>> _taxBrackets;

        public TaxCalculationEngine()
        {
            _taxBrackets = InitializeTaxBrackets();
        }

        public TaxCalculationResult CalculateTax(TaxCalculationRequest request)
        {
            if (request.TaxableIncome < 0)
                throw new ArgumentException("Taxable income cannot be negative");

            var brackets = _taxBrackets.ContainsKey(request.FinancialYear) 
                ? _taxBrackets[request.FinancialYear] 
                : _taxBrackets["2024-25"]; // Default to current year

            var incomeTax = CalculateProgressiveIncomeTax(request.TaxableIncome, brackets);
            var medicareLevy = request.IncludeMedicareLevy ? request.TaxableIncome * 0.02m : 0;
            var budgetRepairLevy = CalculateBudgetRepairLevy(request.TaxableIncome, request.FinancialYear);
            var taxOffsets = request.IncludeOffsets ? CalculateLITO(request.TaxableIncome) : 0;

            var totalLevies = medicareLevy + budgetRepairLevy;
            var grossTax = incomeTax + totalLevies;
            var netTaxPayable = Math.Max(0, grossTax - taxOffsets);
            var netIncome = request.TaxableIncome - netTaxPayable;
            var effectiveRate = request.TaxableIncome > 0 ? netTaxPayable / request.TaxableIncome : 0;
            var marginalRate = GetMarginalTaxRate(request.TaxableIncome, brackets);

            return new TaxCalculationResult
            {
                TaxableIncome = request.TaxableIncome,
                IncomeTax = incomeTax,
                MedicareLevy = medicareLevy,
                BudgetRepairLevy = budgetRepairLevy,
                TotalLevies = totalLevies,
                TaxOffsets = taxOffsets,
                NetTaxPayable = netTaxPayable,
                NetIncome = netIncome,
                EffectiveRate = effectiveRate,
                MarginalRate = marginalRate,
                CalculationBreakdown = $"Income Tax: ${incomeTax:N2}, Medicare: ${medicareLevy:N2}, Offsets: ${taxOffsets:N2}"
            };
        }

        private decimal CalculateProgressiveIncomeTax(decimal income, List<TaxBracket> brackets)
        {
            decimal totalTax = 0;

            foreach (var bracket in brackets.OrderBy(b => b.BracketOrder))
            {
                if (income <= bracket.MinIncome) continue;

                var bracketMax = bracket.MaxIncome ?? decimal.MaxValue;
                var taxableInBracket = Math.Min(income, bracketMax) - bracket.MinIncome + 1;

                if (taxableInBracket > 0)
                {
                    var taxInBracket = bracket.FixedAmount + (taxableInBracket * bracket.TaxRate);
                    totalTax = taxInBracket; // For progressive calculation, take the applicable bracket
                }

                if (income <= bracketMax) break;
            }

            return totalTax;
        }

        private decimal CalculateBudgetRepairLevy(decimal income, string financialYear)
        {
            // Budget Repair Levy: 2% on income >$180k (2015-16 to 2017-18)
            var budgetRepairYears = new[] { "2015-16", "2016-17", "2017-18" };
            if (budgetRepairYears.Contains(financialYear) && income > 180000)
            {
                return income * 0.02m;
            }
            return 0;
        }

        private decimal CalculateLITO(decimal income)
        {
            // Low Income Tax Offset - simplified calculation
            if (income <= 37500) return 700m;
            if (income <= 45000) return Math.Max(0, 700m - ((income - 37500) * 0.05m));
            return 0;
        }

        private decimal GetMarginalTaxRate(decimal income, List<TaxBracket> brackets)
        {
            var applicableBracket = brackets
                .Where(b => income >= b.MinIncome && (b.MaxIncome == null || income <= b.MaxIncome))
                .OrderBy(b => b.BracketOrder)
                .FirstOrDefault();

            return applicableBracket?.TaxRate ?? 0;
        }

        private Dictionary<string, List<TaxBracket>> InitializeTaxBrackets()
        {
            return new Dictionary<string, List<TaxBracket>>
            {
                ["2024-25"] = new List<TaxBracket>
                {
                    new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0.00m, FixedAmount = 0, BracketOrder = 1 },
                    new TaxBracket { MinIncome = 18201, MaxIncome = 45000, TaxRate = 0.16m, FixedAmount = 0, BracketOrder = 2 },
                    new TaxBracket { MinIncome = 45001, MaxIncome = 135000, TaxRate = 0.30m, FixedAmount = 4288, BracketOrder = 3 },
                    new TaxBracket { MinIncome = 135001, MaxIncome = 190000, TaxRate = 0.37m, FixedAmount = 31288, BracketOrder = 4 },
                    new TaxBracket { MinIncome = 190001, MaxIncome = null, TaxRate = 0.45m, FixedAmount = 51638, BracketOrder = 5 }
                },
                ["2023-24"] = new List<TaxBracket>
                {
                    new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0.00m, FixedAmount = 0, BracketOrder = 1 },
                    new TaxBracket { MinIncome = 18201, MaxIncome = 45000, TaxRate = 0.19m, FixedAmount = 0, BracketOrder = 2 },
                    new TaxBracket { MinIncome = 45001, MaxIncome = 120000, TaxRate = 0.325m, FixedAmount = 5092, BracketOrder = 3 },
                    new TaxBracket { MinIncome = 120001, MaxIncome = 180000, TaxRate = 0.37m, FixedAmount = 29467, BracketOrder = 4 },
                    new TaxBracket { MinIncome = 180001, MaxIncome = null, TaxRate = 0.45m, FixedAmount = 51667, BracketOrder = 5 }
                },
                ["2015-16"] = new List<TaxBracket>
                {
                    new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0.00m, FixedAmount = 0, BracketOrder = 1 },
                    new TaxBracket { MinIncome = 18201, MaxIncome = 37000, TaxRate = 0.19m, FixedAmount = 0, BracketOrder = 2 },
                    new TaxBracket { MinIncome = 37001, MaxIncome = 80000, TaxRate = 0.325m, FixedAmount = 3572, BracketOrder = 3 },
                    new TaxBracket { MinIncome = 80001, MaxIncome = 180000, TaxRate = 0.37m, FixedAmount = 17547, BracketOrder = 4 },
                    new TaxBracket { MinIncome = 180001, MaxIncome = null, TaxRate = 0.45m, FixedAmount = 54547, BracketOrder = 5 }
                }
            };
        }
    }
}
