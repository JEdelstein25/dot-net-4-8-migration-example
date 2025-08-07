# .NET 8 Australian Tax Calculator

A comprehensive Australian tax calculation application built using modern .NET 8 technologies. This project serves as an example of enterprise-grade .NET applications and demonstrates successful migration from .NET Framework 4.8.

## Overview

This application calculates Australian income tax, Medicare levy, and historical levies (such as the Budget Repair Levy) for financial years 2015-16 through 2024-25. It demonstrates:

- **Historical tax calculations** with accurate progressive tax brackets
- **Multiple API endpoints** for tax calculation, bracket retrieval, and health checks
- **Enterprise architecture patterns** with dependency injection and repository pattern
- **Modern .NET 8 stack** with Microsoft.Data.SqlClient data access
- **Self-hosted API server** for demonstration without IIS dependencies

## Solution Architecture

```
AustralianTaxCalculator/
├── TaxCalculator.Core/          # Domain models and entities
├── TaxCalculator.Data/          # ADO.NET repositories and data access
├── TaxCalculator.Services/      # Business logic and tax calculation engine
├── TaxCalculator.Api/           # ASP.NET Web API 2 controllers
├── TaxCalculator.Console/       # Database setup and seeding utility
├── TaxCalculator.StandaloneApi/ # Self-hosted HTTP listener API
├── TaxCalculator.Tests.Unit/    # Unit tests with NUnit
├── Database/                    # SQL Server schema and seed scripts
└── ApiTestClient.cs            # Test client for API validation
```

## How to Run Locally

### Prerequisites

- .NET 8 SDK
- SQL Server LocalDB (optional, uses in-memory data by default)
- Visual Studio 2022 or any compatible IDE

### Option 1: Standalone API Server (Recommended)

The standalone API server runs without IIS and demonstrates all core functionality:

1. **Build the standalone API:**
   ```cmd
   dotnet build TaxCalculator.StandaloneApi\TaxCalculator.StandaloneApi.csproj --configuration Release
   ```

2. **Run the API server:**
   ```cmd
   dotnet run --project TaxCalculator.StandaloneApi\TaxCalculator.StandaloneApi.csproj
   ```
   Server starts on `http://localhost:8080`

3. **Test with the client:**
   ```cmd
   dotnet build ApiTestClient.csproj --configuration Release
   dotnet run --project ApiTestClient.csproj
   ```

### Option 2: Full Web API

1. **Restore dependencies:**
   ```cmd
   dotnet restore AustralianTaxCalculator.sln
   ```

2. **Build solution:**
   ```cmd
   dotnet build AustralianTaxCalculator.sln --configuration Release
   ```

3. **Run the API:**
   ```cmd
   dotnet run --project TaxCalculator.Api\TaxCalculator.Api.csproj
   ```

### Database Setup (Optional)

The application works with in-memory data by default. To use SQL Server:

1. **Create database:**
   ```cmd
   sqlcmd -S "(localdb)\MSSQLLocalDB" -i Database\CreateDatabase.sql
   ```

2. **Seed data:**
   ```cmd
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d AustralianTaxDB -i Database\SeedData.sql
   ```

## Technology Stack

### Core Framework
- **.NET 8** - Modern cross-platform framework
- **C# 12** - Latest language features

### Web API
- **ASP.NET Core Web API** - Modern RESTful API framework
- **Microsoft.AspNetCore** - HTTP request/response handling
- **HttpListener** - Self-hosted API option (legacy compatibility)

### Data Access
- **ADO.NET** - Direct database connectivity (no ORM)
- **Microsoft.Data.SqlClient** - Modern SQL Server data provider
- **SQL Server LocalDB** - Local development database

### Dependency Injection
- **Autofac 8.0** - IoC container for .NET
- **Autofac.Extensions.DependencyInjection** - ASP.NET Core integration

### Caching
- **StackExchange.Redis 1.2.6** - Redis client for distributed caching

### Testing
- **NUnit 3.13.3** - Unit testing framework
- **NUnit3TestAdapter** - Test runner integration

### Key NuGet Packages

```xml
<!-- Core Web API -->
<PackageReference Include="Microsoft.AspNetCore.App" />

<!-- Dependency Injection -->
<PackageReference Include="Autofac" Version="8.0.0" />
<PackageReference Include="Autofac.Extensions.DependencyInjection" Version="10.0.0" />

<!-- Data Access -->
<PackageReference Include="Microsoft.Data.SqlClient" Version="6.0.1" />

<!-- Caching -->
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />

<!-- Testing -->
<PackageReference Include="NUnit" Version="3.13.3" />
<PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />

<!-- JSON Serialization -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## API Endpoints

### Health Check
```http
GET /api/health
```
Returns server health status and timestamp.

### Tax Calculation
```http
POST /api/tax/calculate
Content-Type: application/json

{
  "taxableIncome": 75000,
  "financialYear": "2024-25"
}
```

Returns detailed tax calculation including:
- Income tax
- Medicare levy
- Budget Repair Levy (historical years)
- Tax offsets
- Net tax payable
- Effective tax rate

### Tax Brackets
```http
GET /api/tax/brackets/{financialYear}
```
Example: `GET /api/tax/brackets/2024-25`

Returns progressive tax brackets for the specified financial year.

## Tax Calculation Features

### Supported Financial Years
- **2015-16** to **2024-25** (10 years of historical data)

### Tax Components
- **Progressive Income Tax** - Based on ATO tax brackets
- **Medicare Levy** - 2% for incomes above threshold ($23,365-$29,207 depending on year)
- **Budget Repair Levy** - 2% for incomes above $180,000 (2014-15 to 2016-17)
- **Low Income Tax Offset (LITO)** - Automatic offset for eligible incomes

### Examples

**Middle Income (2024-25):**
- Income: $75,000
- Net Tax: $14,788
- Effective Rate: 19.72%

**High Income with Budget Repair Levy (2015-16):**
- Income: $200,000  
- Net Tax: $71,547
- Effective Rate: 35.77%
- Includes Budget Repair Levy: $4,000

## Testing

Run unit tests with 100% coverage:

```cmd
# Build test project
dotnet build TaxCalculator.Tests.Unit\TaxCalculator.Tests.Unit.csproj

# Run tests
dotnet test TaxCalculator.Tests.Unit\TaxCalculator.Tests.Unit.csproj --logger "console;verbosity=detailed"
```

## Enterprise Patterns Demonstrated

- **Repository Pattern** - Data access abstraction
- **Dependency Injection** - Loose coupling with Autofac
- **Service Layer** - Business logic separation
- **API Versioning** - RESTful endpoint design
- **Configuration Management** - App.config and environment settings
- **Error Handling** - Structured exception management
- **Logging** - Console and structured logging patterns

## Migration Considerations

This project demonstrates common patterns found in legacy .NET Framework applications that may need migration to .NET Core/.NET 5+:

- **ASP.NET Web API 2** → **ASP.NET Core Web API**
- **Autofac 4.x** → **Built-in DI Container** or **Autofac 6.x**
- **ADO.NET** → **Entity Framework Core** or **Dapper**
- **System.Web** → **Microsoft.AspNetCore**
- **App.config** → **appsettings.json**

## License

This project is provided as an educational example for legacy .NET Framework development patterns.

## Contributing

This is a demonstration project. For real-world tax calculations, please consult the Australian Taxation Office (ATO) for current rates and regulations.
