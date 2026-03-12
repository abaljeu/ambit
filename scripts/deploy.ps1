dotnet fable src/Client  -o ../Server/wwwroot

dotnet publish src/Server -c Release -o ./publish

Compress-Archive -Path ./publish/* -DestinationPath ./site.zip -Force

Write-Host ""
Write-Host "Then deploy via Kudu:"
Write-Host ""
Write-Host "  1. Portal -> your Web App -> Advanced Tools -> Go"
Write-Host "  2. Tools -> Zip Push Deploy"
Write-Host "  3. Drag and drop site.zip onto the page"
