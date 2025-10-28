using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;

namespace IsometricGame.States
{
    public class PauseState : GameStateBase
    {
        private List<string> _options = new List<string> { "CONTINUE", "EXIT" };
        private int _selected = 0;

        public override void Start()
        {
            base.Start();
            _selected = 0;
            if (GameEngine.Player != null && !GameEngine.Player.IsRemoved)            {
                Game1.Camera.Follow(GameEngine.Player.ScreenPosition);
            }
            else
            {
                Game1.Camera.Follow(Vector2.Zero);
                Debug.WriteLine("PauseState.Start: Player is null or removed. Camera following origin.");

            }
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            if (input.IsKeyPressed("DOWN"))
            {
                _selected = (_selected + 1) % _options.Count;
                GameEngine.Assets.Sounds["menu_select"].Play();
            }
            if (input.IsKeyPressed("UP"))
            {
                _selected = (_selected - 1 + _options.Count) % _options.Count;
                GameEngine.Assets.Sounds["menu_select"].Play();
            }

            if (input.IsKeyPressed("ESC"))
            {
                IsDone = true;
                NextState = "Game";
            }

            if (input.IsKeyPressed("START"))
            {
                GameEngine.Assets.Sounds["menu_confirm"].Play();
                IsDone = true;
                if (_selected == 0)
                {
                    NextState = "Game";
                }
                else if (_selected == 1)
                {
                    NextState = "ExitConfirm";
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            DrawUtils.DrawMenu(spriteBatch, _options, "PAUSED", _selected);
        }
    }
}