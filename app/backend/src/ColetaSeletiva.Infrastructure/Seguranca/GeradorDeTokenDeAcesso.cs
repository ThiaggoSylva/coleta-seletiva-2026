using System.Text;
using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Domain.Identidade;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ColetaSeletiva.Infrastructure.Seguranca;

internal sealed class GeradorDeTokenDeAcesso : IGeradorDeTokenDeAcesso
{
    private readonly JwtSettings _configuracao;
    private readonly ConfiguracaoDeSessao _sessao;
    private readonly JsonWebTokenHandler _manipulador = new();
    private readonly SigningCredentials _credenciais;

    public GeradorDeTokenDeAcesso(JwtSettings configuracao, ConfiguracaoDeSessao sessao)
    {
        _configuracao = configuracao;
        _sessao = sessao;
        _credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracao.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public TokenDeAcesso Gerar(ContaAutenticada conta, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(conta);

        var expiraEm = agora.Add(_sessao.ValidadeDoTokenDeAcesso);

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [JwtRegisteredClaimNames.Sub] = conta.Id.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            ["role"] = conta.Papel,
        };

        if (conta.OrganizacaoId is Guid organizacaoId)
        {
            claims[ClaimsColetaSeletiva.OrganizacaoId] = organizacaoId.ToString();
        }

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _configuracao.Issuer,
            Audience = _configuracao.Audience,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            Claims = claims,
            SigningCredentials = _credenciais,
        };

        return new TokenDeAcesso(_manipulador.CreateToken(descritor), expiraEm);
    }
}
