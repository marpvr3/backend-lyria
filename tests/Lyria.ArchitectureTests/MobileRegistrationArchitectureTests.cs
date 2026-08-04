using System.Reflection;
using Xunit;

namespace Lyria.ArchitectureTests;

/// <summary>
/// Valida las reglas arquitectónicas y de seguridad del registro móvil:
/// el frontend no decide el rol ni el nivel de importancia, la contraseña nunca
/// se persiste ni se registra en claro y la transacción vive en Infrastructure.
/// </summary>
public class MobileRegistrationArchitectureTests
{
    private static readonly Assembly ApplicationAssembly =
        typeof(Application.DependencyInjection).Assembly;

    private static readonly Assembly InfrastructureAssembly =
        typeof(Infrastructure.DependencyInjection).Assembly;

    private static readonly Assembly ApiAssembly = typeof(Api.Program).Assembly;

    /// <summary>
    /// Identificador del rol base que se configura en el servidor.
    /// Se comprueba que no aparezca fijo en el código productivo.
    /// </summary>
    private const string ProductionRoleId = "B7E61E8B-7A94-4638-9068-CF364B81EE31";

    private static readonly string[] BackendControlledMembers =
    [
        "RoleId", "RoleName", "RoleCode", "ScopeType", "EstablishmentId", "BranchId",
        "IsAdmin", "Status", "PasswordHash", "IsEmailVerified", "ImportanceLevel"
    ];

    private static Type GetType(Assembly assembly, string typeName) =>
        assembly.GetTypes().First(t => t.Name == typeName);

    private static IEnumerable<string> PropertyNames(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name);

    // --- El frontend no decide valores del backend ---

