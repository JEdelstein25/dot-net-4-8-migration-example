using System;

namespace TaxCalculator.Core.Models
{
    public class TaxOffset
    {
        public int Id { get; set; }
        public string FinancialYear { get; set; }
        public string OffsetType { get; set; }
        public decimal? MaxIncome { get; set; }
        public decimal MaxOffset { get; set; }
        public decimal? PhaseOutStart { get; set; }
        public decimal? PhaseOutRate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
