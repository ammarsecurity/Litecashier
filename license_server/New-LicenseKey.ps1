#Requires -Version 5.1
<#
.SYNOPSIS
  Create a Litecashier license key via the license server admin API.

.EXAMPLE
  .\New-LicenseKey.ps1 -Days 2
  .\New-LicenseKey.ps1 -Months 12 -Product Cashier
  .\New-LicenseKey.ps1 -Lifetime -Product Both
#>
param(
    [string]$BaseUrl = "http://localhost:5099",
    [string]$AdminApiKey = "change-me-admin-key",
    [ValidateSet("Cashier", "Restaurant", "Both")]
    [string]$Product = "Both",
    [int]$Days = 0,
    [int]$Months = 0,
    [switch]$Lifetime,
    [int]$MaxActivations = 1,
    [string]$Notes = ""
)

$ErrorActionPreference = "Stop"

if ($Lifetime) {
    $durationType = "Lifetime"
    $durationValue = 0
}
elseif ($Months -gt 0) {
    $durationType = "Months"
    $durationValue = $Months
}
elseif ($Days -gt 0) {
    $durationType = "Days"
    $durationValue = $Days
}
else {
    throw "Specify -Days N, -Months N, or -Lifetime"
}

$body = @{
    product         = $Product
    durationType    = $durationType
    durationValue   = $durationValue
    maxActivations  = $MaxActivations
    notes           = $Notes
} | ConvertTo-Json

$headers = @{ "X-Admin-Key" = $AdminApiKey; "Content-Type" = "application/json" }
$res = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/admin/keys" -Headers $headers -Body $body
Write-Host "Code: $($res.code)"
Write-Host "Product: $($res.product) | $($res.durationType)=$($res.durationValue)"
$res | ConvertTo-Json -Depth 5
