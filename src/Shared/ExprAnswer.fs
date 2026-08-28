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

type ExprSignature =
    { input: ExprAnswerType
      output: ExprAnswerType }
