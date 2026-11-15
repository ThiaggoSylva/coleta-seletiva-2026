namespace ColetaSeletiva.Application.Acesso.Equipe;

/// <summary>
/// Identifica se o alvo de uma operação de equipe existe fora da organização
/// da sessão. A consulta ignora o filtro de tenant apenas para decidir se a
/// negação deve ser auditada; nenhum dado do alvo é devolvido.
/// </summary>
public interface IDeteccaoDeAcessoCruzado
{
    Task<bool> ExisteEmOutraOrganizacaoAsync(
        Guid organizacaoId,
        Guid usuarioId,
        CancellationToken cancellationToken = default);
}
