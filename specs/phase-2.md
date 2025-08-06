# Phase 2: API Migration - Detailed Plan

## Phase Overview

**Duration**: Weeks 2-3  
**Objective**: Complete migration of Web API layer to ASP.NET Core 8 with 100% contract preservation  
**Risk Level**: HIGH (API contract changes)  
**Success Criteria**: All endpoints respond identically to .NET Framework version  
**Commit Goal**: Complete API migration to ASP.NET Core 8 with validated compatibility  

## Pre-Phase 2 Prerequisites

### Phase 1 Completion Validation
- [x] All core libraries running on .NET 8
- [x] Unit tests 100% passing
- [x] New ASP.NET Core project skeleton ready
- [x] CI/CD pipeline operational
- [x] Contract validation framework established

### Environment Readiness  
- [ ] .NET 8 runtime configured for API testing
- [ ] Database connectivity verified
- [ ] Redis caching service available
- [ ] API testing tools (Postman) configured

---

## Build and Test Strategy for Phase 2

### Continuous Validation Approach
1. **Create ASP.NET Core project and build immediately**
2. **Add one controller at a time with immediate testing**
3. **Build and test after each controller migration**
4. **Document any contract differences in progress.md immediately**
5. **Test API endpoints manually after each controller**

### Build Commands to Use
```bash
# After creating new project
dotnet build TaxCalculator.Api.Core

# After adding each controller
dotnet build TaxCalculator.Api.Core
dotnet test TaxCalculator.Tests.Unit

# Test API endpoints (when ready)
dotnet run --project TaxCalculator.Api.Core
# Then test with curl/Postman
```

---

## Task Group 1: ASP.NET Core Project Setup (Week 2, Days 1-2)

### 1.1 Create ASP.NET Core Project

**Objective**: Create new API project with basic structure

#### Tasks:
1. **Create New Project**
   ```bash
   dotnet new webapi -n TaxCalculator.Api.Core -f net8.0
   ```

2. **Initial Build Test**
   ```bash
   dotnet build TaxCalculator.Api.Core
   # Document any issues in progress.md
   ```

3. **Configure Basic Program.cs**
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   
   builder.Services.AddControllers()
       .AddNewtonsoftJson(options =>
       {
           options.SerializerSettings.ContractResolver = 
               new DefaultContractResolver(); // PascalCase preservation
       });
   
   var app = builder.Build();
   
   app.MapControllers();
   app.Run();
   ```

4. **Add Package References**
   ```xml
   <PackageReference Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" Version="8.0.0" />
   <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
   ```

5. **Build and Test**
   ```bash
   dotnet build TaxCalculator.Api.Core
   # If successful, proceed to controller migration
   ```

#### Success Criteria:
- ✅ ASP.NET Core project builds successfully
- ✅ Newtonsoft.Json configured correctly
- ✅ Ready for controller migration

### 1.2 Health Controller Migration

**Objective**: Migrate the simplest endpoint first to establish patterns

#### Tasks:
1. **Analyze Current Implementation**
   ```csharp
   // .NET Framework Web API 2 (examine existing)
   [Route("api/health")]
   public class HealthController : ApiController
   {
       [HttpGet]
       public IHttpActionResult Get()
       {
           return Ok(new { status = "OK", timestamp = DateTime.UtcNow.ToString("O") });
       }
   }
   ```

2. **Create ASP.NET Core Controller**
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class HealthController : ControllerBase
   {
       [HttpGet]
       public IActionResult Get()
       {
           return Ok(new { status = "OK", timestamp = DateTime.UtcNow.ToString("O") });
       }
   }
   ```

3. **Build and Test Immediately**
   ```bash
   dotnet build TaxCalculator.Api.Core
   # If build succeeds, run the API
   dotnet run --project TaxCalculator.Api.Core
   # Test manually: curl http://localhost:5000/api/health
   ```

4. **Manual Testing**
   - Test `/api/health` endpoint manually
   - Compare JSON response with original API
   - Verify HTTP status codes
   - Document any differences in progress.md

#### Success Criteria:
- ✅ Project builds with health controller
- ✅ `/api/health` endpoint responds
- ✅ JSON structure matches original
- ✅ Manual testing confirms compatibility

### 1.2 Tax Calculation Controller Migration

**Objective**: Migrate the core business endpoint with complex request/response

