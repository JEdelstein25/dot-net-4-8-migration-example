# Phase 4: Production Deployment & Validation - Detailed Plan

## Phase Overview

**Duration**: Weeks 7-8  
**Objective**: Zero-downtime production deployment with complete validation of backward compatibility  
**Risk Level**: HIGH (Production impact)  
**Success Criteria**: Production system running .NET Core 8 with 100% client compatibility and performance parity  

## Pre-Phase 4 Prerequisites

### Phase 3 Completion Validation
- [x] Containerized application running reliably in staging
- [x] All security scans passing
- [x] Performance testing completed and optimized
- [x] Monitoring and observability operational
- [x] Helm charts and CI/CD pipeline functional

### Production Readiness
- [ ] Production Kubernetes cluster validated
- [ ] Production database and Redis instances ready
- [ ] SSL certificates installed and configured
- [ ] Monitoring infrastructure deployed
- [ ] Backup and disaster recovery tested
- [ ] Client applications identified and contact points established

---

## Task Group 1: Pre-Production Validation (Week 7, Days 1-2)

### 1.1 Production Environment Setup

**Objective**: Ensure production environment is identical to staging and fully operational

#### Tasks:
1. **Infrastructure Validation**
   ```bash
   # Kubernetes cluster health check
   kubectl cluster-info
   kubectl get nodes -o wide
   kubectl get pods --all-namespaces
   
   # Resource availability check
   kubectl describe nodes | grep -E "Capacity|Allocatable"
   
   # Storage class validation
   kubectl get storageclass
   ```

2. **Network Connectivity Validation**
   ```bash
   # Test database connectivity from cluster
   kubectl run test-pod --image=mcr.microsoft.com/mssql-tools \
     --rm -it --restart=Never -- \
     sqlcmd -S "$DATABASE_SERVER" -U "$DATABASE_USER" -P "$DATABASE_PASSWORD" -Q "SELECT 1"
   
   # Test Redis connectivity
   kubectl run redis-test --image=redis:alpine \
     --rm -it --restart=Never -- \
     redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" ping
   ```

3. **SSL Certificate Validation**
   ```bash
   # Verify SSL certificate installation
   openssl s_client -connect api.tax-calculator.com:443 -servername api.tax-calculator.com
   
   # Check certificate expiration
   echo | openssl s_client -servername api.tax-calculator.com -connect api.tax-calculator.com:443 2>/dev/null | openssl x509 -noout -dates
   ```

#### Success Criteria:
- ✅ Kubernetes cluster healthy and accessible
- ✅ Database and Redis connectivity confirmed
- ✅ SSL certificates valid and properly configured
- ✅ DNS resolution working correctly
- ✅ Network policies allowing required traffic

### 1.2 Disaster Recovery Testing

**Objective**: Validate backup and recovery procedures before production deployment

#### Tasks:
1. **Database Backup and Restore Testing**
   ```sql
   -- Create test backup
   BACKUP DATABASE TaxCalculator 
   TO DISK = '/backups/TaxCalculator_PreMigration.bak'
   WITH FORMAT, INIT, COMPRESSION;
   
   -- Test restore on staging database
   RESTORE DATABASE TaxCalculator_Test 
   FROM DISK = '/backups/TaxCalculator_PreMigration.bak'
   WITH MOVE 'TaxCalculator' TO '/data/TaxCalculator_Test.mdf',
        MOVE 'TaxCalculator_Log' TO '/logs/TaxCalculator_Test.ldf';
   ```

2. **Kubernetes Backup Validation**
   ```bash
   # Test etcd backup (if managing cluster)
   etcdctl snapshot save /backup/etcd-snapshot.db
   
   # Test persistent volume backup
   kubectl apply -f - <<EOF
   apiVersion: batch/v1
   kind: Job
   metadata:
     name: backup-test
   spec:
     template:
       spec:
         containers:
         - name: backup
           image: alpine
           command: ["/bin/sh", "-c"]
           args: ["cp -r /data/* /backup/"]
           volumeMounts:
           - name: data
             mountPath: /data
           - name: backup
             mountPath: /backup
         volumes:
         - name: data
           persistentVolumeClaim:
             claimName: app-data
         - name: backup
           hostPath:
             path: /backup
         restartPolicy: Never
   EOF
   ```

3. **Recovery Time Testing**
   ```bash
   # Time a full application recovery
   time helm install tax-calculator-recovery ./charts/tax-calculator \
     --namespace tax-calculator-recovery \
     --create-namespace \
     --values ./charts/tax-calculator/values-production.yaml \
     --wait
   ```

#### Success Criteria:
- ✅ Database backup and restore completed in <10 minutes
- ✅ Application recovery completed in <5 minutes
- ✅ All recovery procedures documented and tested
- ✅ Recovery time objectives met

---

## Task Group 2: Blue-Green Deployment Strategy (Week 7, Days 3-4)

### 2.1 Blue-Green Infrastructure Setup

**Objective**: Implement zero-downtime deployment using blue-green strategy

#### Tasks:
1. **Dual Environment Configuration**
   ```yaml
   # Blue environment (current .NET Framework)
   apiVersion: v1
   kind: Service
   metadata:
     name: tax-calculator-blue
     labels:
       version: blue
       app: tax-calculator
   spec:
     selector:
       app: tax-calculator
       version: blue
     ports:
     - port: 80
       targetPort: 80
   
   ---
   # Green environment (.NET Core 8)
   apiVersion: v1
   kind: Service
   metadata:
     name: tax-calculator-green
     labels:
       version: green
       app: tax-calculator
   spec:
     selector:
       app: tax-calculator
       version: green
     ports:
     - port: 80
       targetPort: 8080
   ```

