\# 👩‍💻 CephasOps Developer Guide



This guide helps engineers onboard quickly and work correctly within the CephasOps architecture.



---



\# 📚 1. Required Reading



Before writing any code, read these:



1\. `EXEC\_SUMMARY.md`

2\. `ARCHITECTURE\_BOOK.md`

3\. `05\_data\_model/README.md`

4\. `05\_data\_model/DATA\_MODEL\_SUMMARY.md`

5\. Your module spec inside `02\_modules/<module>.md`



These define the domain.



---



\# 🧩 2. Architecture Overview



CephasOps follows:



\- \*\*Clean Architecture\*\*

\- \*\*Domain-Driven Design (DDD)\*\*

\- \*\*Modular feature-based folders\*\*

\- \*\*PostgreSQL + EF Core\*\*

\- \*\*Background Jobs (Hangfire / Quartz)\*\*

\- \*\*REST API\*\*

\- \*\*React/Next.js or Blazor frontend\*\*

\- \*\*Mobile SI App (React Native / Blazor Hybrid)\*\*



---



\# 🗂️ 3. Project Structure



/backend

/frontend

/docs

/tests

/infra



markdown

Copy code



---



\# 🧱 4. Modules



Each module follows the same:



\- Entities

\- Repositories

\- Services

\- Domain events

\- Controllers

\- DTOs

\- Validators

\- Migrations



Example modules:



\- Orders

\- Scheduler

\- SI App

\- Inventory \& RMA

\- Billing

\- Payroll

\- P\&L

\- Parser

\- Settings

\- RBAC



---



\# 🔌 5. Data Model Rules (Critical)



Every entity:



\- MUST have `CompanyId`

\- MUST have `CreatedAt`

\- SHOULD have `UpdatedAt`

\- SHOULD have `MetadataJson` for future-proofing



Every relationship linking two domains must appear in:



05\_data\_model/relationships/<module>\_relationships.md



yaml

Copy code



Cursor depends on this.



---



\# 🏗️ 6. Development Flow



\### Step 1 – Read the spec  

Identify your module in:



02\_modules/



shell

Copy code



\### Step 2 – Check entities  

Find related entities in:



05\_data\_model/entities/



powershell

Copy code



\### Step 3 – Implement  

Using the standard structure:



Domain/

Application/

Infrastructure/

Api/



makefile

Copy code



\### Step 4 – Create migrations  

Run:



dotnet ef migrations add <Name>

dotnet ef database update



shell

Copy code



\### Step 5 – Write tests  

Tests go under:



/tests/<module>/



yaml

Copy code



\### Step 6 – Submit PR  

Every PR must include:



\- What changed  

\- Updated docs  

\- Migration script  

\- Tests  



---



\# 🚦 7. Code Quality Rules



\- No logic inside controllers  

\- Services must validate business rules  

\- Repositories MUST respect company scoping  

\- Never bypass domain rules  

\- No “magic strings” – use enums  

\- No raw SQL unless approved  

\- No duplicate code  

\- No cross-module references without explicit documentation



---



\# 📡 8. Background Jobs



Background job specs live in:



08\_infrastructure/background\_jobs\_infrastructure.md



yaml

Copy code



---



## 8. Troubleshooting

### 8.1 CORS Fix for Frontend Development

**Updated from:** `frontend/CORS_FIX_INSTRUCTIONS.md`

#### Issue: CORS Errors in Development

When developing the frontend, you may encounter CORS errors when making API requests.

#### Solution

The frontend uses a Vite proxy configuration to route `/api/*` to `http://localhost:5000/api/*` in development mode.

**CRITICAL: You MUST Restart the Frontend Server**

The Vite proxy configuration only takes effect after restarting the dev server.

#### Steps to Fix:

1. **Stop the current frontend server:**
   - Find the terminal/console where `npm run dev` is running
   - Press `Ctrl+C` to stop it

2. **Restart the frontend server:**
   ```bash
   cd frontend
   npm run dev
   ```

3. **Hard refresh your browser:**
   - Press `Ctrl+Shift+R` (Windows/Linux) or `Cmd+Shift+R` (Mac)
   - Or: Open DevTools (F12) → Right-click refresh button → "Empty Cache and Hard Reload"

4. **Verify it's working:**
   - Open Browser DevTools (F12) → Console tab
   - Look for log messages:
     - `[API Config] Development mode detected, using relative URL: /api` ✅
     - `[API Client] GET request to: /api/orders` ✅
   - Open Network tab
   - Make an API request
   - Check that requests go to `/api/...` (relative URL) not `http://localhost:5000/api/...`

#### Troubleshooting

**If you still see CORS errors:**

1. Check browser console - Look for `[API Config]` logs to see which URL is being used
2. Check Network tab - Verify requests are going to `/api/...` not `http://localhost:5000/api/...`
3. Verify backend is running - Check `http://localhost:5000` is accessible
4. Clear browser cache completely:
   - Chrome: Settings → Privacy → Clear browsing data → Cached images and files
   - Or use Incognito/Private mode to test

**If requests still go to `http://localhost:5000`:**
- The frontend server wasn't restarted
- Browser is using cached JavaScript
- Hard refresh the browser (Ctrl+Shift+R)

#### How It Works

- **Development**: Uses relative URL `/api` → Vite proxy forwards to `http://localhost:5000/api`
- **Production**: Uses `VITE_API_BASE_URL` env var or falls back to absolute URL

The proxy eliminates CORS issues because the browser sees same-origin requests.

---

\# 📬 9. Contact



For deep architecture changes:



\*\*Contact the CephasOps Lead Architect.\*\*

