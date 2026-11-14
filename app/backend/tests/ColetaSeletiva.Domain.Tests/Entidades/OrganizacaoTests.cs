using ColetaSeletiva.Domain.Entidades;

namespace ColetaSeletiva.Domain.Tests.Entidades;

[TestClass]
public sealed class OrganizacaoTests
{
    [TestMethod]
    public void Criar_normaliza_nome_e_mantem_organizacao_nao_publicada()
    {
        var agora = new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.FromHours(-3));

        var organizacao = Organizacao.Criar(
            "  Cooperativa Verde  ",
            TipoOrganizacao.Cooperativa,
            agora);

        Assert.AreNotEqual(Guid.Empty, organizacao.Id);
        Assert.AreEqual("Cooperativa Verde", organizacao.Nome);
        Assert.AreEqual(TipoOrganizacao.Cooperativa, organizacao.Tipo);
        Assert.IsFalse(organizacao.Publicada);
        Assert.AreEqual(TimeSpan.Zero, organizacao.CriadoEmUtc.Offset);
        Assert.AreEqual(organizacao.CriadoEmUtc, organizacao.AtualizadoEmUtc);
    }

    [TestMethod]
    public void Criar_rejeita_nome_acima_do_limite()
    {
        var nome = new string('a', Organizacao.NomeMaxLength + 1);

        Assert.ThrowsExactly<ArgumentException>(
            () => Organizacao.Criar(nome, TipoOrganizacao.Outro, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void Criar_rejeita_tipo_invalido()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => Organizacao.Criar("Cooperativa Verde", (TipoOrganizacao)99, DateTimeOffset.UtcNow));
    }
}
