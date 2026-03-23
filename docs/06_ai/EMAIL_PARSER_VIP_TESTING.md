# Email Parser VIP Email Testing – Phase 3

This document defines the test plan, scenarios, and test specifications for VIP email detection, rule evaluation, and notification features.

---

## 1. Scope

**In-scope:**
- VIP email list matching (exact email address)
- Email rule evaluation (pattern matching, priority ordering)
- Action handling (VIP marking, routing, ignoring)
- Notification target resolution

**Out-of-scope:**
- Actual notification delivery (email/SMS/push)
- Frontend UI testing
- Database migration testing

---

## 2. Domain Entities

### 2.1 VipEmail Entity

Location: `backend/src/CephasOps.Domain/Parser/Entities/VipEmail.cs`

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Primary key |
| `CompanyId` | Guid? | Company scope |
| `EmailAddress` | string | Email to match (exact) |
| `DisplayName` | string? | Friendly name |
| `Description` | string? | Notes about this VIP |
| `NotifyUserId` | Guid? | User to notify |
| `NotifyRole` | string? | Role to notify (all users with role) |
| `IsActive` | bool | Whether entry is active |
| `CreatedByUserId` | Guid | Creator |
| `UpdatedByUserId` | Guid? | Last updater |

### 2.2 ParserRule Entity (Email Rule)

Location: `backend/src/CephasOps.Domain/Parser/Entities/ParserRule.cs`

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Primary key |
| `CompanyId` | Guid? | Company scope |
| `EmailAccountId` | Guid? | Specific mailbox (null = all) |
| `FromAddressPattern` | string? | FROM address pattern (wildcards supported) |
| `DomainPattern` | string? | Domain pattern (e.g., `time.com.my`) |
| `SubjectContains` | string? | Subject must contain (case-insensitive) |
| `IsVip` | bool | Mark matching emails as VIP |
| `TargetDepartmentId` | Guid? | Route to department |
| `TargetUserId` | Guid? | Route to user |
| `ActionType` | string | Action to take (see enum) |
| `Priority` | int | Evaluation priority (higher = first) |
| `IsActive` | bool | Whether rule is active |
| `Description` | string? | Rule description |

### 2.3 EmailRuleActionType Enum

Location: `backend/src/CephasOps.Domain/Parser/Enums/EmailRuleActionType.cs`

| Value | Description |
|-------|-------------|
| `RouteToDepartment` | Route email to a department |
| `RouteToUser` | Route email to a user |
| `MarkVipOnly` | Mark as VIP only (no routing) |
| `Ignore` | Skip processing this email |
| `MarkVipAndRouteToDepartment` | Mark VIP + route to department |
| `MarkVipAndRouteToUser` | Mark VIP + route to user |

### 2.4 EmailMessage VIP Fields

Location: `backend/src/CephasOps.Domain/Parser/Entities/EmailMessage.cs`

| Field | Type | Description |
|-------|------|-------------|
| `IsVip` | bool | Whether from VIP sender |
| `MatchedRuleId` | Guid? | Rule that matched (if any) |
| `MatchedVipEmailId` | Guid? | VIP entry that matched (if any) |

---

## 3. Pattern Matching Specification

### 3.1 Supported Patterns

| Pattern Type | Syntax | Example | Matches |
|--------------|--------|---------|---------|
| Exact match | `email@domain.com` | `ceo@company.com` | Only `ceo@company.com` |
| Wildcard `*` | `*@domain.com` | `*@company.com` | Any user at company.com |
| Wildcard `?` | `user?@domain.com` | `director?@company.com` | `director1@`, `directorA@` (single char) |
| Domain | `@domain.com` | `@time.com.my` | Any email from time.com.my |
| Complex | `prefix*@*` | `director*@*` | Any director* at any domain |

### 3.2 Pattern Matching Rules

1. All matching is **case-insensitive**
2. Email addresses are **trimmed** before matching
3. Domain patterns starting with `@` match the domain suffix
4. `*` matches zero or more characters
5. `?` matches exactly one character

---

## 4. Evaluation Flow

