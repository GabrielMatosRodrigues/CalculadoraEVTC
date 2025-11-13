using CalculadoraEVTC.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculadoraEVTC.Data
{
    public class CalculadoraContext : DbContext
    {
        public CalculadoraContext(DbContextOptions<CalculadoraContext> options)
            : base(options)
        {
        }

        public DbSet<Cotacao> Cotacoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cotacao>(entity =>
            {
                entity.ToTable("Cotacao");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Data).IsRequired();
                entity.Property(e => e.Indexador).HasMaxLength(30).IsRequired();
                entity.Property(e => e.Valor).HasColumnType("DECIMAL(10,2)").IsRequired();
            });
        }
    }
}