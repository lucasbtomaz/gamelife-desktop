param(
    [string]$OutputPath = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) ("GameLife\Backups\gamelife-{0}.sql" -f (Get-Date -Format 'yyyyMMdd-HHmmss')))
)

$ErrorActionPreference = 'Stop'

function Resolve-MySqlDumpPath {
    param([string[]]$Candidates = @(
        'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe',
        'C:\Program Files\MySQL\MySQL Workbench 8.0\mysqldump.exe'
    ))

    if (-not $PSBoundParameters.ContainsKey('Candidates')) {
        $command = Get-Command mysqldump.exe -ErrorAction SilentlyContinue
        if ($command) {
            return $command.Source
        }
    }

    foreach ($candidate in $Candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw 'mysqldump não foi encontrado. Instale o MySQL Server ou o MySQL Workbench.'
}

function Get-ConnectionValue {
    param(
        [System.Data.Common.DbConnectionStringBuilder]$Builder,
        [string[]]$Names
    )

    foreach ($name in $Names) {
        if ($Builder.ContainsKey($name)) {
            return [string]$Builder[$name]
        }
    }

    return $null
}

function ConvertTo-MySqlOptionValue {
    param([string]$Value)
    return '"' + $Value.Replace('\', '\\').Replace('"', '\"') + '"'
}

function Invoke-MySqlDump {
    param(
        [string]$MySqlDumpPath,
        [string]$DefaultsFile,
        [string]$DatabaseName,
        [string]$OutputPath
    )

    & $MySqlDumpPath `
        "--defaults-extra-file=$DefaultsFile" `
        '--single-transaction' `
        '--routines' `
        '--events' `
        "--result-file=$OutputPath" `
        $DatabaseName
    return $LASTEXITCODE
}

function Invoke-GameLifeBackup {
    param(
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][string]$MySqlDumpPath,
        [Parameter(Mandatory)][string]$ConnectionString
    )

    $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
    $builder.set_ConnectionString($ConnectionString)
    $serverName = Get-ConnectionValue $builder @('Server', 'Host', 'Data Source')
    $databaseName = Get-ConnectionValue $builder @('Database', 'Initial Catalog')
    $userName = Get-ConnectionValue $builder @('User', 'User Id', 'Uid', 'Username')
    $password = Get-ConnectionValue $builder @('Password', 'Pwd')
    $port = Get-ConnectionValue $builder @('Port')

    if ([string]::IsNullOrWhiteSpace($serverName) -or
        [string]::IsNullOrWhiteSpace($databaseName) -or
        [string]::IsNullOrWhiteSpace($userName)) {
        throw 'A conexão MySQL não contém servidor, banco e usuário válidos.'
    }

    $outputDirectory = Split-Path $OutputPath -Parent
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    $defaultsFile = Join-Path ([System.IO.Path]::GetTempPath()) ("gamelife-mysql-{0}.cnf" -f [Guid]::NewGuid())

    try {
        $lines = @(
            '[client]',
            "host=$(ConvertTo-MySqlOptionValue $serverName)",
            "user=$(ConvertTo-MySqlOptionValue $userName)",
            "password=$(ConvertTo-MySqlOptionValue $password)"
        )
        if (-not [string]::IsNullOrWhiteSpace($port)) {
            $lines += "port=$port"
        }
        [System.IO.File]::WriteAllLines(
            $defaultsFile,
            $lines,
            [System.Text.UTF8Encoding]::new($false))

        $exitCode = Invoke-MySqlDump `
            -MySqlDumpPath $MySqlDumpPath `
            -DefaultsFile $defaultsFile `
            -DatabaseName $databaseName `
            -OutputPath $OutputPath

        if ($exitCode -ne 0 -or -not (Test-Path -LiteralPath $OutputPath) -or (Get-Item $OutputPath).Length -eq 0) {
            Remove-Item -LiteralPath $OutputPath -Force -ErrorAction SilentlyContinue
            throw "O backup do MySQL falhou com código $exitCode."
        }

        return $OutputPath
    }
    finally {
        Remove-Item -LiteralPath $defaultsFile -Force -ErrorAction SilentlyContinue
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    . (Join-Path $PSScriptRoot 'start-local.ps1')
    $connectionString = Get-GameLifeConnectionString
    $mySqlDumpPath = Resolve-MySqlDumpPath
    $createdPath = Invoke-GameLifeBackup -OutputPath $OutputPath -MySqlDumpPath $mySqlDumpPath -ConnectionString $connectionString
    Write-Host "Backup criado em '$createdPath'."
}
