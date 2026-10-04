# Incremental search fill and resume

The client node-search dialog fills one page of hits and then continues the same walk. [SearchDialog](src/Client/SearchDialog.fs) asks [takeResults](src/Shared/ViewModelSearch.fs) for each page. This report records that current behavior.

## 1. Start

[currentSearchResults](src/Client/SearchDialog.fs) starts the walk when it reads the result list. The walk uses the [Graph](src/Shared/Model.fs) stored on the [SearchCursor](src/Shared/ViewModelSearch.fs).

1. **Dialog read.** [renderSearchDialog](src/Client/SearchDialogView.fs) calls [currentSearchResults](src/Client/SearchDialog.fs) when the mode is SearchDialog. currentSearchResults calls ensureSearchCache in that same file. ensureSearchCache calls startCache when the cache is absent or the cache does not match.
2. **Cache match.** The cache matches when the query is equal, the zoom root is equal, and the graph is the same object. cacheMatches in [SearchDialog](src/Client/SearchDialog.fs) tests the graph with PhysicalEquality.
3. **Cache clear.** [openSearchDialogWithOnPick](src/Client/SearchDialog.fs) and [closeSearchDialogOp](src/Client/SearchDialog.fs) call resetSearchResults. [NodeSearchQuery](src/Client/Update.fs) calls resetSearchResults when the SearchDialog query changes, and it sets the selected index to 0. The next read starts a new walk.
4. **Remembered query.** openSearchDialogWithOnPick sets the dialog query from lastNodeSearchQuery. closeSearchDialogOp stores the current query in lastNodeSearchQuery before it clears the cache. The initial value in [SearchDialog](src/Client/SearchDialog.fs) is an empty string.
5. **Cursor build.** startCache in [SearchDialog](src/Client/SearchDialog.fs) calls [startSearch](src/Shared/ViewModelSearch.fs) with the dialog query, model.zoomRoot, and model.graph. startSearch returns None when [parseSearchParts](src/Shared/ViewModelSearch.fs) returns None. An empty query and a whitespace query have no parts. The test [searchNodes empty and whitespace query returns no results](tests/Shared.Tests/SearchTests.fs) expects no hits for those queries.
6. **Walk graph.** A cursor from [startSearch](src/Shared/ViewModelSearch.fs) stores that graph, the part filters, and the discovery state. [takeResults](src/Shared/ViewModelSearch.fs) reads cursor.graph for later pages. A new page reads a new model graph only when cacheMatches in [SearchDialog](src/Client/SearchDialog.fs) fails and startCache runs again.
7. **Full list function.** [searchNodes](src/Shared/ViewModelSearch.fs) is a separate function. startCache in [SearchDialog](src/Client/SearchDialog.fs) calls startSearch. The dialog code does not call searchNodes. searchNodes returns [ExprDialog.tryHits](src/Shared/ExprDialog.fs) when the trimmed query starts with "=". For every other query, searchNodes calls startSearch. A cursor then goes to takeResults with count Int32.MaxValue. No cursor returns an empty list.
8. **Queue order.** nextDiscoveryNode in [ViewModelSearch](src/Shared/ViewModelSearch.fs) takes an id from the front of a queue and puts child ids at the back. The child ids are the list from [GraphChildren.get](src/Shared/Model.fs), in that list order. The test [searchNodes matches name or text under root BFS order](tests/Shared.Tests/SearchTests.fs) expects that sibling order.
9. **Two phases.** The first phase is ZoomPhase. The first queue holds the zoom root. An empty ZoomPhase queue sets RootPhase and queues graph.root. This is nextDiscoveryNode in [ViewModelSearch](src/Shared/ViewModelSearch.fs).
10. **Visit rules.** nextDiscoveryNode in [ViewModelSearch](src/Shared/ViewModelSearch.fs) skips a visited id and does not queue its children again. An id that is absent from graph.nodes is marked visited, is not a hit, and does not queue children. [GraphChildren.get](src/Shared/Model.fs) returns an empty list when the childMap key is absent. The comment on GraphChildren says an absent key means Unloaded.
11. **Hit rule.** [takeResults](src/Shared/ViewModelSearch.fs) counts a node only when the node matches every part filter. A node that does not match does not take a place in the page. A part matches when the node text contains the part, when [Filename.tryValue](src/Shared/Filename.fs) gives a string that contains the part, or when the node id is in the ref set for that part. The text test and the name test use ToLowerInvariant. startSearch builds the filters once from the query parts and from [RefExpr.refContext](src/Shared/RefExpr.fs) for the zoom root and the graph. A ref parse error stores an empty ref set. Later pages keep the stored filters. The test [searchNodes requires every whitespace-separated part in name or text](tests/Shared.Tests/SearchTests.fs) expects every part to match.

