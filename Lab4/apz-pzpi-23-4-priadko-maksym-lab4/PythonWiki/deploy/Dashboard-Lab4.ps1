param(
    [string]$ConfigPath = "$PSScriptRoot\config.env"
)

$ErrorActionPreference = "Continue"

function Read-EnvFile($Path) {
    $config = @{}
    if (Test-Path $Path) {
        Get-Content $Path | ForEach-Object {
            $line = $_.Trim()
            if ($line -eq "" -or $line.StartsWith("#")) { return }
            $parts = $line -split "=", 2
            if ($parts.Count -eq 2) { $config[$parts[0].Trim()] = $parts[1].Trim() }
        }
    }
    return $config
}

function Get-ConfigValue($Config, $Key, $Default) {
    if ($Config.ContainsKey($Key) -and $Config[$Key] -ne "") { return $Config[$Key] }
    return $Default
}

function Run {
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$Command,

        [Parameter(ValueFromRemainingArguments=$true)]
        [string[]]$CommandArgs
    )

    Write-Host "> $Command $($CommandArgs -join ' ')" -ForegroundColor DarkGray
    & $Command @CommandArgs
}

function Read-IntOrDefault {
    param(
        [string]$Prompt,
        [int]$Default,
        [int]$Min = 1,
        [int]$Max = 1000000
    )

    while ($true) {
        $text = Read-Host "$Prompt [$Default]"
        if ([string]::IsNullOrWhiteSpace($text)) { return $Default }
        $value = 0
        if ([int]::TryParse($text.Trim(), [ref]$value) -and $value -ge $Min -and $value -le $Max) {
            return $value
        }
        Write-Host "Enter an integer from $Min to $Max." -ForegroundColor Yellow
    }
}

$config = Read-EnvFile $ConfigPath
$Namespace = Get-ConfigValue $config "NAMESPACE" "pythonwiki"
$ApiPort = [int](Get-ConfigValue $config "API_PORT" "8080")
$Root = Resolve-Path (Join-Path $PSScriptRoot "..")

function Show-Pods {
    Run kubectl "get" "pods" "-n" $Namespace "-o" "wide"
}

function Show-Services {
    Run kubectl "get" "svc,pvc" "-n" $Namespace "-o" "wide"
}

function Scale-Api {
    $replicas = Read-IntOrDefault -Prompt "New API replica count" -Default 2 -Min 0 -Max 50
    Run kubectl "scale" "deployment/pythonwiki-api" "--replicas=$replicas" "-n" $Namespace
    Run kubectl "rollout" "status" "deployment/pythonwiki-api" "-n" $Namespace "--timeout=180s"
    Show-Pods
}

function Show-Logs {
    Run kubectl "logs" "deployment/pythonwiki-api" "-n" $Namespace "--tail=120"
}

function Call-Health {
    $healthUrl = "http://localhost:$ApiPort/healthz"
    $swaggerUrl = "http://localhost:$ApiPort/swagger"
    Write-Host "Calling $healthUrl 12 times. With several replicas the machine/pod field should change."

    $healthOk = $false
    1..12 | ForEach-Object {
        $idx = $_
        try {
            $response = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 5
            $healthOk = $true
            Write-Host "$idx. " -NoNewline
            $response | ConvertTo-Json -Compress
        }
        catch {
            Write-Host "$idx. /health failed: $($_.Exception.Message)" -ForegroundColor Yellow
        }
        Start-Sleep -Milliseconds 300
    }

    if (-not $healthOk) {
        Write-Host ""
        Write-Host "/health returned errors, but this does not mean the service is down." -ForegroundColor Yellow
        Write-Host "Checking Swagger endpoint instead: $swaggerUrl"
        try {
            $swagger = Invoke-WebRequest -Uri $swaggerUrl -UseBasicParsing -TimeoutSec 10
            Write-Host "[OK] Swagger is available. StatusCode: $($swagger.StatusCode)" -ForegroundColor Green
            Write-Host "For load testing, use option 6 with the default /swagger URL." -ForegroundColor Green
        }
        catch {
            Write-Host "Swagger check also failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

function Run-LoadTest {
    try {
        $defaultUrl = "http://localhost:$ApiPort/swagger"
        $url = Read-Host "Target URL [$defaultUrl]"
        if ([string]::IsNullOrWhiteSpace($url)) { $url = $defaultUrl }

        $users = Read-IntOrDefault -Prompt "Concurrent users" -Default 20 -Min 1 -Max 200
        $requestsPerUser = Read-IntOrDefault -Prompt "Requests per user" -Default 50 -Min 1 -Max 10000

        $script = Join-Path $Root "loadtest\Invoke-LoadTest.ps1"
        if (-not (Test-Path $script)) {
            Write-Host "Load test script was not found: $script" -ForegroundColor Red
            return
        }

        Write-Host "Starting load test. For the demo, run it with 1, 2, 3 replicas and compare RPS." -ForegroundColor Cyan
        & powershell -NoProfile -ExecutionPolicy Bypass -File $script -Url $url -Users $users -RequestsPerUser $requestsPerUser
    }
    catch {
        Write-Host "Load test failed, but dashboard will continue." -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
    }
}

function Open-Swagger {
    Start-Process "http://localhost:$ApiPort/swagger"
}

while ($true) {
    Write-Host ""
    Write-Host "PythonWiki Kubernetes Dashboard" -ForegroundColor Cyan
    Write-Host "Namespace: $Namespace | API: http://localhost:$ApiPort"
    Write-Host "1. Show pods"
    Write-Host "2. Show services and PVC"
    Write-Host "3. Scale API replicas"
    Write-Host "4. Show API logs"
    Write-Host "5. Call /health several times"
    Write-Host "6. Run PowerShell load test"
    Write-Host "7. Open Swagger"
    Write-Host "8. Exit"
    $choice = Read-Host "Choose option"

    try {
        switch ($choice) {
            "1" { Show-Pods }
            "2" { Show-Services }
            "3" { Scale-Api }
            "4" { Show-Logs }
            "5" { Call-Health }
            "6" { Run-LoadTest }
            "7" { Open-Swagger }
            "8" { break }
            default { Write-Host "Unknown option." -ForegroundColor Yellow }
        }
    }
    catch {
        Write-Host "Operation failed, dashboard is still running." -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
    }
}
