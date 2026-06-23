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

// ---- expressions ----

[<Fact>]
let ``parse function application`` () =
    Assert.Equal(
        FunCall("text", [ aref "#todo" ]),
        parseExpr "text #todo"
    )

[<Fact>]
let ``parse structural ref application`` () =
    Assert.Equal(
        FunCall("name", [ aref "^/notes.md" ]),
        parseExpr "name ^/notes.md"
    )

[<Fact>]
let ``parse of form`` () =
    let expected =
        FunCall("name", [ FunCall("children", [ aref "./folder/" ]) ])
    Assert.Equal(expected, parseExpr "name of children ./folder/")

[<Fact>]
let ``parse parenthesized expr`` () =
    Assert.Equal(
        Paren(FunCall("text", [ aref "#rugbydata" ])),
        parseExpr "(text #rugbydata)"
    )

[<Fact>]
let ``parse infix comma left assoc`` () =
    let a = aref "#a"
    let b = aref "#b"
    let c = aref "#c"
    Assert.Equal(FunCall(",", [ FunCall(",", [ a; b ]); c ]), parseExpr "#a , #b , #c")

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
            ShellWord(WordExpr(FunCall("text", [ aref "#rugbydata" ]))) ] ]
    Assert.Equal(ExprStmt(Cmd stages), parseOk "> tool --data (text #rugbydata)")

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
