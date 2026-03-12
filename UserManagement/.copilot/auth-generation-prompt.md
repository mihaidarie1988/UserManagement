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
- Add `Swashbuckle.AspNetCore` version 6.9.0.
- Do NOT add `Microsoft.AspNetCore.OpenApi` — it conflicts with Swashbuckle.

2) JWT authentication setup
- In `Program.cs`, configure:
  - `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`
  - `AddAuthorizationBuilder()` with policies:
    - `RequireReadRole`   -> role `Read`
    - `RequireCreateRole` -> role `Create`
    - `RequireUpdateRole` -> role `Update`
    - `RequireDeleteRole` -> role `Delete`
  - `UseAuthentication()` before `UseAuthorization()`.
- Store issuer/audience/signing key as local constants:
  - Issuer:     `UserManagement.Local`
  - Audience:   `UserManagement.Api`
  - SigningKey: `UserManagement_Local_JWT_Signing_Key_2026!`
- Create a record `JwtTokenOptions(string Issuer, string Audience, string SigningKey)`
  and register it as a singleton.

3) Token issuing endpoint
- Create `AuthController` with route `auth`.
- Add `POST /auth/token` ([AllowAnonymous]) that accepts:
  - `{ "username": "...", "password": "..." }`
- Validate against in-memory local users:
  - `reader  / reader123!`  => `Read`
  - `creator / creator123!` => `Create`
  - `editor  / editor123!`  => `Update`
  - `deleter / deleter123!` => `Delete`
  - `admin   / admin123!`   => `Read, Create, Update, Delete`
- On success return JWT + expiry + roles; on failure return 401.

4) Authorization policies and custom attributes
- Add `AuthorizationPolicies` static class with role and policy name constants.
- Add custom authorize attributes in `RoleAuthorizeAttributes`:
  - `[ReadAccess]`, `[CreateAccess]`, `[UpdateAccess]`, `[DeleteAccess]`
  - Each inherits `AuthorizeAttribute` and sets its `Policy` from `AuthorizationPolicies`.

5) Apply auth to `UserManagementController` endpoints
- `GET  /UserManagement/users`        -> `[ReadAccess]`
- `GET  /UserManagement/users/{id}`   -> `[ReadAccess]`
- `POST /UserManagement/users`        -> `[CreateAccess]`
- `PUT  /UserManagement/users/{id}`   -> `[UpdateAccess]`
- `PATCH /UserManagement/users/{id}`  -> `[UpdateAccess]`
- `DELETE /UserManagement/users/{id}` -> `[DeleteAccess]`

6) Swagger with Bearer JWT auth
- Keep Swagger enabled in development.
- In `AddSwaggerGen`:
  - Call `AddSecurityDefinition("Bearer", ...)` with:
    - Type = Http, Scheme = bearer, BearerFormat = JWT.
  - Create `BearerSecurityOperationFilter : IOperationFilter` in the
    `Authorization` folder:
    - Detect `[Authorize]` (or derived attributes) on the action or controller.
    - Add an `OpenApiSecurityRequirement` with a `Reference` to the `Bearer`
      scheme using `ReferenceType.SecurityScheme`.
    - Use `using Microsoft.OpenApi.Models;` (NOT `Microsoft.OpenApi`).
  - Register it with `options.OperationFilter<BearerSecurityOperationFilter>()`.
- In Swagger UI: paste your JWT token (without `Bearer ` prefix) in the
  Authorize dialog — Swagger adds the prefix automatically.

7) Cleanup and validation
- Avoid any API-key auth logic or filters.
- Keep minimal changes to existing controller endpoint behavior.
- Ensure project builds successfully.

8) README update
- Document:
  - `POST /auth/token`
  - Local users, roles, and role rules (Read/Create/Update/Delete)
  - Endpoint-to-role mapping
  - curl examples for token + all endpoints with Bearer token