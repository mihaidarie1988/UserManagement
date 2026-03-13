# Document Management API

Small ASP.NET Core Web API sample with:
- `DocumentController` CRUD endpoints
- local JWT authentication (`/auth/token`)
- role-based authorization for read/create/update/delete
- ownership enforcement: each user sees and edits only their own documents
- admin bypasses ownership and can access all documents
- Swagger UI for local testing

## Prerequisites

- .NET SDK `10.0.103` (or newer .NET 10 SDK)
- From repo root, `global.json` should point to .NET 10

Check SDK in terminal:

```powershell
dotnet --info
```

## Run the app

From project folder (`UserManagement/UserManagement`):

```powershell
dotnet restore
dotnet run
```

## Open Swagger

When the app starts, open:

- `https://localhost:7274/swagger/index.html`

(Use the HTTPS port shown in the startup output.)

## Authentication

Authentication is JWT Bearer-based.

1. Obtain a token:

`POST /auth/token`

```json
{ "username": "admin", "password": "admin123!" }
```

2. In Swagger, click **Authorize** and paste the `accessToken` value (without the `Bearer ` prefix — Swagger adds it automatically).

### Local users

| Username  | Password      | Roles                                    |
|-----------|---------------|------------------------------------------|
| `reader`  | `reader123!`  | `Read`                                   |
| `creator` | `creator123!` | `Create`                                 |
| `editor`  | `editor123!`  | `Update`                                 |
| `deleter` | `deleter123!` | `Delete`                                 |
| `admin`   | `admin123!`   | `Read`, `Create`, `Update`, `Delete`, `Admin` |

### Role rules

| Role     | Allowed operations                    |
|----------|---------------------------------------|
| `Read`   | GET endpoints                         |
| `Create` | POST (create) endpoint                |
| `Update` | PUT and PATCH endpoints               |
| `Delete` | DELETE endpoint                       |
| `Admin`  | All operations across all documents   |

### Ownership rules

- Non-admin users can only read and modify **documents they created**.
- `GET /Document` returns only the caller's own documents (admin sees all).
- `GET /Document/{id}`, `PUT`, `PATCH`, `DELETE` return **403** if the document exists but belongs to another user.

## Endpoints

| Method   | Endpoint              | Role required | Ownership check |
|----------|-----------------------|---------------|-----------------|
| `POST`   | `/auth/token`         | public        | —               |
| `GET`    | `/Document`           | `Read`        | own only        |
| `GET`    | `/Document/{id}`      | `Read`        | own only        |
| `POST`   | `/Document`           | `Create`      | stamped on create |
| `PUT`    | `/Document/{id}`      | `Update`      | own only        |
| `PATCH`  | `/Document/{id}`      | `Update`      | own only        |
| `DELETE` | `/Document/{id}`      | `Delete`      | own only        |

## Example curl

```bash
# 1) Get token (admin can call all endpoints)
curl -k -X POST https://localhost:<port>/auth/token \
  -H "Content-Type: application/json" \
  -d '{ "username": "admin", "password": "admin123!" }'

# 2) Set token (paste accessToken from response)
TOKEN="<accessToken>"

# 3) Create document (requires Create role)
curl -k -X POST https://localhost:<port>/Document \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "title": "New Document", "content": "Document content goes here." }'

# 4) Get all documents (requires Read role; non-admin sees own only)
curl -k https://localhost:<port>/Document \
  -H "Authorization: Bearer $TOKEN"

# 5) Get document by id (requires Read role)
curl -k https://localhost:<port>/Document/1 \
  -H "Authorization: Bearer $TOKEN"

# 6) Replace document (requires Update role)
curl -k -X PUT https://localhost:<port>/Document/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "title": "Updated Title", "content": "Updated document content." }'

# 7) Partially update document (requires Update role)
curl -k -X PATCH https://localhost:<port>/Document/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "title": "Partially Updated Title" }'

# 8) Delete document (requires Delete role)
curl -k -X DELETE https://localhost:<port>/Document/2 \
  -H "Authorization: Bearer $TOKEN"
```
