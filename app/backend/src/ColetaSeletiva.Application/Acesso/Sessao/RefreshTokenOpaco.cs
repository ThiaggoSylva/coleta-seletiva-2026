using System.Security.Cryptography;
using System.Text;

namespace ColetaSeletiva.Application.Acesso.Sessao;

public sealed record RefreshTokenOpaco(string Valor, string Hash)
{
    private const int TamanhoDoValorEmBytes = 32;

    public static RefreshTokenOpaco Gerar()
    {
        var valor = Convert
            .ToHexString(RandomNumberGenerator.GetBytes(TamanhoDoValorEmBytes))
            .ToLowerInvariant();

        return new RefreshTokenOpaco(valor, CalcularHash(valor));
    }

    public static string CalcularHash(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(valor));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
