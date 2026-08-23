using Lyria.Infrastructure.Notifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Notifications;

/// <summary>
/// Comprueba que el adaptador SMTP toma su configuración de las opciones y que un
/// problema de configuración produce un error controlado que no revela secretos.
/// </summary>
/// <remarks>
/// No se abre ninguna conexión real: las pruebas se detienen en la validación previa,
/// que es donde se decide si el envío puede intentarse.
/// </remarks>
public sealed class SmtpEmailSenderTests
{
    private const string Password = "contraseña-smtp-de-pruebas";

    private static SmtpEmailSender CreateSender(Action<EmailOptions>? configure = null)
    {
        var options = new EmailOptions
        {
            SmtpHost = "smtp.ejemplo.com",
            SmtpPort = 587,
            UseTls = true,
            Username = "usuario",
            Password = Password,
            FromAddress = "no-reply@ejemplo.com",
            FromName = "Lyria"
        };

        configure?.Invoke(options);

        return new SmtpEmailSender(Options.Create(options));
    }

    private static Task SendAsync(SmtpEmailSender sender) =>
        sender.SendEmailVerificationCodeAsync(
            "andres@email.com",
            "Andres",
            "482731",
            DateTime.UtcNow.AddMinutes(15),
            TestContext.Current.CancellationToken);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Send_WithoutSmtpHost_FailsWithAControlledError(string host)
    {
        SmtpEmailSender sender = CreateSender(options => options.SmtpHost = host);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendAsync(sender));

        Assert.Contains("Email__SmtpHost", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_WithoutFromAddress_FailsWithAControlledError()
    {
        SmtpEmailSender sender = CreateSender(options => options.FromAddress = "");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendAsync(sender));

        Assert.Contains("Email__FromAddress", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(70000)]
    public async Task Send_WithAnInvalidPort_FailsWithAControlledError(int port)
    {
        SmtpEmailSender sender = CreateSender(options => options.SmtpPort = port);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendAsync(sender));

        Assert.Contains("Email:SmtpPort", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ningún error de configuración puede filtrar la contraseña SMTP ni el usuario.
    /// </summary>
    [Fact]
    public async Task Send_ConfigurationErrors_NeverExposeTheCredentials()
    {
        SmtpEmailSender sender = CreateSender(options => options.SmtpHost = "");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendAsync(sender));

        Assert.DoesNotContain(Password, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("usuario", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("482731", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Las opciones no traen ningún valor real por omisión: los datos del proveedor
    /// llegan siempre de la configuración del servidor.
    /// </summary>
    [Fact]
    public void Options_ShipNoRealCredentials()
    {
        var options = new EmailOptions();

        Assert.Equal(string.Empty, options.SmtpHost);
        Assert.Equal(string.Empty, options.Username);
        Assert.Equal(string.Empty, options.Password);
        Assert.Equal(string.Empty, options.FromAddress);
        Assert.Equal(587, options.SmtpPort);
        Assert.True(options.UseTls);
        Assert.Equal("Lyria", options.FromName);
    }

    [Fact]
    public void SectionName_IsEmail()
    {
        Assert.Equal("Email", EmailOptions.SectionName);
    }
}
