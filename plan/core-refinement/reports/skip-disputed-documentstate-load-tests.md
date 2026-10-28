# Skip disputed documentState Load tests

Branch is `dev`. Desktop git protocol allows writes on `dev`. Nothing was committed or pushed.

`src/Shared/History.fs` still has the existing uncommitted change: a new special node’s `documentState` is `Current`. That file was not edited in this pass. Test diffs are skip attributes only. The earlier Unparsed/Current assertion rewrites are not in the working tree.

Skip text on every disabled fact is `pending documentState/Load spec`.

## 1. Disabled facts

### 1. NodeEventExpandTests

File: `tests/Server.Tests/NodeEventExpandTests.fs`

- 1. `git load reconcile leaves an Unparsed parent Unparsed without NodeParsed`
- 2. `git load reconcile parses an Unparsed directory file and NodeParsed sets Current`
- 3. `A file parse whose Change fails posts NodeParsing, then ParseFailed, and no NodeParsed`
- 4. `A directory parse whose Change fails posts NodeParsing, then ParseFailed, and no NodeParsed`
- 5. `planDirectoryReconcile posts NodeParsed and leaves Current`
- 6. `non document state change posts no Node Notice`

### 2. ParseThreadLoadTests

File: `tests/Server.Tests/ParseThreadLoadTests.fs`

- 1. `disk-newer file stays Parsed when its directory reconciles`
- 2. `second Load of a Parsed Directory reconciles and does not requeue`
- 3. `git Load and Workspace Load reconcile walks one level`
- 4. `directory Load reconcile walks one level`
- 5. `Load of a newly Unparsed child Directory reconciles one level`
- 6. `Directory reconcile leaves existing member parse state unchanged`
- 7. `Directory reconcile pushes nothing onto the parse stack`

### 3. GithubTransportLoadPullTests

File: `tests/Server.Tests/GithubTransportLoadPullTests.fs`

- 1. `git Load pull marks a received file and its directory Unparsed` — attribute is `SkippableFact`, which this fact already used

### 4. LazyLoadReconciliationServerTests

File: `tests/Server.Tests/LazyLoadReconciliationServerTests.fs`

- 1. `git pull add marks the containing directory Unparsed`

### 5. DocumentPersistChangeTests

File: `tests/Server.Tests/DocumentPersistChangeTests.fs`

- 1. `readAllDocuments round trips nested workspace tree`
- 2. `readAllDocuments cold load keeps file outline without plain body`

### 6. SpecialNodeStateAxesTests

File: `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`

- 1. `special node stores parse persist and pullingFile only inside DocumentState`
- 2. `NewSpecialNode starts Unparsed and Persisted`
- 3. `graph edit marks only the nearest owning special Unpersisted`
- 4. `failed edit of an Unparsed special does not mark Unpersisted`

### 7. DirectoryReconcileTests

File: `tests/Shared.Tests/DirectoryReconcileTests.fs`

- 1. `disk-newer reconcile ops leave the file Current`
- 2. `missing disk directory is created Unparsed and is not pushed`
- 3. `workspace leaves a disk-newer directory Current`
- 4. `missing directory attaches while the reconciled node is Unparsed`

### 8. HistoryTests

File: `tests/Shared.Tests/HistoryTests.fs`

- 1. `nested file parse under current directory replaces file tree`
- 2. `nested file parse still allowed when enclosing directory is unparsed`

### 9. LazyLoadReconciliationTests

File: `tests/Shared.Tests/LazyLoadReconciliationTests.fs`

- 1. `one added file creates a file stub`
- 2. `nested file parse leaves the new directory Unparsed without NodeParsed`
- 3. `upload-built directories stay Unparsed without NodeParsed`
- 4. `exact amb add marks directory stub unparsed`
- 5. `rediscovered Added Current file stays Current with children`
- 6. `rediscovered Unparsed Directory File still parses on Added`
- 7. `received file add marks the containing directory Unparsed`

### 10. DocumentAssemblyTests

File: `tests/Shared.Tests/DocumentAssemblyTests.fs`

- 1. `assembleFromArtifactsBounded seeds Unparsed File stub without body`

There is no `tests/Server.Tests/DocumentAssemblyTests.fs`.

## 2. Considered and not disabled

