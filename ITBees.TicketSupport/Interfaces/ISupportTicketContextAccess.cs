using ITBees.TicketSupport.Controllers.Models;

namespace ITBees.TicketSupport.Interfaces;

/// <summary>The host supplies currently accessible shared resources; unknown resources must be denied.</summary>
public interface ISupportTicketContextAccess
{
    IReadOnlyCollection<SupportTicketContextVm> GetAvailableContexts();
    SupportTicketContextVm CheckAccess(string contextType, Guid contextGuid, bool forWrite);
}
