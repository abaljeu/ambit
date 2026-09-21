module Gambol.Client.RunLaunch

open Gambol.Client.UpdateHelpers
open Gambol.Shared

/// Commit, then continue only when that Editing commit did not fail.
let afterEditCommit
    (model: VM)
    (cont: VM -> Effect list -> VM * Effect list)
    : VM * Effect list =
    RunEditCommit.afterEditCommit commitIfEditing model cont
