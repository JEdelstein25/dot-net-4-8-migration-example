# Phase 4: API Layer Migration (Critical Phase)

## Overview
Phase 4 is the most critical and complex phase, migrating from ASP.NET Web API 2 to ASP.NET Core Web API while maintaining 100% API contract compatibility.

## Scope
- **Primary Target**: TaxCalculator.Api project
- **Risk Level**: 🔴 HIGH RISK
- **Dependencies**: Phases 1-3 completed
- **Estimated Duration**: 3-4 days

## Objectives
1. Migrate from ASP.NET Web API 2 to ASP.NET Core Web API
2. Maintain 100% identical API contracts (endpoints, models, status codes)
3. Preserve error handling patterns and response formats
4. Update dependency injection from Autofac to built-in DI
5. Ensure zero client impact

## Pre-Migration Checklist
- [ ] Phases 1-3 successfully completed and validated
- [ ] Complete API contract documentation created
- [ ] Current error response formats documented
- [ ] Baseline performance metrics captured
- [ ] Client applications identified for testing

## Migration Tasks

### Task 4.1: Project Structure Migration

#### 4.1.1: ASP.NET Core Project Creation
**Current State**: Web Application project with System.Web.Http
**Target State**: ASP.NET Core Web API project

**New Project File**:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>TaxCalculator.Api</RootNamespace>
    <AssemblyName>TaxCalculator.Api</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TaxCalculator.Core\TaxCalculator.Core.csproj" />
    <ProjectReference Include="..\TaxCalculator.Data\TaxCalculator.Data.csproj" />
    <ProjectReference Include="..\TaxCalculator.Services\TaxCalculator.Services.csproj" />
  </ItemGroup>
</Project>
```

#### 4.1.2: Startup Configuration
**Create Program.cs**:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson(); // Maintain JSON compatibility

// Configure dependency injection
builder.Services.AddScoped<ITaxCalculationService, TaxCalculationService>();
builder.Services.AddScoped<ITaxBracketRepository, TaxBracketRepository>();
// ... other service registrations

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

app.Run();
```

### Task 4.2: Controller Migration

#### 4.2.1: TaxController Migration
**CRITICAL**: Maintain exact same API contracts

**Current Structure** (Web API 2):
```csharp
[RoutePrefix("api/tax")]
public class TaxController : ApiController
{
    [HttpPost]
    [Route("calculate")]
    public async Task<IHttpActionResult> CalculateTax([FromBody] TaxCalculationRequest request)
    {
        // Implementation
    }
}
```

**Target Structure** (ASP.NET Core):
```csharp
[ApiController]
[Route("api/tax")]
public class TaxController : ControllerBase
{
    [HttpPost("calculate")]
    public async Task<ActionResult<TaxCalculationResult>> CalculateTax([FromBody] TaxCalculationRequest request)
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
            return StatusCode(500, ex.Message); // Maintain exact error format
        }
    }
}
```

**Critical Considerations**:
- Route patterns must be identical
- HTTP status codes must match exactly
- Error message formats must be preserved
- Response JSON structure must be identical

#### 4.2.2: Error Handling Migration
**Challenge**: ASP.NET Core error handling differs from Web API 2

**Web API 2 Pattern**:
```csharp
return InternalServerError(ex); // Specific format
```

**ASP.NET Core Equivalent**:
```csharp
return StatusCode(500, ex.Message); // Must produce same JSON format
```

**Global Exception Handling**:
```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Handle exceptions to match Web API 2 format exactly
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Replicate exact Web API 2 error response format
        var response = new
        {
            Message = ex.Message,
            ExceptionType = ex.GetType().Name
        };
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
}
```

#### 4.2.3: All Controller Endpoints Migration

**Endpoints to Migrate**:
1. `GET /api/health` - Health check
2. `POST /api/tax/calculate` - Tax calculation
3. `GET /api/tax/brackets/{year}` - Tax brackets
4. `GET /api/tax/compare` - Tax comparison
5. `GET /api/tax/history/{income}` - Tax history

