$scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'backup-mysql.ps1'

Describe 'Backup MySQL do GameLife' {
    BeforeAll {
        . $scriptPath
    }

    It 'falha quando o mysqldump não está instalado' {
        $exception = $null
        try {
            Resolve-MySqlDumpPath -Candidates @((Join-Path $TestDrive 'inexistente.exe'))
        }
        catch {
            $exception = $_
        }

        $exception | Should Not BeNullOrEmpty
    }

    It 'não mantém arquivo incompleto quando o mysqldump falha' {
        $output = Join-Path $TestDrive 'falha.sql'
        Mock Invoke-MySqlDump { 2 }
        $exception = $null

        try {
            Invoke-GameLifeBackup -OutputPath $output -MySqlDumpPath 'mysqldump.exe' -ConnectionString 'Server=local;Database=GameLifeDb;User=test;Password=test'
        }
        catch {
            $exception = $_
        }

        $exception | Should Not BeNullOrEmpty
        Test-Path -LiteralPath $output | Should Be $false
    }

    It 'retorna um arquivo SQL válido quando o mysqldump termina com sucesso' {
        $output = Join-Path $TestDrive 'sucesso.sql'
        Mock Invoke-MySqlDump {
            Set-Content -LiteralPath $OutputPath -Value '-- GameLife backup'
            return 0
        }

        $result = Invoke-GameLifeBackup -OutputPath $output -MySqlDumpPath 'mysqldump.exe' -ConnectionString 'Server=local;Database=GameLifeDb;User=test;Password=test'

        $result | Should Be $output
        (Get-Item -LiteralPath $output).Length | Should BeGreaterThan 0
    }
}
