using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace gamelife_api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAssinaturasEFormasDeAcesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CompraDigital",
                table: "PossesJogos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PorAssinatura",
                table: "PossesJogos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE PossesJogos SET CompraDigital = 1 WHERE MidiaFisica = 0;");

            migrationBuilder.AddColumn<bool>(
                name: "AlertaBibliotecaSilenciado",
                table: "ItensListaDesejos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AssinaturasJogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    JogoId = table.Column<int>(type: "int", nullable: false),
                    Servico = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Situacao = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    DataDisponibilidade = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataEncerramento = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssinaturasJogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssinaturasJogos_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasJogos_JogoId_Servico",
                table: "AssinaturasJogos",
                columns: new[] { "JogoId", "Servico" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssinaturasJogos");

            migrationBuilder.DropColumn(
                name: "AlertaBibliotecaSilenciado",
                table: "ItensListaDesejos");

            migrationBuilder.DropColumn(
                name: "CompraDigital",
                table: "PossesJogos");

            migrationBuilder.DropColumn(
                name: "PorAssinatura",
                table: "PossesJogos");
        }
    }
}
