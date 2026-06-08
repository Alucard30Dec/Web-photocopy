[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ConnectionString,

    [Parameter(Mandatory = $false)]
    [switch]$AllowDirectSupabaseHost
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectRoot = Join-Path $PSScriptRoot "Project"
$WebProjectPath = Join-Path $ProjectRoot "WebPhotocopyHub_Web\WebPhotocopyHub_Web.csproj"

if (-not (Test-Path -LiteralPath $WebProjectPath)) {
    throw "Không tìm thấy Web project: $WebProjectPath"
}

function Convert-SecureStringToPlainText {
    param([Parameter(Mandatory = $true)][System.Security.SecureString]$SecureValue)

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

function Get-BuilderValue {
    param(
        [Parameter(Mandatory = $true)][System.Data.Common.DbConnectionStringBuilder]$Builder,
        [Parameter(Mandatory = $true)][string[]]$Keys
    )

    foreach ($key in $Builder.Keys) {
        foreach ($candidateKey in $Keys) {
            if ([string]::Equals($key.ToString(), $candidateKey, [System.StringComparison]::OrdinalIgnoreCase)) {
                $value = $Builder[$key]
                if ($null -ne $value) {
                    return $value.ToString()
                }
            }
        }
    }

    return ""
}

function Get-ConnectionInfo {
    param([Parameter(Mandatory = $true)][string]$Value)

    $uri = $null
    $isUri = [System.Uri]::TryCreate($Value, [System.UriKind]::Absolute, [ref]$uri)

    if ($isUri -and ($uri.Scheme.Equals("postgres", [System.StringComparison]::OrdinalIgnoreCase) -or $uri.Scheme.Equals("postgresql", [System.StringComparison]::OrdinalIgnoreCase))) {
        $userInfoParts = $uri.UserInfo.Split(":", 2)
        $username = ""
        $password = ""

        if ($userInfoParts.Length -gt 0) {
            $username = [System.Uri]::UnescapeDataString($userInfoParts[0])
        }

        if ($userInfoParts.Length -gt 1) {
            $password = [System.Uri]::UnescapeDataString($userInfoParts[1])
        }

        return [pscustomobject]@{
            Host = $uri.IdnHost
            Port = $uri.Port
            Database = [System.Uri]::UnescapeDataString($uri.AbsolutePath.Trim("/"))
            Username = $username
            Password = $password
            Format = "Uri"
        }
    }

    $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
    $builder.ConnectionString = $Value

    return [pscustomobject]@{
        Host = Get-BuilderValue -Builder $builder -Keys @("Host", "Server")
        Port = Get-BuilderValue -Builder $builder -Keys @("Port")
        Database = Get-BuilderValue -Builder $builder -Keys @("Database")
        Username = Get-BuilderValue -Builder $builder -Keys @("Username", "User ID", "User Id", "UserID", "User")
        Password = Get-BuilderValue -Builder $builder -Keys @("Password", "Pwd")
        Format = "KeyValue"
    }
}

function Test-SupabaseDirectHost {
    param([Parameter(Mandatory = $true)][string]$HostName)

    return $HostName.StartsWith("db.", [System.StringComparison]::OrdinalIgnoreCase) -and $HostName.EndsWith(".supabase.co", [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-SupabasePoolerHost {
    param([Parameter(Mandatory = $true)][string]$HostName)

    return $HostName.IndexOf("pooler.supabase.com", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
}

function Assert-ValidSupabaseConnectionString {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Connection string không được rỗng."
    }

    if ($Value.IndexOf("[YOUR-PASSWORD]", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or $Value.IndexOf("<password>", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Connection string vẫn còn placeholder password. Hãy thay bằng database password thật."
    }

    $info = Get-ConnectionInfo -Value $Value

    if ([string]::IsNullOrWhiteSpace($info.Host)) {
        throw "Connection string thiếu Host."
    }

    $isSupabaseHost = $info.Host.IndexOf("supabase.co", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or $info.Host.IndexOf("pooler.supabase.com", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    if (-not $isSupabaseHost) {
        throw "Host '$($info.Host)' không phải Supabase host."
    }

    if ((Test-SupabaseDirectHost -HostName $info.Host) -and -not $AllowDirectSupabaseHost) {
        throw "Bạn đang dùng Direct host '$($info.Host)'. Với lỗi DNS/IPv4 hiện tại, hãy dùng Session Pooler host dạng aws-<region>.pooler.supabase.com, port 5432. Nếu bạn chắc chắn có IPv6 hoặc IPv4 add-on, chạy lại với -AllowDirectSupabaseHost."
    }

    if ((Test-SupabasePoolerHost -HostName $info.Host) -and $info.Format -eq "Uri" -and $info.Port -ne 5432) {
        throw "Bạn đang dùng Supabase pooler nhưng port không phải 5432. Với ASP.NET Core MVC/EF Core local, hãy dùng Session Pooler port 5432, không dùng Transaction Pooler port 6543."
    }

    if ([string]::IsNullOrWhiteSpace($info.Database)) {
        throw "Connection string thiếu database. Với Supabase hosted, database phải là postgres."
    }

    if (-not [string]::Equals($info.Database, "postgres", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Supabase hosted phải dùng database postgres. Hiện tại đang là '$($info.Database)'. Không dùng DTBWebPhotocopyHub làm database name."
    }

    if ([string]::IsNullOrWhiteSpace($info.Username)) {
        throw "Connection string thiếu Username."
    }

    if ([string]::IsNullOrWhiteSpace($info.Password)) {
        throw "Connection string thiếu Password."
    }
}

function Get-RedactedConnectionString {
    param([Parameter(Mandatory = $true)][string]$Value)

    $uri = $null
    $isUri = [System.Uri]::TryCreate($Value, [System.UriKind]::Absolute, [ref]$uri)

    if ($isUri -and ($uri.Scheme.Equals("postgres", [System.StringComparison]::OrdinalIgnoreCase) -or $uri.Scheme.Equals("postgresql", [System.StringComparison]::OrdinalIgnoreCase))) {
        return ($Value -replace "(?i)(postgres(?:ql)?://[^:/@]+:)[^@]*@", '$1***@')
    }

    $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
    $builder.ConnectionString = $Value

    foreach ($key in @($builder.Keys)) {
        if ([string]::Equals($key.ToString(), "Password", [System.StringComparison]::OrdinalIgnoreCase) -or [string]::Equals($key.ToString(), "Pwd", [System.StringComparison]::OrdinalIgnoreCase)) {
            $builder[$key] = "***"
        }
    }

    return $builder.ConnectionString
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $secure = Read-Host -Prompt "Paste Supabase Session Pooler connection string" -AsSecureString
    $ConnectionString = Convert-SecureStringToPlainText -SecureValue $secure
}

$ConnectionString = $ConnectionString.Trim()
Assert-ValidSupabaseConnectionString -Value $ConnectionString

[System.Environment]::SetEnvironmentVariable("PHOTOCOPYHUB_POSTGRES_CONNECTION", $ConnectionString, [System.EnvironmentVariableTarget]::User)
$env:PHOTOCOPYHUB_POSTGRES_CONNECTION = $ConnectionString

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "$ConnectionString" --project "$WebProjectPath"

Write-Host ""
Write-Host "Đã lưu connection string vào User Secrets và user environment variable." -ForegroundColor Green
Write-Host ("Giá trị đã che password: " + (Get-RedactedConnectionString -Value $ConnectionString))
Write-Host "Hãy đóng Visual Studio rồi mở lại để Visual Studio nhận env var mới." -ForegroundColor Yellow
