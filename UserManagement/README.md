# UserManagement API

Small ASP.NET Core Web API sample with:
- `UserManagementController` CRUD endpoints
- local API key authorization via custom attribute (`ApiKeyAuthorizeAttribute`)
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

Protected endpoints require header:

- Header name: `X-Api-Key`
- Value: `local-secret-key`

In Swagger, click **Authorize** and provide the API key value.

## Endpoints

Base route: `/UserManagement`

- `GET /UserManagement/weather` (public)
- `GET /UserManagement/users` (protected)
- `GET /UserManagement/users/{id}` (protected)
- `POST /UserManagement/users` (protected)
- `PUT /UserManagement/users/{id}` (protected)
- `PATCH /UserManagement/users/{id}` (protected)
- `DELETE /UserManagement/users/{id}` (protected)

## Example curl

```bash
curl -k -H "X-Api-Key: local-secret-key" https://localhost:<port>/UserManagement/users
```
