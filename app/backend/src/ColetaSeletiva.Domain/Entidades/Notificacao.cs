namespace ColetaSeletiva.Domain.Entidades;

/// <summary>
/// Evento que gera uma notificação interna (RF-20).
/// </summary>
public enum TipoDeNotificacao
{
    SolicitacaoCriada,
    RetiradaAgendada,
    RetiradaReagendada,
    RetiradaRecusada,
    RetiradaColetada,
    RetiradaNaoRealizada,
    RetiradaCancelada,
}

/// <summary>
/// Notificação interna destinada a um único usuário (RF-20; DT-14).
/// </summary>
public sealed class Notificacao
{
    public const int TituloMaxLength = 160;
    public const int MensagemMaxLength = 500;

    private Notificacao()
    {
    }

    private Notificacao(
        Guid id,
        Guid usuarioId,
        Guid? organizacaoId,
        TipoDeNotificacao tipo,
        Guid referenciaId,
        string titulo,
        string mensagem,
        DateTimeOffset criadaEmUtc)
    {
        Id = id;
        UsuarioId = usuarioId;
        OrganizacaoId = organizacaoId;
        Tipo = tipo;
        ReferenciaId = referenciaId;
        Titulo = titulo;
        Mensagem = mensagem;
        CriadaEmUtc = criadaEmUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public Guid? OrganizacaoId { get; private set; }

    public TipoDeNotificacao Tipo { get; private set; }

    public Guid ReferenciaId { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    public string Mensagem { get; private set; } = string.Empty;

    public bool Lida { get; private set; }

    public DateTimeOffset? LidaEmUtc { get; private set; }

    public DateTimeOffset CriadaEmUtc { get; private set; }

    public static Notificacao Criar(
        Guid usuarioId,
        Guid? organizacaoId,
        TipoDeNotificacao tipo,
        Guid referenciaId,
        string titulo,
        string mensagem,
        DateTimeOffset criadaEmUtc)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("O destinatário é obrigatório.", nameof(usuarioId));
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de notificação inválido.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        ArgumentException.ThrowIfNullOrWhiteSpace(mensagem);

        var tituloNormalizado = titulo.Trim();
        var mensagemNormalizada = mensagem.Trim();

        if (tituloNormalizado.Length > TituloMaxLength)
        {
            throw new ArgumentException(
                $"O título deve ter no máximo {TituloMaxLength} caracteres.",
                nameof(titulo));
        }

        if (mensagemNormalizada.Length > MensagemMaxLength)
        {
            throw new ArgumentException(
                $"A mensagem deve ter no máximo {MensagemMaxLength} caracteres.",
                nameof(mensagem));
        }

        return new Notificacao(
            Guid.NewGuid(),
            usuarioId,
            organizacaoId,
            tipo,
            referenciaId,
            tituloNormalizado,
            mensagemNormalizada,
            criadaEmUtc);
    }

    public void MarcarLida(DateTimeOffset agora)
    {
        if (Lida)
        {
            return;
        }

        Lida = true;
        LidaEmUtc = agora.ToUniversalTime();
    }
}
