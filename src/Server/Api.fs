namespace Gambol.Server

open Microsoft.AspNetCore.Http
open Gambol.Shared
open Thoth.Json.Newtonsoft

/// Thin abstraction over FileAgent and DbAgent so Api functions are backend-agnostic.
type AgentHandle =
    { getState        : unit -> Async<string>
      getRevision     : unit -> Async<int>
      getChangesSince : int -> Async<Change list>
      postChange      : string -> Async<Result<string, string>> }

[<RequireQualifiedAccess>]
module AgentHandle =
    let ofFile (agent: FileAgent) : AgentHandle =
        { getState        = fun () -> FileAgent.getState agent
          getRevision     = fun () -> FileAgent.getRevision agent
          getChangesSince = fun after -> FileAgent.getChangesSince agent after
          postChange      = fun body -> FileAgent.postChange agent body }

    let ofDb (agent: DbAgent) : AgentHandle =
        { getState        = fun () -> DbAgent.getState agent
          getRevision     = fun () -> DbAgent.getRevision agent
          getChangesSince = fun after -> DbAgent.getChangesSince agent after
          postChange      = fun body -> DbAgent.postChange agent body }

module Api =

    let private jsonResult (json: string) : IResult =
        Results.Content(json, "application/json")

    let getPoll
        (handle: AgentHandle)
        (buildEpochSec: int)
        (pageBuildEpochSec: int)
        (clientRev: int)
        : Async<IResult> = async {
        let! rev = handle.getRevision ()
        let! changes =
            if rev > clientRev then handle.getChangesSince clientRev
            else async.Return []
        let poll: PollResponse =
            { revision = rev
              buildEpochSec = buildEpochSec
              pageBuildEpochSec = pageBuildEpochSec
              changes = changes }
        let json = Encode.toString 0 (Serialization.encodePollResponse poll)
        return jsonResult json
    }

    let getState (handle: AgentHandle) : Async<IResult> = async {
        let! json = handle.getState ()
        return jsonResult json
    }

    let postChange (handle: AgentHandle) (body: string) : Async<IResult> = async {
        let! result = handle.postChange body
        match result with
        | Ok json -> return jsonResult json
        | Error err -> return Results.BadRequest({| error = err |})
    }
