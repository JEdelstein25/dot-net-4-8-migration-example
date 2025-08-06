using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using TaxCalculator.Core.Models;

namespace TaxCalculator.StandaloneApi
{
    public class TaxApiServer
    {
        private readonly HttpListener _listener;
        private readonly Dictionary<string, List<TaxBracket>> _taxBrackets;
        private readonly string _baseUrl;

        public TaxApiServer(string baseUrl = "http://localhost:8080/")
        {
            _baseUrl = baseUrl;
            _listener = new HttpListener();
            _listener.Prefixes.Add(baseUrl);
            _taxBrackets = InitializeTaxBrackets();
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"Tax Calculator API Server started at {_baseUrl}");
            Console.WriteLine("Available endpoints:");
            Console.WriteLine($"  GET  {_baseUrl}api/health");
            Console.WriteLine($"  POST {_baseUrl}api/tax/calculate");
            Console.WriteLine($"  GET  {_baseUrl}api/tax/brackets/{{year}}");
            Console.WriteLine();

            while (_listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ProcessRequest(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing request: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _listener?.Stop();
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {request.HttpMethod} {request.Url.AbsolutePath}");

            try
            {
                string responseText = "";
                int statusCode = 200;

                var path = request.Url.AbsolutePath.ToLower();
                var method = request.HttpMethod.ToUpper();

                if (path == "/api/health" && method == "GET")
                {
                    responseText = "{\"status\":\"OK\",\"timestamp\":\"" + DateTime.UtcNow.ToString("o") + "\"}";
                }
                else if (path == "/api/tax/calculate" && method == "POST")
                {
                    responseText = HandleTaxCalculation(request);
                }
                else if (path.StartsWith("/api/tax/brackets/") && method == "GET")
                {
                    var year = path.Substring("/api/tax/brackets/".Length);
                    responseText = HandleGetTaxBrackets(year);
                }
                else
                {
                    statusCode = 404;
                    responseText = "{\"error\":\"Endpoint not found\"}";
                }

                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                response.ContentEncoding = Encoding.UTF8;

                var buffer = Encoding.UTF8.GetBytes(responseText);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
                response.StatusCode = 500;
                var errorResponse = "{\"error\":\"Internal server error\"}";
                var errorBuffer = Encoding.UTF8.GetBytes(errorResponse);
                response.ContentLength64 = errorBuffer.Length;
                response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        private string HandleTaxCalculation(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                var body = reader.ReadToEnd();
                Console.WriteLine($"Request body: {body}");

                // Simple JSON parsing for demo (in production, use proper JSON library)
                var income = ExtractJsonValue(body, "taxableIncome");
                var year = ExtractJsonValue(body, "financialYear");

                if (string.IsNullOrEmpty(income) || string.IsNullOrEmpty(year))
                {
                    return "{\"error\":\"Missing required fields: taxableIncome, financialYear\"}";
                }

                if (!decimal.TryParse(income, out var taxableIncome))
                {
                    return "{\"error\":\"Invalid taxable income\"}";
                }

                var result = CalculateTax(taxableIncome, year);
                return SerializeTaxResult(result);
            }
        }

        private string HandleGetTaxBrackets(string year)
        {
            if (_taxBrackets.ContainsKey(year))
            {
                var brackets = _taxBrackets[year];
                var json = "[";
                for (int i = 0; i < brackets.Count; i++)
                {
                    if (i > 0) json += ",";
                    var bracket = brackets[i];
                    json += $"{{\"minIncome\":{bracket.MinIncome},\"maxIncome\":{(bracket.MaxIncome?.ToString() ?? "null")},\"taxRate\":{bracket.TaxRate},\"fixedAmount\":{bracket.FixedAmount}}}";
                }
                json += "]";
                return json;
            }
            return "{\"error\":\"Tax brackets not found for year: " + year + "\"}";
        }

        private TaxCalculationResult CalculateTax(decimal income, string year)
        {
            var brackets = _taxBrackets.ContainsKey(year) ? _taxBrackets[year] : _taxBrackets["2024-25"];
            
            decimal totalTax = 0;
            foreach (var bracket in brackets)
            {
                if (income <= bracket.MinIncome) continue;

                var bracketMax = bracket.MaxIncome ?? decimal.MaxValue;
                if (income > bracketMax)
                {
                    var taxableInBracket = bracketMax - bracket.MinIncome + 1;
                    totalTax = bracket.FixedAmount + (taxableInBracket * bracket.TaxRate);
                }
                else
                {
                    var taxableInBracket = income - bracket.MinIncome + 1;
                    totalTax = bracket.FixedAmount + (taxableInBracket * bracket.TaxRate);
                    break;
                }
            }

            var medicareLevy = income * 0.02m;
            var budgetRepairLevy = (year == "2015-16" || year == "2016-17" || year == "2017-18") && income > 180000 ? income * 0.02m : 0;
            var lito = income <= 37500 ? 700m : (income <= 45000 ? Math.Max(0, 700m - ((income - 37500) * 0.05m)) : 0);

            var grossTax = totalTax + medicareLevy + budgetRepairLevy;
            var netTax = Math.Max(0, grossTax - lito);

            return new TaxCalculationResult
            {
                TaxableIncome = income,
                IncomeTax = totalTax,
                MedicareLevy = medicareLevy,
                BudgetRepairLevy = budgetRepairLevy,
                TotalLevies = medicareLevy + budgetRepairLevy,
                TaxOffsets = lito,
                NetTaxPayable = netTax,
                NetIncome = income - netTax,
                EffectiveRate = income > 0 ? netTax / income : 0,
                MarginalRate = GetMarginalRate(income, brackets)
            };
        }

        private decimal GetMarginalRate(decimal income, List<TaxBracket> brackets)
        {
            foreach (var bracket in brackets)
            {
                if (income >= bracket.MinIncome && (bracket.MaxIncome == null || income <= bracket.MaxIncome))
                    return bracket.TaxRate;
            }
            return 0;
        }

        private string SerializeTaxResult(TaxCalculationResult result)
        {
            return $"{{" +
                   $"\"taxableIncome\":{result.TaxableIncome}," +
                   $"\"incomeTax\":{result.IncomeTax:F2}," +
                   $"\"medicareLevy\":{result.MedicareLevy:F2}," +
                   $"\"budgetRepairLevy\":{result.BudgetRepairLevy:F2}," +
                   $"\"totalLevies\":{result.TotalLevies:F2}," +
                   $"\"taxOffsets\":{result.TaxOffsets:F2}," +
                   $"\"netTaxPayable\":{result.NetTaxPayable:F2}," +
                   $"\"netIncome\":{result.NetIncome:F2}," +
                   $"\"effectiveRate\":{result.EffectiveRate:F4}," +
                   $"\"marginalRate\":{result.MarginalRate:F4}" +
                   $"}}";
        }

        private string ExtractJsonValue(string json, string key)
        {
            var keyPattern = $"\"{key}\"";
            var index = json.IndexOf(keyPattern);
            if (index == -1) return null;

            var colonIndex = json.IndexOf(":", index);
            if (colonIndex == -1) return null;

            var valueStart = colonIndex + 1;
            while (valueStart < json.Length && (json[valueStart] == ' ' || json[valueStart] == '\t')) valueStart++;

            if (valueStart >= json.Length) return null;

            int valueEnd;
            if (json[valueStart] == '"')
            {
                valueStart++; // Skip opening quote
                valueEnd = json.IndexOf('"', valueStart);
            }
            else
            {
                valueEnd = valueStart;
                while (valueEnd < json.Length && json[valueEnd] != ',' && json[valueEnd] != '}') valueEnd++;
            }

            return valueEnd > valueStart ? json.Substring(valueStart, valueEnd - valueStart).Trim() : null;
        }

        private Dictionary<string, List<TaxBracket>> InitializeTaxBrackets()
        {
            return new Dictionary<string, List<TaxBracket>>
            {
                ["2024-25"] = new List<TaxBracket>
                {
                    new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0.00m, FixedAmount = 0, BracketOrder = 1 },
                    new TaxBracket { MinIncome = 18201, MaxIncome = 45000, TaxRate = 0.16m, FixedAmount = 0, BracketOrder = 2 },
                    new TaxBracket { MinIncome = 45001, MaxIncome = 135000, TaxRate = 0.30m, FixedAmount = 4288, BracketOrder = 3 },
                    new TaxBracket { MinIncome = 135001, MaxIncome = 190000, TaxRate = 0.37m, FixedAmount = 31288, BracketOrder = 4 },
                    new TaxBracket { MinIncome = 190001, MaxIncome = null, TaxRate = 0.45m, FixedAmount = 51638, BracketOrder = 5 }
                },
                ["2023-24"] = new List<TaxBracket>
                {
                    new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0.00m, FixedAmount = 0, BracketOrder = 1 },
                    new TaxBracket { MinIncome = 18201, MaxIncome = 45000, TaxRate = 0.19m, FixedAmount = 0, BracketOrder = 2 },
                    new TaxBracket { MinIncome = 45001, MaxIncome = 120000, TaxRate = 0.325m, FixedAmount = 5092, BracketOrder = 3 },
                    new TaxBracket { MinIncome = 120001, MaxIncome = 180000, TaxRate = 0.37m, FixedAmount = 29467, BracketOrder = 4 },
                    new TaxBracket { MinIncome = 180001, MaxIncome = null, TaxRate = 0.45m, FixedAmount = 51667, BracketOrder = 5 }
                },
                ["2015-16"] = new List<TaxBracket>
                {
                    new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0.00m, FixedAmount = 0, BracketOrder = 1 },
                    new TaxBracket { MinIncome = 18201, MaxIncome = 37000, TaxRate = 0.19m, FixedAmount = 0, BracketOrder = 2 },
                    new TaxBracket { MinIncome = 37001, MaxIncome = 80000, TaxRate = 0.325m, FixedAmount = 3572, BracketOrder = 3 },
                    new TaxBracket { MinIncome = 80001, MaxIncome = 180000, TaxRate = 0.37m, FixedAmount = 17547, BracketOrder = 4 },
                    new TaxBracket { MinIncome = 180001, MaxIncome = null, TaxRate = 0.45m, FixedAmount = 54547, BracketOrder = 5 }
                }
            };
        }
    }
}
