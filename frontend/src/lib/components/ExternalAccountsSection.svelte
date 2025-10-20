<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import IconButton from '$lib/components/IconButton.svelte';
	import { 
		getOidcProviders, 
		getExternalLinks, 
		linkExternalAccount, 
		unlinkExternalAccount,
		type OidcProvider,
		type ExternalLink
	} from '$lib/services/authService';
	import { notify } from '$lib/services/notificationService';
	import { ColorStyle } from '$lib/types/ColorStyle';

	let links = $state<ExternalLink[]>([]);
	let availableProviders = $state<OidcProvider[]>([]);
	let isLoading = $state(true);
	let isDeleting = $state<string | null>(null);
	let isLinking = $state<string | null>(null);

	onMount(async () => {
		// Check if we just linked an account
		const linked = $page.url.searchParams.get('oidc_linked');
		if (linked === 'true') {
			notify.success('External account linked successfully!');
			// Clean up URL
			const url = new URL(window.location.href);
			url.searchParams.delete('oidc_linked');
			window.history.replaceState({}, '', url);
		}

		await loadData();
	});

	async function loadData() {
		isLoading = true;
		try {
			// Load existing links and available providers
			[links, availableProviders] = await Promise.all([
				getExternalLinks(),
				getOidcProviders()
			]);
		} catch (error) {
			console.error('Failed to load external accounts:', error);
			notify.error('Failed to load external accounts');
		} finally {
			isLoading = false;
		}
	}

	function isProviderLinked(providerId: string): boolean {
		return links.some((link) => link.providerId === providerId);
	}

	async function handleLinkProvider(providerId: string) {
		isLinking = providerId;
		try {
			const result = await linkExternalAccount(providerId);
			
			if (!result) {
				notify.error('Failed to initiate linking');
				isLinking = null;
				return;
			}

			// Redirect to the authorization URL
			window.location.href = result.authorizationUrl;
		} catch (error) {
			console.error('Failed to initiate linking:', error);
			notify.error('Failed to initiate linking');
			isLinking = null;
		}
	}

	async function handleUnlinkProvider(linkId: string, providerName: string) {
		if (!confirm(`Are you sure you want to unlink ${providerName}? You will need to log in with your password.`)) {
			return;
		}

		isDeleting = linkId;
		try {
			const success = await unlinkExternalAccount(linkId);
			
			if (success) {
				notify.success('Account unlinked successfully');
				await loadData(); // Reload the list
			} else {
				notify.error('Failed to unlink account');
			}
		} catch (error) {
			console.error('Failed to unlink account:', error);
			notify.error('Failed to unlink account');
		} finally {
			isDeleting = null;
		}
	}

	function formatDate(dateString: string): string {
		const date = new Date(dateString);
		return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
	}
</script>

<div class="bg-card mt-6 rounded-lg p-6 shadow-lg">
	<h2 class="mb-2 text-xl font-semibold">External Accounts</h2>
	<p class="color-muted mb-4">Link external authentication providers to your account.</p>

	{#if isLoading}
		<div class="flex justify-center py-8">
			<div class="bouncing-dots">
				<div class="dot"></div>
				<div class="dot"></div>
				<div class="dot"></div>
			</div>
		</div>
	{:else}
		<div class="bg-background md:w-md relative mt-2 w-full overflow-hidden rounded-lg">
			<div class="grid w-full grid-cols-1">
				{#if availableProviders.length === 0}
					<div class="p-8 text-center">
						<i class="fas fa-link color-muted mb-3 text-3xl"></i>
						<p class="color-muted">No external authentication providers are configured.</p>
					</div>
				{:else}
					{#each availableProviders as provider, i}
						{@const linked = links.find((link) => link.providerId === provider.providerId)}
						<div class="provider-item flex h-16 items-center justify-between px-4 py-2">
							<div class="flex items-center gap-3">
								<div class="flex h-8 w-8 items-center justify-center">
									{#if provider.faviconUrl}
										<img src={provider.faviconUrl} alt={provider.displayName} class="h-full w-full object-contain" />
									{:else}
										<i class="fas fa-building text-xl"></i>
									{/if}
								</div>
								<div class="flex-1">
									<div class="font-medium">{provider.displayName}</div>
									{#if linked}
										<div class="text-xs color-muted">
											{#if linked.providerEmail}
												{linked.providerEmail} •
											{/if}
											Linked {formatDate(linked.linkedAt)}
										</div>
									{:else}
										<div class="text-xs color-muted">Not linked</div>
									{/if}
								</div>
							</div>
							<div class="flex gap-1">
								{#if linked}
									<div class="w-6">
										<IconButton
											icon="fa-solid fa-unlink"
											tooltip="Unlink Account"
											hoverColor={ColorStyle.Error}
											isLoading={isDeleting === linked.id}
											onclick={() => handleUnlinkProvider(linked.id, provider.displayName)}
										/>
									</div>
								{:else}
									<div class="w-6">
										<IconButton
											icon="fa-solid fa-link"
											tooltip="Link Account"
											hoverColor={ColorStyle.Success}
											isLoading={isLinking === provider.providerId}
											onclick={() => handleLinkProvider(provider.providerId)}
										/>
									</div>
								{/if}
							</div>
						</div>
						{#if i < availableProviders.length - 1}
							<hr class="color-muted mx-4 my-1 w-5 border-t" />
						{/if}
					{/each}
				{/if}
			</div>
		</div>
	{/if}
</div>

<style>
	.provider-item:hover {
		background-color: var(--color-background-backdrop);
	}

	.bouncing-dots {
		display: flex;
		gap: 10px;
		justify-content: center;
	}

	.dot {
		width: 12px;
		height: 12px;
		background: var(--color-primary);
		border-radius: 50%;
		animation: bounce 1.4s infinite ease-in-out both;
	}

	.dot:nth-child(1) {
		animation-delay: -0.32s;
	}

	.dot:nth-child(2) {
		animation-delay: -0.16s;
	}

	@keyframes bounce {
		0%,
		80%,
		100% {
			transform: scale(0);
		}
		40% {
			transform: scale(1);
		}
	}
</style>
