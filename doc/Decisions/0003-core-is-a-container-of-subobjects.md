# Core is a container of subobjects

Status: provisional. This record fixes the agreed framing so the team does not argue it again. A more substantial structuring of Core can supersede it.

Core is a container of subobjects, not a facade. A call site reaches Core functionality through the owning subobject, for example `core.changeAgent.postChange changes`. Core holds the subobjects; each subobject owns its own operations. This came out of the standards review [[plan/core-creation/reports/review-standards-initial-core-changes.md]] for the [[plan/core-creation/project.md]] Project, which reported a Middle Man finding against the per-field adapters in [[src/Server/Core/GraphAgentHandle.fs]]. The container framing retires that finding, because the agent supplies its own handle once and Core holds it. Work on the Core code continues, so the current code is not the final shape.

## What this means

We keep owned mutable state instead of threading a value. The earlier idea was a pure transition, `newCore = coreFunction core parameters`. The Change path is not a pure transition: it interleaves amend and apply with disk validation, disk write, persist-stamp absorption, log append, and publish. A pure form needs a decide / perform / absorb split, which is not worth its cost now. Instead Core keeps state behind its seam, and `core.subobject.function parameters` updates Core and its subobjects.

Mutation is confined behind the Core seam. [[.cursor/rules/project-values.mdc]] and [[.cursor/rules/fsharp-source.mdc]] ask for pure functional F# with no mutable. Core satisfies this at the seam, not inside: the state reference lives in one owner's mailbox loop, the arrangement [[src/Server/FileAgent.fs]] already uses. This is the intended exception, not a regression to report.

In F# the container is a record of closures over private state. The handle in [[src/Server/Core/CoreChanges.fs]] already has this form, so no new mechanism is necessary.

Core must not re-declare a subobject's operation at Core level. A forwarder such as `core.postChange` that calls `core.changeAgent.postChange` adds a needless hop and is the Middle Man smell. Keep the nesting visible at the call site.

The change agent owns the Change sequence, and Core owns the change agent. [[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] stay change agent implementations, not storage ports. Core selects between them.

Core is more than a namespace, because Core still owns real policy: persistence-mode selection, the read-only rejection when the Database is unavailable, and the Database mirror. These are Core decisions about the agent, not forwarding of the agent.

Containment follows from the nesting. If the Graph agent is reachable only as a subobject of Core, other Server code has no public constructor to call, so no code can bypass Core to publish a Change. Two agents on one data directory put two writers on one Change log, so Core owns the single agent for a data directory.

## What would replace this

Replace this record if Core must enforce an invariant that a container cannot hold, or if the pure decide / perform / absorb transition becomes worth its cost.