2. **Traffic Routing Configuration**
   ```yaml
   # Ingress with traffic splitting capability
   apiVersion: networking.k8s.io/v1
   kind: Ingress
   metadata:
     name: tax-calculator-ingress
     annotations:
       nginx.ingress.kubernetes.io/canary: "true"
       nginx.ingress.kubernetes.io/canary-weight: "0"  # Start with 0% to green
   spec:
     rules:
     - host: api.tax-calculator.com
       http:
         paths:
         - path: /
           pathType: Prefix
           backend:
             service:
               name: tax-calculator-green
               port:
                 number: 80
   ```

3. **Automated Traffic Switching Script**
   ```bash
   #!/bin/bash
   # traffic-switch.sh
   
   NAMESPACE="tax-calculator-prod"
   INGRESS_NAME="tax-calculator-ingress"
   
   switch_to_green() {
       echo "Switching traffic to green environment..."
       kubectl patch ingress $INGRESS_NAME -n $NAMESPACE -p '
       {
         "metadata": {
           "annotations": {
             "nginx.ingress.kubernetes.io/canary": "false"
           }
         },
         "spec": {
           "rules": [{
             "host": "api.tax-calculator.com",
             "http": {
               "paths": [{
                 "path": "/",
                 "pathType": "Prefix",
                 "backend": {
                   "service": {
                     "name": "tax-calculator-green",
                     "port": {
                       "number": 80
                     }
                   }
                 }
               }]
             }
           }]
         }
       }'
   }
   
   rollback_to_blue() {
       echo "Rolling back to blue environment..."
       kubectl patch ingress $INGRESS_NAME -n $NAMESPACE -p '
       {
         "spec": {
           "rules": [{
             "host": "api.tax-calculator.com",
             "http": {
               "paths": [{
                 "path": "/",
                 "pathType": "Prefix",
                 "backend": {
                   "service": {
                     "name": "tax-calculator-blue",
                     "port": {
                       "number": 80
                     }
                   }
                 }
               }]
             }
           }]
         }
       }'
   }
   
   # Health check before switching
   check_health() {
       local service_name=$1
       local max_attempts=30
       local attempt=1
       
       while [ $attempt -le $max_attempts ]; do
           if kubectl exec -n $NAMESPACE deployment/$service_name -- \
              curl -f http://localhost:8080/api/health; then
               echo "Health check passed for $service_name"
               return 0
           fi
           
           echo "Health check attempt $attempt failed for $service_name"
           sleep 10
           ((attempt++))
       done
       
       echo "Health check failed for $service_name after $max_attempts attempts"
       return 1
   }
   ```

#### Success Criteria:
- ✅ Blue and green environments deployed and operational
- ✅ Traffic routing configurable via ingress annotations
- ✅ Health checks prevent traffic to unhealthy environments
- ✅ Rollback script tested and functional

### 2.2 Canary Deployment Testing

**Objective**: Validate canary deployment process with real production traffic

#### Tasks:
1. **Gradual Traffic Shift Configuration**
   ```yaml
   # 5% traffic to green (canary)
   nginx.ingress.kubernetes.io/canary: "true"
   nginx.ingress.kubernetes.io/canary-weight: "5"
   
   # 50% traffic split for validation
   nginx.ingress.kubernetes.io/canary-weight: "50"
   
   # Full traffic to green
   nginx.ingress.kubernetes.io/canary: "false"
   ```

2. **Automated Canary Deployment Script**
   ```bash
   #!/bin/bash
   # canary-deploy.sh
   
   deploy_canary() {
       local weight=$1
       echo "Deploying canary with $weight% traffic"
       
       kubectl patch ingress $INGRESS_NAME -n $NAMESPACE -p "
       {
         \"metadata\": {
           \"annotations\": {
             \"nginx.ingress.kubernetes.io/canary\": \"true\",
             \"nginx.ingress.kubernetes.io/canary-weight\": \"$weight\"
           }
         }
       }"
       
       # Wait for configuration to take effect
       sleep 30
       
       # Monitor error rates
       monitor_error_rates $weight
   }
   
   monitor_error_rates() {
       local weight=$1
       local error_threshold=5  # 5% error rate threshold
       
       # Query Prometheus for error rate
       error_rate=$(curl -s "http://prometheus:9090/api/v1/query?query=rate(http_requests_total{status=~\"5..\"}[5m])/rate(http_requests_total[5m])*100" | jq -r '.data.result[0].value[1]')
       
       if (( $(echo "$error_rate > $error_threshold" | bc -l) )); then
           echo "Error rate $error_rate% exceeds threshold. Rolling back..."
           rollback_canary
           exit 1
       fi
       
       echo "Error rate $error_rate% is within acceptable limits"
   }
   
   rollback_canary() {
       kubectl patch ingress $INGRESS_NAME -n $NAMESPACE -p '
       {
         "metadata": {
           "annotations": {
             "nginx.ingress.kubernetes.io/canary": "false"
           }
         }
       }'
   }
   ```

3. **Client Impact Monitoring**
   ```bash
   # Monitor client behavior during canary
   watch -n 5 'curl -s http://api.tax-calculator.com/api/health | jq .'
   
   # Track response times during traffic shift
   watch -n 10 'curl -w "@curl-format.txt" -o /dev/null -s http://api.tax-calculator.com/api/tax/calculate'
   ```

#### Success Criteria:
- ✅ Canary deployment routes traffic correctly
- ✅ Error rate monitoring prevents bad deployments
- ✅ Response time monitoring detects performance issues
- ✅ Automated rollback works under failure conditions

