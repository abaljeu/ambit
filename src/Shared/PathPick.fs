namespace Gambol.Shared

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

/// GitHub labels and local-mapping labels for one plain Load choice.
type WorkspaceMappingPresence = {
    githubLabels: Set<string>
    localLabels: Set<string>
}

/// Operation, pre-pick, and subject. The chosen path stays outside this record.
type LoadPathChoice = {
    operation: LoadSaveOperation
    prePick: LoadSavePrePick
    subject: LoadSubject option
}

[<RequireQualifiedAccess>]
module PathPick =

    /// Remote flag for `resolve`. Plain Save uses `plainSavePath` and the Save actor.
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

    /// Load of a Directory, a File, or a non-subject is the parse-stack
    /// Load of that node. It does no git pull or push, even on explicit git.
    let private nonWorkspaceLoad (choice: LoadPathChoice) =
        choice.operation = LoadSaveOperation.Load
        && choice.subject <> Some LoadSubject.Workspace

    /// Load of a Workspace and Save enter their actors on Plain or Git.
    /// Non-Workspace Load and explicit Desk stay Desk.
    /// The Load and Save actors choose git or Neither from the work tree.
    let resolveCommand
        (choice: LoadPathChoice)
        : Result<LoadSavePath, string> =
        match choice.prePick with
        | LoadSavePrePick.Desk -> Ok LoadSavePath.Desk
        | _ when nonWorkspaceLoad choice -> Ok LoadSavePath.Desk
        | LoadSavePrePick.Plain
        | LoadSavePrePick.Git -> Ok LoadSavePath.Git

    let private normalizedLabel (label: string) =
        if isNull label then ""
        else label.Trim().ToLowerInvariant()

    let private labeledWorkspace (graph: Graph) (nodeId: NodeId) =
        NodeDesktopPath.enclosingWorkspaceName graph nodeId
        |> Option.map normalizedLabel

    let mappingPresence
        (remoteLabels: Set<string>)
        (localLabels: Set<string>)
        : WorkspaceMappingPresence =
        { githubLabels = remoteLabels
          localLabels = localLabels }

    /// Plain Save chooses Desk only for a local mapping and no remote.
    /// Any remote, and a workspace with no local mapping, stay on the Save actor.
    let plainSavePath
        (graph: Graph)
        (nodeId: NodeId)
        (prePick: LoadSavePrePick)
        (presence: WorkspaceMappingPresence)
        : LoadSavePath option =
        match prePick, labeledWorkspace graph nodeId with
        | LoadSavePrePick.Plain, Some label
            when Set.contains label presence.localLabels
                 && not (Set.contains label presence.githubLabels) ->
            Some LoadSavePath.Desk
        | _ -> None

    /// Plain Load of a Workspace chooses Git when it has both mappings.
    /// A Directory or a File stays plain, so it does no git pull.
    let plainLoadPath
        (graph: Graph)
        (nodeId: NodeId)
        (prePick: LoadSavePrePick)
        (presence: WorkspaceMappingPresence)
        : LoadSavePath option =
        match prePick, labeledWorkspace graph nodeId with
        | LoadSavePrePick.Plain, Some label
            when subjectOf graph nodeId = Some LoadSubject.Workspace
                 && Set.contains label presence.githubLabels
                 && Set.contains label presence.localLabels ->
            Some LoadSavePath.Git
        | _ -> None
