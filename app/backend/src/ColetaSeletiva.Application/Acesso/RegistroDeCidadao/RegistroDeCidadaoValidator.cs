using ColetaSeletiva.Domain.Identidade;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.RegistroDeCidadao;

public sealed class RegistroDeCidadaoValidator : AbstractValidator<RegistroDeCidadaoEntrada>
{
    public const int EmailMaxLength = LimitesDeConta.EmailMaxLength;

    public RegistroDeCidadaoValidator()
    {
        RuleFor(entrada => entrada.Nome)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o nome.")
            .MaximumLength(LimitesDeConta.NomeMaxLength)
            .WithMessage($"O nome deve ter no máximo {LimitesDeConta.NomeMaxLength} caracteres.");

        RuleFor(entrada => entrada.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o e-mail.")
            .MaximumLength(EmailMaxLength)
            .WithMessage($"O e-mail deve ter no máximo {EmailMaxLength} caracteres.")
            .EmailAddress()
            .WithMessage("Informe um e-mail válido.");

        RuleFor(entrada => entrada.Telefone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o telefone.")
            .MaximumLength(LimitesDeConta.TelefoneMaxLength)
            .WithMessage($"O telefone deve ter no máximo {LimitesDeConta.TelefoneMaxLength} caracteres.");

        RuleFor(entrada => entrada.Senha)
            .NotEmpty()
            .WithMessage("Informe a senha.");
    }
}
