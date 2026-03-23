# Automation Engine

## Overview

The automation engine executes rules when domain events occur. Rules are defined with **AutomationRule** (trigger + action) and evaluated per tenant (CompanyId).

## Model

- **AutomationRule:** TriggerType (StatusChange, TimeBased, ConditionBased, EventBased), TriggerStatus (for StatusChange), EntityType (e.g. Order), ActionType (AssignToUser, ChangeStatus, Notify, **GenerateInvoice**, etc.), ActionConfigJson, Priority, IsActive, StopOnMatch.
- **Trigger:** StatusChange with TriggerStatus = "OrderCompleted" → when order reaches OrderCompleted.
- **Action:** GenerateInvoice → create invoice from order via BillingService (idempotent: skip if order already has InvoiceId).

## Event-Driven Execution

- **OrderCompletedEvent** is emitted by WorkflowEngineService when an order's status transitions to OrderCompleted or Completed.
- **OrderCompletedAutomationHandler** handles OrderCompletedEvent: loads order (tenant-scoped), skips if already invoiced, gets applicable rules via GetApplicableRulesAsync(companyId, "Order", "OrderCompleted", partnerId, departmentId, orderTypeCode), then for each rule with ActionType "GenerateInvoice" builds invoice lines, creates invoice, and sets order.InvoiceId.

## Rule Configuration

To enable "WHEN OrderCompleted THEN GenerateInvoice":

1. Create an automation rule with: EntityType=Order, TriggerType=StatusChange, TriggerStatus=OrderCompleted, ActionType=GenerateInvoice, IsActive=true.
2. Optionally set PartnerId, DepartmentId, OrderType to scope the rule.
3. The handler runs in the event pipeline after OrderCompletedEvent is persisted and dispatched.

## Tenant Safety

- Rules are stored with CompanyId (CompanyScopedEntity). GetApplicableRulesAsync filters by companyId.
- The handler loads order only for the event's CompanyId and creates invoice for that company.
