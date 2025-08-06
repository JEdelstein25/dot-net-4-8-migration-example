# Phase 3: Validation & Documentation - Detailed Plan

## Phase Overview

**Duration**: Week 4  
**Objective**: Comprehensive validation and documentation of the completed migration  
**Risk Level**: LOW (Validation and documentation)  
**Success Criteria**: Migration fully validated with complete documentation and updated CI/CD pipeline  
**Commit Goal**: Migration completed with full validation and documentation  

## Pre-Phase 3 Prerequisites

### Phase 2 Completion Validation
- [x] ASP.NET Core 8 API project created and functional
- [x] All controllers migrated to ASP.NET Core
- [x] Configuration system migrated to appsettings.json
- [x] Dependency injection configured
- [x] API endpoints responding correctly
- [x] Manual testing completed successfully

### Validation Environment Readiness
- [ ] Both original and migrated APIs can run for comparison
- [ ] Testing tools available for comprehensive validation
- [ ] Documentation templates prepared
- [ ] CI/CD pipeline ready for updates

---

## Task Group 1: Comprehensive Contract Validation (Week 4, Days 1-2)

### 1.1 Dockerfile Optimization

**Objective**: Create production-optimized Docker images with security best practices

#### Current Basic Dockerfile:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaxCalculator.Api.Core.dll"]
```

#### Tasks:
1. **Multi-Stage Build Optimization**
   ```dockerfile
   # Stage 1: Base runtime image
   FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
   WORKDIR /app
   EXPOSE 8080
   
   # Create non-root user for security
   RUN addgroup -g 1001 appgroup && \
       adduser -u 1001 -G appgroup -s /bin/sh -D appuser
   
   # Stage 2: Build environment
   FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
   WORKDIR /src
   
   # Copy project files for dependency restoration
   COPY ["TaxCalculator.Api.Core/*.csproj", "TaxCalculator.Api.Core/"]
   COPY ["TaxCalculator.Core/*.csproj", "TaxCalculator.Core/"]
   COPY ["TaxCalculator.Services/*.csproj", "TaxCalculator.Services/"]
   COPY ["TaxCalculator.Data/*.csproj", "TaxCalculator.Data/"]
   COPY ["TaxCalculator.Infrastructure/*.csproj", "TaxCalculator.Infrastructure/"]
   
   # Restore dependencies
   RUN dotnet restore "TaxCalculator.Api.Core/TaxCalculator.Api.Core.csproj"
   
   # Copy source code
   COPY . .
   WORKDIR "/src/TaxCalculator.Api.Core"
   
   # Build application
   RUN dotnet build "TaxCalculator.Api.Core.csproj" -c Release -o /app/build
   
   # Stage 3: Publish
   FROM build AS publish
   RUN dotnet publish "TaxCalculator.Api.Core.csproj" -c Release -o /app/publish \
       --no-restore --no-build
   
   # Stage 4: Final runtime image
   FROM base AS final
   WORKDIR /app
   
   # Install security updates
   RUN apk upgrade --no-cache
   
   # Copy published application
   COPY --from=publish /app/publish .
   
   # Set up non-root user
   RUN chown -R appuser:appgroup /app
   USER appuser
   
   # Health check
   HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
     CMD curl -f http://localhost:8080/api/health || exit 1
   
   ENTRYPOINT ["dotnet", "TaxCalculator.Api.Core.dll"]
   ```

2. **Build Performance Optimization**
   - Layer caching optimization for dependency restoration
   - Multi-architecture support (linux/amd64, linux/arm64)
   - Build argument support for version tagging
   - Source code exclusion (.dockerignore)

3. **Security Hardening**
   ```dockerfile
   # Security best practices
   - Non-root user execution
   - Alpine Linux base (smaller attack surface)
   - Security update installation
   - Minimal package installation
   - No sensitive data in layers
   ```

4. **.dockerignore Optimization**
   ```dockerignore
   # Build artifacts
   bin/
   obj/
   
   # Version control
   .git/
   .gitignore
   
   # IDE files
   .vs/
   .vscode/
   *.user
   
   # Documentation
   *.md
   docs/
   
   # Test files
   **/*Tests/
   **/*.Tests/
   
   # Docker files
   **/Dockerfile*
   **/docker-compose*
   ```

#### Success Criteria:
- ✅ Image size < 150MB (optimized layers)
- ✅ Build time < 3 minutes (with cold cache)
- ✅ Security scan passes with zero high/critical vulnerabilities
- ✅ Multi-architecture builds successful
- ✅ Health check responds within 3 seconds

### 1.2 Container Runtime Configuration

**Objective**: Optimize container runtime for production performance

#### Tasks:
1. **Runtime Performance Tuning**
   ```dockerfile
   # Runtime optimization environment variables
   ENV DOTNET_EnableDiagnostics=0 \
       DOTNET_USE_POLLING_FILE_WATCHER=true \
       ASPNETCORE_URLS=http://+:8080 \
       ASPNETCORE_ENVIRONMENT=Production
   
   # GC optimization for containers
   ENV DOTNET_gcServer=1 \
       DOTNET_gcConcurrent=1 \
       DOTNET_GCHeapCount=2
   ```

2. **Memory and Resource Limits**
   ```yaml
   # Container resource configuration
   resources:
     requests:
       memory: "256Mi"
       cpu: "100m"
     limits:
       memory: "512Mi"
       cpu: "500m"
   ```

3. **Startup Optimization**
   ```csharp
   // Program.cs optimizations
   var builder = WebApplication.CreateBuilder(args);
   
   // Optimize for container startup
   if (builder.Environment.IsProduction())
   {
       builder.Services.Configure<HostOptions>(opts => 
       {
           opts.ServicesStartConcurrently = true;
           opts.ServicesStopConcurrently = true;
       });
   }
   ```

#### Success Criteria:
- ✅ Container starts in < 5 seconds
- ✅ Memory usage < 200MB at idle
- ✅ CPU usage < 10% at idle
- ✅ Ready for traffic within 10 seconds

---

## Task Group 2: Kubernetes Manifests (Week 5, Days 3-4)

### 2.1 Core Kubernetes Resources

**Objective**: Create comprehensive Kubernetes deployment manifests

#### Tasks:
1. **Namespace and Labels**
   ```yaml
   # namespace.yaml
   apiVersion: v1
   kind: Namespace
   metadata:
     name: tax-calculator
     labels:
       app.kubernetes.io/name: tax-calculator
       app.kubernetes.io/version: "1.0.0"
       app.kubernetes.io/component: api
   ```

2. **Deployment Configuration**
   ```yaml
   # deployment.yaml
   apiVersion: apps/v1
   kind: Deployment
   metadata:
     name: tax-calculator-api
     namespace: tax-calculator
     labels:
       app.kubernetes.io/name: tax-calculator
       app.kubernetes.io/component: api
   spec:
     replicas: 3
     strategy:
       type: RollingUpdate
       rollingUpdate:
         maxSurge: 1
         maxUnavailable: 0  # Zero-downtime requirement
     selector:
       matchLabels:
         app.kubernetes.io/name: tax-calculator
         app.kubernetes.io/component: api
     template:
       metadata:
         labels:
           app.kubernetes.io/name: tax-calculator
           app.kubernetes.io/component: api
       spec:
         containers:
         - name: api
           image: tax-calculator:latest
           ports:
           - containerPort: 8080
             name: http
           resources:
             requests:
               memory: "256Mi"
               cpu: "100m"
             limits:
               memory: "512Mi"
               cpu: "500m"
           env:
           - name: ASPNETCORE_ENVIRONMENT
             value: "Production"
           - name: DATABASE_CONNECTION_STRING
             valueFrom:
               secretKeyRef:
                 name: tax-calculator-secrets
                 key: database-connection
           livenessProbe:
             httpGet:
               path: /healthz
               port: 8080
             initialDelaySeconds: 30
             periodSeconds: 10
             failureThreshold: 3
           readinessProbe:
             httpGet:
               path: /api/health
               port: 8080
             initialDelaySeconds: 5
             periodSeconds: 5
             failureThreshold: 2
           startupProbe:
             httpGet:
               path: /api/health
               port: 8080
             initialDelaySeconds: 10
             periodSeconds: 5
             failureThreshold: 6
         securityContext:
           runAsNonRoot: true
           runAsUser: 1001
           fsGroup: 1001
   ```

3. **Service Configuration**
   ```yaml
   # service.yaml
   apiVersion: v1
   kind: Service
   metadata:
     name: tax-calculator-service
     namespace: tax-calculator
     labels:
       app.kubernetes.io/name: tax-calculator
       app.kubernetes.io/component: api
   spec:
     selector:
       app.kubernetes.io/name: tax-calculator
       app.kubernetes.io/component: api
     ports:
     - name: http
       port: 80
       targetPort: 8080
       protocol: TCP
     type: ClusterIP
   ```

4. **ConfigMap and Secrets**
   ```yaml
   # configmap.yaml
   apiVersion: v1
   kind: ConfigMap
   metadata:
     name: tax-calculator-config
     namespace: tax-calculator
   data:
     appsettings.json: |
       {
         "Logging": {
           "LogLevel": {
             "Default": "Information",
             "Microsoft": "Warning"
           }
         },
         "Cache": {
           "Redis": {
             "ExpirationMinutes": 30
           }
         }
       }
   
   ---
   # secrets.yaml
   apiVersion: v1
   kind: Secret
   metadata:
     name: tax-calculator-secrets
     namespace: tax-calculator
   type: Opaque
   data:
     database-connection: <base64-encoded-connection-string>
     redis-connection: <base64-encoded-redis-connection>
   ```

#### Success Criteria:
- ✅ Pods deploy successfully across multiple nodes
- ✅ Rolling updates complete without downtime
- ✅ Health checks prevent traffic to unhealthy pods
- ✅ Resource limits prevent resource starvation
- ✅ Secrets and config mounted correctly

### 2.2 Ingress and Load Balancing

**Objective**: Configure external access and load balancing

#### Tasks:
1. **Ingress Configuration**
   ```yaml
   # ingress.yaml
   apiVersion: networking.k8s.io/v1
   kind: Ingress
   metadata:
     name: tax-calculator-ingress
     namespace: tax-calculator
     annotations:
       kubernetes.io/ingress.class: "nginx"
       cert-manager.io/cluster-issuer: "letsencrypt-prod"
       nginx.ingress.kubernetes.io/rewrite-target: /
       nginx.ingress.kubernetes.io/ssl-redirect: "true"
       nginx.ingress.kubernetes.io/rate-limit: "100"
   spec:
     tls:
     - hosts:
       - api.tax-calculator.com
       secretName: tax-calculator-tls
     rules:
     - host: api.tax-calculator.com
       http:
         paths:
         - path: /
           pathType: Prefix
           backend:
             service:
               name: tax-calculator-service
               port:
                 number: 80
   ```

2. **Network Policies**
   ```yaml
   # network-policy.yaml
   apiVersion: networking.k8s.io/v1
   kind: NetworkPolicy
   metadata:
     name: tax-calculator-network-policy
     namespace: tax-calculator
   spec:
     podSelector:
       matchLabels:
         app.kubernetes.io/name: tax-calculator
     policyTypes:
     - Ingress
     - Egress
     ingress:
     - from:
       - namespaceSelector:
           matchLabels:
             name: ingress-nginx
       ports:
       - protocol: TCP
         port: 8080
     egress:
     - to: []  # Allow all egress (for database, Redis)
       ports:
       - protocol: TCP
         port: 1433  # SQL Server
       - protocol: TCP
         port: 6379  # Redis
   ```

#### Success Criteria:
- ✅ External traffic routes correctly to API
- ✅ SSL termination working properly
- ✅ Rate limiting prevents abuse
- ✅ Network policies enforce security boundaries

---

## Task Group 3: Observability and Monitoring (Week 5, Day 5)

### 3.1 Health Checks and Probes

**Objective**: Comprehensive health monitoring for Kubernetes

#### Tasks:
1. **Enhanced Health Check Implementation**
   ```csharp
   // Program.cs
   builder.Services.AddHealthChecks()
       .AddCheck("self", () => HealthCheckResult.Healthy())
       .AddSqlServer(
           connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
           name: "database",
           timeout: TimeSpan.FromSeconds(5))
       .AddRedis(
           connectionString: builder.Configuration.GetSection("Cache:Redis:ConnectionString").Value,
           name: "cache",
           timeout: TimeSpan.FromSeconds(3));
   
   // Configure health check endpoints
   app.MapHealthChecks("/healthz", new HealthCheckOptions
   {
       Predicate = _ => true,
       ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
   });
   
   app.MapHealthChecks("/api/health", new HealthCheckOptions
   {
       Predicate = _ => false,  // Simple endpoint for backward compatibility
       ResponseWriter = async (context, report) =>
       {
           var response = new { status = "OK", timestamp = DateTime.UtcNow.ToString("O") };
           await context.Response.WriteAsync(JsonSerializer.Serialize(response));
       }
   });
   
   app.MapHealthChecks("/ready", new HealthCheckOptions
   {
       Predicate = check => check.Tags.Contains("ready")
   });
   ```

2. **Custom Health Check Implementation**
   ```csharp
   public class TaxCalculationHealthCheck : IHealthCheck
   {
       private readonly ITaxCalculationService _taxService;
       
       public TaxCalculationHealthCheck(ITaxCalculationService taxService)
       {
           _taxService = taxService;
       }
       
       public async Task<HealthCheckResult> CheckHealthAsync(
           HealthCheckContext context, 
           CancellationToken cancellationToken = default)
       {
           try
           {
               // Perform a lightweight calculation test
               var testResult = await _taxService.CalculateTaxAsync(new TaxCalculationRequest
               {
                   TaxableIncome = 50000,
                   FinancialYear = "2023-24"
               });
               
               return testResult != null 
                   ? HealthCheckResult.Healthy("Tax calculation service is working")
                   : HealthCheckResult.Unhealthy("Tax calculation returned null");
           }
           catch (Exception ex)
           {
               return HealthCheckResult.Unhealthy("Tax calculation failed", ex);
           }
       }
   }
   ```

#### Success Criteria:
- ✅ Kubernetes probes detect unhealthy pods correctly
- ✅ Database connectivity monitored
- ✅ Cache connectivity monitored
- ✅ Business logic health validated

### 3.2 Metrics and Logging

**Objective**: Production-grade observability

#### Tasks:
1. **Prometheus Metrics Integration**
   ```csharp
   // Add metrics collection
   builder.Services.AddSingleton<IMetrics, Metrics>();
   
   // Custom metrics
   public class TaxCalculationMetrics
   {
       private readonly Counter _calculationRequests;
       private readonly Histogram _calculationDuration;
       private readonly Counter _calculationErrors;
       
       public TaxCalculationMetrics()
       {
           _calculationRequests = Metrics.CreateCounter(
               "tax_calculations_total", 
               "Total number of tax calculations");
               
           _calculationDuration = Metrics.CreateHistogram(
               "tax_calculation_duration_seconds",
               "Duration of tax calculations");
               
           _calculationErrors = Metrics.CreateCounter(
               "tax_calculation_errors_total",
               "Total number of tax calculation errors");
       }
   }
   ```

2. **Structured Logging Configuration**
   ```csharp
   builder.Services.AddLogging(builder =>
   {
       builder.AddConsole(options =>
       {
           options.FormatterName = "json";
       });
       builder.AddJsonConsole();
   });
   
   // Correlation ID middleware
   app.UseMiddleware<CorrelationIdMiddleware>();
   ```

3. **Application Performance Monitoring**
   ```yaml
   # ServiceMonitor for Prometheus
   apiVersion: monitoring.coreos.com/v1
   kind: ServiceMonitor
   metadata:
     name: tax-calculator-metrics
     namespace: tax-calculator
   spec:
     selector:
       matchLabels:
         app.kubernetes.io/name: tax-calculator
     endpoints:
     - port: http
       path: /metrics
       interval: 30s
   ```

#### Success Criteria:
- ✅ Application metrics collected by Prometheus
- ✅ Structured logs ingested by log aggregation system
- ✅ Correlation IDs trace requests across services
- ✅ Performance metrics within acceptable ranges

---

## Task Group 4: Security Hardening (Week 6, Days 1-2)

### 4.1 Container Security

**Objective**: Implement security best practices for container deployment

#### Tasks:
1. **Image Security Scanning**
   ```yaml
   # GitHub Actions security scan
   - name: Run Trivy vulnerability scanner
     uses: aquasecurity/trivy-action@master
     with:
       image-ref: 'tax-calculator:${{ github.sha }}'
       format: 'sarif'
       output: 'trivy-results.sarif'
   
   - name: Upload Trivy scan results to GitHub Security tab
     uses: github/codeql-action/upload-sarif@v2
     with:
       sarif_file: 'trivy-results.sarif'
   ```

2. **Pod Security Standards**
   ```yaml
   # Pod Security Policy
   apiVersion: v1
   kind: Pod
   spec:
     securityContext:
       runAsNonRoot: true
       runAsUser: 1001
       fsGroup: 1001
       seccompProfile:
         type: RuntimeDefault
     containers:
     - name: api
       securityContext:
         allowPrivilegeEscalation: false
         readOnlyRootFilesystem: true
         capabilities:
           drop:
           - ALL
       volumeMounts:
       - name: tmp
         mountPath: /tmp
         readOnly: false
     volumes:
     - name: tmp
       emptyDir: {}
   ```

3. **Secret Management**
   ```yaml
   # External Secrets Operator (if using Azure Key Vault)
   apiVersion: external-secrets.io/v1beta1
   kind: SecretStore
   metadata:
     name: azure-keyvault-store
     namespace: tax-calculator
   spec:
     provider:
       azurekv:
         tenantId: "${AZURE_TENANT_ID}"
         vaultUrl: "https://tax-calc-vault.vault.azure.net/"
         authSecretRef:
           clientId:
             name: azure-secret
             key: client-id
           clientSecret:
             name: azure-secret
             key: client-secret
   ```

#### Success Criteria:
- ✅ Container images pass security scans (zero critical vulnerabilities)
- ✅ Pods run with minimal privileges
- ✅ Secrets properly encrypted and managed
- ✅ Network policies limit access appropriately

### 4.2 API Security

**Objective**: Secure API endpoints and communication

#### Tasks:
1. **Rate Limiting Implementation**
   ```csharp
   // Rate limiting middleware
   builder.Services.AddRateLimiter(options =>
   {
       options.AddFixedWindowLimiter("api", limiterOptions =>
       {
           limiterOptions.PermitLimit = 100;
           limiterOptions.Window = TimeSpan.FromMinutes(1);
           limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
           limiterOptions.QueueLimit = 10;
       });
   });
   
   app.UseRateLimiter();
   ```

2. **Security Headers**
   ```csharp
   app.Use(async (context, next) =>
   {
       context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
       context.Response.Headers.Add("X-Frame-Options", "DENY");
       context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
       context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
       
       await next();
   });
   ```

3. **Input Validation Enhancement**
   ```csharp
   [ApiController]
   public class TaxController : ControllerBase
   {
       [HttpPost("calculate")]
       public async Task<IActionResult> Calculate([FromBody] TaxCalculationRequest request)
       {
           // Enhanced input validation
           if (!ModelState.IsValid)
           {
               var errors = ModelState.Values
                   .SelectMany(v => v.Errors)
                   .Select(e => e.ErrorMessage);
               return BadRequest(string.Join("; ", errors));
           }
           
           // Additional business validation
           if (request.TaxableIncome > 1_000_000_000) // Reasonable upper limit
           {
               return BadRequest("Taxable income exceeds maximum allowed value");
           }
           
           // Proceed with calculation...
       }
   }
   ```

#### Success Criteria:
- ✅ Rate limiting prevents abuse
- ✅ Security headers properly configured
- ✅ Input validation prevents injection attacks
- ✅ Error responses don't leak sensitive information

---

## Task Group 5: Performance Testing and Optimization (Week 6, Days 3-4)

### 5.1 Load Testing Setup

**Objective**: Validate performance under production-like load

#### Tasks:
1. **K6 Load Testing Scripts**
   ```javascript
   // load-test.js
   import http from 'k6/http';
   import { check, sleep } from 'k6';
   
   export let options = {
     stages: [
       { duration: '2m', target: 10 },   // Ramp up
       { duration: '5m', target: 50 },   // Stay at 50 users
       { duration: '2m', target: 100 },  // Ramp to 100 users
       { duration: '5m', target: 100 },  // Stay at 100 users
       { duration: '2m', target: 0 },    // Ramp down
     ],
     thresholds: {
       http_req_duration: ['p(95)<500'], // 95% of requests under 500ms
       http_req_failed: ['rate<0.1'],    // Error rate under 10%
     },
   };
   
   export default function () {
     const payload = JSON.stringify({
       taxableIncome: Math.random() * 150000,
       financialYear: '2023-24',
       residencyStatus: 'Resident',
       includeMedicareLevy: true
     });
   
     const params = {
       headers: {
         'Content-Type': 'application/json',
       },
     };
   
     let response = http.post('http://api.tax-calculator.com/api/tax/calculate', payload, params);
     
     check(response, {
       'status is 200': (r) => r.status === 200,
       'response time < 500ms': (r) => r.timings.duration < 500,
       'has tax result': (r) => JSON.parse(r.body).totalTax !== undefined,
     });
   
     sleep(1);
   }
   ```

2. **Performance Baseline Validation**
   ```bash
   # Run baseline test
   k6 run --out json=baseline-results.json load-test.js
   
   # Compare with .NET Framework baseline
   k6 run --out json=dotnet8-results.json load-test.js
   
   # Generate comparison report
   node compare-performance.js baseline-results.json dotnet8-results.json
   ```

3. **Database Performance Testing**
   ```sql
   -- Database load simulation
   DECLARE @i INT = 1;
   WHILE @i <= 1000
   BEGIN
       EXEC GetTaxBrackets '2023-24';
       SET @i = @i + 1;
   END
   ```

#### Success Criteria:
- ✅ 95th percentile response time < 500ms
- ✅ Error rate < 1% under normal load
- ✅ System handles 100 concurrent users
- ✅ Memory usage remains stable under load
- ✅ Database connection pool handles load efficiently

### 5.2 Performance Optimization

**Objective**: Optimize application performance for container deployment

#### Tasks:
1. **Memory Optimization**
   ```csharp
   // Optimize GC for containers
   public static void Main(string[] args)
   {
       if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
       {
           GCSettings.LatencyMode = GCLatencyMode.Batch;
       }
       
       CreateHostBuilder(args).Build().Run();
   }
   ```

2. **Connection Pool Optimization**
   ```csharp
   // Optimize SQL connection pooling
   builder.Services.AddScoped<IConnectionFactory>(provider =>
   {
       var connectionString = provider.GetRequiredService<IConfiguration>()
           .GetConnectionString("DefaultConnection");
       
       // Add pooling parameters for container environment
       var builder = new SqlConnectionStringBuilder(connectionString)
       {
           Pooling = true,
           MinPoolSize = 5,
           MaxPoolSize = 20,
           ConnectionTimeout = 30
       };
       
       return new SqlConnectionFactory(builder.ConnectionString);
   });
   ```

3. **Redis Optimization**
   ```csharp
   // Redis connection multiplexer optimization
   builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
   {
       var configuration = provider.GetRequiredService<IConfiguration>();
       var connectionString = configuration.GetSection("Cache:Redis:ConnectionString").Value;
       
       var config = ConfigurationOptions.Parse(connectionString);
       config.AbortOnConnectFail = false;
       config.ConnectRetry = 3;
       config.ConnectTimeout = 5000;
       
       return ConnectionMultiplexer.Connect(config);
   });
   ```

#### Success Criteria:
- ✅ Memory usage optimized for container limits
- ✅ Database connections efficiently managed
- ✅ Cache performance meets requirements
- ✅ GC pressure minimized

---

## Task Group 6: Deployment Automation (Week 6, Day 5)

### 6.1 Helm Chart Creation

**Objective**: Create reusable Helm charts for deployment automation

#### Tasks:
1. **Helm Chart Structure**
   ```
   charts/tax-calculator/
   ├── Chart.yaml
   ├── values.yaml
   ├── values-production.yaml
   ├── values-staging.yaml
   └── templates/
       ├── deployment.yaml
       ├── service.yaml
       ├── ingress.yaml
       ├── configmap.yaml
       ├── secret.yaml
       ├── hpa.yaml
       └── _helpers.tpl
   ```

2. **Chart Configuration**
   ```yaml
   # Chart.yaml
   apiVersion: v2
   name: tax-calculator
   description: A Helm chart for Tax Calculator API
   version: 1.0.0
   appVersion: "1.0.0"
   ```

3. **Values Configuration**
   ```yaml
   # values.yaml
   replicaCount: 3
   
   image:
     repository: tax-calculator
     pullPolicy: IfNotPresent
     tag: "latest"
   
   service:
     type: ClusterIP
     port: 80
     targetPort: 8080
   
   ingress:
     enabled: true
     className: "nginx"
     annotations:
       cert-manager.io/cluster-issuer: letsencrypt-prod
     hosts:
       - host: api.tax-calculator.com
         paths:
           - path: /
             pathType: Prefix
     tls:
       - secretName: tax-calculator-tls
         hosts:
           - api.tax-calculator.com
   
   resources:
     limits:
       cpu: 500m
       memory: 512Mi
     requests:
       cpu: 100m
       memory: 256Mi
   
   autoscaling:
     enabled: true
     minReplicas: 3
     maxReplicas: 10
     targetCPUUtilizationPercentage: 70
     targetMemoryUtilizationPercentage: 80
   
   nodeSelector: {}
   tolerations: []
   affinity: {}
   ```

#### Success Criteria:
- ✅ Helm chart deploys successfully
- ✅ Configuration overrides work correctly
- ✅ Multiple environments supported
- ✅ Rollback functionality operational

### 6.2 CI/CD Pipeline Enhancement

**Objective**: Complete CI/CD pipeline for automated deployment

#### Tasks:
1. **Enhanced GitHub Actions Workflow**
   ```yaml
   name: Build and Deploy to Kubernetes
   
   on:
     push:
       branches: [ dot-net-core-8-upgrade ]
     pull_request:
       branches: [ dot-net-core-8-upgrade ]
   
   jobs:
     build:
       runs-on: ubuntu-latest
       
       steps:
       - uses: actions/checkout@v4
       
       - name: Set up .NET 8
         uses: actions/setup-dotnet@v3
         with:
           dotnet-version: 8.0.x
       
       - name: Restore dependencies
         run: dotnet restore
       
       - name: Build
         run: dotnet build --no-restore --configuration Release
       
       - name: Test
         run: dotnet test --no-build --configuration Release --logger trx --results-directory "TestResults-Unit"
       
       - name: Publish Test Results
         uses: dorny/test-reporter@v1
         if: success() || failure()
         with:
           name: Unit Tests
           path: TestResults-Unit/*.trx
           reporter: dotnet-trx
       
       - name: Build Docker image
         run: |
           docker build -t tax-calculator:${{ github.sha }} .
           docker tag tax-calculator:${{ github.sha }} tax-calculator:latest
       
       - name: Run Trivy vulnerability scanner
         uses: aquasecurity/trivy-action@master
         with:
           image-ref: 'tax-calculator:${{ github.sha }}'
           format: 'sarif'
           output: 'trivy-results.sarif'
       
       - name: Upload Trivy scan results
         uses: github/codeql-action/upload-sarif@v2
         with:
           sarif_file: 'trivy-results.sarif'
       
       - name: Push to registry
         if: github.ref == 'refs/heads/dot-net-core-8-upgrade'
         run: |
           echo ${{ secrets.REGISTRY_PASSWORD }} | docker login ${{ secrets.REGISTRY_URL }} -u ${{ secrets.REGISTRY_USERNAME }} --password-stdin
           docker push tax-calculator:${{ github.sha }}
           docker push tax-calculator:latest
   
     deploy:
       needs: build
       runs-on: ubuntu-latest
       if: github.ref == 'refs/heads/dot-net-core-8-upgrade'
       
       steps:
       - uses: actions/checkout@v4
       
       - name: Set up Kubectl
         uses: azure/setup-kubectl@v3
         with:
           version: 'v1.28.0'
       
       - name: Set up Helm
         uses: azure/setup-helm@v3
         with:
           version: '3.12.0'
       
       - name: Deploy to staging
         run: |
           helm upgrade --install tax-calculator-staging ./charts/tax-calculator \
             --namespace tax-calculator-staging \
             --create-namespace \
             --values ./charts/tax-calculator/values-staging.yaml \
             --set image.tag=${{ github.sha }} \
             --wait
       
       - name: Run integration tests
         run: |
           kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=tax-calculator -n tax-calculator-staging --timeout=300s
           # Run contract validation tests against staging
           dotnet test TaxCalculator.Tests.Integration --logger trx --results-directory "TestResults-Integration"
       
       - name: Deploy to production
         if: success()
         run: |
           helm upgrade --install tax-calculator-prod ./charts/tax-calculator \
             --namespace tax-calculator-prod \
             --create-namespace \
             --values ./charts/tax-calculator/values-production.yaml \
             --set image.tag=${{ github.sha }} \
             --wait
   ```

#### Success Criteria:
- ✅ Pipeline builds and tests automatically
- ✅ Security scanning prevents vulnerable deployments
- ✅ Staging deployment validates functionality
- ✅ Production deployment succeeds without downtime
- ✅ Rollback capability functional

---

## Phase 3 Validation & Success Criteria

### Container Validation
- [ ] **Image Security**: Zero critical/high vulnerabilities in container scans
- [ ] **Performance**: Container starts in <5 seconds, memory usage <200MB idle
- [ ] **Multi-Architecture**: Images build for both amd64 and arm64
- [ ] **Health Checks**: All health endpoints respond correctly

### Kubernetes Validation
- [ ] **Deployment**: Pods deploy successfully across multiple nodes
- [ ] **Scaling**: Horizontal Pod Autoscaler functions correctly
- [ ] **Rolling Updates**: Updates complete without service interruption
- [ ] **Network Security**: Network policies enforce proper segmentation

### Observability Validation
- [ ] **Metrics**: Prometheus collects application and infrastructure metrics
- [ ] **Logging**: Structured logs flow to centralized logging system
- [ ] **Tracing**: Request correlation IDs trace end-to-end
- [ ] **Alerting**: Critical alerts fire appropriately

### Performance Validation
- [ ] **Load Testing**: System handles target load (100 concurrent users)
- [ ] **Response Times**: 95th percentile <500ms under normal load
- [ ] **Error Rates**: <1% error rate under normal operation
- [ ] **Resource Usage**: Memory and CPU within expected limits

### Security Validation
- [ ] **Pod Security**: Containers run with minimal privileges
- [ ] **Network Security**: Traffic properly segmented and secured
- [ ] **Secrets**: Sensitive data encrypted and properly managed
- [ ] **Rate Limiting**: API abuse protection functional

---

## Phase 3 Delivery Checklist

### Infrastructure Documentation
- [ ] Kubernetes deployment guide
- [ ] Monitoring and alerting runbook
- [ ] Security configuration documentation
- [ ] Performance tuning guide

### Operational Readiness
- [ ] Production deployment procedures
- [ ] Incident response procedures
- [ ] Backup and recovery procedures
- [ ] Scaling procedures

### Quality Assurance
- [ ] All security scans pass
- [ ] Performance baselines established
- [ ] Load testing results documented
- [ ] Chaos testing completed

---

## Rollback Plan for Phase 3

### Rollback Triggers
- Container security vulnerabilities (critical/high)
- Performance degradation >50%
- Kubernetes deployment failures
- Monitoring/observability issues

### Rollback Procedure
1. **Helm Rollback**: `helm rollback tax-calculator [REVISION]`
2. **DNS/Ingress**: Switch traffic back if needed
3. **Validation**: Confirm service restoration
4. **Investigation**: Root cause analysis

### Recovery Time Objective
- **Detection**: Within 5 minutes via monitoring
- **Decision**: Within 2 minutes of detection
- **Rollback**: Within 3 minutes via Helm
- **Validation**: Within 2 minutes post-rollback

---

*Phase 3 Success Criteria: Production-ready containerized application running reliably in Kubernetes with comprehensive observability and security.*
