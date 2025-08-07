# Phase 5: Testing Infrastructure Migration

## Overview
Phase 5 migrates all testing projects to .NET 8 while maintaining identical test scenarios and improving the testing infrastructure for the migrated application.

## Scope
- **Primary Target**: TaxCalculator.Tests.Unit project and testing infrastructure
- **Risk Level**: 🟡 MEDIUM RISK
- **Dependencies**: Phases 1-4 completed
- **Estimated Duration**: 2-3 days

## Objectives
1. Migrate test projects to .NET 8
2. Update testing frameworks to latest compatible versions
3. Maintain all existing test scenarios and assertions
4. Improve test coverage and add migration-specific tests
5. Ensure test suite validates API contract compliance

## Pre-Migration Checklist
- [ ] Phases 1-4 successfully completed
- [ ] Current test coverage documented and measured
- [ ] All existing tests cataloged and understood
- [ ] Testing frameworks compatibility verified

## Migration Tasks

### Task 5.1: Test Project Migration

#### 5.1.1: Project File Conversion
**Current State**: Legacy test project with packages.config
**Target State**: .NET 8 SDK-style test project

**New Project Structure**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>TaxCalculator.Tests.Unit</RootNamespace>
    <AssemblyName>TaxCalculator.Tests.Unit</AssemblyName>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="4.0.1" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageReference Include="NUnit.Analyzers" Version="3.9.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TaxCalculator.Core\TaxCalculator.Core.csproj" />
    <ProjectReference Include="..\TaxCalculator.Data\TaxCalculator.Data.csproj" />
    <ProjectReference Include="..\TaxCalculator.Services\TaxCalculator.Services.csproj" />
    <ProjectReference Include="..\TaxCalculator.Api\TaxCalculator.Api.csproj" />
  </ItemGroup>
</Project>
```

#### 5.1.2: Framework Updates
**Testing Framework Migration Matrix**:

| Component | Current Version | Target Version | Risk Level |
|-----------|----------------|----------------|------------|
| NUnit | 3.13.3 | 4.0.1 | LOW |
| NUnit3TestAdapter | 4.5.0 | 4.5.0 | NONE |
| Moq | 4.16.1 | 4.20.69 | LOW |
| Castle.Core | 4.4.0 | 5.1.1 | LOW |

**Migration Strategy**:
- Update frameworks incrementally
- Maintain all existing test scenarios
- Preserve test assertions and logic
- Add new testing capabilities

### Task 5.2: Existing Test Migration

#### 5.2.1: Tax Calculation Service Tests
**Current Tests to Preserve**:
```csharp
[TestFixture]
public class TaxCalculationServiceTests
{
    // All existing tests MUST be preserved exactly
    [Test]
    public async Task CalculateTaxAsync_2024_25_Income85000_ReturnsCorrectTax()
    [Test]
    public async Task CalculateTaxAsync_NegativeIncome_ThrowsArgumentException()
    [TestCase(18200, 0)] // All test cases must remain identical
    [TestCase(45000, 4288)]
    public async Task CalculateTaxAsync_2024_25_VariousIncomes_ReturnsExpectedTax(decimal income, decimal expectedTax)
    // ... all other existing tests
}
```

**Migration Requirements**:
- [ ] All test methods migrate without changes
- [ ] All test assertions remain identical
- [ ] All test data and expected results preserved
- [ ] Test execution behavior unchanged

#### 5.2.2: Framework Syntax Updates
**NUnit 3 to NUnit 4 Changes** (minimal):
```csharp
// Most syntax remains the same
[Test]
public async Task ExistingTest_StillWorks()
{
    // Assertions remain identical
    Assert.That(result.IncomeTax, Is.EqualTo(expectedTax).Within(0.01m));
}

// SetUp/TearDown remain the same
[SetUp]
public void Setup()
{
    // Existing setup code unchanged
}
```

**Moq Updates** (minimal changes expected):
```csharp
// Mock setup syntax remains largely the same
_mockTaxBracketRepository.Setup(x => x.GetTaxBracketsAsync("2024-25"))
                        .ReturnsAsync(taxBrackets);
