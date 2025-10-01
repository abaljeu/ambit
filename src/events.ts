import * as elements from './elements.js';
import * as vf from './view.js';

elements.saveButton.onclick = async () => {
    const text = elements.editor.value;   
    const change = {
         changeId: Date.now().toString(),     
         content: text   
    };    

    const response = await fetch(window.location.href, {     
        method: "POST",     
        headers: { "Content-Type": "application/json" },     
        body: JSON.stringify(change)   
    });

    if (response.ok) {
        const result = await response.json();
        vf.setMessage(`${response.status} ${result.message}`);
    } else {
        const error = await response.json();
        alert(error.message); // Show error message
    }
}; 
elements.editor.addEventListener('input', vf.links);

elements.editor.addEventListener("keydown", vf.editorKeyDown);

elements.editor.addEventListener('click', function(event) {
    event.preventDefault();
});