```
Email Received
     │
     ▼
┌─────────────────────┐
│ 1. Check VIP List   │ ─── Exact email match
│    (VipEmail table) │
└─────────────────────┘
     │
     ▼
┌─────────────────────┐
│ 2. Evaluate Rules   │ ─── Priority order (highest first)
│    (ParserRule)     │
└─────────────────────┘
     │
     ▼
┌─────────────────────┐
│ 3. Apply Action     │
│    - Mark VIP       │
│    - Route          │
│    - Ignore         │
└─────────────────────┘
     │
     ▼
┌─────────────────────┐
│ 4. Continue Parsing │ ─── Unless Ignore action
└─────────────────────┘
```

### 4.1 Evaluation Order

1. **VIP email list** is checked first (exact match only)
2. **Rules** are evaluated in **priority order** (highest priority first)
3. **First matching rule wins** (except Ignore which stops immediately)
4. Both VIP email match and rule match can occur simultaneously

---

## 5. Test Scenarios

### 5.1 VIP Email Detection

#### Scenario V1: Exact VIP Email Match
**Input:** `ceo@company.com`
**VIP List:** `ceo@company.com` (active)
**Expected:**
- `IsVip = true`
- `MatchedVipEmail` is set
- `NotifyUserId` from VIP entry

#### Scenario V2: Case-Insensitive VIP Match
**Input:** `CEO@COMPANY.COM`
**VIP List:** `ceo@company.com`
**Expected:** Match succeeds

#### Scenario V3: Inactive VIP Entry
**Input:** `ceo@company.com`
**VIP List:** `ceo@company.com` (inactive)
**Expected:** No match, `IsVip = false`

#### Scenario V4: VIP with Notify User
**Input:** `ceo@company.com`
**VIP List:** `ceo@company.com` with `NotifyUserId = user-123`
**Expected:** `NotifyUserId = user-123`

#### Scenario V5: VIP with Notify Role
**Input:** `ceo@company.com`
**VIP List:** `ceo@company.com` with `NotifyRole = "Admin"`
**Expected:** `NotifyRole = "Admin"`

---

### 5.2 Rule Pattern Matching

#### Scenario R1: FROM Address Pattern with Wildcard
**Input:** `noreply@time.com.my`
**Rule:** `FromAddressPattern = "*@time.com.my"`
**Expected:** Rule matches

#### Scenario R2: Domain Pattern
**Input:** `activation@time.com.my`
**Rule:** `DomainPattern = "time.com.my"`
**Expected:** Rule matches

#### Scenario R3: Subject Contains
**Input Subject:** `URGENT: Please respond immediately`
**Rule:** `SubjectContains = "URGENT"`
**Expected:** Rule matches

#### Scenario R4: Subject Contains Case-Insensitive
**Input Subject:** `urgent request`
**Rule:** `SubjectContains = "URGENT"`
**Expected:** Rule matches

#### Scenario R5: Multiple Conditions (AND logic)
**Input:** `noreply@time.com.my`, Subject: `FTTH Activation`
**Rule:** `FromAddressPattern = "*@time.com.my"`, `SubjectContains = "FTTH"`
**Expected:** Rule matches (both conditions satisfied)

#### Scenario R6: Inactive Rule
**Input:** `noreply@time.com.my`
**Rule:** `FromAddressPattern = "*@time.com.my"` (inactive)
**Expected:** Rule does NOT match

---

### 5.3 Rule Priority

#### Scenario P1: Higher Priority Wins
**Rules:**
1. Priority 100: `MarkVipOnly`
2. Priority 50: `RouteToDepartment`

**Expected:** Priority 100 rule is applied

#### Scenario P2: Same Priority - First Defined Wins
**Rules:** Two rules with same priority
**Expected:** First matching rule wins

---

### 5.4 Action Types

#### Scenario A1: Ignore Action
**Rule:** `ActionType = "Ignore"`
**Expected:**
- `ShouldIgnore = true`
- Evaluation stops immediately
- No further rules processed

#### Scenario A2: MarkVipOnly Action
**Rule:** `ActionType = "MarkVipOnly"`
**Expected:**
- `IsVip = true`
- No routing

#### Scenario A3: RouteToDepartment Action
**Rule:** `ActionType = "RouteToDepartment"`, `TargetDepartmentId = dept-123`
**Expected:**
- `TargetDepartmentId = dept-123`
- `IsVip = false` (unless IsVip flag set)

#### Scenario A4: RouteToUser Action
**Rule:** `ActionType = "RouteToUser"`, `TargetUserId = user-456`
**Expected:**
- `TargetUserId = user-456`

