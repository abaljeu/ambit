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
let mutable private shownRows: NodeSearchResult list = []
let mutable private showZeroResults = false
let mutable private pictureOutstanding = false

let resetSearchResults () : unit =
    searchCache <- None
    shownRows <- []
    showZeroResults <- false
    pictureOutstanding <- false

let private publishShown (incoming: NodeSearchResult list) : unit =
    let rows, zero =
        DialogResultRows.holdRows
            shownRows
            DialogResultRows.Readiness.Ready
            pictureOutstanding
            incoming
    shownRows <- rows
    showZeroResults <- zero

let private pictureWillStart (query: string) (hitCount: int) : bool =
    let key = SearchPicture.Text query
    let gap = SearchPicture.awaitGap hitCount key
    SearchPicture.startAfterGap gap key hitCount |> Option.isSome

let private clientHitCount (model: VM) (query: string) : int =
    ViewModelSearch.searchNodes query model.zoomRoot model.graph
    |> List.length

let private republishCached (s: SearchDialogState) (model: VM) : unit =
    match searchCache with
    | Some cache when cache.query = s.query && cache.zoomRoot = model.zoomRoot ->
        publishShown (List.rev cache.resultsRev)
    | _ -> ()

/// The quiet gap has ended for `armedText`. A newer query sends nothing.
let searchPictureEffects (armedText: string) (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        let hits = clientHitCount model s.query
        let armed = SearchPicture.Text armedText
        let current = SearchPicture.Text s.query
        let gap = SearchPicture.awaitGap hits armed
        match SearchPicture.startAfterGap gap current hits with
        | None ->
            pictureOutstanding <- false
            republishCached s model
            model, []
        | Some _ ->
            pictureOutstanding <- true
            model, [ RequestSearchPicture (s.query, model.zoomRoot) ]
    | _ -> model, []

let applyServerReply
    (reply: SearchPicture.Reply)
    (model: VM)
    : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        let hits = clientHitCount model s.query
        let key = SearchPicture.Text s.query
        let ids = SearchPicture.keepReply hits key s.serverHitIds reply
        if SearchPicture.applyReply hits key reply |> Option.isSome then
            pictureOutstanding <- false
            republishCached s model
        { model with
            mode = SearchDialog { s with serverHitIds = ids } }, []
    | _ -> model, []

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

let private installCache (s: SearchDialogState) (model: VM) : SearchCache =
    let cache = startCache s model
    searchCache <- Some cache
    publishShown (List.rev cache.resultsRev)
    cache

let private ensureSearchCache (s: SearchDialogState) (model: VM) : SearchCache =
    match searchCache with
    | Some cache when cacheMatches s model cache -> cache
    | _ -> installCache s model

let searchDialogShowsZeroResults (model: VM) : bool =
    match model.mode with
    | SearchDialog _ -> showZeroResults
    | _ -> false

let currentSearchResults (model: VM) : NodeSearchResult list =
    match model.mode with
    | SearchDialog s ->
        ensureSearchCache s model |> ignore
        shownRows
    | _ -> []

let retargetNodeSearch (query: string) (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s when s.query <> query ->
        let nextState =
            { s with
                query = query
                selectedIndex = 0
                serverHitIds = [] }
        let count = clientHitCount model query
        pictureOutstanding <- pictureWillStart query count
        installCache nextState model |> ignore
        { model with mode = SearchDialog nextState },
        [ ArmSearchQuietGap query ]
    | _ -> model, []

let openSearchDialogWithOnPick
    (invokedCommand: string)
    (onPick: NodeSearchResult -> VM -> VM * Effect list)
    (model: VM)
    : VM * Effect list =
    resetSearchResults ()
    let next =
        { model with
            mode =
                SearchDialog
                    { invokedCommand = invokedCommand
                      query = lastNodeSearchQuery
                      selectedIndex = 0
                      returnTo = model.mode
                      onPick = onPick
                      serverHitIds = [] } }
    next, [ ArmSearchQuietGap lastNodeSearchQuery ]

let closeSearchDialogOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        rememberSearchQuery s.query
        resetSearchResults ()
        { model with mode = s.returnTo }, [ CancelSearchQuietGap ]
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

let private holdingShownRows (cache: SearchCache) : bool =
    showZeroResults || (pictureOutstanding && cache.resultsRev.IsEmpty)

let searchSelectDownOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        let loaded = ensureSearchCache s model
        let holding = holdingShownRows loaded
        let cache =
            if holding || s.selectedIndex + 1 < loaded.resultsRev.Length then
                loaded
            else
                loadPage loaded
        searchCache <- Some cache
        if not holding then publishShown (List.rev cache.resultsRev)
        let count = if showZeroResults then 0 else shownRows.Length
        let next = min (s.selectedIndex + 1) (max 0 (count - 1))
        { model with mode = SearchDialog { s with selectedIndex = next } }, []
    | _ -> model, []

let loadMoreSearchResultsOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        let cache = ensureSearchCache s model |> loadPage
        searchCache <- Some cache
        publishShown (List.rev cache.resultsRev)
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
        match hit with
        | None -> model, []
        | Some hit ->
            resetSearchResults ()
            s.onPick hit closed
    | _ -> model, []
