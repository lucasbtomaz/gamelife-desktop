Describe 'Atalho desktop do GameLife' {
    BeforeAll {
        $script:scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'create-shortcut.ps1'
        . $script:scriptPath -LibraryOnly
    }

    It 'usa o executavel publicado dentro do repositorio como o FinBot' {
        $projectRoot = Join-Path $TestDrive 'GameLife'

        Get-GameLifePublishedExecutablePath -ProjectRoot $projectRoot |
            Should Be (Join-Path $projectRoot 'artifacts\GameLife\GameLife.exe')
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

    It 'instala o atalho no caminho explicito sem acessar a Area de Trabalho real' {
        $executavel = Join-Path $TestDrive 'GameLife instalado\GameLife.exe'
        New-Item -ItemType Directory -Path (Split-Path $executavel -Parent) -Force | Out-Null
        Set-Content -LiteralPath $executavel -Value ''
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
        $caminhoAtalho = Join-Path $TestDrive 'Desktop de teste\GameLife.lnk'

        Install-GameLifeShortcut `
            -InstalledExecutablePath $executavel `
            -ShortcutPath $caminhoAtalho `
            -Shell $shell

        $atalho.TargetPath | Should Be $executavel
        $atalho.Salvo | Should Be $true
    }

    It 'remove o arquivo antigo antes de criar um novo atalho' {
        $executavel = Join-Path $TestDrive 'GameLife.exe'
        $caminhoAtalho = Join-Path $TestDrive 'GameLife.lnk'
        Set-Content -LiteralPath $executavel -Value ''
        Set-Content -LiteralPath $caminhoAtalho -Value 'atalho antigo'
        $atalho = [pscustomobject]@{
            TargetPath = $null
            Arguments = $null
            WorkingDirectory = $null
            IconLocation = $null
            Description = $null
        }
        $atalho | Add-Member -MemberType ScriptMethod -Name Save -Value { }
        $shell = [pscustomobject]@{ Atalho = $atalho }
        $shell | Add-Member -MemberType ScriptMethod -Name CreateShortcut -Value { param($caminho) $this.Atalho }

        New-GameLifeShortcut `
            -ExecutablePath $executavel `
            -ShortcutPath $caminhoAtalho `
            -Shell $shell

        $caminhoAtalho | Should Not Exist
        $atalho.TargetPath | Should Be $executavel
    }
}
