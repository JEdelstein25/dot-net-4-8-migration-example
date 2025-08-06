using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using TaxCalculator.Core.Models;
using TaxCalculator.Services.Interfaces;

namespace TaxCalculator.Api.Controllers
{
    [RoutePrefix("api/tax")]
    public class TaxController : ApiController
    {
        private readonly ITaxCalculationService _taxCalculationService;
        private readonly ILogger _logger;

        public TaxController(ITaxCalculationService taxCalculationService, ILogger logger)
        {
            _taxCalculationService = taxCalculationService;
            _logger = logger;
        }

        [HttpPost]
        [Route("calculate")]
        public async Task<IHttpActionResult> CalculateTax([FromBody] TaxCalculationRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request cannot be null");

                if (request.TaxableIncome < 0)
                    return BadRequest("Taxable income cannot be negative");

                if (string.IsNullOrEmpty(request.FinancialYear))
                    return BadRequest("Financial year is required");

                var result = await _taxCalculationService.CalculateTaxAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Invalid request: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating tax: {ex.Message}", ex);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("brackets/{year}")]
        public async Task<IHttpActionResult> GetTaxBrackets(string year)
        {
            try
            {
                if (string.IsNullOrEmpty(year))
                    return BadRequest("Year is required");

                var brackets = await _taxCalculationService.GetTaxBracketsAsync(year);
                return Ok(brackets);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting tax brackets: {ex.Message}", ex);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("compare")]
        public async Task<IHttpActionResult> CompareTax(decimal income, [FromUri] string[] years)
        {
            try
            {
                if (income < 0)
                    return BadRequest("Income cannot be negative");

                if (years == null || years.Length == 0)
                    return BadRequest("Years are required");

                var result = await _taxCalculationService.CompareTaxAcrossYearsAsync(income, new List<string>(years));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error comparing tax: {ex.Message}", ex);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("history/{income}")]
        public async Task<IHttpActionResult> GetTaxHistory(decimal income, int years = 10)
        {
            try
            {
                if (income < 0)
                    return BadRequest("Income cannot be negative");

                if (years <= 0 || years > 20)
                    return BadRequest("Years must be between 1 and 20");

                var result = await _taxCalculationService.GetTaxHistoryAsync(income, years);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting tax history: {ex.Message}", ex);
                return InternalServerError(ex);
            }
        }
    }
}
