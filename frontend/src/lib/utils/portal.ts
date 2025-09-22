/**
 * Portal utility for rendering components outside their normal DOM hierarchy
 * Useful for dropdowns, modals, tooltips that need to escape overflow constraints
 */

export function createPortal(node: HTMLElement, target?: HTMLElement | string) {
	let targetElement: HTMLElement;
	
	if (typeof target === 'string') {
		targetElement = document.querySelector(target) as HTMLElement;
	} else if (target instanceof HTMLElement) {
		targetElement = target;
	} else {
		targetElement = document.body;
	}
	
	if (!targetElement) {
		console.warn('Portal target not found, falling back to document.body');
		targetElement = document.body;
	}
	
	// Move the node to the target
	targetElement.appendChild(node);
	
	return {
		update(newTarget?: HTMLElement | string) {
			if (newTarget) {
				let newTargetElement: HTMLElement;
				
				if (typeof newTarget === 'string') {
					newTargetElement = document.querySelector(newTarget) as HTMLElement;
				} else {
					newTargetElement = newTarget;
				}
				
				if (newTargetElement && newTargetElement !== targetElement) {
					targetElement = newTargetElement;
					targetElement.appendChild(node);
				}
			}
		},
		destroy() {
			if (node.parentNode) {
				node.parentNode.removeChild(node);
			}
		}
	};
}