using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ColetaSeletiva.IntegrationTests.Persistence;

[TestClass]
public sealed class ColetaSeletivaDbContextModelTests
{
    private static readonly IModel Modelo = CriarModelo();

    [TestMethod]
    public void Modelo_contem_entidades_da_f1()
    {
        Assert.IsTrue(typeof(IdentityUser<Guid>).IsAssignableFrom(typeof(Usuario)));
        Assert.IsNotNull(Modelo.FindEntityType(typeof(Organizacao)));
        Assert.IsNotNull(Modelo.FindEntityType(typeof(Usuario)));
        Assert.IsNotNull(Modelo.FindEntityType(typeof(RefreshToken)));
        Assert.IsNotNull(Modelo.FindEntityType(typeof(Auditoria)));
    }

    [TestMethod]
    public void Organizacao_tem_tamanhos_nulabilidade_e_rowversion_do_design()
    {
        var entidade = ObterEntidade<Organizacao>();

        Assert.AreEqual("Organizacao", entidade.GetTableName());

        var nome = ObterPropriedade(entidade, nameof(Organizacao.Nome));
        Assert.AreEqual(typeof(string), nome.ClrType);
        Assert.AreEqual(Organizacao.NomeMaxLength, nome.GetMaxLength());
        Assert.IsFalse(nome.IsNullable);

        var tipo = ObterPropriedade(entidade, nameof(Organizacao.Tipo));
        Assert.AreEqual(typeof(TipoOrganizacao), tipo.ClrType);
        Assert.AreEqual(typeof(string), tipo.GetProviderClrType());
        Assert.AreEqual(20, tipo.GetMaxLength());
        Assert.IsFalse(tipo.IsNullable);

        var publicada = ObterPropriedade(entidade, nameof(Organizacao.Publicada));
        Assert.AreEqual(typeof(bool), publicada.ClrType);
        Assert.IsFalse(publicada.IsNullable);

        AssertarDatasUtc(entidade, nameof(Organizacao.CriadoEmUtc), nameof(Organizacao.AtualizadoEmUtc));
        AssertarRowVersion(entidade, nameof(Organizacao.RowVersion));
    }

    [TestMethod]
    public void Usuario_tem_organizacao_nulavel_rowversion_e_indices_unicos_filtrados()
    {
        var entidade = ObterEntidade<Usuario>();

        var organizacaoId = ObterPropriedade(entidade, nameof(Usuario.OrganizacaoId));
        Assert.AreEqual(typeof(Guid?), organizacaoId.ClrType);
        Assert.IsTrue(organizacaoId.IsNullable);

        var nome = ObterPropriedade(entidade, nameof(Usuario.Nome));
        Assert.AreEqual(Usuario.NomeMaxLength, nome.GetMaxLength());
        Assert.IsFalse(nome.IsNullable);

        var telefone = ObterPropriedade(entidade, nameof(Usuario.Telefone));
        Assert.AreEqual(Usuario.TelefoneMaxLength, telefone.GetMaxLength());
        Assert.IsTrue(telefone.IsNullable);

        var ativo = ObterPropriedade(entidade, nameof(Usuario.Ativo));
        Assert.AreEqual(typeof(bool), ativo.ClrType);
        Assert.IsFalse(ativo.IsNullable);

        var trocaSenhaPendente = ObterPropriedade(entidade, nameof(Usuario.TrocaSenhaPendente));
        Assert.AreEqual(typeof(bool), trocaSenhaPendente.ClrType);
        Assert.IsFalse(trocaSenhaPendente.IsNullable);

        AssertarDatasUtc(entidade, nameof(Usuario.CriadoEmUtc), nameof(Usuario.AtualizadoEmUtc));
        AssertarRowVersion(entidade, nameof(Usuario.RowVersion));

        var indiceUsuario = ObterIndice(entidade, nameof(Usuario.NormalizedUserName));
        Assert.IsTrue(indiceUsuario.IsUnique);
        Assert.AreEqual("[NormalizedUserName] IS NOT NULL", indiceUsuario.GetFilter());

        var indiceEmail = ObterIndice(entidade, nameof(Usuario.NormalizedEmail));
        Assert.IsTrue(indiceEmail.IsUnique);
        Assert.AreEqual("[NormalizedEmail] IS NOT NULL", indiceEmail.GetFilter());

        var indiceOrganizacao = ObterIndice(
            entidade,
            nameof(Usuario.OrganizacaoId),
            nameof(Usuario.Id));
        Assert.IsTrue(indiceOrganizacao.IsUnique);
        Assert.AreEqual("[OrganizacaoId] IS NOT NULL", indiceOrganizacao.GetFilter());

        var chaveEstrangeira = entidade
            .GetForeignKeys()
            .Single(fk => fk.Properties.Any(propriedade => propriedade.Name == nameof(Usuario.OrganizacaoId)));
        Assert.AreEqual(DeleteBehavior.Restrict, chaveEstrangeira.DeleteBehavior);
    }

