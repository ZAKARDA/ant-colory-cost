using UnityEngine;
namespace Antchemy
{
    // Ant Colony Colors' own palette: 24 curated hues designed in-house. Index = ColorIndex in level files.
    public static class ColorPalette
    {
        public const int Count = 24;
        static readonly string[] hex = {
            "FFD23F", // 0  Sunflower
            "FF9F1C", // 1  Tangerine
            "FF6B35", // 2  Ember
            "EF476F", // 3  Raspberry
            "B5174B", // 4  Garnet
            "9BDE5B", // 5  Lime
            "2EC4B6", // 6  Lagoon
            "14825F", // 7  Emerald
            "A8E1F0", // 8  Sky
            "3FB8F5", // 9  Azure
            "FFB8D9", // 10 Blush
            "F25FC0", // 11 Orchid
            "A63CFF", // 12 Violet
            "4C4FC7", // 13 Indigo
            "9A5A3C", // 14 Cocoa
            "E9EEF0", // 15 Pearl
            "3B3F5C", // 16 Slate
            "F2C9A6", // 17 Sand
            "1F78D1", // 18 Cobalt
            "D98A3D", // 19 Caramel
            "E63946", // 20 Cherry
            "F7E8A4", // 21 Butter
            "8F8FE0", // 22 Lavender
            "7AD3A2", // 23 Mint
        };
        static readonly Color[] baseColors = Build(1f);
        static readonly Color[] colonyColors = Build(.88f);
        static Color[] Build(float shade)
        {
            var list = new Color[hex.Length];
            for (int i = 0; i < hex.Length; i++)
            {
                ColorUtility.TryParseHtmlString("#" + hex[i], out var c);
                list[i] = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
            }
            return list;
        }
        public static Color GetColor(int index) => index >= 0 && index < baseColors.Length ? baseColors[index] : Color.magenta;
        public static Color GetColonyColor(int index) => index >= 0 && index < colonyColors.Length ? colonyColors[index] : Color.magenta;
        public static Color GetAntColor(int index) => GetColor(index);
    }
}
