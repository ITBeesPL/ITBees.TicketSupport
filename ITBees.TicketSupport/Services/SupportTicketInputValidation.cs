using System.Text.RegularExpressions;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Abstractions;

namespace ITBees.TicketSupport.Services;

/// <summary>
/// Everything that can arrive from a form is trimmed and bounded here, so the
/// services below can assume sane values and the columns cannot overflow.
/// </summary>
public static class SupportTicketInputValidation
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s\.]+(\.[^@\s\.]+)+$",
        RegexOptions.Compiled);

    public static bool IsEmailAddress(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= SupportTicketContentLimits.Email &&
        EmailRegex.IsMatch(value.Trim());

    public static string Trim(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }

    public static string RequireText(string value, int maxLength, string fieldName)
    {
        var trimmed = Trim(value, maxLength);
        if (trimmed == null)
            throw new FasApiErrorException($"{fieldName} is required", 400);

        return trimmed;
    }
}
