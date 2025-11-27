import { CellTextSelection } from './cellblock.js';
import { initModel } from './model.js';
import { RowCell } from './site.js';
import * as Ops from './ops.js';
import * as Controller from './ctrl/controller.js';
import * as lm from './web/elements.js';
import * as WebEvents from './ctrl/events.js';
import * as WebUI from './web/ui.js';
import { model } from './model.js';
import * as Test from './test.js';
import { testFixTags } from './htmlutil.js';
import { Doc } from './doc.js';
// Use local file storage via loadsave.php
const baseUrl = "loadsave.php?doc=";

const params = new URLSearchParams(window.location.search);
console.log("URL search:", window.location.search);
console.log("Params:", params.toString());
console.log("doc param:", params.get("doc"));

const filePath: string = params.get("doc") 
    ?? (() => {
        console.error("doc parameter is null, redirecting to index");
        window.location.href = "/";
        throw new Error("Redirecting");
        })();

WebUI.setPath(filePath);


export async function  loadFromPath(filePath : string) {
    await fetchDoc(filePath).then(text => Controller.loadDoc(text, filePath));
}
export async function fetchDoc(filePath : string) : Promise<string> {
    // If not in cache, fetch from server
    let url = baseUrl + filePath;
    return await fetch(url).then(result => result.text())
        .catch(err => {
            Controller.setMessage("Error loading file " + filePath);
            console.error(err);
            return Promise.reject(err.message);
        });
}

export function postDoc(filePath :string, content : string) {
    // Update global documents array first
    model.addOrUpdateDoc(filePath, content);
    
    // Then Controller.save to server
    let url = baseUrl + filePath;
    fetch(url, {
        method: "POST",
        headers: { "Content-Type": "text/plain" },
        body: content
        })
    .then(r => r.ok ? r.text() : Promise.reject("Error " + r.status))
    .then(text => {
        Controller.setMessage("Saved - " + text);  
        console.log("Documents in array:", model.docArray.length);
        })
    .catch(err => {
        Controller.setMessage(err);
        });    

}

if (typeof window !== 'undefined' && window.location.pathname.includes('ambit.php')) {
    // add globals for debugging.
    initModel();
    Object.assign(window as any, { model, Controller });
    
    WebEvents.installEditorEvents({
        onKeyDown: Controller.editorHandleKey,
        onSave: Controller.save,
    });
    
    await Test.runAllTests();
    await loadFromPath(filePath);
    const row = model.scene.rows[0];
    const cell = row.cells.at(0);
    const rowcell = new RowCell(row, cell);
    Ops.setCaretInCell(rowcell, 0);

}