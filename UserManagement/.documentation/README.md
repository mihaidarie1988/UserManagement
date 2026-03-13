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

| Username  | Password      | Roles                                         | Seeded documents           | What this demonstrates             |
|-----------|---------------|-----------------------------------------------|----------------------------|------------------------------------|
| `alice`   | `alice123!`   | Read, Create, Update, Delete                  | Doc 1 — Project Proposal<br>Doc 2 — Meeting Notes | Full CRUD, ownership isolation |
| `bob`     | `bob123!`     | Read, Create, Update, Delete                  | Doc 3 — Budget Overview    | Full CRUD, ownership isolation     |
| `charlie` | `charlie123!` | Read, Create, Update *(no Delete)*            | Doc 4 — Technical Specification | Role restriction: DELETE returns 403 |
| `admin`   | `admin123!`   | Read, Create, Update, Delete, Admin           | all documents              | Ownership bypass                   |

### Role rules

| Role     | Allowed operations                  |
|----------|-------------------------------------|
| `Read`   | GET endpoints                       |
| `Create` | POST (create) endpoint              |
| `Update` | PUT and PATCH endpoints             |
| `Delete` | DELETE endpoint                     |
| `Admin`  | All operations across all documents |

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

### bash

```bash
# ── Ownership scenario (alice vs bob) ─────────────────────────────────────────

ALICE=$(curl -s -k -X POST https://localhost:7274/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"alice","password":"alice123!"}' | sed 's/.*"accessToken":"\([^"]*\)".*/\1/')

BOB=$(curl -s -k -X POST https://localhost:7274/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"bob","password":"bob123!"}' | sed 's/.*"accessToken":"\([^"]*\)".*/\1/')

# Alice sees only her documents (ids 1 and 2)
curl -s -k https://localhost:7274/Document -H "Authorization: Bearer $ALICE"

# Bob sees only his document (id 3)
curl -s -k https://localhost:7274/Document -H "Authorization: Bearer $BOB"

# Alice updates her own document — 204
curl -s -k -X PATCH https://localhost:7274/Document/1 \
  -H "Authorization: Bearer $ALICE" \
  -H "Content-Type: application/json" \
  -d '{"title":"Updated by Alice"}'

# Alice tries to update Bob's document — 403
curl -s -k -o /dev/null -w "%{http_code}" -X PATCH https://localhost:7274/Document/3 \
  -H "Authorization: Bearer $ALICE" \
  -H "Content-Type: application/json" \
  -d '{"title":"Alice tries to hijack"}'

# ── Role restriction scenario (charlie has no Delete role) ────────────────────

CHARLIE=$(curl -s -k -X POST https://localhost:7274/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"charlie","password":"charlie123!"}' | sed 's/.*"accessToken":"\([^"]*\)".*/\1/')

# Charlie reads and updates his own document — both succeed
curl -s -k https://localhost:7274/Document/4 -H "Authorization: Bearer $CHARLIE"

curl -s -k -X PATCH https://localhost:7274/Document/4 \
  -H "Authorization: Bearer $CHARLIE" \
  -H "Content-Type: application/json" \
  -d '{"title":"Updated by Charlie"}'

# Charlie tries to delete — 403 (missing Delete role, not an ownership issue)
curl -s -k -o /dev/null -w "%{http_code}" -X DELETE https://localhost:7274/Document/4 \
  -H "Authorization: Bearer $CHARLIE"

# ── Admin bypasses ownership ──────────────────────────────────────────────────

ADMIN=$(curl -s -k -X POST https://localhost:7274/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123!"}' | sed 's/.*"accessToken":"\([^"]*\)".*/\1/')

# Admin sees all four documents
curl -s -k https://localhost:7274/Document -H "Authorization: Bearer $ADMIN"
```
