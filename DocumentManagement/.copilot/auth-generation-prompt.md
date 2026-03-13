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
- Protect API endpoints by role using custom authorization attributes and policies.
- Enforce ownership: each authenticated user can only read/edit/delete documents they created;
  `admin` bypasses ownership and can access all documents.

REQUIREMENTS

1) NuGet packages
- Add `Microsoft.AspNetCore.Authentication.JwtBearer` (match runtime version).
- Do NOT add `Microsoft.AspNetCore.OpenApi` — it conflicts with Swashbuckle.

2) Authorization constants — create `Authorization/AuthorizationPolicies.cs`
- Add `AuthorizationPolicies` static class with role and policy name constants:
  - `ReadRole = "Read"`, `CreateRole = "Create"`, `UpdateRole = "Update"`, `DeleteRole = "Delete"`
  - `AdminRole = "Admin"` — assigned only to `admin`; used to bypass ownership checks.
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
  - Issuer:     `DocumentManagement.Local`
  - Audience:   `DocumentManagement.Api`
  - SigningKey: `DocumentManagement_Local_JWT_Signing_Key_2026!`
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
  - `alice   / alice123!`   => `Read, Create, Update, Delete`
  - `bob     / bob123!`     => `Read, Create, Update, Delete`
  - `charlie / charlie123!` => `Read, Create, Update`          (no Delete — role restriction demo)
  - `admin   / admin123!`   => `Read, Create, Update, Delete, Admin`
- On success return JWT + expiry + roles; on failure return 401.
- JWT: `sub` claim = username, `role` claims = assigned roles, expiry = 60 min, HS256.

8) Add ownership field — update `Models/Document.cs`
- Add `string CreatedBy` as the last field of the `Document` record:
  `public record Document(int Id, string Title, string Content, string CreatedBy);`
- `WriteDocumentRequest` and `PatchDocumentRequest` remain unchanged (`CreatedBy` is stamped server-side).

9) Seed ownership data — update `Services/DocumentStore.cs`
- Add the `CreatedBy` argument to all 4 seeded documents:
  - Doc 1 "Project Proposal"          → `"alice"`
  - Doc 2 "Meeting Notes"             → `"alice"`
  - Doc 3 "Budget Overview"           → `"bob"`
  - Doc 4 "Technical Specification"   → `"charlie"`
- Add `GetByOwner(string username)` returning documents where `CreatedBy == username`.

10) Ownership filter — create `Authorization/DocumentOwnershipFilter.cs`
- Create `DocumentOwnershipAttribute : TypeFilterAttribute(typeof(DocumentOwnershipFilter))`.
  Using `TypeFilterAttribute` lets the filter receive `DocumentStore` from the DI container.
- Create `DocumentOwnershipFilter(DocumentStore store) : IAsyncAuthorizationFilter`.
- In `OnAuthorizationAsync`:
  - Read `id` from `context.RouteData.Values["id"]`; return early if not present or not an int.
  - If `user.IsInRole(AuthorizationPolicies.AdminRole)` → pass through.
  - If `store.FindById(id) is null` → pass through (let the action return 404).
  - If `document.CreatedBy != user.FindFirstValue(ClaimTypes.NameIdentifier)` →
    set `context.Result = new ForbidResult()`.

11) Apply auth and ownership — update `Controllers/DocumentController.cs`
- `DocumentStore` is already injected via primary constructor — keep it.
- Add `using System.Security.Claims;`.
- Role-to-endpoint mapping (apply the corresponding attribute to each action):
  - `GET  /Document`         → `[ReadAccess]`
  - `GET  /Document/{id}`    → `[ReadAccess]`    + `[DocumentOwnership]`
  - `POST /Document`         → `[CreateAccess]`
  - `PUT  /Document/{id}`    → `[UpdateAccess]`  + `[DocumentOwnership]`
  - `PATCH /Document/{id}`   → `[UpdateAccess]`  + `[DocumentOwnership]`
  - `DELETE /Document/{id}`  → `[DeleteAccess]`  + `[DocumentOwnership]`
- `POST /Document`: stamp `CreatedBy = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!`.
- `GET /Document`: admin sees all; others see only their own documents:
  ```csharp
  var docs = HttpContext.User.IsInRole(AuthorizationPolicies.AdminRole)
      ? store.GetAll()
      : store.GetByOwner(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  ```
- PUT/PATCH: read the existing record first and preserve its `CreatedBy` when rebuilding.
- Use `HttpContext.User` (not `User`) to avoid any naming collision.

12) Cleanup and validation
- Avoid any API-key auth logic or filters.
- Keep minimal changes to existing controller endpoint behavior.
- Ensure project builds successfully.

13) README update
- Document:
  - `POST /auth/token`
  - Local users table (alice/bob/charlie/admin), passwords, roles, seeded documents, what each user demonstrates
  - Role rules (Read/Create/Update/Delete/Admin)
  - Ownership rules
  - Endpoint-to-role mapping table
  - curl examples for token + all endpoints with Bearer token

14) Bruno collection updates — `.bruno/DocumentManagement.Workshop/`
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
   - `01-create-document.bru`    → `02-create-document.bru`    (seq: 1 → seq: 2)
   - `02-get-documents.bru`      → `03-get-documents.bru`      (seq: 2 → seq: 3)
   - `03-get-document-by-id.bru` → `04-get-document-by-id.bru` (seq: 3 → seq: 4)
   - `04-update-document-put.bru`→ `05-update-document-put.bru`(seq: 4 → seq: 5)
   - `05-update-document-patch.bru`→`06-update-document-patch.bru`(seq: 5 → seq: 6)
   - `06-delete-document.bru`    → `07-delete-document.bru`    (seq: 6 → seq: 7)

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
