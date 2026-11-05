module Gambol.Client.FileSearchDialog

open Gambol.Shared
open Gambol.Shared.ViewModel

let mutable lastFileSearchQuery = ""

let rememberFileSearchQuery (q: string) : unit =
    lastFileSearchQuery <- q

/// The 320px result viewport shows about nine 35px rows; keep a small prefetch margin.
let private fileSearchPageSize = 12

type private FileSearchCache =
    { query: string
      focusNodeId: NodeId
      graph: Graph
      resultsRev: FileSearchResult list
      cursor: ViewModelFileSearch.FileSearchCursor option }

let mutable private fileSearchCache: FileSearchCache option = None
let mutable private shownRows: FileSearchResult list = []
let mutable private showZeroResults = false

let resetFileSearchResults () : unit =
    fileSearchCache <- None
    shownRows <- []
    showZeroResults <- false

let private publishFile (incoming: FileSearchResult list) : unit =
    let rows, zero =
        DialogResultRows.holdRows
            shownRows
            DialogResultRows.Readiness.Ready
            false
            incoming
    shownRows <- rows
    showZeroResults <- zero

let private focusNodeIdOpt (model: VM) : NodeId option =
    match model.selectedNodes with
    | None -> None
    | Some sel -> Some (focusedNodeId model.graph sel)

let insertDialogFocusIsWorkspaces (model: VM) : bool =
    match focusNodeIdOpt model with
    | Some focus -> focus = Graph.workspacesId
    | None -> false

let insertDialogShowsFileFolder (model: VM) : bool =
    match focusNodeIdOpt model with
    | Some focus -> focus <> Graph.workspacesId
    | None -> false

let private withCmdError (msg: string) (model: VM) : VM * Effect list =
    { model with lastCmdResult = Some (CmdLastResult.Error (None, msg)) }, []

let private cacheMatches
    (s: FileSearchDialogState)
    (focus: NodeId)
    (model: VM)
    (cache: FileSearchCache)
    : bool =
    cache.query = s.query
    && cache.focusNodeId = focus
    && LanguagePrimitives.PhysicalEquality cache.graph model.graph

let private loadPage (cache: FileSearchCache) : FileSearchCache =
    match cache.cursor with
    | None -> cache
    | Some cursor ->
        let page, nextCursor = ViewModelFileSearch.takeResults fileSearchPageSize cursor
        { cache with
            resultsRev = List.fold (fun results result -> result :: results) cache.resultsRev page
            cursor = nextCursor }

let private startCache (s: FileSearchDialogState) (focus: NodeId) (model: VM) : FileSearchCache =
    { query = s.query
      focusNodeId = focus
      graph = model.graph
      resultsRev = []
      cursor = ViewModelFileSearch.startFind s.query focus model.graph }
    |> loadPage

let private installFileCache
    (s: FileSearchDialogState)
    (model: VM)
    : FileSearchCache option =
    match focusNodeIdOpt model with
    | None ->
        fileSearchCache <- None
        publishFile []
        None
    | Some focus ->
        let cache = startCache s focus model
        fileSearchCache <- Some cache
        publishFile (List.rev cache.resultsRev)
        Some cache

let private ensureFileSearchCache
    (s: FileSearchDialogState)
    (model: VM)
    : FileSearchCache option =
    match focusNodeIdOpt model with
    | None -> installFileCache s model
    | Some focus ->
        match fileSearchCache with
        | Some cache when cacheMatches s focus model cache -> Some cache
        | _ -> installFileCache s model

let fileSearchShowsZeroResults (model: VM) : bool =
    match model.mode with
    | FileSearchDialog _ -> showZeroResults
    | _ -> false

let currentFileSearchResults (model: VM) : FileSearchResult list =
    match model.mode with
    | FileSearchDialog s ->
        ensureFileSearchCache s model |> ignore
        shownRows
    | _ -> []

let retargetFileSearch (query: string) (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s when s.query <> query ->
        let nextState = { s with query = query; selectedIndex = 0 }
        installFileCache nextState model |> ignore
        { model with mode = FileSearchDialog nextState }, []
    | _ -> model, []

let openFileSearchDialogOp (model: VM) : VM * Effect list =
    match focusNodeIdOpt model with
    | None -> withCmdError "no selection" model
    | Some focus when focus = Graph.workspacesId ->
        resetFileSearchResults ()
        { model with
            mode =
                FileSearchDialog
                    { query = lastFileSearchQuery
                      selectedIndex = 0
                      returnTo = model.mode } }, []
    | Some focus ->
        match Graph.resolveOwnedFileDirectoryInsert model.graph focus with
        | None -> withCmdError "cannot insert here" model
        | Some _ ->
            resetFileSearchResults ()
            { model with
                mode =
                    FileSearchDialog
                        { query = lastFileSearchQuery
                          selectedIndex = 0
                          returnTo = model.mode } }, []

let closeFileSearchDialogOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        rememberFileSearchQuery s.query
        resetFileSearchResults ()
        { model with mode = s.returnTo }, []
    | _ -> model, []

let fileSearchSelectUpOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        { model with
            mode =
                FileSearchDialog
                    { s with
                        selectedIndex = max 0 (s.selectedIndex - 1) } }, []
    | _ -> model, []

let fileSearchSelectDownOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        match ensureFileSearchCache s model with
        | None -> model, []
        | Some loaded ->
            let cache =
                if showZeroResults || s.selectedIndex + 1 < loaded.resultsRev.Length then
                    loaded
                else
                    loadPage loaded
            fileSearchCache <- Some cache
            if not showZeroResults then publishFile (List.rev cache.resultsRev)
            let count = if showZeroResults then 0 else shownRows.Length
            let next = min (s.selectedIndex + 1) (max 0 (count - 1))
            { model with mode = FileSearchDialog { s with selectedIndex = next } }, []
    | _ -> model, []

let loadMoreFileSearchResultsOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        match ensureFileSearchCache s model with
        | None -> model, []
        | Some cache ->
            let loaded = loadPage cache
            fileSearchCache <- Some loaded
            publishFile (List.rev loaded.resultsRev)
            model, []
    | _ -> model, []

let fileSearchSetQueryOp (query: string) (model: VM) : VM * Effect list =
    retargetFileSearch query model

let private tryResultAtIndex
    (selectedIndex: int)
    (results: FileSearchResult list)
    : FileSearchResult option =
    if results.IsEmpty then
        None
    else
        selectedIndex
        |> min (results.Length - 1)
        |> max 0
        |> fun index -> List.tryItem index results

let runFileSearchSelectionFromCache (s: FileSearchDialogState) (model: VM) : FileSearchResult option =
    currentFileSearchResults model |> tryResultAtIndex s.selectedIndex
