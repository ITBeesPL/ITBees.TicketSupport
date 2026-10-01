using ITBees.Interfaces.Repository;
using ITBees.RestfulApiControllers;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.TicketSupport.Controllers;

/// <summary>
/// The desk side - whichever panel the host gives its support staff. Every endpoint here goes
/// through ISupportTicketDeskAccess.CheckDeskAccess, so the host decides in one place who works the
/// desk; the [Authorize] attribute only keeps anonymous callers out. No role name is baked in,
/// because "operator" and "administrator" mean different things in different applications.
/// </summary>
[Authorize]
public class SupportTicketDeskQueueController : RestfulControllerBase<SupportTicketDeskQueueController>
{
    private readonly ISupportTicketQueryService _supportTicketQueryService;

    public SupportTicketDeskQueueController(ILogger<SupportTicketDeskQueueController> logger,
        ISupportTicketQueryService supportTicketQueryService) : base(logger)
    {
        _supportTicketQueryService = supportTicketQueryService;
    }

    /// <summary>The open-tickets list: date raised, subject and who raised it, newest activity first.</summary>
    [HttpGet]
    [Produces<PaginatedResult<SupportTicketListItemVm>>]
    public IActionResult Get([FromQuery] SupportTicketStatus? status = null,
        [FromQuery] bool onlyOpen = true, [FromQuery] bool onlyUnassigned = false,
        [FromQuery] Guid? assignedToGuid = null, [FromQuery] SupportTicketPriority? priority = null,
        [FromQuery] string search = null, [FromQuery] DateTime? createdFromUtc = null,
        [FromQuery] DateTime? createdToUtc = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, [FromQuery] string sortColumn = null,
        [FromQuery] SortOrder sortOrder = SortOrder.Descending)
    {
        return ReturnOkResult(() => _supportTicketQueryService.GetForDesk(new SupportTicketListFilter
        {
            Status = status,
            OnlyOpen = onlyOpen,
            OnlyUnassigned = onlyUnassigned,
            AssignedToGuid = assignedToGuid,
            Priority = priority,
            Search = search,
            CreatedFromUtc = createdFromUtc,
            CreatedToUtc = createdToUtc,
            Page = page,
            PageSize = pageSize,
            SortColumn = sortColumn,
            SortOrder = sortOrder
        }));
    }
}

/// <summary>One ticket on the desk: the whole correspondence, status, and tickets taken by phone.</summary>
[Authorize]
public class SupportTicketDeskController : RestfulControllerBase<SupportTicketDeskController>
{
    private readonly ISupportTicketService _supportTicketService;
    private readonly ISupportTicketQueryService _supportTicketQueryService;

    public SupportTicketDeskController(ILogger<SupportTicketDeskController> logger,
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
        return ReturnOkResult(() => _supportTicketQueryService.GetDetails(supportTicketGuid));
    }

    /// <summary>A ticket entered by support, typically after a phone call.</summary>
    [HttpPost]
    [Produces<SupportTicketVm>]
    public IActionResult Post(SupportTicketIm supportTicketIm)
    {
        return ReturnOkResult(() => _supportTicketService.Create(supportTicketIm), supportTicketIm);
    }

    [HttpPut]
    [Produces<SupportTicketVm>]
    public IActionResult Put(SupportTicketStatusUm supportTicketStatusUm)
    {
        return ReturnOkResult(() => _supportTicketService.ChangeStatus(supportTicketStatusUm),
            supportTicketStatusUm);
    }
}

[Authorize]
public class SupportTicketDeskAssignmentController
    : RestfulControllerBase<SupportTicketDeskAssignmentController>
{
    private readonly ISupportTicketService _supportTicketService;

    public SupportTicketDeskAssignmentController(ILogger<SupportTicketDeskAssignmentController> logger,
        ISupportTicketService supportTicketService) : base(logger)
    {
        _supportTicketService = supportTicketService;
    }

    [HttpPut]
    [Produces<SupportTicketVm>]
    public IActionResult Put(SupportTicketAssignUm supportTicketAssignUm)
    {
        return ReturnOkResult(() => _supportTicketService.Assign(supportTicketAssignUm),
            supportTicketAssignUm);
    }
}

