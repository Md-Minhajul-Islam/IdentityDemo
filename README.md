# IdentityDemo API

A complete ASP.NET Core Web API demo for authentication and authorization using:

- ASP.NET Core Identity
- JWT Bearer authentication
- Role-based authorization
- Claim-based policy authorization
- SQL Server + Entity Framework Core

## Features

- User registration and login with hashed passwords
- JWT token generation with user roles and custom claims
- Protected endpoints for authenticated users
- Admin-only endpoints for role and claim management
- Claim policy example (`Department=HR`) for HR dashboard access
- Startup seeding for a default admin user and Admin role

## Tech Stack

- .NET `10.0`
- ASP.NET Core Identity
- Entity Framework Core (SQL Server provider)
- JWT Bearer Authentication

## Project Structure

- `Controllers/AuthController.cs` - registration and login
- `Controllers/UserController.cs` - authenticated/public/user policy endpoints
- `Controllers/AdminController.cs` - admin-only role/claim/user management
- `Services/TokenService.cs` - builds JWT with roles and claims
- `Data/AppDbContext.cs` - Identity EF Core context
- `Models/AppUser.cs` - custom identity user model
- `DTOs/` - request payload models
- `Program.cs` - DI, auth, authorization policy, middleware, seeding

## Prerequisites

- .NET SDK 10+
- SQL Server (local or remote)
- `dotnet-ef` tool (for migrations), optional but recommended:

```bash
dotnet tool install --global dotnet-ef
```

## Configuration

Update `appsettings.json`:

- `ConnectionStrings:DefaultConnection`
- `JwtSettings:Key`
- `JwtSettings:Issuer`
- `JwtSettings:Audience`
- `JwtSettings:ExpiryInDays`

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=IdentityDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "Key": "replace_with_a_strong_secret_key",
    "Issuer": "IdentityDemo",
    "Audience": "IdentityDemo",
    "ExpiryInDays": 7
  }
}
```

## Run Locally

1. Restore dependencies:

```bash
dotnet restore
```

2. Apply migrations:

```bash
dotnet ef database update
```

3. Start the API:

```bash
dotnet run
```

By default, ASP.NET Core prints the local URL (for example `https://localhost:7xxx`).

## Seeded Admin Account

On startup, the app seeds:

- Role: `Admin`
- User email: `admin@demo.com`
- Password: `Password_Admin@123`

Use this account to call admin endpoints first.

## Authentication and Authorization Model

### JWT contents

When a user logs in, `TokenService` issues a JWT containing:

- `nameid` (`ClaimTypes.NameIdentifier`) -> user ID
- `email` (`ClaimTypes.Email`) -> user email
- `role` claims for each assigned role
- all custom claims assigned to the user

### Access rules

- Any endpoint with `[Authorize]` requires a valid Bearer token.
- `AdminController` endpoints require role `Admin`.
- `UserController/hr-dashboard` requires policy `HROnly`.
- `HROnly` policy requires claim: `Department=HR`.

## How to Use Token

After login, send this header to protected endpoints:

```http
Authorization: Bearer <your_jwt_token>
```

## API Endpoints

Base route prefix: `api`

---

### Auth Endpoints

#### POST `api/auth/register`

Creates a new user.

Request body:

```json
{
  "email": "user1@demo.com",
  "password": "Pass@1234"
}
```

Success response:

```json
"User registered successfully!"
```

Common failures:

- `400 Bad Request` for validation or identity errors (weak password, duplicate user, etc.)

#### POST `api/auth/login`

Validates credentials and returns JWT token.

Request body:

```json
{
  "email": "user1@demo.com",
  "password": "Pass@1234"
}
```

Success response:

```json
{
  "token": "<jwt_token>"
}
```

Common failures:

- `401 Unauthorized` for invalid email/password

---

### User Endpoints

#### GET `api/user/public`

Public endpoint, no token required.

Success response:

```json
"Anyone can see this!"
```

#### GET `api/user/profile`

Requires authentication. Returns user ID and email from token claims.

Headers:

```http
Authorization: Bearer <jwt_token>
```

Success response:

```json
{
  "userId": "user-id-guid-or-string",
  "email": "user1@demo.com"
}
```

Common failures:

- `401 Unauthorized` when token is missing/invalid

#### GET `api/user/hr-dashboard`

Requires policy `HROnly`, which means claim `Department=HR` must exist on the user.

Headers:

```http
Authorization: Bearer <jwt_token>
```

Success response:

```json
"Welcome to the HR Dashboard!"
```

Common failures:

- `401 Unauthorized` for missing/invalid token
- `403 Forbidden` when authenticated but missing required claim

---

### Admin Endpoints (Role: Admin required)

All endpoints below require an Admin token.

#### POST `api/admin/create-role`

Creates a new role.

Headers:

```http
Authorization: Bearer <admin_jwt_token>
Content-Type: application/json
```

Request body (raw JSON string):

```json
"HR"
```

Success response:

```json
"Role 'HR' created successfully!"
```

Common failures:

- `400 Bad Request` when role already exists

#### POST `api/admin/assign-role`

Assigns an existing role to a user.

Headers:

```http
Authorization: Bearer <admin_jwt_token>
Content-Type: application/json
```

Request body:

```json
{
  "email": "user1@demo.com",
  "role": "HR"
}
```

Success response:

```json
"Role 'HR' assigned to 'user1@demo.com'"
```

Common failures:

- `404 Not Found` when user does not exist
- `400 Bad Request` when role does not exist

#### POST `api/admin/assign-claim?email={email}&claimType={type}&claimValue={value}`

Assigns or replaces a custom claim for a user.

Headers:

```http
Authorization: Bearer <admin_jwt_token>
```

Example request:

```http
POST /api/admin/assign-claim?email=user1@demo.com&claimType=Department&claimValue=HR
```

Success response:

```json
"Claim 'Department:HR' assigned to 'user1@demo.com'"
```

Common failures:

- `404 Not Found` when user does not exist
- `400 Bad Request` for claim update failure

#### GET `api/admin/users`

Returns all users (ID + email).

Headers:

```http
Authorization: Bearer <admin_jwt_token>
```

Success response:

```json
[
  {
    "id": "user-id-1",
    "email": "admin@demo.com"
  },
  {
    "id": "user-id-2",
    "email": "user1@demo.com"
  }
]
```

---

## Suggested Test Flow

1. Login as seeded admin.
2. Register a normal user.
3. Create role `HR` (admin endpoint).
4. Assign role `HR` to the user.
5. Assign claim `Department=HR` to the user.
6. Login as that user to receive updated token.
7. Call `GET api/user/hr-dashboard` with that user token.

## Security Notes

- Replace demo JWT key with a strong secret in real environments.
- Do not keep seeded admin credentials in production.
- Prefer environment variables or user-secrets for secrets.
- Add HTTPS enforcement and production-grade password/account policies.

## Known Behavior

- `register` currently sets `FullName` to the email value.
- `assign-claim` parameters are taken from query string.
- Role/claim changes affect authorization after user logs in again with a fresh token.
# IdentityDemo
