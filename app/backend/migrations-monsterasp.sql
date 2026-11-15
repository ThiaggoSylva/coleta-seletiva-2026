IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE TABLE [Organizacao] (
        [Id] uniqueidentifier NOT NULL,
        [Nome] nvarchar(150) NOT NULL,
        [Tipo] nvarchar(20) NOT NULL,
        [Publicada] bit NOT NULL,
        [CriadoEmUtc] datetimeoffset NOT NULL,
        [AtualizadoEmUtc] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Organizacao] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizacaoId] uniqueidentifier NULL,
        [Nome] nvarchar(150) NOT NULL,
        [Telefone] nvarchar(20) NULL,
        [Ativo] bit NOT NULL,
        [TrocaSenhaPendente] bit NOT NULL,
        [CriadoEmUtc] datetimeoffset NOT NULL,
        [AtualizadoEmUtc] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUsers_Organizacao_OrganizacaoId] FOREIGN KEY ([OrganizacaoId]) REFERENCES [Organizacao] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE TABLE [Auditoria] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizacaoId] uniqueidentifier NULL,
        [AtorUsuarioId] uniqueidentifier NULL,
        [Acao] nvarchar(100) NOT NULL,
        [Entidade] nvarchar(100) NOT NULL,
        [EntidadeId] uniqueidentifier NULL,
        [CorrelacaoId] uniqueidentifier NOT NULL,
        [OcorridoEmUtc] datetimeoffset NOT NULL,
        [CamposAlterados] nvarchar(2000) NULL,
        CONSTRAINT [PK_Auditoria] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Auditoria_AspNetUsers_AtorUsuarioId] FOREIGN KEY ([AtorUsuarioId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Auditoria_Organizacao_OrganizacaoId] FOREIGN KEY ([OrganizacaoId]) REFERENCES [Organizacao] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE TABLE [RefreshToken] (
        [Id] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [FamiliaId] uniqueidentifier NOT NULL,
        [Hash] char(64) NOT NULL,
        [CriadoEmUtc] datetimeoffset NOT NULL,
        [ExpiraEmUtc] datetimeoffset NOT NULL,
        [ConsumidoEmUtc] datetimeoffset NULL,
        [RevogadoEmUtc] datetimeoffset NULL,
        CONSTRAINT [PK_RefreshToken] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshToken_AspNetUsers_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_Usuario_OrganizacaoId] ON [AspNetUsers] ([OrganizacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Usuario_NormalizedEmail] ON [AspNetUsers] ([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Usuario_NormalizedUserName] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Usuario_OrganizacaoId_Id] ON [AspNetUsers] ([OrganizacaoId], [Id]) WHERE [OrganizacaoId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_Auditoria_AtorUsuarioId_OcorridoEmUtc] ON [Auditoria] ([AtorUsuarioId], [OcorridoEmUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_Auditoria_CorrelacaoId] ON [Auditoria] ([CorrelacaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_Auditoria_OrganizacaoId_OcorridoEmUtc] ON [Auditoria] ([OrganizacaoId], [OcorridoEmUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_RefreshToken_UsuarioId_FamiliaId] ON [RefreshToken] ([UsuarioId], [FamiliaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    CREATE UNIQUE INDEX [UX_RefreshToken_Hash] ON [RefreshToken] ([Hash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111110704_CriacaoInicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111110704_CriacaoInicial', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111114807_IsolamentoDeTenant'
)
BEGIN
    DROP INDEX [UX_Usuario_OrganizacaoId_Id] ON [AspNetUsers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111114807_IsolamentoDeTenant'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Usuario_OrganizacaoId_Id_Composta] ON [AspNetUsers] ([OrganizacaoId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111114807_IsolamentoDeTenant'
)
BEGIN
    ALTER TABLE [Auditoria]
    ADD CONSTRAINT [FK_Auditoria_AspNetUsers_OrganizacaoId_AtorUsuarioId]
    FOREIGN KEY ([OrganizacaoId], [AtorUsuarioId])
    REFERENCES [AspNetUsers] ([OrganizacaoId], [Id])
    ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111114807_IsolamentoDeTenant'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111114807_IsolamentoDeTenant', N'10.0.12');
END;

COMMIT;
GO

