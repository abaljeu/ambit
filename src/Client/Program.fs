module App

open Browser.Dom
open Fable.Core
open Gambol.Shared

[<Emit("fetch($0).then(r => r.text()).then($1)")>]
let fetchText (url: string) (callback: string -> unit) : unit = jsNative

let rec renderNode (container: Browser.Types.Element) (graph: Graph) (depth: int) (nodeId: NodeId) =
    let node = graph.nodes.[nodeId]
    let row = document.createElement "div"
    row.classList.add "row"
    for _ in 1 .. depth do
        let indent = document.createElement "div"
        indent.classList.add "indent"
        row.appendChild indent |> ignore
    let text = document.createElement "div"
    text.classList.add "text"
    text.textContent <- node.text
    row.appendChild text |> ignore
    container.appendChild row |> ignore
    for childId in node.children do
        renderNode container graph (depth + 1) childId

let app = document.getElementById "app"

fetchText "/state" (fun text ->
    let decoder =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "graph" Serialization.decodeGraph)
    match Thoth.Json.JavaScript.Decode.fromString decoder text with
    | Ok graph ->
        app.innerHTML <- ""
        let root = graph.nodes.[graph.root]
        for childId in root.children do
            renderNode app graph 0 childId
    | Error err ->
        app.textContent <- $"Error: {err}"
)
