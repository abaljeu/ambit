# 35b — Browser Run hello

**Status:** blocked
**Blocked by:** [[34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]].

## Context

The outside-Core proof establishes the TestActor `hello` lifecycle without Browser HTTP. This ticket connects that lifecycle to the existing Browser Run action. When the current Node text starts with `?`, Run sends a one-Node Command through HTTP and Core. The Browser then shows one Owned child with text `hello` under the current Focus.

This ticket follows Story path **Browser Run hello** in [[plan/core-creation/arch.md|Core creation architecture]]. The architecture Module map owns State, Interface, and Uses details.

## What to build

Make the existing Browser Run action complete one successful `?test hello` path. The Browser creates the Command request and credentials, the HTTP Adapter calls the CoreMailbox door, Core runs TestActor through the ordered lifecycle, and the universal response lets the Browser show the resulting Owned child. Keep non-`?` Run behavior unchanged.

### 1. Loaded descendant id list

Provide the shared id list used by Browser Command requests. Follow module **Loaded descendant id list** in [[plan/core-creation/arch.md|Core creation architecture]].

1. [ ] Include Zoom root — return a flat NodeId list that starts with the requested Zoom root.
2. [ ] Walk Loaded children — recurse through Loaded child lists and include every child id.
3. [ ] Stop at Unloaded children — do not descend through Unloaded child lists.
4. [ ] Ignore ownership — do not filter or branch on child ownership.
5. [ ] Return ids only — return no Graph, edge, or ownership data.

### 2. Browser Run

Turn the existing Run action into the Browser entry point for a Command. Follow module **Browser Run** in [[plan/core-creation/arch.md|Core creation architecture]].

1. [ ] Detect Command text — when the current Node text starts with literal `?`, use the Command path.
2. [ ] Use one Node — use the current Node as Command, Zoom root, and Focus.
3. [ ] Send the request — include caller credentials, `zoomId`, `focusId`, `commandId`, and the Loaded descendant `graphIds`.
4. [ ] Preserve AmbleRun — keep existing behavior for text that does not start with `?`.

### 3. HTTP Adapter

Carry the Command request across the transport boundary. Follow module **HTTP Adapter** in [[plan/core-creation/arch.md|Core creation architecture]].

1. [ ] Decode Command and credentials — accept the Browser request with the same named ids used by Core.
2. [ ] Call CoreMailbox — submit the request through `startActor`.
3. [ ] Encode the universal response — return `{ nodes; events; latestId }` when this Command path runs.

### 4. CoreMailbox / CoreMsg / CoreActorPool

Run start, output admission, and stop through the Core modules named in [[plan/core-creation/arch.md|Core creation architecture]].

1. [ ] Start through CoreMailbox — validate the caller and secret before handing StartActor to CoreActorPool.
2. [ ] Start through CoreActorPool — expand the Graph, select registered Actor `test`, create the live row, append ActorStarted, and schedule the body.
3. [ ] Admit before PostChange — accept TestActor output only while its live identity and secret are valid.
4. [ ] Finish after output — process `ActorStop ActorSucceeded` after the Actor Change.

### 5. TestActor / History / CoreRuntime

Produce `hello`, record the lifecycle, and compose the registered Actor. Follow the named modules in [[plan/core-creation/arch.md|Core creation architecture]].

1. [ ] Register TestActor — make Actor name `test` available at startup.
2. [ ] Interpret hello — read the command Node and select the `hello` behavior.
3. [ ] Post the child — add one Owned child with text `hello` under Focus.
4. [ ] Stop successfully — queue `ActorStop ActorSucceeded` after the Change.
5. [ ] Record completion — append one ActorFinished and remove the live row and secret.

### 6. Browser proof

Verify the complete Story path from the user-visible boundary.

1. [ ] Run hello — use existing Browser Run on a current Node with text `?test hello`.
2. [ ] Show one result — show one Owned child with text `hello` under the current Focus.
3. [ ] Use no Agent service — complete the proof without a live external Agent service or key.

## See also

[[plan/core-creation/arch.md|Core creation architecture]]

[[Implementation Planning and Record.md|Implementation Planning and Record]]
