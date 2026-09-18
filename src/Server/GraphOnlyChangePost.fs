namespace Gambol.Server

open System
open Gambol.Shared

[<RequireQualifiedAccess>]
module GraphOnlyChangePost =

    let mint (commandName: string) (ops: Op list) : Ev =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Parse"
          commandName = commandName
          body = EventBody.Change ops }

    let rec postChunks
        (post: Ev -> Async<Result<CoreChangesAccepted, string>>)
        (commandName: string)
        (chunks: Op list list)
        : Async<Result<unit, string>> =
        match chunks with
        | [] -> async.Return(Ok ())
        | chunk :: rest ->
            async {
                let event = mint commandName chunk
                let! result = post event
                match result with
                | Error err -> return Error err
                | Ok _ -> return! postChunks post commandName rest
            }
