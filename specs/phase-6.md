# Phase 6: Standalone Applications and Final Validation

## Overview
Phase 6 completes the migration by updating standalone applications, console utilities, and test clients while conducting final comprehensive validation of the entire migrated system.

## Scope
- **Primary Targets**: TaxCalculator.Console, TaxCalculator.StandaloneApi, TaxCalculator.TestClient, ApiTestClient
- **Risk Level**: 🟢 LOW RISK
- **Dependencies**: Phases 1-5 completed
- **Estimated Duration**: 1-2 days

## Objectives
1. Migrate all standalone applications to .NET 8
2. Update console applications for cross-platform compatibility
3. Validate complete system functionality
4. Perform final integration testing
5. Prepare deployment artifacts and documentation

## Pre-Migration Checklist
- [ ] Phases 1-5 successfully completed and validated
- [ ] Standalone application functionality documented
- [ ] Console application usage patterns identified
- [ ] Test client requirements understood

## Migration Tasks

### Task 6.1: Console Application Migration

#### 6.1.1: TaxCalculator.Console Migration
**Purpose**: Database setup and seeding utility
**Current State**: .NET Framework 4.8 console application
**Target State**: .NET 8 console application

**New Project Structure**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>TaxCalculator.Console</RootNamespace>
    <AssemblyName>TaxCalculator.Console</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TaxCalculator.Core\TaxCalculator.Core.csproj" />
    <ProjectReference Include="..\TaxCalculator.Data\TaxCalculator.Data.csproj" />
    <ProjectReference Include="..\TaxCalculator.Services\TaxCalculator.Services.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

**Modern Console Application Pattern**:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace TaxCalculator.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            try
            {
                var app = host.Services.GetRequiredService<ConsoleApplication>();
                await app.RunAsync(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register services
                    services.AddScoped<ITaxBracketRepository, TaxBracketRepository>();
                    services.AddScoped<ConsoleApplication>();
                });
    }

    public class ConsoleApplication
    {
        private readonly ITaxBracketRepository _repository;
        private readonly IConfiguration _configuration;

        public ConsoleApplication(ITaxBracketRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        public async Task RunAsync(string[] args)
        {
            Console.WriteLine("Tax Calculator Database Setup Utility");
            Console.WriteLine("=====================================");

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            switch (args[0].ToLower())
            {
                case "seed":
                    await SeedDatabase();
                    break;
                case "verify":
                    await VerifyDatabase();
                    break;
                default:
                    ShowUsage();
                    break;
            }
        }

        private async Task SeedDatabase()
        {
            // Database seeding logic - preserve exact same functionality
            Console.WriteLine("Seeding database with tax bracket data...");
            // Implementation remains identical to original
        }

        private async Task VerifyDatabase()
        {
            // Database verification logic
            Console.WriteLine("Verifying database integrity...");
            // Implementation remains identical to original
        }

        private void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  TaxCalculator.Console seed    - Seed database with tax data");
            Console.WriteLine("  TaxCalculator.Console verify  - Verify database integrity");
        }
    }
}
```

#### 6.1.2: Configuration for Console App
**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=AustralianTaxDB;Integrated Security=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Task 6.2: Standalone API Migration

#### 6.2.1: TaxCalculator.StandaloneApi Migration
**Purpose**: Self-hosted HTTP listener API (alternative to IIS)
**Migration Strategy**: Replace with ASP.NET Core Kestrel self-hosting

**New Project Structure**:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>TaxCalculator.StandaloneApi</RootNamespace>
    <AssemblyName>TaxCalculator.StandaloneApi</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\TaxCalculator.Api\TaxCalculator.Api.csproj" />
  </ItemGroup>
</Project>
```

**Standalone API Implementation**:
```csharp
using TaxCalculator.Api;

namespace TaxCalculator.StandaloneApi
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure services (reuse from main API project)
            builder.Services.AddControllers().AddNewtonsoftJson();
            
            // Add all services from main API
            builder.Services.AddScoped<ITaxCalculationService, TaxCalculationService>();
            builder.Services.AddScoped<ITaxBracketRepository, TaxBracketRepository>();
            // ... other service registrations

            // Configure to listen on specific port
            builder.WebHost.UseUrls("http://localhost:8080");

            var app = builder.Build();

            // Configure pipeline
            app.UseRouting();
            app.MapControllers();

            Console.WriteLine("Tax Calculator Standalone API");
            Console.WriteLine("============================");
            Console.WriteLine("Server starting on http://localhost:8080");
            Console.WriteLine("Press Ctrl+C to stop the server");

            try
            {
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
```

