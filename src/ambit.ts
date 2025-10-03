import './events.js';
import * as View from './view.js';
import * as lm from './elements.js';
import * as Model from './model.js';
import * as Scene from './scene.js';

// Use local file storage via loadsave.php
const baseUrl = "loadsave.php?doc=";

const params = new URLSearchParams(window.location.search);
console.log("URL search:", window.location.search);
console.log("Params:", params.toString());
console.log("doc param:", params.get("doc"));

const filePath: string = params.get("doc") 
    ?? (() => {
        console.error("doc parameter is null, redirecting to error");
        window.location.href = "/error.html";
        throw new Error("Redirecting");
        })();

document.title = filePath;
lm.path.textContent = filePath;


function GetDoc(filePath : string) {
    // First check if document exists in global array
    const cachedDoc : Model.Doc = Model.findDoc(filePath);
    if (cachedDoc.path !== "") {
		Scene.getContent().loadFromDoc(cachedDoc);
        View.setEditorContent(Scene.getContent()!);
        View.setMessage("Loaded");
        View.links();
        return;
    }
    
    // If not in cache, fetch from server
    let url = baseUrl + filePath;
    fetch(url)
        .then(result => result.text())
        .then(text => {
            // Store in global documents array
            const doc = Model.addOrUpdateDoc(filePath, text);
            // Fill lm objects
			Scene.getContent().loadFromDoc(doc);
            View.setEditorContent(Scene.getContent());
            View.links();
        })
        .catch(err => {
            View.setMessage("Error loading file");
            console.error(err);
        });
}

export function PostDoc(filePath :string, content : string) {
    // Update global documents array first
    Model.addOrUpdateDoc(filePath, content);
    
    // Then View.save to server
    let url = baseUrl + filePath;
    fetch(url, {
        method: "POST",
        headers: { "Content-Type": "text/plain" },
        body: content
        })
    .then(r => r.ok ? r.text() : Promise.reject("Error " + r.status))
    .then(text => {
        View.setMessage("Saved - " + text);  
        console.log("Documents in array:", Model.getDocumentCount());
        })
    .catch(err => {
        View.setMessage(err);
        });    

}
GetDoc(filePath);
