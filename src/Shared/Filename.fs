namespace Gambol.Shared

open System

type FilenameError =
    | FilenameEmpty
    | FilenameTooLong
    | FilenameReserved
    | FilenameHasInvalidChar of char

[<Struct>]
type Filename =
    private | Filename of string

    member this.Value =
        let (Filename v) = this
        v

[<RequireQualifiedAccess>]
module Filename =

    let maxLength = 255

    let private isValidChar (c: char) : bool =
        Char.IsLetterOrDigit c || c = '.' || c = '-' || c = '_'

    /// Valid filenames are non-empty, not "." or "..", at most 255 characters,
    /// and composed only of [A-Za-z0-9._-] so they are transparently usable
    /// as HTML GET query parameter values without percent-encoding.
    let create (s: string) : Result<Filename, FilenameError> =
        if String.IsNullOrEmpty s then Error FilenameEmpty
        elif s.Length > maxLength then Error FilenameTooLong
        elif s = "." || s = ".." then Error FilenameReserved
        else
            match s |> Seq.tryFind (fun c -> not (isValidChar c)) with
            | Some c -> Error (FilenameHasInvalidChar c)
            | None -> Ok (Filename s)
