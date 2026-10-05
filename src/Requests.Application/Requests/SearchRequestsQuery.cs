using System.ComponentModel.DataAnnotations;
using Requests.Application.Common;
using Requests.Domain.Entities;

namespace Requests.Application.Requests;

public record SearchRequestsQuery : IValidatableObject
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int RequestNumberMaxLength = 50;

    [MaxLength(RequestNumberMaxLength)]
    public string? RequestNumber { get; init; }

    public RequestStatus[] Status { get; init; } = [];

    public RequestType[] Type { get; init; } = [];

    public DateOnly? CreatedFrom { get; init; }

    public DateOnly? CreatedTo { get; init; }

    public RequestSortField SortBy { get; init; } = RequestSortField.CreatedAt;

    public SortDirection SortDir { get; init; } = SortDirection.Desc;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CreatedFrom > CreatedTo)
        {
            yield return new ValidationResult(
                "createdFrom must be on or before createdTo.",
                [nameof(CreatedFrom)]);
        }
    }
}
