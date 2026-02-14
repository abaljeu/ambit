
import { Record, Union } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Types.js";
import { Graph, Node$, GraphModule_replace, GraphModule_setText, Graph_$reflection, NodeId_$reflection } from "./Model.js";
import { record_type, union_type, list_type, int32_type, string_type } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Reflection.js";
import { remove, add } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Map.js";
import { tail, head, isEmpty, cons, reverse, fold, singleton, append, empty } from "../fable_modules/fable-library-js.5.0.0-alpha.23/List.js";
import { FSharpResult$2 } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Result.js";
import { max } from "../fable_modules/fable-library-js.5.0.0-alpha.23/Double.js";

export class Op extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["NewNode", "SetText", "Replace"];
    }
}

export function Op_$reflection() {
    return union_type("Gambol.Shared.Op", [], Op, () => [[["nodeId", NodeId_$reflection()], ["text", string_type]], [["nodeId", NodeId_$reflection()], ["oldText", string_type], ["newText", string_type]], [["parentId", NodeId_$reflection()], ["index", int32_type], ["oldIds", list_type(NodeId_$reflection())], ["newIds", list_type(NodeId_$reflection())]]]);
}

export class Change extends Record {
    constructor(id, ops) {
        super();
        this.id = (id | 0);
        this.ops = ops;
    }
}

export function Change_$reflection() {
    return record_type("Gambol.Shared.Change", [], Change, () => [["id", int32_type], ["ops", list_type(Op_$reflection())]]);
}

export class History extends Record {
    constructor(past, future, nextId) {
        super();
        this.past = past;
        this.future = future;
        this.nextId = (nextId | 0);
    }
}

export function History_$reflection() {
    return record_type("Gambol.Shared.History", [], History, () => [["past", list_type(Change_$reflection())], ["future", list_type(Change_$reflection())], ["nextId", int32_type]]);
}

export class State extends Record {
    constructor(graph, history) {
        super();
        this.graph = graph;
        this.history = history;
    }
}

export function State_$reflection() {
    return record_type("Gambol.Shared.State", [], State, () => [["graph", Graph_$reflection()], ["history", History_$reflection()]]);
}

export class ApplyResult extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Changed", "Unchanged", "Invalid"];
    }
}

export function ApplyResult_$reflection() {
    return union_type("Gambol.Shared.ApplyResult", [], ApplyResult, () => [[["Item", State_$reflection()]], [["Item", State_$reflection()]], [["Item1", State_$reflection()], ["Item2", string_type]]]);
}

function OpModule_fromGraphResult(state, result) {
    if (result.tag === 1) {
        return new ApplyResult(2, [state, result.fields[0]]);
    }
    else {
        return new ApplyResult(0, [new State(result.fields[0], state.history)]);
    }
}

function OpModule_fromGraphResultUnchanged(state, result) {
    if (result.tag === 1) {
        return new ApplyResult(2, [state, result.fields[0]]);
    }
    else {
        return new ApplyResult(0, [new State(result.fields[0], state.history)]);
    }
}

export function OpModule_apply(op, state) {
    switch (op.tag) {
        case 1:
            return OpModule_fromGraphResult(state, GraphModule_setText(op.fields[0], op.fields[1], op.fields[2], state.graph));
        case 2:
            return OpModule_fromGraphResult(state, GraphModule_replace(op.fields[0], op.fields[1], op.fields[2], op.fields[3], state.graph));
        default: {
            const nodeId = op.fields[0];
            return new ApplyResult(0, [new State(new Graph(state.graph.root, add(nodeId, new Node$(nodeId, op.fields[1], undefined, empty()), state.graph.nodes)), state.history)]);
        }
    }
}

export function OpModule_undo(op, state) {
    switch (op.tag) {
        case 1:
            return OpModule_fromGraphResult(state, GraphModule_setText(op.fields[0], op.fields[2], op.fields[1], state.graph));
        case 2:
            return OpModule_fromGraphResult(state, GraphModule_replace(op.fields[0], op.fields[1], op.fields[3], op.fields[2], state.graph));
        default:
            return new ApplyResult(0, [new State(new Graph(state.graph.root, remove(op.fields[0], state.graph.nodes)), state.history)]);
    }
}

