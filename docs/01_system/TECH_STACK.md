# CephasOps – Technology Stack Summary

**Version:** 1.0  
**Date:** December 2025  
**Status:** Production System

---

## 1. Backend Technologies

### 1.1 Core Framework

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 10.0 | Runtime and framework |
| **ASP.NET Core** | 10.0 | Web API framework |
| **C#** | Latest | Programming language |

### 1.2 Database & ORM

| Technology | Version | Purpose |
|------------|---------|---------|
| **PostgreSQL** | Latest (Supabase) | Primary database |
| **Entity Framework Core** | 10.0 | ORM |
| **Npgsql.EntityFrameworkCore.PostgreSQL** | 10.0 | PostgreSQL provider |

### 1.3 Authentication & Security

| Technology | Version | Purpose |
|------------|---------|---------|
| **JWT Bearer Authentication** | 10.0 | Token-based authentication |
| **System.IdentityModel.Tokens.Jwt** | 8.2.1 | JWT token handling |

### 1.4 Document Generation

| Technology | Version | Purpose |
|------------|---------|---------|
| **Syncfusion.Pdf.Net.Core** | 31.1.17 | PDF generation |
| **Syncfusion.XlsIO.Net.Core** | 31.1.17 | Excel generation |
| **Syncfusion.DocIO.Net.Core** | 31.1.17 | Word document generation |
| **Syncfusion.DocIORenderer.Net.Core** | 31.1.17 | Word to PDF conversion |
| **QuestPDF** | 2025.7.4 | Alternative PDF generation |
| **Handlebars.Net** | 2.1.6 | Template rendering |

### 1.5 Email & Communication

| Technology | Version | Purpose |
|------------|---------|---------|
| **MailKit** | 4.7.1 | POP3/IMAP/SMTP client |
| **MimeKit** | 4.7.1 | MIME message handling |

### 1.6 File Processing

| Technology | Version | Purpose |
|------------|---------|---------|
| **PdfPig** | 0.1.8 | PDF parsing |
| **MSGReader** | 5.5.1 | Outlook MSG file parsing |
| **ExcelDataReader** | (Removed) | Excel parsing (using Syncfusion instead) |

### 1.7 Data Import/Export

| Technology | Version | Purpose |
|------------|---------|---------|
| **CsvHelper** | 33.0.1 | CSV import/export |

### 1.8 API Documentation

| Technology | Version | Purpose |
|------------|---------|---------|
| **Swashbuckle.AspNetCore** | 6.8.1 | Swagger/OpenAPI generation |
| **Microsoft.OpenApi** | 1.6.14 | OpenAPI specification |

### 1.9 Image Processing

| Technology | Version | Purpose |
|------------|---------|---------|
| **SixLabors.ImageSharp** | 3.1.12 | Image manipulation |

### 1.10 JSON Serialization

| Technology | Version | Purpose |
|------------|---------|---------|
| **Newtonsoft.Json** | 13.0.3 | JSON serialization (override for vulnerable dependencies) |

---

## 2. Frontend Technologies (Admin Portal)

### 2.1 Core Framework

| Technology | Version | Purpose |
|------------|---------|---------|
| **React** | 18.2.0 | UI framework |
| **TypeScript** | 5.9.3 | Type-safe JavaScript |
| **Vite** | Latest | Build tool and dev server |

### 2.2 UI Components & Styling

| Technology | Version | Purpose |
|------------|---------|---------|
| **shadcn/ui** | Latest | Component library |
| **Tailwind CSS** | 3.4.18 | Utility-first CSS |
| **lucide-react** | 0.344.0 | Icon library |
| **class-variance-authority** | 0.7.1 | Component variants |
| **clsx** | 2.1.1 | Conditional class names |
| **tailwind-merge** | 2.6.0 | Tailwind class merging |

### 2.3 Data Fetching & State

| Technology | Version | Purpose |
|------------|---------|---------|
| **TanStack Query** | 5.90.11 | Data fetching and caching |
| **React Context API** | Built-in | Global state (Auth, Department, Theme) |

### 2.4 Forms & Validation

| Technology | Version | Purpose |
|------------|---------|---------|
| **React Hook Form** | 7.53.0 | Form management |
| **Zod** | 3.23.8 | Schema validation |
| **@hookform/resolvers** | 3.9.0 | Zod integration |

### 2.5 Routing

| Technology | Version | Purpose |
|------------|---------|---------|
| **React Router DOM** | 6.20.0 | Client-side routing |

### 2.6 Syncfusion Components (Enhanced Pages)

| Technology | Version | Purpose |
|------------|---------|---------|
| **@syncfusion/ej2-react-grids** | 31.1.17 | Data grids |
| **@syncfusion/ej2-react-treegrid** | 31.1.17 | Tree grids |
| **@syncfusion/ej2-react-schedule** | 31.1.17 | Calendar/scheduler |
| **@syncfusion/ej2-react-kanban** | 31.1.17 | Kanban boards |
| **@syncfusion/ej2-react-charts** | 31.1.17 | Charts and graphs |
| **@syncfusion/ej2-react-dropdowns** | 31.1.17 | Dropdowns |
| **@syncfusion/ej2-react-inputs** | 31.1.17 | Input components |
| **@syncfusion/ej2-react-calendars** | 31.1.17 | Date pickers |
| **@syncfusion/ej2-react-diagrams** | 31.1.17 | Diagram components |
| **@syncfusion/ej2-react-pdfviewer** | 31.1.17 | PDF viewer |
| **@syncfusion/ej2-react-richtexteditor** | 31.1.17 | Rich text editor |
| **@syncfusion/ej2-react-spreadsheet** | 31.1.17 | Spreadsheet component |

### 2.7 Drag & Drop

