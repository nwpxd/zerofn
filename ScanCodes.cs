using System.Collections.Generic;
using System.Linq;

namespace ZeroFN;

public static class ScanCodes
{
    public static readonly Dictionary<string, ushort> Map = new()
    {
        // Letters
        ["A"] = 0x1E, ["B"] = 0x30, ["C"] = 0x2E, ["D"] = 0x20,
        ["E"] = 0x12, ["F"] = 0x21, ["G"] = 0x22, ["H"] = 0x23,
        ["I"] = 0x17, ["J"] = 0x24, ["K"] = 0x25, ["L"] = 0x26,
        ["M"] = 0x32, ["N"] = 0x31, ["O"] = 0x18, ["P"] = 0x19,
        ["Q"] = 0x10, ["R"] = 0x13, ["S"] = 0x1F, ["T"] = 0x14,
        ["U"] = 0x16, ["V"] = 0x2F, ["W"] = 0x11, ["X"] = 0x2D,
        ["Y"] = 0x15, ["Z"] = 0x2C,

        // Numbers
        ["1"] = 0x02, ["2"] = 0x03, ["3"] = 0x04, ["4"] = 0x05,
        ["5"] = 0x06, ["6"] = 0x07, ["7"] = 0x08, ["8"] = 0x09,
        ["9"] = 0x0A, ["0"] = 0x0B,

        // Function keys
        ["F1"] = 0x3B, ["F2"] = 0x3C, ["F3"] = 0x3D, ["F4"] = 0x3E,
        ["F5"] = 0x3F, ["F6"] = 0x40, ["F7"] = 0x41, ["F8"] = 0x42,
        ["F9"] = 0x43, ["F10"] = 0x44, ["F11"] = 0x57, ["F12"] = 0x58,

        // Special keys
        ["Escape"] = 0x01, ["Tab"] = 0x0F, ["CapsLock"] = 0x3A,
        ["LeftShift"] = 0x2A, ["RightShift"] = 0x36,
        ["LeftCtrl"] = 0x1D, ["LeftAlt"] = 0x38,
        ["Space"] = 0x39, ["Enter"] = 0x1C, ["Backspace"] = 0x0E,

        // Symbols
        ["Minus"] = 0x0C, ["Equals"] = 0x0D,
        ["LeftBracket"] = 0x1A, ["RightBracket"] = 0x1B,
        ["Semicolon"] = 0x27, ["Apostrophe"] = 0x28,
        ["Grave"] = 0x29, ["Backslash"] = 0x2B,
        ["Comma"] = 0x33, ["Period"] = 0x34, ["Slash"] = 0x35,

        // Arrow keys (E0 prefix — handled by checking E0 flag)
        ["Up"] = 0x48, ["Down"] = 0x50, ["Left"] = 0x4B, ["Right"] = 0x4D,

        // Navigation
        ["Insert"] = 0x52, ["Delete"] = 0x53, ["Home"] = 0x47,
        ["End"] = 0x4F, ["PageUp"] = 0x49, ["PageDown"] = 0x51,
    };

    private static readonly Dictionary<ushort, string> _reverse =
        Map.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static string? GetNameFromCode(ushort code)
    {
        return _reverse.TryGetValue(code, out var name) ? name : null;
    }
}
