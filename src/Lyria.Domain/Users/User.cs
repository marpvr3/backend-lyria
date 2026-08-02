using System.Text.RegularExpressions;
using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Users;

public sealed partial class User : AggregateRoot<UserId>, IAuditableEntity
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int LastNameMinLength = 2;
    public const int LastNameMaxLength = 100;
    public const int EmailMaxLength = 254;
    public const int PasswordHashMaxLength = 256;
    public const int PhoneMaxLength = 30;
    public const int PhotoUrlMaxLength = 500;

    public string Name { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? Phone { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? PhotoUrl { get; private set; }
    public UserStatus Status { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private User()
    {
    }

    private User(
        UserId id,
        string name,
        string lastName,
        string email,
        string passwordHash,
        string? phone,
        DateOnly? birthDate,
        string? photoUrl)
        : base(id)
    {
        Name = name;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        Phone = phone;
        BirthDate = birthDate;
        PhotoUrl = photoUrl;
        Status = UserStatus.Unverified;
        IsEmailVerified = false;
        LastLoginAtUtc = null;
    }

    public static User Create(
        UserId id,
        string name,
        string lastName,
        string email,
        string passwordHash,
        string? phone,
        DateOnly? birthDate,
        string? photoUrl)
    {
        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string normalizedLastName = NormalizeName(lastName);
        ValidateLastName(normalizedLastName);

        string normalizedEmail = NormalizeEmail(email);
        ValidateEmail(normalizedEmail);

        ValidatePasswordHash(passwordHash);

        string? normalizedPhone = NormalizeOptionalString(phone);
        ValidatePhone(normalizedPhone);

        string? normalizedPhotoUrl = NormalizeOptionalUrl(photoUrl);
        ValidatePhotoUrl(normalizedPhotoUrl);

        return new User(
            id, normalizedName, normalizedLastName, normalizedEmail,
            passwordHash, normalizedPhone, birthDate, normalizedPhotoUrl);
    }

    public void UpdateProfile(
        string name,
        string lastName,
        string? phone,
        DateOnly? birthDate,
        string? photoUrl)
    {
        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string normalizedLastName = NormalizeName(lastName);
        ValidateLastName(normalizedLastName);

        string? normalizedPhone = NormalizeOptionalString(phone);
        ValidatePhone(normalizedPhone);

        string? normalizedPhotoUrl = NormalizeOptionalUrl(photoUrl);
        ValidatePhotoUrl(normalizedPhotoUrl);

        Name = normalizedName;
        LastName = normalizedLastName;
        Phone = normalizedPhone;
        BirthDate = birthDate;
        PhotoUrl = normalizedPhotoUrl;
    }

    public void ChangeEmail(string email)
    {
        string normalizedEmail = NormalizeEmail(email);
        ValidateEmail(normalizedEmail);

        Email = normalizedEmail;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        ValidatePasswordHash(passwordHash);

        PasswordHash = passwordHash;
    }

    public void ChangeStatus(UserStatus newStatus)
    {
        bool isValid = (Status, newStatus) switch
        {
            (UserStatus.Unverified, UserStatus.Active) => true,
            (UserStatus.Active, UserStatus.Suspended) => true,
            (UserStatus.Suspended, UserStatus.Deleted) => true,
            _ => false
        };

        if (!isValid)
        {
            throw new UserException(
                $"La transición de estado de '{Status}' a '{newStatus}' no es válida.");
        }

        Status = newStatus;
    }

    public void MarkEmailAsVerified()
    {
        IsEmailVerified = true;
    }

    public void RegisterLastLogin(DateTime lastLoginAtUtc)
    {
        LastLoginAtUtc = lastLoginAtUtc;
    }

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        string trimmed = name.Trim();
        return MultipleSpacesRegex().Replace(trimmed, " ");
    }

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        return email.Trim().ToLowerInvariant();
    }

    public static string? NormalizeOptionalString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    public static string? NormalizeOptionalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.Trim();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new UserException("El nombre del usuario es obligatorio.");
        }

        if (name.Length < NameMinLength)
        {
            throw new UserException(
                $"El nombre del usuario debe tener al menos {NameMinLength} caracteres.");
        }

        if (name.Length > NameMaxLength)
        {
            throw new UserException(
                $"El nombre del usuario no puede superar los {NameMaxLength} caracteres.");
        }
    }

    private static void ValidateLastName(string lastName)
    {
        if (string.IsNullOrEmpty(lastName))
        {
            throw new UserException("El apellido del usuario es obligatorio.");
        }

        if (lastName.Length < LastNameMinLength)
        {
            throw new UserException(
                $"El apellido del usuario debe tener al menos {LastNameMinLength} caracteres.");
        }

        if (lastName.Length > LastNameMaxLength)
        {
            throw new UserException(
                $"El apellido del usuario no puede superar los {LastNameMaxLength} caracteres.");
        }
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            throw new UserException("El correo electrónico del usuario es obligatorio.");
        }

        if (email.Length > EmailMaxLength)
        {
            throw new UserException(
                $"El correo electrónico no puede superar los {EmailMaxLength} caracteres.");
        }

        int atIndex = email.IndexOf('@');
        if (atIndex < 1)
        {
            throw new UserException(
                "El correo electrónico no tiene un formato válido.");
        }

        string domain = email[(atIndex + 1)..];
        if (!domain.Contains('.'))
        {
            throw new UserException(
                "El correo electrónico no tiene un formato válido.");
        }
    }

    private static void ValidatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            throw new UserException("El hash de contraseña es obligatorio.");
        }

        if (passwordHash.Length > PasswordHashMaxLength)
        {
            throw new UserException(
                $"El hash de contraseña no puede superar los {PasswordHashMaxLength} caracteres.");
        }
    }

    private static void ValidatePhone(string? phone)
    {
        if (phone is not null && phone.Length > PhoneMaxLength)
        {
            throw new UserException(
                $"El teléfono no puede superar los {PhoneMaxLength} caracteres.");
        }
    }

    private static void ValidatePhotoUrl(string? photoUrl)
    {
        if (photoUrl is not null && photoUrl.Length > PhotoUrlMaxLength)
        {
            throw new UserException(
                $"La URL de la foto no puede superar los {PhotoUrlMaxLength} caracteres.");
        }
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesRegex();
}
