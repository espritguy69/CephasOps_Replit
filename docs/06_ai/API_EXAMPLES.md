
# API_EXAMPLES.md – Full Version
Full working request/response examples.

## Authentication
### POST /auth/login
Request:
```json
{ "email": "admin@cephas.com", "password": "123456" }
```
Response:
```json
{ "token": "jwt-token", "companyId": "..." }
```

## Orders
### GET /orders
```json
[
  {
    "orderId": "123",
    "serviceId": "TBBN062587G",
    "customerName": "John Doe",
    "currentStatus": "Pending"
  }
]
```

### POST /orders/{id}/assign
```json
{
  "installerId": "si-001",
  "scheduledTime": "2025-02-20T10:00:00"
}
```

### POST /orders/{id}/status
```json
{
  "newStatus": "OnTheWay",
  "timestamp": "2025-02-20T09:40:00",
  "gps": { "lat": 3.139, "lng": 101.693 }
}
```

## Inventory
### POST /inventory/movements
```json
{
  "materialId": "router-001",
  "from": { "type": "Warehouse" },
  "to": { "type": "SI", "id": "si-001" },
  "quantity": 1
}
```

## Billing Example
### POST /invoices/{id}/submit
```json
{
  "portalSubmissionId": "SUBM-20250101-1234"
}
```

