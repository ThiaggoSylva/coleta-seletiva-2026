using FluentResults;

namespace ColetaSeletiva.Application.Erros;

public sealed class ErroDeValidacao : Error
{
    public ErroDeValidacao(string campo, string mensagem)
        : base(mensagem)
    {
        Campo = campo;
        Metadata["codigo"] = CodigosDeErro.Validacao;
        Metadata["campo"] = campo;
    }

    public string Campo { get; }
}
