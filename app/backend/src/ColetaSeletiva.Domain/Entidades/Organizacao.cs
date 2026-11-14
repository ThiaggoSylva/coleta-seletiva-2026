namespace ColetaSeletiva.Domain.Entidades;

public sealed class Organizacao
{
    public const int NomeMaxLength = 150;

    private Organizacao()
    {
    }

    private Organizacao(Guid id, string nome, TipoOrganizacao tipo, DateTimeOffset criadoEmUtc)
    {
        Id = id;
        Nome = nome;
        Tipo = tipo;
        Publicada = false;
        CriadoEmUtc = criadoEmUtc.ToUniversalTime();
        AtualizadoEmUtc = CriadoEmUtc;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public TipoOrganizacao Tipo { get; private set; }

    public bool Publicada { get; private set; }

    public DateTimeOffset CriadoEmUtc { get; private set; }

    public DateTimeOffset AtualizadoEmUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Organizacao Criar(string nome, TipoOrganizacao tipo, DateTimeOffset criadoEmUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);

        var nomeNormalizado = nome.Trim();
        if (nomeNormalizado.Length > NomeMaxLength)
        {
            throw new ArgumentException(
                $"O nome da organização deve ter no máximo {NomeMaxLength} caracteres.",
                nameof(nome));
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de organização inválido.");
        }

        return new Organizacao(Guid.NewGuid(), nomeNormalizado, tipo, criadoEmUtc);
    }
}
