param(
    [switch]$LibraryOnly,
    [string]$ExecutablePath
)

$ErrorActionPreference = 'Stop'

$launcherPath = Join-Path $PSScriptRoot 'start-local.ps1'
. (Join-Path $PSScriptRoot 'local-paths.ps1')

function Get-GameLifeInstalledExecutablePath {
    param(
        [string]$LocalApplicationData = (Get-GameLifeLocalApplicationDataPath)
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

function Install-GameLifeConfiguration {
    param([Parameter(Mandatory)][string]$InstalledExecutablePath)

    . $launcherPath
    $connectionString = Get-GameLifeConnectionString
    $configurationPath = Save-GameLifeLocalConfiguration -ConnectionString $connectionString
    $installedConfigurationPath = Join-Path `
        (Split-Path $InstalledExecutablePath -Parent) `
        'appsettings.Local.json'
    Save-GameLifeLocalConfiguration `
        -ConnectionString $connectionString `
        -ConfigurationPath $installedConfigurationPath | Out-Null

    Write-Host "Configuração local criada em '$configurationPath'."
    Write-Host "Cópia de segurança criada em '$installedConfigurationPath'."
}

if (-not $LibraryOnly) {
    if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
        $ExecutablePath = Get-GameLifeInstalledExecutablePath
    }
    $desktopPath = Get-GameLifeDesktopPath
    $shortcutPath = Join-Path $desktopPath 'GameLife.lnk'
    $shell = New-Object -ComObject WScript.Shell
    Install-GameLifeConfiguration -InstalledExecutablePath $ExecutablePath
    Install-GameLifeShortcut `
        -InstalledExecutablePath $ExecutablePath `
        -ShortcutPath $shortcutPath `
        -Shell $shell
    Write-Host "Atalho criado em '$shortcutPath'."
}
