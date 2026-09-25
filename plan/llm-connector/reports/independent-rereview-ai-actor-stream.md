# Independent re-review — AI Actor stream

Pin: base `46f6edb4` ([17 — CloudAgents Console stream](plan/llm-connector/issues/17-cloudagents-console-stream.md) tip — “cloud agent streaming”) to tip `3e1e2f5f`. Diff: `git diff 46f6edb4...3e1e2f5f`. Log: `git log 46f6edb4..3e1e2f5f --oneline` — `1c523991 Stream Focus children from CloudAgents with pending-buffer addChild`; `b4fa1ccb Add independent review of AI Actor stream`; `f212d92d Pass StreamArgs into streamUntilDone`; `3e1e2f5f Remove CloudAgents poll client`. Do not use `origin/staging` three-dot; staging has moved past this branch base.

Ticket: [18 — AI Actor stream](plan/llm-connector/issues/18-ai-actor-stream.md). Status stays `coded`. A report is not approval.

Axis reports: [Standards](code-review-standards-rereview-ai-actor-stream.md), [Spec](code-review-spec-rereview-ai-actor-stream.md).

## Scan

`python3 .agents/skills/code-review/scripts/standards-scan.py --diff 46f6edb4` from the tip checkout. The script printed only `--- measure-fs-size ---` bindings. Every printed function is under 40 lines. No LONG, TAB, MUTABLE, BARE_ID, BLANK_BLANK, or FILE lines. Count a printed line as a finding only when it breaks the cited limit in [fsharp-source.md](.agents/rules/fsharp-source.md). Surgical under-100-line preference is not a script fail.

Prior first-review items re-checked on this pin (not findings unless still true below): `streamUntilDone` now takes typed [StreamArgs](src/CloudAgents/PublicTypes.fs) — cleared. Public CloudAgents `poll` / `waitUntilComplete`, live wait-poll, fake wait twins, and Cursor GET-run-status are gone; completion is `streamUntilComplete` / `streamFake` — cleared. `rememberedChildren` / `nextChildren` Focus-miss arms were not raised again.

## Standards

Range: `git diff 46f6edb4...3e1e2f5f` (`3e1e2f5f`). Scan printed no LONG / TAB / MUTABLE / BARE_ID / BLANK_BLANK / FILE lines. Printed bindings stay under 40 lines. Surgical under-100-line preference is not a script fail ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

### Hard violations

#### 1. One-off `(Draft * PlannedAdd list)` next to `Step`

[fsharp-source.md](.agents/rules/fsharp-source.md): group related values into a named reused type; reuse a type that already exists; do not invent a one-off tuple.

[FocusXmlStream](src/Shared/documents/FocusXmlStream.fs) already has `Step = { draft; adds }`. `stepToken` still takes a tuple, and `commitPending` / `addLeaf` / `apply` / `flush` still return or fold that tuple:

```
    let private stepToken
        (newId: unit -> NodeId)
        (draft: Draft, adds: PlannedAdd list)
        token
```

```
        let draft, adds =
            List.fold
                (fun acc token -> stepToken newId acc token)
                (draft, [])
                tokens
        { draft = draft
          adds = List.rev adds }
```

#### 2. Test member is 49 lines

[fsharp-source.md](.agents/rules/fsharp-source.md): 40 lines or less per function. That limit still applies to tests.

[AgentRunnerFakeTests](tests/CloudAgents.Tests/AgentRunnerFakeTests.fs) member `partial fake stream waits until cancel` is lines 195–243 (49 lines).

### Judgement-call smells

#### Possible Mysterious Name — `Draft.hold`

[SMELLS.md](.agents/skills/code-review/SMELLS.md): a name that does not reveal what the field holds.

```
    type Draft =
        { hold: string
          pending: Pending option
          parents: Frame list }
```

`hold` is the unparsed remainder (partial tag bytes until `>`). The name does not say that.

## Spec

No spec-axis findings versus [18 — AI Actor stream](plan/llm-connector/issues/18-ai-actor-stream.md) on pin `46f6edb4...3e1e2f5f`.

## Summary

Standards: 3 findings (2 hard, 1 smell); worst: one-off `(Draft * PlannedAdd list)` beside `Step` in [FocusXmlStream](src/Shared/documents/FocusXmlStream.fs). Spec: 0 findings.
