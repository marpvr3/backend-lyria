using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Branches;

public sealed class EstablishmentBranchCreateTests
{
    private static readonly EstablishmentBranchId DefaultId = EstablishmentBranchId.New();
    private static readonly EstablishmentId DefaultEstablishmentId = EstablishmentId.New();

    private static EstablishmentBranch CreateDefault(
        EstablishmentBranchId? id = null,
        EstablishmentId? establishmentId = null,
        string name = "Palermo",
        string street = "Costa Rica",
        string? number = "5865",
        string? addressComplement = null,
        string? neighborhood = "Palermo",
        string? city = "Buenos Aires",
        string? province = "Buenos Aires",
        string? postalCode = "C1414",
        string? country = "Argentina",
        decimal? latitude = -34.586m,
        decimal? longitude = -58.432m,
        string? phone = "+54 11 1234-5678",
        string? whatsApp = "+54 11 1234-5678",
        string? email = "palermo@letitv.com") =>
        EstablishmentBranch.Create(
            id ?? DefaultId, establishmentId ?? DefaultEstablishmentId,
            name, street, number, addressComplement,
            neighborhood, city, province, postalCode, country,
            latitude, longitude, phone, whatsApp, email, "America/Argentina/Buenos_Aires");

    [Fact]
    public void Create_WithValidData_CreatesEstablishmentBranch()
    {
        var branch = CreateDefault();

        Assert.Equal(DefaultId, branch.Id);
        Assert.Equal(DefaultEstablishmentId, branch.EstablishmentId);
        Assert.Equal("Palermo", branch.Name);
        Assert.Equal("Costa Rica", branch.Street);
        Assert.Equal("5865", branch.Number);
        Assert.Null(branch.AddressComplement);
        Assert.Equal("Palermo", branch.Neighborhood);
        Assert.Equal("Buenos Aires", branch.City);
        Assert.Equal("Buenos Aires", branch.Province);
        Assert.Equal("C1414", branch.PostalCode);
        Assert.Equal("Argentina", branch.Country);
        Assert.Equal(-34.586m, branch.Latitude);
        Assert.Equal(-58.432m, branch.Longitude);
        Assert.Equal("+54 11 1234-5678", branch.Phone);
        Assert.Equal("+54 11 1234-5678", branch.WhatsApp);
        Assert.Equal("palermo@letitv.com", branch.Email);
    }

    [Fact]
    public void Create_StartsActive()
    {
        var branch = CreateDefault();

        Assert.True(branch.IsActive);
    }

    [Fact]
    public void Create_RatingStartsAtZero()
    {
        var branch = CreateDefault();

        Assert.Equal(0, branch.RatingAverage);
    }

    [Fact]
    public void Create_TotalReviewsStartsAtZero()
    {
        var branch = CreateDefault();

        Assert.Equal(0, branch.TotalReviews);
    }

