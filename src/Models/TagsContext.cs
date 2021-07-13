using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace discordbot
{
    public partial class TagsContext : DbContext
    {
        /// <summary>
        /// Provides a context to conduct database operations on the Tags table.
        /// </summary>
        public TagsContext()
        {
        }

        public TagsContext(DbContextOptions<TagsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Tags> Tags { get; set; }

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

            modelBuilder.Entity<Tags>(entity =>
            {
                entity.ToTable("tags");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int");

                entity.Property(e => e.TagName)
                    .HasColumnName("tag_name")
                    .HasColumnType("string");

                entity.Property(e => e.TagContent)
                    .HasColumnName("tag_content")
                    .HasColumnType("string");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
