# Search Amble

Map: [[map.md]]

## 1. User stories

1. Dialog list — As a person, I want the Find dialog to list the Answers of an Amble expression I type there, so that I see those Answers in the dialog.
2. Outline held — As a person, I want the outline to keep its Nodes, its Children, and its names while that dialog lists the Answers, so that the outline is the same after the dialog evaluates the expression.
3. Word search held — As a person, I want Find text that the dialog treats as search text to keep the current Find search, so that a word search still lists Nodes from the residency and from the full database.

## 2. Acceptance criteria

[ ] Dialog list — The Find dialog lists the Answers of an Amble expression typed in that dialog.
[ ] Answer order — That list follows Answer order.
[ ] Outline held — The outline has the same Nodes, the same Children, and the same names after the dialog evaluates the expression.
[ ] Empty list — The dialog list is empty when the expression yields no Answer, when the expression fails to parse, and when the expression has a type error.
[ ] Outline run — An outline Amble run places Children through `ExprRun.run`.
[ ] Node Answer child — That run places a Node Answer as a Ref child.
[ ] Text Answer child — That run places a Text Answer as an owned child that carries that Text.
[ ] Reply run — [Amble tools](../bot-amble-tools/spec.md) is that placement when a reply adds the run line.
[ ] Word search home — Word search in this dialog stays [Search](../../doc/current/search.md).
[ ] Client walk — Search text searches the residency Graph from the zoom root.
[ ] Server walk — Search text searches the full database from that same zoom root.
[ ] Append — The dialog keeps the residency hits and appends the server hits that sit outside that client hit set.
[ ] Query line — An expression that inserts Refs under a query line stays the Query spec in [Online search](../online-search/spec.md).

## 3. Limits

[ ] Find cap — A word search stops at 200 hits. That number is `ViewModelSearch.searchHitCap` and `SearchDialog.searchPictureCount`. The dialog sends it as `SearchPicture.Request.count`. A request that omits `count` decodes as 200.
[ ] Run cap — `ExprRun.run` places at most 50 Answers. That bound is `ExprRun.maxMaterialisedAnswers`.

The bound on Answers listed from an Amble expression in the Find dialog is open question 4 — Result cap.

## 4. Open questions

### 1. Recognition

The Find dialog passes the query to the word search. Three recognition rules are in the program.

- `ViewModelSearch.parseSearchTerm` trims the query and accepts every non-empty string as search text. A leading `=` stays search text on this path.
- `ExprRun.isRunStatement` accepts a trimmed line that starts with `=`. It also accepts one Amble name token, then `=`, then the expression.
- `CommandRequest.isQueryText` accepts text that contains `=` and does not start with `?`.

[Expression language](../expression-language/spec.md) section 8 says Search and Move evaluate a leading `=`, and that `Name=` stays on Run.

Which rule tells the Find dialog that the typed text is an Amble expression?

### 2. Graph

The plain Find search is Client walk, Server walk, and Append. The server walk starts after the quiet gap. `SearchActor.reply` is that walk. The client starts that walk while the residency search is under the Find cap.

Outline run calls `ExprRun.run` on one Graph. The query Actor calls `ExprRun.answerNodeIds` on the server Graph and returns Node ids.

Does the Find dialog evaluate the expression on the residency Graph, on the server's full database, or on both in that same two-place way? Does a keypress evaluate it, or does the quiet gap still gate the server?

### 3. Text Answers

A dialog row is a `NodeSearchResult` with a Node id, Header text, and a name. `SearchDialog.rowFromNode` builds that row from a Node. Node Answer child and Text Answer child are the outline placement. A number Answer sits outside Node Answer child and Text Answer child. `ExprRun.answerNodeIds` returns Node ids only.

How does a Text Answer appear in the dialog list? What does the list do with a number Answer?

### 4. Result cap

The Find cap and the Run cap are the Limits above. Which bound applies to the Answers in this dialog list?

### 5. Selection

`SearchDialog.runSearchSelectionOp` closes the dialog and calls `onPick` with one `NodeSearchResult`. Find's pick is `ViewModelSearch.searchPickSetRoot`, which zooms to that Node. A Text Answer has no Node id.

What does selecting an Answer do?

### 6. Move

Move opens this same dialog and uses this same search. Does an Amble expression in the Move dialog follow this spec?
