using ITBees.Alerts.Catalog;
using ITBees.TicketSupport.Configuration;
using ITBees.TicketSupport.Interfaces;
using ITBees.TicketSupport.Notifications;
using ITBees.TicketSupport.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ITBees.TicketSupport.Setup;

public class SupportTicketSetup
{
    public static void Register(IServiceCollection services, SupportTicketConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddSingleton(configuration);
        services.AddScoped<SupportTicketWriter>();
        services.AddScoped<SupportTicketViewMapper>();
        services.AddScoped<SupportTicketRequesterAccess>();
        services.TryAddScoped<ISupportTicketContextAccess, PrivateSupportTicketContextAccess>();
        services.AddScoped<ISupportTicketContextsService, SupportTicketContextsService>();
        services.AddScoped<ISupportTicketNumberGenerator, SupportTicketNumberGenerator>();
        services.AddScoped<ISupportTicketService, SupportTicketService>();
        services.AddScoped<ISupportTicketQueryService, SupportTicketQueryService>();
        services.AddScoped<ISupportTicketRatingService, SupportTicketRatingService>();
        services.AddScoped<ISupportTicketRequesterRatingService, SupportTicketRequesterRatingService>();
        services.AddScoped<ISupportTicketRequesterClosureService, SupportTicketRequesterClosureService>();
        services.AddScoped<ISupportTicketStatisticsService, SupportTicketStatisticsService>();
        services.TryAddScoped<ISupportTicketLinkBuilder, ConfiguredSupportTicketLinkBuilder>();
        services.TryAddScoped<ISupportTicketAttachmentStore, NullSupportTicketAttachmentStore>();
        services.TryAddScoped<ISupportTicketNotifier, NullSupportTicketNotifier>();
        // One instance per process: the buckets must outlive the request that fills them.
        services.TryAddSingleton<ISupportTicketRateLimiter, InMemorySupportTicketRateLimiter>();
    }

    /// <summary>Routes desk events through ITBees.Alerts instead of dropping them.</summary>
    public static void AddAlertsNotifications(IServiceCollection services)
    {
        services.RemoveAll<ISupportTicketNotifier>();
        services.AddScoped<ISupportTicketNotifier, AlertsSupportTicketNotifier>();
        services.AddSingleton<IAlertCatalogSource, SupportTicketAlertCatalogSource>();
    }
}
