using System;

namespace TaxCalculator.TestClient
{
    public class TaxBracket
    {
        public decimal MinIncome { get; set; }
        public decimal? MaxIncome { get; set; }
        public decimal TaxRate { get; set; }
        public decimal FixedAmount { get; set; }
        public int BracketOrder { get; set; }
    }

    public class TaxCalculationRequest
    {
        public string FinancialYear { get; set; }
        public decimal TaxableIncome { get; set; }
        public string ResidencyStatus { get; set; } = "Resident";
        public bool IncludeMedicareLevy { get; set; } = true;
        public bool IncludeOffsets { get; set; } = true;
    }

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
        public string CalculationBreakdown { get; set; }
    }

    public class ApiTestResult
    {
        public string EndpointName { get; set; }
        public bool Success { get; set; }
        public string Response { get; set; }
        public string Error { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
