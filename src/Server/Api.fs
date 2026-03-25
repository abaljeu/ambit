namespace Gambol.Server

open Microsoft.AspNetCore.Http
open Gambol.Shared
open Thoth.Json.Newtonsoft
open Thoth.Json.Newtonsoft

module Api =

    let private jsonResult (json: string) : IResult =
        Results.Content(json, "application/json")

    let getPoll (agent: FileAgent) (buildEpochSec: int) (pageBuildEpochSec: int) : Async<IResult> = async {
        let! rev = FileAgent.getRevision agent
        let poll: PollResponse =
            { revision = rev
              buildEpochSec = buildEpochSec
              pageBuildEpochSec = pageBuildEpochSec }
        let json = Encode.toString 0 (Serialization.encodePollResponse poll)
        return jsonResult json
    }

    let getState (agent: FileAgent) : Async<IResult> = async {
        let! json = FileAgent.getState agent
        return jsonResult json
    }

    let postChange (agent: FileAgent) (body: string) : Async<IResult> = async {
        let! result = FileAgent.postChange agent body
        match result with
        | Ok json -> return jsonResult json
        | Error err -> return Results.BadRequest({| error = err |})
    }
