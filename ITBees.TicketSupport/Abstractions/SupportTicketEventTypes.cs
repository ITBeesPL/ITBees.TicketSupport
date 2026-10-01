namespace ITBees.TicketSupport.Abstractions;

public static class SupportTicketEventTypes
{
    public const string Created = "created";

    public const string Assigned = "assigned";

    public const string StatusChanged = "status_changed";

    public const string PriorityChanged = "priority_changed";

    public const string Replied = "replied";

    public const string MessageReceived = "message_received";

    public const string NoteAdded = "note_added";

    public const string Closed = "closed";

    public const string Reopened = "reopened";

    public const string RatingRequested = "rating_requested";

    public const string Rated = "rated";

    /// <summary>A trusted caller proposed an answer; the actor name is the caller's.</summary>
    public const string DraftProposed = "draft_proposed";

    /// <summary>Support published a proposed answer to the requester; the actor is who published it.</summary>
    public const string DraftPublished = "draft_published";

    /// <summary>Support threw a proposed answer away.</summary>
    public const string DraftDiscarded = "draft_discarded";
}
