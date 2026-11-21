# Search results same text

## Result

Branch `dev`. No commit.

## Files

- [SearchDialog](src/Client/SearchDialog.fs)
- [SearchDialogChrome](src/Client/SearchDialogChrome.fs)
- [style.css](src/Server/wwwroot/style.css)
- [SearchDialogTests](tests/Server.Tests/SearchDialogTests.fs)
- [Gambol.Server.Tests](tests/Server.Tests/Gambol.Server.Tests.fsproj)

## Behavior

Find paints `currentSearchResults`.

- The same search text keeps the local rows.
- A server reply adds server hits after those rows.
- An empty server reply leaves the local rows in place.
- A later read whose new walk is empty leaves the rows already shown for that text.
- A new search text shows that search's list only. File search follows the same rule on a text change.
- An empty list draws one shared row, `Zero results`. Enter, arrow keys, and click leave that row unselected.

## Tests

Command: `scripts/agent-test.sh --filter FullyQualifiedName~SearchDialogTests server`

Passed: 6. Failed: 0. Duration: 229 ms.

## Unverified

Browser paint of the Find dialog and the file-search dialog. The tests call the list the view reads. They do not paint the result rows, so the live flicker and the Zero results row on screen are still unseen.
