import './events.js';
import { links } from './view.js';
import * as lm from './elements.js';

// Use local file storage (server rewrites to loadsave.php)
const baseUrl = "doc/";

const params = new URLSearchParams(window.location.search);
const filePath: string = params.get("doc") 
    ?? (() => {
        window.location.href = "/error.html";
        throw new Error("Redirecting");
        })();

document.title = filePath;
lm.path.textContent = filePath;


// save handler (example: POST to same path)
lm.saveButton.onclick= () => PostDoc(filePath, lm.editor.value);



function GetDoc(filePath : string) {
      let url = baseUrl + filePath;
  return fetch(url)
    .then(result => result.text())
    .then(text => lm.editor.value = text)
    .catch(err => {
        lm.messageArea.textContent = "Error loading file";
        console.error(err);
    });
}
function PostDoc(filePath :string, content : string) {
    let url = baseUrl + filePath;
    fetch(url, {
        method: "POST",
        headers: { "Content-Type": "text/plain" },
        body: content
        })
    .then(r => r.ok ? r.text() : Promise.reject("Error " + r.status))
    .then(text => {
    lm.messageArea.textContent = "Done - " + text;  // should show "Saved Doc"
        })
    .catch(err => {
        lm.messageArea.textContent = err;
        });    
}

GetDoc(filePath)
.then(links);
