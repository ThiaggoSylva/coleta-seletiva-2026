namespace ColetaSeletiva.Application.ConsultaPublica;

/// <summary>Origem de um ponto exibido na consulta pública.</summary>
public enum OrigemDoPonto
{
    /// <summary>Ponto cadastrado por uma organização na plataforma.</summary>
    Organizacao = 1,

    /// <summary>Ponto lido de uma fonte pública externa (OpenStreetMap).</summary>
    OpenStreetMap = 2,
}

/// <summary>Ponto de coleta lido de uma fonte pública externa, sem vínculo com organização.</summary>
public sealed record PontoDeFontePublica(
    string Identificador,
    string Nome,
    string? Operador,
    double Latitude,
    double Longitude,
    string? Rua,
    string? Bairro,
    string? HorarioLivre,
    IReadOnlyList<string> CodigosDeTipos,
    string UrlDoRegistro,
    string Atribuicao);

/// <summary>Escopo geográfico e filtros aplicados à fonte pública externa.</summary>
public sealed record ConsultaDeFontePublica(
    string? MunicipioNome = null,
    string? Uf = null,
    double? Latitude = null,
    double? Longitude = null,
    double? RaioKm = null,
    IReadOnlyList<string>? CodigosDeTipos = null,
    int MaximoDePontos = 100);

/// <summary>
/// Resultado da consulta externa. <see cref="Consultada"/> indica se a fonte foi
/// acionada; <see cref="Disponivel"/> indica se ela respondeu com sucesso.
/// </summary>
public sealed record ResultadoDaFontePublica(
    IReadOnlyList<PontoDeFontePublica> Pontos,
    bool Consultada,
    bool Disponivel,
    string? Aviso);

/// <summary>
/// Porta de leitura de pontos de coleta em fontes públicas externas.
/// A implementação MUST NOT persistir dados pessoais nem bloquear a consulta
/// pública quando a fonte estiver indisponível.
/// </summary>
public interface IFontePublicaDePontos
{
    Task<ResultadoDaFontePublica> BuscarAsync(
        ConsultaDeFontePublica consulta,
        CancellationToken cancellationToken = default);
}

/// <summary>Fonte desativada por configuração: responde vazio sem consultar a rede.</summary>
public sealed class FontePublicaDesativada : IFontePublicaDePontos
{
    public Task<ResultadoDaFontePublica> BuscarAsync(
        ConsultaDeFontePublica consulta,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        return Task.FromResult(new ResultadoDaFontePublica(
            Pontos: [],
            Consultada: false,
            Disponivel: false,
            Aviso: null));
    }
}
