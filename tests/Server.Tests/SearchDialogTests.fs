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

let private findModel () : VM * NodeId * NodeId =
    SearchDialog.resetSearchResults ()
    let graph0 = Graph.create ()
    let graph1, nodeIds =
        ModelBuilder.createNodes [ "alpha row"; "beta row" ] graph0
    match nodeIds with
    | alphaId :: betaId :: _ ->
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
                      serverHitIds = [] } },
        alphaId,
        betaId
    | _ -> failwith "expected alpha and beta"

let private searchAt (query: string) (model: VM) : VM =
    match model.mode with
    | SearchDialog state ->
        { model with
            mode = SearchDialog { state with query = query; serverHitIds = [] } }
    | _ -> failwith "expected Find"

let private fileSearchModel () : VM * NodeId * NodeId =
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
    let notesId = NodeId.New()
    let notes =
        Node.Create(
            notesId,
            text = "notes.md",
            name = Filename.create "notes.md",
            owner = dirId,
            kind = Special File)
    let readmeId = NodeId.New()
    let readme =
        Node.Create(
            readmeId,
            text = "readme.md",
            name = Filename.create "readme.md",
            owner = dirId,
            kind = Special File)
    let graph3 =
        graph2
        |> Graph.addDetachedNode notes
        |> Graph.addDetachedNode readme
    let graph =
        replaceChildren
            dirId
            [ ChildNode.owner notesId; ChildNode.owner readmeId ]
            graph3
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
        notesId,
        readmeId

[<Fact>]
let ``query change shows the new Find list`` () =
    let model, alphaId, betaId = findModel ()
    let prior = SearchDialog.currentSearchResults model
    Assert.Equal<NodeId list>([ alphaId ], ids prior)
    let beta, _ = Update.update (NodeSearchQuery "beta") model
    Assert.Equal<NodeId list>(
        [ betaId ],
        ids (SearchDialog.currentSearchResults beta))
    let miss, _ = Update.update (NodeSearchQuery "zzzz-absent") beta
    Assert.Empty(SearchDialog.currentSearchResults miss)

[<Fact>]
let ``query change shows the new file search list`` () =
    let model, notesId, readmeId = fileSearchModel ()
    let prior = FileSearchDialog.currentFileSearchResults model
    Assert.Equal<NodeId list>([ notesId ], fileIds prior)
    let readme, _ = Update.update (FileSearchQuery "readme") model
    Assert.Equal<NodeId list>(
        [ readmeId ],
        fileIds (FileSearchDialog.currentFileSearchResults readme))
    let miss, _ = Update.update (FileSearchQuery "zzzz-absent") readme
    Assert.Empty(FileSearchDialog.currentFileSearchResults miss)

[<Fact>]
let ``same text adds server hits and an empty reply keeps local rows`` () =
    let model, alphaId, betaId = findModel ()
    let local = SearchDialog.currentSearchResults model
    Assert.Equal<NodeId list>([ alphaId ], ids local)
    let reply: SearchPicture.Reply =
        { ids = [ betaId ]
          matchKey = SearchPicture.Text "alpha" }
    let withServer, effects =
        SearchDialog.applyServerReply
            ViewModelSearch.searchHitCap
            reply
            []
            model
    Assert.Empty(effects)
    Assert.Equal<NodeId list>(
        [ alphaId; betaId ],
        ids (SearchDialog.currentSearchResults withServer))
    let emptyReply: SearchPicture.Reply =
        { ids = []
          matchKey = SearchPicture.Text "alpha" }
    let kept, _ =
        SearchDialog.applyServerReply
            ViewModelSearch.searchHitCap
            emptyReply
            []
            withServer
    Assert.Equal<NodeId list>(
        [ alphaId ],
        ids (SearchDialog.currentSearchResults kept))

