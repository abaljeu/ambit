# 13 — Lock the persist path moves

Ticket: [[13-lock-the-persist-path-moves-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- **Judgement** — Duplicated Code. Source: [[../../../context/SMELLS.md]]. ``persist path move waits while the moved file is held`` and ``persist path move waits while a file under the moved directory is held`` repeat the same holder, the same `waitUntilQueued 2`, and the same release.

```fsharp
let holder =
    Task.Run(fun () ->
        match NodeLockPool.withLock postGraph fileId (fun () ->
            entered.SetResult()
            release.Task.Wait()) with
        | Ok () -> ()
        | Error rejection -> failwith $"hold: {rejection}")

entered.Task.Wait()

let persist =
    Task.Run(fun () ->
        DocumentPersistChange.persistGraphChange dataDir graph postGraph)

(NodeLockPool.waitUntilQueued 2).Wait()
Assert.False(persist.IsCompleted)
```

## 2. Spec

- **Partial** — Story 5 Directory Node move is not this ticket's whole relocate. The lock this ticket names is in place: each coalesced move runs inside `NodeLockPool.withLockResult` on the post graph and `move.nodeId`.
- **Partial** — Outputs for ``a sibling file lock lets a file path move finish`` say `persistGraphChange` returns during the sibling hold. `File.Move` finishes during that hold: `notes.txt` exists and `readme.txt` is absent while the sibling File is still held. `persistGraphChange` is still running then. It returns `Ok` after the sibling releases. The file rename's affected roots include the parent Directory. `DocumentPersistWrite.writeDocument` takes `withLock` on that Directory. A Directory request waits while a descendant File is held.

## 3. Arch

- No findings.

## 4. Tests

- `bash scripts/check-test.sh 5621` printed `DONE` and `/tmp/ambit-full-suite-20261110-210235.log`. Server.Tests passed 766, failed 0, skipped 1, total 767. Shared.Tests passed 1937, failed 0, skipped 3, total 1940.
- ``persist path move waits while the moved file is held`` holds the File, keeps `persistGraphChange` outside that hold, leaves `readme.txt` in place, and after release leaves `notes.txt` and removes `readme.txt`.
- ``persist path move waits while a file under the moved directory is held`` holds the File under `docs`, keeps the Directory rename outside that hold, leaves `home/docs`, and after release leaves `home/archive`.
- ``a path move on a thread that already holds a lock leaves the artifact`` holds the sibling File on the calling thread, gets `Error "holder already holds a lock"`, leaves `readme.txt`, and keeps `entryCount` at 1 until that hold returns.
- ``a sibling file lock lets a file path move finish`` holds the sibling File on another thread. The renamed file is at `notes.txt` before that hold releases. The test then releases the sibling and waits for `persistGraphChange` to return `Ok`. It does not show `persistGraphChange` returning during the hold.
- `python3 scripts/standards-scan.py --diff origin/staging` printed no line.

## 5. Miss

- **Partial** — ``a sibling file lock lets a file path move finish`` does not show `persistGraphChange` returning during the sibling hold.
- The same test checks the path move, then releases the sibling before `persistGraphChange` returns. Outputs say the call returns during the hold.

## 6. Found elsewhere

The following issues were found beyond the ticket scope.  The fixes are suggested.

- No findings.
