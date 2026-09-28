using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Services;
using NUnit.Framework;

namespace ITBees.TicketSupport.Tests;

public class SupportTicketRichTextTests
{
    [Test]
    public void KeepsTableAndPhotoWhileRemovingExecutableMarkup()
    {
        var value = SupportTicketRichText.Normalize("forged preview", "<p onclick='alert(1)'>Awaria</p>" +
            "<script>alert(2)</script><table><tbody><tr><td data-row='one'>Brama 1</td></tr></tbody></table>" +
            "<img src='data:image/png;base64,iVBORw0KGgo=' onerror='alert(3)'>" +
            "<a href='javascript:alert(4)'>link</a><a href='data:text/html,test'>link2</a>");
        Assert.Multiple(() =>
        {
            Assert.That(value.Html, Does.Contain("<table>"));
            Assert.That(value.Html, Does.Contain("data-row=\"one\""));
            Assert.That(value.Html, Does.Contain("data:image/png;base64,"));
            Assert.That(value.Html, Does.Not.Contain("onclick").And.Not.Contain("onerror").And.Not.Contain("<script"));
            Assert.That(value.Html, Does.Not.Contain("javascript:").And.Not.Contain("data:text/html"));
            Assert.That(value.Body, Does.Contain("Awaria").And.Contain("Brama 1").And.Not.Contain("forged preview"));
        });
    }

    [TestCase("<p><br></p>")]
    [TestCase("<p>&nbsp; </p>")]
    [TestCase("<img src='data:image/svg+xml;base64,PHN2Zz4='>")]
    public void RejectsEmptyOrUnsupportedImageOnlyContent(string html) =>
        Assert.Throws<FasApiErrorException>(() => SupportTicketRichText.Normalize("forged", html));

    [Test]
    public void AcceptsAnImageOnlyMessage() => Assert.That(
        SupportTicketRichText.Normalize(null, "<p><img src='data:image/png;base64,iVBORw0KGgo='></p>").Body,
        Is.EqualTo("[Zdjęcie]"));

    [Test]
    public void RejectsOversizedHtmlInsteadOfTruncatingAnImage() =>
        Assert.Throws<FasApiErrorException>(() => SupportTicketRichText.Normalize("test", new string('x', SupportTicketContentLimits.BodyHtml + 1)));

    [TestCase("https://tracker.example.test/pixel.png")]
    [TestCase("http://192.168.1.1/cgi-bin/reboot")]
    [TestCase("//tracker.example.test/pixel.png")]
    public void DropsImagesFetchedFromElsewhere(string src)
    {
        var value = SupportTicketRichText.Normalize(null, $"<p>Brama <img src='{src}'></p>");
        Assert.Multiple(() =>
        {
            Assert.That(value.Html, Does.Not.Contain("<img"));
            Assert.That(value.Html, Does.Not.Contain(src));
            Assert.That(value.Body, Is.EqualTo("Brama"));
        });
    }

    [Test]
    public void RejectsAMessageMadeOnlyOfARemoteImage() =>
        Assert.Throws<FasApiErrorException>(() =>
            SupportTicketRichText.Normalize(null, "<p><img src='https://tracker.example.test/pixel.png'></p>"));

    [Test]
    public void RejectsMarkupThatGrowsPastTheLimitWhenSerialized()
    {
        // 2 M characters in, "&" -> "&amp;" makes it 10 M out - over the 8 MB the column and the panels expect.
        var html = "<p title=\"" + new string('&', 2_000_000) + "\">x</p>";
        Assert.That(html.Length, Is.LessThan(SupportTicketContentLimits.BodyHtml));
        var exception = Assert.Throws<FasApiErrorException>(() => SupportTicketRichText.Normalize(null, html));
        Assert.That(exception!.Message, Is.EqualTo("Message is too large"));
    }

    [Test]
    public void PreservesLegacyPlainMessages() => Assert.That(
        SupportTicketRichText.Normalize("Pierwsza linia\nDruga linia", null).Body, Is.EqualTo("Pierwsza linia\nDruga linia"));
}
