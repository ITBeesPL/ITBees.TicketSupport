using ITBees.Alerts.Abstractions;
using ITBees.Alerts.Interfaces;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;
using Microsoft.Extensions.Logging;

namespace ITBees.TicketSupport.Notifications;

/// <summary>Raises desk events through ITBees.Alerts, so the host's alerting rules decide who hears about them.</summary>
public class AlertsSupportTicketNotifier : ISupportTicketNotifier
{
    private readonly IAlertPublisher _alertPublisher;
    private readonly ISupportTicketLinkBuilder _supportTicketLinkBuilder;
    private readonly ILogger<AlertsSupportTicketNotifier> _logger;

    public AlertsSupportTicketNotifier(IAlertPublisher alertPublisher,
        ISupportTicketLinkBuilder supportTicketLinkBuilder, ILogger<AlertsSupportTicketNotifier> logger)
    {
        _alertPublisher = alertPublisher;
        _supportTicketLinkBuilder = supportTicketLinkBuilder;
        _logger = logger;
    }

    public void TicketCreated(SupportTicket supportTicket)
    {
        Raise(SupportTicketAlertKeys.TicketCreated, supportTicket, AlertSeverity.Info);
    }

    public void RequesterReplied(SupportTicket supportTicket, SupportTicketMessage message)
    {
        Raise(SupportTicketAlertKeys.RequesterReplied, supportTicket, AlertSeverity.Info);
    }

    public void TicketClosed(SupportTicket supportTicket)
    {
        Raise(SupportTicketAlertKeys.TicketClosed, supportTicket, AlertSeverity.Info,
            alertEvent => alertEvent.With("reason", supportTicket.CloseReason?.ToString()));
    }

    public void LowRatingReceived(SupportTicket supportTicket, int score, string comment)
    {
        Raise(SupportTicketAlertKeys.LowRating, supportTicket, AlertSeverity.Warning,
            alertEvent => alertEvent.With("score", score).With("comment", comment));
    }

    private void Raise(string key, SupportTicket supportTicket, AlertSeverity severity,
        Action<AlertEvent> enrich = null)
    {
        try
        {
            var alertEvent = new AlertEvent(key, AlertScope.Global)
            {
                Severity = severity,
                SourceId = supportTicket.Number.ToString(),
                SourceName = supportTicket.Subject,
                Link = _supportTicketLinkBuilder.BuildDeskTicketLink(supportTicket.Number, supportTicket.Guid)
            };
            alertEvent
                .With("number", supportTicket.Number)
                .With("subject", supportTicket.Subject)
                .With("requester", supportTicket.RequesterName ?? supportTicket.RequesterEmail);
            enrich?.Invoke(alertEvent);
            _alertPublisher.RaiseAsync(alertEvent);
        }
        catch (Exception exception)
        {
            // An alert is worth less than the ticket it announces; never fail the caller over it.
            _logger.LogWarning(exception, "Ticket alert {Key} for ticket {Number} could not be raised", key,
                supportTicket.Number);
        }
    }
}