```

### Task 5.3: API Integration Testing Enhancement

#### 5.3.1: ASP.NET Core Integration Tests
**New Test Category**: API Contract Validation
```csharp
[TestFixture]
public class ApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Configure test services
                    services.AddScoped<ITaxBracketRepository, MockTaxBracketRepository>();
                });
            });
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task GET_Health_ReturnsOkWithExpectedFormat()
    {
        // Test API endpoint returns expected format
        var response = await _client.GetAsync("/api/health");
        
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonConvert.DeserializeObject<dynamic>(content);
        
        Assert.That(healthResponse.status.ToString(), Is.EqualTo("OK"));
        Assert.That(healthResponse.timestamp, Is.Not.Null);
    }

    [Test]
    public async Task POST_TaxCalculate_WithValidRequest_ReturnsCorrectCalculation()
    {
        // Validate complete API workflow
        var request = new TaxCalculationRequest
        {
            TaxableIncome = 85000m,
            FinancialYear = "2024-25",
            ResidencyStatus = "Resident",
            IncludeMedicareLevy = true,
            IncludeOffsets = true
        };

        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/tax/calculate", content);
        
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<TaxCalculationResult>(responseContent);
        
        // Validate calculation accuracy
        Assert.That(result.TaxableIncome, Is.EqualTo(85000m));
        Assert.That(result.IncomeTax, Is.GreaterThan(0));
        Assert.That(result.MedicareLevy, Is.GreaterThan(0));
        Assert.That(result.NetTaxPayable, Is.GreaterThan(0));
        Assert.That(result.EffectiveRate, Is.InRange(0.15m, 0.30m));
    }
}
```

#### 5.3.2: Contract Compliance Tests
**Critical API Contract Validation**:
```csharp
[TestFixture]
public class ApiContractComplianceTests
{
    [TestCaseSource(nameof(AllApiEndpoints))]
    public async Task ApiEndpoint_ResponseFormat_MatchesContract(ApiTestCase testCase)
    {
        // Comprehensive API contract validation
        var response = await _client.SendAsync(testCase.CreateRequest());
        
        // Validate HTTP status codes
        Assert.That(response.StatusCode, Is.EqualTo(testCase.ExpectedStatusCode));
        
        // Validate response schema
        var responseBody = await response.Content.ReadAsStringAsync();
        ValidateJsonSchema(responseBody, testCase.ExpectedSchema);
        
        // Validate response headers
        ValidateResponseHeaders(response.Headers, testCase.ExpectedHeaders);
    }

    private static IEnumerable<ApiTestCase> AllApiEndpoints()
    {
        // Test all API endpoints with various scenarios
        yield return new ApiTestCase
        {
            Name = "Health Check",
            Method = HttpMethod.Get,
            Url = "/api/health",
            ExpectedStatusCode = HttpStatusCode.OK
        };
        
        yield return new ApiTestCase
        {
            Name = "Tax Calculation - Valid Request",
            Method = HttpMethod.Post,
            Url = "/api/tax/calculate",
            RequestBody = new TaxCalculationRequest { /* valid data */ },
            ExpectedStatusCode = HttpStatusCode.OK
        };
        
        yield return new ApiTestCase
        {
            Name = "Tax Calculation - Invalid Request",
            Method = HttpMethod.Post,
            Url = "/api/tax/calculate",
            RequestBody = new TaxCalculationRequest { TaxableIncome = -1 },
            ExpectedStatusCode = HttpStatusCode.BadRequest
        };
        
        // ... all other endpoint scenarios
    }
}
```

### Task 5.4: Performance Testing

#### 5.4.1: Benchmark Tests
**Performance Regression Testing**:
```csharp
[TestFixture]
public class PerformanceTests
{
    [Test]
    public async Task TaxCalculation_Performance_WithinBaseline()
    {
        // Measure performance against .NET Framework baseline
        var request = new TaxCalculationRequest
        {
            TaxableIncome = 85000m,
            FinancialYear = "2024-25"
        };

        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        
        // Run 100 concurrent calculations
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_taxCalculationService.CalculateTaxAsync(request));
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Performance should be within 10% of baseline
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(BaselinePerformance * 1.1));
    }

    [Test]
    public async Task ApiEndpoint_ResponseTime_WithinThreshold()
    {
        // Test API endpoint response times
        var request = CreateHttpRequest();
        
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.SendAsync(request);
        stopwatch.Stop();
        
        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(MaxAcceptableResponseTime));
    }
}
```

#### 5.4.2: Load Testing Preparation
**Setup for Load Testing**:
```csharp
[TestFixture]
public class LoadTestPreparation
{
    [Test]
    public async Task ConcurrentRequests_HandleCorrectly()
    {
        // Test concurrent request handling
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(_client.PostAsync("/api/tax/calculate", CreateValidRequest()));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // All requests should succeed
        Assert.That(responses.All(r => r.IsSuccessStatusCode), Is.True);
    }
}
```

### Task 5.5: Test Coverage Enhancement

#### 5.5.1: Coverage Measurement
**Setup Coverage Tools**:
```xml
<!-- Add to test project -->
<ItemGroup>
  <PackageReference Include="coverlet.collector" Version="6.0.0" />
  <PackageReference Include="coverlet.msbuild" Version="6.0.0" />
