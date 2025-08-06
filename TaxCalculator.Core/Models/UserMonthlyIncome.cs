using System;

namespace TaxCalculator.Core.Models
{
    public class UserMonthlyIncome
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }
        public string FinancialYear { get; set; }
        public int Month { get; set; }
        public decimal GrossIncome { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal DeductionsAmount { get; set; }
        public decimal SuperContributions { get; set; }
        public string IncomeType { get; set; } = "Salary";
        public string PayPeriod { get; set; } = "Monthly";
        public DateTime RecordedDate { get; set; }
    }
}
