using System;
using Microsoft.EntityFrameworkCore;
using OSISDiscordAssistant.Services;

#nullable disable

namespace OSISDiscordAssistant.Models
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

                entity.Property(e => e.Name)
                    .HasColumnName("tag_name")
                    .HasColumnType("string");

                entity.Property(e => e.Content)
                    .HasColumnName("tag_content")
                    .HasColumnType("string");

                entity.Property(e => e.CreatorUserId)
                    .HasColumnName("creator_user_id")
                    .HasColumnType("bigint");

                entity.Property(e => e.UpdaterUserId)
                    .HasColumnName("updater_user_id")
                    .HasColumnType("bigint");

                entity.Property(e => e.CreatedTimestamp)
                    .HasColumnName("created_timestamp")
                    .HasColumnType("bigint");

                entity.Property(e => e.LastUpdatedTimestamp)
                    .HasColumnName("last_updated_timestamp")
                    .HasColumnType("bigint");

                entity.Property(e => e.VersionCount)
                    .HasColumnName("version_count")
                    .HasColumnType("int");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
