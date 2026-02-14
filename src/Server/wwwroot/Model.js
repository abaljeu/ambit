
import { Record, Union } from "./fable_modules/fable-library-js.5.0.0-alpha.23/Types.js";
import { record_type, option_type, union_type, string_type } from "./fable_modules/fable-library-js.5.0.0-alpha.23/Reflection.js";
import { NodeId_$reflection, Revision_$reflection, Graph_$reflection } from "./Shared/Model.js";

export class Mode extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Selection", "Editing"];
    }
}

export function Mode_$reflection() {
    return union_type("Gambol.Client.Mode", [], Mode, () => [[], [["originalText", string_type]]]);
}

export class Model extends Record {
    constructor(graph, revision, selectedNode, mode) {
        super();
        this.graph = graph;
        this.revision = revision;
        this.selectedNode = selectedNode;
        this.mode = mode;
    }
}

export function Model_$reflection() {
    return record_type("Gambol.Client.Model", [], Model, () => [["graph", Graph_$reflection()], ["revision", Revision_$reflection()], ["selectedNode", option_type(NodeId_$reflection())], ["mode", Mode_$reflection()]]);
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["StateLoaded", "SelectRow", "StartEdit", "CommitEdit", "CancelEdit", "SubmitResponse"];
    }
}

export function Msg_$reflection() {
    return union_type("Gambol.Client.Msg", [], Msg, () => [[["Item1", Graph_$reflection()], ["Item2", Revision_$reflection()]], [["Item", NodeId_$reflection()]], [["prefill", string_type]], [["newText", string_type]], [], [["Item", Revision_$reflection()]]]);
}

