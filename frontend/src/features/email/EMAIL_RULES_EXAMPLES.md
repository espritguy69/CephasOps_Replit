# Email Rules - Example Rules

This document provides example email rules that you can create using the Email Rules page.

## Example Rules to Create

### 1. Block Spam/Unsubscribe Emails (High Priority)
```
Rule Name: Block Unsubscribe Emails
Description: Automatically ignore unsubscribe and spam emails
Sender Pattern: (leave empty)
Domain Pattern: (leave empty)
Subject Pattern: unsubscribe|opt-out|remove|spam
Action: Ignore
Priority: 200
Is VIP: No
Active: Yes
```

### 2. Block Auto-Replies (High Priority)
```
Rule Name: Block Auto-Replies
Description: Ignore automated reply emails
Sender Pattern: noreply@*|no-reply@*|donotreply@*
Domain Pattern: (leave empty)
Subject Pattern: (leave empty)
Action: Ignore
Priority: 200
Is VIP: No
Active: Yes
```

### 3. TIME Partner Orders (Main Rule)
```
Rule Name: TIME FTTH/FTTO Orders
Description: Process TIME partner order emails automatically
Sender Pattern: *@time.com.my
Domain Pattern: (leave empty)
Subject Pattern: FTTH|FTTO|Activation|Modification
Action: Process
Priority: 100
Is VIP: No
Active: Yes
```

### 4. Celcom Partner Orders
```
Rule Name: Celcom Orders
Description: Process Celcom partner order emails
Sender Pattern: *@celcom.com.my
Domain Pattern: (leave empty)
Subject Pattern: Order|Activation|Installation
Action: Process
Priority: 100
Is VIP: No
Active: Yes
```

### 5. VIP CEO Emails
```
Rule Name: CEO Emails - VIP
Description: Mark CEO emails as VIP for priority handling
Sender Pattern: ceo@*|director@*
Domain Pattern: (leave empty)
Subject Pattern: (leave empty)
Action: MarkVipOnly
Priority: 150
Is VIP: Yes
Active: Yes
```

### 6. Route GPON Orders to Department
```
Rule Name: GPON Orders to GPON Department
Description: Route GPON-related orders to GPON department
Sender Pattern: (leave empty)
Domain Pattern: (leave empty)
Subject Pattern: GPON|NWO|CWO
Action: RouteToDepartment
Target Department ID: [Enter your GPON department ID]
Priority: 80
Is VIP: No
Active: Yes
```

### 7. Route Assurance Tickets
```
Rule Name: Assurance Tickets to Support
Description: Route assurance/complaint emails to support department
Sender Pattern: (leave empty)
Domain Pattern: (leave empty)
Subject Pattern: Assurance|Complaint|Issue|Problem
Action: RouteToDepartment
Target Department ID: [Enter your Support department ID]
Priority: 90
Is VIP: No
Active: Yes
```

### 8. VIP + Route Critical Orders
```
Rule Name: Critical Orders - VIP + Route
Description: Mark critical orders as VIP and route to management
Sender Pattern: critical@*|urgent@*
Domain Pattern: (leave empty)
Subject Pattern: URGENT|CRITICAL|PRIORITY
Action: MarkVipAndRouteToDepartment
Target Department ID: [Enter your Management department ID]
Priority: 120
Is VIP: Yes
Active: Yes
```

## Priority Guidelines

- **200+**: Spam filters, auto-replies (evaluate first to block)
- **150-199**: VIP rules, critical routing
- **100-149**: Main business rules (partner orders)
- **50-99**: Department routing rules
- **0-49**: Default/fallback rules

## Pattern Matching Tips

### Sender Patterns
- `*@time.com.my` - Matches any email from time.com.my domain
- `noreply@*` - Matches any noreply address from any domain
- `director*@company.com` - Matches director1@, director2@, etc.

### Domain Patterns
- `@time.com.my` - Matches any email from this domain
- `@celcom.com.my` - Matches any email from this domain

### Subject Patterns
- `FTTH|FTTO` - Matches if subject contains "FTTH" OR "FTTO"
- `Activation|Installation|Modification` - Multiple keywords with OR
- Case-insensitive matching

## Action Types Explained

1. **Process**: Normal processing - create order from email
2. **Ignore**: Skip this email completely (no processing)
3. **MarkVipOnly**: Mark as VIP but don't route
4. **RouteToDepartment**: Route to a specific department
5. **RouteToUser**: Route to a specific user
6. **MarkVipAndRouteToDepartment**: Mark as VIP AND route to department
7. **MarkVipAndRouteToUser**: Mark as VIP AND route to user

## Testing Your Rules

1. Create a test rule with low priority
2. Send a test email that matches the pattern
3. Check if the rule is applied correctly
4. Adjust priority or patterns as needed
5. Activate the rule once tested