#### Current Implementation Analysis:
```csharp
[Route("api/tax")]
public class TaxController : ApiController
{
    [HttpPost]
    [Route("calculate")]
    public async Task<IHttpActionResult> Calculate([FromBody] TaxCalculationRequest request)
    {
        // Complex business logic with validation
        // Returns TaxCalculationResult
    }
    
    [HttpGet]
    [Route("brackets/{year}")]
    public async Task<IHttpActionResult> GetBrackets(string year)
    {
        // Returns TaxBracket[]
    }
    
    // Additional endpoints...
}
```

#### Tasks:
1. **Controller Migration with Identical Routing**
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class TaxController : ControllerBase
   {
       private readonly ITaxCalculationService _taxService;
       
       public TaxController(ITaxCalculationService taxService)
       {
           _taxService = taxService;
       }
       
       [HttpPost("calculate")]
       public async Task<IActionResult> Calculate([FromBody] TaxCalculationRequest request)
       {
           // Identical business logic flow
           // Preserve all validation and error handling
       }
       
       [HttpGet("brackets/{year}")]
       public async Task<IActionResult> GetBrackets(string year)
       {
           // Identical implementation
       }
   }
   ```

2. **Request/Response Model Validation**
   - Verify TaxCalculationRequest deserializes identically
   - Ensure TaxCalculationResult serializes with same JSON structure
   - Test all property names and data types
   - Validate nested object serialization

3. **Error Handling Preservation**
   ```csharp
   // Must preserve exact error response format
   [HttpPost("calculate")]
   public async Task<IActionResult> Calculate([FromBody] TaxCalculationRequest request)
   {
       try
       {
           if (request == null)
               return BadRequest("Request cannot be null");
               
           // Validation logic - must return identical error messages
           if (request.TaxableIncome < 0)
               return BadRequest("Taxable income must be greater than or equal to zero");
               
           var result = await _taxService.CalculateTaxAsync(request);
           return Ok(result);
       }
       catch (ValidationException ex)
       {
           return BadRequest(ex.Message); // Same message format
       }
       catch (Exception ex)
       {
           // Log error but return same generic message
           return StatusCode(500, "An error occurred processing your request");
       }
   }
   ```

4. **Business Logic Integration**
   - Ensure ITaxCalculationService injection works
   - Test all calculation scenarios
   - Verify decimal precision maintained
   - Test edge cases and boundary conditions

#### Success Criteria:
- ✅ All endpoints return identical JSON structures
- ✅ Request validation produces same error messages
- ✅ Business calculations produce identical results
- ✅ HTTP status codes match exactly
- ✅ Error responses format preserved

### 1.3 Additional Endpoints Migration

**Objective**: Complete migration of remaining endpoints

#### Tasks:
1. **Tax Comparison Endpoint**
   ```csharp
   [HttpGet("compare")]
   public async Task<IActionResult> Compare(
       [FromQuery] decimal income,
       [FromQuery] string[] years)
   {
       // Query parameter binding validation
       // Multiple years handling
       // Comparison result format
   }
   ```

2. **Tax History Endpoint**
   ```csharp
   [HttpGet("history/{income}")]
   public async Task<IActionResult> GetHistory(
       decimal income,
       [FromQuery] int years = 5)
   {
       // Path parameter and query parameter combination
       // Historical data retrieval
       // Default parameter handling
   }
   ```

3. **Query Parameter Binding Validation**
   - Test query string parsing behavior
   - Verify array parameter handling (`years=2019-20&years=2024-25`)
   - Ensure default values work identically
   - Test URL encoding/decoding

#### Success Criteria:
- ✅ Query parameter binding works identically
- ✅ Array parameters parsed correctly
- ✅ Default values applied as expected
- ✅ URL encoding handled consistently

---

## Task Group 2: JSON Serialization Validation (Week 3, Days 4-5)

### 2.1 Newtonsoft.Json Configuration Validation

**Objective**: Ensure exact JSON compatibility with legacy system

#### Tasks:
1. **Serialization Settings Verification**
   ```csharp
   builder.Services.AddControllers()
       .AddNewtonsoftJson(options =>
       {
           // Critical: Must match legacy settings exactly
           options.SerializerSettings.ContractResolver = new DefaultContractResolver();
           options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
           options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
           options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
           options.SerializerSettings.Formatting = Formatting.None;
       });
   ```

2. **Property Name Comparison**
   ```csharp
   [Test]
   public void TaxCalculationResult_PropertyNamesMatch()
   {
       var legacyJson = GetLegacyCalculationJson();
       var coreJson = GetCoreCalculationJson();
       
       var legacyProperties = JObject.Parse(legacyJson).Properties().Select(p => p.Name);
       var coreProperties = JObject.Parse(coreJson).Properties().Select(p => p.Name);
       
       CollectionAssert.AreEquivalent(legacyProperties, coreProperties);
   }
   ```

3. **Data Type Serialization Testing**
   - Decimal precision testing (critical for tax calculations)
   - DateTime format validation (ISO 8601)
   - Boolean value serialization
   - Null value handling
   - Array/collection serialization

4. **Complex Object Testing**
   ```csharp
   [Test]
   public void ComplexTaxResult_SerializesIdentically()
   {
       var complexResult = new TaxCalculationResult
       {
           TotalTax = 15432.75m,
           Brackets = new List<TaxBracketResult>
           {
               new() { Rate = 0.19m, TaxableAmount = 45000m },
               new() { Rate = 0.325m, TaxableAmount = 30000m }
           },
           CalculationDate = DateTime.UtcNow,
           MedicareLevy = 1234.56m
       };
       
       var legacyJson = SerializeWithLegacySettings(complexResult);
       var coreJson = SerializeWithCoreSettings(complexResult);
       
       Assert.AreEqual(legacyJson, coreJson);
   }
   ```

#### Success Criteria:
- ✅ Property names are PascalCase (not camelCase)
- ✅ Decimal values maintain precision
- ✅ DateTime values in ISO 8601 format
- ✅ Null values handled identically
- ✅ Complex nested objects serialize correctly

### 2.2 Response Schema Validation

**Objective**: Create automated validation for all response schemas

#### Tasks:
1. **JSON Schema Generation**
   ```csharp
   [Test]
   public void GenerateResponseSchemas()
   {
       var generator = new JSchemaGenerator();
       var taxResultSchema = generator.Generate(typeof(TaxCalculationResult));
       var bracketsSchema = generator.Generate(typeof(TaxBracket[]));
       
       File.WriteAllText("schemas/tax-result.schema.json", taxResultSchema.ToString());
       File.WriteAllText("schemas/tax-brackets.schema.json", bracketsSchema.ToString());
   }
   ```

2. **Schema Validation Tests**
   ```csharp
   [TestCase("/api/tax/calculate")]
   [TestCase("/api/tax/brackets/2023-24")]
   [TestCase("/api/tax/compare?income=75000&years=2023-24")]
   public async Task EndpointResponse_MatchesSchema(string endpoint)
   {
       var response = await _httpClient.GetAsync(endpoint);
       var json = await response.Content.ReadAsStringAsync();
       var schema = LoadSchemaForEndpoint(endpoint);
       
       var isValid = JToken.Parse(json).IsValid(schema, out IList<string> errors);
       Assert.IsTrue(isValid, $"Schema validation failed: {string.Join(", ", errors)}");
   }
   ```

3. **Regression Detection**
   - Compare response schemas between versions
   - Alert on any schema differences
   - Prevent deployment if breaking changes detected

#### Success Criteria:
- ✅ All response schemas documented
- ✅ Schema validation tests pass
- ✅ Regression detection operational
- ✅ Breaking change detection working

---

## Task Group 3: Configuration System Migration (Week 4, Days 1-2)

### 3.1 App.config to appsettings.json Migration

**Objective**: Complete configuration system overhaul while preserving all values

#### Current Configuration Analysis:
```xml
<!-- App.config / Web.config -->
<configuration>
  <connectionStrings>
    <add name="DefaultConnection" connectionString="..." />
  </connectionStrings>
  <appSettings>
    <add key="RedisConnectionString" value="..." />
    <add key="CacheExpirationMinutes" value="30" />
    <add key="LogLevel" value="Information" />
  </appSettings>
