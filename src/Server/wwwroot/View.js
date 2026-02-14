
import { equals, disposeSafe, getEnumerator } from "./fable_modules/fable-library-js.5.0.0-alpha.23/Util.js";
import { FSharpMap__get_Item } from "./fable_modules/fable-library-js.5.0.0-alpha.23/Map.js";
import { Msg } from "./Model.js";
import { Operators_IsNull } from "./fable_modules/fable-library-js.5.0.0-alpha.23/FSharp.Core.js";

export const app = document.getElementById("app");

/**
 * Check if a key string represents a single printable character
 */
export function isPrintableKey(key) {
    if (key.length === 1) {
        return key >= " ";
    }
    else {
        return false;
    }
}

/**
 * Render the full outline from the model.
 * Wires event handlers that call dispatch.
 */
export function render(model, dispatch) {
    app.innerHTML = "";
    const enumerator = getEnumerator(FSharpMap__get_Item(model.graph.nodes, model.graph.root).children);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            renderNode(model, dispatch, 0, enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]());
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    const hiddenInput = document.createElement("input");
    hiddenInput.id = "hidden-input";
    hiddenInput.setAttribute("autocomplete", "off");
    hiddenInput.addEventListener("keydown", (ev) => {
        const ke = ev;
        const matchValue = model.selectedNode;
        if (matchValue != null) {
            const node = FSharpMap__get_Item(model.graph.nodes, matchValue);
            switch (ke.key) {
                case "F2": {
                    ke.preventDefault();
                    dispatch(new Msg(2, [node.text]));
                    break;
                }
                case "Escape": {
                    ke.preventDefault();
                    dispatch(new Msg(4, []));
                    break;
                }
                default:
                    if (((isPrintableKey(ke.key) && !ke.ctrlKey) && !ke.metaKey) && !ke.altKey) {
                        ke.preventDefault();
                        dispatch(new Msg(2, [ke.key]));
                    }
            }
        }
    });
    app.appendChild(hiddenInput);
    if (model.mode.tag === 0) {
        hiddenInput.focus();
    }
    else {
        const editInput = document.getElementById("edit-input");
        if (!Operators_IsNull(editInput)) {
            editInput.focus();
            const inp = editInput;
            const len = inp.value.length | 0;
            inp.setSelectionRange(len, len);
        }
    }
}

export function renderNode(model, dispatch, depth, nodeId) {
    const node = FSharpMap__get_Item(model.graph.nodes, nodeId);
    const row = document.createElement("div");
    row.classList.add("row");
    const isSelected = equals(model.selectedNode, nodeId);
    if (isSelected) {
        row.classList.add("selected");
    }
    for (let forLoopVar = 1; forLoopVar <= depth; forLoopVar++) {
        const indent = document.createElement("div");
        indent.classList.add("indent");
        row.appendChild(indent);
    }
    if (isSelected && (model.mode.tag === 1)) {
        const editInput = document.createElement("input");
        editInput.id = "edit-input";
        editInput.classList.add("edit-input");
        let prefill;
        const matchValue_1 = model.mode;
        prefill = ((matchValue_1.tag === 1) ? matchValue_1.fields[0] : node.text);
        editInput.value = prefill;
        editInput.addEventListener("keydown", (ev) => {
            const ke = ev;
            switch (ke.key) {
                case "Enter": {
                    ke.preventDefault();
                    dispatch(new Msg(3, [editInput.value]));
                    break;
                }
                case "Escape": {
                    ke.preventDefault();
                    dispatch(new Msg(4, []));
                    break;
                }
                default:
                    undefined;
            }
        });
        editInput.addEventListener("mousedown", (ev_1) => {
            ev_1.stopPropagation();
        });
        row.appendChild(editInput);
    }
    else {
        const textDiv = document.createElement("div");
        textDiv.classList.add("text");
        textDiv.textContent = node.text;
        row.appendChild(textDiv);
    }
    row.addEventListener("mousedown", (ev_2) => {
        ev_2.preventDefault();
        dispatch(new Msg(1, [nodeId]));
    });
    app.appendChild(row);
    const enumerator = getEnumerator(node.children);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            renderNode(model, dispatch, depth + 1, enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]());
        }
    }
    finally {
        disposeSafe(enumerator);
    }
}