---

## Task Group 3: Client Compatibility Validation (Week 7, Day 5)

### 3.1 Client Application Testing

**Objective**: Validate that all existing client applications continue to work without changes

#### Tasks:
1. **Client Application Inventory**
   ```markdown
   # Client Application Registry
   
   ## Internal Applications
   - **Tax Calculator Web Frontend** (React SPA)
     - Contact: frontend-team@company.com
     - Usage: High volume, user-facing
     - Testing: Automated E2E tests
   
   - **Mobile App** (React Native)
     - Contact: mobile-team@company.com
     - Usage: Medium volume, customer-facing
     - Testing: Manual testing + automated API tests
   
   - **Reporting Service** (Python)
     - Contact: data-team@company.com
     - Usage: Batch processing, internal
     - Testing: Integration test suite
   
   ## External Applications
   - **Partner Integration A**
     - Contact: partner-a@external.com
     - Usage: Low volume, B2B integration
     - Testing: Coordinated testing with partner
   ```

2. **Compatibility Test Suite**
   ```csharp
   [TestFixture]
   public class ClientCompatibilityTests
   {
       private HttpClient _legacyClient;
       private HttpClient _coreClient;
       
       [SetUp]
       public void Setup()
       {
           _legacyClient = new HttpClient { BaseAddress = new Uri("http://legacy-api.internal") };
           _coreClient = new HttpClient { BaseAddress = new Uri("http://api.tax-calculator.com") };
       }
       
       [Test]
       public async Task TaxCalculation_WebFrontend_IdenticalBehavior()
       {
           var request = new
           {
               taxableIncome = 75000m,
               financialYear = "2023-24",
               residencyStatus = "Resident",
               includeMedicareLevy = true
           };
           
           var legacyResponse = await _legacyClient.PostAsJsonAsync("/api/tax/calculate", request);
           var coreResponse = await _coreClient.PostAsJsonAsync("/api/tax/calculate", request);
           
           // Validate identical status codes
           Assert.AreEqual(legacyResponse.StatusCode, coreResponse.StatusCode);
           
           // Validate identical response structure
           var legacyContent = await legacyResponse.Content.ReadAsStringAsync();
           var coreContent = await coreResponse.Content.ReadAsStringAsync();
           
           var legacyJson = JObject.Parse(legacyContent);
           var coreJson = JObject.Parse(coreContent);
           
           // Property-by-property validation
           Assert.AreEqual(legacyJson["totalTax"], coreJson["totalTax"]);
           Assert.AreEqual(legacyJson["medicareLevy"], coreJson["medicareLevy"]);
           CollectionAssert.AreEquivalent(
               legacyJson["brackets"].Select(b => b["rate"]),
               coreJson["brackets"].Select(b => b["rate"])
           );
       }
       
       [Test]
       public async Task ErrorHandling_MobileApp_IdenticalErrorMessages()
       {
           var invalidRequest = new { taxableIncome = -1000 };
           
           var legacyResponse = await _legacyClient.PostAsJsonAsync("/api/tax/calculate", invalidRequest);
           var coreResponse = await _coreClient.PostAsJsonAsync("/api/tax/calculate", invalidRequest);
           
           Assert.AreEqual(legacyResponse.StatusCode, coreResponse.StatusCode);
           Assert.AreEqual(
               await legacyResponse.Content.ReadAsStringAsync(),
               await coreResponse.Content.ReadAsStringAsync()
           );
       }
   }
   ```

3. **Real Client Application Testing**
   ```bash
   # Coordinate testing with client teams
   
   # Web Frontend E2E Tests
   cd ../tax-calculator-frontend
   npm test:e2e:api -- --env.API_URL=http://api.tax-calculator.com
   
   # Mobile App API Tests
   cd ../tax-calculator-mobile
   npm run test:api -- --apiUrl=http://api.tax-calculator.com
   
   # Reporting Service Integration Tests
   cd ../tax-reporting-service
   python -m pytest tests/integration/ --api-url=http://api.tax-calculator.com
   ```

#### Success Criteria:
- ✅ All client applications' automated tests pass
- ✅ Manual testing by client teams confirms compatibility
- ✅ Error handling behavior identical across all clients
- ✅ Performance acceptable for all client usage patterns

### 3.2 Production Traffic Shadowing

**Objective**: Test .NET Core 8 version with real production traffic without impacting users

#### Tasks:
1. **Traffic Mirroring Setup**
   ```yaml
   # Istio VirtualService for traffic mirroring
   apiVersion: networking.istio.io/v1alpha3
   kind: VirtualService
   metadata:
     name: tax-calculator-mirror
   spec:
     hosts:
     - api.tax-calculator.com
     http:
     - match:
       - uri:
           prefix: /api/
       route:
       - destination:
           host: tax-calculator-blue  # Live traffic
           port:
             number: 80
         weight: 100
       mirror:
         host: tax-calculator-green   # Shadow traffic
         port:
           number: 80
       mirror_percent: 100
   ```

2. **Shadow Traffic Monitoring**
   ```bash
   # Monitor shadow traffic processing
   kubectl logs -f deployment/tax-calculator-green -n tax-calculator-prod
   
   # Compare response times between blue and green
   watch -n 10 '
   echo "Blue (Legacy) response times:"
   curl -w "%{time_total}\n" -o /dev/null -s http://tax-calculator-blue/api/health
   echo "Green (.NET Core 8) response times:"
   curl -w "%{time_total}\n" -o /dev/null -s http://tax-calculator-green/api/health
   '
   ```

