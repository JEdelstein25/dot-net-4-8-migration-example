# .NET Framework 4.8 to .NET 8 Migration Analysis

## Executive Summary

This analysis covers 10 projects in the TaxCalculator solution targeting .NET Framework 4.8. The migration to .NET 8 presents **MODERATE** complexity with several critical blockers requiring careful planning.

## NuGet Packages Analysis

### ✅ Compatible Packages (Direct Migration)

| Package | Current Version | .NET 8 Support | Recommendation |
|---------|----------------|----------------|----------------|
| **Autofac** | 4.9.4 | ✅ Excellent | Upgrade to 8.x.x for better performance |
| **Newtonsoft.Json** | 12.0.3 | ✅ Excellent | Consider migrating to System.Text.Json |
| **NUnit** | 3.13.3 | ✅ Excellent | Upgrade to 4.x.x |
| **Moq** | 4.16.1 | ✅ Excellent | Upgrade to 4.20.x |
| **Castle.Core** | 4.4.0 | ✅ Excellent | Auto-updated with Moq |
| **System.Data.SqlClient** | 4.8.5 | ✅ Compatible | Migrate to Microsoft.Data.SqlClient |

### ⚠️ Requires Major Changes

| Package | Current Version | Status | Migration Path |
|---------|----------------|--------|----------------|
| **Microsoft.AspNet.WebApi** | 5.2.9 | ❌ Not Compatible | Migrate to ASP.NET Core |
| **Autofac.WebApi2** | 4.3.1 | ❌ Not Compatible | Use Autofac.Extensions.DependencyInjection |
| **Microsoft.CodeDom.Providers** | 2.0.1 | ❌ Not Compatible | Remove - built into .NET 8 |
| **Microsoft.Web.Infrastructure** | 1.0.0.0 | ❌ Not Compatible | Remove - replaced by ASP.NET Core |

### 🆕 New Packages Required for .NET 8

```xml
<PackageReference Include="Microsoft.AspNetCore.App" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.1" />
<PackageReference Include="Autofac.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
```

## Framework-Specific API Analysis

### 🚨 Critical Blockers

#### 1. ASP.NET Web API → ASP.NET Core Migration
**Impact: HIGH** - Complete rewrite required
- **Files Affected**: TaxCalculator.Api project (6 files)
- **Current**: System.Web.Http controllers
- **Migration**: Convert to ASP.NET Core controllers

```csharp
// BEFORE (.NET Framework)
public class TaxController : ApiController
{
    [HttpGet]
    public IHttpActionResult Calculate(decimal income)
    {
        return Ok(result);
    }
}

// AFTER (.NET 8)
[ApiController]
[Route("api/[controller]")]
public class TaxController : ControllerBase
{
    [HttpGet]
    public ActionResult<TaxResult> Calculate(decimal income)
    {
        return Ok(result);
    }
}
```

#### 2. Configuration System Migration
**Impact: HIGH** - Complete configuration overhaul
- **Current**: Web.config/App.config with ConfigurationManager
- **Migration**: appsettings.json with IConfiguration

```csharp
// BEFORE (.NET Framework)
var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
var setting = ConfigurationManager.AppSettings["EnableCaching"];

// AFTER (.NET 8)
var connectionString = configuration.GetConnectionString("DefaultConnection");
var setting = configuration["EnableCaching"];
```

#### 3. Global.asax → Program.cs/Startup.cs
**Impact: MEDIUM** - Application startup migration
- **Current**: Global.asax.cs with Application_Start
- **Migration**: Program.cs with WebApplication builder

### ⚠️ Windows-Specific Dependencies

#### SQL Server LocalDB
**Impact: MEDIUM** - Development environment dependency
- **Current**: LocalDB with AttachDbFilename
- **Migration Options**:
  1. Keep LocalDB for development (Windows only)
  2. Migrate to SQL Server Express
  3. Use Docker SQL Server (cross-platform)

```xml
<!-- BEFORE -->
<connectionString>Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\TaxCalculator.mdf;Integrated Security=True;Connect Timeout=30</connectionString>

<!-- AFTER - Option 1: Keep LocalDB -->
"ConnectionStrings": {
  "DefaultConnection": "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=TaxCalculator;Integrated Security=true;TrustServerCertificate=true"
}

<!-- AFTER - Option 2: Docker SQL Server -->
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=TaxCalculator;User Id=sa;Password=YourPassword123;TrustServerCertificate=true"
}
```

## Project-by-Project Migration Strategy

### Phase 1: Core Libraries (Low Risk)
1. **TaxCalculator.Core** - ✅ Direct migration
2. **TaxCalculator.Infrastructure** - ✅ Direct migration 
3. **TaxCalculator.Services** - ⚠️ Requires configuration migration

### Phase 2: Data Layer (Medium Risk)
4. **TaxCalculator.Data** - ⚠️ Requires SqlClient migration

### Phase 3: Applications (High Risk)
5. **TaxCalculator.Api** - 🚨 Complete rewrite to ASP.NET Core
6. **TaxCalculator.Console** - ⚠️ Requires configuration migration
7. **TaxCalculator.StandaloneApi** - 🚨 Complete rewrite
8. **TaxCalculator.TestClient** - ⚠️ Minor changes

