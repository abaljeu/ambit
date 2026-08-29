# Expression language specification

This document is the hand-off specification for the Gambol Expression language: lexical structure, grammar, type system, evaluation semantics, the first primitive catalog, and the consumers that run Expressions. It extends the implemented path-reference base ([[doc/roadmap/reference-expression-interpretation.md]]) with a left-to-right word pipeline. Implementation is a later effort. This spec supersedes [[.scratch/expression-language/spec-draft.md]]; every Locked section of that draft lands in a chapter below, amended only where a decision listed in chapter 1 says so.

## 1. Scope, hand-off, and reading guide

The abstraction core comes from [[.scratch/expression-language/reports/spec-abstraction-core-and-barriers.md]]: every term denotes a function from one input Answer to an ordered sequence of Answers, and every word and path symbol is a row in one catalog table. The quality test for each detail is that it reads as a catalog row or a one-line definition against that core, not as a special case in the grammar or the semantics.

User decisions of 2026-08-28, incorporated throughout:

- Literals are arguments, never terms. Quoted strings, numbers, and glob name patterns inside clusters are operator arguments; every path symbol is a catalog row with an argument slot, uniform with `containing "the"` and `:3`; symbols are never operator arguments. A literal with no operator wanting it is a parse error. This one statement generalizes the barrier 3 number rule.
- Two-layer lexing (barrier 1), reworded to this model: inside a space-free path cluster, names are the literal arguments of the adjacent operators; spaced, an unquoted word is a symbol (catalog word) and a quoted string is a literal argument for the preceding operator that wants one. `//ws` and `// "ws"` are the same Expression; `// ws` stays a parse error, by design.
- `/` is an infix operator with a required name argument: structural search (Workspace Node, Directory Node, or File Node — containers including files) under the left input. The earlier postfix-filter denotation is withdrawn. Bare `/` and `// OR /` are missing-argument parse errors, uniform with `containing` lacking its string; this is the final barrier 2 resolution.
- A leading bare name in a cluster (`a/b/c`) is the argument of an implicit `/` whose left side is omitted (the current Node), matching RefExpr's leading FileStep. RefExpr's prefix anchors `/` (nearest Workspace ancestor) and `#` (nearest named Normal ancestor) stay dropped; `wsroot` covers the containing-Workspace need.
- `//` is not its own catalog row and has no optional argument. `root` is a value row spelled `root` only, and the cluster fragment `//` is exactly shorthand for `root /`, so `//ws` equals `root / "ws"` and a bare `//` is a missing-argument parse error like bare `/`. This amends the locked spellings (cluster `//` as a bare spelling of `root` is dropped) and the locked example `// tree`, which becomes `root tree`.
- Four postfix filters by Node classification join the catalog: `ws`, `dir`, `file`, and `normal`. Each is `Node ⇒ Node` with no slot, a pure filter that satisfies the pure-filter theorem. They fill the dropped DirStep gap and more: directories named `d` is `x / "d" dir`. Pleasantly, `root ws` equals `root`, because ROOT is a Workspace Node.
- One more infix pure filter joins the catalog: `class`, with a required quoted Text argument. `x class "y"` yields the input Node when `y` is in the Node's cssClasses, matching the implemented representation ([[src/Shared/CssClass.fs]]: a token list, tested by exact case-sensitive membership).
- New catalog word `wsroot`: up to the nearest Workspace Node. An ordinary row beside `^` and `.`.
- `#` and `named` are separate rows. `#` is subsection search (spoken spelling `subsection`; `subsection "todo"` equals `#todo`): it takes a required name and searches strictly below each input through Children (Owned and Ref) for sections, with the walls and deterministic Node-identity deduplication defined in chapter 7. `named` takes a required quoted name and is a pure filter on the input Node's name glob. `section` is a zero-argument pure filter: yield the input when it is a named Normal Node.
- Barriers 4 through 8 absorb as the report recommends: Answer equality is defined once; closure entries dedupe and composition never does; parse error, type error, and zero Answers stay distinct outcomes.
- Terminology: this spec says type (`τ`) and never "kind" for the Node/Text classification, because Kind is the established Node classification in [[CONTEXT.md]].

Notation: `τ` ranges over Answer types; `τ1 ⇒ τ2` is the type of a term (chapter 2); `E⟦e⟧` is the Answer function of Expression `e` (chapter 6); `δ(w)` is the catalog Answer function of row `w`; `⟨⟩` is the empty sequence, `⟨x⟩` the one-Answer sequence, `++` concatenation. Grammar is EBNF. Domain words — Node, Answer, Graph, Header, Children, Owned, Ref, Normal Node, section, subsection, File Node, Directory Node, Workspace Node, ROOT, Loaded, Unloaded, zoomRoot, Zoom, Find — follow [[CONTEXT.md]] exactly.

