
import { Graph, Node$, Revision, Revision__get_Value, NodeId, NodeId__get_Value } from "./Model.js";
import { field, fail, andThen, list as list_3, string, object, int, guid, map } from "../fable_modules/Thoth.Json.Core.0.7.1/Decode.fs.js";
import { list as list_2, lossyOption } from "../fable_modules/Thoth.Json.Core.0.7.1/Encode.fs.js";
import { map as map_1 } from "../fable_modules/fable-library-js.5.0.0-alpha.23/List.js";
import { map as map_2 } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Seq.js";
import { ofList, toList } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Map.js";
import { compare } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Util.js";
import { Change, Op } from "./History.js";
import { concat } from "../fable_modules/fable-library-js.5.0.0-alpha.23/String.js";

export function encodeNodeId(nodeId) {
    let value_1;
    let copyOfStruct = NodeId__get_Value(nodeId);
    value_1 = copyOfStruct;
    return {
        Encode(helpers) {
            return helpers.encodeString(value_1);
        },
    };
}

export const decodeNodeId = map((Item) => (new NodeId(Item)), guid);

export function encodeRevision(rev) {
    const value = Revision__get_Value(rev) | 0;
    return {
        Encode(helpers) {
            return helpers.encodeSignedIntegralNumber(value);
        },
    };
}

export const decodeRevision = map((Item) => (new Revision(Item)), int);

export function encodeNode(node) {
    const values_1 = [["id", encodeNodeId(node.id)], ["text", {
        Encode(helpers) {
            return helpers.encodeString(node.text);
        },
    }], ["name", lossyOption((value_1) => ({
        Encode(helpers_1) {
            return helpers_1.encodeString(value_1);
        },
    }))(node.name)], ["children", list_2(map_1(encodeNodeId, node.children))]];
    return {
        Encode(helpers_2) {
            const arg = map_2((tupledArg) => [tupledArg[0], tupledArg[1].Encode(helpers_2)], values_1);
            return helpers_2.encodeObject(arg);
        },
    };
}

export const decodeNode = object((get$) => {
    let objectArg, objectArg_1, objectArg_2, arg_7, objectArg_3;
    return new Node$((objectArg = get$.Required, objectArg.Field("id", decodeNodeId)), (objectArg_1 = get$.Required, objectArg_1.Field("text", string)), (objectArg_2 = get$.Optional, objectArg_2.Field("name", string)), (arg_7 = list_3(decodeNodeId), (objectArg_3 = get$.Required, objectArg_3.Field("children", arg_7))));
});

export function encodeGraph(graph) {
    const nodeList = map_1((arg) => encodeNode(arg[1]), toList(graph.nodes));
    const values = [["root", encodeNodeId(graph.root)], ["nodes", list_2(nodeList)]];
    return {
        Encode(helpers) {
            const arg_1 = map_2((tupledArg) => [tupledArg[0], tupledArg[1].Encode(helpers)], values);
            return helpers.encodeObject(arg_1);
        },
    };
}

export const decodeGraph = object((get$) => {
    let objectArg, arg_3, objectArg_1;
    return new Graph((objectArg = get$.Required, objectArg.Field("root", decodeNodeId)), ofList(map_1((n) => [n.id, n], (arg_3 = list_3(decodeNode), (objectArg_1 = get$.Required, objectArg_1.Field("nodes", arg_3)))), {
        Compare: (x, y) => (compare(x, y) | 0),
    }));
});