/// <summary>Answers and internal notes.</summary>
[Authorize]
public class SupportTicketDeskMessageController : RestfulControllerBase<SupportTicketDeskMessageController>
{
    private readonly ISupportTicketService _supportTicketService;

    public SupportTicketDeskMessageController(ILogger<SupportTicketDeskMessageController> logger,
        ISupportTicketService supportTicketService) : base(logger)
    {
        _supportTicketService = supportTicketService;
    }

    /// <summary>An answer to the requester, shown in their panel.</summary>
    [HttpPost]
    [Produces<SupportTicketVm>]
    public IActionResult Post(SupportTicketReplyIm supportTicketReplyIm)
    {
        return ReturnOkResult(() => _supportTicketService.ReplyAsAgent(supportTicketReplyIm),
            supportTicketReplyIm);
    }

    /// <summary>A note only the desk sees.</summary>
    [HttpPut]
    [Produces<SupportTicketVm>]
    public IActionResult Put(SupportTicketNoteIm supportTicketNoteIm)
    {
        return ReturnOkResult(() => _supportTicketService.AddInternalNote(supportTicketNoteIm),
            supportTicketNoteIm);
    }
}

[Authorize]
public class SupportTicketDeskClosureController : RestfulControllerBase<SupportTicketDeskClosureController>
{
    private readonly ISupportTicketService _supportTicketService;

    public SupportTicketDeskClosureController(ILogger<SupportTicketDeskClosureController> logger,
        ISupportTicketService supportTicketService) : base(logger)
    {
        _supportTicketService = supportTicketService;
    }

    [HttpPost]
    [Produces<SupportTicketVm>]
    public IActionResult Post(SupportTicketCloseIm supportTicketCloseIm)
    {
        return ReturnOkResult(() => _supportTicketService.Close(supportTicketCloseIm), supportTicketCloseIm);
    }

    [HttpPut]
    [Produces<SupportTicketVm>]
    public IActionResult Put(SupportTicketReopenIm supportTicketReopenIm)
    {
        return ReturnOkResult(() => _supportTicketService.Reopen(supportTicketReopenIm),
            supportTicketReopenIm);
    }

}

/// <summary>The audit trail of one ticket.</summary>
[Authorize]
public class SupportTicketDeskEventsController : RestfulControllerBase<SupportTicketDeskEventsController>
{
    private readonly ISupportTicketQueryService _supportTicketQueryService;

    public SupportTicketDeskEventsController(ILogger<SupportTicketDeskEventsController> logger,
        ISupportTicketQueryService supportTicketQueryService) : base(logger)
    {
        _supportTicketQueryService = supportTicketQueryService;
    }

    [HttpGet]
    [Produces<List<SupportTicketEventVm>>]
    public IActionResult Get([FromQuery] Guid supportTicketGuid)
    {
        return ReturnOkResult(() => _supportTicketQueryService.GetEvents(supportTicketGuid));
    }
}

/// <summary>The statistics screen.</summary>
[Authorize]
public class SupportTicketDeskStatisticsController
    : RestfulControllerBase<SupportTicketDeskStatisticsController>
{
    private readonly ISupportTicketStatisticsService _statisticsService;

    public SupportTicketDeskStatisticsController(ILogger<SupportTicketDeskStatisticsController> logger,
        ISupportTicketStatisticsService statisticsService) : base(logger)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet]
    [Produces<SupportTicketStatisticsVm>]
    public IActionResult Get([FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null,
        [FromQuery] int lowestRatedCount = 20)
    {
        return ReturnOkResult(() => _statisticsService.Get(fromUtc, toUtc, lowestRatedCount));
    }
}
