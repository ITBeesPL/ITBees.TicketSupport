namespace ITBees.TicketSupport.Interfaces;

/// <summary>
/// The posting budget of one requester, checked before anything is written. The default keeps its
/// counters in the memory of the API process: a host running several instances behind a balancer
/// gets the limits once per instance, and can register a shared implementation instead.
/// </summary>
public interface ISupportTicketRateLimiter
{
    /// <summary>
    /// A new ticket. <paramref name="contentLength"/> is the length of the message as posted - the
    /// HTML when there is one. Throws a 429 <c>FasApiErrorException</c> when the budget is used up.
    /// </summary>
    void CheckNewTicket(Guid requesterGuid, int contentLength);

    /// <summary>An answer on an existing ticket; otherwise as <see cref="CheckNewTicket"/>.</summary>
    void CheckMessage(Guid requesterGuid, int contentLength);
}
