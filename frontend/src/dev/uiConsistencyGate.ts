/**
 * UI Consistency Gate Checklist
 *
 * Reference for developers and PR reviewers to prevent UI inconsistency regressions.
 * Aligns with: docs/UI_CONSISTENCY_REPORT.md, docs/P0_UI_CONSISTENCY_PATCH_SUMMARY.md
 *
 * Usage:
 * - Import UI_CONSISTENCY_GATE or UI_CONSISTENCY_GATE_CHECKLIST in PR descriptions / CI.
 * - Reviewers: run through CHECKLIST.rules and apply "How to check" for changed pages.
 */

export type RuleCategory =
  | 'layout'
  | 'status-badges'
  | 'empty-loading'
  | 'libraries'
  | 'syncfusion';

export interface UIConsistencyRule {
  id: string;
  category: RuleCategory;
  title: string;
  description: string;
  /** What to grep or look for in the PR (file patterns / anti-patterns). */
  howToCheck: string;
  /** Allowed exceptions (e.g. specific pages that use Syncfusion Grid). */
  allowedExceptions?: string[];
}

export interface UIConsistencyGate {
  version: string;
  lastUpdated: string;
  rules: UIConsistencyRule[];
}

const RULES: UIConsistencyRule[] = [
  {
    id: 'layout-page-shell',
    category: 'layout',
    title: 'Use PageShell or PageHeader for content pages',
    description:
      'Content pages (lists, detail views, dashboards) must use PageShell (Admin) or PageHeader (SI) for consistent title, optional breadcrumbs, and actions. Do not add new pages with only a raw <h1> and ad-hoc padding.',
    howToCheck:
      'For any new or modified route/page: ensure the root content wrapper is PageShell (frontend) or PageHeader (frontend-si). Search for "PageShell" or "PageHeader" in the page file; if neither is used and the file is a page component, flag it.',
    allowedExceptions: [
      'LoginPage',
      'auth/LoginPage',
      'Redirect or minimal placeholder pages',
    ],
  },
  {
    id: 'layout-content-padding',
    category: 'layout',
    title: 'Use consistent content padding',
    description:
      'Page content area should use the padding provided by PageShell (p-3 md:p-4 lg:p-6) or, when not using PageShell, the same scale (e.g. p-4 md:p-6). Avoid one-off values like p-5 or py-8 without design-tokens alignment.',
    howToCheck:
      'In changed page TSX, look for root content divs: prefer PageShell children (which get padding from PageShell) or classes like p-4, md:p-6, or designTokens.padding.',
  },
  {
    id: 'status-badges-no-inline',
    category: 'status-badges',
    title: 'Use StatusBadge for statuses (no inline pill classes)',
    description:
      'Any status, priority, or type pill must use the StatusBadge component (or OrderStatusBadge / shared status helpers). Do not add new inline classes such as "bg-green-100 text-green-800" or "rounded-full px-2 py-1" for status display.',
    howToCheck:
      'Grep for "bg-*-100 text-*-800", "rounded-full.*px-2 py-1", or similar ad-hoc status styling in new or modified TSX. Replace with <StatusBadge> or equivalent and status color helpers (getStatusBadgeColor, getPriorityBadgeVariant, getBuildingTypeBadgeColor, getOrderStatusVariant).',
  },
  {
    id: 'empty-loading-patterns',
    category: 'empty-loading',
    title: 'Use EmptyState and LoadingSpinner patterns',
    description:
      'Empty data and loading states must use the shared EmptyState and LoadingSpinner components. Do not introduce new "No data" divs or custom spinner markup.',
    howToCheck:
      'For list/detail pages: ensure empty state uses <EmptyState title=... description=... action=... /> and loading uses <LoadingSpinner message=... fullPage=... /> or Skeleton where layout is known. Grep for "No data", "No items", "Loading..." in raw JSX; prefer component usage.',
  },
  {
    id: 'no-new-ui-libraries',
    category: 'libraries',
    title: 'Do not introduce new UI libraries',
    description:
      'Do not add MUI, Ant Design, Chakra, Bootstrap, or other full UI frameworks. Use existing primitives: Tailwind + shadcn-style components (Button, Card, Modal, Select, etc.), Syncfusion only where already allowed.',
    howToCheck:
      'In package.json (or lockfile) diff: ensure no new dependencies like @mui/*, antd, @chakra-ui/*, bootstrap. If the PR adds a UI library, request removal and use of existing components.',
  },
  {
    id: 'syncfusion-allowed-usage',
    category: 'syncfusion',
    title: 'Syncfusion: allowed only for specific heavy grids (scheduler is now custom)',
    description:
      'Syncfusion components are allowed only for: Complex grid/editor pages that already use Syncfusion Grid/TreeGrid/Kanban (e.g. Business Hours, Integrations, GuardConditions, AutomationRules, SlaConfiguration, Buildings TreeGrid, Tasks Kanban). The installer timeline scheduler uses a custom Fresha-style UI (no ScheduleComponent). Do not add new Syncfusion widgets elsewhere; use DataTable or StandardListTable for new list views.',
    howToCheck:
      'Grep for "@syncfusion/" in new or modified files. If new Syncfusion imports appear, verify they are for an existing heavy-grid page. New list/CRUD pages should use DataTable or StandardListTable, not GridComponent.',
    allowedExceptions: [
      'TasksKanbanPage (KanbanComponent)',
      'BuildingsTreeGridPage (TreeGridComponent)',
      'BusinessHoursPage, IntegrationsPage, SmsWhatsAppSettingsPage (GridComponent)',
      'GuardConditionDefinitionsPage, SideEffectDefinitionsPage, ApprovalWorkflowsPage',
      'AutomationRulesPage, EscalationRulesPage, SlaConfigurationPage (GridComponent)',
      'ParserSnapshotViewerPage (PdfViewerComponent)',
      'WarehouseLayoutPage (DiagramComponent)',
    ],
  },
  {
    id: 'syncfusion-wrap-chrome',
    category: 'syncfusion',
    title: 'Syncfusion pages: wrap with consistent page chrome',
    description:
      'Pages that use Syncfusion (Scheduler, Grid, etc.) must still use PageShell for title and actions. Do not remove PageShell/PageHeader from these pages; standardize only the surrounding UI, not the Syncfusion widget itself.',
    howToCheck:
      'Any file that imports from @syncfusion should still render inside PageShell (or PageHeader in SI) for title/actions. Ensure no Syncfusion-only page drops the shell.',
  },
];

