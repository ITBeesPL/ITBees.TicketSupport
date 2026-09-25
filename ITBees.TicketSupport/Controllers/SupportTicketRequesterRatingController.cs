using ITBees.RestfulApiControllers;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.TicketSupport.Controllers;

[Authorize]
public class SupportTicketRequesterRatingController : RestfulControllerBase<SupportTicketRequesterRatingController>
{
    private readonly ISupportTicketRequesterRatingService _supportTicketRequesterRatingService;

    public SupportTicketRequesterRatingController(ILogger<SupportTicketRequesterRatingController> logger,
        ISupportTicketRequesterRatingService supportTicketRequesterRatingService) : base(logger)
    {
        _supportTicketRequesterRatingService = supportTicketRequesterRatingService;
    }

    [HttpPost]
    [Produces<SupportTicketVm>]
    public IActionResult Post(SupportTicketRequesterRatingIm supportTicketRequesterRatingIm)
    {
        return ReturnOkResult(() => _supportTicketRequesterRatingService.Create(supportTicketRequesterRatingIm),
            supportTicketRequesterRatingIm);
    }
}
