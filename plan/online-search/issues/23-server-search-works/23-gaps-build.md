# 23 — Server search works gaps

`SearchPicture.Request` carries `count`, the result cap. Decode uses 200 when the request omits `count`. The server walk stops at `count`. The client reply room uses `count` after excluded Node ids leave, and each remaining id is listed once. `SearchPicture.ReplyRoom` holds that room.

## Files

- `src/Shared/SearchPicture.fs`
- `src/Shared/ViewModelSearch.fs`
- `src/Server/SearchActor.fs`
- `src/Client/SearchDialog.fs`
- `src/Client/SearchPictureClient.fs`
- `tests/Shared.Tests/SearchPictureTests.fs`
- `tests/Server.Tests/SearchActorTests.fs`
- `tests/Server.Tests/SearchDialogTests.fs`
- `tests/Server.Tests/QueryActorTests.fs`

## Tests

- ``room fills after excluded client hits``
- ``dialog lists a repeated server-only hit once``
- ``reply stops at the request count``

## Suites

- `tests/Server.Tests` with `AMBIT_FULL_SUITE=1` and `-nodeReuse:false` passed 776, failed 0, skipped 1. Total 777.
- `tests/Shared.Tests` with `AMBIT_FULL_SUITE=1` and `-nodeReuse:false` passed 1945, failed 0, skipped 3. Total 1948.
