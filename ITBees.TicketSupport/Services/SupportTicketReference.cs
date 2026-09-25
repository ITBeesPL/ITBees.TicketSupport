namespace ITBees.TicketSupport.Services;

/// <summary>
/// The host object a ticket was raised about, e.g. <c>parking_session</c> / <c>477660</c>. Only
/// host code sets it, after checking the caller may see that object - it never comes from a
/// request payload.
/// </summary>
public class SupportTicketReference
{
    public SupportTicketReference() { }

    public SupportTicketReference(string type, string id)
    {
        Type = type;
        Id = id;
    }

    public string Type { get; set; }

    public string Id { get; set; }
}
