using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace discordbot
{
    public partial class PollCounterContext : DbContext
    {
        /// <summary>
        /// Provides a context to conduct database operations on the PollCounter table.
        /// </summary>
        public PollCounterContext()
        {
        }

        public PollCounterContext(DbContextOptions<PollCounterContext> options)
            : base(options)
        {
        }

        public virtual DbSet<PollCounter> PollCounter { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = ClientUtilities.GetConnectionString();
                optionsBuilder.UseNpgsql(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("adminpack")
                .HasAnnotation("Relational:Collation", "English_United States.1252");

            modelBuilder.Entity<PollCounter>(entity =>
            {
                entity.ToTable("pollcounter");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int");

                entity.Property(e => e.Counter)
                    .HasColumnName("counter")
                    .HasColumnType("int");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
