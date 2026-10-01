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
}
