namespace Gambol.Shared

open System

/// Volume ladder: Full / TreeStructure / TopLevel and directory-complete plans.
[<RequireQualifiedAccess>]
module WorkspaceSyncLimits =

    let maxNamesFull = 200
    let maxTransferBytes = 16L * 1024L * 1024L
    let maxStructurePaths = 1500
    let maxFileBytes = 4L * 1024L * 1024L

    type Mode =
        | Full
        | TreeStructure
        | TopLevel

    /// Inventory row with size for classify / plan (dirs use byteSize 0).
    type SizedItem =
        { relative: string
          isDirectory: bool
          byteSize: int64 }

    type FilePlan =
        | Body of byteSize: int64
        /// No WebDAV body transfer (TreeStructure / oversized); graph stub only.
        | StubOnly

    type PlannedPath =
        { relative: string
          isDirectory: bool
          file: FilePlan option }

    let nameCount (items: SizedItem list) = List.length items

    let isBodyTransfer (path: PlannedPath) =
        match path.file with
        | Some(Body _) -> true
        | _ -> false

    /// File paths that get a body PUT/GET (excludes dirs and StubOnly).
    let bodyTransfers (planned: PlannedPath list) =
        planned |> List.filter isBodyTransfer

    /// Soft-limit sum: bytes Full would transfer (exclude >4 MiB bodies).
    let transferByteSum (items: SizedItem list) =
        items
        |> List.choose (fun i ->
            if i.isDirectory then None
            elif i.byteSize > maxFileBytes then None
            else Some i.byteSize)
        |> List.sum

    let classify (items: SizedItem list) : Mode =
        let names = nameCount items
        let bytes = transferByteSum items

        if names <= maxNamesFull && bytes <= maxTransferBytes then
            Full
        elif names <= maxStructurePaths then
            TreeStructure
        else
            TopLevel

    let private parentOf (relative: string) =
        let i = relative.LastIndexOf('/')

        if i < 0 then
            ""
        else
            relative.Substring(0, i)

    let private isImmediateChild (scopeRel: string) (relative: string) =
        if relative = scopeRel then
            false
        elif scopeRel = "" then
            relative.IndexOf('/') < 0
        else
            let prefix = scopeRel + "/"

            relative.StartsWith(prefix, StringComparison.Ordinal)
            && relative.IndexOf('/', prefix.Length) < 0

    let private filePlanFor (mode: Mode) (byteSize: int64) =
        match mode with
        | TreeStructure -> StubOnly
        | Full
        | TopLevel ->
            if byteSize > maxFileBytes then
                StubOnly
            else
                Body byteSize

    let private toPlanned (mode: Mode) (item: SizedItem) : PlannedPath =
        if item.isDirectory then
            { relative = item.relative
              isDirectory = true
              file = None }
        else
            { relative = item.relative
              isDirectory = false
              file = Some(filePlanFor mode item.byteSize) }

    /// Files that receive a WebDAV body PUT/GET (no dirs, no StubOnly).
    let bodyUploadPaths (planned: PlannedPath list) = bodyTransfers planned

    /// Directory-complete plan for the classified mode under scope.
    /// Path set: Full/TreeStructure keep all; TopLevel = immediate children.
    let selectForVolume
        (scopeRelative: string)
        (items: SizedItem list)
        : Mode * SizedItem list =
        let mode = classify items

        let selected =
            match mode with
            | Full
            | TreeStructure -> items
            | TopLevel ->
                items
                |> List.filter (fun i ->
                    isImmediateChild scopeRelative i.relative)

        mode, selected

    /// Directory-complete plan for the classified mode under scope.
    let plan
        (scopeRelative: string)
        (items: SizedItem list)
        : Mode * PlannedPath list =
        let mode, selected = selectForVolume scopeRelative items
        mode, selected |> List.map (toPlanned mode)

    let filesByParent (paths: PlannedPath list) : Map<string, Set<string>> =
        paths
        |> List.filter (fun p -> not p.isDirectory)
        |> List.groupBy (fun p -> parentOf p.relative)
        |> List.map (fun (p, xs) ->
            p, xs |> List.map (fun x -> x.relative) |> Set.ofList)
        |> Map.ofList

    /// Directories in the plan with no planned file children.
    let emptyDirectories (planned: PlannedPath list) : string list =
        let parentsWithFiles =
            filesByParent planned |> Map.toList |> List.map fst |> Set.ofList

        planned
        |> List.choose (fun p ->
            if p.isDirectory then Some p.relative else None)
        |> List.filter (fun d -> not (Set.contains d parentsWithFiles))

    let private intendedFilesByParent
        (intended: SizedItem list)
        : Map<string, Set<string>> =
        intended
        |> List.filter (fun i -> not i.isDirectory)
        |> List.groupBy (fun i -> parentOf i.relative)
        |> List.map (fun (p, xs) ->
            p, xs |> List.map (fun x -> x.relative) |> Set.ofList)
        |> Map.ofList

    /// True when every parent's planned file set matches intended siblings.
    let isEveryDirectoryComplete
        (intended: SizedItem list)
        (planned: PlannedPath list)
        : bool =
        let intendedMap = intendedFilesByParent intended
        let plannedMap = filesByParent planned

        let parents =
            Set.union
                (intendedMap |> Map.toList |> List.map fst |> Set.ofList)
                (plannedMap |> Map.toList |> List.map fst |> Set.ofList)

        parents
        |> Set.forall (fun p ->
            let i =
                Map.tryFind p intendedMap |> Option.defaultValue Set.empty

            let pl =
                Map.tryFind p plannedMap |> Option.defaultValue Set.empty

            i = pl)
