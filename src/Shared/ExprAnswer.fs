namespace Gambol.Shared

[<RequireQualifiedAccess>]
type ExprAnswer =
    | Node of Node
    | Text of string

[<RequireQualifiedAccess>]
module ExprAnswer =
    let equal (left: ExprAnswer) (right: ExprAnswer) : bool =
        match left, right with
        | ExprAnswer.Node leftNode, ExprAnswer.Node rightNode -> leftNode.id = rightNode.id
        | ExprAnswer.Text leftText, ExprAnswer.Text rightText -> leftText = rightText
        | _ -> false

[<RequireQualifiedAccess>]
type ExprAnswerType =
    | Node
    | Text

/// A catalog row's typing. `Same` is the dual shape `τ ⇒ τ`: one row that serves
/// both a Node input and a Text input, as `containing`, `re`, and `rei` do.
[<RequireQualifiedAccess>]
type ExprSignature =
    | Fixed of input: ExprAnswerType * output: ExprAnswerType
    | Same