#### Scenario A5: MarkVipAndRouteToDepartment
**Rule:** `ActionType = "MarkVipAndRouteToDepartment"`
**Expected:**
- `IsVip = true`
- `TargetDepartmentId` is set

#### Scenario A6: MarkVipAndRouteToUser
**Rule:** `ActionType = "MarkVipAndRouteToUser"`
**Expected:**
- `IsVip = true`
- `TargetUserId` is set

#### Scenario A7: Rule with IsVip Flag
**Rule:** `ActionType = "RouteToDepartment"`, `IsVip = true`
**Expected:**
- `IsVip = true`
- Routing also applied

---

### 5.5 Combined Scenarios

#### Scenario C1: VIP Email + Rule Match
**Input:** `ceo@company.com`
**VIP List:** `ceo@company.com`
**Rule:** `*@company.com` → `RouteToDepartment`
**Expected:**
- Both `MatchedVipEmail` and `MatchedRule` are set
- `IsVip = true`
- Routing applied

#### Scenario C2: Ignore Rule Stops VIP Processing
**Input:** `spam@blocked.com`
**VIP List:** (empty)
**Rules:**
1. Priority 100: `*@blocked.com` → `Ignore`
2. Priority 50: `*@blocked.com` → `MarkVipOnly`
**Expected:**
- `ShouldIgnore = true`
- `IsVip = false` (second rule not evaluated)

---

### 5.6 Edge Cases

#### Scenario E1: Empty FROM Address
**Input:** (empty string)
**Expected:** No match, empty result

#### Scenario E2: No Rules or VIP Entries
**Input:** `anyone@test.com`
**VIP List:** (empty)
**Rules:** (empty)
**Expected:** Empty result, no VIP, no routing

#### Scenario E3: Rule with No Patterns
**Rule:** No `FromAddressPattern`, no `DomainPattern`, no `SubjectContains`
**Expected:** Rule does NOT match any email

#### Scenario E4: Whitespace in Email
**Input:** `  ceo@company.com  `
**VIP List:** `ceo@company.com`
**Expected:** Match succeeds (trimmed)

---

## 6. Unit Test Specifications

### 6.1 Test Class: `EmailRuleEvaluationServiceTests`

Location: `backend/tests/CephasOps.Application.Tests/Parser/Services/EmailRuleEvaluationServiceTests.cs`

#### Pattern Matching Tests (14 tests)
```csharp
// Exact match
MatchesPattern_ExactMatch_ReturnsExpectedResult(email, pattern, expected)

// Wildcard *
MatchesPattern_WildcardStar_MatchesAnyCharacters(email, pattern, expected)

// Wildcard ?
MatchesPattern_WildcardQuestion_MatchesSingleCharacter(email, pattern, expected)

// Domain pattern
MatchesPattern_DomainPattern_MatchesAnyUserAtDomain(email, pattern, expected)

// Complex wildcards
MatchesPattern_ComplexWildcard_MatchesCorrectly(email, pattern, expected)

// Null/empty
MatchesPattern_NullOrEmpty_ReturnsFalse(email, pattern, expected)
```

#### VIP Email Detection Tests (7 tests)
```csharp
Evaluate_VipEmailExactMatch_ReturnsIsVipTrue()
Evaluate_VipEmailCaseInsensitive_MatchesCorrectly()
Evaluate_VipEmailNotInList_ReturnsIsVipFalse()
Evaluate_InactiveVipEmail_IsNotMatched()
Evaluate_VipEmailWithNotifyUser_ReturnsNotifyUserId()
Evaluate_VipEmailWithNotifyRole_ReturnsNotifyRole()
```

#### Rule Evaluation Tests (6 tests)
```csharp
Evaluate_RuleWithFromAddressPattern_MatchesCorrectly()
Evaluate_RuleWithDomainPattern_MatchesCorrectly()
Evaluate_RuleWithSubjectContains_MatchesCorrectly()
Evaluate_RuleWithSubjectContains_CaseInsensitive()
Evaluate_InactiveRule_IsNotEvaluated()
Evaluate_MultipleRules_HighestPriorityWins()
```

#### Action Type Tests (8 tests)
```csharp
Evaluate_IgnoreAction_SetsShoudIgnoreTrue()
Evaluate_MarkVipOnlyAction_SetsIsVipTrue()
Evaluate_RouteToDepartmentAction_SetsTargetDepartmentId()
Evaluate_RouteToUserAction_SetsTargetUserId()
Evaluate_MarkVipAndRouteToDepartment_SetsBothFields()
Evaluate_MarkVipAndRouteToUser_SetsBothFields()
Evaluate_RuleWithIsVipFlag_SetsIsVipTrue()
```

