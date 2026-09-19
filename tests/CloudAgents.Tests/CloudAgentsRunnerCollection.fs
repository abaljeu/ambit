module Gambol.CloudAgents.Tests.CloudAgentsRunnerCollection

open Xunit

[<CollectionDefinition("CloudAgents runner", DisableParallelization = true)>]
type CloudAgentsRunnerCollection() =
    class
    end
