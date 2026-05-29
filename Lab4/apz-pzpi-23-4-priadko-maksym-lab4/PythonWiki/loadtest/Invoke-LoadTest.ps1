param(
    [string]$Url = "http://localhost:8080/swagger",
    [int]$Users = 20,
    [int]$RequestsPerUser = 50,
    [string]$OutFile = ""
)

$ErrorActionPreference = "Continue"

if ($Users -lt 1) { $Users = 1 }
if ($Users -gt 100) {
    Write-Host "Users value is too high for this simple PowerShell test. Limiting to 100." -ForegroundColor Yellow
    $Users = 100
}
if ($RequestsPerUser -lt 1) { $RequestsPerUser = 1 }

$totalRequests = $Users * $RequestsPerUser
Write-Host "Target: $Url"
Write-Host "Concurrent users: $Users"
Write-Host "Requests per user: $RequestsPerUser"
Write-Host "Total requests: $totalRequests"

try {
    $warmup = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10
    Write-Host "Warm-up status: $($warmup.StatusCode)" -ForegroundColor Green
}
catch {
    Write-Host "Warm-up request failed: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "The load test will still run, but failures will be counted." -ForegroundColor Yellow
}

# This worker intentionally uses Invoke-WebRequest instead of System.Net.Http.HttpClient.
# HttpClient can be unavailable inside Start-Job in Windows PowerShell 5.x.
$worker = {
    param($Url, $RequestsPerUser)

    $ok = 0
    $fail = 0
    $latencies = @()

    for ($i = 0; $i -lt $RequestsPerUser; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 15
            $sw.Stop()
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) { $ok++ } else { $fail++ }
        }
        catch {
            $sw.Stop()
            $fail++
        }
        $latencies += [Math]::Round($sw.Elapsed.TotalMilliseconds, 2)
    }

    [PSCustomObject]@{
        Ok = $ok
        Fail = $fail
        Latencies = $latencies
    }
}

$started = Get-Date
$swTotal = [System.Diagnostics.Stopwatch]::StartNew()
$jobs = @()

try {
    for ($u = 1; $u -le $Users; $u++) {
        $jobs += Start-Job -ScriptBlock $worker -ArgumentList $Url, $RequestsPerUser
    }

    $results = Receive-Job -Job $jobs -Wait -AutoRemoveJob -ErrorAction Continue
}
finally {
    Get-Job | Where-Object { $_.State -in @("Running", "Failed", "Stopped") } | Remove-Job -Force -ErrorAction SilentlyContinue
}

$swTotal.Stop()

if (-not $results) {
    Write-Host "No results were returned by worker jobs." -ForegroundColor Red
    exit 1
}

$ok = (($results | Measure-Object -Property Ok -Sum).Sum)
$fail = (($results | Measure-Object -Property Fail -Sum).Sum)
if ($null -eq $ok) { $ok = 0 }
if ($null -eq $fail) { $fail = 0 }

$latencies = @($results | ForEach-Object { $_.Latencies } | ForEach-Object { $_ }) | Sort-Object

function Percentile($Values, [double]$P) {
    if (-not $Values -or $Values.Count -eq 0) { return 0 }
    $index = [Math]::Ceiling(($P / 100.0) * $Values.Count) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $Values.Count) { $index = $Values.Count - 1 }
    return [Math]::Round([double]$Values[$index], 2)
}

$duration = [Math]::Max($swTotal.Elapsed.TotalSeconds, 0.001)
$rps = [Math]::Round($ok / $duration, 2)
$avg = if ($latencies.Count -gt 0) { [Math]::Round(($latencies | Measure-Object -Average).Average, 2) } else { 0 }
$p95 = Percentile $latencies 95
$p99 = Percentile $latencies 99

$summary = [PSCustomObject]@{
    Started = $started.ToString("s")
    Url = $Url
    Users = $Users
    RequestsPerUser = $RequestsPerUser
    TotalRequests = $totalRequests
    Success = [int]$ok
    Failed = [int]$fail
    DurationSeconds = [Math]::Round($duration, 2)
    RequestsPerSecond = $rps
    AvgLatencyMs = $avg
    P95LatencyMs = $p95
    P99LatencyMs = $p99
}

Write-Host ""
Write-Host "Load test result" -ForegroundColor Cyan
$summary | Format-List

if ([string]::IsNullOrWhiteSpace($OutFile)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutFile = Join-Path $PSScriptRoot "results-$timestamp.csv"
}

$summary | Export-Csv -Path $OutFile -NoTypeInformation -Encoding UTF8
Write-Host "Saved: $OutFile" -ForegroundColor Green
