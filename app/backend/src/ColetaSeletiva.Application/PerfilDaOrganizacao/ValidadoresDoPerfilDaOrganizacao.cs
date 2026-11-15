using System.Globalization;
using ColetaSeletiva.Domain.Entidades;
using FluentValidation;

namespace ColetaSeletiva.Application.PerfilDaOrganizacao;

public static class LimitesDoPerfilDaOrganizacao
{
    public const int TamanhoMaximoDaListaDeMunicipios = 100;
    public const int TamanhoMaximoDaListaDeTipos = 100;
    public const string FormatoDeHora = "HH:mm";
}

public sealed class AtualizarPerfilDaOrganizacaoValidator
    : AbstractValidator<AtualizarPerfilDaOrganizacaoEntrada>
{
    public AtualizarPerfilDaOrganizacaoValidator()
    {
        RuleFor(entrada => entrada.Nome)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o nome da organização.")
            .MaximumLength(Organizacao.NomeMaxLength)
            .WithMessage($"O nome deve ter no máximo {Organizacao.NomeMaxLength} caracteres.");

        RuleFor(entrada => entrada.Tipo)
            .Must(tipo => Enum.IsDefined(tipo))
            .WithMessage("Informe um tipo de organização válido.");

        RuleFor(entrada => entrada.ContatoEmail)
            .MaximumLength(Organizacao.ContatoEmailMaxLength)
            .WithMessage($"O e-mail deve ter no máximo {Organizacao.ContatoEmailMaxLength} caracteres.")
            .EmailAddress()
            .When(entrada => !string.IsNullOrWhiteSpace(entrada.ContatoEmail))
            .WithMessage("Informe um e-mail de contato válido.");

        RuleFor(entrada => entrada.ContatoTelefone)
            .MaximumLength(Organizacao.ContatoTelefoneMaxLength)
            .WithMessage($"O telefone deve ter no máximo {Organizacao.ContatoTelefoneMaxLength} caracteres.");

        RuleFor(entrada => entrada.Fuso)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o fuso da organização.")
            .MaximumLength(Organizacao.FusoMaxLength)
            .WithMessage($"O fuso deve ter no máximo {Organizacao.FusoMaxLength} caracteres.")
            .Must(SerFusoValido)
            .WithMessage("Informe um fuso IANA válido, como America/Sao_Paulo.");

        RuleFor(entrada => entrada.LimiteSolicitacoesAbertas)
            .InclusiveBetween(1, Organizacao.LimiteSolicitacoesAbertasMaximo)
            .WithMessage(
                $"O limite deve estar entre 1 e {Organizacao.LimiteSolicitacoesAbertasMaximo}.");
    }

    private static bool SerFusoValido(string fuso)
    {
        return !string.IsNullOrWhiteSpace(fuso)
            && TimeZoneInfo.TryFindSystemTimeZoneById(fuso.Trim(), out _);
    }
}

public sealed class DefinirMunicipiosValidator : AbstractValidator<DefinirMunicipiosEntrada>
{
    public DefinirMunicipiosValidator()
    {
        RuleFor(entrada => entrada.MunicipioIds)
            .NotNull()
            .WithMessage("Informe a lista de municípios.")
            .Must(ids => ids.Count <= LimitesDoPerfilDaOrganizacao.TamanhoMaximoDaListaDeMunicipios)
            .WithMessage(
                $"Informe no máximo {LimitesDoPerfilDaOrganizacao.TamanhoMaximoDaListaDeMunicipios} municípios.");

        RuleForEach(entrada => entrada.MunicipioIds)
            .GreaterThan(0)
            .WithMessage("Código de município inválido.");

        RuleFor(entrada => entrada.MunicipioIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Não repita municípios.")
            .When(entrada => entrada.MunicipioIds is not null);
    }
}

public sealed class DefinirPeriodosValidator : AbstractValidator<DefinirPeriodosEntrada>
{
    public DefinirPeriodosValidator()
    {
        RuleFor(entrada => entrada.Periodos)
            .NotNull()
            .WithMessage("Informe a lista de períodos.")
            .Must(periodos => periodos.Count <= 2)
            .WithMessage("Informe no máximo os períodos Manhã e Tarde.");

        RuleForEach(entrada => entrada.Periodos).ChildRules(periodo =>
        {
            periodo.RuleFor(item => item.Periodo)
                .Must(SerPeriodoValido)
                .WithMessage("Período inválido; use Manha ou Tarde.");

            periodo.RuleFor(item => item.HoraInicio)
                .Must(hora => TimeOnly.TryParseExact(
                    hora,
                    LimitesDoPerfilDaOrganizacao.FormatoDeHora,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
                .WithMessage("Informe a hora inicial no formato HH:mm.");

            periodo.RuleFor(item => item.HoraFim)
                .Must(hora => TimeOnly.TryParseExact(
                    hora,
                    LimitesDoPerfilDaOrganizacao.FormatoDeHora,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
                .WithMessage("Informe a hora final no formato HH:mm.");

            periodo.RuleFor(item => item)
                .Must(SerIntervaloValido)
                .WithMessage("A hora final deve ser maior que a inicial no mesmo dia.");
        });

        RuleFor(entrada => entrada.Periodos)
            .Must(periodos => periodos
                .GroupBy(periodo => periodo.Periodo, StringComparer.OrdinalIgnoreCase)
                .All(grupo => grupo.Count() == 1))
            .WithMessage("Não repita o mesmo período.")
            .When(entrada => entrada.Periodos is not null);
    }

    private static bool SerPeriodoValido(string periodo)
    {
        return Enum.TryParse<PeriodoDeRetirada>(periodo?.Trim(), ignoreCase: true, out _);
    }

    private static bool SerIntervaloValido(PeriodoDaOrganizacaoEntrada periodo)
    {
        var inicioValido = TimeOnly.TryParseExact(
            periodo.HoraInicio,
            LimitesDoPerfilDaOrganizacao.FormatoDeHora,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var inicio);
        var fimValido = TimeOnly.TryParseExact(
            periodo.HoraFim,
            LimitesDoPerfilDaOrganizacao.FormatoDeHora,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var fim);

        return inicioValido && fimValido && fim > inicio;
    }
}

public sealed class DefinirTiposHabilitadosValidator : AbstractValidator<DefinirTiposHabilitadosEntrada>
{
    public DefinirTiposHabilitadosValidator()
    {
        RuleFor(entrada => entrada.Tipos)
            .NotNull()
            .WithMessage("Informe a lista de tipos.")
            .Must(tipos => tipos.Count <= LimitesDoPerfilDaOrganizacao.TamanhoMaximoDaListaDeTipos)
            .WithMessage(
                $"Informe no máximo {LimitesDoPerfilDaOrganizacao.TamanhoMaximoDaListaDeTipos} tipos.");

        RuleForEach(entrada => entrada.Tipos).ChildRules(tipo =>
        {
            tipo.RuleFor(item => item.TipoResiduoId)
                .NotEmpty()
                .WithMessage("Tipo de resíduo inválido.");
        });

        RuleFor(entrada => entrada.Tipos)
            .Must(tipos => tipos
                .GroupBy(tipo => tipo.TipoResiduoId)
                .All(grupo => grupo.Count() == 1))
            .WithMessage("Não repita tipos de resíduo.")
            .When(entrada => entrada.Tipos is not null);
    }
}
