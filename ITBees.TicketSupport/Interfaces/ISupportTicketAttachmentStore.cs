namespace ITBees.TicketSupport.Interfaces;

/// <summary>
/// Where attachment bytes go. Hosts implement it over ITBees.MediaStorage; the library only
/// keeps the key that comes back.
/// </summary>
public interface ISupportTicketAttachmentStore
{
    /// <summary>Returns the storage key, or null when the file was rejected.</summary>
    string Save(SupportTicketAttachmentContent content);
}

public class SupportTicketAttachmentContent
{
    public Guid SupportTicketGuid { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public byte[] Bytes { get; set; }
}

/// <summary>
/// Default when the host registers no store: attachments are recorded with their metadata and no
/// storage key, so the conversation still says a file was there.
/// </summary>
public sealed class NullSupportTicketAttachmentStore : ISupportTicketAttachmentStore
{
    public string Save(SupportTicketAttachmentContent content) => null;
}
