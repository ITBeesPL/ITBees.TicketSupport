namespace ITBees.TicketSupport.Configuration;

public sealed class SupportTicketLinkConfiguration
{
    /// <summary>
    /// Deep link to a ticket in the desk panel, carried by the desk alerts. Supports the
    /// <c>{ticketGuid}</c> and <c>{ticketNumber}</c> placeholders.
    /// </summary>
    public string DeskTicketUrlTemplate { get; init; }
}
