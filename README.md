# Prepround

Interview preparation quizzes. Pick a direction and a stack, the levels, how many questions you want and how they
split across topics, then take a timed test and see which topics need more work. Every question comes with an
explanation.

- No sign-up: results history and "already seen" questions stay on your device; the server stores nothing about you.
- Answers are checked on the client: the test arrives with correct answers and explanations, so the only person you
  can cheat is yourself.
- The catalog (fields, levels, directions, stacks, topics) is data, not code: it lives in YAML under `content/`.

The site will live at **prepround.app** (not deployed yet). The UI and content are in Ukrainian for now.

## Repository layout

| Folder | What's inside |
|---|---|
| `backend/` | ASP.NET Core 10 Web API, PostgreSQL 18, `Prepround.slnx` |
| `content/` | question bank in YAML |
| `backend/.claude/` | product spec, decisions (ADR), backlog and agent instructions |

The frontend (Angular) lives in a separate repository, `PreproundWeb`.

## Backend

Clean Architecture without MediatR: `Domain` → `Application` (queries, commands, handlers) → `Infrastructure`
(EF Core + Npgsql) → `Api` (thin controllers). `ContentTool` is a console app for validating and importing content.

### Prerequisites

- .NET SDK 10.0.100 or newer (`backend/global.json` rolls forward to newer feature bands)
- Docker

### Run locally

All commands run from `backend/`.

```bash
# 1. PostgreSQL 18 in Docker on localhost:5433 (container prepround-postgres)
docker compose up -d

# 2. Local tools (dotnet-ef 10)
dotnet tool restore

# 3. Connection string for ContentTool. The API (and dotnet ef) takes it from appsettings.Development.json,
#    but ContentTool does not read appsettings: it uses user-secrets (shared UserSecretsId with the API)
#    or the ConnectionStrings__Prepround environment variable, which overrides user-secrets.
dotnet user-secrets set "ConnectionStrings:Prepround" "Host=localhost;Port=5433;Database=prepround;Username=prepround;Password=prepround" --project src/Prepround.Api

# 4. Create the schema and load the content: without them /api/catalog is empty or answers 500.
#    The dev database takes draft topics too (see "Content").
dotnet ef database update --project src/Prepround.Infrastructure --startup-project src/Prepround.Api
dotnet run --project src/Prepround.ContentTool -- import ../content --include-drafts

# 5. Trust the ASP.NET Core dev certificate: the frontend calls https://localhost:7120
#    and the browser rejects requests to an untrusted certificate.
dotnet dev-certs https --trust

# 6. Run the API
dotnet run --project src/Prepround.Api --launch-profile https
```

Check it:

```bash
curl -k https://localhost:7120/api/health    # {"status":"ok"}
```

The API starts without a database: `/api/health` does not touch it, and the connection is opened only on first use.
The password above is a dev-only password for the local container.

| What | Address |
|---|---|
| API | `https://localhost:7120`, `http://localhost:5120` |
| OpenAPI document (Development only) | `https://localhost:7120/openapi/v1.json` |
| PostgreSQL | `localhost:5433` |

Allowed CORS origins come from configuration (`Cors:Origins`): the frontend dev server `http://localhost:4300` in
`appsettings.Development.json`, `https://prepround.pages.dev` in `appsettings.Production.json`. The base
`appsettings.json` allows none.

### Build and test

```bash
dotnet build    # warnings are errors
dotnet test     # xUnit v3 on Microsoft.Testing.Platform (enabled in global.json)
```

### API Docker image

`backend/Dockerfile` builds the image that Render runs
([ADR 005](backend/.claude/pm/decisions/005-mvp-hosting.md)). It builds on `mcr.microsoft.com/dotnet/sdk:10.0` and runs
on `mcr.microsoft.com/dotnet/aspnet:10.0`. That runtime image is Ubuntu with ICU. Do not switch it to `-alpine` or
`-chiseled`: they have no ICU, and the topic order in the catalog would change. The app runs as a non-root user (1654)
with `ASPNETCORE_ENVIRONMENT=Production`, so OpenAPI is off, and `appsettings.Development.json` is not in the image.

