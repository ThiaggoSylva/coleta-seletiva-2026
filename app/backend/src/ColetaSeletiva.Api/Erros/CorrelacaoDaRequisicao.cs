namespace ColetaSeletiva.Api.Erros;

public static class CorrelacaoDaRequisicao
{
    public static Guid ObterCorrelacaoId(this HttpRequest requisicao)
    {
        ArgumentNullException.ThrowIfNull(requisicao);

        if (requisicao.Headers.TryGetValue(
                ResultadoDeErroExtensions.CabecalhoDeCorrelacao,
                out var valores)
            && Guid.TryParse(valores.FirstOrDefault(), out var correlacaoId))
        {
            return correlacaoId;
        }

        return Guid.NewGuid();
    }
}