## 2. Screen

One screen is one page from [takeResults](src/Shared/ViewModelSearch.fs). The dialog page count in [SearchDialog](src/Client/SearchDialog.fs) is 12 hits.

1. **Page size.** searchPageSize in [SearchDialog](src/Client/SearchDialog.fs) is 12. loadPage calls takeResults with that count. The comment in that file says the 320px result viewport shows about nine 35px rows and the page keeps a small prefetch margin.
2. **Viewport.** The rule .amb-dialog-results in [style.css](src/Server/wwwroot/style.css) sets max-height to 320px and sets overflow-y to auto. The list item rule in that file sets padding to 8px 14px. A row height of 35px is absent from the style sheet. The result list is the element search-dialog-results in [gambol.template.html](src/Server/wwwroot/gambol.template.html).
3. **First page.** startCache calls loadPage one time. That is the first screen. currentSearchResults reverses resultsRev, so the shown order is the walk order. [SearchDialog](src/Client/SearchDialog.fs) prepends each new page onto resultsRev and the read reverses that list.
4. **Down key.** [searchSelectDownOp](src/Client/SearchDialog.fs) calls loadPage when the selected index plus one is greater than or equal to the loaded hit count. ArrowDown in [handleSearchKey](src/Client/SearchDialogView.fs) runs that operation. The selection then moves by one row and stops at the last loaded row.
5. **Scroll.** The scroll listener in [SearchDialogView](src/Client/SearchDialogView.fs) dispatches [loadMoreSearchResultsOp](src/Client/SearchDialog.fs) when scrollTop plus clientHeight is greater than or equal to scrollHeight minus 48. That operation calls loadPage one time. It does not change the selected index.
6. **Page end.** takeResults returns the hits and Some cursor when the requested count is full. The returned cursor keeps the same graph and the same filters, and it stores the discovery state after the last hit. takeResults returns None when discovery ends. loadPage does not call takeResults when the cursor is None. These returns are in [takeResults](src/Shared/ViewModelSearch.fs) and loadPage in [SearchDialog](src/Client/SearchDialog.fs).
7. **Resume test.** The test [search cursor resumes pages with searchNodes ordering](tests/Shared.Tests/SearchTests.fs) takes three pages of three hits. The joined ids equal the searchNodes ids for the same query, zoom root, and graph. The last cursor is None. That test does not set the dialog page size. A test that locks searchPageSize is absent.

## 3. Pick

[searchPickSetRoot](src/Shared/ViewModelSearch.fs) runs when Find picks a hit. The function sets the view root from that hit and returns no effects.

1. **Find command.** The Find command in [Commands](src/Client/Commands.fs) runs [findRootOp](src/Client/UpdateOps.fs). findRootOp calls openSearchDialogWithOnPick with the command name "Find" and with searchPickSetRoot.
2. **Selection run.** Enter and a result click in [SearchDialogView](src/Client/SearchDialogView.fs) call [runSearchSelectionOp](src/Client/SearchDialog.fs). That function reads the hit at the selected index from currentSearchResults. It calls onPick with that hit and with a model whose mode is returnTo. No hit returns the original model and an empty effect list. The function clears the search cache before the call.
3. **Zoom id.** [searchPickSetRoot](src/Shared/ViewModelSearch.fs) reads the hit from model.graph.nodes. It reads the child list with [GraphChildren.get](src/Shared/Model.fs). An empty child list calls [tryFindParentAndIndex](src/Shared/GraphQuery.fs). A parent result sets the zoom id to that parent and saves the child index. No parent sets the zoom id to the hit and saves no index. A non-empty child list sets the zoom id to the hit and saves no index.
4. **Absent child list.** [GraphChildren.get](src/Shared/Model.fs) returns an empty list when the childMap key is absent. [searchPickSetRoot](src/Shared/ViewModelSearch.fs) then uses the empty-list branch above. The function does not name Unloaded.
5. **Frame.** [searchPickSetRoot](src/Shared/ViewModelSearch.fs) calls [buildSiteMapFrom](src/Shared/ViewModelSiteMap.fs) for the zoom id and model.nextSiteId. It sets zoomRoot to the zoom id. It sets zoomIngress to [ownerPathIngress](src/Shared/ViewModelOccurrence.fs) for the zoom id. It sets mode to Selecting. The effect list is empty.
6. **Row selection.** A saved child index calls [childSelectionAt](src/Shared/ViewModelSiteMap.fs) with that index. No saved index calls [firstChildSelection](src/Shared/ViewModelSiteMap.fs). firstChildSelection returns None when that site-map entry has no children.
7. **Leaf test.** The test [searchPickSetRoot leaf fallback zooms parent and selects target](tests/Shared.Tests/ViewModelTests.fs) expects the parent as zoomRoot, the leaf as the selection, and mode Selecting.
8. **Sibling test.** The test [searchPickSetRoot leaf fallback selects non-first sibling not first child](tests/Shared.Tests/ViewModelTests.fs) expects the later sibling as the selection.
9. **Parent test.** The test [searchPickSetRoot with children zooms target and selects first child](tests/Shared.Tests/ViewModelTests.fs) expects the hit as zoomRoot and the first child as the selection.
10. **Outside zoom test.** The test [searchPickSetRoot reframes outside prior zoom when hit is not under zoom root](tests/Shared.Tests/ViewModelTests.fs) starts from another zoom root. The result zoom root is still the parent of the leaf, and the selection is the leaf.
11. **Ingress test.** The test [searchPickSetRoot seeds owner ingress for shared zoom-out](tests/Shared.Tests/ViewModelTests.fs) expects zoomIngress equal to ownerPathIngress for the hit. The zoom-out parent is the owner parent.

