param([switch]$LibraryOnly)

$ErrorActionPreference = 'Stop'

function Get-GameLifePublishPaths {
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [string]$LocalApplicationData = [Environment]::GetFolderPath('LocalApplicationData')
    )

    $installPath = Join-Path $LocalApplicationData 'GameLife\App'

    [pscustomobject]@{
        ProjectRoot = $ProjectRoot
        ScriptsRoot = Join-Path $ProjectRoot 'scripts'
        WebRoot = Join-Path $ProjectRoot 'src\GameLife.Web'
        SolutionPath = Join-Path $ProjectRoot 'GameLife.slnx'
        ApiProject = Join-Path $ProjectRoot 'src\GameLife.Api\GameLife.Api.csproj'
        DesktopProject = Join-Path $ProjectRoot 'src\GameLife.Desktop\GameLife.Desktop.csproj'
        OutputPath = Join-Path $ProjectRoot 'artifacts\GameLife'
        InstallPath = $installPath
        InstalledExecutablePath = Join-Path $installPath 'GameLife.exe'
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
        [pscustomobject]@{ Name = 'dotnet tool restore'; WorkingDirectory = $Paths.ProjectRoot; Command = { dotnet tool restore } },
        [pscustomobject]@{
            Name = 'dotnet ef database update'
            WorkingDirectory = $Paths.ProjectRoot
            Command = {
                . (Join-Path $Paths.ScriptsRoot 'start-local.ps1')
                $connectionString = Get-GameLifeConnectionString
                $previousConnectionString = [Environment]::GetEnvironmentVariable('ConnectionStrings__GameLife', 'Process')
                try {
                    [Environment]::SetEnvironmentVariable('ConnectionStrings__GameLife', $connectionString, 'Process')
                    dotnet ef database update --project $Paths.ApiProject --startup-project $Paths.ApiProject --configuration Release --no-build
                }
                finally {
                    [Environment]::SetEnvironmentVariable('ConnectionStrings__GameLife', $previousConnectionString, 'Process')
                }
            }.GetNewClosure()
        },
        [pscustomobject]@{ Name = 'dotnet publish'; WorkingDirectory = $Paths.ProjectRoot; Command = { dotnet publish $Paths.DesktopProject --configuration Release --runtime win-x64 --self-contained true --output $Paths.OutputPath }.GetNewClosure() },
        [pscustomobject]@{
            Name = 'install local'
            WorkingDirectory = $Paths.ProjectRoot
            DestinationPath = $Paths.InstallPath
            Command = {
                & (Join-Path $Paths.ScriptsRoot 'install-local.ps1') `
                    -SourcePath $Paths.OutputPath `
                    -DestinationPath $Paths.InstallPath
            }.GetNewClosure()
        },
        [pscustomobject]@{
            Name = 'create shortcut'
            WorkingDirectory = $Paths.ProjectRoot
            ExecutablePath = $Paths.InstalledExecutablePath
            Command = {
                & (Join-Path $Paths.ScriptsRoot 'create-shortcut.ps1') `
                    -ExecutablePath $Paths.InstalledExecutablePath
            }.GetNewClosure()
        }
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
