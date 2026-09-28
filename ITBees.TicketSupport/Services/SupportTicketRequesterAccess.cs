using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;
using ITBees.UserManager.Interfaces;

namespace ITBees.TicketSupport.Services;

public class SupportTicketRequesterAccess
{
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly ISupportTicketContextAccess _supportTicketContextAccess;

    public SupportTicketRequesterAccess(IAspCurrentUserService aspCurrentUserService,
        ISupportTicketContextAccess supportTicketContextAccess)
    {
        _aspCurrentUserService = aspCurrentUserService;
        _supportTicketContextAccess = supportTicketContextAccess;
    }

    public SupportTicketContextVm CheckContext(string contextType, Guid? contextGuid, bool forWrite)
    {
        if (contextType == null && contextGuid == null) return null;
        if (string.IsNullOrWhiteSpace(contextType) || contextType.Length > 64 ||
            !contextGuid.HasValue || contextGuid == Guid.Empty)
            throw new FasApiErrorException("Context type and identifier must be provided together", 400);
        if (_aspCurrentUserService.GetCurrentUserGuid() == null)
            throw new FasApiErrorException("Sign in to access shared tickets", 401);

        // The adapter is host code. Anything but the context that was asked for - null included - is a
        // denial: Check below ignores the value, so a null would otherwise read as "allowed", and on
        // create it would quietly turn a shared ticket into a private one.
        var context = _supportTicketContextAccess.CheckAccess(contextType, contextGuid.Value, forWrite);
        if (context == null || context.Guid != contextGuid.Value ||
            !string.Equals(context.Type, contextType, StringComparison.OrdinalIgnoreCase))
            throw new FasApiErrorException("You do not have access to this ticket context", 403);

        return context;
    }

    public void Check(SupportTicket ticket, bool forWrite)
    {
        var currentUserGuid = _aspCurrentUserService.GetCurrentUserGuid()
                              ?? throw new FasApiErrorException("Sign in to access tickets", 401);
        if (ticket.ContextType != null || ticket.ContextGuid != null)
        {
            CheckContext(ticket.ContextType, ticket.ContextGuid, forWrite);
            return;
        }
        if (ticket.RequesterGuid != currentUserGuid)
            throw new FasApiErrorException("This ticket belongs to somebody else", 403);
    }

    public IQueryable<SupportTicket> Filter(IQueryable<SupportTicket> tickets)
    {
        var currentUserGuid = _aspCurrentUserService.GetCurrentUserGuid()
                              ?? throw new FasApiErrorException("Sign in to list tickets", 401);
        var visible = tickets.Where(x => x.ContextType == null && x.ContextGuid == null && x.RequesterGuid == currentUserGuid);
        // Disjoint branches preserve database-side filtering, counts and pagination.
        foreach (var group in _supportTicketContextAccess.GetAvailableContexts().GroupBy(x => x.Type))
        {
            var type = group.Key;
            if (string.IsNullOrWhiteSpace(type)) continue;
            var guids = group.Select(x => x.Guid).Where(x => x != Guid.Empty).Distinct().ToList();
            visible = visible.Concat(tickets.Where(x => x.ContextType == type && x.ContextGuid.HasValue && guids.Contains(x.ContextGuid.Value)));
        }
        return visible;
    }
}
