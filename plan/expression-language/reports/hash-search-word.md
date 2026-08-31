# Hash-search word (HITL)

This report is a ubiquitous-language proposal for the catalog/spoken name of cluster `#`. It does not lock a word. The user must pick. Do not treat “semantic search” as a candidate: [[CONTEXT.md]] has **Expression** (the language) and **Find** (the command). The spec already pairs `#` with `/` as two searches. This pass does not edit [[CONTEXT.md]], [[plan/expression-language/spec.md]], or production code.

What `#` does, and must not collapse: `#` is a downward search from each input Answer. It takes a name argument (`d#e`, `a#b#c`, `^#blue`, `#todo`). It finds Normal Nodes whose name matches the glob. It searches strictly below the input through Children (Owned and Ref). A named Normal Node is a wall. An unnamed Normal Node is transparent. It does not enter Children of a File Node, Directory Node, or Workspace Node. Ticket 01 HITL: `(left) #todo` means `(left) tagged "todo"`. `named` is already a pure filter (same-input subset) and is not this search.

## 1. Current conflicting names

| Informal name | Where it appears | What it actually names |
| --- | --- | --- |
| tagged / tag search / TagStep | Ticket 01 HITL ([[plan/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md]]): `#todo` is `#` plus `todo`, meaning tagged. Ticket 02 heading “`#` (tag / Normal names)” ([[plan/expression-language/issues/02-path-references-as-pipeline-terms.md]]). Roadmap “Tagged nodes” and `#` as “current tagged node” ([[doc/roadmap/reference-expression-interpretation.md]], [[doc/roadmap/reference-expressions.md]]). Code: `ExprAnchor.Tagged`, `TagStep`, `tagSearchFrom`, `visitTagged`, `RefContext.tagged`, parse error “expected tag name after #” ([[src/Shared/RefExprTypes.fs]], [[src/Shared/RefExprMatch.fs]], [[src/Shared/RefExprParse.fs]]). Survey: “tag filter” / “path/tag” ([[plan/expression-language/reports/existing-language-survey.md]]). Prototype row `#todo text`: “Nodes tagged `todo`” ([[plan/expression-language/reports/pipeline-examples.md]]). | The `#` search, and also the old bare-`#` *up* anchor (nearest named Normal ancestor). The spec drops that up-anchor ([[plan/expression-language/spec.md]] chapter 9 item 4). |
| named (as short form of `#`) | Ticket 03 HITL: `named` — not `tagged`; finds Normal Nodes with that name; short form `#x`; same glob as `#re*ed` ([[plan/expression-language/issues/03-first-primitive-catalog.md]]). Map and spec-draft still repeat that collapse ([[plan/expression-language/map.md]], [[plan/expression-language/spec-draft.md]]). Prototype: `containing "the" #blue` as the same intent as `named "blue"` ([[plan/expression-language/reports/pipeline-examples.md]]). | A later lock undid this. Ticket 20 and spec chapter 1: `#` and `named` are separate rows. |
| named (pure filter) | Spec catalog row `named` ([[plan/expression-language/spec.md]] chapter 7). Ticket 20 ([[plan/expression-language/issues/20-pure-filter-catalog-rows.md]]). Code `ExprWalk.named` ([[src/Shared/ExprWalk.fs]]). | Same-input subset: keep a Normal Node whose name matches. Not `#`. |
| named search | Interpretation doc: “Named search follows owned children only” ([[doc/roadmap/reference-expression-interpretation.md]]). Seams report uses that phrase for the Owned-only claim ([[plan/expression-language/reports/amble-refexpr-seams.md]]). Ticket 12 repeats it ([[plan/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md]]). | Ambiguous: the doc’s phrase covers path steps in general, not only `#`. Implementation `TagStep` follows Ref. |
| content search / Content | Spec catalog Entry “content search”, spelling cluster `#` ([[plan/expression-language/spec.md]] chapters 1, 7, 9, 11). Ticket 18 title and body ([[plan/expression-language/issues/18-content-search-path-step-evaluation.md]]). Tickets 16–17, 20, 25. Code: `ClusterStep.Content`, pending `ContentName`, `ExprWalk.contentSearch`, catalog spelling `"#"` ([[src/Shared/ExprPathClusterTypes.fs]], [[src/Shared/ExprPathClusterParse.fs]], [[src/Shared/ExprWalk.fs]], [[src/Shared/ExprPrimitive.fs]]). | The `#` row. Interpretation-doc **Content** is a different noun: the tree of Normal Nodes under a Workspace Node, Directory Node, or File Node. |
| ident | Spec consumers: `#ident = Expression` is rejected because `#ident` is a cluster, not a Name ([[plan/expression-language/spec.md]] chapter 8). Tickets 07 / 21. | A metasyntactic cluster, not an operator name. |
| Header | [[CONTEXT.md]] **Header**: everything in a Node except its Children. Spec `containing` matches Header text, not the name. | Not `#`. `#` matches the Node name of a Normal Node. |
| class | Spec catalog row `class` (cssClasses token; exact, case-sensitive). Ticket 20. HITL 2026-08-28: `class "h1"` is a pure filter. [[CONTEXT.md]] **Kind** lists *class* as an avoid-word for Kind. | Not `#`. |
| descendant-search | Spec chapter 11: `#x , #y` “descendant-search Answers”; `#todo text` “Descendant-search Nodes named `todo`”. | Informal. Catalog `descendant` is a different row (no name argument; closure of `child`; no named-Normal wall). |

