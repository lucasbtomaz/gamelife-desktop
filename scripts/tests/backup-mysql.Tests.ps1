Describe 'Backup MySQL do GameLife' {
    BeforeAll {
        $script:scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'backup-mysql.ps1'
        . $script:scriptPath
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

    It 'preserva credenciais UTF-8 sem BOM no arquivo temporário' {
        $output = Join-Path $TestDrive 'unicode.sql'
        Mock Invoke-MySqlDump {
            $bytes = [System.IO.File]::ReadAllBytes($DefaultsFile)
            if ($bytes[0] -eq 239) { throw 'O arquivo contém BOM.' }
            if ([System.IO.File]::ReadAllText($DefaultsFile) -notmatch 'usuário') { throw 'O arquivo perdeu caracteres UTF-8.' }
            Set-Content -LiteralPath $OutputPath -Value '-- GameLife backup'
            return 0
        }

        Invoke-GameLifeBackup -OutputPath $output -MySqlDumpPath 'mysqldump.exe' -ConnectionString 'Server=local;Database=GameLifeDb;User=usuário;Password=senhã' | Out-Null

        Assert-MockCalled Invoke-MySqlDump -Times 1
    }
}
