namespace Gambol.Shared

open System

[<Struct>]
type NodeId =
    | NodeId of Guid

    member this.Value =
        let (NodeId value) = this
        value

    /// Last 8 chars of `Guid.ToString()` (compact id suffix for messages).
    static member GuidTail8 (guid: Guid) : string =
        let s = guid.ToString()
        if s.Length >= 8 then s.Substring(s.Length - 8) else s

    static member New() = NodeId(Guid.NewGuid())


[<Struct>]
type Revision =
    | Revision of int

    member this.Value =
        let (Revision value) = this
        value

    static member Zero = Revision 0

type Ownership =
    | Ref
    | Owner

// For each id:NodeId exactly one will have ref: Owner.
type ChildNode =
    { ref: Ownership
      id: NodeId }

    static member New() : ChildNode =
        { ref = Ownership.Owner
          id = NodeId.New() }


type SpecialKind =
    | Workspaces
    | Workspace
    | Directory
    | File

type NodeKind =
    | Normal
    | Special of SpecialKind

[<RequireQualifiedAccess>]
module NodeKind =
    /// File, Directory, or named Workspace (artifact on disk).
    let artifact (kind: NodeKind) : bool =
        match kind with
        | Special (File | Directory | Workspace) -> true
        | _ -> false

    /// Workspace or Directory (can own nested artifacts).
    let container (kind: NodeKind) : bool =
        match kind with
        | Special (Workspace | Directory) -> true
        | _ -> false


type DocumentState =
    | Current
    | Unparsed


type Node =
    { id         : NodeId
      text       : string
      name       : Filename
      children   : ChildNode list
      cssClasses : CssClasses
      owner      : NodeId
      kind       : NodeKind
      documentState : DocumentState
      /// Mutation time via `touch`; after server persist, artifact disk mtime.
      updateTime : DateTime }


[<RequireQualifiedAccess>]
module NodeUpdateTime =
    /// Canonical nodes and JSON without `updateTime`.
    /// UTC kind so `toDbPrecision` does not shift through PostgreSQL `timestamptz`.
    let missing = DateTime(0L, DateTimeKind.Utc)

    /// PostgreSQL `timestamptz` stores microseconds; align before DB round-trip.
    let private ticksPerMicrosecond = 10L

    let toDbPrecision (time: DateTime) : DateTime =
        let utc =
            match time.Kind with
            | DateTimeKind.Utc -> time
            | DateTimeKind.Local -> time.ToUniversalTime()
            | DateTimeKind.Unspecified ->
                // PostgreSQL `timestamptz` via Npgsql/Dapper: UTC clock, Unspecified kind.
                DateTime.SpecifyKind(time, DateTimeKind.Utc)
            | _ -> time.ToUniversalTime()
        DateTime(utc.Ticks - utc.Ticks % ticksPerMicrosecond, DateTimeKind.Utc)

    let now () = DateTime.UtcNow |> toDbPrecision

    let touch (node: Node) : Node = { node with updateTime = now () }

    /// After server persist, artifact `updateTime` is the DataDir file mtime.
    /// Between edits, `touch` sets mutation time (FileSyncIndicator "edited").
    let withStamp (time: DateTime) (node: Node) : Node =
        { node with updateTime = toDbPrecision time }


type Node with
    /// Build a node; omit fields to use defaults (empty text/name/children/classes,
    /// owner = root Guid.Empty, kind = Normal, updateTime = missing).
    static member Create
        (
            id: NodeId,
            ?text: string,
            ?name: Filename,
            ?children: ChildNode list,
            ?cssClasses: CssClasses,
            ?owner: NodeId,
            ?kind: NodeKind,
            ?documentState: DocumentState,
            ?updateTime: DateTime
        ) : Node =
        { id = id
          text = defaultArg text ""
          name = defaultArg name Filename.Empty
          children = defaultArg children []
          cssClasses = defaultArg cssClasses CssClass.empty
          owner = defaultArg owner (NodeId Guid.Empty)
          kind = defaultArg kind Normal
          documentState = defaultArg documentState Current
          updateTime = defaultArg updateTime NodeUpdateTime.missing }


// Span of child indices [start, endd) under graph node `pnode` (parent NodeId).
type NodeRange =
    { pnode: NodeId
      start: int
      endd : int }

/// One row from node search (Ctrl+F); shared by ViewModelSearch and SearchDialog onPick.
type NodeSearchResult =
    { nodeId: NodeId
      text: string
      name: Filename }

type Graph =
    { root: NodeId
      nodes: Map<NodeId, Node>
      /// Child id -> structural parent and index (min parent NodeId wins when shared).
      parentByChild: Map<NodeId, NodeId * int>
      /// Child id -> graph parent along the single Ownership.Owner edge.
      ownerParentByChild: Map<NodeId, NodeId> }

/// Carries a fixed `Graph` and current `NodeId`; steps compose like `SiteNav`.
type NodeNav = NodeNav of Graph * NodeId option

[<RequireQualifiedAccess>]
module Node =
    let at (graph: Graph) (id: NodeId option) : NodeNav = NodeNav(graph, id)
    let current (NodeNav(_, id)) : NodeId option = id

    let private step f (NodeNav(g, id)) = NodeNav(g, f g id)

    /// Parent along the canonical `Ownership.Owner` edge. `None` id -> `None`.
    let owner =
        step (fun graph id ->
            id |> Option.bind (fun nid -> Map.tryFind nid graph.ownerParentByChild))

    let firstChild =
        step (fun graph id ->
            id
            |> Option.bind (fun nid ->
                Map.tryFind nid graph.nodes
                |> Option.bind (fun node ->
                    node.children |> List.tryHead |> Option.map (fun c -> c.id))))

    let lastChild =
        step (fun graph id ->
            id
            |> Option.bind (fun nid ->
                Map.tryFind nid graph.nodes
                |> Option.bind (fun node ->
                    let n = List.length node.children
                    if n = 0 then
                        None
                    else
                        List.tryItem (n - 1) node.children
                        |> Option.map (fun c -> c.id))))
