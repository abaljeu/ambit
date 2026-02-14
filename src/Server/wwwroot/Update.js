
import { toString } from "./fable_modules/Thoth.Json.JavaScript.0.4.1/Encode.fs.js";
import { decodeGraph, decodeRevision, encodeChange, encodeRevision } from "./Shared/Serialization.js";
import { map } from "./fable_modules/fable-library-js.5.0.0-alpha.23/Seq.js";
import { fromString } from "./fable_modules/Thoth.Json.JavaScript.0.4.1/Decode.fs.js";
import { object } from "./fable_modules/Thoth.Json.Core.0.7.1/Decode.fs.js";
import { Operators_IsNull } from "./fable_modules/fable-library-js.5.0.0-alpha.23/FSharp.Core.js";
import { Msg, Model, Mode } from "./Model.js";
import { State, HistoryModule_empty, ChangeModule_apply, Change, Op } from "./Shared/History.js";
import { singleton } from "./fable_modules/fable-library-js.5.0.0-alpha.23/List.js";
import { FSharpMap__get_Item } from "./fable_modules/fable-library-js.5.0.0-alpha.23/Map.js";

/**
 * Encode the body for POST /submit
 */
export function encodeSubmitBody(change, revision) {
    let values;
    return toString(0, (values = [["clientRevision", encodeRevision(revision)], ["change", encodeChange(change)]], {
        Encode(helpers) {
            const arg = map((tupledArg) => [tupledArg[0], tupledArg[1].Encode(helpers)], values);
            return helpers.encodeObject(arg);
        },
    }));
}

/**
 * Decode the response from POST /submit (just need revision)
 */
export function decodeSubmitResponse(text) {
    return fromString(object((get$) => {
        const objectArg = get$.Required;
        return objectArg.Field("revision", decodeRevision);
    }), text);
}

/**
 * Decode the initial GET /state response
 */
export function decodeStateResponse(text) {
    return fromString(object((get$) => {
        let objectArg, objectArg_1;
        return [(objectArg = get$.Required, objectArg.Field("graph", decodeGraph)), (objectArg_1 = get$.Required, objectArg_1.Field("revision", decodeRevision))];
    }), text);
}

/**
 * Read the edit input value from the DOM (impure — pragmatic for MVP)
 */
export function readEditInputValue() {
    const el = document.getElementById("edit-input");
    if (Operators_IsNull(el)) {
        return "";
    }
    else {
        return el.value;
    }
}

/**
 * Apply a committed text edit to the model and POST to server.
 * Returns the updated model. Dispatches SubmitResponse asynchronously.
 */
export function commitTextEdit(nodeId, originalText, newText, model, dispatch) {
    if (newText === originalText) {
        return new Model(model.graph, model.revision, model.selectedNode, new Mode(0, []));
    }
    else {
        const change = new Change(0, singleton(new Op(1, [nodeId, originalText, newText])));
        const matchValue = ChangeModule_apply(change, new State(model.graph, HistoryModule_empty));
        switch (matchValue.tag) {
            case 2:
                return new Model(model.graph, model.revision, model.selectedNode, new Mode(0, []));
            case 1:
                return new Model(model.graph, model.revision, model.selectedNode, new Mode(0, []));
            default: {
                const body = encodeSubmitBody(change, model.revision);
                fetch("/submit",{method:'POST',headers:{'Content-Type':'application/json'},body:body}).then(r=>r.text()).then((responseText) => {
                    const matchValue_1 = decodeSubmitResponse(responseText);
                    if (matchValue_1.tag === 1) {
                    }
                    else {
                        dispatch(new Msg(5, [matchValue_1.fields[0]]));
                    }
                });
                return new Model(matchValue.fields[0].graph, model.revision, model.selectedNode, new Mode(0, []));
            }
        }
    }
}

/**
 * Update function. The dispatch parameter is needed for async effects
 * (server POST callbacks).
 */
export function update(msg, model, dispatch) {
    switch (msg.tag) {
        case 1: {
            const nodeId = msg.fields[0];
            const matchValue = model.mode;
            const matchValue_1 = model.selectedNode;
            let matchResult, editingId, originalText;
            if (matchValue.tag === 1) {
                if (matchValue_1 != null) {
                    matchResult = 0;
                    editingId = matchValue_1;
                    originalText = matchValue.fields[0];
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
                    const model$0027 = commitTextEdit(editingId, originalText, readEditInputValue(), model, dispatch);
                    return new Model(model$0027.graph, model$0027.revision, nodeId, model$0027.mode);
                }
                default:
                    return new Model(model.graph, model.revision, nodeId, new Mode(0, []));
            }
        }
        case 2: {
            const matchValue_3 = model.selectedNode;
            if (matchValue_3 != null) {
                return new Model(model.graph, model.revision, model.selectedNode, new Mode(1, [FSharpMap__get_Item(model.graph.nodes, matchValue_3).text]));
            }
            else {
                return model;
            }
        }
        case 3: {
            const matchValue_4 = model.mode;
            const matchValue_5 = model.selectedNode;
            let matchResult_1, nodeId_1, originalText_1;
            if (matchValue_4.tag === 1) {
                if (matchValue_5 != null) {
                    matchResult_1 = 0;
                    nodeId_1 = matchValue_5;
                    originalText_1 = matchValue_4.fields[0];
                }
                else {
                    matchResult_1 = 1;
                }
            }
            else {
                matchResult_1 = 1;
            }
            switch (matchResult_1) {
                case 0:
                    return commitTextEdit(nodeId_1, originalText_1, msg.fields[0], model, dispatch);
                default:
                    return model;
            }
        }
        case 4:
            if (model.mode.tag === 0) {
                return new Model(model.graph, model.revision, undefined, model.mode);
            }
            else {
                return new Model(model.graph, model.revision, model.selectedNode, new Mode(0, []));
            }
        case 5:
            return new Model(model.graph, msg.fields[0], model.selectedNode, model.mode);
        default:
            return new Model(msg.fields[0], msg.fields[1], undefined, new Mode(0, []));
    }
}

