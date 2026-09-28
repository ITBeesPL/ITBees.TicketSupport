using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.Services;

namespace ITBees.TicketSupport.Interfaces;

public interface ISupportTicketService
{
    /// <summary>
    /// A new ticket from a panel, raised by the signed-in user for themselves - the requester fields
    /// of the payload are ignored. <paramref name="reference"/> is for host code only: it links the
    /// ticket to the host object it was raised about, after the host validated that object.
    /// </summary>
    SupportTicketVm Create(SupportTicketIm supportTicketIm, SupportTicketReference reference = null);

    /// <summary>
    /// A ticket entered by support, typically after a phone call, for the person named by
    /// <see cref="SupportTicketIm.RequesterEmail"/>. Requires desk access.
    /// </summary>
    SupportTicketVm CreateFromDesk(SupportTicketIm supportTicketIm, SupportTicketReference reference = null);

    SupportTicketVm ReplyAsRequester(SupportTicketReplyIm supportTicketReplyIm);

    SupportTicketVm ReplyAsAgent(SupportTicketReplyIm supportTicketReplyIm);

    SupportTicketVm AddInternalNote(SupportTicketNoteIm supportTicketNoteIm);

    /// <summary>
    /// A desk answer or internal note written by a caller that is not a signed-in person - an
    /// assistant working through the host's MCP tools. No controller reaches this; the host decides
    /// who may call it.
    /// </summary>
    SupportTicketVm ReplyAsTrustedCaller(SupportTicketTrustedReplyCommand command);

    SupportTicketVm Assign(SupportTicketAssignUm supportTicketAssignUm);

    SupportTicketVm ChangeStatus(SupportTicketStatusUm supportTicketStatusUm);

    SupportTicketVm Close(SupportTicketCloseIm supportTicketCloseIm);

    SupportTicketVm Reopen(SupportTicketReopenIm supportTicketReopenIm);
}
