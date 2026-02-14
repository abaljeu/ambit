
import { StringBuilder__Append_Z721C83C5, StringBuilder__Append_244C7CD6, StringBuilder_$ctor } from "../fable_modules/fable-library-js.5.0.0-alpha.23/System.Text.js";
import { ofList, add, FSharpMap__get_Item } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Map.js";
import { substring, replace, trimEnd, split, isNullOrEmpty, replicate } from "../fable_modules/fable-library-js.5.0.0-alpha.23/String.js";
import { compare, disposeSafe, getEnumerator } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Util.js";
import { toString } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Types.js";
import { Graph, Node$, NodeId_New } from "./Model.js";
import { cons, singleton, append, empty, tail as tail_2, head, isEmpty } from "../fable_modules/fable-library-js.5.0.0-alpha.23/List.js";
import { fold } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Array.js";
import { takeWhile, length } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Seq.js";

/**
 * Serialize a graph to tab-indented text outline format.
 * The root node is implicit; its children become top-level lines.
 */
export function write(graph) {
    const sb = StringBuilder_$ctor();
    const writeNode = (depth, nodeId) => {
        const node = FSharpMap__get_Item(graph.nodes, nodeId);
        StringBuilder__Append_244C7CD6(StringBuilder__Append_Z721C83C5(StringBuilder__Append_Z721C83C5(sb, replicate(depth, "\t")), node.text), "\n");
        const enumerator = getEnumerator(node.children);
        try {
            while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                writeNode(depth + 1, enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]());
            }
        }
        finally {
            disposeSafe(enumerator);
        }
    };
    const enumerator_1 = getEnumerator(FSharpMap__get_Item(graph.nodes, graph.root).children);
    try {
        while (enumerator_1["System.Collections.IEnumerator.MoveNext"]()) {
            writeNode(0, enumerator_1["System.Collections.Generic.IEnumerator`1.get_Current"]());
        }
    }
    finally {
        disposeSafe(enumerator_1);
    }
    return toString(sb);
}

/**
 * Parse tab-indented text outline into a new Graph.
 * Creates new NodeIds; original IDs are not preserved.
 */
export function read(text) {
    const lines = isNullOrEmpty(text) ? (new Array(0)) : split(trimEnd(replace(text, "\r\n", "\n"), "\n"), ["\n"], undefined, 0);
    const rootId = NodeId_New();
    const popStack = (depth_mut, stack_mut) => {
        popStack:
        while (true) {
            const depth = depth_mut, stack = stack_mut;
            let matchResult, d_1, tail_1;
            if (!isEmpty(stack)) {
                if (compare(head(stack)[0], depth) >= 0) {
                    matchResult = 0;
                    d_1 = head(stack)[0];
                    tail_1 = tail_2(stack);
                }
                else {
                    matchResult = 1;
                }
            }
            else {
                matchResult = 1;
            }
            switch (matchResult) {
                case 0: {
                    depth_mut = depth;
                    stack_mut = tail_1;
                    continue popStack;
                }
                default:
                    return stack;
            }
            break;
        }
    };
    return new Graph(rootId, fold((tupledArg, line) => {
        const depth_1 = length(takeWhile((y) => ("\t" === y), line.split(""))) | 0;
        const nodeText = substring(line, depth_1);
        const nodeId = NodeId_New();
        const nodes_1 = add(nodeId, new Node$(nodeId, nodeText, undefined, empty()), tupledArg[0]);
        const stack_2 = popStack(depth_1, tupledArg[1]);
        const parentId = head(stack_2)[1];
        const parent = FSharpMap__get_Item(nodes_1, parentId);
        return [add(parentId, new Node$(parent.id, parent.text, parent.name, append(parent.children, singleton(nodeId))), nodes_1), cons([depth_1, nodeId], stack_2)];
    }, [ofList(singleton([rootId, new Node$(rootId, "", undefined, empty())]), {
        Compare: (x_1, y_1) => (compare(x_1, y_1) | 0),
    }), singleton([-1, rootId])], lines)[0]);
}

