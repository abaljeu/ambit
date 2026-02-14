
import { Record, Union } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Types.js";
import { record_type, list_type, option_type, string_type, int32_type, union_type, class_type } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Reflection.js";
import { newGuid } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Guid.js";
import { tryFind, add, FSharpMap__ContainsKey, FSharpMap__get_Count, ofList } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Map.js";
import { append, skip, take, exists, length, singleton, empty } from "../fable_modules/fable-library-js.5.0.0-alpha.23/List.js";
import { equals, compare } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Util.js";
import { FSharpResult$2 } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Result.js";

export class NodeId extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["NodeId"];
    }
}

export function NodeId_$reflection() {
    return union_type("Gambol.Shared.NodeId", [], NodeId, () => [[["Item", class_type("System.Guid")]]]);
}

export function NodeId__get_Value(this$) {
    return this$.fields[0];
}

export function NodeId_New() {
    return new NodeId(newGuid());
}

export class Revision extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["Revision"];
    }
}

export function Revision_$reflection() {
    return union_type("Gambol.Shared.Revision", [], Revision, () => [[["Item", int32_type]]]);
}

export function Revision__get_Value(this$) {
    return this$.fields[0] | 0;
}

export function Revision_get_Zero() {
    return new Revision(0);
}

export function Revision_get_One() {
    return new Revision(1);
}

export class Node$ extends Record {
    constructor(id, text, name, children) {
        super();
        this.id = id;
        this.text = text;
        this.name = name;
        this.children = children;
    }
}

export function Node$_$reflection() {
    return record_type("Gambol.Shared.Node", [], Node$, () => [["id", NodeId_$reflection()], ["text", string_type], ["name", option_type(string_type)], ["children", list_type(NodeId_$reflection())]]);
}

export class Graph extends Record {
    constructor(root, nodes) {
        super();
        this.root = root;
        this.nodes = nodes;
    }
}

export function Graph_$reflection() {
    return record_type("Gambol.Shared.Graph", [], Graph, () => [["root", NodeId_$reflection()], ["nodes", class_type("Microsoft.FSharp.Collections.FSharpMap`2", [NodeId_$reflection(), Node$_$reflection()])]]);
}

export function GraphModule_create() {
    const rootId = NodeId_New();
    return new Graph(rootId, ofList(singleton([rootId, new Node$(rootId, "", undefined, empty())]), {
        Compare: (x, y) => (compare(x, y) | 0),
    }));
}

export function GraphModule_nodeCount(graph) {
    return FSharpMap__get_Count(graph.nodes) | 0;
}

export function GraphModule_contains(nodeId, graph) {
    return FSharpMap__ContainsKey(graph.nodes, nodeId);
}

export function GraphModule_newNode(text, graph) {
    const nodeId = NodeId_New();
    return [new Graph(graph.root, add(nodeId, new Node$(nodeId, text, undefined, empty()), graph.nodes)), nodeId];
}

export function GraphModule_setText(nodeId, oldText, newText, graph) {
    const matchValue = tryFind(nodeId, graph.nodes);
    if (matchValue != null) {
        const node = matchValue;
        if (node.text !== oldText) {
            return new FSharpResult$2(1, ["old text does not match"]);
        }
        else {
            return new FSharpResult$2(0, [new Graph(graph.root, add(nodeId, new Node$(node.id, newText, node.name, node.children), graph.nodes))]);
        }
    }
    else {
        return new FSharpResult$2(1, ["node not found"]);
    }
}

export function GraphModule_replace(parentId, index, oldIds, newIds, graph) {
    const parentOpt = tryFind(parentId, graph.nodes);
    if (parentOpt != null) {
        const parent = parentOpt;
        const children = parent.children;
        const childCount = length(children) | 0;
        const oldCount = length(oldIds) | 0;
        if ((index < 0) ? true : (index > childCount)) {
            return new FSharpResult$2(1, ["index out of bounds"]);
        }
        else if ((index + oldCount) > childCount) {
            return new FSharpResult$2(1, ["old span out of bounds"]);
        }
        else if (exists((nodeId) => !FSharpMap__ContainsKey(graph.nodes, nodeId), newIds)) {
            return new FSharpResult$2(1, ["new child not found"]);
        }
        else if (!equals(take(oldCount, skip(index, children)), oldIds)) {
            return new FSharpResult$2(1, ["old span does not match"]);
        }
        else {
            return new FSharpResult$2(0, [new Graph(graph.root, add(parentId, new Node$(parent.id, parent.text, parent.name, append(take(index, children), append(newIds, skip(index + oldCount, children)))), graph.nodes))]);
        }
    }
    else {
        return new FSharpResult$2(1, ["parent not found"]);
    }
}

