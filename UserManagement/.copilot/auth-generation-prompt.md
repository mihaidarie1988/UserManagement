I have an existing ASP.NET Core Web API project (.NET 10) with CRUD endpoints in project UserManagement-workshop

Let's add authentication and authorization to UserManagement-workshop project:

in `UserManagementController`.
Please add local JWT authentication + role-based authorization to match this exact behavior:

GOAL
- Use local JWT auth (no Azure/Entra/external identity provider).
- Add token endpoint: `POST /auth/token`.
- Protect API endpoints by role using custom authorization attributes and policies.

REQUIREMENTS

1) NuGet packages
- Add `Microsoft.AspNetCore.Authentication.JwtBearer` (match runtime version).
- Do NOT add `Microsoft.AspNetCore.OpenApi` — it conflicts with Swashbuckle.

2) Authorization constants — create `Authorization/AuthorizationPolicies.cs`
- Add `AuthorizationPolicies` static class with role and policy name constants:
  - `ReadRole = "Read"`, `CreateRole = "Create"`, `UpdateRole = "Update"`, `DeleteRole = "Delete"`
  - `ReadPolicy = "RequireReadRole"`, `CreatePolicy = "RequireCreateRole"`,
    `UpdatePolicy = "RequireUpdateRole"`, `DeletePolicy = "RequireDeleteRole"`

3) JWT options record — create `Authorization/JwtTokenOptions.cs`
- Create a record `JwtTokenOptions(string Issuer, string Audience, string SigningKey)`.

4) Custom authorize attributes — create `Authorization/RoleAuthorizeAttributes.cs`
- Add `[ReadAccess]`, `[CreateAccess]`, `[UpdateAccess]`, `[DeleteAccess]` attributes.
- Each inherits `AuthorizeAttribute` and sets its `Policy` from `AuthorizationPolicies`.

5) Swagger operation filter — create `Authorization/BearerSecurityOperationFilter.cs`
- Implement `BearerSecurityOperationFilter : IOperationFilter`.
- Detect `[Authorize]` (or derived attributes) on the action or its declaring type.
- If found, add an `OpenApiSecurityRequirement` referencing the `Bearer` scheme
  (`ReferenceType.SecurityScheme`, Id = `"Bearer"`).
- Use `using Microsoft.OpenApi.Models;` (NOT `Microsoft.OpenApi`).

6) JWT authentication setup — update `Program.cs`
- All types from steps 2–5 now exist; reference them here.
- Store issuer/audience/signing key as local constants:
  - Issuer:     `UserManagement.Local`
  - Audience:   `UserManagement.Api`
  - SigningKey: `UserManagement_Local_JWT_Signing_Key_2026!`
- Register `JwtTokenOptions` as a singleton (these are the values `AuthController` will inject).
- Configure:
  - `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`
  - `AddAuthorizationBuilder()` with policies from `AuthorizationPolicies`:
    - `RequireReadRole`   -> role `Read`
    - `RequireCreateRole` -> role `Create`
    - `RequireUpdateRole` -> role `Update`
    - `RequireDeleteRole` -> role `Delete`
  - `UseAuthentication()` before `UseAuthorization()`.
- In `AddSwaggerGen`:
  - Call `AddSecurityDefinition("Bearer", ...)` with:
    - Type = Http, Scheme = bearer, BearerFormat = JWT.
  - Register `options.OperationFilter<BearerSecurityOperationFilter>()`.
- Keep Swagger enabled in development.

7) Token issuing endpoint — create `Controllers/AuthController.cs`
- The JWT constants are now registered (step 6); inject `JwtTokenOptions` via primary constructor.
- Add `AuthController` with route `auth`.
- Add `POST /auth/token` ([AllowAnonymous]) that accepts:
  - `{ "username": "...", "password": "..." }`
- Validate against in-memory local users:
  - `reader  / reader123!`  => `Read`
  - `creator / creator123!` => `Create`
  - `editor  / editor123!`  => `Update`
  - `deleter / deleter123!` => `Delete`
  - `admin   / admin123!`   => `Read, Create, Update, Delete`
- On success return JWT + expiry + roles; on failure return 401.

8) Apply auth to `UserManagementController` endpoints
- `GET  /UserManagement/users`        -> `[ReadAccess]`
- `GET  /UserManagement/users/{id}`   -> `[ReadAccess]`
- `POST /UserManagement/users`        -> `[CreateAccess]`
- `PUT  /UserManagement/users/{id}`   -> `[UpdateAccess]`
- `PATCH /UserManagement/users/{id}`  -> `[UpdateAccess]`
- `DELETE /UserManagement/users/{id}` -> `[DeleteAccess]`

9) Cleanup and validation
- Avoid any API-key auth logic or filters.
- Keep minimal changes to existing controller endpoint behavior.
- Ensure project builds successfully.

10) README update
- Document:
  - `POST /auth/token`
  - Local users, roles, and role rules (Read/Create/Update/Delete)
  - Endpoint-to-role mapping
  - curl examples for token + all endpoints with Bearer token
