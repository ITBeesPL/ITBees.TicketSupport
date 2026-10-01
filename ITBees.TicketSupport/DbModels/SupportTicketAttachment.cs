namespace ITBees.TicketSupport.DbModels;

/// <summary>
/// File metadata only. The bytes live wherever the host implementation of
/// <see cref="Interfaces.ISupportTicketAttachmentStore"/> puts them (ITBees.MediaStorage in the existing
/// applications), and <see cref="StorageKey"/> is whatever that store hands back.
/// </summary>
public class SupportTicketAttachment
{
    public Guid Guid { get; set; }

    public Guid SupportTicketGuid { get; set; }
    public SupportTicket SupportTicket { get; set; }

    public Guid? SupportTicketMessageGuid { get; set; }
    public SupportTicketMessage SupportTicketMessage { get; set; }

    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long SizeBytes { get; set; }

    public string StorageKey { get; set; }

    public DateTime CreatedUtc { get; set; }
}
