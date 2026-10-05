using System.ComponentModel.DataAnnotations;
using Requests.Application.Requests;
using Xunit;

namespace Requests.Tests;

public class SearchRequestsQueryTests
{
    [Fact]
    public void SearchRequestsQuery_Defaults_AreValid()
    {
        var errors = Validate(new SearchRequestsQuery());

        Assert.Empty(errors);
    }

    [Fact]
    public void SearchRequestsQuery_PageLessThanOne_IsInvalid()
    {
        var errors = Validate(new SearchRequestsQuery { Page = 0 });

        AssertSingleErrorFor(nameof(SearchRequestsQuery.Page), errors);
    }

    [Fact]
    public void SearchRequestsQuery_PageSizeZero_IsInvalid()
    {
        var errors = Validate(new SearchRequestsQuery { PageSize = 0 });

        AssertSingleErrorFor(nameof(SearchRequestsQuery.PageSize), errors);
    }

    [Fact]
    public void SearchRequestsQuery_PageSizeAbove100_IsInvalid()
    {
        var errors = Validate(new SearchRequestsQuery { PageSize = 101 });

        AssertSingleErrorFor(nameof(SearchRequestsQuery.PageSize), errors);
    }

    [Fact]
    public void SearchRequestsQuery_PageSize100_IsValid()
    {
        var errors = Validate(new SearchRequestsQuery { PageSize = 100 });

        Assert.Empty(errors);
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
    public void SearchRequestsQuery_CreatedFromEqualsCreatedTo_IsValid()
    {
        var errors = Validate(new SearchRequestsQuery
        {
            CreatedFrom = new DateOnly(2026, 3, 1),
            CreatedTo = new DateOnly(2026, 3, 1)
        });

        Assert.Empty(errors);
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
