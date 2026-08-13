# Vertical Slice Architecture (VSA) — Request Execution Flow & Lifecycle

This document explains the end-to-end execution flow of an HTTP request in `FinTrack.Api` following **Vertical Slice Architecture (VSA)**, detailing how Controllers, Validators, and Handlers interact without direct dependencies.

---

## 1. Overview & Core Concept

In `fintrack-dotnet-app`, features are organized into self-contained slices under `Features/{FeatureName}/`. Controllers do **not** directly instantiate or call Validators or Handlers. 

Instead:
- The **Controller** receives the HTTP request and dispatches a Command/Query object via MediatR (`_sender.Send(command)`).
- **MediatR Pipeline (`ValidationBehavior`)** intercepts the request and runs FluentValidation rules.
- If validation passes, MediatR dispatches the request to the matching **Handler** (`IRequestHandler<TRequest, TResponse>`).

---

## 2. Request Lifecycle & Execution Flow

```
[ HTTP Request ]
       │
       ▼
1. Feature Controller        ---> Calls _sender.Send(command)
       │
       ▼
2. MediatR Pipeline (ValidationBehavior)
       │
       ├──► Finds Validator matching AbstractValidator<TCommand> in DI
       ├──► Executes validation rules
       │    ├── If Failed  --> Throws ValidationException (Returns 400 Bad Request)
       │    └── If Passed  --> Calls next()
       ▼
3. Feature Handler           ---> Executes business logic & MongoDB queries
       │
       ▼
[ HTTP Response ]
```

---

## 3. Step-by-Step Code Example

Using `UpdateProfile` feature slice (`src/FinTrack.Modules.Users/Features/UpdateProfile/`):

### Step 1: Controller Dispatches Command
File: `UpdateProfileController.cs`
```csharp
[ApiController]
[Route("api/users")]
public sealed class UpdateProfileController : ControllerBase
{
    private readonly ISender _sender;

    public UpdateProfileController(ISender sender) => _sender = sender;

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command, CancellationToken ct)
    {
        // Controller delegates execution to MediatR bus.
        // It does NOT depend on UpdateProfileValidator or UpdateProfileHandler directly.
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
```

### Step 2: MediatR Pipeline Interceptor Runs Validation
File: `src/FinTrack.BuildingBlocks/Behaviors/ValidationBehavior.cs`
```csharp
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(); // Skip if no validator exists for this command

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next(); // Proceed to Handler
    }
}
```

### Step 3: Handler Executes Business Logic
File: `UpdateProfileHandler.cs`
```csharp
internal sealed class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileResponse>>
{
    private readonly IMongoCollection<User> _users;
    private readonly ICurrentUser _currentUser;

    public UpdateProfileHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _users = database.GetCollection<User>("users");
        _currentUser = currentUser;
    }

    public async Task<Result<UpdateProfileResponse>> Handle(
        UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        // Business logic, MongoDB query/update, domain logic...
    }
}
```

---

## 4. Configuration & Assembly Registration

Everything is auto-wired via Assembly Scanning and DI registration:

### A. Module Assembly Scanning (`FinTrack.Modules.Users/DependencyInjection.cs`)
```csharp
public static IServiceCollection AddUsersModule(this IServiceCollection services)
{
    // Scans assembly and registers all IRequestHandler<TRequest, TResponse>
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

    // Scans assembly and registers all AbstractValidator<T>
    services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

    return services;
}
```

### B. Global Pipeline Middleware Registration (`FinTrack.Api/Program.cs`)
```csharp
// Registers ValidationBehavior middleware for all MediatR commands/queries
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

---

## 5. Interface Matching Reference Table

| Component | Implemented Interface | Matching Criterion |
|---|---|---|
| **Command / Query** | `IRequest<TResponse>` | Payload contract defining input and return type |
| **Validator** | `AbstractValidator<TCommand>` | Matched automatically by FluentValidation & DI |
| **Handler** | `IRequestHandler<TCommand, TResponse>` | Matched automatically by MediatR |
