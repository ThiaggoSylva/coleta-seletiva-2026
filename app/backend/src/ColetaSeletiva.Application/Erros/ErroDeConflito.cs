using FluentResults;

namespace ColetaSeletiva.Application.Erros;

public sealed class ErroDeConflito : Error
{
    public ErroDeConflito(string codigo, string mensagem)
        : base(mensagem)
    {
        Codigo = codigo;
        Metadata["codigo"] = codigo;
    }

    public string Codigo { get; }
}
