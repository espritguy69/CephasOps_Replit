/**
 * SI Mobile theme colors.
 * Aligned with CephasOps SI App – clear, high-contrast for field use.
 */
export const colors = {
  primary: '#2563eb',
  primaryForeground: '#ffffff',
  secondary: '#64748b',
  secondaryForeground: '#f8fafc',
  background: '#f8fafc',
  foreground: '#0f172a',
  card: '#ffffff',
  cardForeground: '#0f172a',
  muted: '#94a3b8',
  mutedForeground: '#64748b',
  border: '#e2e8f0',
  destructive: '#dc2626',
  destructiveForeground: '#ffffff',
  success: '#16a34a',
  warning: '#ca8a04',
  /** Status colors for job states */
  statusAssigned: '#94a3b8',
  statusOnTheWay: '#2563eb',
  statusMetCustomer: '#ea580c',
  statusComplete: '#16a34a',
  statusProblem: '#dc2626',
} as const;

export type Colors = typeof colors;
