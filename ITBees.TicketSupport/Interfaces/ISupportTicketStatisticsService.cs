using ITBees.TicketSupport.Controllers.Models;

namespace ITBees.TicketSupport.Interfaces;

public interface ISupportTicketStatisticsService
{
    /// <summary>
    /// The whole statistics screen for one period. Defaults to the last 30 days when no range is
    /// given. Requires desk access.
    /// </summary>
    SupportTicketStatisticsVm Get(DateTime? fromUtc, DateTime? toUtc, int lowestRatedCount = 20);
}
