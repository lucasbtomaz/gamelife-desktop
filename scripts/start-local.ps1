param(
    [string]$ExecutablePath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\GameLife\GameLife.exe'),
    [string]$Url = 'http://127.0.0.1:5044',
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'local-paths.ps1')

function Get-GameLifeLocalConfigurationPath {
    param([string]$LocalApplicationData = (Get-GameLifeLocalApplicationDataPath))

    Join-Path $LocalApplicationData 'GameLife\appsettings.Local.json'
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
    param(
        [Parameter(Mandatory)][string]$ConnectionString,
        [string]$ConfigurationPath = (Get-GameLifeLocalConfigurationPath)
    )

    $directory = Split-Path $ConfigurationPath -Parent
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    @{ ConnectionStrings = @{ GameLife = $ConnectionString } } |
        ConvertTo-Json -Depth 3 |
        Set-Content -LiteralPath $ConfigurationPath -Encoding UTF8
    return $ConfigurationPath
}

function Test-GameLifeReady {
    param(
        [string]$HealthUrl,
        [int]$TimeoutSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -Uri "$HealthUrl/health" -Method Get -TimeoutSec 2 -UseBasicParsing
            $health = $response.Content | ConvertFrom-Json
            if ($response.StatusCode -eq 200 -and
                $health.application -eq 'GameLife' -and
                $health.status -eq 'ready') {
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

function Show-GameLifeError {
    param([Parameter(Mandatory)][string]$Message)

    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show(
        $Message,
        'GameLife',
        [System.Windows.MessageBoxButton]::OK,
        [System.Windows.MessageBoxImage]::Error
    ) | Out-Null
}

function Invoke-GameLifeLauncher {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [string]$Url = 'http://127.0.0.1:5044',
        [switch]$NoBrowser
    )

    try {
        Start-GameLife -ExecutablePath $ExecutablePath -Url $Url -NoBrowser:$NoBrowser
        return 0
    }
    catch {
        $dataDirectory = Split-Path (Get-GameLifeLocalConfigurationPath) -Parent
        New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
        $diagnosticPath = Join-Path $dataDirectory 'launcher-error.log'
        $_.Exception.Message | Set-Content -LiteralPath $diagnosticPath -Encoding UTF8
        Show-GameLifeError -Message "O GameLife não conseguiu iniciar. Verifique se o MySQL está ativo. Detalhes técnicos: $diagnosticPath"
        return 1
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    exit (Invoke-GameLifeLauncher -ExecutablePath $ExecutablePath -Url $Url -NoBrowser:$NoBrowser)
}