All commands run from `backend/`.

```bash
docker build -t prepround-api .

# Against the local dev database (docker compose up -d); host.docker.internal is the host machine.
docker run --rm --name prepround-api -p 18080:8080 \
  -e "ConnectionStrings__Prepround=Host=host.docker.internal;Port=5433;Database=prepround;Username=prepround;Password=prepround" \
  prepround-api

curl -s http://localhost:18080/api/health    # {"status":"ok"}
```

- **Port.** The app listens on the port from the `PORT` variable, and on 8080 without it. Render passes `PORT` itself
  (10000 by default), so the service needs no port setting.
- **Client IP.** Behind the Render proxy and Cloudflare, the rate limit takes the client IP from `CF-Connecting-IP`
  (`ForwardedHeaders` in `appsettings.Production.json`). The fallback is switched on with variables, without a new build:
  `ForwardedHeaders__ForwardedForHeaderName=X-Forwarded-For` and `ForwardedHeaders__ForwardLimit=2`.

## Content

Questions are written in YAML under `content/`. The API never reads these files: `ContentTool import` loads them
into PostgreSQL, and the app works only with the database. The full format and validation rules are in
[`backend/.claude/pm/decisions/002-content-format.md`](backend/.claude/pm/decisions/002-content-format.md).

```
content/
  <field>/
    field.yaml            # the field's level scale, directions, stacks with their topics
    topics/
      <topic-id>.yaml     # one topic with its questions
```

Any other `.yaml` / `.yml` file in the tree is an error.

A question may have an optional `sources` list of absolute `https` links to the official documentation that backs its
correct answer ([ADR 004](backend/.claude/pm/decisions/004-content-sources-and-difficulty.md)); it lives only in YAML and is
not imported.

All commands run from `backend/`.

```bash
# Validate the content; no database needed
dotnet run --project src/Prepround.ContentTool -- validate ../content

# Import into the database: apply the migrations first
dotnet ef database update --project src/Prepround.Infrastructure --startup-project src/Prepround.Api
dotnet run --project src/Prepround.ContentTool -- import ../content                    # production: no drafts
dotnet run --project src/Prepround.ContentTool -- import ../content --include-drafts   # dev database: drafts too
```

`validate` exit codes:

| Code | Meaning |
|---|---|
| `0` | content is valid; a summary per field is printed |
| `1` | content errors, one `file: id: message` line each on stderr |
| `2` | wrong arguments or the content folder does not exist |

`import` runs the same validation, then loads everything in one transaction. It is idempotent: a second run without
changes in the files changes nothing. Questions (and other entities) that are no longer in the files are deactivated,
not deleted, because clients keep question ids in their history. Exit codes are `0`, `1` and `2` as above, plus `3`
for the database: no connection string, the database is unreachable, or migrations are pending.

**Draft topics.** A topic file may say `draft: true` at the top level, next to `id` and `title`: its questions have not
passed the fact check yet ([ADR 005](backend/.claude/pm/decisions/005-mvp-hosting.md), section 6). `validate` checks
drafts like any other topic and prints how many questions are in them. `import` leaves the questions of draft topics
out, as if they were not in the files: new ones are not added, imported ones are deactivated (the topic itself is
imported). `import <dir> --include-drafts` imports everything. The dev database is imported with the flag, production
without it. To switch off a published topic, mark it `draft: true` again and run `import` in production.

The connection string comes from user-secrets (step 3 of "Run locally"); the `ConnectionStrings__Prepround`
environment variable overrides it.

## Frontend

The web client lives in a separate repository, `PreproundWeb`, checked out next to this one (`../PreproundWeb`).
How to run it is described in its README.
