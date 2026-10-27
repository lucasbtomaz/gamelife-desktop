$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $PSScriptRoot -Parent
$webRoot = Join-Path (Split-Path $projectRoot -Parent) 'gamelife-web'
$outputPath = Join-Path $projectRoot 'artifacts\GameLife'

function Invoke-Checked {
    param([scriptblock]$Command)

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Comando falhou com código $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $webRoot 'package.json'))) {
    throw "Frontend não encontrado em '$webRoot'."
}

Push-Location $webRoot
try {
    Invoke-Checked { npm.cmd ci }
    Invoke-Checked { npm.cmd test }
    Invoke-Checked { npm.cmd run build }
}
finally {
    Pop-Location
}

Push-Location $projectRoot
try {
    Invoke-Checked { dotnet restore gamelife-api.slnx }
    Invoke-Checked { dotnet test gamelife-api.slnx --configuration Release --no-restore }
    Invoke-Checked { dotnet restore gamelife-api\gamelife-api.csproj --runtime win-x64 }
    Invoke-Checked {
        dotnet publish gamelife-api\gamelife-api.csproj `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --no-restore `
            --output $outputPath
    }
}
finally {
    Pop-Location
}

Write-Host "Publicação criada em '$outputPath'."