export function ChangeModule_addOp(op, change) {
    return new Change(change.id, append(change.ops, singleton(op)));
}

export function ChangeModule_apply(change, state) {
    const result = fold((acc, op_1) => {
        if (acc.tag === 0) {
            const tupledArg = [acc.fields[0][0], acc.fields[0][1]];
            const matchValue = OpModule_apply(op_1, tupledArg[0]);
            switch (matchValue.tag) {
                case 1:
                    return new FSharpResult$2(0, [[matchValue.fields[0], tupledArg[1]]]);
                case 0:
                    return new FSharpResult$2(0, [[matchValue.fields[0], true]]);
                default:
                    return new FSharpResult$2(1, [matchValue]);
            }
        }
        else {
            return new FSharpResult$2(1, [acc.fields[0]]);
        }
    }, new FSharpResult$2(0, [[state, false]]), change.ops);
    if (result.tag === 0) {
        if (result.fields[0][1]) {
            return new ApplyResult(0, [result.fields[0][0]]);
        }
        else {
            return new ApplyResult(1, [result.fields[0][0]]);
        }
    }
    else {
        return result.fields[0];
    }
}

export function ChangeModule_undo(change, state) {
    const result = fold((acc, op_1) => {
        if (acc.tag === 0) {
            const tupledArg = [acc.fields[0][0], acc.fields[0][1]];
            const matchValue = OpModule_undo(op_1, tupledArg[0]);
            switch (matchValue.tag) {
                case 1:
                    return new FSharpResult$2(0, [[matchValue.fields[0], tupledArg[1]]]);
                case 0:
                    return new FSharpResult$2(0, [[matchValue.fields[0], true]]);
                default:
                    return new FSharpResult$2(1, [matchValue]);
            }
        }
        else {
            return new FSharpResult$2(1, [acc.fields[0]]);
        }
    }, new FSharpResult$2(0, [[state, false]]), reverse(change.ops));
    if (result.tag === 0) {
        if (result.fields[0][1]) {
            return new ApplyResult(0, [result.fields[0][0]]);
        }
        else {
            return new ApplyResult(1, [result.fields[0][0]]);
        }
    }
    else {
        return result.fields[0];
    }
}

export const HistoryModule_empty = new History(empty(), empty(), 0);

export function HistoryModule_newChange(history) {
    return new Change(history.nextId, empty());
}

export function HistoryModule_addChange(change, history) {
    return new History(cons(change, history.past), empty(), max(history.nextId, change.id + 1));
}

export function HistoryModule_applyChange(change, state) {
    const matchValue = ChangeModule_apply(change, state);
    switch (matchValue.tag) {
        case 1:
            return new ApplyResult(1, [matchValue.fields[0]]);
        case 0: {
            const s_1 = matchValue.fields[0];
            return new ApplyResult(0, [new State(s_1.graph, HistoryModule_addChange(change, s_1.history))]);
        }
        default:
            return matchValue;
    }
}

export function HistoryModule_undo(state) {
    const matchValue = state.history.past;
    if (!isEmpty(matchValue)) {
        const change = head(matchValue);
        const matchValue_1 = ChangeModule_undo(change, state);
        switch (matchValue_1.tag) {
            case 1:
                return new ApplyResult(1, [matchValue_1.fields[0]]);
            case 0: {
                const s_1 = matchValue_1.fields[0];
                return new ApplyResult(0, [new State(s_1.graph, new History(tail(matchValue), cons(change, s_1.history.future), s_1.history.nextId))]);
            }
            default:
                return matchValue_1;
        }
    }
    else {
        return new ApplyResult(1, [state]);
    }
}

export function HistoryModule_redo(state) {
    const matchValue = state.history.future;
    if (!isEmpty(matchValue)) {
        const change = head(matchValue);
        const matchValue_1 = ChangeModule_apply(change, state);
        switch (matchValue_1.tag) {
            case 1:
                return new ApplyResult(1, [matchValue_1.fields[0]]);
            case 0: {
                const s_1 = matchValue_1.fields[0];
                return new ApplyResult(0, [new State(s_1.graph, new History(cons(change, s_1.history.past), tail(matchValue), s_1.history.nextId))]);
            }
            default:
                return matchValue_1;
        }
    }
    else {
        return new ApplyResult(1, [state]);
    }
}

