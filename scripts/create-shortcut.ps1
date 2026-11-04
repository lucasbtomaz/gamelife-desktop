param(
    [switch]$LibraryOnly,
    [string]$ExecutablePath
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'local-paths.ps1')

function Get-GameLifePublishedExecutablePath {
    param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))

    Join-Path $ProjectRoot 'artifacts\GameLife\GameLife.exe'
}

function New-GameLifeShortcut {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [Parameter(Mandatory)][string]$ShortcutPath,
        [Parameter(Mandatory)]$Shell
    )

    if (Test-Path -LiteralPath $ShortcutPath) {
        Remove-Item -LiteralPath $ShortcutPath -Force
    }

    $shortcut = $Shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $ExecutablePath
    $shortcut.Arguments = ''
    $shortcut.WorkingDirectory = Split-Path $ExecutablePath -Parent
    $shortcut.IconLocation = "$ExecutablePath,0"
    $shortcut.Description = 'GameLife - Biblioteca de jogos'
    $shortcut.Save()
}

function Install-GameLifeShortcut {
    param(
        [Parameter(Mandatory)][string]$InstalledExecutablePath,
        [Parameter(Mandatory)][string]$ShortcutPath,
        [Parameter(Mandatory)]$Shell
    )

    if (-not (Test-Path -LiteralPath $InstalledExecutablePath)) {
        throw "Executável não encontrado. Execute 'scripts\publish-local.ps1' primeiro."
    }

    New-GameLifeShortcut `
        -ExecutablePath $InstalledExecutablePath `
        -ShortcutPath $ShortcutPath `
        -Shell $Shell
}

if (-not $LibraryOnly) {
    if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
        $ExecutablePath = Get-GameLifePublishedExecutablePath
    }
    $desktopPath = Get-GameLifeDesktopPath
    $shortcutPath = Join-Path $desktopPath 'GameLife.lnk'
    $shell = New-Object -ComObject WScript.Shell
    Install-GameLifeShortcut `
        -InstalledExecutablePath $ExecutablePath `
        -ShortcutPath $shortcutPath `
        -Shell $shell
    Write-Host "Atalho criado em '$shortcutPath'."
}
