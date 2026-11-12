using FluentResults;

namespace ColetaSeletiva.Application.Erros;

public sealed class ErroInterno : Error
{
    public ErroInterno(string mensagem)
        : base(mensagem)
    {
        Metadata["codigo"] = CodigosDeErro.FalhaDeCadastro;
    }
}
