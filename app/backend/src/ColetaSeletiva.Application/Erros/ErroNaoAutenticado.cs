using FluentResults;

namespace ColetaSeletiva.Application.Erros;

public sealed class ErroNaoAutenticado : Error
{
    public ErroNaoAutenticado(string mensagem)
        : base(mensagem)
    {
        Metadata["codigo"] = CodigosDeErro.NaoAutenticado;
    }
}
