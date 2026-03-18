$ErrorActionPreference = "Stop"

dotnet fable src/Client --outDir src/Server/wwwroot
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet publish src/Server -c Release -o ./publish
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Compress-Archive -Path ./publish/* -DestinationPath ./site.zip -Force

Write-Host ""
Write-Host "Then deploy via Kudu:"
Write-Host ""
Write-Host "  1. Portal -> your Web App -> Advanced Tools -> Go"
Write-Host "  2. Tools -> Zip Push Deploy"
Write-Host "  3. IMPORTANT: set Target/Destination directory to / (NOT /wwwroot)"
Write-Host "  4. Drag and drop site.zip onto the page"