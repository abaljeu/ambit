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

/// Incremental events while a run is open (vendor-neutral).
type AgentStreamEvent =
    | AssistantText of string
    | RunFinished of AgentResult
    | RunFailed of string
    | RunCancelled

/// Result of a cancel request the provider answered.
type CancelOutcome =
    | CancelRequested
    /// Run already terminal or never active; nothing left to cancel.
    | NotCancellable

/// Cursor runner config. Caller binds `AiKeys` from User Secrets or Azure App Settings
/// and resolves the selected value into `ApiKey`. Library stays settings-blind.
type RunnerConfig =
    { ApiKey: string }

/// Arguments passed to `AgentRunner.start` and to a `setFake` handler.
type StartArgs =
    { Config: RunnerConfig
      Prompt: string
      Repos: RepoConfig list option
      Options: AgentOptions }

/// Which run to stream, and how long to wait for it.
type StreamArgs =
    { Config: RunnerConfig
      AgentId: string
      RunId: string
      MaxWaitMs: int option }

/// Accumulator stepped once per stream event.
type StreamFold<'a> =
    { Seed: 'a
      OnEvent: 'a -> AgentStreamEvent -> 'a }

/// Errors that can occur during agent operations
type AgentError =
    | AuthenticationFailed of string
    | NetworkError of string
    | ApiError of code: string * message: string
    | InvalidResponse of string
    | Timeout

/// Grok Bot oneshot config. Caller binds User Secrets
/// grokbot:WakeUrl, grokbot:WakeSecret, grokbot:InboundSecret.
/// Library stays settings-blind. InboundSecret is Server door
/// auth; this library consumes deliver(sessionId, text).
type GrokBotConfig =
    { WakeUrl: string
      WakeSecret: string
      InboundSecret: string }

/// One wake POST (ack-only). Next query is a new oneshot.
/// ResponseUrl is the absolute deliver door; caller supplies it.
type GrokBotWakeArgs =
    { Config: GrokBotConfig
      Text: string
      CommandId: string
      FocusId: string
      SessionId: string
      ResponseUrl: string }

/// Which oneshot to stream, and how long to wait.
type GrokBotStreamArgs =
    { Config: GrokBotConfig
      SessionId: string
      PollIntervalMs: int
      MaxWaitMs: int option }

/// Safe client text: names the connector, not raw provider dumps.
[<RequireQualifiedAccess>]
module AgentMessage =
    let couldNotSend (provider: string) (reason: string) =
        $"Could not send message to {provider}: {reason}"
