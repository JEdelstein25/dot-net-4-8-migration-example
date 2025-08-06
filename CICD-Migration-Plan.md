# CI/CD Migration Plan: .NET Framework 4.8 to .NET 8

## Current State Analysis

### Current CI/CD Pipeline (.github/workflows/ci.yml)

**Build Process:**
- **Runner**: Windows Server 2022
- **Build Tools**: MSBuild with Visual Studio toolchain
- **Package Management**: NuGet with packages.config (legacy format)
- **Target Framework**: .NET Framework 4.8
- **Build Strategy**: Individual project builds with MSBuild commands
- **Test Framework**: NUnit (manual execution via console runner)

**Pipeline Structure:**
1. **build-and-test** job:
   - Package restoration (individual projects)
   - Sequential MSBuild compilation
   - Build verification and artifact validation
   - NUnit test execution (with fallback handling)
   - Build artifact uploading

2. **code-analysis** job:
   - Basic code metrics (file count, LOC)
   - Static analysis placeholder

3. **security-scan** job:
   - Hardcoded connection string detection
   - Secret/API key scanning

### Current Technology Stack
- **.NET Framework**: 4.8
- **Build System**: MSBuild (legacy .csproj format)
- **Package Management**: packages.config
- **Test Framework**: NUnit 3.x
- **Project Structure**: Traditional Visual Studio solution
- **Deployment**: Build artifacts (executables/libraries)

### Current Limitations
- Windows-only builds (due to .NET Framework dependency)
- Legacy package management system
- Manual test discovery and execution
- No containerization support
- Limited cross-platform compatibility
- Basic quality gates
- No deployment automation

## Migration Strategy

### Phase 1: Infrastructure Preparation (Weeks 1-2)

#### 1.1 Dual-Pipeline Setup
Create parallel workflows to support both .NET Framework 4.8 and .NET 8:

```yaml
# .github/workflows/dotnet-framework.yml (existing, renamed)
# .github/workflows/dotnet8-migration.yml (new)
```

#### 1.2 Environment Configuration
- Add Ubuntu runners for .NET 8 builds
- Maintain Windows runners for .NET Framework compatibility
- Configure Docker build environment

### Phase 2: .NET 8 Pipeline Development (Weeks 3-4)

#### 2.1 New Workflow Structure

```yaml
name: .NET 8 CI/CD Pipeline

on:
  push:
    branches: [ main, develop, feature/dotnet8-migration ]
  pull_request:
    branches: [ main, develop ]

env:
  DOTNET_VERSION: '8.0.x'
  DOCKER_REGISTRY: 'your-registry.azurecr.io'
  IMAGE_NAME: 'tax-calculator'

jobs:
  build:
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest]
    runs-on: ${{ matrix.os }}
    
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --configuration Release --no-restore
      
    - name: Test
      run: dotnet test --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
    - name: Upload coverage reports
      uses: codecov/codecov-action@v4
      if: matrix.os == 'ubuntu-latest'
```

#### 2.2 Docker Integration

