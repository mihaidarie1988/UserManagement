# Document Management API

Small ASP.NET Core Web API sample with:
- `DocumentController` CRUD endpoints
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

From project folder (`DocumentManagement-workshop`):

```powershell
dotnet restore
dotnet run
```

## Open Swagger

When the app starts, open:

- `https://localhost:7274/swagger/index.html`

(Use the HTTPS port shown in the startup output.)

## Endpoints

| Method   | Endpoint              |
|----------|-----------------------|
| `GET`    | `/Document`           |
| `GET`    | `/Document/{id}`      |
| `POST`   | `/Document`           |
| `PUT`    | `/Document/{id}`      |
| `PATCH`  | `/Document/{id}`      |
| `DELETE` | `/Document/{id}`      |

## Example curl

> **Port:** The examples below use `7274`. Check your startup output and adjust if yours differs.

### curl (Git Bash · WSL · macOS Terminal)

> **Windows:** open **Git Bash** (installed with [Git for Windows](https://git-scm.com/downloads)) — `cmd` does not support `$()` or `sed`.

```bash
# Create a document
curl -s -k -X POST https://localhost:7274/Document -H "Content-Type: application/json" -d '{"title":"My Document","content":"Document content goes here."}'

# Get all documents
curl -s -k https://localhost:7274/Document

# Get document by id
curl -s -k https://localhost:7274/Document/1

# Update a document (PUT)
curl -s -k -X PUT https://localhost:7274/Document/1 -H "Content-Type: application/json" -d '{"title":"Updated Title","content":"Updated content."}'

# Partially update a document (PATCH)
curl -s -k -X PATCH https://localhost:7274/Document/1 -H "Content-Type: application/json" -d '{"title":"Partially Updated Title"}'

# Delete a document
curl -s -k -X DELETE https://localhost:7274/Document/1
```