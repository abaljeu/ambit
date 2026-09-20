module Gambol.Client.RunLaunch

open Gambol.Client.UpdateHelpers
open Gambol.Shared

/// Commit when Editing. Third value is false when that commit failed.
let commitIfEditingForRun (model: VM) : VM * Effect list * bool =
    CommandRequest.commitIfEditingForRun commitIfEditing model

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
