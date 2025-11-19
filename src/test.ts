// // tests need to be reviewed.
// // fold is broken
import { Doc, DocLine } from './doc.js';
import * as Change from './change.js';
import { model } from './model.js';
import { Site, SiteRow } from './site.js';
import { Scene, SceneRow } from './scene.js';
import * as Editor from './editor.js';
import * as Controller from './controller.js';
import * as ambit from './ambit.js';
import { CellSelection, CellBlock, CellTextSelection } from './cellblock.js';
import * as Ops from './ops.js';
import {
    TestRunner,
    assert,
    assertEquals,
    assertNotEquals
} from './test-infra.js';

// // Global test runner instance
const testRunner = new TestRunner();

// // Test data setup
const TEST_DOC_PATH = "test.amb";
let TEST_DOC_CONTENT = "";

// // Cache the test data from a single call to GetDoc
async function initializeTestData(): Promise<void> {
    TEST_DOC_CONTENT = await ambit.fetchDoc(TEST_DOC_PATH);
}

// Helper function to load test document using cached content
function loadTestDoc(): Doc {
    var doc = Controller.loadDoc(TEST_DOC_CONTENT, TEST_DOC_PATH);
    return doc;
}

function sendKey(key :string, modifiers: string[] = []): void {
    const e = new KeyboardEvent('keydown', 
        { key: key, 
            ctrlKey: modifiers.includes('C'), 
            shiftKey: modifiers.includes('S'), 
            altKey: modifiers.includes('A'), 
            metaKey: modifiers.includes('M') });
    Controller.editorHandleKey(e);
}
export function assertSceneMatchesSite(sceneRow: SceneRow, siteRow: SiteRow): void {
    assertEquals(sceneRow.siteRow, siteRow);
    assertEquals(sceneRow.treeLength, siteRow.treeLength);
    for (let i = 0; i < sceneRow.siteRow.children.length; i++) {
        assertEquals(sceneRow.treeLength, siteRow.treeLength);
    }
}
export function assertDocMatchesSite(docLine: DocLine, siteRow: SiteRow): void {
    assertEquals(docLine, siteRow.docLine);
    assertEquals(docLine.parent, siteRow.parent.docLine);
    assertEquals(docLine.children.length, siteRow.children.length);
    for (let i = 0; i < docLine.children.length; i++) {
        assertEquals(docLine.children[i], siteRow.children[i].docLine);
    }
    
}
function sceneRowFromRow(row: Editor.Row): SceneRow {
    return model.scene.findRow(row.id);
}

// Load test file and assert model/scene/editor state

