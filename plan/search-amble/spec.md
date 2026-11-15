# Search Amble

Map: [[map.md]]

## 1. User stories

1. Word search — As a person, I want the dialog to list Nodes as I type in word-search mode, so that Find and Move keep the current search.
2. AMBLE switch — As a person, I want a switch in the dialog to select AMBLE mode, so that the text is an Amble expression.
3. Execute list — As a person, I want AMBLE mode to list Node Answers when I execute a parsed expression, so that I see those Nodes in the dialog.
4. Outline held — As a person, I want the outline to keep its Nodes, its Children, and its names, so that the outline is the same after that execute.
5. Pick — As a person, I want a pick in this dialog to keep its current effect, so that Find zooms to the Node and Move moves to the Node.

## 2. Acceptance criteria

### Word search

[ ] Word-search mode — The dialog builds the node list as the person types.
[ ] Word search home — Word search in this dialog stays [Search](../../doc/current/search.md).
[ ] Client walk — Search text searches the residency Graph from the zoom root.
[ ] Server walk — Search text searches the full database from that same zoom root.
[ ] Append — The dialog keeps the residency hits and appends the server hits that sit outside that client hit set.
[ ] Find cap — A word search stops at the Find cap in Limits.

### AMBLE mode

[ ] Mode switch — A switch in the dialog selects AMBLE mode.
[ ] Execute — AMBLE mode builds the node list on an explicit execute.
[ ] Parsed expression — That execute builds the node list when the expression parses.
[ ] Server Graph — The server evaluates the expression on the full database.
[ ] Answer order — The node list follows Answer order.
[ ] Run cap — The node list stops at the Run cap in Limits.
[ ] Parse failure — A parse failure leaves the node list empty.
[ ] Type error — A type error leaves the node list empty.
[ ] No Node Answer — The node list is empty when the expression yields no Node Answer.

### Text Answers

[ ] Text Answer — An expression that yields Text Answers leaves the node list empty. `ExprDialog.tryHits` returns that empty list when `ExprCompile.evalOutcome` is `Hits` of `ExprAnswerType.Text`.
[ ] Node row — A Node Answer is one row. `ExprDialog.tryHits` copies that Node's id, `node.text`, and name onto the row.
[ ] Named row — `formatNamedDialogHit` shows a `Filename.Ok` name, two spaces, and the Header text.
[ ] Unnamed row — `formatNamedDialogHit` shows the Header text for any other name.
[ ] Empty label — An empty node list shows the label `Zero results`. `dialogResultLabels` supplies that label.
[ ] Empty pick — An empty node list has no selectable row. `dialogResultPick` returns no index for that list.
[ ] Answer cases — An Answer here is a Node or Text. `ExprAnswer` has those two cases.

### Selection

[ ] Find pick — Find's pick zooms to the Node through `ViewModelSearch.searchPickSetRoot`.
[ ] Move pick — Move's pick moves to the Node through `searchPickMoveAfterHit`.
[ ] Dialog scope — Find and Move open this dialog, and AMBLE mode applies to both.
[ ] Pick row — `SearchDialog.runSearchSelectionOp` closes the dialog and calls `onPick` with one `NodeSearchResult`.

### Outline

[ ] Outline held — The outline has the same Nodes, the same Children, and the same names after an AMBLE execute and after a parse failure.
[ ] Outline run — An outline Amble run places Children through `ExprRun.run`.
[ ] Node Answer child — That run places a Node Answer as a Ref child.
[ ] Text Answer child — That run places a Text Answer as an owned child that carries that Text.
[ ] Run home — That placement stays [Expression language](../expression-language/spec.md) section 8.
[ ] Reply run — [Amble tools](../bot-amble-tools/spec.md) is that placement when a reply adds the run line.
[ ] Query line — An expression that inserts Refs under a query line stays the Query spec in [Online search](../online-search/spec.md).

## 3. Limits

[ ] Find cap — A word search stops at 200 hits. That number is `ViewModelSearch.searchHitCap` and `SearchDialog.searchPictureCount`. The dialog sends it as `SearchPicture.Request.count`. A request that omits `count` decodes as 200.
[ ] Run cap — AMBLE mode lists at most 50 Node Answers. That bound is `ExprRun.maxMaterialisedAnswers`, the same bound as an Amble run.

## 4. Open questions

### 1. Input Node

The server evaluates the expression on the full database.

`ExprDialog.tryHits` applies the expression to the zoom root.

`ExprRun.run` applies a run line to the focus Node.

Which Node is the input Answer in AMBLE mode?
