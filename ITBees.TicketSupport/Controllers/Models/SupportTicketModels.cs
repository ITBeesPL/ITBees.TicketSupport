using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.DbModels;
using RestIm = ITBees.RestClient.Interfaces.RestModelMarkup.Im;
using RestUm = ITBees.RestClient.Interfaces.RestModelMarkup.Um;
using RestVm = ITBees.RestClient.Interfaces.RestModelMarkup.Vm;

namespace ITBees.TicketSupport.Controllers.Models;

/// <summary>
/// One row of the desk queue and of the "my tickets" list. The queue is read at a glance, so the
/// three things the list exists for - when it was opened, what it says and who reported it - are
/// the first fields on it.
/// </summary>
public class SupportTicketListItemVm : RestVm
{
    public SupportTicketListItemVm() { }

    public SupportTicketListItemVm(SupportTicket supportTicket)
    {
        Guid = supportTicket.Guid;
        Number = supportTicket.Number;
        CreatedUtc = SupportTicketDates.Utc(supportTicket.CreatedUtc);
        Subject = supportTicket.Subject;
        ContextType = supportTicket.ContextType;
        ContextGuid = supportTicket.ContextGuid;
        ContextName = supportTicket.ContextName;
        ReferenceType = supportTicket.ReferenceType;
        ReferenceId = supportTicket.ReferenceId;
        RequesterGuid = supportTicket.RequesterGuid;
        RequesterEmail = supportTicket.RequesterEmail;
        RequesterName = supportTicket.RequesterName;
        Status = supportTicket.Status;
        Priority = supportTicket.Priority;
        Channel = supportTicket.Channel;
        AssignedToGuid = supportTicket.AssignedToGuid;
        UpdatedUtc = SupportTicketDates.Utc(supportTicket.UpdatedUtc);
        FirstResponseUtc = SupportTicketDates.Utc(supportTicket.FirstResponseUtc);
        ClosedUtc = SupportTicketDates.Utc(supportTicket.ClosedUtc);
        ReopenCount = supportTicket.ReopenCount;
    }

    public Guid Guid { get; set; }
    public long Number { get; set; }

    /// <summary>When the ticket was opened - the column the desk list is read by.</summary>
    public DateTime CreatedUtc { get; set; }

    public string Subject { get; set; }
    public string ContextType { get; set; }
    public Guid? ContextGuid { get; set; }
    public string ContextName { get; set; }

    /// <summary>The host object the ticket was raised about, e.g. <c>parking_session</c> / <c>477660</c>.</summary>
    public string ReferenceType { get; set; }

    public string ReferenceId { get; set; }
    public Guid? RequesterGuid { get; set; }
    public string RequesterEmail { get; set; }
    public string RequesterName { get; set; }

    public SupportTicketStatus Status { get; set; }
    public SupportTicketPriority Priority { get; set; }
    public SupportTicketChannel Channel { get; set; }
    public Guid? AssignedToGuid { get; set; }
    public string AssignedToName { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime? FirstResponseUtc { get; set; }
    public DateTime? ClosedUtc { get; set; }
    public int ReopenCount { get; set; }

    /// <summary>Score once the requester answered, null otherwise. Shown next to closed rows.</summary>
    public int? RatingScore { get; set; }

    /// <summary>Messages in the conversation so far, so the list can show "3 replies".</summary>
    public int MessageCount { get; set; }
}

/// <summary>One ticket with its whole conversation - the detail view of both panels.</summary>
public class SupportTicketVm : SupportTicketListItemVm
{
    public SupportTicketVm() { }

    public SupportTicketVm(SupportTicket supportTicket) : base(supportTicket)
    {
        CloseReason = supportTicket.CloseReason;
        CloseNote = supportTicket.CloseNote;
        ClosedByGuid = supportTicket.ClosedByGuid;
        ResolvedUtc = SupportTicketDates.Utc(supportTicket.ResolvedUtc);
    }

    public SupportTicketCloseReason? CloseReason { get; set; }
    public string CloseNote { get; set; }
    public Guid? ClosedByGuid { get; set; }
    public string ClosedByName { get; set; }
    public DateTime? ResolvedUtc { get; set; }

    /// <summary>The shared correspondence, oldest first. Internal notes only for the desk.</summary>
    public List<SupportTicketMessageVm> Messages { get; set; } = new();

    public List<SupportTicketAttachmentVm> Attachments { get; set; } = new();
    public List<SupportTicketEventVm> Events { get; set; } = new();
    public SupportTicketRatingVm Rating { get; set; }
    public bool CanRate { get; set; }
}

/// <summary>A new ticket raised from a panel, or entered by support after a phone call.</summary>
public class SupportTicketIm : RestIm
{
    public string ContextType { get; set; }
    public Guid? ContextGuid { get; set; }
    public string MessageHtml { get; set; }
    public string Subject { get; set; }
    public string Message { get; set; }
    public SupportTicketPriority? Priority { get; set; }

    /// <summary>
    /// Read only by the desk endpoint (<c>ISupportTicketService.CreateFromDesk</c>), for a ticket taken
    /// over the phone. A requester filing their own ticket is identified from the session, never from
    /// the payload - even when the requester is support staff using the requester endpoint.
    /// </summary>
    public string RequesterEmail { get; set; }

    public string RequesterName { get; set; }
}

public class SupportTicketReplyIm : RestIm
{
    public string MessageHtml { get; set; }
    public Guid SupportTicketGuid { get; set; }
    public string Message { get; set; }
}

public class SupportTicketNoteIm : RestIm
{
    public string MessageHtml { get; set; }
    public Guid SupportTicketGuid { get; set; }
    public string Message { get; set; }
}

/// <summary>A drafted answer the desk publishes or discards.</summary>
public class SupportTicketDraftIm : RestIm
{
    public Guid SupportTicketGuid { get; set; }

    /// <summary>The draft message, as the desk view of the ticket lists it.</summary>
    public Guid SupportTicketMessageGuid { get; set; }
}

public class SupportTicketAssignUm : RestUm
{
    public Guid SupportTicketGuid { get; set; }

    /// <summary>Null hands the ticket back to the unassigned desk inbox.</summary>
    public Guid? AssignedToGuid { get; set; }
}

public class SupportTicketStatusUm : RestUm
{
    public Guid SupportTicketGuid { get; set; }
    public SupportTicketStatus Status { get; set; }
    public SupportTicketPriority? Priority { get; set; }
}

public class SupportTicketCloseIm : RestIm
{
    public Guid SupportTicketGuid { get; set; }
    public SupportTicketCloseReason CloseReason { get; set; } = SupportTicketCloseReason.Solved;

    /// <summary>Required for an arbitrary close - it is the text somebody reads after a low rating.</summary>
    public string CloseNote { get; set; }

    /// <summary>Ask the requester to rate the handling in their panel. Off for spam and duplicates.</summary>
    public bool AskForRating { get; set; } = true;
}
