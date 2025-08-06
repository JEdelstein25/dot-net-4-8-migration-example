# .NET Framework 4.8 to .NET Core 8 Migration Overview

## Executive Summary

The Australian Tax Calculator application is well-architected for migration from .NET Framework 4.8 to .NET Core 8. The clean layered architecture with minimal external dependencies makes this a **MODERATE complexity** migration estimated at **3-4 weeks** with a 3-phase approach focusing on core framework migration and API compatibility.

## Application Architecture Analysis

### Current Structure
- **TaxCalculator.Api**: ASP.NET Web API 2 (System.Web) - Main API surface
- **TaxCalculator.StandaloneApi**: Self-hosted HttpListener alternative
- **TaxCalculator.Core**: Domain models and entities (clean, portable)
- **TaxCalculator.Services**: Business logic layer
- **TaxCalculator.Data**: ADO.NET repositories 
- **TaxCalculator.Infrastructure**: Cross-cutting concerns
- **TaxCalculator.Console**: Utility/seeding tools
- **TaxCalculator.Tests.Unit**: NUnit test suite

### API Contract Analysis
The API surface must remain **100% identical**:

```
Base Path: /api

Health Endpoints:
GET /api/health → { status: "OK", timestamp: "<UTC-ISO8601>" }

Tax Calculation Endpoints:
POST /api/tax/calculate (with complex TaxCalculationRequest body)
GET  /api/tax/brackets/{year}
GET  /api/tax/compare?income=123&years=2019-20&years=2024-25
GET  /api/tax/history/{income}?years=10
```

**Critical**: All endpoints use PascalCase JSON (Newtonsoft.Json default) - must preserve this exactly.

## Migration Approach: 3-Phase Strategy

### Phase 1: Foundation Migration (Week 1)
- Retarget Core, Services, Data, Infrastructure projects to .NET 8
- Update NuGet packages to .NET 8 compatible versions
- Migrate unit test projects to .NET 8
- Build and test after each project migration
- **Commit**: Foundation libraries migrated to .NET 8

### Phase 2: API Migration (Weeks 2-3)
- Create new ASP.NET Core 8 API project
- Migrate controllers to ASP.NET Core with identical routing
- Implement configuration migration (App.config → appsettings.json)
- Set up dependency injection (Autofac → built-in DI)
- Preserve exact JSON serialization with Newtonsoft.Json
- Build and test API functionality
- **Commit**: Complete API migration to ASP.NET Core 8

### Phase 3: Validation & Documentation (Week 4)
- Comprehensive contract validation testing
- Integration testing and API compatibility verification
- Update CI/CD pipeline for .NET 8
- Documentation and knowledge transfer
- **Commit**: Migration completed with full validation

## Build and Test Strategy

### Continuous Validation
- **Build after every project migration**: Ensure compilation succeeds before proceeding
- **Run unit tests after each change**: Validate business logic remains intact
- **Integration testing at phase completion**: Verify end-to-end functionality
- **Commit at phase completion**: Preserve working state with updated progress

### Build Commands
```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Build specific project
dotnet build TaxCalculator.Core

# Run tests for specific project
dotnet test TaxCalculator.Tests.Unit
```

### Blocker Handling
- **Document blockers immediately** in progress.md
- **Include root cause and attempted solutions**
- **Mark tasks as blocked with clear next steps**
- **Escalate if blocked for more than 4 hours**

## Risk Assessment & Mitigation

### High Risk Items
1. **JSON Serialization Changes**
   - Risk: System.Text.Json defaults differ from Newtonsoft.Json
   - Mitigation: Force Newtonsoft.Json usage with PascalCase settings

2. **Configuration System Changes**
   - Risk: App.config → appsettings.json behavioral differences
   - Mitigation: Create configuration validation tests

3. **Dependency Injection Container Changes**
   - Risk: Autofac → built-in DI service resolution differences
   - Mitigation: Comprehensive integration testing

### Medium Risk Items
1. **SqlClient Namespace Changes**
2. **Logging Infrastructure Changes** 
3. **NuGet Package Compatibility**

## Success Criteria

### Contract Integrity Validation
- Automated contract tests for all endpoints
- JSON schema validation for responses
- HTTP status code verification
- Error message format preservation

### Quality Assurance
- 100% unit test migration with identical assertions
- Integration test coverage maintained
- Zero test logic changes (framework syntax updates only)
- All builds complete successfully

## Technology Stack Changes

| Component | .NET Framework 4.8 | .NET Core 8 |
|-----------|-------------------|--------------|
| Web Framework | ASP.NET Web API 2 | ASP.NET Core 8 |
| JSON Serialization | Newtonsoft.Json | Newtonsoft.Json (preserved) |
| DI Container | Autofac 4.x | Built-in DI |
| Configuration | App.config/Web.config | appsettings.json |
| Data Access | System.Data.SqlClient | Microsoft.Data.SqlClient |
| Logging | Custom ILogger | Microsoft.Extensions.Logging |
| Hosting | IIS/HttpListener | Kestrel |

## Git Workflow

### Commit Strategy
1. **Update progress.md** with completed tasks and any blockers
2. **Build and test** to ensure everything works
3. **Commit changes** with descriptive message including phase completion
4. **Push to remote** to preserve work

### Commit Message Format
```
Phase X: [Brief description of work completed]

- Task 1 completed
- Task 2 completed  
- Task 3 blocked: [reason]

Tests: [number] passing
Build: ✅ Successful
```

## Next Steps

1. Review and approve this simplified migration plan
2. Set up development environment for .NET 8
3. Begin Phase 1 execution as outlined in phase-1.md
4. Build and test frequently, commit at phase completion

## Timeline Overview

```
Week 1: Foundation Migration (Core libraries to .NET 8)
Week 2-3: API Migration (ASP.NET Core conversion)  
Week 4: Validation & Documentation
```

**Total Estimated Effort**: 3-4 weeks with 1-2 developers
**Go-Live Target**: End of Week 4 with validated API compatibility
