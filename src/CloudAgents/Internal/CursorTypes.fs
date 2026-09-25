namespace Gambol.CloudAgents.Internal

module CursorTypes =

    type CursorPrompt =
        { text: string }

    type CursorRepo =
        { url: string
          startingRef: string option }

    type CursorParamValue =
        { value: string
          displayName: string option }

    type CursorParamAssignment =
        { id: string
          value: string }

    type CursorModelVariant =
        { id: string
          displayName: string option
          ``params``: CursorParamAssignment list }

    type CursorModelParameter =
        { id: string
          displayName: string option
          values: CursorParamValue list }

    type CursorModel =
        { id: string
          displayName: string
          description: string option
          aliases: string list
          parameters: CursorModelParameter list
          variants: CursorModelVariant list }

    type CursorModelRef =
        { id: string
          ``params``: CursorParamAssignment list }

    type CursorCreateRequest =
        { prompt: CursorPrompt
          name: string option
          model: CursorModelRef option
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
