$repositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent

Describe 'Estrutura do monorepositório GameLife' {
    It 'mantém API, desktop, frontend e testes nos caminhos canônicos' {
        (Join-Path $repositoryRoot 'src\GameLife.Api\GameLife.Api.csproj') | Should Exist
        (Join-Path $repositoryRoot 'src\GameLife.Desktop\GameLife.Desktop.csproj') | Should Exist
        (Join-Path $repositoryRoot 'src\GameLife.Web\package.json') | Should Exist
        (Join-Path $repositoryRoot 'tests\GameLife.Api.Tests\GameLife.Api.Tests.csproj') | Should Exist
        (Join-Path $repositoryRoot 'GameLife.slnx') | Should Exist
    }

    It 'não mantém arquivos nas pastas antigas dos projetos' {
        @(Get-ChildItem -File -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $repositoryRoot 'gamelife-api')).Count | Should Be 0
        @(Get-ChildItem -File -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $repositoryRoot 'GameLife.Desktop')).Count | Should Be 0
        @(Get-ChildItem -File -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $repositoryRoot 'gamelife-api.Tests')).Count | Should Be 0
        (Join-Path $repositoryRoot 'gamelife-api.slnx') | Should Not Exist
    }

    It 'lista os três projetos dotnet pelos novos caminhos' {
        $solutionPath = Join-Path $repositoryRoot 'GameLife.slnx'
        $projects = @(& dotnet sln $solutionPath list 2>&1)

        $LASTEXITCODE | Should Be 0
        ($projects -join "`n") | Should Match 'src[\\/]GameLife.Api[\\/]GameLife.Api.csproj'
        ($projects -join "`n") | Should Match 'src[\\/]GameLife.Desktop[\\/]GameLife.Desktop.csproj'
        ($projects -join "`n") | Should Match 'tests[\\/]GameLife.Api.Tests[\\/]GameLife.Api.Tests.csproj'
    }
}
