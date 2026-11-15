using System.Globalization;
using ColetaSeletiva.Domain.Entidades;
using FluentValidation;

namespace ColetaSeletiva.Application.Pontos;

public static class LimitesDosPontos
{
    public const int TamanhoMaximoDeHorarios = 50;
    public const int TamanhoMaximoDeTipos = 100;
    public const string FormatoDeHora = "HH:mm";
}

internal static class RegrasDoPonto
{
    public static bool SerCepValido(string cep)
    {
        return !string.IsNullOrWhiteSpace(cep)
            && cep.Count(char.IsDigit) == PontoDeColeta.CepLength;
    }

    public static bool SerHoraValida(string hora)
    {
        return TimeOnly.TryParseExact(
            hora,
            LimitesDosPontos.FormatoDeHora,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    public static bool SerStatusValido(string status)
    {
        return Enum.TryParse<StatusPonto>(status?.Trim(), ignoreCase: true, out _);
    }

    public static bool SerIntervaloValido(HorarioDoPontoEntrada horario)
    {
        var inicioValido = TimeOnly.TryParseExact(
            horario.HoraInicio,
            LimitesDosPontos.FormatoDeHora,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var inicio);
        var fimValido = TimeOnly.TryParseExact(
            horario.HoraFim,
            LimitesDosPontos.FormatoDeHora,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var fim);

        return inicioValido && fimValido && fim > inicio;
    }
}

public sealed class CriarPontoValidator : AbstractValidator<CriarPontoEntrada>
{
    public CriarPontoValidator()
    {
        RuleFor(entrada => entrada.Nome)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o nome do ponto.")
            .MaximumLength(PontoDeColeta.NomeMaxLength)
            .WithMessage($"O nome deve ter no máximo {PontoDeColeta.NomeMaxLength} caracteres.");

        RuleFor(entrada => entrada.MunicipioId)
            .GreaterThan(0)
            .WithMessage("Selecione o município do ponto.");

        RuleFor(entrada => entrada.Bairro)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o bairro.")
            .MaximumLength(PontoDeColeta.BairroMaxLength)
            .WithMessage($"O bairro deve ter no máximo {PontoDeColeta.BairroMaxLength} caracteres.");

        RuleFor(entrada => entrada.Rua)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe a rua.")
            .MaximumLength(PontoDeColeta.RuaMaxLength)
            .WithMessage($"A rua deve ter no máximo {PontoDeColeta.RuaMaxLength} caracteres.");

        RuleFor(entrada => entrada.Numero)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o número.")
            .MaximumLength(PontoDeColeta.NumeroMaxLength)
            .WithMessage($"O número deve ter no máximo {PontoDeColeta.NumeroMaxLength} caracteres.");

        RuleFor(entrada => entrada.Complemento)
            .MaximumLength(PontoDeColeta.ComplementoMaxLength)
            .WithMessage(
                $"O complemento deve ter no máximo {PontoDeColeta.ComplementoMaxLength} caracteres.");

        RuleFor(entrada => entrada.Cep)
            .Must(RegrasDoPonto.SerCepValido)
            .WithMessage("Informe um CEP com oito dígitos.");

        RuleFor(entrada => entrada.Latitude)
            .InclusiveBetween(PontoDeColeta.LatitudeMinima, PontoDeColeta.LatitudeMaxima)
            .WithMessage("Informe uma latitude válida.");

        RuleFor(entrada => entrada.Longitude)
            .InclusiveBetween(PontoDeColeta.LongitudeMinima, PontoDeColeta.LongitudeMaxima)
            .WithMessage("Informe uma longitude válida.");

        RuleFor(entrada => entrada.Observacoes)
            .MaximumLength(PontoDeColeta.ObservacoesMaxLength)
            .WithMessage(
                $"As observações devem ter no máximo {PontoDeColeta.ObservacoesMaxLength} caracteres.");

        RuleFor(entrada => entrada.Status)
            .Must(RegrasDoPonto.SerStatusValido)
            .WithMessage("Informe um status válido: Ativo, Inativo ou IndisponivelTemporariamente.");

        RuleFor(entrada => entrada.Horarios)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Informe ao menos um horário de funcionamento.")
            .Must(lista => lista.Count > 0)
            .WithMessage("Informe ao menos um horário de funcionamento.")
            .Must(lista => lista.Count <= LimitesDosPontos.TamanhoMaximoDeHorarios)
            .WithMessage(
                $"Informe no máximo {LimitesDosPontos.TamanhoMaximoDeHorarios} horários.");

        RuleForEach(entrada => entrada.Horarios).ChildRules(horario =>
        {
            horario.RuleFor(item => item.DiaDaSemana)
                .Must(dia => Enum.TryParse<DayOfWeek>(dia?.Trim(), ignoreCase: true, out _))
                .WithMessage("Informe um dia da semana válido.");

            horario.RuleFor(item => item.HoraInicio)
                .Must(RegrasDoPonto.SerHoraValida)
                .WithMessage("Informe a hora inicial no formato HH:mm.");

            horario.RuleFor(item => item.HoraFim)
                .Must(RegrasDoPonto.SerHoraValida)
                .WithMessage("Informe a hora final no formato HH:mm.");

            horario.RuleFor(item => item)
                .Must(RegrasDoPonto.SerIntervaloValido)
                .WithMessage("O horário deve terminar no mesmo dia, com fim maior que o início.");
        });

        RuleFor(entrada => entrada.TiposResiduoIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Informe ao menos um tipo aceito.")
            .Must(lista => lista.Count > 0)
            .WithMessage("Informe ao menos um tipo aceito.")
            .Must(lista => lista.Count <= LimitesDosPontos.TamanhoMaximoDeTipos)
            .WithMessage($"Informe no máximo {LimitesDosPontos.TamanhoMaximoDeTipos} tipos.")
            .Must(lista => lista.All(tipo => tipo != Guid.Empty))
            .WithMessage("Tipo de resíduo inválido.")
            .Must(lista => lista.Distinct().Count() == lista.Count)
            .WithMessage("Não repita tipos aceitos.");
    }
}

public sealed class AtualizarPontoValidator : AbstractValidator<AtualizarPontoEntrada>
{
    public AtualizarPontoValidator()
    {
        RuleFor(entrada => entrada.Nome)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o nome do ponto.")
            .MaximumLength(PontoDeColeta.NomeMaxLength)
            .WithMessage($"O nome deve ter no máximo {PontoDeColeta.NomeMaxLength} caracteres.");

        RuleFor(entrada => entrada.MunicipioId)
            .GreaterThan(0)
            .WithMessage("Selecione o município do ponto.");

        RuleFor(entrada => entrada.Bairro)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o bairro.")
            .MaximumLength(PontoDeColeta.BairroMaxLength)
            .WithMessage($"O bairro deve ter no máximo {PontoDeColeta.BairroMaxLength} caracteres.");

        RuleFor(entrada => entrada.Rua)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe a rua.")
            .MaximumLength(PontoDeColeta.RuaMaxLength)
            .WithMessage($"A rua deve ter no máximo {PontoDeColeta.RuaMaxLength} caracteres.");

        RuleFor(entrada => entrada.Numero)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o número.")
            .MaximumLength(PontoDeColeta.NumeroMaxLength)
            .WithMessage($"O número deve ter no máximo {PontoDeColeta.NumeroMaxLength} caracteres.");

        RuleFor(entrada => entrada.Complemento)
            .MaximumLength(PontoDeColeta.ComplementoMaxLength)
            .WithMessage(
                $"O complemento deve ter no máximo {PontoDeColeta.ComplementoMaxLength} caracteres.");

        RuleFor(entrada => entrada.Cep)
            .Must(RegrasDoPonto.SerCepValido)
            .WithMessage("Informe um CEP com oito dígitos.");

        RuleFor(entrada => entrada.Latitude)
            .InclusiveBetween(PontoDeColeta.LatitudeMinima, PontoDeColeta.LatitudeMaxima)
            .WithMessage("Informe uma latitude válida.");

        RuleFor(entrada => entrada.Longitude)
            .InclusiveBetween(PontoDeColeta.LongitudeMinima, PontoDeColeta.LongitudeMaxima)
            .WithMessage("Informe uma longitude válida.");

        RuleFor(entrada => entrada.Observacoes)
            .MaximumLength(PontoDeColeta.ObservacoesMaxLength)
            .WithMessage(
                $"As observações devem ter no máximo {PontoDeColeta.ObservacoesMaxLength} caracteres.");

        RuleFor(entrada => entrada.Status)
            .Must(RegrasDoPonto.SerStatusValido)
            .WithMessage("Informe um status válido: Ativo, Inativo ou IndisponivelTemporariamente.");

        RuleFor(entrada => entrada.Horarios)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Informe ao menos um horário de funcionamento.")
            .Must(lista => lista.Count > 0)
            .WithMessage("Informe ao menos um horário de funcionamento.")
            .Must(lista => lista.Count <= LimitesDosPontos.TamanhoMaximoDeHorarios)
            .WithMessage(
                $"Informe no máximo {LimitesDosPontos.TamanhoMaximoDeHorarios} horários.");

        RuleForEach(entrada => entrada.Horarios).ChildRules(horario =>
        {
            horario.RuleFor(item => item.DiaDaSemana)
                .Must(dia => Enum.TryParse<DayOfWeek>(dia?.Trim(), ignoreCase: true, out _))
                .WithMessage("Informe um dia da semana válido.");

            horario.RuleFor(item => item.HoraInicio)
                .Must(RegrasDoPonto.SerHoraValida)
                .WithMessage("Informe a hora inicial no formato HH:mm.");

            horario.RuleFor(item => item.HoraFim)
                .Must(RegrasDoPonto.SerHoraValida)
                .WithMessage("Informe a hora final no formato HH:mm.");

            horario.RuleFor(item => item)
                .Must(RegrasDoPonto.SerIntervaloValido)
                .WithMessage("O horário deve terminar no mesmo dia, com fim maior que o início.");
        });

        RuleFor(entrada => entrada.TiposResiduoIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Informe ao menos um tipo aceito.")
            .Must(lista => lista.Count > 0)
            .WithMessage("Informe ao menos um tipo aceito.")
            .Must(lista => lista.Count <= LimitesDosPontos.TamanhoMaximoDeTipos)
            .WithMessage($"Informe no máximo {LimitesDosPontos.TamanhoMaximoDeTipos} tipos.")
            .Must(lista => lista.All(tipo => tipo != Guid.Empty))
            .WithMessage("Tipo de resíduo inválido.")
            .Must(lista => lista.Distinct().Count() == lista.Count)
            .WithMessage("Não repita tipos aceitos.");
    }
}

public sealed class AlterarStatusPontoValidator : AbstractValidator<AlterarStatusPontoEntrada>
{
    public AlterarStatusPontoValidator()
    {
        RuleFor(entrada => entrada.Status)
            .Must(RegrasDoPonto.SerStatusValido)
            .WithMessage("Informe um status válido: Ativo, Inativo ou IndisponivelTemporariamente.");
    }
}
