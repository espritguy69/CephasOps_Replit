# CephasOps Cursor Guides

Reference patterns for AI-assisted development in this workspace.

## Purpose

These guides show **actual patterns** used in the CephasOps codebase. When Cursor generates new code, it should follow these examples.

---

## Backend Patterns (.NET 10 / ASP.NET Core)

### `backend-example-endpoints/`

| File | Pattern |
|------|---------|
| `OrdersController.cs` | **Controller-based API** with `[ApiController]`, route attributes, and standard response envelope |

### `backend-example-handlers/`

| File | Pattern |
|------|---------|
| `OrderService.cs` | **Service layer** with business logic, EF Core queries, and department resolution |

### Key Backend Conventions

- Use `[ApiController]` with route attributes (`[HttpGet]`, `[HttpPost]`, etc.)
- Inject services via constructor DI
- Return `ApiResponse<T>` envelope: `{ success, message, data, errors }`
- Services handle business logic, repositories are optional
- Department filtering via `DepartmentId` parameter

---

## Frontend Patterns (React + TypeScript)

### `frontend-example-api/`

| File | Pattern |
|------|---------|
| `orders.ts` | **API client module** with typed request/response functions |

### `frontend-example-hooks/`

| File | Pattern |
|------|---------|
| `useOrders.ts` | **TanStack Query hooks** for data fetching with department context |
| `useDepartmentContext.ts` | **Context hook** for department state management |

### `frontend-example-pages/`

| File | Pattern |
|------|---------|
| `OrdersListPage.tsx` | **List page** with filters, department loading, and data table |
| `OrderFormPage.tsx` | **Form page** with React Hook Form, Zod validation, and mutations |
| `ParserReviewPage.tsx` | **Parser workflow** with file upload, preview, and approval |

### Key Frontend Conventions

- **TypeScript** for all new files (`.ts`, `.tsx`)
- Use `apiClient` from `/frontend/src/api/client.ts` (not raw fetch/axios)
- TanStack Query for server state (`useQuery`, `useMutation`)
- React Hook Form + Zod for forms
- shadcn/ui components with Tailwind CSS
- Wait for `departmentLoading === false` before fetching department-scoped data

---

## How to Use These Guides

1. **New API endpoint** → Reference `OrdersController.cs`
2. **New service** → Reference `OrderService.cs`
3. **New API module** → Reference `orders.ts`
4. **New TanStack Query hook** → Reference `useOrders.ts`
5. **New list page** → Reference `OrdersListPage.tsx`
6. **New form page** → Reference `OrderFormPage.tsx`
7. **File upload/parser** → Reference `ParserReviewPage.tsx`
8. **Department context** → Reference `useDepartmentContext.ts`

---

## Important Notes

- **Frontend is TypeScript** - All guides use `.ts`/`.tsx` as the target pattern
- **Backend uses Controllers + Services** - NOT Minimal API or MediatR/CQRS
- **Department context** - Always check `departmentLoading` before fetching scoped data
- **Response envelope** - Backend returns `{ success, message, data, errors }`

