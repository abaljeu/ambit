namespace Gambol.Server

open Microsoft.AspNetCore.Http

module Api =

    let private jsonResult (json: string) : IResult =
        Results.Content(json, "application/json")

    let getState (agent: FileAgent) : Async<IResult> = async {
        let! json = agent.GetState()
        return jsonResult json
    }

    let postChange (agent: FileAgent) (body: string) : Async<IResult> = async {
        let! result = agent.PostChange(body)
        match result with
        | Ok json -> return jsonResult json
        | Error err -> return Results.BadRequest({| error = err |})
    }