### Phase 4: Testing (Low-Medium Risk)
9. **TaxCalculator.Tests.Unit** - ⚠️ Update test frameworks
10. **ApiTestClient** - ⚠️ Minor HTTP client updates

## Configuration Migration Plan

### Web.config → appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=TaxCalculator;Integrated Security=true;TrustServerCertificate=true"
  },
  "AppSettings": {
    "RedisConnectionString": "localhost:6379",
    "EnableCaching": true,
    "CacheExpirationHours": 24
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "File": {
      "Path": "C:\\Logs\\TaxCalculator\\"
    }
  }
}
```

### Dependency Injection Migration

```csharp
// BEFORE (AutofacConfig.cs)
var builder = new ContainerBuilder();
builder.RegisterType<TaxCalculationService>().As<ITaxCalculationService>();
var container = builder.Build();

// AFTER (Program.cs)
builder.Services.AddScoped<ITaxCalculationService, TaxCalculationService>();
builder.Services.AddAutofac(); // If keeping Autofac
```

## Compatibility Risk Matrix

| Component | Risk Level | Effort (Days) | Breaking Changes |
|-----------|------------|---------------|-----------------|
| **TaxCalculator.Core** | 🟢 Low | 1 | Minimal |
| **TaxCalculator.Data** | 🟡 Medium | 2-3 | Connection string format |
| **TaxCalculator.Services** | 🟡 Medium | 2-3 | Configuration injection |
| **TaxCalculator.Api** | 🔴 High | 8-10 | Complete rewrite |
| **TaxCalculator.Console** | 🟡 Medium | 2 | Configuration system |
| **TaxCalculator.StandaloneApi** | 🔴 High | 5-7 | HTTP server migration |
| **Tests** | 🟡 Medium | 3-4 | Framework updates |

## Migration Blockers & Solutions

### 🚨 Blocker 1: Web API Dependency
**Problem**: System.Web.Http not available in .NET 8
**Solution**: Complete migration to ASP.NET Core
**Timeline**: 2-3 weeks

### 🚨 Blocker 2: Configuration System
**Problem**: ConfigurationManager not recommended in .NET 8
**Solution**: Implement IConfiguration pattern
**Timeline**: 1 week

### 🚨 Blocker 3: Global.asax Application Lifecycle
**Problem**: Global.asax doesn't exist in ASP.NET Core
**Solution**: Migrate to Program.cs startup
**Timeline**: 3-5 days

## Recommended Migration Path

### Step 1: Prepare Environment (1 week)
1. Install .NET 8 SDK
2. Update Visual Studio to latest
3. Create new .NET 8 solution structure
4. Set up CI/CD for dual framework support

### Step 2: Migrate Core Libraries (1 week)
1. Convert TaxCalculator.Core to .NET 8
2. Convert TaxCalculator.Infrastructure to .NET 8
3. Update TaxCalculator.Services with DI pattern
4. Update TaxCalculator.Data to use Microsoft.Data.SqlClient

### Step 3: Migrate Console Apps (1 week)
1. Convert TaxCalculator.Console to .NET 8
2. Implement new configuration system
3. Update TaxCalculator.TestClient

### Step 4: Rewrite Web APIs (2-3 weeks)
1. Create new ASP.NET Core project for TaxCalculator.Api
2. Migrate controllers and routing
3. Implement middleware and DI
4. Convert TaxCalculator.StandaloneApi to minimal APIs

### Step 5: Update Tests (1 week)
1. Migrate test projects to .NET 8
2. Update test frameworks
3. Ensure compatibility with new APIs

### Step 6: Final Integration (1 week)
1. End-to-end testing
2. Performance benchmarking
3. Documentation updates

## Performance & Feature Benefits

### .NET 8 Advantages
- **40-60% better performance** for API endpoints
- **Native AOT compilation** for faster startup
- **Better memory management** with GC improvements
- **Cross-platform deployment** (Windows, Linux, macOS)
- **Container optimization** for Docker deployments
- **Built-in health checks** and metrics

### Potential Challenges
- **Learning curve** for ASP.NET Core patterns
- **Deployment changes** from IIS to Kestrel
- **Configuration complexity** increase
- **Third-party package** compatibility verification needed

## Estimated Timeline
- **Total Migration Time**: 6-8 weeks
- **Testing & Validation**: 2 weeks
- **Documentation & Training**: 1 week
- **Total Project Duration**: 9-11 weeks

## Success Criteria
✅ All tests passing on .NET 8  
✅ API functionality preserved  
✅ Performance meets or exceeds .NET Framework baseline  
✅ Cross-platform deployment capability  
✅ Maintainable configuration system  
✅ No regression in existing features  

## Next Steps
1. **Validate migration approach** with stakeholders
2. **Set up development environment** for .NET 8
3. **Create proof of concept** with TaxCalculator.Core
4. **Begin systematic migration** following the recommended path
5. **Establish continuous integration** for both frameworks during transition
