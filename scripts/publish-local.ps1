param([switch]$LibraryOnly)

$ErrorActionPreference = 'Stop'

function Get-GameLifePublishPaths {
    param([Parameter(Mandatory)][string]$ProjectRoot)

    [pscustomobject]@{
        ProjectRoot = $ProjectRoot
        WebRoot = Join-Path $ProjectRoot 'src\GameLife.Web'
        SolutionPath = Join-Path $ProjectRoot 'GameLife.slnx'
        DesktopProject = Join-Path $ProjectRoot 'src\GameLife.Desktop\GameLife.Desktop.csproj'
        OutputPath = Join-Path $ProjectRoot 'artifacts\GameLife'
    }
}

function Get-GameLifePublishSteps {
    param([Parameter(Mandatory)]$Paths)

    @(
        [pscustomobject]@{ Name = 'npm ci'; WorkingDirectory = $Paths.WebRoot; Command = { npm.cmd ci } },
        [pscustomobject]@{ Name = 'npm test'; WorkingDirectory = $Paths.WebRoot; Command = { npm.cmd test } },
        [pscustomobject]@{ Name = 'npm run build'; WorkingDirectory = $Paths.WebRoot; Command = { npm.cmd run build } },
        [pscustomobject]@{ Name = 'dotnet restore'; WorkingDirectory = $Paths.ProjectRoot; Command = { dotnet restore $Paths.SolutionPath }.GetNewClosure() },
        [pscustomobject]@{ Name = 'dotnet test'; WorkingDirectory = $Paths.ProjectRoot; Command = { dotnet test $Paths.SolutionPath --configuration Release --no-restore }.GetNewClosure() },
        [pscustomobject]@{ Name = 'dotnet publish'; WorkingDirectory = $Paths.ProjectRoot; Command = { dotnet publish $Paths.DesktopProject --configuration Release --runtime win-x64 --self-contained true --output $Paths.OutputPath }.GetNewClosure() }
    )
}

function Invoke-GameLifePublishStep {
    param([Parameter(Mandatory)]$Step)

    Push-Location $Step.WorkingDirectory
    try {
        $global:LASTEXITCODE = 0
        & $Step.Command | Out-Host
        return [int]$global:LASTEXITCODE
    }
    finally {
        Pop-Location
    }
}

function Invoke-GameLifePublish {
    param(
        [Parameter(Mandatory)]$Paths,
        [scriptblock]$StepRunner
    )

    if ($null -eq $StepRunner) {
        $StepRunner = {
            param($step)
            Invoke-GameLifePublishStep -Step $step
        }
    }

    foreach ($step in Get-GameLifePublishSteps -Paths $Paths) {
        $exitCode = & $StepRunner $step
        if ($exitCode -ne 0) {
            throw "A etapa '$($step.Name)' falhou com código $exitCode."
        }
    }
}

if (-not $LibraryOnly) {
    $projectRoot = Split-Path $PSScriptRoot -Parent
    $paths = Get-GameLifePublishPaths -ProjectRoot $projectRoot
    if (-not (Test-Path -LiteralPath (Join-Path $paths.WebRoot 'package.json'))) {
        throw "Frontend não encontrado em '$($paths.WebRoot)'."
    }

    Invoke-GameLifePublish -Paths $paths
    Write-Host "Publicação criada em '$($paths.OutputPath)'."
}
