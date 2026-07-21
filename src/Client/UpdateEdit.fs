module Gambol.Client.UpdateEdit

open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Shared.ViewModelJoinOps
open Gambol.Shared.ViewModelMoveOps


let moveEdit (delta: int) (cursorPos: int) (model: VM) : VM * Effect list =
    moveEditImpl delta (MoveEditUtf16 cursorPos) model

let moveEditUpAtClientX (clientX: float) (model: VM) : VM * Effect list =
    moveEditImpl -1 (MoveEditPrevLastLineX clientX) model

let moveEditDownAtClientX (clientX: float) (model: VM) : VM * Effect list =
    moveEditImpl 1 (MoveEditNextFirstLineX clientX) model

let private applyJoin ops text caret instanceId (model: VM) =
    let change =
        { id = model.revision.Value
          changeId = System.Guid.NewGuid()
          ops = ops }

    match applyAndPost change model with
    | Error msg -> withMoveError msg model, []
    | Ok (m, effects) ->
        let result = withSiteMap m

        { result with
            mode = Editing (text, caret)
            selectedNodes = singleSelectionForInstance result.siteMap instanceId },
        effects

let private restoreEditCursor model =
    let pos = readEditInputCursor ()

    match model.mode with
    | Editing (text, _) -> { model with mode = Editing (text, EditCaret.Utf16Index pos) }, []
    | _ -> model, []

let private applyJoinPlan model plan =
    match plan with
    | JoinEditPlan.Apply (ops, text, caret, instanceId) ->
        applyJoin ops text caret instanceId model
    | JoinEditPlan.RestoreCaret -> restoreEditCursor model

let joinWithNext (currentText: string) (model: VM) : VM * Effect list =
    joinWithNextPlan currentText model
    |> Option.map (applyJoinPlan model)
    |> Option.defaultValue (model, [])

let joinWithPrevious (currentText: string) (model: VM) : VM * Effect list =
    joinWithPreviousPlan currentText model
    |> Option.map (applyJoinPlan model)
    |> Option.defaultValue (model, [])
