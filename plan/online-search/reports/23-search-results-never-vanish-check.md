pass

## Round

[ ] `scripts/agent-test.sh` with no arguments ran `dotnet test tests/Shared.Tests --filter FullyQualifiedName~SearchPictureTests`. Result: passed 13, failed 0, skipped 0.

[ ] `scripts/agent-test.sh --filter "FullyQualifiedName~pending query keeps the prior result rows" shared` result: passed 1, failed 0, skipped 0.

[ ] `scripts/agent-test.sh --filter "FullyQualifiedName~ready empty results show the zero-results row" shared` result: passed 1, failed 0, skipped 0.

[ ] `scripts/agent-test.sh --filter "FullyQualifiedName~empty results stay while the server picture is outstanding" shared` result: passed 1, failed 0, skipped 0.

[ ] `scripts/agent-test.sh --filter "FullyQualifiedName~pick on the zero-results row does nothing" shared` result: passed 1, failed 0, skipped 0.

[ ] `scripts/agent-test.sh --full` log `/tmp/ambit-full-suite-20261105-054601.log` finished. Server.Tests: passed 742, failed 0, skipped 1, total 743. Shared.Tests: passed 1935, failed 0, skipped 3, total 1938.

[ ] `dotnet build src/Client/Gambol.Client.fsproj` succeeded with 0 warnings.

[ ] `dotnet fable src/Client --outDir /tmp/ambit-fable-23 --sourceMaps` finished in 13015ms with exit 0.

## Browser

[ ] This round has no browser run. The zero row and the held rows were not clicked in a browser.
