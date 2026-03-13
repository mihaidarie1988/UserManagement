# Document Management API

Small ASP.NET Core Web API sample with:
- `DocumentController` CRUD endpoints
- local JWT authentication (`/auth/token`)
- role-based authorization for read/create/update/delete
- ownership enforcement: each user sees and edits only their own documents
- admin bypasses ownership and can access all documents
- Swagger UI for local testing
- Testing with curl
- Testing with Bruno tool

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

> **Port:** The examples below use `7274`. Check your startup output and adjust if yours differs.

### bash (requires [`jq`](https://jqlang.org/download/))

```bash
# 1) Get token and store it
TOKEN=$(curl -s -k -X POST https://localhost:7274/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123!"}' \
  | jq -r '.accessToken')

# 2) Create document (requires Create role)
curl -s -k -X POST https://localhost:7274/Document \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"New Document","content":"Document content goes here."}'

# 3) Get all documents (requires Read role; non-admin sees own only)
curl -s -k https://localhost:7274/Document \
  -H "Authorization: Bearer $TOKEN"

# 4) Get document by id (requires Read role)
curl -s -k https://localhost:7274/Document/1 \
  -H "Authorization: Bearer $TOKEN"

# 5) Replace document (requires Update role)
curl -s -k -X PUT https://localhost:7274/Document/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Updated Title","content":"Updated document content."}'

# 6) Partially update document (requires Update role)
curl -s -k -X PATCH https://localhost:7274/Document/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Partially Updated Title"}'

# 7) Delete document (requires Delete role)
curl -s -k -X DELETE https://localhost:7274/Document/2 \
  -H "Authorization: Bearer $TOKEN"
```
