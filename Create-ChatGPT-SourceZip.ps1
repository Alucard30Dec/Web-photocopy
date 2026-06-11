param(
    [string]$WebPhotocopyRoot = $PSScriptRoot,
    [string]$ProjectRoot = (Join-Path $PSScriptRoot 'Project'),
    [string]$ZipPath = (Join-Path $PSScriptRoot 'WebPhotocopy.zip'),
    [switch]$NoClipboard
)

$ErrorActionPreference = "Stop"

$WebPhotocopyRoot = [System.IO.Path]::GetFullPath($WebPhotocopyRoot)
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$ZipPath = [System.IO.Path]::GetFullPath($ZipPath)
$SolutionFileName = 'WebPhotocopyHub.sln'
$SolutionPath = Join-Path $ProjectRoot $SolutionFileName

if (-not (Test-Path -LiteralPath $ProjectRoot)) {
    throw "Khong tim thay thu muc Project: $ProjectRoot"
}

if (-not (Test-Path -LiteralPath $SolutionPath)) {
    throw "Khong tim thay WebPhotocopyHub.sln: $SolutionPath"
}

$TempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("WebPhotocopyHub_ChatGPT_Source_" + [System.Guid]::NewGuid().ToString("N"))
$TempProjectRoot = Join-Path $TempRoot 'Project'
New-Item -ItemType Directory -Path $TempProjectRoot -Force | Out-Null

$ExcludedDirectoryNames = @(
    '.git',
    '.vs',
    '.vscode-server',
    'bin',
    'obj',
    'node_modules',
    'packages',
    'TestResults',
    'coverage',
    'logs',
    'log',
    'runtime-data',
    'uploaded',
    'uploads',
    'files',
    'App_Data',
    '_chatgpt_backups',
    '_chatgpt_backups_WebPhotocopy'
)

$ExcludedRelativeDirectoryPrefixes = @(
    'WebPhotocopyHub.Web\wwwroot\uploads',
    'WebPhotocopyHub.Web\wwwroot\files',
    'WebPhotocopyHub.Web\App_Data',
    'WebPhotocopyHub.Web\logs',
    'WebPhotocopyHub.Web\runtime-data',
    'WebPhotocopyHub.Web\_ProjectSupport\RootFiles\logs',
    'WebPhotocopyHub.Web\_ProjectSupport\RootFiles\runtime-data'
)

$ExcludedFileNames = @(
    'appsettings.Development.json',
    'appsettings.Local.json',
    'appsettings.Production.json',
    'appsettings.Staging.json',
    'secrets.json',
    '.env',
    '.env.local',
    '.env.development',
    '.env.production',
    'service-account.json',
    'credentials.json',
    'account.md',
    'id_rsa',
    'id_rsa.pub',
    'publishsettings',
    'WebPhotocopy.zip',
    'Project.zip',
    'photocopyhub-dev.err.log',
    'photocopyhub-dev.out.log'
)

$ExcludedExtensions = @(
    '.zip',
    '.7z',
    '.rar',
    '.db',
    '.db-shm',
    '.db-wal',
    '.db-journal',
    '.sqlite',
    '.sqlite-shm',
    '.sqlite-wal',
    '.sqlite-journal',
    '.sqlite3',
    '.sqlite3-shm',
    '.sqlite3-wal',
    '.sqlite3-journal',
    '.bak',
    '.log',
    '.pfx',
    '.p12',
    '.pem',
    '.key',
    '.crt',
    '.cer',
    '.user',
    '.suo',
    '.wsuo',
    '.docstates',
    '.cache',
    '.tmp',
    '.nupkg',
    '.snupkg',
    '.pubxml',
    '.publishsettings',
    '.dll',
    '.exe',
    '.pdb'
)

$MaxSingleFileMb = 8
$MaxSingleFileBytes = $MaxSingleFileMb * 1MB

