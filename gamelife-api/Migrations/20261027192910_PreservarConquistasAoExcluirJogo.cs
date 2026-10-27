using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gamelife_api.Migrations
{
    /// <inheritdoc />
    public partial class PreservarConquistasAoExcluirJogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conquistas_Jogos_JogoId",
                table: "Conquistas");

            migrationBuilder.AddForeignKey(
                name: "FK_Conquistas_Jogos_JogoId",
                table: "Conquistas",
                column: "JogoId",
                principalTable: "Jogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conquistas_Jogos_JogoId",
                table: "Conquistas");

            migrationBuilder.AddForeignKey(
                name: "FK_Conquistas_Jogos_JogoId",
                table: "Conquistas",
                column: "JogoId",
                principalTable: "Jogos",
                principalColumn: "Id");
        }
    }
}