</configuration>
```

#### Tasks:
1. **appsettings.json Structure Creation**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "[exact connection string from legacy]"
     },
     "Cache": {
       "Redis": {
         "ConnectionString": "[from app settings]",
         "ExpirationMinutes": 30
       }
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft": "Warning",
         "Microsoft.Hosting.Lifetime": "Information"
       }
     }
   }
   ```

2. **Configuration Options Classes**
   ```csharp
   public class CacheOptions
   {
       public RedisOptions Redis { get; set; }
   }
   
   public class RedisOptions
   {
       public string ConnectionString { get; set; }
       public int ExpirationMinutes { get; set; }
   }
   ```

3. **Service Registration**
   ```csharp
   builder.Services.Configure<CacheOptions>(
       builder.Configuration.GetSection("Cache"));
   
   builder.Services.AddSingleton<ICacheService>(provider =>
   {
       var cacheOptions = provider.GetRequiredService<IOptions<CacheOptions>>();
       return new CacheService(cacheOptions.Value.Redis.ConnectionString);
   });
   ```

4. **Configuration Validation Tests**
   ```csharp
   [Test]
   public void DatabaseConnectionString_MigrationSuccessful()
   {
       var legacyConnection = ConfigurationManager.ConnectionStrings["DefaultConnection"];
       var coreConnection = _configuration.GetConnectionString("DefaultConnection");
       
       Assert.AreEqual(legacyConnection.ConnectionString, coreConnection);
   }
   
   [Test]
   public void CacheSettings_MigrationSuccessful()
   {
       var legacyRedisConnection = ConfigurationManager.AppSettings["RedisConnectionString"];
       var cacheOptions = _serviceProvider.GetRequiredService<IOptions<CacheOptions>>();
       
       Assert.AreEqual(legacyRedisConnection, cacheOptions.Value.Redis.ConnectionString);
   }
   ```

