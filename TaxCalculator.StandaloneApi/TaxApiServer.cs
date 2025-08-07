using TaxCalculator.Core.Models;

namespace TaxCalculator.StandaloneApi;

public class TaxCalculatorService
{
    private readonly Dictionary<string, List<TaxBracket>> _taxBrackets;

    public TaxCalculatorService()
    {
        _taxBrackets = InitializeTaxBrackets();
    }

    public Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request)
    {
        var result = CalculateTax(request.TaxableIncome, request.FinancialYear);
        return Task.FromResult(result);
    }

    public Task<List<TaxBracket>> GetTaxBracketsAsync(string year)
    {
        if (_taxBrackets.ContainsKey(year))
        {
            return Task.FromResult(_taxBrackets[year]);
        }
        
        throw new ArgumentException($"Tax brackets not found for year: {year}");
    }

    private TaxCalculationResult CalculateTax(decimal income, string year)
    {
        var brackets = _taxBrackets.ContainsKey(year) ? _taxBrackets[year] : _taxBrackets["2024-25"];
        
        decimal totalTax = 0;
        foreach (var bracket in brackets)
        {
            if (income <= bracket.MinIncome) continue;

            var bracketMax = bracket.MaxIncome ?? decimal.MaxValue;
            if (income > bracketMax)
            {
                var taxableInBracket = bracketMax - bracket.MinIncome + 1;
                totalTax = bracket.FixedAmount + (taxableInBracket * bracket.TaxRate);
            }
            else
            {
                var taxableInBracket = income - bracket.MinIncome + 1;
                totalTax = bracket.FixedAmount + (taxableInBracket * bracket.TaxRate);
                break;
            }
        }

        var medicareLevy = income * 0.02m;
        var budgetRepairLevy = (year == "2015-16" || year == "2016-17" || year == "2017-18") && income > 180000 ? income * 0.02m : 0;
        var lito = income <= 37500 ? 700m : (income <= 45000 ? Math.Max(0, 700m - ((income - 37500) * 0.05m)) : 0);

        var grossTax = totalTax + medicareLevy + budgetRepairLevy;
        var netTax = Math.Max(0, grossTax - lito);

        return new TaxCalculationResult
        {
            TaxableIncome = income,
            IncomeTax = totalTax,
            MedicareLevy = medicareLevy,
            BudgetRepairLevy = budgetRepairLevy,
            TotalLevies = medicareLevy + budgetRepairLevy,
            TaxOffsets = lito,
            NetTaxPayable = netTax,
            NetIncome = income - netTax,
            EffectiveRate = income > 0 ? netTax / income : 0,
            MarginalRate = GetMarginalRate(income, brackets)
        };
    }

    private decimal GetMarginalRate(decimal income, List<TaxBracket> brackets)
    {
        foreach (var bracket in brackets)
        {
            if (income >= bracket.MinIncome && (bracket.MaxIncome == null || income <= bracket.MaxIncome))
                return bracket.TaxRate;
        }
        return 0;
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
