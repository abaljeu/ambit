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

let resetFileSearchResults () : unit =
    fileSearchCache <- None

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

let private ensureFileSearchCache (s: FileSearchDialogState) (model: VM) : FileSearchCache option =
    match focusNodeIdOpt model with
    | None -> None
    | Some focus ->
        match fileSearchCache with
        | Some cache when cacheMatches s focus model cache -> Some cache
        | _ ->
            let cache = startCache s focus model
            fileSearchCache <- Some cache
            Some cache

let currentFileSearchResults (model: VM) : FileSearchResult list =
    match model.mode with
    | FileSearchDialog s ->
        match ensureFileSearchCache s model with
        | None -> []
        | Some cache -> List.rev cache.resultsRev
    | _ -> []

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
        | Some cache ->
            let loaded = cache.resultsRev.Length
            let cache =
                if s.selectedIndex + 1 >= loaded then loadPage cache else cache
            fileSearchCache <- Some cache
            let n = cache.resultsRev.Length
            let cap = max 0 (n - 1)
            let next = min (s.selectedIndex + 1) cap
            { model with mode = FileSearchDialog { s with selectedIndex = next } }, []
    | _ -> model, []

let loadMoreFileSearchResultsOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        match ensureFileSearchCache s model with
        | None -> model, []
        | Some cache ->
            fileSearchCache <- Some (loadPage cache)
            model, []
    | _ -> model, []

let fileSearchSetQueryOp (query: string) (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        resetFileSearchResults ()
        { model with
            mode = FileSearchDialog { s with query = query; selectedIndex = 0 } }, []
    | _ -> model, []

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
