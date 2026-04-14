namespace CaptureRunner.Services;

internal static class ShortcutEncoding
{
    public static string ToSendKeys(string shortcut)
    {
        var parts = shortcut
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (parts.Count == 0)
        {
            return shortcut;
        }

        var modifiers = string.Concat(parts.Take(parts.Count - 1).Select(ConvertModifier));
        var key = ConvertKey(parts[^1]);
        return $"{modifiers}{key}";
    }

    private static string ConvertModifier(string modifier)
    {
        return modifier.ToUpperInvariant() switch
        {
            "ALT" => "%",
            "CTRL" => "^",
            "CONTROL" => "^",
            "SHIFT" => "+",
            _ => string.Empty
        };
    }

    private static string ConvertKey(string key)
    {
        return key.ToUpperInvariant() switch
        {
            "ENTER" => "{ENTER}",
            "ESC" or "ESCAPE" => "{ESC}",
            "TAB" => "{TAB}",
            "UP" => "{UP}",
            "DOWN" => "{DOWN}",
            "LEFT" => "{LEFT}",
            "RIGHT" => "{RIGHT}",
            _ when key.Length == 1 => key,
            _ when key.StartsWith("F", StringComparison.OrdinalIgnoreCase) => $"{{{key.ToUpperInvariant()}}}",
            _ => $"{{{key.ToUpperInvariant()}}}"
        };
    }
}
