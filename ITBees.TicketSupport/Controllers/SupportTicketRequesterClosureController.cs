using ITBees.RestfulApiControllers;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.TicketSupport.Controllers;

/// <summary>The requester closes their ticket once the matter is settled.</summary>
[Authorize]
public class SupportTicketRequesterClosureController : RestfulControllerBase<SupportTicketRequesterClosureController>
{
    private readonly ISupportTicketRequesterClosureService _supportTicketRequesterClosureService;

    public SupportTicketRequesterClosureController(ILogger<SupportTicketRequesterClosureController> logger,
        ISupportTicketRequesterClosureService supportTicketRequesterClosureService) : base(logger)
    {
        _supportTicketRequesterClosureService = supportTicketRequesterClosureService;
    }

    [HttpPost]
    [Produces<SupportTicketVm>]
    public IActionResult Post(SupportTicketRequesterClosureIm supportTicketRequesterClosureIm)
    {
        return ReturnOkResult(() => _supportTicketRequesterClosureService.Create(supportTicketRequesterClosureIm),
            supportTicketRequesterClosureIm);
    }
}
