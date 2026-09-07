# Code review: 25 (Bind Changes at the Core seam)

Date: 2026-09-06

Independent review of commit `eba2f53` on `dev` versus parent `bd3f9c23`. Working tree has no extra 25 product files. Spec: [[../issues/25-bind-changes-at-core-seam.md|25 (Bind Changes at the Core seam)]], [[../project.md]], [[../map.md]], [[../mitigations.md]], [[../issues/23-close-core-object-seam.md|23 (Close Core object seam)]], [[../issues/24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]]. Standards: [[.cursor/rules/fsharp-source.mdc]], [[.cursor/rules/testing-workflow.mdc]], [[.cursor/rules/core-agent-behavior.mdc]], [[.cursor/rules/core-api.mdc]], [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]], Fowler smell baseline.

## Parent

**pass.** [[../issues/25-bind-changes-at-core-seam.md|25 (Bind Changes at the Core seam)]] is met. Parse HTTP leftover (`parseBound` / [[src/Server/Api.fs]] `postParseFile` still take credentials and `parseCredential`) is **out of 25**. Three findings (two Standards judgement, one Spec partial). No hard standard breach. No product-seam miss.

## Standards

Size is clean ([[.cursor/rules/fsharp-source.mdc]]): no over-40 bindings, no added long lines. No Core-level `postChange` facade: [[src/Server/Core/CoreRuntime.fs]] adds nested `bindChanges` / `browserChanges` that return a Changes handle ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]). `CoreAuth.bindHandle` still wraps only `postChange` / `postGraphOnlyChange`; that wrap is pre-existing CoreAuth, not this commit. `parseBound` still unpacks credentials; that leftover is Parse, not 25. HTTP Adapter tests that still call `CoreAuth.bindHandle` bind a recording stub; `CoreRuntime.bindChanges` always wraps live `changes()`, so that unpack is not Feature Envy.

**1 (Mysterious Name)** — judgement (Fowler Mysterious Name; own-mess cleanup in [[.cursor/rules/core-agent-behavior.mdc]]). [[tests/Server.Tests/CoreCredentialsTests.fs]] ``Adapter cookie fail and inactive sender are the same refuse family``: after `Api.postChange` dropped sender, the body posts a stub that already returns `CoreAuth.refuse`. No inactive Credential. The name still says inactive sender.

**2 (Middle Man)** — judgement ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]] “Keep the nesting visible at the call site”; [[.cursor/rules/core-agent-behavior.mdc]] no single-use abstraction). Hunk in [[src/Server/RouteRegistration.fs]]:

```
let private changesBound (persistence: PersistenceContext) =
    persistence.Core.browserChanges ()
```

Used once. Hides nested `Core.browserChanges` behind an Adapter helper. Matching `parseBound` helper shape is style, not required.

No hard documented-standard breaches. Two judgement smells.

## Spec

[[../issues/25-bind-changes-at-core-seam.md|25 (Bind Changes at the Core seam)]] is met.

Parse HTTP leftover (`parseBound` / `Api.postParseFile` still take credentials and `parseCredential`): **out of 25**. Spec: “same pattern as `parseBound`” — Parse is the pattern to copy, not the leftover. The leftover is `/ambit/changes` after [[../issues/23-close-core-object-seam.md|23 (Close Core object seam)]].

HTTP Adapter tests that still construct `CoreAuth.bindHandle` rather than `runtime.browserChanges()`: **do not fail** “Callers do not unpack CoreRuntime into admission primitives.” Those tests never hold a CoreRuntime. Production `/ambit/changes` posts `core.browserChanges()`. CoreRuntimeTests admit a live Browser via `browserChanges` and auth-refuse an inactive sender via `bindChanges`.

### (a) Missing or partial

**1 (Tests hit a test-built bind)** — partial. Spec: “Tests hit that bound interface: a live Browser credential is admitted; an inactive sender is auth-refused.” HTTP Adapter tests still assemble `CoreAuth.bindHandle` from a credentials set and a recording handle. They hit a test-built bind, not the Core-nested handle. The two admission facts on Core are covered.

### (b) Scope creep

None. Diff does not start [[../issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]], [[../issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]], Graph-only chunking, Browser dual Change POST, [[../issues/17-cancel-a-job.md|17 (Cancel a job)]], or [[../issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]]. No Core-level `postChange` facade.

### (c) Looks implemented, looks wrong

None on the product seam. Spec: “Decode JSON and map HTTP status only. Admission stays behind the bound Changes interface.” [[src/Server/Api.fs]] `postChange` does that. Change-applying methods `postChange` / `postGraphOnlyChange` only — `CoreAuth.bindHandle` wraps those two; other handle methods pass through. Nested `browserChanges` uses the process-lifetime Browser Credential.

## Summary

Standards: 2 findings (both judgement). Worst in Standards: **1 (Mysterious Name)** on the refuse-family test. Spec: 1 finding (tests partial). Worst in Spec: **1 (Tests hit a test-built bind)** on HTTP Adapter tests.
