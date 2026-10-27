using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gamelife_api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarMidiaFisicaNaBiblioteca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MidiaFisica",
                table: "PossesJogos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MidiaFisica",
                table: "PossesJogos");
        }
    }
}
