using System.Collections.Generic;
using System.Threading.Tasks;
using TaxCalculator.Core.Models;

namespace TaxCalculator.Data.Interfaces
{
    public interface ITaxBracketRepository
    {
        Task<List<TaxBracket>> GetTaxBracketsAsync(string financialYear);
        Task<List<TaxOffset>> GetTaxOffsetsAsync(string financialYear);
        Task<List<TaxLevy>> GetTaxLeviesAsync(string financialYear);
        Task<List<string>> GetAvailableFinancialYearsAsync();
    }
}
