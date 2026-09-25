namespace ITBees.TicketSupport.Services;

/// <summary>Input of <see cref="Interfaces.ISupportTicketService.ReplyAsTrustedCaller"/>.</summary>
public class SupportTicketTrustedReplyCommand
{
    public Guid SupportTicketGuid { get; set; }

    /// <summary>Shown as the author of the message, e.g. the name of the assistant.</summary>
    public string AuthorName { get; set; }

    public string Message { get; set; }

    /// <summary>True keeps the message on the desk side; false answers the requester.</summary>
    public bool InternalNote { get; set; }
}
