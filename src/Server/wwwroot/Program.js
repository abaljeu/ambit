
import { compare, createAtom } from "./fable_modules/fable-library-js.5.0.0-alpha.23/Util.js";
import { Revision_get_Zero, Graph, NodeId } from "./Shared/Model.js";
import { empty } from "./fable_modules/fable-library-js.5.0.0-alpha.23/Map.js";
import { Msg, Model, Mode } from "./Model.js";
import { decodeStateResponse, update } from "./Update.js";
import { app, render } from "./View.js";
import { Operators_IsNull } from "./fable_modules/fable-library-js.5.0.0-alpha.23/FSharp.Core.js";
import { concat } from "./fable_modules/fable-library-js.5.0.0-alpha.23/String.js";

export let currentModel = createAtom(new Model(new Graph(new NodeId("00000000-0000-0000-0000-000000000000"), empty({
    Compare: (x, y) => (compare(x, y) | 0),
})), Revision_get_Zero(), undefined, new Mode(0, [])));

export let editPrefill = createAtom(undefined);

export function dispatch(msg) {
    if (msg.tag === 2) {
        editPrefill(msg.fields[0]);
    }
    currentModel(update(msg, currentModel(), (msg_1) => {
        dispatch(msg_1);
    }));
    render(currentModel(), (msg_2) => {
        dispatch(msg_2);
    });
    const matchValue = editPrefill();
    let matchResult, prefill_1;
    if (msg.tag === 2) {
        if (matchValue != null) {
            matchResult = 0;
            prefill_1 = matchValue;
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
            const editInput = document.getElementById("edit-input");
            if (!Operators_IsNull(editInput)) {
                const inp = editInput;
                inp.value = prefill_1;
                inp.focus();
                const len = prefill_1.length | 0;
                inp.setSelectionRange(len, len);
            }
            editPrefill(undefined);
            break;
        }
        case 1: {
            break;
        }
    }
}

fetch("/state").then(r => r.text()).then((text) => {
    const matchValue = decodeStateResponse(text);
    if (matchValue.tag === 1) {
        app.textContent = concat("Error: ", ...matchValue.fields[0]);
    }
    else {
        dispatch(new Msg(0, [matchValue.fields[0][0], matchValue.fields[0][1]]));
    }
});

