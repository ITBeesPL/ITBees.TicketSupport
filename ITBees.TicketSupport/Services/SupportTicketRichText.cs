using AngleSharp.Html.Parser;
using Ganss.Xss;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Abstractions;

namespace ITBees.TicketSupport.Services;

public static class SupportTicketRichText
{
    // Only pictures carried inside the message itself. A remote address would be fetched by the
    // browser of everybody who opens the ticket - support staff included - which turns a message
    // into a tracking pixel (IP address, time of reading) or into a GET request against whatever the
    // reader's browser can reach. The panels' editors embed pictures as data URIs anyway.
    private static readonly string[] InlineRasterImagePrefixes =
    {
        "data:image/png;base64,", "data:image/jpeg;base64,", "data:image/gif;base64,", "data:image/webp;base64,"
    };

    public static (string Body, string Html) Normalize(string body, string html)
    {
        if (html?.Length > SupportTicketContentLimits.BodyHtml || body?.Length > SupportTicketContentLimits.Body)
            throw new FasApiErrorException("Message is too large", 400);

        if (string.IsNullOrWhiteSpace(html))
            return (SupportTicketInputValidation.RequireText(body, SupportTicketContentLimits.Body, "Message"), null);

        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(new[] { "p", "br", "strong", "b", "em", "i", "u", "s", "ol", "ul", "li", "blockquote", "h1", "h2", "h3", "a", "img", "table", "thead", "tbody", "tr", "th", "td", "span" });
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(new[] { "href", "src", "alt", "title", "colspan", "rowspan", "data-row" });
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(new[] { "https", "http", "mailto", "data" });
        var document = new HtmlParser().ParseDocument(sanitizer.Sanitize(html));

        foreach (var link in document.QuerySelectorAll("a"))
        {
            var href = link.GetAttribute("href");
            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("https" or "http" or "mailto"))
                link.RemoveAttribute("href");
        }

        foreach (var img in document.QuerySelectorAll("img"))
        {
            var src = img.GetAttribute("src") ?? "";
            if (!InlineRasterImagePrefixes.Any(prefix => src.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                img.Remove();
        }

        var safeHtml = document.Body.InnerHtml;
        // Serializing escapes what the parser read as text ("&" becomes "&amp;"), so the stored markup
        // can be several times longer than the input the first check measured.
        if (safeHtml.Length > SupportTicketContentLimits.BodyHtml)
            throw new FasApiErrorException("Message is too large", 400);
        // Derive searchable plain text on the server; never trust a separate client preview.
        foreach (var br in document.QuerySelectorAll("br"))
            br.Replace(document.CreateTextNode("\n"));
        foreach (var block in document.QuerySelectorAll("p,li,tr,h1,h2,h3,blockquote"))
            block.AppendChild(document.CreateTextNode("\n"));
        var text = document.Body.TextContent.Trim();
        if (text.Length > SupportTicketContentLimits.Body)
            throw new FasApiErrorException("Message text is too long", 400);
        if (text.Length == 0 && document.QuerySelector("img") != null)
            text = "[Zdjęcie]";
        if (string.IsNullOrWhiteSpace(text))
            throw new FasApiErrorException("Message is required", 400);

        return (text, safeHtml);
    }
}
