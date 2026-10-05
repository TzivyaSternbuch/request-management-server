using System.ComponentModel.DataAnnotations;
using Requests.Application.Common;
using Requests.Application.Requests;
using Xunit;

namespace Requests.Tests;

public class SearchRequestsQueryTests
{
    [Fact]
    public void SearchRequestsQuery_PageSizeAbove100_IsInvalid()
    {
        var errors = Validate(new SearchRequestsQuery { PageSize = 101 });

        AssertSingleErrorFor(nameof(SearchRequestsQuery.PageSize), errors);
    }

    [Fact]
    public void SearchRequestsQuery_RequestNumberTooLong_IsInvalid()
    {
        var errors = Validate(new SearchRequestsQuery { RequestNumber = new string('1', 51) });

        AssertSingleErrorFor(nameof(SearchRequestsQuery.RequestNumber), errors);
    }

    [Fact]
    public void SearchRequestsQuery_CreatedFromAfterCreatedTo_IsInvalid()
    {
        var errors = Validate(new SearchRequestsQuery
        {
            CreatedFrom = new DateOnly(2026, 3, 2),
            CreatedTo = new DateOnly(2026, 3, 1)
        });

        AssertSingleErrorFor(nameof(SearchRequestsQuery.CreatedFrom), errors);
    }

    [Fact]
    public void SearchRequestsQuery_SortDirCountDiffersFromSortBy_IsInvalid()
    {
        var errors = Validate(new SearchRequestsQuery
        {
            SortBy = [RequestSortField.Status, RequestSortField.CreatedAt],
            SortDir = [SortDirection.Asc]
        });

        AssertSingleErrorFor(nameof(SearchRequestsQuery.SortDir), errors);
    }

    [Fact]
    public void GetSorts_Defaults_NewestFirst()
    {
        var sorts = new SearchRequestsQuery().GetSorts();

        Assert.Equal([new RequestSort(RequestSortField.CreatedAt, SortDirection.Desc)], sorts);
    }

    private static List<ValidationResult> Validate(SearchRequestsQuery query)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(query, new ValidationContext(query), errors, validateAllProperties: true);
        return errors;
    }

    private static void AssertSingleErrorFor(string memberName, List<ValidationResult> errors)
    {
        var error = Assert.Single(errors);
        Assert.Equal([memberName], error.MemberNames);
    }
}
