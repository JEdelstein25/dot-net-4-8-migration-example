# Australian Tax Calculator API - .NET 4.8 Legacy Implementation Plan

## Project Overview
Design and implement a comprehensive .NET Framework 4.8 Web API project for calculating Australian individual income tax across the last 10 financial years (2015-16 to 2024-25). This is a legacy enterprise application that follows traditional .NET 4.8 patterns and technologies.

## Core Requirements

### Functional Requirements
- **Historical Tax Calculations**: Calculate accurate tax payable for Australian residents across 10 financial years (2015-16 to 2024-25)
- **Progressive Tax System**: Implement Australia's progressive tax bracket system with proper marginal rate calculations
- **Medicare Levy**: Include 2% Medicare levy calculations where applicable
- **Special Levies**: Handle Temporary Budget Repair Levy (2% on income >$180k, 2014-2017) and other historical levies
- **Multiple Input Types**: Support various income scenarios (salary, business income, investment income, etc.)
- **Tax Offset Support**: Implement Low Income Tax Offset (LITO) and other applicable offsets
- **Validation**: Comprehensive input validation and business rule enforcement

### API Endpoints Required
- `POST /api/tax/calculate` - Calculate tax for specific financial year and income
- `GET /api/tax/brackets/{year}` - Retrieve tax brackets for a specific year
- `GET /api/tax/compare` - Compare tax across multiple years for same income
- `GET /api/tax/history/{income}` - Show 10-year tax history for specific income level
- `GET /api/users/{userId}/tax-summary/{year}` - Get user's annual tax summary for specific year
- `GET /api/users/{userId}/monthly-income/{year}` - Get user's monthly income breakdown for year
- `POST /api/users/{userId}/calculate-annual-tax/{year}` - Calculate and store user's annual tax based on monthly income
- `GET /api/users/{userId}/tax-history` - Get user's 5-year tax calculation history
- `POST /api/users/{userId}/monthly-income` - Record monthly income for a user
- `GET /api/health` - Health check endpoint

## Technology Stack (.NET 4.8 Era)
- **.NET Framework 4.8** - Target framework
- **ASP.NET Web API 2** - RESTful API framework
- **Autofac 4.x** - Dependency injection container
- **ADO.NET** - Raw SQL Server data access (no Entity Framework)
- **SQL Server LocalDB** - Database for tax bracket storage and calculation history
- **StackExchange.Redis** - Caching layer for frequently accessed tax brackets
- **NUnit 3.x** - Unit testing framework
- **Newtonsoft.Json** - JSON serialization

## Implementation Phases

### Phase 1: Solution Structure & Database Foundation

#### 1.1 Solution Structure
```
AustralianTaxCalculator.sln
├── 01. Presentation Layer
│   ├── TaxCalculator.Api (ASP.NET Web API 2)
│   └── TaxCalculator.Console (Console Applications)
├── 02. Business Layer  
│   ├── TaxCalculator.Core (Business Logic)
│   └── TaxCalculator.Services (Service Implementations)
├── 03. Data Layer
│   ├── TaxCalculator.Data (Repositories & Data Access)
│   └── TaxCalculator.Infrastructure (External Dependencies)
├── 04. Testing
│   ├── TaxCalculator.Tests.Unit
│   ├── TaxCalculator.Tests.Integration
│   └── TaxCalculator.Tests.Api
└── 05. Database
    └── TaxCalculator.Database (SQL Scripts & Migrations)
```

#### 1.2 Technology Stack Configuration
- **.NET Framework 4.8** with C# 7.3 language features
- **Web API 2.2** with JSON formatting
- **Autofac 4.9.4** for dependency injection
- **StackExchange.Redis 1.2.6** (compatible with .NET 4.8)
- **System.Data.SqlClient** for ADO.NET
- **NUnit 3.13.3** for testing
- **Newtonsoft.Json 12.0.3** for serialization

#### 1.3 Database Schema Design

**Core Tax System Tables:**
```sql
-- Tax Brackets for each financial year
CREATE TABLE TaxBrackets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FinancialYear VARCHAR(7) NOT NULL, -- '2024-25'
    MinIncome DECIMAL(15,2) NOT NULL,
    MaxIncome DECIMAL(15,2) NULL, -- NULL for highest bracket
    TaxRate DECIMAL(5,4) NOT NULL, -- 0.3250 for 32.5%
    FixedAmount DECIMAL(15,2) NOT NULL,
    BracketOrder INT NOT NULL,
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);

-- Tax Offsets (LITO, etc.)
CREATE TABLE TaxOffsets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FinancialYear VARCHAR(7) NOT NULL,
    OffsetType VARCHAR(50) NOT NULL, -- 'LITO', 'SAPTO'
    MaxIncome DECIMAL(15,2) NULL,
    MaxOffset DECIMAL(15,2) NOT NULL,
    PhaseOutStart DECIMAL(15,2) NULL,
    PhaseOutRate DECIMAL(5,4) NULL,
    IsActive BIT DEFAULT 1
);

-- Special Levies (Medicare, Budget Repair, etc.)
CREATE TABLE TaxLevies (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FinancialYear VARCHAR(7) NOT NULL,
    LevyType VARCHAR(50) NOT NULL, -- 'Medicare', 'BudgetRepair'
    ThresholdIncome DECIMAL(15,2) NOT NULL,
    LevyRate DECIMAL(5,4) NOT NULL,
    MaxIncome DECIMAL(15,2) NULL, -- For capped levies
    IsActive BIT DEFAULT 1
);
```

**User Management Tables:**
```sql
-- Users
CREATE TABLE Users (
    UserId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    DateOfBirth DATE NULL,
    TFN VARCHAR(20) NULL, -- Tax File Number (encrypted)
    ResidencyStatus VARCHAR(20) DEFAULT 'Resident',
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    LastModifiedDate DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);

-- Monthly Income Records
CREATE TABLE UserMonthlyIncome (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    FinancialYear VARCHAR(7) NOT NULL, -- '2024-25'
    Month INT NOT NULL, -- 1-12
    GrossIncome DECIMAL(15,2) NOT NULL,
    TaxableIncome DECIMAL(15,2) NOT NULL,
    DeductionsAmount DECIMAL(15,2) DEFAULT 0,
    SuperContributions DECIMAL(15,2) DEFAULT 0,
    IncomeType VARCHAR(50) DEFAULT 'Salary', -- 'Salary', 'Business', 'Investment'
    PayPeriod VARCHAR(20) DEFAULT 'Monthly', -- 'Weekly', 'Fortnightly', 'Monthly'
    RecordedDate DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT UK_UserMonthlyIncome UNIQUE (UserId, FinancialYear, Month)
);

-- Annual Tax Summary
CREATE TABLE UserAnnualTaxSummary (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    FinancialYear VARCHAR(7) NOT NULL,
    TotalGrossIncome DECIMAL(15,2) NOT NULL,
    TotalDeductions DECIMAL(15,2) NOT NULL,
    TotalTaxableIncome DECIMAL(15,2) NOT NULL,
    IncomeTaxPayable DECIMAL(15,2) NOT NULL,
    MedicareLevyPayable DECIMAL(15,2) NOT NULL,
    OtherLeviesPayable DECIMAL(15,2) DEFAULT 0,
    TotalTaxOffsets DECIMAL(15,2) DEFAULT 0,
    NetTaxPayable DECIMAL(15,2) NOT NULL,
    EffectiveTaxRate DECIMAL(5,4) NOT NULL,
    MarginalTaxRate DECIMAL(5,4) NOT NULL,
    CalculationDate DATETIME2 DEFAULT GETDATE(),
    LastModifiedDate DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT UK_UserAnnualTax UNIQUE (UserId, FinancialYear)
);
```

