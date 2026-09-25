using ITBees.TicketSupport.DbModels;

namespace ITBees.TicketSupport.Interfaces;

public interface ISupportTicketRatingService
{
    /// <summary>
    /// Creates the rating row the requester answers in their panel. Idempotent - a ticket closed
    /// twice keeps its first request.
    /// </summary>
    void EnsureRatingRequested(Guid supportTicketGuid, Guid? closingAgentGuid);

    /// <summary>
    /// Records the grade. Access is checked by the caller; this only enforces the rating rules
    /// (range, one answer per ticket, closed tickets only).
    /// </summary>
    void Submit(SupportTicket supportTicket, int score, string comment);
}
