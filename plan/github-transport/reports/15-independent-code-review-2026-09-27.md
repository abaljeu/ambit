# 15 — Keep the App outside Peer Actor hosting — independent code review

Review range: `3cde2784...0a532cb1`.

## 1. Landing decision

**Fail.** The current production wiring keeps GitHub transport on Server, but this ticket is a proof increment and the added proofs do not close the full Server-door and App-host boundary requirements. Leave [15 — Keep the App outside Peer Actor hosting](../issues/15-keep-app-outside-peer-actor-hosting.md) at `coded`.

## 2. Must-fix findings

1. **Prove each Git route at the Server integration seam.** The requirement says, “Tests or repository-boundary checks prove every git choice crosses the Server request door.” The new `Git Load and Save cross the Server command request door` test replaces `post` with a local function. It proves that explicit Git Load and Git Save encode the correct URL and request, but it does not prove that Server receives either request or starts the Peer Actor. Existing `LoadSaveCommandTests` prove explicit Git Save and plain-to-Git Load through `Api.postLoadSaveCommand` and `CoreMailbox.startLoadSaveCommand`; they do not prove explicit Git Load or plain-to-Git Save at that seam. Add a Server integration matrix for Load and Save with explicit Git and plain resolved to Git.
2. **Make the no-App-host boundary check cover the App compile surface.** The requirement says, “Tests or repository-boundary checks prove … no App Actor host is added.” `appSourceFiles` uses `SearchOption.TopDirectoryOnly`, so a nested or linked compiled `.fs` file is invisible. The project check only forbids an exact `Gambol.Server.fsproj` reference, and the source check only forbids selected spellings. A nested App Actor host, another host dependency, or renamed remote/branch/credential state can pass. Resolve and scan every `<Compile Include>` for the Browser and App projects, including linked and nested files, and check the relevant project dependency boundary.

## 3. Should-fix findings

1. **Follow the F# no-exception rule in new tests.** New paths use `failwith` and `Result.defaultWith failwith`. This conflicts with `.agents/rules/fsharp-source.md`: “Don't use Exceptions. Use Error types.” Use xUnit failure assertions or result-returning helpers.
2. **Remove duplicated operation decoding in the test.** The same `"load"` versus `"save"` conversion appears twice in `LoadSaveCommandClientTests.fs`. This is a small duplicated-code smell and makes the intended route matrix less direct.
3. **Strengthen the state proof beyond a spelling blacklist.** The current blacklist catches `remoteMap`, `trackedBranch`, and two credential names, but equivalent state can use other names. The PR adds no product state, so current behavior is compliant; however, this test alone does not prove the durable architectural boundary it claims.

## 4. Good findings

1. **Current behavior keeps the App outside Peer Actor hosting.** Browser commands emit `SubmitLoadSaveCommand`; `LoadSaveCommandClient` posts `/ambit/load-save-command`; Server registers `GithubTransportActor` and routes the request through `CoreMailbox.startLoadSaveCommand`. No product code changed in this PR.
2. **The command surface proof is direct.** The six command cases prove Load, Save, git Load, git Save, desk Load, and desk Save preserve their `Plain`, `Git`, or `Desk` pre-pick.
3. **The desk path proof exercises behavior.** Desk Load continues to `/_desktop/workspace-push`, and desk Save continues to `/ambit/save`; this is stronger than a source-name scan.
4. **The focused and Server tests pass.** The review run passed 16 focused boundary/routing tests and all 569 Server tests. The required mechanical standards scan produced no size or naming findings.
5. **The PR body classifies the Shared failure correctly.** `AmbDocumentTests.read ambiguous owner-link candidates keeps map order` fails in the full suite and in isolation. Neither `tests/Shared.Tests` nor `src/Shared` differs from `3cde2784`, and the same test exists at the fixed point. It is pre-existing and out of scope for this ticket. It does not repair the proof gaps above, and it should not be charged as a regression from this PR.

## 5. Acceptance verdict

1. **App does not construct or run `GithubTransportActor`; Load and Save go through Server: partial.** Current behavior passes; the added regression proof is incomplete.
2. **App does not invoke `GitRun` for GitHub pull or push: pass for current behavior.** The App only uses `DesktopGit.isAvailable` as an existing capability probe; Server owns pull and push.
3. **App adds no remote map, tracked-branch map, or GitHub credential state: pass for this diff, weak regression proof.** The PR adds no product state, but the blacklist is not a complete boundary.
4. **Command surface exposes Load and Save with pre-picks; desk WebDAV remains: pass.**
5. **Proof that every Git choice crosses the Server request door and no App Actor host exists: fail.** This is the landing blocker.

## 6. Summary

Standards: one hard no-exception violation and one duplicated-code judgment call. Spec: two must-fix proof gaps. Worst Standards issue: new exception-based test helpers. Worst Spec issue: the tests do not prove every Git route through the real Server request seam. Overall: **Fail for landing.**
