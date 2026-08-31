# Spec abstraction core and barriers

Report for the comprehensive Expression language spec. Sources: [[plan/expression-language/spec-draft.md]] (all Locked sections), every ticket under [[plan/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md|issues/]], the research and resolve reports, [[doc/roadmap/reference-expression-interpretation.md]], [[CONTEXT.md]], and the implemented parsers [[src/Shared/RefExprParse.fs]], [[src/Shared/RefExprMatch.fs]], [[src/Shared/AmbleParse.fs]]. This report proposes; it does not lock. Part 1 gives the smallest abstraction core. Part 2 lists each locked detail that resists that core, ordered by how much it distorts the core, each with a proposed resolution. Part 3 sketches the spec table of contents.

## Part 1 — Proposed abstraction core

The test for this core: each Locked detail becomes one catalog line or one consumer line, not a special case in the semantics.

### The semantic domain

One domain carries the whole language. Every Expression term denotes a function from one input Answer to an ordered, possibly empty, possibly repeating sequence of Answers. Write the domain as `τ1 ⇒ τ2`, a function from an Answer of type `τ1` to a sequence of Answers of type `τ2`. This is exactly a Prolog-style non-deterministic predicate: fail is the empty sequence, succeed is one or more Answers, and backtracking is the sequence. The Locked sections on Expressions, Multiple Answers, and Prolog control ([[plan/expression-language/reports/prolog-control-mapping.md]]) all name this domain already.

Types are `τ ::= Node | Text` in this spec, with `Number` reserved for a later catalog producer. There is no Boolean type; succeed and fail are control. A number literal is not a term (see Barrier 3). A quoted string is the one Text literal.

### The catalog

Every word and every path symbol is an entry in one catalog table. An entry has a spelling (one or more), an optional argument slot, a signature, and an Answer function. Nothing else in the semantics knows any entry by name. Anchors, postfix words, and infix words are not three mechanisms: an anchor is an entry that ignores its input Answer, a postfix word is an entry with no argument slot, and an infix word is an entry with one argument slot. The sketch below shows the shape; the full spec completes each Answer function from the Locked path-operator and catalog sections.

| Entry | Spellings | Signature | Answer function (one line) |
| --- | --- | --- | --- |
| root | `root`, `//` | `τ ⇒ Node` | Ignore the input; yield ROOT. |
| child | `child`, `:*` | `Node ⇒ Node` | The Children of the input (Owned and Ref), in Children order. |
| descendant | `descendant` | `Node ⇒ Node` | Closure of child; follows Ref; each Node at most once (see Barrier 6). |
| tree | `tree`, `**` | `Node ⇒ Node` | Transitively Owned Nodes from the input; no Directory or Workspace stop. |
| containing | `containing` Text | `Node ⇒ Node` | Yield the input if its Header text contains the argument text. |
| content search | `#` name | `Node ⇒ Node` | Search strictly below through Owned and Ref Children for matching Normal Nodes, with content-search walls and depth-first Node-identity deduplication. |
| named | `named` Text | `Node ⇒ Node` | Yield the input if it is a Normal Node whose name matches the glob; otherwise yield no Answers. |
| structural up | `^` | `Node ⇒ Node` | The nearest File Node, Directory Node, or Workspace Node up the Owned chain. |
| directory up | `.` | `Node ⇒ Node` | The nearest Directory Node or Workspace Node up the Owned chain. |
| container filter | `/` | `Node ⇒ Node` | Yield the input if it is a Directory Node or Workspace Node (see Barrier 2). |
| path search | name inside a path cluster | `Node ⇒ Node` | Path search for File Node or Directory Node per [[doc/roadmap/reference-expression-interpretation.md]] (see Barrier 1). |
| child at | `:` Int | `Node ⇒ Node` | The Child at index n, or nothing when out of range. |
| sibling at | `!` Int | `Node ⇒ Node` | The sibling at offset n; `!0` is the input; nothing when out of range. |
| sibling all | `!*` | `Node ⇒ Node` | All Children of the input's parent, including the input. |
| text (later slice) | `text` | `Node ⇒ Text` | The input Node's text. |

