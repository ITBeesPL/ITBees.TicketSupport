namespace ITBees.TicketSupport.DbModels;

/// <summary>
/// Append-only audit of what happened to a ticket. Statistics read this rather than the current
/// state of <see cref="SupportTicket"/>, because a reopen would otherwise erase the first handling.
/// </summary>
public class SupportTicketEvent
{
    public Guid Guid { get; set; }

    public Guid SupportTicketGuid { get; set; }
    public SupportTicket SupportTicket { get; set; }

    /// <summary>See <see cref="Abstractions.SupportTicketEventTypes"/>.</summary>
    public string EventType { get; set; }

    public string FromValue { get; set; }
    public string ToValue { get; set; }

    /// <summary>Null for something an automatic rule or a trusted tool did.</summary>
    public Guid? ActorGuid { get; set; }

    public string ActorName { get; set; }

    public DateTime CreatedUtc { get; set; }
}
