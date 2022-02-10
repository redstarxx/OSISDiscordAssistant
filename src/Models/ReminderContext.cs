using Microsoft.EntityFrameworkCore;

namespace OSISDiscordAssistant.Models
{
    public partial class ReminderContext : DbContext
    {
        /// <summary>
        /// Provides a context to conduct database operations on the Verifications table.
        /// </summary>
        public ReminderContext()
        {
        }

        public ReminderContext(DbContextOptions<ReminderContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Reminder> Reminders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("adminpack")
                .HasAnnotation("Relational:Collation", "English_United States.1252");

            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.ToTable("reminders");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int");

                entity.Property(e => e.InitiatingUserId)
                    .HasColumnType("bigint")
                    .HasColumnName("initiating_user_id");

                entity.Property(e => e.TargetedUserOrRoleMention)
                    .HasColumnType("string")
                    .HasColumnName("targeted_user_or_role_mention");

                entity.Property(e => e.UnixTimestampRemindAt)
                    .HasColumnType("int")
                    .HasColumnName("unix_timestamp_remind_time");

                entity.Property(e => e.TargetGuildId)
                    .HasColumnType("bigint")
                    .HasColumnName("target_guild_id");

                entity.Property(e => e.ReplyMessageId)
                    .HasColumnType("bigint")
                    .HasColumnName("reply_message_id");

                entity.Property(e => e.TargetChannelId)
                    .HasColumnType("bigint")
                    .HasColumnName("target_channel_id");

                entity.Property(e => e.Cancelled)
                    .HasColumnType("boolean")
                    .HasColumnName("cancelled");

                entity.Property(e => e.Content)
                    .HasColumnType("string")
                    .HasColumnName("content");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
