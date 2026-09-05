namespace Gambol.Server

open System
open Gambol.Shared

[<RequireQualifiedAccess>]
module GraphOnlyChangePost =

    let rec postChunks
        (post: Change list -> Async<Result<CoreChangesAccepted, string>>)
        (revision: int)
        (chunks: Op list list)
        : Async<Result<unit, string>> =
        match chunks with
        | [] -> async.Return(Ok ())
        | chunk :: rest ->
            async {
                let change =
                    { id = revision
                      changeId = Guid.NewGuid()
                      ops = chunk }
                let! result = post [ change ]
                match result with
                | Error err -> return Error err
                | Ok accepted ->
                    return! postChunks post accepted.revision.Value rest
            }
