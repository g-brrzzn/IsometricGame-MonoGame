using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace IsometricGame
{
    public static class Constants
    {
        public static Point InternalResolution = new Point(1600, 900);
        public static readonly Point[] Resolutions = new Point[]
        {
            new Point(960, 540),
            new Point(1024, 768),
            new Point(1280, 720),
            new Point(1920, 1080),
            new Point(2560, 1440),
            new Point(3840, 2160)
        };
        public static bool ShowFPS = false;
        public static bool SetFullscreen = false;
        public static Point WindowSize = Resolutions[3];
        public const int FrameRate = 75;
        public const int MaxLife = 3;
        public const float BaseSpeedMultiplier = 2.0f;
        public static Point IsoTileSize = new Point(36, 16);
        public static float TileHeightFactor = 8f;
        public const int MaxZLevel = 10;
        public static Point WorldSize = new Point(100, 100);
        public static Color BackgroundColor = new Color(15, 25, 27, 200);
        public static Color BackgroundColorGame1 = new Color(15, 25, 27, 255);
        public static Color BackgroundColorGame2 = new Color(15, 25, 27, 255);
        public static Color BackgroundColorMenu1 = new Color(15, 25, 27, 20);
        public static Color BackgroundColorMenu2 = new Color(15, 25, 27, 20);

        public static Color GameColor = new Color(100, 40, 80);
        public static Color TitleYellow1 = new Color(221, 245, 154);
        public static Color TitleYellow2 = new Color(185, 174, 115);
        public static Color PlayerColorGreen = new Color(28, 162, 111);
    }
}