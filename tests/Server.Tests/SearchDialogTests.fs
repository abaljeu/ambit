module Gambol.Server.Tests.SearchDialogTests

open Gambol.Client
open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

let private ids (results: NodeSearchResult list) : NodeId list =
    results |> List.map (fun result -> result.nodeId)

let private fileIds (results: FileSearchResult list) : NodeId list =
    results |> List.map (fun result -> result.nodeId)

let private replaceChildren (parentId: NodeId) (kids: ChildNode list) (graph: Graph) : Graph =
    match Graph.replace parentId 0 [] kids graph with
    | Ok next -> next
    | Error message -> failwith message

let private findModel () : VM =
    SearchDialog.resetSearchResults ()
    let graph0 = Graph.create ()
    let graph1, nodeIds = ModelBuilder.createNodes [ "alpha row" ] graph0
    let graph = replaceChildren graph1.root (ChildNode.owners nodeIds) graph1
    let model = VmTestHelpers.emptyModel graph
    { model with
        mode =
            SearchDialog
                { invokedCommand = "Find"
                  query = "alpha"
                  selectedIndex = 0
                  returnTo = Selecting
                  onPick = fun _ next -> next, []
                  serverHitIds = [] } }

let private fileSearchModel () : VM * NodeId =
    FileSearchDialog.resetFileSearchResults ()
    let graph0 = Graph.create ()
    let dirId = NodeId.New()
    let dir =
        Node.Create(
            dirId,
            text = "docs",
            name = Filename.create "docs",
            kind = Special Directory)
    let graph1 = Graph.addDetachedNode dir graph0
    let graph2 = replaceChildren Graph.rootId [ ChildNode.owner dirId ] graph1
    let fileId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = "notes.md",
            name = Filename.create "notes.md",
            owner = dirId,
            kind = Special File)
    let graph3 = Graph.addDetachedNode file graph2
    let graph = replaceChildren dirId [ ChildNode.owner fileId ] graph3
    let model = VmTestHelpers.emptyModel graph
    match ViewModel.firstChildSelection model.siteMap graph.root with
    | None -> failwith "expected the docs directory"
    | Some selection ->
        { model with
            selectedNodes = Some selection
            mode =
                FileSearchDialog
                    { query = "notes"
                      selectedIndex = 0
                      returnTo = Selecting } },
        fileId

[<Fact>]
let ``query change keeps prior Find rows`` () =
    let model = findModel ()
    let prior = SearchDialog.currentSearchResults model
    Assert.NotEmpty(prior)
    let next, _ = Update.update (NodeSearchQuery "zzzz-absent") model
    let held = SearchDialog.shownSearchResults ()
    Assert.Equal<NodeId list>(ids prior, ids held)
    Assert.Empty(SearchDialog.currentSearchResults next)

[<Fact>]
let ``query change keeps prior file search rows`` () =
    let model, fileId = fileSearchModel ()
    let prior = FileSearchDialog.currentFileSearchResults model
    Assert.Equal<NodeId list>([ fileId ], fileIds prior)
    let next, _ = Update.update (FileSearchQuery "zzzz-absent") model
    let held = FileSearchDialog.shownFileSearchResults ()
    Assert.Equal<NodeId list>([ fileId ], fileIds held)
    Assert.Empty(FileSearchDialog.currentFileSearchResults next)

[<Fact>]
let ``empty result list shows the zero-results row`` () =
    Assert.Equal<string list>(
        [ "Zero results" ],
        SearchDialogChrome.dialogResultLabels [])
    Assert.Equal<string list>(
        [ "alpha" ],
        SearchDialogChrome.dialogResultLabels [ "alpha" ])

[<Fact>]
let ``pick on an empty result list does nothing`` () =
    Assert.Equal(None, SearchDialogChrome.dialogResultPick [] 0)
    Assert.Equal(Some 0, SearchDialogChrome.dialogResultPick [ "alpha" ] 0)

    let find = findModel ()
    let findQuery, _ = Update.update (NodeSearchQuery "zzzz-absent") find
    let _ = SearchDialog.currentSearchResults findQuery
    let afterFind, findEffects =
        SearchDialog.runSearchSelectionOp findQuery.mode findQuery
    Assert.Empty(findEffects)
    match afterFind.mode with
    | SearchDialog s -> Assert.Equal("zzzz-absent", s.query)
    | _ -> failwith "expected Find to stay open"

    let fileSearch, _ = fileSearchModel ()
    let fileQuery, _ = Update.update (FileSearchQuery "zzzz-absent") fileSearch
    let _ = FileSearchDialog.currentFileSearchResults fileQuery
    match fileQuery.mode with
    | FileSearchDialog state ->
        Assert.Equal(None, FileSearchDialog.runFileSearchSelectionFromCache state fileQuery)
        let afterDown, _ = FileSearchDialog.fileSearchSelectDownOp fileQuery
        match afterDown.mode with
        | FileSearchDialog down -> Assert.Equal(0, down.selectedIndex)
        | _ -> failwith "expected file search to stay open"
    | _ -> failwith "expected file search"

    let afterDown, _ = SearchDialog.searchSelectDownOp findQuery
    match afterDown.mode with
    | SearchDialog down -> Assert.Equal(0, down.selectedIndex)
    | _ -> failwith "expected Find to stay open"
