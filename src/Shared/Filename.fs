namespace Gambol.Shared

open System

/// A node's filesystem name. Every node carries one of these three states.
[<RequireQualifiedAccess>]
type Filename =
    | Empty
    | Invalid of string
    | Ok of string

[<RequireQualifiedAccess>]
module Filename =

    let maxLength = 255

    /// System bookkeeping files use the case-insensitive `gambol.*` namespace.
    /// The dot is required; the legacy artifact named exactly `gambol` is not reserved.
    let isReservedSystemName (name: string) : bool =
        name.StartsWith("gambol.", StringComparison.OrdinalIgnoreCase)

    let private isValidChar (c: char) : bool =
        not (Char.IsControl c)
        && c <> '/'
        && c <> '\\'
        && c <> ':'
        && c <> '*'
        && c <> '?'
        && c <> '"'
        && c <> '<'
        && c <> '>'
        && c <> '|'

    /// Maps a raw string to a Filename:
    ///   null / empty  → Empty
    ///   "." or ".."   → Invalid s
    ///   > 255 chars   → Invalid s
    ///   bad char      → Invalid s
    ///   otherwise     → Ok s
    let create (s: string) : Filename =
        if String.IsNullOrEmpty s then Filename.Empty
        elif s.Length > maxLength then Filename.Invalid s
        elif s = "." || s = ".." then Filename.Invalid s
        else
            match s |> Seq.tryFind (fun c -> not (isValidChar c)) with
            | Some _ -> Filename.Invalid s
            | None -> Filename.Ok s

    /// Returns the string value for Ok; None for Empty and Invalid.
    let tryValue (f: Filename) : string option =
        match f with
        | Filename.Ok s -> Some s
        | _ -> None