| Technology | Version | Purpose |
|------------|---------|---------|
| **@dnd-kit/core** | 6.3.1 | Drag and drop core |
| **@dnd-kit/sortable** | 10.0.0 | Sortable lists |
| **@dnd-kit/utilities** | 3.2.2 | DnD utilities |

### 2.8 Utilities

| Technology | Version | Purpose |
|------------|---------|---------|
| **date-fns** | 4.1.0 | Date manipulation |
| **exceljs** | 4.4.0 | Excel file generation |

### 2.9 Testing

| Technology | Version | Purpose |
|------------|---------|---------|
| **Vitest** | 4.0.14 | Test runner |
| **@testing-library/react** | 14.1.2 | React testing utilities |
| **@testing-library/jest-dom** | 6.1.5 | DOM matchers |
| **@testing-library/user-event** | 14.5.1 | User interaction simulation |
| **@vitest/ui** | 4.0.14 | Test UI |
| **@vitest/coverage-v8** | 4.0.14 | Code coverage |
| **@vitest/browser-playwright** | 4.0.14 | Browser testing |
| **playwright** | 1.57.0 | Browser automation |
| **jsdom** | 23.0.1 | DOM environment for tests |

---

## 3. SI App Technologies

### 3.1 Core Framework

| Technology | Version | Purpose |
|------------|---------|---------|
| **React** | 18.2.0 | UI framework |
| **TypeScript** | 5.9.3 | Programming language (fully migrated from JavaScript) |
| **Vite** | 6.4.1 | Build tool |

### 3.2 Mobile Optimization

- **PWA (Progressive Web App):** Installable on mobile devices
- **Responsive Design:** Mobile-first approach
- **Touch-Friendly:** Large touch targets

---

## 4. Infrastructure & Deployment

### 4.1 Database Hosting

| Service | Purpose |
|---------|---------|
| **Supabase (PostgreSQL)** | Managed PostgreSQL database |

### 4.2 File Storage

| Storage | Purpose |
|---------|---------|
| **Local File System** | Current file storage (uploads, documents) |
| **Future:** Cloud Storage (S3, Azure Blob) | Planned migration |

### 4.3 Development Tools

| Tool | Purpose |
|------|---------|
| **Cursor** | AI-powered IDE |
| **Git** | Version control |
| **PowerShell** | Scripting (Windows) |

---

## 5. DevOps & CI/CD

### 5.1 Version Control

- **Git:** Source control
- **GitHub:** Repository hosting (assumed)

### 5.2 Build & Deployment

- **Local Development:** `dotnet watch run` for backend, `vite dev` for frontend
- **Production:** Manual deployment (future: automated CI/CD)

### 5.3 Scripts

| Script | Purpose |
|--------|---------|
| `start-backend.ps1` | Start backend API |
| `start-frontend.ps1` | Start admin portal |
| `restart-all.ps1` | Restart all services |
| `quick-code-sync.ps1` | Sync code between PCs |
| `quick-db-sync.ps1` | Sync database |

---

## 6. Development Environment

### 6.1 Required Tools

- **.NET 10 SDK:** Backend development
- **Node.js:** Frontend development
- **PostgreSQL Client:** Database management
- **Visual Studio / VS Code / Cursor:** IDE

### 6.2 Environment Variables

**Backend:**
- `ConnectionStrings:DefaultConnection` - Database connection string
- `Jwt:Key` - JWT secret key
- `Jwt:Issuer` - JWT issuer
- `Jwt:Audience` - JWT audience
- `SYNCFUSION_LICENSE_KEY` - Syncfusion license key
- `Cors:AllowedOrigins` - CORS allowed origins

**Frontend:**
- `VITE_API_BASE_URL` - Backend API base URL

---

## 7. License Keys

### 7.1 Syncfusion License

- **License Key:** Stored in environment variable `SYNCFUSION_LICENSE_KEY`
- **Usage:** PDF, Excel, Word generation, UI components
- **Registration:** In `Program.cs` via `SyncfusionLicenseProvider.RegisterLicense()`

---

## 8. Technology Decisions & Rationale

### 8.1 Why .NET 10?

- **Latest LTS:** Long-term support
- **Performance:** High-performance runtime
- **Ecosystem:** Rich library ecosystem
- **Type Safety:** Strong typing

### 8.2 Why PostgreSQL?

- **Open Source:** Cost-effective
- **ACID Compliance:** Strong data integrity
- **Feature Rich:** JSON support, full-text search
- **Scalability:** Handles high concurrency

### 8.3 Why React?

- **Popular:** Large community and ecosystem
- **Component-Based:** Reusable UI components
- **TypeScript Support:** Type safety
- **Performance:** Virtual DOM optimization

### 8.4 Why Syncfusion?

- **Comprehensive:** PDF, Excel, Word, UI components
- **Enterprise-Grade:** Professional components
- **License Available:** Already licensed

### 8.5 Why TanStack Query?

- **Data Fetching:** Simplifies API calls
- **Caching:** Automatic caching and invalidation
- **State Management:** Reduces need for Redux
- **Optimistic Updates:** Better UX

---

## 9. Future Technology Considerations

### 9.1 Potential Additions

- **SignalR:** Real-time notifications
- **Redis:** Caching layer
- **Docker:** Containerization
- **Kubernetes:** Orchestration (if needed)
- **Azure Blob Storage / S3:** Cloud file storage
- **SMS/WhatsApp APIs:** Notification channels

### 9.2 Migration Considerations

- **File Storage:** Migrate to cloud storage
- **Caching:** Add Redis for performance
- **Real-Time:** Add SignalR for live updates
- **Mobile App:** Native iOS/Android (if needed)

---

**Document Status:** This tech stack summary reflects the current production system as of December 2025.

