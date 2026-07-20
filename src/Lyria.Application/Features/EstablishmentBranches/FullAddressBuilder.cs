namespace Lyria.Application.Features.EstablishmentBranches;

public static class FullAddressBuilder
{
    public static string Build(
        string street,
        string? number,
        string? addressComplement,
        string? neighborhood,
        string? city,
        string? province,
        string? postalCode,
        string? country)
    {
        var parts = new List<string>();

        string streetPart = street;
        if (!string.IsNullOrWhiteSpace(number))
        {
            streetPart += " " + number;
        }
        parts.Add(streetPart);

        if (!string.IsNullOrWhiteSpace(addressComplement))
        {
            parts.Add(addressComplement);
        }

        if (!string.IsNullOrWhiteSpace(neighborhood))
        {
            parts.Add(neighborhood);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            parts.Add(city);
        }

        if (!string.IsNullOrWhiteSpace(province))
        {
            parts.Add(province);
        }

        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            parts.Add(postalCode);
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            parts.Add(country);
        }

        return string.Join(", ", parts);
    }
}
