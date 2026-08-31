# Amble and RefExpr seams

Research note for [[plan/expression-language/issues/10-research-amble-refexpr-seams.md]]. Local sources: [[plan/expression-language/reports/existing-language-survey.md]], [[doc/roadmap/language-syntax-and-semantics.md]], [[doc/roadmap/reference-expression-interpretation.md]], [[doc/roadmap/reference-expressions.md]].

## Seams the spec must not contradict

**Empty match versus error.** `RefExpr.match_` returns an empty list on miss. That is not a parse error ([[doc/roadmap/reference-expression-interpretation.md]] indexing: out of range yields nothing). [[doc/roadmap/reference-expressions.md]] forbids silent empty success for the surrounding language. Run today treats empty eval as failure and writes redletter Children. Find treats empty as no hits. The spec must name the context: query fail (no Answer) versus parse or unresolved-required-reference error.

**Amble juxtaposition parse.** [[doc/roadmap/language-syntax-and-semantics.md]] and `AmbleParse` treat space as prefix `FunCall` (`text #todo` → `FunCall("text", [Ref])`). `of` and `,` are infix, right-associative, looser than juxtaposition. The pipeline surface uses space as left-to-right relation composition. One eval model requires a documented desugar. Two greps of space (prefix application versus pipeline) would contradict the live parser unless the spec says how they unify.

**Amble comma versus Prolog comma.** Amble `,` concatenates values. Prolog `,` is conjunction. The spec must not redefine `,` as and.

**`text` eval.** `AmbleEval.evalExpr` implements `Ref` and `FunCall("text", [Ref])` only. `text` yields new text Nodes under Run (`NewSpec`). `name`, `children`, `of`, `,`, `Cmd`, numbers, and `Paren` parse and do not evaluate. A first catalog that names `text` must match this meaning or record a deliberate change.

**Find AND.** `ViewModelSearch` splits on whitespace. Each part is a substring of text or name, or a successful RefExpr match. A Node must satisfy every part. That is same-Node intersection, not a pipeline that transforms a set and then filters descendants. Replacing Find is out of scope. The spec must not claim Find already is the pipeline.

**`**` Owned-only versus other steps following Ref.** Interpretation doc: named search follows Owned Children only. Implementation: `DirStep`, `FileStep`, `TagStep`, `ChildStep`, and `IndexStep` follow all Children, including Ref (tests lock this). `**` (`pathScopeDescendants`) follows Owned Children and stops at Directory and Workspace, so `**` from ROOT does not enter Workspace Children. A `descendant` word must pick a walk and say how it relates to `**`. This is still fog on the map; the spec must not silently pick the interpretation doc over the tests, or the reverse.

**Amble requires an explicit RefExpr base.** Search allows implicit context (`:0`, `.amb`, bare names). Amble parse rejects a leading name without an anchor. Pipeline words such as `root` and `descendant` are not today's anchors. The spec must say when a pipeline may start with a word versus a path term.

**Quoted names.** Interpretation doc says quoted path segments are not in RefExpr syntax. `RefExprParse` implements quotes. The spec should treat quotes as real for strings (`containing "the"`) and say whether path quotes stay.

**Statements already parsed.** Amble `Name = Expr` parses. `= Expression` and `#ident = Expression` appear only in the superseded draft in [[doc/roadmap/reference-expressions.md]]. Shell `> … | …` parses as `Cmd` and does not evaluate.

**No PathExpr persistence codec.** Document Ref lines address Nodes by identity. The spec must not assume stored path strings.

## Implication for the spec

Keep RefExpr as the implemented path base. Document empty-match as fail-to-answer, not as success, and keep parse errors distinct. Unify pipeline space with Amble juxtaposition through desugar, not a second eval. Preserve Amble `,` as concatenate. Treat Find AND and Run empty-as-error as context rules. Call out the Owned-versus-Ref walk split as an open choice, not a hidden default.
