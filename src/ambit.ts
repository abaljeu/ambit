import './events.js';
import { RowId, endRowId } from './rowid.js';
import * as Controller from './controller.js';
import * as lm from './elements.js';
import { model } from './model.js';
import * as Test from './test.js';
import { testFixTags } from './htmlutil.js';

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

document.title = filePath;
lm.path.textContent = filePath;


export function loadFromPath(filePath : string) {
    fetchDoc(filePath).then(text => loadDoc(text));
}
export function loadDoc(data : string) {
    model.addOrUpdateDoc(data, filePath);
    model.scene.loadFromSite(model.site.getRoot());
    Controller.setEditorContent();
    Controller.setMessage("Loaded");
    Controller.links();
}
export function fetchDoc(filePath : string) : Promise<string> {
    // If not in cache, fetch from server
    let url = baseUrl + filePath;
    return fetch(url).then(result => result.text())
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
// Run HTML utility tests
// testFixTags();

// Only auto-load if we're in the main ambit context (not in tests)
if (typeof window !== 'undefined' && window.location.pathname.includes('ambit.php')) {
    loadFromPath(filePath);
    Object.assign(window as any, { model, Controller });
}