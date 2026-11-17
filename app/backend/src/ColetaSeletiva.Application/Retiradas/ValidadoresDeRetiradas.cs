using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Retiradas;
using FluentValidation;

namespace ColetaSeletiva.Application.Retiradas;

public sealed class CriarRetiradaValidator : AbstractValidator<CriarRetiradaEntrada>
{
    public const int TamanhoMaximoDaListaDeItens = 50;

    public CriarRetiradaValidator()
    {
        RuleFor(entrada => entrada.OrganizacaoId)
            .NotEmpty()
            .WithMessage("Informe a organização.");

        RuleFor(entrada => entrada.MunicipioId)
            .GreaterThan(0)
            .WithMessage("Informe o município.");

        RuleFor(entrada => entrada.Bairro)
            .NotEmpty()
            .WithMessage("Informe o bairro.")
            .MaximumLength(SolicitacaoDeRetirada.BairroMaxLength)
            .WithMessage($"O bairro deve ter no máximo {SolicitacaoDeRetirada.BairroMaxLength} caracteres.");

        RuleFor(entrada => entrada.Rua)
            .NotEmpty()
            .WithMessage("Informe a rua.")
            .MaximumLength(SolicitacaoDeRetirada.RuaMaxLength)
            .WithMessage($"A rua deve ter no máximo {SolicitacaoDeRetirada.RuaMaxLength} caracteres.");

        RuleFor(entrada => entrada.Numero)
            .NotEmpty()
            .WithMessage("Informe o número.")
            .MaximumLength(SolicitacaoDeRetirada.NumeroMaxLength)
            .WithMessage($"O número deve ter no máximo {SolicitacaoDeRetirada.NumeroMaxLength} caracteres.");

        RuleFor(entrada => entrada.Complemento)
            .MaximumLength(SolicitacaoDeRetirada.ComplementoMaxLength)
            .WithMessage($"O complemento deve ter no máximo {SolicitacaoDeRetirada.ComplementoMaxLength} caracteres.");

        RuleFor(entrada => entrada.Cep)
            .Must(cep => cep is not null && cep.Count(char.IsDigit) == SolicitacaoDeRetirada.CepLength)
            .WithMessage("O CEP deve ter oito dígitos.");

        RuleFor(entrada => entrada.PontoReferencia)
            .MaximumLength(SolicitacaoDeRetirada.ReferenciaMaxLength)
            .WithMessage($"O ponto de referência deve ter no máximo {SolicitacaoDeRetirada.ReferenciaMaxLength} caracteres.");

        RuleFor(entrada => entrada.Observacoes)
            .MaximumLength(SolicitacaoDeRetirada.ObservacoesMaxLength)
            .WithMessage($"As observações devem ter no máximo {SolicitacaoDeRetirada.ObservacoesMaxLength} caracteres.");

        RuleFor(entrada => entrada.Data)
            .NotEqual(default(DateOnly))
            .WithMessage("Informe a data da retirada.");

        RuleFor(entrada => entrada.Periodo)
            .Must(periodo => Enum.TryParse<PeriodoDeRetirada>(periodo, true, out _))
            .WithMessage("Informe um período válido.");

        RuleFor(entrada => entrada.Itens)
            .NotEmpty()
            .WithMessage("Informe ao menos um tipo de resíduo.")
            .Must(itens => itens.Count <= TamanhoMaximoDaListaDeItens)
            .WithMessage($"Informe no máximo {TamanhoMaximoDaListaDeItens} tipos.")
            .Must(itens => itens.Select(item => item.TipoResiduoId).Distinct().Count() == itens.Count)
            .WithMessage("Não repita tipos de resíduo.");

        RuleForEach(entrada => entrada.Itens).ChildRules(item =>
        {
            item.RuleFor(valor => valor.TipoResiduoId)
                .NotEmpty()
                .WithMessage("Informe o tipo de resíduo.");

            item.RuleFor(valor => valor.QuantidadeEstimada)
                .Must(quantidade => quantidade > 0)
                .WithMessage("A quantidade estimada deve ser maior que zero.")
                .Must(SolicitacaoItem.QuantidadeValida)
                .WithMessage("A quantidade estimada deve ter no máximo três casas decimais.");
        });
    }
}

public sealed class DisponibilidadeSemanaValidator
    : AbstractValidator<DisponibilidadeSemanaEntrada>
{
    public DisponibilidadeSemanaValidator()
    {
        RuleFor(entrada => entrada.Dias)
            .NotEmpty()
            .WithMessage("Informe ao menos um dia e período.")
            .Must(dias => dias.Count <= 14)
            .WithMessage("Informe no máximo 14 combinações de dia e período.")
            .Must(dias => dias
                .Select(dia => $"{dia.DiaDaSemana}|{dia.Periodo}".ToUpperInvariant())
                .Distinct()
                .Count() == dias.Count)
            .WithMessage("Não repita a combinação de dia e período.");

        RuleForEach(entrada => entrada.Dias).ChildRules(dia =>
        {
            dia.RuleFor(valor => valor.DiaDaSemana)
                .Must(valor => Enum.TryParse<DayOfWeek>(valor, true, out _))
                .WithMessage("Informe um dia da semana válido.");

            dia.RuleFor(valor => valor.Periodo)
                .Must(valor => Enum.TryParse<PeriodoDeRetirada>(valor, true, out _))
                .WithMessage("Informe um período válido.");

            dia.RuleFor(valor => valor.Capacidade)
                .InclusiveBetween(0, DisponibilidadeDeRetirada.CapacidadeMaxima)
                .WithMessage($"A capacidade deve estar entre 0 e {DisponibilidadeDeRetirada.CapacidadeMaxima}.");
        });
    }
}

