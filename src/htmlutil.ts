import { LinkedList, LinkedListNode } from '@datastructures-js/linked-list';

enum OpenClose { OPEN, CLOSE, NONE }

class HalfTag {
	constructor(public readonly oc: OpenClose, public readonly name: string) {}
}

function toHalfTag(t: string): HalfTag {
	const isClosing = t.startsWith('</');
	const isSelfClosing = t.endsWith('/>');
	const tagName = t.replace(/<\/?([^>\s/]+).*/, '$1');
	
	if (isClosing) {
		return new HalfTag(OpenClose.CLOSE, tagName);
	} else if (isSelfClosing) {
		return new HalfTag(OpenClose.NONE, tagName);
	} else {
		return new HalfTag(OpenClose.OPEN, tagName);
	}
}
export function fixTags(content: string): string {
	const missingOpeners : string[] = [];
	const missingClosers : string[] = [];
	const tags : string[] = content.match(/<[^>]+>/g)?? [];
	const typedTags : HalfTag[] = tags.map((t: string) => toHalfTag(t));
	
	for (let i = 0; i< typedTags.length; i++)
	{
		const tagI  = typedTags[i];
		if (tagI.oc === OpenClose.CLOSE) {
			missingOpeners.push(tagI.name);
		} else if (tagI.oc === OpenClose.OPEN) {
			let foundMatch = false;
			for (let j = i+1; j < typedTags.length; j++) {
				const tagJ = typedTags[j];
				if (tagJ.oc === OpenClose.CLOSE && tagJ.name === tagI.name) {
					typedTags[j] = new HalfTag(OpenClose.NONE, tagJ.name);
					typedTags[i] = new HalfTag(OpenClose.NONE, tagI.name);
					foundMatch = true;
					break;
				}
			}
			if (!foundMatch) {
				missingClosers.push(tagI.name);
			}
		}
	}
	
	// Generate opening tags for unmatched closers
	const openerTags = missingOpeners.reverse().map(name => `<${name}>`).join('');
	// Generate closing tags for unmatched openers
	const closerTags = missingClosers.reverse().map(name => `</${name}>`).join('');
	
	return openerTags + content + closerTags;
}

export function stripHtmlTags(html: string): string {
    const temp = document.createElement('div');
    temp.innerHTML = html;
    return temp.textContent ?? '';
}

const valid = "<div><p>Hello <strong>world</strong></p><span>test</span></div>";
const missingFront = "</p><span>test</span></div>";
const missingFrontFixed = "<div><p></p><span>test</span></div>";
const missingBack = "<div><p>Hello <strong>world</strong>";
const missingBackFixed = "<div><p>Hello <strong>world</strong></p></div>";


export function testFixTags() : void {
	console.log('Testing fixTags function...');
	
	// Test 1: Valid HTML (should remain unchanged)
	console.log('Test 1 - Valid HTML:');
	console.log('Input:', valid);
	console.log('Output:', fixTags(valid));
	console.log('Expected: Should be unchanged');
	console.log('Match:', fixTags(valid) === valid ? 'PASS' : 'FAIL');
	console.log('');
	
	// Test 2: Missing opening tags
	console.log('Test 2 - Missing opening tags:');
	console.log('Input:', missingFront);
	console.log('Output:', fixTags(missingFront));
	console.log('Expected:', missingFrontFixed);
	console.log('Match:', fixTags(missingFront) === missingFrontFixed ? 'PASS' : 'FAIL');
	console.log('');
	
	// Test 3: Missing closing tags
	console.log('Test 3 - Missing closing tags:');
	console.log('Input:', missingBack);
	console.log('Output:', fixTags(missingBack));
	console.log('Expected:', missingBackFixed);
	console.log('Match:', fixTags(missingBack) === missingBackFixed ? 'PASS' : 'FAIL');
	console.log('');
}