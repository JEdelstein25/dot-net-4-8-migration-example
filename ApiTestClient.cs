using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

class ApiTestClient
{
    private const string BaseUrl = "http://localhost:8080";
    
    static void Main()
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("Australian Tax Calculator - API Test Client");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // Give the server a moment to start if needed
        Thread.Sleep(1000);

        // Test all endpoints
        TestHealthEndpoint();
        TestTaxCalculationEndpoint();
        TestTaxBracketsEndpoint();

        Console.WriteLine();
        Console.WriteLine("API testing complete!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static void TestHealthEndpoint()
    {
        Console.WriteLine("1. Testing Health Endpoint");
        Console.WriteLine("   GET /api/health");
        
        try
        {
            var response = MakeHttpRequest("GET", $"{BaseUrl}/api/health", null);
            Console.WriteLine($"   Status: ✅ {response.StatusCode}");
            Console.WriteLine($"   Response: {response.Body}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Status: ❌ Error - {ex.Message}");
        }
        Console.WriteLine();
    }

    static void TestTaxCalculationEndpoint()
    {
        Console.WriteLine("2. Testing Tax Calculation Endpoint");
        Console.WriteLine("   POST /api/tax/calculate");

        var testCases = new[]
        {
            new { income = 85000, year = "2024-25", description = "Middle income (2024-25)" },
            new { income = 200000, year = "2015-16", description = "High income with Budget Repair Levy" },
            new { income = 18200, year = "2024-25", description = "Tax-free threshold" }
        };

        foreach (var testCase in testCases)
        {
            try
            {
                var requestBody = $"{{\"taxableIncome\":{testCase.income},\"financialYear\":\"{testCase.year}\"}}";
                var response = MakeHttpRequest("POST", $"{BaseUrl}/api/tax/calculate", requestBody);
                
                Console.WriteLine($"   {testCase.description}:");
                Console.WriteLine($"   Status: ✅ {response.StatusCode}");
                Console.WriteLine($"   Income: ${testCase.income:N0} ({testCase.year})");
                
                // Parse key values from response
                var netTax = ExtractJsonValue(response.Body, "netTaxPayable");
                var effectiveRate = ExtractJsonValue(response.Body, "effectiveRate");
                var medicareLevy = ExtractJsonValue(response.Body, "medicareLevy");
                
                if (!string.IsNullOrEmpty(netTax))
                    Console.WriteLine($"   Net Tax: ${decimal.Parse(netTax):N2}");
                if (!string.IsNullOrEmpty(effectiveRate))
                    Console.WriteLine($"   Effective Rate: {(decimal.Parse(effectiveRate) * 100):F2}%");
                if (!string.IsNullOrEmpty(medicareLevy))
                    Console.WriteLine($"   Medicare Levy: ${decimal.Parse(medicareLevy):N2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   {testCase.description}: ❌ Error - {ex.Message}");
            }
            Console.WriteLine();
        }
    }

    static void TestTaxBracketsEndpoint()
    {
        Console.WriteLine("3. Testing Tax Brackets Endpoint");
        Console.WriteLine("   GET /api/tax/brackets/{year}");

        var years = new[] { "2024-25", "2023-24", "2015-16" };

        foreach (var year in years)
        {
            try
            {
                var response = MakeHttpRequest("GET", $"{BaseUrl}/api/tax/brackets/{year}", null);
                Console.WriteLine($"   {year} brackets: ✅ {response.StatusCode}");
                
                // Count brackets in response
                var bracketCount = response.Body.Split(new[] { "minIncome" }, StringSplitOptions.None).Length - 1;
                Console.WriteLine($"   Found {bracketCount} tax brackets");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   {year} brackets: ❌ Error - {ex.Message}");
            }
        }
        Console.WriteLine();
    }

    static HttpResponse MakeHttpRequest(string method, string url, string body)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = method;
        request.ContentType = "application/json";
        request.Timeout = 5000; // 5 second timeout

        if (!string.IsNullOrEmpty(body))
        {
            var data = Encoding.UTF8.GetBytes(body);
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
        }

        try
        {
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return new HttpResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Body = reader.ReadToEnd()
                };
            }
        }
        catch (WebException ex)
        {
            if (ex.Response != null)
            {
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    return new HttpResponse
                    {
                        StatusCode = (int)((HttpWebResponse)ex.Response).StatusCode,
                        Body = reader.ReadToEnd()
                    };
                }
            }
            throw;
        }
    }

    static string ExtractJsonValue(string json, string key)
    {
        var keyPattern = $"\"{key}\":";
        var index = json.IndexOf(keyPattern);
        if (index == -1) return null;

        var valueStart = index + keyPattern.Length;
        while (valueStart < json.Length && (json[valueStart] == ' ' || json[valueStart] == '\t')) valueStart++;

        if (valueStart >= json.Length) return null;

        int valueEnd = valueStart;
        while (valueEnd < json.Length && json[valueEnd] != ',' && json[valueEnd] != '}') valueEnd++;

        return valueEnd > valueStart ? json.Substring(valueStart, valueEnd - valueStart).Trim() : null;
    }

    class HttpResponse
    {
        public int StatusCode { get; set; }
        public string Body { get; set; }
    }
}