export function encodeOp(op) {
    switch (op.tag) {
        case 1: {
            const values_1 = [["type", {
                Encode(helpers_3) {
                    return helpers_3.encodeString("SetText");
                },
            }], ["nodeId", encodeNodeId(op.fields[0])], ["oldText", {
                Encode(helpers_4) {
                    return helpers_4.encodeString(op.fields[1]);
                },
            }], ["newText", {
                Encode(helpers_5) {
                    return helpers_5.encodeString(op.fields[2]);
                },
            }]];
            return {
                Encode(helpers_6) {
                    const arg_1 = map_2((tupledArg_1) => [tupledArg_1[0], tupledArg_1[1].Encode(helpers_6)], values_1);
                    return helpers_6.encodeObject(arg_1);
                },
            };
        }
        case 2: {
            const values_4 = [["type", {
                Encode(helpers_7) {
                    return helpers_7.encodeString("Replace");
                },
            }], ["parentId", encodeNodeId(op.fields[0])], ["index", {
                Encode(helpers_8) {
                    return helpers_8.encodeSignedIntegralNumber(op.fields[1]);
                },
            }], ["oldIds", list_2(map_1(encodeNodeId, op.fields[2]))], ["newIds", list_2(map_1(encodeNodeId, op.fields[3]))]];
            return {
                Encode(helpers_9) {
                    const arg_2 = map_2((tupledArg_2) => [tupledArg_2[0], tupledArg_2[1].Encode(helpers_9)], values_4);
                    return helpers_9.encodeObject(arg_2);
                },
            };
        }
        default: {
            const values = [["type", {
                Encode(helpers) {
                    return helpers.encodeString("NewNode");
                },
            }], ["nodeId", encodeNodeId(op.fields[0])], ["text", {
                Encode(helpers_1) {
                    return helpers_1.encodeString(op.fields[1]);
                },
            }]];
            return {
                Encode(helpers_2) {
                    const arg = map_2((tupledArg) => [tupledArg[0], tupledArg[1].Encode(helpers_2)], values);
                    return helpers_2.encodeObject(arg);
                },
            };
        }
    }
}

export const decodeOp = andThen((opType) => {
    switch (opType) {
        case "NewNode":
            return object((get$) => {
                let objectArg, objectArg_1;
                return new Op(0, [(objectArg = get$.Required, objectArg.Field("nodeId", decodeNodeId)), (objectArg_1 = get$.Required, objectArg_1.Field("text", string))]);
            });
        case "SetText":
            return object((get$_1) => {
                let objectArg_2, objectArg_3, objectArg_4;
                return new Op(1, [(objectArg_2 = get$_1.Required, objectArg_2.Field("nodeId", decodeNodeId)), (objectArg_3 = get$_1.Required, objectArg_3.Field("oldText", string)), (objectArg_4 = get$_1.Required, objectArg_4.Field("newText", string))]);
            });
        case "Replace":
            return object((get$_2) => {
                let objectArg_5, objectArg_6, arg_15, objectArg_7, arg_17, objectArg_8;
                return new Op(2, [(objectArg_5 = get$_2.Required, objectArg_5.Field("parentId", decodeNodeId)), (objectArg_6 = get$_2.Required, objectArg_6.Field("index", int)), (arg_15 = list_3(decodeNodeId), (objectArg_7 = get$_2.Required, objectArg_7.Field("oldIds", arg_15))), (arg_17 = list_3(decodeNodeId), (objectArg_8 = get$_2.Required, objectArg_8.Field("newIds", arg_17)))]);
            });
        default:
            return fail(concat("Unknown Op type: ", ...opType));
    }
}, field("type", string));

export function encodeChange(change) {
    const values_1 = [["id", {
        Encode(helpers) {
            return helpers.encodeSignedIntegralNumber(change.id);
        },
    }], ["ops", list_2(map_1(encodeOp, change.ops))]];
    return {
        Encode(helpers_1) {
            const arg = map_2((tupledArg) => [tupledArg[0], tupledArg[1].Encode(helpers_1)], values_1);
            return helpers_1.encodeObject(arg);
        },
    };
}

export const decodeChange = object((get$) => {
    let objectArg, arg_3, objectArg_1;
    return new Change((objectArg = get$.Required, objectArg.Field("id", int)), (arg_3 = list_3(decodeOp), (objectArg_1 = get$.Required, objectArg_1.Field("ops", arg_3))));
});