    [Theory]
    [InlineData("RegisterMobileUserCommand")]
    public void Command_DoesNotAcceptBackendControlledMembers(string typeName)
    {
        IEnumerable<string> properties = PropertyNames(GetType(ApplicationAssembly, typeName));

        foreach (string forbidden in BackendControlledMembers)
        {
            Assert.DoesNotContain(forbidden, properties, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Request_DoesNotAcceptBackendControlledMembers()
    {
        IEnumerable<string> properties =
            PropertyNames(GetType(ApiAssembly, "MobileRegistrationRequest"));

        foreach (string forbidden in BackendControlledMembers)
        {
            Assert.DoesNotContain(forbidden, properties, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Response_DoesNotExposePasswordOrInternalRole()
    {
        IEnumerable<string> properties =
            PropertyNames(GetType(ApplicationAssembly, "MobileRegistrationResponse"));

        Assert.DoesNotContain("Password", properties, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", properties, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("RoleId", properties, StringComparer.OrdinalIgnoreCase);
    }

    // --- El rol se localiza por identificador, nunca por nombre ---

    [Fact]
    public void Handler_DoesNotLookUpRoleByNameOrCode()
    {
        string source = ReadSource(
            "src", "Lyria.Application", "Features", "MobileRegistrations", "Register",
            "RegisterMobileUserCommandHandler.cs");

        Assert.DoesNotContain("ExistsByNameAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("role.Name", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RoleName", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RoleCode", source, StringComparison.Ordinal);
        Assert.Contains("roleRepository.GetByIdAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RoleRepository_DoesNotExposeNameOrCodeLookup()
    {
        Type repository = GetType(ApplicationAssembly, "IRoleRepository");

        Assert.DoesNotContain(
            repository.GetMethods(),
            m => m.Name.Contains("Name", StringComparison.OrdinalIgnoreCase) ||
                 m.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
    }

    // --- No hay GUID de rol fijo en código productivo ---

    [Fact]
    public void ProductionCode_DoesNotHardcodeTheConfiguredRoleId()
    {
        foreach (string file in ProductionSourceFiles())
        {
            string source = File.ReadAllText(file);

            Assert.False(
                source.Contains(ProductionRoleId, StringComparison.OrdinalIgnoreCase),
                $"El identificador del rol base está fijo en el código: {file}");
        }
    }

    [Fact]
    public void AppSettings_DoesNotShipTheProductionRoleId()
    {
        string appSettings = ReadSource("src", "Lyria.Api", "appsettings.json");

        Assert.Contains("MobileRegistration", appSettings, StringComparison.Ordinal);
        Assert.DoesNotContain(
            ProductionRoleId, appSettings, StringComparison.OrdinalIgnoreCase);
    }

    // --- La transacción pertenece a Infrastructure ---

    [Fact]
    public void TransactionalWriter_LivesInInfrastructure()
    {
        Type writer = GetType(InfrastructureAssembly, "MobileRegistrationWriter");

        Assert.Equal(InfrastructureAssembly, writer.Assembly);
        Assert.False(writer.IsPublic);
    }

    [Fact]
    public void WriterAbstraction_DoesNotExposeTransactionControl()
    {
        Type abstraction = GetType(ApplicationAssembly, "IMobileRegistrationWriter");

        Assert.DoesNotContain(
            abstraction.GetMethods(),
            m => m.Name.Contains("Transaction", StringComparison.OrdinalIgnoreCase) ||
                 m.Name.Contains("Commit", StringComparison.OrdinalIgnoreCase) ||
                 m.Name.Contains("Rollback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Controller_DoesNotCoordinateTransactions()
    {
        string source = ReadSource(
            "src", "Lyria.Api", "Controllers", "V1", "MobileRegistrationsController.cs");

        Assert.DoesNotContain("BeginTransaction", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IMobileRegistrationWriter", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Handler_DoesNotCoordinateTransactionsItself()
    {
        string source = ReadSource(
            "src", "Lyria.Application", "Features", "MobileRegistrations", "Register",
            "RegisterMobileUserCommandHandler.cs");

        Assert.DoesNotContain("BeginTransaction", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Commit", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_DoesNotReferenceEntityFrameworkCore()
    {
        Assert.DoesNotContain(
            ApplicationAssembly.GetReferencedAssemblies(),
            a => a.Name?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                 == true);
    }

    /// <summary>
    /// La capa API no usa EF Core, con una única excepción preexistente y deliberada:
    /// <c>UniqueConstraintExceptionHandler</c> traduce <c>DbUpdateException</c> a Problem
    /// Details en el borde del pipeline. El registro móvil no amplía esa excepción.
    /// </summary>
    [Fact]
    public void Api_DoesNotReferenceEntityFrameworkCoreDirectly()
    {
        string[] knownExceptions = ["UniqueConstraintExceptionHandler.cs"];

        foreach (string file in SourceFilesIn("src", "Lyria.Api"))
        {
            if (knownExceptions.Contains(Path.GetFileName(file), StringComparer.Ordinal))
            {
                continue;
            }

            string source = File.ReadAllText(file);

            Assert.False(
                source.Contains("using Microsoft.EntityFrameworkCore", StringComparison.Ordinal),
                $"La capa API no debe usar EF Core directamente: {file}");
        }
    }

    // --- Seguridad de la contraseña ---

    [Fact]
    public void MobileRegistrationCode_DoesNotLogPasswords()
    {
        foreach (string file in MobileRegistrationSourceFiles())
        {
            string source = File.ReadAllText(file);

            foreach (string logCall in new[]
            {
                "Log.Information", "Log.Debug", "Log.Warning", "Log.Error",
                "logger.Log", "LogInformation", "LogDebug", "LogWarning", "LogError"
            })
            {
                Assert.False(
                    source.Contains(logCall, StringComparison.Ordinal),
                    $"El registro móvil no debe escribir logs con datos sensibles: {file}");
            }
        }
    }

    [Fact]
    public void MobileRegistrationCode_DoesNotUseHomegrownCryptography()
    {
        foreach (string file in MobileRegistrationSourceFiles())
        {
            string source = File.ReadAllText(file);

            foreach (string forbidden in new[]
            {
                "MD5", "SHA1", "SHA256", "SHA512", "System.Security.Cryptography"
            })
            {
                Assert.False(
                    source.Contains(forbidden, StringComparison.Ordinal),
                    $"No se permite criptografía propia en el registro móvil: {file}");
            }
        }
    }

    [Fact]
    public void PasswordHasher_DelegatesToAspNetCoreIdentity()
    {
        string source = ReadSource("src", "Lyria.Infrastructure", "Security", "PasswordHasher.cs");

        Assert.Contains("Microsoft.AspNetCore.Identity", source, StringComparison.Ordinal);

        foreach (string forbidden in new[] { "MD5", "SHA1", "SHA256" })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Domain_DoesNotAcceptPlainPassword()
    {
        Type user = typeof(Domain.Users.User);

        Assert.DoesNotContain(
            user.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            p => string.Equals(p.Name, "Password", StringComparison.OrdinalIgnoreCase));

        MethodInfo create = user.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!;

        Assert.Contains(create.GetParameters(), p => p.Name == "passwordHash");
        Assert.DoesNotContain(create.GetParameters(), p => p.Name == "password");
    }

    [Fact]
    public void Handler_HashesPasswordBeforeBuildingTheUser()
    {
        string source = ReadSource(
            "src", "Lyria.Application", "Features", "MobileRegistrations", "Register",
            "RegisterMobileUserCommandHandler.cs");

        int hashIndex = source.IndexOf("passwordHasher.Hash", StringComparison.Ordinal);
        int createIndex = source.IndexOf("User.Create", StringComparison.Ordinal);

        Assert.True(hashIndex >= 0, "El handler debe hashear la contraseña.");
        Assert.True(createIndex >= 0, "El handler debe construir el usuario.");
        Assert.True(hashIndex < createIndex, "El hash debe calcularse antes de crear el usuario.");
    }

    // --- Separación respecto al flujo administrativo ---

    [Fact]
    public void AdministrativeUserCreation_DoesNotAssignRolesAutomatically()
    {
        string source = ReadSource(
            "src", "Lyria.Application", "Features", "Users", "Create",
            "CreateUserCommandHandler.cs");

        Assert.DoesNotContain("UserRole", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IMobileRegistrationWriter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IMobileRegistrationDefaults", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MobileFlow_IsNotDrivenByRequestFieldsOrHeaders()
    {
        string source = ReadSource(
            "src", "Lyria.Api", "Controllers", "V1", "MobileRegistrationsController.cs");

        Assert.DoesNotContain("isMobileRegistration", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FromHeader", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Request.Headers", source, StringComparison.Ordinal);
        Assert.Contains("api/v1/mobile/registrations", source, StringComparison.Ordinal);
    }

    // --- Utilidades ---

    private static string ReadSource(params string[] relativePath)
    {
        string path = Path.Combine([FindSourceDirectory(), .. relativePath]);

        Assert.True(File.Exists(path), $"No se encontró el archivo: {path}");

        return File.ReadAllText(path);
    }

    private static IEnumerable<string> SourceFilesIn(params string[] relativePath)
    {
        string directory = Path.Combine([FindSourceDirectory(), .. relativePath]);

        return Directory
            .GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
                !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal) &&
                !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal));
    }

    private static IEnumerable<string> ProductionSourceFiles() =>
        SourceFilesIn("src");

    private static IEnumerable<string> MobileRegistrationSourceFiles() =>
        ProductionSourceFiles()
            .Where(f => Path.GetFileName(f).Contains(
                "MobileRegistration", StringComparison.Ordinal) ||
                Path.GetFileName(f).Contains("RegisterMobileUser", StringComparison.Ordinal));

    private static string FindSourceDirectory()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "Lyria.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("No se encontró la raíz del repositorio.");
    }
}