3. **Response Comparison Analysis**
   ```python
   # response-comparison.py
   import json
   import requests
   import difflib
   
   def compare_responses(endpoint, sample_requests):
       differences = []
       
       for request in sample_requests:
           blue_response = requests.post(f"http://tax-calculator-blue{endpoint}", json=request)
           green_response = requests.post(f"http://tax-calculator-green{endpoint}", json=request)
           
           if blue_response.status_code != green_response.status_code:
               differences.append({
                   'request': request,
                   'issue': 'status_code_mismatch',
                   'blue': blue_response.status_code,
                   'green': green_response.status_code
               })
               continue
           
           blue_json = blue_response.json()
           green_json = green_response.json()
           
           if blue_json != green_json:
               diff = list(difflib.unified_diff(
                   json.dumps(blue_json, indent=2, sort_keys=True).splitlines(),
                   json.dumps(green_json, indent=2, sort_keys=True).splitlines(),
                   lineterm='',
                   fromfile='blue',
                   tofile='green'
               ))
               
               differences.append({
                   'request': request,
                   'issue': 'response_mismatch',
                   'diff': diff
               })
       
       return differences
   
   # Run comparison with production request samples
   sample_requests = [
       {'taxableIncome': 50000, 'financialYear': '2023-24'},
       {'taxableIncome': 180000, 'financialYear': '2023-24', 'includeMedicareLevy': True},
       # Add more representative samples
   ]
   
   differences = compare_responses('/api/tax/calculate', sample_requests)
   
   if differences:
       print(f"Found {len(differences)} differences:")
       for diff in differences:
           print(json.dumps(diff, indent=2))
   else:
       print("No differences found - responses are identical!")
   ```

#### Success Criteria:
- ✅ Shadow traffic processed without errors
- ✅ Response times within ±20% of original
- ✅ All responses structurally identical
- ✅ No errors or exceptions in shadow environment

---

## Task Group 4: Production Deployment Execution (Week 8, Days 1-2)

### 4.1 Pre-Deployment Checklist

**Objective**: Final validation before production deployment

#### Tasks:
1. **System Health Validation**
   ```bash
   # Pre-deployment health check script
   #!/bin/bash
   
   echo "=== Pre-Deployment Health Check ==="
   
   # Check Kubernetes cluster health
   if ! kubectl cluster-info > /dev/null 2>&1; then
       echo "❌ Kubernetes cluster not accessible"
       exit 1
   fi
   echo "✅ Kubernetes cluster accessible"
   
   # Check database connectivity
   if ! kubectl run db-test --image=mcr.microsoft.com/mssql-tools --rm -it --restart=Never -- \
        sqlcmd -S "$DB_SERVER" -U "$DB_USER" -P "$DB_PASS" -Q "SELECT 1" > /dev/null 2>&1; then
       echo "❌ Database not accessible"
       exit 1
   fi
   echo "✅ Database accessible"
   
   # Check Redis connectivity
   if ! kubectl run redis-test --image=redis:alpine --rm -it --restart=Never -- \
        redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" ping > /dev/null 2>&1; then
       echo "❌ Redis not accessible"
       exit 1
   fi
   echo "✅ Redis accessible"
   
   # Check image availability
   if ! docker pull tax-calculator:$IMAGE_TAG > /dev/null 2>&1; then
       echo "❌ Container image not available"
       exit 1
   fi
   echo "✅ Container image available"
   
   # Check SSL certificate validity
   if ! echo | openssl s_client -servername api.tax-calculator.com -connect api.tax-calculator.com:443 2>/dev/null | \
        openssl x509 -noout -checkend 86400 > /dev/null 2>&1; then
       echo "❌ SSL certificate expires within 24 hours"
       exit 1
   fi
   echo "✅ SSL certificate valid"
   
   echo "=== All health checks passed! Ready for deployment ==="
   ```

2. **Stakeholder Communication**
   ```markdown
   # Production Deployment Communication Template
   
   **To:** All Stakeholders
   **Subject:** .NET Core 8 Migration - Production Deployment Scheduled
   
   ## Deployment Schedule
   - **Start Time:** [Date] [Time] UTC
   - **Expected Duration:** 30 minutes
   - **Expected Downtime:** 0 minutes (zero-downtime deployment)
   
   ## What's Changing
   - Backend framework upgrade from .NET Framework 4.8 to .NET Core 8
   - No API contract changes - all client applications continue working unchanged
   - Performance improvements expected
   
   ## Validation Completed
   ✅ All automated tests passing (1,200+ test cases)
   ✅ Client compatibility validated with major consumers
   ✅ Production traffic shadowing successful
   ✅ Performance testing shows 15% improvement
   ✅ Security scanning passed with zero critical issues
   
   ## Rollback Plan
   - Immediate rollback capability via traffic routing
   - Full rollback possible within 5 minutes if issues detected
   
   ## Contact Information
   - **Deployment Lead:** [Name] - [Contact]
   - **Technical Lead:** [Name] - [Contact]
   - **Emergency Escalation:** [Name] - [Contact]
   
   ## Post-Deployment Monitoring
   We will monitor the system for 24 hours post-deployment with enhanced alerting.
   ```

3. **Monitoring and Alerting Setup**
   ```yaml
   # Enhanced alerting during deployment
   apiVersion: monitoring.coreos.com/v1
   kind: PrometheusRule
   metadata:
     name: deployment-alerts
   spec:
     groups:
     - name: deployment
       rules:
       - alert: HighErrorRate
         expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
         for: 2m
         labels:
           severity: critical
         annotations:
           summary: "High error rate detected during deployment"
           
       - alert: HighResponseTime
         expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1.0
         for: 5m
         labels:
           severity: warning
         annotations:
           summary: "High response time detected"
           
       - alert: PodRestartingFrequently
         expr: rate(kube_pod_container_status_restarts_total[15m]) > 0
         for: 0m
         labels:
           severity: critical
         annotations:
           summary: "Pod restarting during deployment"
   ```

