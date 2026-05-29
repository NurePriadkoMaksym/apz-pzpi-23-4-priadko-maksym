param(
    [string]$ConfigPath = "$PSScriptRoot\config.env",
    [switch]$SkipBuild,
    [switch]$Recreate,
    [switch]$KeepNamespace,
    [switch]$NonInteractive
)

$ErrorActionPreference = "Stop"

function Write-Step($Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok($Message) {
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Require-Command($Name) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "Command '$Name' was not found. Install it or add it to PATH."
    }
}

function Read-EnvFile($Path) {
    if (-not (Test-Path $Path)) {
        throw "Config file not found: $Path"
    }

    $config = @{}
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -eq "" -or $line.StartsWith("#")) { return }
        $parts = $line -split "=", 2
        if ($parts.Count -eq 2) {
            $config[$parts[0].Trim()] = $parts[1].Trim()
        }
    }
    return $config
}

function Get-ConfigValue($Config, $Key, $Default) {
    if ($Config.ContainsKey($Key) -and $Config[$Key] -ne "") {
        return $Config[$Key]
    }
    return $Default
}

function Confirm-Action($Prompt, $DefaultYes = $true) {
    if ($NonInteractive) { return $DefaultYes }
    $suffix = if ($DefaultYes) { "[Y/n]" } else { "[y/N]" }
    $answer = Read-Host "$Prompt $suffix"
    if ([string]::IsNullOrWhiteSpace($answer)) { return $DefaultYes }
    return $answer.Trim().ToLower() -in @("y", "yes", "так", "т")
}

function Invoke-Logged($File, [string[]]$Arguments) {
    Write-Host "> $File $($Arguments -join ' ')" -ForegroundColor DarkGray
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $File $($Arguments -join ' ')"
    }
}

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$GeneratedDir = Join-Path $Root "k8s\generated"

Write-Step "Checking required tools"
Require-Command docker
Require-Command kubectl
Write-Ok "docker and kubectl are available"

$config = Read-EnvFile $ConfigPath
$Namespace = Get-ConfigValue $config "NAMESPACE" "pythonwiki"
$ApiImage = Get-ConfigValue $config "API_IMAGE" "pythonwiki-api:lab4"
$ApiReplicas = [int](Get-ConfigValue $config "API_REPLICAS" "2")
$ApiPort = [int](Get-ConfigValue $config "API_PORT" "8080")
$DbName = Get-ConfigValue $config "DB_NAME" "PythonWikiDb"
$DbPassword = Get-ConfigValue $config "DB_PASSWORD" "YourStrong!Passw0rd"
$StorageSize = Get-ConfigValue $config "STORAGE_SIZE" "2Gi"
$BuildImage = (Get-ConfigValue $config "BUILD_IMAGE" "true").ToLower() -eq "true"
$NoCacheBuild = (Get-ConfigValue $config "NO_CACHE_BUILD" "true").ToLower() -eq "true"
$DeployVersion = Get-Date -Format "yyyyMMddHHmmss"
$RecreateNamespace = (-not $KeepNamespace) -and ($Recreate -or ((Get-ConfigValue $config "RECREATE_NAMESPACE" "true").ToLower() -eq "true"))

$ConnectionString = "Server=pythonwiki-db,1433;Database=$DbName;User Id=sa;Password=$DbPassword;TrustServerCertificate=True;MultipleActiveResultSets=true;Pooling=true;Min Pool Size=5;Max Pool Size=100"

Write-Step "Deployment settings"
Write-Host "Namespace:      $Namespace"
Write-Host "API image:      $ApiImage"
Write-Host "API replicas:   $ApiReplicas (API starts with 1 replica for safe database migration, then scales)"
Write-Host "API port:       $ApiPort"
Write-Host "Database name:  $DbName"
Write-Host "Storage size:   $StorageSize"
Write-Host "No-cache build: $NoCacheBuild"
Write-Host "Recreate ns:    $RecreateNamespace"

