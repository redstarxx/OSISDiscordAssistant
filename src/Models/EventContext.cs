using System;
using Microsoft.EntityFrameworkCore;
using OSISDiscordAssistant.Services;

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

                entity.Property(e => e.EventDateUnixTimestamp)
                    .HasColumnName("event_date_unix_timestamp")
                    .HasColumnType("int");

                entity.Property(e => e.NextScheduledReminderUnixTimestamp)
                    .HasColumnName("next_scheduled_reminder_timestamp")
                    .HasColumnType("int");

                entity.Property(e => e.EventDescription)
                    .HasColumnName("event_description");

                entity.Property(e => e.PersonInCharge)
                    .HasColumnName("person_in_charge");

                entity.Property(e => e.EventName)
                    .HasColumnName("event_name");

                entity.Property(e => e.ProposalReminded)
                    .HasColumnName("proposal_reminded")
                    .HasColumnType("bool");

                entity.Property(e => e.Expired)
                    .HasColumnName("expired")
                    .HasColumnType("bool");

                entity.Property(e => e.ExecutedReminderLevel)
                    .HasColumnName("executed_reminder_level")
                    .HasColumnType("int");

                entity.Property(e => e.ProposalFileTitle)
                    .HasColumnName("proposal_file_title");

                entity.Property(e => e.ProposalFileContent)
                    .HasColumnName("proposal_file_content");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
