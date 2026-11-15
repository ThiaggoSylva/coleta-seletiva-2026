using System.Text;

namespace ColetaSeletiva.Infrastructure.Seguranca;

public sealed class JwtSettings
{
    public const int MinimumSigningKeyBytes = 32;

    public JwtSettings(string issuer, string audience, string signingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(signingKey);

        if (Encoding.UTF8.GetByteCount(signingKey) < MinimumSigningKeyBytes)
        {
            throw new ArgumentException(
                $"A chave de assinatura deve ter pelo menos {MinimumSigningKeyBytes} bytes.",
                nameof(signingKey));
        }

        Issuer = issuer;
        Audience = audience;
        SigningKey = signingKey;
    }

    public string Issuer { get; }

    public string Audience { get; }

    public string SigningKey { get; }
}
