module AmbleTests

open Gambol.Shared
open Xunit

let private parseOk input =
    match Amble.parse input with
    | Ok stmt -> stmt
    | Error err -> failwith $"parse failed: {err}"

let private parseExpr input =
    match parseOk input with
    | ExprStmt expr -> expr
    | other -> failwith $"expected ExprStmt, got {other}"

let private refExpr input =
    match RefExpr.parse input with
    | Ok expr -> expr
    | Error err -> failwith $"ref parse failed: {err}"

let private aref input = AmbleExpr.Ref(refExpr input)

let private parseFails input =
    match Amble.parse input with
    | Error _ -> ()
    | Ok stmt -> failwith $"expected Error, got {stmt}"

// ---- expressions ----

[<Fact>]
let ``parse rejects prefix FunCall juxtaposition`` () =
    parseFails "text #todo"
    parseFails "name ^/notes.md"

[<Fact>]
let ``parse rejects of form`` () =
    parseFails "name of children ./folder/"

[<Fact>]
let ``parse parenthesized ref`` () =
    Assert.Equal(Paren(aref "#rugbydata"), parseExpr "(#rugbydata)")

[<Fact>]
let ``parse rejects Amble comma FunCall sugar`` () =
    parseFails "#a , #b , #c"
    parseFails "#list , sort #list"
    parseFails "sort 3 , 5 , 2"

// ---- assignment ----

[<Fact>]
let ``parse assignment`` () =
    Assert.Equal(Assign("name", aref "#todo"), parseOk "name = #todo")

// ---- numbers ----

[<Fact>]
let ``parse signed int`` () =
    Assert.Equal(Num(Int -3L), parseExpr "-3")

[<Fact>]
let ``parse float`` () =
    Assert.Equal(Num(Float 1.5m), parseExpr "1.5")

[<Fact>]
let ``parse rejects exponent`` () =
    match Amble.parse "1e10" with
    | Error _ -> ()
    | Ok _ -> Assert.Fail("expected Error")

// ---- commands ----

[<Fact>]
let ``parse simple redirect command`` () =
    let stages =
        [ [ ShellWord(WordBare "python")
            ShellWord(WordRef(refExpr "//@ws/rugby.py"))
            RedirIn(aref "#rugbydata") ] ]
    Assert.Equal(ExprStmt(Cmd stages), parseOk "> python //@ws/rugby.py < #rugbydata")

[<Fact>]
let ``parse command with paren word`` () =
    let stages =
        [ [ ShellWord(WordBare "tool")
            ShellWord(WordBare "--data")
            ShellWord(WordExpr(aref "#rugbydata")) ] ]
    Assert.Equal(ExprStmt(Cmd stages), parseOk "> tool --data (#rugbydata)")

[<Fact>]
let ``parse piped command with redirects`` () =
    let stage1 =
        [ ShellWord(WordBare "python")
          ShellWord(WordRef(refExpr "./step1.py"))
          RedirIn(aref "#rugbydata") ]
    let stage2 =
        [ ShellWord(WordBare "python")
          ShellWord(WordRef(refExpr "./step2.py"))
          RedirOut(aref "^/result.txt") ]
    Assert.Equal(ExprStmt(Cmd [ stage1; stage2 ]), parseOk
        "> python ./step1.py < #rugbydata | python ./step2.py > ^/result.txt")

// ---- errors ----

[<Fact>]
let ``parse rejects empty input`` () =
    match Amble.parse "   " with
    | Error msg -> Assert.Contains("empty", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse rejects unclosed paren`` () =
    match Amble.parse "(text #todo" with
    | Error _ -> ()
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse rejects trailing garbage`` () =
    match Amble.parse "#todo extra" with
    | Error _ -> ()
    | Ok _ -> Assert.Fail("expected Error")
