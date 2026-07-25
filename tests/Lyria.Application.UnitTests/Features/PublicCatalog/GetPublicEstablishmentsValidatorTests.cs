using FluentValidation.TestHelper;
using Lyria.Application.Features.PublicCatalog.GetEstablishments;
using Xunit;

namespace Lyria.Application.UnitTests.Features.PublicCatalog;

public sealed class GetPublicEstablishmentsValidatorTests
{
    private readonly GetPublicEstablishmentsQueryValidator _validator = new();

    [Fact]
    public void Validator_PageLessThan1_HasError()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 0, 20);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void Validator_PageSizeLessThan1_HasError()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 0);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validator_PageSizeGreaterThan100_HasError()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 101);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validator_InvalidSortBy_HasError()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20, "invalid");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.SortBy);
    }

    [Fact]
    public void Validator_InvalidSortDirection_HasError()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20, "name", "invalid");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.SortDirection);
    }

    [Fact]
    public void Validator_ValidQuery_NoErrors()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20, "name", "asc");

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_SortByNewest_NoError()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20, "newest", "desc");

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_SortByBranchCount_NoError()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20, "branchCount", "desc");

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_InvalidComplianceLevel_HasError()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, 99, null, 1, 20);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.ComplianceLevel);
    }

    [Fact]
    public void Validator_ValidComplianceLevel_NoError()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, 1, null, 1, 20);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