### Task 6.3: Test Client Migration

#### 6.3.1: ApiTestClient Migration
**Purpose**: API testing and validation client
**Current State**: Simple HTTP client for API testing

**New Project Structure**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>ApiTestClient</RootNamespace>
    <AssemblyName>ApiTestClient</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TaxCalculator.Core\TaxCalculator.Core.csproj" />
  </ItemGroup>
</Project>
```

**Enhanced Test Client**:
```csharp
using System.Text;
using Newtonsoft.Json;
using TaxCalculator.Core.Models;

namespace ApiTestClient
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _baseUrl = "http://localhost:8080";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tax Calculator API Test Client");
            Console.WriteLine("==============================");

            if (args.Length > 0)
            {
                _baseUrl = args[0];
            }

            Console.WriteLine($"Testing API at: {_baseUrl}");

            try
            {
                await TestHealthEndpoint();
                await TestTaxCalculationEndpoint();
                await TestTaxBracketsEndpoint();
                await TestErrorScenarios();

                Console.WriteLine("\nAll tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static async Task TestHealthEndpoint()
        {
            Console.WriteLine("\nTesting health endpoint...");
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Response: {content}");
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Health check failed");
            }
        }

        private static async Task TestTaxCalculationEndpoint()
        {
            Console.WriteLine("\nTesting tax calculation endpoint...");
            
            var request = new TaxCalculationRequest
            {
                TaxableIncome = 85000m,
                FinancialYear = "2024-25",
                ResidencyStatus = "Resident",
                IncludeMedicareLevy = true,
                IncludeOffsets = true
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/tax/calculate", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<TaxCalculationResult>(responseContent);
                Console.WriteLine($"Income Tax: ${result.IncomeTax:F2}");
                Console.WriteLine($"Medicare Levy: ${result.MedicareLevy:F2}");
                Console.WriteLine($"Net Tax: ${result.NetTaxPayable:F2}");
                Console.WriteLine($"Effective Rate: {result.EffectiveRate:P2}");
            }
            else
            {
                Console.WriteLine($"Error: {responseContent}");
                throw new Exception("Tax calculation failed");
            }
        }

        private static async Task TestTaxBracketsEndpoint()
        {
            Console.WriteLine("\nTesting tax brackets endpoint...");
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tax/brackets/2024-25");
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var brackets = JsonConvert.DeserializeObject<List<TaxBracket>>(content);
                Console.WriteLine($"Retrieved {brackets.Count} tax brackets");
                
                foreach (var bracket in brackets.Take(3))
                {
                    Console.WriteLine($"  ${bracket.MinIncome} - ${bracket.MaxIncome}: {bracket.TaxRate:P1}");
                }
            }
            else
            {
                Console.WriteLine($"Error: {content}");
                throw new Exception("Tax brackets retrieval failed");
            }
        }

        private static async Task TestErrorScenarios()
        {
            Console.WriteLine("\nTesting error scenarios...");
            
            // Test invalid request
            var invalidRequest = new TaxCalculationRequest
            {
                TaxableIncome = -1000m,
                FinancialYear = "2024-25"
            };

            var json = JsonConvert.SerializeObject(invalidRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/tax/calculate", content);
            
            Console.WriteLine($"Invalid request status: {response.StatusCode}");
            
            if (response.StatusCode != System.Net.HttpStatusCode.BadRequest)
            {
                throw new Exception("Expected BadRequest status for invalid input");
            }
        }
    }
}
```

### Task 6.4: Cross-Platform Validation

#### 6.4.1: Platform Testing
**Test Scenarios**:
1. **Windows**: Existing development environment
2. **Linux**: Docker container testing
3. **macOS**: If available in development environment

**Docker Testing Setup**:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "TaxCalculator.Api/TaxCalculator.Api.csproj"
RUN dotnet build "TaxCalculator.Api/TaxCalculator.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaxCalculator.Api/TaxCalculator.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaxCalculator.Api.dll"]
```

#### 6.4.2: Cross-Platform Testing Script
```bash
#!/bin/bash
# test-cross-platform.sh

echo "Testing Tax Calculator on multiple platforms"
echo "==========================================="

# Test on current platform
echo "Testing on current platform..."
dotnet run --project TaxCalculator.StandaloneApi &
API_PID=$!
sleep 5

dotnet run --project ApiTestClient
RESULT=$?

kill $API_PID

if [ $RESULT -eq 0 ]; then
    echo "✅ Current platform test passed"
else
    echo "❌ Current platform test failed"
    exit 1
fi

# Test with Docker (Linux container)
echo "Testing with Docker (Linux container)..."
docker build -t tax-calculator-api .
docker run -d -p 8080:8080 --name tax-calc-test tax-calculator-api
sleep 10

dotnet run --project ApiTestClient -- http://localhost:8080
DOCKER_RESULT=$?

docker stop tax-calc-test
docker rm tax-calc-test

if [ $DOCKER_RESULT -eq 0 ]; then
    echo "✅ Docker platform test passed"
else
    echo "❌ Docker platform test failed"
    exit 1
fi

echo "✅ All cross-platform tests passed!"
```

