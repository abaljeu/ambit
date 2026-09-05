# Place Core Changes in the existing projects

Type: grilling
Status: resolved
Actual: 40m
Blocked by: 03, 04

## Question

Which folders and modules hold the Shared apply implementation and Server Core Changes path, in what compile order and dependency direction, which mailbox owns sequencing and authoritative Server Graph publication, and how does it delegate the current file, database, and mirror persistence modes without adding an fsproj?

## Answer

- Round one extracts the full current Graph-agent package into a new Core module under [[src/Server/Core/]] in the existing Server project: typed current State, Revision, History tail through getChangesSince, readiness, normal Post Change, and Parse-originated Graph-only Post Change. The exact filename and compile insertion are implementation details. Do not add an fsproj.
- Keep and reuse existing Shared modules such as [[src/Shared/History.fs]] and [[src/Shared/ChangeAmendment.fs]]. Do not move source files.
- Replace AgentHandle with GraphAgentHandle. Do not retain two handles. GraphAgentHandle is a fully typed Core Interface that uses domain State, Revision, Change inputs, and typed accepted results. It does not contain JSON, HttpRequest, IResult, or HTTP status. Strings are permitted only for domain text or path values and the current error detail.
- Core exclusively owns the selected FileAgent or DbAgent. Keep the agent files at their current paths. The selected agent retains its mailbox, sequencing, State, and publication logic. Limit agent edits to the typed-interface changes required by [[plan/core-creation/issues/03-define-typed-core-changes-contract.md]] and [[plan/core-creation/issues/04-separate-http-adapter-from-core-changes.md]] unless later design requires more.
- [[src/Server/Api.fs]] is HTTP Adapter logic. It decodes and encodes HTTP and calls GraphAgentHandle. [[src/Server/RouteRegistration.fs]] only maps endpoints, applies authorization, reads request bodies, and composes startup. It does not receive raw FileAgent or DbAgent values.
- Move agent creation and selection behind Core. Production callers outside Core receive GraphAgentHandle only. Existing agent-focused tests can construct agents directly.
- Preserve current persistence behavior. DB-backed operation and read-only fallback matter, but the legacy file-authority and mirror compatibility branches remain unchanged and are deferred for ACID re-examination in [[plan/roadmap/epics/chapters/acid-apply.md]]. This issue does not remove them or change persistence behavior.
- Parse remains outside Core and calls typed Graph-only Post Change after it produces Changes.
- GraphAgentHandle is the implementation-ready initial Core Interface. It does not replace the final four-call Core API of Files, Changes, Query, and Command. Later decision tickets can reorganize it.

The requirement above to preserve the runtime mirror and persistence-mode branches is superseded by [[13-delete-runtime-mirror-and-remove-production-persistence-mode.md]]. This does not change the settled Core contract or placement decisions.

## Time

- 2026-09-05 40m — grilled and resolved Core source placement and the initial Graph-agent package