$RequiredRelativeFiles = New-Object System.Collections.Generic.List[string]
$SolutionProjectRelativeFiles = New-Object System.Collections.Generic.List[string]
$SolutionProjectDirectoryNames = New-Object System.Collections.Generic.List[string]

foreach ($RequiredRelativeFile in @(
    'WebPhotocopyHub.sln',
    'WebPhotocopyHub.slnLaunch',
    'nuget.config',
    'WebPhotocopyHub.Web\Program.cs',
    'WebPhotocopyHub.Web\appsettings.json',
    'WebPhotocopyHub.Web\Properties\launchSettings.json'
)) {
    if (-not $RequiredRelativeFiles.Contains($RequiredRelativeFile)) {
        $RequiredRelativeFiles.Add($RequiredRelativeFile) | Out-Null
    }
}

foreach ($SolutionLine in (Get-Content -LiteralPath $SolutionPath)) {
    if ($SolutionLine -match '^Project\("[^"]+"\)\s*=\s*"[^"]+",\s*"([^"]+\.(csproj|vbproj|fsproj))",') {
        $ProjectRelativeFile = $Matches[1].Replace('/', '\')

        if (-not $SolutionProjectRelativeFiles.Contains($ProjectRelativeFile)) {
            $SolutionProjectRelativeFiles.Add($ProjectRelativeFile) | Out-Null
        }

        if (-not $RequiredRelativeFiles.Contains($ProjectRelativeFile)) {
            $RequiredRelativeFiles.Add($ProjectRelativeFile) | Out-Null
        }

        $ProjectDirectoryName = ($ProjectRelativeFile -split '\\')[0]
        if (-not $SolutionProjectDirectoryNames.Contains($ProjectDirectoryName)) {
            $SolutionProjectDirectoryNames.Add($ProjectDirectoryName) | Out-Null
        }
    }
}

if ($SolutionProjectRelativeFiles.Count -eq 0) {
    throw "Khong doc duoc project .csproj nao tu solution: $SolutionPath"
}

function Get-RelativePathCompat {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$FullPath
    )

    $BaseFull = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/')
    $TargetFull = [System.IO.Path]::GetFullPath($FullPath)

    if (-not $TargetFull.StartsWith($BaseFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path khong nam trong base path. Base=$BaseFull Target=$TargetFull"
    }

    return $TargetFull.Substring($BaseFull.Length).TrimStart('\', '/').Replace('/', '\')
}

function Test-DirectorySegmentExcluded {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $Segments = $RelativePath -split '\\'
    foreach ($Segment in $Segments) {
        foreach ($ExcludedDir in $ExcludedDirectoryNames) {
            if ($Segment.Equals($ExcludedDir, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
        }
    }

    return $false
}

function Test-RelativePrefixExcluded {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    foreach ($ExcludedPrefix in $ExcludedRelativeDirectoryPrefixes) {
        $NormalizedPrefix = $ExcludedPrefix.Trim('\')
        if ($RelativePath.Equals($NormalizedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }

        if ($RelativePath.StartsWith($NormalizedPrefix + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Test-FileNameExcluded {
    param([Parameter(Mandatory = $true)][string]$FullPath)

    $FileName = [System.IO.Path]::GetFileName($FullPath)
    $Extension = [System.IO.Path]::GetExtension($FullPath)

    foreach ($ExcludedFile in $ExcludedFileNames) {
        if ($FileName.Equals($ExcludedFile, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    foreach ($ExcludedExtension in $ExcludedExtensions) {
        if ($Extension.Equals($ExcludedExtension, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    if ($FileName.StartsWith('.env', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    if ($FileName.EndsWith('.csproj.user', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    if ($FileName.EndsWith('.pubxml.user', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    if ($FileName.EndsWith('.sln.docstates', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    if ($FileName -match '(?i)\.bak($|[-.])') {
        return $true
    }

    if ($FileName -match '(?i)\.(db|sqlite|sqlite3)-(shm|wal|journal)$') {
        return $true
    }

    return $false
}

function Test-ConfigFileHasSecretValues {
    param([Parameter(Mandatory = $true)][System.IO.FileInfo]$File)

    $FileName = [System.IO.Path]::GetFileName($File.FullName)
    $Extension = [System.IO.Path]::GetExtension($File.FullName)
    $ConfigExtensions = @('.json', '.config', '.xml')

    if (($ConfigExtensions -notcontains $Extension.ToLowerInvariant()) `
            -and -not $FileName.Equals('nuget.config', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }

    $Raw = Get-Content -Raw -LiteralPath $File.FullName
    if ($Raw -match '(?i)(Password|Pwd|AccountKey|SharedAccessKey)\s*=') {
        return $true
    }

    if ($Raw -match '(?im)"[^"]*(secret|apikey|api_key|token|password|accesskey|connectionstring|defaultconnection)[^"]*"\s*:\s*"(?!\s*"\s*)[^"]*\S[^"]*"') {
        return $true
    }

    if ($Raw -match '(?im)(password|pwd|token|secret|apikey|api_key|accesskey|sharedaccesskey|cleartextpassword)\s*=\s*["''][^"'']*\S[^"'']*["'']') {
        return $true
    }

    if ($Raw -match '(?im)<add\s+key=["''][^"'']*(password|pwd|token|secret|apikey|api_key|accesskey|sharedaccesskey|cleartextpassword)[^"'']*["'']\s+value=["''][^"'']*\S[^"'']*["'']') {
        return $true
    }

    return $false
}

function Test-SensitiveConfigKey {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Key
    )

    return $Key -match '(?i)(password|pwd|secret|token|apikey|api_key|accesskey|sharedaccesskey|connectionstring|defaultconnection|accountnumber|accountname|serviceurl|bucketname)'
}

function Clear-SensitiveJsonValue {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $false)]
        [AllowEmptyString()]
        [string]$Key = ''
    )

    if ($null -eq $Value) {
        return $null
    }

    if (Test-SensitiveConfigKey -Key $Key) {
        if ($Value -is [bool]) {
            return $Value
        }

        if ($Value -is [int] -or $Value -is [long] -or $Value -is [double] -or $Value -is [decimal]) {
            return 0
        }

        return ''
    }

    if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string]) -and -not ($Value -is [pscustomobject])) {
        $SanitizedArray = @()
        foreach ($Item in $Value) {
            $SanitizedArray += Clear-SensitiveJsonValue -Value $Item -Key $Key
        }

        return $SanitizedArray
    }

    if ($Value -is [pscustomobject]) {
        $SanitizedObject = [ordered]@{}
        foreach ($Property in $Value.PSObject.Properties) {
            $SanitizedObject[$Property.Name] = Clear-SensitiveJsonValue -Value $Property.Value -Key $Property.Name
        }

        return $SanitizedObject
    }

    return $Value
}

function Copy-ConfigFileSafely {
    param(
        [Parameter(Mandatory = $true)][System.IO.FileInfo]$File,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    $FileName = [System.IO.Path]::GetFileName($File.FullName)
    if ($FileName.Equals('appsettings.json', [System.StringComparison]::OrdinalIgnoreCase)) {
        $ConfigObject = Get-Content -Raw -LiteralPath $File.FullName | ConvertFrom-Json
        $SanitizedConfig = Clear-SensitiveJsonValue -Value $ConfigObject
        $SanitizedJson = $SanitizedConfig | ConvertTo-Json -Depth 64
        [System.IO.File]::WriteAllText($Destination, $SanitizedJson + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
        return
    }

    if (Test-ConfigFileHasSecretValues -File $File) {
        $Relative = Get-RelativePathCompat -BasePath $ProjectRoot -FullPath $File.FullName
        throw "File cau hinh co gia tri nhay cam, khong nen dua vao zip: $Relative"
    }

    Copy-Item -LiteralPath $File.FullName -Destination $Destination -Force
}

function Test-IsExcludedFile {
    param([Parameter(Mandatory = $true)][System.IO.FileInfo]$File)

    $Relative = Get-RelativePathCompat -BasePath $ProjectRoot -FullPath $File.FullName
    $Relative = $Relative.Replace('/', '\').Trim('\')

    if ([string]::IsNullOrWhiteSpace($Relative)) {
        return $true
    }

    if (Test-DirectorySegmentExcluded -RelativePath $Relative) {
        return $true
    }

    if (Test-RelativePrefixExcluded -RelativePath $Relative) {
        return $true
    }

    if (Test-FileNameExcluded -FullPath $File.FullName) {
        return $true
    }

    if ($File.Length -gt $MaxSingleFileBytes) {
        return $true
    }

    return $false
}

function Copy-FileToClipboard {
    param([Parameter(Mandatory = $true)][string]$FilePath)

    if (-not (Test-Path -LiteralPath $FilePath)) {
        throw "Khong tim thay file de copy clipboard: $FilePath"
    }

    Add-Type -AssemblyName System.Windows.Forms
    $Collection = New-Object System.Collections.Specialized.StringCollection
    [void]$Collection.Add((Get-Item -LiteralPath $FilePath).FullName)
    [System.Windows.Forms.Clipboard]::SetFileDropList($Collection)
}

Write-Host ""
Write-Host "Dang gom source WebPhotocopyHub sach tu:" -ForegroundColor Cyan
Write-Host $ProjectRoot

$AllProjectFiles = Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File -Force
$IncludedFiles = New-Object System.Collections.Generic.List[System.IO.FileInfo]
$SkippedFiles = New-Object System.Collections.Generic.List[string]

foreach ($File in $AllProjectFiles) {
    $Relative = Get-RelativePathCompat -BasePath $ProjectRoot -FullPath $File.FullName
    $Excluded = Test-IsExcludedFile -File $File

    if (-not $Excluded) {
        if (Test-ConfigFileHasSecretValues -File $File) {
            $FileName = [System.IO.Path]::GetFileName($File.FullName)
            if (-not $FileName.Equals('appsettings.json', [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "File cau hinh co gia tri nhay cam, khong nen dua vao zip: $Relative"
            }
        }

        $IncludedFiles.Add($File) | Out-Null
    } else {
        $SkippedFiles.Add($Relative) | Out-Null
    }
}

if ($IncludedFiles.Count -eq 0) {
    throw "Khong tim thay file nao de dua vao zip."
}

foreach ($RequiredRelativeFile in $RequiredRelativeFiles) {
    $RequiredPath = Join-Path $ProjectRoot $RequiredRelativeFile
    if (-not (Test-Path -LiteralPath $RequiredPath)) {
        throw "Source project thieu file quan trong: $RequiredRelativeFile"
    }

    $RequiredFullName = (Get-Item -LiteralPath $RequiredPath).FullName
    $FoundRequired = $false
    foreach ($IncludedFile in $IncludedFiles) {
        if ($IncludedFile.FullName.Equals($RequiredFullName, [System.StringComparison]::OrdinalIgnoreCase)) {
            $FoundRequired = $true
            break
        }
    }

    if (-not $FoundRequired) {
        throw "File quan trong dang bi exclude nham: $RequiredRelativeFile"
    }
}

foreach ($File in $IncludedFiles) {
    $Relative = Get-RelativePathCompat -BasePath $ProjectRoot -FullPath $File.FullName
    $Destination = Join-Path $TempProjectRoot $Relative
    $DestinationDir = Split-Path -Parent $Destination

    if (-not (Test-Path -LiteralPath $DestinationDir)) {
        New-Item -ItemType Directory -Path $DestinationDir -Force | Out-Null
    }

    Copy-ConfigFileSafely -File $File -Destination $Destination
}

$ManifestPath = Join-Path $TempRoot 'CHATGPT_ZIP_MANIFEST.txt'
$IncludedListPath = Join-Path $TempRoot 'CHATGPT_INCLUDED_FILES.txt'
$SkippedListPath = Join-Path $TempRoot 'CHATGPT_SKIPPED_FILES.txt'

$SolutionProjectDirectoryLines = @()
foreach ($DirectoryName in ($SolutionProjectDirectoryNames | Sort-Object)) {
    $SolutionProjectDirectoryLines += "- $DirectoryName"
}

$SolutionProjectFileLines = @()
foreach ($ProjectFile in ($SolutionProjectRelativeFiles | Sort-Object)) {
    $SolutionProjectFileLines += "- $ProjectFile"
}

$ManifestLines = @(
    'WebPhotocopyHub - ChatGPT source zip',
    "CreatedAt: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "WebPhotocopyRoot: $WebPhotocopyRoot",
    "ProjectRoot: $ProjectRoot",
    "Solution: $SolutionFileName",
    "ZipPath: $ZipPath",
    "IncludedFileCount: $($IncludedFiles.Count)",
    "SkippedFileCount: $($SkippedFiles.Count)",
    '',
    'Muc tieu:',
    '- Zip nay dung de upload len ChatGPT web khi can doc code va sua code.',
    '- Da giu day du solution WebPhotocopyHub, csproj, controllers, views, viewmodels, services, SQL scripts, css/js/static assets can thiet.',
    '',
    'Thu muc project trong solution:'
) + $SolutionProjectDirectoryLines + @(
    '',
    'Csproj trong solution:'
) + $SolutionProjectFileLines + @(
    '',
    'Da loai tru co chu dich:',
    '- .git, .vs, bin, obj, node_modules, packages',
    '- runtime-data, App_Data, uploads, wwwroot/uploads, wwwroot/files, logs',
    '- appsettings.Development/Local/Production/Staging.json',
    '- .env*, secrets.json, credentials.json, service-account.json, account.md',
    '- *.db, *.db-shm, *.db-wal, *.sqlite*, *.zip, *.bak*, *.log, publish profile, cert/key/user/cache/temp/binary files',
    "- File lon hon $MaxSingleFileMb MB",
    '',
    'Ghi chu:',
    '- Khong dung zip nay de deploy production.',
    '- Neu can ChatGPT sua loi runtime, gui them log loi rieng.',
    '- Neu vua lo chia se secret, can rotate secret o provider that.'
)

[System.IO.File]::WriteAllLines($ManifestPath, $ManifestLines, [System.Text.UTF8Encoding]::new($false))

$IncludedRelativeLines = New-Object System.Collections.Generic.List[string]
foreach ($File in $IncludedFiles) {
    $IncludedRelativeLines.Add((Get-RelativePathCompat -BasePath $ProjectRoot -FullPath $File.FullName)) | Out-Null
}

[System.IO.File]::WriteAllLines($IncludedListPath, ($IncludedRelativeLines | Sort-Object), [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllLines($SkippedListPath, ($SkippedFiles | Sort-Object), [System.Text.UTF8Encoding]::new($false))

if (Test-Path -LiteralPath $ZipPath) {
    Write-Host ""
    Write-Host "Dang xoa file zip cu de ghi de:" -ForegroundColor Yellow
    Write-Host $ZipPath
    Remove-Item -LiteralPath $ZipPath -Force
}

$ZipDir = Split-Path -Parent $ZipPath
if (-not (Test-Path -LiteralPath $ZipDir)) {
    New-Item -ItemType Directory -Path $ZipDir -Force | Out-Null
}

Write-Host ""
Write-Host "Dang tao zip moi:" -ForegroundColor Cyan
Write-Host $ZipPath

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $TempRoot,
    $ZipPath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false
)

if (-not (Test-Path -LiteralPath $ZipPath)) {
    throw "Tao zip that bai: $ZipPath"
}

$ZipInfo = Get-Item -LiteralPath $ZipPath
$SizeMb = [Math]::Round($ZipInfo.Length / 1MB, 2)

Write-Host ""
Write-Host "Kiem tra nhanh noi dung zip:" -ForegroundColor Cyan

$ZipRead = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
$SuspiciousEntries = New-Object System.Collections.Generic.List[string]
$ZipEntryNames = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
$ForbiddenEntryPatterns = @(
    '(^|/)TKS_Thuc_Tap_V11',
    '(^|/)\.git(/|$)',
    '(^|/)\.vs(/|$)',
    '(^|/)bin(/|$)',
    '(^|/)obj(/|$)',
    '(^|/)logs?(/|$)',
    '(^|/)runtime-data(/|$)',
    '(^|/)uploads?(/|$)',
    '(^|/)files(/|$)',
    '(^|/)App_Data(/|$)',
    'appsettings\.(Development|Local|Production|Staging)\.json$',
    '(^|/)(\.env[^/]*|secrets\.json|credentials\.json|service-account\.json|account\.md|id_rsa|id_rsa\.pub|publishsettings)$',
    '\.(db|sqlite|sqlite3)(-(shm|wal|journal))?$',
    '\.bak($|[-.])',
    '\.(zip|7z|rar|log|pfx|p12|pem|key|crt|cer|user|suo|wsuo|docstates|cache|tmp|dll|exe|pdb|nupkg|snupkg|pubxml|publishsettings)$'
)

foreach ($Entry in $ZipRead.Entries) {
    $Name = $Entry.FullName.Replace('\', '/')
    [void]$ZipEntryNames.Add($Name)

    foreach ($Pattern in $ForbiddenEntryPatterns) {
        if ($Name -match $Pattern) {
            $SuspiciousEntries.Add($Entry.FullName) | Out-Null
            break
        }
    }
}

foreach ($RequiredRelativeFile in $RequiredRelativeFiles) {
    $RequiredZipEntry = ('Project/' + $RequiredRelativeFile.Replace('\', '/'))
    if (-not $ZipEntryNames.Contains($RequiredZipEntry)) {
        $SuspiciousEntries.Add("MISSING REQUIRED: $RequiredZipEntry") | Out-Null
    }
}

$ZipRead.Dispose()

if ($SuspiciousEntries.Count -gt 0) {
    Write-Host "CANH BAO: Co file nghi ngo hoac thieu file bat buoc trong zip:" -ForegroundColor Yellow
    $SuspiciousEntries | Sort-Object -Unique | Select-Object -First 120 | ForEach-Object {
        Write-Host "- $_" -ForegroundColor Yellow
    }

    throw "Zip chua dat chuan. Da dung de ban kiem tra."
}

Write-Host "OK: Zip du source WebPhotocopyHub va khong thay file secret/runtime pho bien." -ForegroundColor Green

if (-not $NoClipboard) {
    Write-Host ""
    Write-Host "Dang copy FILE ZIP vao Clipboard..." -ForegroundColor Cyan
    Copy-FileToClipboard -FilePath $ZipPath
}

Remove-Item -LiteralPath $TempRoot -Recurse -Force

Write-Host ""
Write-Host "HOAN TAT." -ForegroundColor Green
Write-Host "File zip da duoc tao."
if (-not $NoClipboard) {
    Write-Host "File zip da nam trong Clipboard. Mo ChatGPT va nhan Ctrl + V de dan/upload file zip."
}
Write-Host ""
Write-Host "File zip:" -ForegroundColor Cyan
Write-Host $ZipPath
Write-Host "Dung luong: $SizeMb MB"
Write-Host "So file source da dua vao zip: $($IncludedFiles.Count)"
