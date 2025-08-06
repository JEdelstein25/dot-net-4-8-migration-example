# Phase 1: Foundation Migration - Detailed Plan

## Phase Overview

**Duration**: Week 1  
**Objective**: Migrate all core libraries to .NET 8 with continuous building and testing  
**Risk Level**: LOW-MEDIUM  
**Success Criteria**: All core libraries compile and run on .NET 8 with 100% test pass rate  
**Commit Goal**: Foundation libraries successfully migrated to .NET 8  

## Pre-Phase 1 Prerequisites

### Environment Setup
- [ ] .NET 8 SDK installed (8.0.100 or later)
- [ ] Visual Studio 2022 17.8+ or JetBrains Rider 2023.3+
- [ ] Docker Desktop installed and running
- [ ] Git branch `dot-net-core-8-upgrade` checked out

### Baseline Establishment
- [ ] Current solution builds successfully in .NET Framework 4.8
- [ ] All unit tests pass (establish baseline metrics)
- [ ] Performance baseline captured (response times, memory usage)
- [ ] API contract documented (OpenAPI spec generation)

---

## Build and Test Strategy for Phase 1

### Continuous Validation Approach
1. **Migrate one project at a time**
2. **Build immediately after each project change**
3. **Run tests after each successful build**
4. **Document any blockers in progress.md immediately**
5. **Only proceed if build and tests pass**

### Build Commands to Use
```bash
# After each project migration
dotnet build [ProjectName]

# After successful build
dotnet test [TestProjectName]

# Full solution validation
dotnet build
dotnet test
```

---

## Task Group 1: Core Library Migration (Week 1, Days 1-3)

### 1.1 TaxCalculator.Core Migration

**Objective**: Migrate domain models to .NET 8 (easiest first)

#### Tasks:
1. **Update Project File**
   ```xml
   <PropertyGroup>
     <TargetFramework>net8.0</TargetFramework>
     <LangVersion>12.0</LangVersion>
     <Nullable>enable</Nullable>
     <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
   </PropertyGroup>
   ```

2. **Immediate Build and Test**
   ```bash
   dotnet build TaxCalculator.Core
   # If build succeeds, run any related tests
   dotnet test TaxCalculator.Tests.Unit --filter "TaxCalculator.Core"
   ```

3. **Validation Steps**
   - Remove any .NET Framework-specific references
   - Update namespace usings if needed
   - Ensure all models compile without warnings
   - **IMPORTANT**: Document any issues in progress.md immediately

#### Expected Changes:
- **Project file**: Framework target update
- **Code changes**: Minimal (domain models are typically clean)
- **Package references**: None expected for Core project

#### Success Criteria:
- ✅ Project compiles without warnings
- ✅ Related tests pass
- ✅ No behavioral changes in business logic
- ✅ Build successful before proceeding to next project

### 1.2 TaxCalculator.Data Migration

**Objective**: Migrate data access layer to .NET 8

#### Tasks:
1. **Update Project Target**
   ```xml
   <PropertyGroup>
     <TargetFramework>net8.0</TargetFramework>
   </PropertyGroup>
   ```

2. **Update Package References**
   ```xml
   <!-- Replace -->
   <PackageReference Include="System.Data.SqlClient" Version="4.8.3" />
   <!-- With -->
   <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.1" />
   ```

3. **Update Namespace References**
   ```csharp
   // Replace all occurrences
   using System.Data.SqlClient;
   // With
   using Microsoft.Data.SqlClient;
   ```

4. **Build and Test Immediately**
   ```bash
   dotnet build TaxCalculator.Data
   # If build succeeds
   dotnet test TaxCalculator.Tests.Unit --filter "TaxCalculator.Data"
   # Document any failures in progress.md
   ```

5. **Validation Steps**
   - Update `SqlConnectionFactory` implementation
   - Ensure connection string compatibility
   - Verify all SQL operations work identically

#### Expected Changes:
- **Namespace imports**: System.Data.SqlClient → Microsoft.Data.SqlClient
- **Connection strings**: Verify compatibility
- **SQL operations**: Should be identical

#### Success Criteria:
- ✅ Project builds without errors
- ✅ Data access tests pass
- ✅ Connection factory works correctly
- ✅ Build successful before proceeding to next project

### 1.3 TaxCalculator.Services Migration

**Objective**: Migrate business logic layer to .NET 8

#### Tasks:
1. **Update Project Target**
   ```xml
   <PropertyGroup>
     <TargetFramework>net8.0</TargetFramework>
   </PropertyGroup>
   ```

2. **Update Project Dependencies**
   - Reference updated TaxCalculator.Core (.NET 8)
   - Reference updated TaxCalculator.Data (.NET 8)
   - Update any service-specific packages

