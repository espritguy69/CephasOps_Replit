# 🖥️ CephasOps Frontend Documentation

This folder contains all documentation for the **frontend layer** of CephasOps, covering:

- Admin Web Portal (React/Blazor)
- Service Installer Mobile App (Hybrid / React Native / Blazor hybrid)
- Component library
- Flow diagrams
- UI standards
- End-to-end storybook journeys

The goal is to give Cursor AI and frontend developers a clear, modular reference for generating a complete UI layer.

---

# 📁 Folder Structure

7_frontend/
README.md ← You are here
storybook/ ← User flows, journeys, and end-to-end scenarios
ui/ ← Actual screen definitions, components, layouts

**Note:** Technical component library documentation (Storybook-style) is located at:
- [Frontend Component Library](../architecture/ui/storybook.md) - Complete UI component library & screen documentation

---

# 📘 1. storybook/ – User Flow Documentation

`storybook/` contains **real-world usage flows**, not technical screens.

Use this folder to understand:

### ✔ How Admin interacts with Orders  
- Creation  
- Scheduling  
- Material assignment  
- Billing interactions  
- P&L views  

### ✔ How Scheduler interacts with SI availability  
- Calendar logic  
- SI load distribution  
- Rescheduling flows  

### ✔ How SI App works in the field  
- Start job session  
- Capture events  
- Upload photos  
- Scan devices  
- Submit docket  

### ✔ Approval chains & exception handling  
- Reschedule approval by TIME  
- Blocker handling  
- Parsing & email-based flows  

### Examples found here:
- Activation job story  
- Assurance job story  
- Outdoor relocation  
- FTTR flows  
- Order creation via parser  

**Purpose:**  
Provide a business-first experience so UI/UX designers and Cursor can generate the correct pages.

---

# 📘 2. ui/ – Screens, Components, UX Specs

This folder contains:

### **Screens (per module)**
- Orders list & detail  
- Scheduler calendar  
- SI job session screens  
- Inventory pages  
- Billing (invoices, payments)  
- Payroll UI  
- P&L dashboard  

### **Component library**
- Buttons, form controls, modals  
- Table components  
- Filters  
- Multi-company selector  
- Partner selector  
- Status badges  
- Timeline components  
- Photo gallery  
- Map/GPS preview components  

### **UI state definitions**
- Loading states  
- Error states  
- Permission-based visibility  
- Multi-company isolation rules  

### **Validation rules**
- Required fields  
- Invalid combinations  
- Business logic boundaries (from `/docs/03_business`)  

### **Role-based UI**
Admin vs Scheduler vs SI App vs Finance vs Director.

Cursor uses these definitions to generate:

- React/Blazor components  
- Routing structure  
- Forms and validations  
- Page layouts  

---

# 🔌 Backend-to-Frontend Mapping

Frontend screens use backend definitions from:

- `/docs/02_modules` → module logic  
- `/docs/04_api` → endpoints  
- `/docs/05_data_model` → fields & types  

This ensures:

- UI fields match entity fields  
- Validation rules match backend rules  
- API responses map correctly  

---

# 📐 Standards & Conventions

Frontend MUST follow:

### ✔ Shared Component Library  
Defined inside `/ui/components`.

### ✔ Consistent page template  
- Page title  
- Search/filter  
- Table/list  
- Detail view  
- Actions  
- Audit log  

### ✔ Mobile-first for SI App

### ✔ Dark/light mode optional support

### ✔ Multi-company awareness  
UI must always show correct company context.

---

# 📊 How Cursor Should Use This Folder

Cursor must:

### **1. Load storybook first**  
To understand human flows.

### **2. Load ui/screens next**  
To understand page structure.

### **3. Map UI fields to**  
`05_data_model/entities/*.md`

### **4. Map actions to**  
`04_api/API_BLUEPRINT.md`

### **5. Generate components + pages**  
Using the shared UI patterns.

---

# 🧪 Testing Guidance

UI Tests (optional folder):
- Page load tests  
- Role-based visibility tests  
- Form validations  
- API integration mocks  

If needed, create:

