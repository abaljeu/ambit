# Spec review: 53 — Cancel HTTP conveys ActorStop

Range: `git diff origin/staging...HEAD` (three-dot). Tip `HEAD` `0a9d294bc5a020e6352d8c1198554fcee8e0e696`. Base `origin/staging` `48c7a99ccbaf9b6bc6f78744ae817d27dc5c1838`. Spec: [53 — Cancel HTTP conveys ActorStop](plan/core-creation/issues/53-cancel-http-conveys-actorstop.md). Ticket **Status:** stays `coded`.

## a. Missing or partial

None against What to build.

Quote: “Cancel HTTP success carries Events the same way Command carries `ActorStart`: at least the Cancelled `ActorStop` for that Focus, encoded as universal `{ nodes; events; latestId }`.” [Api.fs](src/Server/Api.fs) `postCancel` returns that envelope after `cancelByFocus`. [CancelByFocusTests](tests/Server.Tests/CancelByFocusTests.fs) Adapter cancel-while-live finds Cancelled `ActorStop` for the Focus. `nodes` is `[]`; Command still fills request graph nodes.

Quote: “Client applies those Events (`applyCommandEvents` / equivalent) so `actorLiveFocusIds` drops and chrome + Cancelled (or Error) result update without waiting on Poll.” [App.fs](src/Client/App.fs) `runSubmitCancel` dispatches `CommandDone`, same as Command. That path calls `applyCommandEvents`. [ActorLiveTests](tests/Shared.Tests/ActorLiveTests.fs) apply Cancelled `ActorStop` through `applyServerTail`: the Focus leaves `actorLiveFocusIds`, and `lastCmdResult` is Error `"Actor cancelled."`. [RowView.fs](src/Client/RowView.fs) adds `amb-actor-live` only from that set.

Quote: “Proof: Cancel while live → chrome off and Cancelled (or Error) result from the Cancel response Events. Server still cancels CTS / refuses later Actor output.” Adapter test keeps `waitFakeCancelled`. Shared apply covers chrome/result from those Events. Duplicate completion after cancel stays. Proof is split (HTTP Events, Shared apply, CTS), same shape as Command `ActorStart`.

Quote: “After cancel `finish` drops the live row, a later Actor-body `actorStop` does not emit `ActorStop` for `Graph.rootId` or any other Focus that was not live.” / “Proof: cancel then body stop → one Cancelled `ActorStop` for the Focus; no root stop.” [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `dispatchActorStop` does not default `getFocusId` to `Graph.rootId`. New test: one Focus stop, zero root stops.

Non-goals hold: no Undo, no XML AI pack / Focus cssClass, no Poll protocol change.

## b. Scope creep

Cancel JSON now requires `eventId` so the Server can return the tail. Quote: “the same way Command carries `ActorStart`.” That cursor is the Command pattern, not extra product behaviour.

The three-dot range also contains work [53 — Cancel HTTP conveys ActorStop](plan/core-creation/issues/53-cancel-http-conveys-actorstop.md) did not ask for:

- [.agents/skills/code-review/SKILL.md](.agents/skills/code-review/SKILL.md) wording
- [src/Server/appsettings.Development.json](src/Server/appsettings.Development.json) `AiKeys` `ApiKey` fill (value not quoted)
- [plan/core-creation/reports/changed-files-directional-links.md](plan/core-creation/reports/changed-files-directional-links.md)
- merge commits `6e6a054f`, `6d8bb09d`, `53f1ffee`
- `8349a803` llm connection

## c. Implemented but wrong

None. `postCancel` still calls `cancelByFocus` (CTS `finish`) before it reads Events. Client apply is the Command `CommandDone` path.

Spec verdict: Approve with nits. a=0 missing, b=5 extra/noise, c=0 wrong. Worst within Spec: none. Worst in range: Development `ApiKey` fill (unrelated).
