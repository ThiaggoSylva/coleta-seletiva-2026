using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.Sessao;

public sealed class AlterarSenhaValidator : AbstractValidator<AlterarSenhaEntrada>
{
    public const int SenhaMaxLength = 128;

    public AlterarSenhaValidator()
    {
        RuleFor(entrada => entrada.SenhaAtual)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe a senha atual.")
            .MaximumLength(SenhaMaxLength)
            .WithMessage($"A senha atual deve ter no máximo {SenhaMaxLength} caracteres.");

        RuleFor(entrada => entrada.NovaSenha)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe a nova senha.")
            .MaximumLength(SenhaMaxLength)
            .WithMessage($"A nova senha deve ter no máximo {SenhaMaxLength} caracteres.");
    }
}