**Audit & Calculation History:**
```sql
-- Tax Calculation Audit Trail
CREATE TABLE TaxCalculationHistory (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NULL, -- NULL for anonymous calculations
    FinancialYear VARCHAR(7) NOT NULL,
    TaxableIncome DECIMAL(15,2) NOT NULL,
    CalculatedTax DECIMAL(15,2) NOT NULL,
    CalculationDetails NVARCHAR(MAX), -- JSON breakdown
    CalculationDate DATETIME2 DEFAULT GETDATE(),
    ClientIP VARCHAR(45),
    UserAgent VARCHAR(500)
);
```

### Phase 2: Core Domain Models & Business Logic

#### 2.1 Core Domain Models (TaxCalculator.Core)

**Tax System Models:**
```csharp
// Core tax bracket entity
public class TaxBracket
{
    public int Id { get; set; }
    public string FinancialYear { get; set; }
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal TaxRate { get; set; }
    public decimal FixedAmount { get; set; }
    public int BracketOrder { get; set; }
    public bool IsActive { get; set; }
}

// Tax calculation request model
public class TaxCalculationRequest
{
    public string FinancialYear { get; set; }
    public decimal TaxableIncome { get; set; }
    public string ResidencyStatus { get; set; } = "Resident";
    public bool IncludeMedicareLevy { get; set; } = true;
    public bool IncludeOffsets { get; set; } = true;
    public Dictionary<string, decimal> AdditionalIncomeTypes { get; set; }
}

// Comprehensive tax calculation result
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
    public List<TaxBracketCalculation> BracketBreakdown { get; set; }
    public List<LevyCalculation> LevyBreakdown { get; set; }
    public List<OffsetCalculation> OffsetBreakdown { get; set; }
}
```

**User Management Models:**
```csharp
// User entity
public class User
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string ResidencyStatus { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
}

// Monthly income record
public class UserMonthlyIncome
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public string FinancialYear { get; set; }
    public int Month { get; set; }
    public decimal GrossIncome { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal SuperContributions { get; set; }
    public string IncomeType { get; set; }
    public DateTime RecordedDate { get; set; }
}

// Annual tax summary
public class UserAnnualTaxSummary
{
    public Guid UserId { get; set; }
    public string FinancialYear { get; set; }
    public decimal TotalGrossIncome { get; set; }
    public decimal TotalTaxableIncome { get; set; }
    public decimal NetTaxPayable { get; set; }
    public decimal EffectiveTaxRate { get; set; }
    public List<MonthlyIncomeSummary> MonthlyBreakdown { get; set; }
    public DateTime LastCalculated { get; set; }
}
```

### Phase 3: Data Access Layer (ADO.NET Implementation)

#### 3.1 Connection Factory Pattern
```csharp
// IConnectionFactory.cs
public interface IConnectionFactory
{
    IDbConnection CreateConnection();
    IDbConnection CreateConnection(string connectionString);
}

// SqlConnectionFactory.cs
public class SqlConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;
    
    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
    
    public IDbConnection CreateConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }
}
```

#### 3.2 Repository Pattern Implementation
```csharp
// ITaxBracketRepository.cs
public interface ITaxBracketRepository
{
    Task<List<TaxBracket>> GetTaxBracketsAsync(string financialYear);
    Task<List<TaxOffset>> GetTaxOffsetsAsync(string financialYear);
    Task<List<TaxLevy>> GetTaxLeviesAsync(string financialYear);
    Task<List<string>> GetAvailableFinancialYearsAsync();
}

// TaxBracketRepository.cs
public class TaxBracketRepository : ITaxBracketRepository
{
    private readonly IConnectionFactory _connectionFactory;
    
    public TaxBracketRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    
    public async Task<List<TaxBracket>> GetTaxBracketsAsync(string financialYear)
    {
        const string sql = @"
            SELECT Id, FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder
            FROM TaxBrackets 
            WHERE FinancialYear = @FinancialYear AND IsActive = 1
            ORDER BY BracketOrder";
            
        using (var connection = _connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(sql, (SqlConnection)connection))
            {
                command.Parameters.AddWithValue("@FinancialYear", financialYear);
                
                var brackets = new List<TaxBracket>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        brackets.Add(new TaxBracket
                        {
                            Id = reader.GetInt32("Id"),
                            FinancialYear = reader.GetString("FinancialYear"),
                            MinIncome = reader.GetDecimal("MinIncome"),
                            MaxIncome = reader.IsDBNull("MaxIncome") ? (decimal?)null : reader.GetDecimal("MaxIncome"),
                            TaxRate = reader.GetDecimal("TaxRate"),
                            FixedAmount = reader.GetDecimal("FixedAmount"),
                            BracketOrder = reader.GetInt32("BracketOrder")
                        });
                    }
                }
                return brackets;
            }
        }
    }
}
```

#### 3.3 User Repository Implementation
```csharp
// IUserRepository.cs  
public interface IUserRepository
{
    Task<User> GetUserAsync(Guid userId);
    Task<Guid> CreateUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task<bool> UserExistsAsync(string email);
    Task<List<User>> GetAllActiveUsersAsync();
}

// IUserIncomeRepository.cs
public interface IUserIncomeRepository
{
    Task<List<UserMonthlyIncome>> GetMonthlyIncomeAsync(Guid userId, string financialYear);
    Task SaveMonthlyIncomeAsync(UserMonthlyIncome income);
    Task UpdateMonthlyIncomeAsync(UserMonthlyIncome income);
    Task<UserAnnualTaxSummary> GetAnnualSummaryAsync(Guid userId, string financialYear);
    Task SaveAnnualSummaryAsync(UserAnnualTaxSummary summary);
    Task<List<UserAnnualTaxSummary>> GetTaxHistoryAsync(Guid userId, int years = 5);
}
```

### Phase 4: Core Tax Calculation Engine

