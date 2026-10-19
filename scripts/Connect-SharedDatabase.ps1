[CmdletBinding()]
param(
    [switch]$Configure,
    [switch]$RunBackend,
    [string]$Username = 'smartparking_owner'
)

$ErrorActionPreference = 'Stop'
$parkingRepoRoot = Split-Path -Parent $PSScriptRoot
$parkingApiProject = Join-Path $parkingRepoRoot 'backend/src/Parking.WebAPI'
$parkingPersistenceProject = Join-Path $parkingRepoRoot 'backend/src/Parking.Persistence'
$parkingHost = 'ep-wispy-bread-aoyxxn50.c-2.ap-southeast-1.aws.neon.tech'
$parkingDatabase = 'smartparking'
$parkingPreviousEnvironment = [Environment]::GetEnvironmentVariable('ConnectionStrings__Parking', 'Process')
$parkingSecret = $null
$parkingConnection = $null
$parkingBuilder = $null
$parkingSavedSettings = $null
$parkingSecretJson = $null

function Invoke-ParkingDotNet {
    param([string[]]$Arguments, [string]$InputJson)

    # Capture diagnostics and secret JSON without allowing native stderr to
    # bypass our error handling in Windows PowerShell 5.1.
    $ErrorActionPreference = 'Continue'
    $PSNativeCommandUseErrorActionPreference = $false
    if ($PSBoundParameters.ContainsKey('InputJson')) {
        $parkingOutput = ($InputJson | & dotnet @Arguments 2>&1 | Out-String)
    }
    else {
        $parkingOutput = (& dotnet @Arguments 2>&1 | Out-String)
    }
    [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $parkingOutput }
}

