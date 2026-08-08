# Litecashier License Server

Online license API for Cashier and Restaurant trial / paid activation.

## Run

```powershell
cd license_server
dotnet run
```

Default: `http://localhost:5099`  
Admin key: `AdminApiKey` in `appsettings.json` (change before production).

## Admin UI

Open in browser:

```
http://localhost:5099/
```

Login with `AdminApiKey`, then create/list/revoke serials and view machine activations.

## Create keys (CLI)

```powershell
.\New-LicenseKey.ps1 -Days 2
.\New-LicenseKey.ps1 -Months 12 -Product Cashier
.\New-LicenseKey.ps1 -Lifetime -Product Both
```

## Deploy

Publish to your VPS and set the same URL in POS `License:BaseUrl` / installer `appsettings.Production.json`.
