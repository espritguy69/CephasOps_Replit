# Auth – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Authentication & Authorization module

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    AUTHENTICATION & AUTHORIZATION SYSTEM                 │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   AUTHENTICATION       │      │   AUTHORIZATION       │
        │  (Login, Token)        │      │  (Roles, Permissions) │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Login                │      │ • Role-based access   │
        │ • JWT Token Generation │      │ • Permission checks    │
        │ • Refresh Token        │      │ • Department context   │
        │ • Password Verification│      │ • Company context      │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   USER MANAGEMENT      │      │   SESSION MANAGEMENT  │
        │  (User CRUD)           │      │  (Token Refresh)      │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Login to API Access

```
[STEP 1: USER LOGIN]
         |
         v
┌────────────────────────────────────────┐
│ LOGIN REQUEST                            │
│ POST /api/auth/login                     │
└────────────────────────────────────────┘
         |
         v
LoginRequestDto {
  Email: "user@example.com"
  Password: "password123"
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE USER                            │
│ AuthService.LoginAsync()                 │
└────────────────────────────────────────┘
         |
         v
[Query User by Email]
  User.find(Email = "user@example.com")
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Return 401 Unauthorized]
   |       "Invalid email or password"
   |
   v
[Check User Status]
         |
    ┌────┴────┐
    |         |
    v         v
[ACTIVE] [INACTIVE]
   |            |
   |            v
   |       [Return 401 Unauthorized]
   |       "Invalid email or password"
   |
   v
[Verify Password]
  DatabaseSeeder.VerifyPassword(
    request.Password,
    user.PasswordHash
  )
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |            |
   |            v
   |       [Return 401 Unauthorized]
   |       "Invalid email or password"
   |
   v
[STEP 2: LOAD USER ROLES]
         |
         v
┌────────────────────────────────────────┐
│ GET USER ROLES                           │
└────────────────────────────────────────┘
         |
         v
[Query UserRoles]
  UserRole.find(UserId = userId)
    .Include(Role)
         |
         v
Roles: ["Admin", "Manager"]
         |
         v
[STEP 3: GENERATE JWT TOKEN]
         |
         v
┌────────────────────────────────────────┐
│ GENERATE JWT TOKEN                       │
│ AuthService.GenerateJwtToken()          │
└────────────────────────────────────────┘
         |
         v
JWT Claims {
  sub: userId (Guid)
  email: "user@example.com"
  roles: ["Admin", "Manager"]
  companyId: null (single-company mode)
  exp: expiresAt (DateTime)
  iat: issuedAt (DateTime)
}
         |
         v
[Sign Token with Secret Key]
  JwtSecurityToken {
    SigningCredentials: HS256
    SecretKey: from appsettings.json
    Expires: 1 hour (default)
  }
         |
         v
[Encode Token]
  accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
         |
         v
[STEP 4: GENERATE REFRESH TOKEN]
         |
         v
┌────────────────────────────────────────┐
│ GENERATE REFRESH TOKEN                   │
│ AuthService.GenerateRefreshToken()      │
└────────────────────────────────────────┘
         |
         v
[Generate Random Token]
  refreshTokenValue = RandomString(64)
         |
         v
[Hash Token]
  refreshTokenHash = SHA256(refreshTokenValue)
         |
         v
[STEP 5: REVOKE OLD REFRESH TOKENS]
         |
         v
┌────────────────────────────────────────┐
│ REVOKE EXISTING TOKENS                    │
└────────────────────────────────────────┘
         |
         v
[Query Active Refresh Tokens]
  RefreshToken.find(
    UserId = userId
    IsRevoked = false
    ExpiresAt > Now
  )
         |
         v
[Revoke All]
  For each token:
    token.IsRevoked = true
    token.RevokedAt = DateTime.UtcNow
         |
         v
[STEP 6: STORE REFRESH TOKEN]
         |
         v
┌────────────────────────────────────────┐
│ CREATE REFRESH TOKEN RECORD               │
└────────────────────────────────────────┘
         |
         v
RefreshToken {
  Id: Guid.NewGuid()
  UserId: userId
  TokenHash: refreshTokenHash
  ExpiresAt: DateTime.UtcNow.AddDays(30)
  CreatedAt: DateTime.UtcNow
  IsRevoked: false
}
         |
         v
[Save to Database]
  _context.RefreshTokens.Add(refreshToken)
  await _context.SaveChangesAsync()
         |
         v
[STEP 7: RETURN LOGIN RESPONSE]
         |
         v
┌────────────────────────────────────────┐
│ LOGIN RESPONSE                           │
└────────────────────────────────────────┘
         |
         v
LoginResponseDto {
  AccessToken: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  RefreshToken: "abc123def456..."
  ExpiresAt: 2025-12-12 14:00:00
  User: {
    Id: userId
    Name: "John Doe"
    Email: "user@example.com"
    Phone: "0123456789"
    Roles: ["Admin", "Manager"]
  }
}
         |
         v
[Client Stores Tokens]
  localStorage.setItem("accessToken", accessToken)
  localStorage.setItem("refreshToken", refreshToken)
         |
         v
[STEP 8: API REQUEST WITH TOKEN]
         |
         v
[Client Makes API Request]
  GET /api/orders
  Headers: {
    Authorization: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
         |
         v
┌────────────────────────────────────────┐
│ JWT AUTHENTICATION MIDDLEWARE             │
└────────────────────────────────────────┘
         |
         v
[Extract Token from Header]
  Authorization: "Bearer {token}"
         |
         v
[Validate Token]
  JwtSecurityTokenHandler.ValidateToken(
    token,
    validationParameters
  )
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |            |
   |            v
   |       [Return 401 Unauthorized]
   |
   v
[Extract Claims]
  ClaimsPrincipal {
    UserId: from "sub" claim
    Email: from "email" claim
    Roles: from "roles" claim
  }
         |
         v
[Set Current User Context]
  ICurrentUserService {
    UserId: userId
    Email: email
    Roles: roles
    CompanyId: Guid.Empty (single-company mode)
    IsSuperAdmin: roles.Contains("SuperAdmin")
  }
         |
         v
[Continue to Controller]
         |
         v
[STEP 9: AUTHORIZATION CHECK]
         |
         v
[Controller Action]
  [Authorize(Roles = "Admin")]
  public async Task<ActionResult> GetOrders()
         |
         v
┌────────────────────────────────────────┐
│ CHECK USER ROLES                         │
└────────────────────────────────────────┘
         |
         v
[User Has Required Role?]
  userRoles.Contains("Admin")
         |
    ┌────┴────┐
    |         |
    v         v
[YES] [NO]
   |            |
   |            v
   |       [Return 403 Forbidden]
   |
   v
[Execute Action]
  return await _orderService.GetOrdersAsync()
```

