using ITBees.TicketSupport.Abstractions;

namespace ITBees.TicketSupport.Services;

public class SupportTicketMessageCommand
{
    public SupportTicketMessageDirection Direction { get; set; }

    public Guid? AuthorGuid { get; set; }

    public string AuthorName { get; set; }

    public string AuthorEmail { get; set; }

    public string Body { get; set; }

    public string BodyHtml { get; set; }
}
