using System.Collections.Generic;

namespace TaxCalculator.Core.Models
{
    public class TaxCalculationResult
    {
        public decimal TaxableIncome { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal MedicareLevy { get; set; }
        public decimal BudgetRepairLevy { get; set; }
        public decimal TotalLevies { get; set; }
        public decimal TaxOffsets { get; set; }
        public decimal NetTaxPayable { get; set; }
        public decimal NetIncome { get; set; }
        public decimal EffectiveRate { get; set; }
        public decimal MarginalRate { get; set; }
        public List<TaxBracketCalculation> BracketBreakdown { get; set; } = new List<TaxBracketCalculation>();
        public List<LevyCalculation> LevyBreakdown { get; set; } = new List<LevyCalculation>();
        public List<OffsetCalculation> OffsetBreakdown { get; set; } = new List<OffsetCalculation>();
    }
}
