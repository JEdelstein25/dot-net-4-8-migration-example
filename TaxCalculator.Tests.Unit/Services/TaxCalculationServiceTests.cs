using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using TaxCalculator.Core.Models;
using TaxCalculator.Data.Interfaces;
using TaxCalculator.Services.Interfaces;
using TaxCalculator.Services.Services;

namespace TaxCalculator.Tests.Unit.Services
{
    [TestFixture]
    public class TaxCalculationServiceTests
    {
        private ITaxCalculationService _taxCalculationService;
        private Mock<ITaxBracketRepository> _mockTaxBracketRepository;
        private Mock<ICacheService> _mockCacheService;
        private Mock<ILogger> _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockTaxBracketRepository = new Mock<ITaxBracketRepository>();
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger>();

            _taxCalculationService = new TaxCalculationService(
                _mockTaxBracketRepository.Object,
                _mockCacheService.Object,
                _mockLogger.Object);
        }

        [Test]
        public async Task CalculateTaxAsync_2024_25_Income85000_ReturnsCorrectTax()
        {
            // Arrange
            var request = new TaxCalculationRequest
            {
                FinancialYear = "2024-25",
                TaxableIncome = 85000m,
                ResidencyStatus = "Resident",
                IncludeMedicareLevy = true,
                IncludeOffsets = true
            };

            var taxBrackets = Create2024_25TaxBrackets();
            var medicareLevy = new List<TaxLevy>
            {
                new TaxLevy { LevyType = "Medicare", ThresholdIncome = 0, LevyRate = 0.02m, IsActive = true }
            };
            var offsets = new List<TaxOffset>
            {
                new TaxOffset { OffsetType = "LITO", MaxOffset = 700m, PhaseOutStart = 37500m, PhaseOutRate = 0.05m, IsActive = true }
            };

            _mockCacheService.Setup(x => x.GetAsync<List<TaxBracket>>(It.IsAny<string>()))
                            .ReturnsAsync((List<TaxBracket>)null);

            _mockTaxBracketRepository.Setup(x => x.GetTaxBracketsAsync("2024-25"))
                                    .ReturnsAsync(taxBrackets);
            _mockTaxBracketRepository.Setup(x => x.GetTaxLeviesAsync("2024-25"))
                                    .ReturnsAsync(medicareLevy);
            _mockTaxBracketRepository.Setup(x => x.GetTaxOffsetsAsync("2024-25"))
                                    .ReturnsAsync(offsets);

            // Act
            var result = await _taxCalculationService.CalculateTaxAsync(request);

            // Assert
            Assert.That(result.TaxableIncome, Is.EqualTo(85000m));
            Assert.That(result.IncomeTax, Is.GreaterThan(0));
            Assert.That(result.MedicareLevy, Is.EqualTo(1700m).Within(1m)); // 2% of $85,000
            Assert.That(result.NetTaxPayable, Is.GreaterThan(0));
            Assert.That(result.EffectiveRate, Is.InRange(0.20m, 0.25m));
        }

        [Test]
        public async Task CalculateTaxAsync_NegativeIncome_ThrowsArgumentException()
        {
            // Arrange
            var request = new TaxCalculationRequest
            {
                FinancialYear = "2024-25",
                TaxableIncome = -1000m
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _taxCalculationService.CalculateTaxAsync(request));

            Assert.That(ex.Message, Contains.Substring("cannot be negative"));
        }

        [TestCase(18200, 0)] // Tax-free threshold
        [TestCase(45000, 4288)] // End of second bracket
        [TestCase(85000, 19238)] // Test case income
        [TestCase(135000, 31288)] // End of third bracket
        [TestCase(190000, 51638)] // End of fourth bracket
        public async Task CalculateTaxAsync_2024_25_VariousIncomes_ReturnsExpectedTax(decimal income, decimal expectedTax)
        {
            // Arrange
            var request = new TaxCalculationRequest
            {
                FinancialYear = "2024-25",
                TaxableIncome = income,
                ResidencyStatus = "Resident",
                IncludeMedicareLevy = false, // Test income tax only
                IncludeOffsets = false
            };

            var taxBrackets = Create2024_25TaxBrackets();
            SetupMocksForTaxBrackets("2024-25", taxBrackets);

            // Act
            var result = await _taxCalculationService.CalculateTaxAsync(request);

            // Assert
            Assert.That(result.IncomeTax, Is.EqualTo(expectedTax).Within(50m));
        }

