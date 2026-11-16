using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gamelife_api.Migrations
{
    /// <inheritdoc />
    public partial class GarantirPosseUnicaPorPlataforma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PossesJogos_JogoId",
                table: "PossesJogos");

            migrationBuilder.CreateIndex(
                name: "IX_PossesJogos_JogoId_CanalId",
                table: "PossesJogos",
                columns: new[] { "JogoId", "CanalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PossesJogos_JogoId_CanalId",
                table: "PossesJogos");

            migrationBuilder.CreateIndex(
                name: "IX_PossesJogos_JogoId",
                table: "PossesJogos",
                column: "JogoId");
        }
    }
}