## 2. Semantic domain: Answers, types, and predicates

An Answer is one value: a Node or a Text (a string). Answer types are `τ ::= Node | Text`. A Number type is reserved for a later catalog producer and has no terms in this spec. There is no Boolean type: succeed and fail are control, exactly as in Prolog.

Every term denotes a total function from one input Answer of type `τ1` to an ordered, possibly empty, possibly repeating sequence of Answers of type `τ2`, written `τ1 ⇒ τ2`. This is a Prolog-style non-deterministic predicate: fail is the empty sequence, succeed is one or more Answers, and backtracking is the sequence. Literals are not terms and denote nothing by themselves; they only fill catalog argument slots.

Answer equality is defined once for the whole language: two Node Answers are equal when they are the same Node (Node identity, not appearance — a Node reached as Owned and again through a Ref is one Answer), and two Text Answers are equal when their strings are equal.

The semantics keeps three outcomes distinct: parse error (the text is not an Expression, including a missing or unwanted literal), type error (the Expression does not type per chapter 5), and zero Answers (a well-typed Expression that fails). Zero Answers is a normal value, never an error; this is the fail-to-answer principle. Evaluation never raises: every miss — an out-of-range index, a glob with no match, a walk that meets an Unloaded Node — is the empty sequence. Chapter 8 states which outcomes each consumer merges in its display; the merge never leaks into evaluation.

## 3. Lexical structure

Lexing has two layers. Layer one splits the Expression text into tokens; layer two lexes the inside of each path cluster. The refined form of the draft's lexical lock is: space separates segments; a path segment is one term.

Statement lines are recognized before expression lexing; chapter 8 gives the line forms. The Expression text is what follows the `=`.

Layer one. Whitespace separates tokens. `(`, `)`, and `,` are single-character tokens and also terminate a running segment. A `"` begins a quoted string token, which runs to the next `"` and may contain spaces, path symbols, parentheses, and commas; this spec defines no escape sequences. Every other maximal run of characters is one segment, classified whole:

- A segment that contains any of `/`, `^`, `#`, `:`, `!`, `*`, or that begins with `.`, is one PathCluster token, sub-lexed by layer two.
- A segment that is exactly `AND`, `OR`, or `NOT` (capitals only) is a reserved word; lowercase or mixed case is not reserved. A segment that is exactly `outer` (lowercase only) is reserved, so it is the combinator and not bind; mixed or uppercase is not reserved.
- A segment that is a signed integer is a Number token.
- Any other segment is a Name token: a standalone symbol looked up in the catalog. A `.` after the first character is an ordinary name character, matching the layer-two `.` rule.

The parser looks each Name token up in the catalog; a Name that is no row's spelling is a parse error (unknown word). Inside a cluster and inside quotes, names are literal search strings and the reserved words are not special.

Literal binding. A quoted string token, and a Number token, is a literal argument for the preceding operator that wants one: it fills the unfilled argument slot of the immediately preceding Word or of the final step of the immediately preceding cluster. A literal with no operator wanting it is a parse error — this covers `"d" "e"` (the strings are terms nowhere) and a standalone `3`, whose locked report wording stays: a number is only valid as the right operand of `:` or `!`.

Layer two. Inside a PathCluster: `//` is tokenized before `/`; `**` is tokenized before `*` inside a name; `#`, `^`, `:`, and `!` are single symbols; a NamePattern is a maximal run of characters that are not whitespace, `(`, `)`, `,`, `"`, or one of `/`, `#`, `^`, `:`, `!`, with `*` allowed as a glob character. The `.` rule: a lone `.`, or a `.` followed immediately by `/`, is the directory-up symbol; a `.` followed by other name characters is part of a NamePattern (`.amb` is one name). Quotes do not occur inside clusters in this spec (chapter 10).

Argument binding inside a cluster: `//` is exactly shorthand for `root /`; `/` and `#` each consume the NamePattern immediately after them as required slots; `:` and `!` each consume the immediately following signed integer or `*`; `^`, `.`, and `**` consume nothing. A NamePattern that the operator before it does not consume — a cluster-leading name, or a name after `^`, `.`, or `**` — is the argument of an implicit `/`. That one rule makes `a/b/c` the chain `/ "a" / "b" / "c"` from the current Node, matching RefExpr's leading FileStep.

Argument position is literal-only: a name in an operator's argument slot is always a search string, never a catalog word, even when its spelling collides with one. `//ws`, `// "ws"`, and `root /ws` are all the same valid Expression — `ws` there is a string, although `ws` is also the Workspace filter word; `//file` searches for the name `file`, while the `file` filter word acts only in term position (`root / "x" file`).

A cluster may begin with `/`, `#`, `:`, or `!`: the leading operator takes its left input from whatever precedes it — the preceding spaced term when there is one (`root /ws`), or the consumer's initial Answer when the cluster starts the Expression. One rule, crossing spaces: adjacency never changes an operator's meaning, it only groups the operator with its argument.

