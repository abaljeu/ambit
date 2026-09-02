namespace Gambol.Server

open System
open Gambol.Shared

[<RequireQualifiedAccess>]
module GraphOnlyChangePost =

    module JsonEncode = Thoth.Json.Newtonsoft.Encode

    let encodeChange revision ops =
        let change =
            { id = revision
              changeId = Guid.NewGuid()
              ops = ops }
        JsonEncode.toString 0 (
            Serialization.encodeChangeBatch { changes = [ change ] })

    let rec postChunks
        (post: string -> Async<Result<string, string>>)
        (revision: int)
        (chunks: Op list list)
        : Async<Result<unit, string>> =
        match chunks with
        | [] -> async.Return(Ok ())
        | chunk :: rest ->
            async {
                let! result = post (encodeChange revision chunk)
                match result with
                | Error err -> return Error err
                | Ok _ -> return! postChunks post (revision + 1) rest
            }
