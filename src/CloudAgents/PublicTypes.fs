namespace Gambol.CloudAgents

/// Vendor-neutral repository configuration
type RepoConfig =
    { Url: string
      StartingRef: string option }

/// Selected model parameter for create requests
type ModelParam =
    { Id: string
      Value: string }

/// Vendor-neutral agent options
type AgentOptions =
    { DisplayName: string option
      ModelHint: string option
      ModelParams: ModelParam list }

/// Vendor-neutral git result
type GitResult =
    { RepoUrl: string
      Branch: string option
      PullRequestUrl: string option }

/// Vendor-neutral agent result
type AgentResult =
    { Text: string
      Git: GitResult list }

/// Vendor-neutral agent status
type AgentStatus =
    | Creating
    | Running
    | Finished of AgentResult
    | Cancelled
    | Failed of string

/// Vendor-neutral runner configuration
type RunnerConfig =
    { ApiKey: string }

/// Arguments passed to `AgentRunner.start` and to a `setFake` handler.
type StartArgs =
    { Config: RunnerConfig
      Prompt: string
      Repos: RepoConfig list option
      Options: AgentOptions }

/// Errors that can occur during agent operations
type AgentError =
    | AuthenticationFailed of string
    | NetworkError of string
    | ApiError of code: string * message: string
    | InvalidResponse of string
    | Timeout

/// Safe client text: names the connector, not raw provider dumps.
[<RequireQualifiedAccess>]
module AgentMessage =
    let couldNotSend (provider: string) (reason: string) =
        $"Could not send message to {provider}: {reason}"
