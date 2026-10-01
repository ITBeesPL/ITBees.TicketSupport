using ITBees.Interfaces.Repository;
using ITBees.RestfulApiControllers;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.TicketSupport.Controllers;

/// <summary>
/// The requester side - whichever panel the host gives the people who report problems: raise a
/// ticket, read your own, answer the desk.
/// </summary>
[Authorize]
public class SupportTicketController : RestfulControllerBase<SupportTicketController>
{
    private readonly ISupportTicketService _supportTicketService;
    private readonly ISupportTicketQueryService _supportTicketQueryService;

    public SupportTicketController(ILogger<SupportTicketController> logger,
        ISupportTicketService supportTicketService, ISupportTicketQueryService supportTicketQueryService)
        : base(logger)
    {
        _supportTicketService = supportTicketService;
        _supportTicketQueryService = supportTicketQueryService;
    }

    [HttpGet]
    [Produces<SupportTicketVm>]
    public IActionResult Get([FromQuery] Guid supportTicketGuid)
    {
        return ReturnOkResult(() => _supportTicketQueryService.GetDetails(supportTicketGuid, true));
    }

    [HttpPost]
    [Produces<SupportTicketVm>]
    public IActionResult Post(SupportTicketIm supportTicketIm)
    {
        return ReturnOkResult(() => _supportTicketService.Create(supportTicketIm), supportTicketIm);
    }

    [HttpPut]
    [Produces<SupportTicketVm>]
    public IActionResult Put(SupportTicketReplyIm supportTicketReplyIm)
    {
        return ReturnOkResult(() => _supportTicketService.ReplyAsRequester(supportTicketReplyIm),
            supportTicketReplyIm);
    }
}

/// <summary>Tickets the signed-in person raised.</summary>
[Authorize]
public class SupportTicketsController : RestfulControllerBase<SupportTicketsController>
{
    private readonly ISupportTicketQueryService _supportTicketQueryService;

    public SupportTicketsController(ILogger<SupportTicketsController> logger,
        ISupportTicketQueryService supportTicketQueryService) : base(logger)
    {
        _supportTicketQueryService = supportTicketQueryService;
    }

    [HttpGet]
    [Produces<PaginatedResult<SupportTicketListItemVm>>]
    public IActionResult Get([FromQuery] SupportTicketStatus? status = null,
        [FromQuery] bool onlyOpen = false, [FromQuery] string search = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        [FromQuery] string sortColumn = null, [FromQuery] SortOrder sortOrder = SortOrder.Descending)
    {
        return ReturnOkResult(() => _supportTicketQueryService.GetMine(new SupportTicketListFilter
        {
            Status = status,
            OnlyOpen = onlyOpen,
            Search = search,
            Page = page,
            PageSize = pageSize,
            SortColumn = sortColumn,
            SortOrder = sortOrder
        }));
    }
}