#### Combined Tests (2 tests)
```csharp
Evaluate_BothVipEmailAndRule_BothAreRecorded()
Evaluate_IgnoreRuleStopsEvaluation_NoFurtherRulesProcessed()
```

#### Edge Case Tests (4 tests)
```csharp
Evaluate_EmptyFromAddress_ReturnsEmptyResult()
Evaluate_NoVipEmailsOrRules_ReturnsEmptyResult()
Evaluate_RuleWithNoPatterns_DoesNotMatch()
Evaluate_WhitespaceInEmail_IsTrimmed()
```

**Total: 51 tests** (all passing)

---

## 7. Test Data Examples

### 7.1 VIP Email Entry
```json
{
  "id": "vip-uuid-1",
  "emailAddress": "ceo@company.com",
  "displayName": "CEO",
  "description": "Company CEO - always VIP",
  "notifyUserId": "user-uuid-123",
  "notifyRole": null,
  "isActive": true
}
```

### 7.2 Email Rule - TIME Partner
```json
{
  "id": "rule-uuid-1",
  "fromAddressPattern": "*@time.com.my",
  "domainPattern": null,
  "subjectContains": null,
  "isVip": false,
  "targetDepartmentId": "ops-dept-uuid",
  "targetUserId": null,
  "actionType": "RouteToDepartment",
  "priority": 100,
  "isActive": true,
  "description": "Route all TIME emails to Operations"
}
```

### 7.3 Email Rule - VIP Partner
```json
{
  "id": "rule-uuid-2",
  "fromAddressPattern": "*@vip-partner.com",
  "domainPattern": null,
  "subjectContains": null,
  "isVip": true,
  "targetDepartmentId": null,
  "targetUserId": "manager-uuid",
  "actionType": "MarkVipAndRouteToUser",
  "priority": 200,
  "isActive": true,
  "description": "VIP partner - route to manager"
}
```

### 7.4 Email Rule - Spam Filter
```json
{
  "id": "rule-uuid-3",
  "fromAddressPattern": "*@spam-domain.com",
  "domainPattern": null,
  "subjectContains": null,
  "isVip": false,
  "targetDepartmentId": null,
  "targetUserId": null,
  "actionType": "Ignore",
  "priority": 1000,
  "isActive": true,
  "description": "Block spam domain"
}
```

---

## 8. Priority Classification

### 8.1 Required for Go-Live (P0)
- [x] V1: Exact VIP email match
- [x] V3: Inactive VIP entry handling
- [x] R1: FROM address pattern matching
- [x] A1: Ignore action
- [x] A2: MarkVipOnly action
- [x] P1: Priority ordering

### 8.2 Nice-to-Have (P1)
- [x] V4-V5: Notification targets
- [x] R2-R4: Domain and subject matching
- [x] A3-A6: Routing actions
- [x] C1-C2: Combined scenarios

### 8.3 Extended (P2)
- [ ] Integration tests with database
- [ ] API endpoint tests
- [ ] Performance tests for large rule sets
- [ ] Notification delivery tests

---

## 9. Implementation Status

### 9.1 Completed
- [x] `VipEmail` entity
- [x] `EmailRuleActionType` enum
- [x] VIP fields on `EmailMessage`
- [x] `EmailRuleEvaluationService` with pattern matching
- [x] DTOs for VIP emails and rules
- [x] 51 unit tests (all passing)

### 9.2 Pending
- [ ] DI registration in `Program.cs`
- [ ] Database migration for new tables
- [ ] API controllers for VIP emails and rules
- [ ] Notification service integration
- [ ] Frontend UI

---

## 10. Related Documentation

- [EMAIL_PARSER.md](../01_system/EMAIL_PARSER.md) – Parser specification
- [EMAIL_PARSER_VIP_IMPLEMENTATION_SUMMARY.md](./EMAIL_PARSER_VIP_IMPLEMENTATION_SUMMARY.md) – Implementation summary
- [EMAIL_PARSER_ORDER_CREATION_TESTING.md](./EMAIL_PARSER_ORDER_CREATION_TESTING.md) – Phase 2 testing
- [parser_entities.md](../05_data_model/entities/parser_entities.md) – Data model

