# Phase 3: Business Logic Layer Migration

## Overview
Phase 3 migrates the business logic layer (TaxCalculator.Services) to .NET 8 while preserving all tax calculation algorithms and service implementations.

## Scope
- **Primary Target**: TaxCalculator.Services project
- **Risk Level**: 🟡 MEDIUM RISK
- **Dependencies**: Phase 1 (Core), Phase 2 (Data)
- **Estimated Duration**: 2-3 days

## Objectives
1. Migrate TaxCalculator.Services to .NET 8
2. Update logging to Microsoft.Extensions.Logging compatible interface
3. Migrate caching implementation for .NET 8 compatibility
4. Preserve all business logic and tax calculation algorithms
5. Maintain service interfaces and implementations

## Pre-Migration Checklist
- [ ] Phase 1 and Phase 2 successfully completed
- [ ] Business logic algorithms documented and tested
- [ ] Current service interfaces cataloged
- [ ] Caching behavior documented
- [ ] Logging patterns identified

## Migration Tasks

### Task 3.1: Project File Migration

#### 3.1.1: SDK-Style Project Conversion
**Current State**: Legacy .csproj with System dependencies
**Target State**: .NET 8 SDK-style project

**New Project Structure**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>TaxCalculator.Services</RootNamespace>
    <AssemblyName>TaxCalculator.Services</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="StackExchange.Redis" Version="2.6.122" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TaxCalculator.Core\TaxCalculator.Core.csproj" />
    <ProjectReference Include="..\TaxCalculator.Data\TaxCalculator.Data.csproj" />
  </ItemGroup>
</Project>
```

### Task 3.2: Service Interface Migration

#### 3.2.1: Core Service Interfaces
**Services to Migrate**:
- `ITaxCalculationService` - Core tax calculation logic
- `IUserTaxService` - User-specific tax operations
- `ICacheService` - Caching abstraction
- `ILogger` - Logging abstraction

**Interface Preservation Strategy**:
```csharp
// CRITICAL: All interfaces must remain 100% compatible
public interface ITaxCalculationService
{
    Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request);
    Task<List<TaxBracket>> GetTaxBracketsAsync(string financialYear);
    Task<List<TaxCalculationResult>> CompareTaxAcrossYearsAsync(decimal income, List<string> years);
    Task<List<TaxCalculationResult>> GetTaxHistoryAsync(decimal income, int years);
}
```

#### 3.2.2: Logging Interface Update
**Current State**: Custom ILogger interface
**Migration Strategy**: Maintain compatibility while preparing for Microsoft.Extensions.Logging

**Option 1 - Maintain Custom Interface (Recommended for Phase 3)**:
```csharp
// Keep existing ILogger interface for now
// Implement adapter pattern for Microsoft.Extensions.Logging in Phase 4
public interface ILogger
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message, Exception exception = null);
}
```

**Option 2 - Direct Migration** (Higher risk):
```csharp
// Migrate to Microsoft.Extensions.Logging directly
// Higher risk due to interface changes
```

**Recommendation**: Use Option 1 to minimize risk in this phase

### Task 3.3: Tax Calculation Service Migration

#### 3.3.1: Core Algorithm Preservation
**Critical Requirement**: All tax calculation algorithms must produce identical results

**Key Methods to Validate**:
1. `CalculateTaxAsync` - Primary calculation logic
2. Progressive tax bracket calculations
3. Medicare levy calculations
4. Budget Repair Levy (historical years)
5. Tax offset calculations (LITO)
6. Effective tax rate calculations

**Validation Strategy**:
```csharp
[Test]
public async Task TaxCalculation_AllScenarios_ProducesIdenticalResults()
{
    // Test matrix of all income levels and years
    var testCases = GenerateTestMatrix();
    
    foreach (var testCase in testCases)
    {
        var frameworkResult = await _frameworkService.CalculateTaxAsync(testCase);
        var coreResult = await _coreService.CalculateTaxAsync(testCase);
        
        Assert.That(coreResult.IncomeTax, Is.EqualTo(frameworkResult.IncomeTax).Within(0.01m));
        Assert.That(coreResult.MedicareLevy, Is.EqualTo(frameworkResult.MedicareLevy).Within(0.01m));
        // ... validate all fields
    }
}
```

#### 3.3.2: Business Logic Components
**Components to Migrate**:

1. **Progressive Tax Calculation**:
   ```csharp
   private decimal CalculateProgressiveTax(decimal income, List<TaxBracket> brackets)
   {
       // CRITICAL: Algorithm must remain identical
       // Preserve all rounding, precision, and calculation order
   }
   ```

2. **Medicare Levy Calculation**:
   ```csharp
   private decimal CalculateMedicareLevy(decimal income, List<TaxLevy> levies)
   {
       // Preserve threshold logic and rate application
   }
   ```

3. **Tax Offset Application**:
   ```csharp
   private decimal CalculateTaxOffsets(decimal baseTax, decimal income, List<TaxOffset> offsets)
   {
       // Maintain LITO phase-out logic exactly
   }
   ```

### Task 3.4: Caching Service Migration

#### 3.4.1: Redis Client Upgrade
**Current**: StackExchange.Redis 1.2.6
**Target**: StackExchange.Redis 2.6.122

**Migration Considerations**:
- API compatibility between versions
- Connection string format changes
- Serialization behavior
- Performance characteristics

**Implementation Strategy**:
```csharp
public class CacheService : ICacheService
{
    private readonly IDatabase _database;
    
