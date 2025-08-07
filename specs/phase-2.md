# Phase 2: Data Access Layer Migration

## Overview
Phase 2 migrates the data access infrastructure (TaxCalculator.Data and TaxCalculator.Infrastructure) to .NET 8 while maintaining database connectivity and repository patterns.

## Scope
- **Primary Targets**: TaxCalculator.Data and TaxCalculator.Infrastructure projects
- **Risk Level**: 🟡 MEDIUM RISK
- **Dependencies**: Phase 1 (Core models)
- **Estimated Duration**: 2-3 days

## Objectives
1. Migrate data access projects to .NET 8
2. Replace System.Data.SqlClient with Microsoft.Data.SqlClient
3. Update configuration management for .NET Core
4. Preserve all repository interfaces and implementations
5. Maintain database connection and query behavior

## Pre-Migration Checklist
- [ ] Phase 1 successfully completed
- [ ] Database connectivity tested and documented
- [ ] Current connection strings and configuration documented
- [ ] Repository interfaces and implementations cataloged

## Migration Tasks

### Task 2.1: TaxCalculator.Data Project Migration

#### 2.1.1: Project File Conversion
**Current State**: Legacy .csproj with explicit references
**Target State**: SDK-style project with modern package references

**Steps**:
1. Convert to SDK-style project format
2. Update target framework to net8.0
3. Replace System.Data.SqlClient with Microsoft.Data.SqlClient

**New Project File Structure**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>TaxCalculator.Data</RootNamespace>
    <AssemblyName>TaxCalculator.Data</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TaxCalculator.Core\TaxCalculator.Core.csproj" />
  </ItemGroup>
</Project>
```

#### 2.1.2: SQL Client Migration
**Objective**: Replace System.Data.SqlClient with Microsoft.Data.SqlClient

**Code Changes Required**:
```csharp
// OLD (System.Data.SqlClient)
using System.Data.SqlClient;

// NEW (Microsoft.Data.SqlClient)
using Microsoft.Data.SqlClient;
```

**Files to Update**:
- All repository implementations
- Connection factories
- Database utilities

**Validation**:
- [ ] All SQL connections work identically
- [ ] Query results are unchanged
- [ ] Connection string format compatible
- [ ] Performance characteristics maintained

### Task 2.2: TaxCalculator.Infrastructure Project Migration

#### 2.2.1: Project File Conversion
**Current State**: Infrastructure utilities and configuration
**Target State**: .NET 8 compatible infrastructure layer

**Expected Components**:
- Configuration management
- Logging utilities
- Cache implementations
- Database factories

#### 2.2.2: Configuration System Updates
**Objective**: Prepare for .NET Core configuration system

**Current Pattern** (app.config):
```xml
<connectionStrings>
  <add name="DefaultConnection" 
       connectionString="Data Source=..." />
</connectionStrings>
```

**Target Pattern** (maintain compatibility for now):
```csharp
// Continue supporting ConfigurationManager for this phase
// Will be updated in Phase 4 for full .NET Core configuration
```

### Task 2.3: Repository Interface Validation

#### 2.3.1: Interface Compatibility Check
**Repositories to Validate**:
- `ITaxBracketRepository`
- `IUserRepository` (if exists)
- Database connection interfaces

**Validation Steps**:
1. Verify all interface contracts unchanged
2. Test method signatures and return types
3. Validate async/await patterns
4. Ensure exception handling patterns preserved

#### 2.3.2: Implementation Verification
**Key Areas**:
- Database connection management
- Parameter binding
- Result set mapping
- Error handling
- Transaction management

### Task 2.4: Database Integration Testing

#### 2.4.1: Connection Testing
**Test Scenarios**:
- Local database connections
- Connection string variations
- Connection pooling behavior
- Timeout and retry logic

#### 2.4.2: Query Validation
**Test Categories**:
1. **Tax Bracket Queries**: Verify exact same results
2. **User Data Queries**: Ensure data integrity
3. **Complex Joins**: Validate query performance
4. **Stored Procedures**: If any exist, ensure compatibility

#### 2.4.3: Data Integrity Testing
**Validation Points**:
- INSERT operations produce identical results
- UPDATE operations maintain data consistency
- DELETE operations work correctly
- Transaction rollback behavior

## Testing Strategy

### Unit Tests
```csharp
[TestFixture]
public class TaxBracketRepositoryTests
{
    [Test]
    public async Task GetTaxBracketsAsync_2024_25_ReturnsExpectedData()
    {
        // Verify repository returns identical data structure
        // Compare with .NET Framework version results
    }

