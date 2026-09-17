// Thin FSI entry: argv + print. Upload lives in WorkspaceCloudUpload.

open System
open Gambol.Shared

let private nodeLabels (graph: Graph) =
    graph.nodes
    |> Map.toList
    |> List.choose (fun (_, node) ->
        match node.kind, Filename.tryValue node.name with
        | Special Workspace, Some name -> Some("workspace:" + name)
        | Special File, Some name -> Some("file:" + name)
        | Special Directory, Some name -> Some("directory:" + name)
        | _ -> None)

let private printProof (proof: WorkspaceCloudUploadProof) =
    let names = nodeLabels proof.state.graph
    printfn
        "PASS workspace-created eventId=%d"
        (EventId.value proof.created.eventId)
    printfn "PASS stubs %s" proof.stubDetail
    printfn
        "PASS upload uploaded=%d detail=%s paths=%s"
        proof.pushed.uploaded
        proof.pushed.detail
        (String.concat "," proof.pushed.uploadedPaths)
    printfn "PASS mark %s" proof.markDetail
    printfn "PASS graph-eventId=%d" (EventId.value proof.state.eventId)
    printfn "PASS nodes %s" (String.concat "," names)

let private runArgv (argv: string[]) =
    let args = WorkspaceCloudUpload.parseArgs argv
    printfn "ambitBase=%s" args.ambitBase
    printfn "mappedRoot=%s" args.mappedRoot
    printfn "label=%s" args.label
    match WorkspaceCloudUpload.run args with
    | Error e ->
        eprintfn "FAIL %s" e
        1
    | Ok proof ->
        printProof proof
        0

let argv =
    match fsi.CommandLineArgs |> Array.toList with
    | [] -> [||]
    | _script :: rest -> List.toArray rest

exit (runArgv argv)