        [Test]
        public async Task CalculateTaxAsync_WithMedicareLevy_CalculatesCorrectly()
        {
            // Arrange
            var request = new TaxCalculationRequest
            {
                FinancialYear = "2024-25",
                TaxableIncome = 100000m,
                IncludeMedicareLevy = true
            };

            var taxBrackets = Create2024_25TaxBrackets();
            var medicareLevy = new List<TaxLevy>
            {
                new TaxLevy { LevyType = "Medicare", ThresholdIncome = 0, LevyRate = 0.02m, IsActive = true }
            };

            SetupMocksForTaxBrackets("2024-25", taxBrackets);
            _mockTaxBracketRepository.Setup(x => x.GetTaxLeviesAsync("2024-25"))
                                    .ReturnsAsync(medicareLevy);
            _mockTaxBracketRepository.Setup(x => x.GetTaxOffsetsAsync("2024-25"))
                                    .ReturnsAsync(new List<TaxOffset>());

            // Act
            var result = await _taxCalculationService.CalculateTaxAsync(request);

            // Assert
            Assert.That(result.MedicareLevy, Is.EqualTo(2000m)); // 2% of $100,000
            Assert.That(result.TotalLevies, Is.EqualTo(2000m));
        }

        [Test]
        public async Task CalculateTaxAsync_WithLITO_AppliesOffsetCorrectly()
        {
            // Arrange
            var request = new TaxCalculationRequest
            {
                FinancialYear = "2024-25",
                TaxableIncome = 30000m,
                IncludeOffsets = true
            };

            var taxBrackets = Create2024_25TaxBrackets();
            var offsets = new List<TaxOffset>
            {
                new TaxOffset 
                { 
                    OffsetType = "LITO", 
                    MaxOffset = 700m, 
                    PhaseOutStart = 37500m, 
                    PhaseOutRate = 0.05m, 
                    IsActive = true 
                }
            };

            SetupMocksForTaxBrackets("2024-25", taxBrackets);
            _mockTaxBracketRepository.Setup(x => x.GetTaxLeviesAsync("2024-25"))
                                    .ReturnsAsync(new List<TaxLevy>());
            _mockTaxBracketRepository.Setup(x => x.GetTaxOffsetsAsync("2024-25"))
                                    .ReturnsAsync(offsets);

            // Act
            var result = await _taxCalculationService.CalculateTaxAsync(request);

            // Assert
            Assert.That(result.TaxOffsets, Is.EqualTo(700m)); // Full LITO for income under phase-out threshold
        }

        [Test]
        public async Task GetTaxBracketsAsync_ValidYear_ReturnsBrackets()
        {
            // Arrange
            var year = "2024-25";
            var expectedBrackets = Create2024_25TaxBrackets();

            _mockTaxBracketRepository.Setup(x => x.GetTaxBracketsAsync(year))
                                    .ReturnsAsync(expectedBrackets);

            // Act
            var result = await _taxCalculationService.GetTaxBracketsAsync(year);

            // Assert
            Assert.That(result.Count, Is.EqualTo(5)); // 2024-25 has 5 tax brackets
            Assert.That(result.First().MinIncome, Is.EqualTo(0));
            Assert.That(result.Last().MaxIncome, Is.Null); // Highest bracket has no upper limit
        }

        private List<TaxBracket> Create2024_25TaxBrackets()
        {
            return new List<TaxBracket>
            {
                new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0m, FixedAmount = 0m, BracketOrder = 1, IsActive = true },
                new TaxBracket { MinIncome = 18201, MaxIncome = 45000, TaxRate = 0.16m, FixedAmount = 0m, BracketOrder = 2, IsActive = true },
                new TaxBracket { MinIncome = 45001, MaxIncome = 135000, TaxRate = 0.30m, FixedAmount = 4288m, BracketOrder = 3, IsActive = true },
                new TaxBracket { MinIncome = 135001, MaxIncome = 190000, TaxRate = 0.37m, FixedAmount = 31288m, BracketOrder = 4, IsActive = true },
                new TaxBracket { MinIncome = 190001, MaxIncome = null, TaxRate = 0.45m, FixedAmount = 51638m, BracketOrder = 5, IsActive = true }
            };
        }

        private void SetupMocksForTaxBrackets(string year, List<TaxBracket> brackets)
        {
            _mockCacheService.Setup(x => x.GetAsync<List<TaxBracket>>(It.IsAny<string>()))
                            .ReturnsAsync((List<TaxBracket>)null);

            _mockTaxBracketRepository.Setup(x => x.GetTaxBracketsAsync(year))
                                    .ReturnsAsync(brackets);
        }
    }
}
