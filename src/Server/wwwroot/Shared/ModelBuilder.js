
import { singleton, item, length, ofArray, reverse, empty, cons, fold } from "../fable_modules/fable-library-js.5.0.0-alpha.23/List.js";
import { GraphModule_replace, GraphModule_setText, GraphModule_create, GraphModule_newNode } from "./Model.js";
import { Exception } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Util.js";
import { State, HistoryModule_empty } from "./History.js";

export function createNodes(texts, graph) {
    const patternInput_1 = fold((tupledArg, text) => {
        const patternInput = GraphModule_newNode(text, tupledArg[0]);
        return [patternInput[0], cons(patternInput[1], tupledArg[1])];
    }, [graph, empty()], texts);
    return [patternInput_1[0], reverse(patternInput_1[1])];
}

export function requireOk(label, result) {
    if (result.tag === 1) {
        throw new Exception(`${label}: ${result.fields[0]}`);
    }
    else {
        return result.fields[0];
    }
}

export function createDag12() {
    const patternInput = createNodes(ofArray(["a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k"]), GraphModule_create());
    const ids = patternInput[1];
    const graph1 = patternInput[0];
    if (length(ids) !== 11) {
        throw new Exception(`createDag12: expected 11 ids, got ${length(ids)}`);
    }
    const id = (index) => item(index, ids);
    const graph2 = requireOk("createDag12.setText", GraphModule_setText(graph1.root, "", "root", graph1));
    const replaceInsert = (parentId, newIds, graph) => requireOk("createDag12.replace", GraphModule_replace(parentId, 0, empty(), newIds, graph));
    return replaceInsert(id(5), singleton(id(10)), replaceInsert(id(3), singleton(id(9)), replaceInsert(id(2), ofArray([id(7), id(8)]), replaceInsert(id(1), ofArray([id(5), id(6)]), replaceInsert(id(0), ofArray([id(3), id(4)]), replaceInsert(graph2.root, ofArray([id(0), id(1), id(2)]), graph2))))));
}

export function createState12() {
    return new State(createDag12(), HistoryModule_empty);
}

