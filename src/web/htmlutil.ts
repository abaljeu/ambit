
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

export function escapeHtml(text: string): string {
	return text
		.replace(/&/g, '&amp;')
		.replace(/</g, '&lt;')
		.replace(/>/g, '&gt;')
		.replace(/"/g, '&quot;')
		.replace(/'/g, '&#039;');
}

export function isInsideHtmlTag(text: string, offset: number): boolean {
	// Search backwards to find if we're inside a tag
	for (let i = offset - 1; i >= 0; i--) {
		if (text[i] === '<') {
			return true; // Found opening bracket before closing
		}
		if (text[i] === '>') {
			return false; // Found closing bracket, we're outside
		}
	}
	return false;
}

export function htmlOffsetToVisibleOffset(text: string, htmlOffset: number): number {
	let visibleOffset = 0;
	let i = 0;
	
	while (i < htmlOffset && i < text.length) {
		// Skip HTML tags
		if (text[i] === '<') {
			while (i < text.length && text[i] !== '>') {
				i++;
			}
			i++; // skip the '>'
			continue;
		}
		
		// Check for HTML entities
		if (text[i] === '&') {
			const remaining = text.substring(i);
			const entityMatch = remaining.match(/^(&lt;|&gt;|&amp;|&quot;|&#039;)/);
			if (entityMatch) {
				i += entityMatch[0].length;
				visibleOffset++;
				continue;
			}
		}
		
		// Regular character
		i++;
		visibleOffset++;
	}
	
	return visibleOffset;
}

export function visibleOffsetToHtmlOffset(text: string, visibleOffset: number): number {
	let currentVisibleOffset = 0;
	let htmlOffset = 0;
	
	while (htmlOffset < text.length && currentVisibleOffset < visibleOffset) {
		// Skip HTML tags
		if (text[htmlOffset] === '<') {
			while (htmlOffset < text.length && text[htmlOffset] !== '>') {
				htmlOffset++;
			}
			htmlOffset++; // skip the '>'
			continue;
		}
		
		// Check for HTML entities
		if (text[htmlOffset] === '&') {
			const remaining = text.substring(htmlOffset);
			const entityMatch = remaining.match(/^(&lt;|&gt;|&amp;|&quot;|&#039;)/);
			if (entityMatch) {
				htmlOffset += entityMatch[0].length;
				currentVisibleOffset++;
				continue;
			}
		}
		
		// Regular character
		htmlOffset++;
		currentVisibleOffset++;
	}
	
	return htmlOffset;
}

export function findPreviousVisibleChar(text: string, offset: number): 
	{ start: number, length: number } | null {
	let pos = offset - 1;
	
	while (pos >= 0) {
		// Skip backwards over any HTML tags
		if (text[pos] === '>') {
			// Find the opening '<' of this tag
			let tagStart = pos;
			while (tagStart > 0 && text[tagStart] !== '<') {
				tagStart--;
			}
			pos = tagStart - 1;
			continue;
		}
		
		// Check if we're at the end of an HTML entity
		if (text[pos] === ';') {
			const entityMatch = text.substring(Math.max(0, pos - 6), pos + 1)
				.match(/(&lt;|&gt;|&amp;|&quot;|&#039;)$/);
			if (entityMatch) {
				// Found an entity, return its position and length
				return {
					start: pos - entityMatch[0].length + 1,
					length: entityMatch[0].length
				};
			}
		}
		
		// Found a regular visible character
		return { start: pos, length: 1 };
	}
	
	return null;
}

export function findNextVisibleChar(text: string, offset: number): 
	{ start: number, length: number } | null {
	let pos = offset;
	
	while (pos < text.length) {
		// Skip forwards over any HTML tags
		if (text[pos] === '<') {
			// Find the closing '>' of this tag
			let tagEnd = pos;
			while (tagEnd < text.length && text[tagEnd] !== '>') {
				tagEnd++;
			}
			pos = tagEnd + 1;
			continue;
		}
		
		// Check if we're at the start of an HTML entity
		if (text[pos] === '&') {
			const remaining = text.substring(pos);
			const entityMatch = remaining.match(/^(&lt;|&gt;|&amp;|&quot;|&#039;)/);
			if (entityMatch) {
				// Found an entity, return its position and length
				return {
					start: pos,
					length: entityMatch[0].length
				};
			}
		}
		
		// Found a regular visible character
		return { start: pos, length: 1 };
	}
	
	return null;
}

export interface TagSurroundInfo {
	hasTag: boolean;
	openTagStart?: number;
	openTagEnd?: number;
	closeTagStart?: number;
	closeTagEnd?: number;
}

export interface TagOperation {
	offset: number;
	deleteLength: number;
	insertText: string;
}

export function computeTagToggleOperations(
	htmlContent: string,
	htmlStartOffset: number,
	htmlEndOffset: number,
	tagName: string
): TagOperation[] {
	const tagInfo = checkTagSurrounding(
		htmlContent, 
		htmlStartOffset, 
		htmlEndOffset, 
		tagName
	);
	
	if (tagInfo.hasTag && tagInfo.openTagStart !== undefined && 
		tagInfo.closeTagStart !== undefined) {
		// Remove tags - return operations in reverse order (close first, then open)
		return [
			{ 
				offset: tagInfo.closeTagStart, 
				deleteLength: tagInfo.closeTagEnd! - tagInfo.closeTagStart, 
				insertText: '' 
			},
			{ 
				offset: tagInfo.openTagStart, 
				deleteLength: tagInfo.openTagEnd! - tagInfo.openTagStart, 
				insertText: '' 
			}
		];
	} else if (tagInfo.openTagStart === undefined && tagInfo.closeTagStart === undefined) {
		// Add tags - return operations in reverse order (close first, then open)
		return [
			{ offset: htmlEndOffset, deleteLength: 0, insertText: `</${tagName}>` },
			{ offset: htmlStartOffset, deleteLength: 0, insertText: `<${tagName}>` }
		];
	}
	
	// Invalid selection (crosses tag boundaries or has partial tags)
	return [];
}

export function checkTagSurrounding(
	text: string, 
	startOffset: number, 
	endOffset: number, 
	tagName: string
): TagSurroundInfo {
	const openTag = `<${tagName}>`;
	const closeTag = `</${tagName}>`;
	
	// Validate that selection boundaries are not inside HTML tags
	if (isInsideHtmlTag(text, startOffset) || isInsideHtmlTag(text, endOffset)) {
		return { hasTag: false };
	}
	
	// Check if opening tag is immediately before or at start
	let hasOpenTag = false;
	let openTagStart = -1;
	let openTagEnd = -1;
	
	if (startOffset >= openTag.length) {
		const beforeStart = text.substring(startOffset - openTag.length, startOffset);
		if (beforeStart === openTag) {
			hasOpenTag = true;
			openTagStart = startOffset - openTag.length;
			openTagEnd = startOffset;
		}
	}
	
	// Check at start position if not found before
	if (!hasOpenTag && text.substring(startOffset, startOffset + openTag.length) === openTag) {
		hasOpenTag = true;
		openTagStart = startOffset;
		openTagEnd = startOffset + openTag.length;
	}
	
	// Check if closing tag is immediately after or at end
	let hasCloseTag = false;
	let closeTagStart = -1;
	let closeTagEnd = -1;
	
	if (endOffset + closeTag.length <= text.length) {
		const afterEnd = text.substring(endOffset, endOffset + closeTag.length);
		if (afterEnd === closeTag) {
			hasCloseTag = true;
			closeTagStart = endOffset;
			closeTagEnd = endOffset + closeTag.length;
		}
	}
	
	// Check before end position if not found after
	if (!hasCloseTag && endOffset >= closeTag.length) {
		const beforeEnd = text.substring(endOffset - closeTag.length, endOffset);
		if (beforeEnd === closeTag) {
			hasCloseTag = true;
			closeTagStart = endOffset - closeTag.length;
			closeTagEnd = endOffset;
		}
	}
	
	return {
		hasTag: hasOpenTag && hasCloseTag,
		openTagStart: hasOpenTag ? openTagStart : undefined,
		openTagEnd: hasOpenTag ? openTagEnd : undefined,
		closeTagStart: hasCloseTag ? closeTagStart : undefined,
		closeTagEnd: hasCloseTag ? closeTagEnd : undefined
	};
}

export function getNodeAndOffsetFromTextOffset(
	container: HTMLElement, 
	textOffset: number
): { node: Node, offset: number } | null {
	let currentOffset = 0;
	
	function walk(node: Node): { node: Node, offset: number } | null {
		if (node.nodeType === Node.TEXT_NODE) {
			const textLength = node.textContent?.length ?? 0;
			if (currentOffset + textLength >= textOffset) {
				return { node, offset: textOffset - currentOffset };
			}
			currentOffset += textLength;
		} else if (node.nodeType === Node.ELEMENT_NODE) {
			for (const child of node.childNodes) {
				const result = walk(child);
				if (result) return result;
			}
		}
		return null;
	}
	
	return walk(container);
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


export function convertTextToHtml(text: string): string {
	// Regular expression to find wikilinks
	const wikilinkRegex = /\[\[([a-zA-Z0-9 _\.-]+)\]\]/g;
	
	// Replace wikilinks with anchor tags, preserving existing HTML
	return text.replace(wikilinkRegex, (match, linkText) => {
		// Normalize spaces to underscores in the link name
		const normalizedLink = linkText.replace(/ /g, '_');
		const encodedLink = encodeURIComponent(normalizedLink);
		return `<a href="ambit.php?doc=${encodedLink}.amb">${linkText}</a>`;
	});
}
export function convertHtmlToText(html: string): string {
	// Create a temporary div to parse HTML
	const div = document.createElement('div');
	div.innerHTML = html;
	
	// Find all anchor tags (convert to array to avoid live NodeList issues)
	const links = Array.from(div.querySelectorAll('a'));
	
	// Replace each link
	links.forEach(link => {
		const href = link.getAttribute('href');
		if (href && href.startsWith('ambit.php')) {
			// Extract the doc parameter
			const urlParams = new URLSearchParams(href.split('?')[1]);
			const docParam = urlParams.get('doc');
			if (docParam) {
				// Remove .amb extension if present
				const wikilink = docParam.endsWith('.amb') 
					? docParam.slice(0, -4) 
					: docParam;
				// Decode the URL-encoded text
				const decodedLink = decodeURIComponent(wikilink);
				// Replace the anchor tag with [[wikilink]] format
				const textNode = document.createTextNode(`[[${decodedLink}]]`);
				link.parentNode?.replaceChild(textNode, link);
			}
		} else {
			// For non-ambit.php links, just replace with their text content
			const textNode = document.createTextNode(link.textContent ?? '');
			link.parentNode?.replaceChild(textNode, link);
		}
	});
	
	// Return innerHTML to preserve HTML structure (spans, no <p> tags)
	return div.innerHTML;
}

export interface TagSurroundInfo {
	hasTag: boolean;
	openTagStart?: number;
	openTagEnd?: number;
	closeTagStart?: number;
	closeTagEnd?: number;
}

export interface TagOperation {
	offset: number;
	deleteLength: number;
	insertText: string;
}