    // Maintain identical interface
    public async Task<T> GetAsync<T>(string key)
    {
        // Preserve exact caching behavior
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        // Maintain serialization format compatibility
    }
}
```

#### 3.4.2: Cache Behavior Validation
**Critical Tests**:
- Tax bracket caching (performance critical)
- Cache key generation (must be identical)
- Expiration behavior
- Serialization format compatibility

### Task 3.5: User Tax Service Migration

#### 3.5.1: Service Implementation
**Components**:
- User-specific tax calculations
- Historical tax data management
- Tax comparison services
- Monthly/annual summaries

**Validation Focus**:
- User data processing accuracy
- Historical calculation consistency
- Performance characteristics

## Testing Strategy

### Business Logic Validation

#### 3.5.1: Comprehensive Tax Calculation Tests
```csharp
[TestFixture]
public class TaxCalculationServiceBusinessLogicTests
{
    [TestCaseSource(nameof(AllTaxScenarios))]
    public async Task CalculateTax_AllScenarios_ProducesIdenticalResults(TaxTestCase testCase)
    {
        // Comprehensive test matrix covering:
        // - All financial years (2015-16 to 2024-25)
        // - Income ranges: $0 to $500,000
        // - All tax components: income tax, medicare levy, offsets
        // - Edge cases: thresholds, phase-outs, historical levies
    }
    
    private static IEnumerable<TaxTestCase> AllTaxScenarios()
    {
        // Generate comprehensive test matrix
        var years = new[] { "2015-16", "2016-17", "2017-18", ..., "2024-25" };
        var incomes = new[] { 0m, 18200m, 18201m, 45000m, 45001m, 85000m, 135000m, 190000m, 250000m };
        
        foreach (var year in years)
        {
            foreach (var income in incomes)
            {
                yield return new TaxTestCase { Year = year, Income = income };
            }
        }
    }
}
```

#### 3.5.2: Cache Behavior Tests
```csharp
[TestFixture]
public class CacheServiceTests
{
    [Test]
    public async Task CacheService_TaxBrackets_CachesCorrectly()
    {
        // Verify caching behavior identical to original
    }
    
    [Test]
    public async Task CacheService_Serialization_MaintainsFormat()
    {
        // Ensure serialized cache entries can be read by both versions
    }
}
```

### Performance Testing

#### 3.5.1: Calculation Performance
- Tax calculation response times
- Cache hit/miss ratios
- Memory usage patterns
- CPU utilization

#### 3.5.2: Cache Performance
- Redis connection performance
- Serialization/deserialization speed
- Cache lookup times

## Success Criteria

### Technical Validation
- [ ] TaxCalculator.Services builds successfully on .NET 8
- [ ] All service interfaces unchanged
- [ ] Tax calculations produce identical results (within 0.01m precision)
- [ ] Cache operations work correctly
- [ ] Performance within 10% of baseline

### Business Validation
- [ ] All tax calculation scenarios produce correct results
- [ ] Historical tax data calculations accurate
- [ ] Medicare levy calculations correct
- [ ] Tax offset applications correct
- [ ] Multi-year comparisons accurate

### Integration Validation
- [ ] Services integrate correctly with Data layer
- [ ] Cache service operates properly
- [ ] Logging functions correctly
- [ ] Ready for API layer integration (Phase 4)

## Risk Mitigation

### Primary Risks
1. **Tax Calculation Algorithm Changes**
   - **Risk**: Subtle differences in decimal arithmetic or rounding
   - **Mitigation**: Extensive side-by-side testing with exact precision validation

2. **Cache Behavior Changes**
   - **Risk**: Redis client upgrade changes serialization or connection behavior
   - **Mitigation**: Cache behavior validation and fallback mechanisms

3. **Performance Degradation**
   - **Risk**: .NET 8 performance characteristics differ
   - **Mitigation**: Performance benchmarking and optimization

### Critical Validation Points
- All tax calculations to 2 decimal places
- Cache key generation consistency
- Error handling behavior
- Service method signatures

## Dependencies and Integration

### Prerequisites
- [ ] Phase 1 (Core) completed
- [ ] Phase 2 (Data) completed
- [ ] Redis testing environment available

### Phase 4 Preparation
- Service layer ready for API integration
- Dependency injection interfaces defined
- Error handling patterns established
- Performance characteristics documented

## Rollback Plan

### Rollback Triggers
- Tax calculation results differ by > 0.01m
- Cache operations fail
- Performance degradation > 15%
- Service interface breaking changes

### Recovery Steps
1. Restore original service implementations
2. Revert cache client version
3. Validate business logic
4. Run comprehensive test suite

### Recovery Time: < 30 minutes

## Timeline

### Day 1: Core Service Migration
- Morning: Project conversion and service interface preservation
- Afternoon: Tax calculation service migration

### Day 2: Supporting Services
- Morning: Cache service migration and testing
- Afternoon: User tax service migration

### Day 3: Validation and Testing
- Morning: Comprehensive business logic testing
- Afternoon: Performance testing and optimization

## Quality Gates

### Entry Criteria
- [ ] Phases 1-2 completed successfully
- [ ] Business logic test suite prepared
- [ ] Cache testing environment ready

### Exit Criteria
- [ ] All services build and function correctly
- [ ] Tax calculation accuracy validated
- [ ] Cache operations verified
- [ ] Performance benchmarks met
- [ ] Ready for API layer migration

### Approval Required
- [ ] Business stakeholder validation of tax calculations
- [ ] Technical lead approval of service implementations
- [ ] QA validation of all business scenarios

This phase ensures that all critical business logic continues to function identically while being compatible with .NET 8, setting the foundation for the final API layer migration in Phase 4.
