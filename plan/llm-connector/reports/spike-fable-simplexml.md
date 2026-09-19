# Spike: Fable.SimpleXml for nested-tag pack

Question: can [Fable.SimpleXml](https://www.nuget.org/packages/Fable.SimpleXml) replace Shared.DotNet [DocumentNestedTag](src/Shared/dotnet/DocumentNestedTag.fs) (`XElement`) so the [11 — Simple extract format](../issues/11-simple-extract-format.md) pack can live in Fable Shared for Browser and Server?

Sibling proof module: [DocumentNestedTagSimpleXml](src/Shared/DocumentNestedTagSimpleXml.fs). It keeps `tagName` and pack Focus on `Graph.withFocus`. It does not replace the DotNet pack.

## 1. Package versions added

Added to [Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj):

1. **Fable.SimpleXml** `3.4.0` (latest on nuget.org at restore).
2. **Fable.Parsimmon** `4.1.0` (explicit, also a SimpleXml dependency).

Restore also pulled:

1. **Fable.Core** `4.1.0` (SimpleXml) and **Fable.Core** `3.0.0` (Parsimmon). Client already pins Fable.Core `4.5.0`.
2. The Fable local tool is `5.0.0-rc.6`.

The repo did not already reference SimpleXml.

## 2. .NET Shared build

**Pass.** `dotnet build src/Shared/Gambol.Shared.fsproj` succeeded with 0 warnings and 0 errors. Shared.Tests also compiled.

`.NET` **write** via Generator runs. Four xUnit facts passed: leaf `<div>`, nested `<focus>`, no `<?xml` declaration, and no entity escape versus `XElement`.

`.NET` **parse** does not run. `SimpleXml.tryParseElement` loads [Fable.Parsimmon](https://www.nuget.org/packages/Fable.Parsimmon), which is a JS binding (`import` of `Parsimmon.js`). On .NET the type initializer throws `System.Exception`: "You've hit dummy code used for Fable bindings. This probably means you're compiling Fable code to .NET by mistake, please check." Documented by ``parse cannot run on NET because Parsimmon is a Fable binding``. Existing [DocumentNestedTagTests](tests/Shared.Tests/DocumentNestedTagTests.fs) (XElement) still pass (10 facts).

## 3. Fable Client gate

**Pass.** `./scripts/client.sh build` compiled Client with Fable 5.0.0-rc.6 and `npm run bundle` wrote `Program.bundle.js`.

Client output does not emit unused Shared modules. A second compile `dotnet fable src/Shared/Gambol.Shared.fsproj` emitted [DocumentNestedTagSimpleXml.js](tmp/fable-shared-spike/DocumentNestedTagSimpleXml.js) and the SimpleXml / Parsimmon fable sources with 0 errors.

Node then called `parseExtract` on that JS. Leaf, nested Focus, residue, two roots, incomplete, and mismatch all matched the ticket 11 Result shape.

## 4. Generator + parse coverage

| Concern | Generator write | Parse (Fable / Node) | Parse (.NET) |
| --- | --- | --- | --- |
| 1. Nested elements | Yes. `node` + child `node` writes `<focus>Parent<div>Child</div></focus>`. | Yes. Recovers tag, text, children. | No. Fable dummy. |
| 2. Text | Yes. `text` is raw string content. | Yes for ordinary text. Content or concatenated `IsTextNode` children. | No. |
| 3. Focus tag | Yes. `writeTag` uses `"focus"` when `graph.focus` matches. | Yes. `el.Name` is `"focus"`. | No. |
| 4. Round-trip | Write string matches ticket 11 leaf and nested forms. | Yes on Fable for those forms. | Write only. |
| 5. Residue / reject | Not a write concern. | Yes. Wrap `<r>…</r>`: leftover text or a second root → `residue`; unclosed or mismatched close → `incomplete`. | Throws before a Result. |

Escaping versus the DotNet pack: Generator writes `A <div>B` as `<div>A <div>B</div>` (no `&lt;`). `XElement` writes `&lt;` / `&gt;` and round-trips to `A <div>B`. On Fable, parse of the unescaped Generator string is `incomplete`. Parse of `<div>A &lt;div&gt;B</div>` succeeds but keeps the entity text (`A &lt;div&gt;B`). Ticket 11 marks escaping out of scope.

## 5. Gaps versus ticket 11

1. **Attributes** — Generator `attr` and parse `Attributes` exist. Ticket 11 does not use attributes. No gap for this ticket.
2. **Namespaces** — Generator `namespaceXml` and parse `ns:name` exist. Ticket 11 does not use namespaces. No gap for this ticket.
3. **Mixed content** — Ticket 11 is leading Node text then child tags. Fable parse covers that. Text that contains `<` is not mixed content in the XML sense; it breaks parse unless escaped, and SimpleXml does not escape or decode.
4. **Fragment without declaration** — Generator `serializeXml` writes `<tag>…</tag>` only. Same role as `XElement.ToString(SaveOptions.DisableFormatting)`.
5. **Dual runtime** — This is the blocking gap. Parse cannot run on Server .NET. Paste and later Reference-Paste need Browser (Fable) and Server (.NET) in Shared, not Shared.DotNet / XDocument.
6. **Library age** — SimpleXml 3.4.0 (2023) and Parsimmon 4.1.0 (2019) target Fable.Core 3. They compiled under Fable 5, but parse is still JS-only.

## 6. Recommendation

**Reject** Fable.SimpleXml as the ticket 11 Shared pack.

Write via Generator can sit in Shared and run on both runtimes. Parse cannot. A pack that only writes on Server and only parses in the Browser is not enough for reply parse or a later Shared paste path.

Do not add XDocument. The DotNet pack already uses `XElement`. Leave that as the Server-only coded path. Do not force a second System.Xml stack.

No further SimpleXml spike is required for this question. If the pack must move into Fable Shared, use a Shared-native writer and parser (the earlier hand nested-tag), not a Fable-only JS binding.
