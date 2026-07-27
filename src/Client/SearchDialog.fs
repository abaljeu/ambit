module Gambol.Client.SearchDialog

open Gambol.Shared
open Gambol.Shared.ViewModel

let mutable lastNodeSearchQuery = ""

let private rememberSearchQuery (q: string) : unit =
    lastNodeSearchQuery <- q

/// The 320px result viewport shows about nine 35px rows; keep a small prefetch margin.
let private searchPageSize = 12

type private SearchCache =
    { query: string
      zoomRoot: NodeId
      graph: Graph
      resultsRev: NodeSearchResult list
      cursor: ViewModelSearch.SearchCursor option }

let mutable private searchCache: SearchCache option = None

let resetSearchResults () : unit =
    searchCache <- None

let private cacheMatches (s: SearchDialogState) (model: VM) (cache: SearchCache) : bool =
    cache.query = s.query
    && cache.zoomRoot = model.zoomRoot
    && LanguagePrimitives.PhysicalEquality cache.graph model.graph

let private loadPage (cache: SearchCache) : SearchCache =
    match cache.cursor with
    | None -> cache
    | Some cursor ->
        let page, nextCursor = ViewModelSearch.takeResults searchPageSize cursor
        { cache with
            resultsRev = List.fold (fun results result -> result :: results) cache.resultsRev page
            cursor = nextCursor }

let private startCache (s: SearchDialogState) (model: VM) : SearchCache =
    { query = s.query
      zoomRoot = model.zoomRoot
      graph = model.graph
      resultsRev = []
      cursor = ViewModelSearch.startSearch s.query model.zoomRoot model.graph }
    |> loadPage

let private ensureSearchCache (s: SearchDialogState) (model: VM) : SearchCache =
    match searchCache with
    | Some cache when cacheMatches s model cache -> cache
    | _ ->
        let cache = startCache s model
        searchCache <- Some cache
        cache

let currentSearchResults (model: VM) : NodeSearchResult list =
    match model.mode with
    | SearchDialog s -> ensureSearchCache s model |> fun cache -> List.rev cache.resultsRev
    | _ -> []

let openSearchDialogWithOnPick
    (invokedCommand: string)
    (onPick: NodeSearchResult -> VM -> VM * Effect list)
    (model: VM)
    : VM * Effect list =
    resetSearchResults ()
    { model with
        mode =
            SearchDialog
                { invokedCommand = invokedCommand
                  query = lastNodeSearchQuery
                  selectedIndex = 0
                  returnTo = model.mode
                  onPick = onPick } }, []

let closeSearchDialogOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        rememberSearchQuery s.query
        resetSearchResults ()
        { model with mode = s.returnTo }, []
    | _ -> model, []

let searchSelectUpOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        { model with
            mode =
                SearchDialog
                    { s with
                        selectedIndex = max 0 (s.selectedIndex - 1) } }, []
    | _ -> model, []

let searchSelectDownOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        let cache = ensureSearchCache s model
        let loaded = cache.resultsRev.Length
        let cache =
            if s.selectedIndex + 1 >= loaded then loadPage cache else cache
        searchCache <- Some cache
        let n = cache.resultsRev.Length
        let cap = max 0 (n - 1)
        let next = min (s.selectedIndex + 1) cap
        { model with mode = SearchDialog { s with selectedIndex = next } }, []
    | _ -> model, []

let loadMoreSearchResultsOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        let cache = ensureSearchCache s model |> loadPage
        searchCache <- Some cache
        model, []
    | _ -> model, []

let private tryResultAtIndex
    (selectedIndex: int)
    (results: NodeSearchResult list)
    : NodeSearchResult option =
    if results.IsEmpty then
        None
    else
        selectedIndex
        |> min (results.Length - 1)
        |> max 0
        |> fun index -> List.tryItem index results

let runSearchSelectionOp (mode: Mode) (model: VM) : VM * Effect list =
    match mode with
    | SearchDialog s ->
        rememberSearchQuery s.query
        let closed = { model with mode = s.returnTo }
        let hit = currentSearchResults model |> tryResultAtIndex s.selectedIndex
        resetSearchResults ()
        match hit with
        | None -> model, []
        | Some hit -> s.onPick hit closed
    | _ -> model, []
