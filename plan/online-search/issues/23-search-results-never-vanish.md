# 23 — Search results never vanish
**Status**: coded
One job: A person who types in Find, and a person who types in file search, keeps the rows already on screen until the new results are ready, and sees one non-selectable row `Zero results` when that search is empty. The results area stays on screen. The collapse sits beside [Online search spec](../spec.md) section 1 **Search spec** story **Dialog shows server hits**.

Type: bug-fixing
Blockers: none

## Inputs

### Working

[ ] `DialogResultRows.holdRows`, `DialogResultRows.pickShown`, and `DialogResultRows.stepShownIndex` in `src/Shared/SearchPicture.fs` decide the visible rows.

[ ] `SearchDialog.retargetNodeSearch` in `src/Client/SearchDialog.fs` publishes the next Find rows before the view reads them.

[ ] `SearchDialog.searchPictureEffects` and `SearchDialog.applyServerReply` clear the outstanding picture and publish again.

[ ] `FileSearchDialog.retargetFileSearch` in `src/Client/FileSearchDialog.fs` publishes the next file-search rows. `fileSearchSetQueryOp` stays a shell over that function.

[ ] `SearchDialogChrome.renderDialogResultRows` in `src/Client/SearchDialogChrome.fs` draws the `Zero results` row. `renderDialogResultList` stays a shell.

[ ] `SearchDialogView.renderSearchDialog` and `FileSearchDialogView.renderFileSearchDialog` pass the zero-row flag.

[ ] `Update.update` in `src/Client/Update.fs` sends `NodeSearchQuery` and `FileSearchQuery` to the retarget functions.

[ ] `src/Server/wwwroot/style.css` class `amb-dialog-zero` keeps the zero row from looking selected.

[ ] Tests in `tests/Shared.Tests/SearchPictureTests.fs`: ``pending query keeps the prior result rows``, ``ready empty results show the zero-results row``, ``empty results stay while the server picture is outstanding``, ``pick on the zero-results row does nothing``.

### Reference

[ ] [15 — Dialog shows server hits](15-dialog-shows-server-hits.md) owns drawing the server list. This ticket leaves that drawing in place for 15.

[ ] `serverHitIds` began in [07 — Server completes the picture](07-server-completes-the-picture.md), commit `12ff0a88`, pull request 200. The Find dialog stores those ids and does not draw them.

[ ] Commit `e9554ff1` (*Add search dialog*) clears `.amb-dialog-results` and appends a row only when the label list has an entry. Commit `71544db0` (*search dialog perf and refactor*) moves that clear into `renderDialogResultList`.

[ ] Commit `10d1eb0b` (*perf improvements*) clears the node-search cache on `NodeSearchQuery` before a replacement list exists. Commit `73a0f7f2` does the same for file search through `resetFileSearchResults`.

## Do not load

[ ] `src/Shared/ResidentProjection.fs`, `src/Shared/ViewModelDomPlan.fs`, `WantTests.fs`, `ViewModelTests.fs`, `SyncLogicTests.fs`.

[ ] `src/Server/Core/*`, CommandRequest, CoreActorPool, and actor files.

[ ] Plan tickets under `plan/actor-as-client`.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Expand `src/Shared/SearchPicture.fs` with `DialogResultRows`.
3. [x] Change `src/Client/SearchDialog.fs` so a query change publishes the next rows, and an outstanding picture keeps the prior rows when the client list is empty.
4. [x] Change `src/Client/FileSearchDialog.fs` so a query change publishes the next rows, and an empty ready list is the zero-results state.
5. [x] Change `src/Client/SearchDialogChrome.fs` so the zero row is one non-selectable `li`.
6. [x] Change `src/Client/SearchDialogView.fs` and `src/Client/FileSearchDialogView.fs` to pass that row.
7. [x] Change `src/Client/Update.fs` to call the retarget functions.
8. [x] Change `src/Server/wwwroot/style.css` so the zero row hover state stays plain.

## Outputs

[ ] `src/Shared/SearchPicture.fs`

[ ] `src/Client/SearchDialog.fs`

[ ] `src/Client/FileSearchDialog.fs`

[ ] `src/Client/SearchDialogChrome.fs`

[ ] `src/Client/SearchDialogView.fs`

[ ] `src/Client/FileSearchDialogView.fs`

[ ] `src/Client/Update.fs`

[ ] `src/Server/wwwroot/style.css`

[ ] `tests/Shared.Tests/SearchPictureTests.fs` filter ``pending query keeps the prior result rows``

[ ] `tests/Shared.Tests/SearchPictureTests.fs` filter ``ready empty results show the zero-results row``

[ ] `tests/Shared.Tests/SearchPictureTests.fs` filter ``empty results stay while the server picture is outstanding``

[ ] `tests/Shared.Tests/SearchPictureTests.fs` filter ``pick on the zero-results row does nothing``

[ ] `plan/online-search/reports/23-search-results-never-vanish-check.md`

## Stop when

[ ] Record the stop at `plan/online-search/reports/23-search-results-never-vanish-stop.md`, then ask.

[ ] Stop when the fix needs a file outside Inputs.

[ ] Stop when the fix contradicts section 1 **Search spec** named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `Shared.Tests`: ``pending query keeps the prior result rows``
  - `Shared.Tests`: ``ready empty results show the zero-results row``
  - `Shared.Tests`: ``empty results stay while the server picture is outstanding``
  - `Shared.Tests`: ``pick on the zero-results row does nothing``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/online-search/reports/23-search-results-never-vanish-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
