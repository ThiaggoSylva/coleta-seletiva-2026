namespace ColetaSeletiva.Api.Contratos;

public sealed record SessaoResposta(
    string TokenDeAcesso,
    DateTimeOffset ExpiraEmUtc,
    ContaDaSessaoResposta Usuario);

public sealed record ContaDaSessaoResposta(
    Guid Id,
    string Nome,
    string Email,
    string Papel,
    Guid? OrganizacaoId,
    bool TrocaSenhaPendente);
