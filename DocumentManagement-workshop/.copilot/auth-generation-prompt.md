I have an existing ASP.NET Core Web API project (.NET 10) named DocumentManagement.Workshop with CRUD endpoints for document management.

The project already contains:
- `Models/Document.cs` — `Document(int Id, string Title, string Content)`, `WriteDocumentRequest`, `PatchDocumentRequest`
- `Services/DocumentStore.cs` — singleton in-memory store with 4 seeded documents (no auth context yet)
- `Controllers/DocumentController.cs` — full CRUD, no auth attributes, no ownership logic
- `Program.cs` — basic setup with Swashbuckle; no auth middleware registered

Let's add authentication and authorization on top of this:

GOAL
- Use local JWT auth (no Azure/Entra/external identity provider).
- Add token endpoint: `POST /auth/token`.
- Protect API endpoints using permission-based policies backed by named roles.
  Permissions are fine-grained (e.g. `document:read`); roles are meaningful names
  (e.g. `Manager`) that grant a set of permissions. The policy checks a `permission`
  claim in the JWT — NOT the role claim directly.
- Enforce ownership: each authenticated user can only read/edit/delete documents they
  created; `Admin` bypasses ownership and can access all documents.

AUTHORIZATION DESIGN

Permissions → what an operation requires (checked by the policy).
Roles       → what a user is assigned (determines which permissions they get).
Policies    → bridge: each policy requires a specific permission claim in the JWT.

Role-to-permission mapping:
  Viewer  → document:read
  Editor  → document:read, document:create, document:update
  Manager → document:read, document:create, document:update, document:delete
  Admin   → document:read, document:create, document:update, document:delete
            (also bypasses document ownership checks)

REQUIREMENTS

1) NuGet packages
- Add `Microsoft.AspNetCore.Authentication.JwtBearer` (match runtime version).
- Do NOT add `Microsoft.AspNetCore.OpenApi` — it conflicts with Swashbuckle.

2) Permission constants — create `Authorization/AppPermissions.cs`
- Add `AppPermissions` static class:
  ```csharp
  public const string Read   = "document:read";
  public const string Create = "document:create";
  public const string Update = "document:update";
  public const string Delete = "document:delete";
  ```

3) Role constants — create `Authorization/AppRoles.cs`
- Add `AppRoles` static class with named role constants and a permission resolver:
  ```csharp
  public const string Viewer  = "Viewer";
  public const string Editor  = "Editor";
  public const string Manager = "Manager";
  public const string Admin   = "Admin";

  public static IEnumerable<string> GetPermissions(string role) => role switch
  {
      Viewer  => [AppPermissions.Read],
      Editor  => [AppPermissions.Read, AppPermissions.Create, AppPermissions.Update],
      Manager => [AppPermissions.Read, AppPermissions.Create, AppPermissions.Update, AppPermissions.Delete],
      Admin   => [AppPermissions.Read, AppPermissions.Create, AppPermissions.Update, AppPermissions.Delete],
      _       => []
  };
  ```

4) Policy name constants — create `Authorization/AuthorizationPolicies.cs`
- Add `AuthorizationPolicies` static class with policy name constants only
  (no role or permission values here — those live in `AppRoles` and `AppPermissions`):
  ```csharp
  public const string ReadPolicy   = "RequireReadPermission";
  public const string CreatePolicy = "RequireCreatePermission";
  public const string UpdatePolicy = "RequireUpdatePermission";
  public const string DeletePolicy = "RequireDeletePermission";
  ```

5) JWT options record — create `Authorization/JwtTokenOptions.cs`
- Create a record `JwtTokenOptions(string Issuer, string Audience, string SigningKey)`.

6) Custom authorize attributes — create `Authorization/RoleAuthorizeAttributes.cs`
- Add `[ReadAccess]`, `[CreateAccess]`, `[UpdateAccess]`, `[DeleteAccess]` attributes.
- Each must be a `sealed` class that inherits `AuthorizeAttribute`.
- Use **explicit constructors with a `Policy =` assignment** — do NOT use primary constructor
  syntax (no `AuthorizeAttribute(...)` base call in the class declaration):
  ```csharp
  public sealed class ReadAccessAttribute : AuthorizeAttribute
  {
      public ReadAccessAttribute() { Policy = AuthorizationPolicies.ReadPolicy; }
  }

  public sealed class CreateAccessAttribute : AuthorizeAttribute
  {
      public CreateAccessAttribute() { Policy = AuthorizationPolicies.CreatePolicy; }
  }

  public sealed class UpdateAccessAttribute : AuthorizeAttribute
  {
      public UpdateAccessAttribute() { Policy = AuthorizationPolicies.UpdatePolicy; }
  }

  public sealed class DeleteAccessAttribute : AuthorizeAttribute
  {
      public DeleteAccessAttribute() { Policy = AuthorizationPolicies.DeletePolicy; }
  }
  ```


---------- end of part 1 ------------

