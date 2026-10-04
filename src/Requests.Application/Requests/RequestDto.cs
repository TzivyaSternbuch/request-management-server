using Requests.Domain.Entities;

namespace Requests.Application.Requests;

public record RequestDto(
    int Id,
    string RequestNumber,
    int CustomerId,
    int OwnerId,
    int? AssignedToUserId,
    RequestStatus Status,
    RequestType RequestType,
    DateTime CreatedAt);