public sealed class RegistrarRecebimentoValidator : AbstractValidator<RegistrarRecebimentoEntrada>
{
    public RegistrarRecebimentoValidator()
    {
        RuleFor(entrada => entrada.PontoId)
            .NotEmpty()
            .WithMessage("Informe o ponto de coleta.");

        RuleFor(entrada => entrada.TipoResiduoId)
            .NotEmpty()
            .WithMessage("Informe o tipo de resíduo.");

        RuleFor(entrada => entrada.Quantidade)
            .GreaterThan(0)
            .WithMessage("A quantidade deve ser maior que zero.")
            .Must(SolicitacaoItem.QuantidadeValida)
            .WithMessage("A quantidade deve ter no máximo três casas decimais.");
    }
}

public sealed class AjustarVolumeValidator : AbstractValidator<AjustarVolumeEntrada>
{
    public AjustarVolumeValidator()
    {
        RuleFor(entrada => entrada.Quantidade)
            .NotEqual(0)
            .WithMessage("O ajuste deve ser diferente de zero.")
            .Must(SolicitacaoItem.QuantidadeValida)
            .WithMessage("A quantidade deve ter no máximo três casas decimais.");

        RuleFor(entrada => entrada.Motivo)
            .NotEmpty()
            .WithMessage("Informe o motivo do ajuste.")
            .MaximumLength(RegistroDeVolume.MotivoMaxLength)
            .WithMessage($"O motivo deve ter no máximo {RegistroDeVolume.MotivoMaxLength} caracteres.");
    }
}

public sealed class RecusarRetiradaValidator : AbstractValidator<RecusarRetiradaEntrada>
{
    public RecusarRetiradaValidator()
    {
        RuleFor(entrada => entrada.Motivo)
            .NotEmpty()
            .WithMessage("Informe o motivo da recusa.")
            .MaximumLength(SolicitacaoDeRetirada.MotivoMaxLength)
            .WithMessage($"O motivo deve ter no máximo {SolicitacaoDeRetirada.MotivoMaxLength} caracteres.");
    }
}

public sealed class NaoRealizadaValidator : AbstractValidator<NaoRealizadaEntrada>
{
    public NaoRealizadaValidator()
    {
        RuleFor(entrada => entrada.Motivo)
            .Must(motivo => Enum.TryParse<MotivoNaoRealizacao>(motivo, true, out _))
            .WithMessage("Informe um motivo válido para a não realização.");
    }
}

public sealed class ExecutarRetiradaValidator : AbstractValidator<ExecutarRetiradaEntrada>
{
    public ExecutarRetiradaValidator()
    {
        RuleFor(entrada => entrada.Itens)
            .NotEmpty()
            .WithMessage("Informe ao menos um volume coletado.")
            .Must(itens => itens.Select(item => item.TipoResiduoId).Distinct().Count() == itens.Count)
            .WithMessage("Não repita tipos de resíduo.");

        RuleForEach(entrada => entrada.Itens).ChildRules(item =>
        {
            item.RuleFor(valor => valor.TipoResiduoId)
                .NotEmpty()
                .WithMessage("Informe o tipo de resíduo.");

            item.RuleFor(valor => valor.Quantidade)
                .GreaterThan(0)
                .WithMessage("A quantidade coletada deve ser maior que zero.")
                .Must(SolicitacaoItem.QuantidadeValida)
                .WithMessage("A quantidade deve ter no máximo três casas decimais.");
        });
    }
}

public sealed class ReagendarRetiradaValidator : AbstractValidator<ReagendarRetiradaEntrada>
{
    public ReagendarRetiradaValidator()
    {
        RuleFor(entrada => entrada.Data)
            .NotEqual(default(DateOnly))
            .WithMessage("Informe a nova data.");

        RuleFor(entrada => entrada.Periodo)
            .Must(periodo => Enum.TryParse<PeriodoDeRetirada>(periodo, true, out _))
            .WithMessage("Informe um período válido.");
    }
}

public sealed class AgendarRetiradaValidator : AbstractValidator<AgendarRetiradaEntrada>
{
    public AgendarRetiradaValidator()
    {
        RuleFor(entrada => entrada)
            .Must(entrada => (entrada.Data is null) == (entrada.Periodo is null))
            .WithMessage("Informe data e período juntos ou omita ambos.");

        When(
            entrada => entrada.Data is not null,
            () =>
            {
                RuleFor(entrada => entrada.Data!.Value)
                    .NotEqual(default(DateOnly))
                    .WithMessage("Informe uma data válida.");

                RuleFor(entrada => entrada.Periodo!)
                    .Must(periodo => Enum.TryParse<PeriodoDeRetirada>(periodo, true, out _))
                    .WithMessage("Informe um período válido.");
            });
    }
}
