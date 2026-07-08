using Microsoft.EntityFrameworkCore;
using AutoMatics.Domain.IAM.Model.Aggregates;
using AutoMatics.Domain.Clientes.Model.Aggregates;
using AutoMatics.Domain.Creditos.Model.Aggregates;

namespace AutoMatics.Infrastructure.Data
{
    public class AutoMaticsDbContext : DbContext
    {
        public AutoMaticsDbContext(DbContextOptions<AutoMaticsDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<SustentoCliente> SustentosClientes { get; set; }
        public DbSet<Credito> Creditos { get; set; }
        public DbSet<CronogramaPago> CronogramaPagos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>().HasKey(u => u.Id);
            modelBuilder.Entity<Cliente>().HasKey(c => c.Id);
            modelBuilder.Entity<Vehiculo>().HasKey(v => v.Id);

            modelBuilder.Entity<SustentoCliente>().HasKey(s => s.Id);
            modelBuilder.Entity<Cliente>().HasMany(c => c.Sustentos).WithOne().HasForeignKey(s => s.ClienteId);

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.VehiculoObjetivo)
                .WithOne()
                .HasForeignKey<Vehiculo>(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Credito>().HasKey(c => c.Id);
            modelBuilder.Entity<Credito>().OwnsOne(c => c.EvaluacionRiesgo);

            modelBuilder.Entity<Credito>().Property(c => c.IndicadorVAN).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Credito>().Property(c => c.IndicadorTIR).HasColumnType("decimal(18,6)");
            modelBuilder.Entity<Credito>().Property(c => c.IndicadorTCEA).HasColumnType("decimal(18,6)");

            modelBuilder.Entity<CronogramaPago>().HasKey(cp => cp.Id);
            modelBuilder.Entity<Credito>().HasMany(c => c.CronogramaPagos).WithOne().HasForeignKey(cp => cp.CreditoId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}