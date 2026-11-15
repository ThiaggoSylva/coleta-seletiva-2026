using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace ColetaSeletiva.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class F2OrganizacaoCatalogoPontos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContatoEmail",
                table: "Organizacao",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContatoTelefone",
                table: "Organizacao",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fuso",
                table: "Organizacao",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LimiteSolicitacoesAbertas",
                table: "Organizacao",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ConfiguracaoDePeriodo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFim = table.Column<TimeOnly>(type: "time", nullable: false),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracaoDePeriodo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracaoDePeriodo_Organizacao_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Municipio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipoResiduo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Orientacoes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoResiduo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizacaoMunicipio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipioId = table.Column<int>(type: "int", nullable: false),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizacaoMunicipio", x => x.Id);
                    table.UniqueConstraint("AK_OrganizacaoMunicipio_OrganizacaoId_MunicipioId", x => new { x.OrganizacaoId, x.MunicipioId });
                    table.ForeignKey(
                        name: "FK_OrganizacaoMunicipio_Municipio_MunicipioId",
                        column: x => x.MunicipioId,
                        principalTable: "Municipio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizacaoMunicipio_Organizacao_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizacaoTipo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoResiduoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AceitaRetiradaDomiciliar = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizacaoTipo", x => x.Id);
                    table.UniqueConstraint("AK_OrganizacaoTipo_OrganizacaoId_TipoResiduoId", x => new { x.OrganizacaoId, x.TipoResiduoId });
                    table.ForeignKey(
                        name: "FK_OrganizacaoTipo_Organizacao_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizacaoTipo_TipoResiduo_TipoResiduoId",
                        column: x => x.TipoResiduoId,
                        principalTable: "TipoResiduo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PontoColeta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MunicipioId = table.Column<int>(type: "int", nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Rua = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Complemento = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Localizacao = table.Column<Point>(type: "geography", nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExcluidoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontoColeta", x => x.Id);
                    table.UniqueConstraint("AK_PontoColeta_OrganizacaoId_Id", x => new { x.OrganizacaoId, x.Id });
                    table.ForeignKey(
                        name: "FK_PontoColeta_OrganizacaoMunicipio_OrganizacaoId_MunicipioId",
                        columns: x => new { x.OrganizacaoId, x.MunicipioId },
                        principalTable: "OrganizacaoMunicipio",
                        principalColumns: new[] { "OrganizacaoId", "MunicipioId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PontoColeta_Organizacao_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PontoHorario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PontoColetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaDaSemana = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFim = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontoHorario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PontoHorario_PontoColeta_OrganizacaoId_PontoColetaId",
                        columns: x => new { x.OrganizacaoId, x.PontoColetaId },
                        principalTable: "PontoColeta",
                        principalColumns: new[] { "OrganizacaoId", "Id" });
                });

            migrationBuilder.CreateTable(
                name: "PontoTipo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PontoColetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoResiduoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontoTipo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PontoTipo_OrganizacaoTipo_OrganizacaoId_TipoResiduoId",
                        columns: x => new { x.OrganizacaoId, x.TipoResiduoId },
                        principalTable: "OrganizacaoTipo",
                        principalColumns: new[] { "OrganizacaoId", "TipoResiduoId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PontoTipo_PontoColeta_OrganizacaoId_PontoColetaId",
                        columns: x => new { x.OrganizacaoId, x.PontoColetaId },
                        principalTable: "PontoColeta",
                        principalColumns: new[] { "OrganizacaoId", "Id" });
                });

            migrationBuilder.CreateIndex(
                name: "UX_ConfiguracaoDePeriodo_OrganizacaoId_Periodo",
                table: "ConfiguracaoDePeriodo",
                columns: new[] { "OrganizacaoId", "Periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipio_Uf_Nome",
                table: "Municipio",
                columns: new[] { "Uf", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizacaoMunicipio_MunicipioId",
                table: "OrganizacaoMunicipio",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizacaoTipo_TipoResiduoId",
                table: "OrganizacaoTipo",
                column: "TipoResiduoId");

            migrationBuilder.CreateIndex(
                name: "IX_PontoColeta_OrganizacaoId_MunicipioId",
                table: "PontoColeta",
                columns: new[] { "OrganizacaoId", "MunicipioId" });

            migrationBuilder.CreateIndex(
                name: "UX_PontoColeta_OrganizacaoId_Nome",
                table: "PontoColeta",
                columns: new[] { "OrganizacaoId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PontoHorario_OrganizacaoId_PontoColetaId",
                table: "PontoHorario",
                columns: new[] { "OrganizacaoId", "PontoColetaId" });

            migrationBuilder.CreateIndex(
                name: "IX_PontoTipo_OrganizacaoId_TipoResiduoId",
                table: "PontoTipo",
                columns: new[] { "OrganizacaoId", "TipoResiduoId" });

            migrationBuilder.CreateIndex(
                name: "UX_PontoTipo_OrganizacaoId_PontoColetaId_TipoResiduoId",
                table: "PontoTipo",
                columns: new[] { "OrganizacaoId", "PontoColetaId", "TipoResiduoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TipoResiduo_Codigo",
                table: "TipoResiduo",
                column: "Codigo",
                unique: true);

            migrationBuilder.Sql(
                "CREATE SPATIAL INDEX [IX_PontoColeta_Localizacao] ON [PontoColeta]([Localizacao]) "
                + "USING GEOGRAPHY_AUTO_GRID;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracaoDePeriodo");

            migrationBuilder.DropTable(
                name: "PontoHorario");

            migrationBuilder.DropTable(
                name: "PontoTipo");

            migrationBuilder.DropTable(
                name: "OrganizacaoTipo");

            migrationBuilder.DropTable(
                name: "PontoColeta");

            migrationBuilder.DropTable(
                name: "TipoResiduo");

            migrationBuilder.DropTable(
                name: "OrganizacaoMunicipio");

            migrationBuilder.DropTable(
                name: "Municipio");

            migrationBuilder.DropColumn(
                name: "ContatoEmail",
                table: "Organizacao");

            migrationBuilder.DropColumn(
                name: "ContatoTelefone",
                table: "Organizacao");

            migrationBuilder.DropColumn(
                name: "Fuso",
                table: "Organizacao");

            migrationBuilder.DropColumn(
                name: "LimiteSolicitacoesAbertas",
                table: "Organizacao");
        }
    }
}
