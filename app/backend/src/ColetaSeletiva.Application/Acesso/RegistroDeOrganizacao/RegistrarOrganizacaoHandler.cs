using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Identidade;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;

public sealed class RegistrarOrganizacaoHandler
{
    private readonly IValidator<RegistroDeOrganizacaoEntrada> _validador;
    private readonly IRegistroDeOrganizacoes _organizacoes;
    private readonly ICadastroDeContas _contas;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;

    public RegistrarOrganizacaoHandler(
        IValidator<RegistroDeOrganizacaoEntrada> validador,
        IRegistroDeOrganizacoes organizacoes,
        ICadastroDeContas contas,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio)
    {
        _validador = validador;
        _organizacoes = organizacoes;
        _contas = contas;
        _auditoria = auditoria;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
    }

    public async Task<Result<RegistroDeOrganizacaoResposta>> ExecutarAsync(
        RegistroDeOrganizacaoEntrada entrada,
        Guid correlacaoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        if (correlacaoId == Guid.Empty)
        {
            throw new ArgumentException("A correlação do registro não pode ser vazia.", nameof(correlacaoId));
        }

        var validacao = await _validador.ValidateAsync(entrada, cancellationToken);
        if (!validacao.IsValid)
        {
            return Result.Fail<RegistroDeOrganizacaoResposta>(
                validacao.Errors.Select(erro =>
                    new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage)));
        }

        var agora = _relogio.GetUtcNow();
        var organizacao = Organizacao.Criar(entrada.NomeOrganizacao, entrada.Tipo, agora);
        var usuario = entrada.Usuario.Trim();

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);
        try
        {
            await _organizacoes.AdicionarAsync(organizacao, cancellationToken);

            var conta = new ContaNova(
                Nome: usuario,
                Usuario: usuario,
                Email: entrada.Email.Trim(),
                Senha: entrada.Senha,
                OrganizacaoId: organizacao.Id,
                TrocaSenhaPendente: false);

            var criacao = await _contas.CriarAsync(
                conta,
                Papeis.Administrador,
                cancellationToken);

            if (criacao.IsFailed)
            {
                await transacao.RollbackAsync(cancellationToken);
                return Result.Fail<RegistroDeOrganizacaoResposta>(criacao.Errors);
            }

            await _auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: organizacao.Id,
                    AtorUsuarioId: criacao.Value,
                    Acao: AcoesAuditadas.OrganizacaoRegistrada,
                    Entidade: nameof(Organizacao),
                    EntidadeId: organizacao.Id,
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: agora),
                cancellationToken);

            await transacao.CommitAsync(cancellationToken);

            return Result.Ok(new RegistroDeOrganizacaoResposta(organizacao.Id, criacao.Value));
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
