using UnityEngine;

namespace ThisIsBlast.Data
{
    public enum BlockColor
    {
        None = 0,
        Red = 1,
        Yellow = 2,
        Blue = 3,
        Green = 4,
        Orange = 5,
        Purple = 6,
        Pink = 7,
        Cyan = 8
    }

    public static class BlockColorPalette
    {
        public static readonly Color RedColor = new Color(0.96f, 0.28f, 0.32f);       // Vibrant punchy red
        public static readonly Color YellowColor = new Color(1.0f, 0.78f, 0.12f);     // Warm sunshine yellow
        public static readonly Color BlueColor = new Color(0.18f, 0.58f, 0.98f);      // Bright electric blue
        public static readonly Color GreenColor = new Color(0.20f, 0.82f, 0.40f);     // Fresh emerald green
        public static readonly Color OrangeColor = new Color(1.0f, 0.52f, 0.15f);    // Juicy orange
        public static readonly Color PurpleColor = new Color(0.68f, 0.30f, 0.95f);    // Royal purple
        public static readonly Color PinkColor = new Color(1.0f, 0.32f, 0.65f);       // Hot bubblegum pink
        public static readonly Color CyanColor = new Color(0.12f, 0.85f, 0.92f);      // Vibrant turquoise/cyan
        public static readonly Color NeutralCover = new Color(0.35f, 0.42f, 0.50f);   // Gray-blue shell for hidden blocks
        public static readonly Color ChestGold = new Color(0.95f, 0.72f, 0.15f);      // Golden treasure
        public static readonly Color PiggyPink = new Color(1.0f, 0.45f, 0.65f);       // Soft cute piggy pink
        public static readonly Color KeySilver = new Color(0.85f, 0.88f, 0.92f);      // Metallic key
        public static readonly Color LockColor = new Color(0.25f, 0.28f, 0.33f);      // Dark metallic padlock

        public static readonly BlockColor[] All8Colors = new BlockColor[]
        {
            BlockColor.Red,
            BlockColor.Yellow,
            BlockColor.Blue,
            BlockColor.Green,
            BlockColor.Orange,
            BlockColor.Purple,
            BlockColor.Pink,
            BlockColor.Cyan
        };

        public static Color GetColor(BlockColor color)
        {
            switch (color)
            {
                case BlockColor.Red: return RedColor;
                case BlockColor.Yellow: return YellowColor;
                case BlockColor.Blue: return BlueColor;
                case BlockColor.Green: return GreenColor;
                case BlockColor.Orange: return OrangeColor;
                case BlockColor.Purple: return PurpleColor;
                case BlockColor.Pink: return PinkColor;
                case BlockColor.Cyan: return CyanColor;
                default: return Color.gray;
            }
        }

        public static string GetHex(BlockColor color)
        {
            return ColorUtility.ToHtmlStringRGB(GetColor(color));
        }
    }
}
