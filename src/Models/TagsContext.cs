﻿using System;
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
            if (!optionsBuilder.IsConfigured)
            {
                // Retry reconnecting to the database on failure.
                optionsBuilder.UseNpgsql(SharedData.DbConnectionString, builder =>
                {
                    builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                });

                base.OnConfiguring(optionsBuilder);
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

                entity.Property(e => e.Name)
                    .HasColumnName("tag_name")
                    .HasColumnType("string");

                entity.Property(e => e.Content)
                    .HasColumnName("tag_content")
                    .HasColumnType("string");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
