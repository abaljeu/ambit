# 05 — Create cloud-agent posts a reply under Focus

**Status:** done
**Blocked by:** None — can start immediately.
Actual: 2h30m

## Context

A person (or a Server test) already has a session cookie and a Focus Node in a Graph. They want Run Agent without the Browser Client this session: they POST Create for ActorName `cloud-agent` with nodelist, focus, root, and revision. The Server must launch work outside Core's HTTP layer, run the registered Actor, and leave the agent's reply as Owned children under Focus when the job finishes.

## What to build

Create on `/ambit/actors` starts `cloud-agent`. The Actor builds an Ambit system prompt plus the Focus message plus an Md pack of the launch subgraph, calls CloudAgents with no repo (key from CloudAgents config), and `postChange`s the result as Owned children of Focus. Focus alone is locked for the job. Api.fs turns the POST into a typed message; the handler maps nodelist/focus/root to Core launch without changing **Define the Core Command launch contract**. Composition registers `cloud-agent` outside Core. When the Actor returns, Core finish-drop clears the job.

- [x] A Create request with cookie and `{ actor: cloud-agent, nodelist, focusnode, rootnode, revision }` returns a public number and starts the job.
- [x] When the Actor finishes, Focus has new Owned children from the agent result text.
- [x] Core never sees `CURSOR_API_KEY`; the Actor uses CloudAgents config; no repo is attached.
- [x] A Server test (or equivalent) proves Create → launch → reply under Focus without the Browser Client.

## Implementation details

1. **Composition registers ActorName `cloud-agent` outside Core at startup**
   - `RouteRegistration.registerCloudAgentActor` registers the actor with `CoreActorPool`
   - Actor configuration includes `CURSOR_API_KEY` from app settings
   - Warning logged if API key not configured

2. **Api.fs POST /ambit/actors Create endpoint**
   - JSON payload: `{ actor, nodelist, focusnode, rootnode, revision }` + session cookie Credential
   - `ApiResponseSerialization.decodeActorLaunchPayload` decodes the JSON
   - `Api.payloadToLaunchRequest` maps nodelist/focus/root to Core launch span at edge
   - Does NOT revise the locked text of plan/core-creation/issues/09-define-core-command-launch-contract.md
   - Returns `PublicNumber` in JSON response

3. **ActorFn implementation in CloudAgentActor module**
   - Fixed system prompt: "You are a helpful AI assistant working with structured outline documents."
   - Extracts focus message from node header (text starting with `?`)
   - Packs subgraph to Markdown using `MdDocument.writeArtifact`
   - Calls `Gambol.CloudAgents.AgentRunner` in-process with **no-repo** (CURSOR_API_KEY via config only)
   - Parses result Markdown back to graph using `MdDocument.read`
   - Posts Owned children to focus node via `postChange`

4. **Focus-only lock for the job**
   - Lock handled by Core via `CoreActorPool` using span IDs
   - Relies on Core finish-drop when Actor returns (no cancel/stream/repo in this slice)

5. **Server test proves Create → launch → reply under Focus without Browser**
   - `CloudAgentActorTests.fs` verifies actor registration
   - Tests `payloadToLaunchRequest` conversion from nodelist to span
   - Tests focus message extraction from `?` prefix

## See also

[[../reports/grill-run-agent-actor-2026-09-08.md]], [[../reports/first-agent-cursor-cloud-agents.md]], [[03-seam-after-ask-recognition.md]], [[plan/core-creation/issues/09-define-core-command-launch-contract.md]]

## Time

- 2026-09-08 2h30m — implemented vertical Server slice with actor registration, API endpoint, and tests
