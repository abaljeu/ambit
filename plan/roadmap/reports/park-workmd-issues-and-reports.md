# Park WORK.md issues and reports

Did not implement product code. Did not write wiki how-to. Did not commit. Issue files are the home. No new WORK.md pointers.

## Wiki over-scope revert

[[plan/end-user-wiki/issues/01-describe-documents-from-any-connected-device.md]] is open again (no Answer, no done checklist). Restored [[plan/end-user-wiki/map.md]] Decisions / Not yet specified. Unchecked the Epic Required item on [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]. Deleted the mistaken how-to page and worker report. That issue was not on WORK.md.

## Deleted WORK.md line → issue

### Active

- [[plan/client-start-time/reports/edit-indent-old-text-mismatch.md]] → [[plan/client-start-time/issues/10-edit-indent-old-text-mismatch.md]] — new issue: verify/fix Editing `returnTo` + `adjustModeAfterServerApply` vs Poll/Load; HITL Tab CAS; links the report.
- [[plan/parse-load-demote/issues/01-keep-current-on-rediscovered-added.md]] → same — Comments: parked from Active; outcome already on the issue.

### Pending (existing issues)

- [[plan/roadmap/issues/07-chart-automatic-upload-and-download.md]] → same — Comments: chart auto-upload and remaining pointers for the current Chapter.
- [[plan/webview2-azure-origin/issues/01-webview2-navigate-azure-ambit.md]] — Comments: parked outcome (Navigate, cookie copy, `/_desktop`, no pretty-URL fetch). Status stays `ready-for-agent`. Rehomed from Roadmap `issues/` after this park.
- [[plan/marketing-wiki/issues/01-use-page-documents-from-any-connected-device.md]] → same — Comments: first use page.
- [[plan/expression-language/reports/ref-owned-children-impl.md]] → [[plan/expression-language/issues/32-ref-and-owned-children.md]] — Status `ready-for-human`; HITL checklist; report link.
- [[plan/expression-language/reports/text-ops-impl.md]] → [[plan/expression-language/issues/30-text-operations.md]] — Status `ready-for-human`; HITL checklist; Comments HITL steps.
- [[plan/expression-language/reports/if-impl.md]] → [[plan/expression-language/issues/31-if-pullback.md]] — Status `ready-for-human`; HITL checklist; report link.
- [[plan/expression-language/reports/outer-impl.md]] → [[plan/expression-language/issues/28-outer-prefix-combinator.md]] — Status `ready-for-human`; HITL checklist; See also the impl report.
- [[plan/expression-language/reports/re-filter.md]] → [[plan/expression-language/issues/29-re-and-rei-header-filters.md]] — Status `ready-for-human`; HITL checklist.
- [[plan/client-start-time/issues/09-cache-first-boot-delayed-lcp.md]] → same — See also delayed-LCP report; Comments HITL.
- [[plan/client-start-time/issues/08-poll-hash-fallback-loop.md]] → same — See also poll-hash report; HITL wording: Selection must not jump to ROOT.
- [[plan/selective-client-loading/issues/20-restore-saved-zoom-workspace-during-bootstrap.md]] → same — Status `ready-for-human`; HITL F5; artifacts `sessionTargets` / SessionState.
- [[plan/selective-client-loading/issues/21-load-one-selected-target-through-synchronization.md]] → same — HITL Load after stub-skip; artifacts WorkspaceUploadStructure.
- [[plan/selective-client-loading/issues/22-load-full-selection.md]] → same — Comments: parent spec.
- [[plan/selective-client-loading/issues/24-keep-navigation-and-find-resident-only.md]] → same — Comments: parent spec.
- [[plan/selective-client-loading/issues/25-guard-structural-commands-at-unloaded-boundaries.md]] → same — Comments: parent spec; MoveSelected in the guard.

### Pending (new issues from reports)

- [[plan/roadmap/reports/upload-dot-scratch-directory-stub.md]] → [[plan/roadmap/issues/15-hitl-empty-scratch-directory-stub.md]] — HITL empty `.scratch`; Load `.agents` stays Loaded; classify-batch report linked.
- [[plan/roadmap/reports/graph-only-reconcile-chunk.md]] → [[plan/roadmap/issues/16-hitl-cpanel-proxy-php-large-load.md]] — HITL upload [[proxy.php]]; large Load 200 not 400/502.
- [[plan/roadmap/reports/direct-api-vs-proxy.md]] (pretty URL) → [[plan/roadmap/issues/17-pretty-url-php-or-same-site-azure.md]] — keep PHP or chart same-site Azure hostname; WebView2 is [[plan/webview2-azure-origin/issues/01-webview2-navigate-azure-ambit.md]].
- [[plan/expression-language/reports/run-changes-not-effective.md]] → [[plan/expression-language/issues/34-hitl-run-error-strings-and-unfold.md]] — HITL hard-reload error strings and unfold.
- [[plan/expression-language/reports/run-commit-edit-before-exec.md]] → [[plan/expression-language/issues/35-hitl-run-commit-edit-before-exec.md]] — HITL commit then Run.
- [[plan/expression-language/reports/expr-eval-pull-enumerator-impl.md]] (50 hits) → [[plan/expression-language/issues/36-hitl-run-descendant-cap-50.md]] — HITL 50 Children and unfold.
- [[plan/expression-language/reports/expr-eval-pull-enumerator-impl.md]] (later paging) → [[plan/expression-language/issues/37-search-equals-stream-paging.md]] — SearchCursor / takeResults; no Run cap of 50.
- [[plan/client-start-time/reports/cold-load-loading-hang.md]] → [[plan/client-start-time/issues/11-hitl-cold-load-loading-hang.md]] — HITL cold load then warm F5; parent implement-cache-first-boot report.
- [[plan/client-start-time/reports/page-not-responding-loading.md]] → [[plan/client-start-time/issues/12-hitl-occurrence-fold-restore.md]] — HITL `f` restore, legacy `e`, Zoom fallback; artifact links.
- [[plan/client-start-time/reports/reload-state-reuse-investigation.md]] → [[plan/client-start-time/issues/13-revision-keyed-bootstrap-encode-cache.md]] — revision-keyed encode cache; Api.fs / ResidentProjection; parent state-further-optimization.
- [[plan/client-start-time/reports/bucket-3-post-state-work.md]] → [[plan/client-start-time/issues/14-defer-fold-restore-after-first-paint.md]] — defer fold restore; first paint collapsed; lower priority after 8 ms / 18 rows HITL.
- [[plan/selective-client-loading/reports/two-phase-state-loading-exploration.md]] → [[plan/selective-client-loading/issues/29-validate-two-phase-state-loading.md]] — spec-break, thin-id-list, \|V⁺\|, cache-first; parent spec.

### Blocked

- [[plan/selective-client-loading/issues/26-forbid-unloaded-destinations-in-move-dialog.md]] → same — Comments: Blocked by already recorded.
- [[plan/selective-client-loading/issues/27-document-delivered-selective-loading-baseline.md]] → same — Comments: Blocked by already recorded.
- [[plan/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]] → same — Comments: Blocked by already recorded.

## Kept on WORK.md

Skills, scripts, `map.md`, `spec.md`, `project.md`, `doc/`, `tmp/`, and other non-issue/non-report files. Blocked is empty.
