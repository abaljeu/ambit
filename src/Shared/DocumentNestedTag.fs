namespace Gambol.Shared

/// Nested-tag extract pack. Not a file codec.
///
/// extract = node
/// node    = "<" tag ">" text node* "</" tag ">"
/// tag     = letter+
/// text    = characters until the next open or close tag
[<RequireQualifiedAccess>]
module DocumentNestedTag =

    type NestedTagNode =
        { tag: string
          text: string
          children: NestedTagNode list }

    [<Literal>]
    let missingRoot = "extract root not found"

    [<Literal>]
    let incomplete = "incomplete extract"

    [<Literal>]
    let residue = "residue after extract"

    let tagName (_node: Node) = "div"

    let private writeTag (graph: Graph) (node: Node) =
        match graph.focus with
        | Some id when id = node.id -> "focus"
        | _ -> tagName node

    let rec private writeNode
        (graph: Graph)
        (ancestors: Set<NodeId>)
        (node: Node)
        : string =
        let tag = writeTag graph node
        let openTag = "<" + tag + ">"
        let closeTag = "</" + tag + ">"
        let nextAncestors = Set.add node.id ancestors
        let childStrings =
            node.children
            |> List.choose (fun child ->
                if Set.contains child.id ancestors then
                    None
                else
                    Map.tryFind child.id graph.nodes
                    |> Option.map (writeNode graph nextAncestors))
        openTag + node.text + String.concat "" childStrings + closeTag

    let writeExtract (graph: Graph) : Result<string, string> =
        match Map.tryFind graph.root graph.nodes with
        | None -> Error missingRoot
        | Some root -> Ok (writeNode graph Set.empty root)

    type private Mark =
        | Open of string
        | Close of string

    let private isNameChar (c: char) =
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')

    let private readName (source: string) (i: int) =
        let rec loop j =
            if j < source.Length && isNameChar source.[j] then
                loop (j + 1)
            else
                j
        let j = loop i
        if j > i then Some(source.Substring(i, j - i), j)
        else None

    let private tagAt (source: string) (i: int) : (Mark * int) option =
        if i >= source.Length || source.[i] <> '<' then
            None
        elif i + 1 < source.Length && source.[i + 1] = '/' then
            match readName source (i + 2) with
            | Some(name, after) when after < source.Length
                && source.[after] = '>' ->
                Some(Close name, after + 1 - i)
            | _ -> None
        else
            match readName source (i + 1) with
            | Some(name, after) when after < source.Length
                && source.[after] = '>' ->
                Some(Open name, after + 1 - i)
            | _ -> None

    let rec private nextTag (source: string) (i: int) =
        if i >= source.Length then
            None
        else
            match tagAt source i with
            | Some found -> Some(found, i)
            | None -> nextTag source (i + 1)

    let rec private parseNodes
        (source: string)
        (i: int)
        (acc: NestedTagNode list)
        : Result<NestedTagNode list * int, string> =
        match tagAt source i with
        | Some(Open _, _) ->
            match parseNode source i with
            | Error err -> Error err
            | Ok(node, after) -> parseNodes source after (node :: acc)
        | _ -> Ok(List.rev acc, i)

    and private parseNode
        (source: string)
        (i: int)
        : Result<NestedTagNode * int, string> =
        match tagAt source i with
        | Some(Open name, len) -> parseOpened source (i + len) name
        | _ -> Error incomplete

    and private parseOpened
        (source: string)
        (i: int)
        (tag: string)
        : Result<NestedTagNode * int, string> =
        let textEnd =
            match nextTag source i with
            | None -> source.Length
            | Some(_, at) -> at
        let nodeText = source.Substring(i, textEnd - i)
        match parseNodes source textEnd [] with
        | Error err -> Error err
        | Ok(children, afterChildren) ->
            match tagAt source afterChildren with
            | Some(Close closeName, len) when closeName = tag ->
                let node =
                    { tag = tag
                      text = nodeText
                      children = children }
                Ok(node, afterChildren + len)
            | _ -> Error incomplete

    let parseExtract (text: string) : Result<NestedTagNode, string> =
        match parseNode text 0 with
        | Error err -> Error err
        | Ok(node, after) ->
            if after = text.Length then Ok node
            else Error residue
