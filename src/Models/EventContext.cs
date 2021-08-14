using System;
using Microsoft.EntityFrameworkCore;
using OSISDiscordAssistant.Utilities;

#nullable disable

namespace OSISDiscordAssistant.Models
{
    public partial class EventContext : DbContext
    {
        /// <summary>
        /// Provides a context to conduct database operations on the Events table.
        /// </summary>
        public EventContext()
        {
        }

        public EventContext(DbContextOptions<EventContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Events> Events { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = ClientUtilities.GetConnectionString();

                // Retry reconnecting to the database on failure.
                optionsBuilder.UseNpgsql(connectionString, builder => 
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

            modelBuilder.Entity<Events>(entity =>
            {
                entity.ToTable("events");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int");

                entity.Property(e => e.EventDate)
                    .HasMaxLength(50)
                    .HasColumnName("event_date");

                entity.Property(e => e.EventDateCultureInfo)
                    .HasMaxLength(10)
                    .HasColumnName("event_date_culture_info");

                entity.Property(e => e.EventDescription)
                    .HasMaxLength(255)
                    .HasColumnName("event_description");

                entity.Property(e => e.PersonInCharge)
                    .HasMaxLength(100)
                    .HasColumnName("person_in_charge");

                entity.Property(e => e.EventName)
                    .HasMaxLength(50)
                    .HasColumnName("event_name");

                entity.Property(e => e.ProposalReminded)
                    .HasColumnName("proposal_reminded")
                    .HasColumnType("bool");

                entity.Property(e => e.Expired)
                    .HasColumnName("expired")
                    .HasColumnType("bool");

                entity.Property(e => e.PreviouslyReminded)
                    .HasColumnName("previously_reminded")
                    .HasColumnType("bool");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
