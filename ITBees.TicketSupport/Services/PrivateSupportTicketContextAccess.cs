using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.Interfaces;

namespace ITBees.TicketSupport.Services;

/// <summary>Hosts without a shared-resource adapter retain private tickets only.</summary>
public class PrivateSupportTicketContextAccess : ISupportTicketContextAccess
{
    public IReadOnlyCollection<SupportTicketContextVm> GetAvailableContexts() => Array.Empty<SupportTicketContextVm>();
    public SupportTicketContextVm CheckAccess(string contextType, Guid contextGuid, bool forWrite) =>
        throw new FasApiErrorException("Shared ticket contexts are not available", 403);
}
