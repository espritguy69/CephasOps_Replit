# API Request Examples

## Create Material Allocation

### Endpoint
```
POST /api/departments/{departmentId}/material-allocations
```

### Headers
```
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
```

### Request Body
The `departmentId` is in the URL path, NOT in the request body.

**Correct format:**
```json
{
  "materialId": "guid-here",
  "quantity": 10.5,
  "notes": "Optional notes here"
}
```

**Fields:**
- `materialId` (required): Guid of the material
- `quantity` (required): Decimal number
- `notes` (optional): String

### Example cURL
```bash
curl -X 'POST' \
  'http://localhost:5000/api/departments/{departmentId}/material-allocations' \
  -H 'accept: application/json' \
  -H 'Authorization: Bearer {your-jwt-token}' \
  -H 'Content-Type: application/json' \
  -d '{
  "materialId": "123e4567-e89b-12d3-a456-426614174000",
  "quantity": 10.5,
  "notes": "Initial allocation"
}'
```

### Common Mistakes

❌ **Wrong - Including departmentId in body:**
```json
{
  "departmentId": "...",  // Don't include this!
  "materialId": "...",
  "quantity": 10
}
```

❌ **Wrong - Missing body:**
```bash
curl -X 'POST' 'http://localhost:5000/api/departments/{id}/material-allocations'
```

✅ **Correct:**
```json
{
  "materialId": "123e4567-e89b-12d3-a456-426614174000",
  "quantity": 10.5,
  "notes": "Optional"
}
```

## Switch Company

### Endpoint
```
POST /api/auth/switch-company/{companyId}
```

### Headers
```
Authorization: Bearer {your-current-jwt-token}
```

### Response
```json
{
  "accessToken": "new-jwt-token-with-updated-companyId",
  "expiresAt": "2024-01-01T12:00:00Z",
  "user": {
    "id": "...",
    "email": "...",
    "currentCompanyId": "new-company-id",
    ...
  }
}
```

**Important:** Use the new `accessToken` from the response for all subsequent requests!