#### Success Criteria:
- ✅ All configuration values successfully migrated
- ✅ Connection strings work identically
- ✅ Type conversion works correctly (string to int, bool)
- ✅ Environment-specific overrides functional

### 3.2 Environment Configuration Setup

**Objective**: Set up proper environment-specific configuration

#### Tasks:
1. **Environment Files Creation**
   ```json
   // appsettings.Development.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=TaxCalc_Dev;..."
     },
     "Cache": {
       "Redis": {
         "ConnectionString": "localhost:6379"
       }
     },
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   
   // appsettings.Production.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "${DATABASE_CONNECTION_STRING}"
     },
     "Cache": {
       "Redis": {
         "ConnectionString": "${REDIS_CONNECTION_STRING}"
       }
     }
   }
   ```

2. **Environment Variable Binding**
   ```csharp
   // Support environment variable overrides
   builder.Configuration.AddEnvironmentVariables();
   
   // Support Azure Key Vault in production
   if (builder.Environment.IsProduction())
   {
       builder.Configuration.AddAzureKeyVault(/* vault settings */);
   }
   ```

3. **Configuration Validation**
   ```csharp
   builder.Services.AddOptions<CacheOptions>()
       .Bind(builder.Configuration.GetSection("Cache"))
       .ValidateDataAnnotations()
       .Validate(options => !string.IsNullOrEmpty(options.Redis.ConnectionString), 
                 "Redis connection string is required");
   ```

#### Success Criteria:
- ✅ Development environment configuration works
- ✅ Production environment variables override correctly
- ✅ Configuration validation prevents invalid startup
- ✅ Secret management integrated

---

## Task Group 4: Dependency Injection Finalization (Week 4, Days 3-4)

### 4.1 Service Registration Migration

**Objective**: Complete transition from Autofac to built-in DI

#### Current Autofac Configuration:
```csharp
// AutofacConfig.cs
var builder = new ContainerBuilder();
builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
builder.RegisterType<TaxCalculationService>().As<ITaxCalculationService>().InstancePerRequest();
builder.RegisterType<SqlBracketRepository>().As<ITaxBracketRepository>().InstancePerRequest();
builder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
```

#### Tasks:
1. **Service Registration Translation**
   ```csharp
   // Program.cs
   builder.Services.AddScoped<ITaxCalculationService, TaxCalculationService>();
   builder.Services.AddScoped<ITaxBracketRepository, SqlBracketRepository>();
   builder.Services.AddScoped<IConnectionFactory, SqlConnectionFactory>();
   builder.Services.AddSingleton<ICacheService, CacheService>();
   ```

2. **Lifetime Mapping Validation**
   | Autofac | Built-in DI | Validation Required |
   |---------|-------------|-------------------|
   | InstancePerRequest | Scoped | ✅ Request boundaries |
   | SingleInstance | Singleton | ✅ Thread safety |
   | InstancePerDependency | Transient | ✅ Performance impact |

