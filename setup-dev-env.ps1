[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
param(
    [Parameter(Mandatory = $false)]
    [string]$ConnectionString
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$EnvVarName = 'PHOTOCOPYHUB_POSTGRES_CONNECTION'

function Read-PlainTextSecureString {
    param(
        [Parameter(Mandatory = $true)]
        [System.Security.SecureString]$SecureValue
    )

    $bstr = [System.IntPtr]::Zero
    try {
        $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
        return [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        if ($bstr -ne [System.IntPtr]::Zero) {
            [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }
}

function Get-ConnectionStringValue {
    param(
        [Parameter(Mandatory = $true)]
        [System.Data.Common.DbConnectionStringBuilder]$Builder,

        [Parameter(Mandatory = $true)]
        [string[]]$Keys
    )

    foreach ($key in $Builder.Keys) {
        foreach ($candidateKey in $Keys) {
            if ([string]::Equals($key.ToString(), $candidateKey, [System.StringComparison]::OrdinalIgnoreCase)) {
                $rawValue = $Builder[$key]
                if ($null -ne $rawValue) {
                    return $rawValue.ToString()
                }
            }
        }
    }

    return $null
}

function Test-SupabaseHost {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostName
    )

    return $HostName.IndexOf('supabase.co', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 `
        -or $HostName.IndexOf('pooler.supabase.com', [System.StringComparison]::OrdinalIgnoreCase) -ge 0
}

function Get-PostgreSqlConnectionInfo {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $uri = $null
    if ([System.Uri]::TryCreate($Value, [System.UriKind]::Absolute, [ref]$uri) `
            -and ($uri.Scheme.Equals('postgres', [System.StringComparison]::OrdinalIgnoreCase) `
                -or $uri.Scheme.Equals('postgresql', [System.StringComparison]::OrdinalIgnoreCase))) {
        $userInfoParts = $uri.UserInfo.Split(':', 2)
        $username = if ($userInfoParts.Length -gt 0) { [System.Uri]::UnescapeDataString($userInfoParts[0]) } else { '' }
        $password = if ($userInfoParts.Length -gt 1) { [System.Uri]::UnescapeDataString($userInfoParts[1]) } else { '' }

        return [pscustomobject]@{
            Format = 'Uri'
            Host = $uri.IdnHost
            Database = [System.Uri]::UnescapeDataString($uri.AbsolutePath.Trim('/'))
            Username = $username
            Password = $password
        }
    }

    $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
    try {
        $builder.ConnectionString = $Value
    }
    catch {
        throw "Connection string khong parse duoc. Hay copy lai tu Supabase Connect."
    }

    return [pscustomobject]@{
        Format = 'KeyValue'
        Host = Get-ConnectionStringValue -Builder $builder -Keys @('Host', 'Server')
        Database = Get-ConnectionStringValue -Builder $builder -Keys @('Database')
        Username = Get-ConnectionStringValue -Builder $builder -Keys @('Username', 'User ID', 'User Id', 'UserID', 'User')
        Password = Get-ConnectionStringValue -Builder $builder -Keys @('Password', 'Pwd')
    }
}

function Assert-ValidSupabaseConnectionString {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw 'Connection string khong duoc rong.'
    }

    if ($Value.IndexOf('[YOUR-PASSWORD]', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 `
            -or $Value.IndexOf('<password>', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 `
            -or $Value.IndexOf('<SUPABASE_CONNECTION_STRING>', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw 'Connection string van con placeholder. Hay thay bang database password that.'
    }

    $info = Get-PostgreSqlConnectionInfo -Value $Value

    if ([string]::IsNullOrWhiteSpace($info.Host)) {
        throw 'Connection string thieu Host.'
    }

    if (-not (Test-SupabaseHost -HostName $info.Host)) {
        throw "Host '$($info.Host)' khong phai Supabase hoac Supabase pooler."
    }

    if ([string]::IsNullOrWhiteSpace($info.Database)) {
        throw 'Connection string thieu Database=postgres.'
    }

    if (-not [string]::Equals($info.Database, 'postgres', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Supabase hosted phai dung Database=postgres, hien tai la '$($info.Database)'."
    }

    if ([string]::IsNullOrWhiteSpace($info.Username)) {
        throw 'Connection string thieu Username.'
    }

    if ([string]::IsNullOrWhiteSpace($info.Password)) {
        throw 'Connection string thieu Password.'
    }
}

function Get-RedactedConnectionString {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $uri = $null
    if ([System.Uri]::TryCreate($Value, [System.UriKind]::Absolute, [ref]$uri) `
            -and ($uri.Scheme.Equals('postgres', [System.StringComparison]::OrdinalIgnoreCase) `
                -or $uri.Scheme.Equals('postgresql', [System.StringComparison]::OrdinalIgnoreCase))) {
        return ($Value -replace '(?i)(postgres(?:ql)?://[^:/@]+:)[^@]*@', '$1***@')
    }

    $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
    $builder.ConnectionString = $Value

    foreach ($key in @($builder.Keys)) {
        if ([string]::Equals($key.ToString(), 'Password', [System.StringComparison]::OrdinalIgnoreCase) `
                -or [string]::Equals($key.ToString(), 'Pwd', [System.StringComparison]::OrdinalIgnoreCase)) {
            $builder[$key] = '***'
        }
    }

    return $builder.ConnectionString
}

$hasConnectionStringParameter = $PSBoundParameters.ContainsKey('ConnectionString')

if (-not $hasConnectionStringParameter) {
    $secureConnectionString = Read-Host -Prompt 'Paste Supabase PostgreSQL connection string' -AsSecureString
    $ConnectionString = Read-PlainTextSecureString -SecureValue $secureConnectionString
}

$ConnectionString = if ($null -eq $ConnectionString) { '' } else { $ConnectionString.Trim() }
Assert-ValidSupabaseConnectionString -Value $ConnectionString

if ($PSCmdlet.ShouldProcess($EnvVarName, 'Set user environment variable')) {
    [System.Environment]::SetEnvironmentVariable($EnvVarName, $ConnectionString, [System.EnvironmentVariableTarget]::User)
    $env:PHOTOCOPYHUB_POSTGRES_CONNECTION = $ConnectionString
    Write-Host "Saved user environment variable: $EnvVarName"
    Write-Host 'Current PowerShell process was also updated.'
}
else {
    Write-Host 'Validation passed. -WhatIf skipped saving the environment variable.'
}

Write-Host ('Value: ' + (Get-RedactedConnectionString -Value $ConnectionString))
Write-Host 'Open a new terminal or restart Visual Studio before running the app from another process.'
