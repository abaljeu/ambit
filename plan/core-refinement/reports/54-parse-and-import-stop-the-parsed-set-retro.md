# 54 — Parse and import stop the Parsed Set retro

Session: implement [54 — Parse and import stop the Parsed Set](plan/core-refinement/issues/54-parse-and-import-stop-the-parsed-set.md).

The human undid the source and the scope report.

This file is a proposal.

Skills, tickets, and source stay as they are until the human picks.

## 1. Recut 54 on the Larger path

Place: slicing.

Severity: high.

Owner: [Slicing work](plan/context/slicing-work.md).

This pick edits no file.

A picked slicing pass writes the new tickets.

54 — Parse and import stop the Parsed Set stays as it is until that pass.

[Build work](plan/context/build-work.md) sends a ticket back to slicing when Changes has numbered steps.

Changes on 54 — Parse and import stop the Parsed Set has three checkboxes:

- Import
- File parse
- Directory reconcile

The parent kept that ticket because [50 — Upload emits NodeUploading](plan/core-refinement/issues/50-upload-emits-nodeuploading.md) and [53 — Arrival and reconcile emit NodeReceived](plan/core-refinement/issues/53-arrival-and-reconcile-emit-nodereceived.md) were each built as one ticket.

Each of those tickets has one Changes checkbox.

The implementer then took one TDD shot across every producer and both parse-thread routes.

The next implement starts at slicing.

Each new ticket is one checkbox, adds or changes 4 tests or fewer, and changes 8 source files or fewer.

`Op.guardsUnparsedDocument` is off only when the command name is `Parse`.

`Op.applyAllowing` then rejects the op with `operation cannot modify an unparsed document; parse it first`.

`ImportText.markDocumentCurrentBeforeParse` writes `Op.SetDocumentState` from Unparsed to Current inside the import change.

That Set unlocks the later ops in the same change.

`Notice.NodeParsed` from `ParseThread.finishParse` is a later mailbox post.

`Api.inferredNodeEvents` infers `Notice.NodeParsed` from those Set ops.

1. **Import route.** Route `ImportText.buildImportChange` and `ImportText.buildDirectoryMergeChange` through the parse thread. Leave `ImportText.markDocumentCurrentBeforeParse` in place. One test observes `Notice.NodeParsed` from `ParseThread.finishParse` for the import.
2. **File-parse route.** Route `Api.postParseFile` through the parse thread. Leave the Set in `ImportDocument.planParseFile`. `ParseThread.parseFile` already runs `DocumentPersistWrite.planParseFile`. One test observes `Notice.NodeParsed` from `ParseThread.finishParse` for that command.
3. **Import contract.** Blocked by Import route. `ImportText.markDocumentCurrentBeforeParse` stops `Op.SetDocumentState`. One test observes an import change with no Parsed `Op.SetDocumentState` and a following tree op whose result is `ApplyResult.Changed`. The public text `operation cannot modify an unparsed document; parse it first` stays absent. The test names the `Api.inferredNodeEvents` arm for this producer.
4. **File-parse contract.** Blocked by File-parse route. `ImportDocument.planParseFile` stops `Op.SetDocumentState`. One test uses the same two observations for the file command, including its `Api.inferredNodeEvents` arm.
5. **Directory contract.** `ParseThread.parseDirectory` already runs `DirectoryReconcile.planDirectoryReconcile`. `DirectoryReconcile.markCurrent` stops `Op.SetDocumentState`. The seam is ``Load of a newly Unparsed child Directory reconciles one level`` in [ParseThreadLoadTests.fs](tests/Server.Tests/ParseThreadLoadTests.fs). After `Ev.apply`, the child directory reads `Current`. The undone shot left that replay `Unparsed`. This slice also names the `Api.inferredNodeEvents` arm for directory reconcile.

``parsed producers stop SetDocumentState`` asserts all three producers.

Each contract slice carries the test for its own producer.

The bundled name belongs to Directory contract, after the three producer observations exist.

## 2. Count Changes checkboxes

Place: no-ops.

Severity: high.

Home: Size in Slicing work, the file named in item 1.

Build work cites that sentence and drops its own copy.

The words "numbered steps" match the heading `### 1.` on 50 — Upload emits NodeUploading, 53 — Arrival and reconcile emit NodeReceived, and 54 — Parse and import stop the Parsed Set.

The Larger branch then treated one Changes checkbox and three Changes checkboxes as the same shape.

Replacement in Size:

Count the checkboxes under Changes.

One checkbox is one step.

More than one checkbox returns the ticket to slicing.

A sibling ticket stays outside that count.

Done: the count is 1 for 50 — Upload emits NodeUploading, 1 for 53 — Arrival and reconcile emit NodeReceived, and 3 for 54 — Parse and import stop the Parsed Set.

Size keeps this one home.

## 3. Name the readers of a removed op

Place: information access.

Severity: high.

Home: the implement list in [Cursor scope](plan/context/cursor-scope.md).

Add this fact:

[ ] Each op this ticket removes: every reader, and every guard or later op in the same change that needs the state the op writes, with file, function, and the public error text.

This session that fact is `Op.guardsUnparsedDocument` and `Op.applyAllowing` in [History.fs](src/Shared/History.fs), plus `Api.inferredNodeEvents`.

The named test then passed.

``Load of a newly Unparsed child Directory reconciles one level`` still expected `Current` and got `Unparsed`.

Done: the implement fill contains that line before the first edit.

## 4. The red result is one assertion failure

Place: no-ops.

Severity: medium.

Home: step 2 of [.agents/skills/tdd/SKILL.md](.agents/skills/tdd/SKILL.md).

"See it fail" matched compiler errors, and the shot continued.

The compiler errors were:

- `MailboxEventBody` and `placed` missing in `CoreMailboxApi.fs` and `CoreMailboxLoad.fs`
- a duplicate `postEvent` in `NodeEventExpandTests.fs`
- optional arguments in `CoreRuntime.fs`
- `parsedMark` mismatches in `ParseThread.fs`
- `parsedBoot`, `seedHome`, and `seedOwned` missing from `NodeEventExpandTests.fs`

Replacement Done line for step 2:

Done: the filter reports one assertion failure for that test, and the tree compiles.

A compiler error ends the cycle.

The next edit makes that test compile.

The edit after that assertion failure is the least production change for that one test.

## Left aside

- Context overload has no count over 50K in this session.
- The compiler already reported the duplicate `postEvent` and the missing helpers. Those names get no second checker.
- Source stays as the human left it. The owner is the human after a picked recut.
- The scope report stays absent. The next implement sends a new scope report when that path is empty, per Cursor scope.
- Git stays on dev.
