using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.Interfaces;
using ITBees.UserManager.Interfaces;

namespace ITBees.TicketSupport.Services;

public class SupportTicketContextsService : ISupportTicketContextsService
{
    private readonly ISupportTicketContextAccess _supportTicketContextAccess;
    private readonly IAspCurrentUserService _aspCurrentUserService;
    public SupportTicketContextsService(ISupportTicketContextAccess supportTicketContextAccess,
        IAspCurrentUserService aspCurrentUserService)
    {
        _supportTicketContextAccess = supportTicketContextAccess;
        _aspCurrentUserService = aspCurrentUserService;
    }
    public IReadOnlyCollection<SupportTicketContextVm> GetAll()
    {
        if (_aspCurrentUserService.GetCurrentUserGuid() == null)
            throw new FasApiErrorException("Sign in to list ticket contexts", 401);
        return _supportTicketContextAccess.GetAvailableContexts();
    }
}
