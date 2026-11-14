namespace ColetaSeletiva.Api.Configuration;

public sealed record ConfiguracaoDeLimiteDeTaxa(int PermitLimit, int WindowSeconds);

public sealed record ConfiguracaoDeLimitesDeTaxa(
    ConfiguracaoDeLimiteDeTaxa Login,
    ConfiguracaoDeLimiteDeTaxa Refresh,
    ConfiguracaoDeLimiteDeTaxa Cadastro);
