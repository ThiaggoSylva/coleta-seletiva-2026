namespace ColetaSeletiva.Domain.Identidade;

public static class Papeis
{
    public const string Administrador = "Administrador";
    public const string Operador = "Operador";
    public const string Cidadao = "Cidadao";

    public static IReadOnlyList<string> Todos { get; } =
        [Administrador, Operador, Cidadao];
}
