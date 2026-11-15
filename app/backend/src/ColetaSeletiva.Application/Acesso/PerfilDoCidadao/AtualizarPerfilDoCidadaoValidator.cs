using ColetaSeletiva.Domain.Identidade;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.PerfilDoCidadao;

public sealed class AtualizarPerfilDoCidadaoValidator
    : AbstractValidator<AtualizarPerfilDoCidadaoEntrada>
{
    public AtualizarPerfilDoCidadaoValidator()
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
            .MaximumLength(LimitesDeConta.EmailMaxLength)
            .WithMessage($"O e-mail deve ter no máximo {LimitesDeConta.EmailMaxLength} caracteres.")
            .EmailAddress()
            .WithMessage("Informe um e-mail válido.");

        RuleFor(entrada => entrada.Telefone)
            .MaximumLength(LimitesDeConta.TelefoneMaxLength)
            .WithMessage($"O telefone deve ter no máximo {LimitesDeConta.TelefoneMaxLength} caracteres.");
    }
}
