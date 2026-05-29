param(
    [string]$ConfigPath = "$PSScriptRoot\config.env",
    [switch]$KeepNamespace
)

$ErrorActionPreference = "Stop"

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

$config = Read-EnvFile $ConfigPath
$Namespace = Get-ConfigValue $config "NAMESPACE" "pythonwiki"
$GeneratedDir = Resolve-Path -ErrorAction SilentlyContinue (Join-Path $PSScriptRoot "..\k8s\generated")

if ($KeepNamespace -and $GeneratedDir) {
    Write-Host "Deleting generated manifests but keeping namespace..."
    kubectl delete -f $GeneratedDir --ignore-not-found=true
}
else {
    Write-Host "Deleting namespace $Namespace. This also removes SQL Server PVC/data in that namespace."
    kubectl delete namespace $Namespace --ignore-not-found=true
}
