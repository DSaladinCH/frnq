<script lang="ts">
  // Best practice: Only export props you actually use in the component logic or markup.
  // If you want to pass 'type' and 'groupName' as props, use them in your logic or markup.
  export let type: 'group' | 'quote';
  export let groupName: string = '';
  export let summary: { invested: number; totalValue: number; realized: number; unrealized: number } = { invested: 0, totalValue: 0, realized: 0, unrealized: 0 };
  export let title: string = '';
  export let onView: (() => void) | null = null;
  export let isActiveQuote: boolean = false;
  export let viewLabel: string = '';
  export let profitClass: string = '';
  export let minimal: boolean = false; // new prop for minimal mode (e.g. back card)

  function handleCardClick(e: MouseEvent) {
    if (onView) {
      e.stopPropagation();
      onView();
    }
  }
</script>

<!-- Use the props in a non-visible way to avoid Svelte unused export warning -->
{#if false}{type}{groupName}{/if}

<div class="card card-reactive group-card {minimal ? 'minimal-card' : ''}"
     role={onView ? 'button' : undefined}
     {...(onView ? { tabindex: 0 } : {})}
     aria-label={viewLabel || title}
     on:click={handleCardClick}
     on:keydown={(e) => { if (onView && (e.key === 'Enter' || e.key === ' ')) { e.preventDefault(); onView(); } }}>
  <div class="group-header">
    <span class="group-title">{@html title}</span>
    {#if onView && !minimal}
      <span class="icon-btn view-btn" title={viewLabel} aria-label={viewLabel} tabindex="-1">
        <i class={isActiveQuote ? 'fa-solid fa-xmark fa-lg' : 'fa-solid fa-chart-line fa-lg'}></i>
      </span>
    {/if}
  </div>
  {#if !minimal}
    <div class="group-summary">
      <div class="invested">Invested: <span class="amount">{summary.invested.toLocaleString(undefined, { style: 'currency', currency: 'CHF' })}</span></div>
      <div class="profit-row">
        <span class="profit {profitClass}">
          {(summary.realized + summary.unrealized).toLocaleString(undefined, { style: 'currency', currency: 'CHF' })}
        </span>
        <span class="profit-percent">
          ({summary.invested ? ((summary.realized + summary.unrealized) / summary.invested * 100).toLocaleString(undefined, { maximumFractionDigits: 2 }) : '0.00'}%)
        </span>
      </div>
    </div>
  {/if}
</div>

<style>
  .minimal-card {
    /* Remove always-on hover style, just use default background */
    justify-content: center;
    align-items: center;
    min-height: 4.5rem;
  }
  .group-header {
    margin-bottom: 0.5rem;
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: space-between;
    min-height: 2.2rem;
  }
  .icon-btn {
    background: none;
    border: none;
    padding: 0.2rem;
    margin-left: 0.5rem;
    color: #b3b3b3;
    border-radius: 0.3rem;
    display: flex;
    align-items: center;
    height: 2.2rem;
    pointer-events: none;
  }
  .group-title {
    font-weight: bold;
    font-size: 1.2rem;
    color: #f3f3f3;
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
  .group-summary {
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }
  .invested {
    color: #b3b3b3;
    font-size: 1rem;
    font-weight: 500;
  }
  .amount {
    font-weight: 600;
    color: #f3f3f3;
  }
  .profit-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 1.1rem;
    font-weight: 600;
  }
  .profit {
    font-size: 1.2rem;
    font-weight: 700;
    margin-right: 0.2rem;
  }
  .profit-positive {
    color: #2ecc40;
  }
  .profit-negative {
    color: #ff4d4f;
  }
  .profit-percent {
    font-size: 1rem;
    color: #b3b3b3;
  }
  .minimal-card .group-header {
    margin-bottom: 0;
    justify-content: center;
  }
</style>
