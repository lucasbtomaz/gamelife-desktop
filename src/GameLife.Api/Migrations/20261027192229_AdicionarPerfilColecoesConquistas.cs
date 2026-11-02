using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace gamelife_api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPerfilColecoesConquistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Colecoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colecoes", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Conquistas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Titulo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    JogoId = table.Column<int>(type: "int", nullable: true),
                    DesbloqueadaEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conquistas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conquistas_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PerfisLocais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FotoDataUrl = table.Column<string>(type: "longtext", nullable: true),
                    ExibirJogosRecentes = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisLocais", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ColecoesJogos",
                columns: table => new
                {
                    ColecaoId = table.Column<int>(type: "int", nullable: false),
                    JogoId = table.Column<int>(type: "int", nullable: false),
                    AdicionadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColecoesJogos", x => new { x.ColecaoId, x.JogoId });
                    table.ForeignKey(
                        name: "FK_ColecoesJogos_Colecoes_ColecaoId",
                        column: x => x.ColecaoId,
                        principalTable: "Colecoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ColecoesJogos_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ColecoesJogos_JogoId",
                table: "ColecoesJogos",
                column: "JogoId");

            migrationBuilder.CreateIndex(
                name: "IX_Conquistas_JogoId",
                table: "Conquistas",
                column: "JogoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ColecoesJogos");

            migrationBuilder.DropTable(
                name: "Conquistas");

            migrationBuilder.DropTable(
                name: "PerfisLocais");

            migrationBuilder.DropTable(
                name: "Colecoes");
        }
    }
}
