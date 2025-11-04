import * as lm from './elements.js';
import * as Controller from './controller.js';

lm.newEditor.addEventListener("keydown", Controller.editorHandleKey);

lm.newEditor.addEventListener('click', function(event : MouseEvent) {
    event.preventDefault();
});
lm.saveButton.onclick= Controller.save;
