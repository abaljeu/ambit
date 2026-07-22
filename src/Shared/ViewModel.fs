namespace Gambol.Shared

/// How to place the caret after focusing `#edit-input`.
[<RequireQualifiedAccess>]
type EditCaret =
    | EndOfText
    | Utf16Index of int
    | LastVisualLineAtClientX of float
    | FirstVisualLineAtClientX of float

[<RequireQualifiedAccess>]
module EditCaret =
    /// UTF-16 index clamped to `[0, textLen]` (former `moveEdit` rule with lower bound).
    let utf16ClampedToLength (cursorUtf16: int) (textLen: int) : EditCaret =
        EditCaret.Utf16Index (min (max 0 cursorUtf16) textLen)

type SiteId = Sid of int

/// A rendered appearance of a node in a flat site map. Each appearance gets a unique
/// instanceId so that fold state is per-occurrence, not per-NodeId.
/// In a DAG (including cyclic graphs) the same NodeId may appear multiple times with
/// independent fold states. Cycle termination relies on lazy expansion: a new entry
/// starts collapsed with children = [], so recursion stops naturally.
type SiteEntry =
    { instanceId: SiteId
      nodeId: NodeId
      parentInstanceId: SiteId option   // None = root
      expanded: bool
      childrenStale: bool            // true when children list may not match graph; re-synced on expand
      children: SiteId list }        // instanceId list, ordered to match graph.children (valid when not stale)

/// Flat map keyed by instanceId. O(log S) per-entry access for all operations.
type SiteMap =
    { rootId: SiteId
      entries: Map<SiteId, SiteEntry>
      /// Non-root instanceId -> `parentInstanceId` (root has no key).
      parentByInstanceId: Map<SiteId, SiteId> }

