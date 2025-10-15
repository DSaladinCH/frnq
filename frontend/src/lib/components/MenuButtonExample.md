# MenuButton Component

A reusable menu button component with portal support that automatically positions itself above or below the trigger button to stay within viewport bounds.

## Features

- ✅ Automatic positioning (above/below) based on available space
- ✅ Portal rendering to escape overflow constraints
- ✅ Works inside dialogs and normal page content
- ✅ Keyboard accessible (ESC to close)
- ✅ Click outside to close
- ✅ Uses app.css color scheme with Tailwind CSS
- ✅ Responsive and mobile-friendly
- ✅ Uses IconButton component for trigger
- ✅ MenuItem and MenuDivider components for clean markup

## Basic Usage

```svelte
<script>
  import MenuButton from '$lib/components/MenuButton.svelte';
  import MenuItem from '$lib/components/MenuItem.svelte';
  import MenuDivider from '$lib/components/MenuDivider.svelte';
  
  let menuRef;
  
  function handleEdit() {
    console.log('Edit');
    menuRef?.closeMenu(); // Close menu after action
  }
  
  function handleDuplicate() {
    console.log('Duplicate');
    menuRef?.closeMenu();
  }
  
  function handleDelete() {
    console.log('Delete');
    menuRef?.closeMenu();
  }
</script>

<MenuButton bind:this={menuRef}>
  <MenuItem 
    icon="fa-solid fa-edit"
    text="Edit"
    onclick={handleEdit}
  />
  <MenuItem 
    icon="fa-solid fa-copy"
    text="Duplicate"
    onclick={handleDuplicate}
  />
  <MenuDivider />
  <MenuItem 
    icon="fa-solid fa-trash"
    text="Delete"
    onclick={handleDelete}
    danger={true}
  />
</MenuButton>
```

## MenuButton Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `buttonClass` | `string` | `''` | Additional CSS classes for the trigger button wrapper |
| `menuClass` | `string` | `''` | Additional CSS classes for the menu container |
| `disabled` | `boolean` | `false` | Whether the menu button is disabled |

## MenuItem Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `icon` | `string` | Required | Font Awesome icon class (e.g., `"fa-solid fa-edit"`) |
| `text` | `string` | Required | Text to display for the menu item |
| `onclick` | `() => void` | Required | Function to call when the item is clicked |
| `visible` | `boolean` | `true` | Whether the menu item should be visible |
| `danger` | `boolean` | `false` | Whether to style as a destructive action (uses error color) |

## MenuDivider

Simple horizontal divider component with no props. Just import and use:

```svelte
<MenuDivider />
```

## Advanced Example with Conditional Items

```svelte
<script>
  import MenuButton from '$lib/components/MenuButton.svelte';
  import MenuItem from '$lib/components/MenuItem.svelte';
  
  let hasCustomName = $state(true);
  let menuRef;
  
  function setName() {
    hasCustomName = true;
    menuRef?.closeMenu();
  }
  
  function changeName() {
    console.log('Change name');
    menuRef?.closeMenu();
  }
  
  function removeName() {
    hasCustomName = false;
    menuRef?.closeMenu();
  }
</script>

<MenuButton bind:this={menuRef}>
  <MenuItem 
    icon="fa-solid fa-tag"
    text="Set Name"
    onclick={setName}
    visible={!hasCustomName}
  />
  <MenuItem 
    icon="fa-solid fa-pen"
    text="Change Name"
    onclick={changeName}
    visible={hasCustomName}
  />
  <MenuItem 
    icon="fa-solid fa-trash"
    text="Remove Name"
    onclick={removeName}
    visible={hasCustomName}
    danger={true}
  />
</MenuButton>
```

## Custom Styling

You can customize the appearance by passing classes:

```svelte
<MenuButton 
  buttonClass="my-custom-button-class"
  menuClass="my-custom-menu-class"
>
  <!-- menu items -->
</MenuButton>
```

## Methods

### `closeMenu()`

Programmatically close the menu. Access via component binding:

```svelte
<script>
  let menuRef;
  
  function someAction() {
    // Do something
    menuRef?.closeMenu(); // Close the menu
  }
</script>

<MenuButton bind:this={menuRef}>
  <MenuItem 
    icon="fa-solid fa-check"
    text="Action"
    onclick={someAction}
  />
</MenuButton>
```

## Positioning

The component automatically detects:
- Available viewport space
- Whether it's inside a dialog
- Whether to render above or below the trigger button

The menu will:
- Portal to `document.body` by default
- Portal to parent `<dialog>` if inside one
- Position itself to the right edge of the button
- Flip above the button if there's insufficient space below

## Accessibility

- Uses IconButton component with proper ARIA attributes
- Keyboard navigation with ESC key to close
- Semantic HTML structure
- Focus management handled by IconButton
