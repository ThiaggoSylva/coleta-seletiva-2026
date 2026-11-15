namespace ColetaSeletiva.Domain.Entidades;

public sealed class Organizacao
{
    public const int NomeMaxLength = 150;
    public const int FusoMaxLength = 60;
    public const int ContatoEmailMaxLength = 254;
    public const int ContatoTelefoneMaxLength = 30;
    public const int LimiteSolicitacoesAbertasPadrao = 3;
    public const int LimiteSolicitacoesAbertasMaximo = 50;
    public const string FusoPadrao = "America/Sao_Paulo";

    private Organizacao()
    {
    }

    private Organizacao(Guid id, string nome, TipoOrganizacao tipo, DateTimeOffset criadoEmUtc)
    {
        Id = id;
        Nome = nome;
        Tipo = tipo;
        Publicada = false;
        Fuso = FusoPadrao;
        LimiteSolicitacoesAbertas = LimiteSolicitacoesAbertasPadrao;
        CriadoEmUtc = criadoEmUtc.ToUniversalTime();
        AtualizadoEmUtc = CriadoEmUtc;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public TipoOrganizacao Tipo { get; private set; }

    public string? ContatoEmail { get; private set; }

    public string? ContatoTelefone { get; private set; }

    /// <summary>Fuso IANA da organização, usado para datas e horários locais (DT-09).</summary>
    public string Fuso { get; private set; } = FusoPadrao;

    public int LimiteSolicitacoesAbertas { get; private set; } = LimiteSolicitacoesAbertasPadrao;

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

    public void AtualizarPerfil(
        string nome,
        TipoOrganizacao tipo,
        string? contatoEmail,
        string? contatoTelefone,
        string fuso,
        int limiteSolicitacoesAbertas,
        DateTimeOffset agora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(fuso);

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

        var fusoNormalizado = fuso.Trim();
        if (fusoNormalizado.Length > FusoMaxLength)
        {
            throw new ArgumentException(
                $"O fuso deve ter no máximo {FusoMaxLength} caracteres.",
                nameof(fuso));
        }

        if (limiteSolicitacoesAbertas is < 1 or > LimiteSolicitacoesAbertasMaximo)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limiteSolicitacoesAbertas),
                limiteSolicitacoesAbertas,
                $"O limite de solicitações abertas deve estar entre 1 e {LimiteSolicitacoesAbertasMaximo}.");
        }

        var emailNormalizado = NormalizarOpcional(contatoEmail, ContatoEmailMaxLength, nameof(contatoEmail));
        var telefoneNormalizado = NormalizarOpcional(
            contatoTelefone,
            ContatoTelefoneMaxLength,
            nameof(contatoTelefone));

        Nome = nomeNormalizado;
        Tipo = tipo;
        ContatoEmail = emailNormalizado;
        ContatoTelefone = telefoneNormalizado;
        Fuso = fusoNormalizado;
        LimiteSolicitacoesAbertas = limiteSolicitacoesAbertas;
        AtualizadoEmUtc = agora.ToUniversalTime();
    }

    public void DefinirPublicacao(bool publicada, DateTimeOffset agora)
    {
        Publicada = publicada;
        AtualizadoEmUtc = agora.ToUniversalTime();
    }

    private static string? NormalizarOpcional(string? valor, int tamanhoMaximo, string parametro)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim();
        if (normalizado.Length > tamanhoMaximo)
        {
            throw new ArgumentException(
                $"O valor deve ter no máximo {tamanhoMaximo} caracteres.",
                parametro);
        }

        return normalizado;
    }
}
