using Requests.Application.Common;

namespace Requests.Application.Requests;

public record RequestSort(RequestSortField Field, SortDirection Direction);
