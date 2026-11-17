using ColetaSeletiva.Application.Seguranca;
using ColetaSeletiva.Domain.Entidades;
using ColetaSeletiva.Domain.Retiradas;
using ColetaSeletiva.Infrastructure.Identidade;
using ColetaSeletiva.Infrastructure.Persistencia.Conversores;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ColetaSeletiva.Infrastructure.Persistencia;

public sealed class ColetaSeletivaDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
{
    private readonly ITenantContext _tenantContext;

    public ColetaSeletivaDbContext(
        DbContextOptions<ColetaSeletivaDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Organizacao> Organizacoes => Set<Organizacao>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    public DbSet<Municipio> Municipios => Set<Municipio>();

    public DbSet<TipoResiduo> TiposResiduo => Set<TipoResiduo>();

    public DbSet<OrganizacaoMunicipio> OrganizacaoMunicipios => Set<OrganizacaoMunicipio>();

    public DbSet<OrganizacaoTipo> OrganizacaoTipos => Set<OrganizacaoTipo>();

    public DbSet<ConfiguracaoDePeriodo> ConfiguracoesDePeriodo => Set<ConfiguracaoDePeriodo>();

    public DbSet<PontoDeColeta> PontosDeColeta => Set<PontoDeColeta>();

    public DbSet<PontoHorario> PontoHorarios => Set<PontoHorario>();

    public DbSet<PontoTipo> PontoTipos => Set<PontoTipo>();

    public DbSet<DisponibilidadeDeRetirada> DisponibilidadesDeRetirada =>
        Set<DisponibilidadeDeRetirada>();

    public DbSet<BloqueioDeData> BloqueiosDeData => Set<BloqueioDeData>();

    public DbSet<JanelaDeRetirada> JanelasDeRetirada => Set<JanelaDeRetirada>();

    public DbSet<SolicitacaoDeRetirada> SolicitacoesDeRetirada => Set<SolicitacaoDeRetirada>();

    public DbSet<SolicitacaoItem> SolicitacaoItens => Set<SolicitacaoItem>();

    public DbSet<SolicitacaoHistorico> SolicitacaoHistorico => Set<SolicitacaoHistorico>();

    public DbSet<RegistroDeVolume> RegistrosDeVolume => Set<RegistroDeVolume>();

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ColetaSeletivaDbContext).Assembly);
        AplicarFiltrosDeTenant(builder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<UtcDateTimeOffsetConverter>();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AplicarGuardaDeTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        AplicarGuardaDeTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AplicarFiltrosDeTenant(ModelBuilder builder)
    {
        builder.Entity<Organizacao>().HasQueryFilter(organizacao =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && organizacao.Id == _tenantContext.OrganizacaoId));

        builder.Entity<Usuario>().HasQueryFilter(usuario =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && ((_tenantContext.EhCidadao
                        && _tenantContext.UsuarioId != null
                        && usuario.Id == _tenantContext.UsuarioId
                        && usuario.OrganizacaoId == null)
                    || (!_tenantContext.EhCidadao
                        && _tenantContext.OrganizacaoId != null
                        && usuario.OrganizacaoId == _tenantContext.OrganizacaoId))));

        builder.Entity<Auditoria>().HasQueryFilter(auditoria =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && ((_tenantContext.EhCidadao
                        && _tenantContext.UsuarioId != null
                        && auditoria.OrganizacaoId == null
                        && auditoria.AtorUsuarioId == _tenantContext.UsuarioId)
                    || (!_tenantContext.EhCidadao
                        && _tenantContext.OrganizacaoId != null
                        && auditoria.OrganizacaoId == _tenantContext.OrganizacaoId))));

        builder.Entity<OrganizacaoMunicipio>().HasQueryFilter(vinculo =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && vinculo.OrganizacaoId == _tenantContext.OrganizacaoId));

        builder.Entity<OrganizacaoTipo>().HasQueryFilter(vinculo =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && vinculo.OrganizacaoId == _tenantContext.OrganizacaoId));

        builder.Entity<ConfiguracaoDePeriodo>().HasQueryFilter(periodo =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && periodo.OrganizacaoId == _tenantContext.OrganizacaoId));

        builder.Entity<PontoDeColeta>().HasQueryFilter(ponto =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && ponto.OrganizacaoId == _tenantContext.OrganizacaoId));

        builder.Entity<PontoHorario>().HasQueryFilter(horario =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && horario.OrganizacaoId == _tenantContext.OrganizacaoId));

        builder.Entity<PontoTipo>().HasQueryFilter(tipo =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && tipo.OrganizacaoId == _tenantContext.OrganizacaoId));

        builder.Entity<DisponibilidadeDeRetirada>().HasQueryFilter(disponibilidade =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && disponibilidade.OrganizacaoId == _tenantContext.OrganizacaoId));

        builder.Entity<BloqueioDeData>().HasQueryFilter(bloqueio =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && bloqueio.OrganizacaoId == _tenantContext.OrganizacaoId));

        builder.Entity<JanelaDeRetirada>().HasQueryFilter(janela =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && janela.OrganizacaoId == _tenantContext.OrganizacaoId));

        builder.Entity<SolicitacaoDeRetirada>().HasQueryFilter(solicitacao =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && ((_tenantContext.EhCidadao
                        && _tenantContext.UsuarioId != null
                        && solicitacao.CidadaoId == _tenantContext.UsuarioId)
                    || (!_tenantContext.EhCidadao
                        && _tenantContext.OrganizacaoId != null
                        && solicitacao.OrganizacaoId == _tenantContext.OrganizacaoId))));

        builder.Entity<RegistroDeVolume>().HasQueryFilter(volume =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && !_tenantContext.EhCidadao
                && _tenantContext.OrganizacaoId != null
                && volume.OrganizacaoId == _tenantContext.OrganizacaoId));

        builder.Entity<Notificacao>().HasQueryFilter(notificacao =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && _tenantContext.UsuarioId != null
                && notificacao.UsuarioId == _tenantContext.UsuarioId));

        builder.Entity<RefreshToken>().HasQueryFilter(token =>
            !_tenantContext.Autenticado
            || (_tenantContext.EscopoValido
                && ((_tenantContext.EhCidadao
                        && _tenantContext.UsuarioId != null
                        && token.UsuarioId == _tenantContext.UsuarioId)
                    || (!_tenantContext.EhCidadao
                        && _tenantContext.OrganizacaoId != null
                        && Usuarios.Any(usuario =>
                            usuario.Id == token.UsuarioId
                            && usuario.OrganizacaoId == _tenantContext.OrganizacaoId)))));
    }

    private void AplicarGuardaDeTenant()
    {
        if (!_tenantContext.Autenticado)
        {
            return;
        }

        if (!_tenantContext.EscopoValido)
        {
            throw new ViolacaoDeTenantException(
                "A sessão não possui escopo de organização ou de cidadão válido.");
        }

        var entradas = ChangeTracker
            .Entries()
            .Where(entrada => entrada.State is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted)
            .ToList();

        foreach (var entrada in entradas)
        {
            switch (entrada.Entity)
            {
                case Organizacao organizacao:
                    ValidarOrganizacao(entrada, organizacao);
                    break;
                case Auditoria auditoria:
                    ValidarAuditoria(entrada, auditoria);
                    break;
                case Usuario usuario:
                    ValidarUsuario(entrada, usuario);
                    break;
                case RefreshToken token:
                    ValidarRefreshToken(entrada, token);
                    break;
                case SolicitacaoDeRetirada solicitacao:
                    ValidarSolicitacao(entrada, solicitacao);
                    break;
                case JanelaDeRetirada janela:
                    ValidarJanela(entrada, janela);
                    break;
                case RegistroDeVolume volume:
                    ValidarVolume(entrada, volume);
                    break;
                case Notificacao notificacao:
                    ValidarNotificacao(entrada, notificacao);
                    break;
                case IEntidadeDaOrganizacao entidade:
                    ValidarEntidadeDaOrganizacao(entrada, entidade);
                    break;
            }
        }
    }

    private void ValidarOrganizacao(EntityEntry entrada, Organizacao organizacao)
    {
        if (_tenantContext.EhCidadao || entrada.State == EntityState.Added)
        {
            throw new ViolacaoDeTenantException(
                "A sessão não pode criar ou alterar organizações.");
        }

        if (_tenantContext.OrganizacaoId != organizacao.Id)
        {
            throw new ViolacaoDeTenantException(
                "A organização informada não pertence ao escopo da sessão.");
        }
    }

    private void ValidarUsuario(EntityEntry entrada, Usuario usuario)
    {
        if (_tenantContext.EhCidadao)
        {
            if (entrada.State == EntityState.Added
                || usuario.Id != _tenantContext.UsuarioId
                || usuario.OrganizacaoId is not null)
            {
                throw new ViolacaoDeTenantException(
                    "O cidadão só pode alterar o próprio perfil.");
            }

            if (entrada.State == EntityState.Modified
                && entrada.Property(nameof(Usuario.OrganizacaoId)).OriginalValue is not null)
            {
                throw new ViolacaoDeTenantException(
                    "O cidadão não pode alterar a organização do perfil.");
            }

            if (entrada.State != EntityState.Added
                && !Usuarios.AsNoTracking().Any(item => item.Id == usuario.Id))
            {
                throw new ViolacaoDeTenantException(
                    "O usuário informado não existe no escopo da sessão.");
            }

            return;
        }

        var organizacaoId = _tenantContext.OrganizacaoId
            ?? throw new ViolacaoDeTenantException(
                "A sessão não possui organização válida para alterar usuários.");

        if (entrada.State == EntityState.Added && usuario.OrganizacaoId is null)
        {
            usuario.OrganizacaoId = organizacaoId;
        }

        if (usuario.OrganizacaoId != organizacaoId)
        {
            throw new ViolacaoDeTenantException(
                "O usuário informado não pertence à organização da sessão.");
        }

        var organizacaoOriginal = entrada.Property(nameof(Usuario.OrganizacaoId)).OriginalValue;
        if (entrada.State == EntityState.Modified
            && organizacaoOriginal is Guid organizacaoAnterior
            && organizacaoAnterior != organizacaoId)
        {
            throw new ViolacaoDeTenantException(
                "A organização do usuário não pode ser alterada pela sessão.");
        }

        if (entrada.State != EntityState.Added
            && !Usuarios.AsNoTracking().Any(item => item.Id == usuario.Id))
        {
            throw new ViolacaoDeTenantException(
                "O usuário informado não existe no escopo da sessão.");
        }
    }

    private void ValidarAuditoria(EntityEntry entrada, Auditoria auditoria)
    {
        if (entrada.State != EntityState.Added)
        {
            throw new ViolacaoDeTenantException("Registros de auditoria são imutáveis.");
        }

        if (_tenantContext.EhCidadao)
        {
            if (auditoria.OrganizacaoId is not null)
            {
                throw new ViolacaoDeTenantException(
                    "A auditoria do cidadão não pode pertencer a uma organização.");
            }

            if (auditoria.AtorUsuarioId is null)
            {
                auditoria.AtorUsuarioId = _tenantContext.UsuarioId;
            }
            else if (auditoria.AtorUsuarioId != _tenantContext.UsuarioId)
            {
                throw new ViolacaoDeTenantException(
                    "O ator da auditoria não pertence ao escopo da sessão.");
            }

            return;
        }

        var organizacaoId = _tenantContext.OrganizacaoId
            ?? throw new ViolacaoDeTenantException(
                "A sessão não possui organização válida para auditar.");

        if (auditoria.OrganizacaoId is null)
        {
            auditoria.OrganizacaoId = organizacaoId;
        }
        else if (auditoria.OrganizacaoId != organizacaoId)
        {
            throw new ViolacaoDeTenantException(
                "A auditoria informada não pertence à organização da sessão.");
        }

        auditoria.AtorUsuarioId ??= _tenantContext.UsuarioId;

        if (auditoria.AtorUsuarioId is Guid atorUsuarioId
            && !Usuarios.AsNoTracking().Any(item => item.Id == atorUsuarioId))
        {
            throw new ViolacaoDeTenantException(
                "O ator da auditoria não pertence à organização da sessão.");
        }
    }

    private void ValidarRefreshToken(EntityEntry entrada, RefreshToken token)
    {
        if (entrada.State == EntityState.Modified
            && entrada.Property(nameof(RefreshToken.UsuarioId)).IsModified)
        {
            throw new ViolacaoDeTenantException(
                "O dono do refresh token não pode ser alterado.");
        }

        if (_tenantContext.EhCidadao)
        {
            if (token.UsuarioId != _tenantContext.UsuarioId)
            {
                throw new ViolacaoDeTenantException(
                    "O refresh token não pertence ao cidadão da sessão.");
            }

            return;
        }

        var organizacaoId = _tenantContext.OrganizacaoId
            ?? throw new ViolacaoDeTenantException(
                "A sessão não possui organização válida para alterar tokens.");

        var donoPertenceAOrganizacao = Usuarios.Any(usuario =>
            usuario.Id == token.UsuarioId
            && usuario.OrganizacaoId == organizacaoId);

        if (!donoPertenceAOrganizacao)
        {
            throw new ViolacaoDeTenantException(
                "O refresh token não pertence à organização da sessão.");
        }
    }

    private void ValidarSolicitacao(
        EntityEntry entrada,
        SolicitacaoDeRetirada solicitacao)
    {
        if (_tenantContext.EhCidadao)
        {
            if (solicitacao.CidadaoId != _tenantContext.UsuarioId)
            {
                throw new ViolacaoDeTenantException(
                    "A solicitação informada não pertence ao cidadão da sessão.");
            }

            if (entrada.State == EntityState.Modified
                && entrada.Property(nameof(SolicitacaoDeRetirada.CidadaoId)).OriginalValue
                    is Guid cidadaoOriginal
                && cidadaoOriginal != _tenantContext.UsuarioId)
            {
                throw new ViolacaoDeTenantException(
                    "O dono da solicitação não pode ser alterado.");
            }

            return;
        }

        var organizacaoId = _tenantContext.OrganizacaoId
            ?? throw new ViolacaoDeTenantException(
                "A sessão não possui organização válida para alterar solicitações.");

        if (solicitacao.OrganizacaoId != organizacaoId)
        {
            throw new ViolacaoDeTenantException(
                "A solicitação informada não pertence à organização da sessão.");
        }

        if (entrada.State == EntityState.Modified
            && entrada.Property(nameof(SolicitacaoDeRetirada.OrganizacaoId)).OriginalValue
                is Guid organizacaoOriginal
            && organizacaoOriginal != organizacaoId)
        {
            throw new ViolacaoDeTenantException(
                "A organização da solicitação não pode ser alterada.");
        }
    }

    private void ValidarJanela(EntityEntry entrada, JanelaDeRetirada janela)
    {
        if (_tenantContext.EhCidadao
            && entrada.State == EntityState.Added
            && janela.OrganizacaoId != Guid.Empty)
        {
            return;
        }

        var organizacaoId = _tenantContext.OrganizacaoId;
        if (!_tenantContext.EhCidadao && organizacaoId == janela.OrganizacaoId)
        {
            return;
        }

        throw new ViolacaoDeTenantException(
            "A janela informada não pertence ao escopo da sessão.");
    }

    private void ValidarVolume(EntityEntry entrada, RegistroDeVolume volume)
    {
        if (entrada.State is EntityState.Modified or EntityState.Deleted)
        {
            throw new ViolacaoDeTenantException("Registros de volume são imutáveis.");
        }

        if (_tenantContext.EhCidadao)
        {
            throw new ViolacaoDeTenantException(
                "O cidadão não pode registrar ou alterar volumes.");
        }

        var organizacaoId = _tenantContext.OrganizacaoId
            ?? throw new ViolacaoDeTenantException(
                "A sessão não possui organização válida para registrar volumes.");

        if (volume.OrganizacaoId != organizacaoId)
        {
            throw new ViolacaoDeTenantException(
                "O volume informado não pertence à organização da sessão.");
        }
    }

    private void ValidarNotificacao(EntityEntry entrada, Notificacao notificacao)
    {
        if (entrada.State == EntityState.Deleted)
        {
            throw new ViolacaoDeTenantException("Notificações não podem ser excluídas.");
        }

        if (entrada.State == EntityState.Added)
        {
            return;
        }

        if (notificacao.UsuarioId != _tenantContext.UsuarioId)
        {
            throw new ViolacaoDeTenantException(
                "A notificação informada não pertence ao usuário da sessão.");
        }

        if (entrada.Property(nameof(Notificacao.UsuarioId)).OriginalValue is Guid destinatarioOriginal
            && destinatarioOriginal != _tenantContext.UsuarioId)
        {
            throw new ViolacaoDeTenantException(
                "O destinatário da notificação não pode ser alterado.");
        }
    }

    private void ValidarEntidadeDaOrganizacao(
        EntityEntry entrada,
        IEntidadeDaOrganizacao entidade)
    {
        if (_tenantContext.EhCidadao)
        {
            throw new ViolacaoDeTenantException(
                "O cidadão não pode alterar entidades da organização.");
        }

        var organizacaoId = _tenantContext.OrganizacaoId
            ?? throw new ViolacaoDeTenantException(
                "A sessão não possui organização válida para alterar entidades.");

        if (entrada.State == EntityState.Added && entidade.OrganizacaoId is null)
        {
            entidade.OrganizacaoId = organizacaoId;
        }

        if (entidade.OrganizacaoId != organizacaoId)
        {
            throw new ViolacaoDeTenantException(
                "A entidade informada não pertence à organização da sessão.");
        }

        if (entrada.State == EntityState.Modified
            && entrada.Property(nameof(IEntidadeDaOrganizacao.OrganizacaoId)).OriginalValue
                is Guid organizacaoOriginal
            && organizacaoOriginal != organizacaoId)
        {
            throw new ViolacaoDeTenantException(
                "A organização da entidade não pode ser alterada pela sessão.");
        }
    }
}
