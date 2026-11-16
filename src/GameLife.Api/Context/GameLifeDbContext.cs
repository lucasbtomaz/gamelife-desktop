using gamelife_api.Models;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Context
{
    public class GameLifeDbContext : DbContext
    {
        public GameLifeDbContext(DbContextOptions<GameLifeDbContext> options) : base(options)
        {

        }
        public DbSet<Jogo> Jogos { get; set; }
        public DbSet<Canal> Canais { get; set; }
        public DbSet<PosseJogo> PossesJogos { get; set; }
        public DbSet<ItemListaDesejos> ItensListaDesejos { get; set; }
        public DbSet<MotivoDesejo> MotivosDesejos { get; set; }
        public DbSet<RegistroPreco> RegistrosPrecos { get; set; }
        public DbSet<RegistroJogo> RegistrosJogos { get; set; }
        public DbSet<PerfilLocal> PerfisLocais { get; set; }
        public DbSet<Colecao> Colecoes { get; set; }
        public DbSet<ColecaoJogo> ColecoesJogos { get; set; }
        public DbSet<Conquista> Conquistas { get; set; }
        public DbSet<AcompanhamentoJogo> AcompanhamentosJogos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ColecaoJogo>()
                .HasKey(item => new { item.ColecaoId, item.JogoId });

            modelBuilder.Entity<ColecaoJogo>()
                .HasOne(item => item.Colecao)
                .WithMany(colecao => colecao.Jogos)
                .HasForeignKey(item => item.ColecaoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ColecaoJogo>()
                .HasOne(item => item.Jogo)
                .WithMany(jogo => jogo.Colecoes)
                .HasForeignKey(item => item.JogoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conquista>()
                .HasOne(item => item.Jogo)
                .WithMany()
                .HasForeignKey(item => item.JogoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AcompanhamentoJogo>()
                .HasIndex(item => new { item.JogoId, item.CanalId })
                .IsUnique();

            modelBuilder.Entity<PosseJogo>()
                .HasIndex(item => new { item.JogoId, item.CanalId })
                .IsUnique();

            modelBuilder.Entity<AcompanhamentoJogo>()
                .HasOne(item => item.Jogo)
                .WithMany()
                .HasForeignKey(item => item.JogoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AcompanhamentoJogo>()
                .HasOne(item => item.Canal)
                .WithMany()
                .HasForeignKey(item => item.CanalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AcompanhamentoJogo>()
                .HasOne(item => item.PosseJogo)
                .WithMany()
                .HasForeignKey(item => item.PosseJogoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
