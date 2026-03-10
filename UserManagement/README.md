# UserManagement API

Small ASP.NET Core Web API sample with:
- `UserManagementController` CRUD endpoints
- local JWT authentication (`/auth/token`)
- role-based authorization for read/create/update/delete
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

2. In Swagger, click **Authorize** and provide:

`Bearer <accessToken>`

This adds the `Authorization` header to protected requests:

`Authorization: Bearer <accessToken>`

Local users:

- `reader / reader123!` => role: `Read`
- `creator / creator123!` => role: `Create`
- `editor / editor123!` => role: `Update`
- `deleter / deleter123!` => role: `Delete`
- `admin / admin123!` => roles: `Read, Create, Update, Delete`

Role rules:

- `Read` can call GET endpoints
- `Create` can call POST (create) endpoint
- `Update` can call PUT and PATCH endpoints
- `Delete` can call DELETE

## Endpoints

- `POST /auth/token` (public)

- `GET /UserManagement/users` (`Read`)
- `GET /UserManagement/users/{id}` (`Read`)
- `POST /UserManagement/users` (`Create`)
- `PUT /UserManagement/users/{id}` (`Update`)
- `PATCH /UserManagement/users/{id}` (`Update`)
- `DELETE /UserManagement/users/{id}` (`Delete`)

## Example curl

```bash
# 1) Get token
curl -k -X POST https://localhost:<port>/auth/token \
  -H "Content-Type: application/json" \
  -d '{ "username": "admin", "password": "admin123!" }'

# 2) Set token (paste value from response)
TOKEN="<accessToken>"

# 3) Create user (Create operation, requires Create role)
curl -k -X POST https://localhost:<port>/UserManagement/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "name": "Charlie", "email": "charlie@example.com" }'

# 4) Get all users (Read)
curl -k https://localhost:<port>/UserManagement/users \
  -H "Authorization: Bearer $TOKEN"

# 5) Get user by id (Read)
curl -k https://localhost:<port>/UserManagement/users/1 \
  -H "Authorization: Bearer $TOKEN"

# 6) Replace user (Update)
curl -k -X PUT https://localhost:<port>/UserManagement/users/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "name": "Alice Updated", "email": "alice.updated@example.com" }'

# 7) Partially update user (Update)
curl -k -X PATCH https://localhost:<port>/UserManagement/users/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "name": "Alice Partial" }'

# 8) Delete user (Delete)
curl -k -X DELETE https://localhost:<port>/UserManagement/users/2 \
  -H "Authorization: Bearer $TOKEN"
```
