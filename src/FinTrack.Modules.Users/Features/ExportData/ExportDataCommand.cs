using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Users.Features.ExportData;

public sealed record ExportDataCommand(
    DateTime? FromDate,
    DateTime? ToDate) : IRequest<Result<ExportDataResponse>>;

public sealed record ExportDataResponse(
    byte[] FileBytes,
    string ContentType,
    string FileName);
