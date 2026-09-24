namespace Gambol.Shared

/// Incremental `<>` XML fragment: one addChild at each tag boundary.
///
/// fragment = { wrap | node | text }
/// wrap     = '<>' | '</>'
/// node     = start { text | node } end | empty
/// start    = '<' name { attr } '>'
/// end      = '</' name '>'
/// empty    = '<' name { attr } '/>'
/// text     = { char - '<' }
[<RequireQualifiedAccess>]
module FocusXmlStream =

    type Token =
        | Start of name: string
        | End of name: string
        | Empty of name: string
        | Text of string

    type Pending =
        { name: string
          text: string }

    type Frame =
        { id: NodeId
          name: string }

    type Draft =
        { hold: string
          pending: Pending option
          parents: Frame list }

    type PlannedAdd =
        { parentId: NodeId
          childId: NodeId
          text: string }

    type Step =
        { draft: Draft
          adds: PlannedAdd list }

    let start (focusId: NodeId) : Draft =
        { hold = ""
          pending = None
          parents = [ { id = focusId; name = "" } ] }

    let private skipSlash (s: string) =
        if s.StartsWith("/") then s.Substring(1) else s

    let private dropTailSlash (s: string) =
        if s.EndsWith("/") then
            s.Substring(0, s.Length - 1)
        else
            s

    let private firstWord (s: string) =
        let rec loop i =
            if i >= s.Length then s
            elif s.[i] = ' ' || s.[i] = '\t' then
                s.Substring(0, i)
            elif s.[i] = '\n' || s.[i] = '\r' then
                s.Substring(0, i)
            else
                loop (i + 1)
        loop 0

    let private tagName (inside: string) =
        inside.Trim()
        |> skipSlash
        |> dropTailSlash
        |> fun body -> firstWord (body.Trim())

    let private parseTag (inside: string) : Token option =
        let trimmed = inside.Trim()
        let name = tagName inside
        if name = "" then None
        elif trimmed.StartsWith("/") then Some(End name)
        elif trimmed.EndsWith("/") then Some(Empty name)
        else Some(Start name)

    let rec private takeTokens (hold: string) (acc: Token list) =
        if hold.StartsWith("<") then
            match hold.IndexOf('>') with
            | -1 -> List.rev acc, hold
            | close ->
                let inside = hold.Substring(1, close - 1)
                let rest = hold.Substring(close + 1)
                match parseTag inside with
                | None -> takeTokens rest acc
                | Some token -> takeTokens rest (token :: acc)
        else
            match hold.IndexOf('<') with
            | -1 when hold = "" -> List.rev acc, ""
            | -1 -> List.rev (Text hold :: acc), ""
            | next ->
                let text = hold.Substring(0, next)
                let rest = hold.Substring(next)
                let acc =
                    if text = "" then acc
                    else Text text :: acc
                takeTokens rest acc

    let push (chunk: string) (draft: Draft) : Token list * Draft =
        let tokens, hold = takeTokens (draft.hold + chunk) []
        tokens, { draft with hold = hold }

    let private commitPending
        (newId: unit -> NodeId)
        (draft: Draft)
        (adds: PlannedAdd list)
        =
        match draft.pending, draft.parents with
        | Some pending, parent :: _ ->
            let childId = newId ()
            let add =
                { parentId = parent.id
                  childId = childId
                  text = pending.text }
            let frame = { id = childId; name = pending.name }
            { draft with
                pending = None
                parents = frame :: draft.parents },
            add :: adds
        | _ -> draft, adds

    let private addLeaf
        (newId: unit -> NodeId)
        (draft: Draft)
        (adds: PlannedAdd list)
        =
        match draft.parents with
        | parent :: _ ->
            let childId = newId ()
            let add =
                { parentId = parent.id
                  childId = childId
                  text = "" }
            draft, add :: adds
        | [] -> draft, adds

    let private addText (s: string) (draft: Draft) =
        match draft.pending with
        | None -> draft
        | Some pending ->
            { draft with
                pending =
                    Some { pending with text = pending.text + s } }

    let private popEnd (name: string) (draft: Draft) =
        match draft.parents with
        | frame :: rest when frame.name = name ->
            { draft with parents = rest }
        | _ -> draft

    let private stepToken
        (newId: unit -> NodeId)
        (draft: Draft, adds: PlannedAdd list)
        token
        =
        match token with
        | Start name ->
            let draft, adds = commitPending newId draft adds
            { draft with
                pending = Some { name = name; text = "" } },
            adds
        | Text s -> addText s draft, adds
        | End name ->
            let draft, adds = commitPending newId draft adds
            popEnd name draft, adds
        | Empty _ ->
            let draft, adds = commitPending newId draft adds
            addLeaf newId draft adds

    let apply
        (newId: unit -> NodeId)
        (tokens: Token list)
        (draft: Draft)
        : Step
        =
        let draft, adds =
            List.fold
                (fun acc token -> stepToken newId acc token)
                (draft, [])
                tokens
        { draft = draft
          adds = List.rev adds }

    let flush (newId: unit -> NodeId) (draft: Draft) : Step =
        let draft, adds = commitPending newId draft []
        { draft = { draft with hold = "" }
          adds = List.rev adds }
