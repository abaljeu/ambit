# Create cloud-agent posts reply under Focus

**Type:** vertical
**Status:** done
**Stage:** slice
Actual: 2h30m

## Question

Implement the vertical Server slice (no Browser Client) for the cloud-agent actor that receives a POST request with actor name, nodelist, focus node, root node, and revision, launches a Cloud Agent with the focus message and Markdown-packed subgraph, and posts the agent's response as Owned children under the focus node.

## Answer

Vertical Server slice completed:

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

## Constraints satisfied

- Actors are objects registered into Core; definitions not inside Core ✓
- In-process library reference to CloudAgents, not stdin/stdout console ✓
- Issue Status/checkboxes/Time updated ✓
- Project Started (Stage stays slice until more done) ✓
- Test-first approach followed ✓

## Out of scope (later slices)

- Browser Client integration
- Cancel/stream support
- Repo attachment
- Multiple repositories
- User-selectable models
- Follow-up turns

## Time

- 2026-09-08 2h30m — implemented vertical Server slice with actor registration, API endpoint, and tests
