/**
 * macOS-inspired Design Tokens for CephasOps
 * 
 * These tokens ensure consistent spacing, typography, and visual styling
 * across the entire application, following macOS design principles.
 */

export const designTokens = {
  spacing: {
    tight: 'gap-1',      // 4px - Very tight spacing
    compact: 'gap-2',    // 8px - Compact spacing (forms, grids)
    standard: 'gap-3',   // 12px - Standard spacing
    relaxed: 'gap-4',   // 16px - Relaxed spacing (rarely used)
  },
  
  padding: {
    tight: 'p-2',        // 8px - Tight padding
    compact: 'p-3',     // 12px - Compact padding (cards, sections)
    standard: 'p-4',    // 16px - Standard padding
  },
  
  typography: {
    pageTitle: 'text-base font-semibold text-foreground',
    sectionHeader: 'text-sm font-semibold text-foreground',
    fieldLabel: 'text-xs font-medium uppercase tracking-wide text-muted-foreground',
    body: 'text-sm text-foreground',
    data: 'text-xs text-foreground',
    helper: 'text-[10px] text-muted-foreground',
    caption: 'text-[10px] text-muted-foreground/70',
  },
  
  borderRadius: {
    sm: 'rounded-sm',   // 2px
    md: 'rounded-md',   // 6px
    lg: 'rounded-lg',   // 8px (standard)
    xl: 'rounded-xl',   // 12px
  },
  
  shadows: {
    sm: 'shadow-sm',     // Subtle card shadow
    md: 'shadow-md',    // Hover state shadow
    lg: 'shadow-lg',    // Modal/dropdown shadow
  },
  
  transitions: {
    fast: 'transition-all duration-150',
    standard: 'transition-all duration-200',
    slow: 'transition-all duration-300',
  },
  
  input: {
    height: {
      compact: 'h-8',   // 32px - Compact inputs
      standard: 'h-9',  // 36px - Standard inputs
      large: 'h-10',    // 40px - Large inputs (rarely used)
    },
  },
  
  effects: {
    frosted: 'backdrop-blur-md bg-card/80',
    frostedStrong: 'backdrop-blur-xl bg-card/90',
    glass: 'backdrop-blur-sm bg-card/60',
  },
} as const;

type DesignTokenCategory = keyof typeof designTokens;

/**
 * Helper function to combine design token classes
 */
export const applyTokens = (category: DesignTokenCategory, ...keys: string[]): string => {
  const tokens = designTokens[category] as Record<string, string>;
  if (!tokens) return '';
  
  return keys
    .map(key => tokens[key])
    .filter(Boolean)
    .join(' ');
};

