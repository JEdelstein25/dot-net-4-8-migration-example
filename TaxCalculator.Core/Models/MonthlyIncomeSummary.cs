namespace TaxCalculator.Core.Models
{
    public class MonthlyIncomeSummary
    {
        public int Month { get; set; }
        public decimal GrossIncome { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal Deductions { get; set; }
        public string IncomeType { get; set; }
    }
}
