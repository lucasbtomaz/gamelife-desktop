$scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'start-local.ps1'

Describe 'Inicializador local do GameLife' {
    BeforeAll {
        . $scriptPath
    }

    It 'rejeita resposta de outro aplicativo na mesma porta' {
        Mock Invoke-WebRequest { [pscustomobject]@{ StatusCode = 200; Content = '<html>outro app</html>' } }

        Test-GameLifeReady -HealthUrl 'http://127.0.0.1:5044' -TimeoutSeconds 0 | Should Be $false
    }

    It 'aceita somente a identidade esperada do GameLife' {
        Mock Invoke-WebRequest { [pscustomobject]@{ StatusCode = 200; Content = '{"application":"GameLife","status":"ready"}' } }

        Test-GameLifeReady -HealthUrl 'http://127.0.0.1:5044' -TimeoutSeconds 0 | Should Be $true
    }

    It 'falha com mensagem clara quando o executável não existe' {
        $inexistente = Join-Path $TestDrive 'GameLife.exe'
        $exception = $null

        try {
            Start-GameLife -ExecutablePath $inexistente -NoBrowser
        }
        catch {
            $exception = $_
        }

        $exception | Should Not BeNullOrEmpty
        $exception.Exception.Message | Should Match '^Executável do GameLife não encontrado'
    }

    It 'não inicia uma segunda instância quando a API já está pronta' {
        $executavel = Join-Path $TestDrive 'GameLife.exe'
        Set-Content -LiteralPath $executavel -Value ''
        Mock Test-GameLifeReady { $true }
        Mock Start-Process { throw 'Não deveria iniciar outro processo.' }

        Start-GameLife -ExecutablePath $executavel -NoBrowser

        Assert-MockCalled Start-Process -Times 0
    }

    It 'inicia oculto e aguarda a API quando ela ainda não está pronta' {
        $executavel = Join-Path $TestDrive 'GameLife.exe'
        Set-Content -LiteralPath $executavel -Value ''
        Mock Test-GameLifeReady { $false } -ParameterFilter { $TimeoutSeconds -eq 1 }
        Mock Test-GameLifeReady { $true } -ParameterFilter { $TimeoutSeconds -eq 30 }
        Mock Get-GameLifeConnectionString { 'Server=local;Database=teste;User=teste;Password=teste' }
        Mock Start-Process { [pscustomobject]@{ HasExited = $false } }

        Start-GameLife -ExecutablePath $executavel -NoBrowser

        Assert-MockCalled Start-Process -Times 1 -ParameterFilter { $WindowStyle -eq 'Hidden' }
    }

    It 'mostra uma mensagem quando a inicialização falha' {
        Mock Start-GameLife { throw 'Falha interna sensível.' }
        Mock Show-GameLifeError { }

        $exitCode = Invoke-GameLifeLauncher -ExecutablePath 'GameLife.exe' -NoBrowser

        $exitCode | Should Be 1
        Assert-MockCalled Show-GameLifeError -Times 1 -ParameterFilter {
            $Message -notmatch 'sensível' -and $Message -match 'não conseguiu iniciar'
        }
    }
}
