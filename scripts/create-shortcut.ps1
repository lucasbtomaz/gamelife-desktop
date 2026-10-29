param([switch]$LibraryOnly)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $PSScriptRoot -Parent
$executablePath = Join-Path $projectRoot 'artifacts\GameLife\GameLife.exe'
$launcherPath = Join-Path $PSScriptRoot 'start-local.ps1'

function New-GameLifeShortcut {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [Parameter(Mandatory)][string]$ShortcutPath,
        [Parameter(Mandatory)]$Shell
    )

    $shortcut = $Shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $ExecutablePath
    $shortcut.Arguments = ''
    $shortcut.WorkingDirectory = Split-Path $ExecutablePath -Parent
    $shortcut.IconLocation = "$ExecutablePath,0"
    $shortcut.Description = 'GameLife - Biblioteca de jogos'
    $shortcut.Save()
}

function Install-GameLifeShortcut {
    if (-not (Test-Path -LiteralPath $executablePath)) {
        throw "Executável não encontrado. Execute 'scripts\publish-local.ps1' primeiro."
    }

    . $launcherPath
    $connectionString = Get-GameLifeConnectionString
    $configurationPath = Save-GameLifeLocalConfiguration -ConnectionString $connectionString
    $desktopPath = [Environment]::GetFolderPath('Desktop')
    $shortcutPath = Join-Path $desktopPath 'GameLife.lnk'
    $shell = New-Object -ComObject WScript.Shell
    New-GameLifeShortcut -ExecutablePath $executablePath -ShortcutPath $shortcutPath -Shell $shell

    Write-Host "Atalho criado em '$shortcutPath'."
    Write-Host "Configuração local criada em '$configurationPath'."
}

if (-not $LibraryOnly) {
    Install-GameLifeShortcut
}
