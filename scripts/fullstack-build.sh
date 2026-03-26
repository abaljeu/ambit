#!/usr/bin/env bash
set -e

dotnet build src/Server -c Debug
dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps
