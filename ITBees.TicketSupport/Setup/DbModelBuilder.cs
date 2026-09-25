using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.DbModels;
using Microsoft.EntityFrameworkCore;

namespace ITBees.TicketSupport.Setup;

public class DbModelBuilder
{
    public static void Register(ModelBuilder modelBuilder)
    {
        var supportTicket = modelBuilder.Entity<SupportTicket>();
        supportTicket.HasKey(x => x.Guid);
        supportTicket.Property(x => x.Subject).HasMaxLength(SupportTicketContentLimits.Subject);
        supportTicket.Property(x => x.ContextType).HasMaxLength(SupportTicketContentLimits.ContextType);
        supportTicket.Property(x => x.ContextName).HasMaxLength(SupportTicketContentLimits.ContextName);
        supportTicket.Property(x => x.ReferenceType).HasMaxLength(SupportTicketContentLimits.ReferenceType);
        supportTicket.Property(x => x.ReferenceId).HasMaxLength(SupportTicketContentLimits.ReferenceId);
        supportTicket.Property(x => x.RequesterEmail).HasMaxLength(SupportTicketContentLimits.Email);
        supportTicket.Property(x => x.RequesterName).HasMaxLength(SupportTicketContentLimits.PersonName);
        supportTicket.Property(x => x.CloseNote).HasMaxLength(SupportTicketContentLimits.Body);
        supportTicket.HasIndex(x => x.Number).IsUnique();
        supportTicket.HasIndex(x => new { x.Status, x.UpdatedUtc });
        supportTicket.HasIndex(x => new { x.RequesterGuid, x.CreatedUtc });
        supportTicket.HasIndex(x => x.AssignedToGuid);
        supportTicket.HasIndex(x => new { x.ContextType, x.ContextGuid, x.CreatedUtc });
        supportTicket.HasIndex(x => new { x.ReferenceType, x.ReferenceId });

        var message = modelBuilder.Entity<SupportTicketMessage>();
        message.HasKey(x => x.Guid);
        message.Property(x => x.Body).HasMaxLength(SupportTicketContentLimits.Body);
        message.Property(x => x.BodyHtml).HasMaxLength(SupportTicketContentLimits.BodyHtml);
        message.Property(x => x.AuthorEmail).HasMaxLength(SupportTicketContentLimits.Email);
        message.Property(x => x.AuthorName).HasMaxLength(SupportTicketContentLimits.PersonName);
        message.HasIndex(x => new { x.SupportTicketGuid, x.CreatedUtc });
        message.HasOne(x => x.SupportTicket).WithMany(x => x.Messages)
            .HasForeignKey(x => x.SupportTicketGuid)
            .OnDelete(DeleteBehavior.Cascade);

        var attachment = modelBuilder.Entity<SupportTicketAttachment>();
        attachment.HasKey(x => x.Guid);
        attachment.Property(x => x.FileName).HasMaxLength(SupportTicketContentLimits.FileName);
        attachment.Property(x => x.ContentType).HasMaxLength(SupportTicketContentLimits.Key);
        attachment.Property(x => x.StorageKey).HasMaxLength(SupportTicketContentLimits.StorageKey);
        attachment.HasIndex(x => x.SupportTicketGuid);
        attachment.HasOne(x => x.SupportTicket).WithMany(x => x.Attachments)
            .HasForeignKey(x => x.SupportTicketGuid)
            .OnDelete(DeleteBehavior.Cascade);
        attachment.HasOne(x => x.SupportTicketMessage).WithMany(x => x.Attachments)
            .HasForeignKey(x => x.SupportTicketMessageGuid)
            .OnDelete(DeleteBehavior.NoAction);

        var rating = modelBuilder.Entity<SupportTicketRating>();
        rating.HasKey(x => x.Guid);
        rating.Property(x => x.Comment).HasMaxLength(SupportTicketContentLimits.RatingComment);
        rating.HasIndex(x => x.SupportTicketGuid).IsUnique();
        rating.HasIndex(x => new { x.RatedAgentGuid, x.RatedUtc });
        rating.HasOne(x => x.SupportTicket).WithMany()
            .HasForeignKey(x => x.SupportTicketGuid)
            .OnDelete(DeleteBehavior.Cascade);

        var supportTicketEvent = modelBuilder.Entity<SupportTicketEvent>();
        supportTicketEvent.HasKey(x => x.Guid);
        supportTicketEvent.Property(x => x.EventType).HasMaxLength(SupportTicketContentLimits.Key).IsRequired();
        supportTicketEvent.Property(x => x.FromValue).HasMaxLength(SupportTicketContentLimits.Key);
        supportTicketEvent.Property(x => x.ToValue).HasMaxLength(SupportTicketContentLimits.Key);
        supportTicketEvent.Property(x => x.ActorName).HasMaxLength(SupportTicketContentLimits.PersonName);
        supportTicketEvent.HasIndex(x => new { x.SupportTicketGuid, x.CreatedUtc });
        supportTicketEvent.HasIndex(x => new { x.EventType, x.CreatedUtc });
        supportTicketEvent.HasOne(x => x.SupportTicket).WithMany(x => x.Events)
            .HasForeignKey(x => x.SupportTicketGuid)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
