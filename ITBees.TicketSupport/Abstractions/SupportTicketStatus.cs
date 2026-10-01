namespace ITBees.TicketSupport.Abstractions;

/// <summary>
/// Life cycle of one ticket. The ordering matters only for reporting; transitions are guarded
/// by <see cref="Services.SupportTicketService"/>, not by the numeric value.
/// </summary>
public enum SupportTicketStatus
{
    /// <summary>Created, nobody from the desk has touched it yet.</summary>
    New = 0,

    /// <summary>Assigned or answered at least once, work in progress.</summary>
    Open = 1,

    /// <summary>The desk answered and is waiting for the requester.</summary>
    WaitingForCustomer = 2,

    /// <summary>The requester answered and is waiting for the desk.</summary>
    WaitingForAgent = 3,

    /// <summary>Answer delivered, waiting out the period in which the requester may reopen.</summary>
    Resolved = 4,

    /// <summary>Finished. Only a reopen moves it out of this state.</summary>
    Closed = 5
}