### Task 6.5: Final System Validation

#### 6.5.1: End-to-End Testing
**Complete System Test**:
```csharp
[TestFixture]
public class EndToEndSystemTests
{
    [Test]
    public async Task CompleteWorkflow_DatabaseToApi_WorksCorrectly()
    {
        // 1. Seed database using console app
        await RunConsoleApp("seed");
        
        // 2. Start standalone API
        var apiProcess = StartStandaloneApi();
        await Task.Delay(5000); // Wait for startup
        
        try
        {
            // 3. Test API with test client
            var result = await RunApiTestClient();
            Assert.That(result, Is.True, "API test client should succeed");
            
            // 4. Verify database state
            await VerifyDatabaseState();
        }
        finally
        {
            // 5. Cleanup
            apiProcess.Kill();
        }
    }
}
```

#### 6.5.2: Performance Validation
**System Performance Test**:
```csharp
[Test]
public async Task SystemPerformance_MeetsRequirements()
{
    // Start API server
    var server = StartTestServer();
    
    try
    {
        // Test concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(server.CreateClient().PostAsync("/api/tax/calculate", CreateValidRequest()));
        }
        
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Validate performance
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(10000), "100 requests should complete within 10 seconds");
        Assert.That(responses.All(r => r.IsSuccessStatusCode), Is.True, "All requests should succeed");
    }
    finally
    {
        server.Dispose();
    }
}
```

## Success Criteria

### Technical Validation
- [ ] All standalone applications build and run on .NET 8
- [ ] Console applications work on Windows and Linux
- [ ] Standalone API functions correctly
- [ ] Test clients validate API functionality
- [ ] Cross-platform compatibility verified

### Business Validation
- [ ] Database setup and seeding works correctly
- [ ] API testing and validation functions properly
- [ ] All applications produce identical results to .NET Framework versions
- [ ] Performance meets or exceeds requirements

### System Validation
- [ ] Complete end-to-end workflow functions
- [ ] Cross-platform deployment successful
- [ ] Docker containerization works
- [ ] All quality gates passed

## Risk Mitigation

### Low Risk Areas
- Console applications (minimal complexity)
- Test clients (straightforward HTTP clients)
- Cross-platform compatibility (well-supported by .NET 8)

### Potential Issues
1. **Platform-specific Dependencies**: Unlikely but check for any Windows-specific code
2. **Configuration Differences**: Console app configuration migration
3. **Executable Packaging**: Different executable formats across platforms

## Timeline

### Day 1: Application Migration
- Morning: Console application migration and testing
- Afternoon: Standalone API and test client migration

### Day 2: Validation and Cross-Platform Testing
- Morning: Cross-platform testing and Docker validation
- Afternoon: Final system validation and documentation

## Quality Gates

### Entry Criteria
- [ ] Phases 1-5 completed successfully
- [ ] All core functionality validated
- [ ] API layer fully functional

### Exit Criteria
- [ ] All applications migrated and functional
- [ ] Cross-platform compatibility verified
- [ ] End-to-end system testing completed
- [ ] Performance requirements met
- [ ] Migration project completed

### Final Approval
- [ ] Complete system functionality validation
- [ ] Cross-platform deployment verification
- [ ] Performance and reliability confirmation
- [ ] Project sign-off from stakeholders

## Migration Completion

### Deliverables
1. **Migrated Applications**: All projects running on .NET 8
2. **Cross-Platform Support**: Verified Windows and Linux compatibility
3. **Docker Support**: Containerized deployment capability
4. **Test Suite**: Comprehensive testing infrastructure
5. **Documentation**: Complete migration documentation

### Post-Migration Tasks
1. **Performance Monitoring**: Establish ongoing performance monitoring
2. **Maintenance Documentation**: Create maintenance and troubleshooting guides
3. **Deployment Guides**: Document deployment procedures for different environments
4. **Team Training**: Knowledge transfer to development and operations teams

This final phase completes the migration project and validates that the entire system functions correctly in the .NET 8 environment while maintaining full compatibility with existing functionality and adding cross-platform deployment capabilities.
