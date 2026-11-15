using ColetaSeletiva.Domain.Identidade;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.Sessao;

public sealed class LoginValidator : AbstractValidator<LoginEntrada>
{
    public const int SenhaMaxLength = 128;

    public LoginValidator()
    {
        RuleFor(entrada => entrada.Identificador)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o usuário ou e-mail.")
            .MaximumLength(LimitesDeConta.UsuarioMaxLength)
            .WithMessage(
                $"O usuário ou e-mail deve ter no máximo {LimitesDeConta.UsuarioMaxLength} caracteres.");

        RuleFor(entrada => entrada.Senha)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe a senha.")
            .MaximumLength(SenhaMaxLength)
            .WithMessage($"A senha deve ter no máximo {SenhaMaxLength} caracteres.");
    }
}