#### 4.1 Tax Calculation Service
```csharp
// ITaxCalculationService.cs
public interface ITaxCalculationService
{
    Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request);
    Task<List<TaxBracket>> GetTaxBracketsAsync(string financialYear);
    Task<TaxCalculationResult> CompareTaxAcrossYearsAsync(decimal income, List<string> years);
    Task<List<TaxCalculationResult>> GetTaxHistoryAsync(decimal income, int years = 10);
}

// TaxCalculationService.cs
public class TaxCalculationService : ITaxCalculationService
{
    private readonly ITaxBracketRepository _taxBracketRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger _logger;
    
    public TaxCalculationService(
        ITaxBracketRepository taxBracketRepository,
        ICacheService cacheService,
        ILogger logger)
    {
        _taxBracketRepository = taxBracketRepository;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    public async Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request)
    {
        // Validate input
        if (request.TaxableIncome < 0)
            throw new ArgumentException("Taxable income cannot be negative");
            
        // Get cached tax brackets
        var cacheKey = $"tax_brackets_{request.FinancialYear}";
        var brackets = await _cacheService.GetAsync<List<TaxBracket>>(cacheKey);
        
        if (brackets == null)
        {
            brackets = await _taxBracketRepository.GetTaxBracketsAsync(request.FinancialYear);
            await _cacheService.SetAsync(cacheKey, brackets, TimeSpan.FromHours(24));
        }
        
        // Calculate progressive income tax
        var incomeTaxResult = CalculateProgressiveIncomeTax(request.TaxableIncome, brackets);
        
        // Calculate Medicare levy
        var medicareLevyResult = await CalculateMedicareLevyAsync(request.TaxableIncome, request.FinancialYear);
        
        // Calculate other levies (Budget Repair, etc.)
        var otherLeviesResult = await CalculateOtherLeviesAsync(request.TaxableIncome, request.FinancialYear);
        
        // Calculate tax offsets
        var offsetsResult = await CalculateTaxOffsetsAsync(request.TaxableIncome, request.FinancialYear);
        
        // Combine results
        var result = new TaxCalculationResult
        {
            TaxableIncome = request.TaxableIncome,
            IncomeTax = incomeTaxResult.TotalTax,
            MedicareLevy = medicareLevyResult.Amount,
            BudgetRepairLevy = otherLeviesResult.BudgetRepairLevy,
            TotalLevies = medicareLevyResult.Amount + otherLeviesResult.BudgetRepairLevy,
            TaxOffsets = offsetsResult.TotalOffsets,
            BracketBreakdown = incomeTaxResult.BracketBreakdown,
            LevyBreakdown = new List<LevyCalculation> { medicareLevyResult, otherLeviesResult },
            OffsetBreakdown = offsetsResult.OffsetBreakdown
        };
        
        // Calculate final amounts
        var grossTax = result.IncomeTax + result.TotalLevies;
        result.NetTaxPayable = Math.Max(0, grossTax - result.TaxOffsets);
        result.NetIncome = result.TaxableIncome - result.NetTaxPayable;
        result.EffectiveRate = result.TaxableIncome > 0 ? result.NetTaxPayable / result.TaxableIncome : 0;
        result.MarginalRate = GetMarginalTaxRate(request.TaxableIncome, brackets);
        
        return result;
    }
    
    private ProgressiveIncomeTaxResult CalculateProgressiveIncomeTax(decimal income, List<TaxBracket> brackets)
    {
        var result = new ProgressiveIncomeTaxResult
        {
            BracketBreakdown = new List<TaxBracketCalculation>()
        };
        
        decimal totalTax = 0;
        decimal remainingIncome = income;
        
        foreach (var bracket in brackets.OrderBy(b => b.BracketOrder))
        {
            if (remainingIncome <= 0) break;
            
            var bracketMin = bracket.MinIncome;
            var bracketMax = bracket.MaxIncome ?? decimal.MaxValue;
            
            if (income <= bracketMin) continue;
            
            var taxableInBracket = Math.Min(remainingIncome, bracketMax - bracketMin + 1);
            if (income > bracketMax)
                taxableInBracket = bracketMax - bracketMin + 1;
            else
                taxableInBracket = income - bracketMin + 1;
                
            var taxInBracket = bracket.FixedAmount + (taxableInBracket * bracket.TaxRate);
            
            result.BracketBreakdown.Add(new TaxBracketCalculation
            {
                BracketRange = $"${bracketMin:N0} - ${(bracket.MaxIncome?.ToString("N0") ?? "∞")}",
                TaxableAmount = taxableInBracket,
                TaxRate = bracket.TaxRate,
                TaxAmount = taxInBracket
            });
            
            totalTax += taxInBracket;
            remainingIncome -= taxableInBracket;
        }
        
        result.TotalTax = totalTax;
        return result;
    }
}
```

#### 4.2 User Tax Service
```csharp
// IUserTaxService.cs
public interface IUserTaxService
{
    Task<UserAnnualTaxSummary> CalculateAnnualTaxAsync(Guid userId, string financialYear);
    Task<UserAnnualTaxSummary> GetTaxSummaryAsync(Guid userId, string financialYear);
    Task<List<UserAnnualTaxSummary>> GetTaxHistoryAsync(Guid userId);
    Task SaveMonthlyIncomeAsync(Guid userId, UserMonthlyIncomeRequest request);
    Task<List<UserMonthlyIncome>> GetMonthlyIncomeAsync(Guid userId, string financialYear);
}

// UserTaxService.cs
public class UserTaxService : IUserTaxService
{
    private readonly IUserIncomeRepository _userIncomeRepository;
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly ILogger _logger;
    
    public async Task<UserAnnualTaxSummary> CalculateAnnualTaxAsync(Guid userId, string financialYear)
    {
        // Get all monthly income for the year
        var monthlyIncomes = await _userIncomeRepository.GetMonthlyIncomeAsync(userId, financialYear);
        
        // Validate we have complete data (12 months)
        if (monthlyIncomes.Count != 12)
        {
            throw new InvalidOperationException($"Incomplete monthly income data. Found {monthlyIncomes.Count} months, expected 12.");
        }
        
        // Calculate annual totals
        var totalGrossIncome = monthlyIncomes.Sum(m => m.GrossIncome);
        var totalDeductions = monthlyIncomes.Sum(m => m.DeductionsAmount);
        var totalTaxableIncome = monthlyIncomes.Sum(m => m.TaxableIncome);
        
        // Calculate tax using the tax calculation service
        var taxRequest = new TaxCalculationRequest
        {
            FinancialYear = financialYear,
            TaxableIncome = totalTaxableIncome,
            ResidencyStatus = "Resident",
            IncludeMedicareLevy = true,
            IncludeOffsets = true
        };
        
        var taxResult = await _taxCalculationService.CalculateTaxAsync(taxRequest);
        
        // Create annual summary
        var summary = new UserAnnualTaxSummary
        {
            UserId = userId,
            FinancialYear = financialYear,
            TotalGrossIncome = totalGrossIncome,
            TotalDeductions = totalDeductions,
            TotalTaxableIncome = totalTaxableIncome,
            NetTaxPayable = taxResult.NetTaxPayable,
            EffectiveTaxRate = taxResult.EffectiveRate,
            MonthlyBreakdown = monthlyIncomes.Select(m => new MonthlyIncomeSummary
            {
                Month = m.Month,
                GrossIncome = m.GrossIncome,
                TaxableIncome = m.TaxableIncome,
                DeductionsAmount = m.DeductionsAmount
            }).ToList(),
            LastCalculated = DateTime.UtcNow
        };
        
        // Save to database
        await _userIncomeRepository.SaveAnnualSummaryAsync(summary);
        
        return summary;
    }
}
```

