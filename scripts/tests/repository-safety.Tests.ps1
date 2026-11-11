Describe 'Segurança do repositório público' {
    BeforeAll {
        $script:repositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
        $script:trackedFiles = @(& git -C $script:repositoryRoot ls-files)
        if ($LASTEXITCODE -ne 0) {
            throw 'Não foi possível listar os arquivos rastreados.'
        }
    }

    It 'não rastreia configuração local, dados pessoais ou artefatos gerados' {
        $forbidden = @($script:trackedFiles | Where-Object {
            $_ -match '(^|/)(appsettings\.Local\.json|Backups?|artifacts?|dist|build|bin|obj|logs?)(/|$)' -or
            $_ -match '\.(xlsx?|sql|db|log)$' -or
            ($_ -match '(^|/)\.env($|\.)' -and $_ -notmatch '\.env\.example$')
        })

        ($forbidden -join "`n") | Should BeNullOrEmpty
    }

    It 'não contém caminhos absolutos de perfis ou pastas pessoais' {
        $pattern = '[A-Za-z]:[\\/](Users|Projetos|Downloads)[\\/]'
        $matches = @(& git -C $repositoryRoot grep -n -I -E $pattern -- . 2>$null)
        if ($LASTEXITCODE -notin @(0, 1)) {
            throw 'Não foi possível examinar o conteúdo rastreado.'
        }

        ($matches -join "`n") | Should BeNullOrEmpty
    }

    It 'executa as verificações PowerShell no CI' {
        $workflowPath = Join-Path $repositoryRoot '.github\workflows\ci.yml'
        $workflow = Get-Content -Raw $workflowPath

        $workflow | Should Match 'Invoke-Pester'
    }
}
