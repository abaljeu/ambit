module Gambol.Client.SearchDialogChrome

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Client.Controller
open Gambol.Client.JsInterop
open Gambol.Shared.CommandCategory

let dialogQueryDebounceMs = 50

let private dialogOpenClass = "amb-dialog-open"

type DialogHeading = {
    contextId: string
    title: string
    inputId: string
    query: string
}

type DialogKeys = {
    closeDialog: Updater
    moveSelectionUp: Updater
    moveSelectionDown: Updater
    acceptSelection: Updater
}

type DialogKeyAction = {
    keys: DialogKeys
    flushQuery: unit -> unit
    dispatch: Msg -> unit
}

type DialogListWire = {
    input: HTMLInputElement
    resultsId: string
    onInput: unit -> unit
    onKey: KeyboardEvent -> unit
    onNearEnd: unit -> unit
    onResultClick: int -> unit
}

type TimeoutSlot = {
    clearTimeout: unit -> unit
    armTimeout: int -> (unit -> unit) -> unit
}

type WireOnce = {
    runOnce: (unit -> unit) -> unit
}

type DialogRenderStamp<'scope> = {
    query: string
    scope: 'scope
    graph: Graph
    selectedIndex: int
}

type DialogRenderScroll<'scope> = {
    scrollFor: DialogRenderStamp<'scope> -> bool
    resetScroll: unit -> unit
}

let private dockAccentClasses =
    [ "amb-dock-base"; "amb-dock-move"; "amb-dock-select"; "amb-dock-more"; "amb-dock-file" ]

let private setDialogDockAccent (el: HTMLElement) (cls: string) : unit =
    dockAccentClasses |> List.iter (fun c -> el.classList.remove c)
    el.classList.add cls

/// The field leads the model by a debounce. A later render must not write the
/// stale query back over keys still in the field.
let private syncDialogQuery (wasOpen: bool) (query: string) (input: HTMLInputElement) : unit =
    let focused =
        not (isNull document.activeElement)
        && System.Object.ReferenceEquals(document.activeElement, input)
    if not wasOpen then
        input.value <- query
    elif not focused && input.value <> query then
        input.value <- query

let private focusDialogInput (input: HTMLInputElement) : unit =
    window.setTimeout((fun _ ->
        focusPreventScroll input
        input.select()), 0) |> ignore

let showDialogHeading (container: HTMLElement) (heading: DialogHeading) : HTMLInputElement =
    let wasOpen = container.classList.contains dialogOpenClass
    container.classList.add dialogOpenClass
    let ctx = document.getElementById heading.contextId
    if not (isNull ctx) then
        ctx.textContent <- heading.title
        setDialogDockAccent ctx (searchDialogDockCssClass heading.title)
    let input = document.getElementById heading.inputId :?> HTMLInputElement
    syncDialogQuery wasOpen heading.query input
    if not wasOpen then focusDialogInput input
    input

let hideDialog (container: HTMLElement) : unit =
    container.classList.remove dialogOpenClass

let formatNamedDialogHit (name: Filename) (text: string) : string =
    match name with
    | Filename.Ok label -> $"{label}  {text}"
    | _ -> text

let renderDialogResultList
    (container: HTMLElement)
    (items: string list)
    (selectedIndex: int)
    (scrollSelection: bool)
    : unit =
    let ul = container.querySelector ".amb-dialog-results" :?> HTMLElement
    let previousScrollTop = ul.scrollTop
    ul.innerHTML <- ""
    let clampedSel = if items.IsEmpty then 0 else min selectedIndex (items.Length - 1)
    let selectedLi =
        items
        |> List.mapi (fun i label ->
            let li = document.createElement "li"
            li.textContent <- label
            li.dataset.["idx"] <- string i
            let selected = i = clampedSel
            if selected then li.classList.add "amb-dialog-selected"
            ul.appendChild li |> ignore
            if selected then Some li else None)
        |> List.tryPick id
    if scrollSelection then
        selectedLi |> Option.iter scrollIntoViewNearest
    else
        ul.scrollTop <- previousScrollTop

let private dialogResultIndex (li: HTMLElement) : int =
    let idxStr = li.dataset.["idx"]
    if System.String.IsNullOrEmpty idxStr then 0 else int idxStr

let private dialogScrolledNearEnd (ul: HTMLElement) : bool =
    ul.scrollTop + ul.clientHeight >= ul.scrollHeight - 48.0

let handleDialogKey (action: DialogKeyAction) (ke: KeyboardEvent) : unit =
    if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
        ke.preventDefault()
    let overlay op =
        ke.preventDefault()
        action.flushQuery ()
        action.dispatch (ApplyOp op)
    match formatKeyCombo ke with
    | "Escape" -> overlay action.keys.closeDialog
    | "ArrowUp" -> overlay action.keys.moveSelectionUp
    | "ArrowDown" -> overlay action.keys.moveSelectionDown
    | "Enter" -> overlay action.keys.acceptSelection
    | _ -> ()

let wireDialogList (wire: DialogListWire) : unit =
    let ul = document.getElementById wire.resultsId
    wire.input.addEventListener("input", fun _ -> wire.onInput ())
    wire.input.addEventListener("keydown", fun (ev: Event) ->
        wire.onKey (ev :?> KeyboardEvent))
    ul.addEventListener("scroll", fun _ ->
        if dialogScrolledNearEnd ul then wire.onNearEnd ())
    ul.addEventListener("click", fun (ev: Event) ->
        let target = ev.target :?> HTMLElement
        match target.closest "li" with
        | None -> ()
        | Some li -> wire.onResultClick (dialogResultIndex (li :?> HTMLElement)))

let createTimeoutSlot () : TimeoutSlot =
    let timer = ref (None: float option)
    let clearTimeout () =
        match timer.Value with
        | None -> ()
        | Some id ->
            window.clearTimeout id |> ignore
            timer.Value <- None
    let armTimeout delayMs action =
        clearTimeout ()
        timer.Value <-
            Some (
                window.setTimeout(
                    (fun _ ->
                        timer.Value <- None
                        action ()),
                    delayMs))
    { clearTimeout = clearTimeout; armTimeout = armTimeout }

let createWireOnce () : WireOnce =
    let wired = ref false
    { runOnce = fun action ->
        if not wired.Value then
            wired.Value <- true
            action () }

let private sameDialogStamp<'scope when 'scope : equality>
    (left: DialogRenderStamp<'scope>)
    (right: DialogRenderStamp<'scope>)
    : bool =
    left.query = right.query
    && left.scope = right.scope
    && left.selectedIndex = right.selectedIndex
    && LanguagePrimitives.PhysicalEquality left.graph right.graph

let createDialogRenderScroll<'scope when 'scope : equality> () : DialogRenderScroll<'scope> =
    let last = ref (None: DialogRenderStamp<'scope> option)
    let scrollFor stamp =
        let unchanged =
            match last.Value with
            | Some prev -> sameDialogStamp prev stamp
            | None -> false
        last.Value <- Some stamp
        not unchanged
    { scrollFor = scrollFor
      resetScroll = fun () -> last.Value <- None }
