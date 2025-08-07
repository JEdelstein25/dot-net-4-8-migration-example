using TaxCalculator.Core.Models;

Console.WriteLine("===============================================");
Console.WriteLine("Australian Tax Calculator - Standalone API");
Console.WriteLine("(.NET 8 Implementation)");
Console.WriteLine("===============================================");
Console.WriteLine();

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 8080
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(8080);
});

// Add services
builder.Services.AddSingleton<TaxCalculatorService>();

var app = builder.Build();

// Initialize tax brackets
var taxCalculatorService = app.Services.GetRequiredService<TaxCalculatorService>();

// Configure middleware
app.UseRouting();

// Health endpoint
app.MapGet("/api/health", () => 
{
    return Results.Ok(new { status = "OK", timestamp = DateTime.UtcNow });
});

// Tax calculation endpoint
app.MapPost("/api/tax/calculate", async (TaxCalculationRequest request, TaxCalculatorService service) =>
{
    try
    {
        if (request == null)
            return Results.BadRequest(new { error = "Request cannot be null" });

        if (request.TaxableIncome < 0)
            return Results.BadRequest(new { error = "Taxable income cannot be negative" });

        if (string.IsNullOrEmpty(request.FinancialYear))
            return Results.BadRequest(new { error = "Financial year is required" });

        var result = await service.CalculateTaxAsync(request);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Internal server error: {ex.Message}");
    }
});

// Get tax brackets endpoint
app.MapGet("/api/tax/brackets/{year}", async (string year, TaxCalculatorService service) =>
{
    try
    {
        if (string.IsNullOrEmpty(year))
            return Results.BadRequest(new { error = "Year is required" });

        var brackets = await service.GetTaxBracketsAsync(year);
        return Results.Ok(brackets);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Internal server error: {ex.Message}");
    }
});

Console.WriteLine($"Tax Calculator API Server starting at http://localhost:8080");
Console.WriteLine("Available endpoints:");
Console.WriteLine("  GET  http://localhost:8080/api/health");
Console.WriteLine("  POST http://localhost:8080/api/tax/calculate");
Console.WriteLine("  GET  http://localhost:8080/api/tax/brackets/{year}");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to stop the server");

app.Run();
