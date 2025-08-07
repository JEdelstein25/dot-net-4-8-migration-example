# .NET Framework 4.8 to .NET 8 Core Migration Overview

## Executive Summary

This document outlines the comprehensive migration strategy for the Australian Tax Calculator application from .NET Framework 4.8 to .NET 8 Core while maintaining 100% API contract compatibility and preserving all business logic.

## Current State Analysis

### Application Architecture
- **Type**: ASP.NET Web API 2 application with layered architecture
- **Hosting**: IIS/IIS Express with self-hosted HTTP listener option
- **Framework**: .NET Framework 4.8 (legacy Windows-only)
- **Language**: C# 7.3 features
- **Project Structure**: 7 projects in solution with clear separation of concerns

### API Endpoints (Must Remain Identical)
1. `GET /api/health` - Health check endpoint
2. `POST /api/tax/calculate` - Primary tax calculation endpoint
3. `GET /api/tax/brackets/{year}` - Tax bracket retrieval
4. `GET /api/tax/compare` - Multi-year tax comparison
5. `GET /api/tax/history/{income}` - Tax history analysis

### Current Technology Stack
- **Web Framework**: ASP.NET Web API 2 (System.Web.Http)
- **Dependency Injection**: Autofac 4.9.4
- **Data Access**: ADO.NET with System.Data.SqlClient
- **Caching**: StackExchange.Redis 1.2.6 (legacy)
- **Testing**: NUnit 3.13.3 with Moq 4.16.1
- **JSON**: Newtonsoft.Json 12.0.3
- **Project Format**: Legacy .csproj with packages.config

## Migration Approach

### Strategy: Incremental Port-and-Test
1. **Preserve API Contracts**: Maintain exact same HTTP contracts, request/response models, and error handling
2. **Phased Migration**: Migrate layer by layer, starting with core models and working outward
3. **Automated Validation**: Continuous testing to ensure behavioral parity
4. **Zero Client Impact**: Existing consuming applications continue working without changes

### Target State
- **Framework**: .NET 8.0 (cross-platform)
- **Web Framework**: ASP.NET Core Web API 8.0
- **Dependency Injection**: Built-in Microsoft.Extensions.DependencyInjection
- **Data Access**: Upgraded to Microsoft.Data.SqlClient 5.x
- **Caching**: Updated StackExchange.Redis or equivalent
- **Testing**: Maintain NUnit with updated versions
- **JSON**: System.Text.Json or compatible Newtonsoft.Json
- **Project Format**: Modern SDK-style .csproj files

## Migration Phases

### Phase 1: Core Models and Foundation (Low Risk)
- Migrate TaxCalculator.Core project to .NET 8
- Update project file format to SDK-style
- Preserve all model classes exactly as-is
- Validate with existing unit tests

### Phase 2: Data Access Layer (Medium Risk)
- Migrate TaxCalculator.Data and TaxCalculator.Infrastructure
- Replace System.Data.SqlClient with Microsoft.Data.SqlClient
- Update connection string handling for .NET Core configuration
- Maintain all repository interfaces and implementations

### Phase 3: Business Logic Layer (Medium Risk)
- Migrate TaxCalculator.Services
- Update logging to Microsoft.Extensions.Logging
- Replace cache implementation with .NET Core compatible version
- Preserve all business logic and calculation algorithms

### Phase 4: API Layer (High Risk)
- Migrate TaxCalculator.Api from Web API 2 to ASP.NET Core
- Replace System.Web.Http with Microsoft.AspNetCore.Mvc
- Maintain identical routing, controllers, and response formats
- Update dependency injection from Autofac to built-in DI

### Phase 5: Testing Infrastructure (Medium Risk)
- Migrate test projects to .NET 8
- Update test frameworks to latest versions
- Ensure all test scenarios and assertions remain identical
- Maintain or improve test coverage

### Phase 6: Standalone Applications (Low Risk)
- Migrate console applications and test clients
- Update to .NET 8 console application templates
- Preserve all functionality

## Critical Success Factors

### API Contract Preservation
- **HTTP Status Codes**: Must remain identical (200, 400, 500, etc.)
- **Request Models**: Exact same JSON schema and validation
- **Response Models**: Identical response structure and field names
- **Error Messages**: Same exception types and error message formats
- **Authentication**: Preserve any existing auth mechanisms

### Performance Requirements
- **Response Times**: Must be equivalent or better than current
- **Memory Usage**: Should not exceed current baseline
- **Throughput**: Must handle same or higher request volume
- **Database Performance**: Query patterns and performance maintained

### Testing Strategy
- **Contract Testing**: Automated API contract validation
- **Regression Testing**: Full test suite execution on each phase
- **Performance Testing**: Baseline comparison testing
- **Integration Testing**: End-to-end testing with real clients

## Risk Mitigation

### High-Risk Areas
1. **API Controller Migration**: Web API 2 → ASP.NET Core controller differences
2. **Dependency Injection**: Autofac → Built-in DI container behavioral differences
3. **Configuration**: app.config → appsettings.json migration
4. **Error Handling**: Global exception handling patterns

### Mitigation Strategies
1. **Parallel Implementation**: Keep original and new implementations side-by-side during migration
2. **Extensive Testing**: Automated API testing with real request/response validation
3. **Feature Flags**: Ability to switch between old and new implementations
4. **Rollback Plan**: Clear rollback procedures for each phase

## Validation Framework

### Automated Testing
- **API Contract Tests**: Automated validation of all endpoints
- **Unit Test Suite**: 100% pass rate maintenance
- **Integration Tests**: Database and external service integration
- **Performance Benchmarks**: Automated performance comparison

### Manual Validation
- **Client Application Testing**: Test with actual consuming applications
- **Edge Case Verification**: Manual verification of edge cases and error scenarios
- **User Acceptance Testing**: Business stakeholder validation

## Timeline Estimation

### Phase Duration (Estimates)
- **Phase 1**: 1-2 days (Core models)
- **Phase 2**: 2-3 days (Data access)
- **Phase 3**: 2-3 days (Business logic)
- **Phase 4**: 3-4 days (API layer - highest complexity)
- **Phase 5**: 2-3 days (Testing)
- **Phase 6**: 1-2 days (Console apps)

**Total Estimated Duration**: 11-17 days

### Key Milestones
1. Core foundation migrated and tested
2. Data layer migrated with database connectivity verified
3. Business logic migrated with calculations validated
4. API layer migrated with contract compliance verified
5. Full test suite executing and passing
6. Performance benchmarks meeting requirements

## Success Criteria

### Technical Validation
- [ ] All API endpoints return identical responses for same inputs
- [ ] All unit tests pass with maintained coverage
- [ ] Performance metrics meet or exceed baseline
- [ ] Application runs on Linux and Windows
- [ ] Docker containerization capability

### Business Validation
- [ ] Zero client application changes required
- [ ] All tax calculations produce identical results
- [ ] Error handling maintains same user experience
- [ ] System reliability maintained or improved

## Next Steps

1. **Create Detailed Phase Plans**: Break down each phase into specific tasks
2. **Set Up Validation Infrastructure**: Automated testing and comparison tools
3. **Establish Baseline Metrics**: Performance, memory, and reliability baselines
4. **Begin Phase 1**: Start with low-risk core model migration

This migration approach prioritizes safety and compatibility while enabling the modernization benefits of .NET 8 Core including cross-platform deployment, improved performance, and access to modern .NET ecosystem.
