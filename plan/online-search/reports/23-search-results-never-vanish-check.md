pass

## Commands

- `scripts/agent-test.sh`
- `scripts/agent-test.sh --filter FullyQualifiedName~SearchDialogTests server`
- `scripts/agent-test.sh --full`
- `dotnet build src/Client -c Debug`
- `dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps`

## Result

`SearchDialogTests`: 4 passed, 0 failed.

Full suite:

- Server.Tests: 746 passed, 0 failed, 1 skipped, 747 total
- Shared.Tests: 1931 passed, 0 failed, 3 skipped, 1934 total

Client build: 0 warnings, 0 errors. Fable exit 0.
