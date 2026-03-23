# Task Relationships

CephasOps – Task Entity Relationships

Version 1.0

---

## Overview

The TaskItem entity has relationships with:
- Company (required)
- Department (optional)
- User (RequestedBy - required)
- User (AssignedTo - required)

---

## 1. Company → TaskItem

**Relationship Type:** One-to-Many  
**Cardinality:** 1 Company : N TaskItems

**Foreign Key:**
- `TaskItem.CompanyId → Company.Id`

**Rules:**
- Every task MUST belong to a company
- Tasks are isolated by company (cannot view across companies)
- Company deletion is restricted if tasks exist (ON DELETE RESTRICT)

**Query Pattern:**
```sql
SELECT * FROM TaskItems WHERE CompanyId = @companyId
```

---

## 2. Department → TaskItem

**Relationship Type:** One-to-Many (Optional)  
**Cardinality:** 1 Department : N TaskItems (nullable)

**Foreign Key:**
- `TaskItem.DepartmentId → Department.Id` (nullable)

**Rules:**
- Tasks can exist without a department (DepartmentId = NULL)
- If department is specified, it must belong to the same company
- Department deletion is restricted if tasks exist (ON DELETE RESTRICT)

**Query Pattern:**
```sql
SELECT * FROM TaskItems 
WHERE CompanyId = @companyId 
  AND DepartmentId = @departmentId
```

**Use Cases:**
- Department managers viewing all department tasks
- Department-level KPI reporting
- Task filtering by department

---

## 3. User (RequestedBy) → TaskItem

**Relationship Type:** One-to-Many  
**Cardinality:** 1 User : N TaskItems (as requester)

**Foreign Key:**
- `TaskItem.RequestedByUserId → User.Id`

**Rules:**
- Every task must have a requester
- User can request tasks for any user in the same company
- User deletion is restricted if tasks exist (ON DELETE RESTRICT)

**Query Pattern:**
```sql
SELECT * FROM TaskItems 
WHERE CompanyId = @companyId 
  AND RequestedByUserId = @userId
```

**Use Cases:**
- View tasks I created
- Track task creation history
- Manager/HOD task assignment tracking

---

## 4. User (AssignedTo) → TaskItem

**Relationship Type:** One-to-Many  
**Cardinality:** 1 User : N TaskItems (as assignee)

**Foreign Key:**
- `TaskItem.AssignedToUserId → User.Id`

**Rules:**
- Every task must be assigned to a user
- Assigned user must belong to the same company
- User deletion is restricted if tasks exist (ON DELETE RESTRICT)

**Query Pattern:**
```sql
SELECT * FROM TaskItems 
WHERE CompanyId = @companyId 
  AND AssignedToUserId = @userId
  AND Status = @status
```

**Use Cases:**
- "My Tasks" view
- User workload tracking
- User KPI calculation
- Task assignment management

---

## 5. Relationship Diagram

```
Company (1)
  └── TaskItem (N)
        ├── Department (0..1) [optional]
        ├── User (RequestedBy) (1)
        └── User (AssignedTo) (1)
```

---

## 6. Cross-Entity Queries

### Get Tasks for User in Department
```sql
SELECT t.* 
FROM TaskItems t
WHERE t.CompanyId = @companyId
  AND t.AssignedToUserId = @userId
  AND (t.DepartmentId = @departmentId OR t.DepartmentId IS NULL)
```

### Get Overdue Tasks by Department
```sql
SELECT t.* 
FROM TaskItems t
WHERE t.CompanyId = @companyId
  AND t.DepartmentId = @departmentId
  AND t.DueAt < NOW()
  AND t.Status NOT IN ('Completed', 'Cancelled')
```

### Get Tasks Requested by Manager for Department
```sql
SELECT t.* 
FROM TaskItems t
WHERE t.CompanyId = @companyId
  AND t.DepartmentId = @departmentId
  AND t.RequestedByUserId = @managerUserId
```

---

## 7. Data Integrity

### Referential Integrity
- All foreign keys use `ON DELETE RESTRICT`
- Prevents orphaned tasks
- Ensures data consistency

### Business Rules
- `AssignedToUserId` and `RequestedByUserId` can be the same user (self-assigned)
- `DepartmentId` must match `CompanyId` (enforced in application layer)
- All users in relationships must belong to the same company

---

## 8. Performance Considerations

### Indexes
1. `(CompanyId, AssignedToUserId, Status)` - Fast "My Tasks" queries
2. `(CompanyId, DepartmentId, Status)` - Fast department views
3. `(CompanyId, RequestedByUserId)` - Fast "Tasks I Created" queries
4. `(CompanyId, DueAt)` - Fast overdue task queries

### Query Optimization
- Always filter by `CompanyId` first (most selective)
- Use status filters to reduce result set
- Index on `DueAt` for date range queries

---

## End of Task Relationships

