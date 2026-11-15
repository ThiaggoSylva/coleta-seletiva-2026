using ColetaSeletiva.Domain.Identidade;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.Equipe;

public sealed class EditarOperadorValidator : AbstractValidator<EditarOperadorEntrada>
{
    public const int NomeMaxLength = LimitesDeConta.NomeMaxLength;
    public const int UsuarioMaxLength = LimitesDeConta.UsuarioMaxLength;
    public const int EmailMaxLength = LimitesDeConta.EmailMaxLength;

    public EditarOperadorValidator()
    {
        RuleFor(entrada => entrada.Nome)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o nome.")
            .MaximumLength(NomeMaxLength)
            .WithMessage($"O nome deve ter no máximo {NomeMaxLength} caracteres.");

        RuleFor(entrada => entrada.Usuario)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o usuário.")
            .MaximumLength(UsuarioMaxLength)
            .WithMessage($"O usuário deve ter no máximo {UsuarioMaxLength} caracteres.");

        RuleFor(entrada => entrada.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o e-mail.")
            .MaximumLength(EmailMaxLength)
            .WithMessage($"O e-mail deve ter no máximo {EmailMaxLength} caracteres.")
            .EmailAddress()
            .WithMessage("Informe um e-mail válido.");
    }
}
