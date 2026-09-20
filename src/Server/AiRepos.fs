namespace Gambol.Server

open System
open Microsoft.Extensions.Configuration
open Gambol.Shared
open Gambol.CloudAgents

type AiRepo =
    { Name: string
      Url: string
      StartingRef: string }

type AiAskArgs =
    { Keyname: string option
      Reponame: string option }

/// Binds AiRepos from IConfiguration. CloudAgents stays settings-blind.
[<RequireQualifiedAccess>]
module AiRepos =

    let fromConfig (config: IConfiguration) : AiRepo list =
        config.GetSection("AiRepos").GetChildren()
        |> Seq.map (fun section ->
            { Name =
                section.["Name"]
                |> Option.ofObj
                |> Option.defaultValue ""
              Url =
                section.["Url"]
                |> Option.ofObj
                |> Option.defaultValue ""
              StartingRef =
                section.["StartingRef"]
                |> Option.ofObj
                |> Option.defaultValue "" })
        |> List.ofSeq

    let private startingRef (value: string) =
        if String.IsNullOrEmpty value then None
        else Some value

    let private toRepoConfig (repo: AiRepo) : RepoConfig =
        { Url = repo.Url
          StartingRef = startingRef repo.StartingRef }

    let resolve
        (repos: AiRepo list)
        (reponame: string option)
        : RepoConfig list option =
        match reponame with
        | None -> None
        | Some name ->
            repos
            |> List.tryFind (fun repo ->
                repo.Name.Equals(
                    name, StringComparison.OrdinalIgnoreCase))
            |> Option.map (fun repo -> [ toRepoConfig repo ])

[<RequireQualifiedAccess>]
module AiAskArgs =

    let private hasName (names: string list) (token: string) =
        names
        |> List.exists (fun name ->
            name.Equals(token, StringComparison.OrdinalIgnoreCase))

    let fromText
        (keys: AiKey list)
        (repos: AiRepo list)
        (text: string)
        : AiAskArgs =
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