A cluster denotes the left-fold juxtaposition of its steps, each step one catalog row with its argument bound; adjacency only groups, it never changes a step's meaning. The one deliberate consequence: a name inside a cluster is a literal argument and a standalone name is a symbol, so `//ws` (which equals `// "ws"` and desugars to `root / "ws"`) and `// ws` (a missing-argument parse error: the spaced `ws` is a symbol, so nothing fills the `/` of `//`) differ. A name containing spaces is expressible only as a quoted argument: `// "a b"`, `x / "a b"`, `x # "a b"`.

## 4. Grammar

```ebnf
RunLine     ::= "=" Expression
              | Name "=" Expression
DialogLine  ::= "=" Expression
              | WordSearch                    (* no leading "=": today's word search, outside this spec *)
Expression  ::= OrExpr
OrExpr      ::= AndExpr (("OR" | ",") AndExpr)*
AndExpr     ::= NotExpr ("AND" NotExpr)*
NotExpr     ::= "NOT" NotExpr
              | "outer" NotExpr
              | Seq (("NOT" | "outer") NotExpr)?
Seq         ::= Term Term*
Term        ::= Word Literal?
              | PathCluster Literal?
              | "(" Expression ")"
Literal     ::= QuotedString | Number
PathCluster ::= (NamePattern | Step)+
Step        ::= "//" NamePattern?
              | "/" NamePattern?
              | "#" NamePattern?
              | "^" | "." | "**"
              | ":" (Int | "*")
              | "!" (Int | "*")
Int         ::= ["+" | "-"] Digit+
```

- The locked precedence is encoded directly: juxtaposition (Seq) binds tightest, then `NOT` and `outer`, then `AND`, then `OR` and comma.
- The trailing Literal of a Term is consumed exactly when the Word's catalog row, or the cluster's final step, has an unfilled argument slot; a Literal in any other position is a parse error. A required slot that no adjacent NamePattern and no trailing Literal fills is a missing-argument parse error: bare `/`, bare `//`, bare `#`, bare `subsection`, `containing` without its string, `re` or `rei` without its string, `// OR /`.
- The `named` word requires a quoted name argument. The `subsection` word requires a quoted name argument. The cluster operator `#` requires either an adjacent NamePattern or a trailing quoted name argument. `subsection "todo"` equals `#todo`. Bare `subsection` is a missing-argument parse error, uniform with bare `#`.
- A bare NamePattern element of a cluster is the argument of an implicit `/` (chapter 3).
- Symbols are never operator arguments. The cluster fragment `//` desugars to `root /`, so bare `//` is a missing-argument parse error and the locked example `// tree` becomes `root tree`.
- A compound predicate after `NOT` or `outer` needs parentheses (`left NOT (containing "the" AND named "blue")`, `root outer (containing "blue" AND named "x")`), because each arm takes one NotExpr, as the precedence lock requires. Bare `outer` is a missing-operand parse error, uniform with bare `NOT`.
- Same-operator `AND` and `OR` chains are semantically associative; mixed `d AND b OR c` parses as `(d AND b) OR c` by precedence, but write the parentheses.
- There is no literal in the Term production's head position: literals are arguments, never terms.

## 5. Type system and signatures

The judgment `⊢ e : τ1 ⇒ τ2` reads: `e` denotes a predicate from a `τ1` Answer to `τ2` Answers. Signatures compose by matching types:

```text
w : τ1 ⇒ τ2 in the catalog, its slot filled by a literal of the row's argument sort   ⊢ w a : τ1 ⇒ τ2
⊢ e1 : τ1 ⇒ τ2   ⊢ e2 : τ2 ⇒ τ3                                                       ⊢ e1 e2 : τ1 ⇒ τ3
⊢ e1 : τ1 ⇒ τ2   ⊢ e2 : τ1 ⇒ τ2                                                       ⊢ e1 OR e2 : τ1 ⇒ τ2   (AND and comma the same)
⊢ e : τ ⇒ τ'                                                                           ⊢ NOT e : τ ⇒ τ
⊢ e : Node ⇒ τ                                                                         ⊢ outer e : Node ⇒ Node
```

Argument sorts are syntactic, not Answer types: each row declares name (a glob string, spelled as a cluster NamePattern or a quoted string), or number-or-`*`. Slot filling is a lexical and grammatical concern (chapters 3 and 4), so an ill-filled slot is a parse error, not a type error.

The locked rules fall out as derived facts:

