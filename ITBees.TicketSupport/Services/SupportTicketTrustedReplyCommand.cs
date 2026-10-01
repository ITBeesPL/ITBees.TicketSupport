namespace ITBees.TicketSupport.Services;

/// <summary>Input of <see cref="Interfaces.ISupportTicketService.ReplyAsTrustedCaller"/>.</summary>
public class SupportTicketTrustedReplyCommand
{
    public Guid SupportTicketGuid { get; set; }

    /// <summary>Shown as the author of the message, e.g. the name of the assistant.</summary>
    public string AuthorName { get; set; }

    public string Message { get; set; }

    /// <summary>
    /// What the message becomes. The default is a draft, so a caller that forgets to choose never
    /// reaches the requester without support reading it first.
    /// </summary>
    public SupportTicketTrustedMessageKind Kind { get; set; } = SupportTicketTrustedMessageKind.DraftReply;
}

public enum SupportTicketTrustedMessageKind
{
    /// <summary>An answer support reads and then publishes or discards. The requester sees nothing yet.</summary>
    DraftReply = 0,

    /// <summary>A note for the desk only.</summary>
    InternalNote = 1,

    /// <summary>An answer published to the requester straight away.</summary>
    Reply = 2
}
