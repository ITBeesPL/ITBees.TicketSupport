using ITBees.RestfulApiControllers;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.TicketSupport.Controllers;

[Authorize]
public class SupportTicketContextsController : RestfulControllerBase<SupportTicketContextsController>
{
    private readonly ISupportTicketContextsService _supportTicketContextsService;
    public SupportTicketContextsController(ILogger<SupportTicketContextsController> logger,
        ISupportTicketContextsService supportTicketContextsService) : base(logger)
    {
        _supportTicketContextsService = supportTicketContextsService;
    }
    [HttpGet]
    [Produces<List<SupportTicketContextVm>>]
    public IActionResult Get() => ReturnOkResult(() => _supportTicketContextsService.GetAll());
}