- Mixing types across `AND`, `OR`, comma, or `NOT` is a type error, because the rule premises force the operand types to agree.
- `<expr> containing root` is a missing-argument parse error: `root` is a symbol, and symbols are never operator arguments, so the string slot of `containing` stays unfilled. The draft labeled this case a type error; under the literals-are-arguments model it is a parse error reported in the same format, and every consumer in this slice displays the two alike (chapter 8).
- Old Amble prefix `text #todo` is a type error: no composition matches `Node ⇒ Text` followed by `Node ⇒ Node`.
- Each consumer applies the Expression to a Node Answer (chapter 8), so a top-level Expression must type `Node ⇒ τ`; Run accepts `τ` of Node or Text, Search and Move require `τ` of Node.

## 6. Evaluation semantics

`E⟦e⟧ : Answer → Answer sequence`. The rules cover the whole language; `δ(w)` is the row's Answer function from chapter 7.

```text
E⟦w a⟧ x         = δ(w) a x, where a is the row's literal argument value (absent for rows without a slot)
E⟦e1 e2⟧ x       = concat [ E⟦e2⟧ y | y ← E⟦e1⟧ x ]
E⟦e1 OR e2⟧ x    = E⟦e1⟧ x ++ E⟦e2⟧ x                    (comma the same)
E⟦e1 AND e2⟧ x   = every y in E⟦e1⟧ x, in that order, at most once, that also appears (by Answer equality) in E⟦e2⟧ x
E⟦NOT e⟧ x       = ⟨x⟩ when E⟦e⟧ x is empty, otherwise ⟨⟩
E⟦outer e⟧ x     = Owned depth-first walk strictly below x in Children order: at each visited Node N, if E⟦e⟧ N is nonempty then yield N and do not enter Owned Children of N, else recurse on the Owned Children of N
```

- Juxtaposition is monadic bind over the sequence and therefore associative: `((a) b) c` and `a (b c)` denote the same function; the left-associativity lock only fixes the parse tree.
- Order is fixed by these rules: juxtaposition is left-to-right; `OR` and comma concatenate and may repeat an Answer; `AND` keeps the left operand's order, each Answer at most once.
- Deduplication belongs to `AND`, the closure row `descendant`, and each subsection search (`#`). Composition never dedupes, so `child child` legitimately repeats a Node that appears under two parents, and separate left-input Answers can still produce the same Node.
- Lazy, eager, or backtracking evaluation is an implementation freedom, because the rules fix only the sequence and its order.
- Theorem (provable remark, not a rule): when `f` and `g` are pure filters — each yields a subsequence of `⟨x⟩`, as `containing`, `re`, `rei`, `named`, `class`, `section`, and the classification filters `ws`, `dir`, `file`, and `normal` do — `f g` and `f AND g` denote the same function; a generator such as `descendant` makes them differ.
- `outer` fuses the operand into that Owned walk (prune-during-accept). `acceptable(N)` is `E⟦e⟧ N` nonempty, the `NOT` emptiness test inverted. It is not a generator followed by a filter, and it is not a post-pass over `tree`. The walk is Owned only and does not yield the input, same as `tree`. Unloaded is a miss per the Unloaded rule.
- Unloaded rule: a walk step that needs the Children of an Unloaded Node yields no Answers from that Node. It is a miss, never an error, and it never Loads. All evaluation is local to the Browser Graph ([[.scratch/expression-language/issues/14-server-side-search.md]]).

## 7. The catalog

An entry has one or more spellings, an optional argument slot, a signature, and a one-line Answer function. Nothing else in the semantics knows an entry by name. Fixity is a catalog column, not a syntax class: an anchor is a row that ignores its input, a postfix word is a row with no slot, an infix word is a row with one slot. The catalog is closed: a standalone Name that is no row's spelling is a parse error.

Stated once for every row: a miss is the empty sequence (fail-to-answer); a walk that meets an Unloaded Node is a miss (chapter 6); glob: in any name argument, `*` matches zero or more characters within one name, and matching is case-insensitive.

