namespace Gambol.Server

open System
open Microsoft.Extensions.Configuration
open Gambol.Shared

/// Name-keyed API keys plus the default name. CloudAgents stays settings-blind.
type AiKeySet =
    { DefaultAiKey: string
      Keys: Map<string, string> }

[<RequireQualifiedAccess>]
module AiKeys =

    let empty =
        { DefaultAiKey = ""
          Keys = Map.empty }

    let private sectionValue (section: IConfigurationSection) =
        section.Value |> Option.ofObj |> Option.defaultValue ""

    let fromConfig (config: IConfiguration) : AiKeySet =
        let keys =
            config.GetSection("AiKeys").GetChildren()
            |> Seq.map (fun section -> section.Key, sectionValue section)
            |> Map.ofSeq
        let defaultName =
            config.["DefaultAiKey"]
            |> Option.ofObj
            |> Option.defaultValue ""
        { DefaultAiKey = defaultName
          Keys = keys }

    let private sameName (name: string) (key: string) =
        key.Equals(name, StringComparison.OrdinalIgnoreCase)

    let private valueFor (keys: Map<string, string>) (name: string) =
        if String.IsNullOrEmpty name then
            ""
        else
            keys
            |> Map.tryPick (fun key value ->
                if sameName name key then Some value else None)
            |> Option.defaultValue ""

    let resolve (store: AiKeySet) (keyname: string option) : string =
        let name =
            match keyname with
            | None -> store.DefaultAiKey
            | Some given -> given
        valueFor store.Keys name

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
