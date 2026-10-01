using ITBees.TicketSupport.Abstractions;

namespace ITBees.TicketSupport.Services;

public class SupportTicketCreateCommand
{
    public string ContextType { get; set; }

    public Guid? ContextGuid { get; set; }

    public string ContextName { get; set; }

    public string ReferenceType { get; set; }

    public string ReferenceId { get; set; }

    public string Subject { get; set; }

    public SupportTicketPriority? Priority { get; set; }

    public SupportTicketChannel Channel { get; set; }

    public Guid? RequesterGuid { get; set; }

    public string RequesterEmail { get; set; }

    public string RequesterName { get; set; }

    public Guid? ActorGuid { get; set; }

    public string ActorName { get; set; }
}
