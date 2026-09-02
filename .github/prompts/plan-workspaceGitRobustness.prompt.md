# Plan: Harden workspace-git push (fast-forward vs. dirty-check) and diagnose remote 406

## TL;DR
Replace the push path's blanket "reject-dirty" 403 with the same JIT autosave-commit
already used before pull (`WorkspaceGit.jitCommitIfDirty` / `prepareWorkspacePull`), so
server-regenerated `.amb`/autosave file churn never blocks an Upload by itself. Git's
native fast-forward-only enforcement (`receive.denyNonFastForwards`) becomes the sole
gate for genuine divergence, surfaced as a normal git rejection instead of a custom
"dirty" 403. Separately, diagnose (don't yet fix blind) the 406 seen on the remote
cPanel -> proxy.php -> Azure path for `git-receive-pack` POSTs, and make sure git error
detail is never swallowed/truncated so future issues are self-diagnosing.

## Background / root-cause findings (from investigation)
- `GitGateway.assertPushAllowed`: `Ok None` (unborn repo) -> always allowed (Insert -> Connect
  -> Upload seed case already works this way). `Ok (Some _)` (born repo) -> calls
  `WorkspaceGit.assertCleanForWorkspacePush`, which hard-rejects (403) if `git status
  --porcelain` is non-empty. Nothing ever cleans that dirt automatically except:
  (a) `WorkspaceGit.jitCommitIfDirty`, called only from `prepareWorkspacePull` (pull path), or
  (b) legacy whole-DataDir `/ambit/save` (`GitSave.commitAll`), unrelated per-label repo.
- Practical effect: any actively-edited/born workspace has a dirty working tree almost
  continuously (DocumentPersistence rewrites artifacts on graph changes, uncommitted).
  Upload therefore fails with 403 unless a Download (pull) happened first (which JIT-commits
  and cleans the tree) -- this matches the docs' "typical workflow: commit -> download ->
  upload" but the error message/UX doesn't make this obvious, and it's surprising for a
  workspace that "should" be clean.
- IMPORTANT LIMITATION uncovered during planning: JIT-committing dirty content right before
  push does NOT by itself resolve genuine two-writer divergence. If the server's JIT commit
  introduces content the pushing client never pulled, the client's incoming ref no longer has
  the new HEAD as an ancestor -> git's own `denyNonFastForwards` correctly rejects it as
  non-fast-forward. That's fine/expected (real divergence must still block), but it means this
  fix mainly helps the "trivial artifact-only churn, no real divergence" case -- which is
  exactly the Insert/fresh-workspace and single-editor cases reported by the user.
- 406 report (remote/cPanel path only): `error: RPC failed; HTTP 406 curl 22 ... 406` on
  `send-pack`, i.e. the POST to `git-receive-pack` through `collaborative-systems.org` ->
  Apache `.htaccess` -> `proxy.php` (PHP+curl, buffers whole body/response, `CURLOPT_POSTFIELDS
  = file_get_contents('php://input')`) -> Azure backend. `proxy.php` does NOT forward
  `User-Agent` (forwardHeaders = Accept, Accept-Language, Accept-Encoding, Content-Type,
  Cookie, Authorization, X-Requested-With). Leading hypotheses: HostGator WAF/mod_security
  flagging the binary POST / missing or unusual headers, or Apache mod_negotiation -- not
  confirmed without a captured request trace. Do not guess-fix; diagnose first.

## Steps

### Phase 1 -- Push dirty-check -> JIT-commit parity with pull (depends on nothing)
1. `src/Server/WorkspaceGit.fs`: factor `jitCommitIfDirty` so both pull and push call sites
   can supply their own base commit message (e.g. add a `baseMsg` parameter, or a thin
   `jitCommitBeforeWorkspacePush` wrapper using message "gambol: autosave before
   workspace-push"). Remove `assertCleanForWorkspacePush` (now unused) -- call this out
   explicitly since it's being deleted, not just left as dead code.
2. `src/Server/GitGateway.fs`: change `assertPushAllowed` -- for the `Ok (Some _)` (born)
   branch, call flush() then the new JIT-commit-if-dirty (mirroring `prepareWorkspacePull`)
   instead of `assertCleanForWorkspacePush`. Drop the custom 403 "dirty" short-circuit for
   born-and-dirty; rely on stock git `receive.denyNonFastForwards` (already configured via
   `WorkspaceGit.ensurePushConfig`) to reject genuine divergence during the actual
   `git-receive-pack` exchange.
   - Both `handleInfoRefs` (GET info/refs, push branch) and `handlePackPost` (POST) currently
     call `assertPushAllowed` independently -- keep both calls (JIT-commit is idempotent /
     cheap when already clean) so `git push`'s two-request flow both see a consistent view.
3. *(depends on 1-2)* Verify `assertPushAllowed`'s signature needs `FlushFn`/client hint
   threaded in from `handleInfoRefs`/`handlePackPost` call sites (currently only
   `prepareWorkspacePull` receives `flush`/hint) -- wire flush + `clientHintOf ctx.Request`
   into the push branch too.

### Phase 2 -- Error-detail passthrough (parallel with Phase 1)
4. Confirm the non-fast-forward rejection text from stock git (e.g. `! [rejected] ...
   (non-fast-forward)`) reaches the client UI unmodified:
   - `src/Shared/dotnet/DesktopGit.fs` `errorDetail` / `filterGitErrorDetail` -- confirm no
     line-filtering strips this (current filter only drops the GCM "unencrypted HTTP" notice).
   - `src/Client/UpdateWorkspaceGit.fs` `gitPushOp` / `httpError` -- check
     `LogText.truncateForLog 200` doesn't cut off useful push-rejection detail; raise the
     limit for this path if needed.

### Phase 3 -- Tests (depends on Phase 1)
5. `tests/Server.Tests/GitGatewayTests.fs`: rewrite the now-invalid cases:
   - "GET info refs git-receive-pack rejects when dirty" -> new expectation: JIT-commits the
     dirty tree and returns 200 (not 403).
   - Remove/replace any `assertCleanForWorkspacePush` unit test in `WorkspaceGitTests.fs`.
   - Add: born workspace with only trivial autosave dirt -> push succeeds without a prior pull.
   - Add: born workspace where server has a real divergent JIT commit the pushing client never
     saw -> push is rejected as **non-fast-forward** (assert on git's own rejection wording,
     not a custom "dirty" message) -- documents the accepted limitation from Background above.

### Phase 4 -- Docs (depends on Phase 1 being settled)
6. `doc/roadmap/git-sync-gateway.md`: revise the locked "Dirty push policy" table row and the
   "Clean-tree enforcement (push)" section -- reject-dirty becomes JIT-commit-then-FF-only,
   consistent with pull. Update `doc/roadmap/workspaces-checklist.md`'s matching checklist line.

### Phase 5 -- 406 diagnostic (independent; remote/infra, not code-first)
7. Do NOT ship a blind fix. Capture a trace of the failing push (`GIT_CURL_VERBOSE=1 git push`
   client-side, plus HostGator/Apache error log and Azure log stream for the same request) to
   confirm whether the 406 originates at Apache/mod_security, PHP (`proxy.php`), or Azure/Kestrel.
   - Cross-check: does the same push succeed when targeting the `*.azurewebsites.net` host
     directly (bypassing `proxy.php`/cPanel)? That isolates proxy-layer vs. backend-layer.
   - `proxy.php` currently does not forward `User-Agent` -- note as a candidate variable to
     test (add it to `$forwardHeaders` as a trial) once a trace points at header-based WAF
     filtering.
8. Once root cause is confirmed, pick a mitigation with the user (do not pick blindly):
   (a) allowlist git's traffic pattern in HostGator's WAF/ModSecurity panel if exposed,
   (b) carve out `/ambit/git/*` from whatever WAF layer is intercepting,
   (c) document/default the desktop git remote to the direct Azure hostname for workspace
   git specifically (bypass cPanel proxy for this one traffic type), updating
   `WorkspaceGitRemote.remoteUrl` usage/config guidance accordingly.

## Relevant files
- `src/Server/WorkspaceGit.fs` -- `jitCommitIfDirty`, `assertCleanForWorkspacePush` (remove), `tryHead`
- `src/Server/GitGateway.fs` -- `assertPushAllowed`, `prepareWorkspacePull` (pattern to mirror), `handleInfoRefs`, `handlePackPost`
- `src/Shared/dotnet/DesktopGit.fs` -- `push`, `errorDetail`, `filterGitErrorDetail`
- `src/Client/UpdateWorkspaceGit.fs` -- `gitPushOp`, `httpError` / `LogText.truncateForLog` limit
- `proxy.php` -- `$forwardHeaders` (User-Agent trial), no change until 406 root cause confirmed
- `tests/Server.Tests/GitGatewayTests.fs` -- rewrite dirty-push cases, add FF/non-FF cases
- `tests/Server.Tests/WorkspaceGitTests.fs` -- remove/replace `assertCleanForWorkspacePush` test
- `doc/roadmap/git-sync-gateway.md`, `doc/roadmap/workspaces-checklist.md` -- update locked decision

## Verification
1. `dotnet build src/Server` + `dotnet test` (or `scripts/test.sh`) -- GitGatewayTests pass with rewritten cases.
2. Manual repro (localhost): Insert workspace -> Connect -> edit a node (dirty autosave) -> Upload -> succeeds without a prior Download.
3. Manual repro of real divergence: two clients editing the same workspace; second Upload after the first's changes landed on server surfaces a clear **non-fast-forward** message (not the old "dirty" 403) and still requires pull-first.
4. For 406: capture `GIT_CURL_VERBOSE=1` trace + server logs on a failing remote push; confirm layer (Apache/WAF vs. PHP proxy vs. Azure) before choosing a mitigation.

## Decisions
- JIT-commit-before-push only fixes the "artifact-only churn, no real divergence" case;
  genuine two-writer divergence still correctly blocks the push, now surfaced as a standard
  git non-fast-forward rejection instead of a custom dirty-tree 403. User has directed this
  tradeoff (JIT-then-FF, matching pull's existing precedent) rather than inventing auto-merge.
- Upload must NOT auto-run pull/merge on non-fast-forward. Keep the flow manual and return a
  clear, actionable message that explicitly tells the user to pull first, resolve locally,
  then retry Upload.
- Unborn-repo seed-push behavior (Insert -> Connect -> Upload) is unchanged -- already skips
  the dirty check entirely; this fix targets already-born/actively-used workspaces.
- 406 fix is diagnostic-first; no infra/proxy change ships until the actual cause (WAF vs.
  mod_negotiation vs. header stripping vs. PHP body handling) is confirmed via a captured trace.
- If diagnostics confirm the cPanel/PHP proxy is the culprit, using direct Azure git endpoints
  (bypassing the proxy for git traffic) is an acceptable mitigation path.
- Scope (per user): both concrete bugs (403 dirty-race / FF policy, 406 proxy) plus this
  broader robustness pass (error-detail passthrough, tests, docs) -- confirmed in this session.

## Further considerations
1. Decided: no auto-pull/merge fallback. On non-fast-forward, Upload must fail fast with a
  clear instruction to pull first (Download), resolve locally, then retry Upload.
2. Accepted direction: if 406 is proxy-induced, defaulting workspace git remotes to direct
  Azure git endpoints (bypassing cPanel proxy for git RPC) is acceptable.

## Phase 6 (independent side issue) -- async git commands + live pending indicator
Already fully specced in `doc/roadmap/desktop-git-async.md` (Status: Planned, not yet
implemented) -- do not re-derive, execute that doc's slices.

- Root cause: `src/Client/UpdateWorkspaceGit.fs` git ops (`gitPullOp`, `gitPushOp`,
  `gitStatusOp`, `gitConnectOp`, `cloneAtPath`, `parseOrPushOp`'s push branch) call
  `src/Client/JsInterop.fs` `postJsonSync` / `getJsonSync` / `putJsonSync` (synchronous
  blocking XHR). This freezes Fable's single JS thread for the whole round trip (including
  the desktop-side blocking `git` subprocess), so no re-render -- and therefore no "pending"
  status -- is possible until the call returns. This is exactly the reported symptom (no
  indication anything is happening until the command returns).
- Fix direction (per the doc): reuse the existing Effect -> async fetch -> SysMsg MVU shell
  already used by graph sync (`SubmitPendingBatch`/`SubmitResponse`) and desktop file-status
  polling -- NOT a new ad-hoc queue field. Add a Shared `GitRemotePlanner` (shaped like
  `src/Shared/SyncPlanner.fs`) for FIFO serial execution, and a `gitCommandPending` (or
  `gitRemoteState`) display-only field on `VM` so `renderDiagnostics` (`src/Client/View.fs`)
  can show a pending pill (`Git Pull to Desktop: pulling…`, CSS `amb-last-result-pending`)
  before the result arrives, then replace it with `Detail`/`Error` on completion.
- Slices (execute in order, each independently shippable):
  1. **Slice 1 -- Pull**: add `GitRemotePlanner`/`DesktopGitOp`/`RequestDesktopGitOp`/
     `DesktopGitOpDone`/`gitCommandPending`; refactor `gitPullOp` through the planner; move
     HTTP to an async runner in `src/Client/App.fs`; update `renderDiagnostics` to show
     pending over `lastCmdResult`. This is the minimum slice that fixes the reported symptom.
  2. **Slice 2 -- Push + Status**: extend to `gitPushOp`/`gitStatusOp`/`parseOrPushOp`'s push
     branch; align Push success message to `label → path` (replacing today's `pushed: <detail>`).
  3. **Slice 3 -- Connect + Clone**: multi-step chains; folder picker may stay sync (native
     dialog blocks the desktop thread anyway) but pending starts once a path is chosen.
  4. **Slice 4 -- Parse**: async the file-read branch of Parse/Upload.
- Tests per doc: `tests/Shared.Tests/ViewModelCmdLastResultTests.fs` (pending display format,
  precedence over stale `lastCmdResult`, Push success case), new/extended Shared planner tests
  (enqueue-while-in-flight appends tail, `onDone` dequeues + returns next effect). No Server
  tests expected; `DesktopGitTests.fs` unaffected (desktop subprocess logic untouched).
- Review checkpoints called out in the doc (stop and re-check if hit): pending state leaking
  into `#sync-status` or row indicators; a bare `gitCommandQueue` list reappearing on `VM`
  instead of Shared planner state; desktop endpoints growing job polling/WebSocket progress;
  Slice 3 trying to make the native folder dialog itself async.
- Relationship to Phases 1-5 above: fully independent -- can be implemented in parallel,
  touches different files (`UpdateWorkspaceGit.fs`, `App.fs`, `View.fs`, a new Shared planner
  module) than the push fast-forward/dirty-check fix (`GitGateway.fs`, `WorkspaceGit.fs`).
  Slice 1/2 will also make the improved push error messages (Phase 2 above) visibly async
  rather than freeze-then-show, so sequencing Phase 1-2 before Phase 6 Slice 2 is convenient
  but not required.
</content>
</invoke>
