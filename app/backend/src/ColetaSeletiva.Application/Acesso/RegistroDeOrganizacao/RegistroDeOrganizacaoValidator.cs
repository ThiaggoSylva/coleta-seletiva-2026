using ColetaSeletiva.Domain.Entidades;
using FluentValidation;

namespace ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;

public sealed class RegistroDeOrganizacaoValidator : AbstractValidator<RegistroDeOrganizacaoEntrada>
{
    public const int UsuarioMaxLength = 256;
    public const int EmailMaxLength = 256;

    public RegistroDeOrganizacaoValidator()
    {
        RuleFor(entrada => entrada.NomeOrganizacao)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Informe o nome da organização.")
            .MaximumLength(Organizacao.NomeMaxLength)
            .WithMessage($"O nome da organização deve ter no máximo {Organizacao.NomeMaxLength} caracteres.");

        RuleFor(entrada => entrada.Tipo)
            .IsInEnum()
            .WithMessage("Informe um tipo de organização válido.");

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

        RuleFor(entrada => entrada.Senha)
            .NotEmpty()
            .WithMessage("Informe a senha.");
    }
}