A miss is the empty sequence for every entry: out-of-range index, glob with no match, a walk that meets an Unloaded Node. No entry raises an error at evaluation time. This is the fail-to-answer lock, stated once for the whole table.

### Grammar sketch (EBNF)

```ebnf
RunLine    ::= "=" Expression | Name "=" Expression        (* anything else: Run does nothing *)
DialogLine ::= "=" Expression | WordSearch                 (* Search and Move *)
Expression ::= OrExpr
OrExpr     ::= AndExpr (("OR" | ",") AndExpr)*
AndExpr    ::= NotExpr ("AND" NotExpr)*
NotExpr    ::= "NOT" NotExpr | Seq ("NOT" NotExpr)?
Seq        ::= Term Term*
Term       ::= Word Argument? | PathCluster | "(" Expression ")"
Argument   ::= QuotedString | Name | Int | "*"             (* the entry's signature says which, if any *)
PathCluster::= the implemented RefExpr lexical grammar, one space-free run (see Barrier 1)
```

This encodes the locked precedence directly: juxtaposition (Seq) is tightest, then `NOT`, then `AND`, then `OR` and comma. A compound predicate after `NOT` needs parentheses because the `NOT` arm takes one NotExpr, which the precedence lock requires. Reserved words are `AND`, `OR`, `NOT`; all other standalone names resolve in the catalog.

### Typing judgments

Signatures compose by matching types. `⊢ e : τ1 ⇒ τ2` reads: e denotes a predicate from a `τ1` Answer to `τ2` Answers.

```text
w has signature τ1 ⇒ τ2 in the catalog, argument well-typed   ⊢ w arg : τ1 ⇒ τ2
⊢ e1 : τ1 ⇒ τ2   ⊢ e2 : τ2 ⇒ τ3                                ⊢ e1 e2 : τ1 ⇒ τ3
⊢ e1 : τ1 ⇒ τ2   ⊢ e2 : τ1 ⇒ τ2                                ⊢ e1 OR e2 : τ1 ⇒ τ2   (AND the same)
⊢ e : τ ⇒ τ'                                                    ⊢ NOT e : τ ⇒ τ
```

The locked rules fall out: mixing types across `AND` / `OR` / `NOT` is a type error because the operand types must agree; `<expr> containing root` is a type error because the argument slot of `containing` demands Text; old prefix `text #todo` is a type error because no catalog signature matches that composition.

### Evaluation semantics (denotational)

`E⟦e⟧ : Answer → Answer sequence`. Four rules cover the whole language; `δ(w)` is the catalog Answer function and `++` is concatenation.

```text
E⟦w arg⟧ x       = δ(w) arg x
E⟦e1 e2⟧ x       = concat [ E⟦e2⟧ y | y ← E⟦e1⟧ x ]
E⟦e1 OR e2⟧ x    = E⟦e1⟧ x ++ E⟦e2⟧ x
E⟦e1 AND e2⟧ x   = each y in E⟦e1⟧ x, in that order, at most once, where y also appears in E⟦e2⟧ x
E⟦NOT e⟧ x       = ⟨x⟩ when E⟦e⟧ x is empty, else ⟨⟩
```

Juxtaposition is monadic bind over the sequence, so it is associative: `((a) b) c` and `a (b c)` denote the same function. The left-associativity lock costs the semantics nothing; it only fixes the parse tree. The locked order rules are already these rules: juxtaposition is left-to-right, `OR` concatenates and may repeat, `AND` keeps left-predicate order at most once, `NOT` keeps the left Answer on zero inner Answers. Lazy, eager, or backtracking evaluation is an implementation freedom because the rules fix only the sequence and its order.

### Consumers

