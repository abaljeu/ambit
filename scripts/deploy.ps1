$ErrorActionPreference = "Stop"

try {
    # 1. Build Fable client
    dotnet fable src/Client --outDir src/Server/wwwroot
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # 2. Publish ASP.NET server
    dotnet publish src/Server -c Release -o ./publish
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # 3. Build the zip under %TEMP% with a unique name, then move to .\site.zip.
    #    Compress-Archive opens the destination for exclusive write; writing directly to
    #    .\site.zip can fail if anything holds that path (Explorer, AV, indexer, etc.).
    #    A fresh path in TEMP avoids that during the heavy compression step.
    $deployZip = Join-Path $PWD "site.zip"
    $stagingZip = Join-Path $env:TEMP ("gambol-site-{0}.zip" -f ([guid]::NewGuid().ToString("n")))
    try {
        Compress-Archive -Path .\publish\* -DestinationPath $stagingZip -Force -ErrorAction Stop

        if (-not (Test-Path -LiteralPath $stagingZip -PathType Leaf)) {
            throw "Archive creation failed: staging zip was not created at $stagingZip"
        }

        if (Test-Path -LiteralPath $deployZip) {
            Remove-Item -LiteralPath $deployZip -Force -ErrorAction Stop
        }

        Move-Item -LiteralPath $stagingZip -Destination $deployZip -Force -ErrorAction Stop
    }
    finally {
        if (Test-Path -LiteralPath $stagingZip) {
            Remove-Item -LiteralPath $stagingZip -Force -ErrorAction SilentlyContinue
        }
    }

    if (-not (Test-Path -LiteralPath $deployZip -PathType Leaf)) {
        throw "Archive finalize failed: $deployZip was not created."
    }

    # 4. Deploy to Azure App Service using the new command
    az webapp deploy `
      --resource-group "Amble_group" `
      --name "Amble" `
      --src-path "$deployZip" `
      --type zip
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
catch {
    Write-Host $_
    exit 1
}