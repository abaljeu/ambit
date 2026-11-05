# 23 — Search results never vanish

Scope report for [23 — Search results never vanish](../issues/23-search-results-never-vanish.md). Mode: write-ticket.

## Signatures

[ ] `DialogResultRows.holdRows` keeps prior rows while readiness is pending, and while a ready client list is empty and the server picture is outstanding.

[ ] `DialogResultRows.holdRows` returns `ZeroResults` when a ready list is empty and the server picture is idle.

[ ] `DialogResultRows.pickShown` on `ZeroResults` returns no row.

[ ] `DialogResultRows.stepShownIndex` on `ZeroResults` leaves the index in place.

[ ] `SearchDialog.retargetNodeSearch` replaces `Update.update` on `NodeSearchQuery`.

[ ] `FileSearchDialog.retargetFileSearch` replaces `Update.update` on `FileSearchQuery`.

[ ] `SearchDialogChrome.renderDialogResultList` stays a shell over `renderDialogResultRows`.

## Match sites

[ ] `Update.update` branch `NodeSearchQuery` calls `SearchDialog.retargetNodeSearch`.

[ ] `Update.update` branch `FileSearchQuery` calls `FileSearchDialog.retargetFileSearch`.

[ ] `SearchDialogView.renderSearchDialog` and `FileSearchDialogView.renderFileSearchDialog` call `renderDialogResultRows`.

[ ] `SearchDialogChrome.wireDialogList` ignores a click on the row whose dataset `zero` is `1`.

## Existing tests

[ ] [Search dialog mode](../../../tests/Shared.Tests/SearchDialogModeTests.fs) test ``SearchDialog mode stores invoked command label`` keeps its name and assertions.

[ ] [File search dialog mode](../../../tests/Shared.Tests/FileSearchDialogModeTests.fs) test ``FileSearchDialog mode stores query and return mode`` keeps its name and assertions.

[ ] [Search picture](../../../tests/Shared.Tests/SearchPictureTests.fs) keeps each existing test name and assertion.

## Copy-from

[ ] `SearchPicture.awaitGap` and `SearchPicture.startAfterGap` already decide when a Find picture will start.

[ ] `SearchDialogChrome.renderDialogResultList` already clears `.amb-dialog-results` and appends one `li` per label.

## write-ticket

[ ] Drawing server hits stays [15 — Dialog shows server hits](../issues/15-dialog-shows-server-hits.md).

[ ] `serverHitIds` began in [07 — Server completes the picture](../issues/07-server-completes-the-picture.md), commit `12ff0a88`, pull request 200.

[ ] The empty-list collapse began in commit `e9554ff1` (*Add search dialog*) and moved into `SearchDialogChrome.renderDialogResultList` in commit `71544db0` (*search dialog perf and refactor*).

[ ] `Update.update` clearing the node-search cache on every query change began in commit `10d1eb0b` (*perf improvements*).

[ ] `resetFileSearchResults` on a file-search query began in commit `73a0f7f2`.

[ ] Source files: `src/Shared/SearchPicture.fs`, `src/Client/SearchDialog.fs`, `src/Client/FileSearchDialog.fs`, `src/Client/SearchDialogChrome.fs`, `src/Client/SearchDialogView.fs`, `src/Client/FileSearchDialogView.fs`, `src/Client/Update.fs`, `src/Server/wwwroot/style.css`.

[ ] Test file: `tests/Shared.Tests/SearchPictureTests.fs`.