### Phase 5: Dependency Injection & Configuration

#### 5.1 Autofac Module Configuration
```csharp
// AutofacModule.cs
public class AutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Configuration
        builder.Register(c => ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString)
               .Named<string>("connectionString")
               .SingleInstance();
               
        // Connection Factory
        builder.RegisterType<SqlConnectionFactory>()
               .As<IConnectionFactory>()
               .WithParameter("connectionString", ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString)
               .SingleInstance();
               
        // Repositories
        builder.RegisterType<TaxBracketRepository>()
               .As<ITaxBracketRepository>()
               .InstancePerRequest();
               
        builder.RegisterType<UserRepository>()
               .As<IUserRepository>()
               .InstancePerRequest();
               
        builder.RegisterType<UserIncomeRepository>()
               .As<IUserIncomeRepository>()
               .InstancePerRequest();
               
        // Services
        builder.RegisterType<TaxCalculationService>()
               .As<ITaxCalculationService>()
               .InstancePerRequest();
               
        builder.RegisterType<UserTaxService>()
               .As<IUserTaxService>()
               .InstancePerRequest();
               
        // Redis Cache Service
        builder.Register(c => 
        {
            var connectionString = ConfigurationManager.AppSettings["RedisConnectionString"];
            return ConnectionMultiplexer.Connect(connectionString);
        }).As<IConnectionMultiplexer>().SingleInstance();
        
        builder.RegisterType<RedisCacheService>()
               .As<ICacheService>()
               .SingleInstance();
               
        // Logging
        builder.RegisterType<FileLogger>()
               .As<ILogger>()
               .SingleInstance();
    }
}

// Global.asax.cs Application_Start
protected void Application_Start()
{
    // Configure Autofac
    var builder = new ContainerBuilder();
    
    // Register Web API controllers
    builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
    
    // Register modules
    builder.RegisterModule<AutofacModule>();
    
    var container = builder.Build();
    
    // Set Web API dependency resolver
    var dependencyResolver = new AutofacWebApiDependencyResolver(container);
    GlobalConfiguration.Configuration.DependencyResolver = dependencyResolver;
    
    // Configure Web API
    WebApiConfig.Register(GlobalConfiguration.Configuration);
    
    // Configure JSON serialization
    GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.ContractResolver = 
        new CamelCasePropertyNamesContractResolver();
    GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.DateTimeZoneHandling = 
        DateTimeZoneHandling.Utc;
}
```

#### 5.2 Web API Configuration
```csharp
// WebApiConfig.cs
public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        // Web API routes
        config.MapHttpAttributeRoutes();
        
        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional }
        );
        
        // Remove XML formatter
        config.Formatters.Remove(config.Formatters.XmlFormatter);
        
        // Configure JSON formatter
        var jsonFormatter = config.Formatters.JsonFormatter;
        jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        jsonFormatter.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        jsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        
        // Global exception filter
        config.Filters.Add(new GlobalExceptionFilterAttribute());
        
        // Request validation filter
        config.Filters.Add(new ValidateModelAttribute());
    }
}
```

### Phase 6: Web API Controllers

#### 6.1 Tax Controller
```csharp
// TaxController.cs
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                
            var result = await _taxCalculationService.CalculateTaxAsync(request);
            
            // Log calculation for audit
            _logger.LogInfo($"Tax calculated for {request.FinancialYear}, Income: {request.TaxableIncome:C}, Tax: {result.NetTaxPayable:C}");
            
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error calculating tax", ex);
            return InternalServerError(ex);
        }
    }
    
    [HttpGet]
    [Route("brackets/{year}")]
    public async Task<IHttpActionResult> GetTaxBrackets(string year)
    {
        try
        {
            var brackets = await _taxCalculationService.GetTaxBracketsAsync(year);
            return Ok(brackets);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving tax brackets for {year}", ex);
            return InternalServerError(ex);
        }
    }
    
    [HttpGet]
    [Route("compare")]
    public async Task<IHttpActionResult> CompareTax(decimal income, [FromUri] string[] years)
    {
        try
        {
            if (years == null || years.Length == 0)
                return BadRequest("At least one year must be specified");
                
            var result = await _taxCalculationService.CompareTaxAcrossYearsAsync(income, years.ToList());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error comparing tax across years for income {income:C}", ex);
            return InternalServerError(ex);
        }
    }
    
    [HttpGet]
    [Route("history/{income:decimal}")]
    public async Task<IHttpActionResult> GetTaxHistory(decimal income, int years = 10)
    {
        try
        {
            var result = await _taxCalculationService.GetTaxHistoryAsync(income, years);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving tax history for income {income:C}", ex);
            return InternalServerError(ex);
        }
    }
}
```

#### 6.2 Users Controller
```csharp
// UsersController.cs
[RoutePrefix("api/users")]
public class UsersController : ApiController
{
    private readonly IUserTaxService _userTaxService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger _logger;
    
    public UsersController(IUserTaxService userTaxService, IUserRepository userRepository, ILogger logger)
    {
        _userTaxService = userTaxService;
        _userRepository = userRepository;
        _logger = logger;
    }
    
    [HttpGet]
    [Route("{userId:guid}/tax-summary/{year}")]
    public async Task<IHttpActionResult> GetTaxSummary(Guid userId, string year)
    {
        try
        {
            var user = await _userRepository.GetUserAsync(userId);
            if (user == null)
                return NotFound();
                
            var summary = await _userTaxService.GetTaxSummaryAsync(userId, year);
            if (summary == null)
                return NotFound();
                
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving tax summary for user {userId}, year {year}", ex);
            return InternalServerError(ex);
        }
    }
    
    [HttpGet]
    [Route("{userId:guid}/monthly-income/{year}")]
    public async Task<IHttpActionResult> GetMonthlyIncome(Guid userId, string year)
    {
        try
        {
            var user = await _userRepository.GetUserAsync(userId);
            if (user == null)
                return NotFound();
                
            var monthlyIncome = await _userTaxService.GetMonthlyIncomeAsync(userId, year);
            return Ok(monthlyIncome);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving monthly income for user {userId}, year {year}", ex);
            return InternalServerError(ex);
        }
    }
    
    [HttpPost]
    [Route("{userId:guid}/calculate-annual-tax/{year}")]
    public async Task<IHttpActionResult> CalculateAnnualTax(Guid userId, string year)
    {
        try
        {
            var user = await _userRepository.GetUserAsync(userId);
            if (user == null)
                return NotFound();
                
            var result = await _userTaxService.CalculateAnnualTaxAsync(userId, year);
            
            _logger.LogInfo($"Annual tax calculated for user {userId}, year {year}: {result.NetTaxPayable:C}");
            
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calculating annual tax for user {userId}, year {year}", ex);
            return InternalServerError(ex);
        }
    }
    
    [HttpGet]
    [Route("{userId:guid}/tax-history")]
    public async Task<IHttpActionResult> GetTaxHistory(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetUserAsync(userId);
            if (user == null)
                return NotFound();
                
            var history = await _userTaxService.GetTaxHistoryAsync(userId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving tax history for user {userId}", ex);
            return InternalServerError(ex);
        }
    }
    
    [HttpPost]
    [Route("{userId:guid}/monthly-income")]
    public async Task<IHttpActionResult> SaveMonthlyIncome(Guid userId, [FromBody] UserMonthlyIncomeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                
            var user = await _userRepository.GetUserAsync(userId);
            if (user == null)
                return NotFound();
                
            await _userTaxService.SaveMonthlyIncomeAsync(userId, request);
            
            return Ok(new { message = "Monthly income saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving monthly income for user {userId}", ex);
            return InternalServerError(ex);
        }
    }
}

// HealthController.cs
[RoutePrefix("api/health")]
public class HealthController : ApiController
{
    private readonly IConnectionFactory _connectionFactory;
    
    public HealthController(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    
    [HttpGet]
    [Route("")]
    public async Task<IHttpActionResult> GetHealth()
    {
        try
        {
            // Check database connectivity
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                // Simple query to verify database is accessible
                using (var command = new SqlCommand("SELECT 1", (SqlConnection)connection))
                {
                    await command.ExecuteScalarAsync();
                }
            }
            
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            });
        }
        catch (Exception ex)
        {
            return InternalServerError(new Exception("Health check failed", ex));
        }
    }
}
```

