using ColetaSeletiva.Application.Acesso.Equipe;
using ColetaSeletiva.Application.Acesso.PerfilDoCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeCidadao;
using ColetaSeletiva.Application.Acesso.RegistroDeOrganizacao;
using ColetaSeletiva.Application.Acesso.Sessao;
using ColetaSeletiva.Application.Catalogo;
using ColetaSeletiva.Application.ConsultaPublica;
using ColetaSeletiva.Application.PerfilDaOrganizacao;
using ColetaSeletiva.Application.Pontos;
using ColetaSeletiva.Application.Retiradas;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ColetaSeletiva.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
        services.AddScoped<RegistrarOrganizacaoHandler>();
        services.AddScoped<RegistrarCidadaoHandler>();
        services.AddScoped<ObterPerfilDoCidadaoHandler>();
        services.AddScoped<AtualizarPerfilDoCidadaoHandler>();
        services.AddScoped<IniciarSessaoHandler>();
        services.AddScoped<RenovarSessaoHandler>();
        services.AddScoped<EncerrarSessaoHandler>();
        services.AddScoped<AlterarSenhaHandler>();
        services.AddScoped<ObterContaAtualHandler>();
        services.AddScoped<CriarOperadorHandler>();
        services.AddScoped<ObterOperadorHandler>();
        services.AddScoped<ListarOperadoresHandler>();
        services.AddScoped<EditarOperadorHandler>();
        services.AddScoped<InativarOperadorHandler>();
        services.AddScoped<ConsultarCatalogoDeTiposHandler>();
        services.AddScoped<ConsultarMunicipiosHandler>();
        services.AddScoped<ObterPerfilDaOrganizacaoHandler>();
        services.AddScoped<AtualizarPerfilDaOrganizacaoHandler>();
        services.AddScoped<DefinirMunicipiosHandler>();
        services.AddScoped<DefinirPeriodosHandler>();
        services.AddScoped<DefinirTiposHabilitadosHandler>();
        services.AddScoped<DefinirPublicacaoHandler>();
        services.AddScoped<CriarPontoHandler>();
        services.AddScoped<AtualizarPontoHandler>();
        services.AddScoped<AlterarStatusPontoHandler>();
        services.AddScoped<RetirarPontoHandler>();
        services.AddScoped<ListarPontosHandler>();
        services.AddScoped<ObterPontoHandler>();
        services.AddScoped<ListarOpcoesDePontosHandler>();
        services.AddScoped<BuscarPontosPublicosHandler>();
        services.AddScoped<ListarOrganizacoesElegiveisHandler>();
        services.AddScoped<ListarJanelasDeRetiradaHandler>();
        services.AddScoped<CriarRetiradaHandler>();
        services.AddScoped<ListarMinhasRetiradasHandler>();
        services.AddScoped<ObterMinhaRetiradaHandler>();
        services.AddScoped<CancelarRetiradaHandler>();
        services.AddScoped<ListarRetiradasHandler>();
        services.AddScoped<ObterRetiradaHandler>();
        services.AddScoped<AgendarRetiradaHandler>();
        services.AddScoped<ReagendarRetiradaHandler>();
        services.AddScoped<RecusarRetiradaHandler>();
        services.AddScoped<ExecutarRetiradaHandler>();
        services.AddScoped<MarcarRetiradaNaoRealizadaHandler>();
        services.AddScoped<ObterDisponibilidadeHandler>();
        services.AddScoped<DefinirDisponibilidadeHandler>();
        services.AddScoped<ListarBloqueiosHandler>();
        services.AddScoped<CriarBloqueioHandler>();
        services.AddScoped<RemoverBloqueioHandler>();
        services.AddScoped<ListarVolumesHandler>();
        services.AddScoped<RegistrarRecebimentoHandler>();
        services.AddScoped<AjustarVolumeHandler>();
        services.AddScoped<ObterResumoDoPainelHandler>();
        services.AddScoped<ObterSerieDoPainelHandler>();
        services.AddScoped<ExportarVolumesCsvHandler>();
        services.AddScoped<ListarNotificacoesHandler>();
        services.AddScoped<MarcarNotificacaoLidaHandler>();
        services.AddScoped<MarcarTodasNotificacoesLidasHandler>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        return services;
    }
}
