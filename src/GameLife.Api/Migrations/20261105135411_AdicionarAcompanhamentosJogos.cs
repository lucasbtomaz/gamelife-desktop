using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace gamelife_api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAcompanhamentosJogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcompanhamentosJogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    JogoId = table.Column<int>(type: "int", nullable: false),
                    CanalId = table.Column<int>(type: "int", nullable: false),
                    PosseJogoId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataStatus = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TempoEmHoras = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ConquistasPossiveis = table.Column<int>(type: "int", nullable: true),
                    ConquistasObtidas = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcompanhamentosJogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcompanhamentosJogos_Canais_CanalId",
                        column: x => x.CanalId,
                        principalTable: "Canais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcompanhamentosJogos_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcompanhamentosJogos_PossesJogos_PosseJogoId",
                        column: x => x.PosseJogoId,
                        principalTable: "PossesJogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AcompanhamentosJogos_CanalId",
                table: "AcompanhamentosJogos",
                column: "CanalId");

            migrationBuilder.CreateIndex(
                name: "IX_AcompanhamentosJogos_JogoId_CanalId",
                table: "AcompanhamentosJogos",
                columns: new[] { "JogoId", "CanalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcompanhamentosJogos_PosseJogoId",
                table: "AcompanhamentosJogos",
                column: "PosseJogoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcompanhamentosJogos");
        }
    }
}
