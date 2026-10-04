# Online search

Two specs. They stay separate. File names for a split stay open on [map](map.md) item 7 **Spec file names**. Until that item is named, both specs live in this file.

This file records the Destination, the Notes that still stand, and Alan's later locks. It does not resolve a map ticket. A mechanism Alan asked this project to design is marked **Proposed design** in [arch](arch.md). It is not a lock.

## 1. Search spec

### 1. Problem Statement

1. **Residence-only Find** — Find searches the Resident Graph. `searchNodes` walks that Graph. The person does not see hits that are only on the Server. Move uses that same residence list today.
2. **Short keystroke** — A short keystroke such as the letter e must not return the whole Graph. The cap is 200 hits.

### 2. Solution

1. **Two phases** — Find is a two-phase fulfillment of the existing client search. Every keypress recomputes on the client only and updates the dialog immediately. That local incremental search stays. No server message goes out on a keypress. After the search text has been unchanged for a short quiet gap, one server request completes the picture. There is no later page.
2. **Quiet gap** — The server request fires once, after that short quiet gap. This spec does not name a millisecond value. If the text changes before the gap ends, the request is not sent. If the client already has 200 hits, the request is not sent. A reply for an older search string is ignored. The reply matches the search text, or an equivalent generation of that text. This quiet gap is a lock.
3. **Own server support** — Find has its own server support. Move has its own server support when Move recomputes on each keypress the same way. File search is out of this spec.
4. **Want-fulfillment** — If the server finds N items, the Find dialog shows them. Move shows that same N when it uses this dialog. That showing is Want-fulfillment. The way a found-Node list rides Want is a proposed design in [arch](arch.md). It is not a lock.
5. **Duplicates** — A client hit and a server hit are the same when they share a Node id. The one server request asks only for hits the client does not already have. The start message carries those Node ids. The second phase drops duplicates on Node id.
6. **Cap of 200** — The combined result stops at 200. Find returns at most 200 hits and then stops. Move returns at most 200 hits and then stops. There is no paging. There is no continuation cursor. This cap is a lock.
7. **Trash** — Search skips trash unless it starts at the trash node. The trash node is TRASH.
8. **Move** — Move is in this spec. When Move recomputes on each keypress the same way Find does, it uses this same quiet gap, the same one request, and the same cap of 200. File search is not in this spec.

### 3. User Stories

1. **Keypress stays local** — As a person, I want each keypress to recompute Find on the client and update the dialog immediately, so that typing does not wait on the server.
2. **Quiet gap** — As a person, I want one server request after the search text has been unchanged for a short quiet gap, so that the server completes the picture once the text has settled.
3. **Text change holds the request** — As a person, I want a text change before the quiet gap ends to withhold the server request, so that a keypress does not send a message.
4. **Already 200** — As a person, I want no server request when the client already has 200 hits, so that the combined result stops at 200.
5. **Stale reply** — As a person, I want a server reply for an older search string to be ignored, so that the dialog shows hits for the text I see now.
6. **Duplicates on Node id** — As a person, I want the server request to ask only for hits whose Node ids I do not already have, so that the dialog lists each Node once.
7. **Cap of 200** — As a person, I want the combined Find result to stop at 200 hits, so that a short keystroke such as the letter e does not return the whole Graph.
8. **Skip trash** — As a person, I want search to skip trash, so that an ordinary search stays out of deleted Nodes.
9. **Start at the trash node** — As a person, I want search to include trash when it starts at the trash node TRASH, so that a search that starts there can see trash.
10. **Dialog shows server hits** — As a person, I want the Find dialog to show the N items the server finds, so that the server answer is visible.
11. **Move uses the same search** — As a person, I want Move, when it recomputes on each keypress the same way, to use this same quiet gap and stop at 200 hits, so that Move has its own server support and the same cap.

### 4. Out of Scope

1. **Dialog UI** — Layout and controls of the Find dialog are out of scope for this Search spec.
2. **Query spec** — The query expression is [§2 Query spec](#2-query-spec) in this file.
3. **File search** — File search is out of scope for this Search spec.
4. **Expression catalog** — Expression syntax and the catalog stay on [expression language](plan/expression-language/project.md).
5. **Search hydration** — Fetch before navigate when a Find hit is not Resident stays on [browser residency](plan/browser-residency/project.md). [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md) still points that beat there.
6. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map.
7. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md).
8. **Selective client loading** — That done project stays [selective client loading](plan/selective-client-loading/project.md).
9. **Paging** — Paging, a continuation cursor, and a resume-when-the-dialog-asks bound are out of scope for this Search spec.
10. **Keypress message** — A server message on a keypress is out of scope for this Search spec.
11. **Quiet-gap duration** — A millisecond length for the short quiet gap is out of scope for this Search spec.

### 5. Further Notes

