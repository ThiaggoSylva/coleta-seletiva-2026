using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Seguranca;
using ColetaSeletiva.Domain.Identidade;
using FluentResults;
using Moq;

namespace ColetaSeletiva.Application.Tests.Acesso;

[TestClass]
public sealed class ObterContaAtualHandlerTests
{
    [TestMethod]
    public async Task Sessao_autenticada_e_ativa_retorna_a_conta()
    {
        var cenario = new Cenario();

        var resultado = await cenario.CriarHandler().ExecutarAsync();

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(cenario.Conta, resultado.Value);
        cenario.Autenticacao.Verify(
            servico => servico.ObterPorUsuarioIdAsync(
                cenario.UsuarioId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Sessao_sem_usuario_retorna_nao_autenticado()
    {
        var cenario = new Cenario(autenticado: false);

        var resultado = await cenario.CriarHandler().ExecutarAsync();

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());
        cenario.Autenticacao.Verify(
            servico => servico.ObterPorUsuarioIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Conta_inativa_retorna_nao_autenticado()
    {
        var cenario = new Cenario(conta: NovaConta() with { Ativa = false });

        var resultado = await cenario.CriarHandler().ExecutarAsync();

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());
    }

    [TestMethod]
    public async Task Falha_ao_carregar_a_conta_retorna_nao_autenticado()
    {
        var cenario = new Cenario(
            autenticacao: Result.Fail<ContaAutenticada>(
                new ErroNaoAutenticado(MensagensDeSessao.SessaoInvalida)));

        var resultado = await cenario.CriarHandler().ExecutarAsync();

        Assert.IsTrue(resultado.IsFailed);
        Assert.IsInstanceOfType<ErroNaoAutenticado>(resultado.Errors.Single());
    }

    private static ContaAutenticada NovaConta()
    {
        return new ContaAutenticada(
            Id: Guid.NewGuid(),
            Nome: "Operador de Teste",
            Email: "operador.teste@example.org",
            Papel: Papeis.Operador,
            OrganizacaoId: Guid.NewGuid(),
            TrocaSenhaPendente: true,
            Ativa: true);
    }

    private sealed class Cenario
    {
        public Cenario(
            bool autenticado = true,
            ContaAutenticada? conta = null,
            Result<ContaAutenticada>? autenticacao = null)
        {
            UsuarioId = Guid.NewGuid();
            Conta = conta ?? NovaConta();

            TenantContext
                .SetupGet(contexto => contexto.Autenticado)
                .Returns(autenticado);
            TenantContext
                .SetupGet(contexto => contexto.UsuarioId)
                .Returns(autenticado ? UsuarioId : null);

            Autenticacao
                .Setup(servico => servico.ObterPorUsuarioIdAsync(
                    UsuarioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(autenticacao ?? Result.Ok(Conta));
        }

        public Guid UsuarioId { get; }

        public ContaAutenticada Conta { get; }

        public Mock<ITenantContext> TenantContext { get; } = new();

        public Mock<IAutenticacaoDeContas> Autenticacao { get; } = new();

        public ObterContaAtualHandler CriarHandler()
        {
            return new ObterContaAtualHandler(TenantContext.Object, Autenticacao.Object);
        }
    }
}