// // Test definitions array
const tests: (() => void)[] = [
    function testLoadModel() : void {
        const doc =loadTestDoc();
        
        const rows = Array.from(Editor.rows());
        assertEquals(4, rows.length);
        assertEquals(4, model.scene.rows.length);
        let expectedContent = "test.amb\n" + TEST_DOC_CONTENT.replace(/\r/g, '');

        const expectedLines = expectedContent.split('\n');
        for (let i = 0; i < rows.length; i++) {
            const line = expectedLines[i];
            const leadingTabsMatch = line.match(/^\t*/);
            const leadingTabs = leadingTabsMatch ? leadingTabsMatch[0].length : 0;
            // htmlContent now doesn't include \t for indent cells at the start
            assertEquals(line.substring(leadingTabs), rows[i].htmlContent);
            assertEquals(leadingTabs, rows[i].indent);
        }    
    }
    
    , function testHandleArrowUp() : void {
        // Arrange
        loadTestDoc();
        const rows = Array.from(Editor.rows());
        const secondRow = rows[2];
        Ops.setCaretInRow(secondRow, secondRow.indent, 3); // Position in middle of "Line 2"
        const caret1 = secondRow.caretOffset;
        if (!caret1) throw new Error("Expected caret");
        assertEquals(3, caret1.offset); // depends on char widths
        
        // Act
        sendKey('ArrowUp', []);
        
        // Assert
        const currentRow = Editor.currentRow();

        assertEquals(currentRow.id, rows[1].id, "CurrentRow");
        const caret2 = currentRow.caretOffset;
        if (!caret2) throw new Error("Expected caret");
        assertEquals(3, caret2.offset); // depends on char widths
    }
    
    ,function testHandleArrowDown() : void {
        // Arrange
        loadTestDoc();
        const rows = Array.from(Editor.rows());
        const firstRow = rows[1];
        Ops.setCaretInRow(firstRow, firstRow.indent, 4); // Position in middle of "Line 1"
        const caret1 = firstRow.caretOffset;
        if (!caret1) throw new Error("Expected caret");
        assertEquals(4, caret1.offset); // depends on char widths
        // Act
        sendKey('ArrowDown', []);
        
        // Assert
        const currentRow = Editor.currentRow();
        assertEquals(currentRow.id, rows[2].id, "CurrentRow");
        const caret2 = currentRow.caretOffset;
        if (!caret2) throw new Error("Expected caret");
        assertEquals(4, caret2.offset); // depends on char widths
    }
    
// Test 2: handleEnter function
,function testHandleEnter(): void {
    // Arrange
    Controller.loadDoc("Line 1\n\tLine 2\nLine 3", TEST_DOC_PATH);
    const currentRow = Editor.at(1);
    assert(currentRow.valid());
    let rows = Array.from(Editor.rows());
    assertEquals("Line 2", rows[2].htmlContent);
    assertEquals(1, rows[2].indent);
    
    // Position cursor in middle of first line
    Ops.setCaretInRow(currentRow, currentRow.indent, 3); // "Line 1" -> position after "Lin"   
    sendKey('Enter', []);
    
    rows = Array.from(Editor.rows());
    assertEquals(5, rows.length);
    assertEquals("Lin", rows[1].htmlContent);
    assertEquals("e 1", rows[2].htmlContent);
    assertEquals("Line 2", rows[3].htmlContent);

    Ops.setCaretInRow(rows[3], rows[3].indent, 0);
    sendKey('Enter', []);
    rows = Array.from(Editor.rows());
    assertEquals(6, rows.length);
    assertEquals("", rows[3].htmlContent);
    assertEquals("Line 2", rows[4].htmlContent);

    Ops.setCaretInRow(rows[4], rows[4].indent, 3);
    sendKey('Enter', []);
    rows = Array.from(Editor.rows());
    assertEquals(7, rows.length);
    assertEquals("Lin", rows[4].htmlContent);
    assertEquals("e 2", rows[5].htmlContent);
}

// Test 3: handleBackspace function
, function testHandleBackspace(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const secondRow = rows[2];
    Ops.setCaretInRow(secondRow, secondRow.indent, 0); // Position at start of second row
    
    // Act
    sendKey('Backspace', []);
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    assertEquals(3, updatedRows.length);
    assertEquals("Line 1Line 2", updatedRows[1].htmlContent);
    assertEquals("Line 3", updatedRows[2].htmlContent);
}


, function testHandleArrowLeft(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const secondRow = rows[2];
    Ops.setCaretInRow(secondRow, secondRow.indent, 2); // Position in middle of "Line 2"
    assertEquals(2, secondRow.caretOffset.offset);
    assertEquals(secondRow, Editor.currentRow());
    assertEquals(2, secondRow.caretOffset.offset);
    // Act
    Controller.editorHandleKey(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    
    // Assert
    const currentRow = Editor.currentRow();
    assertEquals(currentRow, secondRow);
    assertEquals(1, currentRow.caretOffset.offset);
}
,
function testHandleArrowRight(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const firstRow = rows[1];
    Ops.setCaretInRow(firstRow, firstRow.indent, 2); // Position in middle of "Line 1"
    let currentRow = Editor.currentRow();
    assert(currentRow.valid());
    assertEquals(2, currentRow.caretOffset.offset);
    
    // Act
    Controller.editorHandleKey(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    
    // Assert
    currentRow = Editor.currentRow();
    assertEquals(3, currentRow.caretOffset.offset);
}

,function testHandleInsertBelow(): void {
    // Arrange
    var doc = loadTestDoc();
    var root = doc.root
    assertEquals(4, doc.length);
    var line2 = root.children[1];
    var change = Change.makeInsertBelow(line2, [Doc.createLine("NewLine"), Doc.createLine("NewLine2")]);
    Doc.processChange(change);
    const rows = Array.from(Editor.rows());
    assertEquals(6, doc.length);
    const newLine = line2.children[1];
    assertEquals("NewLine", newLine.content);
    const newLine2 = line2.children[2];
    assertEquals("NewLine2", newLine2.content);

    const site = model.site;
    const newRow = site.testFindRowByDocLine(newLine);
    assert(newRow.valid);
    assertEquals(newLine, newRow.docLine);

    const siteRow2 = site.testFindRowByDocLine(newLine2);
    assert(siteRow2.valid);
    assertEquals(newLine2, siteRow2.docLine);

    const line2Row = newRow.parent;
    assertEquals(3, line2Row.children.length);
    const scene = model.scene;
    assertEquals(6, scene.rows.length);
}

,function testHandleInsertBefore(): void {
    // Arrange
    var doc = loadTestDoc();
    var root = doc.root
    assertEquals(4, doc.length);
    var line2 = root.children[1];
    var change = Change.makeInsertBefore(line2, [Doc.createLine("NewLine"), Doc.createLine("NewLine2")]);
    Doc.processChange(change);
    const rows = Array.from(Editor.rows());
    assertEquals(6, doc.length);
    const newLine = root.children[1];
    assertEquals("NewLine", newLine.content);
    const newLine2 = root.children[2];
    assertEquals("NewLine2", newLine2.content);

    const site = model.site;
    const newRow = site.testFindRowByDocLine(newLine);
    assert(newRow.valid);
    assertEquals(newLine, newRow.docLine);

    const newRow2 = site.testFindRowByDocLine(newLine2);
    assert(newRow2.valid);
    assertEquals(newLine2, newRow2.docLine);

    assertEquals(site.getRoot(), newRow.parent);
    assertEquals(4, root.children.length);
    const scene = model.scene;
    assertEquals(6, scene.rows.length);
}
, function testMoveRow(): void {
    // Arrange
    const data = "a\nb\n\tc\n\td\n\t\te\n";
    const doc = Controller.loadDoc(data, "test.amb");
    const site = model.site;
    
    const aLine = doc.root.children[0];
    assertEquals(0, aLine.indent);
    const bLine = doc.root.children[1];
    const siteB = site.testFindRowByDocLine(bLine);
    const sceneB = model.scene.search((row: SceneRow) => row.siteRow === siteB);
    assertEquals(2, bLine.children.length);
    assertEquals(4, siteB.treeLength);

    assertEquals(2, siteB.children.length);
    let  sceneIndexB = sceneB.indexInScene();
    assertEquals(2, sceneIndexB);
    assertSceneMatchesSite(sceneB, siteB);
    const dLine = bLine.children[1];

    // Act - move line 'a' (index 1) to after 'c' (index 3)
    Controller.moveBefore(aLine, dLine);
    
    // Assert
    assertEquals(bLine, doc.root.children[0]);
    assertEquals(aLine, bLine.children[1]);
    assertEquals(dLine, bLine.children[2]);

    assertDocMatchesSite(bLine, siteB);
    const siteParent = siteB.parent;
    const siteA = siteB.children[1];
    // assertEquals(siteA, siteB.children[1])
    const sceneA = model.scene.search((row: SceneRow) => row.siteRow === siteA);
    assertEquals(1, sceneA.indent);

    // assertEqual(siteParent.children.length === 4);
    const scene = model.scene;
    assertEquals(5, siteB.treeLength);
    assertEquals(5, sceneB.treeLength);
    assertSceneMatchesSite(sceneB, siteB);

    sceneIndexB = sceneB.indexInScene();
    assertEquals(1, sceneIndexB)
    const sceneIndexA = sceneA.indexInScene();
    assertEquals(3, sceneIndexA);

    const editorRowB : Editor.Row = Editor.at(sceneIndexB);
    // htmlContent includes \t for indent cells, but bLine.content doesn't have leading tabs
    // So we need to check if they match (accounting for tabs in htmlContent)
    const expectedContent = bLine.content;
    const actualContent = editorRowB.htmlContent;
    // If bLine has no leading tabs, htmlContent should match
    // If bLine has leading tabs, htmlContent should include them
    if (expectedContent.startsWith('\t')) {
        assertEquals(actualContent, expectedContent);
    } else {
        // Remove leading tabs from htmlContent for comparison
        const actualWithoutTabs = actualContent.replace(/^\t+/, '');
        assertEquals(actualWithoutTabs, expectedContent);
    }
    // const sceneRow = scene.search((row: SceneRow) => row.siteRow === siteRow);
    // assert(sceneRow.valid);
    // assertEquals(bLine, sceneRow.docLine);
    // const sceneParent = sceneRow.parent;
}
// Test 8: handleSwapUp function
,function testHandleSwapUp(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const secondRow = rows[2];
    Ops.setCaretInRow(secondRow, secondRow.indent, 0);
    
    // Act
    sendKey('ArrowUp', ['C']);
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    assertEquals(updatedRows[1].htmlContent, "Line 2"); // No indent
    assertEquals(updatedRows[3].htmlContent, "Line 1"); // No indent
}

// Test 9: handleSwapDown function
,function testHandleSwapDown(): void {
    // Arrange
    loadTestDoc();
    let rows = Array.from(Editor.rows());
    const firstRow = rows[1];
    Ops.setCaretInRow(firstRow, firstRow.indent, 0);
    
    // Act
    sendKey('ArrowDown', ['C']);
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    assertEquals(updatedRows[1].htmlContent, "Line 2"); // No indent
    assertEquals(updatedRows[3].htmlContent, "Line 1"); // No indent

// swap down at end
    rows = Array.from(Editor.rows());
    const lastRow = rows[rows.length - 1];
    const lastSceneRow = model.scene.findRow(lastRow.id);
    const lastLine = lastSceneRow.siteRow.docLine;
    Ops.setCaretInRow(lastRow, lastRow.indent, 0);
    assertEquals(lastRow.indent, 0);
    sendKey('ArrowDown', ['C']);
    assertEquals(lastRow.htmlContent, "Line 1");
    rows = Array.from(Editor.rows());
    assertEquals(rows[2].htmlContent, "Line 3");
    assertEquals(lastRow.indent, 0);
}

// Test 10: handleTab function
, function testHandleTabIndent(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const firstRow = rows[1];
    const secondRow = rows[2];
    assertEquals(firstRow.indent, 0);
    assertEquals(secondRow.indent, 0);
    
    // Act
    Ops.setCaretInRow(secondRow, secondRow.indent, 0); // Position at start of line
    sendKey('Tab', []);
    
    // Assert
    const sceneRow1 = model.scene.findRow(firstRow.id);
    const sceneRow2 = model.scene.findRow(secondRow.id);
    const siteRow1 = sceneRow1.siteRow;
    const siteRow2 = sceneRow2.siteRow;
    const docLine1 = siteRow1.docLine;
    const docLine2 = siteRow2.docLine;
    const content = secondRow.htmlContent;
    assertEquals(docLine1, docLine2.parent);
    assertEquals(siteRow1, siteRow2.parent);
    const updatedRows = Array.from(Editor.rows());
    assertEquals(updatedRows[2].indent, 1);
    assertEquals(updatedRows[1].indent, 0);
}
, function testHandleTabMidLine(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const row2 = rows[2];
    Ops.setCaretInRow(row2, row2.indent, 4); // Position in middle of line
    assertEquals(row2.indent, 0);
    const content = row2.htmlContent;
    assertEquals(content, "Line 2");
    // Act
    sendKey('Tab', []);
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    assertEquals(updatedRows[2].indent, 0);
    // Internal \t means an extra editable cell, so htmlContent includes it
    assertEquals(updatedRows[2].htmlContent, "Line\t 2");

}

// Test 11: handleShiftTab function
,function testHandleShiftTab(): void {
    loadTestDoc();
    const rows = Array.from(Editor.rows());

    // Indent Row 1 refuses.
    const firstRow = rows[0];
    Ops.setCaretInRow(firstRow, firstRow.indent, 0);
    const firstIndent = firstRow.indent;
    sendKey('Tab', []);
    assertEquals(firstIndent, firstRow.indent);

    // Indent Row 2 refuses; it's already indented.
    const secondRow = rows[1];
    const secondIndent = secondRow.indent;
    assertEquals(secondIndent, 0);
    Ops.setCaretInRow(secondRow, secondRow.indent, 0);
    sendKey('Tab', []);
    assertEquals(secondIndent , secondRow.indent);

    // 3 succeeds.
    const thirdRow = rows[2];
    const thirdIndent = thirdRow.indent;
    assertEquals(thirdIndent, 0);
    let fourthRow = rows[3];
    assertEquals(fourthRow.indent, 1);
    Ops.setCaretInRow(thirdRow, thirdRow.indent, 0);
    sendKey('Tab', []);
    assertEquals(thirdIndent+1 , Editor.currentRow().indent);


    // Then position cursor in indent area
    let updatedRows = Array.from(Editor.rows());
    Ops.setCaretInRow(updatedRows[2], updatedRows[2].indent, 0);
    
    // 4 succeeds.
    fourthRow = updatedRows[3];
    const fourthIndent = fourthRow.indent;
    assertEquals(fourthIndent, 2);

    Ops.setCaretInRow(fourthRow, fourthRow.indent, 0);
    sendKey('Tab', []);
    updatedRows = Array.from(Editor.rows());
    const newFourthRow = updatedRows[3];
    assertEquals(fourthIndent , newFourthRow.indent);
    const expectedContent = "Line 3";
    assertEquals(newFourthRow.htmlContent, expectedContent);
    
    // tab again does nothing.
    sendKey('Tab', []);
    assertEquals(newFourthRow, Editor.findRow(newFourthRow.id));

    // unindent
    const newThirdRow = Editor.findRow(thirdRow.id);
    Ops.setCaretInRow(newThirdRow, newThirdRow.indent, 0);
    sendKey('Tab', ['S']);
    const newRows = Array.from(Editor.rows());
    assertEquals(newRows[2].htmlContent, "Line 2"); // No indent after unindent
    // After unindent, row 3 should have no indent
    const row3Indent = newRows[3].indent;
    const expectedRow3Content = "Line 3";
    assertEquals(newRows[3].htmlContent, expectedRow3Content);
}

// Test 12: handleToggleFold function
, function testHandleToggleFold(): void {
    // Arrange - create content with indented lines
    const foldedContent = "Parent\n\tChild 1\n\tChild 2";
    const doc = Controller.loadDoc(foldedContent, "fold_test.amb");
    
    const rows = Array.from(Editor.rows());
    const parentRow = rows[1];
    Ops.setCaretInRow(parentRow, parentRow.indent, 0);
    
    // Act
    sendKey('.', ['C']);
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    assertEquals(updatedRows.length, 2);
    assertEquals(updatedRows[1].htmlContent, "Parent"); // No indent
}
, function testInitCellSelectionToRow(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const firstRow : Editor.Row =  rows[1];
    Ops.setCaretInRow(firstRow, firstRow.indent, 0);
    Ops.selectRow(firstRow);
6
    const cellSelection = model.site.cellSelection;
    assert(cellSelection instanceof CellBlock);
    const activeCell : Editor.Cell | null = firstRow.activeCell;
    assert(activeCell !== null);
    const sceneRow = model.scene.findRow(firstRow.id);
    const sceneCell = sceneRow.cells.cells.find(cell => cell.column === 0);
    assert(sceneCell != null);
    assertEquals(sceneCell!.column, 0);
    // Find the cell index in the cells array
    const cellIndex = sceneRow.cells.cells.indexOf(sceneCell!);
    assert(cellIndex !== -1);
    // Check if this cell is selected using SceneRow's method
    assert(sceneRow.isCellSelected(cellIndex));
    // Check that the corresponding Editor.Cell has the selected CSS class
    const editorCells = firstRow.cells;
    const editorCell = editorCells[cellIndex];
    assert(editorCell !== undefined);
    assert(editorCell.hasCellBlockSelected());
    assert(editorCell.hasCellBlockActive());

    // firstRow is the active cell
}
, function testExtendSelection(): void {
    // Arrange
    /* make data like
        a
        b
            c
        d
            e
            f
        g
        */

    const data = "a\nb\n\tc\nd\n\te\n\t\tf\ng\n";
    const doc = Controller.loadDoc(data, "test.amb");

    const rows : Editor.Row[] = Array.from(Editor.rows());
    const firstRow = rows[1];
    Ops.setCaretInRow(firstRow, firstRow.indent, 0);
    Ops.selectRow(firstRow);
    
    // Act & Assert: Shift-ArrowDown from row 1 (a)
    let cellSelection = model.site.cellSelection;
    assert(cellSelection instanceof CellBlock);
    let cellBlock : CellBlock = cellSelection as CellBlock;
    assertEquals(rows[0].id, cellBlock.parentSiteRow.id.toString());
    assertEquals(0, cellBlock.startChildIndex);
    assertEquals(0, cellBlock.endChildIndex);


    sendKey('ArrowDown', ['S']);
    // selection should be rows 2 and 3.  active should be 2.
    cellSelection = model.site.cellSelection;
    assert(cellSelection instanceof CellBlock);
    cellBlock = cellSelection as CellBlock;
    assertEquals(0, cellBlock.startChildIndex);
    assertEquals(1, cellBlock.endChildIndex);
    assertEquals(sceneRowFromRow(rows[2]).siteRow, cellBlock.activeSiteRow);

    // assertEquals(cellSelection.activeSiteRow, sceneRowFromRow(rows[1]).siteRow);
    assertEquals(cellBlock.activeSiteRow, sceneRowFromRow(rows[2]).siteRow);
    const row2Cells = rows[2].cells;
    const row3Cells = rows[3].cells;
    assert(row2Cells[0].hasCellBlockSelected());
    assert(row3Cells[0].hasCellBlockSelected());
    assert(row2Cells[0].hasCellBlockActive());

    assertEquals(rows[2], Editor.currentRow());

    // Act & Assert: ArrowRight clears selection
    sendKey('ArrowRight', []);
    // selection should be empty.  cursor should be active in row 2
    cellSelection = model.site.cellSelection;
    assert(cellSelection instanceof CellTextSelection);
    assertEquals(rows[2], Editor.currentRow());
    // Act & Assert: Shift-ArrowDown from row 2 (b)
    sendKey('ArrowDown', ['S']);
    // selection should be rows 2 and 3.  active should be 2
    cellSelection = model.site.cellSelection;
    assert(cellSelection instanceof CellBlock);
    assert(row2Cells[0].hasCellBlockSelected());
    assert(row3Cells[0].hasCellBlockSelected());
    assert(row2Cells[0].hasCellBlockActive());
    
    // Act & Assert: Shift-ArrowDown extends to row 4 (d)
    sendKey('ArrowDown', ['S']);
    // selection should be rows 2-6.  active should be 4.
    cellSelection = model.site.cellSelection;
    assert(cellSelection instanceof CellBlock);
    assert(row2Cells[0].hasCellBlockSelected());
    assert(row3Cells[0].hasCellBlockSelected());
    const row4Cells = rows[4].cells;
    const row5Cells = rows[5].cells;
    const row6Cells = rows[6].cells;
    assert(row4Cells[0].hasCellBlockSelected());
    assert(row5Cells[0].hasCellBlockSelected());
    assert(row6Cells[0].hasCellBlockSelected());
    assert(row4Cells[0].hasCellBlockActive());

    // Act & Assert: Shift-ArrowUp shrinks back
    sendKey('ArrowUp', ['S']);
    // selection should be back to only 2-3, with 2 active.
    cellSelection = model.site.cellSelection;
    assert(cellSelection instanceof CellBlock);
    assert(row2Cells[0].hasCellBlockSelected());
    assert(row3Cells[0].hasCellBlockSelected());
    assert(row2Cells[0].hasCellBlockActive());
    
    // Act & Assert: Shift-ArrowUp extends to row 1 (a)
    sendKey('ArrowUp', ['S']);
    // selections should be rows 1-3, with 1 active.
    cellSelection = model.site.cellSelection;
    assert(cellSelection instanceof CellBlock);
    const row1Cells = rows[1].cells;
    assert(row1Cells[0].hasCellBlockSelected());
    assert(row2Cells[0].hasCellBlockSelected());
    assert(row3Cells[0].hasCellBlockSelected());
    assert(row1Cells[0].hasCellBlockActive());

    Ops.selectRow(rows[4]);
    cellSelection = model.site.cellSelection;
    cellBlock = cellSelection as CellBlock;
    assert(cellBlock !== null);
    assertEquals(2, cellBlock.startChildIndex);
    assertEquals(2, cellBlock.endChildIndex);
    assert(row4Cells[0].hasCellBlockSelected());
    assert(row5Cells[0].hasCellBlockSelected());
    assert(row6Cells[0].hasCellBlockSelected());
    assert(row4Cells[0].hasCellBlockActive());

    sendKey('ArrowDown', ['S']);
    cellSelection = model.site.cellSelection;
    cellBlock = cellSelection as CellBlock;
    assert(cellBlock !== null);
    assertEquals(2, cellBlock.startChildIndex);
    assertEquals(3, cellBlock.endChildIndex);
    sendKey('ArrowDown', ['S']);
    cellSelection = model.site.cellSelection;
    cellBlock = cellSelection as CellBlock;
    assert(cellBlock !== null);
    assertEquals(2, cellBlock.startChildIndex);
    assertEquals(3, cellBlock.endChildIndex);

    sendKey('ArrowUp', ['S']);
    cellSelection = model.site.cellSelection;
    cellBlock = cellSelection as CellBlock;
    assert(cellBlock !== null);
    assertEquals(2, cellBlock.startChildIndex);
    assertEquals(2, cellBlock.endChildIndex);
    assertEquals(2, cellBlock.activeCellIndex);


    sendKey('ArrowUp', ['S']);
    cellSelection = model.site.cellSelection;
    cellBlock = cellSelection as CellBlock;
    assert(cellBlock !== null);
    assertEquals(1, cellBlock.startChildIndex);
    assertEquals(2, cellBlock.endChildIndex);
    assertEquals(1, cellBlock.activeCellIndex);

    sendKey('ArrowUp', ['S']);
    cellSelection = model.site.cellSelection;
    cellBlock = cellSelection as CellBlock;
    assert(cellBlock !== null);
    assertEquals(0, cellBlock.startChildIndex);
    assertEquals(2, cellBlock.endChildIndex);
    assertEquals(0, cellBlock.activeCellIndex);
    

    sendKey('ArrowDown', ['S']);
    cellSelection = model.site.cellSelection;
    cellBlock = cellSelection as CellBlock;
    assert(cellBlock !== null);
    assertEquals(1, cellBlock.startChildIndex);
    assertEquals(2, cellBlock.endChildIndex);
    assertEquals(1, cellBlock.activeCellIndex);
    

    sendKey('ArrowDown', ['S']);
    cellSelection = model.site.cellSelection;
    cellBlock = cellSelection as CellBlock;
    assert(cellBlock !== null);
    assertEquals(2, cellBlock.startChildIndex);
    assertEquals(2, cellBlock.endChildIndex);
    assertEquals(2, cellBlock.activeCellIndex);
    
}
]
// // Run all tests
async function runAllTests(): Promise<void> {
    console.log("Starting Ambit regression tests...\n");
    
//     // Initialize test data first
    await initializeTestData();
    
    tests.forEach(test => testRunner.runTest(test));
    
     console.log(`\nTest Summary: ${testRunner.getSummary()}`);
    
//     const failedTests = testRunner.getResults().filter(r => !r.passed);
//     if (failedTests.length > 0) {
//         console.log("\nFailed tests:");
//         failedTests.forEach(test => {
//             console.log(`- ${test.name}: ${test.error}`);
//         });
//     }
}
export { runAllTests, testRunner }
// copy this into debug console to run the tests
// const { runAllTests } = await import('/dist/test.js'); await runAllTests();