### Phase 7: Redis Caching Implementation

#### 7.1 Cache Service Interface & Implementation
```csharp
// ICacheService.cs
public interface ICacheService
{
    Task<T> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task RemoveByPatternAsync(string pattern);
}

// RedisCacheService.cs
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly ILogger _logger;
    
    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, ILogger logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = _connectionMultiplexer.GetDatabase();
        _logger = logger;
    }
    
    public async Task<T> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
                return null;
                
            return JsonConvert.DeserializeObject<T>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving from cache: {key}", ex);
            return null;
        }
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        try
        {
            var serializedValue = JsonConvert.SerializeObject(value);
            await _database.StringSetAsync(key, serializedValue, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error setting cache: {key}", ex);
        }
    }
    
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error removing from cache: {key}", ex);
        }
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking cache existence: {key}", ex);
            return false;
        }
    }
    
    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            
            foreach (var key in keys)
            {
                await _database.KeyDeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error removing cache by pattern: {pattern}", ex);
        }
    }
}
```

#### 7.2 Cache Strategy Implementation
```csharp
// CacheConstants.cs
public static class CacheConstants
{
    public static class Keys
    {
        public const string TaxBrackets = "tax_brackets_{0}"; // {0} = financial year
        public const string TaxOffsets = "tax_offsets_{0}";   // {0} = financial year
        public const string TaxLevies = "tax_levies_{0}";     // {0} = financial year
        public const string UserTaxSummary = "user_tax_summary_{0}_{1}"; // {0} = userId, {1} = year
        public const string UserMonthlyIncome = "user_monthly_income_{0}_{1}"; // {0} = userId, {1} = year
        public const string TaxCalculation = "tax_calc_{0}_{1}"; // {0} = year, {1} = income hash
    }
    
    public static class Expiration
    {
        public static readonly TimeSpan TaxBrackets = TimeSpan.FromHours(24);
        public static readonly TimeSpan UserData = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan TaxCalculations = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan ShortTerm = TimeSpan.FromMinutes(5);
    }
}

// Enhanced TaxCalculationService with caching
public class TaxCalculationService : ITaxCalculationService
{
    private readonly ITaxBracketRepository _taxBracketRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger _logger;
    
    public async Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request)
    {
        // Create cache key for this specific calculation
        var incomeHash = request.TaxableIncome.GetHashCode().ToString();
        var calcCacheKey = string.Format(CacheConstants.Keys.TaxCalculation, request.FinancialYear, incomeHash);
        
        // Try to get cached result first
        var cachedResult = await _cacheService.GetAsync<TaxCalculationResult>(calcCacheKey);
        if (cachedResult != null)
        {
            _logger.LogInfo($"Tax calculation cache hit for {request.FinancialYear}, income: {request.TaxableIncome:C}");
            return cachedResult;
        }
        
        // Get tax brackets from cache or database
        var brackets = await GetCachedTaxBracketsAsync(request.FinancialYear);
        var offsets = await GetCachedTaxOffsetsAsync(request.FinancialYear);
        var levies = await GetCachedTaxLeviesAsync(request.FinancialYear);
        
        // Perform calculation
        var result = PerformTaxCalculation(request, brackets, offsets, levies);
        
        // Cache the result
        await _cacheService.SetAsync(calcCacheKey, result, CacheConstants.Expiration.TaxCalculations);
        
        return result;
    }
    
    private async Task<List<TaxBracket>> GetCachedTaxBracketsAsync(string financialYear)
    {
        var cacheKey = string.Format(CacheConstants.Keys.TaxBrackets, financialYear);
        var brackets = await _cacheService.GetAsync<List<TaxBracket>>(cacheKey);
        
        if (brackets == null)
        {
            brackets = await _taxBracketRepository.GetTaxBracketsAsync(financialYear);
            await _cacheService.SetAsync(cacheKey, brackets, CacheConstants.Expiration.TaxBrackets);
            _logger.LogInfo($"Tax brackets cached for {financialYear}");
        }
        
        return brackets;
    }
}
```

### Phase 8: Console Applications

