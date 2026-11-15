namespace ColetaSeletiva.Api.Seguranca;

public sealed class ConfiguracaoDeOrigem
{
    public ConfiguracaoDeOrigem(string? origemPermitida)
    {
        OrigemPermitida = string.IsNullOrWhiteSpace(origemPermitida)
            ? null
            : origemPermitida.Trim().TrimEnd('/');
    }

    public string? OrigemPermitida { get; }
}