3. **Service Resolution Testing**
   ```csharp
   [TestFixture]
   public class DependencyInjectionTests
   {
       [Test]
       public void TaxCalculationService_ResolvesWithDependencies()
       {
           var serviceProvider = BuildServiceProvider();
           var taxService = serviceProvider.GetRequiredService<ITaxCalculationService>();
           
           Assert.IsNotNull(taxService);
           Assert.IsInstanceOf<TaxCalculationService>(taxService);
       }
       
       [Test]
       public void CacheService_IsSingleton()
       {
           var serviceProvider = BuildServiceProvider();
           var cache1 = serviceProvider.GetRequiredService<ICacheService>();
           var cache2 = serviceProvider.GetRequiredService<ICacheService>();
           
           Assert.AreSame(cache1, cache2);
       }
   }
   ```

4. **Circular Dependency Validation**
   - Test all service resolution paths
   - Verify no circular dependencies exist
   - Ensure startup performance acceptable

#### Success Criteria:
- ✅ All services resolve correctly
- ✅ Service lifetimes behave identically
- ✅ No circular dependencies
- ✅ Performance within acceptable range

### 4.2 Integration Testing Setup

**Objective**: Comprehensive integration testing for the complete API

#### Tasks:
1. **Test Host Configuration**
   ```csharp
   [SetUpFixture]
   public class ApiIntegrationTestSetup
   {
       private WebApplication _app;
       private HttpClient _httpClient;
       
       [OneTimeSetUp]
       public async Task Setup()
       {
           var builder = WebApplication.CreateBuilder();
           // Configure exactly like production
           
           _app = builder.Build();
           await _app.StartAsync();
           
           _httpClient = new HttpClient();
           _httpClient.BaseAddress = new Uri("http://localhost:5000");
       }
   }
   ```

2. **End-to-End Test Suite**
   ```csharp
   [TestFixture]
   public class TaxCalculationApiIntegrationTests
   {
       [Test]
       public async Task CalculateTax_ComplexScenario_ReturnsCorrectResult()
       {
           var request = new TaxCalculationRequest
           {
               TaxableIncome = 87500m,
               FinancialYear = "2023-24",
               ResidencyStatus = "Resident",
               IncludeMedicareLevy = true
           };
           
           var response = await _httpClient.PostAsJsonAsync("/api/tax/calculate", request);
           var result = await response.Content.ReadFromJsonAsync<TaxCalculationResult>();
           
           Assert.AreEqual(200, (int)response.StatusCode);
           Assert.IsNotNull(result);
           Assert.Greater(result.TotalTax, 0);
           // Validate against known calculation
       }
   }
   ```

3. **Performance Integration Tests**
   ```csharp
   [Test]
   public async Task TaxCalculation_PerformanceBaseline()
   {
       var stopwatch = Stopwatch.StartNew();
       
       var tasks = Enumerable.Range(1, 100).Select(async i =>
       {
           var request = new TaxCalculationRequest { TaxableIncome = i * 1000 };
           await _httpClient.PostAsJsonAsync("/api/tax/calculate", request);
       });
       
       await Task.WhenAll(tasks);
       stopwatch.Stop();
       
       Assert.Less(stopwatch.ElapsedMilliseconds, 5000, "100 requests should complete within 5 seconds");
   }
   ```

#### Success Criteria:
- ✅ All integration tests pass
- ✅ Performance meets baseline requirements
- ✅ Database connectivity working
- ✅ Caching functionality operational

---

## Task Group 5: Error Handling & Logging (Week 4, Day 5)

### 5.1 Error Handling Preservation

**Objective**: Ensure identical error responses and handling

#### Tasks:
1. **Global Exception Handler**
   ```csharp
   public class GlobalExceptionMiddleware
   {
       public async Task InvokeAsync(HttpContext context, RequestDelegate next)
       {
           try
           {
               await next(context);
           }
           catch (ValidationException ex)
           {
               await HandleValidationException(context, ex);
           }
           catch (Exception ex)
           {
               await HandleGenericException(context, ex);
           }
       }
       
       private async Task HandleValidationException(HttpContext context, ValidationException ex)
       {
           context.Response.StatusCode = 400;
           await context.Response.WriteAsync(ex.Message); // Same as legacy
       }
   }
   ```