| Entry | Spellings | Slot | Signature | Answer function |
| --- | --- | --- | --- | --- |
| root | `root` | — | `τ ⇒ Node` | Ignore the input; yield ROOT. The cluster fragment `//` is exactly shorthand for `root /`, so `//name` desugars to `root / "name"` and bare `//` is a missing-argument parse error. |
| structural search | `/`; also implicit for an unconsumed cluster name | required name | `Node ⇒ Node` | The Workspace Nodes, Directory Nodes, and File Nodes (containers including files) whose name matches the glob, by Owned recursive descent below the input that does not enter the Children of a Directory Node or Workspace Node; deeper structure is reached by chaining (`//ws/x`). |
| subsection | `subsection`; cluster `#` | required name | `Node ⇒ Node` | Search strictly below the input for sections whose name matches the glob; wall, traversal, and deduplication rules are below the table. `subsection "todo"` equals `#todo`. Bare `#` and bare `subsection` are missing-argument parse errors. |
| named | `named` | required quoted name | `Node ⇒ Node` | Yield the input when it is a Normal Node whose name matches the glob; otherwise yield no Answers. This row is a pure filter and does not search Children. |
| child | `child` | — | `Node ⇒ Node` | The Children of the input (Owned and Ref), in Children order; the same set as `:` with `*`. |
| descendant | `descendant` | — | `Node ⇒ Node` | Every Node reachable through one or more child steps (follows Ref), depth-first in Children order, each Node at most once by Node identity (first reach wins). |
| tree | `tree`; cluster `**` | — | `Node ⇒ Node` | Every Node reachable through one or more Owned steps, depth-first in Children order; Owned placement is unique and acyclic, so no dedupe clause is needed. |
| containing | `containing` | required quoted string | `Node ⇒ Node` | Yield the input when its Header text contains the argument as a case-insensitive substring (Header text only, not the name). |
| re | `re` | required quoted string | `Node ⇒ Node` | Yield the input when its Header text matches the argument as a regular expression, case-sensitive (Header text only, not the name). The engine is `System.Text.RegularExpressions` in Shared (.NET and Fable/JS). An invalid pattern is a miss. `x re ".*blue.*"` equals `x containing "blue"` when the Header has that same-case substring. |
| rei | `rei` | required quoted string | `Node ⇒ Node` | Same as `re`, but case-insensitive via engine flags (`RegexOptions.IgnoreCase` on .NET; the `i` flag on Fable/JS). Case does not come from an inline `(?i)` group; Fable/JS typically does not honor `(?i)`. An invalid pattern is a miss. |
| ws | `ws` | — | `Node ⇒ Node` | Yield the input when it is a Workspace Node; a pure filter. `root ws` equals `root`, because ROOT is a Workspace Node. |
| dir | `dir` | — | `Node ⇒ Node` | Yield the input when it is a Directory Node; a pure filter. Directories named `d` is `x / "d" dir`. |
| file | `file` | — | `Node ⇒ Node` | Yield the input when it is a File Node; a pure filter. |
| normal | `normal` | — | `Node ⇒ Node` | Yield the input when it is a Normal Node; a pure filter. |
| section | `section` | — | `Node ⇒ Node` | Yield the input when it is a section (a named Normal Node); a pure filter. Unnamed Normal Nodes are not sections. |
| class | `class` | required quoted string | `Node ⇒ Node` | Yield the input when the argument is a member of the Node's cssClasses token list; membership is exact and case-sensitive, aligned with the implemented `CssClass.contains` ([[src/Shared/CssClass.fs]]); a pure filter, no glob and no substring. |
| structural up | cluster `^` | — | `Node ⇒ Node` | The nearest File Node, Directory Node, or Workspace Node up the Owned chain, the input included. |
| directory up | cluster `.` | — | `Node ⇒ Node` | The nearest Directory Node or Workspace Node up the Owned chain, the input included. |
| wsroot | `wsroot` | — | `Node ⇒ Node` | The nearest Workspace Node up the Owned chain, the input included; ROOT is a Workspace Node, so a rooted input always has this Answer. |
| child at | cluster `:` | required Int or `*` | `Node ⇒ Node` | With an Int: the Child at zero-based index n in Children order; out of range is a miss. With `*`: all Children — the child row. Bare `:` does not parse. |
| sibling at | cluster `!` | required Int or `*` | `Node ⇒ Node` | With an Int: among the Children of the input's Owned parent, the appearance at signed offset n from the input's Owned appearance; `!0` is the input; out of range, or ROOT as input, is a miss. With `*`: all Children of the Owned parent, the input included. Bare `!` does not parse. |

Search rules:

- Structural search (`/`, including the `//` desugar and the implicit `/`): decided 2026-08-28 — Owned recursive descent strictly below the input, not entering the Children of a Directory Node or Workspace Node. RefExpr's trailing-slash directory-only constraint has no spelling here; its meaning is spelled with the classification filter, `x / "d" dir` (chapter 9).
- Subsection search (`#`, spoken `subsection`): search strictly below each input through Children, both Owned and Ref. Visit Nodes depth-first in Children order and visit each Node at most once per input by Node identity; first reach wins. An unnamed Normal Node is transparent (not a section). A section is a wall: yield it when its name matches, and never enter its Children. Do not enter the Children of a File Node, Directory Node, or Workspace Node. Therefore `a#b#c` first applies implicit `/ "a"`, then searches below each resulting Answer for sections named `b`, then searches below each resulting `b` for sections named `c`.
- Section filter (`section`): test only the input Node. Yield it when it is a named Normal Node; otherwise yield no Answers.
- Name filter (`named`): test only the input Node. `containing "the" AND named "blue"` applies both pure filters to the same input and keeps it only when its Header contains `the` and it is a Normal Node whose name matches `blue`. `named` is not `#`; `#` is subsection search.

Combinators are rows too, with their Answer functions given by the chapter 6 rules:

| Entry | Spellings | Signature | Answer function |
| --- | --- | --- | --- |
| NOT | `NOT` | `(τ ⇒ τ') → (τ ⇒ τ)` | Negation-as-failure: yield the input when the operand yields nothing from it. |
| outer | `outer` | `(Node ⇒ τ) → (Node ⇒ Node)` | Outermost acceptable Owned descendants: walk strictly below the input, Owned only, depth-first in Children order; at each N, if the operand yields any Answer from N then yield N and do not visit descendants of N, else recurse on the Owned Children of N. Unloaded is a miss. Bare `outer` is a missing-operand parse error. |
| AND | `AND` | `(τ1 ⇒ τ2) → (τ1 ⇒ τ2) → (τ1 ⇒ τ2)` | Pointwise order-preserving intersection by Answer equality, per the AND rule. |
| OR | `OR`; `,` | `(τ1 ⇒ τ2) → (τ1 ⇒ τ2) → (τ1 ⇒ τ2)` | Pointwise concatenation, per the OR rule; may repeat. |

Reserved for later slices, defined here only so their shape is fixed:

| Entry | Spellings | Signature | Answer function |
| --- | --- | --- | --- |
| text | `text` (postfix) | `Node ⇒ Text` | The input Node's text. Its fixity is locked (`Ref text`, never prefix), but the row is outside this closed slice. |
| name, sort | `name`, `sort` | — | Later tickets. |
| Number producers | — | `… ⇒ Number` | When one joins, Number becomes a type; number literals stay operator arguments unless a later spec revises the literals-are-arguments rule. |

## 8. Consumers: Run, Search, and Move

The omitted-left rule is a consumer concern, not an Expression rule: an Expression denotes a function, and each consumer applies it to its own initial Answer. What the draft called the omitted initial left operand is simply this application. A leading `/x` or `a/b/c` therefore searches under the consumer's initial Answer, and `// OR /` is a missing-argument parse error, uniform with every other unfilled required slot (the final barrier 2 resolution).

Statement forms. Run accepts exactly two line forms: `= Expression` and `Name=Expression`, where Name is one Name token and whitespace around `=` is allowed. Any other line is not a statement and Run does nothing; `#ident = Expression` is rejected this way, because `#ident` is a cluster, not a Name, and a reserved word is not a Name. Bare Expression lines are not Run statements. `Name=` is Run-only. Search and Move: a leading `=` evaluates the Expression; otherwise the line is today's word search, outside this spec's semantics.

| Consumer | Initial Answer | Required type | Disposal | Merged outcomes |
| --- | --- | --- | --- | --- |
| Run `= E` | the current Node | `Node ⇒ Node` or `Node ⇒ Text` | Node Answers become Ref Children and Text Answers become new Owned Nodes, in Answer order; when Run writes Children it unfolds that Node | parse error, type error, and zero Answers all write one blueletter Child `No matches found` |
| Run `Name=E` | the current Node | as `= E` | as `= E`, plus rename the current Node to Name | as `= E` |
| Search `= E` | zoomRoot | `Node ⇒ Node` | the scrolling dialog fetches Answers as the user scrolls; the user picks one Node; Zoom to it | parse error, type error, and zero Answers all show no hits |
| Move `= E` | zoomRoot | as Search | as Search, but relocate to the picked Node | as Search |

Gap note: the locks name zero Answers and type error for Run's blueletter Child; this spec merges the Expression parse error into the same display for uniformity with Search and Move. This is a completion of a gap, not a lock.

All evaluation is local (Run, Search `=`, Move `=`); server-side Search is postponed ([[.scratch/expression-language/issues/14-server-side-search.md]]). There is no display consumer in this slice.

## 9. Divergences from the implemented RefExpr and Amble

Revision list for the later implementation effort, against [[src/Shared/RefExprMatch.fs]], [[src/Shared/RefExprParse.fs]], [[src/Shared/AmbleParse.fs]], and their tests; grounded in [[.scratch/expression-language/reports/existing-language-survey.md]].

1. `**` today walks Owned Children, stops at Directory Node and Workspace Node, and includes the base Node; this spec's `**` is `tree`: transitively Owned, no stop, input excluded.
2. `/` today is the anchor "nearest Workspace, else ROOT"; this spec's `/` is the infix structural-search operator with a required name argument, and the up-walk meaning moves to the word `wsroot`.
3. RefExpr's DirStep constraint (trailing `d/` meaning directories only) has no spelling in this spec and is dropped: `/` finds Workspace Nodes, Directory Nodes, and File Nodes uniformly, and the directory-only meaning is spelled `x / "d" dir` with the classification filter. FileStep's file-only match widens the same way (`x / "f" file` restores it).
4. The bare `#` anchor (nearest named Normal ancestor) is removed; `#` always takes a name argument and searches down for subsections (sections below). Spoken spelling `subsection`.
5. The leading `!` anchor and bare `!` (context Node) and bare `:` (all Children) are removed; `!0` is the identity, `!*` and `:*` are the all-forms.
6. Amble prefix `FunCall` juxtaposition is gone: `text Ref` becomes `Ref text` (prefix `text` is a type error), `of` is dropped, and comma-as-`FunCall` sugar is dropped (comma is `OR`).
7. Amble today rejects a leading name without an anchor; this spec accepts it as the argument of an implicit `/` from the current Node, matching search's implicit context.
8. Run today evaluates a bare anchored RefExpr line and writes redletter error Children; this spec runs only `=` and `Name=` forms and writes one blueletter Child `No matches found`.
9. `RefExprParse` today accepts quoted segments inside a path; this spec keeps cluster-internal quotes out of scope (chapter 10), while spaced quoted arguments (`x / "a b"`) are new.
10. Implemented `pathSearchFrom` follows Ref Children and recurses through Directory Node and Workspace Node Children; this spec's structural search is decided as Owned-only descent that stops there, so the implementation and its Ref-following path-step tests must be revised.
11. Implemented content search and this spec's subsection search (`#`) both start strictly below the base. The `named` row tests the input itself because it is a filter, not a search. The `section` row also tests the input: it keeps a named Normal Node and yields nothing otherwise.
12. Implemented `tagSearchFrom` follows Ref Children, consistent with this spec's `#` traversal; [[doc/roadmap/reference-expression-interpretation.md]] says Owned only and must be revised. The `named` word is separate from `#`: it is a pure same-input name-glob filter and performs no traversal. `#` is subsection search, not a short form of `named`.
13. Implemented `globMatch` treats `?` as a one-character wildcard; this spec defines `*` as the only glob metacharacter, so `?` behavior must be specified or revised later.
14. `//` today is the ROOT anchor; this spec has no `//` row — the cluster fragment desugars to `root /` and bare `//` no longer parses.
15. The words `root`, `child`, `descendant`, `tree`, `containing`, `re`, `rei`, `named`, `section`, `subsection`, `wsroot`, `class`, `outer`, the classification filters `ws`/`dir`/`file`/`normal`, the reserved `AND`/`OR`/`NOT`, and the statement forms `=` / `Name=` are all new surface.

## 10. Deferred and not planned

Deferred to later slices: postfix `text`, `name`, and `sort` as catalog rows; the Number type and Number-returning producers; shell `> …`; quoted arguments inside a space-free cluster (`//"a b"/x`); server-side Search ([[.scratch/expression-language/issues/14-server-side-search.md]]); sugar `outer "blue"` for `outer containing "blue"`; a Ref-following analog of `outer`.

Not planned ([[.scratch/expression-language/issues/13-fog-of-the-first-spec.md]]): logical variables and unification; cut and if-then; a `findall`/`bagof` collection primitive (collection stays the consumer); a Boolean Answer type; a full Prolog system.

## 11. Worked examples

Extends [[.scratch/expression-language/reports/pipeline-examples.md]], re-checked against chapters 3 to 8. The current Node is the initial Answer except where a consumer row says zoomRoot. In the equivalence rows, `d` and `e` are literal names; for any left term `x`, `x /e` and `x / "e"` are the same application, and a leading bare name is its own implicit `/` argument.