Consumers sit outside the Expression semantics. Each consumer supplies the initial input Answer and disposes of the output sequence; that is its whole interface to the language.

| Consumer | Initial Answer | Disposal |
| --- | --- | --- |
| Run `= E` | the current Node | Node Answers become Ref Children; Text Answers become new Owned Nodes; unfold when Children are written; 0 Answers or type error writes one blueletter Child `No matches found`. |
| Run `Name=E` | the current Node | As `= E`, plus rename the current Node. |
| Search `= E` | zoomRoot | Scrolling dialog; the user picks one Node; Zoom to it; 0 Answers and parse or type errors both show no hits. |
| Move `= E` | zoomRoot | As Search, but relocate to the picked Node. |

The omitted-initial-left-operand rule is therefore not an Expression rule at all: an Expression denotes a function, and each consumer applies it to its own context Answer. All evaluation is local to the Browser Graph, and an Unloaded Node in any walk is a miss inside the relevant catalog entry.

## Part 2 — Barriers

Each subsection: what the draft locks, why it fights the core, and a proposed resolution. Ordered by distortion, largest first. Recommendation summary: barriers 1 and 2 ask for a definition change; the rest absorb into the core with at most an added definition.

### Barrier 1 — one name token, two meanings: catalog word versus path-search name

The draft locks: the existing lexer stays; space is a lexical separator only, not an operator; `#todo` is two tokens; `//name` is path search from ROOT; after a kept Directory Node or Workspace Node, a following name is path search (`//ws/x`), not tag search. The prototype page also confirms `// descendant containing "the"` where `descendant` is the catalog word. These locks collide in the core: a grammar in which space only separates tokens gives `//ws` and `// ws` the same token stream, so the parser cannot tell whether a name after `//` is a path-search name (`ws`) or a catalog word (`descendant`). One token category with two context-dependent denotations is the single largest special case a uniform grammar can carry.

The implemented lexers already contain the resolution. [[src/Shared/AmbleParse.fs]] splits the line on whitespace first and classifies each space-free segment whole: a segment that starts with a path symbol becomes one `TRef` (a complete PathExpr), and a standalone name becomes a `TWord`. So `//ws/x` is one Path term and `// descendant` is the ROOT term followed by the word `descendant`. Propose: adopt this two-layer lexical rule into the spec. Layer one splits on space into segments; layer two runs the RefExpr sub-lexer inside a path segment (where `#todo` is indeed two sub-tokens and every name is a search name), and looks standalone name segments up in the catalog. A PathCluster term denotes the left-fold composition of its step entries, so the locked "every path operator is one predicate of its left-hand Nodes" is preserved exactly; adjacency only groups, it never changes any step's meaning.

The cost the user must accept: `//ws` and `// ws` differ (`// ws` is ROOT then an unknown-word parse error), so adjacency is significant at the segment boundary. That refines the lock "space is a lexical separator, not an operator" into "space separates segments; a path segment is one term". Recommendation: change — amend the lexical lock to name the two layers. The alternative (reserve all catalog words globally and forbid them as path-search names until quoted path segments arrive) keeps the one-layer story but silently steals names such as `child` and `tree` from every file search, which is worse.

### Barrier 2 — bare `/` undefined and `// OR /` undefined

The draft locks: `/` is postfix only, not a prefix; bare `/` is undefined; the prototype page locks `// OR /` as undefined because "the right side of OR has no left Node". In the core this hole is arbitrary: every entry denotes a total function from its input Answer, the consumer supplies the initial Answer, and operand-free predicates are already accepted everywhere else — `containing "the" AND named "blue"` at top level, `x (f, g)` where f and g have no written left, and the predicate after `NOT`. Under those same rules `/` at expression start denotes the container filter applied to the current Node, and `// OR /` denotes ROOT concatenated with the current Node when it is a Directory Node or Workspace Node. Declaring exactly one entry unusable in exactly one position is a special case the grammar and the semantics must both carry.

