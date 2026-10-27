$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $PSScriptRoot -Parent
$executablePath = Join-Path $projectRoot 'artifacts\GameLife\GameLife.exe'
$launcherPath = Join-Path $PSScriptRoot 'start-local.ps1'

if (-not (Test-Path -LiteralPath $executablePath)) {
    throw "Executável não encontrado. Execute 'scripts\publish-local.ps1' primeiro."
}

. $launcherPath
$connectionString = Get-GameLifeConnectionString
$configurationPath = Save-GameLifeLocalConfiguration -ConnectionString $connectionString

$powerShellPath = (Get-Command powershell.exe).Source
$desktopPath = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopPath 'GameLife.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $powerShellPath
$shortcut.Arguments = "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$launcherPath`""
$shortcut.WorkingDirectory = Split-Path $executablePath -Parent
$shortcut.IconLocation = "$executablePath,0"
$shortcut.Description = 'GameLife - Biblioteca de jogos'
$shortcut.Save()

Write-Host "Atalho criado em '$shortcutPath'."
Write-Host "Configuração local criada em '$configurationPath'."