1. **Duplicate identity** — Locked here as Node id. [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md) is not resolved by this spec.
2. **Cap of 200** — The combined Find and Move result stops at 200. The earlier screen-plus-a-page bound is dropped.
3. **Quiet gap** — One server request follows a short quiet gap. The gap has no millisecond value in this spec. The reply matches the current search text, or an equivalent generation.
4. **Trash start** — The search starts in trash when it starts at the trash node TRASH.
5. **Move** — Move is in this spec when it recomputes on each keypress the same way Find does. File search is not.
6. **Query** — A query is one server evaluation when the line runs. It is not a message per key. See [§2 Query spec](#2-query-spec).
7. **Proposed mechanisms** — The Want ride stays a proposed design in [arch](arch.md). Next and Page are not part of this spec.
8. **Spec file names** — Open on [map](map.md) item 7 **Spec file names**.
9. **Research reports** — Reports linked from the research tickets are not accepted Answers.
10. **Map** — [map](map.md). Decisions so far stays empty. This spec does not resolve a ticket.
11. **Vocabulary** — Say event source. Say Server git Actor for that git Actor. Do not say CAS. Do not say Peer.

## 2. Query spec

### 1. Problem Statement

1. **Results under the query line** — A person writes an expression such as `= root descendants with name like "Bob"` and wants the results under that query line.
2. **Server eval** — The server evaluates the query. This spec drops the earlier assumption that eval stays on the client Graph.
3. **Refs** — Those results are Refs. They are not Owned Children.

### 2. Solution

1. **Insert under the query line** — The expression inserts its results under the query line as Refs.
2. **Server evaluates** — The server evaluates the query once, when the line runs. The query has its own server support. A keypress does not send a query message.
3. **Cap of 200** — The query function may set a lower limit of its own. The server returns at most 200 hits and then stops. If the function asks for more than 200, the server stops at 200. The query does not page. 200 is the cap that keeps a short keystroke from returning the whole Graph. This cap is a lock.
4. **Trash** — Query skips trash, except the trash function. The trash function is named `trash`. It works like `root` for reaching trash.
5. **Want-fulfillment** — If the server finds N items, the query expression inserts them. That insert is Want-fulfillment.
6. **Separate from Find** — This spec is not the Find dialog. Find and Move are [§1 Search spec](#1-search-spec).
7. **Example is not a catalog row** — The expression `= root descendants with name like "Bob"` shows the kind of expression. It does not add words to the expression-language catalog. The word `trash` is the locked function name.
8. **Insertion module** — [ExprRun](src/Shared/ExprRun.fs) already builds a Ref for a Node answer (`ChildNode.reference` on the path `run` uses to materialise). The server eval, and posting that Ref under the query line, are a proposed design in [arch](arch.md). They are not a second lock on top of Refs.

### 3. User Stories

1. **Insert Refs under the query line** — As a person, I want an expression such as `= root descendants with name like "Bob"` to insert Refs under the query line, so that those Nodes appear under that line and the line does not own them.
2. **Cap of 200** — As a person, I want the server to stop at 200 even when the query function asks for more, and to honor a lower limit when the function sets one, so that the query does not return the whole Graph and does not page.
3. **Skip trash** — As a person, I want an ordinary query to skip trash, so that deleted Nodes stay out of the results.
4. **Trash function** — As a person, I want the function `trash` to work like `root` for reaching trash, so that a query can start from trash the way `root` starts from ROOT.
5. **Server items inserted** — As a person, I want the query expression to insert the N items the server finds, so that the server answer appears under the query line as Refs.
6. **Server evaluates** — As a person, I want the server to evaluate the query once when the line runs, so that the result is the server's evaluation and typing a key does not send a message.

### 4. Out of Scope

1. **Find dialog** — Find and Move are [§1 Search spec](#1-search-spec). Dialog UI is out of scope for this Query spec.
2. **File search** — File search is out of scope for this Query spec.
3. **Expression catalog** — Syntax and the catalog, other than the locked name `trash`, stay on [expression language](plan/expression-language/project.md).
4. **Search hydration** — Fetch before navigate when a hit is not Resident stays on [browser residency](plan/browser-residency/project.md).
5. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map.
6. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md).
7. **Selective client loading** — That done project stays [selective client loading](plan/selective-client-loading/project.md).
8. **Owned results** — Owned Children under the query line are out of scope for this Query spec. Results are Refs.
9. **Paging** — Paging and a continuation cursor are out of scope for this Query spec.
10. **Quiet gap** — The Find quiet gap is out of scope for this Query spec. Query runs when the line runs.

### 5. Further Notes

1. **Server eval** — This lock replaces the earlier local-eval assumption for this spec. [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md) is not resolved by this spec. [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) stays that project's amendment.
2. **Cap of 200** — The server never returns more than 200 for a query. A function may set a lower limit. This spec does not spell that lower limit's syntax.
3. **Trash function** — The name is `trash`. It works like `root` for reaching trash.
4. **Refs** — Results under the query line are Refs.
5. **Proposed mechanisms** — How the found Nodes ride Want, and how the Actor posts the Ref insert, are proposed designs in [arch](arch.md).
6. **Spec file names** — Open on [map](map.md) item 7 **Spec file names**.
7. **Research reports** — Reports linked from the research tickets are not accepted Answers.
8. **Map** — [map](map.md). Decisions so far stays empty. This spec does not resolve a ticket.
9. **Query name** — In this project, Query means the expression that inserts Refs under the query line.
10. **Vocabulary** — Say event source. Say Server git Actor for that git Actor. Do not say CAS. Do not say Peer.
