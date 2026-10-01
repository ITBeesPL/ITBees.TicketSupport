namespace ITBees.TicketSupport.DbModels;

/// <summary>
/// How the requester graded the handling, given in their panel after the desk closed the ticket.
/// One row per ticket, created when the rating is requested.
/// </summary>
public class SupportTicketRating
{
    public Guid Guid { get; set; }

    public Guid SupportTicketGuid { get; set; }
    public SupportTicket SupportTicket { get; set; }

    /// <summary>1-5. Null until the requester answers.</summary>
    public int? Score { get; set; }

    public string Comment { get; set; }

    /// <summary>Agent that closed the ticket, snapshotted so the ranking survives a later reassignment.</summary>
    public Guid? RatedAgentGuid { get; set; }

    public DateTime RequestedUtc { get; set; }
    public DateTime? RatedUtc { get; set; }
}
