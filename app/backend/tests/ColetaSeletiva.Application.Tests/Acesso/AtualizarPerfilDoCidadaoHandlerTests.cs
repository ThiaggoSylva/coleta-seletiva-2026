using ColetaSeletiva.Application.Acesso.PerfilDoCidadao;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Seguranca;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.Acesso;

[TestClass]
public sealed class AtualizarPerfilDoCidadaoHandlerTests
{
    private static readonly Guid CidadaoXId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid AdministradorId = Guid.Parse("66666666-7777-8888-9999-000000000000");
    private static readonly Guid OrganizacaoAId = Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");

    [TestMethod]
    public async Task Cidadao_atualiza_somente_o_proprio_perfil_a_partir_do_contexto()
    {
        var perfis = new Mock<IPerfilDeCidadao>();
        perfis
            .Setup(porta => porta.AtualizarAsync(
                It.IsAny<Guid>(),
                It.IsAny<AtualizarPerfilDoCidadaoEntrada>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new PerfilDoCidadao(
                CidadaoXId,
                "Nome novo",
                "novo@example.org",
                "(11) 98888-7777")));

        var handler = CriarHandler(
            new ContextoDeTeste
            {
                Autenticado = true,
                EscopoValido = true,
                EhCidadao = true,
                UsuarioId = CidadaoXId,
            },
            perfis);

        var resultado = await handler.ExecutarAsync(new AtualizarPerfilDoCidadaoEntrada(
            Nome: "Nome novo",
            Email: "novo@example.org",
            Telefone: "(11) 98888-7777"));

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(CidadaoXId, resultado.Value.Id);

        perfis.Verify(porta => porta.AtualizarAsync(
            CidadaoXId,
            It.Is<AtualizarPerfilDoCidadaoEntrada>(entrada =>
                entrada.Nome == "Nome novo"
                && entrada.Email == "novo@example.org"
                && entrada.Telefone == "(11) 98888-7777"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Sem_autenticacao_retorna_nao_autenticado_sem_consultar_perfil()
    {
        var perfis = new Mock<IPerfilDeCidadao>();
        var handler = CriarHandler(new ContextoDeTeste(), perfis);

        var resultado = await handler.ExecutarAsync(EntradaValida());

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());
        perfis.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Administrador_nao_acessa_perfil_de_cidadao()
    {
        var perfis = new Mock<IPerfilDeCidadao>();
        var handler = CriarHandler(
            new ContextoDeTeste
            {
                Autenticado = true,
                EscopoValido = true,
                EhCidadao = false,
                UsuarioId = AdministradorId,
                OrganizacaoId = OrganizacaoAId,
            },
            perfis);

        var resultado = await handler.ExecutarAsync(EntradaValida());

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroDeAcessoNegado>(resultado.Errors.Single());
        perfis.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Entrada_invalida_retorna_erros_por_campo_sem_consultar_perfil()
    {
        var perfis = new Mock<IPerfilDeCidadao>();
        var handler = CriarHandler(
            new ContextoDeTeste
            {
                Autenticado = true,
                EscopoValido = true,
                EhCidadao = true,
                UsuarioId = CidadaoXId,
            },
            perfis);

        var resultado = await handler.ExecutarAsync(new AtualizarPerfilDoCidadaoEntrada(
            Nome: string.Empty,
            Email: "email-invalido",
            Telefone: null));

        Assert.IsTrue(resultado.IsFailed);
        var campos = resultado.Errors
            .OfType<ErroDeValidacao>()
            .Select(erro => erro.Campo)
            .ToList();

        CollectionAssert.Contains(campos, nameof(AtualizarPerfilDoCidadaoEntrada.Nome));
        CollectionAssert.Contains(campos, nameof(AtualizarPerfilDoCidadaoEntrada.Email));
        perfis.VerifyNoOtherCalls();
    }

    private static AtualizarPerfilDoCidadaoHandler CriarHandler(
        ITenantContext contexto,
        Mock<IPerfilDeCidadao> perfis)
    {
        return new AtualizarPerfilDoCidadaoHandler(
            contexto,
            new AtualizarPerfilDoCidadaoValidator(),
            perfis.Object);
    }

    private static AtualizarPerfilDoCidadaoEntrada EntradaValida()
    {
        return new AtualizarPerfilDoCidadaoEntrada(
            Nome: "Cidadão de Teste",
            Email: "cidadao.teste@example.org",
            Telefone: "(11) 99999-0000");
    }

    private sealed class ContextoDeTeste : ITenantContext
    {
        public bool Autenticado { get; init; }

        public bool EscopoValido { get; init; }

        public bool EhCidadao { get; init; }

        public Guid? UsuarioId { get; init; }

        public Guid? OrganizacaoId { get; init; }
    }
}
