# Pipeline versus Amble juxtaposition

Type: grilling
Status: resolved
Blocked by: none

## Question

How does the left-to-right word pipeline `root descendant containing "the" tagged "blue"` relate to existing Amble prefix `FunCall` juxtaposition (`text Ref`, `name of children …`)? One surface, two surfaces, or desugar the pipeline into `FunCall`?

Recommended answer (HITL confirm): the pipeline is the query surface; existing prefix `FunCall` desugars into the same operators. Do not keep two unrelated eval models.

## Answer

HITL 2026-08-27. One surface. Juxtaposition is the syntax. It is not Amble prefix `FunCall`, and there are not two eval models.

Space is a lexical separator only. Space is not an operator.

Juxtaposition is left-associative: `a b c` means `((a) b) c`. This is the reverse of APL (APL is right-associative with prefix monadics). Here monadics are postfix.

The existing lexer stays. `#todo` is two tokens, `#` and `todo`. `(left) #todo` means `(left) tagged "todo"`.

Fixity mix:

- **Anchor:** a value, for example `root`.
- **Postfix:** unary on the left, for example `descendant`.
- **Infix:** left and right, for example `containing`. The Expression checks the right-hand kind. `<expr> containing root` is a type error: `containing` does not take a Node on the right.

What was Amble `text Ref` is now `Ref text`: find a Node through the Ref, then postfix `text` yields that Node’s text. Prefix `text Ref` is a type error.

When the initial left operand is omitted, it is the current Node — the same kind of context that path refs already use.
