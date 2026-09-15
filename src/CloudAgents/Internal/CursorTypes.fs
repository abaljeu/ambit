namespace Gambol.CloudAgents.Internal

module CursorTypes =

    type CursorPrompt =
        { text: string }

    type CursorRepo =
        { url: string
          startingRef: string option }

    type CursorCreateRequest =
        { prompt: CursorPrompt
          name: string option
          repos: CursorRepo list option }

    type CursorAgent =
        { id: string
          name: string
          status: string
          latestRunId: string }

    type CursorRun =
        { id: string
          agentId: string
          status: string }

    type CursorCreateResponse =
        { agent: CursorAgent
          run: CursorRun }

    type CursorGitBranch =
        { repoUrl: string
          branch: string option
          prUrl: string option }

    type CursorGit =
        { branches: CursorGitBranch list }

    type CursorRunStatus =
        { id: string
          agentId: string
          status: string
          result: string option
          durationMs: int option
          git: CursorGit option }
