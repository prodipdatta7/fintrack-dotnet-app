---
goal: User Profile & Settings Backend Module Implementation Plan
version: 1.0
date_created: 2026-08-02
owner: Antigravity AI
status: 'Completed'
tags: ['feature', 'backend', 'dotnet', 'users']
---

# Introduction

![Status: Completed](https://img.shields.io/badge/status-Completed-brightgreen)

This implementation plan details the backend development for the **Profile & Settings** module in `FinTrack.Modules.Users`. It extracts and expands upon the backend specification defined in `fintrack-angular-app/user_profile_plan.md`.

---

## 1. Requirements & Constraints

- **REQ-001**: Implement `UserSettings` domain entity and MongoDB `user_settings` collection with a unique index on `UserId`.
- **REQ-002**: Extend `User` domain entity to include `AvatarUrl`.
- **REQ-003**: Implement `UpdateProfile` endpoint (`PUT /api/users/me`) to update user first name, last name, and email.
- **REQ-004**: Implement `UploadAvatar` endpoint (`POST /api/users/me/avatar`) accepting image file upload, storing files cleanly via `IFileStorageService` abstraction (`LocalFileStorageService` default saving to `wwwroot/uploads/avatars/`).
- **REQ-005**: Implement `ChangePassword` endpoint (`POST /api/users/me/change-password`) with strict password complexity validation (min 8, max 30, uppercase, lowercase, digit, special character).
- **REQ-006**: Implement `GetSettings` endpoint (`GET /api/users/me/settings`) returning current user preferences with defaults (BDT currency, Asia/Dhaka timezone, dd/MM/yyyy format, page size 10).
- **REQ-007**: Implement `UpdateSettings` endpoint (`PUT /api/users/me/settings`) to update user preference settings.
- **REQ-008**: Implement `ExportData` endpoint (`POST /api/users/data/export`) generating CSV formatted export of user transaction records.
- **REQ-009**: Implement `DeleteAccount` endpoint (`POST /api/users/me/delete-account`) verifying password before removing user account data.
- **REQ-010**: Implement `LogoutAllSessions` endpoint (`POST /api/users/me/logout-all`) revoking all refresh tokens associated with the user.
- **PAT-001**: Follow Vertical Slice Architecture (VSA) and MediatR command/query handlers pattern matching existing `FinTrack.Modules.Users` features.
- **SEC-001**: All endpoints require authentication (`[Authorize]` / `ICurrentUser` validation).

---

## 2. Implementation Steps

### Implementation Phase 1: Domain Entities & Database Setup

- GOAL-001: Update existing domain models and introduce `UserSettings` entity.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-001 | Create `UserSettings.cs` in `src/FinTrack.Modules.Users/Domain/` | ✅ | 2026-08-02 |
| TASK-002 | Add `AvatarUrl` property to `src/FinTrack.Modules.Users/Domain/User.cs` | ✅ | 2026-08-02 |
| TASK-003 | Create `IFileStorageService` interface & `LocalFileStorageService` in `BuildingBlocks` | ✅ | 2026-08-02 |

### Implementation Phase 2: Feature Slices & Endpoints

- GOAL-002: Build the 8 vertical slices under `src/FinTrack.Modules.Users/Features/`.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-004 | Implement `UpdateProfile` (Command, Handler, Controller, Validator) | ✅ | 2026-08-02 |
| TASK-005 | Implement `UploadAvatar` (Command, Handler, Controller) with `IFileStorageService` | ✅ | 2026-08-02 |
| TASK-006 | Implement `ChangePassword` (Command, Handler, Controller, Validator with password strength rules) | ✅ | 2026-08-02 |
| TASK-007 | Implement `GetSettings` (Query, Handler, Controller) | ✅ | 2026-08-02 |
| TASK-008 | Implement `UpdateSettings` (Command, Handler, Controller, Validator) | ✅ | 2026-08-02 |
| TASK-009 | Implement `ExportData` (Command, Handler, Controller) returning CSV file stream | ✅ | 2026-08-02 |
| TASK-010 | Implement `DeleteAccount` (Command, Handler, Controller, Validator) | ✅ | 2026-08-02 |
| TASK-011 | Implement `LogoutAllSessions` (Command, Handler, Controller) | ✅ | 2026-08-02 |

### Implementation Phase 3: Unit Testing & Verification

- GOAL-003: Write comprehensive unit tests for validators, handlers, and password complexity verification.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-012 | Add unit tests for validators (`ChangePasswordValidatorTests`, `UpdateProfileValidatorTests`, `UpdateSettingsValidatorTests`) | ✅ | 2026-08-02 |
| TASK-013 | Verify solution builds cleanly with `dotnet build` and all tests pass with `dotnet test` | ✅ | 2026-08-02 |

---

## 3. Alternatives

- **ALT-001**: Embedding user settings directly inside the `User` MongoDB document. *Rejected*: Separating `UserSettings` into `user_settings` collection allows isolated updates and cleaner domain boundary separation.
- **ALT-002**: Direct file system calls inside controllers for avatar uploads. *Rejected*: Created `IFileStorageService` abstraction in `BuildingBlocks` so storage strategy can easily be swapped for AWS S3 or Cloudinary in production via DI.

---

## 4. Dependencies

- **DEP-001**: `MongoDB.Driver` for database persistence.
- **DEP-002**: `FluentValidation` for command validation.
- **DEP-003**: `MediatR` for CQRS dispatching.
- **DEP-004**: `FinTrack.BuildingBlocks.Auth.ICurrentUser` for authenticated user context.
- **DEP-005**: `FinTrack.BuildingBlocks.Storage.IFileStorageService` for file management.

---

## 5. Files

- **FILE-001**: `src/FinTrack.BuildingBlocks/Storage/IFileStorageService.cs` (NEW)
- **FILE-002**: `src/FinTrack.BuildingBlocks/Storage/LocalFileStorageService.cs` (NEW)
- **FILE-003**: `src/FinTrack.Modules.Users/Domain/UserSettings.cs` (NEW)
- **FILE-004**: `src/FinTrack.Modules.Users/Domain/User.cs` (MODIFY)
- **FILE-005**: `src/FinTrack.Modules.Users/Features/UpdateProfile/*` (NEW - 4 files)
- **FILE-006**: `src/FinTrack.Modules.Users/Features/UploadAvatar/*` (NEW - 3 files)
- **FILE-007**: `src/FinTrack.Modules.Users/Features/ChangePassword/*` (NEW - 4 files)
- **FILE-008**: `src/FinTrack.Modules.Users/Features/GetSettings/*` (NEW - 3 files)
- **FILE-009**: `src/FinTrack.Modules.Users/Features/UpdateSettings/*` (NEW - 4 files)
- **FILE-010**: `src/FinTrack.Modules.Users/Features/ExportData/*` (NEW - 3 files)
- **FILE-011**: `src/FinTrack.Modules.Users/Features/DeleteAccount/*` (NEW - 4 files)
- **FILE-012**: `src/FinTrack.Modules.Users/Features/LogoutAllSessions/*` (NEW - 3 files)
- **FILE-013**: `tests/FinTrack.Modules.Users.Tests/Features/*` (NEW - unit test files)

---

## 6. Testing

- **TEST-001**: Validate password strength criteria (min length 8, max length 30, upper, lower, digit, special character).
- **TEST-002**: Verify profile and settings updates persist correctly for the authenticated user.
- **TEST-003**: Verify avatar file upload saves file and updates `AvatarUrl`.
- **TEST-004**: Verify `LogoutAllSessions` revokes all active refresh tokens for the user.

---

## 7. Risks & Assumptions

- **ASSUMPTION-001**: File uploads for avatars are stored locally under `wwwroot/uploads/avatars` via `LocalFileStorageService`, returning relative URL `/uploads/avatars/{filename}`.
- **ASSUMPTION-002**: `ExportData` generates a CSV file stream of transaction history filtered by `FromDate` and `ToDate`.

---

## 8. Related Specifications / Further Reading

- [fintrack-angular-app Profile & Settings Spec](file:///D:/Local-Projects/fintrack-angular-app/user_profile_plan.md)
