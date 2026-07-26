# FinTrack — Personal Finance Tracker

A **modular monolith** personal finance management API built with .NET 10, following **Vertical Slice Architecture (VSA)** and domain-driven design principles. Tracks income, expenses, budgets, accounts, and categories — with JWT authentication and async event-driven integration via MassTransit + RabbitMQ.

---

## Architecture

```
┌──────────────────────────────────────────────────────┐
│                     FinTrack                         │
│                                                      │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐              │
│  │  Api    │  │  Host   │  │ Tests   │              │
│  │ (HTTP)  │  │(Workers)│  │ (xUnit) │              │
│  └────┬────┘  └────┬────┘  └─────────┘              │
│       │            │                                 │
│  ┌────┴────────────┴────────────────┐                │
│  │         Modules (6)              │                │
│  │  Dashboard → Transactions →      │                │
│  │  Categories → Budgets →          │                │
│  │  Accounts → Users                │                │
│  └────────────────┬─────────────────┘                │
│                   │                                  │
│  ┌────────────────┴─────────────────┐                │
│  │  BuildingBlocks  │  Contracts    │                │
│  └──────────────────┴───────────────┘                │
│                                                      │
│  ┌──────────┐  ┌──────────┐                          │
│  │ MongoDB  │  │ RabbitMQ │                          │
│  └──────────┘  └──────────┘                          │
└──────────────────────────────────────────────────────┘
```

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10 |
| Database | MongoDB (via Mongo.Entities) |
| Messaging | MassTransit + RabbitMQ |
| Auth | JWT Bearer (BCrypt password hashing) |
| Validation | FluentValidation |
| Mediator | MediatR |
| Testing | xUnit + FluentAssertions + NSubstitute |
| Infra | Docker Compose (Mongo + RabbitMQ) |

### Module Boundaries (product prominence order)

| Module | Owns | API Surface |
|--------|------|-------------|
| **Dashboard** | Read-model projections, summary snapshots | `GET /api/dashboard/summary` |
| **Transactions** | Income/expense records | `POST /api/transactions` |
| **Categories** | Income/expense categories | `POST /api/categories` |
| **Budgets** | Budget limits per category/period | `POST /api/budgets` |
| **Accounts** | Bank/cash/wallet accounts | `POST /api/accounts` |
| **Users** | Identity, credentials, JWT tokens | `POST /api/auth/register`, `/login`, `/refresh` |

- **No direct project references between modules.** Cross-module communication via integration events (MassTransit/RabbitMQ) or Contracts DTOs.
- **Auth is a hard gate.** Every business endpoint requires authentication via JWT. Only `Register`, `Login`, and `RefreshToken` are anonymous.

---

## Quick Start

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
# 1. Start infrastructure
docker compose up -d

# 2. Set secrets
cd src/FinTrack.Api
dotnet user-secrets set "Jwt:SigningKey" "<your-key>"

# 3. Run API (terminal 1)
dotnet run --project src/FinTrack.Api

# 4. Run Host (terminal 2)
dotnet run --project src/FinTrack.Host
```

> Full setup guide, configuration reference, curl examples, and troubleshooting: **[docs/system-instructions.md](docs/system-instructions.md)**

---

## Project Structure

```
fintrack-dotnet-app/
├── FinTrack.slnx
├── docker-compose.yml
├── README.md
│
├── src/
│   ├── FinTrack.BuildingBlocks/       # ICurrentUser, AuditableEntity, Result, MediatR behaviors
│   ├── FinTrack.Contracts/            # Integration event DTOs (no domain entities)
│   ├── FinTrack.Api/                  # HTTP host — JWT, Swagger, CORS, MassTransit publish
│   ├── FinTrack.Host/                 # MassTransit consumer host — background workers
│   ├── FinTrack.Modules.Dashboard/    # Read-side projections
│   ├── FinTrack.Modules.Transactions/ # Money movement CRUD
│   ├── FinTrack.Modules.Categories/   # Category management
│   ├── FinTrack.Modules.Budgets/      # Budget tracking
│   ├── FinTrack.Modules.Accounts/     # Account management
│   └── FinTrack.Modules.Users/        # Auth & identity
│
├── tests/
│   ├── FinTrack.Modules.Dashboard.Tests/
│   ├── FinTrack.Modules.Transactions.Tests/
│   ├── FinTrack.Modules.Categories.Tests/
│   ├── FinTrack.Modules.Budgets.Tests/
│   ├── FinTrack.Modules.Accounts.Tests/
│   └── FinTrack.Modules.Users.Tests/
│
└── docs/
    ├── architecture-modular-monolith-vsa.md  # Full architecture specification
    ├── system-instructions.md                # Setup, run, and operational guide
    └── learning-path.md                      # Knowledge prerequisites & learning roadmap
```

---

## Coding Conventions

- **Vertical Slice Architecture** — one feature folder per use case (command + handler + validator + controller).
- **Modular monolith** — six bounded contexts as class libraries, no cross-reference spaghetti.
- **Internal by default** — handlers and domain logic are `internal`; only DI entrypoints are `public`.
- **Feature-first naming** — past-tense integration events (`TransactionCreated`, `UserRegistered`).

Full conventions: [docs/architecture-modular-monolith-vsa.md](docs/architecture-modular-monolith-vsa.md)
