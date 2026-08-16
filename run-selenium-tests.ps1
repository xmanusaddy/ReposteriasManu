param(
    [switch]$Headless
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$apiUrl = $env:REPOSTERIAS_API_URL
$webUrl = $env:REPOSTERIAS_WEB_URL

if ([string]::IsNullOrWhiteSpace($apiUrl)) { $apiUrl = "http://localhost:5255" }
if ([string]::IsNullOrWhiteSpace($webUrl)) { $webUrl = "http://localhost:52456" }

function Test-UrlReady {
    param(
        [string]$Url,
        [int]$ExpectedStatus = 200
    )

    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
        return [int]$response.StatusCode -eq $ExpectedStatus
    }
    catch {
        return $false
    }
}

function Wait-UrlReady {
    param(
        [string]$Url,
        [int]$ExpectedStatus = 200,
        [int]$TimeoutSeconds = 120
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-UrlReady -Url $Url -ExpectedStatus $ExpectedStatus) {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "Timed out waiting for $Url"
}

function Start-Project {
    param(
        [string]$ProjectPath
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new("dotnet")
    $startInfo.WorkingDirectory = $root
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Arguments = "run --project `"$ProjectPath`" --no-build"

    return [System.Diagnostics.Process]::Start($startInfo)
}

$startedProcesses = @()

Push-Location $root
try {
    dotnet build ReposteriasManu.AutomatedTests/ReposteriasManu.AutomatedTests.csproj

    $apiHealthUrl = "$($apiUrl.TrimEnd('/'))/health/database"

    if (-not (Test-UrlReady -Url $apiHealthUrl)) {
        dotnet build ReposteriasManu.API/ReposteriasManu.API.csproj
        $startedProcesses += Start-Project "ReposteriasManu.API/ReposteriasManu.API.csproj"
        Wait-UrlReady -Url $apiHealthUrl
    }

    if (-not (Test-UrlReady -Url $webUrl)) {
        dotnet build ReposteriasManu.Web/ReposteriasManu.Web.csproj
        $startedProcesses += Start-Project "ReposteriasManu.Web/ReposteriasManu.Web.csproj"
        Wait-UrlReady -Url $webUrl
    }

    $env:REPOSTERIAS_API_URL = $apiUrl
    $env:REPOSTERIAS_WEB_URL = $webUrl

    if ($Headless) {
        $env:REPOSTERIAS_SELENIUM_HEADLESS = "true"
    }

    dotnet test ReposteriasManu.AutomatedTests/ReposteriasManu.AutomatedTests.csproj --no-build --logger "trx"
}
finally {
    foreach ($process in $startedProcesses) {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }
    }

    Pop-Location
}