#### Success Criteria:
- ✅ All pre-deployment health checks pass
- ✅ Stakeholders notified and deployment approved
- ✅ Enhanced monitoring and alerting active
- ✅ Rollback procedures tested and ready

### 4.2 Production Deployment Execution

**Objective**: Execute zero-downtime production deployment

#### Tasks:
1. **Deployment Execution Script**
   ```bash
   #!/bin/bash
   # production-deploy.sh
   
   set -e  # Exit on any error
   
   NAMESPACE="tax-calculator-prod"
   IMAGE_TAG="${1:-latest}"
   
   echo "=== Starting Production Deployment ==="
   echo "Image: tax-calculator:$IMAGE_TAG"
   echo "Namespace: $NAMESPACE"
   echo "Time: $(date -u)"
   
   # Step 1: Deploy green environment
   echo "Step 1: Deploying green environment..."
   helm upgrade --install tax-calculator-green ./charts/tax-calculator \
     --namespace $NAMESPACE \
     --create-namespace \
     --values ./charts/tax-calculator/values-production.yaml \
     --set image.tag=$IMAGE_TAG \
     --set nameOverride=tax-calculator-green \
     --wait --timeout=10m
   
   # Step 2: Health check green environment
   echo "Step 2: Health checking green environment..."
   if ! check_health tax-calculator-green; then
       echo "❌ Green environment health check failed"
       exit 1
   fi
   echo "✅ Green environment healthy"
   
   # Step 3: Start canary deployment (5% traffic)
   echo "Step 3: Starting canary deployment (5% traffic)..."
   deploy_canary 5
   sleep 300  # 5 minutes observation
   
   if ! validate_canary_metrics; then
       echo "❌ Canary validation failed, rolling back"
       rollback_canary
       exit 1
   fi
   
   # Step 4: Increase to 50% traffic
   echo "Step 4: Increasing to 50% traffic..."
   deploy_canary 50
   sleep 600  # 10 minutes observation
   
   if ! validate_canary_metrics; then
       echo "❌ 50% traffic validation failed, rolling back"
       rollback_canary
       exit 1
   fi
   
   # Step 5: Full traffic switch
   echo "Step 5: Switching to 100% traffic..."
   switch_to_green
   sleep 300  # 5 minutes observation
   
   if ! validate_full_deployment; then
       echo "❌ Full deployment validation failed, rolling back"
       rollback_to_blue
       exit 1
   fi
   
   # Step 6: Cleanup blue environment (after validation)
   echo "Step 6: Deployment successful, scheduling blue environment cleanup..."
   # Note: Keep blue environment for 24h before cleanup
   
   echo "=== Production Deployment Completed Successfully ==="
   echo "Time: $(date -u)"
   
   # Send success notification
   send_deployment_notification "SUCCESS" "Production deployment completed successfully"
   ```

2. **Real-time Monitoring During Deployment**
   ```bash
   # monitoring-dashboard.sh
   #!/bin/bash
   
   watch -n 5 '
   echo "=== Deployment Status Dashboard ==="
   echo "Time: $(date)"
   echo
   
   echo "Pod Status:"
   kubectl get pods -n tax-calculator-prod -l app=tax-calculator -o wide
   echo
   
   echo "Service Endpoints:"
   kubectl get endpoints -n tax-calculator-prod
   echo
   
   echo "Recent Events:"
   kubectl get events -n tax-calculator-prod --sort-by=.lastTimestamp | tail -5
   echo
   
   echo "Current Traffic Distribution:"
   kubectl get ingress -n tax-calculator-prod -o yaml | grep -A5 -B5 canary
   echo
   
   echo "Health Check Status:"
   curl -s http://api.tax-calculator.com/api/health | jq .
   echo
   
   echo "Error Rate (last 5 min):"
   # Prometheus query for error rate
   curl -s "http://prometheus:9090/api/v1/query?query=rate(http_requests_total{status=~\"5..\"}[5m])" | jq -r .data.result[0].value[1]
   '
   ```

#### Success Criteria:
- ✅ Green environment deploys without errors
- ✅ Canary deployment validates successfully at 5% and 50%
- ✅ Full traffic switch completes without issues
- ✅ All monitoring metrics within acceptable ranges
- ✅ No client-reported issues during deployment

---

## Task Group 5: Post-Deployment Validation (Week 8, Days 3-4)

### 5.1 Comprehensive System Validation

**Objective**: Validate all aspects of the production system after deployment

