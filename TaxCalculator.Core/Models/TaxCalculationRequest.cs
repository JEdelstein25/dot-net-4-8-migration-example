using System.Collections.Generic;

namespace TaxCalculator.Core.Models
{
    public class TaxCalculationRequest
    {
        public string FinancialYear { get; set; }
        public decimal TaxableIncome { get; set; }
        public string ResidencyStatus { get; set; } = "Resident";
        public bool IncludeMedicareLevy { get; set; } = true;
        public bool IncludeOffsets { get; set; } = true;
        public Dictionary<string, decimal> AdditionalIncomeTypes { get; set; } = new Dictionary<string, decimal>();
    }
}
