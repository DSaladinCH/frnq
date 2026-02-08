<script lang="ts">
	import Button from "$lib/components/Button.svelte";
	import DropDown from "$lib/components/DropDown.svelte";
	import IconButton from "$lib/components/IconButton.svelte";
	import Input from "$lib/components/Input.svelte";
	import PasswordInput from "$lib/components/PasswordInput.svelte";
	import PillToggle from "$lib/components/PillToggle.svelte";
	import SearchableDropDown from "$lib/components/SearchableDropDown.svelte";
	import { ColorStyle } from "$lib/types/ColorStyle";
	import { ContentWidth } from "$lib/types/ContentSize";

	let pillToggleValue = $state("on");
</script>

<div class="flex flex-col p-8 gap-8">
	<div class="grid grid-rows-[auto_1fr] gap-2">
		<h1 class="text-4xl font-bold">Input</h1>

		<div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
			<Input
				placeholder="Type something..." />

			<PasswordInput
				placeholder="Enter your password..." />

			<DropDown
				options={[
					{ label: 'Option 1', value: 'option1' },
					{ label: 'Option 2', value: 'option2' },
					{ label: 'Option 3', value: 'option3' }
				]}
				placeholder="Select an option" />

			<SearchableDropDown
				placeholder="Search and select..." />

			<div class="w-max">
				<PillToggle
					selected={pillToggleValue}
					onSelect={(value) => pillToggleValue = value}
					options={[
						{ label: 'On', value: 'on' },
						{ label: 'Off', value: 'off' }
					]} />
			</div>
		</div>
	</div>

	<div class="grid grid-rows-[auto_1fr] gap-2">
		<h1 class="text-4xl font-bold">Button</h1>

		<div class="grid grid-cols-3 2xl:grid-cols-6 gap-4">
			<Button text="Primary" style={ColorStyle.Primary} width={ContentWidth.Full} onclick={() => console.log('Primary button clicked')} />
			<Button text="Secondary" style={ColorStyle.Secondary} width={ContentWidth.Full} onclick={() => console.log('Secondary button clicked')} />
			<Button text="Accent" style={ColorStyle.Accent} width={ContentWidth.Full} onclick={() => console.log('Accent button clicked')} />
			<Button text="Success" style={ColorStyle.Success} width={ContentWidth.Full} onclick={() => console.log('Success button clicked')} />
			<Button text="Error" style={ColorStyle.Error} width={ContentWidth.Full} onclick={() => console.log('Error button clicked')} />
			<Button text="Card" style={ColorStyle.Card} width={ContentWidth.Full} onclick={() => console.log('Card button clicked')} />
			<Button text="Control" style={ColorStyle.Control} width={ContentWidth.Full} onclick={() => console.log('Control button clicked')} />
		</div>

		<div class="grid grid-cols-7 gap-4 max-w-md">
			<IconButton
				icon="fa-solid fa-thumbs-up"
				hoverColor={ColorStyle.Primary}
				tooltip="Like"
				onclick={() => console.log('Icon button clicked')} />
			<IconButton
				icon="fa-solid fa-thumbs-up"
				hoverColor={ColorStyle.Secondary}
				tooltip="Like"
				onclick={() => console.log('Icon button clicked')} />
			<IconButton
				icon="fa-solid fa-thumbs-up"
				hoverColor={ColorStyle.Accent}
				tooltip="Like"
				onclick={() => console.log('Icon button clicked')} />
			<IconButton
				icon="fa-solid fa-thumbs-up"
				hoverColor={ColorStyle.Success}
				tooltip="Like"
				onclick={() => console.log('Icon button clicked')} />
			<IconButton
				icon="fa-solid fa-thumbs-up"
				hoverColor={ColorStyle.Error}
				tooltip="Like"
				onclick={() => console.log('Icon button clicked')} />
			<IconButton
				icon="fa-solid fa-thumbs-up"
				hoverColor={ColorStyle.Card}
				tooltip="Like"
				onclick={() => console.log('Icon button clicked')} />
			<IconButton
				icon="fa-solid fa-thumbs-up"
				hoverColor={ColorStyle.Control}
				tooltip="Like"
				onclick={() => console.log('Icon button clicked')} />
		</div>
	</div>
</div>