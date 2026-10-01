namespace ITBees.TicketSupport.Abstractions;

/// <summary>How the ticket reached the desk.</summary>
public enum SupportTicketChannel
{
    /// <summary>Raised by the requester in their panel.</summary>
    Panel = 0,

    /// <summary>Entered by support on behalf of somebody, typically after a phone call.</summary>
    Phone = 1,

    /// <summary>Created by the host application itself.</summary>
    Api = 2
}
