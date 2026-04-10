# Undocumented API Controllers

**Date:** April 2026  
**Status:** These controllers exist in code but lack detailed API documentation

This file serves as a tracking index for controllers that need documentation. The controllers are grouped by functional area.

---

## Analytics & Observability

| Controller | Route Prefix | Purpose |
|------------|-------------|---------|
| `PlatformAnalyticsController` | `/api/platform-analytics` | Platform-wide analytics and dashboards |
| `OperationalInsightsController` | `/api/operational-insights` | Operational intelligence data |
| `ObservabilityController` | `/api/observability` | System health and telemetry endpoints |

---

## Messaging

| Controller | Route Prefix | Purpose |
|------------|-------------|---------|
| `SmsController` | `/api/sms` | SMS sending and status tracking |
| `WhatsAppController` | `/api/whatsapp` | WhatsApp message sending |
| `MessagingController` | `/api/messaging` | Unified messaging interface |

---

## Master Data

| Controller | Route Prefix | Purpose |
|------------|-------------|---------|
| `SkillsController` | `/api/skills` | SI skill definitions and assignments |
| `TeamsController` | `/api/teams` | Team management |
| `WarehousesController` | `/api/warehouses` | Warehouse locations and stock points |
| `VendorsController` | `/api/vendors` | Vendor/supplier management |
| `VerticalsController` | `/api/verticals` | Business vertical definitions |
| `TaxCodesController` | `/api/tax-codes` | Tax code reference data |
| `BrandsController` | `/api/brands` | Brand management |
| `BinsController` | `/api/bins` | Warehouse bin locations |

---

## Financial

| Controller | Route Prefix | Purpose |
|------------|-------------|---------|
| `GponBaseWorkRatesController` | `/api/gpon-base-work-rates` | GPON job rate card management |
| `FinancialAlertsController` | `/api/financial-alerts` | Order financial anomaly alerts |
| `ExcelToPdfController` | `/api/excel-to-pdf` | Document conversion utility |

---

## Order Management (Entity-specific)

| Controller | Route Prefix | Purpose |
|------------|-------------|---------|
| `OrderCategoriesController` | `/api/order-categories` | Order category definitions |
| `OrderStatusesController` | `/api/order-statuses` | Order status reference data |
| `OrderTypesController` | `/api/order-types` | Order type definitions |
| `AssetTypesController` | `/api/asset-types` | Asset type reference data |

---

## Other

| Controller | Route Prefix | Purpose |
|------------|-------------|---------|
| `PaymentTermsController` | `/api/payment-terms` | Payment terms reference data |
| `ReportDefinitionsController` | `/api/report-definitions` | Report template definitions |
| `KpiProfilesController` | `/api/kpi-profiles` | KPI profile configuration |
| `PartnerGroupsController` | `/api/partner-groups` | Partner grouping and billing structures |

---

> **Note:** All controllers follow the standard `ApiResponse<T>` envelope pattern. Most support standard CRUD operations (GET list, GET by ID, POST create, PUT update, DELETE). Detailed request/response schemas should be generated from the Swagger endpoint at `/swagger`.
