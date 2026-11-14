using ColetaSeletiva.Application.Auditoria;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Persistencia;
using ColetaSeletiva.Domain.Identidade;
using FluentResults;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.RegistroDeCidadao;

public sealed class RegistrarCidadaoHandler
{
    private readonly IValidator<RegistroDeCidadaoEntrada> _validador;
    private readonly ICadastroDeContas _contas;
    private readonly IRegistroDeAuditoria _auditoria;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;

    public RegistrarCidadaoHandler(
        IValidator<RegistroDeCidadaoEntrada> validador,
        ICadastroDeContas contas,
        IRegistroDeAuditoria auditoria,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio)
    {
        _validador = validador;
        _contas = contas;
        _auditoria = auditoria;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
    }

    public async Task<Result<RegistroDeCidadaoResposta>> ExecutarAsync(
        RegistroDeCidadaoEntrada entrada,
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
            return Result.Fail<RegistroDeCidadaoResposta>(
                validacao.Errors.Select(erro =>
                    new ErroDeValidacao(erro.PropertyName, erro.ErrorMessage)));
        }

        var agora = _relogio.GetUtcNow();
        var email = entrada.Email.Trim();

        await using var transacao = await _unidadeDeTrabalho.IniciarAsync(cancellationToken);
        try
        {
            var conta = new ContaNova(
                Nome: entrada.Nome.Trim(),
                Usuario: email,
                Email: email,
                Telefone: entrada.Telefone.Trim(),
                Senha: entrada.Senha,
                OrganizacaoId: null,
                TrocaSenhaPendente: false);

            var criacao = await _contas.CriarAsync(conta, Papeis.Cidadao, cancellationToken);
            if (criacao.IsFailed)
            {
                await transacao.RollbackAsync(cancellationToken);
                return Result.Fail<RegistroDeCidadaoResposta>(criacao.Errors);
            }

            await _auditoria.RegistrarAsync(
                new EventoDeAuditoria(
                    OrganizacaoId: null,
                    AtorUsuarioId: criacao.Value,
                    Acao: AcoesAuditadas.CidadaoRegistrado,
                    Entidade: EntidadesAuditadas.Usuario,
                    EntidadeId: criacao.Value,
                    CorrelacaoId: correlacaoId,
                    OcorridoEmUtc: agora),
                cancellationToken);

            await transacao.CommitAsync(cancellationToken);

            return Result.Ok(new RegistroDeCidadaoResposta(criacao.Value));
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