---

## Refresh Token Workflow

```
[Access Token Expired]
  Token ExpiresAt: 2025-12-12 14:00:00
  Current Time: 2025-12-12 14:05:00
         |
         v
┌────────────────────────────────────────┐
│ REFRESH TOKEN REQUEST                    │
│ POST /api/auth/refresh                   │
└────────────────────────────────────────┘
         |
         v
RefreshTokenRequestDto {
  RefreshToken: "abc123def456..."
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE REFRESH TOKEN                   │
│ AuthService.RefreshTokenAsync()          │
└────────────────────────────────────────┘
         |
         v
[Hash Provided Token]
  refreshTokenHash = SHA256(refreshToken)
         |
         v
[Query Refresh Token]
  RefreshToken.find(
    TokenHash = refreshTokenHash
    IsRevoked = false
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Return 401 Unauthorized]
   |       "Invalid or expired refresh token"
   |
   v
[Check Expiration]
  token.ExpiresAt > DateTime.UtcNow
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [EXPIRED]
   |            |
   |            v
   |       [Return 401 Unauthorized]
   |       "Invalid or expired refresh token"
   |
   v
[Check User Status]
  token.User.IsActive
         |
    ┌────┴────┐
    |         |
    v         v
[ACTIVE] [INACTIVE]
   |            |
   |            v
   |       [Return 401 Unauthorized]
   |       "User not found or inactive"
   |
   v
[Revoke Old Token]
  token.IsRevoked = true
  token.RevokedAt = DateTime.UtcNow
         |
         v
[Generate New Tokens]
         |
         v
[New JWT Token]
  accessToken = GenerateJwtToken(...)
         |
         v
[New Refresh Token]
  newRefreshTokenValue = GenerateRefreshToken()
  newRefreshTokenHash = SHA256(newRefreshTokenValue)
         |
         v
[Store New Refresh Token]
  RefreshToken {
    Id: Guid.NewGuid()
    UserId: token.UserId
    TokenHash: newRefreshTokenHash
    ExpiresAt: DateTime.UtcNow.AddDays(30)
  }
         |
         v
[Save to Database]
  _context.RefreshTokens.Add(newRefreshToken)
  await _context.SaveChangesAsync()
         |
         v
[Return New Tokens]
  LoginResponseDto {
    AccessToken: newAccessToken
    RefreshToken: newRefreshTokenValue
    ExpiresAt: newExpiresAt
    User: userDto
  }
```

