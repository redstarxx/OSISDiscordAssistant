using System;
using Microsoft.EntityFrameworkCore;
using OSISDiscordAssistant.Services;

namespace OSISDiscordAssistant.Models
{
    public partial class VerificationContext : DbContext
    {
        /// <summary>
        /// Provides a context to conduct database operations on the Verifications table.
        /// </summary>
        public VerificationContext()
        {
        }

        public VerificationContext(DbContextOptions<VerificationContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Verification> Verifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("adminpack")
                .HasAnnotation("Relational:Collation", "English_United States.1252");

            modelBuilder.Entity<Verification>(entity =>
            {
                entity.ToTable("verification");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int");

                entity.Property(e => e.UserId)
                    .HasColumnType("bigint")
                    .HasColumnName("user_id");

                entity.Property(e => e.VerificationEmbedId)
                    .HasColumnType("bigint")
                    .HasColumnName("verification_embed_id");

                entity.Property(e => e.RequestedName)
                    .HasColumnType("string")
                    .HasColumnName("requested_nickname");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
