import * as lm from './elements.js';
import * as View from './view.js';

lm.saveButton.onclick = async () => {
    const text = lm.editor.value;   
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
        View.setMessage(`${response.status} ${result.message}`);
    } else {
        const error = await response.json();
        alert(error.message); // Show error message
    }
}; 
lm.editor.addEventListener('input', View.links);

lm.editor.addEventListener("keydown", View.editorKeyDown);

lm.editor.addEventListener('click', function(event) {
    event.preventDefault();
});
lm.saveButton.onclick= View.save;
