using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace gamelife_api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarOrigemDeAssinaturaEMinhasAssinaturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ResgatadoDeAssinatura",
                table: "PossesJogos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ServicoExigido",
                table: "PossesJogos",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MinhasAssinaturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Servico = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ProximaRenovacao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinhasAssinaturas", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MinhasAssinaturas_Servico",
                table: "MinhasAssinaturas",
                column: "Servico",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MinhasAssinaturas");

            migrationBuilder.DropColumn(
                name: "ResgatadoDeAssinatura",
                table: "PossesJogos");

            migrationBuilder.DropColumn(
                name: "ServicoExigido",
                table: "PossesJogos");
        }
    }
}
