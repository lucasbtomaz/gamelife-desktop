$scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'create-shortcut.ps1'

Describe 'Atalho desktop do GameLife' {
    BeforeAll {
        . $scriptPath -LibraryOnly
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
        $atalho.Salvo | Should Be $true
    }
}
