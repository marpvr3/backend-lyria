using Lyria.Domain.Abstractions;
using Lyria.Domain.Users;
using Xunit;

namespace Lyria.Domain.UnitTests.Users;

public sealed class UserAuditableTests
{
    [Fact]
    public void User_ImplementsIAuditableEntity()
    {
        var user = CreateUser();

        Assert.IsAssignableFrom<IAuditableEntity>(user);
    }

    [Fact]
    public void SetCreatedAtUtc_SetsValue()
    {
        var user = CreateUser();
        var createdAt = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        user.SetCreatedAtUtc(createdAt);

        Assert.Equal(createdAt, user.CreatedAtUtc);
    }

    [Fact]
    public void SetUpdatedAtUtc_SetsValue()
    {
        var user = CreateUser();
        var updatedAt = new DateTime(2026, 7, 20, 14, 0, 0, DateTimeKind.Utc);

        user.SetUpdatedAtUtc(updatedAt);

        Assert.Equal(updatedAt, user.UpdatedAtUtc);
    }

    private static User CreateUser() =>
        User.Create(
            UserId.New(), "Carlos", "García", "carlos@example.com",
            "hashed123", null, null, null);
}
