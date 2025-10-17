// // tests need to be reviewed.
// // fold is broken
import { Doc } from './doc.js';
import * as Change from './change.js';
import { model } from './model.js';
import { Scene } from './scene.js';
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
            console.log(`✓ ${testFn.name} passed`);
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
function hasEquals(obj: any): obj is { equals(other: any): boolean } {
    return obj && typeof obj.equals === 'function';
}

function assert(condition: boolean, message: string = "Assert"): void {
    if (!condition) {
        throw new Error(message);
    }
}
function assertEquals(expected: any, actual: any, message: string = "Assert"): void {
    if (hasEquals(expected)) {
        if (!expected.equals(actual)) {
            throw new Error(`${message}: Expected "${expected.toString()}", got "${actual.toString()}"`);
        }
    } else {
        if (expected !== actual) {
            throw new Error(`${message}: Expected "${expected.toString()}", got "${actual.toString()}"`);
        }
    }
}

function assertNotEquals(expected: any, actual: any, message: string = "Assert"): void {
    if (hasEquals(expected)) {
        if (expected.equals(actual)) {
            throw new Error(`${message}: Expected not equal to "${expected}", got "${actual}"`);
        }
    } else {
        if (expected === actual) {
            throw new Error(`${message}: Expected not equal to "${expected}", got "${actual}"`);
        }
    }
}// Load test file and assert model/scene/editor state

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
        Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
        
        // Assert
        assertEquals(Editor.CurrentRow().id, rows[1].id, "CurrentRow");
    },
    
    function testHandleArrowDown() {
        // Arrange
        loadTestDoc();
        const rows = Array.from(Editor.rows());
        const firstRow = rows[0];
        firstRow.setCaretInRow(2); // Position in middle of "Line 1"
        
        // Act
        Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
        
        // Assert
        const currentRow = Editor.CurrentRow();
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
    Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    
    // Assert
    const currentRow = Editor.CurrentRow();
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
    Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    
    // Assert
    const currentRow = Editor.CurrentRow();
    if (currentRow.visibleTextOffset !== 3) {
        throw new Error(`Expected cursor at position 3, got ${currentRow.visibleTextOffset}`);
    }
}

,function testHandleInsert(): void {
    // Arrange
    var doc = loadTestDoc();
    var root = doc.getRoot();
    assertEquals(4, doc.lines.length);
    var child = root.children[1];
    var change = Change.makeInsert(doc, root.id, child.id, ["NewLine", "NewLine2"]);
    doc.processChange(change);
    const rows = Array.from(Editor.rows());
    assertEquals(6, doc.lines.length);
    const newLine = doc.lines[2];
    assertEquals("NewLine", newLine.content);
    const newLine2 = doc.lines[3];
    assertEquals("NewLine2", newLine2.content);

    const site = model.site;
    const siteRow = site.findRowByDocLine(newLine);
    assert(siteRow.valid);
    assertEquals(newLine, siteRow.docLine);

    const siteRow2 = site.findRowByDocLine(newLine2);
    assert(siteRow2.valid);
    assertEquals(newLine2, siteRow2.docLine);

    const siteParent = siteRow.parent;
    assert(siteParent.children.length === 5);
    const scene = model.scene;
    assertEquals(6, scene.rows.length);
}

, function testMoveRow(): void {
    // Arrange
    const data = "a\nb\n\tc\n\td\n\t\te\n";
    const doc = Controller.loadDoc(data, "test.amb");
    
    // Act - move line 'a' (index 0) to after 'c' (index 2)
    const aLine = doc.lines[1];
    const bLine = doc.lines[2];
    const dLine = doc.lines[4];
    const moveChange = Change.makeMove(doc, aLine.id, 1, dLine.parent.id, dLine.id);
    doc.processChange(moveChange);
    
    // Assert
    assertEquals(bLine, doc.lines[1]);
    assertEquals(aLine, doc.lines[3]);

}
// // Test 8: handleSwapUp function
// function testHandleSwapUp(): void {
//     // Arrange
//     loadTestDoc();
//     const rows = Array.from(Editor.rows());
//     const secondRow = rows[1];
//     secondRow.setCaretInRow(0);
    
//     // Act
//     Controller.editorKeyDown(new KeyboardEvent('keydown', { 
//         key: 'ArrowUp', 
//         ctrlKey: true 
//     }));
    
//     // Assert
//     const updatedRows = Array.from(Editor.rows());
//     if (updatedRows[0].content !== "Line 2") {
//         throw new Error(`Expected first row to be "Line 2" after swap, got "${updatedRows[0].content}"`);
//     }
    
//     if (updatedRows[1].content !== "Line 1") {
//         throw new Error(`Expected second row to be "Line 1" after swap, got "${updatedRows[1].content}"`);
//     }
// }

// // Test 9: handleSwapDown function
// function testHandleSwapDown(): void {
//     // Arrange
//     loadTestDoc();
//     const rows = Array.from(Editor.rows());
//     const firstRow = rows[0];
//     firstRow.setCaretInRow(0);
    
//     // Act
//     Controller.editorKeyDown(new KeyboardEvent('keydown', { 
//         key: 'ArrowDown', 
//         ctrlKey: true 
//     }));
    
//     // Assert
//     const updatedRows = Array.from(Editor.rows());
//     if (updatedRows[0].content !== "Line 2") {
//         throw new Error(`Expected first row to be "Line 2" after swap, got "${updatedRows[0].content}"`);
//     }
    
//     if (updatedRows[1].content !== "Line 1") {
//         throw new Error(`Expected second row to be "Line 1" after swap, got "${updatedRows[1].content}"`);
//     }
// }

// // Test 10: handleTab function
// function testHandleTab(): void {
//     // Arrange
//     loadTestDoc();
//     const rows = Array.from(Editor.rows());
//     const firstRow = rows[0];
//     firstRow.setCaretInRow(0); // Position at start of line
    
//     // Act
//     Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'Tab' }));
    
//     // Assert
//     const updatedRows = Array.from(Editor.rows());
//     if (!updatedRows[0].content.startsWith('\t')) {
//         throw new Error(`Expected first row to start with tab, got "${updatedRows[0].content}"`);
//     }
// }

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