---

## Get Current User Workflow

```
[API Request: Get Current User]
  GET /api/auth/me
  Headers: {
    Authorization: "Bearer {token}"
  }
         |
         v
┌────────────────────────────────────────┐
│ EXTRACT USER ID FROM TOKEN                │
│ (JWT Middleware)                         │
└────────────────────────────────────────┘
         |
         v
[Get UserId from Claims]
  userId = ClaimsPrincipal.FindFirst("sub")
         |
         v
┌────────────────────────────────────────┐
│ GET USER DETAILS                         │
│ AuthService.GetCurrentUserAsync()        │
└────────────────────────────────────────┘
         |
         v
[Query User]
  User.find(Id = userId)
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Return 401 Unauthorized]
   |
   v
[Query User Roles]
  UserRole.find(UserId = userId)
    .Include(Role)
         |
         v
Roles: ["Admin", "Manager"]
         |
         v
[Build User DTO]
  UserDto {
    Id: userId
    Name: user.Name
    Email: user.Email
    Phone: user.Phone
    Roles: ["Admin", "Manager"]
  }
         |
         v
[Return User DTO]
  return UserDto
```

---

## Entities Involved

### User Entity
```
User
├── Id (Guid)
├── Name (string)
├── Email (string, unique)
├── Phone (string?)
├── PasswordHash (string)
├── IsActive (bool)
├── CreatedAt (DateTime)
└── UpdatedAt (DateTime)
```

### Role Entity
```
Role
├── Id (Guid)
├── Name (string, unique)
├── Description (string?)
├── IsActive (bool)
└── CreatedAt, UpdatedAt
```

### UserRole Entity
```
UserRole
├── Id (Guid)
├── UserId (Guid)
├── RoleId (Guid)
└── CreatedAt (DateTime)
```

### RefreshToken Entity
```
RefreshToken
├── Id (Guid)
├── UserId (Guid)
├── TokenHash (string)
├── ExpiresAt (DateTime)
├── IsRevoked (bool)
├── RevokedAt (DateTime?)
└── CreatedAt (DateTime)
```

---

## API Endpoints Involved

### Authentication
- `POST /api/auth/login` - User login
  - Request: `LoginRequestDto { Email, Password }`
  - Response: `LoginResponseDto { AccessToken, RefreshToken, ExpiresAt, User }`

- `POST /api/auth/refresh` - Refresh access token
  - Request: `RefreshTokenRequestDto { RefreshToken }`
  - Response: `LoginResponseDto { AccessToken, RefreshToken, ExpiresAt, User }`

- `GET /api/auth/me` - Get current user
  - Response: `UserDto { Id, Name, Email, Phone, Roles }`

- `POST /api/auth/logout` - Logout (revoke refresh token)
  - Request: `RefreshTokenRequestDto { RefreshToken }`

---

## Module Rules & Validations

### Login Rules
- Email must exist in database
- User must be active (`IsActive = true`)
- Password must match stored hash
- Password verification uses BCrypt/Argon2

### Token Rules
- JWT access token expires in 1 hour (configurable)
- Refresh token expires in 30 days
- Only one active refresh token per user (old ones revoked on new login)
- Token hash stored (not plain text) for security

### Authorization Rules
- `[Authorize]` attribute required for protected endpoints
- Role-based authorization: `[Authorize(Roles = "Admin")]`
- SuperAdmin role bypasses company/department filters
- Single-company mode: `CompanyId` is `Guid.Empty` for all users

### Security Rules
- Passwords never stored in plain text
- Refresh tokens hashed before storage
- JWT signed with HS256 algorithm
- Secret key from configuration (not hardcoded)
- Token validation includes expiration check

---

## Integration Points

### API Middleware
- JWT authentication middleware validates tokens on every request
- Sets `ICurrentUserService` context for downstream services
- Handles 401 Unauthorized for invalid/expired tokens

### User Management
- User creation/update handled by Users module
- Role assignment handled by RBAC module
- User activation/deactivation affects login ability

### Department Context
- Department context set separately (not in auth flow)
- Department filtering applied in API client layer
- Department membership affects data visibility

### Notifications
- Login events can trigger notifications (optional)
- Failed login attempts can be logged/notified
- Token expiration warnings (optional)

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/rbac/OVERVIEW.md` - RBAC module overview
- `docs/02_modules/users/` - User management

