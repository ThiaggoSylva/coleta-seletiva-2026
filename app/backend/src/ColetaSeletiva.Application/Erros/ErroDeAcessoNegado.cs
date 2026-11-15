using FluentResults;

namespace ColetaSeletiva.Application.Erros;

public sealed class ErroDeAcessoNegado : Error
{
    public ErroDeAcessoNegado(string mensagem)
        : base(mensagem)
    {
        Metadata["codigo"] = CodigosDeErro.AcessoNegado;
    }
}