#### Tasks:
1. **Functional Validation Suite**
   ```csharp
   [TestFixture]
   public class ProductionValidationTests
   {
       private HttpClient _client;
       
       [SetUp]
       public void Setup()
       {
           _client = new HttpClient 
           { 
               BaseAddress = new Uri("https://api.tax-calculator.com"),
               Timeout = TimeSpan.FromSeconds(30)
           };
       }
       
       [Test]
       public async Task HealthEndpoint_RespondsFunctionally()
       {
           var response = await _client.GetAsync("/api/health");
           Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
           
           var content = await response.Content.ReadAsStringAsync();
           var healthResult = JsonSerializer.Deserialize<HealthResponse>(content);
           
           Assert.AreEqual("OK", healthResult.Status);
           Assert.IsTrue(DateTime.TryParse(healthResult.Timestamp, out _));
       }
       
       [Test]
       public async Task TaxCalculation_ProducesCorrectResults()
       {
           var testCases = new[]
           {
               new { Income = 18200m, Year = "2023-24", ExpectedTax = 0m },
               new { Income = 50000m, Year = "2023-24", ExpectedTax = 6717m },
               new { Income = 120000m, Year = "2023-24", ExpectedTax = 26167m }
           };
           
           foreach (var testCase in testCases)
           {
               var request = new TaxCalculationRequest
               {
                   TaxableIncome = testCase.Income,
                   FinancialYear = testCase.Year,
                   ResidencyStatus = "Resident"
               };
               
               var response = await _client.PostAsJsonAsync("/api/tax/calculate", request);
               Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
               
               var result = await response.Content.ReadFromJsonAsync<TaxCalculationResult>();
               Assert.AreEqual(testCase.ExpectedTax, result.TotalTax, 1m); // ±$1 tolerance
           }
       }
       
       [Test]
       public async Task LoadTest_HandlesExpectedVolume()
       {
           var tasks = new List<Task<HttpResponseMessage>>();
           
           // Simulate 50 concurrent requests
           for (int i = 0; i < 50; i++)
           {
               var request = new TaxCalculationRequest
               {
                   TaxableIncome = 50000 + (i * 1000),
                   FinancialYear = "2023-24"
               };
               
               tasks.Add(_client.PostAsJsonAsync("/api/tax/calculate", request));
           }
           
           var responses = await Task.WhenAll(tasks);
           
           Assert.IsTrue(responses.All(r => r.IsSuccessStatusCode), 
               "All requests should succeed under load");
           
           var averageResponseTime = responses.Average(r => 
               r.Headers.GetValues("X-Response-Time-Ms").First().ToDouble());
           
           Assert.Less(averageResponseTime, 1000, 
               "Average response time should be under 1 second");
       }
   }
   ```

2. **Performance Validation**
   ```bash
   # performance-validation.sh
   #!/bin/bash
   
   echo "=== Performance Validation ==="
   
   # Response time validation
   echo "Testing response times..."
   for i in {1..10}; do
       response_time=$(curl -w '%{time_total}' -o /dev/null -s \
           -X POST https://api.tax-calculator.com/api/tax/calculate \
           -H "Content-Type: application/json" \
           -d '{"taxableIncome": 75000, "financialYear": "2023-24"}')
       echo "Request $i: ${response_time}s"
   done
   
   # Throughput testing
   echo "Testing throughput..."
   ab -n 1000 -c 10 -H "Content-Type: application/json" \
      -p request.json https://api.tax-calculator.com/api/tax/calculate
   
   # Memory usage check
   echo "Checking memory usage..."
   kubectl top pods -n tax-calculator-prod
   ```

3. **Security Validation**
   ```bash
   # security-validation.sh
   #!/bin/bash
   
   echo "=== Security Validation ==="
   
   # SSL configuration check
   echo "Checking SSL configuration..."
   nmap --script ssl-enum-ciphers -p 443 api.tax-calculator.com
   
   # Security headers check
   echo "Checking security headers..."
   curl -I https://api.tax-calculator.com/api/health
   
   # Rate limiting check
   echo "Testing rate limiting..."
   for i in {1..105}; do
       status=$(curl -s -o /dev/null -w '%{http_code}' https://api.tax-calculator.com/api/health)
       if [ "$status" = "429" ]; then
           echo "Rate limiting triggered at request $i"
           break
       fi
   done
   ```

#### Success Criteria:
- ✅ All functional tests pass in production
- ✅ Performance meets or exceeds baseline requirements
- ✅ Security configurations properly implemented
- ✅ Load testing demonstrates system stability

### 5.2 Client Validation and Feedback

**Objective**: Confirm all client applications working correctly with production system

#### Tasks:
1. **Client Team Validation Coordination**
   ```markdown
   # Client Validation Checklist
   
   ## Web Frontend Team
   - [ ] User interface loads correctly
   - [ ] Tax calculations display properly
   - [ ] Error handling works as expected
   - [ ] Performance is acceptable
   - [ ] No JavaScript console errors
   
   ## Mobile App Team
   - [ ] API calls succeed from mobile app
   - [ ] Response parsing works correctly
   - [ ] Offline/online state transitions work
   - [ ] Push notifications continue working
   
   ## Reporting Service Team
   - [ ] Batch processing jobs complete successfully
   - [ ] Data extraction works correctly
   - [ ] Schedule integration unaffected
   - [ ] Report generation continues working
   
   ## External Partners
   - [ ] B2B integrations continue functioning
   - [ ] Webhook deliveries working
   - [ ] Authentication mechanisms unchanged
   - [ ] SLA requirements met
   ```

2. **Production Usage Monitoring**
   ```bash
   # Monitor real usage patterns
   watch -n 30 '
   echo "=== Production Usage Monitoring ==="
   echo "Time: $(date)"
   echo
   
   echo "Request Volume (last 5 min):"
   # Prometheus query for request rate
   curl -s "http://prometheus:9090/api/v1/query?query=rate(http_requests_total[5m])" | \
     jq -r ".data.result[] | \"\(.metric.method) \(.metric.path): \(.value[1]) req/s\""
   echo
   
   echo "Error Rate:"
   curl -s "http://prometheus:9090/api/v1/query?query=rate(http_requests_total{status=~\"5..\"}[5m])" | \
     jq -r ".data.result[0].value[1] // \"0\""
   echo
   
   echo "Response Time (95th percentile):"
   curl -s "http://prometheus:9090/api/v1/query?query=histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))" | \
     jq -r ".data.result[0].value[1]"
   '
   ```

