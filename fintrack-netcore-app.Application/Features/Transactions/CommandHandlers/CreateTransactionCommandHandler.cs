using fintrack_netcore_app.Application.Features.Transactions.Commands;
using fintrack_netcore_app.Application.Interfaces;
using fintrack_netcore_app.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fintrack_netcore_app.Application.Features.Transactions.CommandHandlers
{
    public class CreateTransactionCommandHandler(IRepository<Transaction> repository, ILogger<CreateTransactionCommandHandler> logger) : IRequestHandler<CreateTransactionCommand, string>
    {
        private readonly ILogger<CreateTransactionCommandHandler> _logger = logger;
        private readonly IRepository<Transaction> _repository = repository;

        public async Task<string> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
        {
            var transaction = new Transaction
            {
                ItemId = Guid.NewGuid().ToString(),
                Title = request.Title,
                Amount = request.Amount,
                Type = request.Type,
                CreatedBy = "System",
                CreateDate = DateTime.UtcNow,
                LastUpdatedBy = "System",
                LastUpdateDate = DateTime.UtcNow,
                TimeZoneOffsetInMinutes = request.TimeZoneOffsetInMinutes
            };
            var response = await _repository.SaveAsync(transaction, cancellationToken);
            _logger.LogInformation("[DbResponse] CreateTransactionCommandHandler: {Response}", JsonConvert.SerializeObject(response));
            
            return transaction.ItemId;
        }
    }
}
