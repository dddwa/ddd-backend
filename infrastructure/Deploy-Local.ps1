$ErrorActionPreference = "Stop"

$privateParamsFile = "$PSScriptRoot\local.private.json"
if (-not (Test-Path $privateParamsFile)) {
    Copy-Item "$PsScriptRoot\local.private.template.json" $privateParamsFile
    Write-Warning "Please update information in $privateParamsFile"
    exit 1
}

$fields = ConvertFrom-Json ([System.IO.File]::ReadAllText($privateParamsFile))
$fieldsAsHashTable = @{}
$fields.psobject.properties | ForEach-Object { $fieldsAsHashTable[$_.Name] = $_.Value }

$profile = Select-AzureRmProfile -Path "$PSScriptRoot\profile.json" -ErrorAction SilentlyContinue
if (-not $profile.Context) {
    Login-AzureRmAccount -TenantId $TenantId | Out-Null
    Save-AzureRmProfile -Path "$PSScriptRoot\profile.json"
}

& $PSScriptRoot\Deploy.ps1 -AlreadyLoggedIn @fieldsAsHashTable
