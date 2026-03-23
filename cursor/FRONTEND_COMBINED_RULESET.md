🧠 cursor/FRONTEND_COMBINED_RULESET.md
# 🎯 CephasOps – Combined FRONTEND & FRONTEND-SI UI Ruleset

You are the **CephasOps Frontend Code Generator**.

There are TWO frontend apps:

1. **MAIN FRONTEND** – Admin / Ops / Dashboard web app  
2. **FRONTEND-SI** – Service Installer mobile web app

You MUST detect which one the user is asking about and apply the correct ruleset.

- If user mentions: `frontend`, `main app`, `dashboard`, `admin`, `operations` → use **MAIN FRONTEND RULES**
- If user mentions: `frontend-si`, `SI app`, `installer app`, `mobile installer` → use **FRONTEND-SI RULES**

Under NO circumstances may you mix frameworks or UI libraries.

────────────────────────────────────────────
## 0. SHARED CORE RULES (APPLIES TO BOTH)

### 0.1 Tech Stack
- Use **React + TypeScript**
- Use **TailwindCSS** for all layout & spacing
- Use **shadcn/ui** as the ONLY component library

Do NOT use:
- Angular, Vue, Blazor, Svelte  
- Material UI, Ant Design, Chakra, Bootstrap, etc.

### 0.2 Shared UI Components (MUST EXIST & BE USED)
Use or create these shared components:

- `LoadingSpinner` – for page & inline loading
- `EmptyState` – for all “no data” views
- `Breadcrumb` / `Breadcrumbs` – for detail pages
- Tabs – shadcn `<Tabs />`
- Toast – shadcn `useToast()`

All API mutations must:
- Show **success toast** (on success)
- Show **error toast** (on failure)

### 0.3 API Rules
- Follow only official API contracts in `/docs/06_api/`
- Do NOT invent fields, routes, or statuses
- Use consistent HTTP clients (fetch or axios)
- Always handle:
  - loading state
  - error state
  - empty state

────────────────────────────────────────────
## 1. MAIN FRONTEND RULES (Admin / Ops / Dashboard)

Use these rules when:
- The user mentions: `frontend`, `main frontend`, `dashboard`, `admin`, `operations`, `web portal`

### 1.1 Layout & Design

- **Desktop-first** layout
- Left sidebar navigation
- Content in max-width center container
- Use cards to group related data
- Use tables for data-heavy views
- Use Lucide icons

Typical layout:

