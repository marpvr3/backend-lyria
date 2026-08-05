using System.Reflection;
using Xunit;

namespace Lyria.ArchitectureTests;

/// <summary>
/// Valida las reglas arquitectónicas y de seguridad de la autenticación móvil:
/// Application no depende de JWT, la API no emite tokens, los secretos no están
/// incrustados en el código y no se registran credenciales ni tokens.
/// </summary>
public class AuthenticationArchitectureTests
{
    private static readonly Assembly DomainAssembly =
        typeof(Domain.Users.User).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(Application.DependencyInjection).Assembly;

    private static readonly Assembly InfrastructureAssembly =
        typeof(Infrastructure.DependencyInjection).Assembly;

    private static readonly Assembly ApiAssembly = typeof(Api.Program).Assembly;

    private static Type GetType(Assembly assembly, string typeName) =>
        assembly.GetTypes().First(t => t.Name == typeName);

    // --- Application no depende de JWT ni de criptografía concreta ---

    [Fact]
    public void Application_DoesNotReferenceAnyJwtOrIdentityAssembly()
    {
        string[] referenced = [.. ApplicationAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)];

        foreach (string forbidden in new[]
        {
            "Microsoft.IdentityModel.JsonWebTokens",
            "Microsoft.IdentityModel.Tokens",
            "System.IdentityModel.Tokens.Jwt",
            "Microsoft.AspNetCore.Authentication.JwtBearer",
            "Microsoft.Extensions.Identity.Core",
            "Microsoft.AspNetCore.Identity"
        })
        {
            Assert.DoesNotContain(forbidden, referenced, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void Domain_DoesNotReferenceAnySecurityOrPersistenceAssembly()
    {
        string[] referenced = [.. DomainAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)];

        foreach (string forbidden in new[]
        {
            "Microsoft.IdentityModel.JsonWebTokens",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore.Authentication.JwtBearer"
        })
        {
            Assert.DoesNotContain(forbidden, referenced, StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// La emisión del token vive en Infrastructure; Application solo conoce la abstracción.
    /// </summary>
    [Fact]
    public void AccessTokenIssuance_LivesInInfrastructure()
    {
        Assert.NotNull(GetType(ApplicationAssembly, "IAccessTokenService"));
        Assert.NotNull(GetType(InfrastructureAssembly, "JwtAccessTokenService"));

        Assert.DoesNotContain(
            ApplicationAssembly.GetTypes(),
            t => t.Name.Contains("Jwt", StringComparison.Ordinal));
    }

    [Fact]
    public void Api_DoesNotIssueTokensDirectly()
    {
        // La API valida el Bearer, pero la emisión no le corresponde.
        Assert.DoesNotContain(
            ApiAssembly.GetTypes(),
            t => t.Name.Contains("AccessTokenService", StringComparison.Ordinal) ||
                 t.Name.Contains("RefreshTokenGenerator", StringComparison.Ordinal));
    }

    // --- El controller es delgado ---

    [Fact]
    public void AuthenticationController_DependsOnlyOnTheMediator()
    {
        Type controller = GetType(ApiAssembly, "AuthenticationController");

        ParameterInfo[] parameters = controller
            .GetConstructors()
            .Single()
            .GetParameters();

        Assert.Single(parameters);
        Assert.Equal("IMediator", parameters[0].ParameterType.Name);
    }

    [Fact]
    public void Controllers_DoNotVerifyPasswordsNorAccessEfCore()
    {
        foreach (Type controller in ApiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal)))
        {
            foreach (ParameterInfo parameter in controller
                .GetConstructors()
                .SelectMany(c => c.GetParameters()))
            {
                string typeName = parameter.ParameterType.Name;

                Assert.DoesNotContain("PasswordHasher", typeName, StringComparison.Ordinal);
                Assert.DoesNotContain("DbContext", typeName, StringComparison.Ordinal);
                Assert.DoesNotContain("Repository", typeName, StringComparison.Ordinal);
            }
        }
    }

    // --- La identidad procede del token, no del cliente ---

    [Fact]
    public void GetCurrentUserQuery_DoesNotAcceptAnyClientProvidedIdentity()
    {
        Type query = GetType(ApplicationAssembly, "GetCurrentUserQuery");

        PropertyInfo[] properties = query.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.Empty(properties);
    }

    [Fact]
    public void CurrentUser_ReadsTheIdentityFromTheSubClaim()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Api", "Security", "CurrentUser.cs");

        Assert.Contains("JwtRegisteredClaimNames.Sub", source, StringComparison.Ordinal);

        // No se admite ninguna identidad enviada por el cliente.
        foreach (string forbidden in new[]
        {
            "Request.Query",
            "Request.Form",
            "Request.Headers[",
            "FromBody",
            "FromQuery"
        })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Los roles no participan en la autenticación de esta versión, y Role.Code sigue
    /// eliminado.
    /// </summary>
    [Fact]
    public void Authentication_DoesNotUseRolesToIdentifyTheUser()
    {
        foreach (string file in AuthenticationSourceFiles())
        {
            string source = File.ReadAllText(file);

            Assert.DoesNotContain("Role.Name", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Role.Code", source, StringComparison.Ordinal);
            Assert.DoesNotContain("RoleCode", source, StringComparison.Ordinal);
        }
    }

    // --- Secretos ---

    [Fact]
    public void SigningKey_IsNotHardcodedAnywhereInProductionCode()
    {
        foreach (string file in ProductionSourceFiles())
        {
            string source = File.ReadAllText(file);

            Assert.DoesNotContain("SigningKey = \"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SigningKey=\"", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AppSettings_DoesNotShipASigningKeyValue()
    {
        string appSettings = ReadSourceFile("src", "Lyria.Api", "appsettings.json");

        Assert.Contains("\"SigningKey\": \"\"", appSettings, StringComparison.Ordinal);
    }

    [Fact]
    public void DevelopmentAppSettings_DoesNotContainASigningKey()
    {
        string path = Path.Combine(
            FindSourceDirectory(), "src", "Lyria.Api", "appsettings.Development.json");

        if (!File.Exists(path))
        {
            return;
        }

        Assert.DoesNotContain("SigningKey", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void JwtOptions_ValidatesIssuerAudienceKeyAndLifetimes()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Infrastructure", "Security", "JwtOptions.cs");

        Assert.Contains("[Required", source, StringComparison.Ordinal);
        Assert.Contains("MinLength", source, StringComparison.Ordinal);
        Assert.Contains("[Range(1", source, StringComparison.Ordinal);
    }

    // --- Refresh tokens ---

    [Fact]
    public void RefreshTokenGenerator_UsesACryptographicRandomSource()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Infrastructure", "Security", "RefreshTokenGenerator.cs");

        Assert.Contains("RandomNumberGenerator.GetBytes", source, StringComparison.Ordinal);
        Assert.Contains("SHA256.HashData", source, StringComparison.Ordinal);

        // El refresh token no es un JWT.
        Assert.DoesNotContain("JsonWebToken", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PasswordHashing_NeverUsesAWeakAlgorithm()
    {
        foreach (string file in ProductionSourceFiles())
        {
            string source = File.ReadAllText(file);

            Assert.DoesNotContain("MD5", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SHA1.", source, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// SHA-256 solo puede invocarse donde se hashea el refresh token, nunca como
    /// sustituto del password hasher.
    /// </summary>
    [Fact]
    public void Sha256_IsOnlyInvokedByTheRefreshTokenGenerator()
    {
        string[] callers = [.. ProductionSourceFiles()
            .Where(file =>
            {
                string source = File.ReadAllText(file);

                // Invocaciones reales del algoritmo, no menciones en comentarios.
                return source.Contains("SHA256.HashData", StringComparison.Ordinal) ||
                       source.Contains("SHA256.Create", StringComparison.Ordinal) ||
                       source.Contains("new SHA256", StringComparison.Ordinal);
            })
            .Select(file => Path.GetFileName(file)!)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(["RefreshTokenGenerator.cs"], callers);
    }

    [Fact]
    public void PasswordVerification_DoesNotCompareHashesAsStrings()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Infrastructure", "Security", "PasswordHasher.cs");

        Assert.Contains("VerifyHashedPassword", source, StringComparison.Ordinal);
        Assert.DoesNotContain("hashedPassword ==", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string.Equals(hashedPassword", source, StringComparison.Ordinal);
    }

    // --- Logging ---

    [Fact]
    public void AuthenticationLogging_NeverIncludesCredentialsOrTokens()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Application", "Features", "Authentication", "AuthenticationLog.cs");

        foreach (string forbidden in new[]
        {
            "{Email}", "{Password}", "{PasswordHash}", "{Token}",
            "{AccessToken}", "{RefreshToken}", "{TokenHash}", "{SigningKey}",
            "{Authorization}"
        })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AuthenticationHandlers_DoNotLogAnySensitiveValue()
    {
        foreach (string file in AuthenticationSourceFiles())
        {
            string source = File.ReadAllText(file);

            Assert.DoesNotContain("logger.Log", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SensitiveDataRedactor_RedactsTokenAndKeyFields()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Api", "Extensions", "SensitiveDataRedactor.cs");

        foreach (string key in new[]
        {
            "\"password\"", "\"passwordHash\"", "\"accessToken\"", "\"refreshToken\"",
            "\"tokenHash\"", "\"refreshTokenHash\"", "\"signingKey\"", "\"authorization\""
        })
        {
            Assert.Contains(key, source, StringComparison.Ordinal);
        }
    }

    // --- Pipeline y autorización ---

    [Fact]
    public void Pipeline_RunsAuthenticationBeforeAuthorizationAndBothBeforeControllers()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Api", "Extensions", "WebApplicationExtensions.cs");

        int routing = source.IndexOf("UseRouting()", StringComparison.Ordinal);
        int cors = source.IndexOf("UseCors(", StringComparison.Ordinal);
        int rateLimiter = source.IndexOf("UseRateLimiter()", StringComparison.Ordinal);
        int authentication = source.IndexOf("UseAuthentication()", StringComparison.Ordinal);
        int authorization = source.IndexOf("UseAuthorization()", StringComparison.Ordinal);
        int controllers = source.IndexOf("MapControllers()", StringComparison.Ordinal);

        Assert.True(routing > 0);
        Assert.True(routing < cors);
        Assert.True(cors < rateLimiter);
        Assert.True(rateLimiter < authentication);
        Assert.True(authentication < authorization);
        Assert.True(authorization < controllers);
    }

    [Fact]
    public void Pipeline_DoesNotDuplicateRoutingOrCors()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Api", "Extensions", "WebApplicationExtensions.cs");

        Assert.Equal(1, CountOccurrences(source, "UseRouting()"));
        Assert.Equal(1, CountOccurrences(source, "app.UseCors("));
    }

    /// <summary>
    /// No debe existir una política de autorización global: rompería los endpoints
    /// públicos y el registro móvil.
    /// </summary>
    [Fact]
    public void Authorization_HasNoGlobalFallbackPolicy()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Api", "Extensions", "AuthenticationExtensions.cs");

        Assert.DoesNotContain("FallbackPolicy", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RequireAuthenticatedUser", source, StringComparison.Ordinal);
    }

    [Fact]
    public void JwtBearer_ValidatesSignatureIssuerAudienceAndLifetime()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Api", "Extensions", "AuthenticationExtensions.cs");

        foreach (string validation in new[]
        {
            "ValidateIssuerSigningKey = true",
            "ValidateIssuer = true",
            "ValidateAudience = true",
            "ValidateLifetime = true",
            "RequireExpirationTime = true",
            "ClockSkew = TimeSpan.Zero"
        })
        {
            Assert.Contains(validation, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RateLimiting_IsNotAppliedGlobally()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Api", "Extensions", "RateLimitingExtensions.cs");

        Assert.Contains("AddPolicy", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GlobalLimiter", source, StringComparison.Ordinal);
    }

    // --- Migraciones ---

    [Fact]
    public void Startup_DoesNotUseEnsureCreated()
    {
        foreach (string file in ProductionSourceFiles())
        {
            Assert.DoesNotContain(
                "EnsureCreated", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    // --- Paquetes ---

    [Fact]
    public void OnlyAuthorizedAuthenticationPackagesAreDeclared()
    {
        string packages = ReadSourceFile("Directory.Packages.props");

        Assert.Contains(
            "Microsoft.AspNetCore.Authentication.JwtBearer", packages, StringComparison.Ordinal);
        Assert.Contains(
            "Microsoft.IdentityModel.JsonWebTokens", packages, StringComparison.Ordinal);

        // Ningún paquete de identidad completo ni de terceros para autenticación.
        foreach (string forbidden in new[]
        {
            "Microsoft.AspNetCore.Identity.EntityFrameworkCore",
            "IdentityServer",
            "Duende",
            "OpenIddict",
            "MediatR"
        })
        {
            Assert.DoesNotContain(forbidden, packages, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void NoPackageReferenceDeclaresAnInlineVersion()
    {
        foreach (string project in new[]
        {
            Path.Combine("src", "Lyria.Api", "Lyria.Api.csproj"),
            Path.Combine("src", "Lyria.Application", "Lyria.Application.csproj"),
            Path.Combine("src", "Lyria.Infrastructure", "Lyria.Infrastructure.csproj")
        })
        {
            string[] offending = [.. File
                .ReadAllLines(Path.Combine(FindSourceDirectory(), project))
                .Where(line =>
                    line.Contains("PackageReference", StringComparison.Ordinal) &&
                    line.Contains("Version=", StringComparison.Ordinal))];

            Assert.Empty(offending);
        }
    }

    // --- Utilidades ---

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = source.IndexOf(value, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = source.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static IEnumerable<string> AuthenticationSourceFiles()
    {
        string root = FindSourceDirectory();

        return Directory.EnumerateFiles(
            Path.Combine(root, "src", "Lyria.Application", "Features", "Authentication"),
            "*.cs",
            SearchOption.AllDirectories);
    }

    private static IEnumerable<string> ProductionSourceFiles()
    {
        string root = FindSourceDirectory();

        return Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                            StringComparison.Ordinal) &&
                        !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                            StringComparison.Ordinal));
    }

    private static string ReadSourceFile(params string[] segments)
    {
        string path = Path.Combine([FindSourceDirectory(), .. segments]);

        Assert.True(File.Exists(path), $"No se encontró el archivo: {path}");

        return File.ReadAllText(path);
    }

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
