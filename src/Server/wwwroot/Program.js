
import { FSharpMap__get_Item } from "./fable_modules/fable-library-js.5.0.0-alpha.23/Map.js";
import { disposeSafe, getEnumerator, int32ToString } from "./fable_modules/fable-library-js.5.0.0-alpha.23/Util.js";
import { fromString } from "./fable_modules/Thoth.Json.JavaScript.0.4.1/Decode.fs.js";
import { object } from "./fable_modules/Thoth.Json.Core.0.7.1/Decode.fs.js";
import { decodeGraph } from "./Shared/Serialization.js";
import { concat } from "./fable_modules/fable-library-js.5.0.0-alpha.23/String.js";

export function renderNode(container, graph, depth, nodeId) {
    const node = FSharpMap__get_Item(graph.nodes, nodeId);
    const div = document.createElement("div");
    div.setAttribute("style", ("padding-left: " + int32ToString(depth * 24)) + "px");
    div.textContent = node.text;
    container.appendChild(div);
    const enumerator = getEnumerator(node.children);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            renderNode(container, graph, depth + 1, enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]());
        }
    }
    finally {
        disposeSafe(enumerator);
    }
}

export const app = document.getElementById("app");

fetch("/state").then(r => r.text()).then((text) => {
    const matchValue = fromString(object((get$) => {
        const objectArg = get$.Required;
        return objectArg.Field("graph", decodeGraph);
    }), text);
    if (matchValue.tag === 1) {
        app.textContent = concat("Error: ", ...matchValue.fields[0]);
    }
    else {
        const graph = matchValue.fields[0];
        app.innerHTML = "";
        const enumerator = getEnumerator(FSharpMap__get_Item(graph.nodes, graph.root).children);
        try {
            while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                renderNode(app, graph, 0, enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]());
            }
        }
        finally {
            disposeSafe(enumerator);
        }
    }
});

