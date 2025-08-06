using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaxCalculator.Core.Models;

namespace TaxCalculator.Services.Interfaces
{
    public interface IUserTaxService
    {
        Task<UserAnnualTaxSummary> CalculateAnnualTaxAsync(Guid userId, string financialYear);
        Task<List<UserMonthlyIncome>> GetMonthlyIncomeAsync(Guid userId, string financialYear);
        Task SaveMonthlyIncomeAsync(UserMonthlyIncome income);
        Task<UserAnnualTaxSummary> GetAnnualSummaryAsync(Guid userId, string financialYear);
        Task<List<UserAnnualTaxSummary>> GetTaxHistoryAsync(Guid userId, int years = 5);
    }
}
