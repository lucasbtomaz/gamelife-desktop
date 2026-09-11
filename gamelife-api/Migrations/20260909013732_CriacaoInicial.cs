using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace gamelife_api.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Canais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "longtext", nullable: false),
                    PermitePosse = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PermitePreco = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Canais", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Jogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Titulo = table.Column<string>(type: "longtext", nullable: false),
                    AnoLancamento = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jogos", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItensListaDesejos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    JogoId = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "longtext", nullable: true),
                    PrecoDesejado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensListaDesejos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensListaDesejos_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PossesJogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    JogoId = table.Column<int>(type: "int", nullable: false),
                    CanalId = table.Column<int>(type: "int", nullable: false),
                    AdquiridoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PossesJogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PossesJogos_Canais_CanalId",
                        column: x => x.CanalId,
                        principalTable: "Canais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PossesJogos_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MotivosDesejos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ItemListaDesejosId = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotivosDesejos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MotivosDesejos_ItensListaDesejos_ItemListaDesejosId",
                        column: x => x.ItemListaDesejosId,
                        principalTable: "ItensListaDesejos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RegistrosPrecos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ItemListaDesejosId = table.Column<int>(type: "int", nullable: false),
                    CanalId = table.Column<int>(type: "int", nullable: false),
                    PrecoCheio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecoAtual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Disponivel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConsultadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosPrecos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosPrecos_Canais_CanalId",
                        column: x => x.CanalId,
                        principalTable: "Canais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosPrecos_ItensListaDesejos_ItemListaDesejosId",
                        column: x => x.ItemListaDesejosId,
                        principalTable: "ItensListaDesejos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RegistrosJogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PosseJogoId = table.Column<int>(type: "int", nullable: false),
                    FinalizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TempoEmHoras = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Dificuldade = table.Column<string>(type: "longtext", nullable: true),
                    ConquistasPossiveis = table.Column<int>(type: "int", nullable: true),
                    ConquistasObtidas = table.Column<int>(type: "int", nullable: true),
                    ItemListaDesejosId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosJogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosJogos_ItensListaDesejos_ItemListaDesejosId",
                        column: x => x.ItemListaDesejosId,
                        principalTable: "ItensListaDesejos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegistrosJogos_PossesJogos_PosseJogoId",
                        column: x => x.PosseJogoId,
                        principalTable: "PossesJogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ItensListaDesejos_JogoId",
                table: "ItensListaDesejos",
                column: "JogoId");

            migrationBuilder.CreateIndex(
                name: "IX_MotivosDesejos_ItemListaDesejosId",
                table: "MotivosDesejos",
                column: "ItemListaDesejosId");

            migrationBuilder.CreateIndex(
                name: "IX_PossesJogos_CanalId",
                table: "PossesJogos",
                column: "CanalId");

            migrationBuilder.CreateIndex(
                name: "IX_PossesJogos_JogoId",
                table: "PossesJogos",
                column: "JogoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosJogos_ItemListaDesejosId",
                table: "RegistrosJogos",
                column: "ItemListaDesejosId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosJogos_PosseJogoId",
                table: "RegistrosJogos",
                column: "PosseJogoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPrecos_CanalId",
                table: "RegistrosPrecos",
                column: "CanalId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPrecos_ItemListaDesejosId",
                table: "RegistrosPrecos",
                column: "ItemListaDesejosId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MotivosDesejos");

            migrationBuilder.DropTable(
                name: "RegistrosJogos");

            migrationBuilder.DropTable(
                name: "RegistrosPrecos");

            migrationBuilder.DropTable(
                name: "PossesJogos");

            migrationBuilder.DropTable(
                name: "ItensListaDesejos");

            migrationBuilder.DropTable(
                name: "Canais");

            migrationBuilder.DropTable(
                name: "Jogos");
        }
    }
}