</ItemGroup>
```

**Coverage Commands**:
```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
```

#### 5.5.2: Coverage Validation
**Coverage Requirements**:
- [ ] Maintain current coverage levels (minimum)
- [ ] Core business logic: 95%+ coverage
- [ ] API controllers: 90%+ coverage
- [ ] Data access: 85%+ coverage
- [ ] Service layer: 95%+ coverage

### Task 5.6: Test Data Management

#### 5.6.1: Test Data Consistency
**Test Data Strategy**:
```csharp
public static class TestDataFactory
{
    public static List<TaxBracket> Create2024_25TaxBrackets()
    {
        // Centralized test data creation
        return new List<TaxBracket>
        {
            new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0m, FixedAmount = 0m },
            new TaxBracket { MinIncome = 18201, MaxIncome = 45000, TaxRate = 0.16m, FixedAmount = 0m },
            // ... exact same test data used in original tests
        };
    }
    
    public static TaxCalculationRequest CreateValidRequest(decimal income = 85000m, string year = "2024-25")
    {
        return new TaxCalculationRequest
        {
            TaxableIncome = income,
            FinancialYear = year,
            ResidencyStatus = "Resident",
            IncludeMedicareLevy = true,
            IncludeOffsets = true
        };
    }
}
```

## Testing Strategy

### Test Migration Validation
1. **Existing Test Preservation**:
   - All original tests must pass
   - Test logic unchanged
   - Expected results identical

2. **New Test Categories**:
   - API integration tests
   - Contract compliance tests
   - Performance regression tests
   - Cross-platform compatibility tests

### Test Execution Strategy
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test categories
dotnet test --filter Category=Integration
dotnet test --filter Category=Performance
dotnet test --filter Category=Contract
```

## Success Criteria

### Technical Validation
- [ ] All existing tests migrated and passing
- [ ] Test coverage maintained or improved
- [ ] API integration tests functioning
- [ ] Performance tests within thresholds
- [ ] Test execution time acceptable

### Business Validation
- [ ] All business logic test scenarios preserved
- [ ] Tax calculation accuracy validated
- [ ] API contract compliance verified
- [ ] Error scenarios properly tested

### Quality Validation
- [ ] Test code quality maintained
- [ ] Test maintainability improved
- [ ] CI/CD integration working
- [ ] Test reporting functional

## Risk Mitigation

### Primary Risks
1. **Test Framework Breaking Changes**
   - **Risk**: NUnit/Moq updates break existing tests
   - **Mitigation**: Incremental framework updates with validation

2. **Test Coverage Reduction**
   - **Risk**: Migration causes test coverage loss
   - **Mitigation**: Coverage monitoring and validation

3. **Test Performance Degradation**
   - **Risk**: Tests run significantly slower
   - **Mitigation**: Performance monitoring and optimization

### Monitoring Strategy
- Test execution time tracking
- Coverage percentage monitoring
- Test failure rate analysis
- CI/CD pipeline health

## Timeline

### Day 1: Project Migration and Framework Updates
- Morning: Convert test project to .NET 8
- Afternoon: Update testing frameworks and resolve compatibility issues

### Day 2: Test Migration and API Testing
- Morning: Migrate existing unit tests
- Afternoon: Implement API integration tests

### Day 3: Performance and Validation
- Morning: Add performance tests and coverage validation
- Afternoon: Final validation and CI/CD integration

## Quality Gates

### Entry Criteria
- [ ] Phases 1-4 completed successfully
- [ ] Current test coverage documented
- [ ] Test environment prepared

### Exit Criteria
- [ ] All tests migrated and passing
- [ ] Coverage requirements met
- [ ] API integration tests functional
- [ ] Performance tests within thresholds
- [ ] CI/CD pipeline working

### Approval Required
- [ ] QA team validation of test coverage
- [ ] Technical lead approval of test strategy
- [ ] DevOps approval of CI/CD integration

## Preparation for Phase 6

### Deliverables for Final Phase
- Complete test suite for .NET 8 application
- API contract validation framework
- Performance monitoring baseline
- CI/CD test integration
- Test documentation and maintenance guide

This phase ensures that the migrated application has comprehensive test coverage and validates that all functionality works correctly in the .NET 8 environment while maintaining the same quality standards as the original .NET Framework application.
