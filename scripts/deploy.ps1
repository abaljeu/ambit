$ErrorActionPreference = "Stop"

# 1. Build Fable client
dotnet fable src/Client --outDir src/Server/wwwroot
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 2. Publish ASP.NET server
dotnet publish src/Server -c Release -o ./publish
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 3. Remove old zip (if not locked) and create new one
if (Test-Path .\site.zip) {
    # If this throws "in use by another process", you'll know what is locking it
    Remove-Item .\site.zip -Force
}

Compress-Archive -Path .\publish\* -DestinationPath .\site.zip -Force

# 4. Deploy to Azure App Service using the new command
az webapp deploy `
  --resource-group "Amble_group" `
  --name "Amble" `
  --src-path ".\site.zip" `
  --type zip