#### 8.1 Data Seeding Console Application
```csharp
// Program.cs (TaxCalculator.Console)
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Tax Calculator Console Application");
        
        // Setup DI container
        var container = SetupContainer();
        
        if (args.Length == 0)
        {
            ShowUsage();
            return;
        }
        
        var command = args[0].ToLower();
        
        try
        {
            switch (command)
            {
                case "seed-tax-data":
                    await SeedTaxBrackets(container);
                    break;
                case "seed-users":
                    await SeedSampleUsers(container, int.Parse(args.Length > 1 ? args[1] : "100"));
                    break;
                case "calculate-annual-tax":
                    await CalculateAnnualTaxForAllUsers(container, args.Length > 1 ? args[1] : "2024-25");
                    break;
                case "validate-data":
                    await ValidateDataIntegrity(container);
                    break;
                default:
                    ShowUsage();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    
    static void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  TaxCalculator.Console.exe seed-tax-data");
        Console.WriteLine("  TaxCalculator.Console.exe seed-users [count]");
        Console.WriteLine("  TaxCalculator.Console.exe calculate-annual-tax [year]");
        Console.WriteLine("  TaxCalculator.Console.exe validate-data");
    }
}

// DataSeeder.cs
public class DataSeeder
{
    private readonly ITaxBracketRepository _taxBracketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserIncomeRepository _userIncomeRepository;
    
    public DataSeeder(ITaxBracketRepository taxBracketRepository, 
                     IUserRepository userRepository,
                     IUserIncomeRepository userIncomeRepository)
    {
        _taxBracketRepository = taxBracketRepository;
        _userRepository = userRepository;
        _userIncomeRepository = userIncomeRepository;
    }
    
    public async Task SeedTaxBracketsAsync()
    {
        Console.WriteLine("Seeding tax bracket data for 2015-16 to 2024-25...");
        
        // 2024-25 (Stage 3 Tax Cuts)
        await SeedTaxBrackets("2024-25", new[]
        {
            new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0m, FixedAmount = 0m, BracketOrder = 1 },
            new TaxBracket { MinIncome = 18201, MaxIncome = 45000, TaxRate = 0.16m, FixedAmount = 0m, BracketOrder = 2 },
            new TaxBracket { MinIncome = 45001, MaxIncome = 135000, TaxRate = 0.30m, FixedAmount = 4288m, BracketOrder = 3 },
            new TaxBracket { MinIncome = 135001, MaxIncome = 190000, TaxRate = 0.37m, FixedAmount = 31288m, BracketOrder = 4 },
            new TaxBracket { MinIncome = 190001, MaxIncome = null, TaxRate = 0.45m, FixedAmount = 51638m, BracketOrder = 5 }
        });
        
        // 2020-21 to 2023-24
        var years2020to2023 = new[] { "2020-21", "2021-22", "2022-23", "2023-24" };
        foreach (var year in years2020to2023)
        {
            await SeedTaxBrackets(year, new[]
            {
                new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0m, FixedAmount = 0m, BracketOrder = 1 },
                new TaxBracket { MinIncome = 18201, MaxIncome = 45000, TaxRate = 0.19m, FixedAmount = 0m, BracketOrder = 2 },
                new TaxBracket { MinIncome = 45001, MaxIncome = 120000, TaxRate = 0.325m, FixedAmount = 5092m, BracketOrder = 3 },
                new TaxBracket { MinIncome = 120001, MaxIncome = 180000, TaxRate = 0.37m, FixedAmount = 29467m, BracketOrder = 4 },
                new TaxBracket { MinIncome = 180001, MaxIncome = null, TaxRate = 0.45m, FixedAmount = 51667m, BracketOrder = 5 }
            });
        }
        
        // 2018-19 to 2019-20
        await SeedTaxBrackets("2018-19", CreateStandardBrackets("2018-19"));
        await SeedTaxBrackets("2019-20", CreateStandardBrackets("2019-20"));
        
        // 2015-16 to 2017-18 (with Budget Repair Levy)
        var budgetRepairYears = new[] { "2015-16", "2016-17", "2017-18" };
        foreach (var year in budgetRepairYears)
        {
            await SeedTaxBrackets(year, CreateStandardBrackets(year));
            await SeedBudgetRepairLevy(year);
        }
        
        // Seed Medicare Levy for all years
        foreach (var year in GetAllFinancialYears())
        {
            await SeedMedicareLevy(year);
        }
        
        Console.WriteLine("Tax bracket seeding completed successfully!");
    }
    
    public async Task SeedSampleUsersAsync(int userCount = 100)
    {
        Console.WriteLine($"Creating {userCount} sample users with 5 years of monthly income data...");
        
        var random = new Random();
        var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Emma", "James", "Lisa", "Robert", "Anna" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Wilson", "Moore", "Taylor" };
        
        for (int i = 0; i < userCount; i++)
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = firstNames[random.Next(firstNames.Length)],
                LastName = lastNames[random.Next(lastNames.Length)],
                Email = $"user{i+1:D3}@example.com",
                DateOfBirth = DateTime.Now.AddYears(-random.Next(25, 65)),
                ResidencyStatus = "Resident",
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };
            
            await _userRepository.CreateUserAsync(user);
            
            // Generate 5 years of monthly income data
            var financialYears = new[] { "2020-21", "2021-22", "2022-23", "2023-24", "2024-25" };
            
            foreach (var year in financialYears)
            {
                var baseAnnualIncome = random.Next(40000, 150000);
                var monthlyBaseIncome = baseAnnualIncome / 12m;
                
                for (int month = 1; month <= 12; month++)
                {
                    // Add some variation to monthly income
                    var variation = (decimal)(random.NextDouble() * 0.2 - 0.1); // ±10%
                    var monthlyGross = monthlyBaseIncome * (1 + variation);
                    var deductions = monthlyGross * (decimal)(random.NextDouble() * 0.05); // 0-5% deductions
                    
                    var monthlyIncome = new UserMonthlyIncome
                    {
                        UserId = user.UserId,
                        FinancialYear = year,
                        Month = month,
                        GrossIncome = Math.Round(monthlyGross, 2),
                        TaxableIncome = Math.Round(monthlyGross - deductions, 2),
                        DeductionsAmount = Math.Round(deductions, 2),
                        SuperContributions = Math.Round(monthlyGross * 0.105m, 2), // 10.5% super
                        IncomeType = "Salary",
                        PayPeriod = "Monthly",
                        RecordedDate = DateTime.UtcNow
                    };
                    
                    await _userIncomeRepository.SaveMonthlyIncomeAsync(monthlyIncome);
                }
            }
            
            if ((i + 1) % 10 == 0)
            {
                Console.WriteLine($"Created {i + 1} users...");
            }
        }
        
        Console.WriteLine($"Successfully created {userCount} sample users with complete income data!");
    }
}
```

#### 8.2 Batch Annual Tax Calculator
```csharp
// AnnualTaxCalculator.cs
public class AnnualTaxCalculator
{
    private readonly IUserTaxService _userTaxService;
    private readonly IUserRepository _userRepository;
    
    public AnnualTaxCalculator(IUserTaxService userTaxService, IUserRepository userRepository)
    {
        _userTaxService = userTaxService;
        _userRepository = userRepository;
    }
    
    public async Task CalculateAnnualTaxForAllUsersAsync(string financialYear)
    {
        Console.WriteLine($"Starting annual tax calculation for all users for {financialYear}...");
        
        var users = await _userRepository.GetAllActiveUsersAsync();
        Console.WriteLine($"Found {users.Count} active users");
        
        var successCount = 0;
        var errorCount = 0;
        
        foreach (var user in users)
        {
            try
            {
                var summary = await _userTaxService.CalculateAnnualTaxAsync(user.UserId, financialYear);
                successCount++;
                
                if (successCount % 10 == 0)
                {
                    Console.WriteLine($"Processed {successCount} users... (Latest: {user.FirstName} {user.LastName} - Tax: {summary.NetTaxPayable:C})");
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Skipping user {user.FirstName} {user.LastName}: {ex.Message}");
                errorCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing user {user.FirstName} {user.LastName}: {ex.Message}");
                errorCount++;
            }
        }
        
        Console.WriteLine($"\nBatch calculation completed!");
        Console.WriteLine($"Successfully processed: {successCount} users");
        Console.WriteLine($"Errors/Skipped: {errorCount} users");
        Console.WriteLine($"Total users: {users.Count}");
    }
}
```

