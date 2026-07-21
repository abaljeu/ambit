namespace Gambol.Shared

/// Half-open byte/char range in a normalized artifact: [start, end_).
[<RequireQualifiedAccess>]
type TextSpan = { start: int; end_: int }

/// Nested parse tree: parent span encloses children (outline depth or Xml DOM).
/// Every artifact byte belongs to some node's span. No interstitial-only nodes.
[<RequireQualifiedAccess>]
type SpanNode = {
    span: TextSpan
    text: string
    hardKey: string option
    /// Some = graph-bound; None = synthetic root or warm row not yet matched.
    /// Never means "file-only interstitial" — blanks/prologue fold into neighbor spans.
    nodeId: NodeId option
    children: SpanNode list
}

/// RQA: keeps `nodes` off the unqualified field pool (same reason as AmbDocumentReadResult).
[<RequireQualifiedAccess>]
type DocumentNodesRead = {
    documentRootId: NodeId
    nodes: Map<NodeId, Node>
}

/// Per-format codec face used by DocumentFormat dispatch.
[<RequireQualifiedAccess>]
type DocumentHandler = {
    parse: string -> Graph -> NodeId -> Result<SpanNode, string>
    readCold: string -> Graph -> NodeId -> Result<DocumentNodesRead, string>
    readWarm: string -> Graph -> NodeId -> string -> Result<DocumentNodesRead, string>
    write: Graph -> NodeId -> string option -> Result<string, string>
}
