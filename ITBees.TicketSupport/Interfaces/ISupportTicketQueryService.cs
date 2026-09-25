using ITBees.Interfaces.Repository;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Controllers.Models;

namespace ITBees.TicketSupport.Interfaces;

/// <summary>Read side of both panels.</summary>
public interface ISupportTicketQueryService
{
    /// <summary>
    /// The desk queue. Ordered by the newest activity by default, which is what somebody working
    /// the desk wants when a thread has just been answered.
    /// </summary>
    PaginatedResult<SupportTicketListItemVm> GetForDesk(SupportTicketListFilter filter);

    /// <summary>Tickets the current user raised. Nothing else.</summary>
    PaginatedResult<SupportTicketListItemVm> GetMine(SupportTicketListFilter filter);

    /// <summary>Full ticket with the conversation. Internal notes are stripped for a requester.</summary>
    SupportTicketVm GetDetails(Guid supportTicketGuid, bool forRequester = false);

    /// <summary>The audit trail of one ticket - desk side only.</summary>
    List<SupportTicketEventVm> GetEvents(Guid supportTicketGuid);
}

/// <summary>Query parameters shared by both lists.</summary>
public class SupportTicketListFilter
{
    public SupportTicketStatus? Status { get; set; }

    /// <summary>Everything that is not Closed. Convenience for the default desk view.</summary>
    public bool OnlyOpen { get; set; }

    public SupportTicketPriority? Priority { get; set; }
    public Guid? AssignedToGuid { get; set; }
    public bool OnlyUnassigned { get; set; }

    /// <summary>Matches the subject, the ticket number and the requester address.</summary>
    public string Search { get; set; }

    public DateTime? CreatedFromUtc { get; set; }
    public DateTime? CreatedToUtc { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    /// <summary>Column name as the frontend sends it; unknown values fall back to the last activity.</summary>
    public string SortColumn { get; set; }

    public SortOrder SortOrder { get; set; } = SortOrder.Descending;
}
