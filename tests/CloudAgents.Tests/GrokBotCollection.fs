module Gambol.CloudAgents.Tests.GrokBotCollection

open Xunit

[<CollectionDefinition("CloudAgents grokbot", DisableParallelization = true)>]
type GrokBotCollection() =
    class
    end