    [TestMethod]
    public void RefreshToken_guarda_apenas_hash_unico_e_datas_do_design()
    {
        var entidade = ObterEntidade<RefreshToken>();

        Assert.AreEqual("RefreshToken", entidade.GetTableName());

        var hash = ObterPropriedade(entidade, nameof(RefreshToken.Hash));
        Assert.AreEqual(RefreshToken.HashMaxLength, hash.GetMaxLength());
        Assert.IsFalse(hash.IsNullable);
        Assert.IsFalse(hash.IsUnicode());

        var indiceHash = ObterIndice(entidade, nameof(RefreshToken.Hash));
        Assert.IsTrue(indiceHash.IsUnique);

        var indiceFamilia = ObterIndice(
            entidade,
            nameof(RefreshToken.UsuarioId),
            nameof(RefreshToken.FamiliaId));
        Assert.IsFalse(indiceFamilia.IsUnique);

        AssertarDatasUtc(entidade, nameof(RefreshToken.CriadoEmUtc), nameof(RefreshToken.ExpiraEmUtc));

        foreach (var nome in new[] { nameof(RefreshToken.ConsumidoEmUtc), nameof(RefreshToken.RevogadoEmUtc) })
        {
            var propriedade = ObterPropriedade(entidade, nome);
            Assert.AreEqual(typeof(DateTimeOffset?), propriedade.ClrType);
            Assert.IsTrue(propriedade.IsNullable);
        }

        var chaveEstrangeira = entidade.GetForeignKeys().Single();
        Assert.AreEqual(typeof(Usuario), chaveEstrangeira.PrincipalEntityType.ClrType);
        Assert.AreEqual(DeleteBehavior.Restrict, chaveEstrangeira.DeleteBehavior);
    }

    [TestMethod]
    public void Auditoria_tem_campos_identificadores_e_indices_do_design()
    {
        var entidade = ObterEntidade<Auditoria>();

        Assert.AreEqual("Auditoria", entidade.GetTableName());

        foreach (var nome in new[]
                 {
                     nameof(Auditoria.OrganizacaoId),
                     nameof(Auditoria.AtorUsuarioId),
                     nameof(Auditoria.EntidadeId),
                 })
        {
            var propriedade = ObterPropriedade(entidade, nome);
            Assert.AreEqual(typeof(Guid?), propriedade.ClrType);
            Assert.IsTrue(propriedade.IsNullable);
        }

        var acao = ObterPropriedade(entidade, nameof(Auditoria.Acao));
        Assert.AreEqual(Auditoria.AcaoMaxLength, acao.GetMaxLength());
        Assert.IsFalse(acao.IsNullable);

        var nomeEntidade = ObterPropriedade(entidade, nameof(Auditoria.Entidade));
        Assert.AreEqual(Auditoria.EntidadeMaxLength, nomeEntidade.GetMaxLength());
        Assert.IsFalse(nomeEntidade.IsNullable);

        var camposAlterados = ObterPropriedade(entidade, nameof(Auditoria.CamposAlterados));
        Assert.AreEqual(Auditoria.CamposAlteradosMaxLength, camposAlterados.GetMaxLength());
        Assert.IsTrue(camposAlterados.IsNullable);

        var correlacao = ObterPropriedade(entidade, nameof(Auditoria.CorrelacaoId));
        Assert.AreEqual(typeof(Guid), correlacao.ClrType);
        Assert.IsFalse(correlacao.IsNullable);

        AssertarDatasUtc(entidade, nameof(Auditoria.OcorridoEmUtc));

        Assert.IsTrue(ObterIndice(entidade, nameof(Auditoria.OrganizacaoId), nameof(Auditoria.OcorridoEmUtc)).IsUnique == false);
        Assert.IsNotNull(ObterIndice(entidade, nameof(Auditoria.CorrelacaoId)));
        Assert.IsNotNull(ObterIndice(entidade, nameof(Auditoria.AtorUsuarioId), nameof(Auditoria.OcorridoEmUtc)));

        foreach (var chaveEstrangeira in entidade.GetForeignKeys())
        {
            Assert.AreEqual(DeleteBehavior.Restrict, chaveEstrangeira.DeleteBehavior);
        }
    }

    private static IModel CriarModelo()
    {
        var options = new DbContextOptionsBuilder<ColetaSeletivaDbContext>()
            .UseSqlServer("Server=localhost;Database=ColetaSeletivaModelo;Integrated Security=True;")
            .Options;

        using var contexto = new ColetaSeletivaDbContext(options);
        return contexto.Model;
    }

    private static IEntityType ObterEntidade<TEntidade>()
        where TEntidade : class
    {
        return Modelo.FindEntityType(typeof(TEntidade))
            ?? throw new AssertFailedException($"Entidade {typeof(TEntidade).Name} não configurada.");
    }

    private static IProperty ObterPropriedade(IEntityType entidade, string nome)
    {
        return entidade.FindProperty(nome)
            ?? throw new AssertFailedException(
                $"Propriedade {entidade.ClrType.Name}.{nome} não configurada.");
    }

    private static IIndex ObterIndice(IEntityType entidade, params string[] propriedades)
    {
        return entidade
            .GetIndexes()
            .SingleOrDefault(indice => indice.Properties
                .Select(propriedade => propriedade.Name)
                .SequenceEqual(propriedades))
            ?? throw new AssertFailedException(
                $"Índice ({string.Join(", ", propriedades)}) não configurado em {entidade.ClrType.Name}.");
    }

    private static void AssertarDatasUtc(IEntityType entidade, params string[] nomes)
    {
        foreach (var nome in nomes)
        {
            var propriedade = ObterPropriedade(entidade, nome);
            Assert.AreEqual(typeof(DateTimeOffset), propriedade.ClrType);
            Assert.IsFalse(propriedade.IsNullable);
            Assert.AreEqual("datetimeoffset", propriedade.GetColumnType());
        }
    }

    private static void AssertarRowVersion(IEntityType entidade, string nome)
    {
        var propriedade = ObterPropriedade(entidade, nome);
        Assert.AreEqual(typeof(byte[]), propriedade.ClrType);
        Assert.IsTrue(propriedade.IsConcurrencyToken);
        Assert.AreEqual(ValueGenerated.OnAddOrUpdate, propriedade.ValueGenerated);
    }
}
