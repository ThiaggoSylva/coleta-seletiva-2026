namespace ColetaSeletiva.Api.Seguranca;

public static class ConstantesDeSessao
{
    public const string NomeDoCookie = "coleta_refresh";

    public const string CaminhoDoCookie = "/api/v1/auth";

    public const string CabecalhoDeOrigem = "Origin";

    public const string CabecalhoSolicitadoCom = "X-Requested-With";

    public const string ValorSolicitadoCom = "XMLHttpRequest";
}
