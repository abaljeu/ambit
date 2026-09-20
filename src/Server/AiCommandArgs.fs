namespace Gambol.Server

open System

type AiCommandArgs =
    { Keyname: string option
      Reponame: string option }

/// Parses `?ai` keyname then reponame from Command text.
[<RequireQualifiedAccess>]
module AiCommandArgs =

    let private hasName (names: string list) (token: string) =
        names
        |> List.exists (fun name ->
            name.Equals(token, StringComparison.OrdinalIgnoreCase))

    let fromText
        (keys: AiKey list)
        (repos: AiRepo list)
        (text: string)
        : AiCommandArgs =
        let keyNames = keys |> List.map (fun key -> key.Name)
        let repoNames = repos |> List.map (fun repo -> repo.Name)
        match AiKeys.tokensFromText text with
        | [] -> { Keyname = None; Reponame = None }
        | [ one ] ->
            if hasName keyNames one then
                { Keyname = Some one; Reponame = None }
            elif hasName repoNames one then
                { Keyname = None; Reponame = Some one }
            else
                { Keyname = Some one; Reponame = None }
        | first :: second :: _ ->
            { Keyname = Some first; Reponame = Some second }
