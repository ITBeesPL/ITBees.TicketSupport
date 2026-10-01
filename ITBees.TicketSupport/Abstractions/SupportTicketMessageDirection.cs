namespace ITBees.TicketSupport.Abstractions;

public enum SupportTicketMessageDirection
{
    /// <summary>From the requester to the desk.</summary>
    Inbound = 0,

    /// <summary>From the desk to the requester.</summary>
    Outbound = 1,

    /// <summary>Desk-only note. Never rendered to the requester.</summary>
    InternalNote = 2,

    /// <summary>
    /// An answer a trusted caller (an assistant) proposed. Desk-only until support publishes it,
    /// which turns it into <see cref="Outbound"/>; discarding deletes it.
    /// </summary>
    DraftReply = 3
}
