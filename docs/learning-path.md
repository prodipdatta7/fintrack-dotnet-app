# FinTrack — Learning Path

A structured roadmap of the knowledge and concepts needed to work effectively on this project, ordered from foundations to advanced.

---

## Tier 0: Prerequisites (must have before anything else)

| Topic | What you need | Time |
|-------|--------------|------|
| **C# fundamentals** | Classes, records, interfaces, generics, LINQ, async/await, dependency injection | ~2 weeks if new |
| **.NET CLI** | `dotnet build`, `dotnet run`, `dotnet test`, `dotnet user-secrets`, solution files (`.slnx`) | ~1 day |
| **HTTP & REST** | Verbs (GET/POST), status codes, headers, JSON, Swagger/OpenAPI | ~1 day |
| **Git** | Clone, branch, commit, PR workflow | ~1 day |

**Resources:**

- [C# Fundamentals for Absolute Beginners (Microsoft)](https://learn.microsoft.com/en-us/shows/c-fundamentals-for-absolute-beginners/)
- [.NET CLI overview](https://learn.microsoft.com/en-us/dotnet/core/tools/)

---

## Tier 1: ASP.NET Core & API Basics

These are the foundations of the `FinTrack.Api` host.

| Concept | Where it appears in FinTrack | Learn |
|---------|------------------------------|-------|
| **Controllers** | All endpoints use `[ApiController]` + `[Route]` + `[HttpPost]`/`[HttpGet]` attributes | `AuthController.cs`, `TransactionsController.cs` |
| **Middleware pipeline** | `UseAuthentication()`, `UseAuthorization()`, `UseCors()` in `Program.cs` | `Program.cs` line ~70+ |
| **Configuration** | `appsettings.json`, `IConfiguration`, user secrets | `appsettings.json`, user-secrets commands |
| **Dependency Injection** | `Add*Module()`, `AddScoped<ICurrentUser>`, `AddMediatR()` | `Program.cs` DI registrations |
| **CORS** | Allows React frontend at `localhost:3000` | `Program.cs` CORS policy |
| **Swagger** | Auto-generated API docs at `/swagger` | `Program.cs` |

**Resources:**

- [Create web APIs with controllers (Microsoft)](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Dependency injection in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)

---

## Tier 2: MongoDB & Mongo.Entities

Every module persists data to MongoDB. We use **Mongo.Entities** (a high-level library) with **MongoDB.Driver** as a fallback.

| Concept | Where it appears | Learn |
|---------|-----------------|-------|
| **Document model** | NoSQL documents instead of relational tables | `Transaction.cs`, `User.cs` |
| **Mongo.Entities `Entity` base** | All domain entities inherit `AuditableEntity` → `MongoDB.Entities.Entity` | `AuditableEntity.cs` |
| **`[Collection]` attribute** | Maps entity class to a named MongoDB collection | `[Collection("transactions")]` |
| **`SaveAsync()`** | Insert or update a document | `CreateTransactionHandler.cs` |
| **`DB.Find<T>()`** | Query documents with fluent API | `LoginHandler.cs` (find user by email) |
| **`DB.Update<T>()`** | Bulk update with filter + modify | `LoginHandler.cs` (revoke refresh tokens) |
| **`DB.InitAsync()`** | One-time MongoDB connection bootstrap | `MongoInitializer.cs` |
| **Connection string** | `mongodb://localhost:27017` | `appsettings.json` |
| **Indexing** | Covered later (optimization) | — |

**Resources:**

- [Mongo.Entities documentation](https://mongodb-entities.com/)
- [MongoDB CRUD operations](https://www.mongodb.com/docs/manual/crud/)
- [MongoDB Compass (GUI)](https://www.mongodb.com/products/compass)

---

## Tier 3: MediatR & CQRS

Every feature in FinTrack uses MediatR to decouple the API endpoint from business logic.

| Concept | Where it appears | Learn |
|---------|-----------------|-------|
| **`IRequest<T>`** | A command/query DTO (immutable `record`) | `CreateTransactionCommand.cs` |
| **`IRequestHandler<TRequest, TResponse>`** | The handler that executes the use case | `CreateTransactionHandler.cs` |
| **`ISender`** | Injected into controllers to dispatch commands | `TransactionsController.cs` |
| **Pipeline behaviors** | Cross-cutting concerns that wrap every handler | `LoggingBehavior.cs`, `ValidationBehavior.cs` |
| **Assembly scanning** | `RegisterServicesFromAssemblies(...)` discovers all handlers | `Program.cs` |
| **Command vs Query** | Commands mutate state, queries read state | `CreateTransactionCommand` (command), `GetDashboardSummaryQuery` (query) |

**Resources:**

- [MediatR on GitHub](https://github.com/jbogard/MediatR)
- [CQRS pattern (Microsoft)](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)

---

## Tier 4: FluentValidation

Every command has a validator that runs automatically before the handler via the `ValidationBehavior` pipeline.

| Concept | Where it appears | Learn |
|---------|-----------------|-------|
| **`AbstractValidator<T>`** | Define validation rules for a command | `CreateTransactionValidator.cs` |
| **`RuleFor()`** | Declare rules like `.NotEmpty()`, `.GreaterThan()`, `.MaximumLength()` | All validator files |
| **Automatic execution** | `ValidationBehavior` calls all registered validators, throws `ValidationException` on failure | `ValidationBehavior.cs` |
| **Registration** | `AddValidatorsFromAssemblies(...)` scans all module assemblies | `Program.cs` |

**Resources:**

- [FluentValidation documentation](https://docs.fluentvalidation.net/)

---

## Tier 5: JWT Authentication & Authorization

Auth is a **hard gate** — every business endpoint requires a valid JWT.

| Concept | Where it appears | Learn |
|---------|-----------------|-------|
| **JWT structure** | Header, payload (claims), signature | Theory topic |
| **`AddJwtBearer()`** | Configures token validation (issuer, audience, signing key) | `Program.cs` |
| **`[Authorize]`** | Applied to every business controller | All controller files |
| **`[AllowAnonymous]`** | Only on `Register`, `Login`, `RefreshToken` | `AuthController.cs` |
| **Fallback policy** | `options.FallbackPolicy = options.DefaultPolicy` makes auth the default | `Program.cs` |
| **BCrypt hashing** | `BCrypt.HashPassword()` / `BCrypt.Verify()` — never store plaintext | `RegisterHandler.cs`, `LoginHandler.cs` |
| **Access + Refresh tokens** | Short-lived access (15 min) + long-lived refresh (7 days), rotated on use | `JwtTokenGenerator.cs`, `RefreshTokenHandler.cs` |
| **`ICurrentUser`** | Abstraction so handlers don't touch `HttpContext` directly | `ICurrentUser.cs`, `HttpContextCurrentUser.cs` |

**Resources:**

- [JWT.io (debug tokens)](https://jwt.io/)
- [JWT authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt)
- [BCrypt.Net](https://github.com/BcryptNet/bcrypt.net)

---

## Tier 6: MassTransit + RabbitMQ (Event-Driven Architecture)

Cross-module communication happens via integration events over RabbitMQ.

| Concept | Where it appears | Learn |
|---------|-----------------|-------|
| **Message broker** | RabbitMQ routes messages between producers and consumers | Theory |
| **MassTransit** | .NET abstraction over RabbitMQ (and other transports) | `Program.cs` |
| **Publish (Api)** | After a successful write, publish an event | `CreateTransactionHandler.cs`, `RegisterHandler.cs` |
| **Consume (Host)** | Background workers react to events (projections, side effects) | `FinTrack.Host/Program.cs` |
| **Integration events** | Immutable `record`s in `FinTrack.Contracts`, past-tense names | `TransactionCreatedEvent.cs`, `UserRegisteredEvent.cs` |
| **`IPublishEndpoint`** | Injected into handlers to publish events | Handler constructors |
| **`AddConsumers()`** | Registers consumer classes in the Host | `FinTrack.Host/Program.cs` |
| **RabbitMQ config** | Host, username, password, virtual host | `appsettings.json` |
| **RabbitMQ Management UI** | `http://localhost:15672` — see queues, exchanges, message rates | Browser |

**Resources:**

- [MassTransit documentation](https://masstransit.io/documentation)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/tutorials)
- [Event-driven architecture (Microsoft)](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven)

---

## Tier 7: Modular Monolith & Vertical Slice Architecture

These are the **architectural patterns** that give FinTrack its shape.

| Concept | Where it appears | Learn |
|---------|-----------------|-------|
| **Modular monolith** | 6 modules as class libraries, deployed as one process (Api + Host) | Entire `src/` structure |
| **Bounded context** | Each module owns its data and logic; no cross-module entity references | Module boundaries table |
| **No direct references** | Modules only depend on `BuildingBlocks` and `Contracts`, never on each other | `.csproj` references |
| **Vertical Slice** | One feature folder = one use case = command + handler + validator + controller | Every `Features/` folder |
| **Internal by default** | Handlers and validators are `internal`; controllers are `public` | All module files |
| **Public surface** | Each module exposes `Add*Module()` only; controllers are auto-discovered | `DependencyInjection.cs` per module |
| **DI composition** | Api and Host call all module DI extensions in `Program.cs` | Both `Program.cs` files |

**Resources:**

- [Vertical Slice Architecture (Jimmy Bogard)](https://www.youtube.com/watch?v=SUiWfhAhgQw)
- [Modular Monolith (Kamil Grzybek)](https://www.youtube.com/watch?v=ZbK2E_6D7OI)
- [Domain-Driven Design distilled (Vernon)](https://www.amazon.com/Domain-Driven-Design-Distilled-Vaughn-Vernon/dp/0134434420)

---

## Tier 8: Testing

| Concept | Where it appears | Learn |
|---------|-----------------|-------|
| **xUnit** | Test framework — `[Fact]`, `[Theory]`, assertions | All `*.Tests` projects |
| **FluentAssertions** | Readable assertions: `result.Should().Be(...)` | Test files |
| **NSubstitute** | Mocking: `Substitute.For<IRepository<T>>()` | Test files |
| **Unit tests** | Test handlers, validators, domain logic — no database | Mirror of `Features/` in tests |
| **AAA pattern** | Arrange → Act → Assert | Test structure |

**Resources:**

- [xUnit documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [NSubstitute](https://nsubstitute.github.io/)

---

## Tier 9: Docker & DevOps (bonus)

| Concept | Where it appears | Learn |
|---------|-----------------|-------|
| **Docker Compose** | Defines Mongo + RabbitMQ services | `docker-compose.yml` |
| **`docker compose up -d`** | Start both services in background | README |
| **Volumes** | Persist data across container restarts | `docker-compose.yml` volumes section |
| **Container networking** | Services communicate via container names | Implicit in compose |

---

## Suggested learning order

```
Week 1-2:   Tier 0 (C#, .NET CLI, HTTP)
Week 3:     Tier 1 (ASP.NET Core, Controllers, DI)
Week 4:     Tier 2 (MongoDB + Mongo.Entities)
Week 5:     Tier 3 (MediatR + CQRS)
Week 6:     Tier 4 (FluentValidation)
Week 7:     Tier 5 (JWT auth)
Week 8:     Tier 7 (Modular Monolith + VSA — concepts)
Week 9:     Tier 6 (MassTransit + RabbitMQ)
Week 10:    Tier 8 (Testing)
Ongoing:    Tier 9 (Docker as needed)
```

---

## How each tier maps to FinTrack files

| Tier | Key files to study |
|------|-------------------|
| 1 — ASP.NET | `src/FinTrack.Api/Program.cs` |
| 2 — MongoDB | `src/FinTrack.BuildingBlocks/AuditableEntity.cs`, `MongoInitializer.cs`, any `Domain/*.cs` |
| 3 — MediatR | Any `Features/*/Create*Command.cs` + `Create*Handler.cs` + `Controllers/*Controller.cs` |
| 4 — Validation | Any `Features/*/*Validator.cs` |
| 5 — Auth | `src/FinTrack.Modules.Users/Features/*`, `HttpContextCurrentUser.cs` |
| 6 — Messaging | `src/FinTrack.Contracts/*`, `src/FinTrack.Host/Program.cs`, handler `IPublishEndpoint` usage |
| 7 — Architecture | `src/FinTrack.Modules.*/DependencyInjection.cs`, `docs/architecture-modular-monolith-vsa.md` |
| 8 — Testing | `tests/FinTrack.Modules.Transactions.Tests/` |
