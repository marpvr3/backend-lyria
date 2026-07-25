using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments.Branches;

public sealed class BranchImage : Entity<BranchImageId>, IAuditableEntity
{
    public const int UrlMaxLength = 500;
    public const int FileNameMaxLength = 255;
    public const int AlternativeTextMaxLength = 500;

    private static readonly string[] DangerousSchemes =
        ["javascript:", "data:", "vbscript:", "file:"];

    private static readonly char[] DirectorySeparators = ['/', '\\'];

    public EstablishmentBranchId BranchId { get; private set; }
    public string Url { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string? AlternativeText { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private BranchImage() { }

    private BranchImage(
        BranchImageId id,
        EstablishmentBranchId branchId,
        string url,
        string fileName,
        string? alternativeText,
        bool isPrimary,
        int sortOrder)
        : base(id)
    {
        BranchId = branchId;
        Url = url;
        FileName = fileName;
        AlternativeText = alternativeText;
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public static BranchImage Create(
        BranchImageId id,
        EstablishmentBranchId branchId,
        string url,
        string fileName,
        string? alternativeText,
        bool isPrimary,
        int sortOrder)
    {
        ValidateBranchId(branchId);

        string normalizedUrl = NormalizeUrl(url);
        ValidateUrl(normalizedUrl);

        string normalizedFileName = NormalizeFileName(fileName);
        ValidateFileName(normalizedFileName);

        string? normalizedAltText = NormalizeAlternativeText(alternativeText);
        ValidateAlternativeText(normalizedAltText);

        ValidateSortOrder(sortOrder);

        return new BranchImage(
            id, branchId, normalizedUrl, normalizedFileName,
            normalizedAltText, isPrimary, sortOrder);
    }

    public void UpdateMetadata(
        string url,
        string fileName,
        string? alternativeText,
        int sortOrder)
    {
        string normalizedUrl = NormalizeUrl(url);
        ValidateUrl(normalizedUrl);

        string normalizedFileName = NormalizeFileName(fileName);
        ValidateFileName(normalizedFileName);

        string? normalizedAltText = NormalizeAlternativeText(alternativeText);
        ValidateAlternativeText(normalizedAltText);

        ValidateSortOrder(sortOrder);

        Url = normalizedUrl;
        FileName = normalizedFileName;
        AlternativeText = normalizedAltText;
        SortOrder = sortOrder;
    }

    public void SetAsPrimary()
    {
        if (!IsActive)
        {
            throw new EstablishmentBranchException(
                "Una imagen inactiva no puede ser la imagen principal.");
        }

        IsPrimary = true;
    }

    public void UnsetPrimary()
    {
        IsPrimary = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        if (IsPrimary)
        {
            IsPrimary = false;
        }

        IsActive = false;
    }

    public static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        return url.Trim();
    }

    public static string NormalizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        return fileName.Trim();
    }

    public static string? NormalizeAlternativeText(string? alternativeText)
    {
        if (string.IsNullOrWhiteSpace(alternativeText))
        {
            return null;
        }

        return alternativeText.Trim();
    }

    private static void ValidateBranchId(EstablishmentBranchId branchId)
    {
        if (branchId.Value == Guid.Empty)
        {
            throw new EstablishmentBranchException("La sede de la imagen es obligatoria.");
        }
    }

    private static void ValidateUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new EstablishmentBranchException("La URL de la imagen es obligatoria.");
        }

        if (url.Length > UrlMaxLength)
        {
            throw new EstablishmentBranchException(
                $"La URL de la imagen no puede superar los {UrlMaxLength} caracteres.");
        }

        string lowerUrl = url.ToLowerInvariant();
        foreach (string scheme in DangerousSchemes)
        {
            if (lowerUrl.StartsWith(scheme, StringComparison.Ordinal))
            {
                throw new EstablishmentBranchException(
                    $"La URL contiene un esquema no permitido: {scheme.TrimEnd(':')}.");
            }
        }
    }

    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new EstablishmentBranchException(
                "El nombre del archivo es obligatorio.");
        }

        if (fileName.Length > FileNameMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El nombre del archivo no puede superar los {FileNameMaxLength} caracteres.");
        }

        if (fileName.Contains("../", StringComparison.Ordinal) ||
            fileName.Contains("..\\", StringComparison.Ordinal))
        {
            throw new EstablishmentBranchException(
                "El nombre del archivo no puede contener secuencias de navegación de directorio.");
        }

        if (fileName.IndexOfAny(DirectorySeparators) >= 0)
        {
            throw new EstablishmentBranchException(
                "El nombre del archivo no puede contener separadores de directorio.");
        }
    }

    private static void ValidateAlternativeText(string? alternativeText)
    {
        if (alternativeText is not null && alternativeText.Length > AlternativeTextMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El texto alternativo no puede superar los {AlternativeTextMaxLength} caracteres.");
        }
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new EstablishmentBranchException(
                "El orden de la imagen no puede ser negativo.");
        }
    }
}
