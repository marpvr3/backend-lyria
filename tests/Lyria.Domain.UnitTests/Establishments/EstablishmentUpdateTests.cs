using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments;

public sealed class EstablishmentUpdateTests
{
    [Fact]
    public void UpdateDetails_UpdatesAllFields()
    {
        var establishment = CreateEstablishment();

        establishment.UpdateDetails(
            "New Name", "new-slug", "New desc", "https://new.com", "@new", null, null, null);

        Assert.Equal("New Name", establishment.Name);
        Assert.Equal("new-slug", establishment.Slug);
        Assert.Equal("New desc", establishment.Description);
        Assert.Equal("https://new.com", establishment.Website);
        Assert.Equal("@new", establishment.Instagram);
    }

    [Fact]
    public void UpdateDetails_NormalizesName()
    {
        var establishment = CreateEstablishment();

        establishment.UpdateDetails(
            "  New   Name  ", "new-slug", null, null, null, null, null, null);

        Assert.Equal("New Name", establishment.Name);
    }

    [Fact]
    public void UpdateDetails_NormalizesSlug()
    {
        var establishment = CreateEstablishment();

        establishment.UpdateDetails(
            "Name", "  NEW-SLUG  ", null, null, null, null, null, null);

        Assert.Equal("new-slug", establishment.Slug);
    }

    [Fact]
    public void UpdateDetails_RejectsEmptyName()
    {
        var establishment = CreateEstablishment();

        Assert.Throws<EstablishmentException>(() =>
            establishment.UpdateDetails("", "slug", null, null, null, null, null, null));
    }

    [Fact]
    public void UpdateDetails_RejectsInvalidSlug()
    {
        var establishment = CreateEstablishment();

        Assert.Throws<EstablishmentException>(() =>
            establishment.UpdateDetails("Name", "INVALID SLUG", null, null, null, null, null, null));
    }

    [Fact]
    public void UpdateDetails_RejectsInvalidWebsite()
    {
        var establishment = CreateEstablishment();

        Assert.Throws<EstablishmentException>(() =>
            establishment.UpdateDetails("Name", "slug", null, "not-a-url", null, null, null, null));
    }

    [Fact]
    public void ChangeCategory_UpdatesCategoryId()
    {
        var establishment = CreateEstablishment();
        var newCategoryId = EstablishmentCategoryId.New();

        establishment.ChangeCategory(newCategoryId);

        Assert.Equal(newCategoryId, establishment.CategoryId);
    }

    [Fact]
    public void ChangeCategory_RejectsEmptyId()
    {
        var establishment = CreateEstablishment();

        Assert.Throws<EstablishmentException>(() =>
            establishment.ChangeCategory(new EstablishmentCategoryId(Guid.Empty)));
    }

    [Fact]
    public void Verify_SetsVerifiedToTrue()
    {
        var establishment = CreateEstablishment();

        establishment.Verify(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));

        Assert.True(establishment.IsVerified);
    }

    [Fact]
    public void Verify_IsIdempotent()
    {
        var establishment = CreateEstablishment();

        establishment.Verify(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));
        establishment.Verify(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));

        Assert.True(establishment.IsVerified);
    }

    [Fact]
    public void Unverify_SetsVerifiedToFalse()
    {
        var establishment = CreateEstablishment();
        establishment.Verify(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));

        establishment.RevokeVerification();

        Assert.False(establishment.IsVerified);
    }

    [Fact]
    public void Unverify_IsIdempotent()
    {
        var establishment = CreateEstablishment();

        establishment.RevokeVerification();
        establishment.RevokeVerification();

        Assert.False(establishment.IsVerified);
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        var establishment = CreateEstablishment();
        establishment.Deactivate();

        establishment.Activate();

        Assert.True(establishment.IsActive);
    }

    [Fact]
    public void Activate_IsIdempotent()
    {
        var establishment = CreateEstablishment();

        establishment.Activate();
        establishment.Activate();

        Assert.True(establishment.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var establishment = CreateEstablishment();

        establishment.Deactivate();

        Assert.False(establishment.IsActive);
    }

    [Fact]
    public void Deactivate_IsIdempotent()
    {
        var establishment = CreateEstablishment();

        establishment.Deactivate();
        establishment.Deactivate();

        Assert.False(establishment.IsActive);
    }

    [Fact]
    public void Verify_SetsVerifiedAtUtc()
    {
        var establishment = CreateEstablishment();
        var verifiedAt = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        establishment.Verify(verifiedAt);

        Assert.True(establishment.IsVerified);
        Assert.Equal(verifiedAt, establishment.VerifiedAtUtc);
    }

    [Fact]
    public void Verify_WhenAlreadyVerified_DoesNotChangeDate()
    {
        var establishment = CreateEstablishment();
        var firstDate = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var secondDate = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        establishment.Verify(firstDate);
        establishment.Verify(secondDate);

        Assert.True(establishment.IsVerified);
        Assert.Equal(firstDate, establishment.VerifiedAtUtc);
    }

    [Fact]
    public void RevokeVerification_SetsVerifiedAtUtcToNull()
    {
        var establishment = CreateEstablishment();
        establishment.Verify(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));

        establishment.RevokeVerification();

        Assert.False(establishment.IsVerified);
        Assert.Null(establishment.VerifiedAtUtc);
    }

    [Fact]
    public void RevokeVerification_IsIdempotent()
    {
        var establishment = CreateEstablishment();

        establishment.RevokeVerification();
        establishment.RevokeVerification();

        Assert.False(establishment.IsVerified);
        Assert.Null(establishment.VerifiedAtUtc);
    }

    private static Establishment CreateEstablishment() =>
        Establishment.Create(
            EstablishmentId.New(),
            EstablishmentCategoryId.New(),
            "Test Place",
            "test-place",
            null,
            null,
            null,
            null,
            null,
            null);
}
