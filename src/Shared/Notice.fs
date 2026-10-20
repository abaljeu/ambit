namespace Gambol.Shared

[<RequireQualifiedAccess>]
type Notice =
    | NodeParsed of nodeId: NodeId
    | SnapshotDone of nodeId: NodeId
    | MarkUnparsed of nodeId: NodeId
    | NodeUploading of nodeId: NodeId
    | NodeReceived of nodeId: NodeId
    | NodeParsing of nodeId: NodeId
    | NodePersisting of nodeId: NodeId
