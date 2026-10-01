using ITBees.TicketSupport.Configuration;
using ITBees.TicketSupport.Interfaces;

namespace ITBees.TicketSupport.Services;

/// <summary>
/// Expands the URL template from the configuration. A missing template yields no link, and the
/// alert simply goes out without it.
/// </summary>
public class ConfiguredSupportTicketLinkBuilder : ISupportTicketLinkBuilder
{
    private readonly SupportTicketLinkConfiguration _supportTicketLinkConfiguration;

    public ConfiguredSupportTicketLinkBuilder(SupportTicketConfiguration supportTicketConfiguration)
    {
        _supportTicketLinkConfiguration = supportTicketConfiguration.Links;
    }

    public string BuildDeskTicketLink(long ticketNumber, Guid ticketGuid)
    {
        var template = _supportTicketLinkConfiguration?.DeskTicketUrlTemplate;
        if (string.IsNullOrWhiteSpace(template))
            return null;

        return template
            .Replace("{ticketGuid}", ticketGuid.ToString())
            .Replace("{ticketNumber}", ticketNumber.ToString());
    }
}
