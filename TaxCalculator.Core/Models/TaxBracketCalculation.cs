namespace TaxCalculator.Core.Models
{
    public class TaxBracketCalculation
    {
        public decimal MinIncome { get; set; }
        public decimal? MaxIncome { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxableInBracket { get; set; }
        public decimal TaxPayable { get; set; }
    }
}