**Multi-stage Dockerfile for .NET 8:**

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY TaxCalculator.Core/*.csproj ./TaxCalculator.Core/
COPY TaxCalculator.Api/*.csproj ./TaxCalculator.Api/
COPY TaxCalculator.Services/*.csproj ./TaxCalculator.Services/
COPY TaxCalculator.Data/*.csproj ./TaxCalculator.Data/
COPY TaxCalculator.Tests.Unit/*.csproj ./TaxCalculator.Tests.Unit/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and test
RUN dotnet build --configuration Release --no-restore
RUN dotnet test --configuration Release --no-build

# Publish
RUN dotnet publish TaxCalculator.Api/TaxCalculator.Api.csproj \
    --configuration Release \
    --no-build \
    --output /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaxCalculator.Api.dll"]
```

#### 2.3 Recommended Docker Images

**Build Images:**
- `mcr.microsoft.com/dotnet/sdk:8.0` - Full SDK for building
- `mcr.microsoft.com/dotnet/sdk:8.0-alpine` - Lightweight SDK

**Runtime Images:**
- `mcr.microsoft.com/dotnet/aspnet:8.0` - ASP.NET Core runtime
- `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` - Lightweight runtime
- `mcr.microsoft.com/dotnet/runtime:8.0` - Core runtime only

### Phase 3: Enhanced Quality Gates (Weeks 5-6)

#### 3.1 Advanced Code Analysis

```yaml
  code-quality:
    runs-on: ubuntu-latest
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
        
    - name: Install SonarScanner
      run: dotnet tool install --global dotnet-sonarscanner
      
    - name: SonarCloud Analysis
      run: |
        dotnet sonarscanner begin \
          /k:"tax-calculator" \
          /o:"your-org" \
          /d:sonar.token="${{ secrets.SONAR_TOKEN }}" \
          /d:sonar.host.url="https://sonarcloud.io" \
          /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"
        dotnet build
        dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
        dotnet sonarscanner end /d:sonar.token="${{ secrets.SONAR_TOKEN }}"
```

#### 3.2 Security Scanning

```yaml
  security:
    runs-on: ubuntu-latest
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Run Trivy vulnerability scanner
      uses: aquasecurity/trivy-action@master
      with:
        scan-type: 'fs'
        scan-ref: '.'
        format: 'sarif'
        output: 'trivy-results.sarif'
        
    - name: Upload Trivy scan results
      uses: github/codeql-action/upload-sarif@v3
      with:
        sarif_file: 'trivy-results.sarif'
```

### Phase 4: Deployment Pipeline (Weeks 7-8)

#### 4.1 Container Build and Push

```yaml
  build-container:
    needs: [build, code-quality, security]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v3
      
    - name: Login to Container Registry
      uses: docker/login-action@v3
      with:
        registry: ${{ env.DOCKER_REGISTRY }}
        username: ${{ secrets.REGISTRY_USERNAME }}
        password: ${{ secrets.REGISTRY_PASSWORD }}
        
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v5
      with:
        images: ${{ env.DOCKER_REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=ref,event=pr
          type=sha,prefix={{branch}}-
          type=raw,value=latest,enable={{is_default_branch}}
          
    - name: Build and push
      uses: docker/build-push-action@v5
      with:
        context: .
        platforms: linux/amd64,linux/arm64
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
        cache-from: type=gha
        cache-to: type=gha,mode=max
```

#### 4.2 Kubernetes Deployment

```yaml
  deploy:
    needs: build-container
    runs-on: ubuntu-latest
    environment: production
    
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Setup kubectl
      uses: azure/setup-kubectl@v3
      
    - name: Set Kubernetes context
      uses: azure/k8s-set-context@v3
      with:
        method: kubeconfig
        kubeconfig: ${{ secrets.KUBE_CONFIG }}
        
    - name: Deploy to Kubernetes
      uses: azure/k8s-deploy@v4
      with:
        manifests: |
          k8s/deployment.yaml
          k8s/service.yaml
          k8s/ingress.yaml
        images: ${{ env.DOCKER_REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
        namespace: tax-calculator
```

### Phase 5: Parallel Execution Strategy (Weeks 9-10)

#### 5.1 Branch-based Strategy

```yaml
# Workflow trigger logic
on:
  push:
    branches: 
      - main
      - develop
      - 'feature/**'
  pull_request:
    branches: 
      - main
      - develop

jobs:
  detect-changes:
    runs-on: ubuntu-latest
    outputs:
      framework-changed: ${{ steps.changes.outputs.framework }}
      dotnet8-changed: ${{ steps.changes.outputs.dotnet8 }}
    steps:
    - uses: actions/checkout@v4
    - uses: dorny/paths-filter@v2
      id: changes
      with:
        filters: |
          framework:
            - '**/*.config'
            - '**/*.csproj'
            - 'packages.config'
          dotnet8:
            - '**/Program.cs'
            - '**/*.csproj'
            - 'src/**'

  framework-build:
    needs: detect-changes
    if: needs.detect-changes.outputs.framework-changed == 'true'
    # ... existing framework build

  dotnet8-build:
    needs: detect-changes
    if: needs.detect-changes.outputs.dotnet8-changed == 'true'
    # ... new .NET 8 build