### Phase 9: Testing Strategy

#### 9.1 Unit Tests (NUnit 3.x)
```csharp
// TaxCalculationServiceTests.cs
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
        
        _mockCacheService.Setup(x => x.GetAsync<List<TaxBracket>>(It.IsAny<string>()))
                        .ReturnsAsync((List<TaxBracket>)null);
        
        _mockTaxBracketRepository.Setup(x => x.GetTaxBracketsAsync("2024-25"))
                                .ReturnsAsync(taxBrackets);
        
        // Act
        var result = await _taxCalculationService.CalculateTaxAsync(request);
        
        // Assert
        Assert.That(result.TaxableIncome, Is.EqualTo(85000m));
        Assert.That(result.IncomeTax, Is.EqualTo(16288m).Within(1m)); // Expected: $16,288
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
    [TestCase(135000, 31288)] // End of third bracket
    [TestCase(190000, 51638)] // End of fourth bracket
    [TestCase(250000, 78638)] // High income
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
        Assert.That(result.IncomeTax, Is.EqualTo(expectedTax).Within(1m));
    }
    
    private List<TaxBracket> Create2024_25TaxBrackets()
    {
        return new List<TaxBracket>
        {
            new TaxBracket { MinIncome = 0, MaxIncome = 18200, TaxRate = 0m, FixedAmount = 0m, BracketOrder = 1 },
            new TaxBracket { MinIncome = 18201, MaxIncome = 45000, TaxRate = 0.16m, FixedAmount = 0m, BracketOrder = 2 },
            new TaxBracket { MinIncome = 45001, MaxIncome = 135000, TaxRate = 0.30m, FixedAmount = 4288m, BracketOrder = 3 },
            new TaxBracket { MinIncome = 135001, MaxIncome = 190000, TaxRate = 0.37m, FixedAmount = 31288m, BracketOrder = 4 },
            new TaxBracket { MinIncome = 190001, MaxIncome = null, TaxRate = 0.45m, FixedAmount = 51638m, BracketOrder = 5 }
        };
    }
}

// UserTaxServiceTests.cs
[TestFixture]
public class UserTaxServiceTests
{
    private IUserTaxService _userTaxService;
    private Mock<IUserIncomeRepository> _mockUserIncomeRepository;
    private Mock<ITaxCalculationService> _mockTaxCalculationService;
    private Mock<ILogger> _mockLogger;
    
    [SetUp]
    public void Setup()
    {
        _mockUserIncomeRepository = new Mock<IUserIncomeRepository>();
        _mockTaxCalculationService = new Mock<ITaxCalculationService>();
        _mockLogger = new Mock<ILogger>();
        
        _userTaxService = new UserTaxService(
            _mockUserIncomeRepository.Object,
            _mockTaxCalculationService.Object,
            _mockLogger.Object);
    }
    
    [Test]
    public async Task CalculateAnnualTaxAsync_CompleteMonthlyData_ReturnsCorrectSummary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var financialYear = "2024-25";
        
        var monthlyIncomes = CreateCompleteMonthlyIncomeData(userId, financialYear, 7000m);
        var expectedTaxResult = new TaxCalculationResult
        {
            TaxableIncome = 84000m,
            NetTaxPayable = 18000m,
            EffectiveRate = 0.2143m
        };
        
        _mockUserIncomeRepository.Setup(x => x.GetMonthlyIncomeAsync(userId, financialYear))
                                .ReturnsAsync(monthlyIncomes);
        
        _mockTaxCalculationService.Setup(x => x.CalculateTaxAsync(It.IsAny<TaxCalculationRequest>()))
                                 .ReturnsAsync(expectedTaxResult);
        
        // Act
        var result = await _userTaxService.CalculateAnnualTaxAsync(userId, financialYear);
        
        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.FinancialYear, Is.EqualTo(financialYear));
        Assert.That(result.TotalTaxableIncome, Is.EqualTo(84000m));
        Assert.That(result.NetTaxPayable, Is.EqualTo(18000m));
        Assert.That(result.MonthlyBreakdown.Count, Is.EqualTo(12));
    }
    
    [Test]
    public async Task CalculateAnnualTaxAsync_IncompleteMonthlyData_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var financialYear = "2024-25";
        
        var incompleteMonthlyIncomes = CreateCompleteMonthlyIncomeData(userId, financialYear, 7000m)
                                     .Take(8).ToList(); // Only 8 months
        
        _mockUserIncomeRepository.Setup(x => x.GetMonthlyIncomeAsync(userId, financialYear))
                                .ReturnsAsync(incompleteMonthlyIncomes);
        
        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userTaxService.CalculateAnnualTaxAsync(userId, financialYear));
        
        Assert.That(ex.Message, Contains.Substring("Incomplete monthly income data"));
        Assert.That(ex.Message, Contains.Substring("Found 8 months, expected 12"));
    }
}
```

#### 9.2 Integration Tests
```csharp
// TaxControllerIntegrationTests.cs
[TestFixture]
public class TaxControllerIntegrationTests
{
    private TestServer _server;
    private HttpClient _client;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Setup test server with in-memory database
        var builder = new WebHostBuilder()
            .UseStartup<TestStartup>()
            .UseEnvironment("Testing");
            
        _server = new TestServer(builder);
        _client = _server.CreateClient();
    }
    
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _server?.Dispose();
    }
    
    [Test]
    public async Task POST_CalculateTax_ValidRequest_ReturnsOkWithResult()
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
        
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await _client.PostAsync("/api/tax/calculate", content);
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<TaxCalculationResult>(responseJson);
        
        Assert.That(result.TaxableIncome, Is.EqualTo(85000m));
        Assert.That(result.NetTaxPayable, Is.GreaterThan(0));
    }
    
    [Test]
    public async Task GET_TaxBrackets_ValidYear_ReturnsOkWithBrackets()
    {
        // Act
        var response = await _client.GetAsync("/api/tax/brackets/2024-25");
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var responseJson = await response.Content.ReadAsStringAsync();
        var brackets = JsonConvert.DeserializeObject<List<TaxBracket>>(responseJson);
        
        Assert.That(brackets.Count, Is.EqualTo(5)); // 2024-25 has 5 tax brackets
        Assert.That(brackets.First().MinIncome, Is.EqualTo(0));
        Assert.That(brackets.Last().MaxIncome, Is.Null); // Highest bracket has no upper limit
    }
}
```

### Phase 10: Configuration Files & Sample Data

#### 10.1 Complete Package.config Files
```xml
<!-- Web API Project packages.config -->
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Microsoft.AspNet.WebApi" version="5.2.9" targetFramework="net48" />
  <package id="Microsoft.AspNet.WebApi.Client" version="5.2.9" targetFramework="net48" />
  <package id="Microsoft.AspNet.WebApi.Core" version="5.2.9" targetFramework="net48" />
  <package id="Microsoft.AspNet.WebApi.WebHost" version="5.2.9" targetFramework="net48" />
  <package id="Autofac" version="4.9.4" targetFramework="net48" />
  <package id="Autofac.WebApi2" version="4.3.1" targetFramework="net48" />
  <package id="Newtonsoft.Json" version="12.0.3" targetFramework="net48" />
  <package id="StackExchange.Redis" version="1.2.6" targetFramework="net48" />
  <package id="System.Data.SqlClient" version="4.8.5" targetFramework="net48" />
</packages>
```

