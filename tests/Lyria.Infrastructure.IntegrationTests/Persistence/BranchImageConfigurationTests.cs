using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class BranchImageConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void Table_Name_Is_ImagenesSede()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchImage))!;
        Assert.Equal("ImagenesSede", entityType.GetTableName());
    }

    [Fact]
    public void PrimaryKey_Name_Is_PK_ImagenesSede()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchImage))!;
        var pk = entityType.FindPrimaryKey()!;
        Assert.Equal("PK_ImagenesSede", pk.GetName());
    }

    [Fact]
    public void PrimaryKey_Column_Is_ImagenSedeId()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchImage))!;
        var pk = entityType.FindPrimaryKey()!;
        var column = pk.Properties.Single().GetColumnName();
        Assert.Equal("ImagenSedeId", column);
    }

    [Fact]
    public void Column_BranchId_Name_Is_SedeId()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.BranchId))!;
        Assert.Equal("SedeId", property.GetColumnName());
    }

    [Fact]
    public void Column_Url_Name_Is_Url()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.Url))!;
        Assert.Equal("Url", property.GetColumnName());
    }

    [Fact]
    public void Column_Url_IsRequired()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.Url))!;
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Column_Url_MaxLength_Is_500()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.Url))!;
        Assert.Equal(500, property.GetMaxLength());
    }

    [Fact]
    public void Column_FileName_Name_Is_NombreArchivo()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.FileName))!;
        Assert.Equal("NombreArchivo", property.GetColumnName());
    }

    [Fact]
    public void Column_FileName_IsRequired()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.FileName))!;
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Column_FileName_MaxLength_Is_255()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.FileName))!;
        Assert.Equal(255, property.GetMaxLength());
    }

    [Fact]
    public void Column_AlternativeText_Name_Is_TextoAlternativo()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.AlternativeText))!;
        Assert.Equal("TextoAlternativo", property.GetColumnName());
    }

    [Fact]
    public void Column_AlternativeText_IsOptional()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.AlternativeText))!;
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void Column_AlternativeText_MaxLength_Is_500()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.AlternativeText))!;
        Assert.Equal(500, property.GetMaxLength());
    }

    [Fact]
    public void Column_IsPrimary_Name_Is_EsPrincipal()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.IsPrimary))!;
        Assert.Equal("EsPrincipal", property.GetColumnName());
    }

    [Fact]
    public void Column_IsPrimary_IsRequired()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.IsPrimary))!;
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Column_SortOrder_Name_Is_Orden()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.SortOrder))!;
        Assert.Equal("Orden", property.GetColumnName());
    }

    [Fact]
    public void Column_SortOrder_IsRequired()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.SortOrder))!;
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Column_IsActive_Name_Is_EsActivo()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.IsActive))!;
        Assert.Equal("EsActivo", property.GetColumnName());
    }

    [Fact]
    public void Column_IsActive_IsRequired()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.IsActive))!;
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Column_CreatedAtUtc_Name_Is_FechaCreacionUtc()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.CreatedAtUtc))!;
        Assert.Equal("FechaCreacionUtc", property.GetColumnName());
    }

    [Fact]
    public void Column_UpdatedAtUtc_Name_Is_FechaActualizacionUtc()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.UpdatedAtUtc))!;
        Assert.Equal("FechaActualizacionUtc", property.GetColumnName());
    }

    [Fact]
    public void Column_UpdatedAtUtc_IsOptional()
    {
        using var context = _fixture.CreateContext();
        var property = context.Model.FindEntityType(typeof(BranchImage))!
            .FindProperty(nameof(BranchImage.UpdatedAtUtc))!;
        Assert.True(property.IsNullable);
    }

    [Fact]
    public void FK_To_Sedes_IsRestrict()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchImage))!;
        var fk = entityType.GetForeignKeys()
            .First(f => f.Properties.Any(p => p.GetColumnName() == "SedeId"));
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    [Fact]
    public void HasIndex_On_SedeId()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchImage))!;
        var property = entityType.FindProperty(nameof(BranchImage.BranchId))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 1 && i.Properties[0] == property);
        Assert.NotNull(index);
    }

    [Fact]
    public void HasIndex_On_SedeId_EsActivo_Orden()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchImage))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 3 &&
            i.Properties[0].GetColumnName() == "SedeId" &&
            i.Properties[1].GetColumnName() == "EsActivo" &&
            i.Properties[2].GetColumnName() == "Orden");
        Assert.NotNull(index);
    }

    [Fact]
    public void HasIndex_On_SedeId_EsPrincipal()
    {
        using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(BranchImage))!;
        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties[0].GetColumnName() == "SedeId" &&
            i.Properties[1].GetColumnName() == "EsPrincipal");
        Assert.NotNull(index);
    }

    [Fact]
    public void Persists_And_Retrieves_Image()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cat", null, null, 0);

        var establishment = Establishment.Create(
            EstablishmentId.New(), category.Id,
            "Test Est", "test-est", null, null, null, null, null, null);

        var branchId = EstablishmentBranchId.New();
        var branch = EstablishmentBranch.Create(
            branchId, establishment.Id,
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);

        var image = BranchImage.Create(
            BranchImageId.New(), branchId,
            "https://cdn.example.com/img.jpg", "img.jpg",
            "Alt text", true, 0);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            context.Set<Establishment>().Add(establishment);
            context.Set<EstablishmentBranch>().Add(branch);
            context.Set<BranchImage>().Add(image);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var retrieved = context.Set<BranchImage>()
                .First(i => i.Id == image.Id);

            Assert.Equal(branchId, retrieved.BranchId);
            Assert.Equal("https://cdn.example.com/img.jpg", retrieved.Url);
            Assert.Equal("img.jpg", retrieved.FileName);
            Assert.Equal("Alt text", retrieved.AlternativeText);
            Assert.True(retrieved.IsPrimary);
            Assert.Equal(0, retrieved.SortOrder);
            Assert.True(retrieved.IsActive);
        }
    }

    [Fact]
    public void Query_By_Branch_Returns_Ordered()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cat", null, null, 0);

        var establishment = Establishment.Create(
            EstablishmentId.New(), category.Id,
            "Test Est", "test-est", null, null, null, null, null, null);

        var branchId = EstablishmentBranchId.New();
        var branch = EstablishmentBranch.Create(
            branchId, establishment.Id,
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);

        var img1 = BranchImage.Create(
            BranchImageId.New(), branchId,
            "https://cdn.example.com/b.jpg", "b.jpg",
            null, false, 1);

        var img2 = BranchImage.Create(
            BranchImageId.New(), branchId,
            "https://cdn.example.com/a.jpg", "a.jpg",
            null, true, 0);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            context.Set<Establishment>().Add(establishment);
            context.Set<EstablishmentBranch>().Add(branch);
            context.Set<BranchImage>().AddRange(img1, img2);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var images = context.Set<BranchImage>()
                .Where(i => i.BranchId == branchId && i.IsActive)
                .OrderBy(i => i.SortOrder)
                .ToList();

            Assert.Equal(2, images.Count);
            Assert.Equal(0, images[0].SortOrder);
            Assert.Equal(1, images[1].SortOrder);
        }
    }

    [Fact]
    public void SetPrimary_Transactional_Change()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cat", null, null, 0);

        var establishment = Establishment.Create(
            EstablishmentId.New(), category.Id,
            "Test Est", "test-est", null, null, null, null, null, null);

        var branchId = EstablishmentBranchId.New();
        var branch = EstablishmentBranch.Create(
            branchId, establishment.Id,
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);

        var img1 = BranchImage.Create(
            BranchImageId.New(), branchId,
            "https://cdn.example.com/a.jpg", "a.jpg",
            null, true, 0);

        var img2 = BranchImage.Create(
            BranchImageId.New(), branchId,
            "https://cdn.example.com/b.jpg", "b.jpg",
            null, false, 1);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            context.Set<Establishment>().Add(establishment);
            context.Set<EstablishmentBranch>().Add(branch);
            context.Set<BranchImage>().AddRange(img1, img2);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var images = context.Set<BranchImage>()
                .Where(i => i.BranchId == branchId && i.IsActive)
                .ToList();

            var oldPrimary = images.First(i => i.IsPrimary);
            var newPrimary = images.First(i => !i.IsPrimary);

            oldPrimary.UnsetPrimary();
            newPrimary.SetAsPrimary();

            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var images = context.Set<BranchImage>()
                .Where(i => i.BranchId == branchId)
                .ToList();

            Assert.Single(images, i => i.IsPrimary);
            Assert.Equal(img2.Id, images.Single(i => i.IsPrimary).Id);
        }
    }

    [Fact]
    public void Status_Change_Persists()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cat", null, null, 0);

        var establishment = Establishment.Create(
            EstablishmentId.New(), category.Id,
            "Test Est", "test-est", null, null, null, null, null, null);

        var branchId = EstablishmentBranchId.New();
        var branch = EstablishmentBranch.Create(
            branchId, establishment.Id,
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);

        var image = BranchImage.Create(
            BranchImageId.New(), branchId,
            "https://cdn.example.com/img.jpg", "img.jpg",
            null, false, 0);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            context.Set<Establishment>().Add(establishment);
            context.Set<EstablishmentBranch>().Add(branch);
            context.Set<BranchImage>().Add(image);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var img = context.Set<BranchImage>().First(i => i.Id == image.Id);
            img.Deactivate();
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var img = context.Set<BranchImage>().First(i => i.Id == image.Id);
            Assert.False(img.IsActive);
        }
    }

    [Fact]
    public void Reorder_Persists()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cat", null, null, 0);

        var establishment = Establishment.Create(
            EstablishmentId.New(), category.Id,
            "Test Est", "test-est", null, null, null, null, null, null);

        var branchId = EstablishmentBranchId.New();
        var branch = EstablishmentBranch.Create(
            branchId, establishment.Id,
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);

        var img1 = BranchImage.Create(
            BranchImageId.New(), branchId,
            "https://cdn.example.com/a.jpg", "a.jpg",
            null, false, 0);

        var img2 = BranchImage.Create(
            BranchImageId.New(), branchId,
            "https://cdn.example.com/b.jpg", "b.jpg",
            null, false, 1);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            context.Set<Establishment>().Add(establishment);
            context.Set<EstablishmentBranch>().Add(branch);
            context.Set<BranchImage>().AddRange(img1, img2);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var images = context.Set<BranchImage>()
                .Where(i => i.BranchId == branchId)
                .ToList();

            images.First(i => i.Id == img1.Id)
                .UpdateMetadata(img1.Url, img1.FileName, img1.AlternativeText, 1);
            images.First(i => i.Id == img2.Id)
                .UpdateMetadata(img2.Url, img2.FileName, img2.AlternativeText, 0);

            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var images = context.Set<BranchImage>()
                .Where(i => i.BranchId == branchId)
                .OrderBy(i => i.SortOrder)
                .ToList();

            Assert.Equal(img2.Id, images[0].Id);
            Assert.Equal(img1.Id, images[1].Id);
        }
    }

    [Fact]
    public void Audit_CreatedAtUtc_IsSet()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cat", null, null, 0);

        var establishment = Establishment.Create(
            EstablishmentId.New(), category.Id,
            "Test Est", "test-est", null, null, null, null, null, null);

        var branchId = EstablishmentBranchId.New();
        var branch = EstablishmentBranch.Create(
            branchId, establishment.Id,
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);

        var image = BranchImage.Create(
            BranchImageId.New(), branchId,
            "https://cdn.example.com/img.jpg", "img.jpg",
            null, false, 0);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            context.Set<Establishment>().Add(establishment);
            context.Set<EstablishmentBranch>().Add(branch);
            context.Set<BranchImage>().Add(image);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var img = context.Set<BranchImage>().First(i => i.Id == image.Id);
            Assert.NotEqual(default, img.CreatedAtUtc);
            Assert.Null(img.UpdatedAtUtc);
        }
    }

    [Fact]
    public void Audit_UpdatedAtUtc_IsSet_OnModification()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cat", null, null, 0);

        var establishment = Establishment.Create(
            EstablishmentId.New(), category.Id,
            "Test Est", "test-est", null, null, null, null, null, null);

        var branchId = EstablishmentBranchId.New();
        var branch = EstablishmentBranch.Create(
            branchId, establishment.Id,
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);

        var image = BranchImage.Create(
            BranchImageId.New(), branchId,
            "https://cdn.example.com/img.jpg", "img.jpg",
            null, false, 0);

        using (var context = _fixture.CreateContext())
        {
            context.Set<EstablishmentCategory>().Add(category);
            context.Set<Establishment>().Add(establishment);
            context.Set<EstablishmentBranch>().Add(branch);
            context.Set<BranchImage>().Add(image);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var img = context.Set<BranchImage>().First(i => i.Id == image.Id);
            img.UpdateMetadata("https://cdn.example.com/new.jpg", "new.jpg", null, 1);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var img = context.Set<BranchImage>().First(i => i.Id == image.Id);
            Assert.NotNull(img.UpdatedAtUtc);
        }
    }

    public void Dispose() => _fixture.Dispose();
}
