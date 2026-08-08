module Gambol.Client.CommandDock

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Commands
open Gambol.Client.Update
open Gambol.Shared.CommandDockLayout
open Gambol.Shared.CommandCategory

module CommandMeta = Gambol.Shared.CommandEntry

// ---------------------------------------------------------------------------
// Compact command dock
// ---------------------------------------------------------------------------

let mutable private activeToolSurface : DockTriggerEntry option = None
let mutable private lastDockSnapshot : string option = None
let private svgNs = "http://www.w3.org/2000/svg"

let private dockSnapshot (model: VM) =
    let ctx = commandContextMode model.mode
    let sel = paletteWasSelecting ctx
    sprintf "%A|%A|%b" activeToolSurface ctx sel

let private makeDockIcon (iconId: string) : HTMLElement =
    let svg = document.createElementNS(svgNs, "svg")
    svg.setAttribute("class", "amb-dock-icon")
    svg.setAttribute("aria-hidden", "true")
    let useEl = document.createElementNS(svgNs, "use")
    useEl.setAttribute("href", "#" + iconId)
    svg.appendChild useEl |> ignore
    svg :?> HTMLElement

let private appendDockIcon (btn: HTMLButtonElement) (iconId: string) : unit =
    btn.appendChild (makeDockIcon iconId) |> ignore

let private makeDockRow (accentClass: string) : HTMLElement =
    let row = document.createElement "div"
    row.className <- "amb-dock " + accentClass
    row

let private addGlyphClasses (btn: HTMLButtonElement) (classes: string list) : unit =
    for cls in classes do
        if cls <> "" then btn.classList.add cls

/// Keep focus and caret in `#edit-input` when tapping dock buttons while editing.
let private preventDockFocusSteal (btn: HTMLButtonElement) : unit =
    btn.addEventListener("pointerdown", fun (ev: Event) ->
        (ev :?> PointerEvent).preventDefault())

let private makeIconButton
        (label: string)
        (iconId: string)
        (extraClasses: string list)
        (onClick: unit -> unit)
        : HTMLButtonElement =
    let btn = document.createElement "button" :?> HTMLButtonElement
    btn.``type`` <- "button"
    btn.className <- "amb-dock-glyph"
    btn.title <- label
    btn.setAttribute("aria-label", label)
    appendDockIcon btn iconId
    addGlyphClasses btn extraClasses
    preventDockFocusSteal btn
    btn.addEventListener ("click", fun _ -> onClick ())
    btn

let private makeCommandIconButton
        (cmd: CommandEntry2)
        (dispatch: Msg -> unit)
        : HTMLButtonElement =
    let btn = document.createElement "button" :?> HTMLButtonElement
    btn.``type`` <- "button"
    btn.className <- "amb-dock-glyph"
    let label = CommandMeta.displayName cmd.id
    btn.title <- label
    btn.setAttribute("aria-label", label)
    match CommandMeta.commandFor cmd.id with
    | Some meta ->
        match meta.iconId with
        | Some iconId -> appendDockIcon btn iconId
        | None -> ()
    | None -> ()
    preventDockFocusSteal btn
    match cmd.run () with
    | None -> btn.classList.add "amb-inactive"
    | Some op ->
        btn.addEventListener ("click", fun _ -> dispatch (ApplyOp op))
    btn

let private appendDockSlot
        (row: HTMLElement)
        (model: VM)
        (dispatch: Msg -> unit)
        (slot: DockSlot)
        (refresh: VM -> (Msg -> unit) -> unit)
        : unit =
    match slot with
    | DockTrigger entry ->
        let isOpen = activeToolSurface = Some entry
        let extra =
            if isOpen then [ "amb-dock-trigger-open"; triggerDockCssClass entry ] else []
        let toggle =
            makeIconButton entry.name entry.iconId extra
                (fun () ->
                    activeToolSurface <- Some entry
                    refresh model dispatch)
        row.appendChild toggle |> ignore
    | DockCommand id ->
        match tryFindCommand id with
        | None -> ()
        | Some cmd ->
            row.appendChild (makeCommandIconButton cmd dispatch) |> ignore

let private renderDockSlots
        (accentClass: string)
        (slots: DockSlot list)
        (model: VM)
        (dispatch: Msg -> unit)
        (refresh: VM -> (Msg -> unit) -> unit)
        : HTMLElement =
    let row = makeDockRow accentClass
    for slot in slots do
        appendDockSlot row model dispatch slot refresh
    row

let private renderTriggerPanel
        (trigger: DockTriggerEntry)
        (model: VM)
        (dispatch: Msg -> unit)
        (refresh: VM -> (Msg -> unit) -> unit)
        : HTMLElement =
    renderDockSlots (triggerDockCssClass trigger) trigger.slots model dispatch refresh

let rec renderCommandButtons (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.querySelector ".amb-command-buttons"
    if isNull container then () else
    let snapshot = dockSnapshot model
    if lastDockSnapshot <> Some snapshot then
        lastDockSnapshot <- Some snapshot
        container.innerHTML <- ""

        let refresh = renderCommandButtons
        let baseRow = renderDockSlots (dockCssClass Primary) baseStripSlots model dispatch refresh
        container.appendChild baseRow |> ignore

        match activeToolSurface with
        | Some trigger ->
            let row = renderTriggerPanel trigger model dispatch refresh
            container.appendChild row |> ignore
        | None -> ()