Push-Location -LiteralPath $parkingRepoRoot
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'Can cai .NET SDK 10 truoc khi chay script.'
    }

    if ($Configure) {
        if ([string]::IsNullOrWhiteSpace($Username)) { throw 'Username DB khong duoc de trong.' }
        $parkingSecret = Read-Host ('Nhap mat khau DB Neon cho {0}' -f $Username) -AsSecureString
        if ($parkingSecret.Length -eq 0) { throw 'Mat khau DB khong duoc de trong.' }
        $parkingBuilder = [System.Data.Common.DbConnectionStringBuilder]::new()
        $parkingBuilder['Host'] = $parkingHost
        $parkingBuilder['Port'] = 5432
        $parkingBuilder['Database'] = $parkingDatabase
        $parkingBuilder['Username'] = $Username
        $parkingBuilder['Password'] = [System.Net.NetworkCredential]::new('', $parkingSecret).Password
        $parkingBuilder['SSL Mode'] = 'Require'
        $parkingBuilder['Channel Binding'] = 'Require'
        $parkingBuilder['Timeout'] = 15
        $parkingBuilder['Command Timeout'] = 15
        $parkingConnection = $parkingBuilder.get_ConnectionString()
    }
    else {
        $parkingSavedResult = Invoke-ParkingDotNet -Arguments @('user-secrets', 'list', '--json', '--project', $parkingApiProject)
        if ($parkingSavedResult.ExitCode -ne 0) { throw 'Chua doc duoc cau hinh. Chay lai voi -Configure.' }
        try {
            $parkingSavedJson = ($parkingSavedResult.Output -split '\r?\n' |
                Where-Object { $_ -notmatch '^\s*//(?:BEGIN|END)\s*$' }) -join "`n"
            $parkingSavedSettings = $parkingSavedJson | ConvertFrom-Json
            $parkingConnection = $parkingSavedSettings.'ConnectionStrings:Parking'
        }
        catch { throw 'Chua doc duoc User Secrets. Chay lai voi -Configure.' }
        if ([string]::IsNullOrWhiteSpace($parkingConnection)) {
            throw 'Chua luu ket noi tren may nay. Chay lai voi -Configure.'
        }
    }

    try {
        $parkingBuilder = [System.Data.Common.DbConnectionStringBuilder]::new()
        $parkingBuilder.set_ConnectionString($parkingConnection)
        $parkingConfiguredHost = ([string]$parkingBuilder.get_Item('Host')).Trim().TrimEnd('.').ToLowerInvariant().Replace('-pooler.', '.')
        $parkingConfiguredDatabase = [string]$parkingBuilder.get_Item('Database')
        $parkingConfiguredPort = 5432
        if ($parkingBuilder.ContainsKey('Port')) { $parkingConfiguredPort = [int]$parkingBuilder.get_Item('Port') }
        $parkingSslMode = [string]$parkingBuilder.get_Item('SSL Mode')
    }
    catch { throw 'Chuoi ket noi da luu khong hop le. Chay lai voi -Configure.' }

    if ($parkingConfiguredHost -ne $parkingHost -or $parkingConfiguredDatabase -cne $parkingDatabase -or $parkingConfiguredPort -ne 5432) {
        throw 'Cau hinh khac DB chung cua nhom. Chay lai voi -Configure.'
    }
    if ($parkingSslMode -notin @('Require', 'VerifyCA', 'VerifyFull')) {
        throw 'Ket noi Neon can SSL Mode=Require hoac VerifyFull. Chay lai voi -Configure.'
    }

    Write-Host ('DB chung: {0} / {1} (branch production)' -f $parkingHost, $parkingDatabase)
    Write-Host 'Dang restore cong cu va build backend...'
    $parkingRestore = Invoke-ParkingDotNet -Arguments @('tool', 'restore')
    if ($parkingRestore.ExitCode -ne 0) { throw 'dotnet tool restore that bai. Chay lenh nay rieng de xem loi dependency.' }
    $parkingBuild = Invoke-ParkingDotNet -Arguments @('build', $parkingApiProject, '--nologo', '-m:1', '-p:UseSharedCompilation=false', '--verbosity', 'minimal')
    if ($parkingBuild.ExitCode -ne 0) { throw 'Backend chua build duoc. Chay dotnet build backend/src/Parking.WebAPI de xem loi.' }

    $env:ConnectionStrings__Parking = $parkingConnection
    Write-Host 'Dang truy van DB Neon that...'
    $parkingCheck = Invoke-ParkingDotNet -Arguments @('ef', 'migrations', 'list', '--project', $parkingPersistenceProject,
        '--startup-project', $parkingApiProject, '--no-build', '--json', '--prefix-output', '--no-color')
    if ($parkingCheck.ExitCode -ne 0) { throw 'Lenh kiem tra DB that bai. Kiem tra cong cu EF va cau hinh du an.' }
    try {
        $parkingData = ($parkingCheck.Output -split '\r?\n' | Where-Object { $_ -match '^data:' } |
            ForEach-Object { $_ -replace '^data:\s*', '' }) -join "`n"
        $parkingMigrations = @($parkingData | ConvertFrom-Json)
        $parkingVerified = $parkingMigrations.Count -gt 0
        foreach ($parkingMigration in $parkingMigrations) {
            if ($parkingMigration.applied -isnot [bool]) { $parkingVerified = $false }
        }
    }
    catch { $parkingVerified = $false }

    # EF may exit zero after connection failure; applied:null is not proof of a connection.
    if (-not $parkingVerified) {
        if ($parkingCheck.Output -match '28P01|password authentication failed') {
            throw 'Sai username hoac mat khau DB Neon. Xin lai thong tin roi chay voi -Configure.'
        }
        throw 'Chua xac minh duoc DB. Kiem tra mang, quyen truy cap va thong tin Neon voi nguoi phu trach.'
    }

    Write-Host ('DA XAC MINH KET NOI DB NEON CUA NHOM: {0}' -f $parkingDatabase) -ForegroundColor Green
    if (@($parkingMigrations | Where-Object { $_.applied -eq $false }).Count -gt 0) {
        Write-Host 'Co migration chua ap dung/lich su da reset. Bao nguoi phu trach; script khong tao bang.' -ForegroundColor Yellow
    }

    if ($Configure) {
        $parkingSecretJson = @{ 'ConnectionStrings:Parking' = $parkingConnection } | ConvertTo-Json -Compress
        $parkingSave = Invoke-ParkingDotNet -Arguments @('user-secrets', 'set', '--project', $parkingApiProject) -InputJson $parkingSecretJson
        if ($parkingSave.ExitCode -ne 0) { throw 'Da ket noi DB nhung chua luu duoc User Secrets tren may nay.' }
        Write-Host 'Da luu User Secrets tren may nay.' -ForegroundColor Green
    }

    # Launch with the saved configuration, avoiding a stale inherited override.
    [Environment]::SetEnvironmentVariable('ConnectionStrings__Parking', $null, 'Process')
    if ($RunBackend) {
        & dotnet run --project $parkingApiProject --launch-profile http --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Backend dung voi loi. Kiem tra output cua backend.' }
    }
    else { Write-Host 'Lan sau: .\scripts\Connect-SharedDatabase.ps1 -RunBackend' }
}
finally {
    [Environment]::SetEnvironmentVariable('ConnectionStrings__Parking', $parkingPreviousEnvironment, 'Process')
    $parkingConnection = $null
    $parkingSecretJson = $null
    $parkingSavedJson = $null
    $parkingSavedResult = $null
    $parkingSavedSettings = $null
    $parkingBuilder = $null
    if ($null -ne $parkingSecret) { $parkingSecret.Dispose() }
    Pop-Location
}
