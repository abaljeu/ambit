# Standards review — uncommitted vs HEAD

Range: `git diff HEAD` plus untracked Server files from `git status`. No base SHA. Size: [[.agents/skills/code-review-fsharp/SKILL.md]]. No `*.md` hunks. No added line over 100 chars. No TAB.

## Hard violations (documented)

### [[src/Server/Core/CoreRuntime.fs]] `create` (lines 85–135, 51 lines)

[[.agents/rules/fsharp-source.md]]: 40 lines or less per function. HEAD `create` was 38 lines. The new `launch` / `poll` / `post` / `registerActor` closures push it over.

### Same record: Core-level forwarders

[[.agents/rules/core-api.md]] → [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]: Core must not re-declare a subobject operation at Core level. Those four members only call [[src/Server/FileAgentCommand.fs]].

### Same `create`: two writers

Those members always use FileAgent. `rawHandle` may return DbAgent. 0003: two agents on one data directory are two writers on one Change log.

### [[src/Server/Core/CoreActorMailbox.fs]] `nodesOf` / `fromState`

```
graph.nodes |> Map.fold (fun acc _ node -> node :: acc) []
```

[[.agents/rules/fsharp-source.md]]: no O(nodes) full-graph scan on a hot path. `fromState` runs this on every launch, poll, and post reply.

### Same file: `events @`

`recordAccepted`, `commitLaunch`, and `succeed` use `model.events @ stamped` or `events @ [ ... ]`. [[.agents/rules/fsharp-source.md]]: do not grow a list with `@` on a mutation path.

## Judgement calls (baseline smells; repo overrides)

**Mysterious Name** — [[src/Server/Core/CoreEvents.fs]] `ActorStarted.authority: string` holds ActorName (`"test"`), not [[CONTEXT.md]] Authority.

**Middle Man / Duplicated Code** — [[src/Server/FileAgentCommand.fs]] is five copies of `FileAgent.mailbox(agent).PostAndAsyncReply`.

**Divergent Change** — [[src/Server/Core/CoreActorMailbox.fs]] owns `FileAgentMsg` (GetState/PostChange), Actor registry, Zoom walks, and `Async.Start`.

**Shotgun Surgery** — Actor on shared `FileAgentMsg` forces [[src/Server/DbAgent.fs]] context/reply arms. Dispatch still uses `| _ -> ()` for Actor.

**Speculative Generality** — `CommandLaunchRequest.currentEventId` is written in tests and never read. `CoreEvent.Undo` / `Redo` match [[CONTEXT.md]] Event; suppress.

**Data Clump** — `Authority * Credential` travel together on seed, admit, poll, and post.

Mailbox `ref` cells: 0003 permits mutation behind the Core seam. Do not report against [[.agents/rules/fsharp-source.md]] “Don't use mutable.” Nested `handlePostChange` stays over 40 lines but was already over.