**Validation Requirements**:
- [ ] Identical route patterns
- [ ] Same HTTP methods
- [ ] Identical parameter binding
- [ ] Same response formats
- [ ] Identical error responses

### Task 4.3: Dependency Injection Migration

#### 4.3.1: Autofac to Built-in DI Migration
**Current State**: Autofac container with Web API 2 integration
**Target State**: Built-in Microsoft.Extensions.DependencyInjection

**Current Registration** (Autofac):
```csharp
var builder = new ContainerBuilder();
builder.RegisterType<TaxCalculationService>().As<ITaxCalculationService>();
builder.RegisterType<TaxBracketRepository>().As<ITaxBracketRepository>();
// ... other registrations
```

**Target Registration** (Built-in DI):
```csharp
builder.Services.AddScoped<ITaxCalculationService, TaxCalculationService>();
builder.Services.AddScoped<ITaxBracketRepository, ITaxBracketRepository>();
// ... other registrations
```

**Critical Validation**:
- Service lifetimes must match (Singleton, Scoped, Transient)
- Dependency resolution behavior must be identical
- Constructor injection must work the same way

#### 4.3.2: Service Lifetime Mapping
| Autofac | Built-in DI | Usage |
|---------|-------------|-------|
| SingleInstance | AddSingleton | Cache services |
| InstancePerLifetimeScope | AddScoped | Per-request services |
| InstancePerDependency | AddTransient | Stateless services |

### Task 4.4: Configuration Migration

#### 4.4.1: app.config to appsettings.json
**Current Configuration** (app.config):
```xml
<connectionStrings>
  <add name="DefaultConnection" connectionString="..." />
</connectionStrings>
<appSettings>
  <add key="CacheExpiration" value="3600" />
</appSettings>
```