[<Fact>]
let ``dialog appends a server hit that was not resident`` () =
    let model, alphaId, _ = findModel ()
    let local = SearchDialog.currentSearchResults model
    Assert.Equal<NodeId list>([ alphaId ], ids local)
    let absentId = NodeId.New()
    let absent = Node.Create(absentId, text = "alpha elsewhere")
    Assert.False(model.graph.nodes.ContainsKey absentId)
    let reply: SearchPicture.Reply =
        { ids = [ alphaId; absentId ]
          matchKey = SearchPicture.Text "alpha" }
    let withServer, effects =
        SearchDialog.applyServerReply
            ViewModelSearch.searchHitCap
            reply
            [ absent ]
            model
    Assert.Empty(effects)
    let shown = SearchDialog.currentSearchResults withServer
    Assert.Equal<NodeId list>([ alphaId; absentId ], ids shown)
    Assert.Equal("alpha elsewhere", (List.last shown).text)
    Assert.Equal(
        1,
        shown |> List.filter (fun row -> row.nodeId = alphaId) |> List.length)

[<Fact>]
let ``dialog lists a repeated server-only hit once`` () =
    let found, _, _ = findModel ()
    let quiet = searchAt "zzzz-absent" found
    Assert.Empty(SearchDialog.currentSearchResults quiet)
    let absentId = NodeId.New()
    let otherId = NodeId.New()
    let absent = Node.Create(absentId, text = "zzzz-absent")
    let other = Node.Create(otherId, text = "zzzz-absent too")
    let reply: SearchPicture.Reply =
        { ids = [ absentId; absentId; otherId ]
          matchKey = SearchPicture.Text "zzzz-absent" }
    let withServer, effects =
        SearchDialog.applyServerReply 2 reply [ absent; other ] quiet
    Assert.Empty(effects)
    Assert.Equal<NodeId list>(
        [ absentId; otherId ],
        ids (SearchDialog.currentSearchResults withServer))

[<Fact>]
let ``same text keeps Find rows when a later read is empty`` () =
    let model, alphaId, _ = findModel ()
    let prior = SearchDialog.currentSearchResults model
    Assert.Equal<NodeId list>([ alphaId ], ids prior)
    let later = { model with graph = Graph.create () }
    Assert.Equal<NodeId list>(
        [ alphaId ],
        ids (SearchDialog.currentSearchResults later))

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

    let found, _, _ = findModel ()
    let find = searchAt "zzzz-absent" found
    Assert.Empty(SearchDialog.currentSearchResults find)
    let afterFind, findEffects =
        SearchDialog.runSearchSelectionOp find.mode find
    Assert.Empty(findEffects)
    match afterFind.mode with
    | SearchDialog state -> Assert.Equal("zzzz-absent", state.query)
    | _ -> failwith "expected Find to stay open"
    let afterDown, _ = SearchDialog.searchSelectDownOp find
    match afterDown.mode with
    | SearchDialog down -> Assert.Equal(0, down.selectedIndex)
    | _ -> failwith "expected Find to stay open"
    let afterUp, _ = SearchDialog.searchSelectUpOp find
    match afterUp.mode with
    | SearchDialog up -> Assert.Equal(0, up.selectedIndex)
    | _ -> failwith "expected Find to stay open"

    let files, _, _ = fileSearchModel ()
    let fileSearch =
        match files.mode with
        | FileSearchDialog state ->
            { files with mode = FileSearchDialog { state with query = "zzzz-absent" } }
        | _ -> failwith "expected file search"
    FileSearchDialog.resetFileSearchResults ()
    Assert.Empty(FileSearchDialog.currentFileSearchResults fileSearch)
    let afterFile, fileEffects =
        UpdateFileSearch.runFileSearchSelectionOp fileSearch.mode fileSearch
    Assert.Empty(fileEffects)
    match afterFile.mode with
    | FileSearchDialog state -> Assert.Equal("zzzz-absent", state.query)
    | _ -> failwith "expected file search to stay open"
    let afterFileDown, _ = FileSearchDialog.fileSearchSelectDownOp fileSearch
    match afterFileDown.mode with
    | FileSearchDialog down -> Assert.Equal(0, down.selectedIndex)
    | _ -> failwith "expected file search to stay open"
