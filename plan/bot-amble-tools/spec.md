# Amble tools

Map: [[map.md]]

## 1. User stories

1. In-place run — As a person, I want an outline node that this reply adds, whose whole text is a run line such as `x = root child named "bob" re "blueberry"`, to run in place, so that the node gains the children that Amble run places for that line.
2. Answer text — As the Agent, I want the header text of each child that run places, so that I have the text of the found nodes.
3. Continue the reply — As a person, I want the Agent to continue this same reply after that text comes back, so that later outline nodes land after the links.
4. Instruction — As a person, I want the wake text to tell the Agent to write a run line only when that line is meant to run, so that a run happens on purpose.
5. Mild limits — As a person, I want one run to stop at the Amble answer cap and one reply to stop at a round cap, so that a reply stays small.
6. Poll for nodes — As the Agent, I want to send the client's poll request for specific Node ids, so that I can read nodes beyond the wake extract.

## 2. Acceptance criteria

[ ] Run line — A run line is an Amble run statement. The form is `= <expression>`, or `<name> = <expression>` when `<name>` is one Amble name token.
[ ] Whole text — The run starts when the entire text of the new outline node is a run line.
[ ] This reply — The run starts for a node that this reply adds. The reply is the live AI Actor run for the command `?ai gbot`.
[ ] Store, then run — The AI Actor stores that node, then runs `ExprRun.run` with that node as the input Answer, on the graph the actor posts to.
[ ] In-place links — That run posts the plan `ExprRun.run` builds for that line on that graph. A Node Answer becomes a Ref child. A Text Answer becomes an owned child that carries that Text. A name on the left of `=` renames the node. The new children replace the node's previous children.
[ ] Deliver timing — The deliver that completes the node sends its response after those children are stored.
[ ] Answer body — That response body is JSON with one field, `text`. The value is one line per child, in child order. A Ref line is the linked node's header text. An owned line is that child's text.
[ ] Several run lines — When one deliver completes more than one run line, the runs follow outline order, and a blank line separates each run inside `text`.
[ ] Quiet deliver — The response body is empty when every node that deliver stores is ordinary outline text.
[ ] Empty post — An empty posted text flushes an open outline node. When that flush completes a run line, the response carries that run's text, and the actor keeps listening on this same reply.
[ ] Same reply — The Agent posts the continuation to the deliver door. This reply keeps the one wake that started it.
[ ] Wake permission — The wake text says that a run line may be the text of one outline node.
[ ] Wake purpose — The wake text says that the Agent writes a run line only when that line is meant to run.
[ ] Wake continuation — The wake text says that Ambit returns the found nodes' text and that the Agent continues the reply.
[ ] Answer cap — Materialisation stops at the answer cap in Limits.
[ ] Round stop — After this reply has executed the round cap, a later run line stays stored text, and the deliver that completed it has an empty response body. Other outline nodes from the reply still appear.
[ ] Poll permission — The Agent may send the client's poll during the live AI Actor run for the command `?ai gbot`.
[ ] Client poll — The request is `POST /ambit/poll` with JSON fields `eventId` and `want`, as in [HTTP contract](doc/current/http-contract.md) section 2.8 POST /ambit/poll.
[ ] Want list — The Agent sets `want` to the Node ids it wants to read, including ids that are outside the wake extract.
[ ] Poll response — A successful poll returns the same change-success envelope the client's poll returns, including the want-answer fields `nodes` and `childMap`.
[ ] Poll secret — The Agent sends the same inbound secret the deliver door uses, in header `X-Ambit-Inbound-Secret`.
[ ] Live session — That poll is admitted when the session from the wake is live.
[ ] Wake poll — The wake text says that the Agent may poll for specific Node ids.

## 3. Limits

[ ] Answer cap — One run materialises at most 50 Answers.
[ ] Round cap — One reply executes at most 4 run lines.
[ ] Returned-text cap — One run returns at most 8000 characters of text, after the answer cap. The outline holds every materialised child.
[ ] Poll cap — One Agent poll returns at most 200 lines of residency.

## 4. Open questions

- How the Agent presents `sessionId` on `POST /ambit/poll`. The current poll body has only `eventId` and `want`. The Browser authenticates with the `gambol_auth` cookie.
- Which `eventId` the Agent sends. That field is required. Field `c` in the success body is the event tail after that id.
