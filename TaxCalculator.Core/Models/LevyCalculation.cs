namespace TaxCalculator.Core.Models
{
    public class LevyCalculation
    {
        public string LevyType { get; set; }
        public decimal ThresholdIncome { get; set; }
        public decimal LevyRate { get; set; }
        public decimal Amount { get; set; }
        public decimal BudgetRepairLevy { get; set; }
    }
}
