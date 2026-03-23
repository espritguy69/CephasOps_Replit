\# 👨‍💻 CephasOps Developer Handbook



This handbook defines how developers must work inside the CephasOps ecosystem.



────────────────────────────────────────────

\# 1. Core Principles

\- Documentation = Truth

\- No assumptions

\- Multi-company safe

\- RBAC everywhere

\- Modular architecture

\- Predictable patterns

\- Zero silent side effects

\- Fully tested

\- Cursor-driven deterministic workflow



────────────────────────────────────────────

\# 2. Coding Standards

Backend:

\- .NET 10

\- Clean Architecture

\- CQRS (MediatR if enabled)

\- EF Core strict mapping

\- Repository + Service pattern

\- API versioning



Frontend:

\- React + TypeScript

\- Storybook-driven UI

\- Component library

\- Hooks-based architecture

\- Role-based visibility



SI App:

\- React Native (or hybrid)

\- Offline-first

\- Sync queue for uploads

\- Device activation security



────────────────────────────────────────────

\# 3. Database Conventions

\- Always include `CompanyId`

\- Never store undocumented fields

\- Use `MetadataJson` only where defined

\- Use snake\_case table names if documented

\- Use singular table names



────────────────────────────────────────────

\# 4. Branching Strategy

\- `main`: production

\- `develop`: staging

\- `feature/\*`: per-task branches

\- `hotfix/\*`: critical fixes



────────────────────────────────────────────

\# 5. Commit Rules

\- Atomic commits only

\- Reference docs when changing code

\- Never commit commented-out code

\- Use: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`



────────────────────────────────────────────

\# 6. Code Review Guidelines

\- Follow documentation strictly  

\- Check multi-company isolation  

\- Check RBAC  

\- Check migrations  

\- Check tests  

\- Check logs \& error handling  

\- No magic numbers  

\- No leaking domain logic into controllers  

\- No over-fetching data  



────────────────────────────────────────────

\# 7. Testing Requirements

Every PR must include:

\- Unit tests for application layer  

\- Integration tests for API  

\- Snapshot tests for Storybook components  

\- Edge cases for parser  

\- Background job runner tests (if applicable)  



────────────────────────────────────────────

\# 8. Background Jobs

Defined in `/docs/08\_infrastructure`.



Required jobs:

\- Email ingestion

\- Parser workflow

\- Snapshot cleanup

\- P\&L nightly rebuild

\- Scheduler validator

\- Docket reconciliation



────────────────────────────────────────────

\# 9. Parser Rules

Defined in `/docs/06\_ai/\*`.



Key flows:

\- Email → ParseSession → Parser → Approvals → Order

\- Normalization rules

\- Duplicate detection

\- VIP email detection (via GlobalSettings)

\- Snapshot retention (7 days default)



────────────────────────────────────────────

\# 10. Notifications

\- Defined in Global Settings

\- VIP notifications

\- Task notifications

\- System events notifications

\- Email-parser alerts

\- Push notifications (mobile)



────────────────────────────────────────────

\# 11. Deployment

Infra stack:

\- Docker

\- Kubernetes

\- Postgres

\- RabbitMQ (if enabled)

\- Background workers

\- CI/CD pipeline



Dev, UAT, Production require:

\- Environment configs

\- Secrets management

\- Health checks

\- Monitoring



────────────────────────────────────────────

\# 12. Developer Workflow with Cursor



1\. Read docs first  

2\. Use delta prompt  

3\. Implement code  

4\. Add tests  

5\. Run migration  

6\. Push to feature branch  

7\. Create PR  

8\. Send for review  

9\. Merge into develop  

10\. CI/CD deploys automatically



────────────────────────────────────────────

\# Everything must match documentation exactly.