3. **User Experience Monitoring**
   ```javascript
   // Real User Monitoring (RUM) validation
   // Client-side monitoring to ensure no degradation
   
   const performanceObserver = new PerformanceObserver((list) => {
     list.getEntries().forEach((entry) => {
       if (entry.name.includes('api.tax-calculator.com')) {
         console.log(`API Call: ${entry.name}`);
         console.log(`Duration: ${entry.duration}ms`);
         console.log(`Response Start: ${entry.responseStart - entry.requestStart}ms`);
         
         // Send metrics to monitoring system
         sendMetric('api_response_time', entry.duration, {
           endpoint: entry.name,
           timestamp: Date.now()
         });
       }
     });
   });
   
   performanceObserver.observe({ entryTypes: ['resource'] });
   ```

#### Success Criteria:
- ✅ All client teams confirm functionality
- ✅ No user-reported issues within 24 hours
- ✅ Usage patterns match pre-deployment baselines
- ✅ User experience metrics within acceptable ranges

---

## Task Group 6: Documentation and Handover (Week 8, Day 5)

### 6.1 Production Documentation

**Objective**: Complete documentation for production operations

#### Tasks:
1. **Operational Runbook**
   ```markdown
   # Production Operations Runbook
   
   ## System Overview
   - **Application:** Tax Calculator API
   - **Framework:** .NET Core 8.0
   - **Hosting:** Kubernetes on Azure AKS
   - **Database:** SQL Server 2019
   - **Cache:** Redis 6.2
   
   ## Daily Operations
   
   ### Health Monitoring
   - **Health Endpoint:** https://api.tax-calculator.com/api/health
   - **Kubernetes Health:** https://api.tax-calculator.com/healthz
   - **Monitoring Dashboard:** https://monitoring.company.com/tax-calculator
   
   ### Log Locations
   - **Application Logs:** kubectl logs -n tax-calculator-prod deployment/tax-calculator
   - **Ingress Logs:** kubectl logs -n ingress-nginx deployment/nginx-ingress-controller
   - **Centralized Logs:** https://logs.company.com (filter: tax-calculator)
   
   ## Troubleshooting
   
   ### High Response Times
   1. Check database connection pool: `kubectl exec -it deployment/tax-calculator -- env | grep CONNECTION_STRING`
   2. Review Redis performance: `kubectl exec -it redis-pod -- redis-cli info stats`
   3. Check pod resource usage: `kubectl top pods -n tax-calculator-prod`
   
   ### Pod Restart Loops
   1. Check pod events: `kubectl describe pod [POD_NAME] -n tax-calculator-prod`
   2. Review application logs: `kubectl logs [POD_NAME] -n tax-calculator-prod --previous`
   3. Validate configuration: `kubectl get configmap tax-calculator-config -o yaml`
   
   ### Database Connectivity Issues
   1. Test connectivity: `kubectl run db-test --image=mcr.microsoft.com/mssql-tools --rm -it -- sqlcmd -S $DB_SERVER -Q "SELECT 1"`
   2. Check connection string: Review secrets in Azure Key Vault
   3. Verify network policies: `kubectl get networkpolicy -n tax-calculator-prod`
   
   ## Escalation Procedures
   - **Level 1:** On-call engineer (responds within 15 minutes)
   - **Level 2:** Senior engineer + Engineering Manager (responds within 30 minutes)
   - **Level 3:** CTO + Architecture team (responds within 1 hour)
   
   ## Emergency Procedures
   
   ### Immediate Rollback
   ```bash
   # Switch traffic back to blue (legacy) environment
   kubectl patch ingress tax-calculator-ingress -n tax-calculator-prod -p '
   {
     "spec": {
       "rules": [{
         "host": "api.tax-calculator.com",
         "http": {
           "paths": [{
             "path": "/",
             "pathType": "Prefix",
             "backend": {
               "service": {
                 "name": "tax-calculator-blue",
                 "port": { "number": 80 }
               }
             }
           }]
         }
       }]
     }
   }'
   ```
   
   ### Scale Up for High Load
   ```bash
   kubectl scale deployment tax-calculator -n tax-calculator-prod --replicas=10
   ```
   ```

2. **Migration Documentation**
   ```markdown
   # .NET Framework 4.8 to .NET Core 8 Migration - Final Report
   
   ## Migration Summary
   - **Start Date:** [Week 1]
   - **Completion Date:** [Week 8]
   - **Total Effort:** 8 weeks, 2 engineers
   - **Downtime:** 0 minutes (zero-downtime achieved)
   
   ## Changes Made
   
   ### Framework Changes
   - Target framework: .NET Framework 4.8 → .NET Core 8.0
   - Web framework: ASP.NET Web API 2 → ASP.NET Core 8
   - Hosting: IIS → Kubernetes with Kestrel
   - Deployment: Manual → Automated CI/CD with Helm
   
   ### Package Updates
   - System.Data.SqlClient → Microsoft.Data.SqlClient 5.1.1
   - StackExchange.Redis 1.x → 2.7.4
   - Autofac 4.x → Built-in DI
   - Newtonsoft.Json (preserved for compatibility)
   
   ### Configuration Changes
   - App.config/Web.config → appsettings.json
   - Connection strings → Azure Key Vault
   - Environment variables for configuration overrides
   
   ## Validation Results
   
   ### API Contract Validation
   ✅ All 5 endpoints maintain identical contracts
   ✅ JSON response structures unchanged
   ✅ Error message formats preserved
   ✅ HTTP status codes identical
   
   ### Performance Results
   - Response time improvement: 15% faster
   - Memory usage: 25% reduction
   - CPU usage: 20% reduction
   - Cold start time: 40% faster
   
   ### Client Compatibility
   ✅ Web frontend: No changes required
   ✅ Mobile app: No changes required
   ✅ Reporting service: No changes required
   ✅ External partners: No impact
   
   ## Lessons Learned
   
   ### What Went Well
   - Clean architecture made migration straightforward
   - Comprehensive testing prevented production issues
   - Blue-green deployment eliminated downtime
   - Client validation caught edge cases early
   
   ### What Could Be Improved
   - Configuration migration took longer than expected
   - More extensive load testing could have been beneficial
   - Earlier client team engagement would have helped
   
   ## Recommendations for Future Migrations
   1. Start with infrastructure setup to find issues early
   2. Invest heavily in contract validation testing
   3. Use traffic shadowing before production deployment
   4. Maintain both environments during transition period
   ```

