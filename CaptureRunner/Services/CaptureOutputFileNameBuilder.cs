using System.Security.Cryptography;
using System.Text;
using CaptureRunner.Models;

namespace CaptureRunner.Services;

public static class CaptureOutputFileNameBuilder
{
    private const int MaxBaseNameLength = 80;
    private const int MaxSegmentLength = 24;
    private const int HashLength = 10;

    public static string Build(ScreenPlan plan, string extension)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var normalizedExtension = NormalizeExtension(extension);
        var rowId = Sanitize(plan.RowId);
        var screenName = Sanitize(plan.ScreenName);
        var legacyBaseName = JoinParts(rowId, screenName);

        if (!string.IsNullOrWhiteSpace(legacyBaseName)
            && legacyBaseName.Length + normalizedExtension.Length <= MaxBaseNameLength)
        {
            return legacyBaseName + normalizedExtension;
        }

        var compactBaseName = JoinParts(
            Truncate(rowId, MaxSegmentLength),
            Truncate(screenName, MaxSegmentLength),
            ComputeHash($"{plan.RowId}|{plan.ScreenName}"));

        if (string.IsNullOrWhiteSpace(compactBaseName))
        {
            compactBaseName = ComputeHash($"{plan.RowId}|{plan.ScreenName}|capture");
        }

        if (compactBaseName.Length + normalizedExtension.Length > MaxBaseNameLength)
        {
            compactBaseName = compactBaseName[..Math.Max(1, MaxBaseNameLength - normalizedExtension.Length)].Trim('-');
        }

        return compactBaseName + normalizedExtension;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".png";
        }

        return extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : "." + extension;
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var characters = value
            .Trim()
            .Select(ch => invalid.Contains(ch) || char.IsWhiteSpace(ch) ? '-' : char.ToLowerInvariant(ch))
            .ToArray();

        return new string(characters).Trim('-');
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength].Trim('-');
    }

    private static string ComputeHash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant()[..HashLength];
    }

    private static string JoinParts(params string[] parts)
    {
        return string.Join("-", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
