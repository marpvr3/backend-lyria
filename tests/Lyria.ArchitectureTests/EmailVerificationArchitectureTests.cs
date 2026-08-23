using System.Reflection;
using Xunit;

namespace Lyria.ArchitectureTests;

/// <summary>
/// Valida las reglas arquitectónicas y de seguridad de la verificación de correo:
/// Application no conoce SMTP, la API no genera ni hashea códigos, los secretos no están
/// incrustados, el código nunca se persiste ni se registra en claro y el correo se envía
/// siempre fuera de la transacción.
/// </summary>
public class EmailVerificationArchitectureTests
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

    // --- Application no depende del proveedor de correo ---

    [Fact]
    public void Application_DoesNotReferenceAnyEmailProviderAssembly()
    {
        string[] referenced = [.. ApplicationAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)];

        foreach (string forbidden in new[] { "MailKit", "MimeKit", "System.Net.Mail" })
        {
            Assert.DoesNotContain(forbidden, referenced, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void Application_DoesNotMentionAnySmtpType()
    {
        foreach (string file in EmailVerificationApplicationFiles())
        {
            string source = File.ReadAllText(file);

            foreach (string forbidden in new[]
            {
                "SmtpClient", "MailKit", "MimeKit", "MailMessage",
                "System.Net.Mail", "SecureSocketOptions"
            })
            {
                Assert.False(
                    source.Contains(forbidden, StringComparison.Ordinal),
                    $"Application no debe conocer el proveedor de correo: {file}");
            }
        }
    }

    /// <summary>
    /// El envío vive en Infrastructure; Application solo conoce la abstracción.
    /// </summary>
    [Fact]
    public void EmailSending_LivesInInfrastructure()
    {
        Assert.NotNull(GetType(ApplicationAssembly, "IEmailSender"));

        Type sender = GetType(InfrastructureAssembly, "SmtpEmailSender");

        Assert.Equal(InfrastructureAssembly, sender.Assembly);
        Assert.False(sender.IsPublic);
    }

    [Fact]
    public void CodeGenerationAndHashing_LiveInInfrastructure()
    {
        Assert.NotNull(GetType(ApplicationAssembly, "IEmailVerificationCodeGenerator"));
        Assert.NotNull(GetType(ApplicationAssembly, "IEmailVerificationCodeHasher"));

        foreach (string typeName in new[]
        {
            "EmailVerificationCodeGenerator", "EmailVerificationCodeHasher"
        })
        {
            Type type = GetType(InfrastructureAssembly, typeName);

            Assert.Equal(InfrastructureAssembly, type.Assembly);
            Assert.False(type.IsPublic);
        }
    }

    // --- La API no genera códigos ni envía correo ---

    [Fact]
    public void Api_DoesNotGenerateOrHashCodes()
    {
        Assert.DoesNotContain(
            ApiAssembly.GetTypes(),
            t => t.Name.Contains("CodeGenerator", StringComparison.Ordinal) ||
                 t.Name.Contains("CodeHasher", StringComparison.Ordinal) ||
                 t.Name.Contains("EmailSender", StringComparison.Ordinal));
    }

    [Fact]
    public void Controller_DependsOnlyOnTheMediator()
    {
        Type controller = GetType(ApiAssembly, "EmailVerificationController");

        ParameterInfo[] parameters = controller
            .GetConstructors()
            .Single()
            .GetParameters();

        Assert.Single(parameters);
        Assert.Equal("IMediator", parameters[0].ParameterType.Name);
    }

    [Fact]
    public void Controller_DoesNotSendEmailNorAccessEfCore()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Api", "Controllers", "V1", "EmailVerificationController.cs");

        foreach (string forbidden in new[]
        {
            "IEmailSender", "SmtpClient", "DbContext", "BeginTransaction",
            "SaveChanges", "using Microsoft.EntityFrameworkCore", "RandomNumberGenerator",
            "HMACSHA256"
        })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// No se crearon endpoints adicionales: el envío inicial forma parte del registro
    /// móvil.
    /// </summary>
    [Fact]
    public void OnlyTwoEndpointsWereCreated()
    {
        Type controller = GetType(ApiAssembly, "EmailVerificationController");

        MethodInfo[] actions = [.. controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes(true).Any(a =>
                a.GetType().Name.StartsWith("Http", StringComparison.Ordinal) &&
                a.GetType().Name.EndsWith("Attribute", StringComparison.Ordinal)))];

        Assert.Equal(2, actions.Length);
        Assert.Contains(actions, m => m.Name == "Resend");
        Assert.Contains(actions, m => m.Name == "Confirm");
    }

    // --- El código nunca se persiste ni se registra en claro ---

    [Fact]
    public void TheDomainEntity_OnlyStoresTheHash()
    {
        Type entity = GetType(DomainAssembly, "UserEmailVerification");

        string[] properties = [.. entity.GetProperties().Select(p => p.Name)];

        Assert.Contains("CodeHash", properties, StringComparer.Ordinal);

        foreach (string forbidden in new[]
        {
            "Code", "Email", "Password", "Token", "RefreshToken", "SmtpPassword"
        })
        {
            Assert.DoesNotContain(forbidden, properties, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void TheEfConfiguration_DoesNotMapAnyPlainCodeColumn()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Infrastructure", "Persistence", "Configurations",
            "UserEmailVerificationConfiguration.cs");

        Assert.Contains("CodigoHash", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Codigo\"", source, StringComparison.Ordinal);
        Assert.Contains("DeleteBehavior.Restrict", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteBehavior.Cascade", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ningún mensaje de registro puede llevar el código, su hash o el secreto.
    /// </summary>
    [Fact]
    public void Logging_NeverIncludesTheCodeOrAnySecret()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Application", "Features", "EmailVerifications",
            "EmailVerificationLog.cs");

        foreach (string forbidden in new[]
        {
            "{Code}", "{VerificationCode}", "{CodeHash}", "{CodeSecret}",
            "{Email}", "{Password}", "{SmtpPassword}", "{Body}"
        })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheHandlers_DoNotLogAnySensitiveValueDirectly()
    {
        foreach (string file in EmailVerificationApplicationFiles())
        {
            string source = File.ReadAllText(file);

            foreach (string forbidden in new[]
            {
                "logger.Log", "LogInformation", "LogWarning", "LogError", "LogDebug"
            })
            {
                Assert.False(
                    source.Contains(forbidden, StringComparison.Ordinal),
                    $"El registro debe pasar por EmailVerificationLog: {file}");
            }
        }
    }

    [Fact]
    public void TheSmtpAdapter_DoesNotLogAnything()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Infrastructure", "Notifications", "SmtpEmailSender.cs");

        foreach (string forbidden in new[]
        {
            "ILogger", "logger.Log", "LogInformation", "LogError", "Console.Write"
        })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
        }
    }

    // --- Secretos ---

    [Fact]
    public void TheCodeSecret_IsNotHardcodedAnywhereInProductionCode()
    {
        foreach (string file in ProductionSourceFiles())
        {
            string source = File.ReadAllText(file);

            Assert.DoesNotContain("CodeSecret = \"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("CodeSecret=\"", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheSmtpPassword_IsNotHardcodedAnywhereInProductionCode()
    {
        foreach (string file in ProductionSourceFiles())
        {
            string source = File.ReadAllText(file);

            Assert.DoesNotContain("Password = \"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Password=\"", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AppSettings_ShipsNoSecretValue()
    {
        string appSettings = ReadSourceFile("src", "Lyria.Api", "appsettings.json");

        Assert.Contains("\"CodeSecret\": \"\"", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"Password\": \"\"", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"SmtpHost\": \"\"", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"Username\": \"\"", appSettings, StringComparison.Ordinal);
    }

    [Fact]
    public void DevelopmentAppSettings_ContainsNoEmailSecret()
    {
        string path = Path.Combine(
            FindSourceDirectory(), "src", "Lyria.Api", "appsettings.Development.json");

        if (!File.Exists(path))
        {
            return;
        }

        string source = File.ReadAllText(path);

        Assert.DoesNotContain("CodeSecret", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SmtpHost", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// El secreto de los códigos es independiente de la clave de firma de los JWT.
    /// </summary>
    [Fact]
    public void TheCodeSecret_IsNeverTheJwtSigningKey()
    {
        foreach (string file in new[]
        {
            Path.Combine("src", "Lyria.Infrastructure", "Security",
                "EmailVerificationCodeHasher.cs"),
            Path.Combine("src", "Lyria.Infrastructure", "Security",
                "EmailVerificationCodeGenerator.cs"),
            Path.Combine("src", "Lyria.Infrastructure", "Security",
                "EmailVerificationOptions.cs")
        })
        {
            string source = File.ReadAllText(Path.Combine(FindSourceDirectory(), file));

            Assert.DoesNotContain("JwtOptions>", source, StringComparison.Ordinal);
            Assert.DoesNotContain("options.Value.SigningKey", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EmailVerificationOptions_ValidatesTheSecretAndEveryPolicyValue()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Infrastructure", "Security", "EmailVerificationOptions.cs");

        Assert.Contains("[Required", source, StringComparison.Ordinal);
        Assert.Contains("MinLength", source, StringComparison.Ordinal);
        Assert.Contains("[Range(1", source, StringComparison.Ordinal);

        // La longitud del código está fijada en 6 para esta versión.
        Assert.Contains(
            "[Range(SupportedCodeLength, SupportedCodeLength", source, StringComparison.Ordinal);
    }

    // --- El correo se envía fuera de la transacción ---

    /// <summary>
    /// Ningún coordinador transaccional puede conocer el remitente de correo.
    /// </summary>
    [Fact]
    public void TheTransactionalWriters_DoNotSendEmail()
    {
        foreach (string fileName in new[]
        {
            "EmailVerificationWriter.cs", "MobileRegistrationWriter.cs"
        })
        {
            string source = ReadSourceFile(
                "src", "Lyria.Infrastructure", "Persistence", "Repositories", fileName);

            foreach (string forbidden in new[]
            {
                "IEmailSender", "SmtpClient", "SendEmailVerificationCodeAsync"
            })
            {
                Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// En los tres casos de uso que emiten un código, el envío se invoca después de la
    /// escritura transaccional.
    /// </summary>
    [Theory]
    [InlineData("RegisterMobileUserCommandHandler.cs", "registrationWriter.RegisterAsync")]
    [InlineData("ResendEmailVerificationCommandHandler.cs", "verificationWriter.ResendAsync")]
    public void TheEmail_IsSentAfterThePersistence(string fileName, string writerCall)
    {
        string source = HandlerSourceFiles()
            .Where(f => Path.GetFileName(f) == fileName)
            .Select(File.ReadAllText)
            .Single();

        int persistenceIndex = source.IndexOf(writerCall, StringComparison.Ordinal);
        int sendIndex = source.IndexOf("SendSafelyAsync", StringComparison.Ordinal);

        Assert.True(persistenceIndex >= 0, "El handler debe confirmar la escritura.");
        Assert.True(sendIndex >= 0, "El handler debe enviar el correo.");
        Assert.True(
            persistenceIndex < sendIndex,
            "El correo debe enviarse después de confirmar la transacción.");
    }

    [Fact]
    public void TheHandlers_DoNotCoordinateTransactionsThemselves()
    {
        foreach (string file in EmailVerificationApplicationFiles())
        {
            string source = File.ReadAllText(file);

            Assert.DoesNotContain("BeginTransaction", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Rollback", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheWriterAbstraction_DoesNotExposeTransactionControl()
    {
        Type abstraction = GetType(ApplicationAssembly, "IEmailVerificationWriter");

        Assert.DoesNotContain(
            abstraction.GetMethods(),
            m => m.Name.Contains("Transaction", StringComparison.OrdinalIgnoreCase) ||
                 m.Name.Contains("Commit", StringComparison.OrdinalIgnoreCase) ||
                 m.Name.Contains("Rollback", StringComparison.OrdinalIgnoreCase));
    }

    // --- Redacción de datos sensibles ---

    [Fact]
    public void SensitiveDataRedactor_RedactsTheVerificationFields()
    {
        string source = ReadSourceFile(
            "src", "Lyria.Api", "Extensions", "SensitiveDataRedactor.cs");

        foreach (string key in new[]
        {
            "\"code\"", "\"verificationCode\"", "\"codeHash\"", "\"codeSecret\"",
            "\"smtpPassword\""
        })
        {
            Assert.Contains(key, source, StringComparison.Ordinal);
        }
    }

    // --- Paquetes ---

    [Fact]
    public void OnlyOneEmailPackageIsDeclared()
    {
        string packages = ReadSourceFile("Directory.Packages.props");

        Assert.Contains("MailKit", packages, StringComparison.Ordinal);

        // Ningún otro proveedor ni servicio externo de correo.
        foreach (string forbidden in new[]
        {
            "SendGrid", "Mailgun", "Amazon.SimpleEmail", "FluentEmail", "Postmark"
        })
        {
            Assert.DoesNotContain(forbidden, packages, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheEmailPackage_IsOnlyReferencedByInfrastructure()
    {
        foreach (string project in new[]
        {
            Path.Combine("src", "Lyria.Api", "Lyria.Api.csproj"),
            Path.Combine("src", "Lyria.Application", "Lyria.Application.csproj"),
            Path.Combine("src", "Lyria.Domain", "Lyria.Domain.csproj")
        })
        {
            string source = File.ReadAllText(Path.Combine(FindSourceDirectory(), project));

            Assert.DoesNotContain("MailKit", source, StringComparison.Ordinal);
            Assert.DoesNotContain("MimeKit", source, StringComparison.Ordinal);
        }

        string infrastructure = ReadSourceFile(
            "src", "Lyria.Infrastructure", "Lyria.Infrastructure.csproj");

        Assert.Contains("MailKit", infrastructure, StringComparison.Ordinal);
        Assert.DoesNotContain("Version=", infrastructure, StringComparison.Ordinal);
    }

    // --- Utilidades ---

    private static IEnumerable<string> EmailVerificationApplicationFiles()
    {
        string root = FindSourceDirectory();

        return Directory.EnumerateFiles(
            Path.Combine(root, "src", "Lyria.Application", "Features", "EmailVerifications"),
            "*.cs",
            SearchOption.AllDirectories);
    }

    private static IEnumerable<string> HandlerSourceFiles()
    {
        string root = FindSourceDirectory();

        return Directory.EnumerateFiles(
            Path.Combine(root, "src", "Lyria.Application", "Features"),
            "*CommandHandler.cs",
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