2. **Error Response Format Validation**
   ```csharp
   [Test]
   public async Task InvalidRequest_ReturnsIdenticalErrorFormat()
   {
       var invalidRequest = new { TaxableIncome = -1000 };
       
       var legacyResponse = await CallLegacyApi(invalidRequest);
       var coreResponse = await CallCoreApi(invalidRequest);
       
       Assert.AreEqual(legacyResponse.StatusCode, coreResponse.StatusCode);
       Assert.AreEqual(legacyResponse.Content, coreResponse.Content);
   }
   ```

#### Success Criteria:
- ✅ All error status codes identical
- ✅ Error message formats preserved
- ✅ Exception handling behaves consistently

### 5.2 Logging Migration

**Objective**: Migrate custom logging to Microsoft.Extensions.Logging

#### Tasks:
1. **Logging Bridge Implementation**
   ```csharp
   public class LoggingBridge : ILogger // Custom interface
   {
       private readonly Microsoft.Extensions.Logging.ILogger _logger;
       
       public LoggingBridge(Microsoft.Extensions.Logging.ILogger<LoggingBridge> logger)
       {
           _logger = logger;
       }
       
       public void LogInformation(string message)
       {
           _logger.LogInformation(message); // Preserve format
       }
   }
   ```

2. **Log Output Validation**
   - Ensure log levels are preserved
   - Verify log message formats
   - Test structured logging compatibility

#### Success Criteria:
- ✅ Log output format preserved
- ✅ Log levels mapping correctly
- ✅ Performance impact minimal

---

## Phase 2 Validation & Success Criteria

### API Contract Validation
- [ ] **All Endpoints Responding**: Every endpoint returns 200/400/500 as expected
- [ ] **JSON Structure Identical**: Property names, data types, nesting preserved
- [ ] **Request Processing**: All request types accepted and processed identically
- [ ] **Error Responses**: Validation errors and exceptions return same messages

### Business Logic Validation
- [ ] **Tax Calculations**: All calculation results mathematically identical
- [ ] **Data Retrieval**: Bracket data and historical data identical
- [ ] **Edge Cases**: Boundary conditions handled consistently
- [ ] **Performance**: Response times within ±20% of baseline

### Configuration Validation
- [ ] **Database Connectivity**: All database operations working
- [ ] **Caching**: Redis operations functional and performant
- [ ] **Environment Variables**: Override behavior working correctly
- [ ] **Secrets Management**: Sensitive data properly protected

### Integration Validation
- [ ] **Dependency Injection**: All services resolve and function correctly
- [ ] **Logging**: Output format and levels preserved
- [ ] **Error Handling**: Global exception handling working
- [ ] **Health Checks**: Both /api/health and /healthz operational

---

## Phase 2 Delivery Checklist

### Quality Assurance
- [ ] All unit tests pass with .NET 8 API
- [ ] Integration tests cover all endpoints
- [ ] Contract validation tests operational
- [ ] Performance baseline established

### Documentation
- [ ] API migration changes documented
- [ ] Configuration migration guide created
- [ ] Error handling changes noted
- [ ] Performance metrics documented

### Security Review
- [ ] Input validation preserved
- [ ] Error information disclosure reviewed
- [ ] Configuration security validated
- [ ] Dependency security scan clean

### Deployment Readiness
- [ ] Docker images build successfully
- [ ] Health checks responding correctly
- [ ] Configuration externalized properly
- [ ] Monitoring hooks in place

---

## Rollback Plan for Phase 2

### Rollback Triggers
- API contract validation failures
- Business logic calculation errors
- Performance degradation >30%
- Critical integration failures

### Rollback Procedure
1. **Traffic Routing**: Switch load balancer to .NET Framework version
2. **Database State**: Ensure no schema changes to rollback
3. **Configuration**: Preserve configuration changes in rollback
4. **Monitoring**: Confirm error rates return to baseline
5. **Investigation**: Root cause analysis and resolution plan

### Recovery Time Objective
- **Detection**: Within 15 minutes of deployment
- **Rollback Decision**: Within 5 minutes of detection
- **Traffic Switch**: Within 2 minutes of decision
- **Validation**: Within 5 minutes of switch

---

*Phase 2 Success Criteria: Complete API migration with 100% functional and behavioral compatibility validated through automated testing.*
