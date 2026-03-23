# COMPONENT\_LIBRARY.md

\# COMPONENT\_LIBRARY

CephasOps – UI Component Library (Conceptual)



Key reusable components for Admin \& SI apps.



---



\## Admin App Components



\- `PageLayout`

&nbsp; - Top bar + sidebar navigation.



\- `DataTable`

&nbsp; - Used across Orders, Inventory, Invoices.



\- `StatusPill`

&nbsp; - Order statuses, invoice statuses, RMA statuses.



\- `KpiCard`

&nbsp; - For dashboard metrics.



\- `CalendarGrid`

&nbsp; - For `/scheduler`.



\- `OrderDetailTabs`

&nbsp; - For `/orders/:id` sections.



\- `FileUpload`

&nbsp; - For docket \& attachment uploads.



---



\## SI App Components



\- `JobCard`

&nbsp; - Used in Today/Upcoming lists.



\- `JobStatusBar`

&nbsp; - OnTheWay, MetCustomer, Completed buttons.



\- `PhotoCaptureButton`

&nbsp; - Wraps camera access.



\- `GpsStatusIndicator`

&nbsp; - Shows last captured location timestamp \& status.



\- `MaterialList`

&nbsp; - For materials issued to SI or per job.



These components should be implemented once and reused across screens where possible.