| Expression | Type or outcome | Meaning |
| --- | --- | --- |
| `root descendant containing "the" named "blue"` | `Node ⇒ Node` | ROOT; closure of child; keep Nodes whose Header text contains `the`; then keep only those same Nodes that are Normal Nodes named `blue`. |
| `containing "the" AND named "blue"` | `Node ⇒ Node` | Same-input intersection: keep the current Node when its Header text contains `the` and it is a Normal Node named `blue`. |
| `re ".*blue.*"` | `Node ⇒ Node` | Keep the current Node when its Header text matches the regular expression, case-sensitive. Same Header field as `containing`. Equals `containing "blue"` when that substring is present in the same case. |
| `rei ".*BLUE.*"` | `Node ⇒ Node` | Same as `re`, case-insensitive via engine flags. |
| `#x , #y` | `Node ⇒ Node` | Comma is `OR`: subsection-search Answers of `#x`, then subsection-search Answers of `#y`, from the same input; may repeat a Node across the two searches. |
| `root descendant containing "the"` | `Node ⇒ Node` | The value row `root`, then the words compose. Amended from the locked `// descendant …`, which no longer parses (bare `//` lacks its name). |
| `root tree` | `Node ⇒ Node` | Transitively Owned Nodes from ROOT; acyclic; does not follow Ref; the same as `**`. Amended from the locked `// tree`. |
| `root outer containing "blue"` | `Node ⇒ Node` | Outermost Owned descendants of ROOT whose Header contains `blue`: a match yields and its Owned descendants are not visited; a non-match does not yield and the walk continues in its Owned Children. ROOT is not yielded (strictly below), same as `root tree`. Does not follow Ref. |
| `root ws` | `Node ⇒ Node` | Equals `root`: ROOT is a Workspace Node, so the classification filter keeps it. |
| `//ws/x` | `Node ⇒ Node` | One cluster desugaring to `root / "ws" / "x"`: structural Nodes named `ws` under ROOT, then structural Nodes named `x` under those. |
| `//ws` | `Node ⇒ Node` | Desugars to `root / "ws"`: Workspace, Directory, or File Nodes named `ws` under ROOT. Equals `// "ws"` and `root /ws`; `ws` there is a string, although it is also the Workspace filter word. |
| `//file` | `Node ⇒ Node` | Searches for structural Nodes named `file` under ROOT: argument position is literal-only, so the collision with the `file` filter word is harmless. The filter word acts in term position: `root / "x" file`. |
| `// ws` | parse error | Missing argument: the spaced `ws` is a symbol, and symbols are never operator arguments, so nothing fills the `/` of `//`. Adjacency is significant, by design. |
| `// "filename with spaces"` | `Node ⇒ Node` | Equals `root / "filename with spaces"`; a name containing spaces is expressible only as a quoted argument. |
| `// "a b" / "c d"` | `Node ⇒ Node` | Chained structural searches: `root / "a b" / "c d"`. |
| `/ "d" dir` | `Node ⇒ Node` | Structural Nodes named `d` below the current Node, kept only when they are Directory Nodes: the spelling that replaces RefExpr's DirStep. |
| `root descendant class "h1"` | `Node ⇒ Node` | Descendants of ROOT whose cssClasses contain the token `h1`; exact case-sensitive membership, no glob. |
| `d/e` | `Node ⇒ Node` | One cluster from the current Node: implicit `/` with argument `d`, then `/` with argument `e`; the spaced spelling is `/ "d" / "e"`. |
| `d#e` | `Node ⇒ Node` | Implicit `/` with argument `d`, then subsection search strictly below each resulting `d` for sections named `e`; equals `/ "d" # "e"` and `/ "d" subsection "e"`, but not `/ "d" named "e"`. |
| `a#b#c` | `Node ⇒ Node` | Apply implicit `/ "a"`; below each resulting Answer search for sections named `b`; below each resulting `b` search for sections named `c`. Each subsection search follows Owned and Ref Children depth-first in Children order, with Node-identity deduplication per input. |
| `"d" "e"` | parse error | Literals are never terms: neither string has a preceding operator that wants an argument. |
| `/` | parse error | Missing argument: `/` requires a name, uniform with `containing` lacking its string. |
| `// OR /` | parse error | A missing-argument parse error twice over: bare `//` lacks the name of its `root /` desugar, and bare `/` lacks its name (final barrier 2 resolution). |
| `wsroot` | `Node ⇒ Node` | The containing Workspace: nearest Workspace Node up the Owned chain from the current Node. |
| `wsroot #todo` | `Node ⇒ Node` | Up to the containing Workspace, then subsection search strictly below it through Owned and Ref Children for sections named `todo`. Equals `wsroot subsection "todo"`. |
| `^#blue` | `Node ⇒ Node` | From the structural container, subsection search down; a section walls the search, so `blue` under `todo` is missed; `^#todo#blue` finds it. |
| `child` | `Node ⇒ Node` | The Children of the current Node (Owned and Ref); the same set as `:*`. |
| `!-249053534` | zero Answers | Out-of-range sibling offset: a miss, not an error. |
| `root descendant NOT containing "draft"` | `Node ⇒ Node` | Descendants of ROOT kept when the `containing "draft"` predicate yields nothing from them. |
| `#todo text` | `Node ⇒ Text` (later slice) | Subsection-search sections named `todo`, then each Node's text; `text` is postfix and outside this closed catalog slice. |
| `subsection "todo"` | `Node ⇒ Node` | Equals `#todo`: search strictly below the input for sections named `todo`. |
| `section` | `Node ⇒ Node` | Yield the input when it is a section (a named Normal Node); empty on an unnamed Normal Node or any Special Kind. |
| `text #todo` | type error | Old Amble prefix form; no composition matches `Node ⇒ Text` then `Node ⇒ Node`. |
| `root descendant containing root` | parse error | `root` is a symbol and cannot fill the string slot of `containing`; reported in the type-error format (the draft's label for this case). |
| `3` | parse error | A literal with no operator wanting it; reported with the locked wording: a number is only valid as the right operand of `:` or `!`. |
| `= root descendant named "blue"` | Run statement | Materialise Node Answers as Ref Children of the current Node; unfold when Children are written. |
| `todo=root descendant named "blue"` | Run statement | As above, plus rename the current Node to `todo`. |
