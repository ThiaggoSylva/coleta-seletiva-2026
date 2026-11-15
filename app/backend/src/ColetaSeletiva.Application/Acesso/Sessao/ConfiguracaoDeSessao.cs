namespace ColetaSeletiva.Application.Acesso.Sessao;

public sealed class ConfiguracaoDeSessao
{
    public ConfiguracaoDeSessao(TimeSpan validadeDoTokenDeAcesso, TimeSpan validadeDoRefresh)
    {
        if (validadeDoTokenDeAcesso <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(validadeDoTokenDeAcesso),
                "A validade do token de acesso deve ser positiva.");
        }

        if (validadeDoRefresh <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(validadeDoRefresh),
                "A validade do refresh token deve ser positiva.");
        }

        if (validadeDoRefresh <= validadeDoTokenDeAcesso)
        {
            throw new ArgumentOutOfRangeException(
                nameof(validadeDoRefresh),
                "A validade do refresh token deve ser maior que a do token de acesso.");
        }

        ValidadeDoTokenDeAcesso = validadeDoTokenDeAcesso;
        ValidadeDoRefresh = validadeDoRefresh;
    }

    public static ConfiguracaoDeSessao Padrao { get; } =
        new(TimeSpan.FromMinutes(15), TimeSpan.FromDays(7));

    public TimeSpan ValidadeDoTokenDeAcesso { get; }

    public TimeSpan ValidadeDoRefresh { get; }
}
