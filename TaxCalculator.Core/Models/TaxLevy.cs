using System;

namespace TaxCalculator.Core.Models
{
    public class TaxLevy
    {
        public int Id { get; set; }
        public string FinancialYear { get; set; }
        public string LevyType { get; set; }
        public decimal ThresholdIncome { get; set; }
        public decimal LevyRate { get; set; }
        public decimal? MaxIncome { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
