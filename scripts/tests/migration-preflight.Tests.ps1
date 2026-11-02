$scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'migration-preflight.ps1'

Describe 'Verificação das origens da consolidação' {
    BeforeAll {
        . $scriptPath -LibraryOnly
    }

    BeforeEach {
        $script:testRoot = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) '.superpowers\test-repositories'
        $script:testRepositoryPath = Join-Path $script:testRoot ([Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:testRepositoryPath -Force | Out-Null
        git -C $script:testRepositoryPath init --quiet
        if ($LASTEXITCODE -ne 0) {
            throw 'Não foi possível criar o repositório temporário do teste.'
        }
    }

    AfterEach {
        if ($script:testRepositoryPath -and
            [IO.Path]::GetFullPath($script:testRepositoryPath).StartsWith([IO.Path]::GetFullPath($script:testRoot), [StringComparison]::OrdinalIgnoreCase) -and
            (Test-Path -LiteralPath $script:testRepositoryPath)) {
            Remove-Item -LiteralPath $script:testRepositoryPath -Recurse -Force
        }
    }

    It 'aceita uma origem sem alterações locais' {
        { Assert-GameLifeSourceState -RepositoryPath $script:testRepositoryPath } |
            Should Not Throw
    }

    It 'rejeita uma origem com arquivo não rastreado e informa o caminho' {
        Set-Content -LiteralPath (Join-Path $script:testRepositoryPath 'local.txt') -Value 'não versionado'

        $exception = $null
        try {
            Assert-GameLifeSourceState -RepositoryPath $script:testRepositoryPath
        }
        catch {
            $exception = $_
        }

        $exception | Should Not BeNullOrEmpty
        $exception.Exception.Message | Should Match ([regex]::Escape($script:testRepositoryPath))
    }
}
