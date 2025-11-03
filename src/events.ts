import * as lm from './elements.js';
import * as Controller from './controller.js';

lm.editor.addEventListener('input', Controller.editorInput);

lm.editor.addEventListener("keydown", Controller.editorHandleKey);
lm.newEditor.addEventListener("keydown", Controller.editorHandleKey);

lm.editor.addEventListener('click', function(event) {
    event.preventDefault();
});
lm.saveButton.onclick= Controller.save;
