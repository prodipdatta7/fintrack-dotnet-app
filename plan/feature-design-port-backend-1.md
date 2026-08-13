---
goal: Backend Support for the FinTrack UI Design Port — Accounts, Savings Plans, Dashboard Aggregates
version: 1.0
date_created: 2026-08-11
owner: prodip-selise
status: 'In progress'
tags: ['feature', 'backend', 'dotnet', 'accounts', 'budgets', 'dashboard', 'transactions']
---

# Introduction

![Status: In progress](https://img.shields.io/badge/status-In%20progress-yellow)

> **2026-08-12:** Phase 1 (Accounts) shipped — see task table below. Phases 2–6 remain open.
> Also: the MassTransit Mongo outbox is now config-gated (`MassTransit:UseMongoOutbox`, off in
> Development) — on standalone Mongo it wrapped every publish in an unsupported transaction, which
> 500'd registration and silently skipped the `UserRegistered` seeding consumers (extends CON-001).

The Angular client is being rebuilt against the design in
`fintrack-angular-app/docs/fintrack_financial_expenses_planning_app.tsx`. That design introduces three
concepts the API does not serve today — **payment/storage accounts**, **savings plans**, and **dashboard
aggregates** — plus smaller additions to categories and transactions.

This plan fills in the three modules that are currently stubs and extends two that exist. The frontend
counterpart lives at `fintrack-angular-app/plan.md` and `fintrack-angular-app/docs/plans/`.

**Current state of the affected modules:**

| Module | Today |
|---|---|
| `FinTrack.Modules.Accounts` | `Domain/Account.cs` + `EventHandlers/ValidateAccountExistsHandler.cs` only — **no controllers, no features** |
| `FinTrack.Modules.Budgets` | Empty — `DependencyInjection.cs` only |
| `FinTrack.Modules.Dashboard` | Empty — `DependencyInjection.cs` only |
| `FinTrack.Modules.Categories` | Complete; needs a `BudgetLimit` field |
| `FinTrack.Modules.Transactions` | Complete incl. event sourcing; needs extra query filters, a `Note` field, and richer events |

All three stub modules are already registered in `Program.cs` (`AddAccountsModule`, `AddBudgetsModule`,
`AddDashboardModule`) **and** added as MVC application parts, so new controllers are discovered
automatically — no host wiring changes are required.

---

## 1. Requirements & Constraints

### Accounts
- **REQ-001**: Extend `Account` with `Icon`, `Provider` and `Color`; reuse `AuditableEntity.CreatedAt` for the UI's "Date Added" — do **not** add a duplicate date field.
- **REQ-002**: `GET /api/accounts` returns the caller's non-closed accounts plus the portfolio total.
- **REQ-003**: `GET /api/accounts/{id}` returns a single account, 404 for unknown or foreign ids.
- **REQ-004**: `POST /api/accounts`, `PUT /api/accounts/{id}` full create/update.
- **REQ-005**: `PATCH /api/accounts/{id}/balance` adjusts only the balance — powers the inline balance edit that appears on both the dashboard hub and the account detail page.
- **REQ-006**: `DELETE /api/accounts/{id}` is a **soft delete** (`IsClosed = true`); transactions referencing the account must remain readable.
- **REQ-007**: Seed a default account set for new users, mirroring `SeedDefaultCategoriesConsumer`.

### Savings Plans (Budgets module)
- **REQ-008**: New `SavingsPlan` entity in `FinTrack.Modules.Budgets`, collection `savings_plans`.
- **REQ-009**: Full CRUD at `/api/plans` (+ `GET`, `PUT`, `DELETE` by id).
- **REQ-010**: `POST /api/plans/{id}/deposit` adds a positive amount to `CurrentAmount` and returns the updated plan.

### Categories
- **REQ-011**: Add `decimal BudgetLimit` to `Category` (0 = no cap) and thread it through DTO, create/update commands and validators (`>= 0`).

### Dashboard
- **REQ-012**: `GET /api/dashboard/summary` returns `TotalIncome`, `TotalExpense`, `NetSavings`, `CategorySpent[]` and the 5 most recent transactions, scoped to the caller and an optional date range **and optional `accountId`**.
- **REQ-013**: `GET /api/dashboard/cashflow` returns an income/expense series bucketed by `timeframe` (`7D`, `15D`, `30D`, `60D`, `6M`, `1Y`, `Custom`), optionally filtered by `accountId`.
- **REQ-014**: Aggregation runs in MongoDB (`$match` + `$group`), never by loading documents into memory.
- **REQ-015**: Empty buckets return **zero**. The design mock fabricates values for empty periods (`inc || (150 + Math.sin(i) * 50)`); that is demo scaffolding and must not be reproduced.

### Transactions
- **REQ-016**: Extend `GetTransactionsQuery` with `AccountId`, `MinAmount`, `MaxAmount`, `SearchTerm` and `SortBy` (`date-desc` default, `date-asc`, `amount-desc`, `amount-asc`, `title-asc`). `FromDate`/`ToDate` already exist.
- **REQ-017**: Add `Note` to `Transaction`, its DTO, and the create/update commands.
- **REQ-018**: Enrich `TransactionEvent` with `PerformedBy` (display name/email) and `Detail` (human-readable change description); expose both on `TransactionEventDto`.

### Cross-cutting
- **PAT-001**: Vertical Slice Architecture — one folder per feature under `Features/`, each with `{Name}Command|Query.cs`, `{Name}Handler.cs`, `{Name}Controller.cs` and, for writes, `{Name}Validator.cs`.
- **PAT-002**: Handlers are `internal sealed`, return `Result<T>` / `Result`, take `IMongoDatabase` + `ICurrentUser` by constructor, and resolve collections in the constructor.
- **PAT-003**: Cross-module reads go through MediatR contracts in `FinTrack.Contracts.Queries` (as `ValidateAccountExistsQuery` already does) — no direct reference between module assemblies.
- **PAT-004**: Cross-module side effects go through MassTransit integration events in `FinTrack.Contracts.IntegrationEvents`.
- **SEC-001**: Every endpoint is authenticated (the host applies a default-deny fallback policy) and every query filters on `_currentUser.UserId`.
- **CON-001**: MongoDB may run standalone without a replica set — any multi-document write must keep the existing try/catch fallback used by `CreateTransactionHandler`.

---

## 2. Implementation Steps

### Phase 1 — Accounts module

- GOAL-001: Turn `FinTrack.Modules.Accounts` into a complete vertical-slice module.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-001 | Extend `Domain/Account.cs`: `Icon` (emoji), `Provider`, `Color` (default `#6366f1`) | ✅ | 2026-08-12 |
| TASK-002 | `Features/GetAccounts/` — returns `AccountDto[]` + `TotalBalance`; excludes `IsClosed`; sorted by `CreatedAt`. **Deviation:** takes `?includeClosed=true` (resolves QST-001); `TotalBalance` always sums non-closed only | ✅ | 2026-08-12 |
| TASK-003 | `Features/GetAccount/` — by id, user-scoped, `Result.Failure` → 404 (returns closed accounts — the detail page renders them read-only) | ✅ | 2026-08-12 |
| TASK-004 | `Features/CreateAccount/` + validator (name required ≤ 60, balance ≥ 0, valid hex color, `AccountType` in `Bank\|MFS\|Cash\|Credit`) | ✅ | 2026-08-12 |
| TASK-005 | `Features/UpdateAccount/` + validator. **Deviation:** command carries no `Balance` — balance changes only via TASK-006, so a stale edit form cannot clobber an inline adjustment | ✅ | 2026-08-12 |
| TASK-006 | `Features/UpdateAccountBalance/` — `PATCH /{id}/balance`, body `{ balance }`, validator `balance >= 0`; sets `ModifiedAt`; rejects closed accounts | ✅ | 2026-08-12 |
| TASK-007 | Implemented as `Features/SetAccountStatus/` — `PATCH /{id}/status` body `{ isClosed }` closes **and reopens** (the plan had no reopen path); `DELETE /{id}` remains as a thin alias for close | ✅ | 2026-08-12 |
| TASK-008 | `EventHandlers/SeedDefaultAccountsConsumer.cs` — `IConsumer<UserRegistered>`, seeds `Bank Account`, `bKash Wallet`, `Nagad Wallet`, `Cash in Hand` with zero balances (icons/providers/colors from the design mock; **balances start at 0**, not the mock's demo figures) | ✅ | 2026-08-12 |
| TASK-009 | Register the consumer — `Program.cs` currently calls `cfg.AddConsumers(typeof(FinTrack.Modules.Categories.DependencyInjection).Assembly)` only; add the Accounts assembly | ✅ | 2026-08-12 |
| TASK-010 | **Deviation:** non-unique index on `UserId` only. The unique `{ UserId, Name }` index was dropped — a duplicate-key error would surface as an unhandled 500 (no exception middleware), and name uniqueness is not a frontend requirement | ✅ | 2026-08-12 |

### Phase 2 — Savings Plans (Budgets module)

- GOAL-002: Build the savings-plan slice set in the empty `FinTrack.Modules.Budgets`.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-011 | `Domain/SavingsPlan.cs : AuditableEntity` — `Title`, `TargetAmount`, `CurrentAmount`, `Color`, `Deadline` (`DateTime`), collection `savings_plans` | ✅ | 2026-08-12 |
| TASK-012 | `Features/GetPlans/` — user-scoped, sorted by `Deadline` | ✅ | 2026-08-12 |
| TASK-013 | `Features/CreatePlan/` + validator (title required, `TargetAmount > 0`, `CurrentAmount >= 0`, `Deadline` in the future) | ✅ | 2026-08-12 |
| TASK-014 | `Features/UpdatePlan/` + validator | ✅ | 2026-08-12 |
| TASK-015 | `Features/DepositToPlan/` — `POST /{id}/deposit`, `{ amount }`, validator `amount > 0`; `$inc` on `CurrentAmount`; returns the updated plan | ✅ | 2026-08-12 |
| TASK-016 | `Features/DeletePlan/` — hard delete (plans carry no ledger references) | ✅ | 2026-08-12 |
| TASK-017 | Index on `UserId` | ✅ | 2026-08-12 |

### Phase 3 — Category budget caps

- GOAL-003: Let categories carry a monthly spending cap.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-018 | Add `decimal BudgetLimit` to `Domain/Category.cs` (default `0` = unlimited) | ✅ | 2026-08-12 |
| TASK-019 | Add `BudgetLimit` to `CategoryDto` (`GetCategories`, `GetCategory`) | ✅ | 2026-08-12 |
| TASK-020 | Add `BudgetLimit` to `CreateCategoryCommand` / `UpdateCategoryCommand` + validators (`>= 0`) | ✅ | 2026-08-12 |
| TASK-021 | Give the six seeded defaults in `SeedDefaultCategoriesConsumer` a `BudgetLimit` of `0` (explicit, so the intent is readable) | ✅ | 2026-08-12 |

### Phase 4 — Dashboard aggregates

- GOAL-004: Serve the figures the dashboard and account pages render, computed in the database.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-022 | `Features/GetDashboardSummary/` — query `(DateTime? From, DateTime? To, string? AccountId)`; single `$facet` aggregation producing totals, per-category spend and the 5 latest transactions | ✅ | 2026-08-12 |
| TASK-023 | `GetDashboardSummaryController` — `GET /api/dashboard/summary` | ✅ | 2026-08-12 |
| TASK-024 | `Features/GetCashflowSeries/` — query `(string Timeframe, DateTime? From, DateTime? To, string? AccountId)`; bucket boundaries per REQ-013; `$group` by bucket key; zero-fill missing buckets **server-side** so the client receives a dense series | ✅ | 2026-08-12 |
| TASK-025 | `GetCashflowSeriesController` — `GET /api/dashboard/cashflow` | ✅ | 2026-08-12 |
| TASK-026 | Bucket label formats to match the design: `MMM d` for day buckets (`7D`/`15D`/`30D`/`60D`/`Custom`), `MMM` for month buckets (`6M`/`1Y`) | ✅ | 2026-08-12 |
| TASK-027 | Validator: `Timeframe` in the allowed set; `Custom` requires both `From` and `To` with `From <= To`; cap the range at 366 days | ✅ | 2026-08-12 |
| TASK-028 | Compound index on `{ UserId, Date }` in `transactions` (this is the aggregation's driving filter) | ✅ | 2026-08-12 |

### Phase 5 — Transaction extensions

- GOAL-005: Support server-side advanced filtering, notes, and a legible audit trail.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-029 | Extend `GetTransactionsQuery` with `AccountId`, `MinAmount`, `MaxAmount`, `SearchTerm`, `SortBy` | ✅ | 2026-08-12 |
| TASK-030 | Apply them in `GetTransactionsHandler` — `SearchTerm` is a case-insensitive regex over `Title` **and** `Note`; `SortBy` maps to a `SortDefinition`; unknown values fall back to `date-desc` | ✅ | 2026-08-12 |
| TASK-031 | Add `Note` to `Domain/Transaction.cs`, `TransactionDto`, `CreateTransactionCommand`, `UpdateTransactionCommand` (validator: ≤ 500 chars) | ✅ | 2026-08-12 |
| TASK-032 | Add `PerformedBy` and `Detail` to `Domain/TransactionEvent.cs` and `TransactionEventDto` | ✅ | 2026-08-12 |
| TASK-033 | Populate them in `CreateTransactionHandler` (`Created manual record entry`), `UpdateTransactionHandler` (field-level diff, e.g. `Amount $120.00 → $145.50`), `DeleteTransactionHandler` (`Record removed from ledger`); `PerformedBy = _currentUser.Email` | ✅ | 2026-08-12 |
| TASK-034 | Backfill note: existing `transaction_events` documents have no `PerformedBy`/`Detail` — the DTO must tolerate empty strings, and the UI falls back to `System` | ✅ | 2026-08-12 |

### Phase 6 — Balance projection (recommended, gated)

- GOAL-006: Keep account balances truthful without the client recomputing them.

`TransactionCreated` and `TransactionUpdated` already carry `AccountId`, `Amount`, `PreviousAmount` and
`Type` in `FinTrack.Contracts.IntegrationEvents`, and the Accounts module already has an `EventHandlers/`
folder — the projection is nearly free.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-035 | Add `TransactionDeleted` to `FinTrack.Contracts.IntegrationEvents` (`TransactionId`, `UserId`, `AccountId`, `Amount`, `Type`) and publish it from `DeleteTransactionHandler` | ✅ | 2026-08-12 |
| TASK-036 | `EventHandlers/UpdateAccountBalanceConsumer.cs` — `$inc` the account balance: `+Amount` for income, `-Amount` for expense; on update apply the delta against `PreviousAmount`; on delete reverse the original | ✅ | 2026-08-12 |
| TASK-037 | Decide and document precedence between the projection and the manual `PATCH /balance` (the design exposes manual balance editing). **Recommend:** manual edit sets an absolute value and the projection continues from there; log both as balance-change events if an audit trail is wanted later | ✅ | 2026-08-12 |

> If Phase 6 is deferred, the manual `PATCH` from REQ-005 is the only way balances move — which matches the
> design mock's behaviour, and Phases 1–5 remain fully usable.

---

## 3. Alternatives Considered

- **ALT-001: Compute dashboard figures client-side.** Rejected — `/api/transactions` is paginated, so the
  client only ever holds one page; totals derived from it would be wrong (this is exactly what the design
  mock does, over a hard-coded in-memory array).
- **ALT-002: Put savings plans in their own module.** Rejected — `FinTrack.Modules.Budgets` exists, is empty,
  and budgets/goals are the same bounded context.
- **ALT-003: Hard-delete accounts.** Rejected — historical transactions reference `AccountId`; a hard delete
  would orphan them. Soft delete keeps the ledger readable.
- **ALT-004: Store the account's "added date" as a separate field.** Rejected — `AuditableEntity.CreatedAt`
  already carries it.

## 4. Dependencies

- **DEP-001**: MongoDB.Driver aggregation pipelines (`$facet`, `$group`) — already in use.
- **DEP-002**: MassTransit in-memory bus + Mongo outbox — already configured in `Program.cs`.
- **DEP-003**: No new NuGet packages.

## 5. Files

| File | Change |
|---|---|
| `src/FinTrack.Modules.Accounts/Domain/Account.cs` | MODIFY — `Icon`, `Provider`, `Color` |
| `src/FinTrack.Modules.Accounts/Features/{GetAccounts,GetAccount,CreateAccount,UpdateAccount,UpdateAccountBalance,DeleteAccount}/` | NEW |
| `src/FinTrack.Modules.Accounts/EventHandlers/SeedDefaultAccountsConsumer.cs` | NEW |
| `src/FinTrack.Modules.Accounts/EventHandlers/UpdateAccountBalanceConsumer.cs` | NEW (Phase 6) |
| `src/FinTrack.Modules.Budgets/Domain/SavingsPlan.cs` | NEW |
| `src/FinTrack.Modules.Budgets/Features/{GetPlans,CreatePlan,UpdatePlan,DepositToPlan,DeletePlan}/` | NEW |
| `src/FinTrack.Modules.Dashboard/Features/{GetDashboardSummary,GetCashflowSeries}/` | NEW |
| `src/FinTrack.Modules.Categories/Domain/Category.cs` | MODIFY — `BudgetLimit` |
| `src/FinTrack.Modules.Categories/Features/{GetCategories,GetCategory,CreateCategory,UpdateCategory}/` | MODIFY — thread `BudgetLimit` |
| `src/FinTrack.Modules.Categories/EventHandlers/SeedDefaultCategoriesConsumer.cs` | MODIFY — explicit `BudgetLimit = 0` |
| `src/FinTrack.Modules.Transactions/Domain/{Transaction,TransactionEvent}.cs` | MODIFY — `Note`; `PerformedBy`, `Detail` |
| `src/FinTrack.Modules.Transactions/Features/GetTransactions/*` | MODIFY — new filters + sort |
| `src/FinTrack.Modules.Transactions/Features/{CreateTransaction,UpdateTransaction,DeleteTransaction}/*Handler.cs` | MODIFY — event enrichment, `Note`, `TransactionDeleted` publish |
| `src/FinTrack.Modules.Transactions/Features/GetTransactionEvents/TransactionEventDto.cs` | MODIFY |
| `src/FinTrack.Contracts/IntegrationEvents/*.cs` | MODIFY — add `TransactionDeleted` |
| `src/FinTrack.Api/Program.cs` | MODIFY — register the Accounts assembly with `cfg.AddConsumers` |

## 6. Testing

Test projects already exist for all five affected modules.

| Project | Tests |
|---|---|
| `FinTrack.Modules.Accounts.Tests` | Validators (name, balance, color, type); `GetAccounts` total + `IsClosed` exclusion; balance patch sets `ModifiedAt`; soft delete; seed consumer creates four accounts once |
| `FinTrack.Modules.Budgets.Tests` | Plan validators (target > 0, deadline future); deposit rejects `<= 0` and increments correctly; user scoping |
| `FinTrack.Modules.Dashboard.Tests` | Summary totals + per-category spend; recent list capped at 5; **zero-fill for empty buckets** (guards REQ-015); each timeframe's bucket count and labels; `Custom` validation; `accountId` filter |
| `FinTrack.Modules.Categories.Tests` | `BudgetLimit` round-trip; validator rejects negatives |
| `FinTrack.Modules.Transactions.Tests` | Each new filter narrows results; `SortBy` ordering incl. unknown-value fallback; search matches `Note`; created/updated/deleted events carry `PerformedBy` + `Detail`; update diff wording |

```bash
dotnet test D:\Local-Projects\fintrack-dotnet-app\FinTrack.slnx
```

Manual: run `FinTrack.Api`, register a fresh user, confirm four accounts and six categories seed, then
exercise `/api/accounts`, `/api/plans`, `/api/dashboard/summary` and `/api/dashboard/cashflow?timeframe=6M`
via Swagger.

## 7. Risks & Open Questions

- **RISK-001**: Dashboard aggregations without the `{ UserId, Date }` index will table-scan as ledgers grow — TASK-028 is not optional.
- **RISK-002**: Timeframe bucketing must use the caller's timezone or day boundaries drift. Transactions already store `TimeZoneOffsetInMinutes`; decide whether cashflow buckets use it or plain UTC, and document the choice. **Recommend:** UTC for v1, revisit if users report off-by-one days.
- **RISK-003**: Phase 6 changes what "balance" means (derived vs. manual). Ship Phases 1–5 first and treat Phase 6 as an explicit follow-up decision.
- **QST-001** *(resolved 2026-08-12)*: Closed accounts are excluded from `GET /api/accounts` by default; `?includeClosed=true` lists them. The `/accounts` management page in the Angular app fetches with the flag and hides closed accounts behind a "Show closed" toggle; the dashboard hub and transaction pickers use the default and never see them. See `fintrack-angular-app/docs/plans/10-account-management.md`.
