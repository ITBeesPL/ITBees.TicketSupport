using ITBees.TicketSupport.Abstractions;

namespace ITBees.TicketSupport.DbModels;

public class SupportTicketMessage
{
    public Guid Guid { get; set; }

    public Guid SupportTicketGuid { get; set; }

    public SupportTicket SupportTicket { get; set; }

    public SupportTicketMessageDirection Direction { get; set; }

    public Guid? AuthorGuid { get; set; }

    public string AuthorName { get; set; }

    public string AuthorEmail { get; set; }

    public string Body { get; set; }

    public string BodyHtml { get; set; }

    public bool IsPublic { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public ICollection<SupportTicketAttachment> Attachments { get; set; }
}
