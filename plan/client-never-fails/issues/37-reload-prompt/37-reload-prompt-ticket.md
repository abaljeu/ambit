# 37 — Reload prompt
**Status**: 
One job: The outliner never sees a reload prompt for server data, and still sees New version available when the protocol changes. The stories Never reload for server data and Know about a new version in [Client never fails on server data](plan/client-never-fails/spec.md) are the reason.

Type: coding
Blockers: [36 — Edit during a Load](plan/client-never-fails/issues/36-edit-during-load/36-edit-during-load-ticket.md)

## Inputs

### Working

[ ] `StatusView` matches `DataOutdated`, `CodeOutdated`, and `ServerRejected` in `src/Client/StatusView.fs`.
[ ] `Overlays` matches the same three states in `src/Client/Overlays.fs`.
[ ] The status sentence for `DataOutdated` is `Data changed on server — click to reload`. The alert title is `View is out of date`.
[ ] The `CodeOutdated` status sentence is `New version available — click to reload`. The alert title is `New version available`.
[ ] `ServerRejected` has its own arm in both modules.
[ ] No test file contains those sentences. They live in the two view modules.
[ ] No existing fact asserts the reload sentences.
[ ] The `CodeOutdated` arms are the copy for the sentences this ticket keeps.
[ ] [[src/Client/StatusView.fs]] — status line.
[ ] [[src/Client/Overlays.fs]] — blocking alert.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/37-reload-prompt-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `ServerRejected`.
[ ] Closed: the `CodeOutdated` sentences `New version available` and `New version available — click to reload`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `src/Client/StatusView.fs` so the `DataOutdated` arm and the sentence `Data changed on server — click to reload` are gone. Keep `ServerRejected`. Keep the `CodeOutdated` sentence `New version available — click to reload`.
3. [ ] Change `src/Client/Overlays.fs` so the `DataOutdated` arm and the title `View is out of date` are gone. Keep `ServerRejected`. Keep the `CodeOutdated` title `New version available`.

## Outputs

[ ] `src/Client/StatusView.fs`
[ ] `src/Client/Overlays.fs`

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/37-reload-prompt-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: no test name. The sentences live in `src/Client/StatusView.fs` and `src/Client/Overlays.fs`.
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/37-reload-prompt-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
