$appName = "amble-f6bfcfdygjd9b6b7"

dotnet fable src/Client -o src/Server/wwwroot

dotnet publish src/Server -c Release -o ./publish

Compress-Archive -Path ./publish/* -DestinationPath ./site.zip -Force

Write-Host ""
$cred = Get-Credential -Message "Enter Kudu deployment credentials (Portal -> Deployment Center -> Manage publish profile)"

$base64 = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($cred.UserName):$($cred.GetNetworkCredential().Password)"))
$headers = @{ Authorization = "Basic $base64" }

Write-Host "Deploying site.zip to $appName..."
$resp = Invoke-RestMethod `
    -Uri "https://$appName.scm.azurewebsites.net/api/zipdeploy" `
    -Method POST `
    -Headers $headers `
    -InFile (Resolve-Path ./site.zip) `
    -ContentType "application/zip"

Write-Host "Done. Response: $resp"