[<RequireQualifiedAccess>]
module SiteMap =
    let private withEntry (siteMap: SiteMap) (id: SiteId option) (f: SiteEntry -> 'a option) : 'a option =
        id |> Option.bind (fun sid -> Map.tryFind sid siteMap.entries |> Option.bind f)

    /// One step up the instance parent chain. Uses `parentByInstanceId` (root has no key).
    /// Prefer `Site.at siteMap id |> Site.parent` when composing; `None` in → `None` out.
    let siteParent (siteMap: SiteMap) (id: SiteId option) : SiteId option =
        id
        |> Option.bind (fun sid ->
            if sid = siteMap.rootId then
                None
            else
                Map.tryFind sid siteMap.parentByInstanceId)

    /// First child instance under this occurrence (`children` head). Composes like `siteParent`.
    let siteFirstChild (siteMap: SiteMap) (id: SiteId option) : SiteId option =
        withEntry siteMap id (fun e -> List.tryHead e.children)

    /// Last child instance under this occurrence. None if `children` is empty.
    let siteLastChild (siteMap: SiteMap) (id: SiteId option) : SiteId option =
        withEntry siteMap id (fun e -> List.tryItem (e.children.Length - 1) e.children)

    /// 0-based index of `child` in `parent`'s `children` list. `None` if either id is
    /// missing, the parent entry is missing, or `child` is not a direct child of that
    /// parent instance.
    let siteChildIndex (siteMap: SiteMap) (parent: SiteId option) (child: SiteId option) : int option =
        withEntry siteMap parent 
                    (fun p -> 
                        child |> Option.bind (fun cid -> 
                            List.tryFindIndex ((=) cid) p.children))

    let private siteSiblingOffset (delta: int) (siteMap: SiteMap) (id: SiteId option) : SiteId option =
        withEntry siteMap id (fun e ->
            e.parentInstanceId
            |> Option.bind (fun pid ->
                Map.tryFind pid siteMap.entries
                |> Option.bind (fun parent ->
                    List.tryFindIndex ((=) e.instanceId) parent.children
                    |> Option.bind (fun i -> List.tryItem (i + delta) parent.children))))

    /// Next sibling under the same parent (`children` order). Root has no siblings.
    let siteNext = siteSiblingOffset 1

    /// Previous sibling under the same parent. Root has no siblings.
    let sitePrev = siteSiblingOffset -1
    let nodeIsExpanded (siteMap: SiteMap) (instanceId: SiteId option) : bool =
        withEntry siteMap instanceId (fun e -> if e.expanded then Some () else None)
        |> Option.isSome

/// Carries a fixed SiteMap and a current position. Every step is `SiteNav -> SiteNav`,
/// so paths compose freely with `>>` without repeating `siteMap`.
///   let prevCousin = Site.parent >> Site.prev >> Site.lastChild
///   Site.at siteMap (Some id) |> prevCousin |> Site.current
type SiteNav = SiteNav of SiteMap * SiteId option

[<RequireQualifiedAccess>]
module Site =
    let at (siteMap: SiteMap) (id: SiteId option) : SiteNav = SiteNav(siteMap, id)
    let current (SiteNav(_, id)) : SiteId option = id

    let private step f (SiteNav(sm, id)) = SiteNav(sm, f sm id)

    let parent     = step SiteMap.siteParent
    let firstChild = step SiteMap.siteFirstChild
    let lastChild  = step SiteMap.siteLastChild
    let next       = step SiteMap.siteNext
    let prev       = step SiteMap.sitePrev
    let prevCousin = parent >> prev >> lastChild

    /// 0-based index of the current `SiteId` among its parent's `children`, or `None` if
    /// there is no current id, the entry is missing, or the id is the site-map root.
    let childIndex (nav: SiteNav) : int option =
        let (SiteNav (sm, id)) = nav
        let parentId = nav |> parent |> current
        SiteMap.siteChildIndex sm parentId id

/// Like `SiteNav`, but `at` / each step keep `SiteId` only when fold-visible on that `SiteMap`.
type VisiNav = VisiNav of SiteMap * SiteId option

[<RequireQualifiedAccess>]
module VisibleSite =
    let rec private siteEntryIsVisible (siteMap: SiteMap) (sid: SiteId) : bool =
        match Map.tryFind sid siteMap.entries with
        | None -> false
        | Some entry ->
            match entry.parentInstanceId with
            | None -> sid = siteMap.rootId
            | Some pid ->
                match Map.tryFind pid siteMap.entries with
                | None -> false
                | Some parent ->
                    if not parent.expanded then
                        false
                    elif not (List.exists ((=) sid) parent.children) then
                        false
                    else
                        siteEntryIsVisible siteMap pid

    let at (siteMap: SiteMap) (id: SiteId option) : VisiNav =
        let filtered =
            id
            |> Option.bind (fun sid ->
                if siteEntryIsVisible siteMap sid then
                    Some sid
                else
                    None)

        VisiNav(siteMap, filtered)

    let current (VisiNav(_, id)) : SiteId option = id

    let private step f (VisiNav(sm, id)) =
        let nextId = f sm id

        let filtered =
            nextId
            |> Option.bind (fun sid ->
                if siteEntryIsVisible sm sid then
                    Some sid
                else
                    None)

        VisiNav(sm, filtered)

    let parent = step SiteMap.siteParent
    let firstChild = step SiteMap.siteFirstChild
    let lastChild = step SiteMap.siteLastChild
    let next = step SiteMap.siteNext
    let prev = step SiteMap.sitePrev
    let prevCousin = parent >> prev >> lastChild

/// A contiguous span of children under a specific site-map occurrence of a parent node.
/// parent is a SiteEntry (not just a NodeId) so the selection is unambiguous in a DAG
/// where the same NodeId may appear at multiple positions.
type SiteNodeRange =
    { parent: SiteEntry
      start: int
      endd: int }

[<RequireQualifiedAccess>]
module SiteNodeRange =
    /// The SiteEntry for the first node in the range, if in bounds.
    let firstChild (range: SiteNodeRange) (siteMap: SiteMap) : SiteEntry option =
        range.parent.children
        |> List.tryItem range.start
        |> Option.bind (fun id -> Map.tryFind id siteMap.entries)

    /// The SiteEntry for the last node in the range, if in bounds.
    let lastChild (range: SiteNodeRange) (siteMap: SiteMap) : SiteEntry option =
        if range.endd > 0 then
            range.parent.children
            |> List.tryItem (range.endd - 1)
            |> Option.bind (fun id -> Map.tryFind id siteMap.entries)
        else
            None

/// A SiteNodeRange with a focus index marking the "active" end used for Shift-Arrow and editing.
/// focus is always range.start or range.endd - 1.
type Selection =
    { range: SiteNodeRange
      focus: int }

/// Self-contained snapshot of copied/cut nodes for internal clipboard.
/// Independent of graph.nodes — survives graph mutations and snapshot reload.
type ClipboardContent =
    { topLevelIds: NodeId list
      nodes: Map<NodeId, Node> }

type SyncState =
    | Idle                       // all confirmed, nothing pending
    | Sending of attempt: int    // POST in-flight; attempt = 1-based send count
    | Polling                    // GET poll in-flight
    | Uploading                  // workspace push in progress (blocks poll)
    | WaitingToRetry of attempt: int * baseRevision: int * changes: Change list
    | ServerRejected  // server returned 400 — change cannot be applied; reload required
    | CodeOutdated    // server has newer code (build stamp changed) — reload required
    | DataOutdated    // server has newer data with no local pending — reload required

type SyncInfo =
    { syncState: SyncState
      pendingChanges: Change list
      isPollingActive: bool
      syncRiskAcknowledged: bool }

[<RequireQualifiedAccess>]
module SyncInfo =
    let initial: SyncInfo =
        { syncState = Idle
          pendingChanges = []
          isPollingActive = false
          syncRiskAcknowledged = false }

    let withPendingChanges (pending: Change list) (si: SyncInfo) : SyncInfo =
        { si with pendingChanges = pending }

    /// Updates sync state. Clears risk acknowledgment when crossing the risk boundary.
    let withSyncState (newState: SyncState) (si: SyncInfo) : SyncInfo =
        let inRisk =
            function
            | ServerRejected | CodeOutdated | DataOutdated -> true
            | _ -> false
        let wasR = inRisk si.syncState
        let nowR = inRisk newState
        if wasR = nowR then { si with syncState = newState }
        else { si with syncState = newState; syncRiskAcknowledged = false }

type Effect =
    | SubmitPendingBatch of baseRevision: int * changes: Change list
    | PollServer of revision: int
    | ScheduleRetry of delayMs: int
    | SavePendingQueue of Change list
    | RequestDesktopFileStatus of nodeId: NodeId * path: string
    | RequestServerFileStatus of nodeId: NodeId * path: string
    /// After create: inventory + top-level stubs, then ContinueWorkspacePush.
    | ContinueWorkspaceStubsThenPush of WorkspaceSyncScope
    /// Deferred workspace push; Some fileId → parse that file after push.
    | ContinueWorkspacePush of WorkspaceSyncScope * parseFileId: NodeId option

/// Row / active-file indicator vocabulary (desktop status + absent artifacts).
type DesktopFileIndicator =
    | BlankFileIndicator
    | CheckingFileStatus of nodeId: NodeId * path: string
    | InvalidFileReferenceIndicator
    | AbsentArtifactIndicator
    | FileStatusIndicator of
        nodeId: NodeId *
        path: string *
        status: DesktopFileStatus *
        sourceModifiedUtc: System.DateTime option

[<RequireQualifiedAccess>]
module DesktopFileIndicator =
    /// Fixed labels for payload-free cases (status text uses FileSyncIndicator).
    let textByState : Map<DesktopFileIndicator, string> =
        [ BlankFileIndicator, ""
          InvalidFileReferenceIndicator, "invalid"
          AbsentArtifactIndicator, "missing" ]
        |> Map.ofList

    let toText (state: DesktopFileIndicator) : string =
        match state with
        | CheckingFileStatus _ -> "..."
        | FileStatusIndicator _ -> ""
        | other -> Map.find other textByState

/// Result of the most recent command, shown in `#cmd-last-result`.
/// Optional `commandName` is the registry display name (`None` = anonymous / no prefix).
[<RequireQualifiedAccess>]
type CmdLastResult =
    | Ok of commandName: string option
    | Detail of commandName: string option * message: string
    | Error of commandName: string option * message: string

module CmdLastResult =
    let withCommandName (name: string option) = function
        | CmdLastResult.Ok _ -> CmdLastResult.Ok name
        | CmdLastResult.Detail (_, msg) -> CmdLastResult.Detail (name, msg)
        | CmdLastResult.Error (_, msg) -> CmdLastResult.Error (name, msg)

    let private formatNamed (name: string option) (body: string) : string =
        match name with
        | None -> body
        | Some n -> sprintf "%s: %s" n body

    let toDisplay = function
        | CmdLastResult.Ok name -> formatNamed name "OK"
        | CmdLastResult.Detail (name, msg) -> formatNamed name msg
        | CmdLastResult.Error (name, msg) -> formatNamed name msg

    let formatDisplay = function
        | None -> ""
        | Some r -> toDisplay r

/// UI mode; `SearchDialog.onPick` closes over model updates (mutually recursive with `VM`).
type Mode =
    | Selecting
    /// `caret` placement after `#edit-input` receives focus (see `manageFocus`).
    | Editing of originalText: string * caret: EditCaret
    | CommandPalette of query: string * selectedCommand: int * returnTo: Mode
    | SearchDialog of SearchDialogState
    | FileSearchDialog of FileSearchDialogState
    | CssClassPrompt of returnTo: Mode * initialValue: string
    | RenamePrompt of returnTo: Mode * initialValue: string

/// Node search overlay: query, selection, and `onPick` (mutually recursive with `Mode` / `VM`).
and SearchDialogState =
    { invokedCommand: string
      query: string
      selectedIndex: int
      returnTo: Mode
      onPick: NodeSearchResult -> VM -> VM * Effect list }

/// File search overlay: path query, list selection, and optional create via **New** button.
and FileSearchDialogState =
    { query: string
      selectedIndex: int
      returnTo: Mode }

// Server `State` is in `FileAgent`, and mainly the graph.
and VM = // the client state
    { graph: Graph // the core data
      revision: Revision
      history: History
      selectedNodes: Selection option
      mode: Mode
      siteMap: SiteMap
      nextSiteId: SiteId
      zoomRoot: NodeId // display starting from here
      /// Stack of (parentId, childIndex) ingress occurrences for zoom-out.
      zoomIngress: (NodeId * int) list
      clipboard: ClipboardContent option
      desktopCapabilities: DesktopCapabilities option
      serverCapabilities: ServerCapabilities option
      desktopFileIndicator: DesktopFileIndicator
      syncInfo: SyncInfo
      lastCmdResult: CmdLastResult option }

/// Self-contained pure model transformation for the client update loop (see `Msg.ApplyOp`).
type Updater = VM -> VM * Effect list

type SubmitNetworkErrorKind =
    | FetchFailed
    | ClientTimeout

/// Messages dispatched by async server callbacks (not directly caused by user input).
type SystemMsg =
    | StateLoaded of Graph * Revision
    | SubmitResponse of ackedChangeIds: System.Guid list * revision: Revision
    | SubmitRejected of detail: string // server HTTP error (decoded `error` or short body snippet)
    | SubmitNetworkError of
        baseRevision: int * changes: Change list * kind: SubmitNetworkErrorKind
    | DesktopCapabilitiesDetected of DesktopCapabilities option
    | ServerCapabilitiesDetected of ServerCapabilities option
    | DesktopFileStatusReceived of
        nodeId: NodeId *
        path: string *
        status: DesktopFileStatus *
        sourceModifiedUtc: System.DateTime option
    | SetPollingActive of bool
    | PollTick            // polling timer fired; update decides whether to emit PollServer effect
    | PollDone of SyncState option * Change list   // poll GET response arrived
    | RetrySubmit         // retry timer fired; update resends the stored batch snapshot

type Msg =
    | SysMsg of SystemMsg
    | AckSyncRisk
    | ApplyOp of Updater

/// Client `manageFocus`: when this is true, the live DOM caret in `#edit-input` must not be
/// overwritten from `model.mode` — e.g. contenteditable typing with only `syncInfo` changed.
[<RequireQualifiedAccess>]
module EditingCaretPreserve =
    let shouldPreserveDomCaret (previousModel: VM option) (model: VM) : bool =
        match previousModel, model.mode with
        | Some prev, Editing _ ->
            LanguagePrimitives.PhysicalEquality prev.mode model.mode
            && LanguagePrimitives.PhysicalEquality prev.graph model.graph
            && LanguagePrimitives.PhysicalEquality prev.siteMap model.siteMap
            && LanguagePrimitives.PhysicalEquality prev.selectedNodes model.selectedNodes
        | _ -> false
