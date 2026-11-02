param(
    [string[]]$RepositoryPath,
    [switch]$LibraryOnly
)

$ErrorActionPreference = 'Stop'

function Assert-GameLifeSourceState {
    param([Parameter(Mandatory)][string]$RepositoryPath)

    if (-not (Test-Path -LiteralPath $RepositoryPath -PathType Container)) {
        throw "Repositório de origem não encontrado: '$RepositoryPath'."
    }

    $status = @(& git -C $RepositoryPath status --porcelain --untracked-files=normal 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "Não foi possível verificar o repositório de origem '$RepositoryPath': $($status -join [Environment]::NewLine)"
    }

    if ($status.Count -gt 0) {
        throw "O repositório de origem '$RepositoryPath' possui alterações locais. Preserve-as antes da consolidação."
    }
}

if (-not $LibraryOnly) {
    if (-not $RepositoryPath) {
        throw 'Informe ao menos um RepositoryPath para verificar.'
    }

    foreach ($path in $RepositoryPath) {
        Assert-GameLifeSourceState -RepositoryPath $path
    }
}
