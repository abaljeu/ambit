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
// Test infrastructure
interface TestResult {
    name: string;
    passed: boolean;
    error?: string;
}

class TestRunner {
    private results: TestResult[] = [];
    
    runTest(testFn: () => void ): boolean {
        try {
            console.log(`Running test: ${testFn.name}`);
            testFn();
            this.results.push({ name: testFn.name, passed: true });
            // console.log(`✓ ${testFn.name} passed`);
			return true;
        } catch (error) {
            this.results.push({ 
                name: testFn.name, 
                passed: false, 
                error: error instanceof Error ? error.message : String(error) 
            });
            console.log(`✗ ${testFn.name} failed: ${error}`);
			return false;
        }
    }
    
    getResults(): TestResult[] {
        return this.results;
    }
    
    getSummary(): string {
        const passed = this.results.filter(r => r.passed).length;
        const total = this.results.length;
        return `${passed}/${total} tests passed`;
    }
}

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

function assert(condition: boolean, message: string = "Assert"): void {
    if (!condition) {
        throw new Error(message);
    }
}
type Equatable<T> = { equals(other: T): boolean };

function hasEquals<T>(obj: unknown): obj is Equatable<T> {
    return !!obj && typeof (obj as any).equals === 'function';
}

function assertEquals<T>(expected: T, actual: T, message: string = "Assert"): void {
    if (hasEquals<T>(expected)) {
        if (!(expected as unknown as Equatable<T>).equals(actual)) {
            throw new Error(`${message}: Expected "${expected.toString()}", got "${(actual as any).toString()}"`);
        }
    } else {
        if (expected !== actual) {
            throw new Error(`${message}: Expected "${expected?.toString?.()}", got "${(actual as any)?.toString?.()}"`);
        }
    }
}

function assertNotEquals<T>(expected: T, actual: T, message: string = "Assert"): void {
    if (hasEquals<T>(expected)) {
        if ((expected as unknown as Equatable<T>).equals(actual)) {
            throw new Error(`${message}: Expected not equal to "${expected}", got "${actual}"`);
        }
    } else {
        if (expected === actual) {
            throw new Error(`${message}: Expected not equal to "${expected}", got "${actual}"`);
        }
    }
}
// Load test file and assert model/scene/editor state

