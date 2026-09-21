namespace Gambol.Server

open System
open Microsoft.Extensions.Configuration
open Gambol.Shared

type AiKey =
    { Name: string
      ApiKey: string }

/// Binds AiKeys from IConfiguration. CloudAgents stays settings-blind.
[<RequireQualifiedAccess>]
module AiKeys =

    let fromConfig (config: IConfiguration) : AiKey list =
        config.GetSection("AiKeys").GetChildren()
        |> Seq.map (fun section ->
            { Name =
                section.["Name"]
                |> Option.ofObj
                |> Option.defaultValue ""
              ApiKey =
                section.["ApiKey"]
                |> Option.ofObj
                |> Option.defaultValue "" })
        |> List.ofSeq

    let resolve (keys: AiKey list) (keyname: string option) : string =
        match keyname with
        | None ->
            match keys with
            | first :: _ -> first.ApiKey
            | [] -> ""
        | Some name ->
            keys
            |> List.tryFind (fun key ->
                key.Name.Equals(
                    name, StringComparison.OrdinalIgnoreCase))
            |> Option.map (fun key -> key.ApiKey)
            |> Option.defaultValue ""

    let tokensFromText (text: string) : string list =
        match CommandRequest.behaviorFromText text with
        | "" -> []
        | rest ->
            rest.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            |> Array.toList

    let keynameFromText (text: string) : string option =
        match tokensFromText text with
        | [] -> None
        | token :: _ -> Some token