7) JWT authentication setup — update `Program.cs`
- All types from steps 2–7 now exist; reference them here.
- Do NOT create, modify, or read from `appsettings.json` (or any `appsettings.*.json`) for
  JWT settings. Do NOT use `IConfiguration` / `builder.Configuration` to bind them.
  The values are hardcoded as `const string` locals directly in `Program.cs`.
- Store issuer/audience/signing key as local constants:
  - Issuer:     `DocumentManagement.Local`
  - Audience:   `DocumentManagement.Api`
  - SigningKey: `DocumentManagement_Local_JWT_Signing_Key_2026!`
- Register `JwtTokenOptions` as a singleton (injected by `AuthController`).
- Configure:
  - `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`
  - `AddAuthorizationBuilder()` with policies that check a `permission` claim:
    ```csharp
    .AddPolicy(AuthorizationPolicies.ReadPolicy,   policy => policy.RequireClaim("permission", AppPermissions.Read))
    .AddPolicy(AuthorizationPolicies.CreatePolicy, policy => policy.RequireClaim("permission", AppPermissions.Create))
    .AddPolicy(AuthorizationPolicies.UpdatePolicy, policy => policy.RequireClaim("permission", AppPermissions.Update))
    .AddPolicy(AuthorizationPolicies.DeletePolicy, policy => policy.RequireClaim("permission", AppPermissions.Delete))
    ```
  - `UseAuthentication()` before `UseAuthorization()`.
- In `AddSwaggerGen`:
  - Call `AddSecurityDefinition("Bearer", ...)` with Type = Http, Scheme = bearer, BearerFormat = JWT.
  - Register `options.OperationFilter<BearerSecurityOperationFilter>()`.
- Keep Swagger enabled in development.

------ end of part 2 ------------

8) Token issuing endpoint — create `Controllers/AuthController.cs`
- Do NOT create a separate `TokenService` or any other helper class for token generation.
  All JWT creation logic lives directly inside `AuthController`.
- Inject `JwtTokenOptions` via primary constructor.
- Add `AuthController` with route `auth`.
- Add `POST /auth/token` ([AllowAnonymous]) that accepts `{ "username": "...", "password": "..." }`.
- Validate against in-memory local users (each user has one role):
  - `alice   / alice123!`   => role `Manager`  (demonstrates ownership, full CRUD)
  - `bob     / bob123!`     => role `Manager`  (demonstrates ownership, full CRUD)
  - `charlie / charlie123!` => role `Editor`   (no Delete — demonstrates role restriction)
  - `admin   / admin123!`   => role `Admin`    (bypasses ownership, full CRUD)
- On success return JWT + expiry + roles array; on failure return 401.
- JWT must include:
  - `sub` claim = username
  - `ClaimTypes.Role` claim(s) = assigned role(s)
  - `permission` claim(s) = all permissions derived from the role(s) via `AppRoles.GetPermissions`
    (deduplicated; use `SelectMany(...).Distinct()`)
  - Expiry = 60 min, algorithm = HS256

------ end of part 3 ------------

9) Add ownership field — update `Models/Document.cs`
- Add `string CreatedBy` as the last field of the `Document` record:
  `public record Document(int Id, string Title, string Content, string CreatedBy);`
- `WriteDocumentRequest` and `PatchDocumentRequest` remain unchanged (`CreatedBy` is stamped server-side).

10) Seed ownership data — update `Services/DocumentStore.cs`
- Add the `CreatedBy` argument to all 4 seeded documents:
  - Doc 1 "Project Proposal"        → `"alice"`
  - Doc 2 "Meeting Notes"           → `"alice"`
  - Doc 3 "Budget Overview"         → `"bob"`
  - Doc 4 "Technical Specification" → `"charlie"`
- Add `GetByOwner(string username)` returning documents where `CreatedBy == username`.
- Update `NextId()` to use `Max`-based calculation instead of a counter field:
  `public int NextId() => _documents.Count == 0 ? 1 : _documents.Max(d => d.Id) + 1;`

----- end of part 4 ------------

11) Ownership filter — create `Authorization/DocumentOwnershipFilter.cs`
- Create `DocumentOwnershipAttribute : TypeFilterAttribute(typeof(DocumentOwnershipFilter))`.
  Using `TypeFilterAttribute` lets the filter receive `DocumentStore` from the DI container.
- Create `DocumentOwnershipFilter(DocumentStore store) : IAsyncAuthorizationFilter`.
- In `OnAuthorizationAsync`:
  - Read `id` from `context.RouteData.Values["id"]`; return early if not present or not an int.
  - If `user.IsInRole(AppRoles.Admin)` → pass through (admin bypasses ownership).
  - If `store.FindById(id) is null` → pass through (let the action return 404).
  - If `document.CreatedBy != user.FindFirstValue(ClaimTypes.NameIdentifier)` →
    set `context.Result = new ForbidResult()`.


----- end of part 5 ------------