Write-Step "Checking Kubernetes connection"
Invoke-Logged kubectl @("cluster-info")
$currentContext = (& kubectl config current-context).Trim()
Write-Host "Current kubectl context: $currentContext"

if ($currentContext -notmatch "docker-desktop|minikube|kind") {
    Write-Host "Warning: current context does not look like local Docker Desktop/minikube/kind." -ForegroundColor Yellow
}

if ($RecreateNamespace) {
    Write-Step "Recreating namespace $Namespace"
    & kubectl delete namespace $Namespace --ignore-not-found=true
    if ($LASTEXITCODE -ne 0) { throw "Failed to delete namespace $Namespace" }
    Start-Sleep -Seconds 3
}

if ($BuildImage -and -not $SkipBuild) {
    if (Confirm-Action "Build Docker image '$ApiImage' before deploy?" $true) {
        Write-Step "Building Docker image"
        Push-Location $Root
        try {
            $buildArgs = @("build", "-t", $ApiImage)
            if ($NoCacheBuild) { $buildArgs += "--no-cache" }
            $buildArgs += "."
            Invoke-Logged docker $buildArgs
        }
        finally {
            Pop-Location
        }
    }
}
else {
    Write-Host "Skipping Docker build."
}

Write-Step "Generating Kubernetes manifests"
if (Test-Path $GeneratedDir) { Remove-Item $GeneratedDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $GeneratedDir | Out-Null

@"
apiVersion: v1
kind: Namespace
metadata:
  name: $Namespace
"@ | Set-Content -Encoding UTF8 (Join-Path $GeneratedDir "00-namespace.yaml")

@"
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: pythonwiki-db-data
  namespace: $Namespace
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: $StorageSize
"@ | Set-Content -Encoding UTF8 (Join-Path $GeneratedDir "01-db-pvc.yaml")

@"
apiVersion: v1
kind: Secret
metadata:
  name: pythonwiki-db-secret
  namespace: $Namespace
type: Opaque
stringData:
  MSSQL_SA_PASSWORD: "$DbPassword"
  CONNECTION_STRING: "$ConnectionString"
"@ | Set-Content -Encoding UTF8 (Join-Path $GeneratedDir "02-db-secret.yaml")

@"
apiVersion: apps/v1
kind: Deployment
metadata:
  name: pythonwiki-db
  namespace: $Namespace
spec:
  replicas: 1
  selector:
    matchLabels:
      app: pythonwiki-db
  template:
    metadata:
      labels:
        app: pythonwiki-db
    spec:
      containers:
        - name: sqlserver
          image: mcr.microsoft.com/mssql/server:2022-latest
          ports:
            - containerPort: 1433
          env:
            - name: ACCEPT_EULA
              value: "Y"
            - name: MSSQL_PID
              value: "Developer"
            - name: MSSQL_SA_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: pythonwiki-db-secret
                  key: MSSQL_SA_PASSWORD
          volumeMounts:
            - name: db-data
              mountPath: /var/opt/mssql
          readinessProbe:
            tcpSocket:
              port: 1433
            initialDelaySeconds: 20
            periodSeconds: 10
          livenessProbe:
            tcpSocket:
              port: 1433
            initialDelaySeconds: 60
            periodSeconds: 20
      volumes:
        - name: db-data
          persistentVolumeClaim:
            claimName: pythonwiki-db-data
"@ | Set-Content -Encoding UTF8 (Join-Path $GeneratedDir "03-db-deployment.yaml")

@"
apiVersion: v1
kind: Service
metadata:
  name: pythonwiki-db
  namespace: $Namespace
spec:
  type: ClusterIP
  selector:
    app: pythonwiki-db
  ports:
    - name: sqlserver
      port: 1433
      targetPort: 1433
"@ | Set-Content -Encoding UTF8 (Join-Path $GeneratedDir "04-db-service.yaml")

@"
apiVersion: apps/v1
kind: Deployment
metadata:
  name: pythonwiki-api
  namespace: $Namespace
spec:
  replicas: 1
  selector:
    matchLabels:
      app: pythonwiki-api
  template:
    metadata:
      labels:
        app: pythonwiki-api
      annotations:
        lab4/deploy-version: "$DeployVersion"
    spec:
      containers:
        - name: api
          image: $ApiImage
          imagePullPolicy: Never
          ports:
            - containerPort: $ApiPort
          env:
            - name: ASPNETCORE_URLS
              value: "http://+:$ApiPort"
            - name: ASPNETCORE_ENVIRONMENT
              value: "Development"
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: pythonwiki-db-secret
                  key: CONNECTION_STRING
          startupProbe:
            tcpSocket:
              port: $ApiPort
            failureThreshold: 30
            periodSeconds: 5
          readinessProbe:
            tcpSocket:
              port: $ApiPort
            initialDelaySeconds: 5
            periodSeconds: 10
            timeoutSeconds: 3
          livenessProbe:
            tcpSocket:
              port: $ApiPort
            initialDelaySeconds: 30
            periodSeconds: 20
            timeoutSeconds: 3
"@ | Set-Content -Encoding UTF8 (Join-Path $GeneratedDir "05-api-deployment.yaml")

@"
apiVersion: v1
kind: Service
metadata:
  name: pythonwiki-api
  namespace: $Namespace
spec:
  type: LoadBalancer
  selector:
    app: pythonwiki-api
  ports:
    - name: http
      port: $ApiPort
      targetPort: $ApiPort
"@ | Set-Content -Encoding UTF8 (Join-Path $GeneratedDir "06-api-service.yaml")

Write-Ok "Manifests generated in $GeneratedDir"

Write-Step "Applying Kubernetes manifests"
# Remove the API deployment before re-applying so a local image with the same tag is definitely reloaded.
& kubectl delete deployment pythonwiki-api -n $Namespace --ignore-not-found=true | Out-Null
Invoke-Logged kubectl @("apply", "-f", $GeneratedDir)

Write-Step "Waiting for database rollout"
& kubectl rollout status deployment/pythonwiki-db -n $Namespace --timeout=180s
if ($LASTEXITCODE -ne 0) {
    Write-Host "Database is not ready yet. Showing pods for diagnostics:" -ForegroundColor Yellow
    & kubectl get pods -n $Namespace -o wide
}

Write-Step "Waiting for API rollout with 1 replica"
& kubectl rollout status deployment/pythonwiki-api -n $Namespace --timeout=240s
if ($LASTEXITCODE -ne 0) {
    Write-Host "API is not ready yet. Showing logs for diagnostics:" -ForegroundColor Yellow
    & kubectl describe pods -n $Namespace -l app=pythonwiki-api
    & kubectl logs deployment/pythonwiki-api -n $Namespace --tail=100
}

if ($ApiReplicas -gt 1) {
    Write-Step "Scaling API to configured replica count: $ApiReplicas"
    Invoke-Logged kubectl @("scale", "deployment/pythonwiki-api", "--replicas=$ApiReplicas", "-n", $Namespace)
    & kubectl rollout status deployment/pythonwiki-api -n $Namespace --timeout=240s
    if ($LASTEXITCODE -ne 0) {
        Write-Host "API scaling rollout did not finish. Showing diagnostics:" -ForegroundColor Yellow
        & kubectl describe pods -n $Namespace -l app=pythonwiki-api
        & kubectl logs deployment/pythonwiki-api -n $Namespace --tail=100
    }
}

Write-Step "Current Kubernetes resources"
& kubectl get pods,svc,pvc -n $Namespace -o wide

Write-Step "Smoke test"

function Test-Url($Url, $Attempts = 3) {
    for ($i = 1; $i -le $Attempts; $i++) {
        try {
            $response = Invoke-RestMethod -Uri $Url -TimeoutSec 5
            Write-Host "[OK] Smoke test succeeded: $Url" -ForegroundColor Green
            Write-Host "Smoke test response:"
            if ($response -is [string]) { Write-Host ($response.Substring(0, [Math]::Min(200, $response.Length))) } else { $response | ConvertTo-Json -Depth 5 }
            return $true
        }
        catch {
            Write-Host "Attempt $i failed for $Url" -ForegroundColor DarkYellow
            Start-Sleep -Seconds 3
        }
    }
    return $false
}

$serviceJson = (& kubectl get svc pythonwiki-api -n $Namespace -o json) | ConvertFrom-Json
$nodePort = $serviceJson.spec.ports[0].nodePort
$externalIp = $serviceJson.status.loadBalancer.ingress[0].ip

$candidateHealthUrls = @()
$candidateHealthUrls += "http://localhost:$ApiPort/health"
$candidateHealthUrls += "http://localhost:$ApiPort/swagger"
if ($externalIp) { $candidateHealthUrls += "http://$externalIp`:$ApiPort/health" }
if ($externalIp) { $candidateHealthUrls += "http://$externalIp`:$ApiPort/swagger" }
if ($nodePort) { $candidateHealthUrls += "http://localhost:$nodePort/health" }
if ($nodePort) { $candidateHealthUrls += "http://localhost:$nodePort/swagger" }

$ok = $false
foreach ($url in $candidateHealthUrls) {
    if (Test-Url $url 2) {
        $ok = $true
        $healthUrl = $url
        break
    }
}

if (-not $ok) {
    Write-Host "LoadBalancer/NodePort did not answer directly. Trying temporary kubectl port-forward..." -ForegroundColor Yellow
    $SmokePort = 18080
    $pfJob = Start-Job -ScriptBlock {
        param($Ns, $LocalPort, $RemotePort)
        kubectl port-forward svc/pythonwiki-api "$($LocalPort):$($RemotePort)" -n $Ns
    } -ArgumentList $Namespace, $SmokePort, $ApiPort

    try {
        Start-Sleep -Seconds 5
        $pfHealthUrls = @("http://localhost:$SmokePort/health", "http://localhost:$SmokePort/swagger")
        foreach ($pfHealthUrl in $pfHealthUrls) {
            if (Test-Url $pfHealthUrl 5) {
                $ok = $true
                $healthUrl = $pfHealthUrl
                break
            }
        }
    }
    finally {
        Stop-Job $pfJob -ErrorAction SilentlyContinue | Out-Null
        Remove-Job $pfJob -ErrorAction SilentlyContinue | Out-Null
    }
}

if (-not $ok) {
    Write-Host "Smoke test failed, but Kubernetes rollout succeeded. Showing diagnostics:" -ForegroundColor Yellow
    & kubectl get endpoints pythonwiki-api -n $Namespace -o wide
    & kubectl describe svc pythonwiki-api -n $Namespace
    & kubectl logs deployment/pythonwiki-api -n $Namespace --tail=80
}

$lbHealthUrl = "http://localhost:$ApiPort/healthz"
$lbSwaggerUrl = "http://localhost:$ApiPort/swagger"
if ($externalIp) {
    $lbHealthUrl = "http://$externalIp`:$ApiPort/healthz"
    $lbSwaggerUrl = "http://$externalIp`:$ApiPort/swagger"
}
$nodeHealthUrl = if ($nodePort) { "http://localhost:$nodePort/health" } else { "not available" }

Write-Host ""
Write-Host "Installation finished." -ForegroundColor Green
Write-Host "Smoke checked: $healthUrl"
Write-Host "LoadBalancer health: $lbHealthUrl"
Write-Host "NodePort health:     $nodeHealthUrl"
Write-Host "Swagger:             $lbSwaggerUrl"
Write-Host "Manage:              .\deploy\Dashboard-Lab4.ps1"
Write-Host "Remove:              .\deploy\Uninstall-Lab4.ps1"
