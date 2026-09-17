// Thin FSI entry: argv + print. Upload lives in WorkspaceCloudUpload.

open System
open Gambol.Shared

let private clientHint = "stretch-workspace-upload"

let private parseArgs (argv: string[]) =
    let ambitBase =
        if argv.Length > 0 then argv.[0]
        else "http://127.0.0.1:5215/ambit"
    let mappedRoot =
        if argv.Length > 1 then argv.[1]
        else "/tmp/ambit-stretch-upload"
    let label =
        if argv.Length > 2 then argv.[2] else "stretch"
    { ambitBase = ambitBase
      mappedRoot = mappedRoot
      label = label
      clientHint = clientHint }

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
        "PASS workspace-created eventId=%s"
        (EventId.display proof.created.eventId)
    printfn "PASS stubs %s" proof.stubDetail
    printfn
        "PASS upload uploaded=%d detail=%s paths=%s"
        proof.pushed.uploaded
        proof.pushed.detail
        (String.concat "," proof.pushed.uploadedPaths)
    printfn "PASS mark %s" proof.markDetail
    printfn "PASS graph-eventId=%s" (EventId.display proof.state.eventId)
    printfn "PASS nodes %s" (String.concat "," names)

let private runArgv (argv: string[]) =
    let args = parseArgs argv
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