    [Test]
    public async Task DatabaseConnection_ValidConnectionString_ConnectsSuccessfully()
    {
        // Test database connectivity with new SQL client
    }
}
```

### Integration Tests
```csharp
[TestFixture]
public class DatabaseIntegrationTests
{
    [Test]
    public async Task FullDataFlow_TaxCalculation_ProducesIdenticalResults()
    {
        // End-to-end data flow validation
    }
}
```

### Performance Tests
- Connection establishment time
- Query execution time
- Memory usage patterns
- Connection pool behavior

## Configuration Management Strategy

### Phase 2 Approach: Minimal Changes
**Strategy**: Maintain existing configuration patterns to minimize risk

**Current**:
- app.config files
- ConfigurationManager usage
- Connection string management

**Phase 2 Changes**:
- Keep existing configuration reading code
- Ensure compatibility with .NET 8
- Prepare infrastructure for Phase 4 migration

### Future Migration Path (Phase 4)
- Convert to appsettings.json
- Implement IConfiguration pattern
- Environment-specific configuration

## Success Criteria

### Technical Validation
- [ ] Both Data and Infrastructure projects build on .NET 8
- [ ] All database connections work correctly
- [ ] Repository methods return identical results
- [ ] Performance within 10% of baseline
- [ ] All unit tests pass

### Business Validation
- [ ] Tax bracket data retrieval works correctly
- [ ] User data operations function properly
- [ ] Database queries return accurate results
- [ ] Transaction handling works as expected

### Integration Validation
- [ ] Projects integrate correctly with Phase 1 (Core)
- [ ] Database connectivity established
- [ ] Repository pattern functioning
- [ ] Ready for Phase 3 (Services) integration

## Risk Mitigation

### Primary Risks
1. **Database Connectivity Issues**
   - **Mitigation**: Extensive connection testing
   - **Fallback**: Connection string format validation

2. **Query Behavior Changes**
   - **Mitigation**: Side-by-side result comparison
   - **Fallback**: Detailed query analysis and optimization

3. **Performance Degradation**
   - **Mitigation**: Performance benchmarking
   - **Fallback**: Connection pooling optimization

### Risk Monitoring
- Database connection success rate
- Query execution time metrics
- Memory usage patterns
- Error rates and types

## Dependencies and Blockers

### Prerequisites
- [ ] Phase 1 completed successfully
- [ ] Database environment available for testing
- [ ] .NET 8 development environment configured

### Potential Blockers
1. **Connection String Compatibility**: Microsoft.Data.SqlClient format differences
2. **Query Behavior**: Subtle differences in SQL client behavior
3. **Performance**: Connection pooling or query optimization differences

### Blocker Resolution
- Document all connection string variations
- Create comprehensive test suite for database operations
- Establish performance baselines and monitoring

## Rollback Plan

### Rollback Triggers
- Database connectivity failures
- Data integrity issues
- Performance degradation > 15%
- Repository interface breaking changes

### Rollback Steps
1. Restore original project files
2. Revert to System.Data.SqlClient
3. Validate database connectivity
4. Run full test suite

### Recovery Time Objective
- **Detection**: < 10 minutes (automated testing)
- **Rollback**: < 15 minutes
- **Validation**: < 10 minutes
- **Total**: < 35 minutes

## Timeline

### Day 1: Project Migration
- Morning: Convert project files to SDK-style
- Afternoon: Update SQL client references and test compilation

### Day 2: Database Integration
- Morning: Test database connectivity and basic operations
- Afternoon: Validate repository implementations

### Day 3: Validation and Testing
- Morning: Run comprehensive test suite
- Afternoon: Performance testing and optimization if needed

## Quality Gates

### Entry Criteria
- [ ] Phase 1 completed and validated
- [ ] Database test environment prepared
- [ ] Baseline performance metrics documented

### Exit Criteria
- [ ] All projects build successfully
- [ ] Database connectivity validated
- [ ] Repository tests pass
- [ ] Performance within acceptable range
- [ ] Integration with Core layer verified

### Approval Required
- [ ] DBA approval on database compatibility
- [ ] Technical lead approval on repository implementations
- [ ] QA validation of data access patterns

## Preparation for Phase 3

### Deliverables for Next Phase
- Functional data access layer
- Repository interfaces for service layer
- Database connection management
- Configuration infrastructure

### Service Layer Dependencies
- Repository interfaces available
- Database connectivity established
- Error handling patterns defined
- Performance characteristics documented

This phase establishes reliable data access infrastructure that the business logic layer (Phase 3) can depend on while maintaining full compatibility with existing database operations.
