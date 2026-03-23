# Phase 2 Settings Integration Tests

This directory contains integration tests for Phase 2 Settings modules integrated into core services.

## Test Structure

### OrderServiceIntegrationTests.cs

Tests the integration of Phase 2 Settings modules into `OrderService`:

- **SLA Calculation**: Verifies SLA profiles are applied and breaches are tracked
- **Automation Rules**: Verifies automation rules execute on status changes
- **Escalation Rules**: Verifies escalation rules trigger appropriately
- **Business Hours**: Verifies business hours exclusion in SLA calculations

## Running Tests

```bash
cd backend
dotnet test tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj --filter "FullyQualifiedName~Phase2Settings"
```

## Test Coverage

### SLA Integration
- ✅ SLA profile resolution based on order context
- ✅ Response SLA calculation and breach detection
- ✅ Resolution SLA calculation and breach detection
- ✅ Business hours exclusion in SLA calculation
- ✅ VIP order SLA handling
- ✅ SLA breach notifications

### Automation Rules Integration
- ✅ Auto-assign rule execution
- ✅ Auto-escalate rule execution
- ✅ Auto-notify rule execution
- ✅ Auto-status-change rule execution
- ✅ Rule priority handling
- ✅ Rule scope matching

### Escalation Rules Integration
- ✅ Time-based escalation triggers
- ✅ Status-based escalation triggers
- ✅ Condition-based escalation triggers
- ✅ Escalation to role
- ✅ Escalation to user
- ✅ Status change on escalation

### Business Hours Integration
- ✅ Business hours exclusion in time calculations
- ✅ Weekend exclusion
- ✅ Public holiday exclusion
- ✅ Department-specific business hours

## Test Data Setup

Tests use in-memory database and mocks for external services. Each test should:
1. Set up test data (orders, SLA profiles, rules, etc.)
2. Execute the operation
3. Verify expected behavior
4. Clean up test data

## Future Test Additions

- Approval Workflow integration tests
- EmailSendingService integration tests
- RMAService integration tests
- End-to-end workflow tests

