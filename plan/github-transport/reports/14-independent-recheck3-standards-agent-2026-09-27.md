# 14 — Route Load and Save by path pre-pick — Standards recheck 3

Range: commit `4f01a5c7` (`git diff 4f01a5c7^...HEAD`). Scan line `UpdateSave.fs::runDeskSaveWith` 26 lines does not break the 40-line function limit in [fsharp-source](.agents/rules/fsharp-source.md).

## (a) Documented-standard violations

### Hard — unused binding

[LoadSaveCommandClientTests.fs](tests/Server.Tests/LoadSaveCommandClientTests.fs) `DeskHttpHandler.Handle` binds `body` from `ReadAsStringAsync().Result` and never uses it. [core-agent-behavior](.agents/rules/core-agent-behavior.md) Surgical Changes: remove variables that this change made unused.

## (b) Smells (judgment calls)

### Duplicated Code

[LoadSaveCommandClientTests.fs](tests/Server.Tests/LoadSaveCommandClientTests.fs) lists the same three paths twice — once for the body, once for the status:

```
| "/_desktop/workspace-inventory" ->
    """{"mode":"Full","items":[]}"""
| "/_desktop/workspace-push" ->
    """{"ok":true,"uploaded":0,"downloaded":0,"detail":"pushed","error":null}"""
| "/ambit/save" ->
    """{"ok":true,"detail":"saved","error":null}"""
| _ -> """{"error":"unexpected path"}"""
let status =
    if path = "/_desktop/workspace-inventory"
       || path = "/_desktop/workspace-push"
       || path = "/ambit/save" then
        HttpStatusCode.OK
```

Return body and status from one match.

No other smell from [SMELLS.md](.agents/skills/code-review/SMELLS.md) is strong enough to stand behind. Product F# stays under line, function, and file limits. `try/with` and `ResizeArray` in this test match existing Server test HTTP doubles; not counted as a hard [fsharp-source](.agents/rules/fsharp-source.md) break.
