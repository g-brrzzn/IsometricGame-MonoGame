using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using System.Diagnostics;

namespace IsometricGame.States
{
    public class MenuState : GameStateBase
    {
        private List<string> _options = new List<string> { "START", "EDITOR", "OPTIONS", "EXIT" };
        private int _selected = 0;
        private float _titleOffsetY;

        public override void Start()
        {
            base.Start();
            _selected = 0;
            Debug.WriteLine("MenuState Started.");

            GameEngine.ResetGame();
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            _titleOffsetY = (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2 * Math.PI) * (Constants.InternalResolution.Y * 0.04));

            if (input.IsKeyPressed("DOWN"))
            {
                _selected = (_selected + 1) % _options.Count;
                GameEngine.Assets.Sounds["menu_select"]?.Play();
            }
            if (input.IsKeyPressed("UP"))
            {
                _selected = (_selected - 1 + _options.Count) % _options.Count;
                GameEngine.Assets.Sounds["menu_select"]?.Play();
            }

            if (input.IsKeyPressed("START"))
            {
                GameEngine.Assets.Sounds["menu_confirm"]?.Play();
                IsDone = true;

                switch (_options[_selected])
                {
                    case "START":
                        NextState = "Game";
                        break;
                    case "EDITOR":
                        NextState = "Editor";
                        break;
                    case "OPTIONS":
                        NextState = "Options";
                        break;
                    case "EXIT":
                        NextState = "Exit";
                        break;
                    default:
                        NextState = "Menu";
                        break;
                }
            }
            if (input.IsKeyPressed("ESC"))
            {
                IsDone = true;
                NextState = "Exit";
            }
        }
        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            Vector2 titlePosScreen = new Vector2(Constants.InternalResolution.X / 2f, Constants.InternalResolution.Y * 0.33f + _titleOffsetY);
            Vector2 titlePosWorld = Game1.Camera.ScreenToWorld(titlePosScreen);
            DrawUtils.DrawText(spriteBatch, "Isometric Game Base", GameEngine.Assets.Fonts["captain_80"], titlePosWorld, Constants.TitleYellow1, 1.0f);
            DrawUtils.DrawMenu(spriteBatch, _options, "", _selected);
        }
    }
}