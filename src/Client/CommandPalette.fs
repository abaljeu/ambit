module Gambol.Client.CommandPalette

open Gambol.Shared
open Gambol.Shared.ViewModel

// ---------------------------------------------------------------------------
// Command palette ops
// These are invoked when the palette is open or to open/close it.
// ---------------------------------------------------------------------------

/// Op: Open the command palette, preserving the current mode as returnTo.
let openCommandPaletteOp (model: VM) _dispatch : VM =
    { model with mode = CommandPalette ("", 0, model.mode) }

/// Op: Close the command palette, restoring the prior mode.
let closeCommandPaletteOp (model: VM) _dispatch : VM =
    match model.mode with
    | CommandPalette (_, _, ret) -> { model with mode = ret }
    | _ -> model

/// Op: Move palette selection up (ArrowUp when palette is open).
let paletteSelectUpOp (model: VM) _dispatch : VM =
    match model.mode with
    | CommandPalette (q, selectedCommand, ret) ->
        { model with mode = CommandPalette (q, max 0 (selectedCommand - 1), ret) }
    | _ -> model

/// Op: Move palette selection down (ArrowDown when palette is open).
let paletteSelectDownOp (model: VM) _dispatch : VM =
    match model.mode with
    | CommandPalette (q, selectedCommand, ret) ->
        { model with mode = CommandPalette (q, selectedCommand + 1, ret) }
    | _ -> model
