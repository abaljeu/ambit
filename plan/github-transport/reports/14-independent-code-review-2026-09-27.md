# Independent code review — [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md)

## 1. Verdict — Must-fix

The production route matches the ticket, but the PR does not meet the required test matrix and one new test breaks the F# function-size rule. This review is not approval. [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md) correctly stays `coded`.

## 2. Good — Behavior and scope

1. **2.1. Sound pre-pick encoding** — [LoadSaveCommand.fs](src/Shared/LoadSaveCommand.fs), [PathPick.fs](src/Shared/PathPick.fs), and [EventJson.fs](src/Shared/EventJson.fs) carry `Plain | Git | Desk` on one Load/Save request. [Commands.fs](src/Client/Commands.fs) keeps `CommandId.Load` and `CommandId.Save` as the primary IDs and changes only the palette name and pre-pick for `git Load`, `desk Load`, `git Save`, and `desk Save`.
2. **2.2. Correct path choice** — [LoadSaveRouting.fs](src/Server/LoadSaveRouting.fs) calls `WorkspaceGit.remoteExists` only inside the `Plain` chooser. Explicit `Git` and `Desk` return before that callback runs.
3. **2.3. Correct command door** — [RouteRegistration.fs](src/Server/RouteRegistration.fs), [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs), [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs), and [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) carry the request through mailbox → actor pool. Git starts the operation-specific Server Peer Actor with the request Focus. Desk returns without starting an Actor.
4. **2.4. Preserved transport behavior** — [LoadSaveCommandClient.fs](src/Client/LoadSaveCommandClient.fs) dispatches Desk Load and Save to the existing `deskLoadOp` and `deskSaveOp`. Git Load runs pull → Server workspace reconciliation in [GithubTransportActor.fs](src/Server/GithubTransportActor.fs), and the Browser's existing Poll receives those Events. Git Save remains commit then push.
5. **2.5. Scope is controlled** — The diff adds no App git host, no Run or `?git` entrance, and no automatic schedule, post-Persist, or post-Download pull/push. It makes no ticket 15 App-hosting claim.
6. **2.6. Honest lifecycle fields** — The ticket Status is `coded`, the Project Stage remains `build`, and the architecture leaves the later selection-scoped Parse items unchecked.

## 3. Must-fix — Required proof and standards

1. **3.1. Desk preservation has no behavior test** — [LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs) proves only that a Desk response does not start a Peer Actor. It does not pass that response through [LoadSaveCommandClient.fs](src/Client/LoadSaveCommandClient.fs) and prove Desk Load reaches WebDAV / workspace push and Desk Save reaches the existing `/{file}/save` path. This misses review criterion 3 and ticket item 1.3.3 for both operations.
2. **3.2. The routed Git matrix is incomplete** — The only new mailbox → actor-pool test sends Git Load to a stopping stub. [GithubTransportActorTests.fs](tests/Server.Tests/GithubTransportActorTests.fs) separately proves direct Actor Load continuation and direct Actor Save, but no test proves Git Save through the new command-request door or proves that a routed Git Load reaches the real pull → reconciliation path and subsequent Poll-visible Events. The required Peer Actor and Parse-hop coverage is therefore compositional rather than end-to-end across the new seam.
3. **3.3. New test function exceeds 40 lines** — `Git request reaches Peer Actor through mailbox and actor pool` in [LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs) spans lines 116–170, 55 lines. This breaks the “40 lines or less per function” rule in [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md). The test exemption applies to the file-size rule, not the function-size rule.

## 4. Nice-to-have — Narrower quality improvements

1. **4.1. Test the production remote-fact adapter** — [PathPickTests.fs](tests/Shared.Tests/PathPickTests.fs) proves pure choice and explicit bypass, and existing WorkspaceGit tests prove remote detection. A focused [LoadSaveRouting.fs](src/Server/LoadSaveRouting.fs) test would also prove Focus → Workspace root → `remoteExists` → PathPick wiring.
2. **4.2. Remove the duplicated selection guard** — `deskLoadOp` and `loadOpFor` in [UpdateWorkspaceLoad.fs](src/Client/UpdateWorkspaceLoad.fs) repeat `selectedLoadTargetIds`, `selectionSpansMultipleWorkspaces`, and the same error. This is a possible Duplicated Code smell under [.agents/skills/code-review/SMELLS.md](.agents/skills/code-review/SMELLS.md).
3. **4.3. Normalize the added CRLF hunk** — `git diff --check origin/staging...HEAD` reports all 14 added lines of `startLoadSaveCommand` in [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) as trailing whitespace. The file already uses CRLF and the repository has no explicit EOL policy, so this is hygiene rather than a hard standards violation.

## 5. Verification — Executed evidence

1. **5.1. Mechanical standards scan** — `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` passed. Changed production functions are at or below 40 lines; `runDeskLoadAction` is exactly 40. No added source line exceeds 100 characters.
2. **5.2. Focused tests** — PathPick passed 6/6. Load/Save routing plus Server Peer Actor passed 9/9.
3. **5.3. Client build** — Fable compilation and the esbuild bundle passed.
4. **5.4. Full solution gate** — CloudAgents passed 62/62 and Server passed 555/555. Shared passed 1726, skipped 1, and failed 1 in unchanged `AmbDocumentTests.read ambiguous owner-link candidates keeps map order`; this is outside the PR diff and matches the PR description.

## 6. Summary — Finding counts

Standards: 1 Must-fix and 2 Nice-to-have. Spec: 2 Must-fix and 1 Nice-to-have. Worst Standards issue: the new 55-line test function. Worst Spec issue: the required Desk-preservation and routed Git completion proof is incomplete.
