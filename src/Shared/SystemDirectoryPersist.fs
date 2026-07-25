namespace Gambol.Shared

open System

/// Gate File persist writes under DataDir's SYSTEM/ tree.
[<RequireQualifiedAccess>]
module SystemDirectoryPersist =

    /// Direct-child filenames under SYSTEM/ that File persist may overwrite.
    let writeAllowlist = [ "user.css" ]

    let private parts (relativePath: string) =
        relativePath.Replace('\\', '/').Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList

    let private eq (a: string) (b: string) =
        String.Equals(a, b, StringComparison.OrdinalIgnoreCase)

    /// Refuse DataDir-relative paths under SYSTEM except SYSTEM/.amb and
    /// allowlisted direct children (e.g. SYSTEM/user.css).
    let refuseWrite (relativePath: string) : Result<unit, string> =
        match parts relativePath with
        | root :: rest when eq root "SYSTEM" ->
            match rest with
            | [ name ] when Filename.isAmbMarkerName name -> Ok ()
            | [ name ] when writeAllowlist |> List.exists (eq name) ->
                Ok ()
            | _ -> Error "system directory write refused"
        | _ -> Ok ()
