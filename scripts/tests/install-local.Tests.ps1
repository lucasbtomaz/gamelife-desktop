Describe 'Instalação local do GameLife' {
    BeforeAll {
        $script:scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'install-local.ps1'
        . $script:scriptPath -LibraryOnly
    }

    BeforeEach {
        $script:testBase = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) '.superpowers\install-tests'
        $script:testRoot = Join-Path $script:testBase ([Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:testRoot -Force | Out-Null
    }

    AfterEach {
        if ($script:testRoot -and
            [IO.Path]::GetFullPath($script:testRoot).StartsWith([IO.Path]::GetFullPath($script:testBase), [StringComparison]::OrdinalIgnoreCase) -and
            (Test-Path -LiteralPath $script:testRoot)) {
            Remove-Item -LiteralPath $script:testRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    function New-TestPublication {
        param([string]$Path)

        New-Item -ItemType Directory -Path (Join-Path $Path 'wwwroot') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $Path 'GameLife.exe') -Value 'executável novo'
        Set-Content -LiteralPath (Join-Path $Path 'wwwroot\index.html') -Value '<div id="root"></div>'
    }

    It 'resolve a instalação dentro de LocalApplicationData' {
        $localData = Join-Path $script:testRoot 'LocalAppData'

        Get-GameLifeInstallRoot -LocalApplicationData $localData |
            Should Be (Join-Path $localData 'GameLife\App')
    }

    It 'instala o executável e os recursos web' {
        $source = Join-Path $script:testRoot 'source'
        $destination = Join-Path $script:testRoot 'GameLife\App'
        New-TestPublication -Path $source

        Install-GameLifeFiles -Source $source -Destination $destination

        (Join-Path $destination 'GameLife.exe') | Should Exist
        (Join-Path $destination 'wwwroot\index.html') | Should Exist
    }

    It 'preserva a configuração local fora da pasta substituída' {
        $source = Join-Path $script:testRoot 'source'
        $gameLifeRoot = Join-Path $script:testRoot 'GameLife'
        $destination = Join-Path $gameLifeRoot 'App'
        $configuration = Join-Path $gameLifeRoot 'appsettings.Local.json'
        New-TestPublication -Path $source
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $destination 'arquivo-antigo.txt') -Value 'antigo'
        Set-Content -LiteralPath $configuration -Value '{"segredo":"preservado"}'

        Install-GameLifeFiles -Source $source -Destination $destination

        Get-Content -Raw $configuration | Should Match 'preservado'
        (Join-Path $destination 'arquivo-antigo.txt') | Should Not Exist
    }

    It 'preserva a copia da configuracao ao lado do executavel' {
        $source = Join-Path $script:testRoot 'source'
        $destination = Join-Path $script:testRoot 'GameLife\App'
        New-TestPublication -Path $source
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $destination 'GameLife.exe') -Value 'executável atual'
        Set-Content -LiteralPath (Join-Path $destination 'appsettings.Local.json') `
            -Value '{"ConnectionStrings":{"GameLife":"conexao-preservada"}}'

        Install-GameLifeFiles -Source $source -Destination $destination

        Get-Content -Raw (Join-Path $destination 'appsettings.Local.json') |
            Should Match 'conexao-preservada'
    }

    It 'rejeita uma publicação sem executável e mantém a instalação atual' {
        $source = Join-Path $script:testRoot 'source-invalida'
        $destination = Join-Path $script:testRoot 'GameLife\App'
        New-Item -ItemType Directory -Path (Join-Path $source 'wwwroot') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $source 'wwwroot\index.html') -Value 'interface'
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $destination 'GameLife.exe') -Value 'executável atual'

        $exception = $null
        try {
            Install-GameLifeFiles -Source $source -Destination $destination
        }
        catch {
            $exception = $_
        }

        $exception | Should Not BeNullOrEmpty
        Get-Content -Raw (Join-Path $destination 'GameLife.exe') | Should Match 'atual'
    }

    It 'mantém a instalação atual quando o executável está bloqueado' {
        $source = Join-Path $script:testRoot 'source-nova'
        $destination = Join-Path $script:testRoot 'GameLife\App'
        New-TestPublication -Path $source
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        $installedExecutable = Join-Path $destination 'GameLife.exe'
        Set-Content -LiteralPath $installedExecutable -Value 'executável atual'
        $lock = [IO.File]::Open($installedExecutable, 'Open', 'Read', 'None')

        try {
            $exception = $null
            try {
                Install-GameLifeFiles -Source $source -Destination $destination
            }
            catch {
                $exception = $_
            }
        }
        finally {
            $lock.Dispose()
        }

        $exception | Should Not BeNullOrEmpty
        Get-Content -Raw $installedExecutable | Should Match 'atual'
    }
}
