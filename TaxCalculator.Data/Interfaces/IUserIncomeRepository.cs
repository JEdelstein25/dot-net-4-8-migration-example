using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaxCalculator.Core.Models;

namespace TaxCalculator.Data.Interfaces
{
    public interface IUserIncomeRepository
    {
        Task<List<UserMonthlyIncome>> GetMonthlyIncomeAsync(Guid userId, string financialYear);
        Task SaveMonthlyIncomeAsync(UserMonthlyIncome income);
        Task UpdateMonthlyIncomeAsync(UserMonthlyIncome income);
        Task<UserAnnualTaxSummary> GetAnnualSummaryAsync(Guid userId, string financialYear);
        Task SaveAnnualSummaryAsync(UserAnnualTaxSummary summary);
        Task<List<UserAnnualTaxSummary>> GetTaxHistoryAsync(Guid userId, int years = 5);
    }
}
