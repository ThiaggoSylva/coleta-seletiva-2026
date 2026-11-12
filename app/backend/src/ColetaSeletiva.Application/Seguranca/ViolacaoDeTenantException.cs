namespace ColetaSeletiva.Application.Seguranca;

public sealed class ViolacaoDeTenantException : InvalidOperationException
{
    public ViolacaoDeTenantException(string message)
        : base(message)
    {
    }
}
