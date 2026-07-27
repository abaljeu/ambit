#!/usr/bin/env bash
# AGENTS - don't use
set -e

dotnet build src/Server -c Debug
dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps
npm run bundle
