module Gambol.Client.UpdateValidateGraph

open Gambol.Shared
open Gambol.Shared.ViewModel

/// Validate ownership on the client graph; focus a failing node on Error.
let validateGraphOp (model: VM) : VM * Effect list =
    match History.validateOwnershipLocated model.graph with
    | Ok () ->
        { model with lastCmdResult = Some (CmdLastResult.Detail (None, "valid")) }, []
    | Error (msg, nodeId) ->
        let focused = ViewModel.focusNode nodeId model
        { focused with lastCmdResult = Some (CmdLastResult.Error (None, msg)) }, []
