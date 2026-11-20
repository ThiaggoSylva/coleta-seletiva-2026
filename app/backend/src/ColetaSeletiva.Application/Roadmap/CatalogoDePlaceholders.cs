namespace ColetaSeletiva.Application.Roadmap;

/// <summary>
/// Item do catálogo estático de módulos anunciados como "em breve".
/// Não contém dados pessoais nem identificadores de organização (RNF-10).
/// </summary>
public sealed record ModuloEmBreve(
    string Id,
    string Titulo,
    string Descricao,
    string Fase,
    string Rota,
    string Situacao);

public interface ICatalogoDePlaceholders
{
    IReadOnlyList<ModuloEmBreve> Listar();

    ModuloEmBreve? ObterPorId(string id);
}

/// <summary>
/// Fonte canônica dos placeholders de roadmap (DT-38). A lista é criada uma única vez,
/// em tempo de execução, com textos versionados junto ao código e iguais em todos os ambientes.
/// </summary>
public sealed class CatalogoDePlaceholders : ICatalogoDePlaceholders
{
    public const string SituacaoEmBreve = "em_breve";

    private static readonly IReadOnlyList<ModuloEmBreve> Modulos =
    [
        new(
            "preferencias-email",
            "Preferências de e-mail",
            "Escolha das notificações por e-mail que o cidadão deseja receber sobre suas solicitações de retirada.",
            "F7",
            "/em-breve/preferencias-email",
            SituacaoEmBreve),
        new(
            "extrato-pontos",
            "Extrato de pontos",
            "Consulta do histórico de pontos de reconhecimento obtidos e utilizados pelo cidadão.",
            "F8",
            "/em-breve/extrato-pontos",
            SituacaoEmBreve),
        new(
            "rotas-coletor",
            "Rotas do coletor",
            "Acompanhamento das rotas de coleta e das retiradas programadas pela equipe da organização.",
            "F9",
            "/em-breve/rotas-coletor",
            SituacaoEmBreve),
        new(
            "transparencia-impacto",
            "Transparência e impacto",
            "Divulgação pública dos volumes coletados e da estimativa de impacto ambiental, sem dados pessoais.",
            "F10",
            "/em-breve/transparencia-impacto",
            SituacaoEmBreve),
    ];

    public IReadOnlyList<ModuloEmBreve> Listar() => Modulos;

    public ModuloEmBreve? ObterPorId(string id)
    {
        return Modulos.FirstOrDefault(modulo => string.Equals(modulo.Id, id, StringComparison.Ordinal));
    }
}
