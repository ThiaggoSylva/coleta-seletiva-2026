using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColetaSeletiva.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class F3RetiradasVolumesRelatorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloqueioData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataLocal = table.Column<DateOnly>(type: "date", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CriadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueioData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloqueioData_Organizacao_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisponibilidadeRetirada",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaDaSemana = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Capacidade = table.Column<int>(type: "int", nullable: false),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisponibilidadeRetirada", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisponibilidadeRetirada_Organizacao_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JanelaRetirada",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataLocal = table.Column<DateOnly>(type: "date", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JanelaRetirada", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JanelaRetirada_Organizacao_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notificacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Lida = table.Column<bool>(type: "bit", nullable: false),
                    LidaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CriadaEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacao_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notificacao_Organizacao_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacaoRetirada",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CidadaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeCidadao = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TelefoneCidadao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    MunicipioId = table.Column<int>(type: "int", nullable: false),
                    MunicipioNome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Rua = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Complemento = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PontoReferencia = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataLocal = table.Column<DateOnly>(type: "date", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoRetirada", x => x.Id);
                    table.UniqueConstraint("AK_SolicitacaoRetirada_OrganizacaoId_Id", x => new { x.OrganizacaoId, x.Id });
                    table.ForeignKey(
                        name: "FK_SolicitacaoRetirada_AspNetUsers_CidadaoId",
                        column: x => x.CidadaoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacaoRetirada_Organizacao_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistroVolume",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoResiduoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SolicitacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PontoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RegistroOriginalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperadorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperadorNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OcorridoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DataLocal = table.Column<DateOnly>(type: "date", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistroVolume", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistroVolume_AspNetUsers_OperadorUsuarioId",
                        column: x => x.OperadorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistroVolume_Organizacao_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistroVolume_PontoColeta_PontoId",
                        column: x => x.PontoId,
                        principalTable: "PontoColeta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistroVolume_RegistroVolume_RegistroOriginalId",
                        column: x => x.RegistroOriginalId,
                        principalTable: "RegistroVolume",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistroVolume_SolicitacaoRetirada_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "SolicitacaoRetirada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistroVolume_TipoResiduo_TipoResiduoId",
                        column: x => x.TipoResiduoId,
                        principalTable: "TipoResiduo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacaoHistorico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EstadoNovo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AtorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OcorridoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacaoHistorico_SolicitacaoRetirada_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "SolicitacaoRetirada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacaoItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoResiduoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantidadeEstimada = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacaoItem_SolicitacaoRetirada_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "SolicitacaoRetirada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitacaoItem_TipoResiduo_TipoResiduoId",
                        column: x => x.TipoResiduoId,
                        principalTable: "TipoResiduo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_BloqueioData_OrganizacaoId_DataLocal",
                table: "BloqueioData",
                columns: new[] { "OrganizacaoId", "DataLocal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DisponibilidadeRetirada_Organizacao_Dia_Periodo",
                table: "DisponibilidadeRetirada",
                columns: new[] { "OrganizacaoId", "DiaDaSemana", "Periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_JanelaRetirada_Organizacao_Data_Periodo",
                table: "JanelaRetirada",
                columns: new[] { "OrganizacaoId", "DataLocal", "Periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_OrganizacaoId",
                table: "Notificacao",
                column: "OrganizacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_UsuarioId_CriadaEmUtc",
                table: "Notificacao",
                columns: new[] { "UsuarioId", "CriadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_UsuarioId_Lida",
                table: "Notificacao",
                columns: new[] { "UsuarioId", "Lida" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistroVolume_OperadorUsuarioId",
                table: "RegistroVolume",
                column: "OperadorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroVolume_Organizacao_Data_Tipo",
                table: "RegistroVolume",
                columns: new[] { "OrganizacaoId", "DataLocal", "TipoResiduoId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistroVolume_Organizacao_Origem",
                table: "RegistroVolume",
                columns: new[] { "OrganizacaoId", "Origem" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistroVolume_PontoId",
                table: "RegistroVolume",
                column: "PontoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroVolume_RegistroOriginalId",
                table: "RegistroVolume",
                column: "RegistroOriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroVolume_SolicitacaoId",
                table: "RegistroVolume",
                column: "SolicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroVolume_TipoResiduoId",
                table: "RegistroVolume",
                column: "TipoResiduoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoHistorico_SolicitacaoId_Ocorrido",
                table: "SolicitacaoHistorico",
                columns: new[] { "SolicitacaoId", "OcorridoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoItem_TipoResiduoId",
                table: "SolicitacaoItem",
                column: "TipoResiduoId");

            migrationBuilder.CreateIndex(
                name: "UX_SolicitacaoItem_SolicitacaoId_TipoResiduoId",
                table: "SolicitacaoItem",
                columns: new[] { "SolicitacaoId", "TipoResiduoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoRetirada_Cidadao_Organizacao_Estado",
                table: "SolicitacaoRetirada",
                columns: new[] { "CidadaoId", "OrganizacaoId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoRetirada_Organizacao_Data_Periodo_Estado",
                table: "SolicitacaoRetirada",
                columns: new[] { "OrganizacaoId", "DataLocal", "Periodo", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoRetirada_Organizacao_Estado_Data",
                table: "SolicitacaoRetirada",
                columns: new[] { "OrganizacaoId", "Estado", "DataLocal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloqueioData");

            migrationBuilder.DropTable(
                name: "DisponibilidadeRetirada");

            migrationBuilder.DropTable(
                name: "JanelaRetirada");

            migrationBuilder.DropTable(
                name: "Notificacao");

            migrationBuilder.DropTable(
                name: "RegistroVolume");

            migrationBuilder.DropTable(
                name: "SolicitacaoHistorico");

            migrationBuilder.DropTable(
                name: "SolicitacaoItem");

            migrationBuilder.DropTable(
                name: "SolicitacaoRetirada");
        }
    }
}
