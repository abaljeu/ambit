namespace Gambol.Shared

[<RequireQualifiedAccess>]
type LoadSavePath =
    | Git
    | Desk

[<RequireQualifiedAccess>]
type LoadSavePrePick =
    | Plain
    | Git
    | Desk

[<RequireQualifiedAccess>]
type LoadSaveOperation =
    | Load
    | Save

/// Load subject. A node that is none of these is not a Load subject.
[<RequireQualifiedAccess>]
type LoadSubject =
    | Workspace
    | Directory
    | File

[<RequireQualifiedAccess>]
type LoadSubjectRejection =
    | NotLoadSubject
    | NotFound

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LoadSubject =

    /// Workspace, Directory, or File. Any other node is not a Load subject.
    let ofId
        (graph: Graph)
        (nodeId: NodeId)
        : Result<LoadSubject, LoadSubjectRejection> =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error LoadSubjectRejection.NotFound
        | Some node ->
            match node.kind with
            | Special Workspace -> Ok LoadSubject.Workspace
            | Special Directory -> Ok LoadSubject.Directory
            | Special File -> Ok LoadSubject.File
            | _ -> Error LoadSubjectRejection.NotLoadSubject

    let private isSubjectNode (node: Node) =
        match node.kind with
        | Special (Workspace | Directory | File) -> true
        | _ -> false

    /// Nearest Workspace, Directory, or File on the owner chain, inclusive.
    let enclosingSubject
        (graph: Graph)
        (nodeId: NodeId)
        : (NodeId * LoadSubject) option =
        GraphQuery.enclosing graph isSubjectNode nodeId
        |> Option.bind (fun id ->
            match ofId graph id with
            | Ok subject -> Some(id, subject)
            | Error _ -> None)

/// Operation, pre-pick, and subject. The chosen path stays outside this record.
type LoadPathChoice = {
    operation: LoadSaveOperation
    prePick: LoadSavePrePick
    subject: LoadSubject option
}

[<RequireQualifiedAccess>]
module PathPick =

    let choose (remoteExists: bool) : LoadSavePath =
        if remoteExists then
            LoadSavePath.Git
        else
            LoadSavePath.Desk

    let resolve
        (prePick: LoadSavePrePick)
        (remoteExists: unit -> Result<bool, string>)
        : Result<LoadSavePath, string> =
        match prePick with
        | LoadSavePrePick.Plain ->
            remoteExists () |> Result.map choose
        | LoadSavePrePick.Git -> Ok LoadSavePath.Git
        | LoadSavePrePick.Desk -> Ok LoadSavePath.Desk

    let subjectOf (graph: Graph) (focusId: NodeId) : LoadSubject option =
        match LoadSubject.ofId graph focusId with
        | Ok subject -> Some subject
        | Error _ -> None

    /// Plain asks remoteExists. An explicit Git or Desk pick stays.
    let resolveCommand
        (choice: LoadPathChoice)
        (remoteExists: unit -> Result<bool, string>)
        : Result<LoadSavePath, string> =
        resolve choice.prePick remoteExists