export const UI_CONSISTENCY_GATE: UIConsistencyGate = {
  version: '1.0.0',
  lastUpdated: '2025-02-03',
  rules: RULES,
};

/** Flat list of rules for iteration (e.g. CI or checklist rendering). */
export const UI_CONSISTENCY_GATE_CHECKLIST: UIConsistencyRule[] = RULES;

/** Rule IDs for programmatic reference. */
export const UI_CONSISTENCY_RULE_IDS = RULES.map((r) => r.id) as readonly string[];

/**
 * Short summary for PR reviewers: how to apply the UI Consistency Gate.
 * Copy into PR description template or review checklist as needed.
 */
export const PR_REVIEWER_UI_GATE_SUMMARY = `
## UI Consistency Gate — PR review checklist

1. **Layout** — Any new or modified **content page** (list, detail, dashboard) must use \`PageShell\` (Admin) or \`PageHeader\` (SI). If the page has no \`PageShell\`/\`PageHeader\`, request adding it.
2. **Status pills** — No new inline status/pill classes (e.g. \`bg-*-100 text-*-800\`, \`rounded-full px-2 py-1\`). New status display must use \`StatusBadge\` (or \`OrderStatusBadge\`) and shared helpers from \`utils/statusColors\` or SI \`getOrderStatusVariant\`.
3. **Empty & loading** — New empty or loading states must use \`EmptyState\` and \`LoadingSpinner\` (or \`Skeleton\` where appropriate). No ad-hoc "No data" divs or custom spinners.
4. **Libraries** — No new UI libraries (MUI, AntD, Chakra, Bootstrap). Check \`package.json\` diff for new UI deps.
5. **Syncfusion** — New \`@syncfusion/\` usage is allowed only for Scheduler or existing heavy-grid/editor pages (see \`uiConsistencyGate.ts\` allowed exceptions). New list/CRUD pages must use \`DataTable\` or \`StandardListTable\`. Pages that use Syncfusion must still wrap content in \`PageShell\` for title and actions.

**Reference:** \`frontend/src/dev/uiConsistencyGate.ts\` (full rules and \`howToCheck\` per rule).
`.trim();