#### Success Criteria:
- ✅ Complete operational runbook available
- ✅ Migration documentation comprehensive
- ✅ Troubleshooting procedures documented
- ✅ Knowledge transfer completed

### 6.2 Team Knowledge Transfer

**Objective**: Ensure team can operate and maintain the new system

#### Tasks:
1. **Training Sessions**
   ```markdown
   # Training Session Plan
   
   ## Session 1: .NET Core 8 Fundamentals (2 hours)
   - Framework differences from .NET Framework
   - New project structure and configuration
   - Dependency injection changes
   - Performance characteristics
   
   ## Session 2: Kubernetes Operations (2 hours)
   - Pod lifecycle and troubleshooting
   - Service and ingress configuration
   - Scaling and resource management
   - Log aggregation and monitoring
   
   ## Session 3: CI/CD Pipeline (1 hour)
   - GitHub Actions workflow
   - Helm chart structure
   - Deployment strategies
   - Rollback procedures
   
   ## Session 4: Production Support (1 hour)
   - Monitoring and alerting
   - Common issues and resolution
   - Escalation procedures
   - Emergency response
   ```

2. **Hands-on Practice**
   ```bash
   # Practice scenarios for team
   
   # Scenario 1: Scale the application
   kubectl scale deployment tax-calculator -n tax-calculator-prod --replicas=5
   
   # Scenario 2: Update configuration
   kubectl edit configmap tax-calculator-config -n tax-calculator-prod
   
   # Scenario 3: Check application logs
   kubectl logs -f deployment/tax-calculator -n tax-calculator-prod
   
   # Scenario 4: Perform a rollback
   helm rollback tax-calculator 1 -n tax-calculator-prod
   
   # Scenario 5: Debug connectivity issues
   kubectl run debug-pod --image=nicolaka/netshoot --rm -it -- /bin/bash
   ```

#### Success Criteria:
- ✅ All team members trained on new system
- ✅ Team can perform common operational tasks
- ✅ Emergency procedures understood and practiced
- ✅ Support documentation accessible and useful

---

## Phase 4 Validation & Success Criteria

### Production Deployment Validation
- [ ] **Zero Downtime:** No service interruption during deployment
- [ ] **Client Compatibility:** All client applications continue working unchanged
- [ ] **Performance:** System meets or exceeds baseline performance
- [ ] **Monitoring:** All metrics within acceptable ranges for 24 hours

### System Reliability Validation
- [ ] **Health Checks:** All health endpoints responding correctly
- [ ] **Error Rates:** <1% error rate for 48 hours post-deployment
- [ ] **Response Times:** 95th percentile <500ms consistently
- [ ] **Resource Usage:** Memory and CPU within expected limits

### Operational Readiness Validation
- [ ] **Documentation:** Complete operational runbooks available
- [ ] **Team Training:** All team members capable of system operation
- [ ] **Monitoring:** Comprehensive monitoring and alerting operational
- [ ] **Incident Response:** Emergency procedures tested and ready

### Business Continuity Validation
- [ ] **User Experience:** No degradation in user experience metrics
- [ ] **Business Metrics:** All business KPIs maintained or improved
- [ ] **Partner Integration:** All external integrations functioning
- [ ] **Compliance:** All regulatory and compliance requirements met

---

## Phase 4 Delivery Checklist

### Technical Deliverables
- [ ] Production system running .NET Core 8
- [ ] All client applications validated and working
- [ ] Performance benchmarks documented
- [ ] Security configurations implemented

### Operational Deliverables
- [ ] Production runbook completed
- [ ] Team training completed
- [ ] Monitoring and alerting operational
- [ ] Incident response procedures tested

### Business Deliverables
- [ ] Migration success report
- [ ] Performance improvement documentation
- [ ] Cost analysis (infrastructure and operational)
- [ ] ROI analysis for the migration

---

## Success Metrics Summary

### Technical Success
- **Uptime:** 100% during migration (zero-downtime achieved)
- **Performance:** 15% improvement in response times
- **Resource Efficiency:** 25% reduction in memory usage
- **Security:** Zero critical vulnerabilities in production

### Business Success
- **Client Impact:** Zero client application changes required
- **User Experience:** No degradation in user satisfaction metrics
- **Operational Efficiency:** 40% faster deployment process
- **Future Readiness:** Modern platform for future enhancements

### Strategic Success
- **Technology Modernization:** Current technology stack
- **Cloud Native:** Kubernetes-ready for scalability
- **Developer Productivity:** Improved development experience
- **Competitive Advantage:** Faster feature delivery capability

---

*Phase 4 Success Criteria: Production system successfully migrated to .NET Core 8 with zero downtime, 100% client compatibility, and improved performance characteristics.*
