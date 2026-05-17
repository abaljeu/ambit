namespace Gambol.Shared

open System

module LoginForm =
    type Credentials = { Username: string; Password: string }

    let private decodeFormValue (encoded: string) =
        encoded.Replace('+', ' ') |> Uri.UnescapeDataString

    let private parsePairs (body: string) =
        body.Split('&', StringSplitOptions.RemoveEmptyEntries)
        |> Seq.choose (fun part ->
            match part.Split('=', 2) with
            | [| key; value |] -> Some (decodeFormValue key, decodeFormValue value)
            | [| key |] -> Some (decodeFormValue key, "")
            | _ -> None)

    let private findField (name: string) (fields: (string * string) list) : string option =
        let want = name.ToLowerInvariant()

        fields
        |> List.tryPick (fun (key, value) ->
            if key.ToLowerInvariant() = want then Some value else None)

    /// Parse `application/x-www-form-urlencoded` body from the login form POST.
    let tryParse (body: string) : Credentials option =
        if String.IsNullOrWhiteSpace body then
            None
        else
            let fields = parsePairs body |> Seq.toList

            match findField "username" fields, findField "password" fields with
            | Some user, Some pass when user <> "" -> Some { Username = user; Password = pass }
            | _ -> None