// // Test definitions array
const tests = [
    function testLoadModel() {
        loadTestDoc();
        
        const rows = Array.from(Editor.rows());
        assertEquals(4, rows.length);
        assertEquals(4, model.scene.rows.length);
        let expectedContent = "test.amb\n" + TEST_DOC_CONTENT.replace(/\r/g, '');

        const expectedLines = expectedContent.split('\n');
        for (let i = 0; i < rows.length; i++) {
            assertEquals(expectedLines[i], rows[i].content);
        }
        assertEquals(expectedContent, Editor.getContent());
    },
    
    function testHandleArrowUp() {
        // Arrange
        loadTestDoc();
        const rows = Array.from(Editor.rows());
        const secondRow = rows[2];
        secondRow.setCaretInRow(3); // Position in middle of "Line 2"
        
        // Act
        Controller.editorHandleKey(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
        
        // Assert
        assertEquals(Editor.currentRow().id, rows[1].id, "CurrentRow");
    },
    
    function testHandleArrowDown() {
        // Arrange
        loadTestDoc();
        const rows = Array.from(Editor.rows());
        const firstRow = rows[0];
        firstRow.setCaretInRow(2); // Position in middle of "Line 1"
        
        // Act
        Controller.editorHandleKey(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
        
        // Assert
        const currentRow = Editor.currentRow();
        assertEquals(currentRow.id, rows[1].id, "CurrentRow");
    },
    
// Test 2: handleEnter function
// function testHandleEnter(): void {
//     // Arrange
//     loadTestDoc();
//     const currentRow = Editor.at(1);
//     assert(currentRow.valid());
    
//     // Position cursor in middle of first line
//     currentRow.setCaretInRow(3); // "Line 1" -> position after "Lin"   
//     Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'Enter' }));
    
//     const rows = Array.from(Editor.rows());
//     assertEquals(4, rows.length);
//     assertEquals("Lin", rows[1].content);
//     assertEquals("e 1", rows[2].content);
// },

// // Test 3: handleBackspace function
// function testHandleBackspace(): void {
//     // Arrange
//     loadTestDoc();
//     const rows = Array.from(Editor.rows());
//     const secondRow = rows[1];
//     secondRow.setCaretInRow(0); // Position at start of second row
    
//     // Act
//     Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'Backspace' }));
    
//     // Assert
//     const state = getEditorState();
//     if (state.rowCount !== 2) {
//         throw new Error(`Expected 2 rows after Backspace, got ${state.rowCount}`);
//     }
    
//     const updatedRows = Array.from(Editor.rows());
//     if (updatedRows[0].content !== "Line 1Line 2") {
//         throw new Error(`Expected merged content "Line 1Line 2", got "${updatedRows[0].content}"`);
//     }
// }


function testHandleArrowLeft(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const secondRow = rows[1];
    secondRow.setCaretInRow(2); // Position in middle of "Line 2"
    
    // Act
    Controller.editorHandleKey(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    
    // Assert
    const currentRow = Editor.currentRow();
    if (currentRow.visibleTextOffset !== 1) {
        throw new Error(`Expected cursor at position 1, got ${currentRow.visibleTextOffset}`);
    }
}
,
function testHandleArrowRight(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const firstRow = rows[0];
    firstRow.setCaretInRow(2); // Position in middle of "Line 1"
    
    // Act
    Controller.editorHandleKey(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    
    // Assert
    const currentRow = Editor.currentRow();
    if (currentRow.visibleTextOffset !== 3) {
        throw new Error(`Expected cursor at position 3, got ${currentRow.visibleTextOffset}`);
    }
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
    const newLine = line2.children[0];
    assertEquals("NewLine", newLine.content);
    const newLine2 = line2.children[1];
    assertEquals("NewLine2", newLine2.content);

    const site = model.site;
    const newRow = site.testFindRowByDocLine(newLine);
    assert(newRow.valid);
    assertEquals(newLine, newRow.docLine);

    const siteRow2 = site.testFindRowByDocLine(newLine2);
    assert(siteRow2.valid);
    assertEquals(newLine2, siteRow2.docLine);

    const line2Row = newRow.parent;
    assertEquals(2, line2Row.children.length);
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
    assertEquals(5, root.children.length);
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
    assertEquals(editorRowB.content, bLine.content);
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
    secondRow.setCaretInRow(0);
    
    // Act
    sendKey('ArrowUp', ['C']);
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    assertEquals(updatedRows[1].content, "Line 2");
    assertEquals(updatedRows[2].content, "Line 1");
}

// Test 9: handleSwapDown function
,function testHandleSwapDown(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const firstRow = rows[1];
    firstRow.setCaretInRow(0);
    
    // Act
    sendKey('ArrowDown', ['C']);
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    assertEquals(updatedRows[1].content, "Line 2");
    assertEquals(updatedRows[2].content, "Line 1");

// swap down at end
    const lastRow = rows[rows.length - 1];
    const lastSceneRow = model.scene.findRow(lastRow.idString);
    const lastLine = lastSceneRow.siteRow.docLine;
    lastRow.setCaretInRow(0);
    assertEquals(lastRow.indent, 0);
    sendKey('ArrowDown', ['C']);
    assertEquals(lastRow.content, "Line 3");
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
    secondRow.setCaretInRow(0); // Position at start of line
    Controller.editorHandleKey(new KeyboardEvent('keydown', { key: 'Tab' }));
    
    // Assert
    const sceneRow1 = model.scene.findRow(firstRow.idString);
    const sceneRow2 = model.scene.findRow(secondRow.idString);
    const siteRow1 = sceneRow1.siteRow;
    const siteRow2 = sceneRow2.siteRow;
    const docLine1 = siteRow1.docLine;
    const docLine2 = siteRow2.docLine;
    const content = secondRow.content;
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
    row2.setCaretInRow(4); // Position in middle of line
    assertEquals(row2.indent, 0);
    const content = row2.content;
    assertEquals(content, "Line 2");
    // Act
    Controller.editorHandleKey(new KeyboardEvent('keydown', { key: 'Tab' }));
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    assertEquals(updatedRows[2].indent, 0);
    assertEquals(updatedRows[2].content, "Line\t 2");

}

// // Test 11: handleShiftTab function
// function testHandleShiftTab(): void {
//     // Arrange
//     loadTestDoc();
//     const rows = Array.from(Editor.rows());
//     const firstRow = rows[0];
//     firstRow.setCaretInRow(0);
    
//     // First add a tab
//     Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'Tab' }));
    
//     // Then position cursor in indent area
//     const updatedRows = Array.from(Editor.rows());
//     updatedRows[0].setCaretInRow(0);
    
//     // Act
//     Controller.editorKeyDown(new KeyboardEvent('keydown', { 
//         key: 'Tab', 
//         shiftKey: true 
//     }));
    
//     // Assert
//     const finalRows = Array.from(Editor.rows());
//     if (finalRows[0].content !== "Line 1") {
//         throw new Error(`Expected tab to be removed, but content is still "${finalRows[0].content}"`);
//     }
// }

// // Test 12: handleToggleFold function
// function testHandleToggleFold(): void {
//     // Arrange - create content with indented lines
//     const foldedContent = "Parent\n\tChild 1\n\tChild 2";
//     const doc = Model.addOrUpdateDoc("fold_test.amb", foldedContent);
//     Scene.data.loadFromDoc(doc);
//     Controller.setEditorContent(Scene.data);
    
//     const rows = Array.from(Editor.rows());
//     const parentRow = rows[0];
//     parentRow.setCaretInRow(0);
    
//     // Act
//     Controller.editorKeyDown(new KeyboardEvent('keydown', { 
//         key: '.', 
//         ctrlKey: true 
//     }));
    
//     // Assert
//     const state = getEditorState();
//     // After folding, only the parent row should be visible
//     if (state.rowCount !== 1) {
//         throw new Error(`Expected 1 row after fold, got ${state.rowCount}`);
//     }
// }
];
// // Run all tests
async function runAllTests(): Promise<void> {
    console.log("Starting Ambit regression tests...\n");
    
//     // Initialize test data first
    await initializeTestData();
    
    tests.every(test => testRunner.runTest(test));
    
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
