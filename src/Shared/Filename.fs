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
    /// The dot is required; exact `gambol` is not a special disk artifact.
    let isReservedSystemName (name: string) : bool =
        name.StartsWith("gambol.", StringComparison.OrdinalIgnoreCase)

    /// Exact `.amb` basename is the Directory File artifact, never a graph node name.
    let isDirectoryFileBasename (name: string) : bool =
        String.Equals(name, ".amb", StringComparison.OrdinalIgnoreCase)

    let isDirectoryFileFilename (f: Filename) : bool =
        match f with
        | Filename.Ok s
        | Filename.Invalid s -> isDirectoryFileBasename s
        | Filename.Empty -> false

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
    ///   exact `.amb`  → Invalid s (case-insensitive)
    ///   > 255 chars   → Invalid s
    ///   bad char      → Invalid s
    ///   otherwise     → Ok s
    let create (s: string) : Filename =
        if String.IsNullOrEmpty s then Filename.Empty
        elif s.Length > maxLength then Filename.Invalid s
        elif s = "." || s = ".." then Filename.Invalid s
        elif isDirectoryFileBasename s then Filename.Invalid s
        else
            match s |> Seq.tryFind (fun c -> not (isValidChar c)) with
            | Some _ -> Filename.Invalid s
            | None -> Filename.Ok s

    /// Returns the string value for Ok; None for Empty and Invalid.
    let tryValue (f: Filename) : string option =
        match f with
        | Filename.Ok s -> Some s
        | _ -> None