12) Apply auth and ownership — update `Controllers/DocumentController.cs`
- `DocumentStore` is already injected via primary constructor — keep it.
- Add `using System.Security.Claims;` and `using Authorization;`.
- Do NOT add any private helper properties or methods for auth concerns (e.g. `CurrentUser`,
  `IsAdmin`, `CanAccess`). Authorization and ownership *enforcement* belongs exclusively in
  `DocumentOwnershipFilter` (step 11) — the controller must not make any `Forbid()`/access
  decisions itself.
- The two exceptions below are **identity reads for business logic** (not authorization checks)
  and are the only places `HttpContext.User` appears in the controller:
- Permission-to-endpoint mapping (apply the corresponding attribute to each action):
  - `GET  /Document`          → `[ReadAccess]`
  - `GET  /Document/{id}`     → `[ReadAccess]`   + `[DocumentOwnership]`
  - `POST /Document`          → `[CreateAccess]`
  - `PUT  /Document/{id}`     → `[UpdateAccess]` + `[DocumentOwnership]`
  - `PATCH /Document/{id}`    → `[UpdateAccess]` + `[DocumentOwnership]`
  - `DELETE /Document/{id}`   → `[DeleteAccess]` + `[DocumentOwnership]`
- `POST /Document`: stamp `CreatedBy = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!`.
- `GET /Document`: admin sees all; others see only their own:
  ```csharp
  var docs = HttpContext.User.IsInRole(AppRoles.Admin)
      ? store.GetAll()
      : store.GetByOwner(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  ```
- PUT/PATCH: read the existing record first and preserve its `CreatedBy` when rebuilding.
- Use `HttpContext.User` (not `User`) to avoid naming collisions.


--------- end of part 6 ------------

13) Swagger operation filter — create `Authorization/BearerSecurityOperationFilter.cs`
- Implement `BearerSecurityOperationFilter : IOperationFilter`.
- Detect `[Authorize]` (or derived attributes) on the action or its declaring type.
- If found, add an `OpenApiSecurityRequirement` referencing the `Bearer` scheme
  (`ReferenceType.SecurityScheme`, Id = `"Bearer"`).
- Use `using Microsoft.OpenApi.Models;` (NOT `Microsoft.OpenApi`).

14) Cleanup and validation
- Avoid any API-key auth logic or filters.
- Keep minimal changes to existing controller endpoint behavior.
- Ensure project builds successfully.

15) README update
- Document:
  - `POST /auth/token`
  - Local users table: username / password / role / what they demonstrate
  - Role-to-permission mapping table (Viewer / Editor / Manager / Admin)
  - Seeded documents and their owners
  - Ownership rules and admin bypass
  - Endpoint-to-permission mapping table
  - curl examples for token + all endpoints with Bearer token

16) Bruno collection updates — `.bruno/DocumentManagement.Workshop/`
The workshop Bruno collection currently has 6 request files (no auth). Auth needs to be added
throughout. Use the structure of the existing `.bru` files as the format reference.

a) Create `01-auth-token.bru` — new token request (anonymous, no Bearer):
   ```
   meta {
     name: Auth - Get JWT Token
     type: http
     seq: 1
   }

   post {
     url: {{baseUrl}}/auth/token
     body: json
     auth: none
   }

   headers {
     Content-Type: application/json
   }

   body:json {
     {
       "username": "admin",
       "password": "admin123!"
     }
   }
   ```

b) Renumber all 6 existing request files — both the filename prefix AND the `seq` value
   inside each file must be incremented by 1 to make room for the new `01-auth-token.bru`:
   - `01-create-document.bru`      → `02-create-document.bru`      (seq: 1 → seq: 2)
   - `02-get-documents.bru`        → `03-get-documents.bru`        (seq: 2 → seq: 3)
   - `03-get-document-by-id.bru`   → `04-get-document-by-id.bru`   (seq: 3 → seq: 4)
   - `04-update-document-put.bru`  → `05-update-document-put.bru`  (seq: 4 → seq: 5)
   - `05-update-document-patch.bru`→ `06-update-document-patch.bru`(seq: 5 → seq: 6)
   - `06-delete-document.bru`      → `07-delete-document.bru`      (seq: 6 → seq: 7)

c) Add Bearer auth to every renamed document request file (all 6).
   Replace `auth: none` with `auth: bearer` and add the `auth:bearer` block directly after
   the closing `}` of the `post`/`get`/`put`/`patch`/`delete` block, before `headers`:
   ```
   auth:bearer {
     token: {{accessToken}}
   }
   ```

d) Update `environments/local.bru` — add `accessToken` as a secret variable so it can be
   pasted in from the token response without being stored in plain text:
   ```
   vars {
     baseUrl: https://localhost:7275
     documentId: 1
   }
   vars:secret [
     accessToken
   ]
   ```

e) Update `DocumentManagement.Workshop.csproj` — update the `<None Include>` entries in the
   `<ItemGroup>` that lists the `.bru` files to reflect the renamed files and the new one:
   - Remove the 6 old `<None Include>` entries for `01-` through `06-`.
   - Add `<None Include=".bruno\DocumentManagement.Workshop\01-auth-token.bru" />`
   - Add `<None Include>` entries for the 6 renamed files (`02-` through `07-`).

---- end of part 7 ------------