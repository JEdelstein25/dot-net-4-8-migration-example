using System.Collections.Generic;
using System.Threading.Tasks;
using TaxCalculator.Core.Models;

namespace TaxCalculator.Services.Interfaces
{
    public interface ITaxCalculationService
    {
        Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request);
        Task<List<TaxBracket>> GetTaxBracketsAsync(string financialYear);
        Task<TaxCalculationResult> CompareTaxAcrossYearsAsync(decimal income, List<string> years);
        Task<List<TaxCalculationResult>> GetTaxHistoryAsync(decimal income, int years = 10);
    }
}
