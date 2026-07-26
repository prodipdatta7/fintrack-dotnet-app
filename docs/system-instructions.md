# FinTrack — System Instructions

How to set up, run, and operate the FinTrack system for local development.

---

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0+ | Build and run the application |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest | Run MongoDB and RabbitMQ containers |

---

## 1. Clone and restore

```bash
git clone <your-repo-url>
cd fintrack-dotnet-app
dotnet restore FinTrack.slnx
```

---

## 2. Infrastructure (Docker)

Start MongoDB and RabbitMQ:

```bash
docker compose up -d
```

Verify both containers are healthy:

```bash
docker compose ps
```

| Service | Port | Notes |
|---------|------|-------|
| MongoDB 7.0 | `27017` | Database `FinTrackDb` is created automatically |
| RabbitMQ 3 | `5672` (AMQP), `15672` (HTTP UI) | Management UI at `http://localhost:15672` |

Stop services:

```bash
docker compose down          # preserves data volumes
docker compose down -v       # removes all data (fresh start)
```

### Connect to services

**MongoDB** — connection string: `mongodb://localhost:27017`

| Tool | Command |
|------|---------|
| MongoDB Compass (GUI) | Paste `mongodb://localhost:27017` into the connection dialog |
| mongosh (CLI) | `mongosh "mongodb://localhost:27017/FinTrackDb"` |
| Docker exec | `docker exec -it fintrack-mongo mongosh FinTrackDb` |

Useful mongosh commands once connected:

```js
show collections           // list all collections
db.users.find().pretty()   // view registered users
db.transactions.find().pretty()
```

**RabbitMQ** — Management UI at `http://localhost:15672` (login with your configured credentials).

---

## 3. Secrets (user secrets)

Credentials must never be committed. Set them via .NET User Secrets:

```bash
cd src/FinTrack.Api

# Generate a strong random key (PowerShell)
$key = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object { [char]$_ })
dotnet user-secrets set "Jwt:SigningKey" $key

# RabbitMQ credentials (match your docker-compose.yml defaults)
dotnet user-secrets set "RabbitMq:Username" "guest"
dotnet user-secrets set "RabbitMq:Password" "guest"
```

Verify secrets were stored:

```bash
dotnet user-secrets list
```

---

## 4. Build

```bash
dotnet build FinTrack.slnx
```

---

## 5. Run

You need **two terminals** — one for the API and one for the background worker host.

### Terminal 1 — API (HTTP)

```bash
dotnet run --project src/FinTrack.Api
```

| Protocol | URL |
|----------|-----|
| HTTP | `http://localhost:5171` |
| HTTPS | `https://localhost:7241` |
| Swagger UI | `http://localhost:5171/swagger` |

### Terminal 2 — Host (background workers)

```bash
dotnet run --project src/FinTrack.Host
```

The Host consumes integration events from RabbitMQ — Dashboard projections, Budget spend updates, and other async side effects won't work without it.

---

## 6. Configuration Reference

### `appsettings.json` shape

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "FinTrackDb"
  },
  "Jwt": {
    "Issuer": "FinTrack",
    "Audience": "FinTrack",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  },
  "RabbitMq": {
    "Host": "localhost"
  }
}
```

> `Jwt:SigningKey`, `RabbitMq:Username`, and `RabbitMq:Password` are set via **user secrets** (step 3). These keys do not appear in the committed file.

### CORS

The API allows requests from `http://localhost:3000` (React dev server). To change this, edit the `AllowReactApp` policy in `src/FinTrack.Api/Program.cs`.

---

## 7. API Endpoints

### Auth (anonymous — no token required)

| Method | Endpoint | Request Body |
|--------|----------|-------------|
| `POST` | `/api/auth/register` | `{ "email": "...", "password": "..." }` |
| `POST` | `/api/auth/login` | `{ "email": "...", "password": "..." }` |
| `POST` | `/api/auth/refresh` | `{ "accessToken": "...", "refreshToken": "..." }` |

### Business (authenticated — `Authorization: Bearer <token>` required)

| Method | Endpoint | Module |
|--------|----------|--------|
| `GET` | `/api/dashboard/summary` | Dashboard |
| `POST` | `/api/transactions` | Transactions |
| `POST` | `/api/categories` | Categories |
| `POST` | `/api/budgets` | Budgets |
| `POST` | `/api/accounts` | Accounts |

### Example flow (curl)

```bash
# Register
curl -s -X POST http://localhost:5171/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@fintrack.dev","password":"<your-password>"}'

# Login (copy the accessToken from the response)
curl -s -X POST http://localhost:5171/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@fintrack.dev","password":"<your-password>"}'

# Create a category
curl -s -X POST http://localhost:5171/api/categories \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <accessToken>" \
  -d '{"name":"Groceries","type":"Expense"}'

# Create an account
curl -s -X POST http://localhost:5171/api/accounts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <accessToken>" \
  -d '{"name":"Main Bank","type":"Bank","initialBalance":5000}'

# Create a transaction (use real accountId and categoryId from responses above)
curl -s -X POST http://localhost:5171/api/transactions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <accessToken>" \
  -d '{
    "title":"Weekly groceries",
    "amount":85.50,
    "type":"Expense",
    "accountId":"<account-id>",
    "categoryId":"<category-id>"
  }'

# Check dashboard
curl -s http://localhost:5171/api/dashboard/summary \
  -H "Authorization: Bearer <accessToken>"
```

---

## 8. Testing

```bash
# All tests
dotnet test FinTrack.slnx

# Single module
dotnet test tests/FinTrack.Modules.Transactions.Tests

# With verbose output
dotnet test FinTrack.slnx --logger "console;verbosity=detailed"
```

Test stack: **xUnit** + **FluentAssertions** + **NSubstitute**

---

## 9. Troubleshooting

### Docker fails to start

Ensure Docker Desktop is running. On Windows, the Docker engine icon should be visible in the system tray.

```bash
docker info   # verify Docker is reachable
```

### MongoDB connection refused

```bash
docker compose ps          # check if mongo container is running
docker compose logs mongodb  # check for errors
```

### RabbitMQ connection refused

Wait ~30 seconds after `docker compose up -d` — RabbitMQ takes time to initialize on first start.

```bash
docker compose logs rabbitmq  # look for "Server startup complete"
```

### JWT authentication fails

Verify the signing key is set and matches between terminals:

```bash
dotnet user-secrets list --project src/FinTrack.Api
```

If you changed the key, restart the API for the new key to take effect.
