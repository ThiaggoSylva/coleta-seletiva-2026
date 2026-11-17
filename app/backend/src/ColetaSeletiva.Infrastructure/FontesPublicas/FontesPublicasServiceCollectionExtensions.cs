using ColetaSeletiva.Application.ConsultaPublica;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ColetaSeletiva.Infrastructure.FontesPublicas;

public static class FontesPublicasServiceCollectionExtensions
{
    /// <summary>
    /// Registra a leitura de pontos em fontes públicas externas. Um único
    /// <see cref="HttpClient"/> é compartilhado, com timeout curto, e o adaptador
    /// mantém cache em memória próprio.
    /// </summary>
    public static IServiceCollection AddFontesPublicasDePontos(
        this IServiceCollection services,
        OpcoesDaFontePublica opcoes)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(opcoes);

        services.AddSingleton(opcoes);

        if (!opcoes.Ativa)
        {
            services.AddSingleton<IFontePublicaDePontos, FontePublicaDesativada>();
            return services;
        }

        var cliente = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip
                | System.Net.DecompressionMethods.Deflate,
        })
        {
            Timeout = opcoes.Timeout,
        };

        cliente.DefaultRequestHeaders.UserAgent.ParseAdd(
            "coleta-seletiva/1.0 (+https://github.com/ThiaggoSylva/coleta-seletiva-2026)");
        cliente.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        services.AddSingleton<IFontePublicaDePontos>(provedor =>
            new OverpassFontePublicaDePontos(
                cliente,
                provedor.GetRequiredService<ILogger<OverpassFontePublicaDePontos>>(),
                opcoes));

        return services;
    }
}
