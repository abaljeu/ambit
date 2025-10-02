import * as lm from './elements.js';
import * as View from './view.js';

lm.editor.addEventListener('input', View.links);

lm.editor.addEventListener("keydown", View.editorKeyDown);

lm.editor.addEventListener('click', function(event) {
    event.preventDefault();
});
lm.saveButton.onclick= View.save;
