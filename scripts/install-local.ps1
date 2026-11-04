param(
    [string]$SourcePath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\GameLife'),
    [string]$DestinationPath,
    [switch]$LibraryOnly
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'local-paths.ps1')

function Get-GameLifeInstallRoot {
    param(
        [string]$LocalApplicationData = (Get-GameLifeLocalApplicationDataPath)
    )

    Join-Path $LocalApplicationData 'GameLife\App'
}

function Test-GameLifePublication {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath (Join-Path $Path 'GameLife.exe')) -or
        -not (Test-Path -LiteralPath (Join-Path $Path 'wwwroot\index.html'))) {
        throw "A publicação em '$Path' está incompleta. Gere o aplicativo novamente."
    }
}

function Install-GameLifeFiles {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    Test-GameLifePublication -Path $Source
    $parent = Split-Path $Destination -Parent
    $name = Split-Path $Destination -Leaf
    $token = [Guid]::NewGuid().ToString('N')
    $staging = Join-Path $parent "$name.install-$token"
    $previous = Join-Path $parent "$name.previous-$token"
    $previousMoved = $false

    New-Item -ItemType Directory -Path $parent -Force | Out-Null
    try {
        Copy-Item -LiteralPath $Source -Destination $staging -Recurse -Force
        Test-GameLifePublication -Path $staging

        $installedConfiguration = Join-Path $Destination 'appsettings.Local.json'
        if (Test-Path -LiteralPath $installedConfiguration) {
            Copy-Item -LiteralPath $installedConfiguration `
                -Destination (Join-Path $staging 'appsettings.Local.json') `
                -Force
        }

        if (Test-Path -LiteralPath $Destination) {
            Move-Item -LiteralPath $Destination -Destination $previous
            $previousMoved = $true
        }

        Move-Item -LiteralPath $staging -Destination $Destination
        if (Test-Path -LiteralPath $previous) {
            Remove-Item -LiteralPath $previous -Recurse -Force -ErrorAction SilentlyContinue
        }

        return $Destination
    }
    catch {
        if ($previousMoved -and
            -not (Test-Path -LiteralPath $Destination) -and
            (Test-Path -LiteralPath $previous)) {
            Move-Item -LiteralPath $previous -Destination $Destination -ErrorAction SilentlyContinue
        }
        if (Test-Path -LiteralPath $staging) {
            Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
        }

        throw "Não foi possível atualizar o GameLife. Feche o aplicativo e tente novamente. $($_.Exception.Message)"
    }
}

if (-not $LibraryOnly) {
    if ([string]::IsNullOrWhiteSpace($DestinationPath)) {
        $DestinationPath = Get-GameLifeInstallRoot
    }
    Install-GameLifeFiles -Source $SourcePath -Destination $DestinationPath | Out-Null
    Write-Host "GameLife instalado em '$DestinationPath'."
}
