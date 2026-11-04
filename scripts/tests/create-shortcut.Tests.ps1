$scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'create-shortcut.ps1'

Describe 'Atalho desktop do GameLife' {
    BeforeAll {
        . $scriptPath -LibraryOnly
    }

    It 'usa o executável instalado em LocalApplicationData' {
        $localData = Join-Path $TestDrive 'LocalAppData'

        Get-GameLifeInstalledExecutablePath -LocalApplicationData $localData |
            Should Be (Join-Path $localData 'GameLife\App\GameLife.exe')
    }

    It 'aponta diretamente para o executável sem PowerShell ou argumentos' {
        $atalho = [pscustomobject]@{
            TargetPath = $null
            Arguments = $null
            WorkingDirectory = $null
            IconLocation = $null
            Description = $null
            Salvo = $false
        }
        $atalho | Add-Member -MemberType ScriptMethod -Name Save -Value { $this.Salvo = $true }
        $shell = [pscustomobject]@{ Atalho = $atalho }
        $shell | Add-Member -MemberType ScriptMethod -Name CreateShortcut -Value { param($caminho) $this.Atalho }
        $executavel = Join-Path $TestDrive 'GameLife.exe'
        Set-Content -LiteralPath $executavel -Value ''

        New-GameLifeShortcut -ExecutablePath $executavel -ShortcutPath (Join-Path $TestDrive 'GameLife.lnk') -Shell $shell

        $atalho.TargetPath | Should Be $executavel
        $atalho.Arguments | Should BeNullOrEmpty
        $atalho.WorkingDirectory | Should Be (Split-Path $executavel -Parent)
        $atalho.IconLocation | Should Be "$executavel,0"
        $atalho.Salvo | Should Be $true
    }

    It 'preserva o destino instalado ao carregar a configuracao local' {
        $executavel = Join-Path $TestDrive 'GameLife instalado\GameLife.exe'
        New-Item -ItemType Directory -Path (Split-Path $executavel -Parent) -Force | Out-Null
        Set-Content -LiteralPath $executavel -Value ''
        $script:destinoRecebido = $null

        . $launcherPath
        Mock Get-GameLifeConnectionString { 'conexao-de-teste' }
        Mock Save-GameLifeLocalConfiguration { Join-Path $TestDrive 'appsettings.Local.json' }
        Mock New-Object { [pscustomobject]@{} } -ParameterFilter { $ComObject -eq 'WScript.Shell' }
        Mock New-GameLifeShortcut {
            param($ExecutablePath)
            $script:destinoRecebido = $ExecutablePath
        }

        Install-GameLifeShortcut -InstalledExecutablePath $executavel

        $script:destinoRecebido | Should Be $executavel
    }
}
