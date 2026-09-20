module Gambol.Client.RunLaunch

open Gambol.Client.UpdateHelpers
open Gambol.Shared

/// Editing commit wrote a new Error: do not ActorStart or Amble.
let mayLaunchAfterEditCommit
    (wasEditing: bool)
    (before: CmdLastResult option)
    (after: CmdLastResult option)
    : bool =
    match wasEditing, after with
    | true, Some (CmdLastResult.Error _) when after <> before ->
        false
    | _ -> true

/// Commit when Editing. Third value is false when that commit failed.
let commitIfEditingForRun (model: VM) : VM * Effect list * bool =
    let wasEditing =
        match model.mode with
        | Editing _ -> true
        | _ -> false
    let before = model.lastCmdResult
    let committed, effects = commitIfEditing model
    let mayLaunch =
        mayLaunchAfterEditCommit
            wasEditing before committed.lastCmdResult
    committed, effects, mayLaunch

/// Commit, then continue only when that Editing commit did not fail.
let afterEditCommit
    (model: VM)
    (cont: VM -> Effect list -> VM * Effect list)
    : VM * Effect list =
    let committed, commitEffects, mayLaunch = commitIfEditingForRun model
    if not mayLaunch then
        committed, commitEffects
    else
        cont committed commitEffects