[[CONTEXT.md]] has no term for `#`. **Find** is the command that searches the resident Graph. Do not reuse Find as the catalog word.

## 2. How `#` differs from `named`, `/`, and `class`

HITL 2026-08-28 (ticket 25): a pure filter takes a set of Nodes and returns a subset. `class "h1"` and `dir` are such. `/ "d" dir` is not.

`named` is a pure filter. It tests only the input Node. It yields that Node when it is a Normal Node whose name matches the glob; otherwise nothing. It does not walk Children. Spec chapter 11: `d#e` equals `/ "d" # "e"`, and is not `/ "d" named "e"`. Ticket 20 example: `named "blue"` on a File Node is empty; `#blue` from that File Node can find a Child.

`/` is structural search. It finds Workspace Nodes, Directory Nodes, and File Nodes whose name matches, by Owned recursive descent that does not enter Children of a Directory Node or Workspace Node. It does not find Normal Nodes. Ticket 02: `/x` is not tag search. `/ "d" dir` is structural search then a classification filter, not a pure filter.

`class` is a pure filter on cssClasses. Exact token membership. No glob. No Children walk. It does not look at the Node name.

`#` is a search, not a pure filter. Output Nodes are below the input, not a subset of the input set. It finds Normal Nodes by name. It follows Owned and Ref. It walls at a named Normal Node. Pair with `/`: two name searches, two domains (structural containers versus Normal Node names).

## 3. Candidate words

Not candidates: `named` (locked as the pure filter); `ident` (metasyntax); `Find` (command); `Header` (Node part); `class` (cssClasses); `descendant` (different walk); `content` as a spoken pipeline word (`content "todo"` is a poor analog of `tree`, and clashes with interpretation-doc Content and with `containing`).

### tagged

One-line meaning: from each input, search strictly below for Normal Nodes whose name matches the argument.

Clash: ticket 03 HITL said `named` — not `tagged`, and treated `#x` as the short form of `named`. That collapse is already undone (ticket 20; spec chapter 1). [[CONTEXT.md]] has no tag. Other systems call cssClasses “tags”; this catalog already uses `class` for that list, so the clash is informal English, not an existing row.

Matches HITL `tagged`: yes. Ticket 01 locked `(left) #todo` as `(left) tagged "todo"`. Code and the roadmap already say tag / tagged / TagStep.

### heading

One-line meaning: from each input, search strictly below for named Normal Nodes treated as outline headings (the wall is the next heading).

Clash: none in [[CONTEXT.md]]. **Header** is the Node field set, not a heading. Markdown `#` is a heading marker, which matches the glyph, not the HITL word.

Matches HITL `tagged`: no.

### label

One-line meaning: from each input, search strictly below for Normal Nodes that carry that name as a label.

Clash: none in [[CONTEXT.md]]. Weaker than `tagged`: every named Node already has a name; “label” adds no domain fact.

Matches HITL `tagged`: no.

### topic

One-line meaning: from each input, search strictly below for Normal Nodes named as that topic.

Clash: none in [[CONTEXT.md]]. Vague: a topic could be Header text (`containing`) as easily as a name.

Matches HITL `tagged`: no.

### section

One-line meaning: from each input, search strictly below for named Normal Nodes that open a section (the wall is the section boundary).

Clash: none in [[CONTEXT.md]]. Captures the wall rule well. Does not capture the hashtag surface `#todo`.

Matches HITL `tagged`: no.

## 4. Recommended candidate (not locked)

This report recommends **tagged**. It does not lock it.

