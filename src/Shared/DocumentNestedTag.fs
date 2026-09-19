namespace Gambol.Shared

/// Nested-tag extract pack. Not a file codec.
///
/// extract = node
/// node    = "<div>" text node* "</div>"
///         | "<focus>" text node* "</focus>"
/// text    = characters until the next open or close tag
[<RequireQualifiedAccess>]
module DocumentNestedTag =

    type NestedTagNode =
        { text: string
          isFocus: bool
          children: NestedTagNode list }

    [<Literal>]
    let missingRoot = "extract root not found"

    [<Literal>]
    let incomplete = "incomplete extract"

    [<Literal>]
    let residue = "residue after extract"

    let private tagName isFocus =
        if isFocus then "focus" else "div"

    let rec private writeNode
        (graph: Graph)
        (focusId: NodeId)
        (ancestors: Set<NodeId>)
        (node: Node)
        : string =
        let isFocus = node.id = focusId
        let openTag = "<" + tagName isFocus + ">"
        let closeTag = "</" + tagName isFocus + ">"
        let nextAncestors = Set.add node.id ancestors
        let childStrings =
            node.children
            |> List.choose (fun child ->
                if Set.contains child.id ancestors then
                    None
                else
                    Map.tryFind child.id graph.nodes
                    |> Option.map (writeNode graph focusId nextAncestors))
        openTag + node.text + String.concat "" childStrings + closeTag

    let writeExtract (graph: Graph) (focusId: NodeId) : Result<string, string> =
        match Map.tryFind graph.root graph.nodes with
        | None -> Error missingRoot
        | Some root -> Ok (writeNode graph focusId Set.empty root)

    type private Tag =
        | OpenDiv
        | OpenFocus
        | CloseDiv
        | CloseFocus

    let private tagAt (source: string) (i: int) : (Tag * int) option =
        let isToken (token: string) =
            i + token.Length <= source.Length
            && source.Substring(i, token.Length) = token
        if isToken "<div>" then Some(OpenDiv, 5)
        elif isToken "<focus>" then Some(OpenFocus, 7)
        elif isToken "</div>" then Some(CloseDiv, 6)
        elif isToken "</focus>" then Some(CloseFocus, 8)
        else None

    let rec private nextTag (source: string) (i: int) =
        if i >= source.Length then
            None
        else
            match tagAt source i with
            | Some found -> Some(found, i)
            | None -> nextTag source (i + 1)

    let private matchingClose openTag closeTag =
        match openTag, closeTag with
        | OpenDiv, CloseDiv -> true
        | OpenFocus, CloseFocus -> true
        | _ -> false

    let rec private parseNodes
        (source: string)
        (i: int)
        (acc: NestedTagNode list)
        : Result<NestedTagNode list * int, string> =
        match tagAt source i with
        | Some(OpenDiv, _)
        | Some(OpenFocus, _) ->
            match parseNode source i with
            | Error err -> Error err
            | Ok(node, after) -> parseNodes source after (node :: acc)
        | _ -> Ok(List.rev acc, i)

    and private parseNode
        (source: string)
        (i: int)
        : Result<NestedTagNode * int, string> =
        match tagAt source i with
        | Some(OpenDiv, len) -> parseOpened source (i + len) false OpenDiv
        | Some(OpenFocus, len) -> parseOpened source (i + len) true OpenFocus
        | _ -> Error incomplete

    and private parseOpened
        (source: string)
        (i: int)
        (isFocus: bool)
        (openTag: Tag)
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
            | Some(closeTag, len) when matchingClose openTag closeTag ->
                let node =
                    { text = nodeText
                      isFocus = isFocus
                      children = children }
                Ok(node, afterChildren + len)
            | _ -> Error incomplete

    let parseExtract (text: string) : Result<NestedTagNode, string> =
        match parseNode text 0 with
        | Error err -> Error err
        | Ok(node, after) ->
            if after = text.Length then Ok node
            else Error residue
