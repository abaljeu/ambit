open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()

    app.UseDefaultFiles() |> ignore
    app.UseStaticFiles() |> ignore

    app.MapGet("/api/hello", Func<obj>(fun () ->
        {| message = "Hello from Gambol server!" |} :> obj
    )) |> ignore

    app.Run()

    0 // Exit code
