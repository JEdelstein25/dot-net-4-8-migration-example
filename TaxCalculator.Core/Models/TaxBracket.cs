using System;

namespace TaxCalculator.Core.Models
{
    public class TaxBracket
    {
        public int Id { get; set; }
        public string FinancialYear { get; set; }
        public decimal MinIncome { get; set; }
        public decimal? MaxIncome { get; set; }
        public decimal TaxRate { get; set; }
        public decimal FixedAmount { get; set; }
        public int BracketOrder { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
