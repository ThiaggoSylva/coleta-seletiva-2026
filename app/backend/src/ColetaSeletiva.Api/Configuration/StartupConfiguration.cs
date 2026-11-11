using System.Text;

namespace ColetaSeletiva.Api.Configuration;

/// <summary>
/// Valida a configuração mínima de segurança antes de a API começar a ouvir.
/// Nenhuma mensagem inclui o valor das credenciais.
/// </summary>
public static class StartupConfiguration
{
    public const string ConnectionStringName = "ColetaSeletiva";
    public const int MinimumJwtSigningKeyBytes = 32;

    private const string JwtSectionName = "Jwt";

    public static void Validate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ValidateConnectionString(configuration);

        var jwt = configuration.GetSection(JwtSectionName);

        RequireValue(jwt, "Issuer", "Jwt__Issuer");
        RequireValue(jwt, "Audience", "Jwt__Audience");
        ValidateSigningKey(jwt);
    }

    private static void ValidateConnectionString(IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(ConnectionStringName)))
        {
            throw new InvalidOperationException(
                $"A connection string '{ConnectionStringName}' é obrigatória. "
                + "Configure-a com user-secrets no desenvolvimento ou com a variável de ambiente "
                + "'ConnectionStrings__ColetaSeletiva'.");
        }
    }

    private static void ValidateSigningKey(IConfigurationSection jwt)
    {
        RequireValue(jwt, "SigningKey", "Jwt__SigningKey");

        var signingKey = jwt["SigningKey"];
        if (Encoding.UTF8.GetByteCount(signingKey!) < MinimumJwtSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"A configuração 'Jwt:SigningKey' deve ter pelo menos {MinimumJwtSigningKeyBytes} bytes.");
        }
    }

    private static void RequireValue(IConfigurationSection section, string key, string environmentVariable)
    {
        if (string.IsNullOrWhiteSpace(section[key]))
        {
            throw new InvalidOperationException(
                $"A configuração 'Jwt:{key}' é obrigatória. "
                + $"Defina-a em appsettings ou na variável de ambiente '{environmentVariable}'.");
        }
    }
}