```

## Container Strategy

### Multi-Architecture Support

```dockerfile
# Dockerfile.net8
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /source

# Build for target architecture
RUN dotnet publish --arch $TARGETARCH --os linux --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY --from=build /app /app
WORKDIR /app
ENTRYPOINT ["./TaxCalculator.Api"]
```

### Build Optimization

```yaml
# Docker build with cache optimization
- name: Build and push with cache
  uses: docker/build-push-action@v5
  with:
    context: .
    file: ./Dockerfile.net8
    platforms: linux/amd64,linux/arm64
    cache-from: type=gha
    cache-to: type=gha,mode=max
    build-args: |
      BUILDKIT_INLINE_CACHE=1
```

## Kubernetes Integration

### Base Deployment Manifests

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tax-calculator-api
  namespace: tax-calculator
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tax-calculator-api
  template:
    metadata:
      labels:
        app: tax-calculator-api
    spec:
      containers:
      - name: api
        image: your-registry.azurecr.io/tax-calculator:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: tax-calculator-secrets
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
```

## Migration Timeline

### Week 1-2: Foundation
- [ ] Create parallel workflow files
- [ ] Set up .NET 8 development environment
- [ ] Create basic Dockerfiles
- [ ] Establish container registry

### Week 3-4: Core Pipeline
- [ ] Implement .NET 8 build pipeline
- [ ] Add comprehensive testing
- [ ] Integrate code coverage
- [ ] Set up artifact management

### Week 5-6: Quality & Security
- [ ] Implement SonarCloud integration
- [ ] Add security scanning
- [ ] Performance testing setup
- [ ] Integration testing framework

### Week 7-8: Deployment
- [ ] Container build automation
- [ ] Kubernetes manifest creation
- [ ] Environment promotion strategy
- [ ] Rollback procedures

### Week 9-10: Parallel Operation
- [ ] Branch-based pipeline selection
- [ ] Feature flag integration
- [ ] Monitoring and observability
- [ ] Documentation and training

### Week 11-12: Migration Completion
- [ ] Feature parity validation
- [ ] Performance comparison
- [ ] Legacy pipeline deprecation
- [ ] Team training and handover

## Success Metrics

### Build Performance
- **Build Time**: Target <5 minutes (vs current ~8 minutes)
- **Test Execution**: Target <2 minutes
- **Container Build**: Target <3 minutes

### Quality Gates
- **Code Coverage**: Minimum 80%
- **Security Vulnerabilities**: Zero high/critical
- **Code Duplication**: <5%
- **Technical Debt**: <1 day

### Deployment Metrics
- **Deployment Frequency**: Multiple times per day
- **Lead Time**: <2 hours from commit to production
- **MTTR**: <15 minutes
- **Change Failure Rate**: <5%

## Risk Mitigation

### Technical Risks
1. **Compatibility Issues**: Maintain parallel pipelines during transition
2. **Performance Degradation**: Comprehensive benchmarking
3. **Security Vulnerabilities**: Enhanced scanning and monitoring
4. **Data Migration**: Isolated test environments

### Operational Risks
1. **Team Knowledge Gap**: Training and documentation
2. **Tool Dependencies**: Fallback procedures
3. **Infrastructure Changes**: Gradual migration approach
4. **Rollback Requirements**: Automated rollback procedures

## Post-Migration Benefits

### Performance Improvements
- Cross-platform deployment capabilities
- Improved containerization support
- Better resource utilization
- Enhanced development productivity

### Security Enhancements
- Modern security frameworks
- Container security scanning
- Runtime protection
- Compliance automation

### Operational Excellence
- Infrastructure as Code
- Automated deployment
- Comprehensive monitoring
- Self-healing capabilities

This migration plan provides a structured approach to transitioning from .NET Framework 4.8 to .NET 8 while maintaining service reliability and introducing modern DevOps practices.
