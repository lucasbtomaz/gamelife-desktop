param(
    [switch]$LibraryOnly,
    [string]$ExecutablePath
)

$ErrorActionPreference = 'Stop'

$launcherPath = Join-Path $PSScriptRoot 'start-local.ps1'

function Get-GameLifeInstalledExecutablePath {
    param(
        [string]$LocalApplicationData = [Environment]::GetFolderPath('LocalApplicationData')
    )

    Join-Path $LocalApplicationData 'GameLife\App\GameLife.exe'
}

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
    param([Parameter(Mandatory)][string]$InstalledExecutablePath)

    if (-not (Test-Path -LiteralPath $InstalledExecutablePath)) {
        throw "Executável não encontrado. Execute 'scripts\publish-local.ps1' primeiro."
    }

    . $launcherPath
    $connectionString = Get-GameLifeConnectionString
    $configurationPath = Save-GameLifeLocalConfiguration -ConnectionString $connectionString
    $desktopPath = [Environment]::GetFolderPath('Desktop')
    $shortcutPath = Join-Path $desktopPath 'GameLife.lnk'
    $shell = New-Object -ComObject WScript.Shell
    New-GameLifeShortcut -ExecutablePath $InstalledExecutablePath -ShortcutPath $shortcutPath -Shell $shell

    Write-Host "Atalho criado em '$shortcutPath'."
    Write-Host "Configuração local criada em '$configurationPath'."
}

if (-not $LibraryOnly) {
    if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
        $ExecutablePath = Get-GameLifeInstalledExecutablePath
    }
    Install-GameLifeShortcut -InstalledExecutablePath $ExecutablePath
}
