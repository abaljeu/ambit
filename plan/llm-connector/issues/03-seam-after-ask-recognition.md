# Seam after expression-language recognizes `?`

Type: grilling
Status: done
Blocked by:
Actual: 60m

## Question

After [[plan/expression-language/issues/33-recognize-ask-run-statement.md]] recognizes `?` as a Run statement, what seam does llm-connector own (launch Actor, pack, write-back)? Decide the boundary. Do not implement in this ticket.

## Answer

Spoken name is Run Agent. Issue 33 only recognizes a Focus line that starts with `?`. This Project owns the rest: Browser Run Agent POSTs `/ambit/actors` with `{ actor, nodelist, focusnode, rootnode, revision }` and the [[plan/core-creation/issues/20-client-presents-credential.md]] cookie. Server launches the Actor. Extract is the nodelist subgraph under `rootnode`. The Actor Md-writes that graph, calls Grok Bot, converts reply Md→graph, and posts Owned children of Focus.

ActorName selects which Actor (first is Grok Bot). The LLM message is the `?` remainder on Focus Header, not a LaunchRequest field. Create returns `PublicNumber`. This Project changes extract/`LaunchRequest`. Do not revise [[plan/core-creation/issues/09-define-core-command-launch-contract.md]].

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Map: [[../map.md]].
- 2026-09-06: Q2: First-slice executing `?` launches the Actor (not recognition-only, not harness-only). Q3: Run is async — Browser sends a command; Server launches the Actor; the Actor does the work. Issue 33 still owns only recognition. HTTP launch surface, Command payload, and send-time Graph Change are still open.
- 2026-09-06: Q5: Proposed Create-only REST at `/ambit/actor(s)`. Pack travels as NodeIds on that Create, not as a Graph Change. Confirm shape and `ActorFn` payload still open.
- 2026-09-06: Q8: Keep Create-only `POST /ambit/actors` over Core `launch`. Q9: Do not add an LLM message to `LaunchRequest`. ActorName selects which Actor to run. Add nodeIds only.
- 2026-09-06: Q12: The `?` remainder is the LLM message, not ActorName. With Q9: the Actor reads it from Focus Header. Other Create fields (Zoom) still open.
- 2026-09-06: Proposed Create JSON: actor, nodelist, focusnode, rootnode, credential. Q18: Actor converts reply Md→graph and adds to Focus. Credential-in-body and revision still open.
- 2026-09-06: Q19: Add `revision`. Client token: user says all Server APIs need a client token the Server validates. Not in `plan/` or `doc/` yet. Cookie vs JSON still open.
- 2026-09-06: Q22: Wait for a Core token standard. Q23: This Project changes extract; do not revise issue 09. Q24: Create returns `PublicNumber`.
- 2026-09-06: Q25: That standard is [[plan/core-creation/issues/20-client-presents-credential.md]]. Create uses the cookie. JSON is `{ actor, nodelist, focusnode, rootnode, revision }`.
- 2026-09-06: Q26: Lock. Spoken name is Run Agent. Answer written.
- 2026-09-06: Alan confirmed lock.

## Time

- 2026-09-06 10m — Q2 execute-`?` launches; Q3 async Browser command (from chat)
- 2026-09-06 5m — Q5 Create-only actors REST proposed (from chat)
- 2026-09-06 5m — Q8 POST `/ambit/actors`; Q9 nodeIds only, no message field (from chat)
- 2026-09-06 5m — Q12 message on Focus Header (from chat)
- 2026-09-06 10m — Create body fields; Md→graph onto Focus (from chat)
- 2026-09-06 5m — Q19 revision; token spec named (from chat)
- 2026-09-06 5m — Q22–24 wait Core token; this Project extract; PublicNumber (from chat)
- 2026-09-06 5m — Q25 issue 20 cookie (from chat)
- 2026-09-06 10m — Q26 lock; Run Agent name; wrote Answer (from chat)
