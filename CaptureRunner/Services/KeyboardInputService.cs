using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CaptureRunner.Services;

internal static class KeyboardInputService
{
    private const uint KeyEventFKeyUp = 0x0002;

    public static void SendHotkey(string shortcut)
    {
        var stroke = ParseShortcut(shortcut);
        if (stroke.Key == Keys.None)
        {
            throw new InvalidOperationException($"Shortcut '{shortcut}' does not contain a valid key.");
        }

        foreach (var modifier in stroke.Modifiers)
        {
            keybd_event((byte)modifier, 0, 0, 0);
        }

        keybd_event((byte)stroke.Key, 0, 0, 0);
        Thread.Sleep(50);
        keybd_event((byte)stroke.Key, 0, KeyEventFKeyUp, 0);

        for (var index = stroke.Modifiers.Count - 1; index >= 0; index--)
        {
            keybd_event((byte)stroke.Modifiers[index], 0, KeyEventFKeyUp, 0);
        }

        Thread.Sleep(250);
    }

    private static ShortcutStroke ParseShortcut(string shortcut)
    {
        var parts = shortcut
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (parts.Count == 0)
        {
            return new ShortcutStroke(Keys.None, new List<Keys>());
        }

        var modifiers = new List<Keys>();
        foreach (var part in parts.Take(parts.Count - 1))
        {
            switch (part.Trim().ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers.Add(Keys.ControlKey);
                    break;
                case "SHIFT":
                    modifiers.Add(Keys.ShiftKey);
                    break;
                case "ALT":
                    modifiers.Add(Keys.Menu);
                    break;
            }
        }

        return new ShortcutStroke(ParseKey(parts[^1]), modifiers);
    }

    private static Keys ParseKey(string rawKey)
    {
        var key = rawKey.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(key))
        {
            return Keys.None;
        }

        if (key.Length == 1)
        {
            if (char.IsLetter(key[0]))
            {
                return (Keys)Enum.Parse(typeof(Keys), key);
            }

            if (char.IsDigit(key[0]))
            {
                return (Keys)Enum.Parse(typeof(Keys), $"D{key}");
            }
        }

        return key switch
        {
            "ENTER" => Keys.Enter,
            "ESC" or "ESCAPE" => Keys.Escape,
            "TAB" => Keys.Tab,
            "UP" => Keys.Up,
            "DOWN" => Keys.Down,
            "LEFT" => Keys.Left,
            "RIGHT" => Keys.Right,
            _ when Enum.TryParse<Keys>(key, ignoreCase: true, out var parsed) => parsed,
            _ => Keys.None
        };
    }

    private readonly record struct ShortcutStroke(Keys Key, IReadOnlyList<Keys> Modifiers);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);
}