- 1. `planParseFile posts NodeParsing, the Change, then NodeParsed` in `NodeEventExpandTests.fs`. The `NodeUnparsed` wrap was on the shared `startParseOn` helper and is already gone. This fact’s body was not edited.
- 2. `planDirectoryReconcile posts NodeParsing, the Change, then NodeParsed` in `NodeEventExpandTests.fs`. Same helper. Body not edited.
- 3. `empty directory plan posts NodeParsing then NodeParsed` in `NodeEventExpandTests.fs`. Same helper. Body not edited.
- 4. `unparsed directory with a normal child drops a missing member` in `NodeEventExpandTests.fs`. Same helper. Body not edited.
- 5. `File Load parses its file regardless of Parsed state or disk date` in `ParseThreadLoadTests.fs`. It sat in the following context of an edit hunk. Its body was not changed.
- 6. `user Load on a Parsed Directory marks it Unparsed then reconciles` in `ParseThreadLoadTests.fs`. It is a real Load. It was not one of the newly sent child or injected-notice edits.
- 7. `unparsed invariant also applies to directory and workspace documents` in `HistoryTests.fs`. The nearby rewrite uses `withDocumentState directoryId`, not `withDocumentState fileId`.
- 8. `readAllDocuments cold load file-owns-directory without plain body` in `DocumentPersistChangeTests.fs`. Its documentState assertion was not in the disputed hunks.
- 9. `readAllDocuments cold load skips plain file body artifact` in `DocumentPersistChangeTests.fs`. No documentState assertion was changed.
- 10. `nested added path creates missing directory stubs` in `LazyLoadReconciliationTests.fs`. Not in the disputed hunks.
- 11. `rediscovered Current Directory Files skip parse on Added` in `LazyLoadReconciliationTests.fs`. Different fact from the Unparsed Directory File fact.
- 12. `disk-newer directory stays Current and is not pushed` in `DirectoryReconcileTests.fs`. Not a new-directory documentState hunk.

## 3. Key Load tests

These were not modified.

### 1. Workspace

- 1. `mailbox Load of a Workspace pushes onto the Parse stack` in `tests/Server.Tests/ParseThreadLoadTests.fs`. `mbLoadNode` on the workspace succeeds and pushes that workspace id onto the parse stack.
- 2. `Desk Load pushes a Workspace` in `tests/Server.Tests/ParseThreadLoadTests.fs`. A Desk Load command on the workspace pushes the workspace id and does not push its file.
- 3. `plain Load of a Workspace still requires the git actor` in `tests/Server.Tests/ParseThreadLoadTests.fs`. A plain Load of a workspace still demands the git actor.

### 2. Directory

- 1. `mailbox Load of a Directory pushes onto the Parse stack` in `tests/Server.Tests/ParseThreadLoadTests.fs`. `mbLoadNode` on the directory succeeds and pushes that directory id.
- 2. `Directory Load push reconciles when the parse thread pops it` in `tests/Server.Tests/ParseThreadLoadTests.fs`. A Desk Load of the directory, once the parse thread pops it, reconciles a new `extra.txt` on disk.
- 3. `user Load on a Parsed Directory marks it Unparsed then reconciles` in `tests/Server.Tests/ParseThreadLoadTests.fs`. User Load on an already Parsed directory sets it Unparsed, pushes it, then reconcile brings it back to Parsed and adds `extra.txt`.

### 3. File

- 1. `mailbox Load of File node pushes onto Parse stack` in `tests/Server.Tests/ParseThreadLoadTests.fs`. `mbLoadNode` on the file succeeds and the parse consumer receives that file id.
- 2. `File Load push parses when the parse thread pops it` in `tests/Server.Tests/ParseThreadLoadTests.fs`. A Desk Load of the file, once popped, parses disk text `HELLO` into the graph.
- 3. `Load on a file parses that file` in `tests/Server.Tests/LoadSaveCommandTests.fs`. A plain Load command parses the file to Current and installs the on-disk body `seed` as child text.
- 4. `File Load parses its file regardless of Parsed state or disk date` in `tests/Server.Tests/ParseThreadLoadTests.fs`. User Load sets an already Parsed file Unparsed, then parses even when the disk timestamp is old, including a second Load.
