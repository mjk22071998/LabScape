using System.Text;

namespace LabScape.Core.Helpers;

/// <summary>
/// Helper for formatting and wrapping labels to ensure text exceeding UI boundaries
/// flows onto the next line without clipping or truncation.
/// </summary>
public static class TextWrapHelper
{
    public const char SoftBreak = '\u200B';

    /// <summary>
    /// Inserts zero-width spaces (\u200B) as soft-break opportunities for UI elements with TextWrapping="Wrap",
    /// ensuring long PC names, names with underscores, slashes, or long uninterrupted alphanumeric tokens
    /// wrap onto the next line when reaching the item boundary, while keeping the whole text visible without truncation.
    /// </summary>
    public static string InsertSoftBreakOpportunities(string? text, int maxChunkLength = 7)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length + 16);
        int chunkLen = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            sb.Append(c);
            chunkLen++;

            // Soft-break after separators that don't trigger native word breaks in some UI toolkits
            if (c is '_' or '.' or '/' or '\\' or ':' or ';' or '|' or '@')
            {
                if (i + 1 < text.Length && text[i + 1] != SoftBreak)
                {
                    sb.Append(SoftBreak);
                }
                chunkLen = 0;
            }
            else if (char.IsWhiteSpace(c) || c == '-')
            {
                // Native word-break boundary
                chunkLen = 0;
            }
            else if (chunkLen >= maxChunkLength && i + 1 < text.Length && !char.IsWhiteSpace(text[i + 1]) && text[i + 1] != '-' && text[i + 1] != SoftBreak)
            {
                // Insert soft break opportunity for unbroken continuous token runs
                sb.Append(SoftBreak);
                chunkLen = 0;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Strips any injected zero-width spaces to recover the original raw text.
    /// </summary>
    public static string StripSoftBreaks(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Replace(SoftBreak.ToString(), string.Empty);
    }
}