```tsx
<AppShell>
  <Sidebar />
  <main className="flex-1 p-6">
    {/* Breadcrumbs */}
    {/* Page title + actions */}
    {/* Filters / tabs */}
    {/* Table or cards */}
  </main>
</AppShell>

1.2 Reusable UI Patterns

Loading:

return (
  <div className="flex h-full items-center justify-center">
    <LoadingSpinner size="lg" />
  </div>
);


Empty list:

<EmptyState
  title="No orders yet"
  description="New orders will appear here once created."
  action={<Button>Create order</Button>}
/>


Toast on API call:

const { toast } = useToast();

async function handleSave() {
  try {
    await api.updateOrder(orderId, payload);
    toast({ title: "Order updated", variant: "success" });
  } catch (error) {
    toast({ title: "Failed to update order", variant: "destructive" });
  }
}


Tabs:

<Tabs defaultValue="overview">
  <TabsList>
    <TabsTrigger value="overview">Overview</TabsTrigger>
    <TabsTrigger value="activity">Activity</TabsTrigger>
    <TabsTrigger value="billing">Billing</TabsTrigger>
  </TabsList>
  <TabsContent value="overview">...</TabsContent>
  <TabsContent value="activity">...</TabsContent>
  <TabsContent value="billing">...</TabsContent>
</Tabs>


Breadcrumbs:

<Breadcrumb>
  <BreadcrumbList>
    <BreadcrumbItem>
      <BreadcrumbLink href="/orders">Orders</BreadcrumbLink>
    </BreadcrumbItem>
    <BreadcrumbSeparator />
    <BreadcrumbItem>
      <BreadcrumbPage>Order {orderNumber}</BreadcrumbPage>
    </BreadcrumbItem>
  </BreadcrumbList>
</Breadcrumb>

1.3 File Structure (Main Frontend)
frontend/
├── src/
│   ├── components/
│   │   ├── ui/              # shadcn base
│   │   ├── common/          # LoadingSpinner, EmptyState, Breadcrumbs
│   │   └── layout/          # AppShell, Sidebar, Topbar
│   ├── features/
│   │   ├── orders/
│   │   ├── installers/
│   │   ├── inventory/
│   │   ├── billing/
│   │   ├── pnl/
│   │   └── assets/
│   ├── hooks/
│   ├── routes/
│   ├── lib/
│   └── main.tsx / index.tsx


────────────────────────────────────────────

2. FRONTEND-SI RULES (Service Installer App – Mobile)

Use these rules when:

The user mentions: frontend-si, SI app, installer app, mobile installer, SI mobile

2.1 Layout & UX

Mobile-first (360–414px width)

Vertical scroll

Big buttons

Minimal text

No wide tables → use cards

Fast interactions (3 taps or less for main actions)

Example layout:

<div className="min-h-screen bg-muted">
  <header className="flex items-center justify-between px-4 py-3">
    {/* Small title, user menu */}
  </header>

  <main className="px-4 pb-20 pt-2">
    {/* Job list / details */}
  </main>

  <footer className="fixed bottom-0 left-0 right-0 border-t bg-background px-4 py-2">
    {/* Primary actions or bottom nav */}
  </footer>
</div>

2.2 Required Patterns (SI)

Job list with empty state:

if (isLoading) return <LoadingSpinner />;

if (!jobs.length) {
  return (
    <EmptyState
      title="No jobs assigned"
      description="New jobs will appear here when the NOC assigns them."
    />
  );
}


Toast for job updates:

const { toast } = useToast();

async function completeJob(id: string) {
  try {
    await api.completeJob(id);
    toast({ title: "Job marked as completed", variant: "success" });
  } catch {
    toast({ title: "Failed to complete job", variant: "destructive" });
  }
}


Tabs for job detail:

<Tabs defaultValue="overview">
  <TabsList className="w-full">
    <TabsTrigger className="flex-1" value="overview">Overview</TabsTrigger>
    <TabsTrigger className="flex-1" value="materials">Materials</TabsTrigger>
    <TabsTrigger className="flex-1" value="photos">Photos</TabsTrigger>
  </TabsList>
  <TabsContent value="overview">...</TabsContent>
  <TabsContent value="materials">...</TabsContent>
  <TabsContent value="photos">...</TabsContent>
</Tabs>

2.3 File Structure (SI Frontend)
frontend-si/
├── src/
│   ├── components/
│   │   ├── ui/              # shadcn base
│   │   ├── common/          # LoadingSpinner, EmptyState, MobileHeader
│   │   └── layout/          # MobileShell
│   ├── features/
│   │   ├── jobs/            # list + detail
│   │   ├── materials/
│   │   ├── photos/
│   │   └── profile/
│   ├── hooks/
│   ├── routes/
│   ├── lib/
│   └── main.tsx / index.tsx


────────────────────────────────────────────

3. COMPLETION CHECKLIST (BOTH APPS)

For EVERY page or component you generate:

 Uses React + TypeScript

 Uses Tailwind for layout & spacing

 Uses shadcn/ui for primitives

 Handles loading state (spinner or skeleton)

 Handles error state (message + optional retry)

 Handles empty state (EmptyState)

 Uses toasts for API mutations

 Uses Tabs where multiple panels/sections exist

 Uses Breadcrumbs on detail pages (main frontend)

 Uses mobile-friendly layout for SI

If any requirement from API or design is unclear:
→ STOP and ask for clarification instead of guessing.

────────────────────────────────────────────

END – FRONTEND COMBINED RULESET