Ticket 01 already spoke `#` as `tagged`. Ticket 03 rejected `tagged` only while `#` and `named` were one row; ticket 20 split them, and `named` now owns the pure filter. Restoring `tagged` as the search word (analogous to `tree` for `**`) keeps `named` as the same-input subset, matches the RefExpr names (`TagStep`, `tagSearchFrom`), and matches the hashtag surface. The remaining risk is informal “tag” versus `class` / cssClasses; if that risk is too high, pick **heading** (glyph and wall) or **section** (wall only).

Pick one of: `tagged`, `heading`, `label`, `topic`, `section`, or a different word. After a pick, lock it in the spec catalog and then in [[CONTEXT.md]].

## 5. If a word is locked later (do not edit now)

List only. No edits in this pass.

- [[plan/expression-language/spec.md]] chapter 1 bullet that `#` and `named` are separate: add the spoken spelling (`tagged "todo"` or the picked word) beside cluster `#`.
- Chapter 7 catalog table: Entry column “content search”; Spellings column cluster `#`; Answer-function line. Add the word as a spelling, parallel to `tree` / `**`.
- Chapter 7 “Search rules” bullet “Content search (`#`)”.
- Chapter 6 dedupe sentence that names “each `#` search”.
- Chapter 3 / 4 lines that `#` consumes a NamePattern or trailing quoted name (if the spaced word form is `WORD "name"`).
- Chapter 8 / 9: `#ident` as a cluster example can stay; item 11–12 “content search”; item 15 list of new surface words (add the picked word next to `named`).
- Chapter 11 rows that say “content search” or “descendant-search” for `#`: `d#e`, `a#b#c`, `^#blue`, `wsroot #todo`, `#x , #y`, `#todo text`. Add an equivalence row `WORD "todo"` = `#todo` if the word is a catalog spelling.
- [[plan/expression-language/issues/03-first-primitive-catalog.md]] and [[plan/expression-language/map.md]]: record an amendment that `named` is the pure filter only; `#` is not its short form.
- [[plan/expression-language/spec-draft.md]] line that still says `named` (not `tagged`; short form `#x`).
- [[CONTEXT.md]]: add the locked term (glossary only; no implementation). List `named` (filter), `class`, `containing`, and **Find** under `_Avoid_` as needed so they are not synonyms.
- Code names (`ClusterStep.Content`, `contentSearch`, `contentRow`) can wait; they are not the spoken lock. `TagStep` / `tagSearchFrom` already match `tagged`.

## 6. HITL 2026-08-28 — not an adjective

`#` bypasses unnamed Normal Nodes and stops at named ones. `/` is the same kind of name-search with a tighter walk (structural Kind, Owned, no descent into Directory Node or Workspace Node Children). A simple adjective names a quality of an already-found Node, so **tagged is out**. **heading** is reserved by `h1` / `h2`. **label** and **topic** stay weak. **section** might work; think again.

The spoken word belongs with `child`, `descendant`, and `tree` (generators that walk), not with `named`, `dir`, and `class` (pure filters). [[CONTEXT.md]] Graph avoids *outline*; Node avoids *entry*.

Walk-family candidates still open: `section` (named Normal as wall), `seek` (verb of descent, `seek "todo"`), or a new word. Do not lock.

## 7. HITL 2026-08-28 lock — `section` filter and `subsection` search

Locked. Tagged, heading, and seek are out.

**`section`**: a new builtin pure filter (same family as `dir`, `file`, `ws`, `normal`, `named`). It tests the input Node only. A section is a named Normal Node. Unnamed Normal Nodes are not sections; they are the transparent stretch that `#` walks through. Shape: zero-argument classification-style filter like `dir`. The name glob stays on `named` and on cluster `#` / `subsection`. Not `section "todo"`.

**`subsection`**: a new builtin function (generator / search). It is the spoken catalog spelling of cluster `#`, parallel to `tree` for `**`. Required name argument: `subsection "todo"` equals `#todo`. Downward search that bypasses unnamed Normal Nodes and stops at named ones (sections below). Do not treat `subsection` as prose-only.

`/` is unchanged (structural search). No union operator. `named` remains the name-glob pure filter.

Correction: a first reading of this HITL treated `subsection` as a description of `#` and not a catalog spelling. That reading is wrong. `subsection` is a function.

Catalog and glossary: [[plan/expression-language/spec.md]], [[CONTEXT.md]], [[plan/expression-language/map.md]]. Implementation is later ([[plan/expression-language/issues/26-section-and-subsection-catalog-rows.md]]). Do not implement `src/` in this pass.

## WORK.md

No mutation until the user picks. Do not add a decision ticket yet.

After the lock in section 7: propose `add` of issue 26. Parent applies [[WORK.md]]. See [[section-filter-lock.md]].
