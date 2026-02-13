module App

open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop

let app = document.getElementById "app"
app.textContent <- "Hello from Gambol (Fable client)!"

[<Emit("fetch($0).then(r => r.json()).then($1)")>]
let fetchJson (url: string) (callback: obj -> unit) : unit = jsNative

fetchJson "/api/hello" (fun data ->
    let msg: string = !!data?message
    app.textContent <- "Server says: " + msg
)
