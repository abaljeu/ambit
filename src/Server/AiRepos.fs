namespace Gambol.Server

open System
open Microsoft.Extensions.Configuration
open Gambol.CloudAgents

type AiRepo =
    { Name: string
      Url: string
      StartingRef: string }

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