Propose: change — give `/` its filter denotation uniformly and delete the two "undefined" rulings; "not a prefix" stays true in spirit because nothing is a prefix in this language, and the old anchor meaning (nearest Workspace) does not return. If the user keeps the lock (for example to make a lone `/` keystroke inert in Run), absorb it as a layer-one lexical rule — a segment consisting of `/` alone does not lex — so the semantics stay total and the restriction lives in one lexer line.

### Barrier 3 — numbers valid only as the right operand of `:` or `!`

The draft locks: a number is only valid as the right operand of `:` or `!`; bare `3` is a type error; `sort 3,5,2` is not defined; no catalog function returns a number, yet Number is a future Answer type. A uniform grammar would admit a number literal as a term of type Number, and then need a rule forbidding it everywhere, which is backwards.

Propose: absorb — numbers are arguments, not terms. The catalog signature of `child at` and `sibling at` declares an Int argument slot (accepting `*` as the every-Child / every-sibling variant), the Argument production in the grammar is only reachable from an entry that declares a slot, and the Term production has no number literal at all. Bare `3` then fails at parse time with the locked message. One wording nuance for the spec: the draft calls bare `3` a type error, but under this resolution it is a parse error; the spec should either rename it or state that the parser reports it in the type-error format. When a Number-returning producer later joins the catalog, Number becomes a type and literals can become terms without touching `:` and `!`, whose arguments stay syntactic.

### Barrier 4 — infix right side: full expression or literal only

The draft locks two signals that point different ways. `<expr> containing root` is a type error with the right-hand type checked, which implies the right side parses as an expression whose type the checker rejects. But quotes are locked as filter strings only, and no in-slice entry produces Text, which implies the right side of `containing` can only ever be a literal, making `containing root` a parse error instead.

Propose: absorb by choosing the expression reading — an infix entry's argument slot takes one Term, the type system checks the Term's output type against the slot's declared type, and the argument Term evaluates against the same input Answer as the entry (each argument Answer combines with the entry per its Answer function). In this catalog slice the only Text-typed Term is the quoted literal, so behavior is identical to literal-only, the locked type-error wording is exactly right, and a later slice (postfix `text` as an argument, computed names) needs no grammar change. The literal-only reading also absorbs but hard-codes a restriction the locks do not actually demand and changes the error class of `containing root`.

### Barrier 5 — AND as same-left intersection needs Answer equality

The draft locks: `AND` keeps the left predicate's Answers that also appear on the right, in left-predicate order, at most once; composition of two pure filters equals `AND` but a generator makes them differ. The domain absorbs this cleanly — `AND`, `OR`, and `NOT` are pointwise per input Answer, so "same left" is automatic — but "also appear" requires an equality on Answers that no lock defines, and "at most once" requires the same equality for deduplication.

Propose: absorb with one added definition: two Node Answers are equal when they are the same Node (Node identity, not appearance — a Node reached as Owned and again through a Ref is one Answer for `AND`); two Text Answers are equal when their strings are equal. The filter-composition-equals-AND observation then falls out as a provable remark rather than a rule, which is worth stating in the spec as a theorem the implementation can exploit.

### Barrier 6 — closure entries over Ref: order and termination

The draft locks: `descendant` is the closure of `child` and follows Ref; an Expression yields a sequence whose order the spec fixes; `OR` may repeat a Node. Ref appearances allow sharing and cycles, so a naive closure stream repeats a shared subtree and never terminates on a cycle. The sequence lock therefore cannot be satisfied without a definition the draft does not yet contain. This is a gap rather than a conflict, but the core cannot state `descendant` in one line until it is filled.

Propose: absorb with one added definition on the entry: `descendant` enumerates depth-first in Children order and yields each Node at most once, skipping any Node already yielded from this input (the visited-set walk that [[src/Shared/RefExprMatch.fs]] already uses). State explicitly that this deduplication belongs to the closure entries alone: composition never dedupes, so `child child` legitimately repeats a Node that appears under two parents. `tree` needs no such clause because Owned placement is unique and acyclic, which is worth one remark.