## 4. Trash

[ViewModelSearch](src/Shared/ViewModelSearch.fs) and [SearchDialog](src/Client/SearchDialog.fs) do not name trash. A trash skip is absent. A change for a start in trash is absent.

1. **No trash name.** [ViewModelSearch](src/Shared/ViewModelSearch.fs) does not name trash. [SearchDialog](src/Client/SearchDialog.fs) does not name trash. [SearchTests](tests/Shared.Tests/SearchTests.fs) does not name trash. A rule that skips trash is absent. A different procedure for a start in trash is absent.
2. **Every child.** nextDiscoveryNode in [ViewModelSearch](src/Shared/ViewModelSearch.fs) enqueues each child id from GraphChildren.get. [GraphChildren.get](src/Shared/Model.fs) does not remove an id.
3. **Trash id.** [startSearch](src/Shared/ViewModelSearch.fs) does not compare the zoom root with trashId. The trash id is defined in [GraphBuild](src/Shared/GraphBuild.fs). The start queue is the given zoom root for every call.

## 5. Root

[startSearch](src/Shared/ViewModelSearch.fs) uses two roots. The first root is the zoom root. The second root is the root field of the cursor graph.

1. **Zoom root.** startSearch in [ViewModelSearch](src/Shared/ViewModelSearch.fs) puts the zoom-root argument alone in the ZoomPhase queue.
2. **Dialog zoom root.** startCache in [SearchDialog](src/Client/SearchDialog.fs) passes model.zoomRoot. The view-model field comment in [ViewModel](src/Shared/ViewModel.fs) says the display starts at zoomRoot.
3. **Ref root.** The ref context for the part filters uses that same zoom root. [startSearch](src/Shared/ViewModelSearch.fs) calls [RefExpr.refContext](src/Shared/RefExpr.fs) with the zoom root and the graph. Later pages keep the filters on the cursor.
4. **Graph root.** An empty ZoomPhase queue in [ViewModelSearch](src/Shared/ViewModelSearch.fs) sets RootPhase and queues graph.root. graph.root is the root field on [Graph](src/Shared/Model.fs). The walk does not read Graph.rootId. A fact that the second root is always the canonical root id is absent from the search code.
5. **Dialog graph root.** The cursor graph is model.graph from startCache in [SearchDialog](src/Client/SearchDialog.fs). The second root is the root field of that graph.
6. **Second visit.** nextDiscoveryNode in [ViewModelSearch](src/Shared/ViewModelSearch.fs) skips an id that the visited set already holds. A zoom root equal to graph.root is already visited when RootPhase queues it. The test [search cursor terminates and deduplicates a cycle](tests/Shared.Tests/SearchTests.fs) expects one hit and a None cursor when a child has an owner edge to the graph root.
7. **Phase order.** The test [searchNodes phase A then B puts zoom subtree before rest of root tree](tests/Shared.Tests/SearchTests.fs) expects the hit under the zoom root before the hit that is a direct child of the graph root.
