using FluentResults;

namespace ColetaSeletiva.Application.Erros;

public sealed class ErroNaoEncontrado : Error
{
    public ErroNaoEncontrado(string mensagem)
        : base(mensagem)
    {
        Metadata["codigo"] = CodigosDeErro.NaoEncontrado;
    }
}