    [Fact]
    public void Create_RejectsEmptyName()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(name: ""));
    }

    [Fact]
    public void Create_RejectsWhitespaceOnlyName()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(name: "   "));
    }

    [Fact]
    public void Create_RejectsTooShortName()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(name: "A"));
    }

    [Fact]
    public void Create_RejectsTooLongName()
    {
        string longName = new('A', EstablishmentBranch.NameMaxLength + 1);

        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(name: longName));
    }

    [Fact]
    public void Create_NormalizesName_TrimsWhitespace()
    {
        var branch = CreateDefault(name: "  Palermo  ");

        Assert.Equal("Palermo", branch.Name);
    }

    [Fact]
    public void Create_NormalizesName_CollapsesMultipleSpaces()
    {
        var branch = CreateDefault(name: "Palermo   Soho");

        Assert.Equal("Palermo Soho", branch.Name);
    }

    [Fact]
    public void Create_RejectsEmptyStreet()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(street: ""));
    }

    [Fact]
    public void Create_RejectsWhitespaceOnlyStreet()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(street: "   "));
    }

    [Fact]
    public void Create_RejectsTooShortStreet()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(street: "A"));
    }

    [Fact]
    public void Create_RejectsTooLongStreet()
    {
        string longStreet = new('A', EstablishmentBranch.StreetMaxLength + 1);

        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(street: longStreet));
    }

    [Fact]
    public void Create_NormalizesStreet_TrimsWhitespace()
    {
        var branch = CreateDefault(street: "  Costa Rica  ");

        Assert.Equal("Costa Rica", branch.Street);
    }

    [Fact]
    public void Create_NormalizesEmptyOptionals_ToNull()
    {
        var branch = CreateDefault(
            number: "   ",
            addressComplement: "   ",
            neighborhood: "   ",
            city: "   ",
            province: "   ",
            postalCode: "   ",
            country: "   ",
            phone: "   ",
            whatsApp: "   ",
            email: "   ");

        Assert.Null(branch.Number);
        Assert.Null(branch.AddressComplement);
        Assert.Null(branch.Neighborhood);
        Assert.Null(branch.City);
        Assert.Null(branch.Province);
        Assert.Null(branch.PostalCode);
        Assert.Null(branch.Country);
        Assert.Null(branch.Phone);
        Assert.Null(branch.WhatsApp);
        Assert.Null(branch.Email);
    }

    [Fact]
    public void Create_AcceptsBothCoordinatesNull()
    {
        var branch = CreateDefault(latitude: null, longitude: null);

        Assert.Null(branch.Latitude);
        Assert.Null(branch.Longitude);
    }

    [Fact]
    public void Create_RejectsOnlyLatitude()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(latitude: -34.586m, longitude: null));
    }

    [Fact]
    public void Create_RejectsOnlyLongitude()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(latitude: null, longitude: -58.432m));
    }

    [Fact]
    public void Create_RejectsLatitudeOutOfRange()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(latitude: -91m, longitude: -58.432m));

        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(latitude: 91m, longitude: -58.432m));
    }

    [Fact]
    public void Create_RejectsLongitudeOutOfRange()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(latitude: -34.586m, longitude: -181m));

        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(latitude: -34.586m, longitude: 181m));
    }

    [Fact]
    public void Create_RejectsInvalidPhone()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(phone: "abc123"));
    }

    [Fact]
    public void Create_RejectsInvalidWhatsApp()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(whatsApp: "abc123"));
    }

    [Fact]
    public void Create_RejectsInvalidEmail()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(email: "not-an-email"));
    }

    [Fact]
    public void Create_RejectsEmptyEstablishmentId()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateDefault(establishmentId: new EstablishmentId(Guid.Empty)));
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var branch = CreateDefault();
        branch.Deactivate();

        branch.Activate();

        Assert.True(branch.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var branch = CreateDefault();

        branch.Deactivate();

        Assert.False(branch.IsActive);
    }

    [Fact]
    public void Activate_IsIdempotent()
    {
        var branch = CreateDefault();

        branch.Activate();
        branch.Activate();

        Assert.True(branch.IsActive);
    }

    [Fact]
    public void Deactivate_IsIdempotent()
    {
        var branch = CreateDefault();

        branch.Deactivate();
        branch.Deactivate();

        Assert.False(branch.IsActive);
    }

    [Fact]
    public void UpdateDetails_DoesNotChangeEstablishmentId()
    {
        var branch = CreateDefault();
        var originalEstablishmentId = branch.EstablishmentId;

        branch.UpdateDetails(
            "Nuevo Nombre", "Nueva Calle", "100", null,
            "Recoleta", "Córdoba", "Córdoba", "X5000", "Argentina",
            -31.420m, -64.188m, "+54 351 123-4567", "+54 351 123-4567", "cordoba@letitv.com");

        Assert.Equal(originalEstablishmentId, branch.EstablishmentId);
    }

    [Fact]
    public void UpdateDetails_DoesNotChangeRating()
    {
        var branch = CreateDefault();
        var originalRating = branch.RatingAverage;
        var originalTotalReviews = branch.TotalReviews;

        branch.UpdateDetails(
            "Nuevo Nombre", "Nueva Calle", "100", null,
            "Recoleta", "Córdoba", "Córdoba", "X5000", "Argentina",
            -31.420m, -64.188m, "+54 351 123-4567", "+54 351 123-4567", "cordoba@letitv.com");

        Assert.Equal(originalRating, branch.RatingAverage);
        Assert.Equal(originalTotalReviews, branch.TotalReviews);
    }

    [Fact]
    public void UpdateDetails_UpdatesAllFields()
    {
        var branch = CreateDefault();

        branch.UpdateDetails(
            "Recoleta", "Junín", "1930", "Piso 2", "Recoleta",
            "CABA", "Buenos Aires", "C1113", "Argentina",
            -34.588m, -58.393m, "+54 11 9876-5432", "+54 11 9876-5432", "recoleta@letitv.com");

        Assert.Equal("Recoleta", branch.Name);
        Assert.Equal("Junín", branch.Street);
        Assert.Equal("1930", branch.Number);
        Assert.Equal("Piso 2", branch.AddressComplement);
        Assert.Equal("Recoleta", branch.Neighborhood);
        Assert.Equal("CABA", branch.City);
        Assert.Equal("Buenos Aires", branch.Province);
        Assert.Equal("C1113", branch.PostalCode);
        Assert.Equal("Argentina", branch.Country);
        Assert.Equal(-34.588m, branch.Latitude);
        Assert.Equal(-58.393m, branch.Longitude);
        Assert.Equal("+54 11 9876-5432", branch.Phone);
        Assert.Equal("+54 11 9876-5432", branch.WhatsApp);
        Assert.Equal("recoleta@letitv.com", branch.Email);
    }
}
