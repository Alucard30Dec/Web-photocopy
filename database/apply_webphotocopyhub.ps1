param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$UserName = "postgres",
    [string]$SourceDatabase = "",
    [switch]$MigrateExistingAppSchema
)

$ErrorActionPreference = "Stop"

$databaseName = "WebPhotocopyHub"
$databaseDir = $PSScriptRoot
$schemaScript = Join-Path $databaseDir "patches\V20260704_001_tks_canonical_webphotocopyhub.sql"
$migrationScript = Join-Path $databaseDir "patches\V20260704_002_migrate_app_schema_to_tks_canonical.sql"

function Resolve-PsqlPath {
    $candidates = @(
        "C:\Program Files\PostgreSQL\18\bin\psql.exe",
        "C:\Program Files\PostgreSQL\18\pgAdmin 4\runtime\psql.exe",
        "psql.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }

        $command = Get-Command $candidate -ErrorAction SilentlyContinue
        if ($command) {
            return $command.Source
        }
    }

    throw "psql.exe was not found. Install PostgreSQL client tools or update Resolve-PsqlPath."
}

function ConvertTo-SqlLiteral {
    param([string]$Value)
    return "'" + $Value.Replace("'", "''") + "'"
}

function ConvertTo-SqlIdentifier {
    param([string]$Value)
    return '"' + $Value.Replace('"', '""') + '"'
}

function Invoke-PsqlScalar {
    param(
        [string]$Database,
        [string]$Sql
    )

    $output = & $psql @commonArgs -d $Database -tAc $Sql
    if ($LASTEXITCODE -ne 0) {
        throw "psql scalar command failed with exit code $LASTEXITCODE"
    }

    if ($null -eq $output) {
        return ""
    }

    return ($output -join "`n").Trim()
}

function Invoke-PsqlNonQuery {
    param(
        [string]$Database,
        [string]$Sql
    )

    $Sql | & $psql @commonArgs -d $Database
    if ($LASTEXITCODE -ne 0) {
        throw "psql command failed with exit code $LASTEXITCODE"
    }
}

if (-not (Test-Path $schemaScript)) {
    throw "Missing schema script: $schemaScript"
}

if ($MigrateExistingAppSchema -and -not (Test-Path $migrationScript)) {
    throw "Missing migration script: $migrationScript"
}

$psql = Resolve-PsqlPath
$setPasswordForProcess = [string]::IsNullOrWhiteSpace($env:PGPASSWORD)

if ($setPasswordForProcess) {
    $securePassword = Read-Host -Prompt "PostgreSQL password for $UserName" -AsSecureString
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $env:PGPASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }
}

try {
    $commonArgs = @(
        "-h", $HostName,
        "-p", $Port.ToString(),
        "-U", $UserName,
        "-v", "ON_ERROR_STOP=1",
        "-X"
    )

    $databaseLiteral = ConvertTo-SqlLiteral $databaseName
    $databaseIdentifier = ConvertTo-SqlIdentifier $databaseName
    $exists = Invoke-PsqlScalar -Database "postgres" -Sql "SELECT 1 FROM pg_database WHERE datname = $databaseLiteral;"
    if ($exists -ne "1") {
        if ([string]::IsNullOrWhiteSpace($SourceDatabase)) {
            Invoke-PsqlNonQuery -Database "postgres" -Sql "CREATE DATABASE $databaseIdentifier;"
        }
        else {
            $sourceLiteral = ConvertTo-SqlLiteral $SourceDatabase
            $sourceIdentifier = ConvertTo-SqlIdentifier $SourceDatabase
            $sourceExists = Invoke-PsqlScalar -Database "postgres" -Sql "SELECT 1 FROM pg_database WHERE datname = $sourceLiteral;"
            if ($sourceExists -ne "1") {
                throw "Source database was not found: $SourceDatabase"
            }

            Invoke-PsqlNonQuery -Database "postgres" -Sql "CREATE DATABASE $databaseIdentifier WITH TEMPLATE $sourceIdentifier;"
        }
    }
    else {
        Write-Host "Database ""$databaseName"" already exists."
    }

    & $psql @commonArgs -d $databaseName -f $schemaScript
    if ($LASTEXITCODE -ne 0) {
        throw "Schema script failed with exit code $LASTEXITCODE"
    }

    if ($MigrateExistingAppSchema) {
        & $psql @commonArgs -d $databaseName -f $migrationScript
        if ($LASTEXITCODE -ne 0) {
            throw "Migration script failed with exit code $LASTEXITCODE"
        }
    }

    Write-Host "Done. Refresh pgAdmin Databases to see ""$databaseName""."
}
finally {
    if ($setPasswordForProcess) {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    }
}
