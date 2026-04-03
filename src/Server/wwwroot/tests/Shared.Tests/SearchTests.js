
import { Exception, defaultOf } from "../../fable_modules/fable-library-js.5.0.0-alpha.23/Util.js";
import { map } from "../../fable_modules/fable-library-js.5.0.0-alpha.23/List.js";
import { some } from "../../fable_modules/fable-library-js.5.0.0-alpha.23/Option.js";

function setNodeName(nodeId, name, graph) {
    let node;
    throw 1;
    return defaultOf();
}

function ownedRootChildren(ids, graph) {
    const ch = map((id) => defaultOf(), ids);
    let matchValue;
    throw 1;
    if (matchValue.tag === 1) {
        throw new Exception(matchValue.fields[0]);
    }
    else {
        return matchValue.fields[0];
    }
}

export function searchNodes$0020with$0020$query$0020matches$0020name$0020first$0020and$0020text$0020too() {
    let graph0;
    throw 1;
    let byText;
    throw 1;
    const graph2 = setNodeName((() => {
        throw 1;
    })(), "report-tag", (() => {
        throw 1;
    })()[0]);
    const resultIds = map((r) => {
        throw 1;
    }, (() => {
        throw 1;
    })());
    throw 1;
}

export function searchNodes$0020plain$0020query$0020matches$0020text$0020only() {
    let graph0;
    throw 1;
    const graph2 = setNodeName((() => {
        throw 1;
    })(), "match me", (() => {
        throw 1;
    })()[0]);
    const resultIds = map((r) => {
        throw 1;
    }, (() => {
        throw 1;
    })());
    throw 1;
}

export function searchNodes$0020ordering$0020is$0020deterministic$0020for$0020equal$002Dscore$0020matches() {
    let graph0;
    throw 1;
    let patternInput;
    throw 1;
    const first = map((r) => {
        throw 1;
    }, (() => {
        throw 1;
    })());
    const second = map((r_1) => {
        throw 1;
    }, (() => {
        throw 1;
    })());
    throw 1;
}

export function searchNodes$0020empty$0020and$0020whitespace$0020query$0020returns$0020no$0020results() {
    let graph;
    throw 1;
    throw 1;
    throw 1;
    throw 1;
}

export function tryNodeRangeInsertAfter$0020is$0020slot$0020after$0020sibling$0020under$0020root() {
    let graph0;
    throw 1;
    let patternInput;
    throw 1;
    const graph2 = ownedRootChildren(patternInput[1], patternInput[0]);
    let got;
    throw 1;
    const expect = some(defaultOf());
    throw 1;
}

export function tryNodeRangeInsertAfter$0020root$0020is$0020None() {
    let graph;
    throw 1;
    throw 1;
}

//# sourceMappingURL=SearchTests.js.map