3. **Build and Test Immediately**
   ```bash
   dotnet build TaxCalculator.Services
   # If build succeeds
   dotnet test TaxCalculator.Tests.Unit --filter "TaxCalculator.Services"
   # Document any failures in progress.md
   ```

4. **Configuration and Logging Notes**
   - Document all ConfigurationManager.AppSettings usage for Phase 2
   - Note ILogger interface usage for Phase 2 migration
   - Ensure no immediate changes to behavior

#### Expected Changes:
- **Project references**: Updated to .NET 8 projects
- **Service logic**: No behavioral changes
- **Configuration**: Document for later migration

#### Success Criteria:
- ✅ Project builds without errors
- ✅ Business logic tests pass
- ✅ Tax calculations produce identical results
- ✅ Build successful before proceeding to next project

### 1.4 TaxCalculator.Infrastructure Migration

**Objective**: Migrate cross-cutting concerns to .NET 8

#### Tasks:
1. **Update Project Target**
   ```xml
   <PropertyGroup>
     <TargetFramework>net8.0</TargetFramework>
   </PropertyGroup>
   ```

2. **Update Package References**
   ```xml
   <PackageReference Include="StackExchange.Redis" Version="2.7.4" />
   ```

3. **Build and Test Immediately**
   ```bash
   dotnet build TaxCalculator.Infrastructure
   # If build succeeds
   dotnet test TaxCalculator.Tests.Unit --filter "TaxCalculator.Infrastructure"
   # Document any failures in progress.md
   ```

4. **Validation Steps**
   - Review all helper classes for .NET 8 compatibility
   - Update any framework-specific implementations
   - Note logging interface for Phase 2 migration

#### Expected Changes:
- **Redis client**: Version 2.x updates
- **Utilities**: Minimal changes expected
- **Logging**: Document for Phase 2 migration

#### Success Criteria:
- ✅ Project builds without errors
- ✅ Infrastructure tests pass
- ✅ Redis operations work correctly
- ✅ Build successful before proceeding to test migration

---

## Task Group 2: Testing Infrastructure (Week 1, Days 4-5)

### 2.1 Unit Test Project Migration

**Objective**: Migrate test projects to .NET 8 while preserving all test logic

#### Tasks:
1. **Update Test Project Framework**
   ```xml
   <PropertyGroup>
     <TargetFramework>net8.0</TargetFramework>
   </PropertyGroup>
   ```

2. **Update Test Packages**
   ```xml
   <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
   <PackageReference Include="NUnit" Version="4.0.1" />
   <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
   <PackageReference Include="Moq" Version="4.20.69" />
   ```

3. **Build and Test Immediately**
   ```bash
   dotnet build TaxCalculator.Tests.Unit
   # If build succeeds
   dotnet test TaxCalculator.Tests.Unit
   # Document any failures in progress.md with details
   ```

4. **Test Logic Preservation**
   - **CRITICAL**: No test assertions should change
   - Framework syntax updates allowed only if tests fail
   - Test scenarios must remain identical
   - Document any changes required in progress.md

#### Expected Changes:
- **Framework target**: net8.0
- **Package versions**: Updated to .NET 8 compatible
- **Test logic**: **ZERO CHANGES ALLOWED** unless required for compilation

#### Success Criteria:
- ✅ Test project builds without errors
- ✅ 100% test pass rate (identical to baseline)
- ✅ No test logic modifications
- ✅ All tests discovered and executed

---

## Phase 1 Completion and Commit

### Final Validation
Before committing Phase 1, ensure:

1. **Full Solution Build**
   ```bash
   dotnet build
   ```

2. **Complete Test Suite**
   ```bash
   dotnet test
   ```

3. **Update Progress File**
   - Mark all Phase 1 tasks as completed
   - Document any issues encountered and resolved
   - Note any blockers for Phase 2

4. **Commit Changes**
   ```bash
   git add .
   git commit -m "Phase 1: Foundation libraries migrated to .NET 8

   - TaxCalculator.Core: ✅ Migrated to .NET 8
   - TaxCalculator.Data: ✅ Migrated to .NET 8, updated to Microsoft.Data.SqlClient
   - TaxCalculator.Services: ✅ Migrated to .NET 8
   - TaxCalculator.Infrastructure: ✅ Migrated to .NET 8, updated Redis client
   - TaxCalculator.Tests.Unit: ✅ Migrated to .NET 8

   Tests: [X] passing
   Build: ✅ Successful
   Ready for Phase 2: API Migration"
   ```

### Phase 1 Success Criteria Summary
- ✅ All core libraries target .NET 8
- ✅ All projects build without errors
- ✅ All unit tests pass
- ✅ Package references updated to .NET 8 compatible versions
- ✅ No behavioral changes to business logic
- ✅ Progress documented and committed

---

*Phase 1 Success Criteria: All foundation libraries successfully migrated to .NET 8 with 100% test compatibility and committed to version control.*
