using ITBees.TicketSupport.Abstractions;

namespace ITBees.TicketSupport.DbModels;

public class SupportTicket
{
    public Guid Guid { get; set; }

    public long Number { get; set; }

    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.New;

    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;

    public SupportTicketChannel Channel { get; set; }

    public string Subject { get; set; }

    /// <summary>
    /// Shared resource the ticket belongs to (e.g. a parking). Everybody the host lets into that
    /// resource sees the ticket; null keeps it private to the requester.
    /// </summary>
    public string ContextType { get; set; }

    public Guid? ContextGuid { get; set; }

    public string ContextName { get; set; }

    /// <summary>
    /// The host object the ticket was raised about (e.g. a parking session or a payment), set only
    /// by host code that validated it. Lets the desk and its tools go straight to the evidence.
    /// </summary>
    public string ReferenceType { get; set; }

    public string ReferenceId { get; set; }

    public Guid? RequesterGuid { get; set; }

    public string RequesterEmail { get; set; }

    public string RequesterName { get; set; }

    public Guid? AssignedToGuid { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public DateTime? FirstResponseUtc { get; set; }

    public DateTime? ResolvedUtc { get; set; }

    public DateTime? ClosedUtc { get; set; }

    public SupportTicketCloseReason? CloseReason { get; set; }

    public Guid? ClosedByGuid { get; set; }

    public string CloseNote { get; set; }

    public int ReopenCount { get; set; }

    public bool Deleted { get; set; }

    public ICollection<SupportTicketMessage> Messages { get; set; }

    public ICollection<SupportTicketAttachment> Attachments { get; set; }

    public ICollection<SupportTicketEvent> Events { get; set; }
}
