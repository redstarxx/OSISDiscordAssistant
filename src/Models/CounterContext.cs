using Microsoft.EntityFrameworkCore;
using OSISDiscordAssistant.Utilities;

#nullable disable

namespace OSISDiscordAssistant.Models
{
    public partial class CounterContext : DbContext
    {
        /// <summary>
        /// Provides a context to conduct database operations on the PollCounter table.
        /// </summary>
        public CounterContext()
        {
        }

        public CounterContext(DbContextOptions<CounterContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Counter> Counter { get; set; }

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

            modelBuilder.Entity<Counter>(entity =>
            {
                entity.ToTable("counter");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int");

                entity.Property(e => e.PollCounter)
                    .HasColumnName("pollcounter")
                    .HasColumnType("int");

                entity.Property(e => e.VerifyCounter)
                    .HasColumnName("verifycounter")
                    .HasColumnType("int");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