**Target Configuration** (appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "AppSettings": {
    "CacheExpiration": "3600"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**Configuration Service**:
```csharp
public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name);
    }
    
    public string GetAppSetting(string key)
    {
        return _configuration[$"AppSettings:{key}"];
    }
}
```

### Task 4.5: API Contract Validation

#### 4.5.1: Automated Contract Testing
**Test Framework**:
```csharp
[TestFixture]
public class ApiContractTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task POST_TaxCalculate_ReturnsIdenticalFormat()
    {
        // Arrange
        var request = new TaxCalculationRequest
        {
            TaxableIncome = 85000,
            FinancialYear = "2024-25"
        };

        // Act
        var response = await _client.PostAsync("/api/tax/calculate", 
            new StringContent(JsonConvert.SerializeObject(request), 
            Encoding.UTF8, "application/json"));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<TaxCalculationResult>(content);
        
        // Validate response structure matches exactly
        Assert.That(result.TaxableIncome, Is.EqualTo(85000));
        Assert.That(result.IncomeTax, Is.GreaterThan(0));
        // ... validate all response fields
    }

    [Test]
    public async Task POST_TaxCalculate_InvalidRequest_ReturnsIdenticalError()
    {
        // Test error responses match Web API 2 exactly
        var response = await _client.PostAsync("/api/tax/calculate", 
            new StringContent("null", Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Contains.Substring("Request cannot be null"));
    }
}
```

#### 4.5.2: Response Format Validation
**JSON Schema Validation**:
```csharp
[Test]
public async Task AllEndpoints_ResponseFormat_MatchesSchema()
{
    // Validate all API responses match documented schemas exactly
    var endpoints = GetAllApiEndpoints();
    
    foreach (var endpoint in endpoints)
    {
        var response = await CallEndpoint(endpoint);
        ValidateJsonSchema(response, endpoint.ExpectedSchema);
    }
}
```

## Testing Strategy

### Contract Compliance Testing

#### 4.5.1: Complete API Test Suite
```csharp
[TestFixture]
public class ApiMigrationValidationTests
{
    [TestCaseSource(nameof(AllApiEndpoints))]
    public async Task ApiEndpoint_Behavior_IdenticalToWebApi2(ApiTestCase testCase)
    {
        // Test every endpoint with various inputs
        // Compare responses with Web API 2 baseline
    }
    
    private static IEnumerable<ApiTestCase> AllApiEndpoints()
    {
        yield return new ApiTestCase
        {
            Method = "POST",
            Url = "/api/tax/calculate",
            RequestBody = new TaxCalculationRequest { TaxableIncome = 50000, FinancialYear = "2024-25" }
        };
        // ... all other endpoints and test cases
    }
}
```

#### 4.5.2: Error Handling Validation
```csharp
[TestFixture]
public class ErrorHandlingTests
{
    [Test]
    public async Task InvalidInput_ReturnsIdenticalErrorFormat()
    {
        // Test all error scenarios produce identical responses
    }
    
    [Test]
    public async Task InternalServerError_ReturnsIdenticalFormat()
    {
        // Test exception handling produces same format
    }
}
```

### Performance Testing
- API response times comparison
- Memory usage analysis
- Concurrent request handling
- Database connection performance

## Success Criteria

### Technical Validation
- [ ] All API endpoints respond correctly
- [ ] HTTP status codes match exactly
- [ ] Response JSON format identical
- [ ] Error messages match exactly
- [ ] Performance within 10% of baseline

### Business Validation
- [ ] All tax calculation endpoints work correctly
- [ ] Tax bracket retrieval accurate
- [ ] Error handling maintains user experience
- [ ] Client applications continue working without changes

### Integration Validation
- [ ] All service layer integrations work
- [ ] Database operations function correctly
- [ ] Caching operates properly
- [ ] Logging functions correctly

## Risk Mitigation

### Critical Risks
1. **API Contract Breaking Changes**
   - **Mitigation**: Comprehensive automated testing
   - **Monitoring**: Real-time contract validation

2. **Error Response Format Changes**
   - **Mitigation**: Custom error handling middleware
   - **Validation**: Error scenario testing

3. **Dependency Injection Behavior Changes**
   - **Mitigation**: Service lifetime validation
   - **Testing**: Integration testing with all services

### Monitoring Strategy
- API response format validation
- Performance monitoring
- Error rate tracking
- Client application health checks

## Rollback Plan

### Immediate Rollback Triggers
- Any API contract violation detected
- Performance degradation > 10%
- Client application failures
- Critical business logic errors

### Rollback Procedure
1. **Switch back to Web API 2**: < 5 minutes
2. **Validate functionality**: < 10 minutes
3. **Notify stakeholders**: < 5 minutes
4. **Total rollback time**: < 20 minutes

## Timeline

### Day 1: Project Setup and Basic Controllers
- Morning: ASP.NET Core project creation and basic structure
- Afternoon: Controller migration and basic functionality

### Day 2: Error Handling and DI
- Morning: Error handling middleware and response format validation
- Afternoon: Dependency injection migration and testing

### Day 3: Configuration and Integration
- Morning: Configuration migration and validation
- Afternoon: End-to-end integration testing

### Day 4: Final Validation and Performance
- Morning: Comprehensive API contract testing
- Afternoon: Performance validation and optimization

## Quality Gates

### Entry Criteria
- [ ] Phases 1-3 completed successfully
- [ ] API contract documentation complete
- [ ] Test environment prepared for validation

### Exit Criteria
- [ ] All API endpoints function identically
- [ ] Error handling matches exactly
- [ ] Performance meets requirements
- [ ] Client compatibility validated
- [ ] Ready for production deployment

### Approval Required
- [ ] Business stakeholder approval on API functionality
- [ ] Client application team validation
- [ ] Technical architecture review
- [ ] Security and compliance review

## Preparation for Phase 5

### Deliverables for Testing Phase
- Fully functional ASP.NET Core API
- Complete API contract compliance
- Performance benchmarks
- Error handling validation
- Ready for comprehensive testing

This phase is the culmination of the migration effort and requires the most careful attention to detail to ensure zero client impact while achieving the modernization goals.
