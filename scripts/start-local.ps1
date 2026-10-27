param(
    [string]$ExecutablePath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\GameLife\GameLife.exe'),
    [string]$Url = 'http://127.0.0.1:5044',
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'

function Get-GameLifeLocalConfigurationPath {
    Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'GameLife\appsettings.Local.json'
}

function Get-GameLifeConnectionString {
    $localPath = Get-GameLifeLocalConfigurationPath
    if (Test-Path -LiteralPath $localPath) {
        $local = Get-Content -Raw -LiteralPath $localPath | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace($local.ConnectionStrings.GameLife)) {
            return $local.ConnectionStrings.GameLife
        }
    }

    $userSecretsPath = Join-Path $env:APPDATA 'Microsoft\UserSecrets\c59ebefd-a1cb-4fe9-a8bd-3cb748f6c9b7\secrets.json'
    if (Test-Path -LiteralPath $userSecretsPath) {
        $secrets = Get-Content -Raw -LiteralPath $userSecretsPath | ConvertFrom-Json
        $connectionString = $secrets.'ConnectionStrings:GameLife'
        if (-not [string]::IsNullOrWhiteSpace($connectionString)) {
            return $connectionString
        }
    }

    throw "A conexão do MySQL não foi encontrada. Execute 'scripts\create-shortcut.ps1' novamente."
}

function Save-GameLifeLocalConfiguration {
    param([Parameter(Mandatory)][string]$ConnectionString)

    $path = Get-GameLifeLocalConfigurationPath
    $directory = Split-Path $path -Parent
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    @{ ConnectionStrings = @{ GameLife = $ConnectionString } } |
        ConvertTo-Json -Depth 3 |
        Set-Content -LiteralPath $path -Encoding UTF8
    return $path
}

function Test-GameLifeReady {
    param(
        [string]$HealthUrl,
        [int]$TimeoutSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -Uri "$HealthUrl/biblioteca" -Method Get -TimeoutSec 2 -SkipHttpErrorCheck
            if ($response.StatusCode -eq 200) {
                return $true
            }
        }
        catch {
        }

        if ([DateTime]::UtcNow -lt $deadline) {
            Start-Sleep -Milliseconds 250
        }
    } while ([DateTime]::UtcNow -lt $deadline)

    return $false
}

function Start-GameLife {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [string]$Url = 'http://127.0.0.1:5044',
        [switch]$NoBrowser
    )

    if (-not (Test-Path -LiteralPath $ExecutablePath)) {
        throw "Executável do GameLife não encontrado em '$ExecutablePath'. Execute 'scripts\publish-local.ps1'."
    }

    if (-not (Test-GameLifeReady -HealthUrl $Url -TimeoutSeconds 1)) {
        $connectionString = Get-GameLifeConnectionString
        $env:ConnectionStrings__GameLife = $connectionString
        $dataDirectory = Split-Path (Get-GameLifeLocalConfigurationPath) -Parent
        New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null

        $process = Start-Process `
            -FilePath $ExecutablePath `
            -ArgumentList @('--urls', $Url) `
            -WorkingDirectory (Split-Path $ExecutablePath -Parent) `
            -WindowStyle Hidden `
            -RedirectStandardOutput (Join-Path $dataDirectory 'GameLife.log') `
            -RedirectStandardError (Join-Path $dataDirectory 'GameLife-error.log') `
            -PassThru

        if (-not (Test-GameLifeReady -HealthUrl $Url -TimeoutSeconds 30)) {
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force
            }
            throw "O GameLife não conseguiu iniciar. Consulte '$dataDirectory\GameLife-error.log'."
        }
    }

    if (-not $NoBrowser) {
        Start-Process $Url
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    Start-GameLife -ExecutablePath $ExecutablePath -Url $Url -NoBrowser:$NoBrowser
}
