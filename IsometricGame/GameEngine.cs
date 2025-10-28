
using System;
using System.Collections.Generic;
using IsometricGame.Classes;
using Microsoft.Xna.Framework;

namespace IsometricGame
{
    public static class GameEngine
    {
        public static Player Player { get; set; }

        public static Vector2 TargetWorldPosition { get; set; }
        public static Vector2 CursorScreenPosition { get; set; }

        public static List<Sprite> AllSprites { get; private set; }
        public static List<EnemyBase> AllEnemies { get; private set; }
        public static List<Bullet> PlayerBullets { get; private set; }
        public static List<Bullet> EnemyBullets { get; private set; }

        public static Dictionary<Vector3, Sprite> SolidTiles { get; private set; }

        public static AssetManager Assets { get; set; }
        public static int Level { get; set; }
        public static int ScreenShake { get; set; }

        public static Random Random { get; private set; }
        public static Action<string> OnTextInputComplete { get; set; }
        public static string TextInputPrompt { get; set; } = "Enter Text:";
        public static string TextInputDefaultValue { get; set; } = "";
        public static string TextInputReturnState { get; set; } = "Menu";


        public static void Initialize()
        {
            AllSprites = new List<Sprite>();
            AllEnemies = new List<EnemyBase>();
            PlayerBullets = new List<Bullet>();
            EnemyBullets = new List<Bullet>();
            SolidTiles = new Dictionary<Vector3, Sprite>();
            Assets = new AssetManager();
            Random = new Random();
            Level = 1;
            ScreenShake = 0;
            Player = null;
            TargetWorldPosition = Vector2.Zero;
            CursorScreenPosition = Vector2.Zero;
            Level = 1;
            ScreenShake = 0;
            OnTextInputComplete = null;
        }

        public static void ResetGame()
        {
            AllSprites.Clear();
            AllEnemies.Clear();
            PlayerBullets.Clear();
            EnemyBullets.Clear();
            SolidTiles.Clear();
            Player = null;
            TargetWorldPosition = Vector2.Zero;
            CursorScreenPosition = Vector2.Zero;
            Level = 1;
        }
    }
}