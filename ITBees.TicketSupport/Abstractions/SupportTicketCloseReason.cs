namespace ITBees.TicketSupport.Abstractions;

/// <summary>
/// Why a ticket was closed. <see cref="Arbitrary"/> exists because an agent may end a thread
/// the requester never confirmed — those rows are exactly the ones worth re-reading next to a
/// low rating, so they are recorded as their own reason rather than folded into "solved".
/// </summary>
public enum SupportTicketCloseReason
{
    Solved = 0,
    Arbitrary = 1,
    Duplicate = 2,
    NoResponse = 3,
    Spam = 4,
    WithdrawnByRequester = 5,

    /// <summary>
    /// The requester closed it from their panel. Not a desk closure, so it never counts towards
    /// the closing figures of a support person.
    /// </summary>
    ClosedByRequester = 6
}