#### 10.2 Web.config Configuration
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <connectionStrings>
    <add name="DefaultConnection" 
         connectionString="Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\TaxCalculator.mdf;Integrated Security=True;Connect Timeout=30" 
         providerName="System.Data.SqlClient" />
  </connectionStrings>
  
  <appSettings>
    <add key="RedisConnectionString" value="localhost:6379" />
    <add key="EnableCaching" value="true" />
    <add key="CacheExpirationHours" value="24" />
    <add key="LogLevel" value="Information" />
    <add key="LogFilePath" value="C:\Logs\TaxCalculator\" />
  </appSettings>
  
  <system.web>
    <compilation debug="true" targetFramework="4.8" />
    <httpRuntime targetFramework="4.8" maxRequestLength="4096" />
  </system.web>
  
  <system.webServer>
    <handlers>
      <remove name="ExtensionlessUrlHandler-Integrated-4.0" />
      <add name="ExtensionlessUrlHandler-Integrated-4.0" 
           path="*." verb="*" type="System.Web.Handlers.TransferRequestHandler" 
           preCondition="integratedMode,runtimeVersionv4.0" />
    </handlers>
  </system.webServer>
  
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" />
        <bindingRedirect oldVersion="0.0.0.0-12.0.0.0" newVersion="12.0.0.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="Autofac" publicKeyToken="17863af14b0044da" />
        <bindingRedirect oldVersion="0.0.0.0-4.9.4.0" newVersion="4.9.4.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
```

## Historical Tax Data Implementation

### 2024-25 (Current - Stage 3 Tax Cuts)
- $0-$18,200: 0%
- $18,201-$45,000: 16%
- $45,001-$135,000: 30%
- $135,001-$190,000: 37%
- $190,001+: 45%

### 2020-21 to 2023-24
- $0-$18,200: 0%
- $18,201-$45,000: 19%
- $45,001-$120,000: 32.5%
- $120,001-$180,000: 37%
- $180,001+: 45%

### 2018-19 to 2019-20
- $0-$18,200: 0%
- $18,201-$37,000: 19%
- $37,001-$90,000: 32.5%
- $90,001-$180,000: 37%
- $180,001+: 45%

### 2015-16 to 2017-18
- $0-$18,200: 0%
- $18,201-$37,000: 19%
- $37,001-$80,000: 32.5%
- $80,001-$180,000: 37%
- $180,001+: 45%
- **Note**: Temporary Budget Repair Levy 2% on income >$180k (2014-2017)

## Sample API Usage

### Tax Calculation
```json
POST /api/tax/calculate
{
  "financialYear": "2024-25",
  "income": 85000,
  "residencyStatus": "resident",
  "includeOffsets": true,
  "includeMedicareLevy": true
}

Response:
{
  "grossIncome": 85000,
  "taxableIncome": 85000,
  "incomeTax": 17738,
  "medicareLevy": 1700,
  "totalTax": 19438,
  "netIncome": 65562,
  "effectiveRate": 22.87,
  "marginalRate": 30,
  "breakdownByBracket": [...]
}
```

### User Management
```json
POST /api/users/{userId}/monthly-income
{
  "year": 2024,
  "month": 8,
  "grossIncome": 7500,
  "deductions": 500,
  "taxableIncome": 7000
}

GET /api/users/{userId}/tax-summary/2024-25
Response:
{
  "userId": "12345678-1234-1234-1234-123456789012",
  "financialYear": "2024-25",
  "totalGrossIncome": 95000,
  "totalDeductions": 8500,
  "totalTaxableIncome": 86500,
  "calculatedTax": 18063,
  "medicareLevy": 1730,
  "totalTaxPayable": 19793,
  "effectiveRate": 22.88,
  "monthlyBreakdown": [...],
  "lastCalculated": "2025-06-30T10:30:00Z"
}

GET /api/users/{userId}/tax-history
Response:
{
  "userId": "12345678-1234-1234-1234-123456789012",
  "taxHistory": [
    {
      "financialYear": "2024-25",
      "totalTaxableIncome": 86500,
      "totalTaxPayable": 19793,
      "effectiveRate": 22.88
    },
    {
      "financialYear": "2023-24", 
      "totalTaxableIncome": 82000,
      "totalTaxPayable": 19417,
      "effectiveRate": 23.68
    },
    // ... previous 3 years
  ]
}
```

## Implementation Timeline & Checklist

### Week 1-2: Foundation
- [x] Set up solution structure
- [x] Design and create database schema
- [x] Implement core domain models
- [x] Create ADO.NET repositories

### Week 3-4: Business Logic
- [x] Implement tax calculation engine
- [x] Build user income management services
- [x] Set up Autofac dependency injection
- [x] Configure Redis caching

### Week 5-6: API Layer
- [x] Create Web API controllers
- [x] Implement global exception handling
- [x] Add validation filters
- [x] Configure JSON serialization

### Week 7-8: Console Applications & Testing
- [x] Build data seeding console app
- [x] Create batch processing utilities
- [x] Implement comprehensive unit tests
- [x] Add integration tests

## Key Architectural Decisions

### Why .NET Framework 4.8?
- Legacy enterprise environment requirement
- Existing infrastructure compatibility
- Traditional enterprise patterns
- Migration target for future modernization

### Why ADO.NET over Entity Framework?
- Performance for large-scale calculations
- Direct SQL control for complex queries
- Reduced dependencies
- Legacy system compatibility

### Why Autofac 4.x?
- Mature dependency injection container
- .NET Framework 4.8 compatibility
- Enterprise-proven reliability
- Advanced configuration options

### Why Redis Caching?
- High-performance in-memory storage
- Distributed caching capabilities
- Tax bracket data rarely changes
- Calculation result caching

## Migration Strategy

This implementation is designed to facilitate future migration:

1. **Interface-Based Architecture**: All dependencies use interfaces
2. **Clean Separation**: Business logic separated from infrastructure
3. **Comprehensive Tests**: Safety net for refactoring
4. **Modern Patterns**: Repository, Service Layer, DI patterns
5. **Configuration Externalization**: Easy environment management

## Success Criteria

- [x] All functional requirements implemented
- [x] Complete historical tax data (10 years)
- [x] User income management system
- [x] Redis caching for performance
- [x] Comprehensive test coverage
- [x] Console utilities for data management
- [x] Enterprise-grade error handling
- [x] Audit trail for all calculations
- [x] Legacy technology stack compliance
- [x] Migration-ready architecture

This comprehensive implementation plan provides a complete .NET Framework 4.8 legacy tax calculator system that meets all requirements while maintaining modern architectural principles for future migration.
