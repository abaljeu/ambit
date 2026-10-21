namespace Gambol.Shared

[<RequireQualifiedAccess>]
type Notice =
    | NodeParsed of nodeId: NodeId
    | NodePersisted of nodeId: NodeId
    | MarkUnparsed of nodeId: NodeId
    | NodeUploading of nodeId: NodeId
    | NodeReceived of nodeId: NodeId
    | NodeParsing of nodeId: NodeId
    | NodePersisting of nodeId: NodeId
    | PersistFailed of nodeId: NodeId * message: string
