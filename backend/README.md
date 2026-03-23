\# CephasOps Backend



Cursor AI: build the backend HERE under `/backend/src`.



\## Tech Stack

\- .NET 10 (SDK 10.0.x, target net10.0)

\- PostgreSQL

\- Clean Architecture

\- JWT Authentication



\## Rules

1\. Read `/docs` first.

2\. Follow Storybook + Module docs strictly.

3\. All status transitions must go through WorkflowEngine.

4\. All stock/serial changes must use Inventory \& RMA module.

5\. All queries must enforce companyId isolation.

6\. No business logic in Controllers — all in Domain/Application layers.



Start by generating:

\- /backend/src/Domain

\- /backend/src/Application

\- /backend/src/Infrastructure

\- /backend/src/API



No business logic yet — only file structure + placeholders.



