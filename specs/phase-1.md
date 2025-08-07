# Phase 1: Core Models and Foundation Migration

## Overview
Phase 1 focuses on migrating the foundational layer (TaxCalculator.Core) to .NET 8 while preserving all model classes and ensuring zero behavioral changes.

## Scope
- **Primary Target**: TaxCalculator.Core project
- **Risk Level**: 🟢 LOW RISK
- **Dependencies**: None (foundational layer)
- **Estimated Duration**: 1-2 days

## Objectives
1. Convert TaxCalculator.Core to .NET 8 SDK-style project
2. Preserve all existing model classes exactly as-is
3. Maintain assembly references and namespaces
4. Ensure no breaking changes to dependent projects

## Pre-Migration Checklist
- [ ] Create backup of current TaxCalculator.Core project
- [ ] Document current assembly references
- [ ] Identify all model classes and their dependencies
- [ ] Prepare validation tests for model serialization/deserialization

## Migration Tasks

### Task 1.1: Project File Conversion
**Objective**: Convert legacy .csproj to SDK-style format

**Current State**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <!-- Legacy format with explicit file references -->
  </PropertyGroup>
</Project>
```

**Target State**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>TaxCalculator.Core</RootNamespace>
    <AssemblyName>TaxCalculator.Core</AssemblyName>
  </PropertyGroup>
</Project>
```

**Steps**:
1. Backup existing TaxCalculator.Core.csproj
2. Replace with SDK-style project file
3. Verify all .cs files are automatically included
4. Test build and ensure no compilation errors

**Validation**:
- [ ] Project builds successfully
- [ ] All model classes compile without errors
- [ ] Assembly name and namespace unchanged
- [ ] No missing file references

### Task 1.2: Model Class Validation
**Objective**: Ensure all model classes remain functionally identical

**Model Classes to Validate**:
- `TaxBracket.cs`
- `TaxCalculationRequest.cs`
- `TaxCalculationResult.cs`
- `TaxOffset.cs`
- `TaxLevy.cs`
- `User.cs`
- `UserMonthlyIncome.cs`
- `UserAnnualTaxSummary.cs`
- `TaxBracketCalculation.cs`
- `LevyCalculation.cs`
- `OffsetCalculation.cs`
- `MonthlyIncomeSummary.cs`

**Validation Steps**:
1. Review each model class for .NET 8 compatibility
2. Verify property types and attributes
3. Test JSON serialization/deserialization
4. Ensure no behavioral changes

**Validation Criteria**:
- [ ] All properties serialize to identical JSON
- [ ] All properties deserialize correctly
- [ ] Property validation attributes work correctly
- [ ] No changes to public API surface

### Task 1.3: Dependency Analysis
**Objective**: Identify and resolve any missing dependencies

**Current Dependencies**:
- System
- System.Core
- System.Xml.Linq
- System.Data.DataSetExtensions
- Microsoft.CSharp
- System.Data
- System.Net.Http
- System.Xml

**Steps**:
1. Identify which dependencies are still needed
2. Add explicit package references if required
3. Test compilation and runtime behavior

**Expected Outcome**: Most dependencies should be automatically available in .NET 8

### Task 1.4: Interface Structure Validation
**Objective**: Verify the Interfaces folder structure and dependencies

**Current State**: Empty Interfaces folder exists

**Steps**:
1. Verify interfaces folder is preserved
2. Ensure folder structure matches expectations
3. Prepare for future interface additions

## Testing Strategy

### Unit Test Validation
1. **Model Serialization Tests**:
   ```csharp
   [Test]
   public void TaxCalculationRequest_SerializesToJson_IdenticalFormat()
   {
       // Test that JSON output matches .NET Framework version exactly
   }
   ```

2. **Property Validation Tests**:
   ```csharp
   [Test]
   public void TaxBracket_Properties_BehaviorUnchanged()
   {
       // Verify all property getters/setters work identically
   }
   ```

3. **Type Compatibility Tests**:
   ```csharp
   [Test]
   public void Models_AssemblyInfo_Unchanged()
   {
       // Verify assembly metadata and type information
   }
   ```

### Integration Test Preparation
1. Create test harness for model validation
2. Prepare JSON comparison utilities
3. Set up automated testing pipeline

## Success Criteria

### Technical Validation
- [ ] Project builds successfully on .NET 8
- [ ] All model classes compile without errors
- [ ] JSON serialization produces identical output
- [ ] No breaking changes to public API
- [ ] Assembly can be referenced by other projects

### Business Validation
- [ ] All tax calculation models work correctly
- [ ] Request/response models serialize properly
- [ ] No data loss or corruption
- [ ] Performance characteristics maintained

## Rollback Plan

### Rollback Triggers
- Compilation errors that cannot be resolved
- JSON serialization format changes
- Any breaking changes to model behavior
- Performance degradation

### Rollback Steps
1. Restore original .csproj file from backup
2. Verify original functionality
3. Document lessons learned
4. Revise migration approach

### Recovery Time
- **Rollback Execution**: < 5 minutes
- **Validation**: < 10 minutes
- **Total Recovery**: < 15 minutes

## Dependencies and Next Steps

### Dependencies for Other Phases
This phase blocks:
- Phase 2: Data Access Layer (requires Core models)
- Phase 3: Business Logic Layer (requires Core models)

### Preparation for Phase 2
- Document model interfaces needed for data layer
- Prepare dependency injection interfaces
- Plan repository pattern implementations

## Known Issues and Workarounds

### Issue 1: Assembly Version Changes
**Problem**: .NET 8 may change assembly version format
**Workaround**: Explicitly set AssemblyVersion attribute if needed

### Issue 2: Nullable Reference Types
**Problem**: .NET 8 enables nullable reference types by default
**Workaround**: Disable nullable warnings initially with `<Nullable>disable</Nullable>`

## Quality Gates

### Entry Criteria
- [ ] Phase 0 analysis completed
- [ ] Backup of current code created
- [ ] Development environment setup for .NET 8

### Exit Criteria
- [ ] TaxCalculator.Core builds successfully on .NET 8
- [ ] All validation tests pass
- [ ] No breaking changes detected
- [ ] Documentation updated
- [ ] Code committed to version control

### Approval Required
- [ ] Technical lead approval on model compatibility
- [ ] QA validation of serialization behavior
- [ ] Architecture review of project structure changes

## Timeline

### Day 1
- Morning: Project file conversion and initial build
- Afternoon: Model class validation and testing

### Day 2 (if needed)
- Morning: Address any compatibility issues
- Afternoon: Final validation and documentation

**Milestone**: Core foundation ready for Phase 2 migration

This phase establishes the foundation for the entire migration by ensuring the core domain models work correctly in .NET 8 while maintaining complete compatibility with existing code.