### Barrier 7 — statements and the two-message error surface

The draft locks: Run accepts only `= Expression` and `Name=Expression` and does nothing on any other line; `#ident =` is rejected; 0 Answers and a type error both produce the blueletter Child in Run, and both show no hits in Search and Move. None of this fights the core once statements are consumer syntax above the Expression grammar, but one conflation needs care: the semantics must keep three outcomes distinct — parse error, type error, empty Answer sequence — even though every consumer in this slice happens to render them alike. If the spec defined "error equals empty", the seams lock (empty miss is fail-to-answer, parse errors stay distinct, [[plan/expression-language/reports/amble-refexpr-seams.md]]) would break and a future consumer could not tell a typo from a true miss.

Propose: absorb — the spec defines the three outcomes in the semantics chapter, and each consumer row says which outcomes it merges in its display. No lock changes; this is a structural instruction for writing the spec, so the merge stays a consumer fact and never leaks into evaluation.

### Barrier 8 — closed items that look like barriers but absorb silently

These locked details each threatened a special case and dissolve in the core; the spec should record them as one-liners so nobody reopens them. Mixed fixity (anchor, postfix, infix): all three are the one entry shape — ignore-input, no-argument, one-argument — so fixity is a catalog column, not a syntax class. `NOT` needing parentheses for compound predicates: pure precedence, already in the grammar sketch. Omitted left differing by consumer (current Node versus zoomRoot): the consumer table supplies the initial Answer; the Expression rules never mention it. The final decision supersedes the earlier `#x` / `named "x"` alias: `#` is descendant search through Owned and Ref Children, while `named` is a pure same-input filter. `:*` and `!*` replacing bare `:` and `!`: the argument slot accepts Int or `*`, and a slot with a missing argument does not lex. Reserved `AND` / `OR` / `NOT` caps-only: a two-line lexical rule; lowercase falls through to catalog lookup. The `**` divergence from today's implementation (this spec's `**` is `tree` with no Directory or Workspace stop, and old anchors `/`, bare `#`, bare `:` / `!` change meaning): not a semantics problem at all, but the spec needs a divergence appendix so the later implementation effort has an explicit revision list against [[src/Shared/RefExprMatch.fs]] and its tests.

## Part 3 — Spec skeleton

Proposed table of contents for the comprehensive spec, with the draft section each chapter absorbs.

1. Scope and hand-off — from draft: What an Expression is; Hand-off.
2. Semantic domain: Answers, types, predicates as functions — from draft: What an Expression is; Multiple Answers.
3. Lexical structure: segments, path clusters, reserved words, quoted strings — from draft: Juxtaposition and fixity (lexer, `#todo` two tokens); resolution of Barriers 1 and 2.
4. Grammar (EBNF) — from draft: Juxtaposition and fixity; Boolean operators (precedence); Statements (line forms).
5. Type system and signatures — from draft: Top-level context (typed functions, type-mix error, number rule via Barrier 3).
6. Evaluation semantics — from draft: Multiple Answers; Boolean operators as control; Unloaded walks; Answer equality (Barrier 5).
7. The catalog — from draft: First primitive catalog; Path operators; Owned-versus-Ref walk words; closure order (Barrier 6).
8. Consumers: Run, Search, Move — from draft: Statements; Top-level context; error display (Barrier 7).
9. Divergences from the implemented RefExpr and Amble — `**` stop, old anchors, dropped `of` and comma-as-FunCall, prefix `text`; grounded in [[plan/expression-language/reports/existing-language-survey.md]].
10. Deferred and not planned — from draft: Unloaded walks (server), Fog; quoted path segments, numbers, shell, unification, cut, collection.
11. Worked examples — from [[plan/expression-language/reports/pipeline-examples.md]], re-checked against chapters 3 to 8.
