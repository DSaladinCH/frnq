<script lang="ts">
	import { notify } from '$lib/services/notificationService';
	
	let customMessage = $state('');
	let customDuration = $state(5000);
	
	function showInfo() {
		notify.info('This is an informational message');
	}
	
	function showSuccess() {
		notify.success('Operation completed successfully!');
	}
	
	function showWarning() {
		notify.warning('Please be careful with this action');
	}
	
	function showError() {
		notify.error('An error occurred while processing your request');
	}
	
	function showPersistent() {
		notify.error('This notification will not auto-dismiss', 0);
	}
	
	function showLongMessage() {
		notify.info('This is a very long notification message that demonstrates how the notification component handles longer text content. It should wrap nicely and remain readable on both desktop and mobile devices.');
	}
	
	function showMultiple() {
		notify.info('First notification');
		setTimeout(() => notify.success('Second notification'), 200);
		setTimeout(() => notify.warning('Third notification'), 400);
		setTimeout(() => notify.error('Fourth notification'), 600);
	}
	
	function showCustom() {
		if (customMessage.trim()) {
			notify.info(customMessage, customDuration);
		}
	}
	
	function changePosition(pos: 'top-right' | 'top-left' | 'top-center' | 'bottom-right' | 'bottom-left' | 'bottom-center') {
		notify.setPosition(pos);
		notify.success(`Position changed to ${pos}`);
	}
	
	function setMaxNotifications(max: number) {
		notify.setMaxNotifications(max);
		notify.info(`Max notifications set to ${max}`);
	}
	
	function clearAll() {
		notify.clear();
	}
</script>

<div class="container mx-auto p-4 md:p-8 max-w-4xl">
	<h1 class="text-3xl font-bold mb-6 color-primary">Notification Service Demo</h1>
	
	<div class="space-y-8">
		<!-- Basic Notifications -->
		<section class="card">
			<h2 class="text-2xl font-semibold mb-4">Basic Notifications</h2>
			<div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-3">
				<button class="btn btn-primary" onclick={showInfo}>
					<i class="fa-solid fa-circle-info mr-2"></i>Info
				</button>
				<button class="btn btn-success" onclick={showSuccess}>
					<i class="fa-solid fa-circle-check mr-2"></i>Success
				</button>
				<button class="btn" style="--btn-bg: #f59e0b;" onclick={showWarning}>
					<i class="fa-solid fa-triangle-exclamation mr-2"></i>Warning
				</button>
				<button class="btn btn-error" onclick={showError}>
					<i class="fa-solid fa-circle-xmark mr-2"></i>Error
				</button>
			</div>
		</section>
		
		<!-- Advanced Examples -->
		<section class="card">
			<h2 class="text-2xl font-semibold mb-4">Advanced Examples</h2>
			<div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
				<button class="btn" onclick={showPersistent}>
					<i class="fa-solid fa-infinity mr-2"></i>Persistent
				</button>
				<button class="btn" onclick={showLongMessage}>
					<i class="fa-solid fa-align-left mr-2"></i>Long Message
				</button>
				<button class="btn" onclick={showMultiple}>
					<i class="fa-solid fa-layer-group mr-2"></i>Multiple
				</button>
			</div>
		</section>
		
		<!-- Custom Notification -->
		<section class="card">
			<h2 class="text-2xl font-semibold mb-4">Custom Notification</h2>
			<div class="space-y-3">
				<div>
					<label for="message" class="block mb-2">Message:</label>
					<input 
						id="message"
						type="text" 
						class="textbox w-full" 
						bind:value={customMessage}
						placeholder="Enter your custom message..."
					/>
				</div>
				<div>
					<label for="duration" class="block mb-2">Duration (ms):</label>
					<input 
						id="duration"
						type="number" 
						class="textbox w-full" 
						bind:value={customDuration}
						min="0"
						step="1000"
					/>
					<p class="text-sm color-muted mt-1">Set to 0 for persistent notification</p>
				</div>
				<button class="btn btn-primary w-full" onclick={showCustom}>
					<i class="fa-solid fa-paper-plane mr-2"></i>Show Custom Notification
				</button>
			</div>
		</section>
		
		<!-- Position Configuration -->
		<section class="card">
			<h2 class="text-2xl font-semibold mb-4">Position Configuration</h2>
			<p class="color-muted mb-4">Click to change the global notification position:</p>
			<div class="grid grid-cols-2 md:grid-cols-3 gap-3">
				<button class="btn" onclick={() => changePosition('top-right')}>Top Right</button>
				<button class="btn" onclick={() => changePosition('top-left')}>Top Left</button>
				<button class="btn" onclick={() => changePosition('top-center')}>Top Center</button>
				<button class="btn" onclick={() => changePosition('bottom-right')}>Bottom Right</button>
				<button class="btn" onclick={() => changePosition('bottom-left')}>Bottom Left</button>
				<button class="btn" onclick={() => changePosition('bottom-center')}>Bottom Center</button>
			</div>
		</section>
		
		<!-- Max Notifications -->
		<section class="card">
			<h2 class="text-2xl font-semibold mb-4">Max Notifications</h2>
			<p class="color-muted mb-4">Set the maximum number of notifications shown at once:</p>
			<div class="grid grid-cols-3 md:grid-cols-5 gap-3">
				<button class="btn" onclick={() => setMaxNotifications(1)}>1</button>
				<button class="btn" onclick={() => setMaxNotifications(3)}>3</button>
				<button class="btn" onclick={() => setMaxNotifications(5)}>5</button>
				<button class="btn" onclick={() => setMaxNotifications(7)}>7</button>
				<button class="btn" onclick={() => setMaxNotifications(10)}>10</button>
			</div>
		</section>
		
		<!-- Clear All -->
		<section class="card">
			<h2 class="text-2xl font-semibold mb-4">Clear Notifications</h2>
			<button class="btn btn-error w-full" onclick={clearAll}>
				<i class="fa-solid fa-trash mr-2"></i>Clear All Notifications
			</button>
		</section>
		
		<!-- Usage Example -->
		<section class="card">
			<h2 class="text-2xl font-semibold mb-4">Usage Example</h2>
			<pre class="bg-background p-4 rounded overflow-x-auto text-sm"><code>{`import { notify } from '$lib/services/notificationService';

// Simple usage
notify.success('Operation completed!');
notify.error('Something went wrong!');
notify.warning('Please be careful!');
notify.info('Here is some information');

// With custom duration (in milliseconds)
notify.success('This will disappear in 10 seconds', 10000);

// Persistent notification (won't auto-dismiss)
notify.error('Critical error - please address', 0);

// Configure globally
notify.setPosition('top-left');
notify.setMaxNotifications(3);

// Clear all notifications
notify.clear();`}</code></pre>
		</section>
	</div>
</div>

<style>
	.container {
		min-height: 100vh;
	}
	
	.space-y-8 > * + * {
		margin-top: 2rem;
	}
	
	.space-y-3 > * + * {
		margin-top: 0.75rem;
	}
	
	pre {
		white-space: pre-wrap;
		word-wrap: break-word;
	}
	
	code {
		color: #e5e5e5;
		font-family: 'Fira Mono', 'Consolas', monospace;
	}
</style>
