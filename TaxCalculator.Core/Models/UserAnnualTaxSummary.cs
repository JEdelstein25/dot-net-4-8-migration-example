using System;
using System.Collections.Generic;

namespace TaxCalculator.Core.Models
{
    public class UserAnnualTaxSummary
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }
        public string FinancialYear { get; set; }
        public decimal TotalGrossIncome { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalTaxableIncome { get; set; }
        public decimal IncomeTaxPayable { get; set; }
        public decimal MedicareLevyPayable { get; set; }
        public decimal OtherLeviesPayable { get; set; }
        public decimal TotalTaxOffsets { get; set; }
        public decimal NetTaxPayable { get; set; }
        public decimal EffectiveTaxRate { get; set; }
        public decimal MarginalTaxRate { get; set; }
        public DateTime CalculationDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public List<MonthlyIncomeSummary> MonthlyBreakdown { get; set; } = new List<MonthlyIncomeSummary>();
        public DateTime LastCalculated { get; set; }
    }
}
