using ITBees.Alerts.Abstractions;
using ITBees.Alerts.Catalog;

namespace ITBees.TicketSupport.Notifications;

public class SupportTicketAlertCatalogSource : IAlertCatalogSource
{
    public IEnumerable<AlertDefinition> GetDefinitions()
    {
        yield return new AlertDefinition
        {
            Key = SupportTicketAlertKeys.TicketCreated,
            Category = "support",
            DisplayName = "New support ticket",
            Description = "Somebody raised a ticket in the panel.",
            ScopeKind = "global",
            DefaultSeverity = AlertSeverity.Info,
            SuggestedChannels = AlertChannels.InApp,
            DefaultTitleTemplate = "New ticket {number}",
            DefaultMessageTemplate = "{subject} - {requester}"
        };
        yield return new AlertDefinition
        {
            Key = SupportTicketAlertKeys.RequesterReplied,
            Category = "support",
            DisplayName = "Requester replied",
            Description = "The person who reported the problem answered and the ticket is waiting again.",
            ScopeKind = "global",
            DefaultSeverity = AlertSeverity.Info,
            SuggestedChannels = AlertChannels.InApp,
            DefaultTitleTemplate = "Reply on ticket {number}",
            DefaultMessageTemplate = "{subject}"
        };
        yield return new AlertDefinition
        {
            Key = SupportTicketAlertKeys.TicketClosed,
            Category = "support",
            DisplayName = "Ticket closed",
            Description = "A ticket was closed - by support, or by the requester in their panel.",
            ScopeKind = "global",
            DefaultSeverity = AlertSeverity.Info,
            SuggestedChannels = AlertChannels.InApp,
            DefaultTitleTemplate = "Closed ticket {number}",
            DefaultMessageTemplate = "{subject} - {reason}"
        };
        yield return new AlertDefinition
        {
            Key = SupportTicketAlertKeys.LowRating,
            Category = "support",
            DisplayName = "Low satisfaction rating",
            Description = "The requester graded the handling poorly - worth re-reading that conversation.",
            ScopeKind = "global",
            DefaultSeverity = AlertSeverity.Warning,
            SuggestedChannels = AlertChannels.InApp,
            DefaultTitleTemplate = "Rating {score}/5 on ticket {number}",
            DefaultMessageTemplate = "{subject} - {comment}"
        };
    }
}
