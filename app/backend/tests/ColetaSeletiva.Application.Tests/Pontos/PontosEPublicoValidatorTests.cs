using ColetaSeletiva.Application.ConsultaPublica;
using ColetaSeletiva.Application.Erros;
using ColetaSeletiva.Application.Pontos;
using Moq;

namespace ColetaSeletiva.Application.Tests.Pontos;

[TestClass]
public sealed class PontosEPublicoValidatorTests
{
    private static readonly Guid TipoPapel = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [TestMethod]
    public async Task Ponto_com_horario_que_atravessa_a_meia_noite_e_rejeitado()
    {
        var validador = new CriarPontoValidator();
        var entrada = EntradaValida() with
        {
            Horarios =
            [
                new HorarioDoPontoEntrada("Monday", "22:00", "02:00"),
            ],
        };

        var resultado = await validador.ValidateAsync(entrada);

        Assert.IsFalse(resultado.IsValid);
        Assert.IsTrue(resultado.Errors.Any(erro => erro.ErrorMessage.Contains("mesmo dia", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Ponto_sem_cep_valido_ou_sem_tipo_e_rejeitado()
    {
        var validador = new CriarPontoValidator();
        var entrada = EntradaValida() with { Cep = "123", TiposResiduoIds = [] };

        var resultado = await validador.ValidateAsync(entrada);

        Assert.IsFalse(resultado.IsValid);
        var campos = resultado.Errors.Select(erro => erro.PropertyName).ToList();
        CollectionAssert.Contains(campos, nameof(CriarPontoEntrada.Cep));
        CollectionAssert.Contains(campos, nameof(CriarPontoEntrada.TiposResiduoIds));
    }

    [TestMethod]
    public async Task Busca_publica_exige_latitude_e_longitude_juntas()
    {
        var consulta = new Mock<IConsultaPublicaDePontos>();
        var handler = new BuscarPontosPublicosHandler(consulta.Object);

        var resultado = await handler.ExecutarAsync(
            new BuscaPublicaDePontosEntrada(Latitude: -23.5));

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            "latitude",
            resultado.Errors.OfType<ErroDeValidacao>().Single().Campo);
        consulta.Verify(
            item => item.BuscarAsync(
                It.IsAny<BuscaPublicaDePontosEntrada>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Busca_publica_aplica_raio_padrao_e_limita_tamanho_da_pagina()
    {
        BuscaPublicaDePontosEntrada? recebida = null;
        var consulta = new Mock<IConsultaPublicaDePontos>();
        consulta
            .Setup(item => item.BuscarAsync(
                It.IsAny<BuscaPublicaDePontosEntrada>(),
                It.IsAny<CancellationToken>()))
            .Callback<BuscaPublicaDePontosEntrada, CancellationToken>(
                (entrada, _) => recebida = entrada)
            .ReturnsAsync(new PaginaDePontosPublicos([], 1, 20, 0));

        var handler = new BuscarPontosPublicosHandler(consulta.Object);

        var semCoordenadas = await handler.ExecutarAsync(
            new BuscaPublicaDePontosEntrada(TamanhoPagina: 500));

        Assert.IsTrue(semCoordenadas.IsFailed);
        Assert.AreEqual(
            "tamanhoPagina",
            semCoordenadas.Errors.OfType<ErroDeValidacao>().Single().Campo);

        var comCoordenadas = await handler.ExecutarAsync(
            new BuscaPublicaDePontosEntrada(Latitude: -23.5, Longitude: -46.6));

        Assert.IsTrue(comCoordenadas.IsSuccess);
        Assert.IsNotNull(recebida);
        Assert.AreEqual(BuscarPontosPublicosHandler.RaioPadraoKm, recebida.RaioKm);
    }

    private static CriarPontoEntrada EntradaValida()
    {
        return new CriarPontoEntrada(
            Nome: "Ponto Centro",
            MunicipioId: 3550308,
            Bairro: "Bela Vista",
            Rua: "Avenida Paulista",
            Numero: "1000",
            Complemento: null,
            Cep: "01310-100",
            Latitude: -23.5613,
            Longitude: -46.6558,
            Observacoes: null,
            Status: "Ativo",
            Horarios:
            [
                new HorarioDoPontoEntrada("Monday", "08:00", "17:00"),
            ],
            TiposResiduoIds: [TipoPapel]);
    }
}
