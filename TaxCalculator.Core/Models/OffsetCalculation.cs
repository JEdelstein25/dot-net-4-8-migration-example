using System.Collections.Generic;

namespace TaxCalculator.Core.Models
{
    public class OffsetCalculation
    {
        public string OffsetType { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalOffsets { get; set; }
        public List<OffsetCalculation> OffsetBreakdown { get; set; } = new List<OffsetCalculation>();
    }
}
