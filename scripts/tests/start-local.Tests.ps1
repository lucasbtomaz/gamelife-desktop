$scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'start-local.ps1'

Describe 'Inicializador local do GameLife' {
    BeforeAll {
        . $scriptPath
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
}
