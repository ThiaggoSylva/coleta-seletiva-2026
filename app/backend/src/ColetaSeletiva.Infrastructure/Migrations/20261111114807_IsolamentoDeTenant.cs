using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColetaSeletiva.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IsolamentoDeTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Usuario_OrganizacaoId_Id",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "UX_Usuario_OrganizacaoId_Id_Composta",
                table: "AspNetUsers",
                columns: new[] { "OrganizacaoId", "Id" },
                unique: true);

            // A FK composta usa um índice único sem filtro porque o SQL Server não
            // permite referenciar índices filtrados em chaves estrangeiras.
            migrationBuilder.Sql(
                """
                ALTER TABLE [Auditoria]
                ADD CONSTRAINT [FK_Auditoria_AspNetUsers_OrganizacaoId_AtorUsuarioId]
                FOREIGN KEY ([OrganizacaoId], [AtorUsuarioId])
                REFERENCES [AspNetUsers] ([OrganizacaoId], [Id])
                ON DELETE NO ACTION;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE [Auditoria]
                DROP CONSTRAINT [FK_Auditoria_AspNetUsers_OrganizacaoId_AtorUsuarioId];
                """);

            migrationBuilder.DropIndex(
                name: "UX_Usuario_OrganizacaoId_Id_Composta",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "UX_Usuario_OrganizacaoId_Id",
                table: "AspNetUsers",
                columns: new[] { "OrganizacaoId", "Id" },
                unique: true,
                filter: "[OrganizacaoId] IS NOT NULL");
        }
    }
}
