module GitRunTests

open System.Reflection
open Gambol.Shared
open Xunit

[<Fact>]
let ``gitExec has no private work tree gate`` () =
    let gitRun =
        System.Reflection.Assembly.Load("Gambol.Shared.DotNet")
            .GetType("Gambol.Shared.GitRun")
    Assert.NotNull gitRun
    let names =
        gitRun.GetMethods(
            BindingFlags.Static
            ||| BindingFlags.Public
            ||| BindingFlags.NonPublic
            ||| BindingFlags.DeclaredOnly)
        |> Array.map (fun method -> method.Name)
    Assert.Contains("gitExec", names)
    Assert.DoesNotContain("withWorkTreeGate", names)
