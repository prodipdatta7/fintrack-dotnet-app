# FinTrack — Modern Personal Finance App (.NET 10 Modular Monolith)

FinTrack is an enterprise-grade personal finance backend built with **.NET 10**, **Vertical Slice Architecture (VSA)**, **ASP.NET Core API Controllers**, **MongoDB**, and **MassTransit**.

The solution is architected as a **Modular Monolith** with strict bounded-context isolation across 6 domain modules (`Dashboard`, `Transactions`, `Categories`, `Budgets`, `Accounts`, `Users`).

---

## 🏛️ Key Architectural Features

- **Modular Monolith:** Strict bounded-context boundaries with zero cross-module project references. Communication occurs strictly via `Contracts` integration events or synchronous query interfaces.
- **Vertical Slice Architecture (VSA):** Features organized by use-case folders (`Features/CreateTransaction/*`) containing the Controller, Command/Query, Handler, Validator, and DTOs together.
- **Thin API Controllers:** Presentation layer uses ASP.NET Core API Controllers (`ControllerBase`) delegating directly to MediatR pipeline handlers.
- **Hard Auth Gateway (Default-Deny):** Global `RequireAuthenticatedUser` authorization fallback policy. Anonymous access is strictly limited to auth entrypoints (`Register`, `Login`, `RefreshToken`).
- **Resilient Messaging (MongoDB Outbox):** MassTransit in-memory bus with MongoDB Outbox pattern ensuring atomic database writes and event dispatching.
- **Multi-Tenant Scoping:** All financial domain data is isolated and queried by the authenticated user's `UserId`.

---

## 🛠️ Technology Stack

| Layer / Concern | Technology |
|---|---|
| **Framework** | .NET 10 / ASP.NET Core |
| **Architecture** | Modular Monolith + Vertical Slice Architecture (VSA) |
| **Presentation** | ASP.NET Core API Controllers + Swagger / OpenAPI |
| **Application / CQRS** | MediatR 12.x + FluentValidation 11.x |
| **Persistence** | MongoDB.Driver 3.x (BSON Mongo collections) |
| **Messaging & Outbox** | MassTransit 8.x + MassTransit MongoDB Outbox |
| **Auth & Security** | JWT Bearer Tokens + BCrypt Password Hashing |
| **Testing** | xUnit 2.9 + FluentAssertions 8.0 + NSubstitute 5.3 |
| **Containerization** | Docker & Docker Compose |

---

## 📁 Repository Structure

```
.
├── FinTrack.slnx                    # Solution file
├── project-spec.md                  # Comprehensive Project Specification
├── README.md                        # Project Overview & Quick Start Guide
│
├── docs/                            # Architecture Documentation
│   ├── architecture-modular-monolith-vsa.md   # Foundational Architecture Blueprint
│   ├── architecture-phase1-initial.md          # Phase 1: Single-Process Architecture
│   └── architecture-phase2-scaleout.md         # Phase 2: Scale-Out Architecture
│
├── docker/                          # Docker Containerization
│   ├── README.md                    # Docker usage instructions
│   ├── phase1/                      # Single-Process Docker setup (API + MongoDB)
│   └── phase2/                      # Split-Process Docker setup (API + Worker + Mongo + RabbitMQ)
│
├── src/                             # Source Code
│   ├── FinTrack.Api                 # Web API Composition Root (Program.cs)
│   ├── FinTrack.BuildingBlocks      # Shared primitives (Result, ICurrentUser, AuditableEntity, Mongo setup)
│   ├── FinTrack.Contracts           # Integration events & cross-module validation queries
│   ├── FinTrack.Modules.Users       # Bounded Context: Identity & Auth
│   ├── FinTrack.Modules.Transactions# Bounded Context: Transactions CRUD
│   ├── FinTrack.Modules.Categories  # Bounded Context: Category Management & Default Seeding
│   ├── FinTrack.Modules.Dashboard   # Bounded Context: Read Aggregation Summaries
│   ├── FinTrack.Modules.Budgets     # Bounded Context: Category Budgets & Spend Alerts
│   └── FinTrack.Modules.Accounts    # Bounded Context: Bank/Cash/Wallet Accounts
│
└── tests/                           # Unit & Integration Tests
    ├── FinTrack.Modules.Users.Tests
    ├── FinTrack.Modules.Transactions.Tests
    ├── FinTrack.Modules.Categories.Tests
    ├── FinTrack.Modules.Dashboard.Tests
    ├── FinTrack.Modules.Budgets.Tests
    └── FinTrack.Modules.Accounts.Tests
```

---

## 🚀 Quick Start Guide

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or MongoDB local instance)

### 1. Launch Environment via Docker Compose

Run the Phase 1 docker compose stack (starts API + MongoDB):

```bash
docker compose -f docker/phase1/docker-compose.yml up --build -d
```

- **Swagger UI:** `http://localhost:5000/swagger`
- **MongoDB:** `mongodb://localhost:27017`

### 2. Run Local Development Server (.NET CLI)

If running MongoDB separately on `localhost:27017`:

```bash
dotnet run --project src/FinTrack.Api
```

### 3. Run Unit Tests

Execute the full xUnit test suite across all module test projects:

```bash
dotnet test FinTrack.slnx
```

---

## 🌐 API Overview

### Auth Endpoints (`Modules.Users`)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `POST` | `/api/users/auth/register` | Public | Register new user account |
| `POST` | `/api/users/auth/login` | Public | Authenticate user & receive JWT access + refresh tokens |
| `POST` | `/api/users/auth/refresh` | Public | Rotate refresh token for new access token |
| `GET` | `/api/users/me` | Authorized | Get current user profile |

### Transactions Endpoints (`Modules.Transactions`)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `POST` | `/api/transactions` | Authorized | Create income/expense transaction |
| `GET` | `/api/transactions` | Authorized | Get paged & filtered list of transactions |
| `GET` | `/api/transactions/{id}` | Authorized | Get transaction by ID |
| `PUT` | `/api/transactions/{id}` | Authorized | Update transaction details |
| `DELETE` | `/api/transactions/{id}` | Authorized | Delete transaction |

### Categories Endpoints (`Modules.Categories`)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `POST` | `/api/categories` | Authorized | Create custom category |
| `GET` | `/api/categories` | Authorized | List default & user-defined categories |
| `PUT` | `/api/categories/{id}` | Authorized | Update custom category |

---

## 📚 Related Documentation

- 📋 [Comprehensive Project Specification](project-spec.md)
- 📐 [Phase 1 Architecture Blueprint](docs/architecture-phase1-initial.md)
- ⚡ [Phase 2 Scale-Out Architecture Blueprint](docs/architecture-phase2-scaleout.md)
- 🐳 [Docker Execution README](docker/README.md)
