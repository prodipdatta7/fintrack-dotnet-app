using MediatR;

namespace FinTrack.Contracts.Queries;

public sealed record ValidateAccountExistsQuery(string AccountId, string UserId) : IRequest<bool>;

public sealed record ValidateCategoryExistsQuery(string CategoryId, string UserId) : IRequest<bool>;
