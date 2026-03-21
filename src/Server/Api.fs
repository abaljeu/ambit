namespace Gambol.Server

open Microsoft.AspNetCore.Http

module Api =

    let private jsonResult (json: string) : IResult =
        Results.Content(json, "application/json")

    /// Lightweight poll response: { r: revision, b: deployEpochSec (__BUILD_TS__), p: pageBuildEpochSec }
    let getPoll (agent: FileAgent) (buildEpochSec: int) (pageBuildEpochSec: int) : Async<IResult> = async {
        let! rev = FileAgent.getRevision agent
        let json = sprintf "{\"r\":%d,\"b\":%d,\"p\":%d}" rev buildEpochSec pageBuildEpochSec
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
